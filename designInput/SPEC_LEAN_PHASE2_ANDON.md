# Lean Phase 2: Andon Event Lifecycle and Escalation

## Purpose

Define a first-class andon workflow that converts downtime/quality abnormalities into owned, timed, and verifiable response actions.

## Scope

- Integrate with existing downtime and annotation systems.
- Support manual and auto-raised andon events.
- Enforce SLA-based escalation by severity and station type.

## New Domain Model

### `AndonEvent`

- `Id` (Guid)
- `PlantId` (Guid)
- `ProductionLineId` (Guid)
- `WorkCenterId` (Guid)
- `AssetId` (Guid?)
- `SourceType` (enum: `Downtime`, `Defect`, `QualityGate`, `Manual`)
- `SourceRecordId` (Guid?)
- `Severity` (enum: `Info`, `Warning`, `Critical`)
- `State` (enum: `Raised`, `Acknowledged`, `InProgress`, `Resolved`, `Verified`, `Cancelled`)
- `OwnerUserId` (Guid?)
- `RaisedAtUtc` (DateTime)
- `AcknowledgedAtUtc` (DateTime?)
- `ResolvedAtUtc` (DateTime?)
- `VerifiedAtUtc` (DateTime?)
- `ResolutionCode` (string?)
- `RootCauseCategory` (string?)
- `Summary` (string)
- `Details` (string?)

### `AndonEscalationLog`

- `Id` (Guid)
- `AndonEventId` (Guid)
- `EscalationLevel` (int)
- `EscalatedToRole` (string)
- `EscalatedToUserId` (Guid?)
- `TriggeredAtUtc` (DateTime)
- `AcknowledgedAtUtc` (DateTime?)

## Trigger Rules

1. **Downtime trigger**
   - Auto-raise when downtime duration exceeds configured threshold and no owner assigned.
2. **Annotation trigger**
   - Auto-raise when an annotation type marked `RequiresResolution` remains open beyond SLA.
3. **Manual trigger**
   - Operator/Team Lead can raise andon with bounded reason codes.

## Escalation Rules

- SLA clock starts at `RaisedAtUtc`.
- Escalation levels:
  - L1: Team Lead (after `X` minutes unacknowledged).
  - L2: Supervisor (after `Y` minutes unresolved).
  - L3: Plant Manager / Quality Manager for critical events.
- SLA values are configurable per plant + work center type.

## API Contract

- `POST /api/andon-events` (manual raise)
- `GET /api/andon-events/open` (line/work-center filtered)
- `POST /api/andon-events/{id}/acknowledge`
- `POST /api/andon-events/{id}/start`
- `POST /api/andon-events/{id}/resolve`
- `POST /api/andon-events/{id}/verify`
- `GET /api/andon-events/{id}/timeline`

## Integration With Existing Services

- `DowntimeService` creates/updates andon when events exceed thresholds.
- Annotation workflows can link open annotations to an andon event ID.
- Supervisor dashboard includes open andon count, mean acknowledge time, and SLA breach count.

## UI Requirements

- Work-center operator shell: compact andon indicator (state + aging timer).
- Supervisor dashboard: active andon board grouped by severity and time-to-breach.
- Resolution flow requires owner assignment, action note, and resolution code.

## Governance

- Role permissions:
  - Raise: Operator+
  - Acknowledge/Start/Resolve: Team Lead+
  - Verify/Close: Supervisor+ (or Quality Manager for quality-source events)

## Acceptance Criteria

- Every raised andon has owner + timeline.
- SLA escalation occurs automatically and is auditable.
- Downtime/annotation-origin events can be traced to andon resolution outcome.
