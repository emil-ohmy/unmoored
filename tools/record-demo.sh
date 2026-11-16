#!/usr/bin/env bash
# Record the scripted demo session to an MP4 with sound: builds/recordings/demo.mp4
#   tools/record-demo.sh [project path]   (default: game/; the editor must not have that project open)
set -euo pipefail
source "$(dirname "$0")/env.sh"
need unity blender-py ffmpeg
PROJECT="$(native "${1:-$ROOT/game}")"
OUT="$ROOT/builds/recordings"
mkdir -p "$OUT"

echo "Building the scene and playing the demo session (a minute or two)..."
"$UNITY" -batchmode -quit -projectPath "$PROJECT" -executeMethod MainSceneBuilder.BuildFromCommandLine -logFile "$OUT/build.log"
RECORD="$OUT" "$UNITY" -batchmode -projectPath "$PROJECT" -runTests -testPlatform PlayMode \
  -testFilter RecordDemoTests -testResults "$OUT/results.xml" -logFile "$OUT/record.log" || true
grep -q 'result="Passed"' "$OUT/results.xml" || { echo "Recording failed; see $OUT/record.log"; exit 1; }

"$BLENDER_PY" "$ROOT/tools/audio/mix_recording.py" "$OUT"
ffmpeg -loglevel error -y -framerate 30 -i "$OUT/frames/%05d.jpg" -i "$OUT/mix.wav" \
  -c:v libx264 -pix_fmt yuv420p -crf 20 -preset slow -c:a aac -b:a 192k -shortest -movflags +faststart "$OUT/demo.mp4"
echo "$OUT/demo.mp4"
