#!/usr/bin/env bash
set -euo pipefail

require_env() {
  local var_name="$1"
  if [[ -z "${!var_name:-}" ]]; then
    echo "Missing required value: ${var_name}" >&2
    exit 1
  fi
}

require_env "BACKEND_URL"
require_env "FRONTEND_URL"
require_env "SMOKE_EMP_NO"

BACKEND_URL="${BACKEND_URL%/}"
FRONTEND_URL="${FRONTEND_URL%/}"

echo "Smoke check: backend health endpoint"
for attempt in {1..10}; do
  if curl -fsS "${BACKEND_URL}/healthz" > /dev/null; then
    break
  fi
  if [[ "$attempt" -eq 10 ]]; then
    echo "Backend /healthz did not become healthy." >&2
    exit 1
  fi
  sleep 10
done

echo "Smoke check: frontend shell"
for attempt in {1..10}; do
  if curl -fsS "${FRONTEND_URL}" > /dev/null; then
    break
  fi
  if [[ "$attempt" -eq 10 ]]; then
    echo "Frontend did not become reachable." >&2
    exit 1
  fi
  sleep 10
done

echo "Smoke check: login-config API"
LOGIN_CONFIG_JSON="$(curl -fsS "${BACKEND_URL}/api/users/login-config?empNo=${SMOKE_EMP_NO}")"
SITE_ID="$(python3 -c "import json,sys; print(json.loads(sys.stdin.read()).get('defaultSiteId',''))" <<< "$LOGIN_CONFIG_JSON")"
if [[ -z "$SITE_ID" ]]; then
  echo "Smoke login-config response missing defaultSiteId." >&2
  exit 1
fi

echo "Smoke check: auth login API"
PIN_VALUE="null"
if [[ -n "${SMOKE_EMP_PIN:-}" ]]; then
  PIN_VALUE="\"${SMOKE_EMP_PIN}\""
fi

LOGIN_PAYLOAD="$(cat <<EOF
{
  "employeeNumber": "${SMOKE_EMP_NO}",
  "pin": ${PIN_VALUE},
  "siteId": "${SITE_ID}",
  "isWelder": false
}
EOF
)"

LOGIN_RESPONSE="$(curl -fsS \
  -X POST "${BACKEND_URL}/api/auth/login" \
  -H "Content-Type: application/json" \
  -d "${LOGIN_PAYLOAD}")"

HAS_TOKEN="$(python3 -c "import json,sys; print('yes' if json.loads(sys.stdin.read()).get('token') else '')" <<< "$LOGIN_RESPONSE")"
if [[ "$HAS_TOKEN" != "yes" ]]; then
  echo "Smoke login response missing token." >&2
  exit 1
fi

echo "Post-deploy smoke checks passed."
