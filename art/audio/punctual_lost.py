"""A Punctual losing count: its spring slipping, the hands spinning: a fast ratchet running down, a twang of the
mainspring, a few stray ticks."""
PEAK = 0.7


def render(v, rng):
    d = 1.3
    out = np.zeros(int(d * synth.SR))
    at, gap = 0.0, 0.018
    while at < 0.9:
        c = synth.bandpass(synth.white(0.02, rng), rng.uniform(2800, 3800), 4.0) * synth.decay(0.02, 0.003)
        synth.place(out, c * (0.8 - 0.7 * at / 0.9), at)
        at += gap
        gap *= 1.05
    w = 0.9
    t = synth.seconds(w)
    f = 760 * np.exp(-t * 1.6) + 180
    phase = 2 * np.pi * np.cumsum(f * (1 + 0.02 * np.sin(2 * np.pi * 11 * t))) / synth.SR
    twang = (np.sin(phase) + 0.5 * np.sin(2.7 * phase)) * np.exp(-t / 0.3) * 0.45
    synth.place(out, twang * np.clip(t / 0.003, 0, 1), 0.05)
    for at in (0.95, 1.12):
        c = synth.bandpass(synth.white(0.03, rng), 4500, 5.0) * synth.decay(0.03, 0.003)
        synth.place(out, c * 0.4, at)
    return out
