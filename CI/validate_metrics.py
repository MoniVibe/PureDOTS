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
            if scenario_id.startswith(key.split("_")[0]):
                budget = BUDGETS[key]
                break
    
    if not budget:
        warnings.append(f"No budget defined for scenario: {scenario_id}")
        return True, warnings
    
    # Check tick time
    avg_tick_time = report.get("averageTickTimeMs", 0)
    max_tick_time = report.get("maxTickTimeMs", 0)
    target_tick_time = report.get("targetTickTimeMs", budget["maxTickTimeMs"])
    
    if avg_tick_time > target_tick_time:
        errors.append(f"Average tick time {avg_tick_time:.2f}ms exceeds budget {target_tick_time:.2f}ms")
    elif avg_tick_time > target_tick_time * 0.8:
        warnings.append(f"Average tick time {avg_tick_time:.2f}ms approaching budget {target_tick_time:.2f}ms")
    
    if max_tick_time > target_tick_time * 2:
        errors.append(f"Max tick time {max_tick_time:.2f}ms exceeds 2x budget {target_tick_time * 2:.2f}ms")
    
    # Check memory
    peak_memory_mb = report.get("peakMemoryMB", 0)
    if peak_memory_mb > budget["maxMemoryMB"]:
        errors.append(f"Peak memory {peak_memory_mb:.0f}MB exceeds budget {budget['maxMemoryMB']}MB")
    elif peak_memory_mb > budget["maxMemoryMB"] * 0.75:
        warnings.append(f"Peak memory {peak_memory_mb:.0f}MB approaching budget {budget['maxMemoryMB']}MB")
    
    # Check entity counts
    total_entities = report.get("totalEntities", 0)
    if total_entities > 100000 and scenario_id == "scale_baseline_10k":
        errors.append(f"Entity count {total_entities} exceeds baseline target of 10k")
    
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

