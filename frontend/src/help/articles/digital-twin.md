# Digital Twin

The Digital Twin is a live, read-only visualization of a production line. It displays a pipeline diagram of stations with real-time status coloring, work-in-progress counts, and gate-check icons. KPI cards and a station detail table provide additional context for supervisors monitoring line performance. Data auto-refreshes every 30 seconds.

**Access:** All roles with admin menu access. This is a read-only monitoring screen.

## How It Works

1. **Select a plant and production line.** Use the Plant and Production Line selectors at the top of the screen.
2. **View the pipeline diagram.** Each station in the production line is represented as a node in the pipeline. The node's color indicates its current status:

| Color | Status | Meaning |
|---|---|---|
| Green | Active | Station is producing normally. |
| Yellow | Slow | Station is producing below target pace. |
| Gray | Idle | No activity detected. |
| Red | Down | Station has reported downtime. |
| Orange | Bottleneck | Station is the current throughput constraint. |

3. **Check WIP counts.** Each station node displays the number of units currently in progress at that station.
4. **Check gate-check icons.** Stations that are gate-check points (Hydro, RT X-ray, Spot X-ray) display pass/fail icons for the most recent inspections.
5. **Review KPI cards.** Three summary cards are displayed:

| Card | Description |
|---|---|
| **Line Throughput** | Total units completed at Hydro today, with a delta vs. yesterday. |
| **Avg Cycle Time** | The average end-to-end time from Rolls to Hydro for units completed today. |
| **Line Efficiency** | Hydro output as a percentage of the theoretical maximum (target rate x hours elapsed). |

6. **Review the station detail table.** Below the pipeline, a table provides per-station metrics.

| Column | Description |
|---|---|
| **Station** | The work center name. |
| **Status** | Current status (Active, Slow, Idle, Down, Bottleneck). |
| **Operator** | The operator with the most scans at this station today. |
| **Units Today** | Distinct serial numbers processed at this station today. |
| **Avg Cycle Time** | Average time between consecutive scans at this station today. |
| **FPY%** | First Pass Yield percentage at this station (reserved for future use). |

7. **View the unit tracker.** A grid at the bottom shows individual serial numbers and which station each unit is currently at, giving a real-time map of WIP across the line. Up to 15 of the most recent units are shown. Assemblies (Fitup and beyond) are visually distinguished.

## Data Sources

All data on the Digital Twin comes from the MES database, scoped to the selected production line. The snapshot is recalculated from scratch on every 30-second refresh.

| Data | Source Table(s) | Scope |
|---|---|---|
| Production activity | **ProductionRecords** joined with SerialNumbers, Operators, Products | Selected production line, last 3 days (for WIP) or current day (for KPIs) |
| Station list | **WorkCenters** | All work centers with a recognized DataEntryType |
| Downtime events | **DowntimeEvents** joined with WorkCenterProductionLines | Open (unended) events on the selected production line |
| Assembly consumption | **TraceabilityLogs** | ShellToAssembly relationships — used to exclude consumed shells from WIP |
| Material queues | **MaterialQueueItems** | Queued or active items for Rolls Material and Heads Queue work centers |

### Production Sequence

The pipeline always displays stations in this fixed order, regardless of how work centers are named:

| Sequence | Station | DataEntryType |
|---|---|---|
| 1 | Rolls | Rolls |
| 2 | Long Seam | Barcode-LongSeam |
| 3 | LS Inspect | Barcode-LongSeamInsp |
| 4 | RT X-ray | MatQueue-Shell |
| 5 | Fitup | Fitup |
| 6 | Round Seam | Barcode-RoundSeam |
| 7 | RS Inspect | Barcode-RoundSeamInsp |
| 8 | Spot X-ray | Spot |
| 9 | Hydro | Hydro |

## Calculations

### Station Status

Each station's status is determined by a set of rules evaluated in this order:

1. **Down** — The station has an open (unended) downtime event on this production line.
2. **Active** — The most recent production record at this station was within the last **15 minutes**.
3. **Slow** — The most recent production record was within the last **60 minutes** but more than 15 minutes ago.
4. **Idle** — No production record in the last 60 minutes, or no records at all.

### Bottleneck Detection

