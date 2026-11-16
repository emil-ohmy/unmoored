# Where we left off

_Updated 2026-11-16, on the Mac, before the user moved to working on their PC._

## The work in progress: the Hold, the tutorial level

Branch `feature/level-editor` (not merged to `main`). The repo is github.com/emil-ohmy/unmoored (the project's
codename, "unmoored"; the game is The Ferryman's Apprentice; the code still says MyGame).

The Hold is the game's opening, the tutorial, and the first level built from the level editor (see
[level-editor](level-editor.md)): `game/Assets/_Project/Levels/the_hold_spiral.json` → `hold_drawn.py` (the living cave
grown round its floors) → `MainSceneBuilder.HoldLayout` → `Lab_HoldLayout.unity` (Build Lab (The Hold, the level)).
`HoldLayoutTests` walk it as it's played and read the level's own file, so they follow its edits.

What's in it: one path winding a turn and a third inward (~195 m) to the Boss's chamber at the centre; the start on
the stone; the floor messages (dodge and strike, the Mooring post, healing, the hidden path, "Boss ahead"); the Kept
alone, then in twos; the Mooring post (the first lantern); a narrow optional way north to a grotto with a Warm Stone and
Obols; the door of eyes (a hint of a future path, says "..."); a one-way shortcut from the antechamber back to the
post; the arena's seal; the Hold, That Would Not Let Go; the way out behind its face, up to the Hub.

Done last (2026-11-03, commit 4ca1afd), after the user's play-testing: the seal truly sinks; the Boss's name comes up
only once its face is on screen; the walk from the seal to it is 11.6 m (was 19 m); its fall gets BossFall.Moment.

**The user's view (2026-11-16): "not really finished, but it's getting there."** Nothing specific was open when we
stopped. Next session: ask the user what they found in their last play-through, and check the level file for edits
they made in the editor since (`git diff` on the json; their notes on floors and markers say what they want).

## On the PC

- Unity 6000.6.8f1, Blender 5.2, Git LFS (`git lfs install` before cloning).
- The `tools/*.sh` scripts were written on macOS: Blender and Unity are looked up under `/Applications/` (overridable
  with `BLENDER=` and `UNITY=`, except Blender's Python in `build-audio.sh` and `record-demo.sh`). On Windows they need
  Git Bash or WSL and those paths. Making them work on both machines is likely the first job there.
- On the Mac, port 8765 is the user's own watcher.py; the level editor uses 8777.
