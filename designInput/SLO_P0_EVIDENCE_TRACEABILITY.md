# SLO P0 Evidence Traceability

This matrix maps P0 checklist items from `SLO_REMEDIATION_CHECKLIST.md` to concrete artifacts and enforcement points in the repository.

| Checklist ID | Requirement | Evidence Artifact(s) | Source / Workflow |
|---|---|---|---|
| A1 | Endpoint telemetry tags | `backend/MESv2.Api/Observability/SLO_ENDPOINT_INVENTORY.md` + App Insights request customDimensions (`EndpointCategory`, `Feature`, `PlantId`, `WorkCenterId`) | `backend/MESv2.Api/Observability/*`, `backend/MESv2.Api/Program.cs` |
| A2 | 15-min rolling p50/p95/p99 | `infra/observability/kql/operator_api_latency_15min.kql` | Query imported to Azure Monitor workbook |
| A3 | Server-side API duration measurement | Application Insights request telemetry + `operator_api_latency_15min.kql` outputs | `backend/MESv2.Api/Program.cs`, AI request telemetry |
| B1 | Canonical operator-critical endpoint list | `backend/MESv2.Api/Observability/SLO_ENDPOINT_INVENTORY.md` | Source-controlled inventory |
| B2 | Login endpoints in perf suite | `perf/artifacts/perf-summary.json` (`login_config_baseline`, `login_submit_baseline`) | `.github/workflows/build-test-package.yml` |
| C2 | Offline queue resync metrics | Frontend `queue_resync` telemetry events + `perf/artifacts/queue-reconciliation-summary.json` | `frontend/src/telemetry/telemetryClient.ts`, `perf/reconcile-queue-results.mjs` |
| C3 | No data loss during resync | `perf/artifacts/queue-reconciliation-summary.json` (`lostCount=0`, `duplicateCount=0`) | `.github/scripts/evaluate-slo-gates.py` |
| D1 | Operator-critical 5xx budget | `infra/observability/kql/operator_api_5xx_budget.kql` + alert template | `infra/observability/alerts/operator-5xx-alert.json` |
| D3 | Idempotent save/submit | Backend tests + idempotent service behavior in write paths | `backend/MESv2.Api.Tests/*ServiceTests.cs` |
| D4 | Duplicate/lost transaction detection | `perf/artifacts/queue-reconciliation-summary.json` | `perf/reconcile-queue-results.mjs` |
| E1 | 50 concurrent sessions | `perf/artifacts/perf-summary.json` (`concurrency_50_sessions`) | `perf/run-slo-scenarios.mjs` |
| E2 | 5x burst for 30s | `perf/artifacts/perf-summary.json` (`burst_5x_30s`) | `perf/run-slo-scenarios.mjs` |
| G1 | Operator API p95 alert | `infra/observability/alerts/operator-p95-latency-alert.json` | Azure Monitor scheduled-query alert |
| G2 | Operator API 5xx alert | `infra/observability/alerts/operator-5xx-alert.json` | Azure Monitor scheduled-query alert |
| G4 | Queue resync failure alert | `infra/observability/alerts/queue-resync-failure-alert.json` | Azure Monitor scheduled-query alert |
| H1 | Gate A CI green + evidence links | CI step summary with perf artifact references | `.github/workflows/build-test-package.yml` |
| H2 | QA/UAT perf + failure-injection evidence | `perf-summary.*` + `queue-reconciliation-summary.*` artifacts | `perf/artifacts/*` |
| H3 | Release gate blocks red SLO | `evaluate-slo-gates.py` fails deploy/promotion workflows on red metrics | `deploy.yml`, `promote-to-test.yml`, `promote-to-prod.yml` |

## Artifact Pack Location

Build-time artifact bundle:

- `perf/artifacts/perf-summary.json`
- `perf/artifacts/perf-summary.md`
- `perf/artifacts/queue-reconciliation-summary.json`
- `perf/artifacts/queue-reconciliation-summary.md`

## Notes

- Gate thresholds are controlled by env vars in `evaluate-slo-gates.py` (`SLO_P95_THRESHOLD_MS`, `SLO_MAX_ERROR_RATE_PCT`).
- Alert files in `infra/observability/alerts/` are source-of-truth templates and should be applied in Azure with environment-specific action groups.
