targetScope = 'resourceGroup'

@description('Environment name: test or prod')
@allowed([
  'test'
  'prod'
])
param environmentName string

@description('Azure region for all resources')
param location string = resourceGroup().location

@description('SQL Server administrator login')
param sqlAdminUser string

@secure()
@description('SQL Server administrator password')
param sqlAdminPassword string

@secure()
@description('JWT signing key (min 32 chars)')
param jwtKey string

// ---------------------------------------------------------------------------
// Application Insights + Log Analytics
// ---------------------------------------------------------------------------
module appInsights 'modules/appInsights.bicep' = {
  name: 'appInsights-${environmentName}'
  params: {
    environmentName: environmentName
    location: location
  }
}

// ---------------------------------------------------------------------------
// SQL Server + Database
// ---------------------------------------------------------------------------
module sql 'modules/sqlServer.bicep' = {
  name: 'sql-${environmentName}'
  params: {
    environmentName: environmentName
    location: location
    sqlAdminUser: sqlAdminUser
    sqlAdminPassword: sqlAdminPassword
  }
}

// ---------------------------------------------------------------------------
// Static Web App (frontend)
// ---------------------------------------------------------------------------
module swa 'modules/staticWebApp.bicep' = {
  name: 'swa-${environmentName}'
  params: {
    environmentName: environmentName
    location: location
  }
}

// ---------------------------------------------------------------------------
// App Service (backend) — deployed first without Key Vault refs so we can
// capture the managed-identity principal ID for Key Vault access policies.
// ---------------------------------------------------------------------------
module appService 'modules/appService.bicep' = {
  name: 'appService-${environmentName}'
  params: {
    environmentName: environmentName
    location: location
    keyVaultName: 'mes-${environmentName}-kv'
  }
}

// ---------------------------------------------------------------------------
// Key Vault — stores secrets, grants GET to App Service managed identity
// ---------------------------------------------------------------------------
module keyVault 'modules/keyVault.bicep' = {
  name: 'keyVault-${environmentName}'
  params: {
    environmentName: environmentName
    location: location
    appServicePrincipalId: appService.outputs.webAppPrincipalId
    sqlConnectionString: sql.outputs.connectionString
    jwtKey: jwtKey
    appInsightsConnectionString: appInsights.outputs.connectionString
    corsAllowedOrigin: 'https://${swa.outputs.defaultHostname}'
  }
}

// ---------------------------------------------------------------------------
// Storage Account (for BACPAC database backup/restore)
// ---------------------------------------------------------------------------
module storage 'modules/storage.bicep' = {
  name: 'storage-${environmentName}'
  params: {
    location: location
  }
}

// ---------------------------------------------------------------------------
// Outputs
// ---------------------------------------------------------------------------

@description('Backend App Service name (use in deploy workflow)')
output appServiceName string = appService.outputs.webAppName

@description('Backend App Service hostname')
output appServiceHostname string = appService.outputs.webAppHostname

@description('Static Web App default hostname (use for CORS)')
output swaHostname string = swa.outputs.defaultHostname

@description('SQL Server FQDN')
output sqlServerFqdn string = sql.outputs.sqlServerFqdn

@description('Database name')
output databaseName string = sql.outputs.databaseName
