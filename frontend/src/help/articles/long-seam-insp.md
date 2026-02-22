# Long Seam Inspection

The Long Seam Inspection station is where an inspector visually checks the longitudinal weld seam on each shell. Defects are logged via barcode scan cards or manual dropdowns, and each shell is saved with either a clean record or a list of defects.

## How It Works

1. **Scan the shell** (`SC;{serial}`). The screen loads the serial number, tank size, and transitions to defect entry mode.
2. **If no defects** — scan **Save** (`S;1`) immediately. A clean inspection record is created. Two scans total for the happy path.
3. **If defects found** — scan one or more defect entries, then scan Save:
   - Scan a **defect code** (`D;{code}`).
   - Scan a **location** (`L;{loc}`).
   - The **characteristic** (Long Seam) is auto-assumed — you do not need to scan or select it.
   - Repeat for additional defects.
4. **Save** (`S;1`). The inspection record is created with all logged defects. Green overlay confirms.
5. The screen resets, ready for the next shell.

### Clearing Mistakes

Scan `CL;1` to **clear all** defect entries and start over. The screen stays in defect entry mode so you can re-enter or save clean.

### Compound Scan

Use `FD;{code}-{char}-{loc}` to log defect code, characteristic, and location in a single scan.

## Fields & Controls

| Element | Description |
|---|---|
| **Serial Number input** | Manual entry field (hidden when External Input is On). |
| **Defect table** | Shows logged defects with columns: Defect, Characteristic, Location. |
| **Defect Code dropdown** | Manual mode — select from defect codes applicable to this work center. |
| **Location dropdown** | Manual mode — select from locations applicable to this work center. |
| **Save button** | Manual mode — saves the inspection record. |
| **Clear All button** | Manual mode — clears all defect entries. |

## Barcode Commands

| Barcode | Action | When |
|---|---|---|
| `SC;{serial}` | Load a shell for inspection | Before defects |
| `D;{code}` | Log a defect code | During defect entry |
| `L;{loc}` | Log a defect location | During defect entry |
| `FD;{code}-{char}-{loc}` | Log a full defect in one scan | During defect entry |
| `S;1` | Save the inspection record | After shell loaded |
| `CL;1` | Clear all defect entries | During defect entry |

## Tips

- Defect code and location can be scanned in either order. The entry completes when both are provided.
- Laminated scan sheets at the station list all applicable defect codes and locations with their barcodes.
- If you scan a new shell while one is already loaded, the system rejects it. Save or clear first.
- The same catch-up logic from Long Seam applies — if the serial is unknown, it is auto-created with an annotation.

## Changes from MES v1

- **O;1 override removed.** The override save command is no longer supported. If override is needed in the future it can be re-added.
- **Characteristic auto-assumed.** Same as v1 — the Long Seam characteristic is applied automatically.
