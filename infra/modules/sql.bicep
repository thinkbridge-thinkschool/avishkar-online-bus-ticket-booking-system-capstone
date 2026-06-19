// ============================================================================
// Module: sql.bicep
// Provisions: SQL Server + SQL Database + firewall rules + Azure AD admin
//             + Private Endpoint + Private DNS Zone (Day 27)
//
// Day 25 change: Added Azure AD administrator resource so that the
// post-provision hook can connect using Azure AD auth and run
// CREATE USER [<app-service-name>] FROM EXTERNAL PROVIDER
// (required before the App Service MI can authenticate to SQL).
//
// Day 27 change: Added private endpoint so App Service reaches SQL through
// the VNet rather than the public internet. Removed AllowDevClientAccess
// (0.0.0.0-255.255.255.255) — the wide-open rule was a critical gap.
// Dev machines needing direct SQL access must add their own IP via:
//   az sql server firewall-rule create --name DevMachine \
//     --start-ip-address <your-ip> --end-ip-address <your-ip>
// ============================================================================

@description('Short application name used for resource naming.')
param appName string

@description('Azure region for all resources.')
param location string

@description('Deployment environment — drives naming and tier defaults.')
@allowed(['dev', 'prod'])
param environment string

@description('SQL Server administrator login name.')
param adminLogin string

@secure()
@description('SQL Server administrator password (≥12 chars, upper+lower+digit+special).')
param adminPassword string

@description('Azure SQL Database SKU name.')
@allowed(['Basic', 'S1', 'S2', 'S3'])
param skuName string = 'Basic'

@description('DTU capacity. Basic = 5, S1 = 20, S2 = 50, S3 = 100.')
param capacity int = 5

@description('Object ID of the Azure AD user to set as SQL Azure AD admin.')
param sqlAdminPrincipalId string

@description('UPN of the Azure AD user to set as SQL Azure AD admin.')
param sqlAdminPrincipalName string

@description('Resource ID of the private endpoints subnet — used for private endpoint NIC placement.')
param epSubnetId string

@description('VNet resource ID — used to link the private DNS zone.')
param vnetId string

// ── Derived names ─────────────────────────────────────────────────────────────
var suffix     = take(uniqueString(resourceGroup().id), 6)
var serverName = 'sql-${appName}-${environment}-${suffix}'
var dbName     = 'sqldb-${appName}-${environment}'
var skuTier    = skuName == 'Basic' ? 'Basic' : 'Standard'

// ── SQL Server ────────────────────────────────────────────────────────────────

resource sqlServer 'Microsoft.Sql/servers@2022-11-01-preview' = {
  name: serverName
  location: location
  properties: {
    administratorLogin: adminLogin
    administratorLoginPassword: adminPassword
    minimalTlsVersion: '1.2'
    // Keep public access enabled in dev so the post-provision hook (go-sqlcmd)
    // can run EF migrations. In prod the private endpoint handles all traffic
    // and this can be toggled to Disabled once the hook is migrated to run
    // from within the VNet (e.g., a Container App Job).
    publicNetworkAccess: environment == 'prod' ? 'Disabled' : 'Enabled'
  }
  tags: {
    environment: environment
    app: appName
  }
}

// ── Azure AD administrator ────────────────────────────────────────────────────
// Required so the post-provision hook can connect via Azure AD and run
// CREATE USER [...] FROM EXTERNAL PROVIDER for the App Service MI.

resource sqlAadAdmin 'Microsoft.Sql/servers/administrators@2022-11-01-preview' = {
  parent: sqlServer
  name: 'ActiveDirectory'
  properties: {
    administratorType: 'ActiveDirectory'
    login: sqlAdminPrincipalName
    sid: sqlAdminPrincipalId
    tenantId: tenant().tenantId
  }
}

