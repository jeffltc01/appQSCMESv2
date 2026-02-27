# MES v2 - Operator Entry Protection Strategy

## Purpose

Define enforceable guardrails so `Operator Data Entry` remains stable, consistent, and performant as Scheduling and Quality/Ops capabilities continue to evolve.

This strategy is intentionally release-focused: it defines architecture boundaries, required test gates, and operational controls that prevent regressions in operator-critical workflows.

## Scope

### In scope

- Operator work center data-entry workflows (scan and manual fallback)
- Shared backend/frontend changes that can indirectly impact operator entry
- CI/CD gates required before merge and release
- Performance and reliability controls for operator-critical APIs

### Out of scope

- Full rewrite of current module boundaries
- One-time migration away from all existing shared dependencies
- Supervisory analytics feature design details

## Priority Statement

`Operator Data Entry` is a protected product surface. If there is a conflict between new feature velocity and operator-entry stability/performance, stability/performance wins.

## Core Architectural Guardrails

1. **Bounded context**
  - Treat operator entry as a distinct domain slice in both frontend and backend.
  - Operator screens and services must not directly depend on scheduling-specific business logic.
2. **Dependency direction**
  - Allowed: Operator Entry -> shared platform utilities.
  - Not allowed: Operator Entry -> Scheduling domain logic, Operator Entry -> Quality/Ops domain logic.
  - Allowed: Scheduling and Quality/Ops consume operator-entry outputs.
3. **Shared model isolation**
  - Avoid binding operator UI directly to broad shared DTOs when a narrower operator contract is sufficient.
  - Add mapping/adapters at boundaries to absorb non-operator changes.
4. **Controller thinness**
  - Keep business decisions in services, not controllers.
  - Controller actions for operator endpoints should orchestrate and validate only.
5. **No text-matching routing logic**
  - Screen/workflow routing must use canonical keys (for example `DataEntryType`) and IDs, not work center display-name matching.

## Contract Stability Policy

1. **Contract-first APIs**
  - Treat operator endpoint request/response shapes as stable contracts.
  - Add API contract tests for operator-critical endpoints.
2. **Breaking change policy**
  - Do not make in-place breaking changes to operator contracts.
  - Use additive evolution or versioning (`/api/v2/...`) for unavoidable breaks.
3. **Backward compatibility window**
  - Keep prior contract version active until frontend rollout is complete across plants.

## Performance and Reliability Requirements

Use `SPEC_PERFORMANCE_SLO.md` as canonical thresholds; operator-entry paths are release-gated by these targets.

- Operator critical actions:
  - p50 <= 250ms
  - p95 <= 800ms
  - p99 <= 1.5s
  - hard timeout 3s
- Operator-critical 5xx rate < 0.5%
- Save actions must be idempotent (no duplicate production records under retries)

## Operator-Critical Workflow Inventory

At minimum, treat these as protected:

- Login and workstation entry routing
- External input (barcode) capture and command handling
- Save/submit actions at operator work centers (`Rolls`, `Long Seam`, `Fitup`, `Round Seam`, `Nameplate`, `Hydro`, queue stations)
- Welder gate enforcement and session-dependent submission checks
- WC history refresh after save

## Required Test Gates

## Gate 1 - Pull Request (branch protection target)

Run when PR touches backend/frontend shared code or operator files:

- Backend unit/integration tests for impacted operator services
- Frontend tests for operator layout/screen behavior
- At least one operator E2E smoke path
- Contract validation for modified operator endpoints

## Gate 2 - Verify Dev Build

In `verify-dev-build.yml`/`build-test-package.yml`:

- Keep existing backend and frontend tests green
- Add an operator-critical test subset result section in workflow summary
- If `run_e2e` is enabled, include operator smoke scenario pass/fail in gating decision

## Gate 3 - Deploy/Promote

In deploy and promotion workflows:

- Keep post-deploy smoke checks mandatory
- Require successful operator endpoint smoke (for example login config + operator read endpoint + one write-safe check endpoint)
- Block promotion if smoke fails

## PR Governance

Every PR must declare whether operator entry is potentially impacted and how it was validated.

Minimum required metadata:

- Impact area selection (Operator Entry / Scheduling / Quality-Ops / Shared)
- Risk level
- Operator validation evidence (tests run + results)
- Rollback or kill-switch note for high-risk shared changes

## Feature Flags and Safe Rollout

- Any high-risk shared change must be wrapped in a feature flag where practical.
- Rollout order: dev -> test -> pilot plant -> full rollout.
- Have a fast disable path for non-critical dependent features if operator-entry telemetry degrades.

## Observability Standards

Add and maintain telemetry segmentation for operator workflows:

- Endpoint tags: `Feature=OperatorEntry`, `EndpointCategory=OperatorCritical`, `PlantId`
- Dashboard panels: p95 latency, 5xx rate, submit volume, duplicate-submit detection
- Alerts aligned to `SPEC_PERFORMANCE_SLO.md` thresholds

## Ownership and Change Control

- Assign explicit code owner(s) for operator-entry backend and frontend surfaces.
- Require operator-entry owner approval on PRs touching:
  - shared auth/session logic
  - shared DTOs used by operator APIs
  - operator routing, barcode parsing, submission pipelines
  - CI release scripts affecting test execution order or smoke checks

## Implementation Roadmap

### Phase 1 (immediate)

- Adopt PR template with operator impact declaration and required validation checklist
- Document and publish CI gate plan aligned to current workflows
- Label high-risk PRs (`operator-critical`, `shared-contract`, `session/auth`)

### Phase 2 (next)

- Add/expand operator contract tests
- Add operator E2E smoke to verify pipeline path
- Add operator telemetry dashboard and baseline SLO tracking

### Phase 3 (hardening)

- Enforce code owners for operator-critical paths
- Add performance budget checks to CI summaries
- Add staged rollout automation by environment

## Acceptance Criteria

1. A PR cannot merge without declaring operator-entry impact and validation evidence.
2. Operator-critical regressions are detected by automated tests before deployment.
3. Post-deploy smoke verifies at least one operator-critical API chain.
4. Operator latency/error trends are observable and alertable.
5. Shared-domain changes no longer silently alter operator behavior.

## References

- `designInput/GENERAL_DESIGN_INPUT.md`
- `designInput/SPEC_OPERATOR_WC_LAYOUT.md`
- `designInput/SPEC_DATA_REFERENCE.md`
- `designInput/SPEC_PERFORMANCE_SLO.md`
- `designInput/OPERATOR_PERFORMANCE_PLAYBOOK.md`
- `designInput/SPEC_LEAN_TEST_AND_ROLLOUT_STRATEGY.md`
- `designInput/SECURITY_ROLES.md`

