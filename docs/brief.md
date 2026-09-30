# The Ferryman's Apprentice: brief

> *The dead have stopped crossing. Take up your vanished master's oar (your weapon, your tool, your way across the water)
> and find out what he's been keeping from them.*

A small top-down 3D soulslike/metroidvania (about 1–2 hours) in the vein of Death's Door and Tunic. Hobby project:
Unity 6.6 URP, fixed angled camera, gamepad-first. All code and art are made by Claude; the user designs and art-directs.
Terms follow [`CONTEXT.md`](../CONTEXT.md); every line traces to a decision on the
[wayfinder map](../.scratch/game-brief/map.md).

## Pillars and theme

1. **One oar, many uses.** The oar is the only weapon. Every Ability is a new oar technique with a combat use *and* a traversal use.
2. **Deliberate, fair combat.** Stamina, readable telegraphs, few but dangerous enemies. Every death is the player's mistake.
3. **A world that folds back.** Islands and paths loop back through shortcuts, water routes and secrets.

**Theme: letting go.** The Ferryman hoarded the dead because he couldn't let them go. The Apprentice learns to.

## Story

The Apprentice, a small masked creature with an oversized oar, lives in a misty, flooded underworld of islands. The
tone is melancholic and whimsical, and death is everyday routine. The Ferryman has vanished and souls pile up on the
shore. He stopped letting them cross and started keeping them. The kept souls soured into the enemies. He is the final
Boss, and the Apprentice inherits his role. The mystery is told mainly through **Lingerers** (5–7 souls with unfinished
business) and the environment.

## Tone and voice

**The world is sincere; the game's own voice is allowed to be dry.** The underworld, its souls and its grief are
played straight. But the game knows what genre it's in, and where it speaks to you directly (its screens, not its
people) it may wink at being a soulslike. Remember to put the humour in: it's part of the game, not a garnish.

- **Where it jokes:** the game's own screens and descriptions. Boss defeat banners, item and Charm descriptions, the
  Obol counter, the death screen's rare variants, the merchant's patter, the occasional Lantern Lit.
  Examples: "Obols (souls, basically)"; the Tester's Knot "for play-testing"; "You Sank (Again)".
- **Where it never jokes:** Bosses and the lore themselves, their words and their stories. A Boss is never silly in
  the fight; the joke comes after, from the game.
- **Lingerers may be wry, in character.** A soul can have a dry wit of their own, and may notice, from inside the
  world, the things a soulslike does (the Apprentice rolling about, dying and walking back dripping, Resting and every
  Soured getting back up, carrying a fortune past monsters), without ever naming the genre. Their grief is played
  straight: the joke never lands on the line that should hurt. Old Hesper: *"That post of yours. You sit at it, and
  every one of those poor things out there gets back up. Did you notice? I'd find another post."*
- **The pattern for a Boss beaten: the joke big, the sincere line small beneath it.** The laugh lands first, then the
  quiet line catches you. The Anchoress: **"ANCHOR DOWN"** / *She let go.*
- **Sparingly:** a joke that's always there wears out. Rare variants (a death screen now and then, not every time),
  one per Boss, and never over a moment that should hurt.

## Core loop

Row out to an Area. Fight Soured, find Obols and secrets, light Mooring posts and open shortcuts back to them. Beat the
Area's Boss to gain an Ability. Return to the Hub and reach places you couldn't before. On the side: resolve Lingerers'
business and pay their fare to row them across.

## Combat

