#!/usr/bin/env bash
# postprovision.sh — creates the SQL contained database user for the App Service MI
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

set -euo pipefail

echo "==> postprovision: creating SQL contained user for App Service MI"

# azd populates these from the provision outputs
APP_SERVICE_NAME=$(azd env get-value appServiceName 2>/dev/null || echo "")
SQL_SERVER_FQDN=$(azd env get-value sqlServerFqdn 2>/dev/null || echo "")
SQL_DATABASE=$(azd env get-value sqlDatabaseName 2>/dev/null || echo "")

if [[ -z "$APP_SERVICE_NAME" || -z "$SQL_SERVER_FQDN" || -z "$SQL_DATABASE" ]]; then
  echo "==> WARNING: One or more azd outputs are empty. Skipping SQL user creation."
  echo "    APP_SERVICE_NAME=${APP_SERVICE_NAME}"
  echo "    SQL_SERVER_FQDN=${SQL_SERVER_FQDN}"
  echo "    SQL_DATABASE=${SQL_DATABASE}"
  exit 0
fi

echo "    App Service : ${APP_SERVICE_NAME}"
echo "    SQL Server  : ${SQL_SERVER_FQDN}"
echo "    Database    : ${SQL_DATABASE}"

# Check sqlcmd is available (comes with mssql-tools or sqlcmd standalone)
if ! command -v sqlcmd &>/dev/null; then
  echo "==> WARNING: sqlcmd not found. Skipping SQL user creation."
  echo "    Run this manually after install:"
  echo "    sqlcmd -S ${SQL_SERVER_FQDN} -d ${SQL_DATABASE} \\"
  echo "      --authentication-method ActiveDirectoryDefault \\"
  echo "      -Q \"IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = N'${APP_SERVICE_NAME}')"
  echo "         BEGIN"
  echo "           CREATE USER [${APP_SERVICE_NAME}] FROM EXTERNAL PROVIDER;"
  echo "           ALTER ROLE db_datareader ADD MEMBER [${APP_SERVICE_NAME}];"
  echo "           ALTER ROLE db_datawriter ADD MEMBER [${APP_SERVICE_NAME}];"
  echo "         END\""
  exit 0
fi

# Connect using the deploying user's Azure AD credentials (DefaultAzureDefault
# picks up the 'az login' / 'azd auth login' session automatically).
sqlcmd \
  -S "${SQL_SERVER_FQDN}" \
  -d "${SQL_DATABASE}" \
  --authentication-method ActiveDirectoryDefault \
  -Q "
IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = N'${APP_SERVICE_NAME}')
BEGIN
  CREATE USER [${APP_SERVICE_NAME}] FROM EXTERNAL PROVIDER;
  ALTER ROLE db_datareader ADD MEMBER [${APP_SERVICE_NAME}];
  ALTER ROLE db_datawriter ADD MEMBER [${APP_SERVICE_NAME}];
  PRINT 'Created contained user: ${APP_SERVICE_NAME}';
END
ELSE
BEGIN
  PRINT 'Contained user already exists: ${APP_SERVICE_NAME}';
END
"

echo "==> postprovision: SQL user creation complete"
