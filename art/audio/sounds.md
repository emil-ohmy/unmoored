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
| Oar hit on a Soured | planned | Synth or AI | dull, cloth-and-soul thud; distinct from the block clang |
| Laden wind-up / slam | planned | AI | heap lifted (creak), heavy wooden slam on stone |
| Soured alert (eyes flare) | planned | AI or Synth | a breathy, sour exhale; the audio half of the green flare |
| Footsteps on wood / stone | planned | Library or AI | boardwalk vs quay should sound different |
| Water lapping (ambient) | planned | Library or AI | bed under everything near the water |
| Underworld drone (ambient) | planned | Synth | low, slow, uneasy |
| Mooring post lit / Rest | planned | Synth | warm swell + lantern ignition |
| Healing | planned | Synth | soft glow |
| Apprentice hurt / death | planned | AI | small, muffled |
