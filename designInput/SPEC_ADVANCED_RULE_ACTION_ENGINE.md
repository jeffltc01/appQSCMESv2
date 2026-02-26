# Advanced Rule Action Engine

## Purpose

Define a configurable, auditable rule system that allows authorized users to create rules against MES data and automatically execute operational actions when rule conditions are satisfied.

This specification is the canonical contract for rule definition, evaluation, and action execution across MES domains.

## Scope

### In Scope (Phase 1)

- User-defined rule definitions with enable/disable controls
- Rule conditions based on approved source domains and filter criteria
- Time-window and sequence-based evaluation primitives
- Event-driven and scheduled reevaluation
- Action execution for:
  - Notification creation
  - Annotation creation
  - Downtime entry creation
  - Hold tag creation (if hold-tag domain is available at runtime)
- Rule execution audit history and explainability
- Role-based access for authoring, approving, and viewing rule outcomes

### Out of Scope (Phase 1)

- Free-form SQL or arbitrary script execution in rules
- Cross-tenant rule sharing/federation
- Full no-code expression language with nested boolean groups beyond approved operators
- AI-authored rules without human approval
- Multi-step workflow orchestration with compensating transactions

## Design Principles

1. Rule authoring must be safe by default (bounded inputs, allowed fields, rate limits).
2. Rule evaluation must be deterministic and auditable.
3. Action execution must be idempotent and resilient to retries.
4. Domain ownership remains with source services; the rule engine orchestrates only.
5. Rules must be explainable to non-developers ("why this fired").

## Core Concepts

| Concept | Description |
|---|---|
| `RuleDefinition` | Top-level user-authored rule metadata and lifecycle state |
| `RuleCondition` | Domain-specific condition config evaluated by a provider |
| `RuleAction` | Action to run when condition is met |
| `EvaluationMode` | `EventDriven`, `Scheduled`, or `EventAndScheduled` |
| `CooldownPolicy` | Prevents repeated firing in a short interval |
| `RuleExecution` | Single evaluation run with outcome and evidence |
| `RuleActionExecution` | Per-action execution result for a rule firing |
| `ConditionProvider` | Pluggable evaluator for a source domain |

## Rule Definition Model

### Required Fields

- `Name`
- `Description`
- `SourceDomain`
- `ConditionType`
- `ConditionConfig`
- `Actions`
- `EvaluationMode`
- `IsEnabled`

### Optional Fields

- `Priority` (`Low`, `Normal`, `High`, `Critical`)
- `Scope` (plant, production line, work center)
- `CooldownMinutes`
- `MaxFiringsPerHour`
- `OwnerUserId`
- `Tags`

### Example Rule Contract

```json
{
  "name": "Repeat failure overlay by same user",
  "description": "Detect repeated frontend failures to force operational visibility.",
  "sourceDomain": "FrontendTelemetryEvent",
  "conditionType": "CountInWindow",
  "conditionConfig": {
    "filters": [
      { "field": "isReactRuntimeOverlayCandidate", "operator": "eq", "value": true }
    ],
    "groupBy": ["userId"],
    "window": { "unit": "minutes", "value": 15 },
    "threshold": { "operator": "gte", "value": 5 }
  },
  "evaluationMode": "EventAndScheduled",
  "priority": "High",
  "cooldownMinutes": 30,
  "maxFiringsPerHour": 2,
  "actions": [
    {
      "type": "CreateNotification",
      "config": {
        "subjectTemplate": "User {userId} hit runtime failure threshold",
        "messageTemplate": "Runtime overlay candidate seen {count} times in 15 minutes."
      }
    },
    {
      "type": "CreateAnnotation",
      "config": {
        "annotationTypeId": "runtime-risk",
        "notesTemplate": "Auto-created by rule engine due to repeated runtime failures."
      }
    }
  ],
  "isEnabled": true
}
```

## Condition Model

`conditionType` is constrained to approved, provider-supported primitives.

### Phase 1 Condition Types

1. `CountInWindow`
   - Count records matching filters in a lookback window.
   - Supports optional `groupBy`.

2. `TrendDownConsecutivePeriods`
   - Detect metric decline over N consecutive periods.
   - Example: FPY down 3 days in a row.

3. `SequentialMatch`
   - Detect same attribute value in last N ordered entities.
   - Example: same defect on last 6 sequential shells.

4. `ExistsInRecentSequence`
   - Detect recurring pattern in the most recent K entities.
   - Example: 3 hold tags created on the last 3 shells.

### Allowed Operators (Phase 1)

- `eq`, `neq`, `gt`, `gte`, `lt`, `lte`, `in`, `notIn`

### Validation Constraints

- Maximum lookback window: 30 days
- Maximum sequence depth: 100
- Maximum filters per rule: 10
- Only approved fields may be referenced per source domain
- Null handling must be explicit per provider

## Source Domain Providers

Each source domain maps to a backend provider that owns query translation and evaluation.

### Phase 1 Providers

- `FrontendTelemetryEventProvider`
  - Supports repeated failure-overlay thresholds by user/session/route.
- `FpyTrendProvider`
  - Supports consecutive decline detection at plant/line/work-center scope.
- `DefectSequenceProvider`
  - Supports repeated defect detection across sequential shells.
- `HoldTagProvider`
  - Supports hold-tag count/sequence detection when hold-tag model is present.

Provider contract:

- Input: normalized `ConditionConfig`, evaluation trigger context, current UTC timestamp
- Output:
  - `isSatisfied`
  - `evidence` (key values, matched rows count, time range)
  - `aggregationContext` (for action templates)

