# Round Seam Inspection

The Round Seam Inspection station is where an inspector checks the circumferential weld seams on each assembly. Unlike Long Seam Inspection where the characteristic is auto-assumed, the inspector must explicitly specify which round seam position (RS1–RS4) has the defect.

## How It Works

1. **Scan a shell** (`SC;{serial}`). The system looks up the assembly alpha code via the traceability log. The screen displays the alpha code, tank size, and an empty defect table.
2. **If no defects** — scan **Save** (`S;1`) immediately. A clean inspection record is created.
3. **If defects found** — scan one or more defect entries, then Save:
   - Scan a **defect code** (`D;{code}`).
   - Scan a **location + characteristic** (`L;{loc};C;{char}`) — the `C;{char}` part specifies which round seam (e.g., RS1, RS2).
   - Or use the compound scan: `FD;{code}-{char}-{loc}`.
   - Repeat for additional defects.
4. **Save** (`S;1`). The inspection record is created against the assembly. Green overlay confirms.
5. The screen resets, ready for the next shell.

### Clearing Mistakes

Scan `CL;1` to clear all defect entries.

## Fields & Controls

| Element | Description |
|---|---|
| **Serial Number input** | Manual entry field (hidden when External Input is On). |
| **Assembly / Tank Size display** | Shows the assembly alpha code and tank size after a shell is scanned. |
| **Defect table** | Columns: Defect, Characteristic (RS1–RS4), Location. |
| **Defect Code dropdown** | Manual mode — defect codes applicable to this work center. |
| **Characteristic dropdown** | Manual mode — round seam positions filtered by tank size. |
| **Location dropdown** | Manual mode — locations applicable to this work center. |
| **Save button** | Saves the inspection record. |
| **Clear All button** | Clears all defect entries. |

## Barcode Commands

| Barcode | Action | When |
|---|---|---|
| `SC;{serial}` | Scan shell — looks up the assembly | Before defects |
| `D;{code}` | Log a defect code | During defect entry |
| `L;{loc};C;{char}` | Log location and characteristic together | During defect entry |
| `FD;{code}-{char}-{loc}` | Log a full defect in one scan | During defect entry |
| `S;1` | Save the inspection record | After shell loaded |
| `CL;1` | Clear all defect entries | During defect entry |

## Characteristic Rules by Tank Size

| Tank Size | Available Characteristics |
|---|---|
| ≤ 500 | RS1, RS2 |
| 1000 | RS1, RS2, RS3 |
| > 1000 | RS1, RS2, RS3, RS4 |

Scanning a characteristic that doesn't apply to the current tank size (e.g., RS3 on a 500-gallon assembly) is rejected with a red overlay.

## Tips

- Defect code and location+characteristic can be scanned in either order.
- Laminated scan sheets at the station list all applicable defect codes, locations, and characteristics with their barcodes.
- Data is logged against the **assembly** (alpha code), not the individual shell serial number.
- The shell must have a Round Seam production record before it can be inspected here.

## Changes from MES v1

- **O;1 override removed.** The override save command is no longer supported.
- **RS3 rejected for ≤ 500 assemblies.** The system now validates that the characteristic is valid for the assembly's tank size.
