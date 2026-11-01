"""A Wound's spring running: a quick dry ratchet and a thin humming spring, a seamless 1 s loop (the game pitches it
down as the hound runs down)."""
LOOP = True
PEAK = 0.5


def render(v, rng):
    d = 1.0
    n = int(d * synth.SR)
    out = np.zeros(n + int(0.1 * synth.SR))
    k, at = 0, 0.0
    while at < d:
        c = synth.bandpass(synth.white(0.02, rng), rng.uniform(2600, 3300), 4.0) * synth.decay(0.02, 0.003)
        synth.place(out, c * (0.8 if k % 4 == 0 else 0.45), at)
        at += 1.0 / 24
        k += 1
    t = np.arange(len(out)) / synth.SR
    hum = (np.sin(2 * np.pi * 220 * t) + 0.5 * np.sin(2 * np.pi * 440 * t) + 0.3 * np.sin(2 * np.pi * 661 * t)) * 0.06
    out += hum * (0.8 + 0.2 * np.sin(2 * np.pi * 4 * t))
    looped = out[:n].copy()
    looped[: len(out) - n] += out[n:]
    return looped
