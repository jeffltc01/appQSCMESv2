# Capacity Targets

Capacity Targets defines the expected units-per-hour throughput for each work center at each gear level. The screen presents an editable grid where rows are work centers (sorted by production sequence) and columns are the plant's gear levels. These targets are used by the Supervisor Dashboard to calculate OEE Performance and to populate the "Planned" column in performance tables.

**Access:** Quality Manager (3.0) and above. Directors (2.0) and above can switch between plants.

## How It Works

1. **Select a plant and production line.** Use the Plant and Production Line selectors at the top of the screen. The grid loads the work centers for that production line.
2. **View the grid.** Each row is a work center, sorted by its position in the production sequence. Each column is a gear level. Cells display the current target value (units per hour).
3. **Edit a cell.** Click any cell to enter or change the target value. Type the new units-per-hour number.
4. **Toggle per-tank-size breakdown.** Any cell can be toggled between a single default value (applies to all tank sizes) and a per-tank-size breakdown (separate targets for each tank size). Use the toggle on the cell to switch modes.
5. **Track unsaved changes.** The screen tracks dirty state. An unsaved changes indicator appears when you have modified any cells. Modified cells are visually highlighted.
6. **Save all changes.** Click **Save** to submit all changes as a bulk upsert. All modified cells are saved in a single operation.
7. **Reset.** Click **Reset** to discard all unsaved changes and revert to the last saved state.

## Fields & Controls


| Element                       | Description                                                                         |
| ----------------------------- | ----------------------------------------------------------------------------------- |
| **Plant selector**            | Chooses which plant to configure targets for. Directors (2.0)+ can switch plants.   |
| **Production Line selector**  | Chooses which production line's work centers to display.                            |
| **Work Center rows**          | One row per work center, sorted by production sequence order.                       |
| **Gear Level columns**        | One column per gear level defined for the plant.                                    |
| **Target cell**               | Editable cell containing the units-per-hour target.                                 |
| **Per-tank-size toggle**      | Switches a cell between a single default value and individual values per tank size. |
| **Per-tank-size breakdown**   | When expanded, shows a sub-row for each tank size with its own target value.        |
| **Unsaved changes indicator** | Visual badge or banner that appears when any cell has been modified.                |
| **Save button**               | Bulk-saves all modified cells in a single operation.                                |
| **Reset button**              | Reverts all unsaved changes to the last saved values.                               |


## Tips

- Capacity targets directly affect OEE Performance calculations. If targets are too high, Performance will appear artificially low; if too low, it will appear inflated. Set realistic targets based on historical throughput data.
- Use the per-tank-size breakdown for work centers where cycle time varies significantly by tank size (e.g., a large tank takes much longer than a small tank at the same station).
- The grid saves all changes in a single bulk operation, so you can edit multiple cells and save once rather than saving each cell individually.
- If a cell is left empty (no target), OEE Performance cannot be calculated for that work center at that gear level — the Supervisor Dashboard will show a warning.

