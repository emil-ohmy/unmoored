"""A Thresher crashing into something: a great iron-and-oak impact, timber splitting, the reel jamming with a screech,
then a long hiss of steam as it stalls."""
VARIANTS = 2


def render(v, rng):
    duration = 2.4
    out = np.zeros(int(duration * synth.SR))
    t = synth.seconds(0.6)
    boom = synth.lowpass(synth.brown(0.6, rng), 300) * np.exp(-t / 0.12) + 0.6 * np.sin(2 * np.pi * synth.glide(70, 35, 0.6) * t) * np.exp(-t / 0.18)
    synth.place(out, boom / (np.max(np.abs(boom)) + 1e-9), 0.0)
    clang = sum(np.sin(2 * np.pi * f * synth.seconds(0.8)) * np.exp(-synth.seconds(0.8) / tau) for f, tau in ((420, 0.3), (1130, 0.15), (2210, 0.08)))
    synth.place(out, clang / (np.max(np.abs(clang)) + 1e-9) * 0.45, 0.01)
    split = synth.bandpass(synth.white(0.3, rng), 1800, 2.0) * synth.decay(0.3, 0.05)
    synth.place(out, split / (np.max(np.abs(split)) + 1e-9) * 0.4, 0.04)
    screech = np.sin(2 * np.pi * np.cumsum(synth.glide(2400, 1300, 0.5)) / synth.SR) * synth.envelope(0.5, 0.05, 0, curve=2.0)
    synth.place(out, screech * 0.15, 0.08)
    hiss = synth.highpass(synth.white(1.9, rng), 2200) * synth.envelope(1.9, 0.15, 0, curve=1.8)
    synth.place(out, hiss / (np.max(np.abs(hiss)) + 1e-9) * 0.35, 0.3)
    return out
