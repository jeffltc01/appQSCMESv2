# Log Viewer (Production Logs)

The Log Viewer provides a filterable, read-only view of production log records across five log types. Quality staff and supervisors use it to review historical production data, spot trends, and investigate defects. Result cells are color-coded for quick visual scanning.

**Access:** All roles with admin menu access can view production logs.

## How It Works

1. **Select a log type.** Choose from the five available log types using the tab or dropdown at the top: Rolls, Fitup, Hydro, RT X-ray, or Spot X-ray. Each type displays its own set of columns.
2. **Set filters.** Use the site selector and date range picker to narrow the results. Filters are applied immediately.
3. **Scan the results.** The table loads matching records. Result columns are color-coded — green for accepted, red for rejected — so you can spot failures at a glance.
4. **Check annotations.** Records with annotations display an inline annotation icon. Tap the icon to view the annotation details.
5. **Deep-link.** The selected log type is stored as a URL parameter, so you can bookmark or share a direct link to a specific log view.

## Log Types & Columns

### Rolls

| Column | Description |
|---|---|
| **Date** | Production date. |
| **Coil / Heat / Lot** | Material traceability identifiers. |
| **Thickness** | Measured steel thickness. |
| **Shell Code** | The shell's unique code. |
| **Size** | Tank size in gallons. |
| **Welders** | Welder(s) who worked on the shell. |
| **Annotations** | Inline annotation icon, if any. |

### Fitup

| Column | Description |
|---|---|
| **Date** | Production date. |
| **Head Nos** | Head serial numbers joined at fitup. |
| **Shell Nos** | Shell serial numbers joined at fitup. |
| **Alpha Code** | The assembly's alpha code identifier. |
| **Size** | Tank size in gallons. |
| **Welders** | Welder(s) who performed the fitup. |
| **Annotations** | Inline annotation icon, if any. |

### Hydro

| Column | Description |
|---|---|
| **Date** | Test date. |
| **Nameplate** | The nameplate number of the tank. |
| **Alpha Code** | The assembly's alpha code. |
| **Size** | Tank size in gallons. |
| **Operator** | The operator who ran the test. |
| **Welders** | Welder(s) associated with the tank. |
| **Result** | Pass or Fail (color-coded). |
| **Defects** | Any defects recorded during the test. |
| **Annotations** | Inline annotation icon, if any. |

### RT X-ray

| Column | Description |
|---|---|
| **Date** | Inspection date. |
| **Shell Code** | The shell's unique code. |
| **Size** | Tank size in gallons. |
| **Operator** | The inspector who performed the X-ray. |
| **Result** | Accept or Reject (color-coded). |
| **Defects** | Any defects identified. |
| **Annotations** | Inline annotation icon, if any. |

### Spot X-ray

| Column | Description |
|---|---|
| **Date** | Inspection date. |
| **Tanks** | Number of tanks in the spot lot. |
| **Inspected** | Number of tanks actually inspected. |
| **Size** | Tank size in gallons. |
| **Operator** | The inspector who performed the X-ray. |
| **Result** | Accept or Reject (color-coded). |
| **Shots** | Number of X-ray shots taken. |
| **Annotations** | Inline annotation icon, if any. |

## Fields & Controls

| Element | Description |
|---|---|
| **Log Type selector** | Switches between Rolls, Fitup, Hydro, RT X-ray, and Spot X-ray. |
| **Site selector** | Filters records to a specific plant. |
| **Date Range picker** | Sets the start and end dates for the query. |
| **Result cell** | Color-coded: green background for Accept/Pass, red background for Reject/Fail. |
| **Annotation icon** | Inline icon on rows that have annotations. Tap to view details. |

## Tips

- Use a narrow date range when investigating a specific incident — wide ranges on busy plants can return thousands of rows.
- The color-coded result cells make it easy to scan for failures without reading every row. Look for red cells to find rejects quickly.
- Bookmark a URL with the log type parameter to jump directly to your most-used log view.
- Annotations are a shared record — any annotation you see was created by a supervisor or quality staff member and is visible to all users with access.
