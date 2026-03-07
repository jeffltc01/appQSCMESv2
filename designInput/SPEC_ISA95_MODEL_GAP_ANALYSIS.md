# MES v2 — ISA-95 Model Gap Analysis & Implementation Specification

This document analyzes the gaps between the ISA-95 Production Model defined for FRE Inner Shell Line (ISL) and the current MESv2 application, and provides implementation specifications for closing those gaps.

**Source Document**: ISA-95 Production Model FRE Site - Inner Shell Line (ISL) Process Segments, Product Routings, Equipment, Gear Configurations, and Rework Patterns

---

## 1. Executive Summary

The ISA-95 document defines a formal manufacturing model with **Process Segments**, **Product Routings (Operations Definitions)**, **Equipment Classes**, **Personnel Classes**, **Gear Configurations (Production Capability)**, and **Rework Operations Definitions**. The current MESv2 application has evolved organically with a domain-driven approach that partially maps to ISA-95 concepts but lacks explicit modeling of several key abstractions.

### 1.1 Key ISA-95 Principles from Source Document

1. **The product determines the routing.** A 500AG tank always needs the same sequence of production steps regardless of plant configuration. Routing is a property of the product, not the plant.

2. **The gear determines how the plant is configured to execute routings.** Gears are about resources—adding or removing workstations, operators, and parallel capacity to hit a targeted plant output rate.

3. **Rework is event-driven, not pre-planned.** The standard routing describes the happy path. When a defect is found, the MES generates a rework operations request that pulls the vessel off the main routing, executes repair segments, and re-enters the main routing at the appropriate re-inspection point.

4. **Plasma cutting is the product commitment point.** Before plasma, a rolled and welded shell is generic. After plasma cuts the hole pattern, the shell is committed to a specific product configuration.

### 1.2 ISA-95 Modeling Concepts

| ISA-95 Concept | Purpose |
|----------------|---------|
| **Process Segments** | Reusable building blocks of all routings |
| **Operations Definitions** | Product-specific routing (sequence of Process Segments) |
| **Equipment Model** | Physical plant hierarchy (Site → Area → Work Center → Work Unit) |
| **Equipment Classes** | Logical grouping of equipment by capability |
| **Personnel Classes** | Certification/qualification requirements |
| **Production Capability (Gears)** | Plant configuration profiles with resource allocations |
| **Rework Operations Definitions** | Predefined repair loops triggered by nonconformance |

---

## 2. Gap Summary Matrix

| # | Gap | Priority | Complexity | Current Coverage |
|---|-----|----------|------------|------------------|
| 1 | No Explicit Process Segment Entity | High | Medium | Partial (WorkCenter acts as implicit segment) |
| 2 | No Product Routing / Operations Definition | High | High | None (routing implicit via ProductionSequence) |
| 3 | No Equipment Class Abstraction | Medium | Low | None |
| 4 | No Personnel Class / Certification Model | Medium | Medium | None (WelderLog exists but no qualifications) |
| 5 | Limited Gear / Production Capability Model | Medium | Medium | Partial (PlantGear levels 1-5 only) |
| 6 | No Rework Operations Definitions | High | High | Partial (NCR/HoldTag exist, no rework routing) |
| 7 | No Product Parameters on Routing Steps | Medium | Medium | None |
| 8 | No Sub-Assembly / Parallel Feed Support | Medium | Medium-High | Partial (MaterialQueue for raw materials only) |
| 9 | No Multi-Shell Assembly Pattern (OSL) | Low | High | Partial (TraceabilityLog supports parent-child) |
| 10 | No Line Reconfiguration Support (WJO) | Low | High | None |
| 11 | No Product Commitment Point Tracking | Low | Low | None |
| 12 | Spot X-Ray Sampling Model | Low | Low | Likely exists (SpotXrayIncrement) |

---

## 3. Detailed Gap Analysis & Specifications

### 3.1 GAP 1: Process Segment Entity

#### 3.1.1 ISA-95 Requirement

Process Segments are the fundamental building blocks with:
- Unique Segment ID (e.g., `SEG-ROLL-MATERIAL`, `SEG-LONG-SEAM`, `SEG-PLASMA-CUT`)
- Description of the operation
- Equipment Class (what type of equipment can execute it)
- Personnel Class (what certifications are required)
- Dependencies (which segments must complete first)
- Segment type (Standard, PWHT, OffLine, Rework)

**ISL Process Segment Library (from source document):**

