#!/bin/bash
# Finds overlapping faces in one plane, facing the same way, in models (they flicker: z-fighting), e.g.
#   tools/find-coplanar.sh press_house:ground orchard_wall:gate crate
# Faces under the ground or the water don't matter, nor slivers a centimetre or two across.
source "$(dirname "$0")/env.sh"
need blender
for spec in "$@"; do
    echo "== $spec"
    "$BLENDER" -b --python "$ROOT/tools/blender/coplanar.py" -- "$spec" 2>&1 | grep COPLANAR
done
