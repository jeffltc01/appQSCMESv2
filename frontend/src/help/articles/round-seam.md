# Round Seam

The Round Seam work center is where circumferential welds are made, joining shells to heads and shells to shells using automated SAW and MIG weld machines. Each round seam weld position is tracked to the individual welder who performed it.

## How It Works

### Round Seam Setup (Required First)

Before any shells can be scanned, the operator must complete the setup:

1. **Tap "Roundseam Setup"** (or acknowledge the yellow warning banner).
2. **Select the tank size** from the dropdown. This determines how many welder assignments are needed.
3. **Assign a welder to each round seam position** from the dropdowns. Welders are drawn from the top-bar welder list.
4. **Tap "Save Setup."** The warning banner disappears and scanning is enabled.

| Tank Size | Round Seam Positions |
|---|---|
| ≤ 500 (1 shell) | RS1, RS2 |
| 1000 (2 shells) | RS1, RS2, RS3 |
| > 1000 (3 shells) | RS1, RS2, RS3, RS4 |

The same welder can be assigned to all positions. Setup can be re-run mid-shift when welders rotate — a button is always accessible on the main screen.

### Scanning Shells

1. **Scan a shell barcode** (`SC;{serial}`).
2. The system looks up the assembly alpha code via the traceability log.
3. A **production record** is created against the assembly with the per-seam welder assignments from setup.
4. **Green overlay** confirms: "Assembly {alphaCode} recorded."
5. The input clears, ready for the next shell.

### Manual Mode

With External Input off, type the serial number and tap **Submit**.

## Fields & Controls

| Element | Description |
|---|---|
| **Warning Banner** | Yellow banner: "WARNING: Roundseam setup hasn't been completed!" Shown until setup is saved. Blocks scanning. |
| **Roundseam Setup button** | Opens the setup screen. Always accessible, not just during the warning. |
| **Tank Size dropdown** | In setup — select the current tank size being run. |
| **Welder dropdowns** | In setup — one per round seam position (2–4 depending on tank size). |
| **Save Setup button** | Saves the welder assignments for the current session. |
| **Serial Number input** | Manual entry field on the main screen (hidden when External Input is On). |
| **Submit button** | Submits the manually entered serial number. |

## Barcode Commands

| Barcode | Action |
|---|---|
| `SC;{serial}` | Scan a shell — looks up the assembly and creates a production record. |

No other barcode commands are used at this station. The setup screen is touch-only.

## Tips

- Per-seam welder tracking is critical for downstream Spot X-ray — the system needs to know which welder made each weld.
- If the assembly's tank size doesn't match the setup tank size, the system warns you to update setup.
- If a welder assigned in setup is later removed from the top bar, you'll see a warning on the next scan. Update the setup.
- The asset cached during Tablet Setup identifies which machine type (SAW or MIG) is being used.
