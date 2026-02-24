-- Normalize ProductTypes.SystemTypeName values from migrated environments.
-- Safe to run multiple times.
BEGIN TRANSACTION;

UPDATE [ProductTypes]
SET [SystemTypeName] = LTRIM(RTRIM(LOWER([SystemTypeName])))
WHERE [SystemTypeName] IS NOT NULL
  AND [SystemTypeName] <> LTRIM(RTRIM(LOWER([SystemTypeName])));

UPDATE [ProductTypes]
SET [SystemTypeName] = 'assembled'
WHERE [SystemTypeName] = 'assembeled';

COMMIT TRANSACTION;
