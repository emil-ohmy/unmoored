"""The Apprentice alight (a crackle played over and over while it lasts): cloth burning, a low flutter of flame and
a scatter of dry cracks."""
VARIANTS = 3


def render(v, rng):
    duration = 0.7
    out = np.zeros(int(duration * synth.SR))
    flutter = synth.lowpass(synth.brown(duration, rng), 700) * (1 + 0.5 * np.sin(2 * np.pi * rng.uniform(9, 13) * synth.seconds(duration)))
    flutter *= synth.envelope(duration, 0.15, 0, curve=1.2, peak_at=0.35)
    synth.place(out, flutter / (np.max(np.abs(flutter)) + 1e-9) * 0.5, 0.0)
    for _ in range(14):
        at = rng.uniform(0.02, 0.6)
        c = synth.bandpass(synth.white(0.012, rng), rng.uniform(2200, 5200), 3.0)
        synth.place(out, c / (np.max(np.abs(c)) + 1e-9) * rng.uniform(0.15, 0.45), at)
    return out
