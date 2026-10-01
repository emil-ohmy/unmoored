"""Warm Broth drunk: the cork pulled with a soft pop, three gulps through the mask, a small warm sigh after."""


def render(v, rng):
    duration = 1.3
    out = np.zeros(int(duration * synth.SR))
    pop = synth.bandpass(synth.white(0.05, rng), 900, 1.5) * synth.decay(0.05, 0.012)
    synth.place(out, pop * 0.6, 0.0)
    for k in range(3):
        gulp = synth.sine(synth.glide(rng.uniform(190, 230), 110, 0.12, 0.6), 0.12) * synth.envelope(0.12, 0.01, 0, curve=2.0)
        gulp += synth.lowpass(synth.white(0.12, rng), 700) * synth.envelope(0.12, 0.01, 0, curve=2.0) * 0.3
        synth.place(out, gulp * 0.55, 0.15 + k * 0.2 + rng.uniform(0, 0.03))
    sigh = synth.bandpass(synth.white(0.45, rng), 1000, 0.8) * synth.envelope(0.45, 0.08, 0, curve=2.0)
    synth.place(out, sigh / (np.max(np.abs(sigh)) + 1e-9) * 0.15, 0.8)
    return out
