# MES v2 — Reference Data & Entity Definitions

This document consolidates all supporting/reference data entity definitions for the MES v2 application. It captures field definitions, v1-to-v2 migration decisions, management screen access levels, and relationships.

For Product and Product Type details (including seed data tables), see [SPEC_DATA_PRODUCTS.md](SPEC_DATA_PRODUCTS.md).

---

## 1. Serial Number Master

> **The central entity of the entire MES.** Every serial number in the plant — raw material (plate and heads), shells, assemblies, and finished/nameplate serial numbers — lives in this table. It is purely system-managed; no management screen.

### 1.1 Entity Definition

| Field | Type | Description |
|---|---|---|
| **Id** | GUID (PK) | Unique identifier |
| **SerialNumber** | string, NOT NULL | The display serial number (see format by type below) |
| **ProductId** | GUID (FK), NOT NULL | Which product — determines the product type (stage in manufacturing) |
| **SiteCode** | string, NOT NULL | Plant code |
| **Notes** | string (nullable) | Notes |
| **MillVendorId** | GUID (FK, nullable) | For Plate: the steel mill that produced the plate |
| **ProcessorVendorId** | GUID (FK, nullable) | For Plate: the processor/distributor |
| **HeadsVendorId** | GUID (FK, nullable) | For Heads: the head vendor (CMF or Compco) |
| **CoilNumber** | string (nullable) | Coil identifier for traceability |
| **HeatNumber** | string (nullable) | Steel heat number for traceability |
| **LotNumber** | string (nullable) | Lot identifier for traceability |
| **ReplaceBySNId** | GUID (FK to self, nullable) | Points to the replacement serial number (e.g., reassembly) — kept for now |
| **RS1Changed** | bit | Round Seam 1 welder changed after initial setup |
| **RS2Changed** | bit | Round Seam 2 welder changed after initial setup |
| **RS3Changed** | bit | Round Seam 3 welder changed after initial setup |
| **RS4Changed** | bit | Round Seam 4 welder changed after initial setup |
| **IsObsolete** | bit | Record superseded or soft-deleted |
| **CreatedByUserId** | GUID (FK) | User who created the record |
| **CreatedDateTime** | datetime, NOT NULL | When the record was created — also used for alpha code uniqueness |
| **ModifiedByUserId** | GUID (FK, nullable) | Last user to modify |
| **ModifiedDateTime** | datetime (nullable) | Last modification timestamp |

**Unique constraint**: `(SerialNumber, SiteCode, CreatedDateTime)` — solves the alpha code (AA–ZZ) cycling problem while keeping the display value clean.

**Dropped from v1**: `ReviewFlag` (superseded by Annotations), `IsTest` (replaced by separate dev/test/prod environments).

### 1.2 Serial Number Formats by Product Type

| Product Type | Format | Example | Created At | Fields Populated |
|---|---|---|---|---|
| **Plate** | `Heat {heat} Coil {coil}` | "Heat B25563050 Coil 25B360890" | Rolls Material (via queue) | MillVendorId, ProcessorVendorId, CoilNumber, HeatNumber |
| **Head (CMF)** | `Lot {lot}` | "Lot W030" | Fitup Queue (via queue) | HeadsVendorId, LotNumber, CoilNumber (= lot) |
| **Head (Compco)** | `Heat {heat} Coil {coil}` | "Heat B516375 Coil 25B657458" | Fitup Queue (via queue) | HeadsVendorId, CoilNumber, HeatNumber |
| **Shell** | Sequential 6-digit number | "012744", "015977" | Rolls (barcode scan) | None — material data linked via TraceabilityLog |
| **Assembled Tank** | 2-letter alpha code (AA–ZZ, cycles) | "BG", "CQ", "TE" | Fitup (system-assigned) | None — components linked via TraceabilityLog |
| **Sellable Tank** | `W00` + sequential digits | "W00272052" | Nameplate (hand-typed) | None — assembly linked via TraceabilityLog at Hydro |

### 1.3 Alpha Code Uniqueness

Alpha codes cycle from AA through ZZ (676 values) and then restart. This means the same alpha code (e.g., "BG") can exist multiple times in the table for the same site. The composite unique constraint `(SerialNumber, SiteCode, CreatedDateTime)` ensures database-level uniqueness while the UI always displays just the two-letter code.

