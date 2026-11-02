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
  attack's recovery; it eases out into a run if you steer, with no standing about after it, and only another dash
  waits out its short recovery), a 2-swing light combo and a heavy attack. Attacks go where the stick points.
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
    RT (heavy stays on Y alone).
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
  - **Stroke**, from Boss 2 (the **Drowned Moon**: the harvest moon's reflection, set into the oar blade, which pulls
    water as the moon pulls the tide). On the water, the heavy attack while rowing: a surge that carries the boat through
    a current (without it, fast water pushes you back) and a wave rolling ahead (the Drowned knocked under, floating
    debris pushed aside). On land, the heavy attack held and let go: a wave of force in an arc ahead, about 4 m, edged in
    silver foam, costing stamina; it knocks Soured back and staggers them (little harm), throws a Clinger off (mid-leap
    or off your back), can push them into water; in shallows it carries water (bigger, and it douses fire). It shoves
    what floats or is loose and turns wheels (a wave down a mill-race turns its wheel, the wheel opens a sluice). In the
    Hall of the Last Hour it sets stopped things moving: a Stroke against a gear or a pendulum runs that part of the clock a
    while.
  - **Vault**, a secret in Area 1: plant the oar and leap, always the same distance (misjudge a gap and you fall
    short), over gaps and low walls and onto ledges, and over what's low (rings across the floor, sweeps, charges).
    Locked on, it carries you to your target. No slam: tried, and it made the move janky (the user, 2026-10-31).
    It's optional early and required in Area 3, which hints where it is.
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

**The Hold** (working name; the user's direction, 2026-11-01): the game begins here, before the Hub, and it's the
tutorial. Unknown to the player, it's where the final Boss is: the Ferryman's hold beneath the Hub, where he keeps
what he's taken (the "Beneath the Hub" of the table below). You climb out of it up into the Hub, it's forgotten, and
you return to it at the end, when the water drains. Dark, eerie, desolate, dead, and surreal (the user's direction,
2026-11-02: a cave, not the drowned undercroft first tried): **a cave whose walls are made of the souls that were kept
there**, so many for so long that they became the place. The walls breathe, slowly swelling and sinking; ripples run
along them towards you; faces and hands press out of the stone and sink back; eyes set in the walls open as you come
and follow you, and close behind you. Black still water in pools and a lake (the water that drains at the end); the
lantern nearly the only light; at the way out, a crack of grey daylight. Its only creatures are **the Kept** (like Dark
Souls' Humanity; not "Shades": a Shade is the ghost you leave where you die): what the living walls let go of (see
the Soured families table). The story around it isn't decided yet (why the
Apprentice wakes there). Its lab: `Lab_Hold` (the feeling and the atmosphere only, for now).

**The Hold's Boss, The Hold, That Would Not Let Go** (the user's direction, 2026-11-02): an easy first Boss, a tutorial in
what a Boss is (a name plate, a bar, big tells, windows to punish). At the way out, in a round chamber before the
crack of daylight, the cave itself won't let you go: a great face forms in the chamber's north wall, high up, and two
enormous hands tear out of the stone either side of it. Rooted in the wall: a fight of patterns.
- **The hands:** one rises and slams down where you stand (dodge); after a slam it lies stunned on the floor a while:
  strike it (it hurts the whole). A hand sweeps low across the floor (vault it, or dodge). Wound a hand enough and it
  withdraws into the wall a while (one less to dodge); with both withdrawn, the face sags down into reach: strike it
  (great harm), until it heaves itself up and the hands tear out again. One health bar.
- **Two phases:** first the hands alone; at half the face joins: it draws breath and the cave's breath gusts you back,
  and it bites down at you if you're close under it; the hands quicker.
