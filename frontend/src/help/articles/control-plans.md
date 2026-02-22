# Control Plans

Configure which inspections for characteristics are enforced as control plan checks at each work center. Control plans appear as a card grid showing the characteristic name, assigned work center and production line, site, result type, and status badges for enabled and gate check.

## How It Works

1. **Browse.** Each card in the grid displays the characteristic name, work center, production line, site, result type, an enabled/disabled badge, and a gate check badge.
2. **Filter by site.** Directors and above see a site dropdown to narrow the list to a specific plant. Other users see only their assigned site.
3. **Add a control plan.** Click **Add** to open the create form. Select a characteristic and a work center / production line, then configure result type, enabled state, and gate check. When a site filter is active, the work center dropdown is scoped to that site.
4. **Edit a control plan.** Click a card to open the edit form. Toggle the enabled state, change the result type, or enable/disable the gate check and click **Save**.
5. **Enable or disable.** Use the **Enabled** checkbox to activate or deactivate the control plan. Disabled plans are skipped during inspection.  NOTE: Code Required control plans cannot be disabled.
6. **Gate check.** Enable the **Gate Check** checkbox to flag this control plan as a mandatory quality gate. Gate check results feed into the **Sellable Tank Status** screen and the **Digital Twin** visualization, making it easy to see which serials have passed or failed critical inspections.

## Fields & Controls


| Element                | Description                                                                                                                                                                                      |
| ---------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| **Site Filter**        | Dropdown to filter by site/plant. Available to Directors (Role 2.0) and above.                                                                                                                   |
| **Characteristic**     | The inspection characteristic this plan governs. Set when creating; read-only afterwards.                                                                                                        |
| **Work Center / Line** | The work center and production line where this control plan is enforced. When creating, the dropdown includes the plant name for disambiguation.                                                 |
| **Site**               | The plant this control plan belongs to (derived from the production line). Displayed on each card.                                                                                               |
| **Enabled**            | Checkbox. When checked, this plan is active and inspectors are prompted for the result.                                                                                                          |
| **Result Type**        | The type of result collected: PassFail, AcceptReject, GoNoGo, NumericInt, NumericDecimal, or Text.                                                                                               |
| **Gate Check**         | Checkbox. When checked, this plan is treated as a critical quality gate. Gate check results are shown on the Sellable Tank Status screen and the Digital Twin. Displayed as a badge on the card. |
| **Code Required**      | Checkbox. When checked, marks this as an ASME code-required inspection. Code Required plans must also be gate checks.                                                                            |


## Tips

- Gate checks are important — they determine whether a tank is considered sellable. A serial without passing gate check results will be flagged on the Sellable Tank Status screen.
- Disabling a control plan does not remove historical results. It only stops future inspections from prompting for that characteristic.
- Use the site filter to focus on control plans for a specific plant — especially useful when work center names repeat across sites.

