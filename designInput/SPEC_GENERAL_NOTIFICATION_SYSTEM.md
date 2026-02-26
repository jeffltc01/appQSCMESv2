# General Notification System

## Purpose

Define a reusable notification platform that any MES feature can use to publish user-targeted notifications with consistent behavior across app inbox, email delivery, deep links, and clear/retention rules.

This specification is the canonical cross-feature contract for notification creation and consumption.

## Scope

### In Scope (Phase 1)

- App inbox notifications for targeted users (by `UserId`)
- Optional immediate email delivery based on user preference + notification priority
- Notification payload contract (subject, message, priority, recipients, deep link metadata)
- Clear policies:
  - Read-to-clear
  - Must-complete-entity (condition-key based)
- Recipient-only read/dismiss actions
- Delivery logging, retry policy, and auditability

### Out of Scope (Phase 1)

- SMS delivery
- Digest batching
- Cross-tenant federation
- User-defined ad hoc filtering expressions for completion rules

## Design Principles

1. Feature teams own notification intent (who, why, priority, completion behavior).
2. Notification platform owns delivery, persistence, state transitions, and policy enforcement.
3. Deep links are app-native route metadata, not hardcoded URL strings.
4. Must-complete behavior is deterministic and auditable.
5. Delivery channels are extensible (App + Email now, SMS later).

## Core Concepts

| Concept | Description |
|---|---|
| `Notification` | Shared event payload published by a feature |
| `NotificationRecipient` | Per-user state for a notification (read/clear/active) |
| `DeliveryChannel` | App, Email (SMS future) |
| `DeliveryPreference` | User setting for channel by priority |
| `Priority` | `Low`, `Normal`, `High`, `Critical` |
| `ClearPolicy` | `ReadToClear` or `MustCompleteEntity(conditionKey)` |
| `ConditionKey` | Named completion rule evaluated by feature-owned logic |
| `DeepLinkDescriptor` | Route key and route params used by frontend navigation |

## Ownership Model

### Feature Publisher Responsibilities

- Determine recipients (`UserId` list)
- Define business message (`subject`, `message`, `priority`)
- Choose `clearPolicy`
- Provide deep-link metadata and entity reference when relevant
- Register/support condition-key evaluation for must-complete notifications

### Notification Platform Responsibilities

- Validate payload shape and required fields
- Fan out recipients
- Persist inbox state and delivery state
- Dispatch channels based on user preferences
- Enforce recipient-only actions
- Evaluate clear policy transitions
- Record delivery and lifecycle audit events

## Data Contract

### Notification Publish Contract (Application Service Boundary)

```json
{
  "sourceFeature": "HoldTag",
  "sourceEventType": "HoldTagCreated",
  "subject": "Hold Tag Requires Disposition",
  "message": "Hold tag HT-2026-001 is awaiting quality disposition.",
  "priority": "High",
  "recipients": [
    { "userId": "9f6b2c3d-8db8-44c2-8fd2-7f7e949a9f11" },
    { "userId": "69f3f8d1-fd58-48c5-8a38-97595a6cf784" }
  ],
  "clearPolicy": {
    "type": "MustCompleteEntity",
    "conditionKey": "HOLDTAG_DISPOSITION_COMPLETE"
  },
  "entityRef": {
    "entityType": "HoldTag",
    "entityId": "0f4f7d3b-6fc6-48e8-a4cc-e822ce7fdb98"
  },
  "deepLink": {
    "routeKey": "holdTagDetail",
    "routeParams": {
      "holdTagId": "0f4f7d3b-6fc6-48e8-a4cc-e822ce7fdb98"
    }
  },
  "metadata": {
    "siteCode": "000",
    "productionLineId": "44c6f0d1-a9d9-4a89-aabf-618fd4f7ef6a"
  }
}
```

### Required Fields

- `sourceFeature`
- `sourceEventType`
- `subject`
- `message`
- `priority`
- `recipients[].userId`
- `clearPolicy.type`

### Optional Fields

- `clearPolicy.conditionKey` (required only when `MustCompleteEntity`)
- `entityRef`
- `deepLink`
- `metadata`

