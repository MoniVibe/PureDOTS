#!/usr/bin/env bash
set -euo pipefail

# Generate documentation from Type Reflection Index
# This script should be run after generate_reflection_index.sh

UNITY_PATH="${UNITY_PATH:-/Applications/Unity/Hub/Editor/2022.3.14f1/Unity.app/Contents/MacOS/Unity}"
PROJECT_PATH="${PROJECT_PATH:-$(pwd)}"

echo "Generating documentation..."

"$UNITY_PATH" \
  -batchmode \
  -quit \
  -projectPath "$PROJECT_PATH" \
  -executeMethod PureDOTS.Editor.Documentation.DocumentationGenerator.GenerateDocumentation \
  -logFile CI/docs_generation.log

if [ $? -eq 0 ]; then
    echo "Documentation generated successfully"
else
    echo "Failed to generate documentation. Check CI/docs_generation.log"
    exit 1
fi

