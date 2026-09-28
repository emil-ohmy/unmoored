#!/usr/bin/env bash
# Drive Unity from the CLI.
#   tools/unity.sh open          # open the project in the editor
#   tools/unity.sh import        # headless: import/refresh assets, compile scripts
#   tools/unity.sh build-mac     # headless: build builds/mac/MyGame.app
#   tools/unity.sh build-webgl   # headless: build builds/webgl/
#   tools/unity.sh test          # headless: run EditMode + PlayMode tests, skipping [Category("Slow")] ones
#   tools/unity.sh test-all      # headless: every test, the slow measurements too (before a merge)
# Headless commands fail if the editor already has the project open; close it first.
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
PROJECT="$ROOT/game"
VERSION="$(sed -n 's/^m_EditorVersion: //p' "$PROJECT/ProjectSettings/ProjectVersion.txt")"
UNITY="${UNITY:-/Applications/Unity/Hub/Editor/$VERSION/Unity.app/Contents/MacOS/Unity}"
LOG="$ROOT/builds/unity-${1:-help}.log"
mkdir -p "$ROOT/builds"

batch() {
  if "$UNITY" -batchmode -nographics -quit -projectPath "$PROJECT" -logFile "$LOG" "$@"; then
    echo "OK (log: $LOG)"
  else
    echo "FAILED — errors from $LOG:"; grep -E "error|Error|Exception" "$LOG" | head -30; exit 1
  fi
}

case "${1:-}" in
  open)        open -a "$(dirname "$UNITY")/../.." --args -projectPath "$PROJECT" ;;
  import)      batch ;;
  build-mac)   batch -executeMethod BuildScript.BuildMac ;;
  build-webgl) batch -executeMethod BuildScript.BuildWebGL ;;
  test|test-all)
    categories=(); [[ "$1" == test ]] && categories=(-testCategory "!Slow")
    for platform in EditMode PlayMode; do
      results="$ROOT/builds/test-results-$platform.xml"
      rm -f "$results"
      "$UNITY" -batchmode -projectPath "$PROJECT" -runTests -testPlatform "$platform" \
        -testResults "$results" -logFile "$LOG" ${categories[@]+"${categories[@]}"} || true
      [[ -f "$results" ]] || { echo "$platform: no results, see $LOG"; exit 1; }
      summary="$(grep -o '<test-run[^>]*' "$results" | grep -oE '(total|passed|failed)="[0-9]+"' | tr '\n' ' ')"
      echo "$platform: $summary"
      grep -q 'result="Passed"' <(grep -o '<test-run[^>]*' "$results") || { echo "  failures in $results"; exit 1; }
    done ;;
  *)           sed -n '2,9p' "$0"; exit 1 ;;
esac