## Clear Policy Contract

### 1) `ReadToClear`

Use when notification should clear after recipient reads/acknowledges it.

Applies to:
- Unlinked FYI notifications
- Linked notify-only notifications

### 2) `MustCompleteEntity(conditionKey)`

Use when notification remains active until the linked business requirement is complete.

Rules:
- Read state may become `Read`, but notification remains active until condition passes.
- Clear transition occurs only when condition evaluator returns complete.
- Condition result is re-evaluated on relevant entity update events and/or background sweep.

## Condition-Key Evaluation Model

`conditionKey` is a named semantic rule, not a runtime query expression.

Examples:
- `HOLDTAG_DISPOSITION_COMPLETE`
- `ANDON_ACKNOWLEDGED`
- `RCA_EFFECTIVENESS_VERIFIED`

Each key maps to a backend evaluator implementation that returns:

```json
{
  "isComplete": true,
  "completedAtUtc": "2026-02-25T21:58:12Z",
  "completionEvidence": "HoldTag.Status=Resolved"
}
```

## Deep Link and Navigation Contract

Deep links are route metadata so frontend remains environment-safe (no hardcoded host/port in payload).

```json
{
  "routeKey": "holdTagDetail",
  "routeParams": {
    "holdTagId": "0f4f7d3b-6fc6-48e8-a4cc-e822ce7fdb98"
  },
  "entityType": "HoldTag",
  "entityId": "0f4f7d3b-6fc6-48e8-a4cc-e822ce7fdb98"
}
```

Frontend behavior:
- Inbox card click opens notification detail panel
- "Go to Item" action resolves route by `routeKey` + `routeParams`
- If route resolution fails, show fallback error and keep notification intact

## User Delivery Preferences

Preferences are stored by user and priority. App channel is always available; email is opt-in by priority.

| Priority | Channel Setting Options |
|---|---|
| Low | `AppOnly`, `EmailOnly`, `AppAndEmail` |
| Normal | `AppOnly`, `EmailOnly`, `AppAndEmail` |
| High | `AppOnly`, `EmailOnly`, `AppAndEmail` |
| Critical | `AppOnly`, `EmailOnly`, `AppAndEmail` |

Recommended default profile:
- Low: AppOnly
- Normal: AppOnly
- High: AppAndEmail
- Critical: AppAndEmail

### Delivery Decision

For each recipient:
1. Read recipient preference for incoming priority.
2. Queue app delivery (for inbox persistence) and email delivery when preference includes email.
3. Execute channel handlers independently; one failure must not drop the other.

## Lifecycle and State Model

### Recipient State Fields

- `IsRead`
- `ReadAtUtc`
- `IsActive`
- `ClearedAtUtc`
- `ClearReason` (`Read`, `EntityCompleted`, `SystemPolicy`, `AdminFuture`)

### Lifecycle Rules

1. Notification created -> recipient rows created as `IsRead=false`, `IsActive=true`
2. Recipient reads notification:
   - If `ReadToClear` -> set `IsRead=true`, `IsActive=false`, `ClearReason=Read`
   - If `MustCompleteEntity` -> set `IsRead=true`, keep `IsActive=true`
3. Entity completion confirmed for must-complete:
   - Set `IsActive=false`, `ClearReason=EntityCompleted`
4. Cleared notifications remain queryable for audit/history

## API Surface (Proposed)

### Publisher-Facing

- `POST /api/notifications`
  - Creates notification and recipient fan-out
  - Reserved for service-to-service and backend feature flows

### Recipient-Facing

- `GET /api/notifications/inbox?activeOnly=true&priority=High&unreadOnly=false`
  - Returns recipient-specific inbox page
- `POST /api/notifications/{recipientNotificationId}/read`
  - Marks as read (recipient only)
- `POST /api/notifications/{recipientNotificationId}/dismiss`
  - Allowed only for `ReadToClear` policies (recipient only)

### System/Internal

- `POST /api/notifications/conditions/recheck`
  - Triggers condition reevaluation for active must-complete recipients
  - Intended for background worker/integration events

## Authorization and Security

