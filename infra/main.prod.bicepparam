// ============================================================================
// main.prod.bicepparam — Production environment parameter file
//
// SKU choices are now derived inside main.bicep from environment='prod':
//   SQL     : S2 (50 DTUs, 250 GB)  ~$75/month
//   Bus     : Standard              (same tier; Premium adds Geo-DR if needed)
//   App Svc : P1v3 (2 vCPU, 8 GB)  ~$138/month — supports zone redundancy
//
// The SQL admin password is read from an environment variable.
// In CI/CD (GitHub Actions), set SQL_ADMIN_PASSWORD as a repository secret
// and expose it with:  env: { SQL_ADMIN_PASSWORD: ${{ secrets.SQL_ADMIN_PASSWORD }} }
//
// Standalone deploy (az CLI):
//   az group create --name rg-busbooking-prod --location southeastasia
//   az deployment group create \
//     --resource-group rg-busbooking-prod \
//     --template-file main.bicep \
//     --parameters main.prod.bicepparam
//
// azd deploy:
//   azd up --environment prod
// ============================================================================

using 'main.bicep'

param appName          = 'busbooking'
param location         = 'southeastasia'
param environment      = 'prod'

param sqlAdminLogin         = 'sqladmin'
param sqlAdminPassword      = readEnvironmentVariable('SQL_ADMIN_PASSWORD')
param tenantId              = readEnvironmentVariable('AZURE_TENANT_ID', '')
param sqlAdminPrincipalId   = readEnvironmentVariable('AZURE_PRINCIPAL_ID', '')
param sqlAdminPrincipalName = readEnvironmentVariable('AZURE_PRINCIPAL_NAME', '')
param aadClientId           = readEnvironmentVariable('AAD_CLIENT_ID', '')
