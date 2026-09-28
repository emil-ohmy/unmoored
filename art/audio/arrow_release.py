"""An arrow loosed: the string's twang (a quick, damped low pluck with a buzz) and the whoosh of the shaft leaving."""
VARIANTS = 2


def render(v, rng):
    duration = 0.5
    out = np.zeros(int(duration * synth.SR))
    f = rng.uniform(150, 175)
    twang = sum(a * synth.sine(f * k, duration, rng.uniform(0, 6)) for k, a in ((1, 1.0), (2, 0.5), (3, 0.3), (5, 0.15)))
    twang *= synth.decay(duration, 0.06) * np.clip(synth.seconds(duration) / 0.002, 0, 1)
    buzz = synth.bandpass(synth.white(0.08, rng), 2400, 3.0) * synth.decay(0.08, 0.02)
    whoosh = synth.bandpass(synth.white(0.3, rng), synth.glide(2500, 900, 0.3), 1.5) * synth.envelope(0.3, 0.02, 0, curve=2.0)
    synth.place(out, twang / (np.max(np.abs(twang)) + 1e-9) * 0.7, 0.0)
    synth.place(out, buzz / (np.max(np.abs(buzz)) + 1e-9) * 0.25, 0.0)
    synth.place(out, whoosh / (np.max(np.abs(whoosh)) + 1e-9) * 0.45, 0.01)
    return out
