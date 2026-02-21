@description('Environment name (test or prod)')
param environmentName string

@description('Azure region for the Static Web App')
param location string

var staticWebAppName = 'mes-${environmentName}-swa'

resource staticWebApp 'Microsoft.Web/staticSites@2023-12-01' = {
  name: staticWebAppName
  location: location
  sku: {
    name: 'Free'
    tier: 'Free'
  }
  properties: {}
}

@description('The Static Web App default hostname (for CORS configuration)')
output defaultHostname string = staticWebApp.properties.defaultHostname

@description('The Static Web App name')
output staticWebAppName string = staticWebApp.name
