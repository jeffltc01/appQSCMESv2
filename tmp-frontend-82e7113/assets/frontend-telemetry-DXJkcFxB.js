const e=`# Frontend Telemetry

Frontend Telemetry provides searchable client-side diagnostics so admins can investigate runtime and API-related issues reported by users.

## How It Works

1. **Load telemetry.** The screen opens with current filters and page 1 results.
2. **Apply filters.** Filter by category, source, severity, user ID, work center ID, date range, and React overlay-only mode.
3. **Search.** Click **Search** to run with your current filter set.
4. **Review records.** Inspect message text, HTTP context, and whether an event is a React runtime overlay candidate.
5. **Archive when needed.** If row count exceeds warning threshold, use **Archive Oldest** to trim older data.

## Fields & Controls

| Element | Description |
|---|---|
| **Category / Source / Severity** | Core telemetry classification filters. |
| **User ID / Work Center ID** | Targeted troubleshooting filters for specific contexts. |
| **From / To** | Date-range filters applied to event occurrence time. |
| **React red overlay only** | Limits results to React runtime overlay candidate events. |
| **Archive Oldest** | Keeps recent telemetry and archives older rows when threshold is reached. |
| **Pagination** | Moves through filtered result pages. |

## Tips

- Start with Severity + date range to reduce noise quickly.
- Use User ID or Work Center ID to correlate a field complaint with telemetry.
- Archive only after reviewing active incidents to avoid removing useful short-term context.
`;export{e as default};
