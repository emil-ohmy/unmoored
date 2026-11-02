"""One of the Kept struck, 0.3-0.5 s: a muffled, wet thump (a low tone bending down in mud, a squelch of pops) and a small
choked sound (a thin voice cut off in the throat, a bubble of tar on it; sometimes a tiny breath after). Frail:
it barely makes a sound.
Variants: 1 plain, 2 lighter and higher, 3 the heaviest, a breath after, 4 choked twice.
Uses kept_emerge's code. `synth` and `np` are injected by tools/audio/build_sound.py."""
VARIANTS = 4
PEAK = 0.8

SR = synth.SR
# (length, thump Hz, voice Hz, choke length, breath after, choked twice)
SHAPES = [(0.38, 95, 430, 0.11, False, False), (0.32, 115, 490, 0.09, False, False),
          (0.5, 82, 380, 0.13, True, False), (0.44, 100, 450, 0.08, False, True)]


def choke(kit, rng, f0, d):
    """A thin voice cut off in the throat: a quick start, then stopped (a glottal catch), a bubble on it."""
    t = synth.seconds(d)
    x = kit.throat(d, rng, synth.glide(f0, f0 * 0.82, d), synth.glide(800, 600, d), 0.65, 0.6)
    x = kit.norm(x) * np.clip(t / 0.01, 0, 1) * np.clip((d - t) / 0.008, 0, 1)
    return x


def render(v, rng):
    import importlib.util, os
    spec = importlib.util.spec_from_file_location("kept", os.path.join(os.path.dirname(__file__), "kept_emerge.py"))
    kit = importlib.util.module_from_spec(spec)
    kit.synth, kit.np = synth, np
    spec.loader.exec_module(kit)

    d, low, f0, cd, after, twice = SHAPES[v]
    n = int(d * SR)
    t = synth.seconds(d)
    out = np.zeros(n)
    synth.place(out, kit.norm(kit.thump(rng, low, 0.28)), 0.0)
    synth.place(out, kit.norm(kit.pops(0.1, 9, rng, 500, 1800, density=lambda u: 1 - u)) * 0.3, 0.004)
    at = 0.045
    synth.place(out, choke(kit, rng, f0, cd) * 0.42, at)
    if twice:
        synth.place(out, choke(kit, rng, f0 * 1.06, cd * 0.8) * 0.3, at + cd + 0.05)
    synth.place(out, kit.norm(kit.gurgle(0.08, rng, 2, 220, 420)) * 0.22, at + cd - 0.01)
    if after:
        bd = 0.16
        bt = synth.seconds(bd)
        b = synth.bandpass(synth.white(bd, rng), synth.glide(1100, 700, bd), 1.6) * np.sin(np.pi * bt / bd) ** 2
        synth.place(out, kit.norm(b) * 0.12, at + cd + 0.07)
    out = synth.highpass(out, 40)
    out = synth.lowpass(out, 6000)
    out = kit.room(out, rng, 0.12)
    return out * np.clip((d - t) / 0.08, 0, 1) ** 2
