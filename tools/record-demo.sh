#!/usr/bin/env bash
# Record the scripted demo session to an MP4 with sound: builds/recordings/demo.mp4
#   tools/record-demo.sh [project path]   (default: game/; the editor must not have that project open)
set -euo pipefail
ROOT="$(cd "$(dirname "$0")/.." && pwd)"
PROJECT="${1:-$ROOT/game}"
OUT="$ROOT/builds/recordings"
VERSION="$(sed -n 's/^m_EditorVersion: //p' "$ROOT/game/ProjectSettings/ProjectVersion.txt")"
UNITY="${UNITY:-/Applications/Unity/Hub/Editor/$VERSION/Unity.app/Contents/MacOS/Unity}"
PY="$(ls /Applications/Blender.app/Contents/Resources/*/python/bin/python3.* | head -1)"
mkdir -p "$OUT"

echo "Building the scene and playing the demo session (a minute or two)..."
"$UNITY" -batchmode -quit -projectPath "$PROJECT" -executeMethod MainSceneBuilder.BuildFromCommandLine -logFile "$OUT/build.log"
RECORD="$OUT" "$UNITY" -batchmode -projectPath "$PROJECT" -runTests -testPlatform PlayMode \
  -testFilter RecordDemoTests -testResults "$OUT/results.xml" -logFile "$OUT/record.log" || true
grep -q 'result="Passed"' "$OUT/results.xml" || { echo "Recording failed; see $OUT/record.log"; exit 1; }

"$PY" "$ROOT/tools/audio/mix_recording.py" "$OUT"
ffmpeg -loglevel error -y -framerate 30 -i "$OUT/frames/%05d.jpg" -i "$OUT/mix.wav" \
  -c:v libx264 -pix_fmt yuv420p -crf 20 -preset slow -c:a aac -b:a 192k -shortest -movflags +faststart "$OUT/demo.mp4"
echo "$OUT/demo.mp4"
