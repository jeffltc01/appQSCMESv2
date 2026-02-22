# Annotation Maintenance

View and manage annotations across production records in a table layout. Annotations are quality flags, holds, or notes attached to serial numbers. Create new annotations, edit existing ones, toggle flags, add resolution notes, and filter by type or status.

## How It Works

1. **Browse.** The table lists all annotations with key details in columns. Use the filters above the table to narrow results.
2. **Filter.** Apply filters for text search, annotation type, status (All, Flagged, Resolved, Unresolved), and site (Directors and above can filter across sites).
3. **Create an annotation.** Click **Create Annotation**. Select an annotation type, enter notes, and optionally link the annotation to a specific Plant, Production Line, or Work Center. Click **Save**.
4. **Edit an annotation.** Click a row to open the edit panel. You can toggle the flag, edit notes, add resolution notes, and mark the annotation as resolved.
5. **Resolve.** Enter resolution notes and mark the annotation resolved. The Resolved By field is automatically set to your name.

## Fields & Controls

| Element | Description |
|---|---|
| **Text Search** | Filters the table by serial number, notes, or other text fields. |
| **Type Filter** | Dropdown to filter by annotation type. |
| **Status Filter** | Filter by status: All, Flagged, Resolved, or Unresolved. |
| **Site Filter** | Filter by site. Available to Directors (Role 2.0) and above. |
| **Date** | The date the annotation was created. |
| **Serial #** | The serial number the annotation is attached to. |
| **Type** | The annotation type, displayed as a colored flag matching the type's display color. |
| **Flag** | Whether the annotation is currently flagged. Can be toggled in the edit panel. |
| **Notes** | Free-text notes entered when the annotation was created. Editable. |
| **Initiated By** | The user who created the annotation. Auto-populated. |
| **Resolved By** | The user who resolved the annotation. Auto-populated when resolved. |
| **Resolved Notes** | Notes entered at resolution explaining the corrective action. |
| **Linked To** | Optional link to a Plant, Production Line, or Work Center for context. |

## Tips

- Use the status filter to quickly find unresolved annotations that need attention.
- Resolution notes are important for audit trails — always describe the corrective action taken.
- Annotations linked to a specific work center help supervisors identify recurring issues at a particular station.
