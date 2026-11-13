#!/usr/bin/env bash
# Run Unity tests headless. Usage: run_unity_tests.sh <EditMode|PlayMode> [test-filter]
# Needs UNITY_PATH (path to the Unity 6 editor binary) and an existing Unity project.
# Exit codes: 0 = all tests passed, 1 = tests failed, 3 = cannot run here (no Unity/project).
set -uo pipefail
MODE="${1:?EditMode or PlayMode}"; FILTER="${2:-}"
PROJECT="${UNITY_PROJECT_PATH:-.}"
if [ -z "${UNITY_PATH:-}" ] || [ ! -x "$UNITY_PATH" ] || [ ! -f "$PROJECT/ProjectSettings/ProjectVersion.txt" ]; then
  echo "Unity not available (set UNITY_PATH and run from the project root)."; exit 3
fi
RESULTS="$(mktemp -d)/results.xml"
ARGS=(-batchmode -nographics -projectPath "$PROJECT" -runTests -testPlatform "$MODE" -testResults "$RESULTS" -logFile -)
[ -n "$FILTER" ] && ARGS+=(-testFilter "$FILTER")
"$UNITY_PATH" "${ARGS[@]}"; RC=$?
# Once the editor starts, a nonzero exit means verification failed.
[ $RC -eq 0 ] && exit 0
exit 1
