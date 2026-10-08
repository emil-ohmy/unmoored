"""A withered orange shaken down from the glasshouse's vault, landing: a soft wet thud on the stone, the dry rind
splitting, a few seeds skittering."""
VARIANTS = 3


def render(v, rng):
    duration = 0.6
    out = np.zeros(int(duration * synth.SR))
    thud = synth.sine(synth.glide(140, 70, 0.15), 0.15) * synth.decay(0.15, 0.04)
    thud += 0.5 * synth.lowpass(synth.white(0.15, rng), 900) * synth.decay(0.15, 0.03)
    synth.place(out, thud / (np.max(np.abs(thud)) + 1e-9) * 0.8, 0.0)
    split = synth.bandpass(synth.white(0.12, rng), rng.uniform(1800, 2600), 2.0) * synth.decay(0.12, 0.025)
    synth.place(out, split / (np.max(np.abs(split)) + 1e-9) * 0.35, 0.01)
    for _ in range(4):
        tick = synth.bandpass(synth.white(0.03, rng), rng.uniform(3000, 5000), 6.0) * synth.decay(0.03, 0.006)
        synth.place(out, tick / (np.max(np.abs(tick)) + 1e-9) * rng.uniform(0.1, 0.2), rng.uniform(0.05, 0.35))
    return out