When looking up an assembly by alpha code (e.g., at Round Seam or Hydro), the system should resolve to the **most recent, non-obsolete** record for that alpha code at the given site.

### 1.4 Raw Material Serial Numbers

Raw materials (plate and heads) do not have vendor-assigned unique serial numbers. The MES synthesizes a serial number from the traceability identifiers:
- **Plate**: Composed from the Heat Number and Coil Number
- **Heads (CMF)**: Composed from the Lot Number
- **Heads (Compco)**: Composed from the Heat Number and Coil/Slab Number

These synthesized serials allow raw material to participate in the same TraceabilityLog relationships as all other serial numbers.

### 1.5 Field Population by Work Center

| Work Center | Fields Set | Notes |
|---|---|---|
| **Rolls Material** (queue) | Creates Plate serial: MillVendorId, ProcessorVendorId, HeatNumber, CoilNumber | When material is added to queue |
| **Fitup Queue** (queue) | Creates Head serial: HeadsVendorId, LotNumber or HeatNumber + CoilNumber | When heads are added to queue |
| **Rolls** | Creates Shell serial: SerialNumber (from barcode) | On dual-label scan |
| **Long Seam** | May create Shell serial (catch-up flow) | If Rolls missed the scan |
| **Long Seam Inspection** | May create Shell serial (catch-up flow) | Last station for catch-up |
| **Fitup** | Creates Assembly serial: SerialNumber (alpha code) | System-assigned on save |
| **Nameplate** | Creates Sellable Tank serial: SerialNumber (W00... hand-typed) | Operator types from etched nameplate |
| **Round Seam** | May set RS1Changed–RS4Changed flags | When welder changes after setup |

---

## 2. Manufacturing Log (Production Record)

> **The second most important table.** Every manufacturing event — from rolling a shell to hydro testing — creates a record here. System-managed; no management screen.

### 2.1 ManufacturingLog Entity

| Field | Type | Description |
|---|---|---|
| **Id** | GUID (PK) | Unique identifier |
| **SerialNumberMasterId** | GUID (FK), NOT NULL | The serial number this event is for |
| **LogDateTime** | datetime, NOT NULL | When the event happened |
| **WorkCenterId** | GUID (FK) | Which work center |
| **ProductionLineId** | GUID (FK) | Which production line |
| **PlantGearId** | GUID (FK) | Gear at time of event (snapshot for analysis) |
| **CompletedByUserId** | GUID (FK) | The operator who completed the event (v2: proper FK to User; v1 stored as nvarchar) |
| **EventType** | string | "Manufacturing", "Inspection", or "XrayQueue" |
| **AssetId** | GUID (FK, nullable) | Which asset (machine/station) |
| **CreatedByUserId** | GUID (FK) | Audit |
| **CreatedDateTime** | datetime | Audit |
| **ModifiedByUserId** | GUID (FK, nullable) | Audit |
| **ModifiedDateTime** | datetime (nullable) | Audit |

**Dropped from v1**: `ProductIdIn`, `ProductIdOut` (unused), `IsTest`.

### 2.2 ManufacturingLogWelder Entity (Welder Link)

Many-to-many link between a manufacturing event and the welders who performed it. At most work centers, the `CharacteristicId` is NULL (general welder). At Round Seam, each welder is tied to a specific seam characteristic (RS1–RS4).

| Field | Type | Description |
|---|---|---|
| **Id** | GUID (PK) | Unique identifier |
| **ManufacturingLogId** | GUID (FK), NOT NULL | The production record |
| **WelderUserId** | GUID (FK), NOT NULL | The welder |
| **CharacteristicId** | GUID (FK, nullable) | Per-seam assignment at Round Seam (RS1, RS2, RS3, RS4). NULL = general welder for the station. |
| **CreatedByUserId** | GUID (FK) | Audit |
| **CreatedDateTime** | datetime | Audit |
| **ModifiedByUserId** | GUID (FK, nullable) | Audit |
| **ModifiedDateTime** | datetime (nullable) | Audit |

**Dropped from v1**: `IsTest`.

### 2.3 ManufacturingTraceLog Entity (Traceability)

Links a parent serial number to its component serial numbers, creating the full traceability chain from raw material through to the finished tank. Created at work centers where components are combined (Rolls → shell-to-plate, Fitup → assembly-to-shells/heads, Hydro → sellable-to-assembly).

