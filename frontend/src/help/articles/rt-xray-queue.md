# RT X-ray Queue

A scan-to-add queue that feeds a separate Real Time X-ray application. An operator near the x-ray loading area scans shell barcodes as they enter the machine. The RT X-ray operator — who cannot see the shell once it's inside the equipment — pulls from this queue in their separate application.

## How It Works

1. **Scan a shell barcode** (`SC;{serial}`). The shell is **instantly added** to the FIFO queue — no form, no additional input.
2. **Green overlay** confirms: "Shell {serial} added to queue."
3. The queue list **auto-refreshes** after each scan, reflecting any removals made by the RT X-ray application.
4. Repeat for each shell entering the x-ray machine.

### Queue Item Removal

Items are removed in two ways:

| Trigger | Description |
|---|---|
| **RT X-ray app processes the shell** | The separate application pulls the next item and removes it after saving its inspection record. This is automatic. |
| **Manual delete** | Tap the trash icon on a queue row to remove an incorrectly scanned shell. A confirmation prompt appears. |

### Manual Mode

With External Input off, tap "Add Shell to Queue," type the serial number, and tap Save.

## Fields & Controls

| Element | Description |
|---|---|
| **Queue list** | FIFO list of scanned shells. Each row shows the serial number, timestamp, and a delete (trash) icon. |
| **Add Shell to Queue** | Button — opens a text input in manual mode. In External Input mode, scanning auto-adds. |
| **Refresh** | Manually reloads the queue from the server. |
| **Delete icon** | Removes a queue item (with confirmation). |

## Barcode Commands

| Barcode | Action |
|---|---|
| `SC;{serial}` | Add a shell to the queue. |

## Tips

- Duplicate shells are blocked — if a serial number is already in the queue, the system rejects the scan.
- Unknown serial numbers are also rejected — the shell must exist in the system before it can be queued.
- The queue auto-refreshes on every scan, so you don't need to manually tap Refresh to see removals by the RT X-ray app.
- This screen exists solely to bridge the physical gap — once a shell is inside the x-ray machine, it can't be scanned.
