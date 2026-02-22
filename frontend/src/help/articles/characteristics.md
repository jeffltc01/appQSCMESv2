# Characteristics

Configure the measurable characteristics used during inspection (e.g., wall thickness, diameter, etc.). Characteristics appear as a card grid. Admins (Role 1.0) can add new characteristics, edit existing ones, and deactivate ones that are no longer needed.

## How It Works

1. **Browse or search.** Use the search box to filter by name.
2. **Edit a characteristic.** Click a card to open the edit form. Adjust specification limits, target, product type, or work center assignments and click **Save**.
3. **Assign to work centers.** Use the work center checkboxes to control where this characteristic is collected. Only work centers with an Inspection data entry type appear in the list.

## Fields & Controls


| Element                    | Description                                                                                                                                      |
| -------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------ |
| **Search**                 | Filters the card grid by characteristic name.                                                                                                    |
| **Code**                   | A short identifier for the characteristic (e.g., "001"). This is the field that should be use for the value of a pre-printed barcode scan sheet. |
| **Name**                   | The name of the characteristic (e.g., "Wall Thickness").                                                                                         |
| **Min Tank Size**          | The minimum tank size (in gallons) this characteristic applies to. Optional.                                                                     |
| **Spec Low**               | The lower specification limit. Measurements below this value fail.                                                                               |
| **Spec High**              | The upper specification limit. Measurements above this value fail.                                                                               |
| **Spec Target**            | The ideal target value for this measurement.                                                                                                     |
| **Product Type**           | The product type this characteristic applies to.                                                                                                 |
| **Assign to Work Centers** | Checkboxes listing Inspection-type work centers. Check a work center to make this characteristic available to inspectors at that station.        |


## Tips

- In the future, Spec limits may help drive pass/fail logic at inspection. Verify limits against engineering drawings before changing them.
- If a characteristic is not appearing at an inspection work center, check that the work center is checked in the Assign to Work Centers list.
- Deactivated characteristics are dimmed and marked with an "Inactive" badge. They are no longer available for selection at inspection stations.

