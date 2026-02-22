# Downtime Reasons

Downtime Reasons manages the categories and individual reasons used to classify production downtime events. The screen uses a two-panel master-detail layout: categories on the left, reasons within the selected category on the right. Both categories and reasons support add, edit, and delete operations.

**Access:** Quality Manager (3.0) and above. Directors (2.0) and above can switch between plants.

## How It Works

### Managing Categories

1. **View categories.** The left panel lists all downtime reason categories for the current plant, sorted by sort order.
2. **Add a category.** Click **Add Category**. Enter the category name, sort order, and active status. Click **Save**.
3. **Edit a category.** Click the edit action on a category row. Update the fields and click **Save**.
4. **Delete a category.** Click the delete action on a category row and confirm. A category can only be deleted if it has no reasons assigned to it.

### Managing Reasons

1. **Select a category.** Click a category in the left panel to load its reasons in the right panel.
2. **Add a reason.** Click **Add Reason**. Enter the reason name, sort order, and active status. Click **Save**.
3. **Edit a reason.** Click the edit action on a reason row. Update the fields and click **Save**.
4. **Delete a reason.** Click the delete action on a reason row and confirm.

## Fields & Controls

### Category Fields


| Field             | Required | Description                                                                                  |
| ----------------- | -------- | -------------------------------------------------------------------------------------------- |
| **Category Name** | Yes      | A descriptive name for the downtime category (e.g., "Mechanical," "Electrical," "Material"). |
| **Sort Order**    | Yes      | Numeric value that controls the display order of categories. Lower numbers appear first.     |
| **Active**        | Yes      | Whether the category is available for selection when recording downtime.                     |


### Reason Fields


| Field           | Required | Description                                                                               |
| --------------- | -------- | ----------------------------------------------------------------------------------------- |
| **Reason Name**          | Yes      | A specific downtime reason within the category (e.g., "Bearing Failure," "Power Outage").                                                                                                        |
| **Sort Order**           | Yes      | Numeric value that controls the display order within the category.                                                                                                                               |
| **Active**               | Yes      | Whether the reason is available for selection when recording downtime.                                                                                                                           |
| **Counts as Downtime**   | Yes      | When ON (default), this reason counts against OEE availability. Turn OFF for reasons like "Scanner Issue" or "System Outage" where the operator was still producing but couldn't scan. |


### Controls


| Element                        | Description                                                                                      |
| ------------------------------ | ------------------------------------------------------------------------------------------------ |
| **Plant Selector**             | Switches the view to another plant's downtime reasons. Visible only to Director (2.0) and above. |
| **Category list (left panel)** | Master list of downtime categories. Click to select and load reasons.                            |
| **Reason list (right panel)**  | Detail list of reasons within the selected category.                                             |
| **Add Category / Add Reason**  | Opens the creation form for a category or reason respectively.                                   |


## Tips

- Use sort order to group related items logically. Operators see these in sort order when recording downtime, so put the most common reasons near the top.
- Deactivating a category or reason is preferable to deleting it — deactivated items stop appearing in selection lists but historical downtime records that reference them remain intact.
- Each plant has its own set of downtime reasons. If you need the same categories across plants, they must be created separately in each plant.

