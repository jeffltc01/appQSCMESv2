# Defect Codes

Manage the library of defect codes used during inspection and hydro testing. Defect codes appear as a searchable card grid. Directors (Role 2.0) and above can add, edit, and deactivate codes. All other users have read-only access.

## How It Works

1. **Browse or search.** Use the search box to filter by code or name.
2. **Add a defect code.** Click **Add Defect Code**. Fill in the fields and click **Save**.
3. **Edit a defect code.** Click a card to open the edit form. Update any fields and click **Save**.
4. **Assign to work centers.** Use the work center checkboxes to control where this defect code is available. Only work centers with an Inspection or Hydro data entry type appear in the checkbox list.
5. **Deactivate.** Toggle **Active** off. Deactivated codes stop appearing in operator selection lists but remain on historical records.

## Fields & Controls


| Element                    | Description                                                                                                                                          |
| -------------------------- | ---------------------------------------------------------------------------------------------------------------------------------------------------- |
| **Search**                 | Filters the card grid by code or name.                                                                                                               |
| **Code**                   | Short alphanumeric identifier (e.g., "POR", "UC"). Required. This is the field that should be use for the value of a pre-printed barcode scan sheet. |
| **Name**                   | Descriptive name of the defect (e.g., "Porosity", "Undercut"). Required.                                                                             |
| **Severity**               | Optional severity classification for the defect.                                                                                                     |
| **System Type**            | Optional system-level grouping used for reporting.                                                                                                   |
| **Assign to Work Centers** | Checkboxes listing Inspection and Hydro work centers. Check a work center to make this defect code available to operators at that station.           |
| **Active**                 | Toggle switch. Inactive codes are hidden from operator screens.                                                                                      |


## Tips

- Keep codes short and consistent — operators may need to select them quickly during inspection.
- The work center assignment checkboxes only show Inspection and Hydro types. If you don't see a work center, verify its Data Entry Type in Work Center Configuration.
- Deactivating a defect code does not remove it from existing inspection records, it just makes it non selectable for future recorded defects.

