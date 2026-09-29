"""The Hook thrown: a quick whoosh and the chain rattling out after it."""


def render(v, rng):
    duration = 0.4
    out = np.zeros(int(duration * synth.SR))
    whoosh = synth.bandpass(synth.white(0.25, rng), synth.glide(900, 2600, 0.25), 1.4) * synth.envelope(0.25, 0.03, 0, curve=2.0)
    synth.place(out, whoosh / (np.max(np.abs(whoosh)) + 1e-9) * 0.5, 0.0)
    for i in range(14):  # links rattling out, quickening
        d = rng.uniform(0.004, 0.009)
        link = synth.bandpass(synth.white(d, rng), rng.uniform(2500, 4500), 4.0) * synth.decay(d, d / 3)
        synth.place(out, link / (np.max(np.abs(link)) + 1e-9) * rng.uniform(0.2, 0.45), 0.02 + 0.3 * (i / 14) ** 0.8)
    return out
