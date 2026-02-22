# Annotation Maintenance

View and manage annotations across production records in a table layout. Annotations are quality holds, notes, or other markers attached to serial numbers or other records in the system, such as Downtime Events, Plants, Production Lines, or Work Centers. Create new annotations, edit existing ones, update their status, add resolution notes, and filter by type or status.

## How It Works

1. **Browse.** The table lists all annotations with key details in columns. Use the filters above the table to narrow results.
2. **Filter.** Apply filters for text search, annotation type, status (All, Open, Closed, Resolved, Unresolved), and site (Directors and above can filter across sites).
3. **Create an annotation.** Click **Create Annotation**. Select an annotation type, enter notes, and optionally link the annotation to a specific Plant, Production Line, or Work Center. Click **Save**.
4. **Edit an annotation.** Click a row to open the edit panel. You can change the status (Open / Closed), edit notes, add resolution notes, and mark the annotation as resolved.
5. **Resolve.** Enter resolution notes and mark the annotation resolved. The Resolved By field is automatically set to your name.

## Fields & Controls


| Element            | Description                                                                           |
| ------------------ | ------------------------------------------------------------------------------------- |
| **Text Search**    | Filters the table by serial number, notes, or other text fields.                      |
| **Type Filter**    | Dropdown to filter by annotation type.                                                |
| **Status Filter**  | Filter by status: All, Open, Closed, Resolved, or Unresolved.                         |
| **Site Filter**    | Filter by site. Available to Directors (Role 2.0) and above.                          |
| **Date**           | The date the annotation was created.                                                  |
| **Serial #**       | The serial number the annotation is attached to.                                      |
| **Type**           | The annotation type name.                                                             |
| **Status**         | Current status of the annotation: **Open** or **Closed**. Editable in the edit panel. |
| **Notes**          | Free-text notes entered when the annotation was created. Editable.                    |
| **Initiated By**   | The user who created the annotation. Auto-populated.                                  |
| **Resolved By**    | The user who resolved the annotation. Auto-populated when resolved.                   |
| **Resolved Notes** | Notes entered at resolution explaining the corrective action.                         |
| **Linked To**      | Optional link to a Plant, Production Line, or Work Center for context.                |


## Automatically Generated Annotations

Some annotations are created by the system without manual user action. These are always created with status **Open** and should be reviewed and resolved by a supervisor.


| Trigger                  | Annotation Type   | When It Fires                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                         | Notes Text                                                                                                                                                                                                                                            |
| ------------------------ | ----------------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| **Rolls catch-up**       | Correction Needed | A shell serial number is scanned at any non-Rolls work center but has never been scanned at Rolls. The system creates the serial on the fly and attempts to match it to the correct Rolls record by finding the nearest predecessor shell serial number on the same production line (shell serials are sequential, so the system picks the Rolls record with the highest serial that is still less than the catch-up serial). It then inherits the product (tank size) and copies the plate material lot traceability record from that matched Rolls shell. If no Rolls predecessor exists on the line, product and material lot are left unassigned. | "Rolls scan missed for shell {serial}. Product and material lot inherited from previous Rolls record — verify correct product and material lot." (or "No previous Rolls record found — product and material lot unknown." if no Rolls history exists) |
| **Auto-logout downtime** | Correction Needed | The system auto-generates a downtime event when an operator is logged out automatically (e.g., session timeout) and no downtime reason was selected. The event is created with an unknown reason and linked to the annotation.                                                                                                                                                                                                                                                                                                                                                                                                                        | "Auto-generated downtime event — reason unknown. Please review and assign the correct reason."                                                                                                                                                        |


Annotations created by the Rolls catch-up and auto-logout downtime triggers use the **Correction Needed** type, which requires resolution. They will remain **Open** until someone views the record and adds resolution notes.

## Tips

- Use the status filter to quickly find unresolved annotations that need attention.
- Look for "Correction Needed" annotations regularly — these are often auto-generated and indicate records that need supervisor review.
- Resolution notes are important for audit trails — always describe the corrective action taken.
- Annotations linked to a specific work center help supervisors identify recurring issues at a particular station.

## Changes from MES v1

- **Rolls catch-up** v1 only automatically added the record at Long Seam, v2 now will add the Shell from any work center.  The automatically added shell will inherits it's product and traceability from the Rolls scan that was just previous to the shell being automatically added.  The record that will be inherited from will be found based on the knowledge that the shell numbers are sequential.
- Downtime Events are new to v2 and annotations can now be attached to Downtine Events.

