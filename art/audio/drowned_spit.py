"""A Drowned spitting: a wet gathering hawk in its throat, then the spit leaving with a thick 'phtt'."""
VARIANTS = 2


def render(v, rng):
    duration = 0.7
    out = np.zeros(int(duration * synth.SR))
    hawk = synth.bandpass(synth.brown(0.3, rng), 500, 1.2) * synth.envelope(0.3, 0.2, 0, curve=1.6)
    hawk *= 1 + 0.7 * np.sin(2 * np.pi * 28 * np.arange(len(hawk)) / synth.SR)
    phtt = synth.bandpass(synth.white(0.12, rng), 2200, 1.4) * synth.decay(0.12, 0.03)
    synth.place(out, hawk / (np.max(np.abs(hawk)) + 1e-9) * 0.5, 0.0)
    synth.place(out, phtt * 0.8, 0.3)
    return out
