// ============================================================================
// Module: keyvault.bicep
// Provisions: Key Vault + App Insights connection string secret
//             + Key Vault Secrets User role for the App Service MI
//             + Private Endpoint + Private DNS Zone (Day 27)
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

@description('Resource ID of the private endpoints subnet — used for private endpoint NIC placement.')
param epSubnetId string

@description('VNet resource ID — used to link the private DNS zone.')
param vnetId string

// ── Derived names ─────────────────────────────────────────────────────────────
var suffix  = take(uniqueString(resourceGroup().id), 5)
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
    enableRbacAuthorization: true
    enableSoftDelete: true
    softDeleteRetentionInDays: 7
    enabledForTemplateDeployment: false
    // Disable public internet access in prod — all traffic via private endpoint.
    // Keep enabled in dev so azd provision (ARM deployment) can write secrets.
    publicNetworkAccess: environment == 'prod' ? 'Disabled' : 'Enabled'
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

// ── Private Endpoint ──────────────────────────────────────────────────────────
// App Service (via VNet integration) reaches Key Vault through the private NIC
// instead of over the public internet. Group ID for Key Vault is 'vault'.

resource kvPrivateEndpoint 'Microsoft.Network/privateEndpoints@2023-05-01' = {
  name: 'pe-${kvName}'
  location: location
  properties: {
    subnet: {
      id: epSubnetId
    }
    privateLinkServiceConnections: [
      {
        name: 'plsc-${kvName}'
        properties: {
          privateLinkServiceId: keyVault.id
          groupIds: ['vault']
        }
      }
    ]
  }
  tags: {
    environment: environment
    app: appName
  }
}

// ── Private DNS Zone ──────────────────────────────────────────────────────────
// Key Vault private link uses privatelink.vaultcore.azure.net — note this is
// different from the public FQDN suffix (.vault.azure.net). Both are required:
// the public CNAME redirects to the private FQDN at resolution time.
#disable-next-line no-hardcoded-env-urls
resource kvPrivateDnsZone 'Microsoft.Network/privateDnsZones@2020-06-01' = {
  name: 'privatelink.vaultcore.azure.net'
  location: 'global'
  tags: {
    environment: environment
    app: appName
  }
}

resource kvDnsVnetLink 'Microsoft.Network/privateDnsZones/virtualNetworkLinks@2020-06-01' = {
  parent: kvPrivateDnsZone
  name: 'link-kv-${appName}-${environment}'
  location: 'global'
  properties: {
    virtualNetwork: {
      id: vnetId
    }
    registrationEnabled: false
  }
}

resource kvDnsZoneGroup 'Microsoft.Network/privateEndpoints/privateDnsZoneGroups@2023-05-01' = {
  parent: kvPrivateEndpoint
  name: 'dzg-kv'
  properties: {
    privateDnsZoneConfigs: [
      {
        name: 'privatelink-vaultcore-azure-net'
        properties: {
          privateDnsZoneId: kvPrivateDnsZone.id
        }
      }
    ]
  }
}

// ── Outputs ───────────────────────────────────────────────────────────────────

@description('Key Vault name — used to build KV reference strings in api.bicep.')
output keyVaultName string = keyVault.name

@description('Full resource ID of the Key Vault.')
output keyVaultId string = keyVault.id
