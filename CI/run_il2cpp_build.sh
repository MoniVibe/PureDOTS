#!/bin/bash
# IL2CPP Build Script for PureDOTS
# Validates IL2CPP compilation and performs post-build smoke tests

set -e

UNITY_PATH="${UNITY_PATH:-/Applications/Unity/Hub/Editor/*/Unity.app/Contents/MacOS/Unity}"
PROJECT_PATH="${PROJECT_PATH:-$(dirname "$0")/..}"
BUILD_TARGET="${BUILD_TARGET:-Windows64}"
LOG_DIR="${PROJECT_PATH}/build/Logs"
LOG_FILE="${LOG_DIR}/IL2CPP_Build.log"

# Ensure log directory exists
mkdir -p "${LOG_DIR}"

echo "Starting IL2CPP build for PureDOTS..."
echo "Unity Path: ${UNITY_PATH}"
echo "Project Path: ${PROJECT_PATH}"
echo "Build Target: ${BUILD_TARGET}"
echo "Log File: ${LOG_FILE}"

# Pre-build validation
echo "Pre-build validation..."
if [ ! -f "${PROJECT_PATH}/Assets/Config/Linker/link.xml" ]; then
    echo "WARNING: link.xml not found at Assets/Config/Linker/link.xml"
    echo "IL2CPP builds may fail due to missing type preservation"
fi

# Run EditMode tests first to catch compilation errors
echo "Running EditMode tests..."
"${UNITY_PATH}" -batchmode -quit \
    -projectPath "${PROJECT_PATH}" \
    -runTests \
    -testPlatform EditMode \
    -testResults "${LOG_DIR}/EditMode_TestResults.xml" \
    -logFile "${LOG_DIR}/EditMode_Tests.log" || {
    echo "ERROR: EditMode tests failed. Check ${LOG_DIR}/EditMode_Tests.log"
    exit 1
}

# Perform IL2CPP build
echo "Building IL2CPP..."
"${UNITY_PATH}" -batchmode -quit \
    -projectPath "${PROJECT_PATH}" \
    -buildTarget "${BUILD_TARGET}" \
    -executeMethod BuildScript.BuildIL2CPP \
    -logFile "${LOG_FILE}" || {
    echo "ERROR: IL2CPP build failed. Check ${LOG_FILE}"
    exit 1
}

# Post-build validation
echo "Post-build validation..."
if grep -q "MissingMethodException\|MissingTypeException\|MissingFieldException" "${LOG_FILE}"; then
    echo "ERROR: IL2CPP build contains missing type/method exceptions"
    echo "Check ${LOG_FILE} for details"
    exit 1
fi

if grep -q "Burst compilation failed\|Burst error" "${LOG_FILE}"; then
    echo "ERROR: Burst compilation failed"
    echo "Check ${LOG_FILE} for details"
    exit 1
fi

echo "IL2CPP build completed successfully"
echo "Build log: ${LOG_FILE}"
echo "Build artifacts should be in ${PROJECT_PATH}/build/${BUILD_TARGET}/"

