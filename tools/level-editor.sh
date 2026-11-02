#!/bin/bash
# The level editor: starts its little server (it saves straight into game/Assets/_Project/Levels/) and opens it.
#   tools/level-editor.sh [level]      e.g. tools/level-editor.sh the_hold_spiral
# Ctrl-C stops it.
cd "$(dirname "$0")/.."
PORT=${PORT:-8777}
URL="http://localhost:$PORT/"
[ -n "$1" ] && URL="$URL?level=$1"
( sleep 0.8; open "$URL" 2>/dev/null || xdg-open "$URL" 2>/dev/null ) &
exec python3 tools/level-editor/serve.py "$PORT"
