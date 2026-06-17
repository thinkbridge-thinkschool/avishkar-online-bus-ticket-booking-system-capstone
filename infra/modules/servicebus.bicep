// ============================================================================
// Module: servicebus.bicep
// Provisions: Service Bus Namespace + booking-confirmed + booking-cancelled
//             topics + Azure Service Bus Data Sender role for the App Service MI
//
// Day 25 changes:
//   - disableLocalAuth: true  — SAS keys disabled; all connections must use
//     Azure AD (Managed Identity). Equivalent of removing password auth.
//   - Removed api-send-listen SAS auth rule — no longer needed.
//   - Added Azure Service Bus Data Sender role assignment for the App Service MI.
//   - Output changed from SAS connection string to fully qualified namespace
//     hostname (no secret).
// ============================================================================

@description('Short application name used for resource naming.')
param appName string

@description('Azure region for all resources.')
param location string

@description('Deployment environment.')
@allowed(['dev', 'prod'])
param environment string

@description('Service Bus namespace SKU. Standard is the minimum for topics.')
@allowed(['Standard', 'Premium'])
param sku string = 'Standard'

@description('Principal ID of the App Service system-assigned managed identity.')
param webAppPrincipalId string

// ── Derived names ─────────────────────────────────────────────────────────────
var suffix        = take(uniqueString(resourceGroup().id), 6)
var namespaceName = 'sb-${appName}-${environment}-${suffix}'
var topicConfirmed = 'booking-confirmed'
var topicCancelled = 'booking-cancelled'

// Azure Service Bus Data Sender — send messages only; no listen or manage.
var serviceBusDataSenderRoleId = '69a216fc-b8fb-44d8-bc22-1f3c2cd27a39'

// ── Namespace ─────────────────────────────────────────────────────────────────

resource serviceBusNamespace 'Microsoft.ServiceBus/namespaces@2022-10-01-preview' = {
  name: namespaceName
  location: location
  sku: {
    name: sku
    tier: sku
  }
  properties: {
    minimumTlsVersion: '1.2'
    disableLocalAuth: true             // SAS keys globally disabled — MI only
    zoneRedundant: sku == 'Premium' && environment == 'prod'
  }
  tags: {
    environment: environment
    app: appName
  }
}

// ── Topics ────────────────────────────────────────────────────────────────────

resource topicBookingConfirmed 'Microsoft.ServiceBus/namespaces/topics@2022-10-01-preview' = {
  parent: serviceBusNamespace
  name: topicConfirmed
  properties: {
    defaultMessageTimeToLive: 'P14D'
    maxSizeInMegabytes: 1024
    requiresDuplicateDetection: false
    enableBatchedOperations: true
    supportOrdering: false
  }
}

resource topicBookingCancelled 'Microsoft.ServiceBus/namespaces/topics@2022-10-01-preview' = {
  parent: serviceBusNamespace
  name: topicCancelled
  properties: {
    defaultMessageTimeToLive: 'P14D'
    maxSizeInMegabytes: 1024
    requiresDuplicateDetection: false
    enableBatchedOperations: true
    supportOrdering: false
  }
}

// ── RBAC: App Service MI → Azure Service Bus Data Sender ──────────────────────
// Grants the App Service MI the right to send messages to any topic in this
// namespace. Does not grant Listen or Manage — least privilege.

resource sbDataSenderRole 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(serviceBusNamespace.id, webAppPrincipalId, serviceBusDataSenderRoleId)
  scope: serviceBusNamespace
  properties: {
    roleDefinitionId: subscriptionResourceId(
      'Microsoft.Authorization/roleDefinitions',
      serviceBusDataSenderRoleId
    )
    principalId: webAppPrincipalId
    principalType: 'ServicePrincipal'
  }
}

// ── Outputs ───────────────────────────────────────────────────────────────────

@description('Service Bus namespace name.')
output namespaceName string = serviceBusNamespace.name

@description('Fully qualified Service Bus namespace hostname — no secret, safe as app setting.')
output fullyQualifiedNamespace string = '${serviceBusNamespace.name}.servicebus.windows.net'
