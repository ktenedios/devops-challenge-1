[CmdletBinding()]
param (
    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [string]$Environment,

    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [string]$FileProcessorContainerInstanceName,

    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [string]$FileProcessorStorageAccountName,

    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [string]$ImageRegistryUsername,

    [Parameter(Mandatory = $true)]
    [securestring]$ImageRegistryPassword,

    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [string]$WarehouseDbContainerInstanceName,

    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [string]$WarehouseDbContainerDnsName,

    [Parameter(Mandatory = $true)]
    [securestring]$SqlServerSaPassword,

    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [string]$FileProcessorAppDbUsername,

    [Parameter(Mandatory = $true)]
    [securestring]$FileProcessorAppDbPassword,

    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [string]$DeployToResourceGroupName,

    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [string]$DeploymentsStorageAccountResourceGroupName,

    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [string]$DeploymentsStorageAccountName,

    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [string]$DeploymentsStorageAccountContainer
)

$modulePath = Join-Path -Path $PSScriptRoot -ChildPath "secure-string-handler.psm1"
Import-Module -Name $modulePath -Force

# Ensure that if there is an error that the deployment stops
$ErrorActionPreference = "Stop"

# Ensure that the resource group that we want to deploy to exists.
# Hard-coding location to Australia East.
$deployToResourceGroup = Get-AzResourceGroup -Name $DeployToResourceGroupName -ErrorAction SilentlyContinue

if (-not $deployToResourceGroup) {
    Write-Host "Resource group '$($DeployToResourceGroupName)' does not exist, creating..." -ForegroundColor Green
    New-AzResourceGroup -Name $DeployToResourceGroupName -Location "Australia East"
}

# Upload the nested ARM templates to the required storage account
Write-Host "Uploading nested ARM templates to storage account $($DeploymentsStorageAccountName)..." -ForegroundColor Green
$deploymentsStorageAccountRootFolder = [Guid]::NewGuid().ToString("d")
$armTemplateFiles = Get-ChildItem -Path $PSScriptRoot -Filter *.json -Exclude "stack.json" -Recurse
$deploymentsStorageAccount = 
    Get-AzStorageAccount -ResourceGroupName $DeploymentsStorageAccountResourceGroupName -Name $DeploymentsStorageAccountName

$armTemplateFiles | ForEach-Object {
    $uploadToPath = "$($deploymentsStorageAccountRootFolder)/$($_.FullName.Replace(($PSScriptRoot + '/'), [string]::Empty))"
    Set-AzStorageBlobContent -File $_.FullName -Container $DeploymentsStorageAccountContainer `
        -Blob $uploadToPath -Context $deploymentsStorageAccount.Context
}

$deploymentStorageAccountSasToken = New-AzStorageAccountSASToken -Service Blob -ResourceType Service,Container,Object `
    -Permission "racwdlup" -Context $deploymentsStorageAccount.Context

# Prepare dictionary of parameters to be passed to the main ARM template
$stackDeployParams = @{
    "environment" = $Environment;
    "fileProcessorContainerInstanceName" = $FileProcessorContainerInstanceName;
    "fileProcessorStorageAccountName" = $FileProcessorStorageAccountName;
    "imageRegistryUsername" = $ImageRegistryUsername;
    "imageRegistryPassword" = Convert-SecureStringToAzureSecureString -SecureStringToConvert $ImageRegistryPassword;
    "warehouseDbContainerInstanceName" = $WarehouseDbContainerInstanceName;
    "warehouseDbContainerDnsName" = $WarehouseDbContainerDnsName;
    "sqlServerSaPassword" = Convert-SecureStringToAzureSecureString -SecureStringToConvert $SqlServerSaPassword;
    "fileProcessorAppDbUsername" = $FileProcessorAppDbUsername;
    "fileProcessorAppDbPassword" = Convert-SecureStringToAzureSecureString -SecureStringToConvert $FileProcessorAppDbPassword;
    "deploymentStorageAccountRootFolder" = $deploymentsStorageAccountRootFolder;
    "deploymentStorageAccountSasToken" = $deploymentStorageAccountSasToken
}

# Provision the infrastructure and deploy the application stack using the main ARM template
Write-Host "Provisioning environment '$($Environment)' and deploying application stack..." -ForegroundColor Green
$deploymentName = "warehouse-$($Environment)-$((Get-Date).ToString('yyyyMMddHHmmss'))"
$mainTemplateFile = Join-Path -Path $PSScriptRoot -ChildPath "stack.json"
$stackDeployResult = New-AzResourceGroupDeployment -ResourceGroupName $DeployToResourceGroupName -Name $deploymentName `
    -TemplateFile $mainTemplateFile -TemplateParameterObject $stackDeployParams

# Need to check that the SQL Server in the database container has started
Write-Host "Checking that SQL Server in container instance $($deployedDbContainerInstanceName) has started..." -ForegroundColor Green
$deployedDbContainerInstanceName = $stackDeployResult.Outputs.databaseContainerInstanceName.value
$deployedDbContainerName = $stackDeployResult.Outputs.databaseContainerName.value
$endTime = (Get-Date).AddMinutes(5)
$currentTime = Get-Date
$serverStarted = $false

while ($currentTime -le $endTime -and -not $serverStarted) {
    $logs = Get-AzContainerInstanceLog -ResourceGroupName $DeployToResourceGroupName `
        -ContainerGroupName $deployedDbContainerInstanceName -Name $deployedDbContainerName -Tail 100

    if ($logs.Contains("SQL Server is now ready for client connections")) {
        $serverStarted = $true
    }
    else {
        Start-Sleep -Seconds 10
        $currentTime = Get-Date
    }
}

if (-not $serverStarted) {
    Write-Error "SQL Server in container instance $($deployedDbContainerInstanceName) failed to start. Deployment has failed."
}

# Deploy the database schema and provision the login to be used by the application
Write-Host "Deploying database schema and setting up application login in SQL Server container instance $($deployedDbContainerInstanceName)..." -ForegroundColor Green
$sqlServerFqdn = $stackDeployResult.Outputs.databaseContainerIPAddressFqdn.value
$setupScriptFile = Join-Path -Path $PSScriptRoot -ChildPath "database/setup-db.ps1"
& $setupScriptFile -SqlServerInstance $sqlServerFqdn -SaPassword $SqlServerSaPassword `
    -AppDbName warehousedb -AppDbUsername $FileProcessorAppDbUsername -AppDbPassword $FileProcessorAppDbPassword

Write-Host "Environment '$($Environment)' successfully provisioned!" -ForegroundColor Green
