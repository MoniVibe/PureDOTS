#!/bin/bash
# PureDOTS Scale Test Runner
# Usage: ./CI/run_scale_tests.sh [--all|--mini|--baseline|--stress|--extreme]

set -e

UNITY_PATH="${UNITY_PATH:-Unity}"
PROJECT_PATH="${PROJECT_PATH:-.}"
REPORTS_DIR="${REPORTS_DIR:-CI/Reports}"
SAMPLES_PATH="Packages/com.moni.puredots/Runtime/Runtime/Scenarios/Samples"

# Ensure reports directory exists
mkdir -p "$REPORTS_DIR"

# Parse arguments
RUN_MODE="${1:---baseline}"

run_scenario() {
    local scenario_name=$1
    local scenario_file="${SAMPLES_PATH}/${scenario_name}.json"
    local report_file="${REPORTS_DIR}/${scenario_name}_report.json"
    
    echo "========================================"
    echo "Running: ${scenario_name}"
    echo "========================================"
    
    if [ ! -f "$scenario_file" ]; then
        echo "ERROR: Scenario file not found: $scenario_file"
        return 1
    fi
    
    "$UNITY_PATH" -batchmode -quit -projectPath "$PROJECT_PATH" \
        -executeMethod PureDOTS.Runtime.Devtools.ScenarioRunnerEntryPoints.RunScaleTest \
        --scenario "$scenario_name" \
        --metrics "$report_file" \
        --enable-lod-debug \
        --enable-aggregate-debug \
        -logFile "${REPORTS_DIR}/${scenario_name}.log"
    
    local exit_code=$?
    
    if [ $exit_code -eq 0 ]; then
        echo "SUCCESS: ${scenario_name} completed"
    else
        echo "FAILED: ${scenario_name} exited with code $exit_code"
        return $exit_code
    fi
}

run_mini_tests() {
    echo "Running mini sanity tests..."
    run_scenario "scale_mini_lod_demo"
    run_scenario "scale_mini_aggregate_demo"
}

run_baseline() {
    echo "Running baseline 10k test..."
    run_scenario "scale_baseline_10k"
}

run_stress() {
    echo "Running stress 100k test..."
    run_scenario "scale_stress_100k"
}

run_extreme() {
    echo "Running extreme 1M test..."
    run_scenario "scale_extreme_1m"
}

run_all() {
    run_mini_tests
    run_baseline
    run_stress
    run_extreme
}

# Main execution
echo "========================================"
echo "PureDOTS Scale Test Suite"
echo "Mode: $RUN_MODE"
echo "Reports: $REPORTS_DIR"
echo "========================================"

case "$RUN_MODE" in
    --all)
        run_all
        ;;
    --mini)
        run_mini_tests
        ;;
    --baseline)
        run_baseline
        ;;
    --stress)
        run_stress
        ;;
    --extreme)
        run_extreme
        ;;
    *)
        echo "Unknown mode: $RUN_MODE"
        echo "Usage: $0 [--all|--mini|--baseline|--stress|--extreme]"
        exit 1
        ;;
esac

# Validate results
echo ""
echo "========================================"
echo "Validating results..."
echo "========================================"

if [ -f "CI/validate_metrics.py" ]; then
    python3 CI/validate_metrics.py "$REPORTS_DIR"
    VALIDATE_EXIT=$?
    
    if [ $VALIDATE_EXIT -ne 0 ]; then
        echo "VALIDATION FAILED"
        exit 1
    fi
else
    echo "WARNING: validate_metrics.py not found, skipping validation"
fi

echo ""
echo "========================================"
echo "Scale tests completed successfully"
echo "========================================"

