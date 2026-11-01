"""A Punctual finding the beat again (on the first of a bar): one firm tick, and the watch's small bell struck once
(E6)."""
PEAK = 0.65


def render(v, rng):
    d = 1.2
    t = synth.seconds(d)
    click = synth.bandpass(synth.white(d, rng), 4200, 4.0) * np.exp(-t / 0.004)
    f = synth.note("E6")
    bell = (np.sin(2 * np.pi * f * t) + 0.4 * np.sin(2 * np.pi * f * 2.0 * t) + 0.2 * np.sin(2 * np.pi * f * 2.76 * t)) * np.exp(-t / 0.4)
    return (click / (np.max(np.abs(click)) + 1e-9) + bell * 0.45) * np.clip(t / 0.0006, 0, 1)
