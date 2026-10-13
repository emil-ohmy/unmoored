"""A Wound's jaws snapping shut: iron teeth clacking, a short metal ring."""
VARIANTS = 3
PEAK = 0.8


def render(v, rng):
    d = 0.35
    t = synth.seconds(d)
    clack = synth.bandpass(synth.white(d, rng), 2000 + 300 * v, 2.5) * np.exp(-t / 0.008)
    ring = (np.sin(2 * np.pi * (1650 + 120 * v) * t) + 0.5 * np.sin(2 * np.pi * (3900 + 200 * v) * t)) * np.exp(-t / 0.06) * 0.35
    snap = synth.lowpass(synth.white(d, rng), 600) * np.exp(-t / 0.015) * 0.6
    return (clack / (np.max(np.abs(clack)) + 1e-9) + ring + snap) * np.clip(t / 0.0008, 0, 1)
