# Nameplate / Data Plate

The Nameplate station creates the data plate that is welded onto the finished tank at Hydro. This is a **PC-based** station (not a tablet) using **keyboard only** — there is no barcode scanner. After the operator enters the serial number, the system auto-prints a barcode label via NiceLabel Cloud.

## How It Works

1. **Select Tank Size / Type** from the dropdown (e.g., "500 - Propane", "1000 - Propane").
2. **Type the finished serial number** (e.g., `W00123456`) that was just etched onto the physical nameplate.
3. **Click Save.**
4. The system creates a nameplate record and **automatically prints a barcode label** to the local printer.
5. Place a foil cover over the etched nameplate, then affix the printed barcode label on top.
6. The screen clears, ready for the next nameplate.

### Reprint

If a label is damaged or the print fails, use the **Reprint** option to print another label for the same record.

## Fields & Controls

| Element | Type | Required | Description |
|---|---|---|---|
| **Tank Size / Type** | Dropdown | Yes | Combined size and type selector (e.g., "500 - Propane"). |
| **Serial Number** | Text input | Yes | The finished serial number etched on the nameplate. Typed by hand. |
| **Save** | Button | — | Creates the record and triggers label printing. Disabled until both fields are filled. |
| **Reprint** | Button | — | Reprints the barcode label for the last saved record. |

## Barcode Format

The nameplate barcode encodes **only the serial number** with **no prefix** — for example, `W00123456`, not `SC;W00123456`. Downstream at Hydro, the system detects a nameplate barcode by the absence of the `SC;` prefix.

## Tips

- This station uses a PC, not a tablet. The same operator layout shell renders at desktop resolution.
- The External Input toggle is not used here since there is no barcode scanner.
- Duplicate serial numbers are rejected — if the number already exists in the system, you'll see an error.
- If the print fails, the record is still saved. Use Reprint to get the label after fixing the printer connection.
