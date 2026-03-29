#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
source "$SCRIPT_DIR/unity_common.sh"

BUILD_PATH="${1:-$PROJECT_ROOT/Builds/StandaloneWindows64}"
BUILD_NAME="${2:-unity-vibe-code.exe}"
LOG_FILE="$(logs_dir)/build-win64.log"

mkdir -p "$BUILD_PATH"
rm -f "$LOG_FILE"

run_unity_editor \
  -batchmode \
  -accept-apiupdate \
  -quit \
  -projectPath "$PROJECT_ROOT" \
  -buildTarget StandaloneWindows64 \
  -executeMethod VibeCode.Build.BuildScripts.BuildWindows64 \
  -customBuildPath "$BUILD_PATH" \
  -customBuildName "$BUILD_NAME" \
  -logFile "$LOG_FILE"

printf 'Windows build completed: %s/%s\n' "$BUILD_PATH" "$BUILD_NAME"
printf 'Unity log: %s\n' "$LOG_FILE"
