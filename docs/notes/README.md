# Notes for Claude

What Claude has learned working on the game with the user: their directions, how they like to work, where we are.
They live in the repo (not in Claude's per-machine memory) so every machine starts from the same place.
`CLAUDE.md` imports them, so they're loaded in every session.

**Keep them here.** When the user gives a direction that should last, corrects how you work, or a piece of work
pauses, write it into one of these files (or a new one, listed below and imported in `CLAUDE.md`), and commit it
with the work. Don't save project knowledge to Claude's own memory directory: it stays on one machine.
Update a note rather than adding a second one on the same subject; delete what turns out to be wrong.
Date what the user said (absolute dates).

- [Where we left off](where-we-left-off.md): the work in progress, for the next session to pick up
- [Set pieces before progression](set-pieces-before-progression.md): build set pieces in labs; placement and progression later
- [Creature done checklist](creature-done-checklist.md): swipes you can see, the Ledger tested in play, lock-on to parts, fitting numbers
- [Level editor](level-editor.md): levels are laid out in tools/level-editor.sh; how to read and build them
- [Test cadence](test-cadence.md): one scene's build and tests while iterating; everything only before a merge
- [The One Who Waits: two endings](one-who-waits-two-endings.md): limp in stopped time (bad ending), hard in running time