| Segment ID | Description | Equipment Class | Personnel Class | Type |
|------------|-------------|-----------------|-----------------|------|
| SEG-ROLL-MATERIAL | Receive and roll raw material | roll-material | roll-operator | Standard |
| SEG-ROLL | Complete rolling operation | rolls | roll-operator | Standard |
| SEG-LONG-SEAM | Longitudinal seam welding | long-seam-weld | asme-ix-welder | Standard |
| SEG-LS-VISUAL-INSP | Visual inspection of long seam | ls-visual-insp | weld-inspector | Standard |
| SEG-RT-XRAY | Real-time radiographic exam | rt-radiography | rt-technician | Standard |
| SEG-PLASMA-CUT | Plasma cut holes for flanges/fittings | plasma-cut | plasma-operator | Standard |
| SEG-FLANGE-WELD | Weld flanges to shell | flange-weld | asme-ix-welder | Standard |
| SEG-FIT-UP | Head and shell fit-up | fit-up | fit-up-operator | Standard |
| SEG-ROUND-SEAM | Circumferential seam welding | round-seam-weld | asme-ix-welder | Standard |
| SEG-RS-INSPECT | Round seam weld inspection | rs-inspection | weld-inspector | Standard |
| SEG-ATTACH-WELD | Weld attachments (legs, lugs) | attach-weld | asme-ix-welder | Standard |
| SEG-NAMEPLATE | Weld nameplate to vessel | nameplate-weld | welder | Standard |
| SEG-HYDRO-PREP | Prepare vessel for hydro test | hydro-prep | test-operator | Standard |
| SEG-HYDRO-TEST | Hydrostatic pressure test | hydro-test | test-operator | Standard |
| SEG-ROCK-DRAIN | Rock vessel to drain after hydro | rock-drain | operator | Standard |
| SEG-SPOT-XRAY | Spot radiographic exam (sampling) | spot-radiography | rt-technician | Standard |
| SEG-PWHT | Post-weld heat treat | pwht-furnace | pwht-operator | PWHT |
| SEG-HYDRO-TEST-2 | Second hydro test (post-PWHT) | hydro-test | test-operator | PWHT |
| SEG-ROCK-DRAIN-2 | Second rock/drain (post-PWHT) | rock-drain | operator | PWHT |
| SEG-LEG-FAB | Cut and bend legs (off-line) | leg-fabrication | fab-operator | OffLine |
| SEG-NP-PREP | Engrave and prep nameplate (off-line) | nameplate-prep | np-operator | OffLine |
| SEG-REPAIR-WELD | Repair welding (off-line station) | repair-weld | asme-ix-welder | Rework |
| SEG-PWHT-BURN-OFF | PWHT to burn off paint for rework | pwht-furnace | pwht-operator | Rework |

#### 3.1.2 Current State

Work Centers act as implicit process segments, but:
- No Equipment Class abstraction—assets directly linked to work centers
- No Personnel Class requirements—no validation of operator qualifications
- No formal dependency definitions between work centers
- No distinction between segment types (standard vs rework vs off-line)

#### 3.1.3 Entity Specification

```
ProcessSegment
├── Id: int (PK)
├── SegmentId: string, NOT NULL, UNIQUE (e.g., "SEG-ROLL-MATERIAL")
├── Description: string, NOT NULL
├── EquipmentClassId: int (FK to EquipmentClass), NOT NULL
├── PersonnelClassId: int (FK to PersonnelClass), nullable
├── SegmentType: enum (Standard, PWHT, OffLine, Rework)
├── IsParallel: bool (for sub-assembly feeds like LEG-FAB)
├── IsProductCommitmentPoint: bool (true for SEG-PLASMA-CUT)
├── IsActive: bool
├── CreatedDateTime: datetime
└── ModifiedDateTime: datetime, nullable
```

```
ProcessSegmentDependency
├── Id: int (PK)
├── ProcessSegmentId: int (FK to ProcessSegment), NOT NULL
├── DependsOnSegmentId: int (FK to ProcessSegment), NOT NULL
├── DependencyType: enum (Required, Parallel)
└── Notes: string, nullable
```

#### 3.1.4 Relationship to WorkCenter

Process Segments define **what** operation is performed. Work Centers define **where** it is performed. A Work Center implements one Process Segment:

```
WorkCenter (existing)
├── ...existing fields...
└── ProcessSegmentId: int (FK to ProcessSegment), nullable
```

This allows:
- Multiple work centers to implement the same process segment (parallel capacity)
- Process segment reuse across production lines
- Clear separation of operation definition from physical equipment

---

### 3.2 GAP 2: Product Routing / Operations Definition

#### 3.2.1 ISA-95 Requirement

Each product has an explicit Operations Definition specifying:
- Ordered sequence of Process Segments with sequence numbers (010, 020, 030...)
- Product-specific parameters per step (WPS, test pressure, cut patterns)
- The routing is a property of the product, not the plant

**Example: 500AG Routing (OD-500AG-ISL)**

