# MES v2 - Hold Tag System Specification

## Purpose

Define Hold Tag domain behavior for MES v2 using the shared workflow engine core.

This spec is domain-specific. Reusable workflow primitives (workflow states, approvals, work items/todos, notification framework, and event model) are defined in:

- `SPEC_WORKFLOW_ENGINE_CORE.md`

## Scope

### In Scope (Initial Release)

- Hold Tags can be created by any authenticated user.
- Hold Tag create entry on operator screens is controlled by configuration flags on Production Line and Work Center.
- Hold Tag number should be available quickly for writing on physical tags.
- Hold Tags are typically created with production context (site/line/work center/serial), but minimum required context is serial + site.
- Defect code can use the existing defect model with Hold Tag-specific filtering.
- Disposition options:
  - `ReleaseAsIs`
  - `Repair`
  - `Scrap`
- `Repair` and `Scrap` require NCR handoff before Hold Tag closure.
- Mobile-first quality disposition UX is required (quick note templates, low typing).

### Out of Scope (Initial Release)

- Red Tag process and UI.
- Reopen flow after final closure.
- Per-user notification preference management.

## Domain Roles and Responsibilities

- **Creator (Any authenticated user)**
  - Creates Hold Tag when issue is identified.
  - Applies physical Hold Tag operationally.
- **Quality**
  - Owns disposition decision and closure.
  - Can void tags created in error.
- **Team Lead**
  - Participates in review.
  - Does not finalize disposition unless user also has Quality authority.

## Rollout Control (UI Entry Visibility)

- Add `IsHoldTagEnabled` to Production Line and Work Center maintenance.
- These flags control only whether the **Create Hold Tag** button appears in the left-side button bar on operator screens.
- This is a UI entry visibility control for phased rollout; it does not change Hold Tag workflow semantics.
- Effective visibility rule:
  - if operator context includes both Production Line and Work Center, both must have `IsHoldTagEnabled = true`
  - if operator context has only Production Line, Production Line `IsHoldTagEnabled` controls visibility
- Existing Hold Tags remain accessible regardless of later flag changes.

## Domain Data Contract

### Hold Tag Header Fields

- `Id` (GUID, system generated)
- `HoldTagNumber` (int, system generated, incremental global sequence)
- `SiteCode` (required)
- `ProductionLineId` (optional in create, required before disposition complete)
- `WorkCenterId` (optional)
- `SerialNumberMasterId` (required)
- `ProblemDescription` (required)
- `DefectCodeId` (optional, recommended)
- `CreatedByUserId` (required)
- `CreatedAtUtc` (required)
- `LastModifiedByUserId` (required)
- `LastModifiedAtUtc` (required)

### Disposition Fields

- `Disposition` (`ReleaseAsIs`, `Repair`, `Scrap`)
- `DispositionSetByUserId` (Quality role required)
- `DispositionSetAtUtc`
- `DispositionNotes` (required for `ReleaseAsIs`, optional otherwise)

### Disposition-Specific Required Fields

- **ReleaseAsIs**
  - `ReleaseJustification` (required)
- **Repair**
  - `RepairInstructionTemplateId` (required)
  - `RepairInstructionNotes` (optional)
  - `LinkedNcrId` (required before closure)
- **Scrap**
  - `ScrapReasonCode` or `ScrapReasonText` (required)
  - `LinkedNcrId` (required before closure)

## Hold Tag Workflow Shape (Domain Mapping to Core)

Reference core states and transitions in `SPEC_WORKFLOW_ENGINE_CORE.md`.

Recommended Hold Tag workflow type:

- `WorkflowType = HoldTag`
- Steps:
  1. `TagCreated`
  2. `QualityDisposition`
  3. `NcrLinkRequired` (conditional for `Repair`/`Scrap`)
  4. `FinalizeDisposition`

### Step Rules

- `TagCreated` -> enter on create.
- `QualityDisposition` requires Quality-assigned work item completion.
- If disposition is `Repair` or `Scrap`, transition to `NcrLinkRequired`.
- `NcrLinkRequired` completes only when `LinkedNcrId` exists.
- `FinalizeDisposition` transitions the workflow to terminal status.
- `VoidHoldTag` is a workflow action (not a direct status mutation) allowed from any non-terminal step:
  - `TagCreated`
  - `QualityDisposition`
  - `NcrLinkRequired`
  - `FinalizeDisposition` (only if not already terminal)
- On `VoidHoldTag`, workflow transitions immediately to core terminal status `Voided`.
- `VoidHoldTag` requires a reason/comment and records actor + UTC timestamp in workflow event history.
- When voided, all open work items for the Hold Tag are cancelled according to core clear policy.
- No transitions are permitted from terminal states (`Completed`/`Voided`).

### Domain Status Mapping

- Workflow terminal status `Completed` maps to Hold Tag business status `Resolved`.
- Workflow terminal status `Voided` maps to Hold Tag business status `Voided`.
- UI and reporting should display business status values (`Resolved`/`Voided`), while workflow runtime uses core status values.

## Work Items (Todo Model Usage)

Use shared `WorkItem` model from core spec.

Hold Tag creates these work item patterns:

- `ReviewHoldTag` assigned to Quality role on create.
- `SupportReviewHoldTag` assigned to Team Lead role on create.
- `LinkNcr` assigned to Quality role (or an explicitly assigned Quality user) when disposition requires NCR.

Work items clear on configured step exit or entity completion per core clear policy.

## Notification Rules (Domain Mapping)

Use shared `NotificationRule` model from core spec.

Required notifications for initial release:

- On Hold Tag create:
  - notify Quality for same site context
  - notify Team Lead for same site/line context

Optional future notifications:

- disposition set
- NCR linked
- Hold Tag resolved

## NCR Integration Boundary

- Hold Tag owns rule that `Repair` and `Scrap` require NCR linkage.
- NCR system owns lifecycle after NCR record creation.
- Hold Tag stores `LinkedNcrId` for closure gating and traceability.

## Access Control

- Create Hold Tag: all authenticated users with floor access.
- Operator screen create entry visibility is additionally gated by `IsHoldTagEnabled` rollout flags.
- Edit pre-disposition fields: creator, Team Lead, Quality, Supervisor+.
- Link NCR to Hold Tag: Quality roles only.
- Set disposition: Quality roles only.
- Void Hold Tag: Quality roles only.
- Resolve Hold Tag: Quality roles only.

## Validation Rules

- Required create fields must be present.
- Disposition-specific required fields enforced before closure.
- `Repair` and `Scrap` cannot close without `LinkedNcrId`.
- Invalid transitions are blocked by workflow engine + domain validation.
- All date/time fields stored in UTC.

## API and Service Notes

- Keep controller endpoints thin; enforce domain logic in services.
- Use workflow core service for transitions/work item generation/notifications.
- `VoidHoldTag` must execute through workflow core transition orchestration (same path as other workflow actions), not by directly updating Hold Tag status fields.
- Domain endpoints/actions:
  - `CreateHoldTag`
  - `SetHoldTagDisposition`
  - `LinkHoldTagNcr`
  - `ResolveHoldTag`
  - `VoidHoldTag`

## Acceptance Criteria

1. Hold Tag uses the shared workflow engine core rather than bespoke workflow code.
2. Hold Tag creates role-based work items for review on create.
3. Required create and disposition data is enforced by domain rules.
4. `Repair` and `Scrap` require linked NCR before closure.
5. Notifications fire for create event to Quality and Team Lead.
6. Terminal records are immutable and auditable.

