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
| Chapel floor cracking (`floor_crack`, 2) | in game | Synth | the warning before a section gives way: a grinding rumble swelling, cracks quickening, grit |
| Chapel floor giving way (`floor_break`, 2) | in game | Synth | a splitting crack, the deep boom of it dropping, stone grinding and tumbling, rubble plopping into the water |
| Anchoress's Undertow (`undertow`) | in game | Synth | 4 s: a low drowned murmur of prayer (two muffled voices) over water welling up, deep churning, bubbles thickening; her tell when she kneels |
| Apprentice hurt (`apprentice_hurt`, 3) | in game | Synth | muffled impact + short clipped grunt through the mask; also on washing ashore |
