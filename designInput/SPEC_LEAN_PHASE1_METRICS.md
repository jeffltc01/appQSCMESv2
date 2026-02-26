# Lean Phase 1: Takt, Lead-Time, and Queue-Age Metrics

## Purpose

Add short-interval execution metrics to complement current OEE and count-based reporting.

## Existing Surfaces

- Backend metrics DTOs: `backend/MESv2.Api/DTOs/SupervisorDashboardDtos.cs`
- OEE calculations: `backend/MESv2.Api/Services/OeeService.cs`
- Supervisor dashboard service/UI:
  - `backend/MESv2.Api/Services/SupervisorDashboardService.cs`
  - `frontend/src/features/admin/SupervisorDashboardScreen.tsx`

## New KPI Definitions

| KPI | Definition | Formula |
|---|---|---|
| TaktMinutes | Target minutes per unit for selected date/work center context | `PlannedRunMinutes / PlannedUnits` |
| ActualCycleMinutes | Actual average minutes per produced unit | `ActualRunMinutes / ActualUnits` |
| TaktAdherencePercent | Portion of intervals that met takt | `IntervalsWithinTakt / TotalIntervals * 100` |
| TaktMissCount | Number of intervals where actual cycle exceeded takt target | Count of intervals with `ActualCycleMinutes > TaktMinutes` |
| QueueAgeMinutesP95 | 95th percentile age of queued items | P95(`UtcNow - QueueEnteredAt`) |
| TotalLeadTimeMinutes | Elapsed time from first queue entry to final quality gate completion | `FinalGateTimestamp - FirstQueueTimestamp` |
| ValueAddedRatioPercent | Portion of lead-time classified as process-active | `ValueAddedMinutes / TotalLeadTimeMinutes * 100` |

## Proposed API/DTO Additions

### `SupervisorDashboardMetricsDto` additions

- `decimal? TaktMinutes`
- `decimal? ActualCycleMinutes`
- `decimal? TaktAdherencePercent`
- `int? TaktMissCount`
- `decimal? QueueAgeMinutesP95`
- `decimal? TotalLeadTimeMinutes`
- `decimal? ValueAddedRatioPercent`

### `SupervisorDashboardTrendsDto` additions

- `List<KpiTrendPointDto> TaktAdherence`
- `List<KpiTrendPointDto> QueueAgeP95`
- `List<KpiTrendPointDto> LeadTime`

### New interval endpoint (recommended)

- `GET /api/supervisor-dashboard/intervals`
- Query: `wcId`, `plantId`, `date`, `intervalMinutes` (default 60)
- Response:
  - `intervalStartUtc`
  - `intervalEndUtc`
  - `plannedUnits`
  - `actualUnits`
  - `taktMinutes`
  - `actualCycleMinutes`
  - `withinTakt` (bool)

## Data Dependencies

- `ShiftSchedules` for planned minutes.
- `ProductionRecords` for actual units and event timestamps.
- `MaterialQueueItems` for queue age; requires a durable queue-entry timestamp for active/queued states.
- Traceability and inspection completion timestamps for lead-time decomposition.

## UI Changes (Supervisor Dashboard)

- Add KPI cards for takt adherence, takt misses, queue age P95, and lead-time.
- Add an interval adherence chart (green/red bars by interval).
- Add warning banner when `TaktAdherencePercent < configurable threshold` (default 85%).

## Implementation Notes

- Keep all calculations UTC at storage, convert to plant-local boundaries for period slicing.
- Exclude data labeled test/demo mode from production KPI calculations.
- For missing schedule or denominator zero, return null metrics instead of zero.

## Acceptance Criteria

- New metrics are returned by API and rendered in supervisor dashboard.
- Interval endpoint supports configurable interval granularity.
- KPI calculations are deterministic and timezone-safe.
