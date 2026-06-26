// ============================================================================
// Module: vnet.bicep
// Provisions: Virtual Network with two subnets:
//   snet-api       10.0.1.0/24  — delegated to App Service VNet integration
//   snet-endpoints 10.0.2.0/24  — private endpoints (network policies disabled)
//
// Day 27: Provides network isolation so App Service egresses through the VNet
// and SQL is reachable via private endpoint instead of public internet.
// ============================================================================

@description('Short application name used for resource naming.')
param appName string

@description('Azure region for all resources.')
param location string

@description('Deployment environment.')
@allowed(['dev', 'prod'])
param environment string

// ── Derived names ─────────────────────────────────────────────────────────────
var suffix   = take(uniqueString(resourceGroup().id), 6)
var vnetName = 'vnet-${appName}-${environment}-${suffix}'

// ── Virtual Network ───────────────────────────────────────────────────────────

resource vnet 'Microsoft.Network/virtualNetworks@2023-05-01' = {
  name: vnetName
  location: location
  properties: {
    addressSpace: {
      addressPrefixes: ['10.0.0.0/16']
    }
    subnets: [
      {
        name: 'snet-api'
        properties: {
          addressPrefix: '10.0.1.0/24'
          // Delegation required for App Service regional VNet integration.
          // Without this, App Service rejects the subnet association.
          delegations: [
            {
              name: 'delegation-webapp'
              properties: {
                serviceName: 'Microsoft.Web/serverFarms'
              }
            }
          ]
        }
      }
      {
        name: 'snet-endpoints'
        properties: {
          addressPrefix: '10.0.2.0/24'
          // Must be Disabled to allow private endpoint NIC to be placed here.
          privateEndpointNetworkPolicies: 'Disabled'
        }
      }
    ]
  }
  tags: {
    environment: environment
    app: appName
  }
}

// ── Outputs ───────────────────────────────────────────────────────────────────

@description('VNet resource ID — used for private DNS zone VNet link.')
output vnetId string = vnet.id

@description('Resource ID of the App Service delegation subnet.')
output apiSubnetId string = vnet.properties.subnets[0].id

@description('Resource ID of the private endpoints subnet.')
output epSubnetId string = vnet.properties.subnets[1].id
