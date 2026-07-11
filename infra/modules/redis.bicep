// ============================================================================
// Module: redis.bicep
// Provisions: Azure Managed Redis (Microsoft.Cache/redisEnterprise) — L2 store
//             for HybridCache. Classic Azure Cache for Redis is retiring and
//             no longer accepts new deployments, hence redisEnterprise instead
//             of Microsoft.Cache/redis.
//
// Entra ID access (accessPolicyAssignment) is deliberately NOT created here —
// it needs the App Service's managed identity principal ID, which would
// create a circular module dependency (api needs this module's hostName,
// this module would need api's principal ID). See redis-access.bicep, which
// runs after both this module and api.bicep.
//
// SKU: Balanced_B0 is the smallest/cheapest tier — a single extra replica
// beyond that isn't justified yet for an HybridCache L2 store. Revisit if
// cache load grows.
// ============================================================================

@description('Short application name used for resource naming.')
param appName string

@description('Azure region for all resources.')
param location string

@description('Deployment environment.')
@allowed(['dev', 'prod'])
param environment string

var suffix = take(uniqueString(resourceGroup().id), 6)
var clusterName = 'redis-${appName}-${environment}-${suffix}'
var databaseName = 'default'

resource redisCluster 'Microsoft.Cache/redisEnterprise@2025-04-01' = {
  name: clusterName
  location: location
  sku: {
    name: 'Balanced_B0'
  }
  properties: {
    encryption: {}
    highAvailability: environment == 'prod' ? 'Enabled' : 'Disabled'
    minimumTlsVersion: '1.2'
  }
  tags: {
    environment: environment
    app: appName
  }
}

resource redisDatabase 'Microsoft.Cache/redisEnterprise/databases@2025-04-01' = {
  parent: redisCluster
  name: databaseName
  properties: {
    clientProtocol: 'Encrypted'
    port: 10000
    clusteringPolicy: 'OSSCluster'
    evictionPolicy: 'VolatileLRU'
    // Entra ID auth only — HybridCacheService connects via
    // ConfigureForAzureWithTokenCredentialAsync(DefaultAzureCredential), no access key
    // is ever generated, stored, or read by application code.
    accessKeysAuthentication: 'Disabled'
  }
}

@description('Redis cache hostname — no secret, safe as an app setting.')
output hostName string = redisCluster.properties.hostName

@description('Cluster resource name — needed by redis-access.bicep to attach the access policy.')
output clusterName string = redisCluster.name

@description('Database name — needed by redis-access.bicep to attach the access policy.')
output databaseName string = redisDatabase.name
