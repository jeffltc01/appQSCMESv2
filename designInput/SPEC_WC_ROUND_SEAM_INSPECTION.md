# MES v2 — Round Seam Inspection Work Center Specification

## 1. Work Center Overview

| Attribute | Value |
|---|---|
| **Work Center** | Round Seam Inspection |
| **Position in Line** | 8th — after Round Seam |
| **Purpose** | The inspector visually inspects the circumferential (round seam) welds on each assembly and logs any defects found. If no defects are found, the assembly is saved with a clean record. |
| **Operator Role** | Inspector / Operator (6.0) |
| **NumberOfWelders** | 0 — this is an inspection station |
| **Auto-Print Label** | No |
| **Input Modes** | External Input (barcode scanning) and Manual Mode (touch) |
| **Scan Sheets** | Laminated reference sheets at the station for defect codes, locations, and characteristics |

### 1.1 Key Differences from Long Seam Inspection

| Aspect | Long Seam Inspection | Round Seam Inspection |
|---|---|---|
| **Data logged against** | Shell serial number | Assembly alpha code (looked up via shell → TraceabilityLog) |
| **Characteristic** | Assumed (Long Seam) — inspector does not select | Must be selected — multiple round seam positions (RS1, RS2, RS3, RS4) depending on tank size |
| **Defect codes** | Filtered to Long Seam work center | Filtered to Round Seam Inspection work center (may include additional codes) |
| **Locations** | Filtered to Long Seam work center | Filtered to Round Seam Inspection work center |

---

## 2. Screen Layout

This screen renders inside the Work Center Content Area of the Operator Work Center Layout (per [SPEC_OPERATOR_WC_LAYOUT.md](SPEC_OPERATOR_WC_LAYOUT.md)).

### 2.1 Initial State — Waiting for Shell Scan

Same as Long Seam Inspection: "Scan Serial Number to begin..." with a serial number input and submit button.

### 2.2 Defect Entry State — Awaiting Defects

```
+----------------------------------------------------------+
|  AwaitingDefects                                          |
|                                                           |
|  Assembly  AB              Tank Size  500                 |
|                                                           |
|  Defect          Characteristic         Location          |
|  +-------------------------------------------------+     |
|  | Porosity       | RS1                  | RS1     |     |
|  | Undercut       | RS2                  | RS2     |     |
|  |                |                      |         |     |
|  +-------------------------------------------------+     |
|                                                           |
+----------------------------------------------------------+
```

After the shell is scanned, the screen shows the **assembly alpha code** and tank size (not the shell serial number). The defect table includes Defect, Characteristic, and Location columns — the Characteristic column now shows the specific round seam position.

---

## 3. Screen States

| State | Description | Transitions |
|---|---|---|
| **WaitingForShell** | Initial state. Scan prompt shown. | → AwaitingDefects (on successful shell scan and assembly lookup) |
| **AwaitingDefects** | Assembly loaded. Inspector scans defects or saves immediately. | → WaitingForShell (on Save) |

---

## 4. Workflow

### 4.1 Sequence Diagram

```mermaid
sequenceDiagram
    participant Insp as Inspector
    participant Screen as RS Insp Screen
    participant API

    Note over Screen: State: WaitingForShell

    Insp->>Screen: Scan shell label (SC;XXXXXX)
    Screen->>API: GET /serial-numbers/{serial}/assembly
    API-->>Screen: alphaCode, tankSize, roundSeamCount
    Screen->>Screen: State → AwaitingDefects
    Screen->>Screen: Display assembly alpha code, tank size, empty defect table

    alt No defects found
        Insp->>Screen: Scan Save (S;1)
        Screen->>API: POST /inspection-records
        Note right of Screen: alphaCode, workCenterId, defects: []
        API-->>Screen: Success
        Screen->>Screen: Green overlay, State → WaitingForShell
    else Defects found
        Insp->>Screen: Scan defect code (D;XXX)
        Screen->>Screen: Add defect code to current entry
        Insp->>Screen: Scan location + characteristic (L;XXX;C;YYY)
        Screen->>Screen: Complete defect entry, add row to table
        Note over Screen: Repeat for additional defects
        Insp->>Screen: Scan Save (S;1)
        Screen->>API: POST /inspection-records
        Note right of Screen: alphaCode, workCenterId,<br/>defects: [{defectCode, characteristic, location}, ...]
        API-->>Screen: Success
        Screen->>Screen: Green overlay, State → WaitingForShell
    end
```

### 4.2 Step-by-Step Flow

**1. Scan Shell**