| Seq | Process Segment | Notes | Product Parameters |
|-----|-----------------|-------|--------------------|
| 010 | SEG-ROLL-MATERIAL | Standard | - |
| 020 | SEG-ROLL | Standard | - |
| 030 | SEG-LONG-SEAM | Standard | WPS-101 (GMAW) |
| 040 | SEG-LS-VISUAL-INSP | Standard | - |
| 050 | SEG-RT-XRAY | Required - RT radiographic exam | Per ASME code |
| 060 | SEG-PLASMA-CUT | COMMITMENT POINT | AG hole pattern (4 openings) |
| 070 | SEG-FLANGE-WELD | Depends on SEG-PLASMA-CUT | WPS-101 (GMAW) |
| 080 | SEG-FIT-UP | Standard | - |
| 090 | SEG-ROUND-SEAM | Standard | WPS-101 (GMAW) |
| 100 | SEG-RS-INSPECT | Standard | - |
| ∥ | SEG-LEG-FAB | PARALLEL OFF-LINE | AG leg spec (4 legs) |
| 110 | SEG-ATTACH-WELD | Requires legs from SEG-LEG-FAB | WPS-101 (GMAW) |
| ∥ | SEG-NP-PREP | PARALLEL OFF-LINE | UL/ASME data, serial # |
| 120 | SEG-NAMEPLATE | Requires nameplate from SEG-NP-PREP | UL/ASME nameplate |
| 130 | SEG-HYDRO-TEST | Depends on SEG-NAMEPLATE | 312 PSI test pressure |
| 140 | SEG-ROCK-DRAIN | Rock/drain after hydro | - |

**Example: 500NH3 Routing (OD-500NH3-ISL)** — Same as 500AG but adds PWHT loop:

| Seq | Process Segment | Notes | Product Parameters |
|-----|-----------------|-------|--------------------|
| 010-140 | (Same as 500AG) | ... | Different WPS (WPS-310 SAW) |
| 150 | SEG-PWHT | Post-weld heat treat (separate building) | - |
| 160 | SEG-HYDRO-TEST-2 | Second hydro test (post-PWHT) | - |
| 170 | SEG-ROCK-DRAIN-2 | Second rock/drain | - |

#### 3.2.2 Current State

Routing is implicit through:
- `ProductionSequence` on Work Centers (plant-centric, not product-centric)
- `MaterialQueueForWCId` for material flow between stations
- **No per-product routing variations**
- **No product parameters per step**

This means all products follow the same implicit path through the plant, which doesn't match ISA-95's product-determines-routing principle.

#### 3.2.3 Entity Specification

```
OperationsDefinition
├── Id: int (PK)
├── OperationsDefinitionId: string, NOT NULL, UNIQUE (e.g., "OD-500AG-ISL")
├── ProductId: int (FK to Product), NOT NULL
├── ProductionLineId: int (FK to ProductionLine), NOT NULL
├── Description: string
├── Version: int (for routing revisions)
├── EffectiveDate: datetime
├── IsActive: bool
├── CreatedDateTime: datetime
└── ModifiedDateTime: datetime, nullable

UNIQUE CONSTRAINT: (ProductId, ProductionLineId, Version)
```

```
OperationsDefinitionStep
├── Id: int (PK)
├── OperationsDefinitionId: int (FK to OperationsDefinition), NOT NULL
├── ProcessSegmentId: int (FK to ProcessSegment), NOT NULL
├── SequenceNumber: int, NOT NULL (010, 020, 030...)
├── IsParallelFeed: bool (for off-line sub-assembly steps)
├── Notes: string, nullable
├── CreatedDateTime: datetime
└── ModifiedDateTime: datetime, nullable

UNIQUE CONSTRAINT: (OperationsDefinitionId, SequenceNumber)
```

```
OperationsDefinitionStepParameter
├── Id: int (PK)
├── OperationsDefinitionStepId: int (FK), NOT NULL
├── ParameterName: string, NOT NULL (e.g., "wps", "testPressure", "holePattern")
├── ParameterValue: string, NOT NULL (e.g., "WPS-101", "312", "AG-4-OPENING")
├── ParameterUnit: string, nullable (e.g., "PSI")
└── Notes: string, nullable

UNIQUE CONSTRAINT: (OperationsDefinitionStepId, ParameterName)
```

#### 3.2.4 Usage Pattern

When a production order is created:
1. Look up the `OperationsDefinition` for the product and production line
2. The routing steps define the expected sequence of process segments
3. At each work center, the operator sees the product-specific parameters for that step
4. Production records are validated against the expected routing

---

### 3.3 GAP 3: Equipment Class Abstraction

#### 3.3.1 ISA-95 Requirement

Equipment Classes define **what kind** of equipment can execute a process segment:
- `roll-material`, `long-seam-weld`, `plasma-cut`, `hydro-test`, etc.
- Multiple physical Work Units can belong to the same Equipment Class
- Enables scheduling flexibility (any RT X-ray machine can perform SEG-RT-XRAY)
- Decouples process definition from specific equipment

**Equipment Classes from source document:**

