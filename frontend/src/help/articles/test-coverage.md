# Test Coverage

Test Coverage provides a combined backend/frontend coverage overview and embedded detailed HTML reports.

## How It Works

1. **Open the screen.** Coverage summary cards load for backend and frontend.
2. **Review metrics.** Each card shows line and branch coverage percentages plus covered/valid counts.
3. **Switch report tabs.** Use **Backend Report** or **Frontend Report** to load the detailed coverage report in the embedded viewer.
4. **Check freshness.** Use the **Last updated** timestamp to confirm report recency.

## Fields & Controls

| Element | Description |
|---|---|
| **Backend (.NET) summary** | High-level backend line and branch coverage metrics. |
| **Frontend (React) summary** | High-level frontend line and branch coverage metrics. |
| **Report tabs** | Switches between backend and frontend detailed reports. |
| **Embedded report frame** | Displays HTML coverage details without leaving the app. |
| **Error states** | Shows layer-level or page-level load failures when data is unavailable. |

## Tips

- Use the summary cards for quick health checks before release.
- Use the embedded report to find low-coverage files worth adding tests for.
- Compare backend and frontend trends together when validating cross-layer features.
