"""A Thresher's drive chain wrenched off by the Hook: a hard iron snap, the chain whipping and rattling loose, the reel
winding down, steam escaping."""
VARIANTS = 1


def render(v, rng):
    duration = 2.0
    out = np.zeros(int(duration * synth.SR))
    t = synth.seconds(0.3)
    snap = synth.bandpass(synth.white(0.3, rng), 2600, 1.5) * np.exp(-t / 0.02) + 0.5 * np.sin(2 * np.pi * 1700 * t) * np.exp(-t / 0.06)
    synth.place(out, snap / (np.max(np.abs(snap)) + 1e-9) * 0.8, 0.0)
    rattle = np.zeros(int(0.9 * synth.SR))
    at = 0.0
    while at < 0.85:
        c = sum(np.sin(2 * np.pi * f * synth.seconds(0.05)) * np.exp(-synth.seconds(0.05) / 0.012) for f in (rng.uniform(2500, 4200), rng.uniform(5000, 6500)))
        synth.place(rattle, c * rng.uniform(0.4, 1.0), at)
        at += rng.uniform(0.02, 0.07) * (1 + at)
    synth.place(out, rattle / (np.max(np.abs(rattle)) + 1e-9) * 0.4, 0.05)
    hiss = synth.highpass(synth.white(1.5, rng), 2200) * synth.envelope(1.5, 0.1, 0, curve=2.0)
    synth.place(out, hiss / (np.max(np.abs(hiss)) + 1e-9) * 0.3, 0.3)
    return out
