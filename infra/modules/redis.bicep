// ============================================================================
// Module: redis.bicep
// Provisions: Azure Cache for Redis (L2 store for HybridCache) + an Entra ID
//             access policy assignment for the App Service MI — no access key
//             ever leaves this module, matching the passwordless pattern
//             already used for SQL (Authentication=Active Directory Default)
//             and Service Bus (disableLocalAuth + RBAC).
//
// SKU: Basic (dev) / Standard (prod). Unlike Service Bus, nothing here states a
// private-networking requirement, so Premium (the SKU needed for Private
// Endpoints) isn't justified yet — Standard's single extra replica + SLA is
// the meaningful step up from Basic for prod caching. Revisit if/when Redis
// needs the same private-endpoint treatment as Service Bus/Key Vault.
// ============================================================================

@description('Short application name used for resource naming.')
param appName string

@description('Azure region for all resources.')
param location string

@description('Deployment environment.')
@allowed(['dev', 'prod'])
param environment string

@description('Principal ID of the App Service system-assigned managed identity.')
param webAppPrincipalId string

var suffix = take(uniqueString(resourceGroup().id), 6)
var cacheName = 'redis-${appName}-${environment}-${suffix}'
var skuName = environment == 'prod' ? 'Standard' : 'Basic'

resource redisCache 'Microsoft.Cache/redis@2023-08-01' = {
  name: cacheName
  location: location
  properties: {
    sku: {
      name: skuName
      family: 'C'
      capacity: 0
    }
    minimumTlsVersion: '1.2'
    // Entra ID auth — HybridCacheService connects via
    // ConfigureForAzureWithTokenCredentialAsync(DefaultAzureCredential), no access key
    // is ever generated, stored, or read by application code.
    redisConfiguration: {
      'aad-enabled': 'true'
    }
  }
  tags: {
    environment: environment
    app: appName
  }
}

// Data Contributor — read/write cache data; no access-key management rights.
resource redisAccessPolicy 'Microsoft.Cache/redis/accessPolicyAssignments@2023-08-01' = {
  parent: redisCache
  name: 'webapp-data-contributor'
  properties: {
    accessPolicyName: 'Data Contributor'
    objectId: webAppPrincipalId
    objectIdAlias: 'webapp'
  }
}

@description('Redis cache hostname — no secret, safe as an app setting.')
output hostName string = redisCache.properties.hostName