| Class ID | Description |
|----------|-------------|
| roll-material | Raw material receiving and initial rolling |
| rolls | Shell rolling equipment |
| long-seam-weld | Longitudinal seam welding station |
| ls-visual-insp | Long seam visual inspection station |
| rt-radiography | Real-time radiographic examination equipment |
| plasma-cut | Plasma cutting equipment |
| flange-weld | Flange welding station |
| fit-up | Head/shell fit-up station |
| round-seam-weld | Circumferential seam welding station |
| rs-inspection | Round seam inspection station |
| attach-weld | Attachment welding station |
| nameplate-weld | Nameplate welding station |
| hydro-prep | Hydro test preparation station |
| hydro-test | Hydrostatic test equipment |
| rock-drain | Post-hydro drain station |
| spot-radiography | Spot radiographic examination equipment |
| pwht-furnace | Post-weld heat treatment furnace |
| leg-fabrication | Leg cutting and bending equipment |
| nameplate-prep | Nameplate engraving equipment |
| repair-weld | Repair welding station |

#### 3.3.2 Current State

- Assets are directly linked to Work Centers without a class layer
- No logical grouping of equivalent equipment
- Scheduling must reference specific work centers rather than capability

#### 3.3.3 Entity Specification

```
EquipmentClass
├── Id: int (PK)
├── ClassId: string, NOT NULL, UNIQUE (e.g., "long-seam-weld")
├── Description: string, NOT NULL
├── IsActive: bool
├── CreatedDateTime: datetime
└── ModifiedDateTime: datetime, nullable
```

**Relationship to existing entities:**

```
WorkCenter (existing)
├── ...existing fields...
└── EquipmentClassId: int (FK to EquipmentClass), nullable
```

```
Asset (existing)
├── ...existing fields...
└── EquipmentClassId: int (FK to EquipmentClass), nullable
```

---

### 3.4 GAP 4: Personnel Class / Certification Model

#### 3.4.1 ISA-95 Requirement

Personnel Classes define required certifications for process segments:
- `asme-ix-welder` — ASME Section IX certified welder
- `rt-technician` — Radiography technician (Level II)
- `weld-inspector` — Certified weld inspector
- `test-operator` — Hydrostatic test qualified
- `pwht-operator` — Post-weld heat treatment operator

Process Segments reference Personnel Classes to ensure only qualified personnel execute operations.

#### 3.4.2 Current State

- `User` entity exists but no certification tracking
- `WelderLog` tracks welder assignments but not qualifications
- No validation that assigned personnel meet segment requirements
- Critical for ASME code compliance

#### 3.4.3 Entity Specification

```
PersonnelClass
├── Id: int (PK)
├── ClassId: string, NOT NULL, UNIQUE (e.g., "asme-ix-welder")
├── Description: string, NOT NULL
├── CertificationAuthority: string, nullable (e.g., "ASME", "AWS", "SNT-TC-1A")
├── IsActive: bool
├── CreatedDateTime: datetime
└── ModifiedDateTime: datetime, nullable
```

```
UserCertification
├── Id: int (PK)
├── UserId: int (FK to User), NOT NULL
├── PersonnelClassId: int (FK to PersonnelClass), NOT NULL
├── CertificationNumber: string, nullable
├── CertifiedDate: datetime, NOT NULL
├── ExpirationDate: datetime, nullable
├── CertifyingBody: string, nullable
├── IsActive: bool
├── Notes: string, nullable
├── CreatedDateTime: datetime
└── ModifiedDateTime: datetime, nullable

UNIQUE CONSTRAINT: (UserId, PersonnelClassId, CertificationNumber)
```

#### 3.4.4 Validation Rules

When an operator logs production at a work center:
1. Get the ProcessSegment for the work center
2. Get the required PersonnelClass for the segment
3. Verify the operator has an active, non-expired certification for that class
4. If not qualified, block production and display warning

---

### 3.5 GAP 5: Full Gear / Production Capability Model

#### 3.5.1 ISA-95 Requirement

Gear Configurations (Production Capability) define:
- Named configurations with target output rates: `G1-500` (500/day), `G2-750` (750/day)
- Work Unit availability per gear (which stations are active)
- Operator allocations per station
- Resource assignments

**ISL Gear Configurations from source document:**

| Gear ID | Target Output | Round Seam A | Round Seam B | Operators (typical) |
|---------|---------------|--------------|--------------|---------------------|
| G1-500 | 500 tanks/day | Available | Not Available | 12 |
| G1-750 | 750 tanks/day | Available | Not Available | 14 |
| G2-750 | 750 tanks/day | Available | Available | 16 |
| G2-850 | 850 tanks/day | Available | Available | 18 |

#### 3.5.2 Current State

- `PlantGear` exists as a numeric level (1-5)
- Captured on ProductionRecord for historical analysis
- **No named configurations**
- **No work unit availability mapping per gear**
- **No target output rates**
- **No operator allocation definitions**

#### 3.5.3 Entity Specification

**Enhance existing PlantGear:**

