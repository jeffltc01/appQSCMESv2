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

## Notes

- Route matching is explicit and template-based via `EndpointSloCatalog` (no text matching filters).
- `PlantId` is inferred from route/query (`plantId`, `siteId`) when present.
- `WorkCenterId` is inferred from route values (`id`, `wcId`) for work-center endpoints.
