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
- **Poison** (a Drowned's spit): it builds up in a meter in the middle of the screen, under the Apprentice. Two spits
  taken fill it (a plain block lets a little through), and it ebbs if you keep clear. Full, you're poisoned: about 40 health over 20 s, unless you cure it with a Bitter Root or
  a Rest.
- **Alight:** fire that lands sets the Apprentice alight at once: the Watchman's fire taken (not blocked or parried),
  a scarecrow going up beside you, standing in burning pitch (your own too). For 9 s health and stamina burn away,
  slowly (about 18 health; no breath comes back while it lasts): time to put it out. More fire keeps it going longer
  (3 s more each time), never past 14 s. Water puts it out (going under: you wash ashore, as ever), and so do a
  Sodden Sack and a Rest. No meter: fire is sudden.
- **Warnings** (poisoned, alight) are said as a find is, but in red: a blood-red glow and rule, the words warmed to
  it, below the poison meter.
- **Buttons:** few, and kept that way as Abilities arrive. Gamepad: A interact, B dash, X light, Y heavy, LT block,
  LB heal, RB the Hook, R3 lock on (right stick: switch target), Start pause, Select the character screen.
  - **Only the big Abilities get a button of their own,** on the shoulders and triggers: the Hook on RB, the Vault on
    RT (heavy stays on Y alone), and the Vault then an attack for its slam.
  - **The rest extend what's there,** by context and combination (held or tapped, in the air, while rowing, locked
    on). The Stroke needs no button: the heavy attack while rowing is its wave. The Hook already does this, doing a
    different thing for a post, a Soured and something too heavy.
  - **The d-pad is for Provisions:** left/right choose along the belt, up uses (down is free for now). On a
    keyboard, the arrow keys; so the arrows don't move the Apprentice (WASD does).
  - **Rebindable before release.** Every binding lives in one place.
  - **Hints show the controls last used:** a gamepad's buttons or the keyboard's keys, never both.

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
  light grows with every lit Mooring post and burns through fog to optional routes and secrets. It has five levels,
  counted against every Mooring post in the game. Fully powered, it is the key to a secret last Boss and ending; the
  game never says so.
- **One oar, strengthened.** No other oars and no attachments: the oar itself is made stronger in five steps, each
  with its own look, name and description (the game's voice, dry). Each step costs **Ferry Nails** (1, 2, 3, then 4:
  ten in all), spent at a Mooring post. Steps go in order, so however many nails are found early, no step is skipped.
  Each step is a moment, like taking an Ability: the Apprentice holds the oar up high, the world dims, and the oar
  changes in the flash.
  Each hits harder and staggers more (x1, 1.2, 1.45, 1.7, 2).
  Each step looks more over the top than the last, and from the second on it's fitted in metal:
  1. **The Apprentice's Oar:** plain wood, worn smooth, with a red band.
  2. **The Oarhand's Oar:** tarred and bound with twine, with brass at the butt, the neck and across the blade, and a
     red ribbon.
  3. **The Boatman's Oar:** banded in iron. The blade is broader, with an iron rim, a point and rivets, and there's a
     boat-hook at the neck.
  4. **The Steersman's Oar:** black wood with a notch for every crossing, wound in silver wire. The leaf-shaped blade
     is framed in silver, with a crescent on each face and fins at its neck, and two Obols hang on cords.
  5. **The Ferryman's Oar:** solid gold. It has a spiral ridge, a gold ring round the shaft (his mark, made whole) and
     a great flared blade with his mark glowing on both faces.
- **Charms:** about 8, some with trade-offs. 1 slot at the start, plus 2 bought. Half are found, half sold.
- **Stats are never bought.** What makes the Apprentice stronger for good is found, often behind secrets:
  - **Ferry Nails:** for the oar.
  - **Warm Stones:** more health (+10 each).
  - **Held Breaths:** more stamina (+10 each).
  - **Embers:** one more Healing charge each.
  The starting health and stamina (100) and the bonus each piece gives are placeholders, to be balanced together.
