# MES v2 - Hold Tag System Specification

## Purpose

Define the Hold Tag workflow, data contract, state model, access rules, and NCR handoff behavior for MES v2.

This specification is focused only on Hold Tags and their integration boundary with NCRs.

## Scope

### In Scope (Initial Release)

- Hold Tags can be created by any authenticated user.
- Hold tag number needs to be available quickly, possibly even before saving if possible, because they have to write the number on the shell or product.
- Hold Tags are attached to production records in manufacturing context (serial/work center/line context required), but they could also be created without that much context as well, for the most part they should have at least a serial number.
- Hold Tag creation captures minimal required data to keep floor workflow fast.
- Quality and Team Lead are notified when a Hold Tag is created.
- Disposition is selected by Quality.
- Disposition options:
  - `ReleaseAsIs`
  - `Repair`
  - `Scrap`
- `Repair` and `Scrap` require NCR handoff before Hold Tag closure.
- Hold Tag can be resolved only after all disposition-specific requirements are complete.

### Out of Scope (Initial Release)

- Red Tag process and UI.
- Post-close reopen flow.
- Per-user notification preference management.

## Roles and Responsibilities

- **Creator (Any authenticated user)**
  - Creates Hold Tag when an issue is identified.
  - Applies physical Hold Tag at the station (operational step).
- **Quality**
  - Owns disposition decision.
  - Completes disposition-specific required fields.
  - Confirms Hold Tag closure.
  - Can void Hold Tags created in error.
- **Team Lead**
  - Receives creation notification.
  - Participates in review.
  - Does not finalize disposition unless granted Quality authority by role.

## Data Contract

### Hold Tag Header Fields

- `Id` (GUID, system generated)
- `HoldTagNumber` (int, system generated, incremental global sequence)
- `Status` (`AwaitingDisposition`, `InDisposition`, `NcrRequired`, `Resolved`, `Voided`)
- `SiteCode` (required)
- `ProductionLineId` (required)
- `WorkCenterId` (required)
- `SerialNumberMasterId` (required)
- `ProblemDescription` (required)
- `DefectCodeId` (optional, but recommended)
- `CreatedByUserId` (required, system)
- `CreatedAtUtc` (required, system UTC)
- `LastModifiedByUserId` (required, system)
- `LastModifiedAtUtc` (required, system UTC)

### Disposition Fields

- `Disposition` (`ReleaseAsIs`, `Repair`, `Scrap`)
- `DispositionSetByUserId` (Quality role required)
- `DispositionSetAtUtc` (UTC)
- `DispositionNotes` (optional general notes)

### Disposition-Specific Required Fields

- **ReleaseAsIs**
  - `ReleaseJustification` (required)
- **Repair**
  - `RepairInstructionTemplateId` (required)
  - `RepairInstructionNotes` (optional)
  - `LinkedNcrId` (required before resolve)
- **Scrap**
  - `ScrapReasonCode` or `ScrapReasonText` (required)
  - `LinkedNcrId` (required before resolve)

### Audit Trail (Required)

- State transitions with timestamp and user.
- Notification dispatch events.
- Disposition selections and edits.
- NCR handoff initiation and completion.
- Void action reason and actor.

## Lifecycle States

- `AwaitingDisposition`
  - Initial state after create.
- `InDisposition`
  - Quality has started review/disposition work.
- `NcrRequired`
  - Entered when disposition is `Repair` or `Scrap` and NCR is not yet linked/completed to required point.
- `Resolved`
  - Terminal state after all required fields and handoff rules are complete.
- `Voided`
  - Terminal state for records created in error.

## Workflow

1. User creates Hold Tag with minimal required data.
2. System sets status to `AwaitingDisposition`.
3. System sends notification to Quality and Team Lead for the same site/line context.
4. Quality reviews and sets disposition.
5. System enforces required fields by disposition:
  - `ReleaseAsIs` -> justification required.
  - `Repair` -> repair instruction + NCR link required.
  - `Scrap` -> scrap reason + NCR link required.
6. For `Repair` and `Scrap`, system triggers NCR handoff flow:
  - User can create NCR from Hold Tag.
  - NCR is prefilled using Hold Tag context.
  - Hold Tag remains `NcrRequired` until `LinkedNcrId` exists.
7. Quality resolves Hold Tag when all requirements are satisfied.

## NCR Integration Boundary

- Hold Tag owns decision to require NCR (`Repair` and `Scrap`).
- NCR system owns NCR lifecycle after NCR record creation.
- Hold Tag stores `LinkedNcrId` and optional summary of NCR state for display.
- Hold Tag closure gate for initial release:
  - `ReleaseAsIs`: no NCR required.
  - `Repair` or `Scrap`: linked NCR required before Hold Tag can resolve.

## Notifications

- Trigger on Hold Tag creation.
- Recipients:
  - Quality users in same site.
  - Team Leads in same site and production context.
- Delivery mechanism uses general notification system.
- Optional future triggers (not required now):
  - disposition set
  - NCR linked
  - Hold Tag resolved

## Access Control

- Create Hold Tag: all authenticated users with floor access.
- Edit Hold Tag (pre-disposition fields): creator, Team Lead, Quality, Supervisor+.
- Set disposition: Quality roles only.
- Void Hold Tag: Quality roles only.
- Resolve Hold Tag: Quality roles only.
- Terminal states (`Resolved`, `Voided`) are read-only.

## Validation Rules

- Required create fields must be present.
- Disposition cannot be finalized without disposition-specific required fields.
- `Repair` and `Scrap` cannot resolve without `LinkedNcrId`.
- Invalid state transitions are blocked by service layer.
- All date/time fields are stored in UTC.

## API and Service Design Notes

- Keep controller actions thin; enforce workflow logic in application services.
- Provide explicit endpoints/actions:
  - `CreateHoldTag`
  - `SetDisposition`
  - `LinkNcr`
  - `ResolveHoldTag`
  - `VoidHoldTag`
- Emit domain events for notification and audit logging.

## Initial Acceptance Criteria

1. Any authenticated floor user can create a Hold Tag with minimal required data.
2. New Hold Tags start in `AwaitingDisposition`.
3. Quality and Team Lead notifications are sent on creation.
4. Only Quality can set disposition, resolve, or void.
5. `ReleaseAsIs` requires justification before resolve.
6. `Repair` requires repair instruction and linked NCR before resolve.
7. `Scrap` requires scrap reason and linked NCR before resolve.
8. Terminal states are immutable.
9. All significant actions are auditable.

