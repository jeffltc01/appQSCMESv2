const e=`# Work Centers

Configure base work center settings. This screen is for group-level defaults only: base name, data entry type, and queue mapping when applicable.

## How It Works

1. **Browse work centers.** Review work centers in the card list.
2. **Edit base settings.** Open the popup and update Base Name, Data Entry Type, or Material Queue assignment.
3. **Save changes.** Updates apply to the base work center definition used across production lines.

## Fields & Controls

### Group Settings


| Element                   | Description                                                                                                                                                             |
| ------------------------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| **Base Name**             | The canonical name for the work center group (e.g., "Rolls," "Fitup").                                                                                                  |
| **Data Entry Type**       | Dropdown selecting one of 14 data-entry screen types that determines how operators interact with this work center.                                                      |
| **Material Queue For WC** | Conditional field. When the data entry type supports a material queue, select which work center feeds material into this one. Hidden for types that do not use a queue. |


## Tips

- Per-line overrides are managed on the separate **Production Line Work Centers** screen.
- Changing the Data Entry Type on a base work center affects routing behavior for all lines using that work center.
- If a work center needs a material queue but the option is grayed out, check that the Data Entry Type supports queue input.

`;export{e as default};
