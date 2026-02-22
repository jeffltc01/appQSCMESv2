# Asset Management

Track physical assets (welders, positioners, fixtures) assigned to work centers and production lines. Assets appear as a searchable card grid. There is no delete action — assets can only be deactivated. Site-scoped users see only assets at their site.

## How It Works

1. **Browse or search.** Use the search box to filter by asset name.
2. **Add an asset.** Click **Add Asset**. Fill in the required fields and click **Save**.
3. **Edit an asset.** Click an asset card to open the edit form. Update fields and click **Save**.
4. **Deactivate.** Toggle **Active** off. Deactivated assets stop appearing in selection lists at operator work centers but remain on historical records.

## Fields & Controls


| Element               | Description                                                                  |
| --------------------- | ---------------------------------------------------------------------------- |
| **Search**            | Filters the card grid by asset name.                                         |
| **Asset Name**        | Descriptive name of the asset (e.g., "Welder #3 – Line 1"). Required.        |
| **Work Center**       | The work center this asset is assigned to. Required.                         |
| **Production Line**   | The production line this asset belongs to. Required.                         |
| **Limble Identifier** | Optional. Links this asset to a Limble CMMS record for maintenance tracking. |
| **Lane Name**         | Optional. Physical lane identifier when a work center has parallel paths (e.g., for Spot Xray). |
| **Active**            | Toggle switch. Inactive assets are hidden from operator screens.             |


## Tips

- Assets cannot be deleted because production records reference them for traceability. Use deactivation instead.
- If you use Limble for maintenance management, always fill in the Limble Identifier so maintenance data stays linked.
- Site-scoped users only see assets belonging to their assigned site. An Admin or Director can view all sites.

