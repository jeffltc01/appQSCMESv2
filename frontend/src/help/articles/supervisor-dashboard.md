# Supervisor Dashboard

The Supervisor Dashboard is a comprehensive real-time view of production performance for a selected work center. It combines OEE metrics, KPI cards, hourly/daily/weekly performance tables, and an annotation workflow into a single screen. Data auto-refreshes every 30 seconds.

**Access:** Supervisors (4.0) and above.

## How It Works

### Monitoring Performance

1. **Select a work center.** Use the work center dropdown to choose which station to monitor.
2. **Choose a view mode.** Switch between Day, Week, and Month views using the view mode selector. Each view adjusts the performance table granularity.
3. **Review OEE cards.** Four cards display Overall OEE, Availability, Performance, and Quality. Each card is color-coded against configurable thresholds (green = on target, yellow = warning, red = below target).
4. **Review KPI cards.** Additional cards show Count, First Pass Yield (FPY), Defects, Average Time Between Scans, and Quantity per Hour.
5. **Drill into performance tables.** The table below the cards shows time-bucketed rows (hourly in Day view, daily in Week view, weekly in Month view). Each row displays Planned, Actual, Delta, FPY%, and Downtime.
6. **Filter by operator.** Use the operator chip filter to show performance data for a specific operator or group of operators.

### Annotating Records

1. **Switch to Annotate mode.** Select "Annotate" from the view mode selector.
2. **Select records.** Check the boxes next to the production records you want to annotate.
3. **Create a batch annotation.** Choose an annotation type — Note, Internal Review, or Correction Needed — and enter your comment.
4. **Submit.** The annotation is attached to all selected records. Duplicate annotations (same record + same type) are skipped automatically.

## Data Sources

All dashboard data comes from the MES database, scoped to the selected work center and plant. KPI metrics and the performance table are recalculated on every 30-second refresh.

| Data | Source Table(s) | Scope |
|---|---|---|
| Production counts & timestamps | **ProductionRecords** | Current day and current week (Mon-Sun), filtered to selected WC + plant |
| Shift schedule (planned time) | **ShiftSchedules** | Most recent schedule with an effective date on or before the current date |
| Capacity targets (planned output) | **WorkCenterCapacityTargets** | Targets for this WC's production lines, filtered by current plant gear |
| Downtime | **DowntimeEvents** | Events overlapping the time window for this WC's production lines |
| Defects (for FPY) | **DefectLogs** | Defect entries linked to serial numbers at this WC |
| Plant gear level | **ProductionRecords.PlantGearId** | Most recent record's gear ID is used to select the correct capacity targets |
| Operators | **ProductionRecords → Users** | Operators with records at this WC in the current week |
| Annotations | **Annotations → AnnotationTypes** | Existing annotations linked to production records |

## Calculations

### OEE (Overall Equipment Effectiveness)

OEE is calculated by the OEE Service using the standard formula **OEE = Availability x Performance x Quality**. OEE is always calculated for the current day and is not filtered by operator.

#### Availability

**Availability = Run Time / Planned Time x 100**

- **Planned Time** (minutes) comes from the **Shift Schedule**. The system finds the most recent schedule with an effective date on or before today, then reads the hours and break minutes for today's day of week. Planned Minutes = (Shift Hours x 60) - Break Minutes.
- **Downtime** (minutes) is the sum of all `DowntimeEvent.DurationMinutes` for this work center's production lines that overlap the current day.
- **Run Time** = Planned Time - Downtime (floored at 0).

| Threshold | Color |
|---|---|
| >= 90% | Green |
| >= 70% | Yellow |
| < 70% | Red |

#### Performance

**Performance = Sum of Ideal Cycle Times / Run Time x 100** (capped at 200%)

For each production record created today:
1. The system looks up the matching **Capacity Target** based on the record's work center production line + plant gear ID + tank size. If no exact tank-size match exists, it falls back to the default target (null tank size) for that gear and production line.
2. **Ideal Cycle Time** for that record = 60 / Target Units Per Hour (in minutes).
3. The sum of all ideal cycle times is divided by the actual run time to get the performance ratio.

