# Serial Number Lookup

Search for any serial number to view its complete production genealogy and manufacturing event history. This screen provides deep traceability from raw material through finished product. Supports deep-linking via URL query parameter for integration with other systems.

## How It Works

1. **Search.** Enter a serial number in the search field and press Enter or click **Search**. The serial number can also be provided via URL query parameter for direct linking (e.g., `?serial=ABC123`).
2. **View Production Genealogy.** A horizontal flow diagram displays the relationships between nodes: sellable tank, assembled components, shell, plates, heads, and valves. Click a node for details.
3. **View Manufacturing Events.** Below the genealogy, a timeline lists every manufacturing event for the serial number in chronological order.
4. **Check gate status.** Gate check results are color-coded: green for Pass, red for Fail, and gray for No Record.

### Production Genealogy Diagram

The genealogy diagram shows a horizontal flow of connected nodes representing the serial number's production lineage. Node types include:

- **Sellable** — the finished, sellable tank.
- **Assembled** — the assembled tank before final processing.
- **Shell** — the rolled shell with associated plate and heat/coil data.
- **Heads** — the formed heads with vendor and lot information.
- **Valves** — any valves installed on the assembly.

### Manufacturing Events Timeline

Each event in the timeline shows:

| Element | Description |
|---|---|
| **Work Center** | The work center where the event occurred. |
| **Event Type** | The type of manufacturing event (e.g., "Roll", "Weld", "Inspect", "Hydro Test"). |
| **Timestamp** | When the event was recorded. |
| **Operator** | The operator who performed the event. |
| **Asset** | The asset (equipment) used, if applicable. |
| **Inspection Result** | For inspection events, the pass/fail result with color-coded gate check status. |

## Fields & Controls

| Element | Description |
|---|---|
| **Search** | Text input for the serial number. Press Enter or click Search to load. |
| **Genealogy Diagram** | Horizontal flow visualization of sellable → assembled → shell/heads/valves with plate and material details. |
| **Node Detail** | Click a node in the genealogy to view its details (serial, material, vendor, heat/coil, dates). |
| **Events Timeline** | Chronological list of all manufacturing events for the serial number. |
| **Gate Check Status** | Color-coded indicator on inspection events: green (Pass), red (Fail), gray (No Record). |

## Tips

- Use the URL query parameter (`?serial=ABC123`) to link directly to a serial lookup from other screens, reports, or external systems.
- The genealogy diagram is the fastest way to trace a quality issue back to the source material, vendor, or specific production step.
- Gray "No Record" gate check statuses mean the serial did not pass through that gate check — this may be expected for certain product types or may indicate a missed step.
