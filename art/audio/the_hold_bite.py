"""The Hold's face biting down at you, 0.6 s: stone jaws snapping shut. A short rush of the jaw closing, then a hard
clack of stone on stone (a sharp dry split, a hollow knock, the teeth meeting unevenly: a second, smaller clack a
moment behind), a dull weight under it, and a grinding edge as the jaws grate together, a few chips falling.
The clack is at 0.07 s. Variants: 1 plain, 2 a harder clack, 3 lower and heavier, a longer grate.
Uses the_hold_wake's kit.
`synth` and `np` are injected by tools/audio/build_sound.py."""
VARIANTS = 3
PEAK = 0.89

SR = synth.SR
CLACK = 0.07
# (clack size, knock Hz, second clack after, grind length, grind colour)
SHAPES = [(0.45, 210, 0.016, 0.3, 480), (0.35, 250, 0.011, 0.25, 560), (0.6, 170, 0.022, 0.4, 400)]


def kit():
    import importlib.util, os
    spec = importlib.util.spec_from_file_location("the_hold_wake", os.path.join(os.path.dirname(__file__), "the_hold_wake.py"))
    k = importlib.util.module_from_spec(spec)
    k.synth, k.np = synth, np
    spec.loader.exec_module(k)
    return k


def knock(rng, f, d=0.15):
    """A hollow stone knock: a short low-mid tone with a rough edge."""
    t = synth.seconds(d)
    tone = synth.sine(f * (1 + 0.4 * np.exp(-t / 0.004)), d) * np.exp(-t / 0.03)
    tone += 0.5 * synth.sine(f * 2.3, d, 1.0) * np.exp(-t / 0.015)
    return tone * np.clip(t / 0.0005, 0, 1)


def render(v, rng):
    k = kit()
    size, f, second, glen, colour = SHAPES[v]
    d = 0.6
    n = int(d * SR)
    t = synth.seconds(d)
    out = np.zeros(n)
    # the jaw closing: a short low rush into the clack
    rush = synth.bandpass(synth.white(CLACK + 0.01, rng), synth.glide(250, 700, CLACK + 0.01, 1.5), 1.0)
    rush *= np.clip(synth.seconds(CLACK + 0.01) / CLACK, 0, 1) ** 2
    synth.place(out, k.norm(rush) * 0.25, 0.0)
    synth.place(out, k.norm(k.crack(rng, size, 0.5)), CLACK)
    synth.place(out, k.norm(knock(rng, f)) * 0.75, CLACK)
    synth.place(out, k.norm(k.crack(rng, size * 0.6, 0.3)) * 0.5, CLACK + second)
    synth.place(out, k.norm(knock(rng, f * 1.2, 0.1)) * 0.4, CLACK + second)
    synth.place(out, k.norm(k.impact(rng, 50, 0.4, 0.35, 0.3)) * 0.55, CLACK)
    # the grate: the jaws grinding together a moment
    gd = glen
    speed = k.curve(gd, [(0, 0.9), (gd * 0.3, 0.6), (gd, 0.0)], 0.01)
    synth.place(out, k.norm(k.grind(gd, speed, rng, colour, grit=1.0)) * 0.4, CLACK + 0.03)
    out += k.norm(k.rubble(d, rng, 10, CLACK + 0.05, 0.35, 0.2)) * 0.12
    out = k.saturate(synth.highpass(out, 25), 2.0)
    out = k.chamber(out, rng, 0.9, 0.25)
    return k.trim(out, -48, 0.15)