- The inspector scans a shell barcode (`SC;XXXXXX/L1` or `/L2`).
- The system strips the label suffix, looks up the serial number, and uses the **TraceabilityLog** to find the assembly alpha code.
- The screen transitions to **AwaitingDefects** — displaying the assembly alpha code, tank size, and an empty defect table.

**2. Log Defects (zero or more)**

- **Individual scanning**: The inspector scans a defect code (`D;XXX`) and then a location+characteristic (`L;XXX;C;YYY`). A row is added to the defect table.
- **Compound scanning**: `FD;XXX-YYY-ZZZ` (DefectCode-Characteristic-Location) — a row is added in one scan.
- **Characteristic is required**: Unlike Long Seam Inspection where the characteristic is assumed, the inspector must specify which round seam position (RS1, RS2, RS3, or RS4) is being flagged. The `L;XXX;C;YYY` and `FD;` formats include the characteristic.
- Multiple defects can be logged per assembly. Each entry adds a row.

**3. Clear Defects (optional)**

- Scanning `CL;1` clears all defect entries. The screen stays in AwaitingDefects.

**4. Save**

- Scanning `S;1` saves the inspection record against the **assembly**.
- Clean or with defects — same behavior as Long Seam Inspection.
- Green overlay confirms. Screen returns to WaitingForShell.

---

## 5. Defect Entry Detail

### 5.1 Defect Code

| Property | Value |
|---|---|
| **Source** | Master Defect Code table, filtered by this work center via DefectWorkCenter |
| **Barcode** | `D;XXX` where XXX is the defect code ID |
| **Manual mode** | Dropdown showing defects applicable to this work center |
| **Scan sheet** | Laminated sheet listing applicable defect codes |

### 5.2 Characteristic (Round Seam Position)

| Property | Value |
|---|---|
| **Behavior** | The inspector **must select** which round seam characteristic is being flagged. It is not assumed. |
| **Options** | RS1, RS2 (always). RS3 (if 1000+). RS4 (if > 1000). Filtered by the assembly's tank size. |
| **Barcode** | Included in `L;XXX;C;YYY` (the `C;YYY` part) or in `FD;XXX-YYY-ZZZ` (the YYY part) |
| **Manual mode** | Dropdown showing applicable round seam characteristics for the current tank size |

### 5.3 Defect Location

| Property | Value |
|---|---|
| **Source** | Defect Location table, filtered by this work center |
| **Barcode** | Included in `L;XXX;C;YYY` (the `L;XXX` part) or in `FD;XXX-YYY-ZZZ` (the ZZZ part) |
| **Manual mode** | Dropdown showing applicable locations |

### 5.4 Defect Entry Sequence

| Scan Pattern | Result |
|---|---|
| `D;XXX` then `L;YYY;C;ZZZ` | Defect row: DefectCode=XXX, Location=YYY, Characteristic=ZZZ |
| `L;YYY;C;ZZZ` then `D;XXX` | Defect row: DefectCode=XXX, Location=YYY, Characteristic=ZZZ |
| `FD;XXX-YYY-ZZZ` | Defect row in one scan: DefectCode=XXX, Characteristic=YYY, Location=ZZZ |

A defect entry is complete when defect code, location, and characteristic have all been provided.

---

## 6. Barcode Commands

| Barcode | Action | Context |
|---|---|---|
| `SC;XXXXXX/L1` or `/L2` | Scan shell — look up assembly | State: WaitingForShell |
| `D;XXX` | Log a defect code | State: AwaitingDefects |
| `L;XXX;C;YYY` | Log a defect location and characteristic | State: AwaitingDefects |
| `FD;XXX-YYY-ZZZ` | Log a full defect (DefectCode-Characteristic-Location) | State: AwaitingDefects |
| `S;1` | Save the inspection record | State: AwaitingDefects |
| `CL;1` | Clear all defect entries | State: AwaitingDefects |

---

## 7. Manual Mode Controls

When External Input is toggled OFF:

| Barcode Equivalent | Manual Control | Description |
|---|---|---|
| `SC;XXXXXX` | Text input + Submit | Type shell serial number |
| `D;XXX` | Defect Code dropdown | Select from applicable defect codes |
| `L;XXX` | Location dropdown | Select from applicable locations |
| `C;YYY` | Characteristic dropdown | Select the round seam position (RS1, RS2, etc.) |
| `FD;` | Not needed | Individual dropdowns replace compound scanning |
| `S;1` | Save button | Save the inspection record |
| `CL;1` | Clear All button | Clear all defect entries |

---

## 8. Validation and Error Handling

