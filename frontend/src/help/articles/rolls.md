# Rolls

The Rolls work center is the first station in the tank manufacturing process. Here, flat steel plate is rolled into cylindrical shells. Each shell gets two barcode labels that are scanned to create a production record.

## How It Works

### Barcode-Driven Flow (External Input On)

1. **Advance the Material Queue.** Scan the **INP;2** barcode card (or tap "Yes" on the advance prompt) to load the next material from the queue. The screen displays the shell size, heat number, coil number, queue quantity, and material remaining count.
2. **Scan Label 1.** Scan the first barcode label on the shell. The screen confirms "Label 1 scanned — Scan Label 2."
3. **Scan Label 2.** Scan the second barcode label. The system verifies both labels match the same serial number.
4. **Thickness Inspection.** After both labels match, you are prompted for a thickness inspection result. Scan **INP;3** for Pass or **INP;4** for Fail.
5. **Record Created.** A production record is saved and the WC History panel on the right updates with the new entry.
6. **Repeat.** Scan the next shell's Label 1 to continue. When material remaining reaches zero, you are prompted to advance the queue again.

### Manual Flow (External Input Off)

When the External Input toggle in the bottom bar is Off, you can type a serial number into the "Scan Shell Label" input field and tap Submit. The thickness inspection prompt still appears. You can also tap a material queue card to advance the queue.

## Fields & Controls

| Element | Description |
|---|---|
| **Scan Shell Label** | Manual serial number input (hidden when External Input is On). |
| **Scan state indicator** | Shows the current step: "Advance the material queue to begin," "Scan Label 1," or "Scan Label 2." |
| **Shell Count** | Shows how many shells have been rolled from the current material (e.g., "3 of 10"). |
| **Shell Size** | The tank size for the active material. |
| **Heat Number** | The steel heat number for traceability. |
| **Coil Number** | The coil number for traceability. |
| **Queue Quantity** | Total shells expected from this material queue entry. |
| **Material Remaining** | How many shells are left before the material is consumed. |
| **Material Queue** | A list of queued material entries below the data grid. Tap an entry to advance (manual mode) or use the Refresh button to reload. |
| **Advance Queue prompt** | Appears when material remaining hits zero. Choose Yes to advance or No to stay on the current material. |
| **Thickness Inspection prompt** | Appears after both labels match. Choose Pass or Fail. |

## Barcode Commands

| Barcode | Action |
|---|---|
| **SC;{serial}** | Scan a shell label (Label 1 or Label 2). |
| **INP;2** | Advance the material queue. |
| **INP;3** | Yes / Pass (context-dependent). |
| **INP;4** | No / Fail (context-dependent). |
| **FLT;{text}** | Report a fault (e.g., "Button Stuck," "Gnd Fault"). |

## Tips

- The material queue is managed by Material Handling using the Rolls Material Queue screen. If the queue is empty, contact Material Handling.
- If the two labels do not match, the scan state resets and you must start over with Label 1.
- The WC History panel on the right shows the day count and last 5 records. Tap an annotation icon on a history row to create or view an annotation for that record.

## Changes from MES v1

- **Dual-label scanning** works the same as in v1 but with clearer on-screen prompts between Label 1 and Label 2.
- **Thickness inspection** is now prompted automatically after both labels match, rather than being a separate step triggered by a barcode card.
- **Material queue** is displayed directly on the Rolls screen instead of being managed on a separate page.
- **Advance queue prompt** appears automatically when material remaining reaches zero.
