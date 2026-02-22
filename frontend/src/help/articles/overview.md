# MES v2 Overview

MES v2 is the Manufacturing Execution System used at Quality Steel Corporation (QSC) to track production activity across all three manufacturing plants. It replaces the PowerApp-based MES v1 with a modern web application designed for Samsung tablets mounted at each work center on the plant floor.

## Who Uses MES

| Role | Tier | Typical User |
|---|---|---|
| Administrator | 1.0 | IT staff — full access to every feature |
| Quality Director / Operations Director | 2.0 | Company-wide directors — access across all plants |
| Quality Manager / Plant Manager | 3.0 | One per plant — manages quality programs and plant operations |
| Supervisor | 4.0 | Reports to Plant Manager — oversees operators |
| Quality Tech / Team Lead | 5.0 | Hands-on quality and floor leadership |
| Authorized Inspector | 5.5 | External ASME inspector — limited read access |
| Operator | 6.0 | Runs a work center on the plant floor |

## Navigation

After logging in, the app routes you based on your role:

- **Operators** go directly to their assigned work center screen. The tablet remembers which work center it is configured for.
- **Team Leads and above** land on the Admin Menu, a tile-based grid organized into five groups: Master Data, Quality & Inspection, Production & Operations, Dashboards & Insights, and People & Administration. Only tiles your role has access to are visible.

## Key Concepts

- **Work Center** — A station on the plant floor where a specific manufacturing step happens (e.g., Rolls, Long Seam, Fitup, Hydro).
- **Production Line** — A named line within a plant. A tablet is assigned to one work center on one production line.
- **Asset** — A specific piece of equipment at a work center (e.g., "Longseam A" vs "Longseam B"). Only some work centers have multiple assets.
- **Shell** — The cylindrical steel body of a propane tank, identified by a serial number.
- **Assembly** — A shell joined with heads at the Fitup station, tracked by an alpha code.
- **Barcode Scanning** — Most operator screens are driven by USB barcode scanners. When External Input mode is toggled on, the scanner captures all input and the touchscreen is locked.

## Getting Help

Tap the **?** button in the top bar of any screen to open this help system. Use the table of contents on the left to browse articles by category, or the help will automatically show the article for the screen you are on.

A downloadable PDF version of this manual is available from the help dialog.
