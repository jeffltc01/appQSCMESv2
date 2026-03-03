# Local CI Parity Checklist

Use this checklist before deploy-related pushes so local verification closely matches GitHub Verify.

## 1) Fast Daily Loop

- Run frontend fast lane:
  - `cd frontend`
  - `npm run verify:tier1`

Use this for routine check-ins while iterating.

## 2) Full Frontend Lane

- Run full frontend parity lane:
  - `cd frontend`
  - `npm run verify:tier2`

This matches the frontend CI chain (`build` + `test:coverage`).

## 3) Full Verify Parity (Ubuntu-Like Runtime)

Run from repo root after setting all required environment variables.

Required vars:

- `BACKEND_URL`
- `FRONTEND_URL`
- `SMOKE_ADMIN_EMP_NO`
- `SMOKE_OPERATOR_EMP_NO`
- `SMOKE_SUPERVISOR_EMP_NO`
- `PERF_EMP_NO`
- `PERF_PIN`
- `PERF_SITE_ID`

Commands:

- Windows host using WSL Ubuntu:
  - `powershell -ExecutionPolicy Bypass -File .\scripts\verify-parity-wsl.ps1`
- Bash/WSL directly:
  - `bash ./scripts/verify-parity.sh`

What this runs:

1. Input validation (same required contract as CI verify)
2. `scripts/preflight-dev.sh --tier parity`
3. Playwright install + operator smoke tests
4. SLO perf scenarios + queue reconciliation

## 4) Expected Runtime Guidance

- Tier 1: ~1-3 minutes
- Tier 2: ~4-10 minutes (depends on machine load)
- Full parity (with smoke + perf): ~10-20+ minutes

## 5) Deployment-Readiness Rule

Before triggering Promote/Test:

- Tier 2 must pass locally at minimum.
- Full parity should pass for high-risk or cross-cutting changes.
- Record command outputs and durations in PR notes when possible.
