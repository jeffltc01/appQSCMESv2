# Operator Entry CI Gate Plan

## Purpose

Define a practical first implementation of operator-entry protection gates using existing workflows:

- `.github/workflows/verify-dev-build.yml`
- `.github/workflows/build-test-package.yml`
- `.github/workflows/deploy.yml`
- `.github/workflows/promote-to-test.yml`
- `.github/workflows/promote-to-prod.yml`

This document is a rollout plan. It does not immediately alter workflow behavior until the listed steps are implemented.

## Current Baseline (already present)

1. **Build + tests in reusable workflow**
   - Backend publish
   - Frontend build
   - Backend tests with coverage
   - Frontend tests with coverage
   - Optional Playwright E2E (`run_e2e` input)

2. **Promotion/deploy smoke checks**
   - `post-deploy-smoke.sh` runs in dev, test, and prod promotion flows.

This is a strong baseline; operator-specific enforcement should be layered on top.

## Target Gates

## Gate A - PR/Dev Verification (mandatory)

### Objective

Fail early on operator-regression risk before deployment.

### Mapping to existing workflows

- Trigger path: `verify-dev-build.yml` -> `build-test-package.yml`
- Add policy:
  - Operator-related changes must run operator smoke E2E (`run_e2e: true`) or equivalent targeted operator test subset.

### First implementation step

1. Add path/label policy in PR process:
   - If changed files include operator features, operator layout, shared auth/session, barcode handling, or shared operator DTOs, mark PR as operator-impacting.
2. For operator-impacting PRs/commits to `dev`, run `build-test-package` with `run_e2e: true`.
3. Require green status for:
   - `Run backend tests with coverage`
   - `Run frontend tests with coverage`
   - `Run E2E tests` (operator subset or full)

## Gate B - Deploy to Dev (mandatory)

### Objective

Ensure deployed dev environment still supports operator-critical API chain.

### Mapping to existing workflow

- Trigger path: `deploy.yml` -> job `post-deploy-smoke`
- Keep existing smoke script as required.
- Expand script assertions to include at least one operator-critical endpoint path.

### First implementation step

Update `.github/scripts/post-deploy-smoke.sh` to validate:

1. login-config endpoint
2. one operator read endpoint (for example WC history/read endpoint)
3. one safe operator write-path verification endpoint or write simulation where supported

If any fail, `post-deploy-smoke` fails and deployment is treated as non-promotable.

## Gate C - Promote to Test/Prod (mandatory)

### Objective

Prevent promotion if operator smoke evidence is missing.

### Mapping to existing workflows

- `promote-to-test.yml` -> `post-deploy-smoke`
- `promote-to-prod.yml` -> `post-deploy-smoke`

### First implementation step

1. Keep smoke jobs required.
2. Add summary output lines explicitly showing operator smoke outcomes.
3. Optionally add a manifest flag recording whether operator smoke passed in source environment.

## Recommended Operator Test Inventory (initial)

Use existing tests and add targeted coverage incrementally.

### Existing likely-relevant coverage

- Frontend:
  - `frontend/src/components/layout/OperatorLayout.test.tsx`
  - `frontend/src/features/*/*.test.tsx` for major operator screens
- Backend:
  - work center and production services tests in `backend/MESv2.Api.Tests`
- E2E:
  - `e2e/tests/operator-longseam.spec.ts`
  - `e2e/tests/operator-nameplate.spec.ts`
  - `e2e/tests/production-flow.spec.ts`
  - `e2e/tests/login.spec.ts`
  - `e2e/tests/tablet-setup.spec.ts`

### Minimum operator smoke subset (fast)

1. login -> tablet setup/routing
2. open one operator screen
3. perform one operator transaction/save path
4. verify success indication and persisted/read-back evidence

## Enforcement Sequence

### Stage 1 (now)

- Adopt PR template requiring operator-impact declaration.
- Define branch protection checks for verify workflow status.
- Keep E2E optional globally, required for operator-impacting changes.

### Stage 2

- Make operator smoke subset mandatory for all merges to `dev`.
- Add operator smoke result summary artifacts.

### Stage 3

- Add performance budget checks for operator-critical API p95 thresholds.
- Add telemetry-driven release hold if operator SLOs degrade after deploy.

## Ownership and Operations

- Assign gate ownership to operator-entry maintainers.
- Treat gate failures as release blockers, not informational warnings.
- Review failures weekly to refine flaky tests and keep the gate trustworthy.

## Success Criteria

1. Operator-impacting changes cannot reach test/prod without green operator smoke.
2. Smoke checks in deploy/promote workflows prove operator-critical API viability.
3. Regressions are detected before plant-floor users experience them.