// ── Firewall rules ────────────────────────────────────────────────────────────
// AllowAllWindowsAzureIps (0.0.0.0–0.0.0.0) is the special Azure rule that
// permits connections from any Azure-hosted service, including App Service.
// The formerly present AllowDevClientAccess (0.0.0.0–255.255.255.255) rule
// was removed in Day 27 — it allowed the entire internet. Dev machine access
// must be granted explicitly via 'az sql server firewall-rule create'.

resource allowAzureServices 'Microsoft.Sql/servers/firewallRules@2022-11-01-preview' = {
  parent: sqlServer
  name: 'AllowAllWindowsAzureIps'
  properties: {
    startIpAddress: '0.0.0.0'
    endIpAddress: '0.0.0.0'
  }
}

// ── Database ──────────────────────────────────────────────────────────────────

resource sqlDatabase 'Microsoft.Sql/servers/databases@2022-11-01-preview' = {
  parent: sqlServer
  name: dbName
  location: location
  sku: {
    name: skuName
    tier: skuTier
    capacity: capacity
  }
  properties: {
    collation: 'SQL_Latin1_General_CP1_CI_AS'
    maxSizeBytes: skuName == 'Basic' ? 2147483648 : 107374182400
    zoneRedundant: false
    readScale: 'Disabled'
    requestedBackupStorageRedundancy: environment == 'prod' ? 'Geo' : 'Local'
  }
  tags: {
    environment: environment
    app: appName
  }
}

// ── Private Endpoint ──────────────────────────────────────────────────────────
// Places a NIC in snet-endpoints. App Service (via VNet integration on snet-api)
// routes SQL traffic through this endpoint instead of the public internet.

resource sqlPrivateEndpoint 'Microsoft.Network/privateEndpoints@2023-05-01' = {
  name: 'pe-${serverName}'
  location: location
  properties: {
    subnet: {
      id: epSubnetId
    }
    privateLinkServiceConnections: [
      {
        name: 'plsc-${serverName}'
        properties: {
          privateLinkServiceId: sqlServer.id
          groupIds: ['sqlServer']
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
// Resolves *.database.windows.net to the private endpoint IP inside the VNet.
// Without this, DNS still returns the public IP even when a private endpoint
// exists, and connections bypass the private endpoint.

resource privateDnsZone 'Microsoft.Network/privateDnsZones@2020-06-01' = {
  name: 'privatelink${az.environment().suffixes.sqlServerHostname}'
  location: 'global'
  tags: {
    environment: environment
    app: appName
  }
}

resource dnsVnetLink 'Microsoft.Network/privateDnsZones/virtualNetworkLinks@2020-06-01' = {
  parent: privateDnsZone
  name: 'link-${appName}-${environment}'
  location: 'global'
  properties: {
    virtualNetwork: {
      id: vnetId
    }
    registrationEnabled: false
  }
}

resource dnsZoneGroup 'Microsoft.Network/privateEndpoints/privateDnsZoneGroups@2023-05-01' = {
  parent: sqlPrivateEndpoint
  name: 'dzg-sql'
  properties: {
    privateDnsZoneConfigs: [
      {
        name: 'privatelink-database-windows-net'
        properties: {
          privateDnsZoneId: privateDnsZone.id
        }
      }
    ]
  }
}

// ── Outputs ───────────────────────────────────────────────────────────────────

@description('Fully-qualified domain name of the SQL Server.')
output serverFqdn string = sqlServer.properties.fullyQualifiedDomainName

@description('Name of the created database.')
output databaseName string = sqlDatabase.name

@description('SQL Server resource name — used by post-provision hook.')
output serverName string = sqlServer.name

@secure()
@description('ADO.NET connection string with SQL auth — used only by EF migrations from dev machine, never injected into App Service.')
output adminConnectionString string = 'Server=tcp:${sqlServer.properties.fullyQualifiedDomainName},1433;Initial Catalog=${sqlDatabase.name};Persist Security Info=False;User ID=${adminLogin};Password=${adminPassword};MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;'
