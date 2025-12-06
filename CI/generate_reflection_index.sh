#!/usr/bin/env bash
set -euo pipefail

# Generate Type Reflection Index via Unity Editor
# This script should be run before builds to ensure the index is up to date

UNITY_PATH="${UNITY_PATH:-/Applications/Unity/Hub/Editor/2022.3.14f1/Unity.app/Contents/MacOS/Unity}"
PROJECT_PATH="${PROJECT_PATH:-$(pwd)}"

echo "Generating Type Reflection Index..."

"$UNITY_PATH" \
  -batchmode \
  -quit \
  -projectPath "$PROJECT_PATH" \
  -executeMethod PureDOTS.Editor.Reflection.TypeReflectionIndexGenerator.GenerateIndex \
  -logFile CI/reflection_index_generation.log

if [ $? -eq 0 ]; then
    echo "Type Reflection Index generated successfully"
else
    echo "Failed to generate Type Reflection Index. Check CI/reflection_index_generation.log"
    exit 1
fi

