"""A Moth diving: a soft papery whirr of wings rising to a short dry hiss as it comes."""
VARIANTS = 2


def render(v, rng):
    duration = 0.7
    out = np.zeros(int(duration * synth.SR))
    flutter = synth.bandpass(synth.white(0.6, rng), synth.glide(900, 2600, 0.6, 1.3), 1.6)
    flutter *= 1 + 0.8 * np.sin(2 * np.pi * synth.glide(24, 46, 0.6) * np.arange(len(flutter)) / synth.SR)
    flutter *= synth.envelope(0.6, 0.35, 0, curve=1.8)
    synth.place(out, flutter / (np.max(np.abs(flutter)) + 1e-9) * 0.45, 0.0)
    return out
