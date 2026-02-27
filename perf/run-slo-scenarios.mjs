#!/usr/bin/env node

import fs from 'node:fs/promises';
import path from 'node:path';

const backendUrl = (process.env.BACKEND_URL ?? '').replace(/\/+$/, '');
const empNo = process.env.PERF_EMP_NO ?? 'EMP001';
const pin = process.env.PERF_PIN ?? '1234';
const siteId = process.env.PERF_SITE_ID ?? '';
const workCenterId = process.env.PERF_WC_ID ?? '';
const outDir = path.resolve(process.env.PERF_OUT_DIR ?? 'perf/artifacts');
const requestTimeoutMs = Number(process.env.PERF_REQUEST_TIMEOUT_MS ?? 15_000);

if (!backendUrl) {
  console.error('Missing BACKEND_URL');
  process.exit(1);
}

function pct(sorted, percentile) {
  if (!sorted.length) return 0;
  const idx = Math.min(sorted.length - 1, Math.max(0, Math.ceil((percentile / 100) * sorted.length) - 1));
  return sorted[idx];
}

function summarizeDurations(durationsMs) {
  const sorted = [...durationsMs].sort((a, b) => a - b);
  return {
    count: sorted.length,
    minMs: sorted[0] ?? 0,
    maxMs: sorted[sorted.length - 1] ?? 0,
    p50Ms: pct(sorted, 50),
    p95Ms: pct(sorted, 95),
    p99Ms: pct(sorted, 99),
    avgMs: sorted.length ? Math.round((sorted.reduce((sum, ms) => sum + ms, 0) / sorted.length) * 100) / 100 : 0,
  };
}

async function timedFetch(relativePath, options = {}) {
  const startedAt = performance.now();
  const controller = new AbortController();
  const timeoutHandle = setTimeout(() => controller.abort(), requestTimeoutMs);
  try {
    const response = await fetch(`${backendUrl}${relativePath}`, {
      ...options,
      signal: controller.signal,
      headers: {
        'Content-Type': 'application/json',
        ...(options.headers ?? {}),
      },
    });
    const elapsedMs = Math.round(performance.now() - startedAt);
    return { ok: response.ok, status: response.status, elapsedMs };
  } catch {
    const elapsedMs = Math.round(performance.now() - startedAt);
    return { ok: false, status: 0, elapsedMs };
  } finally {
    clearTimeout(timeoutHandle);
  }
}

async function runWorker(deadlineMs, executeRequest, collector) {
  while (Date.now() < deadlineMs) {
    const result = await executeRequest();
    collector.push(result);
  }
}

async function runScenario(name, { durationSec, concurrency, executeRequest }) {
  const deadlineMs = Date.now() + durationSec * 1000;
  const results = [];
  const workers = Array.from({ length: concurrency }, () =>
    runWorker(deadlineMs, executeRequest, results));
  await Promise.all(workers);

  const durations = results.map((r) => r.elapsedMs);
  const errors = results.filter((r) => !r.ok).length;
  const byStatus = Object.create(null);
  for (const result of results) {
    const key = String(result.status);
    byStatus[key] = (byStatus[key] ?? 0) + 1;
  }

  return {
    name,
    durationSec,
    concurrency,
    totals: {
      requests: results.length,
      errors,
      errorRatePct: results.length ? Math.round((errors / results.length) * 10000) / 100 : 0,
      statusCounts: byStatus,
    },
    latency: summarizeDurations(durations),
  };
}

function utcDateIso() {
  const now = new Date();
  const month = `${now.getUTCMonth() + 1}`.padStart(2, '0');
  const day = `${now.getUTCDate()}`.padStart(2, '0');
  return `${now.getUTCFullYear()}-${month}-${day}`;
}

async function authenticate() {
  const loginResponse = await fetch(`${backendUrl}/api/auth/login`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({
      employeeNumber: empNo,
      pin,
      siteId: siteId || null,
      isWelder: false,
    }),
  });

  if (!loginResponse.ok) {
    throw new Error(`Auth failed with status ${loginResponse.status}`);
  }

  const payload = await loginResponse.json();
  const token = payload?.token;
  if (!token) {
    throw new Error('Auth response missing token');
  }
  return {
    token,
    roleTier: Number(payload?.user?.roleTier ?? process.env.PERF_ROLE_TIER ?? 5),
    effectiveSiteId: siteId || payload?.user?.defaultSiteId || '',
  };
}

