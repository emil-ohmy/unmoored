"""A Drowned waiting under the water: a few bubbles breaking the surface, and under them a low, muffled gurgle, like
a breath it can't take."""
VARIANTS = 3


def render(v, rng):
    duration = 1.6
    out = np.zeros(int(duration * synth.SR))
    for _ in range(rng.integers(4, 8)):
        f = rng.uniform(500, 1300)
        blip = synth.sine(synth.glide(f, f * 1.8, 0.05, 0.6), 0.05) * synth.decay(0.05, 0.012)
        synth.place(out, blip * rng.uniform(0.15, 0.35), rng.uniform(0.0, 1.2))
    gurgle = synth.lowpass(synth.brown(1.3, rng), 260) * synth.envelope(1.3, 0.2, 0, curve=2.0)
    gurgle *= 1 + 0.6 * np.sin(2 * np.pi * 7 * np.arange(len(gurgle)) / synth.SR)
    synth.place(out, gurgle / (np.max(np.abs(gurgle)) + 1e-9) * 0.4, 0.1)
    return out