| Field | Type | Description |
|---|---|---|
| **Id** | GUID (PK) | Unique identifier |
| **SerialNumberMasterId** | GUID (FK), NOT NULL | The parent/resulting serial (e.g., assembly alpha code) |
| **SerialNumberComponentId** | GUID (FK), NOT NULL | The component serial (e.g., shell, head, plate lot) |
| **ManufacturingLogId** | GUID (FK), NOT NULL | The manufacturing event that created this link |
| **Quantity** | decimal | Currently always 1; future use for multi-quantity components (valves, fittings) |
| **TankLocation** | string (nullable) | Position on the tank |

**Dropped from v1**: `IsTest`.

#### TankLocation Values

| Value | Meaning |
|---|---|
| `Head 1` | Left head position |
| `Head 2` | Right head position |
| `Shell 1` | First (or only) shell |
| `Shell 2` | Second shell (1000+ gal tanks) |
| `Shell 3` | Third shell (> 1000 gal tanks) |
| `Replaced` | Component was replaced during reassembly |

### 2.4 ManufacturingInspectionsLog Entity (Inspection Results)

Records inspection outcomes against a ControlPlan entry for a given manufacturing event. Linked to the ManufacturingLog, not standalone.

| Field | Type | Description |
|---|---|---|
| **Id** | GUID (PK) | Unique identifier |
| **ManufacturingLogId** | GUID (FK), NOT NULL | The production record this inspection belongs to |
| **ControlPlanId** | GUID (FK), NOT NULL | Which control plan entry defines this inspection |
| **ResultType** | string | "PassFail" (currently the only type) |
| **ResultText** | string (nullable) | "Pass", "Fail", etc. |
| **ResultNumber** | decimal (nullable) | For future numeric results |
| **SerialNumberId** | GUID (FK, nullable) | For Spot X-ray: the specific tank in the increment that this result applies to (since one x-ray event covers 4–8 tanks) |
| **SpotIncrementId** | GUID (FK, nullable) | For Spot X-ray: links to the increment record |
| **SystemNotes** | string (nullable) | System-generated notes |

**Dropped from v1**: `IsTest`.

### 2.5 Relationships

```
ManufacturingLog (1) ──→ (1) SerialNumberMaster
ManufacturingLog (1) ──→ (N) ManufacturingLogWelder
ManufacturingLog (1) ──→ (N) ManufacturingTraceLog
ManufacturingLog (1) ──→ (N) ManufacturingInspectionsLog
ManufacturingLog (1) ──→ (N) DefectLog (see work center specs)
ManufacturingLog (1) ──→ (N) Annotation
```

### 2.6 Event Creation by Work Center

| Work Center | EventType | Creates Welder Records | Creates Trace Records | Creates Inspection Records |
|---|---|---|---|---|
| **Rolls** | Manufacturing | Yes (general) | Yes (shell → plate lot) | Yes (thickness check PassFail) |
| **Long Seam** | Manufacturing | Yes (general) | No | No |
| **Long Seam Inspection** | Inspection | No | No | No (defects go to DefectLog) |
| **Fitup** | Manufacturing | Yes (general) | Yes (assembly → shells + heads) | No |
| **Round Seam** | Manufacturing | Yes (per-seam via CharacteristicId) | No | No |
| **Round Seam Inspection** | Inspection | No | No | No (defects go to DefectLog) |
| **Spot X-ray** | Inspection | No | No | Yes (per-tank in increment) |
| **Nameplate** | Manufacturing | No | No | No |
| **Hydro** | Inspection | No | Yes (sellable → assembly) | Yes (PassFail gate check) |
| **RT X-ray Queue** | XrayQueue | No | No | No |

---

## 3. Annotation

> System-managed and user-created. Operators can create certain types (Defect, Correction Needed) from the WC History panel. The system auto-creates annotations for catch-up flows (Long Seam, Long Seam Inspection). No dedicated management screen — annotations are created and resolved inline.

### 3.1 Annotation Entity

