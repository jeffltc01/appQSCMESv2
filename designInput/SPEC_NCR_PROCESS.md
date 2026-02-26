# Nonconformance Report (NCR) Process

## Purpose

Define the NCR workflow, data contract, approval behavior, and validation rules for quality-owned nonconformance handling in MES v2.

This spec is the canonical process reference for creating, completing, approving, rejecting, and resubmitting NCRs.

## Release Scope

### In Scope (First Release)

- NCR can be created directly by quality users.
- NCR can originate from a hold-tag escalation path.
- Type-driven checklist requirements.
- Type-driven approval assignment using one or more explicit users and/or security roles (excluding operators).
- Electronic approval signatures captured per assigned approver.
- Rejection loop back to owner with mandatory rejection comments and required resubmission.
- Vendor-specific data fields required when applicable by NCR Type.
- One or more images required on every NCR.
- NCR numbering using global yearly increment.

### Out of Scope (First Release)

- Reopen flow after final approval.
- Delegation/substitution of approvers during active approval cycle.
- Conditional checklist branching logic beyond required/not-required.
- CAPA/RCA workflows (handled by separate specs/processes).
- Hold-tag escalation automation details (defined in hold-tag process specification).

## Actors and Responsibilities

- **Quality Roles (Creator)**:
  - Can create new NCR records.
  - Supported roles for first release: Quality Tech, Quality Manager, Quality Director.
- **Owner (Quality role only)**:
  - Completes NCR report details.
  - Completes all required checklist items.
  - Submits NCR for approval.
  - Addresses rejection comments and resubmits.
- **Approver(s)**:
  - Determined by NCR Type configuration.
  - Can be assigned as explicit users and/or security roles.
  - Operators are excluded from approver assignment.
  - Approve or reject with electronic signature.
- **Quality Director**:
  - Maintains NCR Type definitions in Admin.
  - Configures checklist mapping and approval assignment rules for each type.

## NCR Data Requirements

### Required Core Fields

- NCR Number (system-generated)
- Plant
- Problem Description (multi-line text)
- Detected By (user)
- Owner (user; must be quality role)
- Date
- NCR Type
- One or more images
- Electronic signatures for required approvals (captured during approval stage)

### Vendor-Related Required Fields (Conditional)

When NCR Type is vendor-related, these fields are required:

- Vendor
- PO No.
- Quantity
- Heat No.
- Coil/Slab No.

### System/Traceability Fields (Required by System)

- Status
- CreatedByUserId
- CreatedAtUtc
- LastModifiedByUserId
- LastModifiedAtUtc
- SourceType (`DirectQuality` or `HoldTagEscalation`)
- SourceEntityId (when originated from hold-tag escalation)

## NCR Numbering Rule

- Format: `YYYY-####`
- Sequence scope: single global sequence per calendar year across all plants and NCR types.
- Example: `2026-0001`, `2026-0002`
- Sequence resets to `0001` at start of each new year.
- Number is assigned on create and never changes.

## NCR Type Configuration Rules

NCR Type configuration is managed in Admin by Quality Director and defines:

- Type name/code and active flag.
- Whether the type is vendor-related (enables vendor-required fields).
- One or more checklist templates mapped to the type.
- One or more approver assignments mapped to the type:
  - Explicit users, and/or
  - Security roles (excluding Operator role).

## Checklist Rules

- NCR Type determines one or more required checklists.
- All required checklist items across all mapped checklists must be completed before submit-for-approval.
- Checklist completion is validated at submit time.
- Incomplete checklist items block submission with actionable validation feedback.

## Lifecycle States

- `Draft`
  - Initial state on creation.
  - NCR is editable by authorized quality users.
- `PendingApproval`
  - Entered after owner submits completed NCR.
  - Content is read-only except approval actions.
- `Rejected`
  - Entered when any required approver rejects.
  - Owner revises NCR and resubmits.
- `Approved`
  - Terminal complete state when all required approvers have approved.
  - NCR becomes read-only/immutable.

## Workflow

1. Quality user creates NCR directly, or NCR is created from hold-tag escalation path.
2. System assigns NCR number and initializes status to `Draft`.
3. Owner completes required report fields and uploads one or more images.
4. System enforces vendor-related fields if NCR Type is vendor-related.
5. Owner completes all checklist items required by NCR Type.
6. Owner submits NCR for approval.
7. System resolves approval assignments from NCR Type and enters `PendingApproval`.
8. Approvers sign electronically in any order (parallel approval model).
9. If all required approvers approve, NCR transitions to `Approved` (complete).
10. If any required approver rejects:
    - NCR transitions to `Rejected`.
    - Rejection comments are required.
    - Owner updates NCR and resubmits.
    - All prior signatures are reset; full re-approval is required.

