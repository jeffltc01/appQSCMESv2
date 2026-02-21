using './main.bicep'

// Override these values per environment. Secrets should be passed via
// --parameters on the CLI or from a GitHub Actions secret, never committed.
//
// Example (test):
//   az deployment group create \
//     --resource-group rg-mes-test \
//     --template-file infra/main.bicep \
//     --parameters infra/main.bicepparam \
//     --parameters sqlAdminPassword='<secret>' jwtKey='<secret>'

param environmentName = 'test'
param sqlAdminUser = 'mesadmin'
