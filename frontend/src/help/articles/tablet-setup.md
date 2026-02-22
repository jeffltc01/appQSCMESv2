# Tablet Setup

One-time configuration screen that assigns a tablet to a specific work center, production line, and asset. Accessed by Team Leads (5.0) and above. Operators who log in to an unconfigured tablet are directed to contact a Team Lead.

## How It Works

1. **Open the screen.** Navigate to Tablet Setup from the Admin Menu or the left-panel gear icon on the operator layout.
2. **Select a Work Center.** Choose from the dropdown — only work centers at your plant are listed.
3. **Production Line auto-selects.** If your plant has only one production line, it fills in automatically. If there are multiple, pick one.
4. **Select an Asset (if shown).** The Asset dropdown only appears for **Long Seam**, **Round Seam**, **Round Seam Inspection**, and **Hydro**. For all other work centers it is hidden.
5. **Tap Save.** The configuration is cached to the tablet's browser storage. The next operator to log in will be routed straight to the assigned work center.

## Fields & Controls

| Element | Description |
|---|---|
| **Associated Work Center** *(required)* | Dropdown of work centers at this plant. Selecting a work center refreshes the Asset list and may auto-select the Production Line. |
| **Production Line** | Dropdown of production lines. Auto-selects when there is only one option. |
| **Associated Asset** *(required when visible)* | Dropdown of assets for the selected work center. Only shown for Long Seam, Round Seam, Round Seam Inspection, and Hydro. |
| **Save** | Saves the configuration to the server and caches it in localStorage. Disabled until all required fields are filled. |

## Tips

- Re-run Tablet Setup any time you need to reassign a tablet — it overwrites the previous configuration.
- The plant is inherited from your login session. There is no plant selector on this screen.
- If the previously configured work center is deleted, the next login will redirect to Tablet Setup with a message.

## Changes from MES v1

- **Styling** updated from PowerApp styling to the v2 design system (Fluent UI).
- **Data source** changed from direct Azure SQL queries to API calls through the ASP.NET Core backend.
- **Caching** uses browser `localStorage` instead of PowerApp local state.
- **Cascading dropdowns** — Asset choices are API-driven and filtered by the selected work center.
