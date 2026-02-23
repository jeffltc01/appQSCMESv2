# Audit Log

The Audit Log provides a complete, append-only record of every data change in the system. It captures who changed what, when, and exactly which fields were modified — with before and after values.

## Access

- **Quality Manager (3.0) and above** can view audit log entries.
- Audit log records **cannot be modified or deleted** through the application.

## What Gets Logged

Every create, update, and delete operation on tracked entities is automatically recorded, including:

- **Master data**: products, vendors, users, defect codes, characteristics, control plans, etc.
- **Production data**: serial numbers, production records, inspection records, welder logs, etc.
- **Quality data**: defect logs, annotations, x-ray records, etc.
- **Configuration**: work center settings, production lines, assets, downtime reasons, etc.

Sensitive fields (such as PIN hashes) are excluded from logging.

## Filters

Use the filter bar at the top of the screen to narrow results:

| Filter | Description |
|---|---|
| **Entity** | Filter by entity type (e.g., Vendor, Product, SerialNumber) |
| **Action** | Filter by Created, Updated, or Deleted |
| **Entity ID** | Paste a specific record's GUID to see all changes to that record |
| **From / To** | Date range filter |

Click **Search** to apply filters, or **Clear** to reset all filters.

## Viewing Changes

Each row shows the date, user, action type, entity, and entity ID. Click **View** to expand the row and see the full field-level detail:

- **Field**: the property that was changed
- **Old Value**: the value before the change (null for newly created records)
- **New Value**: the value after the change (null for deleted records)

## Pagination

Results are paginated (50 per page). Use the Previous/Next buttons to navigate through pages.