- **Beaten**, it lets go: the face sags to the floor, its mouth opens wide on the way out behind it, and you climb out
  to the Hub. No Ability (the Hook is the Anchoress's). Its farewell: **"LET GO"** / *It held on as long as it could.*

The **Hub** is the Ferryman's jetty, at the centre of the underworld. It has his hut, the merchant soul, waiting
Lingerers and one Mooring post. The Areas are spokes that link to each other through shortcuts opened from the far side,
so you can loop. There's **no fast travel**: rowing and shortcuts are the travel, and each Boss arena opens a shortcut
back toward the Hub.

Its look and feel (the user's direction, 2026-11-02; Lab_Hub, not yet the level): **a quiet refuge**, the one warm, safe
place after the Hold's dead dark: melancholic but kind, warm lanterns (warm light is safety), still black water like a
mirror, mist, a soft dusk. **A small island**: his hut at its north on a little levelled plot, a worn path down to the
jetty on its south shore and the landing at the jetty's end (his boat, the Mooring post); the merchant soul by the hut.
(A long queue of the dead waiting along the jetty was tried and dropped: it looked wrong. soul.py's six waiting souls
remain, unused.) **You arrive up through a trapdoor in his hut's floor**: the tunnel out of the Hold climbs into
his home, the first hint that the cave beneath is his. The ways to the Areas lead off into the mist, each marked by its
lanterns (the Drowned City's teal west, the Orchard's amber east, the Hall's pale gold north); the Far shore's light out
in the dark to the south-east. Its music: a slow barcarolle on a worn harmonium and a reed, D major (music_hub).

| # | Area | Reached by | Boss | Reward | Introduces |
|---|---|---|---|---|---|
| 1 | The Drowned City | open | The Antediluvian Anchoress | Hook | Laden, Clinger |
| 2 | The Orchard of Last Autumn | Hook | The Gardener (Father Moth, Who Gathered Autumn) | Stroke | Moths, Gleaners, Threshers |
| 3 | The Hall of the Last Hour | Stroke (+ Vault inside) | The One Who Waits | the way to the Ferryman | the Punctual, the Wound, Keeners |
| – | Beneath the Hub (the water drains) | after Boss 3 | The Ferryman | the ending | |

**The Hall of the Last Hour's arc** (the user's direction, 2026-10-12; laid out in level design, not before): the whole
Area is stopped in time, and you make your way through it frozen, setting its machines running a while with the Stroke.
At its heart is a central engine; set going, time starts again throughout the Hall, and with it its traps (in the manner
of Sen's Fortress): the same halls, now moving and dangerous. The traps (built in the lab, 2026-11-01): the great hall's
pendulums armed with silver crescents swinging low across the aisle (vault them where they pass low, or dash
through); the Gear Run, a walled ramp from the court up to the keep's works, a great gear let go down it again and
again (alcoves in its sides to step into); the bell gallery's clock jacks, their hammers falling on the quarters
(each on its own beat of the Hall's bar) on bronze strips across the way; the undercroft's grinding floor, gears in
the floor that carry you round into their mesh. Stopped, each is held where the clock stopped it (harmless, but in the
way); the Stroke sets one going a while, like any machine; the central engine sets them all going for good. They hit
hard (a third of the Apprentice, and a throw), no guard stops machinery (only a dodge), and they catch the Soured too.

**The One Who Waits**, the Hall of the Last Hour's Boss (the user's direction, 2026-11-01): **the Hour**, the Hall's last
hour, which never struck, given a body (inspired by the Clockwork God of Morrowind, hung dead from his own contraption:
the motifs, not the man). A robed, hooded figure whose face is a full mask that is a clock's dial (its two hands its
only expression), long jointed lever arms ending in a silver hammer and a silver crescent blade, no body below the
robe but its works (a brass cage of gears, a pendulum, a green lantern at its point), hung by cables from its
contraption over the middle of **its chamber**: the room behind the great dial, inside the clock, the dial's glass its
north wall (lit through, numerals reversed, its hands at three to twelve), the floor a dial too (Roman numerals round
it, two great hands lying on it). Rooted: the room is its reach.
- **Time stopped** (the Hall as you find it): a husk dangling on its cables, grey like everything else; it only twitches
  an arm now and then (a slow, weak swing); brittle (blows do two and a half times their harm), no quarters, no tricks,
  its machines still. Quickly killed, and it should feel wrong: **beaten so, the hour never comes (the bad ending)**.
- **Time running** (the central engine set going): the real fight, four quarters. At three quarters, half and a quarter
  of its health it strikes the quarter (a cascade of the Hall's chime, untouchable a moment) and one thing is added:
  1 its hammer brought down in front of it, its blade swept round low (vault it, or dodge), its works ground round under
  it; 2 + the floor dial's hands sweeping round the chamber, low (vault them); 3 + **Stop You** (its mask's hands race:
  it stops the Apprentice in time, a beat, drained grey and still, and brings the hammer down on them; a dodge just before
  slips it) and the two pendulums swinging across the floor; 4 the last hour: quicker tells, quicker hands, rewinds
  sooner. All through, **Rewind**: take too much from it at once (a burst within a few seconds) and it winds itself back,
  its mask's hands spinning backwards, mending the burst, unless it's struck while it winds (broken off, reeling).
  **Beaten so, the hour strikes at last: the way on, toward the true ending** (nothing past the Boss is designed yet).
- Its lab: `Lab_OneWhoWaits` (the Tester's Post sets the Hall's time and begins the fight at any quarter).

**The Gardener**, the Orchard's Boss, in full **Father Moth, Who Gathered Autumn**: the man who tended the orchard and
its glasshouse, who couldn't let go of the last autumn. He gathered every memory of it, and the memories gathered to
him as the Moths gather to a light, until he grew into the greatest of them: a vast moth (wings like dead autumn
leaves, six metres across) with the stooped gardener still rising from it, in his apron, his forearms grown into its
forelegs, ending in pruning shears; in his ribs, caged, a small lantern: the last light he tended; in his belly, the
autumn hollow, a ragged glowing opening ringed with dead leaves, that his leaves pour out of. His arena: **the
Glasshouse**, beyond the Unreaped Rows' far gate (the Watchman guards the way), a ruined palm house of iron ribs and
broken panes, raised beds of dead plants, shallow water channels (you wade them; they put out flames), lamps hung on
chains, a dead orange tree against its north wall (he hangs folded on it, like a leaf, until you come in); its middle
left open, his floor.
- **A static fight, of dodging:** he keeps to one place, hovering over the north of the floor, out of the oar's reach,
  and beats his wings. Each beat (his wings raised high, the light in his chest flaring, the hollow in his belly
  swelling and brightening: the tell) throws dead leaves out of that hollow at
  you in a pattern: a fan, a few aimed one after another, a ring all round him. Dodge through them, block them, or step
  between them. After so many beats he's spent: he sinks to the floor and lies there a while, open to the oar, then
  rises and begins again.
- **Four phases** (at three quarters, half, a quarter): each changes the patterns, more and more of them, and more
  complex. Phase 1: leaves alone, one pattern a beat. Phase 2: quicker beats and quicker leaves, several patterns at
  once, and new kinds: a spiral stream of leaves, seed pods lobbed to where you stand that burst into rings, withered
  oranges shaken down from the vault onto glowing marks (no guard stops them: step out).
- **Phase 3 (half), afoot and relentless:** he doesn't fly up again but heaves himself onto his forelegs and comes after
  you on foot: quick snips two or three at a time, a wide sweep of his shears, a lunge across the floor when you're off.
  His shears turn your blows. Two ways through: dodge him till he's spent (a dozen strikes, and he collapses, open), or
  turn a strike with a perfect block (stunned, a riposte's chance). At the quarter mark he takes to the air again.
- **Phase 4 (a quarter), on the wing again, in the dark:** he screams, the glasshouse shakes, and all four lamps fall
  at once, shattering; it goes far darker than the night outside: the hollow in his belly, your bow lantern, his
  smouldering leaves. No more throws: the vault starts coming down (the glasshouse rumbling, debris falling onto glowing
  marks, most of it right where you stand: keep moving) while he charges in the middle of the floor, sucking energy in:
  streaks of light drawn into his hollow from all round, faster and denser, an orb in it swelling to a blinding sun, a
  sound that never stops building. Then one great section of the vault crashes in a run away from you (never already
  between you and him; its warning pale blue, not his orange) and stays standing, its panes glowing pale blue, a patch
  of light on the floor behind it: get there. He lets it all go, a blast as bright as noon through
  the whole glasshouse (the screen white): behind that section it can't reach you; in the open it all but ends you (no
  dodging it). Then he falls, spent for good: finish him.
- **No skipping a phase:** brought down to a phase's mark, his health holds there and nothing harms him (fire neither)
  until he rises; then the next phase begins. The Hook glances off him.
- **Fire** hurts him badly (he's a moth, and the Orchard burns), but doesn't lure him.
- His farewell: **"MOTHBALLED"** / *Winter is nigh.* He gives up the **Drowned Moon** (the **Stroke**). The light caged
  in his ribs all his fight was never a lantern: it's the harvest moon's reflection, which he lifted out of the river and
  kept (the reason the Orchard's moon never sets). Beaten, his cage cracks and it pours out of him, pooling on the floor
  beside him: a circle of liquid moonlight, rippling, its cold light on the leaves; the sky's moon dims as it does. You
  lift it out like a coin from a puddle (a disc of water as thin as glass, a pale crescent shifting in it): **THE
  STROKE**. It sets into the oar blade as a restless silver sheen, seen on the Apprentice from then on; the Stroke's
  waves are edged with moonlight, and on the water the river bends towards the blade in a silver wake. And the Orchard's
  harvest moon begins at last to set: its night cooler and darker for good.

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

The Hall of the Last Hour's is **the chief mourner**, her name plate **The Widow Hale, Who Kept the Minute** (the user's
direction, 2026-11-01): the widow of the Hall, who stopped the great clock itself at the minute of the death, as her
Keeners stopped the small ones. If time runs, the death comes after all; so she keeps vigil at the **central engine**
in the belfry, and the Hall stays stopped. She guards it: the engine's plate can't be struck while she keeps it.
- **Held in the stillness:** she moves through the stopped Hall as the Keeners do, but no blow touches her (it passes
  as through a photograph), as the stopped machines can't be moved.
- **Pockets of time:** the engine's three parts (the escapement, the winding drum, the hour bell) can each be set going
  a while with the Stroke, like any machine of the Hall. Around a running part, time comes back in a ring a few metres
  across (the colour with it): inside it, she can be hurt.
- **She goes to stop it:** a running part draws her; she lays her hands on it (the tell: a few beats) and it stops,
  its ring of time gone. So she comes into the ring, and that's the time to strike her. Struck there, she reels.
- **Her blows:** **a great knell** (as the Keeners', at its fullest: she draws breath, then tolls, rings rolling across
  the floor; dash through them) and **the veil** (her crape cast out, a long low sweep along the floor: caught, you're
  slowed, held in the stillness a moment).
- **Beaten**, she lets the minute go: the engine's plate is free. Starting it stays the Apprentice's choice (the bad
  ending stays possible). Her farewell: **"GOOD MOURNING"** / *She let the minute pass.*

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
| Punctual | their duty | The Hall's household, still keeping its hours: a servant with a pocket watch grown into its chest where the heart was. Melee, and only ever on the beat (the Hall's clock, a second to the beat): it strikes exactly on the tick, in counts you can learn by ear. Staggered, it loses count and stands still to find the beat again | The perfect block (on the beat) |
| Wound | more time | A clockwork hound, a winding key in its back turned tight: a burst of speed and a flurry of bites, then it runs down, slower and slower, open; it stops to wind itself again | Stroke (the wave knocks its key loose: it runs down at once) |
| Keener | grief | The mourners who stopped the Hall's clocks at the minute of a death, in black crape, a stopped watch in hand. Ranged: draws breath, then tolls a knell, rings of sound across the floor you dash through; fragile | Vault |
| Moths | memories | A flock of one-hit wisps drawn to light: they circle their lantern, then come to yours, circling you just out of reach and sending one in at a time. It stalls, flaring (the tell), then dives straight through; a perfect block breaks it | The heavy attack (it sweeps through all it reaches) and fire (one alight sets the next alight) |
| Gleaners | the harvest | Farmhands who went on gleaning, walking the rows at night with lanterns on long poles. They keep their distance (backing off, skipping lightly away from the oar, but never while winding up) and fling fire from the lantern: the pole drawn back over the head, the lantern flaring (the tell), then one slow ball of flame. Turned back with a perfect block, its own fire burns one up at once. In the Orchard, where you learn to turn fire back before the Watchman | Their own fire, turned back (the Hook brings one close; pitch and tar burn it well) |
| Threshers | the work | The orchard's old threshing machines, iron and oak, their drivers grown into the seats. A heavy: it lines up on you (boiler hissing, steam from the stack, the flail reel spinning up: the tell), then charges straight, barely turning: only with a clear run at you (with something between, it drives round, slowly, for one). Sidestepped, it runs into whatever's solid (a tree, a wall, a stook) and stalls a while, steaming, open; then it backs off it. The oar glances off its front; from behind (and stalled, from anywhere) it reaches the driver. Up close, flails sweep in front and steam bursts behind. Fire doesn't burn iron: it stokes the boiler, and it charges faster | Running it into something; the Hook (it wrenches the drive chain off: stalled) |
| The Kept | everything (worn down to nothing) | Only in the Hold, the tutorial (the user's direction, 2026-11-02): what the cave's living walls let go of. A small hunched figure, child-sized, black and soft-edged like wet tar, two pale eyes, long thin arms that nearly drag; it walks bent over, unsteady. As you pass set places in the walls, one tears free of the stone and comes after you, in twos and threes; frail (two blows). One blow: a clawing swipe, the long arm raised slowly first (the tell): the first thing you learn to block or step from. Nothing to do with the lantern. Beaten, it sinks back into the stone, and a face presses out of the wall nearby a moment after: taken back, never released (until the end) | Nothing: they're the first lesson |
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
