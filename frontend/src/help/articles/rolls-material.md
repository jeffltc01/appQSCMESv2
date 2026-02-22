# Rolls Material Queue

Material Handlers use this screen to document steel plate loaded at the Rolls station and add it to a FIFO queue. The Rolls operator advances this queue (via `INP;2`) to pull material data into their active production session. This is a manual-entry-only screen — no barcode scanning.

## How It Works

1. **Tap "Add Material to Queue."** The entry form opens.
2. **Select a Product.** Tap the Product field to open a full-screen popup of plate products (tank size and plate dimensions). Tap to select.
3. **Select Plate Mill.** Tap to open the popup and choose the steel mill that produced the plate.
4. **Select Plate Processor.** Tap to open the popup and choose the plate processor/distributor.
5. **Enter Heat Number.** Type the heat number from the plate certificate.
6. **Enter Coil Number.** Type the coil identifier from the plate certificate.
7. **Enter Lot Number** *(optional).* Type a lot number if available.
8. **Enter Quantity.** Type the number of shells this plate batch will produce.
9. **Tap Save.** The item appears in the queue. The Rolls operator will see it next time they advance or refresh.

### Editing and Deleting

- Tap the **pencil icon** on any queue item to edit it.
- Tap the **trash icon** to delete it (with confirmation). Items currently being used by the Rolls operator cannot be edited or deleted.

## Fields & Controls

| Element | Type | Required | Description |
|---|---|---|---|
| **Product** | Selection popup | Yes | Plate product — tank size and plate dimensions. Large touch-friendly list. |
| **Plate Mill** | Selection popup | Yes | The steel mill vendor. |
| **Plate Processor** | Selection popup | Yes | The plate processor/distributor. |
| **Heat Number** | Text input | Yes | Steel heat number for traceability. |
| **Coil Number** | Text input | Yes | Coil identifier for traceability. |
| **Lot Number** | Text input | No | Optional lot identifier. |
| **Quantity** | Numeric input | Yes | Number of shells this batch will produce. |
| **Save** | Button | — | Saves the entry to the queue. Disabled until all required fields are filled. |
| **Cancel** | Button | — | Discards changes and returns to the queue view. |
| **Refresh** | Button | — | Reloads the queue list from the server. |

## Tips

- Selection popups use large touch targets designed for glove-friendly use — tap anywhere on the row to select.
- Queue items are displayed in the order they were added (FIFO). The Rolls operator advances from the top.
- Once the Rolls operator finishes with a queue item (all shells rolled), the item is automatically deleted. Traceability data is already captured in the serial number records at that point.
- There is no enforced queue depth limit, but typically only 1–3 items are staged at once.
