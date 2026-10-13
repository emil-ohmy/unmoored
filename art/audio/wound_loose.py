"""A Wound's key knocked loose by the Stroke: a hard ping off the brass, the spring let go all at once (a fast ratchet
spinning out), a wobbling twang."""
PEAK = 0.8


def render(v, rng):
    d = 1.2
    out = np.zeros(int(d * synth.SR))
    t = synth.seconds(d)
    ping = (np.sin(2 * np.pi * 2300 * t) + 0.5 * np.sin(2 * np.pi * 5100 * t)) * np.exp(-t / 0.12) * 0.5
    out += ping * np.clip(t / 0.0008, 0, 1)
    at, gap = 0.02, 0.012
    while at < 0.7:
        c = synth.bandpass(synth.white(0.02, rng), rng.uniform(2800, 3600), 4.0) * synth.decay(0.02, 0.003)
        synth.place(out, c * (0.7 - 0.8 * at), at)
        at += gap
        gap *= 1.07
    f = 600 * np.exp(-t * 2.0) + 150
    wob = np.sin(2 * np.pi * np.cumsum(f * (1 + 0.04 * np.sin(2 * np.pi * 9 * t))) / synth.SR) * np.exp(-t / 0.35) * 0.35
    return out + wob
