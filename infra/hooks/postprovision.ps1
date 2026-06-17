# postprovision.ps1 — creates the SQL contained database user for the App Service MI
#
# Why this script exists:
#   "CREATE USER [...] FROM EXTERNAL PROVIDER" cannot be done in Bicep/ARM.
#   It requires an Azure AD-authenticated SQL connection. This hook runs
#   automatically after `azd provision` completes, at which point:
#     - The App Service MI exists (Bicep created it)
#     - The deploying user is the SQL Azure AD admin (Bicep set this)
#     - The AZURE_* env vars are populated by azd
#
# The script is idempotent — it checks whether the user already exists
# before running CREATE USER, so re-running azd provision is safe.

$ErrorActionPreference = 'Stop'

Write-Host "==> postprovision: creating SQL contained user for App Service MI"

$appServiceName = azd env get-value appServiceName 2>$null
$sqlServerFqdn  = azd env get-value sqlServerFqdn 2>$null
$sqlDatabase    = azd env get-value sqlDatabaseName 2>$null

if (-not $appServiceName -or -not $sqlServerFqdn -or -not $sqlDatabase) {
    Write-Warning "One or more azd outputs are empty. Skipping SQL user creation."
    Write-Host "  appServiceName : $appServiceName"
    Write-Host "  sqlServerFqdn  : $sqlServerFqdn"
    Write-Host "  sqlDatabase    : $sqlDatabase"
    exit 0
}

Write-Host "    App Service : $appServiceName"
Write-Host "    SQL Server  : $sqlServerFqdn"
Write-Host "    Database    : $sqlDatabase"

# Check sqlcmd is available (go-sqlcmd or mssql-tools)
if (-not (Get-Command sqlcmd -ErrorAction SilentlyContinue)) {
    Write-Warning "sqlcmd not found. Skipping SQL user creation."
    Write-Host "  Install go-sqlcmd: winget install sqlcmd"
    Write-Host "  Then run manually:"
    Write-Host "    sqlcmd -S $sqlServerFqdn -d $sqlDatabase --authentication-method ActiveDirectoryDefault -Q `"CREATE USER [$appServiceName] FROM EXTERNAL PROVIDER`""
    exit 0
}

$sql = @"
IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = N'$appServiceName')
BEGIN
  CREATE USER [$appServiceName] FROM EXTERNAL PROVIDER;
  PRINT 'Created contained user: $appServiceName';
END
ELSE
BEGIN
  PRINT 'Contained user already exists: $appServiceName';
END
-- Roles are idempotent: ADD MEMBER is a no-op if already a member
ALTER ROLE db_datareader ADD MEMBER [$appServiceName];
ALTER ROLE db_datawriter ADD MEMBER [$appServiceName];
PRINT 'Roles confirmed: db_datareader + db_datawriter';
"@

sqlcmd -S $sqlServerFqdn -d $sqlDatabase --authentication-method ActiveDirectoryDefault -Q $sql

Write-Host "==> postprovision: SQL user creation complete"
