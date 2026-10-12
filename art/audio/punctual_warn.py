"""The Quarters' tell (0.85 s): a clock's warning before it chimes: the lever drops with a click, and the train of wheels
starts to run, a quick dry whirr rising."""
PEAK = 0.7


def render(v, rng):
    d = 0.85
    out = np.zeros(int(d * synth.SR))
    t = synth.seconds(0.15)
    lever = synth.bandpass(synth.white(0.15, rng), 1600, 2.5) * np.exp(-t / 0.01) + np.sin(2 * np.pi * 900 * t) * np.exp(-t / 0.03) * 0.4
    synth.place(out, lever / (np.max(np.abs(lever)) + 1e-9), 0.0)
    at, gap = 0.08, 0.06
    while at < d - 0.05:
        c = synth.bandpass(synth.white(0.02, rng), rng.uniform(2600, 3400), 4.0) * synth.decay(0.02, 0.003)
        synth.place(out, c * (0.25 + 0.6 * at / d), at)
        at += gap
        gap = max(0.022, gap * 0.93)
    return out
