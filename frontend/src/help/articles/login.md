# Login

The Login screen is the first screen you see when opening MES. Every user — from Operators to Administrators — enters through this screen.

## How It Works

1. **Enter your Employee Number.** The field auto-focuses when the screen loads. Your employee number is masked for privacy. After a brief pause, the system looks up your account.
2. **Enter your PIN** (if required). Some accounts are configured to require a PIN. If yours does, a PIN field will appear below the Employee Number field.
3. **Toggle Welder** (optional). If you are a welder and will be welding at this station, toggle the Welder switch to "Yes." This adds you to the active welder list at the work center. Additional welders can be added later from the operator top bar.
4. **Select your Site.** For most users this is locked to your assigned plant. Administrators and Directors can select any plant.
5. **Tap Login.** If your credentials are valid, you are routed to the appropriate screen based on your role.

## Where You Go After Login

| Role | Destination |
|---|---|
| Operators (6.0) | The work center screen cached on this tablet. If no work center is cached, you are redirected to Tablet Setup or shown a message to contact a Team Lead. |
| Team Lead (5.0) and above | The Admin Menu tile grid. |

## Fields & Controls

| Control | Description |
|---|---|
| **Employee No.** | Numeric input, masked with dots. Auto-focuses on page load. |
| **PIN** | Numeric input, masked. Only appears when your account requires it. |
| **Welder toggle** | On/Off switch. Default is Off. Toggle On to register yourself as a welder at this work center for the session. |
| **Site dropdown** | Pre-filled with your default plant. Locked for most roles; selectable for Administrators and Directors. |
| **Login button** | Disabled until your employee number has been validated. |

## Tips

- You can press **Enter** on the keyboard to move from the Employee Number field to the PIN field (if visible), or to submit the form.
- If you get "Login failed," double-check your employee number and PIN. Contact your Team Lead or IT if the problem persists.
- If the server is unreachable, a yellow connectivity error will appear with a Retry option.

## Changes from MES v1

- **Test Mode toggle removed.** MES v2 uses separate environments (dev/test/prod) instead of a data-level Test Mode flag.
- **Debug button removed.** Troubleshooting is now handled through Application Insights and the status area in the operator bottom bar.
- **PIN field is conditional.** It only appears when your account requires it, keeping the screen cleaner for most users.
- **Version number** is displayed as plain text instead of a clickable link.
