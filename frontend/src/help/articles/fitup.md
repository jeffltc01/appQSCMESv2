# Fitup

The Fitup work center is where shells are married with heads to form a tank assembly. The system assigns a sequential **alpha code** (AA through ZZ) that becomes the assembly's identifier for all downstream stations.

## How It Works

### New Assembly

1. **Scan the first shell** (`SC;{serial}`). The system looks up the serial number and determines the tank size. The visual assembly diagram updates.
2. **Scan additional shells** (if needed). Tanks 1000 require 2 shells; tanks larger than 1000 require 3.
3. **Scan a kanban card** (`KC;{XX}`) to identify the head material. The first card applies to **both** the left and right heads. An optional second card overrides the right head only.
4. **Swap heads** *(optional)* — scan `INP;1` to reverse the left/right head lot assignments.
5. **Save** — scan `INP;3`. The system assigns the next alpha code, creates the assembly record and traceability links.
6. The **alpha code is displayed prominently** for 30 seconds (or until the next shell is scanned) so the operator can write it on the physical assembly.

### Reset

Scan `INP;2` or tap the Reset button to clear all scanned data without saving.

### Tank Size Override

If the wrong material was selected at Rolls, scan `TS;{size}` (e.g., `TS;500`, `TS;1000`) to change the assembly's tank size. The number of required shells adjusts automatically.

### Reassembly

If you scan a shell that already belongs to an assembly, the system prompts: "This shell is part of assembly {alphaCode}. Are you reassembling?" Choose Yes to enter reassembly mode where you can replace heads or split multi-shell assemblies.

## Assembly Composition

| Tank Size | Shells | Heads | Layout |
|---|---|---|---|
| ≤ 500 (120, 250, 320, 500) | 1 | 2 | Head — Shell — Head |
| 1000 | 2 | 2 | Head — Shell 1 — Shell 2 — Head |
| > 1000 (1450, 1990) | 3 | 2 | Head — Shell 1 — Shell 2 — Shell 3 — Head |

## Fields & Controls

| Element | Description |
|---|---|
| **Visual Assembly Diagram** | Dynamic display of heads and shells. Fills in as components are scanned. |
| **Tank Size dropdown** | Displays the current tank size. Changeable manually or via `TS;` scan. |
| **Heads Queue** | List of queued head material at the bottom of the screen. Each row shows product, heat/coil info, and a colored kanban card indicator. |
| **Reset button** | Clears all scanned data. |
| **Save button** | Finalizes the assembly and assigns an alpha code. |
| **Refresh button** | Reloads the Heads Queue from the server. |

## Barcode Commands

| Barcode | Action |
|---|---|
| `SC;{serial}` | Scan a shell into the assembly |
| `KC;{XX}` | Scan kanban card — pull head lot from the queue |
| `INP;1` | Swap left/right head assignments |
| `INP;2` | Reset — clear all and start over |
| `INP;3` | Save — finalize assembly, assign alpha code |
| `TS;{size}` | Change tank size (120, 250, 320, 500, 1000, 1450, 1990) |

## Tips

- The alpha code cycles AA → AB → ... → AZ → BA → ... → ZZ, then restarts at AA (676 codes per cycle).
- If the Heads Queue is empty, you cannot save. Contact Material Handling to add head material via the Fitup Queue screen.
- Color swatches on queue rows match the physical kanban card colors — use them to visually confirm you're scanning the right card.
- Changing the tank size mid-assembly adjusts the required shell count. Reducing from 2 shells to 1 drops the second shell with a warning.