| Field | Type | Description |
|---|---|---|
| **Id** | GUID (PK) | Unique identifier |
| **AnnotationTypeId** | GUID (FK), NOT NULL | Which annotation type (Note, AI Review, Defect, Internal Review, Correction Needed) |
| **AnnotationNotes** | string (2000) | The annotation text |
| **FlagStatus** | string | "Open" or "Resolved" |
| **ResolutionNotes** | string (1000, nullable) | Notes added when resolving |
| **SiteCode** | string, NOT NULL | Plant |
| **AnnotationDateTime** | datetime, NOT NULL | When the annotation was created |
| **InitiatedByUserId** | GUID (FK), NOT NULL | Who created it (user or system on behalf of user) |
| **ResolvedByUserId** | GUID (FK, nullable) | Who resolved it |
| **SerialNumberId** | GUID (FK, nullable) | The serial number being annotated (when RecordType = "SerialNumber") |
| **ManufacturingLogId** | GUID (FK, nullable) | The manufacturing event being annotated (when applicable) |
| **SystemTypeInfo** | string (nullable) | Additional system-generated context (e.g., "Catch-up: Rolls missed scan") |

**Dropped from v1**: `RecordType` and `RecordUID` (polymorphic FK) — replaced by explicit nullable FKs (`SerialNumberId`, `ManufacturingLogId`). `IsTest` dropped.

### 3.2 Annotation Lifecycle

```
Created (FlagStatus = "Open")
    ↓
  [Operator or higher-level role adds resolution notes]
    ↓
Resolved (FlagStatus = "Resolved", ResolvedByUserId set)
```

For annotation types where `RequiresResolution = false` (Note, AI Review, Internal Review), the annotation can remain Open indefinitely — resolution is optional.

For types where `RequiresResolution = true` (Defect, Correction Needed), the annotation should be resolved to clear it from active flags.

### 3.3 Auto-Created Annotations

| Scenario | Annotation Type | Notes |
|---|---|---|
| **Rolls missed scan — catch-up at Long Seam** | Correction Needed | "Rolls scan missed for shell {serial}. Material lot assumed from previous shell — validate." |
| **Rolls + Long Seam missed — catch-up at Long Seam Inspection** | Correction Needed | "Rolls and Long Seam scans missed for shell {serial}. Material lot assumed — validate." |
| **Long Seam missed — catch-up at Long Seam Inspection** | Correction Needed | "Long Seam scan missed for shell {serial}. Annotation created for tracking." |

### 3.4 UI Interaction

- **View**: Annotations appear as colored flags (per AnnotationType.AnnotationColor) in the WC History panel on the Operator Work Center Layout.
- **Create**: Operators tap the flag icon on a WC History entry to create an annotation. Only types where `OperatorAllowed = true` are available (Defect, Correction Needed).
- **Resolve**: Higher-level roles (Team Lead 5.0+) can open an annotation, add resolution notes, and mark it as Resolved.

---

## 4. Active Session

> Tracks which users are currently logged in at each work center. No management screen — this is system-managed. Viewable via the **"Who's On the Floor"** screen available to **Team Lead (5.0) and above**.

### 4.1 ActiveSession Entity

| Field | Type | Description |
|---|---|---|
| **Id** | GUID (PK) | Unique identifier |
| **UserId** | GUID (FK), NOT NULL | The logged-in user |
| **SiteCode** | string, NOT NULL | Plant |
| **ProductionLineId** | GUID (FK), NOT NULL | Which line |
| **WorkCenterId** | GUID (FK), NOT NULL | Which station |
| **AssetId** | GUID (FK, nullable) | Which tablet/asset |
| **LoginDateTime** | datetime, NOT NULL | When the user logged in at this station |
| **LastHeartbeatDateTime** | datetime, NOT NULL | Updated periodically by the client (e.g., every 60 seconds) |
| **ActiveWelderIds** | (child rows or separate query) | Welders currently assigned at this station |

### 4.2 Session Lifecycle

```
Login at configured tablet
    → Upsert ActiveSession row for (UserId, WorkCenterId, ProductionLineId)
    → Client starts heartbeat timer (60s interval)

Heartbeat tick
    → API updates LastHeartbeatDateTime

Logout (explicit or session timeout)
    → Remove ActiveSession row
```

**Stale session detection**: If `LastHeartbeatDateTime` is older than a configurable threshold (e.g., 5 minutes), the session is considered stale and can be cleaned up by a background job or ignored in queries.

### 4.3 Concurrent Login Enforcement

**Operators (6.0) through Supervisor (4.0):** Single-session enforcement — when a user logs in at a new tablet, the previous session's ActiveSession row is removed and the old JWT is invalidated. The previous tablet returns to the Login screen on its next API call.

