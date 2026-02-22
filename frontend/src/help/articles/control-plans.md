# Control Plans

Configure which inspection characteristics are enforced as control plan checks at each work center. Control plans appear as a card grid showing the characteristic name, assigned work center, result type, and status badges for enabled and gate check. You can edit existing control plans but cannot add or delete them.

## How It Works

1. **Browse.** Each card in the grid displays the characteristic name, work center, result type, an enabled/disabled badge, and a gate check badge.
2. **Edit a control plan.** Click a card to open the edit form. Toggle the enabled state, change the result type, or enable/disable the gate check and click **Save**.
3. **Enable or disable.** Use the **Enabled** checkbox to activate or deactivate the control plan. Disabled plans are skipped during inspection.
4. **Gate check.** Enable the **Gate Check** checkbox to make this control plan a mandatory pass/fail gate. When enabled, a failing result blocks the serial number from advancing past this work center.

## Fields & Controls

| Element | Description |
|---|---|
| **Characteristic** | The inspection characteristic this plan governs. Read-only (set at the system level). |
| **Work Center** | The work center where this control plan is enforced. Read-only. |
| **Enabled** | Checkbox. When checked, this plan is active and inspectors are prompted for the result. |
| **Result Type** | The type of result collected. Currently supports PassFail. |
| **Gate Check** | Checkbox. When checked, a failing result prevents the serial from progressing. Displayed as a badge on the card. |

## Tips

- Gate checks are powerful — a failing gate check stops production flow for that serial number. Only enable gate check for truly critical characteristics.
- Disabling a control plan does not remove historical results. It only stops future inspections from prompting for that characteristic.
- Control plans cannot be added or deleted from this screen. They are generated from the intersection of characteristics and work center assignments.
