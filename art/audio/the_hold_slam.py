"""A great stone hand of the Hold slammed flat on the floor, 1.6 s with its tail: a very deep thud with the weight
of a wall in it (a low boom bending down, felt more than heard; a dark rumbling body), a hard crack on contact and
a broad flat slap (a palm, not a fist), the floor cracking under it, rubble and grit flung up and pattering down,
and the round chamber ringing a moment (its lowest resonances, a hollow stone boom) into a dark tail.
Variants: 1 plain, 2 deeper and heavier, more rubble, 3 a little higher and sharper. Uses the_hold_wake's kit.
`synth` and `np` are injected by tools/audio/build_sound.py."""
VARIANTS = 3
PEAK = [0.89, 0.78, 0.89]  # (2 is the heaviest: brought down to sit with the others)

SR = synth.SR
# (boom Hz, weight, slap, rubble count, chamber scale)
SHAPES = [(34, 1.0, 0.6, 40, 1.0), (30, 1.25, 0.5, 60, 0.92), (38, 0.85, 0.75, 32, 1.08)]


def kit():
    import importlib.util, os
    spec = importlib.util.spec_from_file_location("the_hold_wake", os.path.join(os.path.dirname(__file__), "the_hold_wake.py"))
    k = importlib.util.module_from_spec(spec)
    k.synth, k.np = synth, np
    spec.loader.exec_module(k)
    return k


def render(v, rng):
    k = kit()
    low, weight, slap, count, scale = SHAPES[v]
    d = 1.2
    n = int(d * SR)
    out = np.zeros(n)
    out += k.norm(k.impact(rng, low, d, weight, slap))
    out += k.norm(k.crack(rng, 1.2, d)) * 0.65
    out += k.norm(k.modes(d, rng, scale, 0.6)) * 0.3
    out += k.norm(k.rubble(d, rng, count, 0.04, 0.85, 1.0, pour=0.25)) * 0.22
    out = k.saturate(synth.highpass(out, 20), 2.2)
    out = k.chamber(out, rng, 1.4, 0.35)
    return k.trim(out, -50, 0.35)
