#!/usr/bin/env node

import fs from 'node:fs/promises';
import path from 'node:path';

const outDir = path.resolve(process.env.PERF_OUT_DIR ?? 'perf/artifacts');
const attemptedPath = path.resolve(process.env.RECON_ATTEMPTED_PATH ?? 'perf/input/attempted-queue-submissions.json');
const persistedPath = path.resolve(process.env.RECON_PERSISTED_PATH ?? 'perf/input/persisted-queue-submissions.json');

function keyOf(item) {
  const workCenterId = item.workCenterId ?? '';
  const serialNumber = item.serialNumber ?? '';
  const action = item.action ?? '';
  const clientRequestId = item.clientRequestId ?? '';
  return `${workCenterId}|${serialNumber}|${action}|${clientRequestId}`;
}

async function readJsonArray(filePath) {
  const raw = await fs.readFile(filePath, 'utf8');
  const parsed = JSON.parse(raw);
  return Array.isArray(parsed) ? parsed : [];
}

async function main() {
  const attempted = await readJsonArray(attemptedPath);
  const persisted = await readJsonArray(persistedPath);

  const attemptedCountByKey = new Map();
  const persistedCountByKey = new Map();

  for (const row of attempted) {
    const key = keyOf(row);
    attemptedCountByKey.set(key, (attemptedCountByKey.get(key) ?? 0) + 1);
  }

  for (const row of persisted) {
    const key = keyOf(row);
    persistedCountByKey.set(key, (persistedCountByKey.get(key) ?? 0) + 1);
  }

  const lost = [];
  const duplicates = [];
  const unexpected = [];

  for (const [key, attemptedCount] of attemptedCountByKey.entries()) {
    const persistedCount = persistedCountByKey.get(key) ?? 0;
    if (persistedCount === 0) {
      lost.push({ key, attemptedCount, persistedCount });
      continue;
    }

    if (persistedCount > attemptedCount) {
      duplicates.push({ key, attemptedCount, persistedCount, extra: persistedCount - attemptedCount });
    }
  }

  for (const [key, persistedCount] of persistedCountByKey.entries()) {
    if (!attemptedCountByKey.has(key)) {
      unexpected.push({ key, persistedCount });
    }
  }

  const summary = {
    generatedAtUtc: new Date().toISOString(),
    totals: {
      attemptedRows: attempted.length,
      persistedRows: persisted.length,
      attemptedKeys: attemptedCountByKey.size,
      persistedKeys: persistedCountByKey.size,
      lostCount: lost.length,
      duplicateCount: duplicates.length,
      unexpectedCount: unexpected.length,
    },
    details: {
      lost,
      duplicates,
      unexpected,
    },
  };

  await fs.mkdir(outDir, { recursive: true });
  const jsonPath = path.join(outDir, 'queue-reconciliation-summary.json');
  await fs.writeFile(jsonPath, `${JSON.stringify(summary, null, 2)}\n`, 'utf8');

  const markdown = [
    '# Queue Reconciliation Summary',
    '',
    `Generated: ${summary.generatedAtUtc}`,
    '',
    `- Attempted rows: ${summary.totals.attemptedRows}`,
    `- Persisted rows: ${summary.totals.persistedRows}`,
    `- Lost transaction keys: ${summary.totals.lostCount}`,
    `- Duplicate transaction keys: ${summary.totals.duplicateCount}`,
    `- Unexpected persisted keys: ${summary.totals.unexpectedCount}`,
    '',
    '## Inputs',
    '',
    `- Attempted: ${attemptedPath}`,
    `- Persisted: ${persistedPath}`,
    '',
  ].join('\n');
  await fs.writeFile(path.join(outDir, 'queue-reconciliation-summary.md'), `${markdown}\n`, 'utf8');

  console.log(`Wrote ${jsonPath}`);
}

await main();
