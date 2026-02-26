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
| **Dev** | Azure App Service + Static Web App | Azure SQL (`QSCMES-Dev`) | Verify on push to `dev`, then manual deploy workflow |
| **Test** | Azure App Service + Static Web App | Azure SQL (`QSCMES-Test`) | Manual — "Promote to Test" workflow |
| **Production** | Azure App Service + Static Web App | Azure SQL (`QSCMES-Prod`) | Manual — "Promote to Production" workflow |

### Promotion Flow

```
Local dev ──push to dev──> GitHub Actions
                               │
               [build once + tests + package artifacts]
                               │
                     Manual deploy to DEV
                               │
                      [required post-deploy smoke]
                               │
     Verify on Dev ──OK?──> Trigger "Promote to Test" with source_run_id
                                                     │
                                          [Copy Prod DB → Test DB]
                                          [Download immutable release artifacts]
                                          [Deploy code to Test]
                                          [required post-deploy smoke]
                                                     │
        Teams verify on Test ──OK?──> Trigger "Promote to Prod" with source_run_id
                                                                                  │
                                                                       [Backup Prod DB]
                                                                       [Download immutable release artifacts]
                                                                       [Deploy code]
                                                                       [required post-deploy smoke]
                                                                       [EF migrations upgrade schema]
                                                                                  │
                                                                             Live in Prod
```

### GitHub Actions Workflows

| Workflow | Trigger | What it does |
|---|---|---|
| `build-test-package.yml` | Reusable (`workflow_call`) | Builds backend/frontend once, runs tests, publishes immutable artifacts + release manifest |
| `verify-dev-build.yml` | Push to `dev` + manual | Runs CI-equivalent build/test/package for the commit and publishes release artifacts |
| `deploy.yml` | Manual (`workflow_dispatch`) | Validates config, requires a successful `verify-dev-build.yml` run for the selected commit, deploys to **Dev**, runs required smoke checks |
| `promote-to-test.yml` | Manual (`confirm=yes`, `source_run_id`) | Copies Prod DB to Test, deploys the exact artifact pair from `source_run_id`, runs required smoke checks |
| `promote-to-prod.yml` | Manual (`confirm=yes`, `source_run_id`) | Backs up Prod DB, deploys the exact artifact pair from `source_run_id`, runs required smoke checks |

### Promotion Operations (Test/Production)

1. Open the successful `Build & Deploy to Dev` run for the release you want.
2. Copy its numeric run ID from the URL (this is `source_run_id`).
3. Trigger `Promote to Test` or `Promote to Production` with:
   - `confirm`: `yes`
   - `source_run_id`: `<that dev run id>`
4. Confirm the run summary shows the expected release SHA and artifact names.

This ensures Test and Production use the same frontend/backend bits that were built together.

### GitHub Environment Setup

Each environment (`dev`, `test`, `production`) needs these configured in GitHub Settings > Environments:

| Setting | `dev` | `test` | `production` |
|---|---|---|---|
| `BACKEND_URL` | required | required | required |
| `FRONTEND_URL` | required | required | required |
| `SMOKE_EMP_NO` | required | required | required |
| `SMOKE_EMP_PIN` | optional secret | optional secret | optional secret |
| `BACKEND_APP_NAME` | `qscmes-dev-app` | `qscmes-test-app` | `qscmes-prod-app` |
| `SWA_DEPLOYMENT_TOKEN` | *(from Dev SWA)* | *(from Test SWA)* | *(from Prod SWA)* |
| `AZURE_CREDENTIALS` | *(service principal)* | *(service principal)* | *(service principal)* |
| `SQL_ADMIN_USER` | — | *(for DB copy)* | *(for DB backup)* |
| `SQL_ADMIN_PASSWORD` | — | *(for DB copy)* | *(for DB backup)* |

The `test` and `production` environments should have **required reviewers** configured for an approval gate.
`BACKEND_URL` is required at build time and is injected into `VITE_API_URL`; CI now fails fast if missing.

## Local Preflight Before Deploy

Run the CI-aligned preflight before dispatching a Dev deploy:

```powershell
./scripts/preflight-dev.ps1
```

On bash-compatible shells:

```bash
./scripts/preflight-dev.sh
```

This runs backend restore/publish/tests and frontend build/coverage tests, matching what Dev verification expects.

## Local Git Hook: TypeScript + Optional Deploy Prompt

To fail fast before pushing code to GitHub, this repo includes a versioned pre-push hook at `.githooks/pre-push` that runs frontend TypeScript validation.
When pushing `origin/dev` from an interactive terminal, the hook can be used to trigger the Azure Dev deploy workflow.

One-time setup:

```bash
git config core.hooksPath .githooks
chmod +x .githooks/pre-push
```

What it runs:

```bash
npm --prefix frontend run typecheck
```

Behavior on `origin/dev` push:

- If you answer `No` (default), push continues and no deploy is triggered.
- If you answer `Yes`, push continues and the hook triggers `Build & Deploy to Dev` via GitHub CLI (`gh workflow run`).

This catches TypeScript compile issues locally before CI and lets you choose deploy-per-push.

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
