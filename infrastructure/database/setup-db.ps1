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
    [string]$AppDbFolderPath,

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
    [int]$MaximumConnectionValidationAttempts = 100
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

# Check to see if the $AppDbUsername login already exists, and if not, create the login
$appCredential = New-Object -TypeName PSCredential -ArgumentList $AppDbUsername, $AppDbPassword
Get-SqlLogin -ServerInstance $SqlServerInstance -Credential $saCredential -LoginName $AppDbUsername -LoginType SqlLogin `
             -ErrorAction SilentlyContinue -ErrorVariable getSqlLoginError

if ($getSqlLoginError) {
    if ($getSqlLoginError[0].Exception.Message -eq $AppDbUsername) {
        Write-Host "Creating SQL Server login $($AppDbUsername)..."
        Add-SqlLogin -ServerInstance $SqlServerInstance -Credential $saCredential -LoginPSCredential $appCredential `
                     -LoginType SqlLogin -Enable -GrantConnectSql
    }
    else {
        Write-Error $getSqlLoginError
    }
}
else {
    Write-Host "SQL Server login $($AppDbUsername) already exists"
}

# Create database and assign database user to the SQL login previously created
Write-Host "Creating database $($AppDbName) and database user $($AppDbUsername)..."
$query = @"
    IF EXISTS (SELECT name FROM master.dbo.sysdatabases WHERE ('[' + name + ']' = '$($AppDbName)' OR name = '$($AppDbName)'))
    BEGIN
        CREATE DATABASE [$($AppDbName)]
        ON PRIMARY (NAME = N'$($AppDbName)_Data', FILENAME = '$($AppDbFolderPath)/$($AppDbName)_Data.mdf')
        LOG ON (NAME = N'$($AppDbName)_Log', FILENAME = '$($AppDbFolderPath)/$($AppDbName)_Log.ldf')
        FOR ATTACH
    ELSE
        CREATE DATABASE [$($AppDbName)]
        ON PRIMARY (NAME = N'$($AppDbName)_Data', FILENAME = '$($AppDbFolderPath)/$($AppDbName)_Data.mdf')
        LOG ON (NAME = N'$($AppDbName)_Log', FILENAME = '$($AppDbFolderPath)/$($AppDbName)_Log.ldf')
    END;
    GO

    USE [$($AppDbName)];
    GO

    IF NOT EXISTS (SELECT [name] FROM [sys].[database_principals] WHERE [type] = N'S' AND [name] = N'$($AppDbUsername)')
    BEGIN
        CREATE USER [$($AppDbUsername)] FOR LOGIN [$($AppDbUsername)]
    END;
    GO

    ALTER ROLE db_datareader ADD MEMBER [$($AppDbUsername)];
    GO

    ALTER ROLE db_datawriter ADD MEMBER [$($AppDbUsername)];
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
