# Sourced by the tools: where things live on this machine, so they run on macOS and on Windows (Git Bash).
#   ROOT            the repo (on Windows as C:/..., which Blender, Unity and ffmpeg all take)
#   BLENDER         Blender (override: BLENDER=/path/to/blender)
#   BLENDER_PY      Blender's bundled Python, which has numpy (override: BLENDER_PY=...)
#   UNITY_VERSION   the Editor the project was saved with (game/ProjectSettings/ProjectVersion.txt)
#   UNITY           that Editor (override: UNITY=/path/to/Unity)
#   native <path>   a path as native programs want it;  make_tmp   a temp dir, the same way

case "$(uname -s)" in
  Darwin)               OS=mac ;;
  MINGW*|MSYS*|CYGWIN*) OS=windows ;;
  *)                    OS=linux ;;
esac

if [[ $OS == windows ]]; then
  # Hand every path over as C:/... ourselves, and stop Git Bash from rewriting arguments behind our back
  # (it would mangle ffmpeg filters and Unity flags that merely look like paths).
  export MSYS_NO_PATHCONV=1
  native() { cygpath -m "$1"; }
else
  native() { printf '%s\n' "$1"; }
fi
make_tmp() { native "$(mktemp -d)"; }

ROOT="$(native "$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)")"

# Newest match of a glob (version-sorted), or nothing.
newest() { ls -d "$@" 2>/dev/null | sort -V | tail -1; }

if [[ -z "${BLENDER:-}" ]]; then
  case $OS in
    mac)     BLENDER=/Applications/Blender.app/Contents/MacOS/Blender ;;
    windows) BLENDER="$(newest "C:/Program Files/Blender Foundation/"Blender*/blender.exe)" ;;
    linux)   BLENDER="$(command -v blender || true)" ;;
  esac
fi
[[ -n "$BLENDER" ]] || BLENDER=blender # (says "not found" when used, not before)

if [[ -z "${BLENDER_PY:-}" ]]; then
  case $OS in
    mac)     BLENDER_PY="$(newest "$(dirname "$BLENDER")"/../Resources/*/python/bin/python3.*)" ;;
    windows) BLENDER_PY="$(newest "$(dirname "$BLENDER")"/*/python/bin/python.exe)" ;;
    linux)   BLENDER_PY="$(newest "$(dirname "$(readlink -f "$BLENDER")")"/*/python/bin/python3.*)" ;;
  esac
fi

# ProjectVersion.txt may have Windows line endings: drop the \r.
UNITY_VERSION="$(sed -n 's/^m_EditorVersion: //p' "$ROOT/game/ProjectSettings/ProjectVersion.txt" | tr -d '\r')"
if [[ -z "${UNITY:-}" ]]; then
  case $OS in
    mac)     UNITY="/Applications/Unity/Hub/Editor/$UNITY_VERSION/Unity.app/Contents/MacOS/Unity" ;;
    windows) UNITY="C:/Program Files/Unity/Hub/Editor/$UNITY_VERSION/Editor/Unity.exe" ;;
    linux)   UNITY="$HOME/Unity/Hub/Editor/$UNITY_VERSION/Editor/Unity" ;;
  esac
fi

# Fail early, by name, when a tool a script needs isn't installed:  need blender unity ffmpeg
need() {
  local missing=0
  for tool in "$@"; do
    case $tool in
      blender)    [[ -x "$BLENDER" ]] || command -v "$BLENDER" >/dev/null || { echo "Blender not found ($BLENDER); set BLENDER=..." >&2; missing=1; } ;;
      blender-py) [[ -n "$BLENDER_PY" && -x "$BLENDER_PY" ]] || { echo "Blender's Python not found; set BLENDER_PY=..." >&2; missing=1; } ;;
      unity)      [[ -x "$UNITY" ]] || { echo "Unity $UNITY_VERSION not found ($UNITY); install it (unity install $UNITY_VERSION) or set UNITY=..." >&2; missing=1; } ;;
      *)          command -v "$tool" >/dev/null || { echo "$tool not found on PATH" >&2; missing=1; } ;;
    esac
  done
  (( missing == 0 )) || exit 1
}
