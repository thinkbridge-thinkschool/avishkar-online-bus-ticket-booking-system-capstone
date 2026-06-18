// ============================================================================
// Module: api.bicep
// Provisions: Log Analytics Workspace → Application Insights
//             → App Service Plan → Linux Web App (.NET 10)
//
// Day 25 changes:
//   - identity: SystemAssigned — enables Managed Identity for the Web App
//   - Removed sqlConnectionString / serviceBusConnectionString @secure() params
//   - ConnectionStrings__DefaultConnection is now passwordless
//     (Authentication=Active Directory Default — MI handles token acquisition)
//   - ServiceBus__Namespace replaces ConnectionStrings__ServiceBus — hostname
//     only, no key
//   - APPLICATIONINSIGHTS_CONNECTION_STRING is now a Key Vault reference —
//     the platform resolves it; app code never calls Key Vault directly
//   - AzureAd__ settings added for Microsoft.Identity.Web JWT validation
//   - Output managedIdentityPrincipalId so main.bicep can pass it to
//     servicebus and keyvault modules for role assignments
// ============================================================================

@description('Short application name used for resource naming.')
param appName string

@description('Azure region for all resources.')
param location string

@description('Deployment environment.')
@allowed(['dev', 'prod'])
param environment string

@description('SQL Server fully qualified domain name.')
param sqlServerFqdn string

@description('SQL Database name.')
param sqlDatabaseName string

@description('Service Bus fully qualified namespace hostname.')
param serviceBusNamespace string

@description('Key Vault name — used to build the KV reference string.')
param keyVaultName string

@description('Entra ID tenant ID for JWT validation.')
param tenantId string

@description('Entra ID application (client) ID for JWT validation.')
param aadClientId string

@description('App Service Plan SKU.')
@allowed(['B1', 'B2', 'P1v3', 'P2v3'])
param appServicePlanSku string = 'B1'

// ── Derived names ─────────────────────────────────────────────────────────────
var suffix   = take(uniqueString(resourceGroup().id), 6)
var planName = 'plan-${appName}-${environment}'
var siteName = 'app-${appName}-${environment}-${suffix}'
var lawName  = 'law-${appName}-${environment}'
var aiName   = 'ai-${appName}-${environment}'

var aspNetEnv = environment == 'prod' ? 'Production' : 'Development'
var isPremium = startsWith(appServicePlanSku, 'P')

// ── Log Analytics Workspace ───────────────────────────────────────────────────

resource logAnalytics 'Microsoft.OperationalInsights/workspaces@2022-10-01' = {
  name: lawName
  location: location
  properties: {
    sku: { name: 'PerGB2018' }
    retentionInDays: environment == 'prod' ? 31 : 30
    features: {
      enableLogAccessUsingOnlyResourcePermissions: true
    }
  }
  tags: { environment: environment, app: appName }
}

// ── Application Insights ──────────────────────────────────────────────────────

resource appInsights 'Microsoft.Insights/components@2020-02-02' = {
  name: aiName
  location: location
  kind: 'web'
  properties: {
    Application_Type: 'web'
    WorkspaceResourceId: logAnalytics.id
    RetentionInDays: environment == 'prod' ? 31 : 30
    IngestionMode: 'LogAnalytics'
    publicNetworkAccessForIngestion: 'Enabled'
    publicNetworkAccessForQuery: 'Enabled'
  }
  tags: { environment: environment, app: appName }
}

// ── App Service Plan ──────────────────────────────────────────────────────────

resource appServicePlan 'Microsoft.Web/serverfarms@2023-01-01' = {
  name: planName
  location: location
  kind: 'linux'
  sku: { name: appServicePlanSku }
  properties: {
    reserved: true
    zoneRedundant: isPremium && environment == 'prod'
  }
  tags: { environment: environment, app: appName }
}

// ── Web App ───────────────────────────────────────────────────────────────────

resource webApp 'Microsoft.Web/sites@2023-01-01' = {
  name: siteName
  location: location
  kind: 'app,linux'
  // SystemAssigned MI — Azure creates and manages the identity certificate.
  // The principalId is used by servicebus and keyvault modules for RBAC.
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    serverFarmId: appServicePlan.id
    httpsOnly: true
    siteConfig: {
      linuxFxVersion: 'DOTNETCORE|10.0'
      alwaysOn: true
      ftpsState: 'Disabled'
      minTlsVersion: '1.2'
      http20Enabled: true
      appSettings: [
        {
          name: 'ASPNETCORE_ENVIRONMENT'
          value: aspNetEnv
        }
        {
          // Passwordless connection string — no User ID or Password.
          // Authentication=Active Directory Default tells Microsoft.Data.SqlClient
          // to acquire a token via DefaultAzureCredential (uses the MI in Azure).
          name: 'ConnectionStrings__DefaultConnection'
          value: 'Server=tcp:${sqlServerFqdn},1433;Initial Catalog=${sqlDatabaseName};Authentication=Active Directory Default;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;'
        }
        {
          // Hostname only — no SAS key.
          // InfrastructureServiceExtensions.cs reads this and constructs
          // ServiceBusClient(namespace, new DefaultAzureCredential()).
          name: 'ServiceBus__Namespace'
          value: serviceBusNamespace
        }
        {
          // Key Vault reference — App Service platform resolves this at runtime.
          // The app reads APPLICATIONINSIGHTS_CONNECTION_STRING as a normal string;
          // it never calls Key Vault directly.
          name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
          value: '@Microsoft.KeyVault(VaultName=${keyVaultName};SecretName=appinsights-connection-string)'
        }
        {
          // Public ID — not a secret. Used by Microsoft.Identity.Web to validate
          // the 'tid' claim in incoming Bearer tokens.
          name: 'AzureAd__TenantId'
          value: tenantId
        }
        {
          // Public ID — not a secret. Used to validate the 'aud' claim.
          name: 'AzureAd__ClientId'
          value: aadClientId
        }
        {
          name: 'AzureAd__Audience'
          value: 'api://${aadClientId}'
        }
      ]
    }
  }
  tags: {
    environment: environment
    app: appName
    'azd-service-name': 'api'
  }
}

// ── Outputs ───────────────────────────────────────────────────────────────────

@description('Default hostname of the Web App.')
output hostName string = webApp.properties.defaultHostName

@description('Resource ID of the Web App.')
output webAppId string = webApp.id

@description('Web App name — used by post-provision hook to create SQL contained user.')
output webAppName string = webApp.name

@description('Principal ID of the system-assigned MI — used for RBAC in servicebus and keyvault modules.')
output managedIdentityPrincipalId string = webApp.identity.principalId

@description('App Insights connection string — passed to keyvault module to store as a secret.')
output appInsightsConnectionString string = appInsights.properties.ConnectionString
