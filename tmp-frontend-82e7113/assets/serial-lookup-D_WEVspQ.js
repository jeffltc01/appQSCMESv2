const e=`# Serial Number Lookup

Search for any serial number to view production genealogy and manufacturing events across shell, assembly, and sellable history. This screen is used for traceability troubleshooting and quality investigations.

## How It Works

1. **Enter serial and run lookup.** Type a serial number and press Enter or click **Go**. You can also deep-link with \`?serial=...\`.
2. **Review production genealogy.** The horizontal genealogy view shows linked node types such as sellable, assembled, shell, plate, heads, valve, and nameplate.
3. **Check gate indicators.** Main cards show gate status icons where applicable: pass, fail, or no record.
4. **Inspect event history.** The events panel lists manufacturing/inspection events in reverse chronological order with timestamp, operator, asset, and inspection result details.

### Production Genealogy Diagram

The genealogy diagram shows connected cards representing the trace chain. Node types include:

- **Sellable** — finished product identity.
- **Assembled** — fitup-level assembly identity.
- **Shell** — shell-level identity.
- **Plate / Heads / Valves** — traced components.
- **Nameplate** — final nameplate-linked identity when available.

### Manufacturing Events Timeline

Each event row includes:

| Element | Description |
|---|---|
| **Work Center** | The work center where the event occurred. |
| **Event Type** | The type of manufacturing event (e.g., "Roll", "Weld", "Inspect", "Hydro Test"). |
| **Timestamp** | When the event was recorded (localized for display). |
| **Operator** | The operator who performed the event. |
| **Asset** | The asset (equipment) used, if applicable. |
| **Inspection Result** | Pass/fail-style result when present. |

## Fields & Controls

| Element | Description |
|---|---|
| **Serial Number input** | Entry field for the serial to trace. |
| **Go button** | Executes the lookup for the entered serial. |
| **Genealogy panel** | Visual trace chain across parent/child component relationships. |
| **Diagram key + gate key** | Legend for node types and gate icons. |
| **Manufacturing Events panel** | Reverse-chronological event list from all nodes in the trace tree. |

## Tips

- Use deep links (\`?serial=ABC123\`) to jump directly to a known unit.
- If you opened this screen from Sellable Tank Status, the Back action returns to that prior context.
- Gray "No Record" means no qualifying gate event was found for that stage.
`;export{e as default};
