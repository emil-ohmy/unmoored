"""A Gleaner skipping away from the oar: the smock flapping, a light double scuff of feet in the leaves, the pole's
lantern rattling on its crook."""
VARIANTS = 3


def render(v, rng):
    duration = 0.5
    out = np.zeros(int(duration * synth.SR))
    flap = synth.bandpass(synth.white(0.25, rng), 900, 1.0) * synth.envelope(0.25, 0.05, 0, curve=2.0)
    synth.place(out, flap / (np.max(np.abs(flap)) + 1e-9) * 0.5, 0.0)
    for at in (0.05, 0.3 + rng.uniform(-0.03, 0.03)):
        t = synth.seconds(0.12)
        scuff = synth.bandpass(synth.white(0.12, rng), rng.uniform(2200, 3600), 1.3) * np.exp(-t / 0.03)
        synth.place(out, scuff / (np.max(np.abs(scuff)) + 1e-9) * 0.4, at)
    t = synth.seconds(0.2)
    rattle = sum(np.sin(2 * np.pi * f * t) * np.exp(-t / 0.04) for f in (2100, 3400))
    synth.place(out, rattle / (np.max(np.abs(rattle)) + 1e-9) * 0.15, 0.08)
    return out
