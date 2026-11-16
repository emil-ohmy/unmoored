# Where we left off

_Updated 2026-11-16, on the PC (the first session there)._

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

**The user's view (2026-11-16): "not really finished, but it's getting there."**

Done 2026-11-16 on the PC, from the user's notes after play-testing:
- **The opening** (their idea): you lie still in complete darkness; a lantern on the floor before you kindles; the words
  "Apprentice. Take light."; a prompt; you take it and rise (TheOpening; the Lie and Wake clips). The cave's lights
  come back nearest first; the Hold is named once you stand. Not yet seen by the user in play.
- **Harder**: the Apprentice starts with 50 health and 60 stamina (was 100 each); a heal mends 22.5. Game-wide.
- **The shortcut gate** no longer floats between the walls: jambs of the living stone run from them to its posts.
- **The floor** isn't bare black: lichen, moss and fungus (a few glowing faint green) along the walls' feet, black
  puddles, and a few things the kept dropped (a shoe, a comb, coins...). The user chose these over roots/tendrils.

Next session: ask how the opening and the harder numbers feel in play, and whether the floor wants more or less (the
densities are GROWTH / DROPPED_PER_M2 in hold_drawn.py). Also check the level file for edits they made in the editor
(`git diff` on the json; their notes on floors and markers say what they want).

## On the PC

- Set up 2026-11-16: Unity 6000.6.8f1 (Unity CLI, `C:\Program Files\Unity\Hub\Editor\`), Blender 5.2.4, Git for
  Windows with LFS, ffmpeg 9.0, clone at `C:\Users\emile\Projects\unmoored`, on this branch.
- The `tools/*.sh` scripts now run on both machines (Git Bash on Windows): `tools/env.sh` finds Blender, its Python
  and Unity per OS. Checked on the PC: build-models, build-audio (byte-identical output), preview-poses, unity.sh
  open. Not yet re-checked on the Mac.
- On the Mac, port 8765 is the user's own watcher.py; the level editor uses 8777.
