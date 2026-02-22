# Admin Menu

Tile-based navigation grid for supervisory and administrative functions. Visible to Team Leads (5.0) and above — each tile is gated by a minimum role tier so users only see what they have access to.

## How It Works

1. **Log in** with a role of Team Lead (5.0) or higher.
2. The menu displays automatically. Tiles are organized into color-coded groups.
3. **Tap a tile** to navigate to that screen. Tiles marked "Coming Soon" are disabled.
4. **Logout** is in the top-right corner of the header.

## Groups & Tiles

### Master Data (navy — `#2b3b84`)

| Tile | Min Role | Description |
|---|---|---|
| Product Maintenance | 3.0 | Manage plate and head products |
| Vendor Maintenance | 3.0 | Manage mills, processors, and head vendors |
| Asset Management | 3.0 | Manage work center assets and lanes |
| Plant Gear | 3.0 | Configure plant production gear levels |
| Plant Printers | 3.0 | Manage NiceLabel Cloud printer mappings |
| Production Lines | 3.0 | Configure production lines per site |

### Quality & Inspection (red — `#e41e2f`)

| Tile | Min Role | Description |
|---|---|---|
| Defect Codes | 3.0 | Manage defect code master data |
| Defect Locations | 3.0 | Manage defect location master data |
| Characteristics | 3.0 | Manage inspection characteristics |
| Control Plans | 2.0 | View and manage quality control plans |
| Annotation Types | 3.0 | Configure annotation type categories |
| Annotations | 3.0 | Browse and manage annotations |

### Production & Operations (purple — `#606ca3`)

| Tile | Min Role | Description |
|---|---|---|
| Work Center Config | 2.0 | Configure work centers, welder requirements, data entry types |
| Kanban Card Mgmt | 5.0 | Add/remove kanban cards for the Fitup Queue |
| Sellable Tank Status | 4.0 | Track finished tank disposition |
| Serial Number Lookup | 5.0 | Search for a serial number and view its full traceability chain |
| Downtime Reasons | 3.0 | Manage downtime reason codes |
| Shift Schedule | 3.0 | Define shift times and patterns |
| Capacity Targets | 3.0 | Set daily production capacity targets |

### Dashboards & Insights (gray — `#343a40`)

| Tile | Min Role | Description |
|---|---|---|
| Supervisor / Team Lead Dashboard | 5.0 | Real-time production counts and metrics |
| Digital Twin | 4.0 | Visual production line status |
| AI Review | 5.5 | AI-assisted quality review of inspection data |
| Who's On the Floor | 5.0 | See which operators and welders are currently signed in |
| Log Viewer | All roles | Browse production and inspection log records |
| Change Logs | 3.0 | *Coming Soon* — Audit trail of data changes |

### People & Administration (dark red — `#aa121f`)

| Tile | Min Role | Description |
|---|---|---|
| User Maintenance | 3.0 | Create, edit, and deactivate user accounts |
| Report Issue | 5.0 | Submit a bug report or feature request |
| Issue Approvals | 3.0 | Review and approve submitted issues |
| Operator View | 5.0 | Navigate to Tablet Setup and enter the operator work center layout |

## Tips

- If a tile you expect to see is missing, your role tier may be too low. Contact an Administrator.
- The "Operator View" tile takes you to Tablet Setup — from there you can enter any work center as if you were the operator.
- Your plant code and display name are shown in the header bar for quick reference.
