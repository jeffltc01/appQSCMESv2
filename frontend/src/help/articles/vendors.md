# Vendor Maintenance

Manage the list of material vendors (mills, processors, head suppliers) used throughout the MES. Vendors appear as a searchable card grid. Admins (Role 1.0) can add, edit, and deactivate vendors.

## How It Works

1. **Browse or search.** Use the search box to filter vendors by name. Each card shows the vendor name, type, assigned sites, and active status.
2. **Add a vendor.** Click **Add Vendor**. Fill in the required fields and click **Save**.
3. **Edit a vendor.** Click a vendor card to open the edit form. Update fields and click **Save**.
4. **Deactivate a vendor.** Toggle the **Active** switch off. Deactivated vendors no longer appear in selection lists but remain on historical records.

### Site-Scoped Behavior

Site-scoped users can view all vendors but can only toggle their own site on or off in the Sites multi-select. They cannot change the vendor name, type, or other sites.

## Fields & Controls

| Element | Description |
|---|---|
| **Search** | Filters the card grid by vendor name. |
| **Vendor Name** | Display name of the vendor. Required. |
| **Vendor Type** | The category of material the vendor supplies: Mill, Processor, or Head. Required. |
| **Sites** | Multi-select of sites the vendor is associated with. A vendor can supply multiple sites. |
| **Active** | Toggle switch. Inactive vendors are hidden from selection lists. |

## Tips

- A vendor must be assigned to at least one site before it appears in material-entry screens for that site.
- Deactivating a vendor does not affect existing production records that reference it.
- If you need a new vendor type, contact an Admin — the type list is fixed.