```
PlantGear (enhanced)
├── Id: int (PK) [existing]
├── SiteCode: string [existing]
├── GearLevel: int [existing, 1-5]
├── GearCode: string, nullable (NEW - e.g., "G2-750")
├── Description: string, nullable (NEW)
├── TargetOutputPerDay: int, nullable (NEW)
├── TargetOperatorCount: int, nullable (NEW)
├── IsActive: bool [existing]
├── CreatedDateTime: datetime
└── ModifiedDateTime: datetime, nullable
```

**New entity for work center availability per gear:**

```
GearWorkCenterConfig
├── Id: int (PK)
├── PlantGearId: int (FK to PlantGear), NOT NULL
├── WorkCenterId: int (FK to WorkCenter), NOT NULL
├── Availability: enum (Available, NotAvailable, Combined)
├── AllocatedOperators: int, nullable
├── AllocatedWelders: int, nullable
├── Notes: string, nullable

UNIQUE CONSTRAINT: (PlantGearId, WorkCenterId)
```

#### 3.5.4 Usage

When the plant switches gears:
1. Update `Plant.CurrentPlantGearId` (already exists)
2. UI queries `GearWorkCenterConfig` to show which work centers are active
3. Scheduling uses availability to allocate work
4. Dashboard shows target vs actual output

---

### 3.6 GAP 6: Rework Operations Definitions

#### 3.6.1 ISA-95 Requirement

Predefined rework patterns triggered by nonconformance:
- Each rework pattern has a defined sequence of repair segments
- Re-entry point back into main routing is specified
- Links original operations request to rework operations
- Full traceability from defect → repair → re-inspection

**Rework Patterns from source document:**

| Rework ID | Trigger | Repair Segments | Re-Entry Point |
|-----------|---------|-----------------|----------------|
| REPAIR-WELD-PRE-PAINT | Defect found before paint (RT fail, visual defect) | SEG-REPAIR-WELD | Re-enter at original inspection |
| REPAIR-WELD-POST-PAINT | Defect found after paint (pinhole, crack) | SEG-PWHT-BURN-OFF → SEG-REPAIR-WELD → Repaint | Re-enter at inspection |
| RT-REJECT-REPAIR | RT radiography failure | SEG-REPAIR-WELD | SEG-RT-XRAY (re-test) |
| HYDRO-FAIL-REPAIR | Hydro test failure | SEG-REPAIR-WELD | SEG-HYDRO-TEST (re-test) |

#### 3.6.2 Current State

- `Ncr` (Non-Conformance Report) entity captures defects
- `HoldTag` can pause production
- Workflow engine supports approval flows
- **No predefined rework routing patterns**
- **No automatic re-entry point handling**
- **Rework is ad-hoc, not systematic**

#### 3.6.3 Entity Specification

```
ReworkOperationsDefinition
├── Id: int (PK)
├── ReworkDefinitionId: string, NOT NULL, UNIQUE (e.g., "REPAIR-WELD-POST-PAINT")
├── Description: string, NOT NULL
├── TriggerCondition: string (human-readable trigger description)
├── ReentryProcessSegmentId: int (FK to ProcessSegment), NOT NULL
├── RequiresSupervisorApproval: bool
├── IsActive: bool
├── CreatedDateTime: datetime
└── ModifiedDateTime: datetime, nullable
```

```
ReworkOperationsStep
├── Id: int (PK)
├── ReworkOperationsDefinitionId: int (FK), NOT NULL
├── ProcessSegmentId: int (FK to ProcessSegment), NOT NULL
├── SequenceNumber: int, NOT NULL
├── Notes: string, nullable

UNIQUE CONSTRAINT: (ReworkOperationsDefinitionId, SequenceNumber)
```

```
ReworkRequest
├── Id: int (PK)
├── SerialNumberId: int (FK to SerialNumber), NOT NULL
├── OriginalProductionRecordId: int (FK to ProductionRecord), NOT NULL
├── ReworkOperationsDefinitionId: int (FK), NOT NULL
├── NcrId: int (FK to Ncr), nullable
├── Status: enum (Pending, InProgress, Completed, Cancelled)
├── InitiatedByUserId: int (FK to User), NOT NULL
├── InitiatedDateTime: datetime, NOT NULL
├── CompletedDateTime: datetime, nullable
├── Notes: string, nullable
├── CreatedDateTime: datetime
└── ModifiedDateTime: datetime, nullable
```

```
ReworkProductionRecord
├── Id: int (PK)
├── ReworkRequestId: int (FK to ReworkRequest), NOT NULL
├── ProductionRecordId: int (FK to ProductionRecord), NOT NULL
├── ReworkStepSequenceNumber: int, NOT NULL
├── IsReentryRecord: bool (true for the re-inspection after rework)
```

#### 3.6.4 Workflow Integration

