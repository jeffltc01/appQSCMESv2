param()

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$repoRootWsl = (& wsl.exe wslpath -a $repoRoot).Trim()

if (-not $repoRootWsl) {
  throw "Unable to resolve repository path in WSL."
}

Write-Host "Running CI-parity chain inside WSL Ubuntu..."
wsl.exe bash -lc "cd '$repoRootWsl' && bash ./scripts/verify-parity.sh"
if ($LASTEXITCODE -ne 0) {
  exit $LASTEXITCODE
}
