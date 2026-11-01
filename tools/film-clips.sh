#!/usr/bin/env bash
# Film a rigged model's clips, every frame, from the game's camera and the side (Blender, Workbench):
#   tools/film-clips.sh laden                      # every clip: builds/film-laden/<Clip>.mp4 and <Clip>-sheet.png
#   CLIPS=Windup,Strike tools/film-clips.sh laden  # just these
#   SHEET_EVERY=2 (frames between the sheet's pictures)   SIZE=420 (pixels per view)
# Each clip's film plays it at 30 fps, both views side by side, looped three times with a beat's pause between;
# all.mp4 strings every clip together. The sheet: every SHEET_EVERY-th frame, numbered, in rows.
set -euo pipefail
ROOT="$(cd "$(dirname "$0")/.." && pwd)"
BLENDER="${BLENDER:-/Applications/Blender.app/Contents/MacOS/Blender}"
name="$1"
blend="$(ls "$ROOT"/art/models/*/"$name".blend)"
out="$ROOT/builds/film-$name"
tmp="$(mktemp -d)"
"$BLENDER" -b "$blend" --python "$ROOT/tools/blender/film_clips.py" -- "$tmp" "${CLIPS:-}" "${SIZE:-420}" | grep "^\[film\]"
mkdir -p "$out"
list="$tmp/all.txt"
: > "$list"
for dir in "$tmp"/*/; do
  clip="$(basename "$dir")"
  # The film: both views side by side, the clip's name and frame number burnt in by a strip over it.
  ffmpeg -loglevel error -y -framerate 30 -i "$dir/game_%04d.png" -framerate 30 -i "$dir/side_%04d.png" \
    -filter_complex "[0][1]hstack=inputs=2,format=yuv420p" "$tmp/$clip-once.mp4"
  frames=$(ls "$dir"/game_*.png | wc -l | tr -d ' ')
  last="$dir/game_$(printf %04d "$frames").png"
  ffmpeg -loglevel error -y -loop 1 -t 0.5 -framerate 30 -i "$last" -loop 1 -t 0.5 -framerate 30 -i "$dir/side_$(printf %04d "$frames").png" \
    -filter_complex "[0][1]hstack=inputs=2,format=yuv420p" "$tmp/$clip-hold.mp4"
  : > "$tmp/$clip.txt"
  for i in 1 2 3; do printf "file '%s'\nfile '%s'\n" "$tmp/$clip-once.mp4" "$tmp/$clip-hold.mp4" >> "$tmp/$clip.txt"; done
  ffmpeg -loglevel error -y -f concat -safe 0 -i "$tmp/$clip.txt" -c copy "$out/$clip.mp4"
  printf "file '%s'\nfile '%s'\n" "$tmp/$clip-once.mp4" "$tmp/$clip-hold.mp4" >> "$list"
  # The sheet: every Nth frame of the game view over the side view, in a row (wrapped).
  every="${SHEET_EVERY:-2}"
  picks=()
  for ((f = 1; f <= frames; f += every)); do picks+=("$f"); done
  [[ "${picks[${#picks[@]}-1]}" != "$frames" ]] && picks+=("$frames")
  inputs=(); filters=""; n=0
  for f in "${picks[@]}"; do
    g="$dir/game_$(printf %04d "$f").png"; s="$dir/side_$(printf %04d "$f").png"
    inputs+=(-i "$g" -i "$s")
    filters+="[$((n * 2))][$((n * 2 + 1))]vstack=inputs=2,scale=300:-1[c$n];"
    n=$((n + 1))
  done
  cols=6
  stack=""; for ((i = 0; i < n; i++)); do stack+="[c$i]"; done
  # (xstack wants pixel offsets: each cell 300 wide, 600 tall)
  layout=""; for ((i = 0; i < n; i++)); do x=$(( (i % cols) * 300 )); y=$(( (i / cols) * 600 )); layout+="${x}_${y}|"; done
  if (( n > 1 )); then
    ffmpeg -loglevel error -y "${inputs[@]}" -filter_complex "${filters}${stack}xstack=inputs=$n:layout=${layout%|}:fill=black" "$out/$clip-sheet.png"
  else
    ffmpeg -loglevel error -y "${inputs[@]}" -filter_complex "${filters}[c0]copy" "$out/$clip-sheet.png"
  fi
  echo "[film] $out/$clip.mp4 ($frames frames; sheet every $every: ${picks[*]})"
done
ffmpeg -loglevel error -y -f concat -safe 0 -i "$list" -c copy "$out/all.mp4"
rm -rf "$tmp"
echo "$out"
