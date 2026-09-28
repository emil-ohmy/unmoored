"""Render one sound script to WAV(s) in the Unity project.

    <blender python> tools/audio/build_sound.py art/audio/oar_swing.py

The script gets `synth` and `np` in scope and defines render(variant, rng) -> float array, plus optional
VARIANTS (default 1), LOOP = True (no fades: the script makes its own seamless loop), PEAK (normalised peak),
and FOLDER (a subfolder, e.g. "Music"). render may return mono (N,) or stereo (2, N).
Output: game/Assets/_Project/Audio/[FOLDER/]<name>.wav, or <name>_1.wav, <name>_2.wav ... for several variants.
"""
import os
import runpy
import sys

import numpy as np

sys.path.insert(0, os.path.dirname(__file__))
import synth  # noqa: E402

REPO = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
OUT = os.path.join(REPO, "game", "Assets", "_Project", "Audio")

source = os.path.abspath(sys.argv[1])
name = os.path.splitext(os.path.basename(source))[0]
module = runpy.run_path(source, init_globals={"synth": synth, "np": np})
variants = module.get("VARIANTS", 1)
loop = module.get("LOOP", False)
peak = module.get("PEAK", 0.89)
out_dir = os.path.join(OUT, module.get("FOLDER", ""))
os.makedirs(out_dir, exist_ok=True)
for v in range(variants):
    rng = np.random.default_rng(sum(map(ord, name)) * 100 + v)
    x = module["render"](v, rng)
    x = synth.finish(x, peak=peak, fade_in=0.0 if loop else 0.002, fade_out=0.0 if loop else 0.01)
    filename = f"{name}.wav" if variants == 1 else f"{name}_{v + 1}.wav"
    synth.write(os.path.join(out_dir, filename), x)
    print(f"[audio] {os.path.relpath(os.path.join(out_dir, filename), OUT)}  {x.shape[-1] / synth.SR:.2f}s  {'stereo' if x.ndim == 2 else 'mono'}")
