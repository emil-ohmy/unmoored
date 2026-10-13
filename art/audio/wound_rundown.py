"""A Wound running down: its spring's whirr slowing, the ratchet's clicks spreading out, the last one, and a sigh of
air."""
PEAK = 0.65


def render(v, rng):
    d = 1.3
    out = np.zeros(int(d * synth.SR))
    at, gap = 0.0, 1.0 / 24
    while at < 1.1:
        c = synth.bandpass(synth.white(0.02, rng), 2900 - 900 * at, 4.0) * synth.decay(0.02, 0.004)
        synth.place(out, c * (0.7 - 0.3 * at), at)
        at += gap
        gap *= 1.12
    t = synth.seconds(d)
    f = 220 * np.exp(-t * 1.8) + 40
    hum = np.sin(2 * np.pi * np.cumsum(f) / synth.SR) * np.exp(-t / 0.5) * 0.15
    sigh = synth.bandpass(synth.white(d, rng), 800, 0.7) * np.exp(-((t - 0.9) / 0.2) ** 2) * 0.12
    return out + hum + sigh