**Quality Manager (3.0) and above:** Multiple concurrent sessions allowed. These users may log in at multiple work centers simultaneously for testing or to cover stations temporarily. Previous sessions are not terminated on new login. Stale sessions are cleaned up via heartbeat expiration.

However, certain work centers enforce a **maximum number of concurrent operators per production line**, controlled by the `WorkCenter.MaxConcurrentLogins` field:

| Scenario | Behavior |
|---|---|
| `MaxConcurrentLogins` is NULL | No limit — any number of operators can log in |
| `MaxConcurrentLogins` = 1 | Only one active (non-stale) session allowed per WC + production line. If a second user attempts to log in, the system **blocks login** and displays: "This work center already has an active operator: {name}. Only one operator is allowed per line." |
| `MaxConcurrentLogins` = N | Up to N active sessions allowed |

Stale sessions (heartbeat expired) do **not** count toward the limit — only sessions with a recent heartbeat block new logins.

### 4.4 "Who's On the Floor" Screen

- **Access**: Team Lead (5.0) and above
- **Scope**: Filtered to the user's current `SiteCode`
- **Display**: A table or card layout showing each production line's work centers with the currently active operator (if any), login time, and assigned welders
- **Stale indicator**: Sessions with expired heartbeats shown as dimmed/italic with a "last seen" timestamp
- **No actions**: Read-only view. Higher-level roles (Quality Director 2.0+) could potentially force-end a stale session in a future enhancement.

---

## 5. Site (Plant)

> No management screen — seed data managed at the database level.

| Field | Type | Description |
|---|---|---|
| **Id** | GUID (PK) | Unique identifier |
| **SiteName** | string | "Cleveland", "Fremont", "West Jordan" |
| **SiteCode** | string (unique) | "000", "600", "700" |
| **CurrentPlantGearId** | GUID (FK) | Current production speed level (references PlantGear) |
| **NextTankAlphaCode** | string | Next alpha code to assign at Fitup (AA–ZZ, per plant) |

**Dropped from v1**: `NextDateSeqCode`, `NextTankCountSeqCode`, `NextDateCode` — unused.

| Site Code | Plant Name |
|---|---|
| 000 | Cleveland |
| 600 | Fremont |
| 700 | West Jordan |

---

## 6. Production Line

> No management screen — seed data managed at the database level.

| Field | Type | Description |
|---|---|---|
| **Id** | GUID (PK) | Unique identifier |
| **LineName** | string | Production line name |
| **SiteCode** | string (FK) | Plant this line belongs to |

**Dropped from v1**: `PlantGearId` (gear tracked at Site level only), `ShellLabelCode` (unused).

---

## 7. Plant Gear

> No management screen for gears themselves — seed data. The Operations Director (2.0) changes the current gear on the Site via a UI control.

| Field | Type | Description |
|---|---|---|
| **Id** | GUID (PK) | Unique identifier |
| **GearName** | string | "Gear 1" through "Gear 5" |
| **Gear** | int | 1–5 |

Shared across all sites (no SiteCode). Production records are tagged with the site's current gear at creation time for analysis purposes. Gear does not affect MES logic.

---

## 8. Work Center

> Management screen: **Quality Director (2.0) and above** — edit only.

| Field | Type | Description |
|---|---|---|
| **Id** | GUID (PK) | Unique identifier |
| **WorkCenterName** | string | Display name (shared across all plants) |
| **DataEntryType** | string | Frontend routing key — determines which screen component to load |
| **MaterialQueueForWCId** | GUID (FK, nullable) | For queue screens — points to the production WC they feed |
| **NoOfWelders** | int | Minimum welders required to start logging data (0 = none) |
| **AllowRecordDefects** | bit | Whether this WC logs defects |
| **ForceLabelValidation** | bit | Dual-label match enforcement (Rolls only) |
| **ProductionSequence** | decimal | Ordering (1.0 Rolls → 9.0 Hydro, 1000 = material handling) |
| **MaxConcurrentLogins** | int, nullable | Max simultaneous operators per production line. NULL = unlimited. E.g., Rolls = 1 (only one operator per line). |

**Dropped from v1**: `SiteCode` — work centers are now a shared list across all plants.

### 8.1 DataEntryType Values

