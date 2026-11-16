#!/bin/bash
# The level editor: starts its little server (it saves straight into game/Assets/_Project/Levels/) and opens it.
#   tools/level-editor.sh [level]      e.g. tools/level-editor.sh the_hold_spiral
# Ctrl-C stops it.
source "$(dirname "$0")/env.sh"
cd "$ROOT"
PORT=${PORT:-8777}
URL="http://localhost:$PORT/"
[ -n "$1" ] && URL="$URL?level=$1"

# The server is plain Python (standard library only). On Windows use Blender's bundled Python: there is often no
# other, and `python3` there may be the Microsoft Store stub.
if [[ $OS == windows ]]; then need blender-py; PY="$BLENDER_PY"; else PY=python3; fi

case $OS in
  mac)     opener=(open) ;;
  windows) opener=(cmd /c start "") ;;
  *)       opener=(xdg-open) ;;
esac
( sleep 0.8; "${opener[@]}" "$URL" >/dev/null 2>&1 ) &
exec "$PY" tools/level-editor/serve.py "$PORT"