Among all stations currently marked Active or Slow, the station with the **highest WIP count** is flagged as the bottleneck. If no station is Active or Slow, or all WIP counts are zero, no bottleneck is shown.

### WIP Count (Work in Progress)

WIP is calculated by looking at all production records from the last 3 days on this production line. For each unique serial number, the system finds its most recent station. Shells that have been consumed into an assembly (tracked via TraceabilityLogs) are excluded — only the assembly identifier continues to count as WIP at downstream stations. The per-station count of these "latest station" records is the WIP number shown on each node.

### Line Throughput

- **Units Today** = Number of production records at the **Hydro** station today (Hydro is the final station, so completing Hydro means the tank is finished).
- **Delta vs. Yesterday** = Today's Hydro count minus yesterday's full-day Hydro count.
- **Units per Hour** = Today's Hydro count divided by the number of hours elapsed since the start of the current day (in the plant's local time zone).

### Average Cycle Time (End-to-End)

For each serial number that completed Hydro today, the system finds the **earliest Rolls timestamp** and the **latest Hydro timestamp** for that serial. The difference is the end-to-end cycle time for that unit. The KPI card shows the average across all such units. If no units have completed both Rolls and Hydro today, the value shows as "-".

### Average Cycle Time (Per Station)

For each station, all of today's production record timestamps are sorted chronologically. The average gap between consecutive timestamps is the per-station cycle time shown in the detail table. This represents the average time between scans — essentially how frequently units are being processed. A station needs at least 2 records today for this to calculate; otherwise it shows "-".

### Line Efficiency

Line efficiency uses a hardcoded target rate of **6 units per hour**. The calculation is:

**Efficiency = (Hydro units today) / (6 x hours elapsed today) x 100**

The result is capped at 100%. If no time has elapsed (start of day), efficiency shows as 0%.

### Current Operator

For each station, the operator who has the **most production records at that station today** is shown. This is the most active operator, not necessarily the one currently logged in.

### Material Feeds

The material feed arrows below Rolls and Fitup show the number of **queued or active** MaterialQueueItems:
- **Rolls Material** — Plate lots queued for the Rolls work center.
- **Heads Queue** — Head lots queued for the Fitup work center.

### Unit Tracker

The tracker shows today's production records, grouped by serial number, with each unit placed at its most recent station. Shells consumed into assemblies are excluded (the assembly takes over). The grid is limited to the **15 most recent** units and sorted by most recent activity.

Units at Fitup or later in the sequence (sequence >= 5) are flagged as assemblies and visually distinguished in the tracker.

## Fields & Controls

| Element | Description |
|---|---|
| **Plant selector** | Chooses which plant to view. |
| **Production Line selector** | Chooses which production line within the plant to display. |
| **Pipeline diagram** | Visual representation of the line with color-coded station nodes. |
| **WIP count** | Number displayed on each station node showing units in progress. |
| **Gate-check icons** | Checkmark on gate-check station nodes (RT X-ray, Spot X-ray, Hydro). |
| **Material feed arrows** | Below Rolls and Fitup, showing queued lot counts from Rolls Material and Heads Queue. |
| **KPI cards** | Line Throughput, Avg Cycle Time, Line Efficiency. |
| **Station detail table** | Per-station breakdown of status, operator, units, cycle time, and FPY. |
| **Unit tracker grid** | Serial-number-level view of where each unit is on the line (up to 15 units). |
| **Last updated indicator** | Timestamp in the toolbar showing when data was last refreshed. |

## Tips

- Use the pipeline diagram's color coding as a quick visual health check — if you see red or orange, that station needs attention.
- The unit tracker grid is especially useful for locating a specific serial number on the line without walking the floor.
- Data refreshes every 30 seconds automatically. There is no manual refresh button since the screen is designed for passive monitoring on a wall-mounted display or supervisor tablet.
- If all stations show Idle (gray), production may not have started for the day or no records have been created yet.
- The "vs. yesterday" delta on Line Throughput compares today's in-progress count against yesterday's full-day total, so it will naturally be negative until production catches up later in the shift.
- Line Efficiency uses a fixed target of 6 units/hour. This is a system-wide default and is not currently configurable per plant or line.
- The bottleneck indicator identifies where WIP is accumulating, which often points to a slower station or a station that needs support.
