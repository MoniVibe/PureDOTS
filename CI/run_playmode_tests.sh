#!/usr/bin/env bash
set -euo pipefail

UNITY_PATH="${UNITY_EDITOR_PATH:-${UNITY_PATH:-${UNITY:-}}}"
if [ -z "$UNITY_PATH" ]; then
  echo "UNITY_EDITOR_PATH (or UNITY_PATH/UNITY) must point to the Unity editor binary."
  exit 1
fi
PROJECT_PATH="${PROJECT_PATH:-$(pwd)}"
RESULTS_DIR="${RESULTS_DIR:-$PROJECT_PATH/CI/TestResults}"
ARTIFACTS_DIR="${ARTIFACTS_DIR:-$RESULTS_DIR/Artifacts}"
TEST_FILTER="${TEST_FILTER:-}"
TEST_NAMES="${TEST_NAMES:-}"

EXTRA_ARGS=()
if [ -n "$TEST_FILTER" ]; then
  EXTRA_ARGS+=("-testFilter" "$TEST_FILTER")
fi
if [ -n "$TEST_NAMES" ]; then
  EXTRA_ARGS+=("-testNames" "$TEST_NAMES")
fi

mkdir -p "$RESULTS_DIR"
mkdir -p "$ARTIFACTS_DIR"

echo "Running EditMode tests..."
"$UNITY_PATH" \
  -batchmode \
  -nographics \
  -projectPath "$PROJECT_PATH" \
  -runTests \
  -testPlatform EditMode \
  "${EXTRA_ARGS[@]}" \
  -testResults "$RESULTS_DIR/editmode-results.xml" \
  -logFile "$RESULTS_DIR/editmode.log" \
  -quit

EDITMODE_EXIT_CODE=$?

echo "Running PlayMode tests..."
"$UNITY_PATH" \
  -batchmode \
  -nographics \
  -projectPath "$PROJECT_PATH" \
  -runTests \
  -testPlatform PlayMode \
  "${EXTRA_ARGS[@]}" \
  -testResults "$RESULTS_DIR/playmode-results.xml" \
  -logFile "$RESULTS_DIR/playmode.log" \
  -quit

PLAYMODE_EXIT_CODE=$?

# Check if budget JSON artifact exists
if [ -f "$ARTIFACTS_DIR/budget_results.json" ]; then
  echo "Budget results artifact found:"
  cat "$ARTIFACTS_DIR/budget_results.json"
else
  echo "Warning: Budget results artifact not found at $ARTIFACTS_DIR/budget_results.json"
fi

# Exit with failure if either test suite failed
if [ $EDITMODE_EXIT_CODE -ne 0 ] || [ $PLAYMODE_EXIT_CODE -ne 0 ]; then
  echo "Test suite failed (EditMode: $EDITMODE_EXIT_CODE, PlayMode: $PLAYMODE_EXIT_CODE)"
  exit 1
fi

echo "All tests passed successfully"
exit 0