If the work center has multiple production lines, the capacity targets are resolved per-line. The target used for the Planned column is the sum of averages across lines.

| Threshold | Color |
|---|---|
| >= 95% | Green |
| >= 70% | Yellow |
| < 70% | Red |

#### Quality

**Quality = Day First Pass Yield (FPY)**

Quality uses the same FPY calculation described below. If FPY is not available for this work center type, Quality defaults to 100%.

| Threshold | Color |
|---|---|
| >= 99% | Green |
| >= 95% | Yellow |
| < 95% | Red |

#### Overall OEE

**Overall OEE = (Availability / 100) x (Performance / 100) x (Quality / 100) x 100**

| Threshold | Color |
|---|---|
| >= 85% | Green |
| >= 60% | Yellow |
| < 60% | Red |

### KPI Cards

#### Count (Day / Week)

Simple count of production records at this work center for the current day (midnight to midnight in the plant's local time zone) and the current Monday-through-Sunday week. If an operator filter is active, only that operator's records are counted.

#### First Pass Yield (FPY)

FPY measures the percentage of units that passed inspection without any defects on their first visit to this work center. It is only available for work centers that have an applicable inspection characteristic:

| Work Center DataEntryType | Applicable Characteristics |
|---|---|
| Rolls, Barcode-LongSeam | Long Seam characteristic |
| Fitup, Barcode-RoundSeam | Round Seam characteristics (RS1-RS4) |
| All others | FPY not shown |

The calculation:

1. **Find first-pass serials.** Identify all serial numbers whose earliest production record at this work center falls within the time window.
2. **Exclude rework.** Remove any serials that had an earlier record at this work center before the window started (they are being re-processed, not first-pass).
3. **Count defects.** Query DefectLogs for those first-pass serials, scoped to the applicable characteristics and the same time window. Count the number of distinct serials that have at least one defect.
4. **FPY = (Opportunities - Serials with Defects) / Opportunities x 100**, where Opportunities = number of first-pass serials.

FPY is shown as Day and Week values. Color-coded: green at >= 95%, red below.

#### Total Defects

The total count of individual DefectLog entries (not distinct serials) for the same first-pass serials and applicable characteristics described in the FPY calculation. Shown as Day and Week values.

#### Avg Time Between Scans

All production record timestamps for the period are sorted chronologically. The average gap between consecutive timestamps is computed in seconds. This indicates the pace of production — a lower number means faster throughput.

**Avg Time Between Scans = Sum of all gaps / (Number of records - 1)**

Requires at least 2 records; otherwise shows "--". Displayed in minutes and seconds (e.g., "2m 15s").

#### Qty / Hour

**Qty/Hour = Record count / Hours elapsed**

Hours elapsed is measured from the start of the window (start of day or start of week) to the current time (or the end of the window if it has passed). This gives a running average that updates in real time.

### Performance Table

The performance table shows time-bucketed rows with planned vs. actual output, FPY, and downtime.

#### Planned Column

The Planned value for each time bucket depends on the **Capacity Target** and the **Shift Schedule**:

1. **Target Units Per Hour** is resolved by:
   - Finding all WorkCenterCapacityTargets for this work center's production lines at this plant.
   - Filtering to the most recently used plant gear (determined from the most recent production record that has a gear ID).
   - Averaging the target per production line, then summing across lines.
2. **Hourly view (Day):** Planned per hour = floor(Target Units Per Hour).
3. **Daily view (Week):** Planned per day = floor(Target Units Per Hour x Planned Minutes for that day / 60).
4. **Weekly view (Month):** Planned per week = floor(sum of daily planned values for each day in the week).

If no shift schedule exists or no capacity targets are configured, Planned shows "--".

#### Delta Column

**Delta = Actual - Planned**

Positive values (ahead of target) are styled green. Negative values (behind target) are styled red.

#### FPY Column

FPY is calculated per time bucket using the same first-pass-yield logic described above, scoped to the bucket's time window.

#### Downtime Column

- **Hourly view:** Downtime events are clamped to the hour boundaries and distributed across the hours they span. For example, a 90-minute downtime event starting at 10:30 contributes 30 minutes to the 10:00 bucket and 60 minutes to the 11:00 bucket.
- **Daily/Weekly views:** Sum of DowntimeEvent.DurationMinutes overlapping each day or week.

#### Total Row

- **Planned:** Sum of all bucket Planned values (floored).
- **Actual:** Sum of all bucket Actual values.
- **Delta:** Total Actual - Total Planned.
- **FPY:** Weighted average across buckets (each bucket's FPY weighted by its Actual count).
- **Downtime:** Sum of all bucket downtime values.

### Operator Chip Filter

The operator chips show all operators who have production records at this work center during the current week, sorted by record count (highest first). Each chip displays the operator's name and their record count. Selecting an operator filters all KPI cards and the performance table to that operator's records. OEE is always unfiltered (calculated for the whole work center).

## Fields & Controls

### OEE Cards

| Card | Description |
|---|---|
| **Overall OEE** | Availability x Performance x Quality. |
| **Availability** | Run Time / Planned Time. Shows downtime minutes as subtext. |
| **Performance** | Ideal cycle time ratio vs. run time. Shows run time minutes as subtext. |
| **Quality** | Day FPY (or 100% if FPY is not applicable). Shows planned minutes as subtext. |

### KPI Cards

| Card | Description |
|---|---|
| **Count** | Day and Week production record counts. |
| **First Pass Yield** | Day and Week FPY percentages (only for Long Seam and Round Seam type work centers). |
| **Total Defects** | Day and Week defect counts (only for Long Seam and Round Seam type work centers). |
| **Avg Time Between Scans** | Day and Week average gap between consecutive scans. |
| **Qty / Hour** | Day and Week running average units per hour. |

### Performance Table

| Column | Description |
|---|---|
| **Time Bucket** | Hour (Day view), day name (Week view), or ISO week number (Month view). |
| **Planned** | Expected output from capacity targets x shift schedule. |
| **Actual** | Production records in this bucket. |
| **Delta** | Actual minus Planned (green if positive, red if negative). |
| **FPY** | First Pass Yield for this bucket's time window. |
| **Downtime (min)** | Downtime event minutes in this bucket. |

### Controls

| Element | Description |
|---|---|
| **Work Center dropdown** | Selects the work center to monitor. |
| **View Mode selector** | Switches between Day, Week, Month, and Annotate views. |
| **Operator chip filter** | Filters KPIs and the performance table to a specific operator. OEE is always unfiltered. |
| **Annotation type** | In Annotate mode: Note, Internal Review, or Correction Needed. |
| **Annotation comment** | Free-text input for the annotation body. |

## Tips

- OEE thresholds are color-coded: green is on target, yellow is a warning zone, and red means the metric is below acceptable levels. Use these colors as a quick health check.
- The 30-second auto-refresh keeps the dashboard current without manual reloading. If you navigate away and come back, the data reloads immediately.
- Use the Annotate mode at the end of a shift to mark records that need follow-up — annotations are visible in the Log Viewer and Serial Number Lookup as well.
- If Planned shows "--", check that a **Shift Schedule** and **Capacity Targets** have been configured for this work center and plant. Both are required for the Planned column and OEE to calculate.
- OEE Availability will be low if downtime events are not being closed properly. Open (unended) downtime events accumulate indefinitely.
- FPY and Total Defects only appear for work centers in the Long Seam family (Rolls, Long Seam) and Round Seam family (Fitup, Round Seam). Other work centers show Count, Avg Time Between Scans, and Qty/Hour only.
- The operator filter affects KPI cards and the performance table but does not affect OEE, which is always calculated for the entire work center.
- Performance can exceed 100% if operators are producing faster than the configured capacity target. It is capped at 200% to prevent outliers from skewing the OEE calculation.
