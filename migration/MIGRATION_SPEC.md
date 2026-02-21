# MES V1 to V2 Data Migration Specification

## Overview

One-time full data migration from the V1 MES database (`QSCApps`, Azure SQL) to the V2 MES database (`MESv2`, Azure SQL on the same server). The migration tool is a standalone .NET 9 console application that reads V1 via Dapper and writes V2 via EF Core, preserving original V1 GUIDs as V2 primary keys.

## Architecture

```
V1: QSCApps (Azure SQL)          V2: MESv2 (Azure SQL)
        |                                  ^
        | ADO.NET / Dapper                 | EF Core (MesDbContext)
        | (read-only)                      | (upsert)
        v                                  |
    +--------------------------------------+
    |     MESv2.Migration Console App      |
    +--------------------------------------+
        |
        v
    migration-report.json (per-table stats, warnings, skipped row IDs)
```

- **Project:** `migration/MESv2.Migration/`
- **References:** `backend/MESv2.Api` (for EF models and DbContext)
- **Config:** `appsettings.json` or CLI args for connection strings
- **Idempotent:** upsert by primary key -- safe to re-run

## Decisions

| Decision | Resolution |
|---|---|
| WorkCenter deduplication | Not needed. V1 allows per-site WC rows but doesn't use them that way. Migrate 1:1, each WC keeps its PlantId. |
| User PINs | V1 stores plain text. Migration hashes via BCrypt before writing to V2 `PinHash`. |
| CompletedByUserId (ManufacturingLog) | V1 stores a GUID as `nvarchar(50)`. Parse directly via `Guid.TryParse`. Fall back to `CreatedByUserId` if null. |
| IsTest rows | **Do not migrate.** All queries filter `WHERE IsTest = 0` on tables that have the column. |
| Annotation polymorphic resolution | V1 uses `RecordType` + `RecordUID`. Migration resolves `RecordUID` against the migrated ProductionRecords set. Unresolvable rows are logged with their V1 ID and skipped -- to be fixed manually post-migration. |
| PlantGears (global in V1, per-plant in V2) | Each V1 gear is duplicated across all 3 plants using a deterministic GUID (XOR of gear ID + plant ID). |
| Batch size | 2,000 rows per EF `SaveChangesAsync` call. |

## Execution Order

Migration runs in strict dependency order. Tables that need special logic have dedicated runner methods; the rest use the generic `MigrateTableAsync` with a mapper function.

### Phase 1: Reference Data

| Step | V1 Table | V2 Entity | Strategy |
|------|----------|-----------|----------|
| 1a | `mesSite` | `Plant` | First pass: insert without `CurrentPlantGearId` (breaks circular dep) |
| 1b | `mesPlantGears` | `PlantGear` | Duplicate each gear x 3 plants; deterministic IDs |
| 1c | `mesSite` | `Plant` | Second pass: update `CurrentPlantGearId` to plant-specific gear |
| 2 | `mesProductionLine` | `ProductionLine` | `SiteNo` resolved to `PlantId` via Plant.Code lookup |
| 3a | `mesWorkCenter` | `WorkCenter` | `SiteCode` to `PlantId`; `WorkCenterTypeId` inferred from name; `DataEntryType` resolved via map + name inference (see below) |
| 3b | (derived) | `WorkCenterProductionLine` | Created from each WC + first production line at same plant. `DisplayName` = WC name, `NumberOfWelders` = WC default |
| 4 | `mesAsset` | `Asset` | No V1 WC FK. Matched by name convention ("Rolls 1 Asset" -> "Rolls 1" WC at same site). PlantId derived via WorkCenterProductionLine junction. Logged warning if no match. |
| 5 | `mesProductType` | `ProductType` | Direct map |
| 6 | `mesProduct` | `Product` | `ProductTypeId` is GUID stored as `nvarchar(50)` in V1; parsed to GUID |
| 7 | `mesUser` | `User` | `UserType` int -> `RoleTier`/`RoleName`; `Pin` -> BCrypt `PinHash`; `DisplayName` = First + Last; `IsCertifiedWelder` defaults false; `IsDisabled` -> `!IsActive` |
| 8 | `mesVendor` | `Vendor` | `IsMill`/`IsProcessor` booleans -> `VendorType` string ("mill", "processor", "head") |
| 9 | `mesAnnotationType` | `AnnotationType` | Column renames: `OperatorAllowed` -> `OperatorCanCreate`, `AnnotationColor` -> `DisplayColor` |
| 10 | `mesCharacteristic` | `Characteristic` | Drop `ProductId` (keep `ProductTypeId` only) |
| 11 | `mesCharacteristicWorkCenter` | `CharacteristicWorkCenter` | Direct map |
| 12 | `mesControlPlan` | `ControlPlan` | `CollectionWorkCenterId` -> `WorkCenterId`; `CollectionEnabled` int -> `IsEnabled` bool; `IsGateCheck` int -> bool. Filtered by `IsTest = 0`. |
| 13 | `mesDefectMaster` | `DefectCode` | `DefectCode` int -> `Code` string; `DefectName` -> `Name`; `DefectSeverity` int -> `Severity` string |
| 14 | `mesDefectLocation` | `DefectLocation` | `LookupId` int -> `Code` string; `LocationName` -> `Name` |
| 15 | `mesDefectWorkCenter` | `DefectWorkCenter` | `DefectId` -> `DefectCodeId`; `EarliestWorkCenterId` -> `EarliestDetectionWorkCenterId`; drop `RepairMasterId` |
| 16 | `mesKanbanCards` | `BarcodeCard` | `BarcodeValue` -> `CardValue`; `ColorName` -> `Color` |

