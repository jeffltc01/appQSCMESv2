const e=`# Downtime Log

Downtime Log tracks and maintains downtime events by work center, including manual entries and auto-generated events.

## How It Works

1. **Select a work center.** The event list loads only after a work center is selected.
2. **Set date range.** Use **From** and optional **To** filters to control the time window.
3. **Review events.** The table shows start/end, duration, production line, operator, reason category, reason, whether it counts toward OEE, and source.
4. **Log new downtime.** Click **Log Downtime** to add a manual event.
5. **Maintain records.** Use row actions to edit or delete events.

## Fields & Controls

| Element | Description |
|---|---|
| **Work Center** | Required filter for event retrieval. |
| **From / To** | Date filters for the event window (\`To\` is optional). |
| **Log Downtime** | Opens the downtime entry dialog for new records. |
| **Counts Toward OEE** | \`Yes\` when the selected reason is configured to count as downtime for OEE, \`No\` when excluded, \`—\` when no reason is assigned. |
| **Source badge** | Shows whether event was \`Auto\` or \`Manual\`. |
| **Edit / Delete actions** | Modify or remove an existing downtime event. |
| **Pagination** | Navigates large result sets (50 rows per page). |

## Tips

- Start with a narrow date range for faster troubleshooting.
- Use reason and category columns to spot recurring causes quickly.
- The screen auto-refreshes periodically, so active events can appear without a manual refresh.
`;export{e as default};
