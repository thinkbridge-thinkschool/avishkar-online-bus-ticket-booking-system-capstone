// ============================================================================
// Module: servicebus.bicep
// Provisions: Service Bus Namespace + booking-confirmed + booking-cancelled
//             topics + one subscription per topic (for ServiceBusConsumerService)
//             + Azure Service Bus Data Sender/Receiver roles for the App Service MI
//             + Private Endpoint + Private DNS Zone (Day 27, Premium SKU only)
//
// Day 25 changes:
//   - disableLocalAuth: true  — SAS keys disabled; all connections must use
//     Azure AD (Managed Identity). Equivalent of removing password auth.
//   - Removed api-send-listen SAS auth rule — no longer needed.
//   - Added Azure Service Bus Data Sender role assignment for the App Service MI.
//   - Output changed from SAS connection string to fully qualified namespace
//     hostname (no secret).
//
// Day 27 change: Private endpoint added (Premium SKU only).
//   Standard SKU does not support private endpoints — Azure platform constraint.
//   Dev uses Standard (no PE). Prod uses Premium (PE enabled).
//   When sku == 'Premium', all Service Bus traffic is routed through the VNet
//   via the private endpoint NIC rather than the public internet.
//
// Day 28 change: completed the messaging story — added a subscription per topic
//   (there was previously nowhere for a consumer to attach), enabled duplicate
//   detection on both topics (send-side dedup, keyed by the Outbox row's MessageId
//   — see ServiceBusEventPublisher), and added the Data Receiver role so
//   ServiceBusConsumerService can actually read from these subscriptions.
// ============================================================================

@description('Short application name used for resource naming.')
param appName string

@description('Azure region for all resources.')
param location string

@description('Deployment environment.')
@allowed(['dev', 'prod'])
param environment string

@description('Service Bus namespace SKU. Standard is the minimum for topics. Premium required for private endpoints.')
@allowed(['Standard', 'Premium'])
param sku string = 'Standard'

@description('Principal ID of the App Service system-assigned managed identity.')
param webAppPrincipalId string

@description('Resource ID of the private endpoints subnet — used for private endpoint NIC placement (Premium only).')
param epSubnetId string

@description('VNet resource ID — used to link the private DNS zone (Premium only).')
param vnetId string

// ── Derived names ─────────────────────────────────────────────────────────────
var suffix        = take(uniqueString(resourceGroup().id), 6)
var namespaceName = 'sb-${appName}-${environment}-${suffix}'
var topicConfirmed = 'booking-confirmed'
var topicCancelled = 'booking-cancelled'

var subConfirmed = 'sub-booking-confirmed'
var subCancelled = 'sub-booking-cancelled'

// Azure Service Bus Data Sender — send messages only; no listen or manage.
var serviceBusDataSenderRoleId = '69a216fc-b8fb-44d8-bc22-1f3c2cd27a39'
// Azure Service Bus Data Receiver — receive/complete/dead-letter messages; no send or manage.
var serviceBusDataReceiverRoleId = '4f6d3b9b-027b-4f4c-9142-0e5a2a2247e0'

var isPremium = sku == 'Premium'

// ── Namespace ─────────────────────────────────────────────────────────────────

resource serviceBusNamespace 'Microsoft.ServiceBus/namespaces@2022-10-01-preview' = {
  name: namespaceName
  location: location
  sku: {
    name: sku
    tier: sku
    // Messaging units required for Premium; ignored by Standard.
    // 1 MU = minimum for dev/prod with private endpoint.
    capacity: isPremium ? 1 : 0
  }
  properties: {
    minimumTlsVersion: '1.2'
    disableLocalAuth: true
    zoneRedundant: isPremium && environment == 'prod'
    // Disable public internet access in prod Premium — all traffic via PE.
    // Standard does not support this property so it is omitted for Standard.
    publicNetworkAccess: isPremium && environment == 'prod' ? 'Disabled' : 'Enabled'
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
    // requiresDuplicateDetection is immutable on an existing topic, so this must match what's
    // already live. Dedup for retried outbox publishes is handled at the consumer instead, via
    // the ProcessedMessage inbox table keyed on (MessageId, SubscriptionName) — see
    // ServiceBusConsumerService — so this isn't the app's actual correctness mechanism.
    requiresDuplicateDetection: false
    duplicateDetectionHistoryTimeWindow: 'PT10M'
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
    duplicateDetectionHistoryTimeWindow: 'PT10M'
    enableBatchedOperations: true
    supportOrdering: false
  }
}