- **Pace:** "Middle", between Death's Door's snappiness and Tunic's weight.
- **Verbs:** move, a short dash (2.8 m) with invincibility from its first frame, shown as ghost images (it can cancel an
  attack's recovery), a 2-swing light combo and a heavy attack. Attacks go where the stick points.
- **Block and perfect block:** holding block lets 30% through and costs stamina (empty stamina breaks the guard). A
  block raised within 0.2 s of the blow is a **perfect block**: no damage, and the attacker is always **stunned**, open to
  double-damage **ripostes**. Mashing block earns no perfect window.
- **Dodging is timing:** Soured follow their target through 90% of a wind-up and lunge after it, so a panic dash gets
  caught; the i-frame window (about 270 ms before the blow) is what saves you. Backing off still escapes some attacks,
  but leaves you too far away to punish.
- **Lock-on:** lock onto a nearby Soured (and flick between targets): the Apprentice keeps facing it, every swing
  goes at it, and a directionless dash becomes a backstep. Only the locked target shows a health bar; the Stagger
  meter stays hidden.
- **Stamina:** all actions cost stamina. Any stamina left lets you act. It regenerates after a short pause.
- **Stagger meter:** hidden. Light hits fill it a little, heavy hits a lot, and it drains if you let up. When full,
  the Soured is staggered and its attack is interrupted.
- **Feel:** hit-stop, knockback and shake on every hit.
- **Telegraphs:** readable body wind-ups in animation only. No ground markers.

## Death, rest and healing

- **Mooring posts** are the checkpoints: moor, light the lantern, **Rest**. Resting heals fully, refills
  **Healing charges** (3 at the start), saves, and respawns every Soured except defeated Bosses.
- **Density:** a few posts per Area, always one before a Boss.
- **Death:** you wake at the last post and drop **all** Obols with your **Shade**. Touch it to get them back. Die
  again first and they're gone.
- **Edge cases:** dying in a Boss fight leaves the Shade outside the arena. You can't die on the water (rowing).
- **Falling in:** costs a third of your health and you wash ashore on solid ground nearby, coughing (a long
  stagger). Never instant death (knockback and breaking floors would make that cheap), but when you're low the
  water finishes you.

## Progression

- **Abilities:**
  - **Hook**, from Boss 1: pull to posts over water, yank Soured close, rip off shields.
  - **Stroke**, from Boss 2: row against currents, and send a wave that knocks Soured back.
  - **Vault**, a secret in Area 1: leap gaps, walls and attacks, and slam down. It's optional early and required
    in Area 3, which hints where it is.
- **Bow lantern:** never required. Carried at the Apprentice's hip on land and hung on the bow when rowing; its cold
  light grows with every lit Mooring post and burns through fog to optional routes and secrets. Fully powered, it
  unlocks a secret ending.
- **Oar attachments,** one fitted at a time and swapped at posts. The Plain blade you start with, plus:
  - **Iron blade:** heavy, big stagger.
  - **Light blade:** fast, and carries the third light swing.
  - **Long shaft:** reach.
  - **Barbed blade:** slows the stagger meter's drain.
  - Three are found, one per Area, and one is sold.
- **Charms:** about 8, some with trade-offs. 1 slot at the start, plus 2 bought. Half are found, half sold.
- **Stats are never bought.** Max-HP and Healing-charge pieces are found behind secrets.
- **Obols**, the fare of the dead, buy:
  - Oar attachments, Charms and slots from the merchant soul in the Hub
  - consumables
  - **passage for souls**

## Souls

- **Lingerers** have varied business: a lost keepsake, a message to deliver, or something to face.
- **A Crossing** needs the business resolved *and* their fare paid in Obols. Paying is always optional.
- **The Crossing itself:** a short, calm row from the Hub jetty to the Far shore. It gives a parting gift (a Charm or
  an upgrade piece) and a memory of the Ferryman.
- **Defeat only scatters the Soured.** The Ferryman's hoard pulls them back. When he falls, all are released.

## World

The **Hub** is the Ferryman's jetty, at the centre of the underworld. It has his hut, the merchant soul, waiting
Lingerers and one Mooring post. The Areas are spokes that link to each other through shortcuts opened from the far side,
so you can loop. There's **no fast travel**: rowing and shortcuts are the travel, and each Boss arena opens a shortcut
back toward the Hub.

| # | Area | Reached by | Boss | Reward | Introduces |
|---|---|---|---|---|---|
| 1 | The Drowned City | open | The Antediluvian Anchoress | Hook | Laden, Clinger |
| 2 | The Orchard of Last Autumn | Hook | The Gardener | Stroke | Moths |
| 3 | The Stopped Clocktower | Stroke (+ Vault inside) | The One Who Waits | the way to the Ferryman | Keener |
| – | Beneath the Hub (the water drains) | after Boss 3 | The Ferryman | the ending | |

**Minibosses:** each Area has one, met on the way (a name plate, a bar and a farewell, but no Ability). The Drowned
City's is **the Unparted, of the Last Embrace**: Hold and Fast, two souls fused into one great Clinger. Its farewell:
**"TILL DEATH DID THEY PART"** / *Again.*

The **Drowned** appear at water edges everywhere.

**Heights** (the Areas have verticality: walkways, towers of several storeys, platforms):
- Drops up to about 1.5 m can be taken, one way (a shortcut down). Taller edges are always walled or railed. No
  falling damage: heights are routes, not traps.
- Inside a building, the storeys above the Apprentice fade away; architecture between the camera and them opens up.
- The Soured find their way (up stairs, round walls) but never walk off a ledge, and never strike through a floor.
  Broad ones (a Laden) can't take a narrow stair; slim ones (an archer, a Clinger) can. Archers aim up and down.

## Soured families

Each couldn't let go of something. Weaknesses are advantages, never requirements.

| Family | Couldn't let go of | Role | Weak to |
|---|---|---|---|
| Laden | things | Tank; approaches with its heap of belongings low (open), then up close lifts it as a shield that blocks frontal hits; two attacks: an overhead slam along a line (long recovery) and a wide low sweep for anyone at its flank or back (short recovery); huge stagger meter | Hook |
| Clinger | a person | Fast pairs that circle, leap and cling to your back: stamina drains (and a little health), you slow; dash to throw off (even spent). Scatter one and the other turns frantic | Stroke |
| Keener | grief | Ranged wailer; dash through its sound rings; fragile | Vault |
| Moths | memories | Flock of one-hit wisps that circle and dive | Long shaft, heavy sweep |
| Drowned | life | Drags you under water: damage, then you wash ashore (not death) | Hook |

## Art direction

- **Shapes:** faceted low-poly. Characters are built from simple, readable forms with a clear silhouette from above.
- **Shading:** stepped toon light with lilac-tinted shadows, dark outlines, a soft rim, fog and bloom.
- **Palette:** a muted, cold teal-grey underworld, with warm focal accents: the Apprentice's red hood, the Orchard's
  autumn colours, lanterns and eyes.
- **Darkness:** it is always night. A weak, cold moon; the world is revealed by its fires (torches, lanterns). Fog,
  film grain and a heavy vignette keep it gritty.
- **Light carries meaning:** warm orange is the world and safety (torches, Mooring posts); sickly green is a Soured
  (their belt lanterns and eyes: green light in the dark means danger); cold pale blue is the Apprentice's bow lantern.
- **Pipeline:** everything is modelled and animated from Blender scripts, and art direction happens by reacting to
  renders. The reference is the `prototype/art-test` branch.
- **Pickups glow:** everything that can be taken glows, so it never has to be hunted for. Items glow softly and warm
  (a few embers rising); key items (Abilities) glow brightly in the Apprentice's cold pale blue, the object itself lit
  and a column of motes rising, readable from across an Area. Not yet takeable (sealed): only a faint glow.
- **Ability gates keep their mystery:** what an Ability will use never glows or gets marked before you have it. It
  gets noticed instead: a silhouette found nowhere else and the same every time, a story on it (a hook post's snapped
  rope trailing into the water), the Ferryman's mark (an oar through a ring) on it and on the Ability's relic so the
  connection clicks, and a single moonlight glint as you walk past. Place the first ones early, each beside something
  you want, so they're seen and remembered before the Ability is found.
- **UI:** sparse and printed. One typeface, IM Fell English (worn old print), its small caps for titles. Bone-white
  text, bronze frames, warm ember for safety, blood red for death. No combat text: hits, blocks and dodges read from
  sparks, flashes and sounds. Messages are few and ceremonial: the Area's name on entering, "Lantern Lit" when a
  Mooring post is first lit, and on death "You Sank" before the screen sinks to black and you wake at the post. A
  Boss: its name revealed large the first time its fight begins, its bar along the bottom, and when it's beaten, its
  own banner (see Tone and voice).

## Scope

A 1–2 hour game:
- the Hub and 3 Areas
- 3 Bosses and the Ferryman
- 3 Abilities and the bow lantern
- the Plain blade and 4 attachments
- about 8 Charms
- 5–7 Lingerers
- 5 Soured families

**Not decided here:** detailed content (rooms, stats, movesets, item lists), what the secret ending shows, and technical
choices. The pipeline findings for those are on the art-test ticket.
