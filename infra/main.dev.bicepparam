// ============================================================================
// main.dev.bicepparam — Development environment parameter file
//
// SKU choices are now derived inside main.bicep from environment='dev':
//   SQL     : Basic (5 DTUs, 2 GB)  ~$5/month
//   Bus     : Standard              ~$10/month + messaging units
//   App Svc : B1 (1 vCPU, 1.75 GB) ~$13/month
//
// The SQL admin password is read from an environment variable so that this
// file can be committed to source control without embedding secrets.
// Before deploying, set:  $env:SQL_ADMIN_PASSWORD = 'YourDevP@ssw0rd!'
//
// Standalone deploy (az CLI):
//   az group create --name rg-busbooking-dev --location southeastasia
//   az deployment group create \
//     --resource-group rg-busbooking-dev \
//     --template-file main.bicep \
//     --parameters main.dev.bicepparam
//
// azd deploy:
//   azd up --environment dev
// ============================================================================

using 'main.bicep'

param appName          = 'busbooking'
param location         = 'southeastasia'
param environment      = 'dev'

param sqlAdminLogin         = 'sqladmin'
param sqlAdminPassword      = readEnvironmentVariable('SQL_ADMIN_PASSWORD')
param tenantId              = readEnvironmentVariable('AZURE_TENANT_ID')
param sqlAdminPrincipalId   = readEnvironmentVariable('AZURE_PRINCIPAL_ID')
param sqlAdminPrincipalName = readEnvironmentVariable('AZURE_PRINCIPAL_NAME')
param aadClientId           = readEnvironmentVariable('AAD_CLIENT_ID')
