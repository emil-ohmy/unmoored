"""A pendulum's blade sweeping through the bottom of its swing, fast and low: a heavy rising-and-falling whoosh of
air cut by a long edge, a thin steel singing riding on it, and the rod's creak at its pivot far above."""
VARIANTS = 2


def render(v, rng):
    duration = 1.1
    out = np.zeros(int(duration * synth.SR))
    n = int(0.8 * synth.SR)
    t = np.arange(n) / synth.SR
    swell = np.sin(np.pi * np.clip(t / 0.8, 0, 1)) ** 2.5   # (peaking as it passes)
    air = synth.white(0.8, rng)[:n]
    centre = 420 + 380 * swell + 60 * v
    lo = synth.bandpass(air, 380 + 60 * v, 1.2)
    hi = synth.bandpass(air, 1100 + 120 * v, 1.6)
    whoosh = (lo * (0.4 + 0.6 * swell) + hi * swell ** 2) * swell
    synth.place(out, whoosh / (np.max(np.abs(whoosh)) + 1e-9) * 0.85, 0.0)
    sing = (np.sin(2 * np.pi * (2350 + 140 * v) * t) * 0.6 + np.sin(2 * np.pi * (3710 + 90 * v) * t) * 0.4) * swell ** 3
    synth.place(out, sing * 0.07, 0.0)
    creak = synth.bandpass(synth.brown(0.25, rng), 260, 3.0) * synth.envelope(0.25, 0.08, 0.17, curve=2)
    synth.place(out, creak / (np.max(np.abs(creak)) + 1e-9) * 0.12, 0.55)
    return out
