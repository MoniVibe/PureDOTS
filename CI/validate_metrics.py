#!/usr/bin/env python3
"""
Validates scale test metrics against performance budgets.
Usage: python validate_metrics.py <reports_directory>
"""

import json
import sys
import re
from pathlib import Path

# Performance budgets
BUDGETS = {
    "scenario.space4x.ship_micro.01": {
        "maxTickTimeMs": 16.67,
        "maxMemoryMB": 512,
        "targetFPS": 60
    },
    "scale_baseline_10k": {
        "maxTickTimeMs": 16.67,
        "maxMemoryMB": 512,
        "targetFPS": 60
    },
    "scale_stress_100k": {
        "maxTickTimeMs": 33.33,
        "maxMemoryMB": 2048,
        "targetFPS": 30
    },
    "scale_extreme_1m": {
        "maxTickTimeMs": 100.0,
        "maxMemoryMB": 4096,
        "targetFPS": 10
    }
}

TIER0_SCENARIO_ID = "scenario.space4x.ship_micro.01"
TIER0_EXPECTED_SEED = 4101


def load_report(report_path: Path):
    try:
        with open(report_path, "r", encoding="utf-8") as f:
            return json.load(f)
    except json.JSONDecodeError as e:
        raise ValueError(f"Failed to parse JSON: {e}") from e
    except FileNotFoundError:
        raise ValueError(f"Report file not found: {report_path}")


def build_metrics_map(report: dict) -> dict:
    metrics_map = {}
    for item in report.get("metrics", []):
        if isinstance(item, dict):
            key = item.get("key")
            value = item.get("value")
            if isinstance(key, str) and isinstance(value, (int, float)):
                metrics_map[key] = float(value)
    return metrics_map


def read_metric(report: dict, metrics_map: dict, keys: list, default: float = 0.0) -> float:
    for key in keys:
        value = report.get(key)
        if isinstance(value, (int, float)):
            return float(value)
    for key in keys:
        if key in metrics_map:
            return metrics_map[key]
    return default


def extract_ship_order_sequence(log_path: Path) -> list:
    if not log_path.exists():
        return []

    sequence = []
    pattern = re.compile(r"\[ShipVillageMicro\].*order=[^/]+/([A-Za-z0-9_]+)")
    with open(log_path, "r", encoding="utf-8", errors="replace") as f:
        for line in f:
            match = pattern.search(line)
            if match:
                sequence.append(match.group(1))
    return sequence


def extract_report_digest(report: dict) -> str:
    top_level_keys = [
        "scenarioDigest",
        "determinismDigest",
        "scenarioHash",
        "determinismHash",
        "digest",
        "hash",
    ]
    for key in top_level_keys:
        value = report.get(key)
        if isinstance(value, (str, int, float)) and str(value):
            return str(value)

    metric_keys = {
        "scenario.digest",
        "determinism.digest",
        "scenario.hash",
        "determinism.hash",
        "digest",
        "hash",
    }
    for item in report.get("metrics", []):
        if not isinstance(item, dict):
            continue
        key = item.get("key")
        value = item.get("value")
        if isinstance(key, str) and key in metric_keys and value is not None:
            return str(value)

    return ""


def report_to_log_path(report_path: Path) -> Path:
    name = report_path.name
    if name.endswith("_report.json"):
        return report_path.with_name(name[: -len("_report.json")] + ".log")
    return report_path.with_suffix(".log")


