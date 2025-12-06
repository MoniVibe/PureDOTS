#!/usr/bin/env bash
set -euo pipefail

# Run tests for a specific package
# Usage: CI/test_package.sh <package-name>

UNITY_PATH="${UNITY_PATH:-/Applications/Unity/Hub/Editor/2022.3.14f1/Unity.app/Contents/MacOS/Unity}"
PROJECT_PATH="${PROJECT_PATH:-$(pwd)}"
PACKAGE_NAME="${1:-}"

if [ -z "$PACKAGE_NAME" ]; then
    echo "Usage: CI/test_package.sh <package-name>"
    echo "Example: CI/test_package.sh com.moni.puredots"
    exit 1
fi

RESULTS_DIR="${RESULTS_DIR:-$PROJECT_PATH/CI/TestResults}"
mkdir -p "$RESULTS_DIR"

echo "Running tests for package: $PACKAGE_NAME"

# Run tests filtered by assembly name
"$UNITY_PATH" \
  -batchmode \
  -nographics \
  -quit \
  -projectPath "$PROJECT_PATH" \
  -runTests \
  -testPlatform EditMode \
  -testFilter "$PACKAGE_NAME" \
  -testResults "$RESULTS_DIR/${PACKAGE_NAME}_editmode_results.xml" \
  -logFile "$RESULTS_DIR/${PACKAGE_NAME}_test.log"

if [ $? -eq 0 ]; then
    echo "Tests completed for package: $PACKAGE_NAME"
else
    echo "Tests failed for package: $PACKAGE_NAME. Check $RESULTS_DIR/${PACKAGE_NAME}_test.log"
    exit 1
fi

