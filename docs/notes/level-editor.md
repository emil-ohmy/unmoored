# Level editor

Since 2026-11-02 the user lays out levels in the repo's own level editor (`tools/level-editor.sh [level]`: a page at
http://localhost:8777, its server saves straight to `game/Assets/_Project/Levels/<level>.json`, format "level/2").

- Floors: path {points, width}, circle {e, n, r}, rect {e, n, w, h, rot}, each with `top`; markers {code, label, note};
  free notes. Every floor and marker can carry a `note`: the user's intent ("a flat wall here", "a door of eyes,
  hints at a future path, says '...'"): read every note before building, and say how each was read.
- The Hold's level is `the_hold_spiral.json`; `hold_drawn.py` builds the cave from it, `MainSceneBuilder.HoldLayout`
  the scene (Lab_HoldLayout). The tests read the level file, so the user can edit it and just rebuild.
- Unsaved edits live only in the user's browser (autosave); the editor offers them back on reload. If a save
  isn't in the server log (POST /api/level), the file on disk is stale: tell the user to press Ctrl/⌘S.
- On the Mac, port 8765 belongs to the user's own watcher.py: never kill it; the editor uses 8777.

**Keeping the editor in sync (the user's routine, 2026-11-16): the level file is the source of truth for the layout,
for the user and for Claude alike.** So that the editor never shows a level that isn't the game's:
- Every change Claude makes to a level's layout goes into `Levels/<level>.json` (a marker moved or added, a floor
  changed), with a `note` saying why, then the rebuild (hold_drawn.py, then the scene). Never only in a builder, a
  model script or the scene: if the scene has something in a place, a marker or floor in the file put it there.
- A new kind of thing in a level gets a sticker in the editor's palette (`GROUPS` in tools/level-editor/index.html), so
  the user sees it and can move it (VOICE, MRN were added this way).
- Things that aren't layout (the floor's small things scattered by hold_drawn.py, the gate's wall, the opening's lantern
  beside the start) are derived from markers and floors: they move when those do.
- The editor watches the file: changed on disk with nothing unsaved in it, it reloads itself; with unsaved edits, it
  warns. The server refuses a save made over a version changed since (409) and the editor asks first; restoring old
  unsaved edits warns when the file changed since. So Claude can edit the file while the editor is open; say so.
- At the end of a session's level changes, say which markers and floors changed, so the user knows what moved.

How to read a layout (promised to the user): snap to ~0.5 m and keep proportions; fix the hard rules (Laden 1.3 m clear,
doors 2.2 m, dash 2.8 m, nothing tall south of paths: the camera looks north from the south) and say what was moved.
In a cave: wall pieces go on the nearest wall face; a Kept drawn on the floor tears out of the nearest wall and wakes
as you pass its spot; walls south of a floor are kept low; an open far end gets "the way on" and a barrier.

History: before the editor, levels were drawn in Figma ("The Ferryman's Apprentice: Level Design",
https://www.figma.com/design/MysuLAJcAzztDJRDTszuD0, the user's Starter team); dropped when the Starter plan's MCP
call limit ran out. The world map (the Hub, the Areas, the Hold, the Far shore, and how they link) still lives there.

See [set pieces before progression](set-pieces-before-progression.md), [test cadence](test-cadence.md).
