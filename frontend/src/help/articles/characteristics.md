# Characteristics

Configure the measurable characteristics used during inspection (e.g., wall thickness, ovality, diameter). Characteristics appear as a card grid. You can edit existing characteristics but cannot add or delete them — the set of characteristics is fixed. Directors (Role 2.0) and above can edit.

## How It Works

1. **Browse or search.** Use the search box to filter by name.
2. **Edit a characteristic.** Click a card to open the edit form. Adjust specification limits, target, product type, or work center assignments and click **Save**.
3. **Assign to work centers.** Use the work center checkboxes to control where this characteristic is collected. Only work centers with an Inspection data entry type appear in the list.

## Fields & Controls

| Element | Description |
|---|---|
| **Search** | Filters the card grid by characteristic name. |
| **Name** | The name of the characteristic (e.g., "Wall Thickness"). Read-only. |
| **Spec Low** | The lower specification limit. Measurements below this value fail. |
| **Spec High** | The upper specification limit. Measurements above this value fail. |
| **Spec Target** | The ideal target value for this measurement. |
| **Product Type** | The product type this characteristic applies to. |
| **Assign to Work Centers** | Checkboxes listing Inspection-type work centers. Check a work center to make this characteristic available to inspectors at that station. |

## Tips

- Spec limits drive pass/fail logic at inspection. Verify limits against engineering drawings before changing them.
- If a characteristic is not appearing at an inspection work center, check that the work center is checked in the Assign to Work Centers list.
- Characteristics cannot be added or deleted from this screen. If a new characteristic is needed, it must be configured at the system level.
