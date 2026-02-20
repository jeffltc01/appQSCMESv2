# MES v2 — Products & Product Types Reference Data

## 1. Overview

Products are central to the MES data model. Every serial number, production record, and material queue entry references a Product. Products are grouped into **Product Types**, which represent the stage of manufacturing.

### 1.1 Product Type → Work Center Mapping

| Product Type | Description | Used At |
|---|---|---|
| **Plate** | Steel plate rolled into shells | Rolls Material (queue), Rolls (active material) |
| **Head** | Purchased heads combined with shells | Fitup Queue (queue), Fitup (assembly) |
| **Shell** | The rolled cylindrical shell | Rolls (production record), Long Seam, Long Seam Inspection |
| **Assembled Tank** | Assembly created at Fitup (shells + heads) | Fitup, Round Seam, Round Seam Inspection, Spot X-ray |
| **Sellable Tank** | Completed tank finished at Hydro | Nameplate (dropdown selection), Hydro |
| **Plasma** | Plasma work center products (hole patterns) | Plasma (not currently active — carry forward for future use) |

---

## 2. Product Type Entity

| Field | Type | Description |
|---|---|---|
| **ProductTypeId** | int (PK) | Unique identifier |
| **Name** | string | e.g., "Plate", "Head", "Shell", "Assembled Tank", "Sellable Tank", "Plasma" |
| **Description** | string | Brief description of what this type represents |
| **IsActive** | bool | Whether this product type is currently in use |

---

## 3. Product Entity

| Field | Type | Description |
|---|---|---|
| **ProductId** | int (PK) | Unique identifier |
| **ProductTypeId** | int (FK) | References ProductType |
| **Description** | string | Product description (e.g., "PL .218NOM X 83.00 X 116.6875") |
| **TankSize** | int | Tank size in gallons (120, 250, 320, 500, 1000, 1450, 1990) |
| **TankType** | string (nullable) | Only for Sellable Tank and Plasma — AG, UG, AG/UG, CAL, etc. |
| **IsActive** | bool | Whether this product is currently available |

### 3.1 Product–Site Availability

Not all products are available at all plants. A junction table controls which products are available at which site.

| Field | Type | Description |
|---|---|---|
| **ProductId** | int (FK) | References Product |
| **SiteCode** | string (FK) | References Plant (000, 600, 700) |

When an operator at a given plant opens a product selector (e.g., Rolls Material, Fitup Queue, Nameplate), the list is filtered to only show products available at their logged-in plant.

---

## 4. Plants

| Site Code | Plant Name |
|---|---|
| **000** | Cleveland |
| **600** | Fremont |
| **700** | West Jordan |

---

## 5. Tank Sizes

| Size (gallons) | Plants |
|---|---|
| **120** | Cleveland (000), Fremont (600), West Jordan (700) |
| **250** | Cleveland (000), Fremont (600), West Jordan (700) |
| **320** | Cleveland (000), Fremont (600), West Jordan (700) |
| **500** | Cleveland (000), Fremont (600), West Jordan (700) |
| **1000** | Cleveland (000), Fremont (600), West Jordan (700) |
| **1450** | Fremont (600) |
| **1990** | Fremont (600) |

---

## 6. Tank Types (Sellable Tank / Plasma)

| Code | Meaning |
|---|---|
| **AG** | Above Ground |
| **UG** | Under Ground |
| **AG/UG** | Above Ground / Under Ground |
| **CAL** | California (compliance) |
| **STANDARD** | Standard (Plasma only) |
| **STANDARD wDRAIN** | Standard with drain (Plasma only) |
| **EDOME** | E-Dome (Plasma only) |

---

## 7. Product Data by Type

### 7.1 Plate Products

