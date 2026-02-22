# Product Maintenance

Product Maintenance lets administrators manage the catalog of tank products manufactured by QSC. Each product represents a specific combination of tank size, tank type, and product type.

**Access:** Administrator (1.0) can add, edit, and deactivate products. Quality Manager (3.0) and above can view the list.

## How It Works

1. **Browse products.** Products are displayed as cards sorted by tank size. Use the Product Type filter dropdown at the top to narrow the list.
2. **Add a product.** Tap the "Add Product" button in the top bar. Fill in the required fields and tap Add.
3. **Edit a product.** Tap the pencil icon on a product card. Update the fields and tap Save.
4. **Deactivate a product.** Tap the trash icon on a product card and confirm. The product is marked Inactive (not permanently deleted) and its card is dimmed.

## Fields & Controls

| Field | Required | Description |
|---|---|---|
| **Product Number** | Yes | A unique identifier for the product (e.g., "P-120-STD"). |
| **Tank Size** | Yes | The tank capacity in gallons (numeric). |
| **Tank Type** | Yes | The type of tank (e.g., "Standard," "LP"). |
| **Product Type** | Yes | A category grouping products (selected from a dropdown of defined types). |
| **Sage Item Number** | No | The item number in the Sage ERP system, for cross-reference. |
| **Nameplate Number** | No | The number used on the nameplate/data plate for this product. |
| **Sites** | No | Which plants manufacture this product. Select one or more sites from the multi-select dropdown. |
| **Active** | Edit only | Checkbox to toggle the product's active status. Inactive products are dimmed in the list. |

## Tips

- Only Administrators (role 1.0) see the Add button and the edit/delete actions on each card. Other roles see the list in read-only mode.
- Deactivating a product does not delete it from the database — historical production records that reference it remain intact.
- If a product is not appearing at a work center, check that the correct Site is assigned to it.
