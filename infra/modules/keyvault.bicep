// ============================================================================
// Module: keyvault.bicep
// Provisions: Key Vault + App Insights connection string secret
//             + Key Vault Secrets User role for the App Service MI
//
// Why this module exists:
//   APPLICATIONINSIGHTS_CONNECTION_STRING must not sit as a plain app setting.
//   Storing it here and using a KV reference in api.bicep means the App Service
//   resolves it transparently — the app code never calls Key Vault directly.
//
// Access model: Azure RBAC (not legacy vault access policies).
//   Key Vault Secrets User = read secrets only; no write, no manage.
// ============================================================================

@description('Short application name used for resource naming.')
param appName string

@description('Azure region for all resources.')
param location string

@description('Deployment environment.')
@allowed(['dev', 'prod'])
param environment string

@description('App Insights connection string to store as a secret.')
@secure()
param appInsightsConnectionString string

@description('Principal ID of the App Service system-assigned managed identity.')
param webAppPrincipalId string

// ── Derived names ─────────────────────────────────────────────────────────────
var suffix  = take(uniqueString(resourceGroup().id), 6)
var kvName  = 'kv-${appName}-${environment}-${suffix}'

// Key Vault Secrets User — allows reading secret values; no write or manage.
var keyVaultSecretsUserRoleId = '4633458b-17de-408a-b874-0445c86b69e6'

// ── Key Vault ─────────────────────────────────────────────────────────────────

resource keyVault 'Microsoft.KeyVault/vaults@2023-07-01' = {
  name: kvName
  location: location
  properties: {
    sku: {
      family: 'A'
      name: 'standard'
    }
    tenantId: tenant().tenantId
    enableRbacAuthorization: true       // RBAC model, not legacy access policies
    enableSoftDelete: true
    softDeleteRetentionInDays: 7        // minimum; fine for a capstone project
    enabledForTemplateDeployment: false
    publicNetworkAccess: 'Enabled'
  }
  tags: {
    environment: environment
    app: appName
  }
}

// ── Secret: App Insights connection string ────────────────────────────────────

resource appInsightsSecret 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: keyVault
  name: 'appinsights-connection-string'
  properties: {
    value: appInsightsConnectionString
    attributes: {
      enabled: true
    }
  }
}

// ── RBAC: App Service MI → Key Vault Secrets User ─────────────────────────────

resource kvSecretsUserRole 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  // Deterministic GUID prevents duplicate role assignments on re-deploy.
  name: guid(keyVault.id, webAppPrincipalId, keyVaultSecretsUserRoleId)
  scope: keyVault
  properties: {
    roleDefinitionId: subscriptionResourceId(
      'Microsoft.Authorization/roleDefinitions',
      keyVaultSecretsUserRoleId
    )
    principalId: webAppPrincipalId
    principalType: 'ServicePrincipal'
  }
}

// ── Outputs ───────────────────────────────────────────────────────────────────

@description('Key Vault name — used to build KV reference strings in api.bicep.')
output keyVaultName string = keyVault.name

@description('Full resource ID of the Key Vault.')
output keyVaultId string = keyVault.id
