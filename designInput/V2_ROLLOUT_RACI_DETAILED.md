# MES v2 Rollout Detailed RACI (Named Roles)

## Role Directory

Use these role seats consistently across all sites:

- **Executive Sponsor (VP Ops)**
- **Rollout Program Manager (Rollout PM)**
- **MES Product Owner (Product Owner)**
- **Engineering Manager (Eng Manager)**
- **QA/Test Lead (QA Lead)**
- **Data Migration Lead (Data Lead)**
- **IT Infrastructure Lead (IT Lead)**
- **Site Plant Manager (Plant Manager)**
- **Site Operations Lead (Ops Lead)**
- **Site Training Lead (Training Lead)**
- **Support/Hypercare Lead (Support Lead)**
- **Compliance/Quality Lead (Quality Lead)**
- **Finance Reporting Lead (Finance Lead)**

## RACI Legend

- **R** = Responsible (does the work)
- **A** = Accountable (final decision owner)
- **C** = Consulted (two-way input)
- **I** = Informed (kept up to date)

## Cross-Site Governance Tasks

| Task | Exec Sponsor | Rollout PM | Product Owner | Eng Manager | QA Lead | Data Lead | IT Lead | Plant Manager | Ops Lead | Training Lead | Support Lead | Quality Lead | Finance Lead |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Approve branch decision (Standard vs Accelerated WJ) | A | R | C | C | C | C | C | C | C | I | I | C | C |
| Approve rollout calendar through July 2026 | A | R | C | C | I | I | C | C | C | C | I | I | I |
| Enforce release gate policy and exception handling | A | R | C | C | C | I | I | C | C | I | C | C | I |
| Weekly steering review and risk burndown | A | R | C | C | C | C | C | C | C | C | C | C | C |

## Fremont Validation Tasks (Gate for WJ Acceleration)

| Task | Exec Sponsor | Rollout PM | Product Owner | Eng Manager | QA Lead | Data Lead | IT Lead | Plant Manager | Ops Lead | Training Lead | Support Lead | Quality Lead | Finance Lead |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Finalize Fremont v2 UAT scope and scripts | I | C | A | C | R | I | I | C | C | C | I | C | I |
| Execute Fremont pilot and collect user signoff | I | R | C | C | C | I | C | A | R | R | C | C | I |
| Validate performance against `SPEC_PERFORMANCE_SLO.md` | I | C | C | C | A | I | C | I | C | I | R | C | I |
| Confirm no unresolved Sev1 in critical workflows | I | C | C | A | R | I | C | I | C | I | R | C | I |
| Fremont go/no-go decision for production success gate | A | R | C | C | C | C | C | C | C | I | C | C | I |

## West Jordan Accelerated Cutover Tasks

| Task | Exec Sponsor | Rollout PM | Product Owner | Eng Manager | QA Lead | Data Lead | IT Lead | Plant Manager | Ops Lead | Training Lead | Support Lead | Quality Lead | Finance Lead |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Confirm WJ acceleration eligibility from Fremont results | A | R | C | C | C | C | I | C | C | I | I | C | I |
| Define Day-1 migration scope (active data only) | I | C | C | C | C | A | C | C | R | I | I | C | C |
| Execute migration dry run #1 + reconciliation | I | C | I | C | C | A/R | C | I | C | I | I | C | C |
| Execute migration dry run #2 + reconciliation | I | C | I | C | C | A/R | C | I | C | I | I | C | C |
| Run WJ cutover rehearsal #1 | I | A | C | C | R | R | R | C | C | C | C | C | I |
| Run WJ cutover rehearsal #2 (timed) | I | A | C | C | R | R | R | C | C | C | C | C | I |
| Deliver WJ delta training (v1 -> v2) | I | C | C | I | I | I | I | C | C | A/R | I | C | I |
| Final WJ go/no-go decision | A | R | C | C | C | C | C | C | C | C | C | C | C |
| Execute WJ production cutover | I | A | C | R | R | R | R | C | C | C | R | C | I |
| Hypercare daily command center (first 14 days) | I | A | C | C | C | C | C | C | C | I | R | C | I |

## Deferred Historical Migration Tasks (WJ)

| Task | Exec Sponsor | Rollout PM | Product Owner | Eng Manager | QA Lead | Data Lead | IT Lead | Plant Manager | Ops Lead | Training Lead | Support Lead | Quality Lead | Finance Lead |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Define history waves (30-90 days, fiscal year, archive) | I | C | C | I | I | A/R | C | I | C | I | I | C | C |
| Publish v1 read-only access SOP and retention policy | I | A | C | I | I | C | R | C | C | C | C | C | C |
| Execute historical backfill wave 1 | I | C | I | C | C | A/R | C | I | I | I | I | C | C |
| Execute historical backfill wave 2 | I | C | I | C | C | A/R | C | I | I | I | I | C | C |
| Reconciliation signoff by function | I | C | I | I | C | R | I | I | C | I | I | A | A |
| Decommission v1 read-only access (after signoff) | A | R | C | C | I | C | R | C | C | I | C | C | C |

## Cleveland Launch Tasks

| Task | Exec Sponsor | Rollout PM | Product Owner | Eng Manager | QA Lead | Data Lead | IT Lead | Plant Manager | Ops Lead | Training Lead | Support Lead | Quality Lead | Finance Lead |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Confirm Cleveland starts on v2 (or fallback) | A | R | C | C | C | C | C | C | C | I | I | C | C |
| Cleveland v2 readiness and training execution | I | C | C | C | C | I | C | A | R | R | C | C | I |
| Cleveland cutover and hypercare | I | A | C | C | C | C | C | C | C | C | R | C | I |

## Go/No-Go Metrics Ownership

| Metric / Trigger | Primary Owner (A) | Operational Owner (R) |
|---|---|---|
| Operator-critical latency and error budget thresholds | QA Lead | Support Lead |
| Sev1 closure and defect readiness | Eng Manager | QA Lead |
| Data integrity and reconciliation pass/fail | Data Lead | Data Lead |
| Site user signoff completeness | Plant Manager | Ops Lead |
| Final release/cutover decision | Executive Sponsor | Rollout PM |

## Naming and Assignment Note

Replace each role seat with a person name before publishing externally (example: `Rollout PM -> First Last`), but keep the role title visible to prevent ambiguity when personnel change.