def validate_tier0_determinism(loaded_reports: dict) -> tuple[bool, list, list]:
    errors = []
    warnings = []

    tier0_reports = []
    for report_path, report in loaded_reports.items():
        if report.get("scenarioId") == TIER0_SCENARIO_ID:
            tier0_reports.append(report_path)

    if not tier0_reports:
        return True, errors, warnings

    run1_name = "scenario_ship_micro_01_run1_report.json"
    run2_name = "scenario_ship_micro_01_run2_report.json"
    by_name = {p.name: p for p in tier0_reports}

    if run1_name in by_name and run2_name in by_name:
        selected = [by_name[run1_name], by_name[run2_name]]
    else:
        selected = sorted(tier0_reports, key=lambda p: p.name)[:2]

    if len(selected) < 2:
        errors.append(
            f"Tier-0 determinism requires 2 reports for {TIER0_SCENARIO_ID}, found {len(selected)}"
        )
        return False, errors, warnings

    report_a_path, report_b_path = selected
    report_a = loaded_reports[report_a_path]
    report_b = loaded_reports[report_b_path]
    metrics_a = build_metrics_map(report_a)
    metrics_b = build_metrics_map(report_b)

    seed_a = int(report_a.get("seed", -1))
    seed_b = int(report_b.get("seed", -1))
    if seed_a != seed_b:
        errors.append(f"Tier-0 seed mismatch: {report_a_path.name} seed={seed_a}, {report_b_path.name} seed={seed_b}")
    if seed_a != TIER0_EXPECTED_SEED or seed_b != TIER0_EXPECTED_SEED:
        errors.append(
            f"Tier-0 expected seed {TIER0_EXPECTED_SEED}, got {report_a_path.name}={seed_a}, {report_b_path.name}={seed_b}"
        )

    digest_a = extract_report_digest(report_a)
    digest_b = extract_report_digest(report_b)
    if digest_a and digest_b:
        if digest_a != digest_b:
            errors.append(
                "Tier-0 digest mismatch: "
                f"{report_a_path.name} digest={digest_a} vs {report_b_path.name} digest={digest_b}"
            )
            return False, errors, warnings

        warnings.append(
            f"Tier-0 digest matched: {report_a_path.name} == {report_b_path.name} ({digest_a})"
        )
        return len(errors) == 0, errors, warnings

    events_a = read_metric(report_a, metrics_a, ["ship.micro.events.count"], -1.0)
    events_b = read_metric(report_b, metrics_b, ["ship.micro.events.count"], -1.0)
    readiness_a = round(read_metric(report_a, metrics_a, ["ship.micro.seat.readiness"], -1.0), 4)
    readiness_b = round(read_metric(report_b, metrics_b, ["ship.micro.seat.readiness"], -1.0), 4)
    order_state_a = read_metric(report_a, metrics_a, ["ship.micro.order.state"], -1.0)
    order_state_b = read_metric(report_b, metrics_b, ["ship.micro.order.state"], -1.0)

    if events_a != events_b:
        errors.append(
            "Tier-0 signature mismatch (ship.micro.events.count): "
            f"{report_a_path.name}={events_a}, {report_b_path.name}={events_b}"
        )
    if readiness_a != readiness_b:
        errors.append(
            "Tier-0 signature mismatch (ship.micro.seat.readiness rounded 4dp): "
            f"{report_a_path.name}={readiness_a}, {report_b_path.name}={readiness_b}"
        )

    sequence_a = extract_ship_order_sequence(report_to_log_path(report_a_path))
    sequence_b = extract_ship_order_sequence(report_to_log_path(report_b_path))
    if sequence_a and sequence_b:
        if sequence_a != sequence_b:
            mismatch_index = None
            for i in range(min(len(sequence_a), len(sequence_b))):
                if sequence_a[i] != sequence_b[i]:
                    mismatch_index = i
                    break
            if mismatch_index is None:
                mismatch_index = min(len(sequence_a), len(sequence_b))

            errors.append(
                "Tier-0 order-state sequence mismatch: "
                f"firstDiffIndex={mismatch_index}, "
                f"{report_a_path.name}={sequence_a}, {report_b_path.name}={sequence_b}"
            )
    else:
        if order_state_a != order_state_b:
            errors.append(
                "Tier-0 fallback order state mismatch (no order-state sequences found in logs): "
                f"{report_a_path.name}={order_state_a}, {report_b_path.name}={order_state_b}"
            )
        else:
            warnings.append(
                "Tier-0 order-state sequence logs unavailable; used final ship.micro.order.state fallback."
            )

    warnings.append(
        f"Tier-0 signature compared reports: {report_a_path.name} vs {report_b_path.name}"
    )
    return len(errors) == 0, errors, warnings


