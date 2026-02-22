# Production Line Maintenance

Manage the production lines defined in the system. Production lines appear as a searchable card grid. Add, edit, or delete lines as needed. Deleting a production line is a hard delete. Site-scoped users are locked to their assigned site.

## How It Works

1. **Browse or search.** Use the search box to filter production lines by name.
2. **Add a production line.** Click **Add Production Line**. Enter a name and select a site, then click **Save**.
3. **Edit a production line.** Click a card to open the edit form. Update the name or site and click **Save**.
4. **Delete a production line.** Click a card and choose **Delete**. This is a permanent hard delete — the line and its associations are removed from the system.

### Site-Scoped Behavior

Site-scoped users see only the production lines at their site. The Site field is locked to their assigned site when adding a new line.

## Fields & Controls

| Element | Description |
|---|---|
| **Search** | Filters the card grid by production line name. |
| **Name** | The display name of the production line (e.g., "Line 1", "Line 2"). Required. |
| **Site** | The site this production line belongs to. Required. Site-scoped users are locked to their own site. |

## Tips

- Deleting a production line is permanent and may break references in work center configurations, assets, and session history. Only delete lines that have never been used in production.
- If you need to retire a line without deleting it, consider renaming it with a "RETIRED" prefix so it remains for historical reference.
- Site-scoped users cannot reassign a production line to a different site.
