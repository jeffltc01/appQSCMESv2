# Azure Line-of-Business App Reference Architecture

## Purpose of this document

This document defines the **standard stack, architecture, and Azure resources** for our internal line-of-business (LOB) applications. It is intended to be given to **Cursor or other AI assistants** so they understand our defaults when proposing designs, generating code, and planning deployments.

---

## High-level stack summary

- **Frontend**
  - **Framework**: React + TypeScript
  - **Bundler/Meta-framework**: Vite or Next.js (per app; Next.js preferred when SSR or API routes are useful)
  - **UI library**: Fluent UI
- **Backend**
  - **Platform**: ASP.NET Core Web API, .NET 8 LTS
  - **ORM**: Entity Framework Core
- **Data**
  - **Primary database**: Azure SQL Database
  - **Optional**: Dataverse for apps that must integrate with an existing Dataverse data model
- **Authentication & authorization**
  - **Corporate users**: Microsoft Entra ID (Azure AD)
  - **Shop-floor/non-AD users**: Microsoft Entra External ID / B2C with local accounts
  - **App-level roles/permissions**: Implemented in our own database (RBAC)
- **Hosting**
  - **Backend**: Azure App Service (Web App for .NET)
  - **Frontend**: Azure Static Web Apps for SPAs or Azure App Service (Web App for Node/Static)
- **Dev workflow**
  - **Source control**: GitHub
  - **CI/CD**: GitHub Actions
  - **Editors**: VS Code and Cursor as primary IDE/AI pair programmer

When AI proposes solutions, it should **default to this stack** unless there is a strong reason to choose otherwise.

Get UI styling information from https://ltcorp-inc.com/.

---

## Logical architecture

### Layers

- **Presentation layer (frontend)**
  - React + TypeScript SPA or Next.js app
  - UI components built with Fluent UI
  - Talks to backend via JSON over HTTPS (REST APIs)
  - Handles token acquisition via MSAL (for Entra ID/External ID)

- **API layer (backend)**
  - ASP.NET Core Web API (.NET 8)
  - Controllers or minimal APIs for feature endpoints
  - Application services layer for business logic
  - Data access via EF Core, repository/specification or similar patterns as needed

- **Data & integration layer**
  - Azure SQL Database as main data store
  - Optional Dataverse integration (for apps extending existing Dataverse environments)
  - Other Azure services as required (Storage, Service Bus, etc.)

### Logical flow (high-level)

```mermaid
flowchart LR
  subgraph users [Users]
    corporateUser["Corporate user (Entra ID)"]
    shopUser["Shop-floor user (External ID/B2C)"]
  end

  frontendApp["Frontend app (React/Next.js)"]
  api["Backend API (ASP.NET Core)"]
  db[("Azure SQL Database")]
  dataverse["Dataverse (optional)"]

  corporateUser --> frontendApp
  shopUser --> frontendApp

  frontendApp -->|HTTPS + JSON| api
  api -->|EF Core| db
  api --> dataverse
```

---

## Authentication and authorization

### Identity providers

- **Corporate users**
  - Auth via **Entra ID** in our primary tenant
  - Frontend uses **MSAL** to obtain ID/access tokens
  - Backend validates JWT bearer tokens from Entra ID

- **Shop-floor / external users**
  - Auth via **Entra External ID / B2C** with local accounts (email/password or other supported mechanisms)
  - Frontend uses MSAL (B2C authority/user flows) to obtain tokens
  - Backend validates JWT tokens from the B2C tenant/configured policies

### App-level RBAC

- We maintain **application roles and permissions in our database**, not only in Entra roles.
- Core tables (conceptual):
  - `Users` (linked to identity provider via `sub` / `oid` and issuer)
  - `Roles` (e.g., `Operator`, `Supervisor`, `Quality`, `Maintenance`, `Admin`)
  - `UserRoles` (many-to-many assignments)
- Backend exposes role information to the frontend where needed and enforces authorization on APIs.

### Auth flow diagram

```mermaid
sequenceDiagram
  participant CorporateUser
  participant ShopUser
  participant Frontend as FrontendApp
  participant EntraID as EntraID
  participant ExternalID as EntraExternalID_B2C
  participant API as BackendAPI

  CorporateUser->>Frontend: Navigate to app
  Frontend->>EntraID: Login request (MSAL)
  EntraID-->>Frontend: ID token + access token
  Frontend->>API: API call with bearer token
  API->>EntraID: Validate token (metadata/keys)
  API-->>Frontend: Data response

  ShopUser->>Frontend: Navigate to app
  Frontend->>ExternalID: B2C login/signup
  ExternalID-->>Frontend: ID token + access token
  Frontend->>API: API call with bearer token
  API->>ExternalID: Validate token (metadata/keys)
  API-->>Frontend: Data response
```

AI assistants should:

- Use **MSAL** on the frontend (React) for token acquisition.
- Use standard **JWT bearer authentication middleware** in ASP.NET Core for token validation.
- Respect and extend the **database-backed RBAC model** when adding new features.

---

## Azure resource reference

### Core resources per application

