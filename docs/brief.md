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

## Core loop

Row out to an Area. Fight Soured, find Obols and secrets, light Mooring posts and open shortcuts back to them. Beat the
Area's Boss to gain an Ability. Return to the Hub and reach places you couldn't before. On the side: resolve Lingerers'
business and pay their fare to row them across.

## Combat

- **Pace:** "Middle", between Death's Door's snappiness and Tunic's weight.
- **Verbs:** move, a dash with invincibility frames, shown as ghost images (it can cancel an attack's recovery), a 2-swing light combo
  and a heavy attack. Attacks go where the stick points.
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
- **Edge cases:** dying in a Boss fight leaves the Shade outside the arena. You can't die on the water.

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
| 1 | The Drowned Village | open | The Hoarder | Hook | Laden, Clinger |
| 2 | The Orchard of Last Autumn | Hook | The Gardener | Stroke | Moths |
| 3 | The Stopped Clocktower | Stroke (+ Vault inside) | The One Who Waits | the way to the Ferryman | Keener |
| – | Beneath the Hub (the water drains) | after Boss 3 | The Ferryman | the ending | |

The **Drowned** appear at water edges everywhere.

## Soured families

Each couldn't let go of something. Weaknesses are advantages, never requirements.

| Family | Couldn't let go of | Role | Weak to |
|---|---|---|---|
| Laden | things | Tank; approaches with its heap of belongings low (open), then up close lifts it as a shield that blocks frontal hits; huge stagger meter | Hook |
| Clinger | a person | Fast pairs that latch on and drain stamina; dash to throw off | Stroke |
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
