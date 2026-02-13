#!/usr/bin/env python3
"""
Validates scale test metrics against performance budgets.
Usage: python validate_metrics.py <reports_directory>
"""

import json
import sys
import os
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

def validate_report(report_path: Path) -> tuple[bool, list[str]]:
    """Validate a single metrics report against its budget."""
    errors = []
    warnings = []
    
    try:
        with open(report_path, 'r') as f:
            report = json.load(f)
    except json.JSONDecodeError as e:
        return False, [f"Failed to parse JSON: {e}"]
    except FileNotFoundError:
        return False, [f"Report file not found: {report_path}"]
    
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
        return True, warnings
    
    metrics_map = {}
    for item in report.get("metrics", []):
        if isinstance(item, dict):
            key = item.get("key")
            value = item.get("value")
            if isinstance(key, str) and isinstance(value, (int, float)):
                metrics_map[key] = float(value)

    def read_metric(keys: list[str], default: float = 0.0) -> float:
        for key in keys:
            value = report.get(key)
            if isinstance(value, (int, float)):
                return float(value)
        for key in keys:
            if key in metrics_map:
                return metrics_map[key]
        return default

    # Check tick time (budget threshold is authoritative for CI gate)
    avg_tick_time = read_metric(["averageTickTimeMs", "scale.averageTickTimeMs"])
    max_tick_time = read_metric(["maxTickTimeMs", "scale.maxTickTimeMs"])
    p95_tick_time = read_metric(["p95TickTimeMs", "scale.p95TickTimeMs"])
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
    peak_memory_mb = read_metric(["peakMemoryMB", "scale.peakMemoryMB"])
    if peak_memory_mb <= 0:
        errors.append("Missing real peakMemoryMB metric (got <= 0)")

    if peak_memory_mb > budget["maxMemoryMB"]:
        errors.append(f"Peak memory {peak_memory_mb:.0f}MB exceeds budget {budget['maxMemoryMB']}MB")
    elif peak_memory_mb > budget["maxMemoryMB"] * 0.75:
        warnings.append(f"Peak memory {peak_memory_mb:.0f}MB approaching budget {budget['maxMemoryMB']}MB")
    
    # Check entity counts
    total_entities = int(read_metric(["totalEntities", "scale.totalEntities"]))
    if total_entities <= 0:
        errors.append("Missing real totalEntities metric (got <= 0)")

    if total_entities > 100000 and scenario_id == "scale_baseline_10k":
        errors.append(f"Entity count {total_entities} exceeds baseline target of 10k")

    # Tier-0 ship-as-village vibe checks
    if scenario_id == "scenario.space4x.ship_micro.01":
        events_count = read_metric(["ship.micro.events.count"])
        seat_readiness = read_metric(["ship.micro.seat.readiness"], -1.0)
        constraints_respected = read_metric(["constraints.respected"], -1.0)
        deterministic_replay = read_metric(["deterministic.replay"], -1.0)

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
    messages = errors + warnings
    
    return passed, messages

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
    
    print("=" * 60)
    print("PureDOTS Scale Test Validation")
    print("=" * 60)
    
    for report_path in report_files:
        print(f"\nValidating: {report_path.name}")
        print("-" * 40)
        
        passed, messages = validate_report(report_path)
        
        if passed:
            print("  Status: PASSED")
        else:
            print("  Status: FAILED")
            all_passed = False
        
        for msg in messages:
            if "exceeds" in msg.lower():
                print(f"  ERROR: {msg}")
                total_errors += 1
            else:
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
