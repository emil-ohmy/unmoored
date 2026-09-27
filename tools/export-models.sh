#!/usr/bin/env bash
# Export Blender sources in art/models/ to FBX in the Unity project.
#   tools/export-models.sh            # every .blend
#   tools/export-models.sh crate      # just art/models/crate.blend
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
BLENDER="${BLENDER:-/Applications/Blender.app/Contents/MacOS/Blender}"
OUT="$ROOT/game/Assets/_Project/Art/Models"

if [[ $# -gt 0 ]]; then
  files=("$ROOT/art/models/$1.blend")
else
  shopt -s nullglob
  files=("$ROOT"/art/models/*.blend)
fi

[[ ${#files[@]} -eq 0 ]] && { echo "No .blend files in art/models/"; exit 0; }

for f in "${files[@]}"; do
  echo "Exporting $(basename "$f")"
  "$BLENDER" -b "$f" --python-exit-code 1 --python "$ROOT/tools/blender/export_fbx.py" -- "$OUT" \
    | grep -E "^\[export_fbx\]|Error" || true
done