| Value | Screen Component |
|---|---|
| `Rolls` | Rolls work center |
| `Barcode` | Scan-driven stations (Long Seam, Long Seam Inspection, Round Seam, Round Seam Inspection). **Note:** These four share a DataEntryType but have distinct workflows — Long Seam is a simple scan-through, Long Seam Inspection adds defect entry mode, Round Seam requires a Setup Screen for per-seam welder assignments, and Round Seam Inspection adds characteristic selection to defect entry. The frontend component must branch based on the specific WorkCenterName or additional configuration. |
| `Fitup` | Fitup assembly |
| `Hydro` | Hydrostatic testing |
| `Spot` | Spot X-ray |
| `DataPlate` | Nameplate (PC-based) |
| `RealTimeXray` | Real Time X-ray (separate app) |
| `Plasma` | Plasma (not currently active) |
| `MatQueue-Material` | Rolls Material queue |
| `MatQueue-Fitup` | Fitup Queue (heads) |
| `MatQueue-Shell` | Real Time X-ray Queue |

### 8.2 Production Sequence

| Sequence | Work Center |
|---|---|
| 1.0 | Rolls |
| 2.0 | Long Seam |
| 3.0 | Long Seam Inspection |
| 4.0 | Real Time X-ray |
| 4.5 | Plasma |
| 5.0 | Fitup |
| 6.0 | Round Seam |
| 7.0 | Round Seam Inspection |
| 7.5 | Spot X-ray |
| 8.0 | Nameplate |
| 9.0 | Hydro |
| 1000 | Material Handling screens |

---

## 9. Asset

> Management screen: **Quality Manager (3.0) and above** — add/edit, no delete (soft delete via IsActive).

| Field | Type | Description |
|---|---|---|
| **Id** | GUID (PK) | Unique identifier |
| **AssetName** | string | Display name (e.g., "SAW Round Seam Lane 1", "Longseam A") |
| **WorkCenterId** | GUID (FK) | Ties asset to a work center |
| **SiteCode** | string (FK) | Site-specific |
| **MaintenanceIdentifier** | string (nullable) | Limble CMMS asset ID — carried forward for future use |
| **LaneName** | string (nullable) | Physical lane when a work center has parallel paths |
| **IsActive** | bool | Soft delete |

**Dropped from v1**: `AssetType` — redundant with `WorkCenterId`.

---

## 10. Vendor

> Management screen: **Quality Manager (3.0) / Plant Manager (3.0) and above** — full CRUD.

| Field | Type | Description |
|---|---|---|
| **Id** | GUID (PK) | Unique identifier |
| **VendorName** | string | Display name |
| **VendorType** | string | Categorization — "Mill", "Processor", "Head", etc. |
| **SiteCode** | string (FK) | Site-specific |
| **SageVendorNo** | string (nullable) | External reference to Sage ERP — for future integration |
| **IsActive** | bool | Soft delete |

**Dropped from v1**: `IsMill`, `IsProcessor` — superseded by `VendorType`.

---

## 11. Characteristic

> Management screen: **Quality Director (2.0) and above** — edit only (no add/delete).

| Field | Type | Description |
|---|---|---|
| **Id** | GUID (PK) | Unique identifier |
| **Characteristic** | string | Name (e.g., "Long Seam", "RS1", "RS2", "RS3", "RS4") |
| **SpecLimitHigh** | decimal (nullable) | *Carried forward, not used in v2 initial release* |
| **SpecLimitLow** | decimal (nullable) | *Carried forward, not used in v2 initial release* |
| **SpecLimitTarget** | decimal (nullable) | *Carried forward, not used in v2 initial release* |
| **SpecLimitsInclude** | int (nullable) | *Carried forward, not used in v2 initial release* |
| **ProductTypeId** | GUID (FK, nullable) | Ties to a product type; can be NULL |
| **EnableLocDetail1** | bit | *Carried forward, not used in v2 initial release* |
| **EnableLocDetail2** | bit | *Carried forward, not used in v2 initial release* |
| **LocDetail1Name** | string (nullable) | *Carried forward, not used in v2 initial release* |
| **LocDetail2Name** | string (nullable) | *Carried forward, not used in v2 initial release* |
| **LookupId** | int (unique) | Integer code used in `C;YYY` barcode format |

**Dropped from v1**: `ProductId` — only `ProductTypeId` is used (nullable).

### 11.1 CharacteristicWorkCenter (Junction Table)

