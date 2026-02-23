# QSC MES v2

Manufacturing Execution System for Quality Steel Corporation — manages production tracking, quality inspection, material traceability, and defect logging across 3 manufacturing plants.

## Tech Stack

| Layer | Technology |
|---|---|
| **Frontend** | React 18 + TypeScript, Fluent UI v9, Vite |
| **Backend** | ASP.NET Core Web API, .NET 9 |
| **ORM** | Entity Framework Core |
| **Database** | SQL Server (local dev) / Azure SQL (Dev, Test, Prod) |
| **Auth** | JWT Bearer (Entra ID / External ID) |
| **Testing** | Vitest + React Testing Library (frontend), xUnit + Moq (backend), Playwright (E2E) |

## Getting Started

### Prerequisites

- Node.js 20+
- .NET 9 SDK
- Git

### Frontend

```bash
cd frontend
npm install
npm run dev
```

Runs at `http://localhost:5173`

### Backend

```bash
cd backend/MESv2.Api
dotnet run
```

Runs at `https://localhost:5001`

### Running Tests

```bash
# Frontend unit tests
cd frontend
npm test

# Backend unit tests
cd backend
dotnet test

# E2E tests (starts both servers automatically)
cd e2e
npm test
```

E2E tests use Playwright and require the backend to be running with seed data (Development mode with a fresh database). The `webServer` config in `playwright.config.ts` will start both the .NET backend and Vite dev server automatically, or reuse already-running instances.

## Project Structure

```
appMESv2/
  designInput/          # Design specifications
  frontend/             # React + TypeScript SPA
  backend/              # ASP.NET Core Web API
    MESv2.Api/          # API project
    MESv2.Api.Tests/    # xUnit tests
    MESv2.sln
  e2e/                  # Playwright E2E tests
  infra/                # Azure Bicep templates (IaC)
    main.bicep          # Orchestrator
    main.bicepparam     # Default parameters
    modules/            # Individual resource modules
```

## Environments & Deployment

The application uses three Azure environments plus local development:

| Environment | Hosting | Database | How it's deployed |
|---|---|---|---|
| **Local** | `localhost:5001` / `:5173` | Local SQL Server (`MESv2Dev`) | `dotnet run` / `npm run dev` |
| **Dev** | Azure App Service + Static Web App | Azure SQL (`QSCMES-Dev`) | Auto on push to `main` |
| **Test** | Azure App Service + Static Web App | Azure SQL (`QSCMES-Test`) | Manual — "Promote to Test" workflow |
| **Production** | Azure App Service + Static Web App | Azure SQL (`QSCMES-Prod`) | Manual — "Promote to Production" workflow |

### Promotion Flow

```
Local dev ──push to main──> GitHub Actions
                               │
                    [build + unit tests]
                               │
                        Auto-deploy to DEV
                               │
                    Verify on Dev ──OK?──> Trigger "Promote to Test"
                                                     │
                                          [Copy Prod DB → Test DB]
                                          [Build + E2E tests]
                                          [Deploy code to Test]
                                                     │
                                          Teams verify on Test ──OK?──> Trigger "Promote to Prod"
                                                                                  │
                                                                       [Backup Prod DB]
                                                                       [Build + deploy code]
                                                                       [EF migrations upgrade schema]
                                                                                  │
                                                                             Live in Prod
```

### GitHub Actions Workflows

| Workflow | Trigger | What it does |
|---|---|---|
| `deploy.yml` | Push to `main` | Builds, runs unit tests, deploys to **Dev** |
| `promote-to-test.yml` | Manual (type "yes") | Copies Prod DB to Test, builds, runs E2E tests, deploys to **Test** |
| `promote-to-prod.yml` | Manual (type "yes") | Backs up Prod DB, builds, deploys to **Production** |

### GitHub Environment Setup

Each environment (`dev`, `test`, `production`) needs these configured in GitHub Settings > Environments:

| Setting | `dev` | `test` | `production` |
|---|---|---|---|
| `BACKEND_APP_NAME` | `qscmes-dev-app` | `qscmes-test-app` | `qscmes-prod-app` |
| `SWA_DEPLOYMENT_TOKEN` | *(from Dev SWA)* | *(from Test SWA)* | *(from Prod SWA)* |
| `AZURE_CREDENTIALS` | *(service principal)* | *(service principal)* | *(service principal)* |
| `SQL_ADMIN_USER` | — | *(for DB copy)* | *(for DB backup)* |
| `SQL_ADMIN_PASSWORD` | — | *(for DB copy)* | *(for DB backup)* |

The `test` and `production` environments should have **required reviewers** configured for an approval gate.

## Infrastructure (Azure)

Azure resources are defined as Infrastructure-as-Code using [Bicep](https://learn.microsoft.com/azure/azure-resource-manager/bicep/) templates in the `infra/` directory.

### Resources Provisioned

| Module | Resources |
|---|---|
| `appService` | App Service Plan + Web App (.NET 9) with managed identity |
| `staticWebApp` | Azure Static Web App (frontend SPA) |
| `sqlServer` | Azure SQL Server + Database |
| `keyVault` | Key Vault with secrets for connection string, JWT key, App Insights, CORS |
| `appInsights` | Log Analytics workspace + Application Insights |
| `storage` | Storage Account + blob container (for BACPAC backup/restore) |

### Provisioning Environments

```bash
# Dev environment
az group create --name rg-qscmes-dev --location <region>
az deployment group create \
  --resource-group rg-qscmes-dev \
  --template-file infra/main.bicep \
  --parameters environmentName='dev' sqlAdminUser='mesadmin' \
               sqlAdminPassword='<secret>' jwtKey='<secret>'

# Test environment
az group create --name rg-qscmes-test --location <region>
az deployment group create \
  --resource-group rg-qscmes-test \
  --template-file infra/main.bicep \
  --parameters environmentName='test' sqlAdminUser='mesadmin' \
               sqlAdminPassword='<secret>' jwtKey='<secret>'

# Production environment
az group create --name rg-qscmes-prod --location <region>
az deployment group create \
  --resource-group rg-qscmes-prod \
  --template-file infra/main.bicep \
  --parameters environmentName='prod' sqlAdminUser='mesadmin' \
               sqlAdminPassword='<secret>' jwtKey='<secret>'
```

Secrets (`sqlAdminPassword`, `jwtKey`) should be provided at deploy time via CLI parameters or CI/CD pipeline secrets — never committed to source control.
