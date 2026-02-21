@description('Environment name (test or prod)')
param environmentName string

@description('Azure region for the resources')
param location string

@description('Principal ID of the App Service managed identity (for access policy)')
param appServicePrincipalId string

@secure()
@description('SQL connection string to store as a secret')
param sqlConnectionString string

@secure()
@description('JWT signing key to store as a secret')
param jwtKey string

@description('Application Insights connection string to store as a secret')
param appInsightsConnectionString string

@description('CORS allowed origin (Static Web App hostname)')
param corsAllowedOrigin string

var keyVaultName = 'mes-${environmentName}-kv'

resource keyVault 'Microsoft.KeyVault/vaults@2023-07-01' = {
  name: keyVaultName
  location: location
  properties: {
    sku: {
      family: 'A'
      name: 'standard'
    }
    tenantId: subscription().tenantId
    accessPolicies: [
      {
        tenantId: subscription().tenantId
        objectId: appServicePrincipalId
        permissions: {
          secrets: [
            'get'
            'list'
          ]
        }
      }
    ]
  }
}

resource secretConnectionString 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: keyVault
  name: 'ConnectionStrings--DefaultConnection'
  properties: {
    value: sqlConnectionString
  }
}

resource secretJwtKey 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: keyVault
  name: 'Jwt--Key'
  properties: {
    value: jwtKey
  }
}

resource secretAppInsights 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: keyVault
  name: 'ApplicationInsights--ConnectionString'
  properties: {
    value: appInsightsConnectionString
  }
}

resource secretCors 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: keyVault
  name: 'Cors--AllowedOrigins--0'
  properties: {
    value: corsAllowedOrigin
  }
}

@description('Key Vault name')
output keyVaultName string = keyVault.name

@description('Key Vault URI')
output keyVaultUri string = keyVault.properties.vaultUri
