# Combat feel: core verbs and pace
Type: prototype
Status: resolved
Blocked by:

## Question

What are the core combat verbs and the pace, somewhere between Death's Door (snappy dodge) and Tunic
(stamina-heavy)? Build a grey-box with the fixed angled camera, gamepad-first: move, dodge roll with i-frames,
light + heavy attack with the oar (the only weapon: long reach, some weight), stamina, hit-stop/knockback, one enemy with a telegraphed attack. The user plays it
and picks the pace. The answer records the verbs and a rough tuning (e.g. roll distance/cost, attack speed).

## Answer

Resolved 2026-09-27: the user played all three pace presets in the grey-box and chose **Middle**.

Prototype (primary source): branch `prototype/combat-feel` (commits a74d1de, bfce67d),
`game/Assets/_Prototypes/CombatFeel/`. Rebuild with right-click in Project window > MyGame > Build Combat Feel Scene.

- **Core verbs**: move; dodge roll with i-frames; light oar attack (2-swing combo); heavy oar attack.
- **Stamina**: rolls and attacks cost stamina; any stamina left lets you act (the cost may take you to zero);
  regenerates after a short delay, not while rolling/attacking.
- **Roll cancels attack recovery**: yes.
- **Third light swing**: unlockable (user's idea). By the pillars it's likely an Oar attachment or Charm rather than
  an Ability, since Abilities also need a traversal use.
- **Attack direction**: attacks go where the stick points at the moment of the swing (gamepad-first).
- **Hit feel**: hit-stop, knockback and camera shake on every hit.
- **Stagger meter (hidden)**: light hits fill it a little, heavy hits a lot; full = the Soured is staggered and its attack
  interrupted. The meter drains when the Apprentice stops hitting. Replaces the prototype's "only heavy interrupts" rule.
- **Telegraphs**: clear body wind-ups as animations. The prototype's red ground line and colour flashes were placeholders;
  the final game has no ground markers. The art test must therefore prove Claude can script readable animations.
- **Rough tuning (Middle)**: move 6 m/s; roll 0.38 s over 4 m, i-frames 0.05–0.28 s, recovery 0.12 s, cost 20;
  stamina 100, regen 45/s after 0.5 s; light 0.13/0.10/0.28 s (windup/active/recovery), cost 15, 14 dmg, reach 2.3 m;
  heavy 0.5/0.12/0.5 s, cost 30, 35 dmg, reach 2.7 m; input buffer 0.2 s; hit-stop 0.06 s light / 0.14 s heavy;
  Soured telegraph 0.7 s, recovery 0.8 s, 25 dmg vs Apprentice HP 100.

## Comments

- 2026-09-28: after playing the first slice the user swapped the dodge roll for a **dash** (same Middle timing and
  i-frames: 0.38 s over 4 m, invulnerable 0.05–0.28 s). The i-frames are shown as fading ghost images. The brief
  now says dash everywhere (including the Clinger and Keener lines).
- 2026-09-28 (later): the user asked for a tighter fight. Added block and perfect block (0.2 s window, always stuns;
  ripostes do double damage; no perfect window when re-raised within 0.4 s). A frame-by-frame play-test of every defence
  against every attack showed dodging needed no timing (the 4 m dash out-ranged any attack) and the i-frames started
  50 ms late. Now: i-frames from frame 0, Soured track 90% of the wind-up and lunge before the blow, and the dash is
  2.8 m (the user chose it) so dodging is a ~270 ms timing window.
