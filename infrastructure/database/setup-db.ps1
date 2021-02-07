[CmdletBinding()]
param (
    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [string]$SqlServerInstance,

    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [securestring]$SaPassword,

    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [string]$AppDbName,

    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [string]$AppDbUsername,

    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [securestring]$AppDbPassword,

    [Parameter(Mandatory = $false)]
    [ValidateScript({$_ -gt 0})]
    [int]$MaximumConnectionValidationAttempts = 10
)

$ErrorActionPreference = "Stop"

# Need to install the SqlServer module if it does not already exist
$sqlServerModule = Get-Module -Name SqlServer -ListAvailable

if (-not $sqlServerModule) {
    Write-Host "PowerShell module SqlServer not found, installing..."
    Install-Module -Name SqlServer -AcceptLicense -Force
}

Import-Module SqlServer

$saCredential = New-Object -TypeName PSCredential -ArgumentList "sa", $SaPassword

# Validate that the database server can be connected to
Write-Host "Validating connection to SQL Server $($SqlServerInstance)..."

$sleepSeconds = 10
for ($connectionAttempt = 1; $connectionAttempt -le $MaximumConnectionValidationAttempts; $connectionAttempt++) {
    Get-SqlInstance -ServerInstance $SqlServerInstance -Credential $saCredential -ErrorAction SilentlyContinue -ErrorVariable validateConnectionError

    if ($validateConnectionError) {
        Write-Host "Cannot validate connection to SQL Server on attempt # $($connectionAttempt). Checking again in $($sleepSeconds) seconds..."
        Start-Sleep -Seconds $sleepSeconds
    }
    else {
        $connectionAttempt = $MaximumConnectionValidationAttempts + 1
    }
}

if ($validateConnectionError) {
    Write-Error "Cannot connect to SQL Server after $($MaximumConnectionValidationAttempts) attempts. Error: $($validateConnectionError)"
}

# Create SQL login for use by applications
Write-Host "Creating SQL Server login $($AppDbUsername)..."
$appCredential = New-Object -TypeName PSCredential -ArgumentList $AppDbUsername, $AppDbPassword
Add-SqlLogin -ServerInstance $SqlServerInstance -Credential $saCredential -LoginPSCredential $appCredential -LoginType SqlLogin -Enable -GrantConnectSql

# Create database and assign database user to the SQL login previously created
Write-Host "Creating database $($AppDbName) and database user $($AppDbUsername)..."
$query = @"
    CREATE DATABASE [$($AppDbName)];
    GO

    USE [$($AppDbName)];
    GO

    CREATE USER [$($AppDbUsername)] FOR LOGIN [$($AppDbUsername)];
    GO

    ALTER ROLE db_datareader ADD MEMBER warehouse_user;
    GO

    ALTER ROLE db_datawriter ADD MEMBER warehouse_user;
    GO
"@

Invoke-SqlCmd -ServerInstance $SqlServerInstance -Credential $saCredential -Query $query

# Populate database with database schema
Write-Host "Populating database $($AppDbName) with schema..."
$schemaFile = Join-Path -Path $PSScriptRoot -ChildPath "setup-db-schema.sql"
$dbSchema = Get-Content -Path $schemaFile -Encoding utf8 -Raw
$dbSchema = $dbSchema.Replace("INSERT_DATABASE_NAME", $AppDbName)

Invoke-Sqlcmd -ServerInstance $SqlServerInstance -Credential $saCredential -Query $dbSchema

# Test that user $AppDbUsername can access the database
Write-Host "Validating that user $($AppDbUsername) can connect to database $($AppDbName)..."
$query = "SELECT COUNT(Id) FROM Category"
Invoke-Sqlcmd -ServerInstance $SqlServerInstance -Database $AppDbName -Credential $appCredential -Query $query

Write-Host "Database setup succeeded!"
