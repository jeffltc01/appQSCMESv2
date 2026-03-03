#!/usr/bin/env bash
set -euo pipefail

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$REPO_ROOT"

TIER="parity"
while [[ $# -gt 0 ]]; do
  case "$1" in
    --tier)
      TIER="${2:-}"
      shift 2
      ;;
    *)
      echo "Unknown argument: $1" >&2
      exit 1
      ;;
  esac
done

if [[ "$TIER" == "tier1" ]]; then
  echo "Running fast verify tier (tier1)..."
  cd frontend
  echo "[1/2] Run targeted brittle-test guardrail suite"
  npm run test -- src/components/layout/TopBar.test.tsx src/features/admin/DowntimeEventsScreen.test.tsx src/features/admin/ProductionLineWorkCentersScreen.test.tsx
  echo "[2/2] Run frontend typecheck"
  npm run typecheck
  echo "Tier1 verify passed."
  exit 0
fi

if [[ "$TIER" != "parity" ]]; then
  echo "Invalid tier '${TIER}'. Expected 'tier1' or 'parity'." >&2
  exit 1
fi

echo "Running parity preflight checks (CI-aligned)..."

echo "[1/5] Restore backend dependencies"
dotnet restore backend/MESv2.sln

echo "[2/5] Build backend release artifact"
dotnet publish backend/MESv2.Api -c Release -o ./publish/backend

echo "[3/5] Run backend tests (Release + coverage collector)"
dotnet test backend/MESv2.Api.Tests \
  --configuration Release \
  --verbosity normal \
  --settings backend/MESv2.Api.Tests/coverage.runsettings \
  --collect:"XPlat Code Coverage" \
  --results-directory ./TestResults

cd frontend

echo "[4/5] Install frontend dependencies"
npm ci

echo "[5/5] Run frontend CI verify (build + coverage tests)"
npm run ci:verify

echo "Parity preflight passed."
