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
| **Testing** | Vitest + React Testing Library (frontend), xUnit + Moq (backend) |

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
# Frontend
cd frontend
npm test

# Backend
cd backend
dotnet test
```

## Project Structure

```
appMESv2/
  designInput/          # Design specifications
  frontend/             # React + TypeScript SPA
  backend/              # ASP.NET Core Web API
    MESv2.Api/          # API project
    MESv2.Api.Tests/    # xUnit tests
    MESv2.sln
```
