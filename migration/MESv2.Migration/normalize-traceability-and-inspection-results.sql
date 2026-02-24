-- Normalize migrated traceability and inspection result shape.
-- Safe to run multiple times.
BEGIN TRANSACTION;

-- 1) Backfill text inspection result when only numeric exists.
UPDATE ir
SET ir.ResultText = CASE
    WHEN ir.ResultNumeric > 0 THEN 'Accept'
    ELSE 'Reject'
END
FROM InspectionRecords ir
WHERE (ir.ResultText IS NULL OR LTRIM(RTRIM(ir.ResultText)) = '')
  AND ir.ResultNumeric IS NOT NULL;

-- 2) Canonicalize plate<->shell links to plate -> shell with relationship 'plate'.
;WITH TypeMap AS (
    SELECT
        sn.Id AS SerialNumberId,
        LOWER(LTRIM(RTRIM(pt.SystemTypeName))) AS SystemTypeName
    FROM SerialNumbers sn
    INNER JOIN Products p ON p.Id = sn.ProductId
    INNER JOIN ProductTypes pt ON pt.Id = p.ProductTypeId
)
UPDATE tl
SET
    tl.FromSerialNumberId = v.NewFromId,
    tl.ToSerialNumberId = v.NewToId,
    tl.Relationship = 'plate'
FROM TraceabilityLogs tl
INNER JOIN TypeMap tf ON tf.SerialNumberId = tl.FromSerialNumberId
INNER JOIN TypeMap tt ON tt.SerialNumberId = tl.ToSerialNumberId
CROSS APPLY (
    VALUES (
        CASE WHEN tf.SystemTypeName = 'plate' AND tt.SystemTypeName = 'shell' THEN tl.FromSerialNumberId ELSE tl.ToSerialNumberId END,
        CASE WHEN tf.SystemTypeName = 'plate' AND tt.SystemTypeName = 'shell' THEN tl.ToSerialNumberId ELSE tl.FromSerialNumberId END
    )
) v(NewFromId, NewToId)
WHERE (tf.SystemTypeName = 'plate' AND tt.SystemTypeName = 'shell')
   OR (tf.SystemTypeName = 'shell' AND tt.SystemTypeName = 'plate');

-- 3) Remove invalid "plate" links that do not originate from a plate serial.
;WITH TypeMap AS (
    SELECT
        sn.Id AS SerialNumberId,
        LOWER(LTRIM(RTRIM(pt.SystemTypeName))) AS SystemTypeName
    FROM SerialNumbers sn
    INNER JOIN Products p ON p.Id = sn.ProductId
    INNER JOIN ProductTypes pt ON pt.Id = p.ProductTypeId
)
DELETE tl
FROM TraceabilityLogs tl
LEFT JOIN TypeMap tf ON tf.SerialNumberId = tl.FromSerialNumberId
WHERE tl.Relationship = 'plate'
  AND (tl.FromSerialNumberId IS NULL OR tf.SystemTypeName <> 'plate');

COMMIT TRANSACTION;
