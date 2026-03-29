#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
source "$SCRIPT_DIR/unity_common.sh"

RESULTS_FILE="$(tests_dir)/playmode-results.xml"
LOG_FILE="$(logs_dir)/playmode-tests.log"

run_unity_editor \
  -batchmode \
  -accept-apiupdate \
  -projectPath "$PROJECT_ROOT" \
  -runTests \
  -testPlatform PlayMode \
  -testResults "$RESULTS_FILE" \
  -logFile "$LOG_FILE"

printf 'PlayMode tests completed. Results: %s\n' "$RESULTS_FILE"
printf 'Unity log: %s\n' "$LOG_FILE"