- **Provisions:** things used up, carried in **the Satchel**, and used from **the belt**: up to four in a row at the
  HUD's bottom left. Left/right on the d-pad (the arrow keys) choose along it, and up uses the chosen one. They're tools, not a second supply of healing (bar one gentle one).
  - Bought from the merchant soul with Obols, and found in the world. Each kind has a carry cap, and Resting doesn't
    refill them.
  - **Pitch Pot** (5): thrown ahead, or at what you're locked on to. It bursts into a pool of burning pitch, and
    whatever stands in it burns; no guard stops fire.
  - **Ember Tar** (3): the oar burns for 30 s. Blows land harder (×1.3) and set the Soured alight.
  - **The Way Back** (3): to black, and back at the last Mooring post you Rested at. Not in a Boss's arena, and not
    on the water.
  - **Warm Broth** (3): mends 30 health over 10 s, and a blow doesn't stop it.
  - **Bitter Root** (3): cures poison and empties its build-up.
  - **Sodden Sack** (3): old sacking soaked through, thrown over yourself: puts out the flames at once.
  - The belt has four slots that keep their places; an empty one is passed by. The first of a kind goes in the first
    empty slot. The Satchel page works slot-first: pick a slot (left/right), then choose what goes in it (up/down,
    with the chosen one turning, as the Oar page shows the oar). One put in another's slot swaps with it, and "empty"
    clears a slot.
