#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
source "$SCRIPT_DIR/unity_common.sh"

APP_PATH="${UNITY_PLAYER_APP_PATH:-$PROJECT_ROOT/Builds/StandaloneOSX/unity-vibe-code.app}"
EXECUTABLE_DIR="$APP_PATH/Contents/MacOS"

if [[ ! -d "$APP_PATH" ]]; then
  cat >&2 <<EOF
macOS player app not found at:
  $APP_PATH

Build it first with:
  ./scripts/build_osx.sh
EOF
  exit 1
fi

if [[ ! -d "$EXECUTABLE_DIR" ]]; then
  cat >&2 <<EOF
No executable directory found inside:
  $APP_PATH
EOF
  exit 1
fi

shopt -s nullglob
executables=("$EXECUTABLE_DIR"/*)
shopt -u nullglob

if [[ "${#executables[@]}" -eq 0 ]]; then
  cat >&2 <<EOF
No player executable was found in:
  $EXECUTABLE_DIR
EOF
  exit 1
fi

exec "${executables[0]}" "$@"
