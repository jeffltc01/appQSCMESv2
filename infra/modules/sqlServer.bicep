@description('Environment name (test or prod)')
param environmentName string

@description('Azure region for the resources')
param location string

@description('SQL Server administrator login')
param sqlAdminUser string

@secure()
@description('SQL Server administrator password')
param sqlAdminPassword string

@description('SQL Database SKU name')
param dbSkuName string = 'Basic'

@description('SQL Database max size in bytes (2 GB default)')
param dbMaxSizeBytes int = 2147483648

var sqlServerName = 'mes-${environmentName}-sql'
var databaseName = 'MES-${environmentName == 'prod' ? 'Prod' : 'Test'}'

resource sqlServer 'Microsoft.Sql/servers@2023-08-01-preview' = {
  name: sqlServerName
  location: location
  properties: {
    administratorLogin: sqlAdminUser
    administratorLoginPassword: sqlAdminPassword
    version: '12.0'
    minimalTlsVersion: '1.2'
  }
}

resource allowAzureServices 'Microsoft.Sql/servers/firewallRules@2023-08-01-preview' = {
  parent: sqlServer
  name: 'AllowAzureServices'
  properties: {
    startIpAddress: '0.0.0.0'
    endIpAddress: '0.0.0.0'
  }
}

resource database 'Microsoft.Sql/servers/databases@2023-08-01-preview' = {
  parent: sqlServer
  name: databaseName
  location: location
  sku: {
    name: dbSkuName
  }
  properties: {
    maxSizeBytes: dbMaxSizeBytes
    collation: 'SQL_Latin1_General_CP1_CI_AS'
  }
}

@description('The fully qualified SQL Server name')
output sqlServerFqdn string = sqlServer.properties.fullyQualifiedDomainName

@description('The database name')
output databaseName string = database.name

@description('Connection string (without credentials — use Key Vault for secrets)')
output connectionString string = 'Server=tcp:${sqlServer.properties.fullyQualifiedDomainName},1433;Initial Catalog=${databaseName};Persist Security Info=False;User ID=${sqlAdminUser};Password=${sqlAdminPassword};MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;'
