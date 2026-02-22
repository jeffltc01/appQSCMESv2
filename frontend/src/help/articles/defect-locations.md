# Defect Locations

Manage the list of defect locations used during inspection to describe where on a tank a defect was found. Defect locations appear as a searchable card grid. Directors (Role 2.0) and above can edit locations.

## How It Works

1. **Browse or search.** Use the search box to filter by code or name.
2. **Add a defect location.** Click **Add Defect Location**. Fill in the fields and click **Save**.
3. **Edit a defect location.** Click a card to open the edit form. Update fields and click **Save**.
4. **Deactivate.** Toggle **Active** off. Deactivated locations stop appearing in selection lists but remain on historical records.

## Fields & Controls


| Element                     | Description                                                                                                                                                                        |
| --------------------------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| **Search**                  | Filters the card grid by code or name.                                                                                                                                             |
| **Code**                    | Short alphanumeric identifier (e.g., "LS", "CS"). Required. This is the field that should be use for the value of a pre-printed barcode scan sheet.                                |
| **Name**                    | Descriptive name of the location (e.g., "Long Seam", "Circ Seam"). Required.                                                                                                       |
| **Default Location Detail** | Optional pre-filled detail text that accompanies this location (e.g., "Top", "Bottom").                                                                                            |
| **Characteristic**          | Optional dropdown linking this location to a measurement characteristic. When set, selecting this location at inspection may prompt for the associated characteristic measurement. |
| **Active**                  | Toggle switch. Inactive locations are hidden from operator screens.                                                                                                                |


## Tips

- Location codes are used in inspection records and reports, so keep them stable once in production use.
- The Characteristic link is optional but powerful — it lets inspectors capture a measurement automatically when they select this defect location.
- If a characteristic is not available in the dropdown, configure it first on the Characteristics screen.

