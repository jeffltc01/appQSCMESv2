#!/usr/bin/env python3
import json
import os
import sys
from pathlib import Path


def read_json(path_value: str):
    path = Path(path_value)
    if not path.exists():
        raise FileNotFoundError(f"Missing required artifact: {path}")
    with path.open("r", encoding="utf-8") as handle:
        return json.load(handle)


def fail(message: str):
    print(message, file=sys.stderr)
    sys.exit(1)


def main():
    perf_path = os.environ.get("SLO_PERF_SUMMARY_PATH", "perf/artifacts/perf-summary.json")
    reconciliation_path = os.environ.get(
        "SLO_RECON_SUMMARY_PATH",
        "perf/artifacts/queue-reconciliation-summary.json",
    )
    p95_threshold_ms = float(os.environ.get("SLO_P95_THRESHOLD_MS", "1200"))
    max_error_rate_pct = float(os.environ.get("SLO_MAX_ERROR_RATE_PCT", "1.0"))
    work_center_read_p95_ms = float(os.environ.get("SLO_WC_READ_P95_THRESHOLD_MS", "900"))
    supervisor_read_p95_ms = float(os.environ.get("SLO_SUPERVISOR_P95_THRESHOLD_MS", "2500"))
    login_p95_ms = float(os.environ.get("SLO_LOGIN_P95_THRESHOLD_MS", str(p95_threshold_ms)))
    require_extended_categories = os.environ.get("SLO_REQUIRE_EXTENDED_CATEGORIES", "false").lower() == "true"

    perf = read_json(perf_path)
    reconciliation = read_json(reconciliation_path)

    scenarios = perf.get("scenarios", [])
    if not scenarios:
        fail("SLO gate failed: perf summary has no scenarios.")

    failed_reasons = []
    category_seen = {
        "login": False,
        "work_center_read": False,
        "supervisor_dashboard_read": False,
    }
    for scenario in scenarios:
        name = scenario.get("name", "unknown")
        latency = scenario.get("latency", {})
        totals = scenario.get("totals", {})
        p95_ms = float(latency.get("p95Ms", 0))
        error_rate = float(totals.get("errorRatePct", 0))

        if name.startswith("wc_"):
            category_seen["work_center_read"] = True
            threshold_ms = work_center_read_p95_ms
        elif name.startswith("supervisor_"):
            category_seen["supervisor_dashboard_read"] = True
            threshold_ms = supervisor_read_p95_ms
        elif name.startswith("login_") or "sessions" in name or "burst" in name:
            category_seen["login"] = True
            threshold_ms = login_p95_ms
        else:
            threshold_ms = p95_threshold_ms

        if p95_ms > threshold_ms:
            failed_reasons.append(
                f"{name}: p95 {p95_ms}ms exceeds threshold {threshold_ms}ms"
            )
        if error_rate > max_error_rate_pct:
            failed_reasons.append(
                f"{name}: error rate {error_rate}% exceeds threshold {max_error_rate_pct}%"
            )

    if require_extended_categories:
        for category, seen in category_seen.items():
            if not seen:
                failed_reasons.append(f"Missing required perf scenario category: {category}")

    recon_totals = reconciliation.get("totals", {})
    lost_count = int(recon_totals.get("lostCount", 0))
    duplicate_count = int(recon_totals.get("duplicateCount", 0))
    unexpected_count = int(recon_totals.get("unexpectedCount", 0))

    if lost_count > 0:
        failed_reasons.append(f"Reconciliation: lostCount={lost_count}")
    if duplicate_count > 0:
        failed_reasons.append(f"Reconciliation: duplicateCount={duplicate_count}")
    if unexpected_count > 0:
        failed_reasons.append(f"Reconciliation: unexpectedCount={unexpected_count}")

    if failed_reasons:
        fail("SLO gate failed:\n- " + "\n- ".join(failed_reasons))

    print("SLO gate passed.")
    print(f"Perf summary: {perf_path}")
    print(f"Reconciliation summary: {reconciliation_path}")


if __name__ == "__main__":
    main()