def validate_report(report_path: Path) -> tuple[bool, list, list, dict]:
    """Validate a single metrics report against its budget."""
    errors = []
    warnings = []

    try:
        report = load_report(report_path)
    except ValueError as e:
        return False, [str(e)], warnings, {}

    scenario_id = report.get("scenarioId", "unknown")
    budget = BUDGETS.get(scenario_id)

    if not budget:
        # Try to match by prefix
        for key in BUDGETS:
            if scenario_id.startswith(key):
                budget = BUDGETS[key]
                break

    if not budget:
        warnings.append(f"No budget defined for scenario: {scenario_id}")
        return True, [], warnings, report

    metrics_map = build_metrics_map(report)

    # Check tick time (budget threshold is authoritative for CI gate)
    avg_tick_time = read_metric(report, metrics_map, ["averageTickTimeMs", "scale.averageTickTimeMs"])
    max_tick_time = read_metric(report, metrics_map, ["maxTickTimeMs", "scale.maxTickTimeMs"])
    p95_tick_time = read_metric(report, metrics_map, ["p95TickTimeMs", "scale.p95TickTimeMs"])
    target_tick_time = budget["maxTickTimeMs"]

    if avg_tick_time <= 0:
        errors.append("Missing real averageTickTimeMs metric (got <= 0)")
    if max_tick_time <= 0:
        errors.append("Missing real maxTickTimeMs metric (got <= 0)")

    if avg_tick_time > target_tick_time:
        errors.append(f"Average tick time {avg_tick_time:.2f}ms exceeds budget {target_tick_time:.2f}ms")
    elif avg_tick_time > target_tick_time * 0.8:
        warnings.append(f"Average tick time {avg_tick_time:.2f}ms approaching budget {target_tick_time:.2f}ms")

    if max_tick_time > target_tick_time * 2:
        errors.append(f"Max tick time {max_tick_time:.2f}ms exceeds 2x budget {target_tick_time * 2:.2f}ms")
    if p95_tick_time > 0 and p95_tick_time > target_tick_time * 1.5:
        warnings.append(f"P95 tick time {p95_tick_time:.2f}ms is high relative to budget {target_tick_time:.2f}ms")

    # Check memory
    peak_memory_mb = read_metric(report, metrics_map, ["peakMemoryMB", "scale.peakMemoryMB"])
    if peak_memory_mb <= 0:
        errors.append("Missing real peakMemoryMB metric (got <= 0)")

    if peak_memory_mb > budget["maxMemoryMB"]:
        errors.append(f"Peak memory {peak_memory_mb:.0f}MB exceeds budget {budget['maxMemoryMB']}MB")
    elif peak_memory_mb > budget["maxMemoryMB"] * 0.75:
        warnings.append(f"Peak memory {peak_memory_mb:.0f}MB approaching budget {budget['maxMemoryMB']}MB")

    # Check entity counts
    total_entities = int(read_metric(report, metrics_map, ["totalEntities", "scale.totalEntities"]))
    if total_entities <= 0:
        errors.append("Missing real totalEntities metric (got <= 0)")

    if total_entities > 100000 and scenario_id == "scale_baseline_10k":
        errors.append(f"Entity count {total_entities} exceeds baseline target of 10k")

    # Tier-0 ship-as-village vibe checks
    if scenario_id == "scenario.space4x.ship_micro.01":
        events_count = read_metric(report, metrics_map, ["ship.micro.events.count"])
        seat_readiness = read_metric(report, metrics_map, ["ship.micro.seat.readiness"], -1.0)
        constraints_respected = read_metric(report, metrics_map, ["constraints.respected"], -1.0)
        deterministic_replay = read_metric(report, metrics_map, ["deterministic.replay"], -1.0)

        if events_count <= 0:
            errors.append("Ship micro vibe proof missing comms activity (ship.micro.events.count <= 0)")
        if seat_readiness <= 0:
            errors.append("Ship micro vibe proof missing seat readiness signal (ship.micro.seat.readiness <= 0)")
        if constraints_respected != 1.0:
            errors.append("Ship micro constraints.respected metric is not 1.0")
        if deterministic_replay != 1.0:
            errors.append("Ship micro deterministic.replay metric is not 1.0")

    # Report status
    passed = len(errors) == 0
    return passed, errors, warnings, report

def main():
    if len(sys.argv) < 2:
        print("Usage: python validate_metrics.py <reports_directory>")
        sys.exit(1)
    
    reports_dir = Path(sys.argv[1])
    if not reports_dir.exists():
        print(f"Reports directory not found: {reports_dir}")
        sys.exit(1)
    
    # Find all JSON reports
    report_files = list(reports_dir.glob("*.json"))
    if not report_files:
        print(f"No JSON reports found in {reports_dir}")
        sys.exit(0)
    
    all_passed = True
    total_errors = 0
    total_warnings = 0
    loaded_reports = {}
    
    print("=" * 60)
    print("PureDOTS Scale Test Validation")
    print("=" * 60)
    
    for report_path in report_files:
        print(f"\nValidating: {report_path.name}")
        print("-" * 40)
        
        passed, errors, warnings, report = validate_report(report_path)
        if report:
            loaded_reports[report_path] = report
        
        if passed:
            print("  Status: PASSED")
        else:
            print("  Status: FAILED")
            all_passed = False
        
        for msg in errors:
            print(f"  ERROR: {msg}")
            total_errors += 1
        for msg in warnings:
            print(f"  WARNING: {msg}")
            total_warnings += 1

    tier0_passed, tier0_errors, tier0_warnings = validate_tier0_determinism(loaded_reports)
    if tier0_errors or tier0_warnings:
        print("\nTier-0 Determinism Check")
        print("-" * 40)
        if tier0_passed:
            print("  Status: PASSED")
        else:
            print("  Status: FAILED")
            all_passed = False

        for msg in tier0_errors:
            print(f"  ERROR: {msg}")
            total_errors += 1
        for msg in tier0_warnings:
            print(f"  WARNING: {msg}")
            total_warnings += 1
    
    print("\n" + "=" * 60)
    print(f"Summary: {len(report_files)} reports, {total_errors} errors, {total_warnings} warnings")
    print("=" * 60)
    
    if not all_passed:
        print("\nValidation FAILED - performance budgets exceeded")
        sys.exit(1)
    else:
        print("\nValidation PASSED - all budgets met")
        sys.exit(0)

if __name__ == "__main__":
    main()
