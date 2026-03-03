# AGENTS

This repository contains a full-stack MES application:
- Frontend: React + TypeScript + Vite in `frontend/`
- Backend: ASP.NET Core Web API (.NET 8) in `backend/MESv2.Api/`
- Frontend tests: Vitest
- Backend tests: xUnit

## Working Agreement

- Do not commit secrets, tokens, or connection strings.
- Prefer targeted tests first, then broader verification when risk increases.
- Keep controller classes thin; place business logic in services.
- Follow project rules in `.cursor/rules/` for workflow and verification requirements.

## Cursor Cloud specific instructions

### Environment and dependencies

- Use Node 20.x and .NET 8 SDK.
- Install dependencies from repo root:
  - `npm ci --prefix frontend`
  - `npm ci --prefix e2e`
  - `dotnet restore backend/MESv2.Api/MESv2.Api.csproj`
  - `dotnet restore backend/MESv2.Api.Tests/MESv2.Api.Tests.csproj`

### Common commands

- Frontend dev server:
  - `npm run dev --prefix frontend -- --host 0.0.0.0 --port 5173`
- Frontend checks:
  - `npm run test --prefix frontend -- <test-path-or-name>`
  - `npm run typecheck --prefix frontend`
- Backend run:
  - `dotnet run --project backend/MESv2.Api/MESv2.Api.csproj --urls http://0.0.0.0:5001`
- Backend tests:
  - `dotnet test backend/MESv2.Api.Tests/MESv2.Api.Tests.csproj --filter "<ClassOrMethodFilter>"`

### Port and proxy notes

- Backend local port should be `5001` for API calls.
- Frontend local port should be `5173`.
- If frontend API calls fail, confirm Vite proxy target matches the backend port.

### Smoke test guidance

After backend start, run at least one API smoke check before claiming readiness:
- `curl http://localhost:5001/healthz`

If this fails, inspect backend startup logs first.
