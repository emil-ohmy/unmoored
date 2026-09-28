#!/usr/bin/env bash
# Render sound scripts into the Unity project (uses Blender's bundled Python, which has numpy).
#   tools/build-audio.sh                 # everything in art/audio/
#   tools/build-audio.sh oar_swing dash  # just these
set -euo pipefail
ROOT="$(cd "$(dirname "$0")/.." && pwd)"
PY="$(ls /Applications/Blender.app/Contents/Resources/*/python/bin/python3.* | head -1)"
shopt -s nullglob
for script in "$ROOT"/art/audio/*.py; do
  name="$(basename "$script" .py)"
  if [[ $# -gt 0 ]] && [[ ! " $* " == *" $name "* ]]; then continue; fi
  "$PY" "$ROOT/tools/audio/build_sound.py" "$script"
done