async function main() {
  const scenarios = [];
  const auth = await authenticate();
  const authHeaders = {
    Authorization: `Bearer ${auth.token}`,
    'X-User-Role-Tier': String(auth.roleTier),
    ...(auth.effectiveSiteId ? { 'X-User-Site-Id': auth.effectiveSiteId } : {}),
  };
  const dateIso = utcDateIso();

  scenarios.push(await runScenario('login_config_baseline', {
    durationSec: 30,
    concurrency: 10,
    executeRequest: () => timedFetch(`/api/users/login-config?empNo=${encodeURIComponent(empNo)}`, { method: 'GET' }),
  }));

  scenarios.push(await runScenario('login_submit_baseline', {
    durationSec: 30,
    concurrency: 8,
    executeRequest: () => timedFetch('/api/auth/login', {
      method: 'POST',
      body: JSON.stringify({
        employeeNumber: empNo,
        pin,
        siteId: siteId || null,
        isWelder: false,
      }),
    }),
  }));

  scenarios.push(await runScenario('concurrency_50_sessions', {
    durationSec: 30,
    concurrency: 50,
    executeRequest: () => timedFetch(`/api/users/login-config?empNo=${encodeURIComponent(empNo)}`, { method: 'GET' }),
  }));

  scenarios.push(await runScenario('burst_5x_30s', {
    durationSec: 30,
    concurrency: 25,
    executeRequest: () => timedFetch(`/api/users/login-config?empNo=${encodeURIComponent(empNo)}`, { method: 'GET' }),
  }));

  if (workCenterId && auth.effectiveSiteId) {
    scenarios.push(await runScenario('wc_history_read_baseline', {
      durationSec: 30,
      concurrency: 12,
      executeRequest: () => timedFetch(
        `/api/workcenters/${encodeURIComponent(workCenterId)}/history?plantId=${encodeURIComponent(auth.effectiveSiteId)}&date=&limit=5`,
        { method: 'GET', headers: authHeaders },
      ),
    }));

    scenarios.push(await runScenario('wc_material_queue_read_baseline', {
      durationSec: 30,
      concurrency: 12,
      executeRequest: () => timedFetch(
        `/api/workcenters/${encodeURIComponent(workCenterId)}/material-queue`,
        { method: 'GET', headers: authHeaders },
      ),
    }));

    scenarios.push(await runScenario('supervisor_metrics_baseline', {
      durationSec: 30,
      concurrency: 8,
      executeRequest: () => timedFetch(
        `/api/supervisor-dashboard/${encodeURIComponent(workCenterId)}/metrics?plantId=${encodeURIComponent(auth.effectiveSiteId)}&date=${encodeURIComponent(dateIso)}`,
        { method: 'GET', headers: authHeaders },
      ),
    }));

    scenarios.push(await runScenario('supervisor_performance_table_baseline', {
      durationSec: 30,
      concurrency: 8,
      executeRequest: () => timedFetch(
        `/api/supervisor-dashboard/${encodeURIComponent(workCenterId)}/performance-table?plantId=${encodeURIComponent(auth.effectiveSiteId)}&date=${encodeURIComponent(dateIso)}&view=day`,
        { method: 'GET', headers: authHeaders },
      ),
    }));
  }

  const summary = {
    generatedAtUtc: new Date().toISOString(),
    backendUrl,
    requestTimeoutMs,
    workCenterId: workCenterId || null,
    siteId: auth.effectiveSiteId || null,
    scenarios,
  };

  await fs.mkdir(outDir, { recursive: true });
  const summaryPath = path.join(outDir, 'perf-summary.json');
  await fs.writeFile(summaryPath, `${JSON.stringify(summary, null, 2)}\n`, 'utf8');

  const markdownLines = [
    '# SLO Perf Summary',
    '',
    `Generated: ${summary.generatedAtUtc}`,
    `Backend: ${backendUrl}`,
    '',
  ];

  for (const scenario of scenarios) {
    markdownLines.push(`## ${scenario.name}`);
    markdownLines.push(`- Duration: ${scenario.durationSec}s`);
    markdownLines.push(`- Concurrency: ${scenario.concurrency}`);
    markdownLines.push(`- Requests: ${scenario.totals.requests}`);
    markdownLines.push(`- Error rate: ${scenario.totals.errorRatePct}%`);
    markdownLines.push(`- p50/p95/p99: ${scenario.latency.p50Ms}ms / ${scenario.latency.p95Ms}ms / ${scenario.latency.p99Ms}ms`);
    markdownLines.push('');
  }

  await fs.writeFile(path.join(outDir, 'perf-summary.md'), `${markdownLines.join('\n')}\n`, 'utf8');
  console.log(`Wrote ${summaryPath}`);
}

await main();
