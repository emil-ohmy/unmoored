"""Father Moth's wingbeat, throwing his leaves: one great beat driven forward, a heavy whoomph, and a wind full of
dead leaves blowing out across the floor."""
VARIANTS = 2


def render(v, rng):
    duration = 1.6
    out = np.zeros(int(duration * synth.SR))
    beat = synth.lowpass(synth.brown(0.5, rng), synth.glide(500, 110, 0.5)) * synth.envelope(0.5, 0.06, 0, curve=2.0)
    synth.place(out, beat / (np.max(np.abs(beat)) + 1e-9) * 0.9, 0.0)
    wind = synth.bandpass(synth.white(1.4, rng), synth.glide(900, 500, 1.4), 0.7) * synth.envelope(1.4, 0.2, 0, curve=1.8)
    synth.place(out, wind / (np.max(np.abs(wind)) + 1e-9) * 0.5, 0.08)
    leaves = synth.bandpass(synth.white(1.3, rng), 3400, 0.9) * synth.envelope(1.3, 0.25, 0, curve=2.0)
    leaves *= np.clip(synth.smooth_random(1.3, rng, rate=50, low=-0.2, high=1.4), 0, None)
    synth.place(out, leaves / (np.max(np.abs(leaves)) + 1e-9) * 0.3, 0.15)
    return out
