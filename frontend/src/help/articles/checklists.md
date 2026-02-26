# Checklist Templates

Checklist Templates manages reusable safety and operations checklists. The flow now uses a dedicated editor route instead of editing in a modal.

## Screen Flow

1. Open `Menu -> Checklist Templates`.
2. Filter by Site and Type on the list page.
3. Click **Add Template** (goes to `/menu/checklists/new`) or use the edit icon (goes to `/menu/checklists/:id`).
4. Complete fields and question definitions, then save.

## Ownership Rules

- **Template owner is required** for every template.
- **Administrators and Directors** can assign any user as owner when creating or updating.
- **Only the owner** can edit an existing template after creation.

## Supported Question Response Types

Allowed response types are:

- `Checkbox`
- `Datetime`
- `Number`
- `Image`
- `Dimension`
- `Score`

Legacy response types like `PassFail` and `Select` are not allowed.

## Response-Type Specific Rules

- **Score**: a Score Type must be selected.
- **Dimension**:
  - target, upper limit, lower limit, and unit are required
  - unit must be `inches`
  - limits must satisfy `Lower <= Target <= Upper`

## Sections

- Each question can be unsectioned or assigned to a section.
- Section picker supports both:
  - selecting an existing section
  - creating a new section inline
- Section groups display alphabetically, with unsectioned items first.

## Rollout Note

- No legacy checklist data migration/reset is required in this environment because legacy data is already empty.
