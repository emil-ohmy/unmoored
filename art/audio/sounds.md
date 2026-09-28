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
| Healing | planned | Synth | soft glow |
| Apprentice death (`death_toll`) | in game | Synth | one deep bell toll that sinks under water (highs close off), thump, sinking whoosh, bubbles; the music ducks under it |
| Apprentice hurt (`apprentice_hurt`, 3) | in game | Synth | muffled impact + short clipped grunt through the mask; also on washing ashore |