### Phase 2: Transactional Data

| Step | V1 Table | V2 Entity | Strategy |
|------|----------|-----------|----------|
| 17 | `mesSerialNumberMaster` | `SerialNumber` | Full column map. Two-pass for `ReplaceBySNId` self-reference: insert with NULL, then update. Filtered by `IsTest = 0`. |
| 18 | `mesManufacturingLog` | `ProductionRecord` | `CompletedByUserId` (GUID-as-text) -> `OperatorId`; `LogDateTime` -> `Timestamp`. Filtered by `IsTest = 0`. |
| 19 | `mesManufacturingLogWelder` | `WelderLog` | `ManufacturingLogId` -> `ProductionRecordId`; `WelderUserId` -> `UserId`. Filtered by `IsTest = 0`. |
| 20 | `mesManufacturingTraceLog` | `TraceabilityLog` | `SerialNumberMasterId` -> `FromSerialNumberId`; `SerialNumberComponentId` -> `ToSerialNumberId`; `Relationship` = "component". Filtered by `IsTest = 0`. |
| 21 | `mesManufacturingInspectionsLog` | `InspectionRecord` | Joined with `mesManufacturingLog` to get `WorkCenterId` and `OperatorId`. `SerialNumberId` resolved to serial string via lookup. Filtered by `IsTest = 0`. |
| 22 | `mesDefectLog` | `DefectLog` | `SerialNumberMasterId` resolved to serial string; `IsRepaired` = `RepairedByUserId IS NOT NULL`; `ManufacturingLogId` -> `ProductionRecordId`. Filtered by `IsTest = 0`. |
| 23 | `mesAnnotation` | `Annotation` | Polymorphic `RecordType`+`RecordUID` resolved to `ProductionRecordId`. Unresolvable rows logged and skipped. Filtered by `IsTest = 0`. |
| 24 | `mesWorkCenterMaterialQueue` | `MaterialQueueItem` | Joined with `mesSerialNumberMaster` + `mesProduct` to denormalize product/vendor/heat/coil fields. `QueuePosition` decimal -> `Position` int. Filtered by `IsTest = 0`. |
| 25 | `mesChangeLog` | `ChangeLog` | Direct map |
| 26 | `mesSpotXrayIncrement` | `SpotXrayIncrement` | `ManufacturingLogId` preserved (FK to `ProductionRecord`). All seam shot/result/welder columns carried as strings. `IsDraft` int -> bool. |
| 27 | `mesSiteSchedule` | `SiteSchedule` | Direct map. Filtered by `IsTest = 0`. No V2 screen; migrated for reporting continuity. |

## DataEntryType Migration

