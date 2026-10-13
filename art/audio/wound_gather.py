"""A Wound's pounce tell (0.45 s): the spring drawn tight, a rising creak and a held clicking."""
PEAK = 0.7


def render(v, rng):
    d = 0.5
    t = synth.seconds(d)
    f = 300 + 500 * (t / d) ** 1.5
    phase = 2 * np.pi * np.cumsum(f) / synth.SR
    creak = (np.sin(phase) + 0.4 * np.sin(2.3 * phase)) * synth.bandpass(synth.white(d, rng), 1200, 0.8) * 3.0
    out = creak * (t / d) * np.clip((d - t) / 0.02, 0, 1)
    at = 0.0
    while at < d - 0.05:
        c = synth.bandpass(synth.white(0.02, rng), 3000, 4.0) * synth.decay(0.02, 0.003)
        synth.place(out, c * 0.4, at)
        at += 0.05
    return out
