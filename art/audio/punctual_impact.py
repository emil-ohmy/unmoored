"""The Count's blow landing: the brass key brought down on stone: a clang (a few inharmonic metal partials), a thud,
grit."""
VARIANTS = 2
PEAK = 0.85


def render(v, rng):
    d = 1.2
    t = synth.seconds(d)
    clang = np.zeros_like(t)
    for f, a, tau in ((410, 1.0, 0.35), (1130, 0.6, 0.22), (2310, 0.35, 0.12), (3570, 0.2, 0.06)):
        clang += a * np.sin(2 * np.pi * f * (1 + 0.03 * v) * t + rng.uniform(0, 6)) * np.exp(-t / tau)
    thud = synth.lowpass(synth.brown(d, rng), 180) * np.exp(-t / 0.08)
    grit = synth.bandpass(synth.white(d, rng), 2500, 1.0) * np.exp(-t / 0.05)
    out = clang * 0.5 + thud / (np.max(np.abs(thud)) + 1e-9) * 0.8 + grit / (np.max(np.abs(grit)) + 1e-9) * 0.25
    return out * np.clip(t / 0.001, 0, 1)
