#!/usr/bin/env bash
# Contact sheet of a rigged model's animations (rows = actions, columns = evenly spaced frames).
#   tools/preview-poses.sh apprentice [out.png]
#   ZOOM=2 ACTIONS=Windup,Strike tools/preview-poses.sh anchoress   # closer, just these clips
set -euo pipefail
ROOT="$(cd "$(dirname "$0")/.." && pwd)"
BLENDER="${BLENDER:-/Applications/Blender.app/Contents/MacOS/Blender}"
blend="$(ls "$ROOT"/art/models/*/"$1".blend)"
out="${2:-$ROOT/builds/poses-$1.png}"
tmp="$(mktemp -d)"
"$BLENDER" -b "$blend" --python "$ROOT/tools/blender/preview_poses.py" -- "$tmp" 5 "${ZOOM:-1}" ${ACTIONS:+"$ACTIONS"} | grep "^\[preview\]"
rows=()
for action in $(ls "$tmp" | sed -E 's/_[0-9]+\.png$//' | sort -u); do
  ffmpeg -loglevel error -y -i "$tmp/${action}_0.png" -i "$tmp/${action}_1.png" -i "$tmp/${action}_2.png" \
    -i "$tmp/${action}_3.png" -i "$tmp/${action}_4.png" -filter_complex "hstack=inputs=5" "$tmp/row_$action.png"
  rows+=(-i "$tmp/row_$action.png")
done
mkdir -p "$(dirname "$out")"
ffmpeg -loglevel error -y "${rows[@]}" -filter_complex "vstack=inputs=$(( ${#rows[@]} / 2 ))" "$out"
rm -rf "$tmp"
echo "$out"