// ── Subscriptions ─────────────────────────────────────────────────────────────
// One subscription per topic — a single consumer (ServiceBusConsumerService) reads
// both. maxDeliveryCount=10 is generous enough to survive transient consumer outages
// (the Outbox already retries the send side) while still bounding a poison message;
// deadLetteringOnMessageExpiration means an unconsumed/failing message ends up in
// $DeadLetterQueue instead of being silently dropped once it exceeds either bound.

resource subBookingConfirmed 'Microsoft.ServiceBus/namespaces/topics/subscriptions@2022-10-01-preview' = {
  parent: topicBookingConfirmed
  name: subConfirmed
  properties: {
    defaultMessageTimeToLive: 'P14D'
    maxDeliveryCount: 10
    deadLetteringOnMessageExpiration: true
    enableBatchedOperations: true
  }
}

resource subBookingCancelled 'Microsoft.ServiceBus/namespaces/topics/subscriptions@2022-10-01-preview' = {
  parent: topicBookingCancelled
  name: subCancelled
  properties: {
    defaultMessageTimeToLive: 'P14D'
    maxDeliveryCount: 10
    deadLetteringOnMessageExpiration: true
    enableBatchedOperations: true
  }
}

// ── RBAC: App Service MI → Azure Service Bus Data Sender + Receiver ───────────
// Sender: ServiceBusEventPublisher (outbound). Receiver: ServiceBusConsumerService
// (inbound, via ServiceBusProcessor against the two subscriptions above).

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

resource sbDataReceiverRole 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(serviceBusNamespace.id, webAppPrincipalId, serviceBusDataReceiverRoleId)
  scope: serviceBusNamespace
  properties: {
    roleDefinitionId: subscriptionResourceId(
      'Microsoft.Authorization/roleDefinitions',
      serviceBusDataReceiverRoleId
    )
    principalId: webAppPrincipalId
    principalType: 'ServicePrincipal'
  }
}

// ── Private Endpoint (Premium SKU only) ───────────────────────────────────────
// Standard SKU does not support private endpoints — this block is skipped on dev.
// Group ID for Service Bus namespaces is 'namespace'.

resource sbPrivateEndpoint 'Microsoft.Network/privateEndpoints@2023-05-01' = if (isPremium) {
  name: 'pe-${namespaceName}'
  location: location
  properties: {
    subnet: {
      id: epSubnetId
    }
    privateLinkServiceConnections: [
      {
        name: 'plsc-${namespaceName}'
        properties: {
          privateLinkServiceId: serviceBusNamespace.id
          groupIds: ['namespace']
        }
      }
    ]
  }
  tags: {
    environment: environment
    app: appName
  }
}

// ── Private DNS Zone (Premium SKU only) ───────────────────────────────────────
// Resolves *.servicebus.windows.net to the private endpoint IP inside the VNet.
#disable-next-line no-hardcoded-env-urls
resource sbPrivateDnsZone 'Microsoft.Network/privateDnsZones@2020-06-01' = if (isPremium) {
  name: 'privatelink.servicebus.windows.net'
  location: 'global'
  tags: {
    environment: environment
    app: appName
  }
}

resource sbDnsVnetLink 'Microsoft.Network/privateDnsZones/virtualNetworkLinks@2020-06-01' = if (isPremium) {
  parent: sbPrivateDnsZone
  name: 'link-sb-${appName}-${environment}'
  location: 'global'
  properties: {
    virtualNetwork: {
      id: vnetId
    }
    registrationEnabled: false
  }
}

resource sbDnsZoneGroup 'Microsoft.Network/privateEndpoints/privateDnsZoneGroups@2023-05-01' = if (isPremium) {
  parent: sbPrivateEndpoint
  name: 'dzg-sb'
  properties: {
    privateDnsZoneConfigs: [
      {
        name: 'privatelink-servicebus-windows-net'
        properties: {
          privateDnsZoneId: sbPrivateDnsZone.id
        }
      }
    ]
  }
}

// ── Outputs ───────────────────────────────────────────────────────────────────

@description('Service Bus namespace name.')
output namespaceName string = serviceBusNamespace.name

@description('Fully qualified Service Bus namespace hostname — no secret, safe as app setting.')
output fullyQualifiedNamespace string = '${serviceBusNamespace.name}.servicebus.windows.net'
