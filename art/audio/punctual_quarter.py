"""A note of the quarter chime rung from a Punctual's chest, one per blow of the Quarters: G, F#, E, then B (the Hall's
own chime, an octave up: small bells). Variant n is the nth note; the last rings longest."""
VARIANTS = 4
PEAK = 0.75
NOTES = ["G5", "F#5", "E5", "B4"]


def render(v, rng):
    f = synth.note(NOTES[v])
    ring = 1.6 if v == 3 else 1.0
    d = 5.5 * ring  # (long enough to ring out)
    t = synth.seconds(d)
    out = np.zeros_like(t)
    for ratio, a, tau in ((0.5, 0.35, 1.6), (1.0, 1.0, 1.2), (1.19, 0.4, 0.8), (1.5, 0.25, 0.6), (2.0, 0.5, 0.7),
                          (2.52, 0.15, 0.35), (3.0, 0.1, 0.25)):
        out += a * np.sin(2 * np.pi * f * ratio * t + rng.uniform(0, 6)) * np.exp(-t / (tau * ring))
    strike = synth.bandpass(synth.white(0.03, rng), f * 4, 1.5) * synth.decay(0.03, 0.006) * 0.4
    out[:len(strike)] += strike
    return out * np.clip(t / 0.002, 0, 1)
