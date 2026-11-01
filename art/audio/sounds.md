# Sounds

Every sound the game needs, and where it comes from. **Synth** = a script in this folder (`tools/build-audio.sh`),
**AI** = text-to-sound (e.g. ElevenLabs Sound Effects), **Library** = Sonniss GDC bundles / Freesound CC0.
Synth suits stylised sounds; organic ones (water, wood, voices) are better from AI or a library.

| Sound | Status | Source | Notes |
|---|---|---|---|
| Oar swing, light (`oar_swing`, 4 variants) | in game | Synth | airy whoosh peaking as the blade moves fastest |
| Oar swing, heavy (`oar_swing_heavy`, 3) | in game | Synth | deeper whoosh with a low "whoom" |
| Dash (`dash`, 3) | in game | Synth | whoosh, push-off thump, ghostly shimmer (the i-frames) |
| Block clang (`block_clang`, 3) | in game | Synth | knock + short metal ring + rattle: oar off a Laden's heap |
| Torch crackle (`torch_loop`) | in game | Synth | seamless 4 s loop, 3D on every torch |
| Music: The Drowned Dark (`Music/music_drowned_dark`) | in game | Synth | 80 s seamless stereo loop, D minor: wind, drone, ghost choir, glass notes, distant wails, circular hall reverb |
| Perfect block (`parry`, 2) | in game | Synth | bright ringing "ting" + low thud; distinct from the block clang |
| Soured's fare absorbed (`soul_absorb`) | in game | Synth | rising hollow whoosh, climbing shimmer, soft chime |
| Oar hit on a Soured (`oar_hit`, 3) | in game | Synth | dull body thud, cloth smack, wooden knock, short hollow "soul" ring; heavy hits and ripostes play it lower |
| Laden slam tell (`laden_slam_windup`) | in game | Synth | 0.9 s, the wind-up's length: deep creak speeding up as the heap rises, junk rattling, low groan; swells to the strike. One variant on purpose: a tell must be recognisable |
| Laden sweep tell (`laden_sweep_windup`) | in game | Synth | 0.6 s: tighter, higher creak, a breathy intake; tells apart from the slam by ear |
| Laden swing (`laden_swing`, 3) | in game | Synth | heavy low whoosh + rumble, every strike, hit or miss (sweep plays it higher) |
| Laden slam impact (`laden_slam_impact`, 2) | in game | Synth | boom, crack, splintering, debris, small stone room; when the slam lands, hit or miss (+ a little camera shake) |
| Soured alert (eyes flare) | planned | AI or Synth | a breathy, sour exhale; the audio half of the green flare |
| Footsteps on wood / stone | planned | Library or AI | boardwalk vs quay should sound different |
| Water lapping (ambient) | planned | Library or AI | bed under everything near the water |
| Underworld drone (ambient) | planned | Synth | low, slow, uneasy |
| Lantern lit (`lantern_lit`) | in game | Synth | soft flame "fwoomp", crackles, warm D-major chime; first Rest at a Mooring post only |
| Healing (`heal`) | in game | Synth | breathy rising shimmer over the 0.7 s heal, then a warm D-major bloom and a soft exhale as it takes hold |
| Obols spent (`obol_clink`) | in game | Synth | a few dull silver clinks tumbling into the merchant's hand |
| Charm fitted (`charm_fit`) | in game | Synth | cloth rustle, a clasp click, a low warm chime |
| Splash (`splash`, 2) | in game | Synth | surface slap, deep bloop, water rushing over, bubbles; a Soured drowning or the Apprentice falling in |
| Laden archer draw (`archer_draw`) | in game | Synth | 0.9 s tell: a taut bow creak tightening and a rising string hum, swelling to the release; higher and thinner than the Laden's heave |
| Laden archer volley (`archer_volley_draw`) | in game | Synth | 1.3 s tell: three nocking clicks and a quiver rattle, then a slower, deeper draw than a single shot |
| Arrow release (`arrow_release`, 2) | in game | Synth | string twang with a buzz, and the whoosh of the shaft |
| Arrow thunk (`arrow_thunk`, 2) | in game | Synth | an arrow striking wood or stone and quivering |
| Shortcut gate opened (`gate_open`) | in game | Synth | the barring plank scraped off and dropped, a long hinge creak, a thud against the stop |
| Shortcut gate barred (`gate_rattle`) | in game | Synth | knocks against the barring plank and loose bars rattling: it won't open from this side |
| Hook thrown (`hook_throw`) | in game | Synth | a quick whoosh and the chain rattling out |
| Hook catches (`hook_catch`) | in game | Synth | a hard iron clank on the ring and the tether's taut thrum as the pull starts |
| Hook whirled (`hook_whirl`, loop) | in game | Synth | a soft whoosh per turn of the Hook on its chain and the links' rattle, while aiming |
| Item taken (`item_take`) | in game | Synth | a cloth rustle as the Apprentice stoops, a small bright tick as the hand closes on it |
| Key item held up (`key_item`) | in game | Synth | a rising motif in D (minor third lifting to major) in glass and low bell, a choir swelling into a held D major chord |
| Menu tick (`ui_tick`) | in game | Synth | a tiny soft wooden tick, moving through a menu |
| Apprentice death (`death_toll`) | in game | Synth | one deep bell toll that sinks under water (highs close off), thump, sinking whoosh, bubbles; the music ducks under it |
| Bow lantern snuffed (`lantern_snuff`) | in game | Synth | the Apprentice falls: a flare, the flame fluttering faster and fainter, a last puff, a deep cold swell sinking under it |
| A soul murmurs (`lingerer_murmur`) | in game | Synth | a Lingerer beginning a line: a soft, wordless, breathy hum, low and old, in a little hall (4 contours) |
| Oar stroke (`oar_stroke`) | in game | Synth | rowing: the blade slipping in, water swirling past it, drips as it lifts (4 variants) |
| Clinger's tell (`clinger_hiss`) | in game | Synth | crouching to leap: a sharp intake of breath into a rising hiss through the teeth (0.35 s) |
| Clinger grabs (`clinger_grab`) | in game | Synth | catching hold: cloth snatched and bunched, a soft bump, a small exhale |
| Clinger thrown off (`clinger_throw`) | in game | Synth | cloth ripping loose, a whoosh, a thump and scrabble as it lands |
| Clinger keens (`clinger_keen`) | in game | Synth | its other half scattered: a thin, rising, wavering wordless cry, cut off short |
| Clinger whispers (`clinger_whisper`) | in game | Synth | on your back, barely there: "don't go", "stay", "please" (breath shaped into the words) |
| Clinger skitters (`clinger_skitter`) | in game | Synth | darting over stone: a quick scrabble of dry taps and clicks |
| Hold groans (`unparted_groan`) | in game | Synth | the Unparted's Hold winding up: a deep, hollow groan sliding down and up, effort and grief |
| Hold howls (`unparted_howl`) | in game | Synth | Fast gone: a long anguished howl cracking into a higher cry, then Hold is berserk |
| The Unparted's music (`music_unparted`) | in game | Synth | a grieving 3/4 waltz in F minor: a low voice and a thin high one circling, a heartbeat drum; variant 2 (one scattered) torn, with a keening wail (2 was a copy of 1 until 2026-10-01: a variant-numbering slip) |
| A Boss's farewell (`boss_farewell`) | in game | Synth | a great bronze bell struck once as a beaten Boss's farewell comes up, a long cathedral tail |
| Chapel floor cracking (`floor_crack`, 2) | in game | Synth | the warning before a section gives way: a grinding rumble swelling, cracks quickening, grit |
| Chapel floor giving way (`floor_break`, 2) | in game | Synth | a splitting crack, the deep boom of it dropping, stone grinding and tumbling, rubble plopping into the water |
| Anchoress's Undertow (`undertow`) | in game | Synth | 4 s: a low drowned murmur of prayer (two muffled voices) over water welling up, deep churning, bubbles thickening; her tell when she kneels |
| Anchoress: Drop tell (`anchoress_drop_windup`) | in game | Synth | 1.1 s: the chain rattling faster as the anchor climbs, iron groaning, her strained drowned groan; also her sound kit (links, iron, groan, scrape) |
| Anchoress: Drop lands (`anchoress_drop_impact`) | in game | Synth | a huge dull clank of iron into stone, a deep boom, the chain crashing down, water rushing away (the ring); lower for her Rise's slam |
| Anchoress: Sweep tell (`anchoress_sweep_windup`) | in game | Synth | 0.85 s: the chain paying out in a fast ratcheting rattle, a low whir as the anchor swings at her side |
| Anchoress: Sweep (`anchoress_sweep`, 2) | in game | Synth | a huge low whoosh as the anchor whirls past, the chain thrumming taut |
| Anchoress: Cast tell (`anchoress_cast_windup`) | in game | Synth | 1.0 s: a long iron creak as it's hefted back, the chain gathered in a slow rattle, a deep breath held |
| Anchoress: Cast (`anchoress_cast`) | in game | Synth | her grunt, a heavy whoosh, the chain paying out behind it |
| Anchor lands (`anchor_land`, 2) | in game | Synth | a heavy clank on stone, a thud, the chain slapping down |
| Anchor hauled (`anchor_haul`) | in game | Synth | iron grinding and juddering over stone, faster and louder, the chain rattling in |
| Anchoress rises (`anchoress_rise`) | in game | Synth | 2.2 s: every link shaking, a swell under the floor, her voice climbing into a drowned many-voiced cry, peaking on the slam |
| Music: The Anchoress (`Music/music_anchoress_1`, `_2`) | in game | Synth | 64 s loop, D minor, 60 bpm: a slow grinding march (deep drum, chain off-beats, bowed bass, drone, the Drowned Dark's choir); 2 is the same loop heavier (every beat, faster chain, a high wail, water surging) and crossfades in step when she rises (2 was a copy of 1 until 2026-10-01: a variant-numbering slip) |
| Apprentice hurt (`apprentice_hurt`, 3) | in game | Synth | muffled impact + short clipped grunt through the mask; also on washing ashore |
| Pitch Pot bursting (`pitch_burst`, 2) | in game | Synth | clay cracking, a deep whumpf as the pitch catches, fire roaring up and settling into a crackle |
| Warm Broth drunk (`drink`) | in game | Synth | a cork's soft pop, three gulps through the mask, a small warm sigh |
| Ember Tar on the oar (`tar_sizzle`) | in game | Synth | a wet slap, the tar catching with a hiss that swells into a low steady burn |
| The Way Back (`way_back`) | in game | Synth | a knot pulled loose, a long breath of wind drawn backwards, a far soft bell as the black closes |
| Drowned waiting (`drowned_bubbles`, 3) | in game | Synth | bubbles breaking the surface over a low muffled gurgle, like a breath it can't take |
| Drowned rising (`drowned_rise`, 2) | in game | Synth | its tell: water surging up and pouring off it, a drowned groan swelling, wet and choked |
| Drowned spitting (`drowned_spit`, 2) | in game | Synth | a wet gathering hawk in its throat, then the spit leaving with a thick "phtt" |
| Spit striking (`spit_splat`, 2) | in game | Synth | a thick wet splat, and a small fizzing hiss as the poison eats at what it hit |
| Moth diving (`moth_dive`, 2) | in game | Synth | a soft papery whirr of wings rising to a short dry hiss |
| Moth scattered (`moth_release`, 6) | in game | Synth | a dry puff of dust and one soft music-box note, each variant a note of the same small tune (A minor) |
| Music: Last Autumn (`Music/music_orchard`) | in game | Synth | 96 s loop, a slow waltz in A minor (60 bpm, 3/4): a reed-organ pad on Am - Fmaj7 - C - G - Am - Dm7 - E7 - Am, a plucked harp keeping the waltz, a low bowed hum on the roots, the tune on an old music box a few cents out of tune (silent through the middle, back for the last phrase, leaning on G# to pull round to A), leaves rustling in a breeze, a distant chime now and then; a warm middling hall |
| Music: The Last Hour (`Music/music_last_hour_1`, `_2`) | in game | Synth | 96 s loops, E minor, 60 bpm (a clock's second to the beat), one loop shared so they cross in step (the Hall of the Last Hour). 1, stopped: held chords on a soft flue organ (Em(add9) - Cmaj7 - Am9 - B7(b9) - Em - G6 - Cmaj7(#11) - B7), bowed glass above, a low E drone; now and then a clock's tick that never gets its tock; twice the quarter chime begins on great bells and stops short (G, F#, E: never the B), the last note left ringing; a cold, very wet stone hall. 2, awake (the central engine set going; for now the Tester's Post toggles it): the same chords driven: tick and tock on every beat, ratchets, a harpsichord turning the chords in quavers, a bowed bass on every beat, timpani, the organ pulsing, the glass trembling; the chime rung through at last and answered, the great bell on each chord's root; gently limited |
| Watchman turning (`watchman_creak`, 3) | in game | Synth | quiet: a stake creaking in the earth as he turns on it to watch you (stick-slip of dry wood), straw settling in his coat; deeper, as the lanterns gutter out |
| Watchman found (`watchman_reveal`) | in game | Synth | his stake splintering as he tears free (cracks, a rending of fibres), a great dry whoosh of straw, a hollow breath in the sack |
| Watchman's hook drawn (`watchman_windup`, 2) | in game | Synth | his tell: his coat creaking as he twists, the blade rasping up through the straw, a thin ring of iron at the top |
| Field gates slamming (`field_gate_slam`) | in game | Synth | the Watchman found: two heavy plank leaves banging to, one after the other, the latch dropping with an iron clank, the old wood rattling as it settles |
| Music: The Watchman (`Music/music_watchman_1`, `_2`) | in game | Synth | 48 s loop, A minor (the Orchard's key), 3/4 at 60 bpm: a dry wooden knock on every beat (tick tock tock), a lone scratchy fiddle creeping through the opening of Last Autumn's tune at a bar a note, sliding between notes, a low bowed hum on A, damp reed chords (Am - F - Dm - E), straw rattling now and then, the music box returning in flat fragments; twice in the loop everything but the hum stops dead for two beats. 2 is the same loop worse (a heartbeat, the knocking doubled, the fiddle an octave up and trembling, a rising scrape every four bars) and crossfades in step at half health |
| Watchman drawing breath (`watchman_breath`, 2) | in game | Synth | his fire's tell: a long hollow inhale through the sack, straw crackling, the lantern's flame roaring up under it |
| His fire breathed out (`fireball_whoosh`, 2) | in game | Synth | a deep whumph and a roar trailing off |
| His fire guttering (`fireball_gutter`, 2) | in game | Synth | where it strikes: a soft burst, a spit of sparks, a hiss |
| Scarecrow going up (`scarecrow_burst`, 2) | in game | Synth | straw catching all at once: a great dry whoomph, crackling and roaring (Tar sizzle plays as it catches) |
| The oar through straw (`straw_puff`, 3) | in game | Synth | the Watchman, struck: a soft dull thump into stuffing, a dry rustle out |
| Watchman scorched (`watchman_scorch`) | in game | Synth | his own fire turned back on him: a rushing roar through the straw, the coat's seams going, a hollow cry through the sack |
| Watchman's breath knocked aside (`watchman_chime`, 3) | in game | Synth | round 3, one per small fireball knocked aside in a row, rising: music-box notes of A minor (A5, C6, E6), each fuller; the third with the octave under it, a finish |
| The row broken (`watchman_sour`) | in game | Synth | round 3, one not knocked aside: a dull wooden thud and a music-box note gone sour (two flat tines a semitone apart, beating) |
| The Apprentice catching fire (`apprentice_catch`, 2) | in game | Synth | a soft dry whumph as the cloak goes up, a rush of flame, a gasp through the mask |
| The Apprentice alight (`apprentice_burning`, 3) | in game | Synth | played every half second while it lasts: cloth burning, a low flutter of flame, a scatter of dry cracks |
| A Sodden Sack (`sodden_sack`) | in game | Synth | a heavy wet slap of sacking, a long hiss of steam, a few drips |
| Gleaner drawing back (`gleaner_windup`, 2) | in game | Synth | its tell: the long pole creaking as it swings up, the lantern's flame gulping air and roaring up, a breath drawn in through the hood (its fire: `fireball_whoosh`) |
| Gleaner skipping away (`gleaner_hop`, 3) | in game | Synth | the smock flapping, a light double scuff of feet in the leaves, the lantern rattling on its crook |
| Thresher revving (`thresher_rev`, 2) | in game | Synth | its tell before a charge (and its flails' sweep): the boiler's hiss building, the reel clattering up to speed, chuffs from the stack quickening |
| Thresher charging (`thresher_charge`, 2) | in game | Synth | iron wheels rumbling, the flails threshing the air, the boiler roaring |
| Thresher crashing (`thresher_crash`, 2) | in game | Synth | a great iron-and-oak impact, timber splitting, the reel jamming with a screech, a long hiss of steam |
| Thresher's chain wrenched off (`thresher_chain`) | in game | Synth | the Hook: a hard iron snap, the chain whipping and rattling loose, steam escaping |
| Thresher stoked (`thresher_stoke`) | in game | Synth | fire on it: the firebox catching with a whoomph, the boiler's whistle shrieking up, a roar from the stack |
| Thresher venting (`thresher_hiss`, `thresher_vent`, 2) | in game | Synth | close at its back: a rising hiss in the pipes (the tell), then a roaring burst of steam |
| Father Moth's wingbeat (`father_moth_wing`, 3) | in game | Synth | on the wing, one per beat: a great soft whump of air, and the papery rustle of dry leaves after it |
| Father Moth's beat (`father_moth_beat`, 2) | in game | Synth | a wingbeat throwing his leaves: one great beat driven forward, a heavy whoomph, a wind full of dead leaves |
| Father Moth waking (`father_moth_wake`) | in game | Synth | on the dead orange tree: dead leaves crackling open (his wings unfolding), the branches creaking under him, two slow wingbeats as he lifts |
| Father Moth spent (`father_moth_downed`) | in game | Synth | his wings giving out, down on the floor: a great heavy crash onto the stone, the shears clattering, leaves crumpling, a long hollow breath out |
| Father Moth beaten (`father_moth_death`) | in game | Synth | his wings folding with a long dry crackle, a great soft fall, a last breath; faintly, music-box notes going down (A, E, D), unresolved |
| A seed pod bursting (`seed_pod_burst`, 2) | in game | Synth | Father Moth's, phase 2: a sharp papery pop, leaves flung out |
| A withered orange landing (`fruit_fall`, 3) | in game | Synth | shaken down from the glasshouse's vault: a soft wet thud on the stone, the rind splitting, seeds skittering |
| Father Moth's shears drawn (`father_moth_snip`, 2) | in game | Synth | phase 3, afoot, before a snip or a sweep: the blades opened with an iron scrape, snip-snip |
| Father Moth's lunge drawn (`father_moth_stall`, 2) | in game | Synth | phase 3, before his lunge: a hard crack of air as he rears, a trembling rattle of leaves, the lantern in his ribs flaring |
| Father Moth's scream (`father_moth_scream`) | in game | Synth | phase 4 begins: an old man's hoarse cry torn ragged, rising into a dry rasping shriek of wings and leaves, the glasshouse's iron ringing under it |
| A lamp shattering (`glass_lamp_shatter`, 2) | in game | Synth | an iron clank, the glass bursting, shards skittering, the flame gone with a puff |
| Father Moth charging (`father_moth_charge`) | in game | Synth | phase 4, his blast charging for 12 s, never stopping building: a deep drone climbing in pitch, throbbing faster and faster, crackling denser, a whine screaming up at the last |
| Father Moth letting it go (`father_moth_release`) | in game | Synth | a huge deep whoomph, a blast of wind and leaves tearing outward, the glasshouse's panes rattling in their iron |
| The Drowned Moon pouring out (`drowned_moon_pour`) | in game | Synth | his rib-cage cracking, moonlight running out like a thin stream and pooling, cold glass tones (A, E, C, A) coming in as it fills |
| The Stroke drawn back (`stroke_draw`) | in game | Synth | water pulled away like a tide, a low swell rising and held, a cold glass tone climbing a fifth under it; cut off when let go |
| The Stroke's wave (`stroke_wave`, 3) | in game | Synth | a deep thump of water, a rush rolling away and breaking into spray, a cold glass chime (A, C or E) |
| The Stroke fully drawn (`stroke_charged`) | in game | Synth | one clear cold glass ring (A over E) and a single drop into still water: let go now |
| A surge down the mill-race (`mill_race_surge`) | in game | Synth | water shoved along a stone channel: a hollow rushing slap, gurgling against the walls |
| The water wheel turning (`mill_wheel_turn`) | in game | Synth | water crashing over the paddles, then the old wheel groaning round, creaks slowing, water pouring off |
| The water wheel rocked (`mill_wheel_creak`, 2) | in game | Synth | a slap of water on its paddles, one groaning creak, a knock as it settles back |
| Fast water (`current_rush`, loop) | in game | Synth | a river running hard through a stone channel: a deep steady rush, a churning surface, tearing at the walls |
| The Stroke from the boat (`boat_surge`, 2) | in game | Synth | the oar dug in and hauled through: a heavy plunge, a long rush past the hull, a cold glass note on it |
| A dock's bell rung (`dock_bell`) | in game | Synth | a small bronze bell struck twice, bright over the water, its hum dying slowly |
| Stopped clockwork set running (`clockwork_start`) | in game | Synth | a heavy brass catch letting go, a ratchet clattering as the train takes up, a rising whirr |
| Clockwork running (`clockwork_tick`, loop) | in game | Synth | a slow deep tick-tock a beat a second, wheels whirring faintly under it |
| Clockwork running down (`clockwork_stop`) | in game | Synth | the ticks slowing, the whirr sinking, a last weary tock and the catch dropping home |
| The sluice gate winding up (`sluice_gate_rise`) | in game | Synth | an iron chain ratcheting over a pulley, the board scraping up its stone grooves |
| The sluice gate held (`sluice_gate_clunk`) | in game | Synth | a heavy iron pawl dropping into its tooth, a deep wooden knock |
| Fire put out (`steam_hiss`, 2) | in game | Synth | a sharp hiss of steam thinning away, a crackle dying in it |
| A crate shoved (`crate_scrape`, 2) | in game | Synth | a rough wooden scrape across stone, a knock as it stops |
| Music: Father Moth (`Music/music_father_moth_1`..`_4`) | in game | Synth | Last Autumn grown huge: a 72 s loop, A minor, 3/4 at 80 bpm, the Orchard's chords and tune, four variants sharing one loop (crossfading in step), one a phase. 1: the waltz as a full dance (reed organ, harp with leaf-fall runs, the music box strong, a deep drum on each first beat: his wingbeats). 2: busier (strings trembling, a reed voice chasing the tune a beat behind and a third below, leaves shaken on the off-beats). 3: afoot, the waltz broken (a stamping drum every beat, shears snipping between, a bass sawing quavers, a fiddle hacking out the tune). 4: the dark, at half the level (a drone, a heartbeat, the music box slowed and flat, wind) |
| Music: Father Moth's end (`Music/music_father_moth_end`) | in game | Synth | beaten: the music box plays Last Autumn's last phrase, winding down, and this once comes home to A, a soft reed A minor swelling under it |
| A Punctual's watch (`punctual_tick`, 3) | in game | Synth | once a beat while it's aware of you: a small, bright, dry pocket-watch tick |
| The Count's tell (`punctual_raise`) | in game | Synth | 1.9 s: the key raised (a ratchet's three clicks), a firm tick on the next beat, then a bowed-metal whine swelling to the blow |
| The Count's blow (`punctual_impact`, 2) | in game | Synth | the brass key on stone: an inharmonic clang, a thud, grit |
| The Quarters' tell (`punctual_warn`) | in game | Synth | 0.85 s: a clock's warning: a lever's click, then the train of wheels whirring up |
| The Quarters' notes (`punctual_quarter`, 4) | in game | Synth | small bells from its chest, one per blow, played true (no pitch varied): G5, F#5, E5, B4 (the Hall's quarter chime an octave up); the last rings longest |
| A Punctual losing count (`punctual_lost`) | in game | Synth | its spring slipping: a ratchet running down, a mainspring's twang, stray ticks |
| A Punctual finding the beat (`punctual_found`) | in game | Synth | on the first of a bar: one firm tick and a small bell (E6) |
| A Wound's spring running (`wound_whirr`) | in game | Synth | a seamless 1 s loop: a quick dry ratchet and a thin humming spring; the game pitches it with the hound's pace (low as it runs down) |
| A Wound's bite tell (`wound_growl`, 2) | in game | Synth | 0.4 s: a growl through gears, a toothed rasp rising |
| A Wound's bite (`wound_bite`, 3) | in game | Synth | iron jaws snapping shut: a clack and a short metal ring (the pounce, pitched down) |
| A Wound's pounce tell (`wound_gather`) | in game | Synth | 0.5 s: the spring drawn tight, a rising creak over held clicks |
| A Wound running down (`wound_rundown`) | in game | Synth | its whirr slowing, the clicks spreading out to the last, a sigh of air |
| A Wound winding (`wound_wind`) | in game | Synth | 2.5 s: four turns of the key, each a run of ratchet clicks and a creak, each a little higher |
| A Wound wound tight (`wound_wound`) | in game | Synth | the key's last click, the spring caught (a bright twang), its whirr starting up |
| A Wound's key knocked loose (`wound_loose`) | in game | Synth | the Stroke: a hard ping, the spring let go all at once (a ratchet spinning out), a wobbling twang |
| A Keener drawing breath (`keener_breath`, 2) | in game | Synth | the knell's tell, 1.2 s: a ragged inhale through crape that catches twice in a sob, a low bell's hum (E2) swelling under it, cut off at the top |
| A Keener's knell (`keener_knell`, 3) | in game | Synth | one per ring: a funeral bell on E3 struck through crape (muffled), a keening voice above it falling B4 to G4 (each variant its own vowel), a cold hall's tail |
| A Keener falling (`keener_death`) | in game | Synth | a sigh letting go (a voice sinking B4 to E4, then breath), and its stopped watch going again: a quiet tick and tock |
| The Vault's plant (`vault_plant`, 2) | in game | Synth | the oar's iron-shod foot knocked onto stone (a bright knock, a scrape, the ferrule's ring), the shaft creaking under the weight, the cloak's whump going up |
| Coming down from the Vault (`vault_land`, 2) | in game | Synth | soft boots on stone, a muffled thud, the cloak settling, a little grit |
| The Hall's hour bell (`last_hour_toll`) | in game | Synth | time set going (the central engine): the works catching (an iron clunk, a ratchet), then one great bronze toll on E2, its hum ringing on through the stone, a cold hall's tail (10 s) |
| A pendulum blade's swish (`trap_blade_swish`, 2) | in game | Synth | a blade through the bottom of its swing: a heavy whoosh rising and falling, a thin steel singing on it, the rod creaking at its pivot |
| The Gear Run's pawl (`trap_gear_release`) | in game | Synth | the warning: an iron catch knocked back, the gear's teeth clicking free over it, a groan as its weight comes on |
| The gear rolling (`trap_gear_roll`, loop) | in game | Synth | a deep rumble of iron on stone, its teeth clacking on the rails (pitched and loudened with its speed) |
| The gear's crash (`trap_gear_crash`) | in game | Synth | into the buffers: a hollow boom of iron into oak, the gear ringing like a struck bell, a timber groan, grit raining |
| The gear drawn up (`trap_gear_draw`, loop) | in game | Synth | its chain's links clattering over the drum, a ratchet ticking, the gear grumbling up the stone |
| A clock jack winding (`trap_jack_wind`) | in game | Synth | its hammer wound back up: a slow heavy ratchet, a spring tightening, a creak at the top |
| A clock jack's blow (`trap_jack_strike`) | in game | Synth | the hammer on the bronze strip: a hard strike and a thud through the boards, the strip ringing out like a bell on a low E |
| The grinding floor (`trap_grind`, loop) | in game | Synth | brass teeth grinding at the mesh: a harsh rasp, a squeal, the teeth knocking, a low drone of the train |
