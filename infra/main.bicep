// ============================================================================
// main.bicep — BusBooking IaC orchestrator
//
// Composes three child modules into a complete environment:
//   sql        → Azure SQL Server + Database
//   serviceBus → Service Bus Namespace + topics + auth rule
//   api        → Log Analytics + App Insights + App Service Plan + Web App
//
// SKU choices are derived automatically from 'environment' — no SKU params
// needed. azd passes environment via main.parameters.json using AZURE_ENV_NAME.
//
// Standalone deploy (az CLI):
//   az deployment group create \
//     --resource-group rg-busbooking-dev \
//     --parameters main.dev.bicepparam
//
// azd deploy:
//   azd up --environment dev
//   azd up --environment prod
//
// What-if (dry run):
//   az deployment group what-if \
//     --resource-group rg-busbooking-dev \
//     --parameters main.dev.bicepparam
// ============================================================================

targetScope = 'resourceGroup'

// ── Parameters ────────────────────────────────────────────────────────────────

@description('Short application name — drives all resource names (e.g. "busbooking"). Max 20 chars.')
@maxLength(20)
param appName string

@description('Azure region. Defaults to the resource group region.')
param location string = resourceGroup().location

@description('Deployment environment. Controls naming, SKUs, and firewall rules.')
@allowed(['dev', 'prod'])
param environment string

@description('SQL Server administrator login name.')
@minLength(1)
param sqlAdminLogin string

@secure()
@description('SQL Server administrator password (≥12 chars, upper+lower+digit+special).')
@minLength(12)
param sqlAdminPassword string

// ── Environment-driven SKU derivation ─────────────────────────────────────────
// SKU choices are inlined here so azd only needs to pass 'environment'.
// This eliminates the four SKU parameters from the public surface area.
//
//   dev  → Basic SQL (5 DTU, ~$5/mo),  B1 App Service (~$13/mo)
//   prod → S2 SQL (50 DTU, ~$75/mo), P1v3 App Service (~$138/mo, zone-redundant)

var sqlSkuName        = environment == 'prod' ? 'S2'    : 'Basic'
var sqlCapacity       = environment == 'prod' ? 50      : 5
var serviceBusSku     = 'Standard'              // Standard is required for topics in both envs
// B2 instead of P1v3: Azure for Students does not include Premium App Service SKUs.
var appServicePlanSku = environment == 'prod' ? 'B2'   : 'B1'

// ── Modules ───────────────────────────────────────────────────────────────────

module sql 'modules/sql.bicep' = {
  name: 'deploy-sql-${environment}'
  params: {
    appName: appName
    location: location
    environment: environment
    adminLogin: sqlAdminLogin
    adminPassword: sqlAdminPassword
    skuName: sqlSkuName
    capacity: sqlCapacity
  }
}

module serviceBus 'modules/servicebus.bicep' = {
  name: 'deploy-servicebus-${environment}'
  params: {
    appName: appName
    location: location
    environment: environment
    sku: serviceBusSku
  }
}

// api module depends implicitly on sql and serviceBus via their outputs.
module api 'modules/api.bicep' = {
  name: 'deploy-api-${environment}'
  params: {
    appName: appName
    location: location
    environment: environment
    sqlConnectionString: sql.outputs.connectionString
    serviceBusConnectionString: serviceBus.outputs.connectionString
    appServicePlanSku: appServicePlanSku
  }
}

// ── Outputs ───────────────────────────────────────────────────────────────────

@description('Full HTTPS URL of the deployed API.')
output apiUrl string = 'https://${api.outputs.hostName}'

@description('App Service hostname (for adding to custom domain / cert).')
output apiHostName string = api.outputs.hostName

@description('SQL Server fully-qualified domain name.')
output sqlServerFqdn string = sql.outputs.serverFqdn

@description('SQL Database name.')
output sqlDatabaseName string = sql.outputs.databaseName
