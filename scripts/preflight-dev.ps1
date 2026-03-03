param(
  [ValidateSet("tier1", "parity")]
  [string]$Tier = "parity"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
Set-Location $repoRoot

if ($Tier -eq "tier1") {
  Write-Host "Running fast verify tier (tier1)..."
  Push-Location "frontend"
  try {
    Write-Host "[1/2] Run targeted brittle-test guardrail suite"
    npm run test -- src/components/layout/TopBar.test.tsx src/features/admin/DowntimeEventsScreen.test.tsx src/features/admin/ProductionLineWorkCentersScreen.test.tsx

    Write-Host "[2/2] Run frontend typecheck"
    npm run typecheck
  }
  finally {
    Pop-Location
  }
  Write-Host "Tier1 verify passed."
  exit 0
}

Write-Host "Running parity preflight checks (CI-aligned)..."

Write-Host "[1/5] Restore backend dependencies"
dotnet restore "backend/MESv2.sln"

Write-Host "[2/5] Build backend release artifact"
dotnet publish "backend/MESv2.Api" -c Release -o "./publish/backend"

Write-Host "[3/5] Run backend tests (Release + coverage collector)"
dotnet test "backend/MESv2.Api.Tests" `
  --configuration Release `
  --verbosity normal `
  --settings "backend/MESv2.Api.Tests/coverage.runsettings" `
  --collect:"XPlat Code Coverage" `
  --results-directory "./TestResults"

Push-Location "frontend"
try {
  Write-Host "[4/5] Install frontend dependencies"
  npm ci

  Write-Host "[5/5] Run frontend CI verify (build + coverage tests)"
  npm run ci:verify
}
finally {
  Pop-Location
}

Write-Host "Parity preflight passed."
