# MES v2 - Operator Performance Playbook

## Purpose

Define operator-first performance behavior so temporary slowness never blocks plant-floor throughput.

This playbook supplements `SPEC_PERFORMANCE_SLO.md` by adding workflow-level behavior, degradation rules, and release checks specifically for operator data entry.

## Scope

### In scope

- Operator login and work center entry
- Scan-driven and manual submit actions
- Work center history and required lookups
- Degraded-mode and offline behavior

### Out of scope

- Supervisor analytics latency optimization
- Broad cost/perf tuning unrelated to operator flow

## Operator Performance Principle

Operator flow continuity is the top priority:

1. Acknowledgement must be immediate.
2. Data capture must remain safe and idempotent.
3. Non-critical UI must degrade before critical actions degrade.

## Canonical Targets (Inherited)

Use `SPEC_PERFORMANCE_SLO.md` as canonical thresholds:

- Operator critical actions: p50 <= 250ms, p95 <= 800ms, p99 <= 1.5s, timeout 3s
- Login config: p95 <= 500ms
- Auth login: p95 <= 1.2s
- Operator-critical 5xx rate < 0.5%
- Save actions idempotent under retries

## Operator-Specific UX Budget Rules

1. **Ack budget**: Show visual action acknowledgement within 100ms.
2. **Processing budget**: If action exceeds 1s, show explicit in-progress state.
3. **Slow-path budget**: If action exceeds 3s, preserve context and provide retry guidance.
4. **Scan feedback**: Keep green/red overlay behavior consistent (~1.5 to 2.0s) per operator layout spec.

## Load and Render Strategy

1. **Essential-first render**
   - Render core action controls before secondary panels.
   - Operator can scan/submit even if history panel is still loading.

2. **Deferred non-critical data**
   - Defer or lazy-load `WC History`, secondary lookups, and non-required widgets.
   - On failure, show stale-but-labeled data rather than blocking the full screen.

3. **Stable interaction during refresh**
   - Background polling and refresh must not reset input state or scanner focus.
   - Never drop pending operator input due to non-critical panel updates.

## Network and Failure-Mode Rules

1. **Retry-safe submit**
   - Submission endpoints must remain idempotent.
   - Client retries must not create duplicate production records.

2. **Offline queue fallback**
   - If API is temporarily unreachable, queue safe actions locally.
   - Sync in order when connection returns; retain operator-visible status.

3. **Progressive degradation**
   - Degrade in this order:
     - secondary panels and diagnostics
     - optional read refresh
     - never primary submit and validation path

4. **Preserve context**
   - On failure/retry prompts, keep serial, selected values, and unsent state intact.

## Endpoint Priority Tiers

### Tier 0 - Must stay fast

- `GET /api/users/login-config`
- `POST /api/auth/login`
- write/submit endpoints used in operator cycle (for example production/inspection/nameplate/round seam submit paths)

### Tier 1 - Important but degradable

- `GET /api/workcenters/{id}/history`
- small lookups needed for current station if not preloaded

### Tier 2 - Defer first under load

- non-essential diagnostics and secondary informational data

## Telemetry Contract

All operator-critical endpoints/events should include tags:

- `Feature=OperatorEntry`
- `EndpointCategory=OperatorCritical`
- `PlantId`
- optional: `WorkCenterId`, `InputMode` (`External`/`Manual`)

Track at minimum:

- p50/p95/p99 latency by endpoint
- 4xx/5xx rates by endpoint
- submit volume and retry rate
- duplicate-submit prevention outcomes
- offline queue depth and flush duration

## Alerting Rules (Operator Focus)

Start with thresholds aligned to `SPEC_PERFORMANCE_SLO.md`:

- Operator-critical p95 > 1.2s for 10 minutes
- Operator-critical 5xx > 1.0% for 5 minutes
- Queue resync failures > 0.5%

Operational response:

1. Confirm scope by plant/work center.
2. Disable or throttle non-critical reads if needed.
3. Preserve submit path and scanner responsiveness.
4. Escalate to rollback/feature-flag disable for recent risky shared changes.

## CI and Release Checks

## Gate A - Dev/PR

- Operator impact detection enabled in verify workflow.
- Operator smoke E2E runs when operator-impacting paths change.
- Contract tests pass for operator read + write payload shape.

## Gate B - QA/UAT

- Validate operator critical p95 targets under expected station concurrency.
- Validate offline/reconnect and duplicate-submit protections.
- Validate scanner-mode and manual-mode parity for critical actions.

## Gate C - Production

- Alerts active for operator-critical latency/error thresholds.
- Rollback/kill-switch path documented and tested.
- Post-deploy smoke confirms login + read + write operator chain.

## Implementation Sequence

1. **Now**
   - Keep Stage 1 CI enforcement active.
   - Maintain operator API contract tests for critical read/write paths.

2. **Next**
   - Add endpoint-level dashboard by plant and work center.
   - Add fast load/perf smoke checks to CI summary.

3. **Hardening**
   - Add synthetic canary for operator submit flow.
   - Add automatic feature degradation toggles for non-critical read load.

## Acceptance Criteria

1. Operator can continue safe data entry during transient slowness.
2. Operator submit path remains available and idempotent under retries.
3. Non-critical UI degradation does not block primary work center actions.
4. Release gates fail when operator latency/error thresholds are exceeded.
5. Telemetry and alerts isolate performance issues by plant/work center quickly.

## References

- `designInput/SPEC_PERFORMANCE_SLO.md`
- `designInput/SPEC_OPERATOR_WC_LAYOUT.md`
- `designInput/OPERATOR_ENTRY_PROTECTION_STRATEGY.md`
- `designInput/SPEC_LEAN_TEST_AND_ROLLOUT_STRATEGY.md`
