// ============================================================================
// Module: sql.bicep
// Provisions: SQL Server + SQL Database + firewall rules + Azure AD admin
//
// Day 25 change: Added Azure AD administrator resource so that the
// post-provision hook can connect using Azure AD auth and run
// CREATE USER [<app-service-name>] FROM EXTERNAL PROVIDER
// (required before the App Service MI can authenticate to SQL).
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
    publicNetworkAccess: 'Enabled'
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

resource allowAzureServices 'Microsoft.Sql/servers/firewallRules@2022-11-01-preview' = {
  parent: sqlServer
  name: 'AllowAllWindowsAzureIps'
  properties: {
    startIpAddress: '0.0.0.0'
    endIpAddress: '0.0.0.0'
  }
}

resource allowDevAccess 'Microsoft.Sql/servers/firewallRules@2022-11-01-preview' = if (environment == 'dev') {
  parent: sqlServer
  name: 'AllowDevClientAccess'
  properties: {
    startIpAddress: '0.0.0.0'
    endIpAddress: '255.255.255.255'
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
