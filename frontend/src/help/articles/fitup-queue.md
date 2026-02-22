# Fitup Queue (Heads Queue)

Material Handlers use this screen to document head material and associate it with a physical kanban card. The Fitup operator scans the kanban card (`KC;{XX}`) at their station to pull the head lot into an assembly.

## How It Works

1. **Tap "Add Material to Queue."** The entry form opens.
2. **Select a Product.** Tap to open a full-screen popup of head products (tank size and head type). Tap to select.
3. **Select the Head Vendor.** Two options: **CMF (Commercial Metal Forming)** or **Compco Industries**. The form fields change based on your selection.
4. **Enter vendor-specific fields:**
   - **CMF** — type the **Lot Number**.
   - **Compco** — type the **Heat Number** and **Coil/Slab Number**.
5. **Scan the Queue Card.** Scan the physical kanban card barcode (`KC;{XX}`). The card code and color are displayed after the scan.
6. **Tap Save.** The item appears in the queue with a color swatch matching the kanban card.

### Editing and Deleting

- Tap the **pencil icon** to edit a queue item. If you change the kanban card, the old card becomes available again.
- Tap the **trash icon** to delete (with confirmation). The kanban card returns to the available pool.

## Fields & Controls

| Element | Type | Required | Description |
|---|---|---|---|
| **Product** | Selection popup | Yes | Head product — tank size and head type/dimensions. |
| **Head Vendor** | Selection popup | Yes | CMF or Compco Industries. Changes which fields appear below. |
| **Lot Number** | Text input | Yes (CMF) | Lot identifier from CMF. Only shown when CMF is selected. |
| **Heat Number** | Text input | Yes (Compco) | Steel heat number. Only shown when Compco is selected. |
| **Coil/Slab Number** | Text input | Yes (Compco) | Coil or slab identifier. Only shown when Compco is selected. |
| **Queue Card** | Barcode scan | Yes | Kanban card barcode (`KC;{XX}`). Displays card code and color after scan. Manual fallback: type the card code. |
| **Save** | Button | — | Saves the entry. Disabled until all required fields are filled. |
| **Cancel** | Button | — | Discards changes and returns to the queue view. |

## Tips

- Each kanban card can only be assigned to **one active queue entry** at a time. If you scan a card that's already in use, you'll see a warning.
- Changing the vendor after filling fields clears the vendor-specific inputs.
- Unlike the Rolls Material Queue, there is no automatic quantity tracking. You are responsible for removing queue entries when head material is consumed.
- Kanban cards are reusable physical objects — after a queue entry is deleted, the card returns to the pool.
- Selection popups use large touch targets for glove-friendly use.