Rework integrates with existing workflow engine:
1. NCR created with defect details
2. Supervisor selects appropriate `ReworkOperationsDefinition`
3. `ReworkRequest` created, triggering workflow
4. Each repair step creates `ProductionRecord` linked via `ReworkProductionRecord`
5. Re-entry step completes the rework loop
6. Full traceability maintained

---

### 3.7 GAP 7: Product Parameters on Routing Steps

#### 3.7.1 ISA-95 Requirement

Product-specific parameters per process step:
- **WPS (Weld Procedure Specification)**: WPS-101 (GMAW), WPS-310 (SAW)
- **Test Pressures**: 312 PSI, 375 PSI
- **Cut Patterns**: AG hole pattern (4 openings), NH3 hole pattern
- **Leg Specifications**: AG leg spec (4 legs), NH3 leg spec

These parameters are displayed to operators and may drive equipment settings.

#### 3.7.2 Current State

- No product-to-step parameter mapping in the system
- Parameters may be in paper documentation or tribal knowledge
- Operators must know which parameters apply to which product

#### 3.7.3 Entity Specification

Already covered in GAP 2 via `OperationsDefinitionStepParameter`. Additionally:

```
ParameterDefinition (reference data)
├── Id: int (PK)
├── ParameterName: string, NOT NULL, UNIQUE (e.g., "wps", "testPressure")
├── DisplayName: string, NOT NULL (e.g., "Weld Procedure", "Test Pressure")
├── DataType: enum (String, Integer, Decimal, Boolean)
├── Unit: string, nullable (e.g., "PSI", "°F")
├── Description: string, nullable
├── ValidationRegex: string, nullable
├── IsActive: bool
```

#### 3.7.4 UI Integration

At each work center screen:
1. Query `OperationsDefinitionStepParameter` for current product and process segment
2. Display parameters in a "Product Specs" panel
3. Critical parameters (test pressure, WPS) highlighted
4. Parameters logged with production record for traceability

---

### 3.8 GAP 8: Sub-Assembly / Parallel Feed Support

#### 3.8.1 ISA-95 Requirement

Off-line component production that feeds main line:
- `SEG-LEG-FAB` — Leg fabrication (cut and bend legs)
- `SEG-NP-PREP` — Nameplate preparation (engrave serialized nameplate)
- These run in parallel with main-line production
- Fan-in at main line: `SEG-ATTACH-WELD` requires main line completion AND `SEG-LEG-FAB`

#### 3.8.2 Current State

- Material queues exist for raw materials (plates, heads)
- No modeling of fabricated sub-components
- No parallel feed tracking for legs or nameplates
- No fan-in dependency enforcement

#### 3.8.3 Entity Specification

```
SubAssemblyDefinition
├── Id: int (PK)
├── SubAssemblyId: string, NOT NULL, UNIQUE (e.g., "LEG-FAB-500AG")
├── Description: string, NOT NULL
├── ProcessSegmentId: int (FK to ProcessSegment), NOT NULL (e.g., SEG-LEG-FAB)
├── ConsumingProcessSegmentId: int (FK), NOT NULL (e.g., SEG-ATTACH-WELD)
├── ProductId: int (FK to Product), nullable (if product-specific)
├── QuantityPerUnit: int (e.g., 4 legs per tank)
├── IsActive: bool
├── CreatedDateTime: datetime
└── ModifiedDateTime: datetime, nullable
```

```
SubAssemblyInstance
├── Id: int (PK)
├── SubAssemblyDefinitionId: int (FK), NOT NULL
├── SerialNumber: string, nullable (for serialized components like nameplates)
├── BatchNumber: string, nullable (for batch components like legs)
├── Status: enum (Produced, InQueue, Consumed, Scrapped)
├── ProducedDateTime: datetime, nullable
├── ProducedByUserId: int (FK to User), nullable
├── ConsumedBySerialNumberId: int (FK to SerialNumber), nullable
├── ConsumedDateTime: datetime, nullable
├── Notes: string, nullable
├── CreatedDateTime: datetime
└── ModifiedDateTime: datetime, nullable
```

#### 3.8.4 Usage

1. Off-line stations (leg fab, nameplate prep) produce `SubAssemblyInstance` records
2. Main-line stations query available sub-assemblies
3. At `SEG-ATTACH-WELD`, system validates legs are available before allowing production
4. Consumption recorded with link to vessel serial number
5. Full traceability from sub-component to finished vessel

---

### 3.9 GAP 9: Multi-Shell Assembly Pattern (OSL)

#### 3.9.1 ISA-95 Requirement

The OSL (Outer Shell Line) produces vessels composed of multiple shells:
- 3-shell product requires three independent shell routings
- Each shell goes through front-end steps (Roll → Long Seam → RT X-ray)
- Some shells receive offset joggle operation
- All shells converge at assembly
- ISA-95 models this with nested Process Segments

#### 3.9.2 Current State

