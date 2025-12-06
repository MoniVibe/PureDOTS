#!/usr/bin/env bash
set -euo pipefail

# Validate scenario determinism by running scenario and comparing tick hashes
# Usage: CI/validate_scenario.sh <scenario_path> [expected_hash_file]

UNITY_PATH="${UNITY_PATH:-/Applications/Unity/Hub/Editor/2022.3.14f1/Unity.app/Contents/MacOS/Unity}"
PROJECT_PATH="${PROJECT_PATH:-$(pwd)}"
SCENARIO_PATH="${1:-}"
EXPECTED_HASH_FILE="${2:-CI/scenario_hashes.json}"
RESULTS_DIR="${RESULTS_DIR:-$PROJECT_PATH/CI/TestResults}"

if [ -z "$SCENARIO_PATH" ]; then
    echo "Usage: CI/validate_scenario.sh <scenario_path> [expected_hash_file]"
    exit 1
fi

if [ ! -f "$SCENARIO_PATH" ]; then
    echo "Error: Scenario file not found: $SCENARIO_PATH"
    exit 1
fi

mkdir -p "$RESULTS_DIR"

echo "Running scenario: $SCENARIO_PATH"
echo "Expected hash file: $EXPECTED_HASH_FILE"

# Run scenario and capture hash
"$UNITY_PATH" \
  -batchmode \
  -nographics \
  -quit \
  -projectPath "$PROJECT_PATH" \
  -executeMethod PureDOTS.Runtime.Devtools.ScenarioRunnerEntryPoints.RunScenarioFromArgs \
  --scenario "$SCENARIO_PATH" \
  --report "$RESULTS_DIR/scenario_hash_output.json" \
  -logFile "$RESULTS_DIR/scenario_validation.log"

if [ $? -ne 0 ]; then
    echo "Failed to run scenario. Check $RESULTS_DIR/scenario_validation.log"
    exit 1
fi

# Extract hash from output (simplified - would need proper JSON parsing)
SCENARIO_HASH=$(grep -o '"hash":[^,]*' "$RESULTS_DIR/scenario_hash_output.json" | cut -d'"' -f4 || echo "")

if [ -z "$SCENARIO_HASH" ]; then
    echo "Warning: Could not extract hash from scenario output"
    exit 0
fi

# Compare with expected hash if file exists
if [ -f "$EXPECTED_HASH_FILE" ]; then
    EXPECTED_HASH=$(grep -o "\"$SCENARIO_PATH\":\"[^\"]*\"" "$EXPECTED_HASH_FILE" | cut -d'"' -f4 || echo "")
    
    if [ -z "$EXPECTED_HASH" ]; then
        echo "Warning: No expected hash found for scenario in $EXPECTED_HASH_FILE"
        exit 0
    fi
    
    if [ "$SCENARIO_HASH" != "$EXPECTED_HASH" ]; then
        echo "ERROR: Hash mismatch!"
        echo "  Expected: $EXPECTED_HASH"
        echo "  Got:      $SCENARIO_HASH"
        echo "Non-determinism detected - scenario output differs from expected"
        exit 1
    else
        echo "SUCCESS: Hash matches expected value"
    fi
else
    echo "No expected hash file found. Hash for this run: $SCENARIO_HASH"
    echo "Add to $EXPECTED_HASH_FILE: \"$SCENARIO_PATH\": \"$SCENARIO_HASH\""
fi

