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
