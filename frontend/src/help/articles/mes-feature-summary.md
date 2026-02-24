# MES Feature Summary

This article summarizes the major feature areas in MES v2 and how they fit together in daily plant operations. It is designed as a quick orientation guide, with deeper details documented in screen-specific help articles.

## Platform Scope

MES v2 supports production and quality workflows across all QSC plants. The system is built for fixed tablets at work centers and emphasizes scan-driven workflows with manual fallback.

- Tracks production events from raw material intake to final hydro acceptance.
- Captures quality inspections, defects, annotations, and repair context.
- Maintains end-to-end traceability across plate, head, shell, assembly, and finished serials.
- Enforces role-based access and plant-aware behavior.

## Core Feature Modules

### 1) Authentication, Session, and Routing

- Employee login with role-aware plant behavior.
- Session enforcement with role-dependent concurrent login rules.
- Tablet-aware routing so operators land directly on assigned work center screens.
- Token-based API access and timed session lifecycle handling.

### 2) Tablet Setup and Work Center Assignment

- Team Lead and above can assign a tablet to a work center, production line, and asset.
- Assignment is cached for kiosk-like usage by operators.
- Setup can be rerun when devices are reassigned.

### 3) Operator Runtime Experience

- Shared operator shell with top bar, bottom bar, and history panel.
- External Input mode for scanner-first operation.
- Manual mode for touch fallback with equivalent actions.
- Full-screen scan feedback overlay (accept/reject) for fast visual confirmation.

### 4) Production Work Centers

The core execution path follows this sequence:

1. Rolls
2. Long Seam
3. Long Seam Inspection
4. RT X-ray (queue handoff support in MES)
5. Fitup
6. Round Seam
7. Round Seam Inspection
8. Spot X-ray
9. Nameplate
10. Hydro

Each work center records manufacturing or inspection events with work-center-specific rules, including welding assignments where required.

### 5) Queue and Material Handling Screens

- **Rolls Material Queue** stages plate material for Rolls.
- **Fitup Queue** stages head material and supports kanban-card workflows.
- **RT X-ray Queue** stages shell units for the external RT workflow.

Queue features reduce operator data-entry burden and preserve upstream traceability details.

### 6) Quality, Defects, and Gate Checks

- Inspection stations capture pass/fail outcomes and defect details.
- Defect entries include code, location, and (where needed) characteristic context.
- Control plans define collection behavior and gate checks.
- Annotation workflows track correction-needed and review items from operator history.

### 7) Admin and Reference Data Management

Admin and supervisor features support:

- Product, vendor, asset, and work center configuration.
- Defect and characteristic reference maintenance.
- Production line/work center behavior tuning.
- Operational oversight tools such as active floor visibility, logs, and dashboards.

### 8) Reporting and Integrations

- NiceLabel Cloud integration for label-print workflows.
- Limble integration fields for maintenance linkage.
- Power BI as downstream analytics/reporting consumer of MES data.
- Entra-based identity integration for corporate and shop-floor user models.

## Work-Center Flow Snapshot

| Stage | Primary Output | Key Trace Link Created |
|---|---|---|
| Rolls Material / Fitup Queue | Raw material serial records | Raw material identity prepared |
| Rolls | Shell event + inspection | Shell linked to plate source |
| Long Seam / Long Seam Insp | Weld + inspection records | Catch-up handling with annotation when needed |
| Fitup | Assembly alpha record | Assembly linked to shell(s) and heads |
| Round Seam / RS Inspection | Seam execution + defect records | Welder/characteristic trace on seams |
| Nameplate | Finished sellable serial record | Finished identity prepared |
| Hydro | Final pass/fail gate | Finished serial linked to assembly |

## Role-Sensitive Capabilities

| Role Tier | Typical Focus |
|---|---|
| 6.0 Operator | Work-center execution, scan/manual production entry |
| 5.0 Team Lead / Quality Tech | Tablet setup, floor coordination, selected admin/quality tools |
| 4.0 Supervisor | Broader operational oversight and supervisory workflows |
| 3.0 Quality/Plant Managers | Reference data management and change visibility |
| 2.0 Directors | Cross-plant quality/operations governance and advanced configuration |
| 1.0 Administrator | Full platform administration |
| 5.5 Authorized Inspector | Restricted read-only inspection visibility |

## Traceability and Data Concepts

MES traceability is built around connected records:

- **SerialNumber** as the central identifier set (material through finished goods).
- **ManufacturingLog** for every production/inspection event.
- **ManufacturingTraceLog** for parent/component lineage links.
- **Inspection and Defect logs** for quality outcomes.
- **Annotations and change logs** for operational context and auditability.

This model allows teams to reconstruct full production history for a unit, including material origin, process steps, inspections, defects, and final disposition.

## When to Use This Article

Use this summary when onboarding users or aligning teams on MES scope. For screen behavior, command details, and field-level definitions, use the specific help articles linked in the Help table of contents.

## Feature Inventory (v1 vs v2)

| # | Feature | In v1 | In v2 |
|---|---|---|---|
| 1 | Employee login with role-based routing | Yes | Yes |
| 3 | Tablet setup (work center + line + asset assignment) | Yes | Yes |
| 4 | Kiosk-style operator interface | Yes | Yes |
| 5 | External Input toggle for scanner-driven mode | Yes | Yes |
| 6 | Manual touch fallback for barcode actions | Partial | Yes |
| 7 | Full-screen scan feedback overlay (accept/reject) | Yes | Yes |
| 9 | Rolls work center workflow | Yes | Yes |
| 10 | Long Seam workflow | Yes | Yes |
| 11 | Long Seam Inspection workflow | Yes | Yes |
| 12 | Fitup workflow (assembly creation / alpha code) | Yes | Yes |
| 13 | Round Seam workflow | Yes | Yes |
| 14 | Round Seam Inspection workflow | Yes | Yes |
| 15 | Spot X-ray workflow | Yes | Yes |
| 16 | Nameplate workflow | Yes | Yes |
| 17 | Hydro workflow and final gate check | Yes | Yes |
| 21 | Defect capture with code/location context | Yes | Yes |
| 22 | Annotation flags and resolution workflow | Yes | Expanded |
| 23 | Traceability chain (material to finished serial) | Yes | Yes |
| 24 | Active sessions / "Who's On the Floor" visibility | No | Yes |
| 25 | Menu and role-gated management screens | Yes | Yes |
| 27 | Frontend telemetry dashboard | No | Yes |
| 28 | Test coverage dashboard | No | Yes |
| 29 | AI Review screen | No | Yes |
| 30 | Audit log screen | No | Yes |
