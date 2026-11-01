"""A Wound wound tight again: the key's last click, the spring caught (a bright twang), and its whirr starting up."""
PEAK = 0.75


def render(v, rng):
    d = 0.9
    t = synth.seconds(d)
    click = synth.bandpass(synth.white(d, rng), 3200, 4.0) * np.exp(-t / 0.004)
    f = 520 + 260 * np.exp(-t * 6)
    twang = (np.sin(2 * np.pi * np.cumsum(f) / synth.SR) + 0.4 * np.sin(2 * np.pi * np.cumsum(f * 2.7) / synth.SR)) * np.exp(-t / 0.25) * 0.5
    whirr = synth.bandpass(synth.white(d, rng), 2800, 3.0) * (0.5 + 0.5 * np.sign(np.sin(2 * np.pi * 24 * t))) * np.clip((t - 0.15) / 0.3, 0, 1) * np.clip((d - t) / 0.2, 0, 1) * 0.25
    return (click / (np.max(np.abs(click)) + 1e-9) + twang + whirr) * np.clip(t / 0.0008, 0, 1)
