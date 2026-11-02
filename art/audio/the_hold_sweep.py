"""A great stone hand of the Hold swept low across the floor, 0.8 s: a heavy grinding scrape of stone dragged over
stone (a rough granular drag swelling to the middle of the sweep, the knocks of its weight catching), a deep whoosh
of the mass through the air with it, pebbles and grit kicked out ahead of it, a rumble under all. Fast in, as fast
out. Variants: 1 plain, 2 rougher and longer, 3 quicker and higher. Uses the_hold_wake's kit.
`synth` and `np` are injected by tools/audio/build_sound.py."""
VARIANTS = 3
PEAK = 0.85

SR = synth.SR
# (length, peak at, grind colour, whoosh centre)
SHAPES = [(0.8, 0.36, 230, 340), (0.86, 0.4, 200, 300), (0.72, 0.32, 270, 400)]


def kit():
    import importlib.util, os
    spec = importlib.util.spec_from_file_location("the_hold_wake", os.path.join(os.path.dirname(__file__), "the_hold_wake.py"))
    k = importlib.util.module_from_spec(spec)
    k.synth, k.np = synth, np
    spec.loader.exec_module(k)
    return k


def render(v, rng):
    k = kit()
    d, top, colour, whoosh_at = SHAPES[v]
    n = int(d * SR)
    t = synth.seconds(d)
    speed = k.curve(d, [(0, 0.1), (0.06, 0.5), (top, 1.0), (d * 0.8, 0.45), (d, 0.0)], 0.03)
    out = k.norm(k.grind(d, speed, rng, colour, grit=0.8)) * 0.8
    # the mass through the air
    def mag(times, freqs):
        sp = np.interp(times * SR, np.arange(n), speed)[:, None]
        f = np.maximum(freqs[None, :], 1)
        centre = whoosh_at * (0.6 + 0.7 * sp)
        return sp ** 1.5 * np.exp(-0.5 * (np.log(f / centre) / 0.8) ** 2) * (f / 50) ** 2 / (1 + (f / 50) ** 2)
    out += k.norm(synth.spectral_noise(d, rng, mag, n_fft=2048, hop=256)) * 0.55
    out += k.norm(k.rubble(d, rng, 30, 0.05, d * 0.75, 0.6)) * 0.25
    out = synth.highpass(out, 22)
    out = k.chamber(out, rng, 1.0, 0.3)
    return k.trim(out, -48, 0.25)
