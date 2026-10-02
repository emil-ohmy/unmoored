"""Father Moth's wingbeat, on the wing (one per beat): a great soft whump of air through dry leaves, and their rustle
after it, papery, like a heap of autumn leaves turned over."""
VARIANTS = 3


def render(v, rng):
    duration = 0.85
    out = np.zeros(int(duration * synth.SR))
    whump = synth.lowpass(synth.brown(0.4, rng), synth.glide(380, 140, 0.4)) * synth.envelope(0.4, 0.09, 0, curve=2.2)
    synth.place(out, whump / (np.max(np.abs(whump)) + 1e-9) * 0.7, 0.0)
    rustle = synth.bandpass(synth.white(0.7, rng), rng.uniform(2600, 3600), 0.9) * synth.envelope(0.7, 0.12, 0, curve=2.5)
    rustle *= 1 + 0.9 * synth.smooth_random(0.7, rng, rate=30, low=-1, high=1)
    synth.place(out, rustle / (np.max(np.abs(rustle)) + 1e-9) * 0.28, 0.06)
    return out
