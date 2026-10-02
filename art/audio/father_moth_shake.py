"""Father Moth shaking Moths from his wings: a long dry shudder of leaves, and in it the faint papery flutter of
small wings scattering."""
VARIANTS = 1


def render(v, rng):
    duration = 1.5
    out = np.zeros(int(duration * synth.SR))
    shudder = synth.bandpass(synth.white(1.1, rng), 2600, 1.0) * synth.envelope(1.1, 0.2, 0, curve=1.6)
    shudder *= 1 + 0.95 * np.sin(2 * np.pi * synth.glide(14, 22, 1.1) * synth.seconds(1.1))
    synth.place(out, shudder / (np.max(np.abs(shudder)) + 1e-9) * 0.5, 0.0)
    for _ in range(7):
        d = rng.uniform(0.25, 0.4)
        flutter = synth.bandpass(synth.white(d, rng), rng.uniform(1800, 3200), 2.0) * synth.envelope(d, 0.05, 0, curve=2.0)
        flutter *= 1 + 0.8 * np.sin(2 * np.pi * rng.uniform(30, 45) * synth.seconds(d))
        synth.place(out, flutter / (np.max(np.abs(flutter)) + 1e-9) * 0.18, rng.uniform(0.5, 1.1))
    return out
