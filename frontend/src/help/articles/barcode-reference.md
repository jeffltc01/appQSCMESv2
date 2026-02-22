# Barcode Command Reference

Quick reference for the MES barcode scanning language. All commands follow the format **PREFIX;VALUE** and are read by a USB barcode scanner acting as a keyboard wedge.

## Command List

| Barcode | Action | Description |
|---|---|---|
| `SC;{serial}` | Scan shell | Identifies a shell by its serial number. Accepts `/L1` or `/L2` label suffix. |
| `D;{code}` | Defect code | Logs a defect code during inspection. |
| `L;{loc}` | Defect location | Logs a defect location during inspection. |
| `L;{loc};C;{char}` | Location + characteristic | Logs both a defect location and characteristic in one scan (Round Seam Inspection). |
| `FD;{code}-{char}-{loc}` | Full defect | Logs defect code, characteristic, and location in a single scan. |
| `S;1` | Save | Saves the current inspection record. |
| `CL;1` | Clear defects | Clears all pending defect entries from the current session. |
| `INP;1` | Swap heads | At Fitup — swaps left and right head lot assignments. |
| `INP;2` | Advance / reset | At Rolls — advances the material queue. At Fitup — resets the current assembly. |
| `INP;3` | Yes / pass / save | Context-dependent — confirms a prompt, passes an inspection, or saves at Fitup. |
| `INP;4` | No / fail | Context-dependent — declines a prompt or fails an inspection step. |
| `FLT;{text}` | Report fault | Logs a machine fault (e.g., `FLT;Button Stuck`). |
| `KC;{value}` | Kanban card | Scans a kanban card to associate head material at Fitup. |
| `TS;{size}` | Change tank size | Overrides the tank size at Fitup (e.g., `TS;500`, `TS;1000`). |
| `NOSHELL;0` | No shell | At Hydro — indicates no physical shell (test/calibration run). |
| `{serial}` *(no prefix)* | Nameplate barcode | At Hydro — scanned nameplate serial number (e.g., `W00123456`). Detected by the absence of a prefix. |

## Station Compatibility

| Command | Rolls | Long Seam | LS Insp | Fitup | Round Seam | RS Insp | RT Queue | Hydro |
|---|---|---|---|---|---|---|---|---|
| `SC;` | Yes | Yes | Yes | Yes | Yes | Yes | Yes | Yes |
| `D;` | — | — | Yes | — | — | Yes | — | — |
| `L;` | — | — | Yes | — | — | — | — | — |
| `L;…;C;` | — | — | — | — | — | Yes | — | — |
| `FD;` | — | — | Yes | — | — | Yes | — | — |
| `S;1` | — | — | Yes | — | — | Yes | — | — |
| `CL;1` | — | — | Yes | — | — | Yes | — | — |
| `INP;1` | — | — | — | Yes | — | — | — | — |
| `INP;2` | Yes | — | — | Yes | — | — | — | — |
| `INP;3` | Yes | — | — | Yes | — | — | — | — |
| `INP;4` | Yes | — | — | — | — | — | — | — |
| `FLT;` | Yes | — | — | — | — | — | — | — |
| `KC;` | — | — | — | Yes | — | — | — | — |
| `TS;` | — | — | — | Yes | — | — | — | — |
| `NOSHELL;0` | — | — | — | — | — | — | — | Yes |

## Tips

- Barcode scan cards are laminated and kept at each station. They are reusable — just scan the card when you need the command.
- The semicolon (`;`) is the separator between the prefix and value. If a scan doesn't register, check that the barcode encodes the semicolon correctly.
- At inspection stations, defect code and location can be scanned in either order. The entry is complete when both are provided.
- Nameplate barcodes at Hydro have **no prefix** — the system distinguishes them from shell barcodes by the absence of `SC;`.
