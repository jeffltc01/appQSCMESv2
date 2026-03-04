const e=`# Production Line Work Centers

Configure production-line-specific overrides for each work center. Use this screen to manage what operators see and line-level behavior without changing base work center defaults.

## How It Works

1. **Choose a work center card.** Find the base work center you want to configure.
2. **Add or edit a line override.** Open the popup from the per-line section.
3. **Set line-specific values.** Update display name, welder requirement, checklist flags, and downtime settings.
4. **Save changes.** The override applies only to that work center + production line combination.

## Fields & Controls

| Element | Description |
|---|---|
| **Production Line** | Line the override applies to. Set on create; fixed on edit. |
| **Display Name** | Name shown to operators for this line-specific work center (for example, "Rolls Line 2"). |
| **Number of Welders** | Required welder count before data capture is allowed for this line at this work center. |
| **Enable WC Checklists** | Enables work-center checklist prompts for this line configuration. |
| **Enable Safety Checklists** | Enables safety checklist prompts for this line configuration. |
| **Enable Downtime Tracking** | Enables inactivity-based downtime tracking for this line configuration. |
| **Inactivity Threshold (minutes)** | Minutes of inactivity before downtime is triggered. |
| **Applicable Reason Codes** | Downtime reasons operators can select when returning from inactivity. |

## Tips

- This screen does not change base fields like work center name or data entry type.
- If no reason codes appear, create them first in **Downtime Reasons** for that plant.
- Line overrides are independent; changing one line does not change others.
`;export{e as default};