| Field | Type | Description |
|---|---|---|
| **Id** | GUID (PK) | Unique identifier |
| **CharacteristicId** | GUID (FK) | References Characteristic |
| **WorkCenterId** | GUID (FK) | Which work center this characteristic applies at |

---

## 12. Control Plan

> Management screen: **Quality Director (2.0) and above** — edit only (no add/delete).

| Field | Type | Description |
|---|---|---|
| **Id** | GUID (PK) | Unique identifier |
| **CharacteristicId** | GUID (FK) | Which characteristic this plan entry is for |
| **SiteCode** | string (FK) | Site-specific |
| **CollectionWorkCenterId** | GUID (FK) | Which work center collects this data |
| **CollectionEnabled** | int | Toggle data collection on/off without removing the record |
| **ResultType** | string | Type of result — currently only "PassFail" |
| **IsGateCheck** | bool | Whether this is a gate check — tank must pass to be sellable |
| **IsRemoved** | bool | Soft delete |

### 12.1 Gate Checks

Three inspections are gate checks — a tank must pass all three to be sellable:

1. **Real Time X-ray** inspection
2. **Spot X-ray** inspection
3. **Hydro** inspection

---

## 13. Defect Code (DefectMaster)

> Management screen: **Quality Director (2.0) and above** — full CRUD, respecting `AllowDelete` flag.

| Field | Type | Description |
|---|---|---|
| **Id** | GUID (PK) | Unique identifier |
| **DefectCode** | int (unique) | Code used in `D;XXX` barcode format |
| **DefectName** | string | Display name (e.g., "Burn Through", "Undercut", "Cold Lap") |
| **SystemTypeName** | string (nullable) | Severity category — carried forward for future use |
| **DefectSeverity** | int (nullable) | Severity level — carried forward for future use |
| **AllowDelete** | bit | Whether this record can be deleted (e.g., code 999 "Shell Plate Thickness" = no) |

### 13.1 DefectWorkCenter (Junction Table)

| Field | Type | Description |
|---|---|---|
| **Id** | GUID (PK) | Unique identifier |
| **DefectId** | GUID (FK) | References DefectMaster |
| **WorkCenterId** | GUID (FK) | Which work center this defect can be logged at |
| **EarliestWorkCenterId** | GUID (FK, nullable) | Earliest point in the line where this defect could have been caught |

**Dropped from v1**: `RepairMasterId` — unused.

---

## 14. Defect Location

> Management screen: **Quality Director (2.0) and above** — full CRUD.

| Field | Type | Description |
|---|---|---|
| **Id** | GUID (PK) | Unique identifier |
| **LocationName** | string | Display name (e.g., "T-Joint", "Tack", "Fill Valve", "Leg") |
| **CharacteristicId** | GUID (FK, nullable) | Ties location to a characteristic for filtering. NULL = universal. |
| **DefaultLocDetails1** | decimal (nullable) | *Carried forward, not used — all NULL currently* |
| **DefaultLocDetails2** | decimal (nullable) | *Carried forward, not used — all NULL currently* |
| **LookupId** | int | Integer code for `L;XXX` barcode format |

**Migration note**: "Start Tack" and "End Tack" have NULL `CharacteristicId` in v1 — likely missing data, needs to be populated during migration.

---

## 15. Annotation Type

> No management screen — seed data.

| Field | Type | Description |
|---|---|---|
| **Id** | GUID (PK) | Unique identifier |
| **Name** | string | Type name |
| **RecordType** | string | What records this can apply to — "Any" or "SerialNumber" |
| **RequiresResolution** | bool | Whether the annotation must be resolved/closed |
| **AnnotationColor** | string | Hex color for UI display (flag in WC History panel) |
| **OperatorAllowed** | bool | Whether operators (6.0) can create this type |
| **Abbreviation** | string(2) | Short display code |

### 15.1 Seed Data

| Name | RecordType | RequiresResolution | Color | OperatorAllowed | Abbrev |
|---|---|---|---|---|---|
| Note | Any | No | #cc00ff (purple) | No | N |
| AI Review | SerialNumber | No | #33cc33 (green) | No | AI |
| Defect | SerialNumber | Yes | #ff0000 (red) | Yes | D |
| Internal Review | SerialNumber | No | #0099ff (blue) | No | IR |
| Correction Needed | SerialNumber | Yes | #ffff00 (yellow) | Yes | C |

