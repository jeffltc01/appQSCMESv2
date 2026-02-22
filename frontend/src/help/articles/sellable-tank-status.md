# Sellable Tank Status

Sellable Tank Status provides a daily snapshot of all sellable tanks produced at a site on a given date. Each row shows a tank's serial number alongside gate-check result icons so supervisors and quality staff can quickly identify which tanks have passed, failed, or are still missing inspections.

**Access:** All roles can view. Directors (2.0) and above can switch between sites.

## How It Works

1. **Select a date.** Use the date picker at the top of the screen. The table loads all sellable tanks produced on that date.
2. **Select a site.** If you are a Director or above, the site selector allows you to view tanks from any plant. Site-scoped users see only their own plant.
3. **Review gate checks.** Each row displays colored icons for RT X-ray, Spot X-ray, and Hydro results. Glance across a row to see whether a tank has cleared all gates.
4. **Look up a serial.** The Serial Number column is a clickable link that opens the Serial Number Lookup screen for that tank's full history.

### Gate Check Legend

| Icon | Meaning |
|---|---|
| Green check | Pass |
| Red X | Rejected |
| Gray dash | No Record |

## Fields & Controls

| Element | Description |
|---|---|
| **Date Picker** | Selects the production date to display. Defaults to today. |
| **Site Selector** | Switches the view to another plant. Visible only to Director (2.0) and above. |
| **Serial Number** | The tank's unique serial number. Tap to open Serial Number Lookup. |
| **Product Number** | The product catalog number for the tank. |
| **Tank Size** | The tank capacity in gallons. |
| **RT X-ray** | Gate check result icon for the RT X-ray inspection. |
| **Spot X-ray** | Gate check result icon for the Spot X-ray inspection. |
| **Hydro** | Gate check result icon for the Hydro test. |

## Tips

- URL parameters for site and date are preserved in the address bar, so you can bookmark or share a specific view.
- If a tank shows a gray dash for a gate check, it means no inspection record exists yet — not necessarily that it was skipped.
- Use this screen for end-of-day audits to confirm all tanks have cleared required inspections before shipping.
