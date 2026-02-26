Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
Set-Location $repoRoot

Write-Host "Running dev preflight checks (CI-aligned)..."

Write-Host "[1/6] Restore backend dependencies"
dotnet restore "backend/MESv2.sln"

Write-Host "[2/6] Build backend release artifact"
dotnet publish "backend/MESv2.Api" -c Release -o "./publish/backend"

Write-Host "[3/6] Run backend tests (Release + coverage collector)"
dotnet test "backend/MESv2.Api.Tests" `
  --configuration Release `
  --verbosity normal `
  --settings "backend/MESv2.Api.Tests/coverage.runsettings" `
  --collect:"XPlat Code Coverage" `
  --results-directory "./TestResults"

Push-Location "frontend"
try {
  Write-Host "[4/6] Install frontend dependencies"
  npm ci

  Write-Host "[5/6] Build frontend (typecheck + vite build)"
  npm run build

  Write-Host "[6/6] Run frontend tests with coverage"
  npm run test:coverage
}
finally {
  Pop-Location
}

Write-Host "Dev preflight passed."