---

## 16. Kanban Card (BarcodeCard)

> Management screen: **Team Lead (5.0) and above** — add/remove cards.

| Field | Type | Description |
|---|---|---|
| **Id** | GUID (PK) | Unique identifier |
| **BarcodeValue** | string | Value encoded in barcode (e.g., "01", "02") — scanned as `KC;{value}` |
| **ColorName** | string | Display name of card color (e.g., "Red", "Yellow") |
| **ColorCode** | string | Hex color code for UI rendering (e.g., "#FF0000") |
| **SiteCode** | string (FK) | Site-specific — cards are physical objects at each plant |
| **IsActive** | bool | Soft delete |

See [SPEC_WC_FITUP_QUEUE.md](SPEC_WC_FITUP_QUEUE.md) for full card lifecycle and usage details.

---

## 17. Site Schedule

> No management screen in v2 — data will come from external scheduling system integration (future).

| Field | Type | Description |
|---|---|---|
| **Id** | GUID (PK) | Unique identifier |
| **SiteCode** | string (FK) | Plant code |
| **Quantity** | decimal | Target quantity |
| **QuantityComplete** | decimal | Completed so far |
| **TankSize** | int | Tank size |
| **TankType** | string (nullable) | AG, UG, etc. |
| **ItemNo** | string (nullable) | External scheduling system reference |
| **ColorName** | string (nullable) | Paint color name (for downstream work centers) |
| **ColorValue** | string (nullable) | Hex color for UI display |
| **BuildDate** | datetime (nullable) | When to build |
| **DispatchDate** | datetime (nullable) | When to ship |
| **Comments** | string (nullable) | Notes |
| **MasterScheduleId** | GUID (nullable) | Link to external scheduling system |
| **Status** | string | "Open", "In Process", "Complete" |

**Note**: Not actively used in v2 initial release. Displayed to operators via the "Schedule" feature in the left panel when populated. Color fields are for communicating paint color to downstream stations, not UI styling.

---

## 18. Management Screen Access Summary

| Entity | Access Level | Operations |
|---|---|---|
| **Characteristics** | Quality Director (2.0)+ | Edit only |
| **Control Plans** | Quality Director (2.0)+ | Edit only |
| **Defect Codes** | Quality Director (2.0)+ | Full CRUD (AllowDelete respected) |
| **Defect Locations** | Quality Director (2.0)+ | Full CRUD |
| **Work Centers** | Quality Director (2.0)+ | Edit only |
| **Plant Gear (change current)** | Operations Director (2.0)+ | Change current gear on Site |
| **Assets** | Quality Manager (3.0)+ | Add/edit, no delete (soft delete) |
| **Vendors** | Quality Manager (3.0)+ | Full CRUD |
| **Products** | Quality Manager (3.0)+ | Full CRUD |
| **Kanban Cards** | Team Lead (5.0)+ | Add/remove |
| **Who's On the Floor** | Team Lead (5.0)+ | Read-only view of active sessions per site |

---

## 19. V1 Tables Not Carried Forward

| V1 Table | Reason |
|---|---|
| `mesErrorLog` | Replaced by Application Insights |
| `mesHeartbeat` | Replaced by Azure App Service health checks |
| `mesUsersCurrent` | Replaced by JWT session management |
| `mesUserNotification` | Removed from initial scope |
| `mesWorkCenterDownsteamWClink` | Not needed in v2 |
| `mesWorkCenterMaterialQueue` | Replaced by v2 queue design (per work center specs) |

---

## References

| Document | Relevance |
|---|---|
| [SPEC_DATA_PRODUCTS.md](SPEC_DATA_PRODUCTS.md) | Product and Product Type definitions with full seed data tables |
| [SPEC_WC_FITUP_QUEUE.md](SPEC_WC_FITUP_QUEUE.md) | BarcodeCard entity details and card lifecycle |
| [SPEC_OPERATOR_WC_LAYOUT.md](SPEC_OPERATOR_WC_LAYOUT.md) | NoOfWelders enforcement, annotation flags, gear display |
| [GENERAL_DESIGN_INPUT.md](GENERAL_DESIGN_INPUT.md) | ER diagram, feature access table |
| [MES_V1_TABLE_SCRIPTS.sql](MES_V1_TABLE_SCRIPTS.sql) | V1 database schema reference |
