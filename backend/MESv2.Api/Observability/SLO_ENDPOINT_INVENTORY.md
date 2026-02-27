# SLO Endpoint Inventory

This inventory defines the canonical backend endpoint map used for SLO tagging (`EndpointCategory`, `Feature`, `PlantId`, `WorkCenterId`).

## Login

- `GET /api/users/login-config` -> `EndpointCategory=Login`, `Feature=LoginConfig`
- `POST /api/auth/login` -> `EndpointCategory=Login`, `Feature=LoginSubmit`

## Operator-Critical Writes

- `POST /api/production-records` -> `EndpointCategory=OperatorCriticalWrite`, `Feature=CreateProductionRecord`
- `POST /api/nameplate` -> `EndpointCategory=OperatorCriticalWrite`, `Feature=CreateNameplateRecord`
- `POST /api/workcenters/{id}/material-queue` -> `EndpointCategory=OperatorCriticalWrite`, `Feature=AddMaterialQueueItem`
- `POST /api/workcenters/{id}/fitup-queue` -> `EndpointCategory=OperatorCriticalWrite`, `Feature=AddFitupQueueItem`
- `POST /api/workcenters/{id}/xray-queue` -> `EndpointCategory=OperatorCriticalWrite`, `Feature=AddXrayQueueItem`
- `POST /api/workcenters/{id}/queue/advance` -> `EndpointCategory=OperatorCriticalWrite`, `Feature=AdvanceQueue`

## Work-Center Reads

- `GET /api/workcenters/{id}/history` -> `EndpointCategory=WorkCenterRead`, `Feature=GetWorkCenterHistory`
- `GET /api/workcenters/{id}/material-queue` -> `EndpointCategory=WorkCenterRead`, `Feature=GetMaterialQueue`
- `GET /api/workcenters/{id}/queue-transactions` -> `EndpointCategory=WorkCenterRead`, `Feature=GetQueueTransactions`
- `GET /api/workcenters/{id}/xray-queue` -> `EndpointCategory=WorkCenterRead`, `Feature=GetXrayQueue`
- `GET /api/workcenters/{id}/defect-codes` -> `EndpointCategory=WorkCenterRead`, `Feature=GetDefectCodes`
- `GET /api/workcenters/{id}/defect-locations` -> `EndpointCategory=WorkCenterRead`, `Feature=GetDefectLocations`

## Supervisor Dashboard Reads

- `GET /api/supervisor-dashboard/{wcId}/metrics` -> `EndpointCategory=SupervisorDashboardRead`, `Feature=GetSupervisorMetrics`
- `GET /api/supervisor-dashboard/{wcId}/trends` -> `EndpointCategory=SupervisorDashboardRead`, `Feature=GetSupervisorTrends`
- `GET /api/supervisor-dashboard/{wcId}/records` -> `EndpointCategory=SupervisorDashboardRead`, `Feature=GetSupervisorRecords`
- `GET /api/supervisor-dashboard/{wcId}/performance-table` -> `EndpointCategory=SupervisorDashboardRead`, `Feature=GetSupervisorPerformanceTable`

## Notes

- Route matching is explicit and template-based via `EndpointSloCatalog` (no text matching filters).
- `PlantId` is inferred from route/query (`plantId`, `siteId`) when present.
- `WorkCenterId` is inferred from route values (`id`, `wcId`) for work-center endpoints.
