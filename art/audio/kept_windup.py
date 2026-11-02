"""One of the Kept's tell as it raises its long arm, ~0.9 s (the wind-up it announces): a thin, rasping breath drawn in
through tar, rising, with a whimper in it: a small wordless voice (a child's height, roughened by a fast irregular
flutter, as if the throat were full of something thick), climbing with the arm; a wet bubble or two in the throat
as it starts. Nearly a person; not quite. It swells to the top and stops there (the swipe follows).
Variants: 1 mostly breath, a whimper in it, 2 more voice (a thin whimper rising), 3 the breath catches twice (a
sob), 4 lower and rougher. Close kin on purpose: a tell must be recognisable.
Uses kept_emerge's code. `synth` and `np` are injected by tools/audio/build_sound.py."""
VARIANTS = 4
PEAK = 0.7

SR = synth.SR
# (voice from Hz, to Hz, voiced, rasp, catches at, breath formant from, to)
SHAPES = [(300, 420, 0.35, 0.5, (), 600, 1400), (340, 520, 0.6, 0.4, (), 650, 1500),
          (310, 450, 0.45, 0.5, (0.3, 0.56), 600, 1400), (240, 350, 0.3, 0.75, (), 500, 1150)]


def render(v, rng):
    import importlib.util, os
    spec = importlib.util.spec_from_file_location("kept", os.path.join(os.path.dirname(__file__), "kept_emerge.py"))
    kit = importlib.util.module_from_spec(spec)
    kit.synth, kit.np = synth, np
    spec.loader.exec_module(kit)

    d = 0.9
    lo, hi, voiced, rasp, catches, b0, b1 = SHAPES[v]
    n = int(d * SR)
    t = synth.seconds(d)
    out = np.zeros(n)
    x = kit.throat(d, rng, synth.glide(lo, hi, d, 1.3), synth.glide(b0, b1, d, 1.2), voiced, rasp, "ah" if v != 3 else "oh")
    top = 0.84
    shape = 0.18 + 0.82 * np.clip(t / top, 0, 1) ** 1.6
    shape *= np.clip(t / 0.05, 0, 1) * np.clip((d - t) / (d - top), 0, 1) ** 1.5
    for at in catches:  # a sob: the breath catches, a tiny voiced hitch on it
        shape *= 1 - 0.85 * np.exp(-0.5 * ((t - at) / 0.022) ** 2)
    synth.place(out, kit.norm(x * shape), 0.0)
    for at in catches:
        hitch = synth.voice(synth.glide(hi * 0.95, hi * 0.8, 0.06), 0.06, rng, "ah") * synth.envelope(0.06, 0.008, 0.05)
        synth.place(out, kit.norm(hitch) * 0.3, at + 0.012)
    # a bubble or two in the throat as it starts
    synth.place(out, kit.norm(kit.gurgle(0.25, rng, 3 if v != 3 else 5, 180, 420, density=lambda u: 1 - u)) * 0.25, 0.02)
    out = synth.highpass(out, 120)
    out = kit.room(out, rng, 0.12)
    return out * np.clip((d - t) / 0.03, 0, 1)
