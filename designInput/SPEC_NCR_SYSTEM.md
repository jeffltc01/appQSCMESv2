# MES v2 - NCR System Specification

## Purpose

Define the NCR workflow engine, data contract, NCR Type configuration model, approval behavior, and lifecycle rules for MES v2.

This system supports NCRs created directly by Quality and NCRs created from Hold Tag escalation.

## Scope

### In Scope (Initial Release)

- NCR creation by Quality roles.
- NCR creation from Hold Tag context with prefilled fields.
- Integer incremental NCR numbering.
- Type-driven process flow (stages, required data, checklists, approval timing).
- Type-driven approvals (users and/or roles).
- Electronic approval/rejection with audit data.
- Rejection and resubmission loops when configured by NCR Type.
- Required image attachments.

### Out of Scope (Initial Release)

- Red Tag integration.
- Post-final-close reopen flow.
- Delegation/substitution logic during active approval (unless explicitly modeled later).

## Key Design Decision

NCR workflow is controlled by **NCR Type**.  
Each NCR Type defines its process steps, required approvals, and approval timing.

This allows business users to change process behavior by type without code changes.

## Roles and Responsibilities

- **Submitter**
  - User who creates the NCR record.
  - Can be direct Quality creator or system-mediated create from Hold Tag flow.
- **Coordinator**
  - Primary responsible user for completing NCR content and progressing workflow.
  - Must be a quality role in initial release.
- **Approver**
  - User or role-based resolved user required at one or more defined steps.
  - Performs approve/reject actions with signature intent capture.
- **Quality Director / Admin Maintainer**
  - Maintains NCR Types and their process definitions.

## Numbering

- Field: `NcrNumber` (int, system generated).
- Sequence behavior:
  - Global incremental integer sequence.
  - Monotonic increase across all plants/types.
  - No yearly reset in initial release.
- Number is assigned on create and does not change.

## Data Contract

### NCR Header

- `Id` (GUID, system generated)
- `NcrNumber` (int, system generated)
- `Status` (see lifecycle)
- `SourceType` (`DirectQuality`, `HoldTagEscalation`)
- `SourceEntityId` (nullable GUID; required for Hold Tag source)
- `SiteCode` (required)
- `DetectedByUserId` (required)
- `SubmitterUserId` (required)
- `CoordinatorUserId` (required, quality role)
- `NcrTypeId` (required)
- `DateUtc` (required, UTC)
- `ProblemDescription` (required)
- `CurrentStepCode` (required, from type-driven process)
- `CreatedByUserId` / `CreatedAtUtc` / `LastModifiedByUserId` / `LastModifiedAtUtc` (required audit fields)

### Required Attachments

- One or more image attachments required before first process step that requires submit/approval.
- Attachments are retained for audit traceability.

### Conditional Vendor Fields

When NCR Type is vendor-related, required fields include:

- `VendorId`
- `PoNumber`
- `Quantity`
- `HeatNumber`
- `CoilOrSlabNumber`

### Type-Driven Additional Fields

NCR Type may define additional required fields at specific steps.
Examples:

- containment action summary
- disposition code
- corrective action owner
- due dates

## NCR Type Configuration Model

### NCR Type Entity

- `Id`
- `Code`
- `Name`
- `IsActive`
- `IsVendorRelated`
- `Description`

### NCR Type Process Definition

Each NCR Type contains ordered process steps.  
Each step defines:

- `StepCode` (unique within type)
- `StepName`
- `Sequence`
- `RequiredFields` (field keys)
- `RequiredChecklistTemplateIds` (0..n)
- `ApprovalMode` (`None`, `AnyOne`, `All`)
- `ApprovalAssignments` (users and/or roles)
- `AllowReject` (bool)
- `OnRejectTargetStepCode` (nullable)
- `OnApproveNextStepCode` (nullable; null means complete)

### Approval Timing by Type

Approvals can occur at any step defined by NCR Type, including pre-work approvals before corrective action starts.

Examples:

- Type A: approve before containment work.
- Type B: approve after corrective action proposal.
- Type C: two-stage approvals (Supervisor, then AI).

## Lifecycle

Base statuses:

- `Draft`
- `InProgress`
- `PendingApproval`
- `Rejected`
- `Approved`
- `Completed`
- `Voided`

Notes:

- Exact transitions are constrained by the active step in NCR Type process definition.
- `Completed` is terminal for initial release.
- `Voided` is terminal for records canceled by authorized quality roles.

## Workflow

1. Submitter creates NCR (directly or from Hold Tag).
2. System assigns `NcrNumber` and initializes process at first step for selected type.
3. Coordinator completes required fields/checklists for current step.
4. If current step requires approval:
   - status becomes `PendingApproval`.
   - assigned approvers are resolved from users/roles at that step.
5. Approvers approve or reject:
   - approve path advances to configured next step.
   - reject path follows `OnRejectTargetStepCode` and captures required comments.
6. Process repeats until terminal completion step.
7. System sets status to `Completed` when final step requirements are satisfied.

## Approval and Signature Rules

- Approval requirements are step-specific and NCR Type driven.
- Approver assignment supports:
  - explicit users
  - role-based dynamic resolution
- Approval decision audit payload:
  - approver user
  - decision (`Approve` or `Reject`)
  - timestamp UTC
  - comments (required for reject; optional for approve)
  - signature intent flag
- If step requires `All`, all resolved approvers must approve.
- If step requires `AnyOne`, first approval can complete the step.

## Hold Tag Integration

- For Hold Tag-origin NCR:
  - `SourceType = HoldTagEscalation`
  - `SourceEntityId = HoldTag.Id`
  - prefill site/serial/problem context from Hold Tag.
- NCR lifecycle remains independent once created.
- Hold Tag system uses `NcrId` linkage for closure gate logic.

## Notifications

Use the general notification platform for these events:

- NCR created -> notify coordinator.
- Step submitted for approval -> notify current step approvers.
- Rejected -> notify coordinator with rejection comments.
- Completed -> notify submitter and coordinator.

## Access Rules

- Create NCR: Quality Tech, Quality Manager, Quality Director, Administrator.
- Set/Change coordinator: quality leadership roles per security policy.
- Edit NCR content: coordinator and authorized quality leadership while not terminal.
- Approve/Reject: only users resolved as approvers for current step.
- Terminal records (`Completed`, `Voided`) are read-only.

## Validation Rules

- `NcrTypeId` is required.
- `CoordinatorUserId` must be quality role.
- Required step fields/checklists must be complete before step submission.
- Vendor-required fields enforced when `IsVendorRelated = true`.
- At least one image attachment required before first approval step.
- Reject actions require comments.
- Invalid step/state transitions are blocked.
- All server timestamps are UTC.

## Implementation Notes

- Keep controllers thin; workflow execution in service layer/state engine.
- Snapshot resolved approvers at step submission time for deterministic audit.
- Store process definition version reference on each NCR to preserve historical behavior if type config later changes.
- Emit domain events for notifications and change logging.

## Initial Acceptance Criteria

1. NCR receives an integer incremental `NcrNumber` on create.
2. Submitter and Coordinator are separate concepts in API and UI.
3. NCR Type defines process steps, approval points, and transitions.
4. Step-level approvals support explicit users and role-based assignments.
5. Pre-work approvals are supported when configured by NCR Type.
6. Reject path is enforced when configured and requires comments.
7. Vendor-related NCR Types enforce vendor-required fields.
8. NCR created from Hold Tag stores source linkage and prefilled context.
9. Terminal NCRs are immutable.
10. Lifecycle, approvals, and transitions are fully auditable.