V1 `mesWorkCenter.DataEntryType` values are inconsistent (often NULL). The mapper applies a two-step resolution:

1. **Map known V1 values:** `"Rolls"` -> `"Rolls"`, `"LS"` -> `"Barcode"`, `"Barcode"` -> `"Barcode"`, `"Fitup"` -> `"Fitup"`, `"Hydro"` -> `"Hydro"`, `"Spot"` -> `"Spot"`, `"DataPlate"` -> `"DataPlate"`
2. **Infer from WC name** when V1 value is null or unrecognized: e.g. names containing "long seam" -> `"Barcode"`, "fitup queue" -> `"MatQueue-Fitup"`, "rolls material" -> `"MatQueue-Material"`, "rt x-ray" -> `"MatQueue-Shell"`, etc.

## WorkCenterProductionLine Migration

After migrating WorkCenters, the runner creates one `WorkCenterProductionLine` record per WC using the first production line at the same plant. Initial values:
- `DisplayName` = `WorkCenter.Name`
- `NumberOfWelders` = `WorkCenter.NumberOfWelders`

These serve as baseline per-line overrides that admins can customize post-migration.

## V1 Tables NOT Migrated

| V1 Table | Reason |
|---|---|
| `mesErrorLog` | Replaced by Azure App Insights |
| `mesHeartbeat` | Replaced by health checks |
| `mesUsersCurrent` | Transient session data; replaced by `ActiveSession` |
| `mesUserNotification` | Out of scope for V2 |
| `mesWorkCenterDownsteamWClink` | Not needed in V2 |
| `mesXrayShotNumber` | Handled by app-level sequencing in V2 |

## V2 Schema Changes Made for Migration

These changes were added to the V2 backend models to accept V1 data:

**`SerialNumber` entity expanded** (was: `Id, Serial, ProductId, CreatedAt`):
- Added: `SiteCode`, `Notes`, `MillVendorId`, `ProcessorVendorId`, `HeadsVendorId`, `CoilNumber`, `HeatNumber`, `LotNumber`, `ReplaceBySNId` (self-ref), `Rs1Changed`-`Rs4Changed`, `IsObsolete`, `CreatedByUserId`, `ModifiedByUserId`, `ModifiedDateTime`
- Added FK relationships and indexes in `MesDbContext`

**`SpotXrayIncrement` entity created:**
- Maps 1:1 to `mesSpotXrayIncrement`
- FK to `ProductionRecord` via `ManufacturingLogId`
- All seam shot/result/welder columns as nullable strings

**`SiteSchedule` entity created:**
- Maps 1:1 to `mesSiteSchedule`
- No FK relationships (standalone reporting data)

## Logging and Validation

### Per-Table Logging

Every table migration records:
- Source row count (before migration)
- Migrated count
- Skipped count
- Individual warnings with V1 row ID and reason (e.g. "Annotation abc123: RecordType='SerialNumber' cannot be resolved")
- Duration

All output goes to console in real-time and is persisted to `migration-report.json`.

### Post-Migration Validation

Runs automatically after migration:

1. **Row count verification** -- compares V1 source counts vs V2 destination counts for all 24 table pairs. Flags any with >10% discrepancy.
2. **FK integrity checks** -- queries V2 for orphaned references on: `ProductionRecord.SerialNumberId`, `WelderLog.ProductionRecordId`, `DefectLog.DefectCodeId`, `Annotation.ProductionRecordId`, `User.DefaultSiteId`.
3. **Spot checks** -- picks 5 random SerialNumbers and traces their full history (production records, defects, trace logs) to verify completeness.

## Running the Tool

```bash
cd migration/MESv2.Migration

# Option 1: Edit appsettings.json with connection strings, then:
dotnet run

# Option 2: Pass connection strings via CLI:
dotnet run -- --ConnectionStrings:V1="Server=...;Database=QSCApps;..." --ConnectionStrings:V2="Server=...;Database=MESv2;..."

# Option 3: Skip test rows (default true):
dotnet run -- --Migration:SkipTestRows=true --Migration:ReportPath=migration-report.json
```

The tool calls `EnsureCreatedAsync()` on the V2 database before starting, so the schema is created automatically if the database is empty.
