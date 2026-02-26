# Hold Tag Process

## Purpose

Hold tags let plant employees flag critical quality issues discovered on a shell and move the issue through a controlled resolution process owned by Quality.

## Release Scope

### In Scope (First Release)

- Hold tags are attached to a **shell only** and identified by **shell number**.
- Multiple active hold tags are allowed for the same shell.
- Workflow includes inspection/measurement wizard support in first release.
- Final disposition is required for completion.
- NCR handoff is included for Scrap and Repair dispositions.

### Out of Scope (First Release)

- Hold tags on assemblies or tanks.
- Notification preferences per user.
- Post-close editing or reopen flow.

## Actors and Responsibilities

- **Employee (Any user)**:
  - Creates hold tag.
  - Physically applies yellow "Hold" tag to product.
- **Quality**:
  - Owns disposition decision and completion.
  - Can void tags created in error.
  - Can override wizard recommendation with documented reason.
- **Team Lead**:
  - Receives notification and participates in review.
  - Does not complete final disposition without Quality involvement.

## Hold Tag Data Requirements

### Required at Creation

- Shell number
- Defect code
- Created date/time (system timestamp)
- Initiated by user
- Plant (assumed from current user)
- Date/time (current date/time)
- Production line/work center context (for notification routing)

### Required at Disposition

- Disposition value: `Scrap`, `Repair`, or `Release As Is`
- Quality approver identity and approval timestamp

### Disposition-Specific Required Fields

- **Scrap**
  - Scrap reason (required)
  - Launch prefilled NCR form (required before final completion)
- **Repair**
  - Standard repair instruction selection (required)
  - Launch prefilled NCR form (required before final completion)
- **Release As Is**
  - Narrative justification (required)

## Lifecycle States

- `Awaiting Disposition`
  - Initial state for every newly created hold tag.
- `Resolved`
  - Terminal state once Quality sets final disposition and all required fields are complete.
- `Voided`
  - Terminal state for tags created in error.
  - Only Quality can void.

## Workflow

1. Employee identifies issue on a shell and creates hold tag record.
2. Employee physically applies yellow Hold tag.
3. System sends notification to Quality and Team Lead (line/work-center based routing).
4. Quality and Team Lead review the issue.
5. User runs inspection/measurement wizard by selecting a manual wizard template.
6. Wizard returns a recommended disposition.
7. Quality selects final disposition:
  - May follow recommendation, or
  - May override recommendation with required override reason.
8. System enforces disposition-specific required fields:
  - Scrap -> reason + NCR launch
  - Repair -> standard repair instruction + NCR launch
  - Release As Is -> narrative
9. After requirements are met and Quality approves, hold tag moves to `Resolved/Closed`.

## Inspection/Measurement Wizard Rules

- Wizard template is selected manually by user.
- Wizard output is a recommendation, not an automatic final decision.
- Quality can override recommendation only with a captured reason.
- Wizard completion is part of the disposition workflow for first release.

## NCR Integration Rules

- For `Scrap` and `Repair`, user is sent to an NCR creation form prefilled from the hold tag.
- Prefilled values should include shell number, defect code, disposition context, and related notes entered in hold tag flow.
- Hold tag cannot be finalized to `Resolved/Closed` until required NCR handoff step is completed.
- `Release As Is` does not create an NCR.

## Notification Rules

- Trigger: hold tag creation only.
- Recipients: Quality and Team Lead.
- Recipient routing: based on the line/work center context tied to the shell.
- No additional notifications are required in first release for disposition, NCR completion, or closure.

## Access and Edit Rules

- Create hold tag: standard floor users in workflow context.
- Void hold tag: Quality only.
- Final disposition: Quality-required path.
- Closed tags are read-only and immutable in first release.

## Validation and Business Rules

- Shell number is required for every hold tag.
- Disposition cannot be saved as final without all required fields for that disposition.
- If wizard recommendation is overridden, override reason is required.
- Multiple active hold tags for one shell are allowed.
- `Voided` and `Resolved/Closed` are terminal states.

## Audit and Traceability

System should retain auditable event history for:

- Hold tag creation (who/when)
- Notification dispatch (who targeted/when)
- Wizard template used and recommendation
- Recommendation override reason (if applicable)
- Disposition decision and Quality approver
- NCR handoff initiation/completion for Scrap/Repair
- Final state transition (`Resolved/Closed` or `Voided`)

## Acceptance Criteria

1. User can create a hold tag for a shell with required creation fields.
2. New hold tags always start in `Awaiting Disposition`.
3. System notifies Quality and Team Lead on creation using line/work-center routing.
4. Wizard template can be selected manually and returns a recommendation.
5. Quality can override recommendation only with a documented reason.
6. Scrap requires scrap reason and NCR launch before closure.
7. Repair requires standard repair instruction and NCR launch before closure.
8. Release As Is requires narrative before closure.
9. Only Quality can void a hold tag.
10. Closed and voided hold tags are read-only.
11. Multiple active hold tags per shell are supported.

