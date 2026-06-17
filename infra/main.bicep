// ============================================================================
// main.bicep — BusBooking IaC orchestrator
//
// Composes four child modules into a complete environment:
//   sql        → Azure SQL Server + Database + Azure AD admin
//   api        → Log Analytics + App Insights + App Service Plan + Web App (MI)
//   serviceBus → Service Bus Namespace + topics + MI role assignment
//   keyVault   → Key Vault + App Insights secret + MI Secrets User role
//
// Day 25 changes:
//   - Removed sqlConnectionString / serviceBusConnectionString from api params.
//     Secrets are no longer passed between modules.
//   - Added sqlAdminPrincipalId / sqlAdminPrincipalName / tenantId / aadClientId
//     params — injected by azd from AZURE_PRINCIPAL_ID, AZURE_TENANT_ID, AAD_CLIENT_ID.
//   - Module ordering: api must run first (produces managedIdentityPrincipalId),
//     then serviceBus and keyVault consume it for RBAC assignments.
//   - Added keyVault module.
//
// azd deploy:
//   azd up --environment dev
//   azd up --environment prod
// ============================================================================

targetScope = 'resourceGroup'

// ── Parameters ────────────────────────────────────────────────────────────────

@description('Short application name. Max 20 chars.')
@maxLength(20)
param appName string

@description('Azure region.')
param location string = resourceGroup().location

@description('Deployment environment.')
@allowed(['dev', 'prod'])
param environment string

@description('SQL Server administrator login name.')
@minLength(1)
param sqlAdminLogin string

@secure()
@description('SQL Server administrator password — used only for EF migrations; never injected into App Service.')
@minLength(12)
param sqlAdminPassword string

@description('Object ID of the deploying Azure AD user — set as SQL Azure AD admin.')
param sqlAdminPrincipalId string

@description('UPN of the deploying Azure AD user — set as SQL Azure AD admin.')
param sqlAdminPrincipalName string

@description('Azure AD tenant ID — used for JWT validation in the API.')
param tenantId string

@description('Entra ID application (client) ID of the BusBooking API app registration.')
param aadClientId string

// ── Environment-driven SKU derivation ─────────────────────────────────────────

var sqlSkuName        = environment == 'prod' ? 'S2'   : 'Basic'
var sqlCapacity       = environment == 'prod' ? 50     : 5
var serviceBusSku     = 'Standard'
var appServicePlanSku = environment == 'prod' ? 'B2'   : 'B1'

// ── Pre-computed resource names ───────────────────────────────────────────────
// Both servicebus.bicep and keyvault.bicep derive their names with the same
// uniqueString formula. Computing them here lets api.bicep receive the SB
// hostname and KV name without depending on module outputs — which would
// create a cycle (api → serviceBus/keyVault AND serviceBus/keyVault → api).
var suffix = take(uniqueString(resourceGroup().id), 6)
var sbFqdn = 'sb-${appName}-${environment}-${suffix}.servicebus.windows.net'
var kvName = 'kv-${appName}-${environment}-${suffix}'

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
    sqlAdminPrincipalId: sqlAdminPrincipalId
    sqlAdminPrincipalName: sqlAdminPrincipalName
  }
}

// api runs before serviceBus and keyVault because it produces
// managedIdentityPrincipalId which both downstream modules need for RBAC.
module api 'modules/api.bicep' = {
  name: 'deploy-api-${environment}'
  params: {
    appName: appName
    location: location
    environment: environment
    sqlServerFqdn: sql.outputs.serverFqdn
    sqlDatabaseName: sql.outputs.databaseName
    serviceBusNamespace: sbFqdn
    keyVaultName: kvName
    tenantId: tenantId
    aadClientId: aadClientId
    appServicePlanSku: appServicePlanSku
  }
}

module serviceBus 'modules/servicebus.bicep' = {
  name: 'deploy-servicebus-${environment}'
  params: {
    appName: appName
    location: location
    environment: environment
    sku: serviceBusSku
    webAppPrincipalId: api.outputs.managedIdentityPrincipalId
  }
}

module keyVault 'modules/keyvault.bicep' = {
  name: 'deploy-keyvault-${environment}'
  params: {
    appName: appName
    location: location
    environment: environment
    appInsightsConnectionString: api.outputs.appInsightsConnectionString
    webAppPrincipalId: api.outputs.managedIdentityPrincipalId
  }
}

// ── Outputs ───────────────────────────────────────────────────────────────────

@description('Full HTTPS URL of the deployed API.')
output apiUrl string = 'https://${api.outputs.hostName}'

@description('App Service hostname.')
output apiHostName string = api.outputs.hostName

@description('App Service name — used by post-provision hook to create SQL contained user.')
output appServiceName string = api.outputs.webAppName

@description('SQL Server FQDN.')
output sqlServerFqdn string = sql.outputs.serverFqdn

@description('SQL Database name.')
output sqlDatabaseName string = sql.outputs.databaseName

@description('Key Vault name.')
output keyVaultName string = kvName
