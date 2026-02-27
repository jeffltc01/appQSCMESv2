# MES v2 Rollout Executive One-Slide

## Decision Needed

Approve one of three rollout branches after Fremont v2 validation and go/no-go gate review.

- **Branch 1 (Standard):** Fremont validates and launches v2 in the next 4-5 weeks; West Jordan (WJ) migrates to v2 3-4 weeks after Fremont v2 go-live; Cleveland launches v2 in July (or per the latest approved Cleveland implementation schedule).
- **Branch 2 (Accelerated):** If Fremont is a clean success against defined go/no-go criteria, WJ fast-follows 1 week after Fremont v2 go-live; Cleveland remains on July v2 launch (or per the latest approved Cleveland implementation schedule). After WJ cutover, execute a separate IT-led historical data migration project to backfill WJ v1 transactions into v2 in phases, since historical records are not required for Day-1 v2 operations.
- **Branch 3 (Hybrid):** Fremont launches v2, and WJ prepares for accelerated cutover in parallel; at Fremont +2 weeks, execute a checkpoint decision. If Fremont KPIs remain green against go/no-go criteria, WJ cuts over at +2 weeks; if not, WJ defaults to +4 weeks. Cleveland remains on July v2 launch (or per the latest approved Cleveland implementation schedule). After WJ cutover, execute a separate IT-led historical data migration project to backfill WJ v1 transactions into v2 in phases, since historical records are not required for Day-1 v2 operations.

## Key Dates (Planning Baseline)

- **Now -> Late March 2026:** Fremont v2 UAT and pilot readiness.
- **Late April to May 2026:** Fremont v2 go-live and validation window.
- **Branch 1 (Standard):** WJ target cutover at Fremont +3 to +4 weeks.
- **Branch 2 (Accelerated):** WJ target cutover at Fremont +1 week (conditional on gates).
- **Branch 3 (Hybrid):** checkpoint at Fremont +2 weeks; WJ cutover at +2 weeks if green, otherwise +4 weeks.
- **July 2026:** Cleveland v2 go-live window.
- **Post WJ cutover (30-90 days):** Deferred WJ historical data backfill in waves.

## Gate Criteria (For Branch 2 and Branch 3 Checkpoint)

- Fremont completes **2 consecutive weeks stable production** on v2.
- No unresolved Sev1 defects in operator-critical workflows.
- Performance meets `SPEC_PERFORMANCE_SLO.md` critical thresholds:
  - operator-critical p95 <= 800 ms, p99 <= 1.5 s, hard timeout <= 3 s,
  - operator-critical 5xx < 0.5%.
- Data integrity holds: no duplicate/lost production transactions.
- Fremont supervisor and operator signoff completed.
- For **Branch 3**, apply the same criteria at the Fremont +2 week checkpoint before committing to WJ +2 week cutover.

## Data Migration Strategy (WJ)

- **Day-1 cutover scope:** active and operational data only (open WIP/work, active holds/exceptions, users/roles, current config/master data).
- **Deferred history approach:** keep v1 read-only for historical lookup and backfill historical transactions after go-live in controlled waves.
- **Why this matters:** faster value realization with lower cutover risk and no dependency on moving full history at go-live.

## Top Risks and Mitigations

- **Risk:** Fremont success does not generalize to WJ line specifics.  
**Mitigation:** WJ cutover rehearsal plus WJ-specific UAT before go/no-go.
- **Risk:** Branch ambiguity delays execution and staffing alignment.  
**Mitigation:** lock branch selection and decision owner at steering review, with a dated checkpoint.
- **Risk:** Hypercare load from overlapping WJ and Cleveland readiness.  
**Mitigation:** fixed staffing plan, blackout on non-critical scope, escalation command center.
- **Risk:** Historical data confusion during deferred migration.  
**Mitigation:** v1 read-only retention policy, clear user access SOP, reconciliation signoff by Ops/Quality/Finance.
- **Risk:** Performance regression under live bursts.  
**Mitigation:** enforce SLO alert thresholds and automatic rollback triggers in first 14 days.

## Rollback Triggers (First 14 Days Post-Cutover)

- Any data integrity event (duplicate/lost transactions).
- Operator-critical API p95 > 1.2 s for 10 minutes.
- Operator-critical API 5xx > 1.0% for 5 minutes.
- Sev1 workflow outage not mitigated within agreed incident window.

## Executive Ask

- Approve one rollout branch (**Standard**, **Accelerated**, or **Hybrid**) with decision owner and date.
- Approve gate criteria as release authority for Branch 2 and Branch 3 checkpoint decisions.
- Approve **deferred historical migration** model for WJ.
- Confirm owner assignments and weekly governance cadence through July 2026.