- **The character screen** (I or C; Select on a gamepad) pauses the game. LB / RB (Q / R) turn its pages, which leaves
  left/right for moving within a page. It has seven pages:
  - **The Apprentice:** health, stamina, Healing charges, Ferry Nails and Obols.
  - **The Oar:** its step, name, description and strength. The next step shows as "???" with its cost.
  - **Abilities:** each one found, with what it is, how it's used (keyboard and gamepad) and what it's for. The list
    scrolls, since how many there will be isn't settled.
  - **The Satchel:** the Provisions carried, how many of each, what they do, and which go on the belt.
  - **Charms:** those found and those fitted, the slots, and a dark socket for each still to find.
  - **The Ledger:** every kind of Soured scattered, and every miniboss and Boss beaten, written in the first time,
    in the order they're met: its model turning, what it is and where, what it couldn't let go of, what's known of how
    to deal with it, and how many times. Only those met are listed (how many there are isn't said). A new Soured gets
    a quiet line as it's written in ("Written in the Ledger: the Laden"); a Boss's farewell is enough. The lab's
    Tester's Post can write in the whole Ledger.
  - **The Lantern:** a glow, five flames and one line. It says as little as it can.
- **Obols**, the fare of the dead, buy:
  - Charms and slots from the merchant soul in the Hub
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
| 2 | The Orchard of Last Autumn | Hook | The Gardener (Father Moth, Who Gathered Autumn) | Stroke | Moths, Gleaners, Threshers |
| 3 | The Stopped Clocktower | Stroke (+ Vault inside) | The One Who Waits | the way to the Ferryman | Keener |
| – | Beneath the Hub (the water drains) | after Boss 3 | The Ferryman | the ending | |

**The Gardener**, the Orchard's Boss, in full **Father Moth, Who Gathered Autumn**: the man who tended the orchard and
its glasshouse, who couldn't let go of the last autumn. He gathered every memory of it, and the memories gathered to
him as the Moths gather to a light, until he grew into the greatest of them: a vast moth (wings like dead autumn
leaves, six metres across) with the stooped gardener still rising from it, in his apron, his forearms grown into its
forelegs, ending in pruning shears; in his ribs, caged, a small lantern: the last light he tended. His arena: **the
Glasshouse**, beyond the Unreaped Rows' far gate (the Watchman guards the way), a ruined palm house of iron ribs and
broken panes, raised beds of dead plants, shallow water channels (you wade them; they put out flames), lamps hung on
chains, a dead orange tree against its north wall (he hangs folded on it, like a leaf, until you come in); its middle
left open, his floor.
- **Kept simple:** two attacks, his dive (and his shears when he lands from it) and his swoop.
- **Phase 1:** he circles the lamps out of reach (the oar passes under him). His dive: a stall, wings flung wide,
  the light in his chest flaring (the tell), then down at you. Dodge it and he lands where you stood, snapping his
  shears round him at once (keep clear, block, or parry: a perfect block knocks him down); then he's on the floor a
  moment, open, before he's up again. A perfect block on the dive itself knocks him down, stunned, every blow a riposte.
  His swoop: his wings snapped right back and one short flare of the lantern (the tell), dropping low; then level
  across the floor at chest height and up again past you. Dodging only saves you (he never lands from it); a perfect
  block knocks him down. Which one is coming is the read: against the dive both answers pay, against the swoop only the
  parry does.
- **The Hook** catches him only in a tell (either): dragged down out of it, the dive never comes (a shorter stun than a
  parry's). Any other time it glances off.
- **Phase 2 (half):** he tears the lamps down one by one; the glasshouse goes dark but for the moon and your bow
  lantern. The same, but two at a time (dive and dive, dive and swoop, swoop and dive): the second straight after
  the first, with a shorter tell, and he lands only after a last dive. Parry the first and the second never comes.
- **Fire** hurts him badly (he's a moth, and the Orchard burns), but doesn't lure him.
- His farewell: **"MOTHBALLED"** / *Winter is nigh.* He gives up the **Stroke**.

**Boss arenas:** nothing that follows the Apprentice crosses into a Boss's (or miniboss's) arena. It stops at the
edge and gives them up, and nothing outside notices them while they're in it.

**Minibosses:** each Area has one, met on the way (a name plate, a bar and a farewell, but no Ability). The Drowned
City's is **the Unparted, of the Last Embrace**: Hold and Fast, two souls fused into one great Clinger. Its farewell:
**"TILL DEATH DID THEY PART"** / *Again.*

The Orchard's is **the Watchman**, his name plate **Abner Wick, Who Merits Flame** (who he was, and what he's owed):
the man who worked the orchard, still keeping watch over a harvest that never came, as a scarecrow in the field above
it. A puzzle-fight. He's straw: **the oar goes through him**
(a puff of straw, no harm). Only fire hurts him: his own most of all, breathed out of his lantern and turned back at
him with a perfect block (each one burns a third of him away). Fire you bring (a Pitch Pot, Ember Tar) burns him a
little.
- **His arena: the Unreaped Rows,** the field above the orchard, through the gateway in its wall. It was never
  harvested: dead wheat stands in rows, there are a few stooks where someone began and stopped, and it's walled round.
  Once he's found, the gateway's two plank gates swing shut behind you and slam, and his own music begins. They open
  again when the fight ends (you fall, or he's beaten).
- **Find him.** Six scarecrows stand in the rows, all alike, all looking at the gate you come in by. Only one turns
  on its stake to watch you: him. Strike a wrong one and it catches, then goes up in flames, burning you if you're
  still near (it's gone until you Rest). Strike him (or burn or hook him) and he tears free of his stake, his lantern
  lights, and the fight is on.
- **Round 1.** He moves only while he isn't watched (the Apprentice facing him, 60° either side). Watched, he stands
  still and breathes fire at you: he leans back with his lantern to his face, its flame roaring up (the tell), and
  spews a slow ball of flame. Unwatched, he lurches at you and sweeps his reaping hook.
- **Round 2.** Scorched, he flings himself back among the scarecrows. The lanterns gutter out, and the moon and even your own lantern
  with them; for a few seconds of dark (stakes creaking, straw rustling across the field) they all move, and when the light comes back they all turn to watch you. He's the one still smouldering: no glow, only a thin thread of
  smoke and now and then an ember, seen if you're near and looking (walk the rows). Found again, his fire
  comes as a fan of three (only the middle one at you), or held back and loosed late (a feint).
- **Round 3.** Scorched again, he tears free for good, alight from head to foot: never still, watched or not, his hook
  up close. His fire comes three small balls at a time, a beat apart. A perfect block only knocks a small one aside (it
  glances off sideways, never back at him), and costs him breath: his lantern gutters down a step and a music-box note
  rings, rising with each (A, C, E: Last Autumn's chord). Knock all three aside, one after another, and his lantern goes
  out for a beat; then he draws his last great breath and it blazes. Turn that one back and he burns down to his
  stake. Miss one of the three and a sour note sounds, his lantern flares back full, and it's three small ones again. His farewell: **"FLAME MERITED"** / *He kept the light for no one.*
- **Lock-on** never picks a scarecrow, nor him while he's still one of them: only him, fighting.
- **Fire turned back** goes to him, through anything in between: it's his.

The **Drowned** appear at water edges everywhere.

**Everything in the Orchard burns:** it's dry as old leaves. Fire does 1.75 times the harm to any Soured there, and
they burn half as long again.

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
| Moths | memories | A flock of one-hit wisps drawn to light: they circle their lantern, then come to yours, circling you just out of reach and sending one in at a time. It stalls, flaring (the tell), then dives straight through; a perfect block breaks it | The heavy attack (it sweeps through all it reaches) and fire (one alight sets the next alight) |
| Gleaners | the harvest | Farmhands who went on gleaning, walking the rows at night with lanterns on long poles. They keep their distance (backing off, skipping lightly away from the oar, but never while winding up) and fling fire from the lantern: the pole drawn back over the head, the lantern flaring (the tell), then one slow ball of flame. Turned back with a perfect block, its own fire burns one up at once. In the Orchard, where you learn to turn fire back before the Watchman | Their own fire, turned back (the Hook brings one close; pitch and tar burn it well) |
| Threshers | the work | The orchard's old threshing machines, iron and oak, their drivers grown into the seats. A heavy: it lines up on you (boiler hissing, steam from the stack, the flail reel spinning up: the tell), then charges straight, barely turning: only with a clear run at you (with something between, it drives round, slowly, for one). Sidestepped, it runs into whatever's solid (a tree, a wall, a stook) and stalls a while, steaming, open; then it backs off it. The oar glances off its front; from behind (and stalled, from anywhere) it reaches the driver. Up close, flails sweep in front and steam bursts behind. Fire doesn't burn iron: it stokes the boiler, and it charges faster | Running it into something; the Hook (it wrenches the drive chain off: stalled) |
| Drowned | life | Waits under the water (only bubbles show), out of reach. Seeing you, it rises and spits poison, then sinks and spits again. A perfect block turns the spit back, and a Drowned struck by its own spit dies outright. Hide from it and it comes to the shore to look for you, open to the oar | Hook: caught while it's up, it's hauled out onto the ground, stranded, and crawls back |

## Art direction

- **Shapes:** faceted low-poly. Characters are built from simple, readable forms with a clear silhouette from above.
- **Shading:** stepped toon light with lilac-tinted shadows, dark outlines, a soft rim, fog and bloom.
- **Palette:** a muted, cold teal-grey underworld, with warm focal accents: the Apprentice's red hood, the Orchard's
  autumn colours, lanterns and eyes.
- **Darkness:** it is always night. A weak, cold moon; the world is revealed by its fires (torches, lanterns). Fog,
  film grain and a heavy vignette keep it gritty.
- **Each Area has its own night** (moon, ambient, fog, grade), and light keeps its meaning in all of them:
  - **The Drowned City:** a weak, cold moon high up and close fog.
  - **The Orchard of Last Autumn:** a huge, low harvest moon, pale honey-silver, throwing long tree shadows across
    the rows. A dusky violet ambient, the air clearer above and mist lying low in the rows, and leaves always
    drifting down.
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
- 3 Bosses and the Ferryman (and a secret last Boss, opened by the fully lit bow lantern)
- 3 Abilities and the bow lantern
- one oar, strengthened in 5 steps (10 Ferry Nails)
- about 8 Charms, and Warm Stones, Held Breaths and Embers to find
- 5–7 Lingerers
- 5 Soured families

**Not decided here:** detailed content (rooms, stats, movesets, item lists), what the secret ending shows, and technical
choices. The pipeline findings for those are on the art-test ticket.