| Description | Tank Size | Sites |
|---|---|---|
| PL .140NOM X 54.00 X 74.625 | 120 | 000, 600, 700 |
| PL .175NOM X 63.25 X 93.375 | 250 | 000, 600, 700 |
| PL .175NOM X 87.00 X 93.375 | 320 | 000, 600, 700 |
| PL .239NOM X 64.25 X 127.5675 | 500 | 600 |
| PL .218NOM X 83.00 X 116.6875 | 500 | 000, 600, 700 |
| PLT 1000 .239 Nom x 75.75 x 12 | 1000 | 000 |
| PL .242NOM X 75.75 X 127.375 | 1000 | 600 |
| PL .239NOM X 75.75 X 127.5675 | 1000 | 600, 700 |
| PL .3125NOM X 96.00 X 143.625 | 1450 | 600 |
| PL .272NOM X 92.00 X 145.6875 | 1450 | 600 |
| PL .375NOM X 82.75 X 150.25 | 1990 | 600 |
| PL .272NOM X 88.00 X 145.6875 | 1990 | 600 |

### 7.2 Head Products

| Description | Tank Size | Sites |
|---|---|---|
| ELLIP 24" OD | 120 | 000, 600, 700 |
| HEMI 30" OD | 250 | 000, 600, 700 |
| HEMI 30" OD | 320 | 000, 600, 700 |
| HEMI 37" ID | 500 | 000, 600, 700 |
| HEMI 40.5" ID | 1000 | 000, 600, 700 |
| ELLIP - 46" ID | 1450 | 600 |
| 48" OD | 1990 | 600 |

### 7.3 Shell Products

| Description | Tank Size | Sites |
|---|---|---|
| 120 gal | 120 | 000, 600, 700 |
| 250 gal | 250 | 000, 600, 700 |
| 320 gal | 320 | 000, 600, 700 |
| 500 gal | 500 | 000, 600, 700 |
| 1000 gal | 1000 | 000, 600, 700 |
| 1450 gal | 1450 | 600 |
| 1990 gal | 1990 | 600 |

### 7.4 Assembled Tank Products

| Description | Tank Size | Sites |
|---|---|---|
| 120 Alpha Code - Assembled Tank | 120 | 000, 600, 700 |
| 250 Alpha Code - Assembled Tank | 250 | 000, 600, 700 |
| 320 Alpha Code - Assembled Tank | 320 | 000, 600, 700 |
| 500 Alpha Code - Assembled Tank | 500 | 000 |
| 1000 Alpha Code - Assembled Tank | 1000 | 000 |
| 1450 Alpha Code - Assembled Tank | 1450 | 600 |
| 1990 Alpha Code - Assembled Tank | 1990 | 000 |

### 7.5 Sellable Tank Products

| Description | Tank Size | Tank Type | Sites |
|---|---|---|---|
| 120 AG/UG | 120 | AG/UG | 000, 600, 700 |
| 120 CAL | 120 | CAL | 000, 600, 700 |
| 120 UG | 120 | UG | 000, 600, 700 |
| 120 AG | 120 | AG | 000, 600, 700 |
| 250 CAL | 250 | CAL | 000, 600, 700 |
| 250 AG/UG | 250 | AG/UG | 000, 600, 700 |
| 250 AG | 250 | AG | 000, 600, 700 |
| 250 UG | 250 | UG | 000, 600, 700 |
| 320 AG/UG | 320 | AG/UG | 000, 600, 700 |
| 320 UG | 320 | UG | 000, 600, 700 |
| 320 AG | 320 | AG | 000, 600, 700 |
| 320 CAL | 320 | CAL | 000, 600, 700 |
| 500 AG | 500 | AG | 000, 600, 700 |
| 500 UG | 500 | UG | 000, 600, 700 |
| 500 CAL | 500 | CAL | 000, 600, 700 |
| 500 AG/UG | 500 | AG/UG | 000, 600, 700 |
| 1000 AG/UG | 1000 | AG/UG | 000, 600, 700 |
| 1000 AG | 1000 | AG | 000, 600, 700 |
| 1000 UG | 1000 | UG | 000, 600, 700 |
| 1000 CAL | 1000 | CAL | 000, 600, 700 |
| 1450 AG | 1450 | AG | 600 |
| 1450 UG | 1450 | UG | 600 |
| 1450 AG/UG | 1450 | AG/UG | 600 |
| 1990 AG/UG | 1990 | AG/UG | 600 |
| 1990 AG | 1990 | AG | 600 |
| 1990 UG | 1990 | UG | 600 |

