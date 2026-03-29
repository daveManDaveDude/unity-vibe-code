#!/usr/bin/env bash
set -euo pipefail

PROJECT_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
UNITY_VERSION="$(sed -n 's/^m_EditorVersion: //p' "$PROJECT_ROOT/ProjectSettings/ProjectVersion.txt")"

resolve_unity_editor() {
  if [[ -n "${UNITY_EDITOR_PATH:-}" ]]; then
    printf '%s\n' "$UNITY_EDITOR_PATH"
    return 0
  fi

  local candidates=(
    "/Applications/Unity/Hub/Editor/${UNITY_VERSION}/Unity.app/Contents/MacOS/Unity"
    "/Applications/Unity/Hub/Editor/${UNITY_VERSION}/Editor/Unity"
    "/Applications/Unity/${UNITY_VERSION}/Unity.app/Contents/MacOS/Unity"
  )

  local candidate
  for candidate in "${candidates[@]}"; do
    if [[ -x "$candidate" ]]; then
      printf '%s\n' "$candidate"
      return 0
    fi
  done

  return 1
}

require_unity_editor() {
  local editor_path
  if ! editor_path="$(resolve_unity_editor)"; then
    cat >&2 <<EOF
Unable to find the Unity editor executable for Unity ${UNITY_VERSION}.

Set UNITY_EDITOR_PATH to the full editor binary path, for example:
  export UNITY_EDITOR_PATH="/Applications/Unity/Hub/Editor/${UNITY_VERSION}/Unity.app/Contents/MacOS/Unity"
EOF
    exit 1
  fi

  printf '%s\n' "$editor_path"
}

artifacts_dir() {
  local path="$PROJECT_ROOT/artifacts"
  mkdir -p "$path"
  printf '%s\n' "$path"
}

logs_dir() {
  local path
  path="$(artifacts_dir)/logs"
  mkdir -p "$path"
  printf '%s\n' "$path"
}

tests_dir() {
  local path
  path="$(artifacts_dir)/tests"
  mkdir -p "$path"
  printf '%s\n' "$path"
}

run_unity_editor() {
  local editor_path
  editor_path="$(require_unity_editor)"
  "$editor_path" "$@"
}
