"""A Punctual's watch ticking, once a beat: small, bright and dry (a pocket watch, not the Hall's great escapement)."""
VARIANTS = 3
PEAK = 0.55


def render(v, rng):
    d = 0.14
    t = synth.seconds(d)
    click = synth.bandpass(synth.white(d, rng), 4600 + 300 * v, 5.0) * np.exp(-t / 0.003)
    ping = np.sin(2 * np.pi * (6200 + 150 * v) * t) * np.exp(-t / 0.014) * 0.35
    body = synth.lowpass(synth.white(d, rng), 900) * np.exp(-t / 0.01) * 0.25
    return (click / (np.max(np.abs(click)) + 1e-9) + ping + body) * np.clip(t / 0.0006, 0, 1)