## Action Model

Action types are first-class and independently retriable.

### Supported Action Types (Phase 1)

1. `CreateNotification`
2. `CreateAnnotation`
3. `CreateDowntimeEntry`
4. `CreateHoldTag` (feature-gated)

### Execution Rules

- Actions execute only when rule state transitions to `Satisfied` and cooldown allows firing.
- Each action receives:
  - Rule metadata
  - Evaluation evidence
  - Tokenized template context
- Action execution is idempotent via deterministic key:
  - `ruleId + conditionFingerprint + firingBucketUtc`
- Failures are captured per action with retry policy.

## Lifecycle and State Model

### Rule Lifecycle

- `Draft`
- `Active`
- `Disabled`
- `Archived`

### Evaluation Outcome

- `NotSatisfied`
- `SatisfiedNoFire` (cooldown/rate-limit blocked)
- `SatisfiedFired`
- `Error`

### Transition Notes

- Enabling a rule does not automatically backfill historical firings unless `BackfillOnEnable` is explicitly enabled.
- Disabling a rule immediately halts firing but preserves historical visibility.

## API Contract (Backend)

### Rule Management

- `POST /api/rules`
  - Create draft/active rule
- `PUT /api/rules/{id}`
  - Update mutable fields and action configs
- `POST /api/rules/{id}/enable`
- `POST /api/rules/{id}/disable`
- `GET /api/rules`
  - List with filters (`sourceDomain`, `status`, `owner`, `enabled`)
- `GET /api/rules/{id}`
- `POST /api/rules/{id}/dry-run`
  - Evaluate once, return evidence, no actions fired

### Execution and Audit

- `GET /api/rules/{id}/executions`
- `GET /api/rules/executions`
  - Cross-rule activity feed
- `GET /api/rules/executions/{executionId}`
  - Includes per-action results and evidence payload

## Evaluation Runtime

### Event-Driven Triggers

- On telemetry ingest events
- On defect log creation
- On production record changes relevant to FPY
- On hold-tag create/update events (if available)

### Scheduled Sweep

- Default interval: every 5 minutes
- Re-evaluates active rules for missed events and time-window transitions
- Uses bounded batching and cancellation tokens

### Performance and Safety

- Max evaluation time per rule: 5 seconds (configurable)
- Global concurrency cap for evaluation workers
- Query projections must be narrow and indexed
- Rules that exceed error threshold auto-disable and alert admins

## Security and Authorization

### Role Permissions

- `Admin`: full create/update/enable/disable/archive
- `QualityDirector` (or equivalent): approve and enable high-impact rules
- `Supervisor`: create/edit draft rules in assigned scope
- `Viewer`: read-only access to rule definitions and execution history

### Guardrails

- Scope enforcement at API and query layer
- Field-level allowlist per source domain
- Action-specific permission checks (example: downtime creation requires authorized role)

## Audit and Traceability

System must retain auditable history for:

- Rule create/update/enable/disable actions (who/when/what changed)
- Evaluation inputs (sanitized), outputs, and evidence snapshots
- Action attempts, outcomes, retries, and final status
- Idempotency key used for each fired action
- Auto-disable events and policy reasons

## Observability

### Metrics

- Evaluations per minute
- Satisfied rate by rule/domain
- Action success/failure counts by type
- Average evaluation latency
- Retry count and dead-letter count

### Logging

- Correlated logs by `RuleExecutionId`
- Structured fields for rule ID, domain, condition type, outcome, action type

## UX Expectations (Admin Rule Builder)

- Rule list with status, last fired, and error indicators
- Guided builder:
  - Source domain
  - Filters
  - Window/sequence config
  - Threshold
  - Action templates
- Dry-run preview with evidence
- Human-readable explanation text:
  - "Fired because defect X appeared on 6 sequential shells."

## Rollout Strategy

### Phase 1A

- Rule definition CRUD
- Dry-run API
- Notification action only

### Phase 1B

- Enable annotation and downtime actions
- Add event-driven triggers for telemetry and defects

### Phase 1C

- Enable hold-tag action once hold-tag domain is implemented
- Add advanced trend providers and stricter approval workflow

## Example Rules (Canonical)

1. Hold tags on last 3 shells
   - Condition: `ExistsInRecentSequence`
   - Source: hold-tag/shell stream
   - Action: create notification for Quality + Team Lead

2. Same user failure overlay 5 times in 15 minutes
   - Condition: `CountInWindow` grouped by `userId`
   - Source: frontend telemetry
   - Action: create notification + annotation

3. First Pass Yield down 3 consecutive days
   - Condition: `TrendDownConsecutivePeriods`
   - Source: FPY trend provider
   - Action: create downtime entry and notification

4. Same defect in last 6 sequential shells
   - Condition: `SequentialMatch`
   - Source: defect sequence provider
   - Action: create hold tag candidate + notification

## Acceptance Criteria

1. Authorized user can create, validate, enable, disable, and view rules.
2. Engine evaluates supported condition types deterministically with explainable evidence.
3. When condition is satisfied, configured actions run once per idempotency window.
4. Cooldown and max-firing policies prevent action storms.
5. Failures are visible per execution with retry/audit trace.
6. Dry-run returns evidence without side effects.
7. Scheduled sweep recovers from missed event triggers.
8. Rule and execution audit history is queryable via API.

## Open Implementation Dependencies

- Hold-tag backend model/service availability for `CreateHoldTag` action.
- Notification backend endpoints and persistence model finalization.
- Final role names/claims mapping from `SECURITY_ROLES` implementation.

