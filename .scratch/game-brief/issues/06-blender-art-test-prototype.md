# Art style test: what Claude can build in Blender
Type: prototype
Status: resolved
Blocked by:

## Question

Can Claude script low-poly toon-shaded models in Blender that look good from the fixed angled camera?
Build the Apprentice (small masked creature with an oversized oar), one enemy, and 2–3 props (jetty, rowing boat, rocks) via bpy, import them into Unity with a
URP toon shader (outline + stepped lighting) and show a screenshot-able scene. It must also include one scripted animation: the Soured's attack wind-up
(telegraph) + strike, readable from the fixed camera. The combat design relies on animation-only telegraphs (no ground markers).
Also try one tree for the Orchard of Last Autumn (foliage is the riskiest thing to script).
The user reacts. The answer
locks the art direction lines of the brief (shape language, palette approach, shading/outlines) or adjusts the style.

## Answer

Resolved 2026-09-27: the user reviewed renders and the live scene; all four verdict questions answered as recommended.

Prototype (primary source): branch `prototype/art-test` (commits 4e56f51, d6091fd). Blender script
`art/prototypes/art-test/build_art_test.py`, Unity side in `game/Assets/_Prototypes/ArtTest/` (toon shader, import rules,
scene builder with headless screenshots). Rebuild: Assets > MyGame > Build Art Test Scene.

- **Style locked**:
  - Low-poly faceted shapes, with characters built from simple readable forms (cone body, round head, mask) that
    have a clear silhouette from above.
  - Stepped toon light with lilac-tinted shadows, dark outlines, a soft rim.
  - A muted, cold teal-grey underworld with warm focal accents (the Apprentice's red hood, the Orchard's autumn
    colours, lanterns and eyes). Fog and bloom.
- **Telegraphs**: animation-only telegraphs are readable from the fixed camera (the Soured's windup: rearing up, arms
  raised, glowing eyes). Confirms the combat ticket's no-ground-marker rule.
- **Pipeline**: Claude models and animates everything from Blender scripts; the user art-directs by reacting to renders.
  Proven: a rigged Soured with 4 actions (Idle/Windup/Strike/Recover), the Apprentice, a jetty with a Mooring post and
  lantern, a boat, rocks/islets, and an Orchard tree (canopy blobs work; foliage is not a blocker).
- **Production improvements (not brief decisions)**: water (waves, foam, lantern reflections), stronger glow and real
  lantern lights, character detail + Apprentice animations, richer islets/ground.
- **Technical findings (for later, out of the brief)**:
  - `tools/blender/export_fbx.py` uses bake_space_transform, which Blender flags as broken with armatures. Rigged
    models should export without it and use Unity's bakeAxisConversion.
  - Static roots then keep a 90° X rotation, so placement code must compose with the prefab's root rotation.
  - Smoothed normals baked into tangents give closed outlines on faceted and skinned meshes.
  - Edit-mode renders don't re-skin; bake the pose for screenshots.
  - bmesh solidify needs normal_update() first.
