// ============================================================================
// Module: redis-access.bicep
// Grants the App Service managed identity Entra ID data access to the Redis
// Enterprise database provisioned by redis.bicep.
//
// Split out from redis.bicep to avoid a circular module dependency: api.bicep
// needs redis.bicep's hostName output, and this access-policy assignment
// needs api.bicep's managed identity principal ID. Runs after both.
// ============================================================================

@description('Name of the existing Redis Enterprise cluster (from redis.bicep output).')
param clusterName string

@description('Name of the existing Redis Enterprise database (from redis.bicep output).')
param databaseName string

@description('Principal ID of the App Service system-assigned managed identity.')
param webAppPrincipalId string

resource existingCluster 'Microsoft.Cache/redisEnterprise@2025-04-01' existing = {
  name: clusterName
}

resource existingDatabase 'Microsoft.Cache/redisEnterprise/databases@2025-04-01' existing = {
  parent: existingCluster
  name: databaseName
}

// Full data access (default policy) — no access-key management rights.
// Name must be alphanumeric only (no hyphens), unlike classic cache's accessPolicyAssignments.
resource redisAccessPolicy 'Microsoft.Cache/redisEnterprise/databases/accessPolicyAssignments@2025-04-01' = {
  parent: existingDatabase
  name: 'webappdatacontributor'
  properties: {
    accessPolicyName: 'default'
    user: {
      objectId: webAppPrincipalId
    }
  }
}
