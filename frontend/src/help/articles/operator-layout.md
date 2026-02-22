# Operator Layout

The operator layout is the persistent shell that wraps every work center screen. It provides consistent navigation, context information, and input mode controls across all stations. Operators (6.0) see this as their only view after login. Team Leads and above see it when they enter a work center from the Admin Menu.

## How It Works

Once a tablet is configured via Tablet Setup and an operator logs in, the layout loads automatically with the assigned work center content in the center.

### Top Bar

Displays at-a-glance context across the top of the screen.

| Element | Description |
|---|---|
| **Work Center Name** | The display name of the assigned work center, production line, and asset (e.g., "Long Seam · Main Line · Longseam A"). |
| **Operator Name** | The currently logged-in operator's display name. |
| **Welder(s)** | List of active welders at this station. Welders can be added (by employee number) or removed. An operator who logged in with the Welder toggle on is auto-added. |

### Welder Gate

Work centers that require welders display a blocking dialog ("Welder Sign-In Required") if fewer welders are signed in than the work center's minimum. The dialog cannot be dismissed — add enough welders to proceed, or tap Cancel to return to Tablet Setup.

### Left Panel

A vertical strip of icon buttons for common actions.

| Icon | Action |
|---|---|
| **Current Gear** | Displays the plant's current production gear level (text only, no action). |
| **Maintenance Request** | Opens a form to create a maintenance work order (integrated with Limble CMMS). |
| **Tablet Setup** | Reopens the Tablet Setup screen to reassign the tablet. |
| **Schedule** | Shows the current production schedule for the plant or work center. |
| **Settings / Menu** | Opens additional options — user settings, logs, serial number review. |
| **Logout** | Logs out and returns to the Login screen. |

### Bottom Bar

Anchored to the bottom of the screen.

| Element | Description |
|---|---|
| **Plant Code & Clock** | Plant code followed by a live date/time that updates every second. |
| **External Input Toggle** | Switches between barcode scanning mode (On) and manual touch mode (Off). |
| **Online Status** | Green dot when connected to the server; red dot with "Offline" when disconnected. |

### External Input Mode

When the toggle is **On**:
- A hidden text input captures barcode scanner output.
- Touch is locked — tapping the screen does nothing except dismiss the scan overlay.
- Only the External Input toggle itself remains tappable (so you can turn it off).

When the toggle is **Off**:
- All on-screen buttons, dropdowns, and text inputs are active.
- The barcode scanner hidden input is removed.

### WC History Panel

Displayed on the right side of the screen.

| Element | Description |
|---|---|
| **Day Count** | Large number showing total transactions at this work center today. |
| **Last 5 Records** | Compact list of recent production records with timestamp, shell/identifier, tank size, and an annotation flag. Tap the flag to create or view an annotation. |

### Scan Overlay

Every barcode scan triggers a full-screen overlay for immediate visual feedback:

| Result | Color | Icon |
|---|---|---|
| **Success** | Green (`#28a745`) | Large checkmark |
| **Error** | Red (`#dc3545`) | Large X |

The overlay auto-dismisses after about 1.5–2 seconds or can be tapped to dismiss immediately.

## Tips

- If External Input is on and you need to interact with the screen by touch, flip the toggle off first.
- The WC History panel refreshes automatically after each new production record — no manual refresh needed.
- Annotation flags on history rows are tappable. Use them to note issues like material lot corrections or quality holds.
- Operators are locked into their assigned work center (kiosk mode). The only way out is to log out.
- Team Leads and Supervisors have a back/menu button to return to the Admin Menu.
