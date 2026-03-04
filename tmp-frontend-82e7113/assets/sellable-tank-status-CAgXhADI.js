const e=`# Sellable Tank Status

Sellable Tank Status provides a daily snapshot of sellable tanks by site and date, including gate-check indicators for RT X-ray, Spot X-ray, and Hydro.

**Access:** All roles can view. Directors (2.0) and above can switch between sites.

## How It Works

1. **Select site/date.** Directors and above can choose site; all users can set date.
2. **Run search.** Click **Search** to load the selected snapshot.
3. **Review gate icons.** Each row shows pass/reject/no-record status for RT X-ray, Spot X-ray, and Hydro.
4. **Open full trace.** Click a serial link to open Serial Number Lookup for that unit.

### Gate Check Legend

| Icon | Meaning |
|---|---|
| Green check | Pass |
| Red X | Rejected |
| Gray dash | No Record |

## Fields & Controls

| Element | Description |
|---|---|
| **Site Selector** | Select site context (visible to Director 2.0 and above). |
| **Date Picker** | Select production date. |
| **Search button** | Loads table data for current site/date selection. |
| **Serial Number** | The tank's unique serial number. Tap to open Serial Number Lookup. |
| **Secondary trace text** | Optional alpha/shell trace text shown under serial when available. |
| **Product Number** | The product catalog number for the tank. |
| **Tank Size** | The tank capacity in gallons. |
| **RT X-ray** | Gate check result icon for the RT X-ray inspection. |
| **Spot X-ray** | Gate check result icon for the Spot X-ray inspection. |
| **Hydro** | Gate check result icon for the Hydro test. |

## Tips

- URL parameters for site/date are preserved, so filtered views can be bookmarked/shared.
- Only finished serial numbers that have an attached assembly (hydro marriage traceability) are included in results.
- A gray dash means no qualifying gate record was found for that checkpoint.
- Use serial links to pivot into full genealogy when a gate result looks unexpected.
`;export{e as default};