- Notification inbox APIs require authenticated user context.
- Recipient mutation endpoints (`read`, `dismiss`) verify the current user owns the recipient row.
- Producer endpoints are restricted to trusted app services/controllers.
- Authorized Inspector and read-only personas may read notifications only if feature-level authorization allows; they cannot mutate another user's notification state.
- All timestamps stored in UTC.

## Delivery Reliability and Retry

### App Channel

- Persist-first strategy: inbox record is committed before channel dispatch.
- App delivery considered successful once recipient inbox row is persisted.

### Email Channel

- Immediate send attempt on publish.
- Retry with exponential backoff (example: 1m, 5m, 15m, 60m).
- Mark failed after max attempts; preserve in delivery log for support review.
- Do not block app inbox delivery if email fails.

## Audit and Observability

Track at minimum:

- Publish request accepted/rejected
- Recipient fan-out results
- Channel dispatch attempts and outcomes
- Read/dismiss actions (user, timestamp)
- Condition checks and completion outcomes
- Active -> cleared transitions with clear reason

Recommended metrics:

- Notification publish count by feature
- Email success/failure rate
- Mean time to read by priority
- Mean time to clear for must-complete policies

## UI/UX Behavior

- Inbox supports filters: unread, active, priority, source feature
- Active must-complete notifications are visually distinct (badge: "Action Required")
- Notification detail shows:
  - Subject
  - Message
  - Priority
  - Created timestamp
  - Source feature
  - Linked entity action ("Go to Item") when deep-link data exists
- For must-complete notifications, show status hint:
  - "Read, waiting for completion"
  - "Completed and cleared"

## Example Scenarios

### 1) Unlinked FYI

- `clearPolicy = ReadToClear`
- No `entityRef`
- Clears immediately on read

### 2) Hold Tag Notify-Only

- `clearPolicy = ReadToClear`
- Has `entityRef` + deep link to hold-tag detail
- Clears on read, regardless of hold tag lifecycle

### 3) Hold Tag Must Complete

- `clearPolicy = MustCompleteEntity("HOLDTAG_DISPOSITION_COMPLETE")`
- Has `entityRef` + deep link to hold-tag detail
- Remains active after read until hold tag reaches resolved/required disposition complete state

## Implementation Notes

- Prefer backend application service for publish operations instead of direct controller logic.
- Keep route keys centrally registered so renamed routes can be remapped without data migration.
- Avoid storing absolute environment URLs in notification payload.
- Use feature-level constants/enums for `sourceFeature`, `priority`, and `conditionKey`.

## Testing Requirements

### Backend Unit Tests

- Validate payload contract rules
- Validate clear policy state transitions
- Validate condition-key evaluator dispatch and completion handling
- Validate preference-to-channel resolution by priority

### Backend Integration Tests

- Publish -> recipient fan-out -> inbox retrieval
- Read action behavior for both policy types
- Must-complete notification auto-clear on entity completion event
- Email failure does not block app inbox creation

### Frontend Tests

- Inbox rendering and filtering by active/unread/priority
- Deep-link navigation action from notification detail
- Must-complete UI state badge and post-completion clear behavior

## Rollout Plan

### Phase 1

- App inbox + immediate email
- Priority-based user delivery preferences
- Hold Tag as first must-complete integration

### Phase 2

- Optional digest mode
- Expanded feature integrations (Andon, RCA/CAPA, maintenance requests)

### Phase 3

- Add SMS channel using same channel abstraction and preference matrix

## Acceptance Criteria

1. Any backend feature can publish a notification using the canonical contract.
2. Recipients are resolved and persisted by `UserId`.
3. Inbox behavior matches clear policy rules for all three scenarios:
   - Unlinked FYI -> clears on read
   - Linked notify-only -> clears on read
   - Linked must-complete -> clears only when completion condition is satisfied
4. Deep-link metadata supports navigation to linked entities.
5. Delivery preference by priority controls app/email behavior per user.
6. Recipient-only mutation enforcement is active on read/dismiss APIs.
7. Delivery attempts and lifecycle transitions are auditable.
8. Spec remains channel-extensible for future SMS support.