- **Resource Group**
  - Contains Web Apps, databases, storage, and monitoring for a given app or set of related apps.

- **App Service Plan**
  - Shared plan if multiple small apps coexist, or dedicated for larger apps.

- **Backend Web App (Azure App Service)**
  - Type: Web App for .NET
  - Hosts the ASP.NET Core Web API
  - Configured with:
    - Managed identity (for Key Vault, databases, etc.)
    - App settings: connection strings, feature flags, environment variables

- **Frontend hosting**
  - **Preferred**: Azure Static Web Apps for SPA/Next.js static output
  - **Alternative**: Azure App Service (Web App) serving static build artifacts

- **Database**
  - Azure SQL Database
  - Single database or elastic pool depending on multi-tenant needs

- **Identity**
  - Entra ID app registration(s) for:
    - Frontend SPA
    - Backend API (if using separate app registrations)
  - Entra External ID / B2C tenant and user flows/policies for shop-floor/local accounts

- **Secrets & configuration**
  - Azure Key Vault (for secrets)
  - App Service Application Settings for non-secret config

- **Monitoring & logging**
  - Application Insights for backend
  - Diagnostic settings to Log Analytics where appropriate

- **Storage (optional)**
  - Azure Storage Account (Blob/File) for documents, images, exports

### Optional/advanced resources

- **Dataverse environment**
  - Used when extending existing Dataverse-based apps or data models
  - Accessed via SDKs/API from the backend

- **Messaging & background processing**
  - Azure Service Bus or Storage Queues for async processing (if required)
  - Timer-triggered functions or background jobs if we add background processing (not default)

- **Networking**
  - Virtual Network integration and private endpoints for databases/Key Vault for higher security environments

### High-level deployment diagram

```mermaid
flowchart TB
  subgraph rg [Azure Resource Group]
    subgraph networking [Networking]
      vnet["(Optional) Virtual Network"]
    end

    subgraph appServicePlan [App Service Plan]
      backendWebApp["Backend Web App (ASP.NET Core)"]
      frontendWebApp["Frontend Web App / Static Web App"]
    end

    db[("Azure SQL Database")]
    keyVault["Azure Key Vault"]
    appInsights["Application Insights"]
    storage["Azure Storage (optional)"]
  end

  users["Users (Corporate + Shop-floor)"]
  users --> frontendWebApp
  frontendWebApp --> backendWebApp
  backendWebApp --> db
  backendWebApp --> keyVault
  backendWebApp --> appInsights
  backendWebApp --> storage
```

---

## Development workflow and conventions

### Repositories

- Each application typically has **a single GitHub repository** with:
  - `frontend/` (React/Next.js app)
  - `backend/` (ASP.NET Core Web API)
  - `.github/workflows/` for CI/CD pipelines
  - `infra/` (optional) for Bicep/Terraform/Azure CLI scripts

### CI/CD with GitHub Actions

- **Build pipeline** (per app):
  - Trigger: push/PR to `main` or `develop` branches
  - Steps:
    - Install dependencies (frontend & backend)
    - Run linting/tests (as configured)
    - Build frontend (Vite/Next) and backend (`dotnet publish`)

- **Deploy pipeline**:
  - Trigger: on merge to `main` or on tag
  - Steps:
    - Deploy backend to Azure App Service
    - Deploy frontend to Azure Static Web Apps or Web App
    - Optionally run database migrations on deployment (EF Core migrations)

AI assistants should:

- Prefer **GitHub Actions** YAML for automation examples.
- Use `dotnet` CLI for backend builds and `npm`/`pnpm`/`yarn` (as chosen) for frontend.
- Treat **infrastructure and pipelines as code**, adding or updating YAML/templates instead of manual portal steps when possible.

### Local development

- Frontend and backend run independently on local ports (e.g., `localhost:5173` for Vite, `localhost:5000` for API).
- Frontend dev server proxies API calls to the backend during development.
- Developers use VS Code/Cursor with AI assistance for feature work.

---

## Dataverse usage

- Default storage is **Azure SQL Database**.
- **Use Dataverse only when**:
  - We are extending a solution that already uses Dataverse.
  - We must integrate tightly with existing Power Platform components.
- When Dataverse is used:
  - The ASP.NET Core backend communicates with Dataverse via official SDKs or REST APIs.
  - Azure SQL may still be used for app-specific data that does not belong in Dataverse.

AI assistants should **ask before assuming Dataverse** and default to Azure SQL unless explicitly overridden.

---

## Expectations for AI-generated designs and code

When generating designs, code, or deployment plans for these apps, AI assistants should:

- **Default to this stack** for new LOB apps unless explicitly instructed otherwise.
- Use **React + TypeScript** on the frontend, with Fluent UI or MUI.
- Use **ASP.NET Core Web API (.NET 8)** with **EF Core** and **Azure SQL**.
- Integrate authentication and authorization using **Entra ID** and **Entra External ID/B2C**, plus database-backed RBAC.
- Target **Azure App Service and Azure Static Web Apps** for hosting, with **GitHub Actions** for CI/CD.
- Keep solutions **simple and maintainable**, suitable for a small team managing multiple standalone apps.

