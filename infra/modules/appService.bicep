@description('Environment name (test or prod)')
param environmentName string

@description('Azure region for the resources')
param location string

@description('App Service Plan SKU name')
param skuName string = 'B1'

@description('Key Vault name for secret references')
param keyVaultName string

var appServicePlanName = 'mes-${environmentName}-plan'
var webAppName = 'mes-${environmentName}-app'

resource appServicePlan 'Microsoft.Web/serverfarms@2023-12-01' = {
  name: appServicePlanName
  location: location
  sku: {
    name: skuName
  }
  properties: {
    reserved: false
  }
}

resource webApp 'Microsoft.Web/sites@2023-12-01' = {
  name: webAppName
  location: location
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    serverFarmId: appServicePlan.id
    httpsOnly: true
    siteConfig: {
      netFrameworkVersion: 'v9.0'
      alwaysOn: skuName != 'F1'
      healthCheckPath: '/healthz'
      appSettings: [
        {
          name: 'ConnectionStrings__DefaultConnection'
          value: '@Microsoft.KeyVault(VaultName=${keyVaultName};SecretName=ConnectionStrings--DefaultConnection)'
        }
        {
          name: 'Jwt__Key'
          value: '@Microsoft.KeyVault(VaultName=${keyVaultName};SecretName=Jwt--Key)'
        }
        {
          name: 'Jwt__Issuer'
          value: 'MESv2'
        }
        {
          name: 'Jwt__Audience'
          value: 'MESv2'
        }
        {
          name: 'ApplicationInsights__ConnectionString'
          value: '@Microsoft.KeyVault(VaultName=${keyVaultName};SecretName=ApplicationInsights--ConnectionString)'
        }
      ]
    }
  }
}

@description('The Web App name (used in deploy workflows)')
output webAppName string = webApp.name

@description('The Web App default hostname')
output webAppHostname string = webApp.properties.defaultHostName

@description('The Web App managed identity principal ID (for Key Vault access policies)')
output webAppPrincipalId string = webApp.identity.principalId
