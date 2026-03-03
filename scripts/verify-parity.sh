#!/usr/bin/env bash
set -euo pipefail

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$REPO_ROOT"

require_env() {
  local var_name="$1"
  if [[ -z "${!var_name:-}" ]]; then
    echo "Missing required value: ${var_name}" >&2
    exit 1
  fi
}

require_url() {
  local var_name="$1"
  local url="${!var_name}"
  if [[ ! "$url" =~ ^https?:// ]]; then
    echo "${var_name} must start with http:// or https:// (got '${url}')" >&2
    exit 1
  fi
  if [[ "${url}" == */ ]]; then
    echo "${var_name} should not end with '/' (got '${url}')" >&2
    exit 1
  fi
}

echo "Running full local parity chain (CI-like)..."

echo "[1/5] Validate verify environment inputs"
for name in BACKEND_URL FRONTEND_URL SMOKE_ADMIN_EMP_NO SMOKE_OPERATOR_EMP_NO SMOKE_SUPERVISOR_EMP_NO PERF_EMP_NO PERF_PIN PERF_SITE_ID; do
  require_env "$name"
done
require_url "BACKEND_URL"
require_url "FRONTEND_URL"

echo "[2/5] Build + test parity preflight"
bash "$REPO_ROOT/scripts/preflight-dev.sh" --tier parity

echo "[3/5] Install Playwright dependencies"
cd "$REPO_ROOT/e2e"
npm ci
npx playwright install --with-deps chromium

echo "[4/5] Run operator smoke E2E tests"
DEPLOY_SMOKE=true \
  npm run test:operator-smoke

echo "[5/5] Run SLO perf + queue reconciliation"
cd "$REPO_ROOT"
node perf/run-slo-scenarios.mjs
node perf/reconcile-queue-results.mjs

echo "Local parity chain passed."
