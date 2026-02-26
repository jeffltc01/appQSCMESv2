# Lean Phases: Test and Rollout Strategy

## Purpose

Define mandatory automated tests and release gates for Lean maturity features before promotion to production.

## Testing Layers

## 1) Backend Unit Tests (xUnit)

- KPI math correctness (takt, queue-age percentiles, lead-time ratios).
- Timezone/day-boundary behavior for plant-local date windows.
- Andon state transitions and SLA escalation timing.
- RCA/CAPA closure guardrails (cannot close without effectiveness check).
- Replenishment signal creation logic and quantity rounding to containers.

### Suggested test files

- `backend/MESv2.Api.Tests/SupervisorDashboardLeanMetricsTests.cs`
- `backend/MESv2.Api.Tests/AndonServiceTests.cs`
- `backend/MESv2.Api.Tests/RcaServiceTests.cs`
- `backend/MESv2.Api.Tests/KanbanPolicyServiceTests.cs`

## 2) Backend Integration Tests

- API contract tests for new endpoints and DTO shape.
- Cross-entity linkage tests:
  - downtime -> andon,
  - andon -> RCA,
  - defect recurrence -> RCA case creation path.
- Persistence and query correctness for open/aging views.

### Suggested test files

- `backend/MESv2.Api.Tests/AndonApiIntegrationTests.cs`
- `backend/MESv2.Api.Tests/RcaApiIntegrationTests.cs`
- `backend/MESv2.Api.Tests/KanbanApiIntegrationTests.cs`

## 3) Frontend Tests (Vitest + RTL)

- Supervisor dashboard renders new KPI cards and trend series.
- Andon board state changes and escalation visual states.
- RCA workflow form validation and closure blockers.
- Replenishment queue/signal panels and exception badges.

### Suggested test files

- `frontend/src/features/admin/SupervisorDashboardLeanMetrics.test.tsx`
- `frontend/src/features/admin/AndonBoardScreen.test.tsx`
- `frontend/src/features/admin/RcaBoardScreen.test.tsx`
- `frontend/src/features/admin/KanbanSignalsScreen.test.tsx`

## Non-Functional Validation

- Query performance under realistic daily record volumes.
- API response latency p95 for supervisor dashboard under peak usage.
- Validate endpoint and workflow performance against `SPEC_PERFORMANCE_SLO.md` targets.
- Concurrency checks for card assignment and signal fulfillment actions.

## Rollout Gates

## Gate A: Dev Complete

- All unit tests green.
- New migrations generated and reviewed (if schema changed).
- Lint/type checks pass.

## Gate B: QA/UAT Ready

- Integration tests green.
- Performance validation green for critical SLO categories defined in `SPEC_PERFORMANCE_SLO.md`.
- End-to-end smoke checks pass for at least:
  - one takt metrics retrieval,
  - one andon lifecycle run,
  - one RCA closure,
  - one replenishment signal fulfillment.
- Product and operations sign-off on dashboard terminology.

## Gate C: Production Release

- Feature flags enabled by plant (staged rollout).
- Monitoring alerts configured:
  - API error rate,
  - escalation job failures,
  - stale open andon volume,
  - SLO alert thresholds from `SPEC_PERFORMANCE_SLO.md`.
- Runbook updated for plant support teams.

## Post-Release Success Metrics

- Mean acknowledgment time for andon events.
- Repeat defect and repeat downtime rates.
- Percentage of takt intervals meeting target.
- Number of emergency replenishments per line/week.
