"""A wrong scarecrow going up: straw catching all at once, a great dry whoomph of flame, crackling and roaring."""
VARIANTS = 2


def render(v, rng):
    duration = 1.6
    out = np.zeros(int(duration * synth.SR))
    t = synth.seconds(0.4)
    whoomph = synth.lowpass(synth.brown(0.4, rng), 500) * np.exp(-t / 0.1) + 0.7 * np.sin(2 * np.pi * synth.glide(90, 40, 0.4) * t) * np.exp(-t / 0.12)
    synth.place(out, whoomph / (np.max(np.abs(whoomph)) + 1e-9), 0.0)
    roar = synth.bandpass(synth.white(1.5, rng), synth.glide(2200, 800, 1.5, 0.6), 0.7) * synth.envelope(1.5, 0.05, 0, curve=2.0)
    synth.place(out, roar / (np.max(np.abs(roar)) + 1e-9) * 0.55, 0.0)
    crackle = np.zeros(int(1.5 * synth.SR))
    for _ in range(70):
        at = rng.uniform(0.0, 1.3)
        c = synth.bandpass(synth.white(0.012, rng), rng.uniform(2000, 6000), 3.0)
        synth.place(crackle, c * rng.uniform(0.3, 1.0) * (1.4 - at), at)
    synth.place(out, crackle / (np.max(np.abs(crackle)) + 1e-9) * 0.35, 0.02)
    return out
