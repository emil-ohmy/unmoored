#!/usr/bin/env bash
# Check a rigged model's clips frame by frame: how low it goes (the floor), each bone's speed and jerks, loops' seams.
#   tools/check-clips.sh laden                  # every clip
#   CLIPS=Windup,Strike tools/check-clips.sh laden
set -euo pipefail
source "$(dirname "$0")/env.sh"
need blender
blend="$(ls "$ROOT"/art/models/*/"$1".blend)"
"$BLENDER" -b "$blend" --python "$ROOT/tools/blender/check_clips.py" -- "${CLIPS:-}" 2>&1 | grep -E "^==|^   "
