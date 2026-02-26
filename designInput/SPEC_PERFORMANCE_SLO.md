# MES v2 — Performance SLO and Response Time Specification

## Purpose

Define measurable, release-gating performance and reliability targets for MES v2 user workflows, APIs, and background jobs.

This document is the canonical source for non-functional response-time expectations and operational thresholds.

## Scope

### In Scope

- User-facing response time targets (p50/p95/p99)
- API availability and error-rate targets
- Throughput/concurrency baseline assumptions
- Background processing latency targets (escalations, queue sync)
- Monitoring and alert thresholds
- Test and rollout gate criteria for performance readiness

### Out of Scope

- Detailed load test scripts and tooling implementation
- Cloud cost optimization strategy
- Per-query SQL optimization guidance

## Measurement Rules

- Percentiles are computed over rolling 15-minute windows and reviewed at hourly/day aggregates.
- Latency is measured server-side for API request duration and client-side for user-perceived interaction timing where applicable.
- SLO reporting excludes planned maintenance windows.
- Endpoint categories use consistent tagging in telemetry (`EndpointCategory`, `Feature`, `PlantId`).
- User-facing SLOs apply to production, not local development environments.

## User Response-Time SLOs

### 1) Operator Critical Actions

Examples: scan-driven save actions, pass/fail confirmations, queue-advance actions.

- p50: <= 250 ms
- p95: <= 800 ms
- p99: <= 1.5 s
- hard timeout: 3 s

### 2) Login Experience

`GET /users/login-config`

- p50: <= 200 ms
- p95: <= 500 ms
- p99: <= 1 s

`POST /auth/login`

- p50: <= 400 ms
- p95: <= 1.2 s
- p99: <= 2 s

### 3) Work Center Read APIs

Examples: history panels, small lookup/list reads.

- p50: <= 300 ms
- p95: <= 900 ms
- p99: <= 2 s

### 4) Supervisor Dashboard APIs

Examples: aggregate KPI and trend endpoints.

- p50: <= 700 ms
- p95: <= 2.5 s
- p99: <= 4 s

### 5) Rule Engine Evaluation

Aligned with `SPEC_ADVANCED_RULE_ACTION_ENGINE.md`.

- p95: <= 3 s per rule evaluation
- hard cap: 5 s per rule evaluation

## Background and Workflow Latency Targets

### Andon Escalation Processing

Time from SLA trigger due to escalation event persisted:

- p95: <= 60 s
- maximum acceptable drift: <= 2 minutes

### Offline Queue Resynchronization

After connectivity is restored:

- normal backlog flush completion: <= 5 minutes
- no data loss from queued actions

## Availability and Error-Rate SLOs

### API Availability

- Operator-critical endpoint monthly availability: 99.9%
- Non-critical dashboard/report endpoints monthly availability: 99.5%

### Error Budgets

- Operator-critical endpoint 5xx rate: < 0.5%
- Dashboard/report endpoint 5xx rate: < 1.0%

### Data Integrity

- Save/submit actions must be idempotent under retries.
- No duplicate production records caused by client or gateway retries.

## Throughput and Concurrency Baseline

- Baseline design target: 50 concurrent active sessions per plant without SLO breach.
- Burst target: 5x normal scan volume sustained for 30 seconds with no data loss.
- Queue-driven workflows must continue to process in FIFO order under burst.

## UX Performance Expectations

- Initial acknowledgement (visual feedback/loading indication) should appear within 100 ms of user action.
- If operation exceeds 1 s, show explicit processing state.
- If operation exceeds 3 s, show retry guidance while preserving user context where possible.
- Scan feedback overlays remain at ~1.5 to 2.0 seconds visual duration per existing UX specs.

## Monitoring and Alerting Thresholds

Trigger operational alerts when any condition persists beyond its threshold:

- Operator-critical API p95 > 1.2 s for 10 minutes
- Operator-critical API 5xx rate > 1.0% for 5 minutes
- Andon escalation drift > 2 minutes
- Queue resync failures > 0.5% of queued actions

## Rollout Gates (Performance)

### Gate A (Dev Complete)

- Endpoint instrumentation and telemetry tags are present.
- Unit/integration tests covering timeout and retry behavior are green.

### Gate B (QA/UAT Ready)

- Load test validates p95 targets for operator-critical actions and login flows.
- Supervisor dashboard latency p95 target validated under peak usage profile.
- Offline queue resync behavior validated in failure-injection scenarios.

### Gate C (Production Release)

- Alert rules enabled in monitoring platform.
- Runbook includes performance triage steps and rollback trigger thresholds.
- Two consecutive pre-release validation runs meet all critical SLOs.

## Acceptance Criteria

1. All in-scope endpoint categories have explicit p50/p95/p99 targets.
2. Performance targets are referenced by test and rollout documentation.
3. Monitoring includes alerts tied to this document's thresholds.
4. Operator-critical workflows meet SLOs under defined baseline and burst conditions.
5. Rule and escalation timing constraints remain aligned with their feature-specific specs.

## References

- `GENERAL_DESIGN_INPUT.md`
- `SPEC_LOGIN_SCREEN.md`
- `SPEC_OPERATOR_WC_LAYOUT.md`
- `SPEC_LEAN_TEST_AND_ROLLOUT_STRATEGY.md`
- `SPEC_LEAN_PHASE2_ANDON.md`
- `SPEC_ADVANCED_RULE_ACTION_ENGINE.md`