## Approval and Signature Rules

- Approval requirements are type-driven and mandatory.
- Approval order is not enforced; approvals can happen in parallel.
- All resolved approvers must approve for completion.
- Rejection by any required approver blocks completion and returns NCR to owner.
- Electronic signature capture includes:
  - Approver identity
  - Approval decision (Approve/Reject)
  - Timestamp (UTC)
  - Signature intent acknowledgement
  - Rejection comments (required for Reject)

## Access and Edit Rules

- Create NCR: quality roles only (Quality Tech, Quality Manager, Quality Director).
- Owner assignment: quality roles only.
- Edit in `Draft` or `Rejected`: owner and authorized quality leadership roles.
- Submit for approval: owner (or authorized quality leadership).
- Approve/reject in `PendingApproval`: only assigned approvers for that NCR.
- `Approved` NCRs are immutable/read-only.

## Hold-Tag Integration Boundary

- NCR may reference hold-tag origin via source linkage fields.
- Hold-tag escalation trigger logic remains defined in hold-tag spec.
- This NCR spec governs only NCR lifecycle after NCR record creation.
- For hold-tag-origin NCRs, prefill behavior may include shared context (plant, issue description, related images), but hold-tag disposition logic is out of scope here.

## Notification Rules

Use the general notification platform contract for NCR events.

- On submit (`Draft` -> `PendingApproval`):
  - Notify all required approvers.
  - Clear policy: `MustCompleteEntity` keyed to NCR approval completion.
- On reject:
  - Notify owner with rejection comments and deep link to NCR.
- On final approval:
  - Notify owner/creator that NCR is complete.

## Validation and Business Rules

- NCR Type is required before submission.
- Problem Description is required and supports multi-line text.
- At least one image is required before submission.
- If type is vendor-related, all vendor fields are required before submission.
- Owner must be a quality-role user.
- Submission is blocked until all required checklist items are complete.
- Submission is blocked until required core fields are valid.
- Rejection comments are mandatory for any reject action.
- On resubmission after rejection, prior signatures are cleared and approval cycle restarts from zero.
- Date/time values are stored in UTC and displayed in local plant context per global conventions.

## Audit and Traceability

System must retain auditable event history for:

- NCR creation (who/when/source)
- Field edits (who/when/what changed)
- Checklist completion updates
- Submit-for-approval action
- Approver decisions and electronic signatures
- Rejection comments
- Signature reset event on resubmission
- State transitions through final approval

## API/Domain Notes (Implementation Guidance)

- Keep controller endpoints thin; place approval/checklist/state logic in application services.
- Resolve role-based approvers at submit time and snapshot the approval roster for that submission cycle.
- Enforce recipient/approver-only mutation for approval actions.
- Use shared constants/enums for status values and source types.

## Testing Requirements

### Backend Unit Tests

- Required-field validation for core and vendor-conditional fields.
- Checklist gate enforcement before submission.
- Type-driven approver resolution (users + roles; operators excluded).
- Approval completion behavior requiring all approvals.
- Rejection flow requiring comments and resetting signatures on resubmit.
- Number generation format and yearly sequence behavior.

### Backend Integration Tests

- Direct quality-created NCR full lifecycle (`Draft` -> `PendingApproval` -> `Approved`).
- Rejection/resubmission lifecycle with signature reset.
- Hold-tag-origin NCR enters NCR lifecycle correctly post-creation.
- Notification events triggered for submit/reject/approve transitions.

### Frontend Tests

- Form validation behavior for required and vendor-conditional fields.
- Checklist completion gate UX before submit.
- Approval panel showing pending/approved/rejected approvers.
- Rejected NCR edit + resubmit behavior and signature reset visibility.

## Acceptance Criteria

1. Quality users can create NCRs and each NCR receives a global yearly number in `YYYY-####` format.
2. Owner must be a quality-role user.
3. NCR Type configuration from Admin controls required checklist(s) and approvers.
4. Operators cannot be configured as approvers for NCR Types.
5. Submit-for-approval is blocked until all required checklist items are complete.
6. Submit-for-approval is blocked if any required core field is missing or invalid.
7. Vendor-related NCR Types require Vendor, PO No., Quantity, Heat No., and Coil/Slab No. before submit.
8. All required approvers must approve (parallel order allowed) for NCR to reach `Approved`.
9. Any rejection returns NCR to owner with required rejection comments.
10. Resubmission after rejection resets all prior signatures and requires full re-approval.
11. Approved NCRs are read-only and considered complete.
12. NCR lifecycle events, signatures, and state transitions are auditable.

