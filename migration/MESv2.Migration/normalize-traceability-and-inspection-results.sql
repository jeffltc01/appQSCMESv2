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

-- 2) Resolve trace links that were migrated with textual tank location only.
;WITH LatestSerialByText AS (
    SELECT
        sn.Id,
        sn.Serial,
        ROW_NUMBER() OVER (
            PARTITION BY sn.Serial
            ORDER BY sn.CreatedAt DESC, sn.Id DESC
        ) AS rn
    FROM SerialNumbers sn
)
UPDATE tl
SET tl.FromSerialNumberId = l.Id
FROM TraceabilityLogs tl
INNER JOIN LatestSerialByText l
    ON l.Serial = LTRIM(RTRIM(tl.TankLocation))
   AND l.rn = 1
WHERE tl.FromSerialNumberId IS NULL
  AND tl.ToSerialNumberId IS NOT NULL
  AND tl.TankLocation IS NOT NULL
  AND LTRIM(RTRIM(tl.TankLocation)) <> '';

-- 3) Canonicalize plate<->shell links to plate -> shell with relationship 'plate'.
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

COMMIT TRANSACTION;
