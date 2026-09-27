# Map: Game brief
Label: wayfinder:map

## Destination

`docs/brief.md`: a one-page brief with pillars, hook, core loop, soulslike rules, metroidvania rules,
art direction and scope locked, concrete enough to start building the combat grey box without guessing.

## Notes

- Domain: game design for a top-down (fixed angled camera) 3D soulslike/metroidvania in Unity 6.6 URP.
- Hobby/learning project. Team: the user (design, playtesting) + Claude (all code AND all Blender art,
  via Python scripting). A few hours/week, so scope must stay small.
- Every grilling ticket: load the `grilling` and `domain-modeling` skills; keep `CONTEXT.md` current.
- When no open tickets remain, the way is clear: write `docs/brief.md` from Decisions so far (no new decisions).
- User prefers quiz format (multiple choice via AskUserQuestion, recommendation first).
- Starting leanings from charting (2026-09-27), to confirm in the relevant tickets, not yet decisions:
  - References: Death's Door, Tunic.
  - Tone: melancholic-whimsical (Death's Door-like).
  - Camera: fixed angled, never rotates. Input: gamepad-first, attacks go where you face.
  - Art: low-poly, toon-shaded.
  - Story: environmental + a few NPCs with short lines.
  - Scope: 1–2 h game: hub + 2–3 areas, 2–3 bosses, 2–3 abilities.
  - Souls: deliberate stamina combat, a death penalty, bosses as area climaxes.
    Not picked: checkpoints with enemy respawn.
  - Metroidvania: ability gates, interconnected world with shortcuts, secrets. Not picked: in-game map.
  - Progression: abilities + equipment (equipment narrowed to oar attachments + charms in the hook ticket). Not picked: stat upgrades.
  - Healing: limited charges.

## Decisions so far

- [Hook, protagonist and pillars](issues/01-hook-and-pillars.md): The Ferryman's Apprentice, an oar-first soulslike in a flooded underworld; theme "letting go"; pillars one oar / deliberate combat / world that folds back; the oar is the only weapon.
- [How Death's Door and Tunic handle death, currency, healing and checkpoints](issues/02-reference-death-and-economy-research.md): both sell capped stat upgrades and both respawn enemies at rest points; Death's Door costs only time on death, Tunic drops coins as a retrievable ghost.

## Not yet specified

- **Equipment's shape**: equipment is oar attachments + charms (decided in the hook ticket), but how many,
  what they change, and how they're found depends on the combat verbs and the currency decision.
- **Enemy families**: which 3–5 enemy archetypes the brief commits to (brief-level only, e.g. "a shield-bearer
  that punishes button mashing"). Depends on the combat verbs and the setting.

## Out of scope

- Detailed content design (per-enemy stats, boss movesets, room layouts, item lists): this comes after the
  brief, when building each area.
- Tech decisions (save system, UI framework, audio pipeline): not needed to lock the brief.
