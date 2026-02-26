#!/usr/bin/env bash
set -euo pipefail

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$REPO_ROOT"

echo "Running dev preflight checks (CI-aligned)..."

echo "[1/6] Restore backend dependencies"
dotnet restore backend/MESv2.sln

echo "[2/6] Build backend release artifact"
dotnet publish backend/MESv2.Api -c Release -o ./publish/backend

echo "[3/6] Run backend tests (Release + coverage collector)"
dotnet test backend/MESv2.Api.Tests \
  --configuration Release \
  --verbosity normal \
  --settings backend/MESv2.Api.Tests/coverage.runsettings \
  --collect:"XPlat Code Coverage" \
  --results-directory ./TestResults

cd frontend

echo "[4/6] Install frontend dependencies"
npm ci

echo "[5/6] Build frontend (typecheck + vite build)"
npm run build

echo "[6/6] Run frontend tests with coverage"
npm run test:coverage

echo "Dev preflight passed."
