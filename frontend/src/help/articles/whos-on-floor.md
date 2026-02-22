# Who's On the Floor

A read-only dashboard showing all active operator sessions across production lines at your site. The display auto-refreshes every 30 seconds. No editing is available — this screen is purely informational.

## How It Works

1. **View active sessions.** Operator sessions are grouped by production line. Each entry shows the work center, operator name, employee number, and login time.
2. **Spot stale sessions.** Sessions that have not sent a heartbeat recently are dimmed and show the last heartbeat timestamp. These may indicate an operator who walked away without logging out.
3. **Auto-refresh.** The dashboard refreshes automatically every 30 seconds. No manual action is required.

### Site Scoping

The dashboard is scoped to the logged-in user's site. Directors and above can see all sites.

## Fields & Controls

| Element | Description |
|---|---|
| **Production Line Group** | Sessions are visually grouped under their production line header. |
| **Work Center** | The work center where the operator is currently logged in. |
| **Operator Name** | The operator's display name. |
| **Employee Number** | The operator's employee number. |
| **Login Time** | When the operator started their current session. |
| **Last Heartbeat** | Shown on stale sessions. Indicates the last time the operator's client sent a heartbeat signal. |
| **Stale Indicator** | Dimmed styling applied to sessions whose heartbeat is overdue, suggesting the operator may no longer be active. |

## Tips

- Use this screen during shift changes to verify all operators are logged in at the correct stations.
- Stale sessions do not automatically log out the operator — a supervisor may need to follow up on the floor.
- If an operator appears at the wrong work center, they should log out and re-scan at the correct station.
