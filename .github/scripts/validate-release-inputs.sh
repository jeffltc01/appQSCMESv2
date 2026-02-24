#!/usr/bin/env bash
set -euo pipefail

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
}

require_env "BACKEND_URL"
require_url "BACKEND_URL"

if [[ "${BACKEND_URL}" == */ ]]; then
  echo "BACKEND_URL should not end with '/' (got '${BACKEND_URL}')" >&2
  exit 1
fi

echo "Release input validation passed."
