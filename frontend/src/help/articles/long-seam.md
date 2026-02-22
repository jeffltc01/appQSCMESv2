# Long Seam

The Long Seam work center is the second station in the production line, immediately after Rolls. The operator welds the longitudinal seam on each rolled shell and scans the shell barcode to create a production record. This is one of the simplest screens in the MES — a single scan captures everything.

## How It Works

1. **Shell arrives** from Rolls with two pre-printed barcode labels already affixed.
2. The operator welds the longitudinal seam.
3. **Scan either label** — Label 1 (`SC;XXXXXX/L1`) or Label 2 (`SC;XXXXXX/L2`). The system accepts either one.
4. A **production record** is created capturing the serial number, welder, operator, and timestamp.
5. **Green overlay** confirms the record. The input clears, ready for the next shell.

### Manual Mode

With External Input toggled off, type the serial number into the text field and tap **Submit**.

### Catch-Up Flow (Missed Rolls Scan)

Occasionally the Rolls operator affixes barcode labels but forgets to scan. When that shell arrives at Long Seam and the serial number is unknown:

1. The system **auto-creates** the serial number record.
2. The material lot is **assumed** from the previous shell scanned at this station.
3. An **automatic annotation** is created flagging the record for lot validation.
4. The operator sees a green overlay with a note: "Shell recorded (Rolls missed — annotation created)."
5. A Team Lead, Supervisor, or Quality can review and correct the lot assignment later via the annotation flag in WC History.

## Fields & Controls

| Element | Description |
|---|---|
| **Scan Shell Label** | Manual serial number input (hidden when External Input is On). |
| **Submit** | Submits the manually entered serial number. |

## Barcode Commands

| Barcode | Action |
|---|---|
| `SC;{serial}` | Scan a shell label (Label 1 or Label 2). The `/L1` or `/L2` suffix is stripped automatically. |

## Tips

- You can scan whichever label is easier to reach — both identify the same shell.
- If you see a green overlay with an annotation note, the upstream Rolls scan was missed. Production continues normally; the annotation flags the data gap for correction.
- Duplicate scans are rejected — the system prevents the same shell from being recorded twice at Long Seam.
- A welder must be signed in before scanning. If no welder is active, the Welder Gate dialog blocks the screen.
