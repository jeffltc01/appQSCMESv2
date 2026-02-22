# Hydrostatic Test (Hydro)

Hydro is the final production station. The tank is filled with water and pressurized to test all welds. The operator **marries the assembly to the finished serial number** (nameplate), performs a final inspection, and accepts or rejects the tank. This creates the traceability link from the customer-facing serial number all the way back to raw material lots.

## How It Works

### Scanning (Either Order)

1. **Scan the shell barcode** (`SC;{serial}`) — the system looks up the assembly alpha code via the traceability log.
2. **Scan the nameplate barcode** (e.g., `W00123456`, no prefix) — the system verifies the nameplate record exists.
3. These can happen in **either order**. The system detects which is which:
   - Starts with `SC;` → shell barcode.
   - No recognized prefix → nameplate barcode.
4. Once both are scanned, the screen shows the assembly alpha code, nameplate serial number, and tank size.

### Accept — No Defects (Fast Path)

If the hydro test passes with no issues, tap **"Accept - No Defects"**. A clean record is saved, the assembly is linked to the finished serial number, and a green overlay confirms. This is the fastest path — most tanks pass.

### Add Defects via the Defect Wizard

1. Tap **"Add Defect"** to open the full-screen Defect Wizard.
2. **Step 1 — Select Defect:** A 3-column grid of large buttons shows all applicable defect codes (e.g., Crack, Undercut, Pinhole/Porosity).
3. **Step 2 — Select Characteristic:** A grid of characteristics (e.g., Long Seam, Round Seam 1–4, Flange, Attachment). A breadcrumb at the bottom shows your defect selection.
4. **Step 3 — Select Location:** A grid of locations filtered by the selected characteristic (e.g., for Flange: Service Valve, Relief Valve, Fill Valve). Breadcrumb shows defect + characteristic.
5. The defect is added to the table. Tap **"Add Defect"** again for additional entries.
6. Individual defects can be removed with the **trash icon** on each row.

### Accept or Reject

After adding defects, tap **Accept** or **Reject**:
- **Accept** — the tank passed overall (defects may be minor/cosmetic).
- **Reject** — the tank failed the hydro test.

The record is saved with the result and defect list. The assembly-to-finished-serial-number link is created regardless of the outcome.

### NOSHELL;0

Scan `NOSHELL;0` when no physical shell is present (test run or calibration scenario).

## Fields & Controls

| Element | Description |
|---|---|
| **Scan input** | Accepts both shell barcodes (`SC;{serial}`) and nameplate barcodes (no prefix). Hidden when External Input is On. |
| **Nameplate / Shell display** | Shows the nameplate serial number and assembly alpha code after both scans. |
| **Accept - No Defects** | One-tap save for clean tanks. Changes to "Save Defect(s)" once defects are added. |
| **Add Defect** | Opens the Defect Wizard. |
| **Defect table** | Shows logged defects with columns: Defect, Characteristic, Location. Each row has a delete icon. |
| **Accept / Reject buttons** | Final save with result. Disabled until both shell and nameplate are scanned. |
| **Reset** | Clears all data and returns to the initial scan state. |

## Barcode Commands

| Barcode | Action |
|---|---|
| `SC;{serial}` | Scan shell — looks up the assembly. |
| `{nameplateSN}` *(no prefix)* | Scan nameplate — identifies the finished serial number. |
| `NOSHELL;0` | No shell — test/calibration scenario. |

Defect entry uses the touch-based Defect Wizard, not barcode scanning.

## Tips

- The Defect Wizard uses large tile buttons in a 3-column grid — designed for easy tapping on a tablet.
- Locations are filtered by the characteristic you selected. For example, choosing "Flange" shows valve locations; choosing "Round Seam 1" shows weld position locations.
- If the assembly has already been matched to a different nameplate, the system warns you but allows you to proceed (for rework scenarios).
- Scanning a second shell or nameplate replaces the first scan of that type.

## Changes from MES v1

- **Defect Wizard with tile grids** replaces the old dropdown/scan sheet approach. The 3-step wizard (Defect → Characteristic → Location) with large touch buttons is easier and faster than cascading dropdowns.
- **Individual defect delete** — each defect row has its own trash icon, replacing the all-or-nothing `CL;1` approach.
- **Breadcrumb navigation** in the wizard shows your selections as you go (e.g., "Crack - Flange").
