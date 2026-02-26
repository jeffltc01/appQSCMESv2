# Lean Phase 4: Replenishment Kanban Policies

## Purpose

Evolve current queue/card behavior into closed-loop pull replenishment with explicit supermarket policies and exception handling.

## Current Baseline

- Queue and card mechanics exist in `backend/MESv2.Api/Services/WorkCenterService.cs`.
- Current behavior is local FIFO with bounded queue length and card uniqueness checks.

## Target Capabilities

1. Supermarket min/max policy per item family and work center.
2. Replenishment trigger generation when stock position reaches reorder point.
3. Closed-loop card circulation (issued, active, returned, unavailable).
4. Exception handling for missing card, stale queue item, and mismatch.

## New Domain Model

### `KanbanPolicy`

- `Id` (Guid)
- `PlantId` (Guid)
- `WorkCenterId` (Guid)
- `ProductId` (Guid)
- `TriggerType` (enum: `TwoBin`, `MinMax`, `ConsumptionRate`)
- `ReorderPointQty` (decimal)
- `TargetQty` (decimal)
- `ContainerQty` (decimal)
- `LeadTimeMinutes` (int)
- `SafetyFactorPercent` (decimal)
- `IsActive` (bool)

### `KanbanReplenishmentSignal`

- `Id` (Guid)
- `KanbanPolicyId` (Guid)
- `WorkCenterId` (Guid)
- `ProductId` (Guid)
- `SignalState` (enum: `Open`, `Acknowledged`, `InProgress`, `Fulfilled`, `Cancelled`)
- `RequestedQty` (decimal)
- `RaisedAtUtc` (DateTime)
- `AcknowledgedAtUtc` (DateTime?)
- `FulfilledAtUtc` (DateTime?)
- `RaisedByRule` (string)

### `KanbanCardLifecycle`

- `CardId` (string)
- `Status` (enum: `Available`, `AttachedToQueue`, `InTransit`, `Held`, `Retired`)
- `CurrentWorkCenterId` (Guid?)
- `LastMovementAtUtc` (DateTime)
- `LastMovementType` (string)

## Replenishment Algorithm (Default)

1. Compute stock position:
   - `StockPosition = OnHandQueueQty + InTransitQty - CommittedQty`.
2. If `StockPosition <= ReorderPointQty`, create/open replenishment signal.
3. Requested quantity:
   - `TargetQty - StockPosition`, rounded to nearest container multiple.
4. Auto-close signal only after receiving transaction confirms quantity and card reconciliation.

## API Contract

- `GET /api/kanban-policies?workCenterId=...`
- `POST /api/kanban-policies`
- `PUT /api/kanban-policies/{id}`
- `GET /api/kanban-signals/open?workCenterId=...`
- `POST /api/kanban-signals/{id}/acknowledge`
- `POST /api/kanban-signals/{id}/fulfill`
- `GET /api/kanban-cards/lifecycle?workCenterId=...`

## UI Requirements

- Material Handler queue screens show:
  - stock position,
  - reorder point,
  - open replenishment signals,
  - card status health.
- Supervisors get exception panel:
  - stale signals,
  - card not returned,
  - quantity mismatch,
  - repeated emergency replenishments.

## Controls and Guardrails

- Do not allow the same card in two active queue records.
- Flag queue items older than policy lead-time + tolerance.
- Require reason code for manual override when fulfilling outside policy quantity.

## Acceptance Criteria

- Replenishment is triggered by policy, not only manual queue entry.
- Card movement is auditable end-to-end.
- Exception events are visible and actionable before line starvation occurs.
