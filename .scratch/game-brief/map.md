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

- [Oar attachments and Charms](issues/10-oar-attachments-and-charms.md): Plain blade + Iron / Light (3rd swing) / Long / Barbed, one at a time; ~8 Charms, 1→3 slots; attachments found (one per Area) + one sold.
- [World structure: hub, areas, bosses, shortcuts](issues/08-world-structure.md): the Ferryman's jetty as hub; Drowned Village (Hoarder) → Orchard of Last Autumn (Gardener) → Stopped Clocktower (One Who Waits); linked spokes, no fast travel; finale beneath the hub.
- [Abilities and the gates they open](issues/07-abilities-and-gates.md): Hook (Boss 1), Stroke (Boss 2), Vault (secret in Area 1, required in Area 3); the bow lantern is optional, grows with lit Mooring posts and unlocks a secret ending.
- [Combat feel: core verbs and pace](issues/05-combat-feel-prototype.md): Middle pace; move, i-frame roll (cancels recovery), 2-swing light (3rd unlockable) + heavy, stamina, hidden stagger meter; telegraphs via animation, no ground markers.
- [Souls with unfinished business](issues/09-souls-with-unfinished-business.md): 5–7 Lingerers with varied business; Crossing = business done + fare paid (optional), rowed from the hub jetty for a gift + a Ferryman memory; enemies are the Soured, scattered not released until the Ferryman falls.
- [Death, checkpoints and healing](issues/04-death-checkpoints-and-healing.md): rest at Mooring posts (heal, refill 3 Healing charges, save; enemies respawn except Bosses); dying drops all Obols with a retrievable Shade.
- [What currency is for](issues/03-currency-and-what-it-buys.md): Obols (fares of the dead) buy oar attachments, charms/slots, consumables and passage for souls; stat upgrades are found, never bought.
- [Hook, protagonist and pillars](issues/01-hook-and-pillars.md): The Ferryman's Apprentice, an oar-first soulslike in a flooded underworld; theme "letting go"; pillars one oar / deliberate combat / world that folds back; the oar is the only weapon.
- [How Death's Door and Tunic handle death, currency, healing and checkpoints](issues/02-reference-death-and-economy-research.md): both sell capped stat upgrades and both respawn enemies at rest points; Death's Door costs only time on death, Tunic drops coins as a retrievable ghost.

## Not yet specified

- **Soured families**: enemies are the Soured (souls kept too long). Which 3–5 archetypes the brief commits to
  (brief-level only, e.g. "a shield-bearer that punishes button mashing"), how their shapes echo who they
  were, and how their stagger meters differ. Combat verbs are known now; waits on what the art test shows is animatable.

## Out of scope

- Detailed content design (per-enemy stats, boss movesets, room layouts, item lists): this comes after the
  brief, when building each area.
- Tech decisions (save system, UI framework, audio pipeline): not needed to lock the brief.
- What the secret ending shows: story content for after the brief. The brief records only that it exists and that a fully
  powered bow lantern unlocks it (see [Abilities and the gates they open](issues/07-abilities-and-gates.md)).
