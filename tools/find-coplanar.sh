#!/bin/bash
# Finds overlapping faces in one plane, facing the same way, in models (they flicker: z-fighting), e.g.
#   tools/find-coplanar.sh press_house:ground orchard_wall:gate crate
# Faces under the ground or the water don't matter, nor slivers a centimetre or two across.
BLENDER=${BLENDER:-/Applications/Blender.app/Contents/MacOS/Blender}
here="$(cd "$(dirname "$0")" && pwd)"
for spec in "$@"; do
    echo "== $spec"
    "$BLENDER" -b --python "$here/blender/coplanar.py" -- "$spec" 2>&1 | grep COPLANAR
done
