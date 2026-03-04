const e=`# Audit Log\r
\r
The Audit Log provides a complete, append-only record of every data change in the system. It captures who changed what, when, and exactly which fields were modified — with before and after values.\r
\r
## Access\r
\r
- **Quality Manager (3.0) and above** can view audit log entries.\r
- Audit log records **cannot be modified or deleted** through the application.\r
\r
## What Gets Logged\r
\r
Every create, update, and delete operation on tracked entities is automatically recorded, including:\r
\r
- **Master data**: products, vendors, users, defect codes, characteristics, control plans, etc.\r
- **Production data**: serial numbers, production records, inspection records, welder logs, etc.\r
- **Quality data**: defect logs, annotations, x-ray records, etc.\r
- **Configuration**: work center settings, production lines, assets, downtime reasons, etc.\r
\r
Sensitive fields (such as PIN hashes) are excluded from logging.\r
\r
## Filters\r
\r
Use the filter bar at the top of the screen to narrow results:\r
\r
| Filter | Description |\r
|---|---|\r
| **Entity** | Filter by entity type (e.g., Vendor, Product, SerialNumber) |\r
| **Action** | Filter by Created, Updated, or Deleted |\r
| **Entity ID** | Paste a specific record's GUID to see all changes to that record |\r
| **From / To** | Date range filter |\r
\r
Click **Search** to apply filters, or **Clear** to reset all filters.\r
\r
## Viewing Changes\r
\r
Each row shows the date, user, action type, entity, and entity ID. Click **View** to expand the row and see the full field-level detail:\r
\r
- **Field**: the property that was changed\r
- **Old Value**: the value before the change (null for newly created records)\r
- **New Value**: the value after the change (null for deleted records)\r
\r
## Pagination\r
\r
Results are paginated (50 per page). Use the Previous/Next buttons to navigate through pages.\r
`;export{e as default};
