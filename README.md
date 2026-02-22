# QSC MES v2

Manufacturing Execution System for Quality Steel Corporation — manages production tracking, quality inspection, material traceability, and defect logging across 3 manufacturing plants.

## Tech Stack

| Layer | Technology |
|---|---|
| **Frontend** | React 18 + TypeScript, Fluent UI v9, Vite |
| **Backend** | ASP.NET Core Web API, .NET 9 |
| **ORM** | Entity Framework Core |
| **Database** | SQLite (dev) / Azure SQL (prod) |
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

### Deploying Infrastructure

```bash
# Test environment
az deployment group create \
  --resource-group rg-mes-test \
  --template-file infra/main.bicep \
  --parameters infra/main.bicepparam \
  --parameters sqlAdminPassword='<secret>' jwtKey='<secret>'

# Production environment
az deployment group create \
  --resource-group rg-mes-prod \
  --template-file infra/main.bicep \
  --parameters environmentName='prod' sqlAdminUser='mesadmin' \
  --parameters sqlAdminPassword='<secret>' jwtKey='<secret>'
```

Secrets (`sqlAdminPassword`, `jwtKey`) should be provided at deploy time via CLI parameters or CI/CD pipeline secrets -- never committed to source control.
