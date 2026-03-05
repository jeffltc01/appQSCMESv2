# Prompt Pack: Heijunka + EPEI + Supermarket Scheduling (Full Build)

Use this file as the detailed execution contract for Cursor AI.

## Objective

Implement the hybrid scheduling feature end-to-end for MVP Phase 1, including:

1. Persistence model and migrations.
2. ERP demand ingestion/normalization.
3. Scheduling lifecycle and freeze controls.
4. Planner APIs and frontend workflows.
5. Final-scan execution capture integration.
6. Phase 1 KPI service and reporting readiness.
7. RBAC and governance enforcement.
8. Tests, smoke checks, and rollout readiness evidence.

## Source of Truth Files (Must Read First)

- `designInput/SPEC_HEIJUNKA_EPEI_SUPERMARKET_HYBRID_SCHEDULING.md`
- `designInput/GENERAL_DESIGN_INPUT.md`
- `designInput/SPEC_DATA_REFERENCE.md`
- `designInput/SECURITY_ROLES.md`
- `designInput/REFERENCE_ARCHITECTURE.md`
- `designInput/QSC_STYLE_GUIDE.md`
- `.cursor/rules/development-workflow.mdc`
- `.cursor/rules/post-change-verification.mdc`
- `.cursor/rules/fast-feedback-workflow.mdc`
- `AGENTS.md`

## Critical Implementation Notes

1. Keep controllers thin; business logic belongs in services.
2. Use Azure-ready configuration patterns (no hardcoded local-only assumptions in app logic).
3. Use phase 1 reduced KPI profile from the scheduling spec:
   - `ScheduleAdherencePercent`
   - `PlanAttainmentPercent`
   - `LoadReadinessPercent`
   - `SupermarketStockoutDurationMinutes`
4. Final execution signal originates at paint-line final scan (`1 scan = 1 finished tank`).
5. Do not force weld-line attribution when not traceable at final scan.
6. Prefer neutral resource modeling (e.g., `PlanningResourceId` / `ExecutionResourceId`) for phase 1.
7. Do not silently drop unmapped ERP demand; route it to exception workflow.

## Required Delivery Phases

Complete phases in order. After each phase, run targeted tests and report outcomes.

### Phase 1 - Foundation and Persistence

Implement/extend data model for:

- `Schedule`
- `ScheduleLine`
- `ScheduleChangeLog`
- `ErpDemandSnapshot`
- ERP SKU to planning group mapping
- `UnmappedDemandException`

Include indexes/constraints for idempotency and query performance.
Create EF migration and verify `Up()` / `Down()` correctness.

### Phase 2 - ERP Demand Ingestion and Normalization

Build ingestion pipeline to support:

- Raw landing in `ErpSalesOrderDemandRaw`
- Canonical normalization fields (`LoadGroupId`, dispatch date, qty, status, etc.)
- Effective-dated SKU mapping to planning groups
- Exception generation for unmapped SKUs
- Idempotent dedup behavior for incremental or full extracts

### Phase 3 - Scheduling Engine and Lifecycle

Implement deterministic rule-based scheduling (not an optimizer) with:

- Priority order:
  1) dispatch-constrained load demand,
  2) supermarket risk protection,
  3) wheel smoothing.
- Lifecycle states: `Draft -> Published -> InExecution -> Closed`
- Freeze-window policy and reasoned override workflow
- Revisioning and concurrency protections
- Change log capture with from/to values and reason codes

### Phase 4 - Planner API Surface

Implement API endpoints and DTOs for:

- draft create/read/update,
- publish/close/reopen,
- freeze-window overrides,
- schedule change history,
- mapping exceptions list/resolve/defer,
- dispatch and mapping risk summaries.

Enforce role/site scoping in service layer and controllers.

### Phase 5 - Planner Frontend MVP

Implement planner workflows:

- weekly planning board,
- risk panel,
- unmapped exception queue,
- publish checklist and publish action,
- freeze override modal with mandatory reason code.

Integrate with backend APIs and update frontend domain/api types.

### Phase 6 - Execution Capture Integration

Integrate final-scan events into execution capture with:

- required event metadata (site/resource/product/time/qty),
- idempotent ingestion,
- schedule matching strategy (direct line id preferred; deterministic fallback),
- support for `Completed`, `Short`, `Missed`, `Moved` outcomes and reason capture.

### Phase 7 - KPI Service (Phase 1 Only)

Implement KPI compute + API for only:

- `ScheduleAdherencePercent`
- `PlanAttainmentPercent`
- `LoadReadinessPercent`
- `SupermarketStockoutDurationMinutes`

Include KPI eligibility checks and explicit null reason codes.

### Phase 8 - RBAC + Governance

Map capabilities to existing role tiers:

- Planner (site-scoped schedule + mapping ownership),
- Supervisor (override approval/recovery actions),
- Plant Manager (accountability/escalation),
- Operator (view-only).

Ensure audit trail completeness for sensitive actions.

### Phase 9 - Verification and Rollout Readiness

Before completion:

1. Run targeted backend/frontend tests for changed features.
2. Run frontend typecheck.
3. Verify backend/frontend port + proxy consistency.
4. Run at least one API smoke test on live backend.
5. Validate one end-to-end flow:
   ERP demand -> normalization/mapping -> schedule draft/publish -> execution capture -> KPI output.
6. Report command list with pass/fail and remaining risks.

## Output Format Requirements

For every phase, provide:

1. Changed files list.
2. Brief rationale per change.
3. Test commands executed and results.
4. Open issues/risks.
5. Next phase start statement.

At final completion, provide:

- end-to-end summary,
- unresolved assumptions,
- recommended next sprint items.