| Scenario | Behavior |
|---|---|
| **Valid shell scan (assembly found)** | Green overlay, transition to AwaitingDefects |
| **Shell not in any assembly** | Red overlay — "This shell is not part of any assembly" |
| **Assembly has no Round Seam record** | Red overlay — "This assembly has not been through Round Seam yet" |
| **Invalid defect code for this work center** | Red overlay — "Defect code not applicable at this work center" |
| **Invalid location for this work center** | Red overlay — "Location not applicable at this work center" |
| **Invalid characteristic for this tank size** | Red overlay — "RS3 is not applicable for a {tankSize} assembly" (e.g., RS3 on a 500-gallon tank) |
| **Save with incomplete defect entry** | Warn: "Incomplete defect entry — add missing fields or clear" |
| **Duplicate assembly scan while in AwaitingDefects** | Red overlay — "Save or clear current assembly before scanning a new one" |
| **API failure** | "Failed to save inspection record. Please try again." |

---

## 9. Data Captured

### 9.1 Inspection Record

| Field | Source | Description |
|---|---|---|
| **Assembly Alpha Code** | Looked up via shell → TraceabilityLog | The assembly being inspected |
| **Work Center ID** | Tablet cache | Round Seam Inspection |
| **Operator ID** | Session | The inspector |
| **Timestamp** | Server-generated | When the inspection was saved |
| **Defects** | Defect entries (0 or more) | Array of defect records |

### 9.2 Each Defect Record

| Field | Source | Description |
|---|---|---|
| **Defect Code ID** | `D;XXX` scan or dropdown | References the DefectCode table |
| **Characteristic ID** | `C;YYY` from scan or dropdown | The specific round seam position (RS1, RS2, RS3, RS4) |
| **Location ID** | `L;XXX` from scan or dropdown | References the DefectLocation table |

---

## 10. API Endpoints

| Method | Endpoint | Purpose |
|---|---|---|
| `GET` | `/serial-numbers/{serial}/assembly` | Look up assembly alpha code from shell serial |
| `GET` | `/workcenters/{id}/defect-codes` | Defect codes applicable to this work center |
| `GET` | `/workcenters/{id}/defect-locations` | Defect locations applicable to this work center |
| `GET` | `/workcenters/{id}/characteristics` | Characteristics for this work center (RS1–RS4, filtered by tank size) |
| `POST` | `/inspection-records` | Save the inspection record with defects |
| `GET` | `/workcenters/{id}/history?date={today}&limit=5` | WC History panel |

---

## 11. Key Design Decisions

| Decision | Resolution | Rationale |
|---|---|---|
| **Assembly-level inspection** | Data logged against alpha code, not shell | From Fitup onward, the assembly is the production unit |
| **Explicit characteristic selection** | Inspector must specify which round seam (RS1–RS4) | Multiple round seam positions exist; the system must track exactly which weld has defects |
| **L;XXX;C;YYY combined format** | Location and characteristic in a single scan | Reduces scan count compared to separate L; and C; scans |
| **Characteristic validated by tank size** | RS3 rejected for ≤500 assemblies, RS4 rejected for ≤1000 | Prevents impossible defect entries |
| **Save with no defects = clean pass** | Scan shell → scan Save immediately | Most assemblies pass; clean path is two scans |
| **No override save (O;1)** | Removed from v2 — not used in practice | Simplifies the workflow; if override is needed in the future, it can be added |

---

## References

| Document | Relevance |
|---|---|
| [SPEC_OPERATOR_WC_LAYOUT.md](SPEC_OPERATOR_WC_LAYOUT.md) | Persistent shell, scan overlay, input modes |
| [SPEC_WC_LONG_SEAM_INSPECTION.md](SPEC_WC_LONG_SEAM_INSPECTION.md) | Similar inspection pattern — this spec follows the same structure with key differences noted |
| [SPEC_WC_ROUND_SEAM.md](SPEC_WC_ROUND_SEAM.md) | Upstream station — the welds this inspection is checking |
| [SPEC_WC_FITUP.md](SPEC_WC_FITUP.md) | Creates the assembly and alpha code |
| [MES_V1_BARCODE_LANG.MD](MES_V1_BARCODE_LANG.MD) | Barcode command reference for Round Seam Inspection |
| [MANFACTURING_CONCEPTS.MD](MANFACTURING_CONCEPTS.MD) | Weld position diagram — RS1, RS2, RS3, RS4 positions |
| [GENERAL_DESIGN_INPUT.md](GENERAL_DESIGN_INPUT.md) | Data model — DefectCode, DefectLocation, Characteristic |
