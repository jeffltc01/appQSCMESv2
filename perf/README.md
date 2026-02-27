# Performance and Reconciliation Harness

This directory provides SLO validation tooling for:

- login endpoint latency (`GET /api/users/login-config`, `POST /api/auth/login`)
- 50-concurrent-session load simulation
- 5x burst load (30 seconds)
- work-center read latency (`GET /api/workcenters/{id}/history`, `GET /api/workcenters/{id}/material-queue`)
- supervisor dashboard read latency (`GET /api/supervisor-dashboard/{wcId}/metrics`, `GET /api/supervisor-dashboard/{wcId}/performance-table`)
- deterministic queue reconciliation (attempted vs persisted submissions)

## Run Perf Scenarios

```bash
BACKEND_URL=https://your-backend-url \
PERF_EMP_NO=EMP001 \
PERF_PIN=1234 \
PERF_SITE_ID=<site-guid> \
PERF_WC_ID=<work-center-guid> \
node perf/run-slo-scenarios.mjs
```

`PERF_WC_ID` is optional. When provided, work-center and supervisor read scenarios are included in the run.

Outputs:

- `perf/artifacts/perf-summary.json`
- `perf/artifacts/perf-summary.md`

## Run Reconciliation

```bash
RECON_ATTEMPTED_PATH=perf/input/attempted-queue-submissions.json \
RECON_PERSISTED_PATH=perf/input/persisted-queue-submissions.json \
node perf/reconcile-queue-results.mjs
```

Outputs:

- `perf/artifacts/queue-reconciliation-summary.json`
- `perf/artifacts/queue-reconciliation-summary.md`

## Input Contract (Reconciliation)

Input files are JSON arrays of objects with:

- `workCenterId` (string)
- `serialNumber` (string)
- `action` (string)
- `clientRequestId` (string; unique per client submission)

Any missing persisted key is flagged as lost. Any persisted count greater than attempted count is flagged as duplicate.
