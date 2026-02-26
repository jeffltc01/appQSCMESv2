# Lean Phase 3: RCA/CAPA Workflow

## Purpose

Introduce a structured problem-solving loop so repeated defects and downtime events are prevented, not just logged.

## Workflow Summary

1. Trigger from defect recurrence, downtime recurrence, or andon severity threshold.
2. Create RCA case with assigned owner and due date.
3. Complete root-cause analysis (5-Why + category coding).
4. Define corrective and preventive actions.
5. Execute actions and collect evidence.
6. Perform effectiveness check and formally close.

## New Domain Model

### `RcaCase`

- `Id` (Guid)
- `PlantId` (Guid)
- `WorkCenterId` (Guid?)
- `SourceType` (enum: `Defect`, `Downtime`, `Andon`, `Manual`)
- `SourceId` (Guid?)
- `Title` (string)
- `ProblemStatement` (string)
- `ContainmentAction` (string?)
- `OwnerUserId` (Guid)
- `FacilitatorUserId` (Guid?)
- `Priority` (enum: `Low`, `Medium`, `High`, `Critical`)
- `DueDateUtc` (DateTime)
- `State` (enum: `Open`, `AnalysisComplete`, `ActionInProgress`, `Verification`, `Closed`, `Cancelled`)
- `CreatedAtUtc` (DateTime)
- `ClosedAtUtc` (DateTime?)

### `RcaWhy`

- `Id` (Guid)
- `RcaCaseId` (Guid)
- `Sequence` (int, 1..5)
- `WhyQuestion` (string)
- `WhyAnswer` (string)

### `CapaAction`

- `Id` (Guid)
- `RcaCaseId` (Guid)
- `ActionType` (enum: `Corrective`, `Preventive`)
- `Description` (string)
- `OwnerUserId` (Guid)
- `DueDateUtc` (DateTime)
- `State` (enum: `Planned`, `InProgress`, `Done`, `Cancelled`)
- `EvidenceNote` (string?)
- `EvidenceAttachmentPath` (string?)
- `CompletedAtUtc` (DateTime?)

### `RcaEffectivenessCheck`

- `Id` (Guid)
- `RcaCaseId` (Guid)
- `CheckDateUtc` (DateTime)
- `WindowStartUtc` (DateTime)
- `WindowEndUtc` (DateTime)
- `BaselineDefectRate` (decimal?)
- `PostActionDefectRate` (decimal?)
- `BaselineDowntimeMinutes` (decimal?)
- `PostActionDowntimeMinutes` (decimal?)
- `Result` (enum: `Effective`, `PartiallyEffective`, `NotEffective`)
- `VerifiedByUserId` (Guid)
- `Notes` (string?)

## Linkage Rules

- `DefectLog` may reference `RcaCaseId` when grouped into a recurring issue.
- `DowntimeEvent` may reference `RcaCaseId`.
- `AndonEvent` may reference `RcaCaseId`.
- A case can aggregate multiple source records from the same issue family.

## API Contract

- `POST /api/rca-cases`
- `GET /api/rca-cases/open`
- `GET /api/rca-cases/{id}`
- `POST /api/rca-cases/{id}/why`
- `POST /api/rca-cases/{id}/actions`
- `POST /api/rca-cases/{id}/actions/{actionId}/complete`
- `POST /api/rca-cases/{id}/effectiveness-check`
- `POST /api/rca-cases/{id}/close`

## UI Requirements

- RCA board for Quality/Supervisor roles: by state, owner, and due-date risk.
- Guided RCA form: problem statement, containment, 5-Why, action planning.
- Closure guardrail: cannot close until all required CAPA actions are completed and effectiveness check is recorded.

## Governance

- Create/edit case: Quality Tech+, Supervisor+.
- Close case: Quality Manager+.
- Read-only visibility: Team Lead+.

## Acceptance Criteria

- Recurring defects/downtime can be linked to a single RCA case.
- CAPA actions are auditable with owner, due date, and evidence.
- Closure requires effectiveness validation, not only task completion.
