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

@description('Optional override for Service Bus SKU. Leave empty to use the environment-driven default (Premium for prod, Standard for dev). Only relevant when enableServiceBus is true.')
@allowed(['', 'Standard', 'Premium'])
param serviceBusSkuOverride string = ''

@description('Optional override for SQL Database SKU. Leave empty to use the lean default (Basic/5 DTU). Set to S1/S2/S3 for a production-grade deployment.')
@allowed(['', 'Basic', 'S1', 'S2', 'S3'])
param sqlSkuOverride string = ''

@description('Optional override for App Service Plan SKU. Leave empty to use the lean default (B1). Set to B2/P1v3/P2v3 for a production-grade deployment.')
@allowed(['', 'B1', 'B2', 'P1v3', 'P2v3'])
param appServicePlanSkuOverride string = ''

@description('Deploy Azure Managed Redis (HybridCache L2 store). Off by default — optional performance cache costing ~$450+/month when on; the app degrades gracefully to an in-memory-only L1 cache when off. Turn on only for a production-grade deployment.')
param enableRedis bool = false

@description('Deploy the Service Bus namespace (async booking-confirmed/booking-cancelled events that drive confirmation/cancellation emails). Off by default; the app falls back to a no-op publisher — no crash, but those emails are skipped. Turn on to restore that feature.')
param enableServiceBus bool = false

@description('Deploy the VNet plus private endpoints/DNS zones for SQL, Key Vault, and Service Bus, and route the App Service through it. Off by default — all resources fall back to public access, still gated by firewall rules, Azure RBAC, and Entra-only auth. Turn on for network-isolated production.')
param enablePrivateNetworking bool = false

// ── SKU derivation — lean by default, overridable for a production-grade run ──

// Exhaustive literal-branch ternaries (rather than passing the override
// straight through) so Bicep infers a literal union matching each downstream
// module's own @allowed list instead of a generic string — '' or any
// unrecognized value falls through to the lean default.
var sqlSkuName = sqlSkuOverride == 'S1' ? 'S1' : sqlSkuOverride == 'S2' ? 'S2' : sqlSkuOverride == 'S3' ? 'S3' : 'Basic'
var sqlCapacityBySku  = { Basic: 5, S1: 20, S2: 50, S3: 100 }
var sqlCapacity       = sqlCapacityBySku[sqlSkuName]
// Premium required for private endpoints. Standard avoids cost and namespace
// recreation (Azure does not support in-place Standard→Premium upgrade) — only
// relevant at all when enableServiceBus is true.
var serviceBusSku     = serviceBusSkuOverride == 'Standard' ? 'Standard' : (serviceBusSkuOverride == 'Premium' ? 'Premium' : (environment == 'prod' ? 'Premium' : 'Standard'))
var appServicePlanSku = appServicePlanSkuOverride == 'B2' ? 'B2' : appServicePlanSkuOverride == 'P1v3' ? 'P1v3' : appServicePlanSkuOverride == 'P2v3' ? 'P2v3' : 'B1'

// ── Pre-computed resource names ───────────────────────────────────────────────
// Both servicebus.bicep and keyvault.bicep derive their names with the same
// uniqueString formula. Computing them here lets api.bicep receive the SB
// hostname and KV name without depending on module outputs — which would
// create a cycle (api → serviceBus/keyVault AND serviceBus/keyVault → api).
var suffix = take(uniqueString(resourceGroup().id), 6)
var sbFqdn = 'sb-${appName}-${environment}-${suffix}.servicebus.windows.net'
// Key Vault name must match keyvault.bicep's own 5-char truncation exactly —
// 'kv-<appName>-<environment>-' is already 19 chars, and Key Vault names are
// capped at 24, so keyvault.bicep truncates to 5 chars instead of the general
// 6-char suffix. Using the 6-char suffix here would produce a name that
// doesn't match the real vault, breaking the Key Vault reference below.
var kvNameSuffix = take(uniqueString(resourceGroup().id), 5)
var kvName = 'kv-${appName}-${environment}-${kvNameSuffix}'

// ── Modules ───────────────────────────────────────────────────────────────────

// vnet is opt-in (enablePrivateNetworking) — off by default for a lean deployment.
// sql, api, keyVault, and serviceBus all guard their use of its outputs with the
// same condition, which Bicep allows since the guard matches the module's own.
module vnet 'modules/vnet.bicep' = if (enablePrivateNetworking) {
  name: 'deploy-vnet-${environment}'
  params: {
    appName: appName
    location: location
    environment: environment
  }
}

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
    enablePrivateNetworking: enablePrivateNetworking
    epSubnetId: vnet.?outputs.epSubnetId ?? ''
    vnetId: vnet.?outputs.vnetId ?? ''
  }
}

// redis is opt-in (enableRedis) — off by default; the app degrades gracefully to
// an in-memory-only L1 cache. Runs before api — api needs its real hostName
// output (Azure-assigned, not guessable like classic cache's DNS name, since
// redisEnterprise's hostname isn't a pure function of the resource name). No
// dependency back on api, so no cycle: the Entra access grant that DOES need
// api's identity is a separate module (redisAccess, below) that runs after both.
module redis 'modules/redis.bicep' = if (enableRedis) {
  name: 'deploy-redis-${environment}'
  params: {
    appName: appName
    location: location
    environment: environment
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
    // Empty string when the underlying resource isn't deployed — matches
    // InfrastructureServiceExtensions.cs's own string.IsNullOrEmpty checks,
    // so the app cleanly falls back to NoOpEventPublisher / in-memory cache.
    serviceBusNamespace: enableServiceBus ? sbFqdn : ''
    keyVaultName: kvName
    redisHostName: redis.?outputs.hostName ?? ''
    tenantId: tenantId
    aadClientId: aadClientId
    appServicePlanSku: appServicePlanSku
    enablePrivateNetworking: enablePrivateNetworking
    apiSubnetId: vnet.?outputs.apiSubnetId ?? ''
  }
}

// serviceBus is opt-in (enableServiceBus) — off by default; disabling it only
// turns off async confirmation/cancellation emails, nothing else.
module serviceBus 'modules/servicebus.bicep' = if (enableServiceBus) {
  name: 'deploy-servicebus-${environment}'
  params: {
    appName: appName
    location: location
    environment: environment
    sku: serviceBusSku
    webAppPrincipalId: api.outputs.managedIdentityPrincipalId
    epSubnetId: vnet.?outputs.epSubnetId ?? ''
    vnetId: vnet.?outputs.vnetId ?? ''
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
    enablePrivateNetworking: enablePrivateNetworking
    epSubnetId: vnet.?outputs.epSubnetId ?? ''
    vnetId: vnet.?outputs.vnetId ?? ''
  }
}

module redisAccess 'modules/redis-access.bicep' = if (enableRedis) {
  name: 'deploy-redis-access-${environment}'
  params: {
    clusterName: redis.?outputs.clusterName ?? ''
    databaseName: redis.?outputs.databaseName ?? ''
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