- `TraceabilityLog` supports parent-child relationships
- Single-shell products assumed in current workflow
- No nested routing support
- No parallel shell execution modeling

#### 3.9.3 Entity Specification (Future Phase)

```
NestedProcessSegment
├── Id: int (PK)
├── ParentSegmentId: int (FK to ProcessSegment), NOT NULL
├── ChildSegmentId: int (FK to ProcessSegment), NOT NULL
├── Iterations: int, NOT NULL (e.g., 3 for 3-shell)
├── IterationParameterName: string, nullable (e.g., "joggleRequired")
├── IterationParameterValues: string, nullable (JSON array: [true, true, false])

UNIQUE CONSTRAINT: (ParentSegmentId, ChildSegmentId)
```

**Enhancement to OperationsDefinitionStep:**

```
OperationsDefinitionStep (enhanced)
├── ...existing fields...
├── NestedIterations: int, nullable
├── IterationParameters: string, nullable (JSON per iteration)
```

#### 3.9.4 Implementation Notes

This is an advanced pattern for OSL line support. Implementation should:
1. Wait until ISL line patterns are stable
2. Leverage existing TraceabilityLog for shell-to-vessel links
3. Consider whether nested segments or parallel OperationsDefinitions better fit the workflow

---

### 3.10 GAP 10: Line Reconfiguration Support (WJO)

#### 3.10.1 ISA-95 Requirement

The WJO line can reconfigure for different product sizes:
- **500-gallon mode**: Round Seam A and B are independent parallel Work Units
- **1,000-gallon mode**: A and B combine to form single multi-head unit
- This is more than a gear change—it changes what Work Units functionally are

#### 3.10.2 Current State

- No equipment capability profiles
- No combined work unit modeling
- No product-size-based configuration switching

#### 3.10.3 Entity Specification (Future Phase)

```
EquipmentCapabilityProfile
├── Id: int (PK)
├── WorkCenterId: int (FK to WorkCenter), NOT NULL
├── ConfigurationName: string, NOT NULL (e.g., "500-Gal", "1000-Gal")
├── EquipmentClassId: int (FK to EquipmentClass), NOT NULL
├── IsCombinedUnit: bool
├── ComposedOfWorkCenterIds: string, nullable (comma-separated IDs)
├── IsActive: bool

UNIQUE CONSTRAINT: (WorkCenterId, ConfigurationName)
```

```
LineConfiguration
├── Id: int (PK)
├── ProductionLineId: int (FK to ProductionLine), NOT NULL
├── ConfigurationName: string, NOT NULL
├── Description: string, nullable
├── IsActive: bool
├── ActivatedDateTime: datetime, nullable
├── ActivatedByUserId: int (FK to User), nullable
```

#### 3.10.4 Implementation Notes

This is an advanced pattern for WJO line support. Implementation should wait until simpler patterns are proven.

---

### 3.11 GAP 11: Product Commitment Point Tracking

#### 3.11.1 ISA-95 Requirement

Plasma cutting is the "product commitment point":
- Before plasma, a shell is generic—could become AG, UG, or AG/UG
- After plasma cuts the hole pattern, the shell is committed to a specific product
- Important for inventory planning and rework decisions

#### 3.11.2 Current State

- No explicit commitment point concept
- Product association may happen at various points

#### 3.11.3 Entity Specification

**Enhancement to ProcessSegment:**

```
ProcessSegment (enhanced)
├── ...existing fields...
├── IsProductCommitmentPoint: bool (true for SEG-PLASMA-CUT)
```

**Enhancement to ProductionRecord:**

```
ProductionRecord (enhanced)
├── ...existing fields...
├── IsCommitmentRecord: bool
├── CommittedProductId: int (FK to Product), nullable
```

#### 3.11.4 Usage

1. When production record created at commitment point work center
2. System marks `IsCommitmentRecord = true`
3. Product locked to vessel from this point forward
4. Pre-commitment rework may change product; post-commitment cannot

---

### 3.12 GAP 12: Spot X-Ray Sampling Model

#### 3.12.1 ISA-95 Requirement

Spot radiography is statistical sampling:
- Approximately 1 in 4-7 tanks selected for spot x-ray
- Ratio driven by tank size and ASME code requirements
- Not a routing step—modeled as Quality Operations requirement
- Single spot x-ray machine serves entire plant

#### 3.12.2 Current State

- `SpotXray` controller and screen exist
- `SpotXrayIncrement` and `XrayShotCounter` suggest sampling logic implemented
- May already align with ISA-95 model

#### 3.12.3 Verification Needed

Review existing implementation to confirm:
1. Sampling ratio is product-configurable
2. Selection is based on statistical sampling, not per-unit routing
3. Quality Operations model (separate from production routing) is followed

If gaps exist:

