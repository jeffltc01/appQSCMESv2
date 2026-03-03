targetScope = 'resourceGroup'

@description('Environment name: dev, test, or prod')
@allowed([
  'dev'
  'test'
  'prod'
])
param environmentName string

@description('Azure region for all resources')
param location string = resourceGroup().location

@description('Azure region for Static Web App (SWA is not available in all regions)')
param swaLocation string = 'eastus2'

@description('Use externally managed SQL connection string instead of provisioning SQL resources')
param useExternalSql bool = false

@secure()
@description('External SQL connection string to use when useExternalSql=true')
param externalSqlConnectionString string = ''

@description('SQL Server administrator login')
param sqlAdminUser string = 'mesadmin'

@secure()
@description('SQL Server administrator password')
param sqlAdminPassword string = ''

@secure()
@description('JWT signing key (min 32 chars)')
param jwtKey string

@secure()
@description('GitHub personal access token for issue creation (empty = feature disabled)')
param githubToken string = ''

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
module sql 'modules/sqlServer.bicep' = if (!useExternalSql) {
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
    location: swaLocation
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
    keyVaultName: 'qscmes-${environmentName}-kv'
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
    sqlConnectionString: useExternalSql ? externalSqlConnectionString : sql.outputs.connectionString
    jwtKey: jwtKey
    appInsightsConnectionString: appInsights.outputs.connectionString
    corsAllowedOrigin: 'https://${swa.outputs.defaultHostname}'
    githubToken: githubToken
  }
}

// ---------------------------------------------------------------------------
// Storage Account (for BACPAC database backup/restore)
// ---------------------------------------------------------------------------
module storage 'modules/storage.bicep' = {
  name: 'storage-${environmentName}'
  params: {
    storageAccountName: 'qscmesbacpacs${environmentName}'
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
output sqlServerFqdn string = useExternalSql ? '' : sql.outputs.sqlServerFqdn

@description('Database name')
output databaseName string = useExternalSql ? '' : sql.outputs.databaseName