### 7.6 Plasma Products

> **Note**: Plasma work center is not currently active. Products are carried forward for future use.

| Description | Tank Size | Tank Type | Sites |
|---|---|---|---|
| 91230 | 120 | UG | 000, 600, 700 |
| 91270 | 120 | EDOME | 000, 600, 700 |
| 9122X | 120 | AG/UG | 000, 600, 700 |
| 9121X | 120 | STANDARD | 000, 600, 700 |
| 9120X | 120 | STANDARD wDRAIN | 000, 600, 700 |
| 9125X | 120 | CAL | 000, 600, 700 |
| 92570 | 250 | EDOME | 000, 600, 700 |
| 9251X | 250 | STANDARD | 000, 600, 700 |
| 9252X | 250 | AG/UG | 000, 600, 700 |
| 9255X | 250 | CAL | 000, 600, 700 |
| 92530 | 250 | UG | 000, 600, 700 |
| 9250X | 250 | STANDARD wDRAIN | 000, 600, 700 |
| 9321X | 320 | STANDARD | 000, 600, 700 |
| 9325X | 320 | CAL | 000, 600, 700 |
| 93230 | 320 | UG | 000, 600, 700 |
| 9322X | 320 | AG/UG | 000, 600, 700 |
| 9320X | 320 | STANDARD wDRAIN | 000, 600, 700 |
| 93270 | 320 | EDOME | 000, 600, 700 |
| 95030 | 500 | UG | 000, 600, 700 |
| 9500X | 500 | STANDARD wDRAIN | 000, 600, 700 |
| 9496X | 500 | CAL | 000, 600, 700 |
| 95070 | 500 | EDOME | 000, 600, 700 |
| 9502X | 500 | AG/UG | 000, 600, 700 |
| 9501X | 500 | STANDARD | 000, 600, 700 |
| 91002X | 1000 | AG/UG | 000, 600, 700 |
| 910030 | 1000 | UG | 000, 600, 700 |
| 910070 | 1000 | EDOME | 000, 600, 700 |
| 91000X | 1000 | STANDARD | 000, 600, 700 |
| 91006X | 1000 | CAL | 000, 600, 700 |

---

## 8. Key Design Decisions

| Decision | Resolution | Rationale |
|---|---|---|
| **Product Type as a grouping** | Products are categorized by manufacturing stage (Plate → Head → Shell → Assembled → Sellable) | Matches the physical flow of manufacturing; each stage has a different product type |
| **Site-filtered product lists** | Product selectors filter by the operator's logged-in plant | Not all plants make all tank sizes; prevents operators from selecting products they don't produce |
| **Plasma carried forward** | Plasma product type and products are included but marked inactive | Future use when the plasma work center is brought online |
| **TankType only on Sellable and Plasma** | Other product types don't need AG/UG/CAL distinction | The tank's end-use (above ground, underground, California) is only determined at the Nameplate/Hydro stage |
| **Relatively stable data** | Managed via admin screen, not frequently changed | New products are rare; admin-level access for additions |

---

## References

| Document | Relevance |
|---|---|
| [Rework Ratio Phase II Production Data Flow - Data Organization.pdf](Rework%20Ratio%20Phase%20II%20Production%20Data%20Flow%20-%20Data%20Organization.pdf) | Source data for product and product type lists |
| [GENERAL_DESIGN_INPUT.md](GENERAL_DESIGN_INPUT.md) | Data model — Product, ProductType entities |
| [SPEC_WC_ROLLS_MATERIAL.md](SPEC_WC_ROLLS_MATERIAL.md) | Uses Plate products |
| [SPEC_WC_FITUP_QUEUE.md](SPEC_WC_FITUP_QUEUE.md) | Uses Head products |
| [SPEC_WC_NAMEPLATE.md](SPEC_WC_NAMEPLATE.md) | Uses Sellable Tank products (Tank Size / Type dropdown) |
