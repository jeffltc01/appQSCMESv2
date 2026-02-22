# Work Center Configuration

Configure how each work center group behaves and how individual production lines override group defaults. Work centers appear as a card grid organized by group. Admins (Role 1.0) can edit group-level settings. Quality Managers (Role 3.0) and above can edit per-production-line overrides.

## How It Works

1. **Select a work center group.** Click a card to expand the group configuration panel.
2. **Edit group settings.** Update the Base Name, Data Entry Type, or Material Queue assignment and click **Save**. These settings apply as defaults to every production line in the group.
3. **Edit per-line overrides.** Expand a production line row within the group. Override the Display Name, Number of Welders, or Downtime Tracking settings for that specific line.
4. **Enable Downtime Tracking.** Toggle Downtime Tracking on for a production line to reveal the Inactivity Threshold and Reason Codes fields.

## Fields & Controls

### Group Settings

| Element | Description |
|---|---|
| **Base Name** | The canonical name for the work center group (e.g., "Rolls," "Fitup"). |
| **Data Entry Type** | Dropdown selecting one of 14 data-entry screen types that determines how operators interact with this work center. |
| **Material Queue For WC** | Conditional field. When the data entry type supports a material queue, select which work center feeds material into this one. Hidden for types that do not use a queue. |

### Per-Production-Line Overrides

| Element | Description |
|---|---|
| **Display Name** | Override the name shown to operators for this line (e.g., "Rolls – Line 2"). |
| **Number of Welders** | How many welder slots are available at this work center on this line. |
| **Downtime Tracking** | Toggle. When enabled, the system monitors operator inactivity. |
| **Inactivity Threshold** | Minutes of inactivity before a downtime event is triggered. Only visible when Downtime Tracking is on. |
| **Reason Codes** | Selectable list of downtime reason codes operators can choose when prompted. Only visible when Downtime Tracking is on. |

## Tips

- Changing the Data Entry Type on a group affects every line in that group. Coordinate with floor supervisors before making this change.
- Per-line overrides do not affect the group defaults — other lines keep the group settings unless they have their own overrides.
- If a work center needs a material queue but the option is grayed out, check that the Data Entry Type supports queue input.
