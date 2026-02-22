# Kanban Card Management

Manage the set of reusable kanban cards used at Fitup to link head lots to tank assemblies. Cards appear as a card grid with color swatches for quick visual identification. Admins can add and delete kanban cards. Deleting a kanban card is a hard delete.

## How It Works

1. **Browse.** Each card in the grid displays the color swatch, card value, color name, and description.
2. **Add a kanban card.** Click **Add Kanban Card**. Enter the barcode value, choose a color name, and add an optional description. Click **Save**.
3. **Delete a kanban card.** Click a card and choose **Delete**. This is a permanent hard delete — the card is removed from the system entirely.

### Usage at Fitup

At the Fitup work center, operators scan a kanban card barcode to associate a head lot with the current assembly. The color coding helps operators visually match physical cards on the floor to the correct lot.

## Fields & Controls

| Element | Description |
|---|---|
| **Card Value** | The barcode value printed on the physical kanban card. Must be unique. Required. |
| **Color Name** | A named color (e.g., "Red", "Blue", "Green"). The system maps color names to hex codes for the swatch display. Required. |
| **Color Swatch** | Visual preview of the color displayed on the card in the grid. |
| **Description** | Optional free-text description of the card's purpose or lot association. |

## Tips

- Card values must match the barcode printed on the physical card exactly. Double-check before saving.
- Deleting a kanban card is permanent. Only delete cards that have been physically retired from the floor.
- Color names are mapped to predefined hex values. If you need a new color, contact an Admin to add it to the color map.
