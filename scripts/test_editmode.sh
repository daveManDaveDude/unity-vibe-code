#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
source "$SCRIPT_DIR/unity_common.sh"

RESULTS_FILE="$(tests_dir)/editmode-results.xml"
LOG_FILE="$(logs_dir)/editmode-tests.log"

rm -f "$RESULTS_FILE" "$LOG_FILE"

run_unity_editor \
  -batchmode \
  -accept-apiupdate \
  -projectPath "$PROJECT_ROOT" \
  -runTests \
  -testPlatform EditMode \
  -testResults "$RESULTS_FILE" \
  -logFile "$LOG_FILE"

printf 'EditMode tests completed. Results: %s\n' "$RESULTS_FILE"
printf 'Unity log: %s\n' "$LOG_FILE"