```
QualitySamplingRequirement
├── Id: int (PK)
├── ProductId: int (FK to Product), NOT NULL
├── ProcessSegmentId: int (FK to ProcessSegment), NOT NULL
├── SampleRatioNumerator: int (e.g., 1)
├── SampleRatioDenominator: int (e.g., 5 for 1-in-5)
├── SamplingBasis: string (e.g., "TankSize", "CodeRequirement")
├── IsActive: bool

UNIQUE CONSTRAINT: (ProductId, ProcessSegmentId)
```

---

## 4. Implementation Phases

### Phase 1: Foundation (ISA-95 Core Entities)

**Entities:**
- EquipmentClass
- PersonnelClass
- UserCertification
- ProcessSegment
- ProcessSegmentDependency

**Relationships:**
- WorkCenter.ProcessSegmentId (FK)
- WorkCenter.EquipmentClassId (FK)

**Management Screens:**
- Equipment Classes CRUD
- Personnel Classes CRUD
- User Certifications (extend User management)
- Process Segments CRUD

**Estimated Scope:** ~15-20 entities/screens

---

### Phase 2: Product Routings

**Entities:**
- OperationsDefinition
- OperationsDefinitionStep
- OperationsDefinitionStepParameter
- ParameterDefinition

**Features:**
- Product routing editor
- Parameter configuration per step
- Routing version management
- Display product parameters at work center screens

**Estimated Scope:** ~10-15 screens/features

---

### Phase 3: Enhanced Capability

**Entities:**
- PlantGear enhancements (GearCode, TargetOutput)
- GearWorkCenterConfig

**Features:**
- Gear configuration editor
- Work center availability per gear
- Target vs actual dashboards
- Gear-aware scheduling hints

**Estimated Scope:** ~5-10 screens/features

---

### Phase 4: Rework Management

**Entities:**
- ReworkOperationsDefinition
- ReworkOperationsStep
- ReworkRequest
- ReworkProductionRecord

**Features:**
- Rework pattern definition
- NCR-triggered rework workflow
- Re-entry point handling
- Rework traceability reports

**Estimated Scope:** ~10-15 screens/features

---

### Phase 5: Advanced Patterns (As Needed)

**Entities:**
- SubAssemblyDefinition
- SubAssemblyInstance
- NestedProcessSegment (OSL)
- EquipmentCapabilityProfile (WJO)
- LineConfiguration (WJO)

**Features:**
- Sub-assembly tracking
- Multi-shell assembly support
- Line reconfiguration management

**Estimated Scope:** Variable based on line requirements

---

## 5. Migration Strategy

### 5.1 Backward Compatibility

All new entities are additive:
- Existing WorkCenters continue to function without ProcessSegment assignment
- Existing PlantGear levels continue to work
- New fields are nullable where needed

### 5.2 Data Seeding

Phase 1 should include seed data for:
- Equipment Classes (from Section 3.3)
- Personnel Classes (from Section 3.4)
- Process Segments for ISL line (from Section 3.1)

### 5.3 Gradual Adoption

1. Create new entities but don't enforce
2. Seed reference data
3. Map existing WorkCenters to ProcessSegments
4. Enable validation once data is complete
5. Roll out per production line

---

## 6. B2MML Compatibility Notes

The ISA-95 source document includes B2MML (Business to Manufacturing Markup Language) XML examples. If future integration with B2MML-compliant systems is needed:

- Entity IDs should be string-based (already specified as `SegmentId`, `ClassId`, etc.)
- Hierarchical relationships match B2MML schema
- Equipment capability properties can be serialized to B2MML format

Example B2MML for Equipment Capability:

```xml
<EquipmentCapability>
  <EquipmentID>ISL-L1-RNDSEAM-A</EquipmentID>
  <EquipmentElementLevel>WorkUnit</EquipmentElementLevel>
  <CapabilityType>Available</CapabilityType>
  <EquipmentCapabilityProperty>
    <ID>equipmentClass</ID>
    <Value>round-seam-weld</Value>
  </EquipmentCapabilityProperty>
</EquipmentCapability>
```

---

## 7. Open Questions

1. **Routing Enforcement**: Should the system enforce routing sequence (block out-of-order production) or just track deviations?

2. **Personnel Validation**: Should certification validation be blocking or warning-only during transition?

3. **Multi-Line Products**: Can a single product have different routings on different lines, or is routing always line-specific?

4. **Rework Authority**: Who can initiate rework requests? Supervisors only, or any qualified operator?

5. **Historical Data**: Should existing ProductionRecords be backfilled with ProcessSegment links?

---

## 8. References

- **Source Document**: ISA-95 Production Model FRE Site - Inner Shell Line (ISL)
- **ISA-95 Standard**: ANSI/ISA-95 (IEC 62264) Enterprise-Control System Integration
- **B2MML**: Business to Manufacturing Markup Language (ISA-95 XML schema)
- **Related Specs**: SPEC_DATA_REFERENCE.md, SPEC_WORKFLOW_ENGINE_CORE.md, SPEC_NCR_SYSTEM.md
