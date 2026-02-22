# User Maintenance

Manage operator and staff accounts across the MES. Users appear as a searchable card grid. Admins (Role 1.0) have full add/edit/deactivate access. Quality Managers (Role 3.0) and above can view the user list but cannot make changes.

## How It Works

1. **Browse or search.** Use the search box to filter users by name or employee number. Cards show the display name, employee number, role, and active status.
2. **Add a user.** Click **Add User**. Fill in the required fields and click **Save**.
3. **Edit a user.** Click a user card to open the edit form. Change any fields and click **Save**.
4. **Deactivate a user.** Toggle the **Active** switch off in the edit form. Deactivated users cannot log in but their history is preserved. There is no hard delete.

### Site-Scoped Behavior

Users whose role is site-scoped see only the users that belong to their site. The role dropdown is restricted to roles at or below their own level, and Default Site is locked to their assigned site.

## Fields & Controls

| Element | Description |
|---|---|
| **Search** | Filters the card grid by name or employee number as you type. |
| **Employee Number** | Unique identifier used for login and barcode scanning. Required. |
| **First Name** | User's first name. Required. |
| **Last Name** | User's last name. Required. |
| **Display Name** | The name shown on screens and reports. Auto-populated from first/last but can be overridden. |
| **User Type** | Standard or Authorized Inspector. Authorized Inspectors can perform gate-check inspections. |
| **Role** | Dropdown of system roles. Determines permissions across the app. Site-scoped users see a restricted list. |
| **Default Site** | The site the user is assigned to. Site-scoped users are locked to their own site. |
| **Certified Welder** | Checkbox. When checked, this user appears in welder selection lists at welding work centers. |
| **Require PIN** | Checkbox. When enabled, the PIN input field appears and the user must enter a PIN at login. |
| **PIN** | Numeric PIN for login. Only visible when Require PIN is checked. |
| **Active** | Toggle switch. Inactive users cannot log in. |

## Tips

- Employee numbers cannot be changed after creation. Double-check before saving a new user.
- Deactivating a user is immediate — they will be signed out at the next heartbeat check.
- If a site-scoped user needs to manage users at another site, a Director or Admin must update their role or site assignment.
