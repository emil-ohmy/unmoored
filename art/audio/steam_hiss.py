"""Fire put out by water: a sharp hiss of steam, quick to start and slowly thinning, a crackle dying in it."""
VARIANTS = 2


def render(v, rng):
    duration = 1.3
    t = synth.seconds(duration)
    hiss = synth.highpass(synth.white(duration, rng), 2500 + 800 * v) * np.clip(t / 0.03, 0, 1) * np.exp(-t / 0.45)
    out = hiss / (np.max(np.abs(hiss)) + 1e-9) * 0.5
    for _ in range(8):
        pop = synth.bandpass(synth.white(0.02, rng), rng.uniform(1500, 3000), 2.0) * synth.decay(0.02, 0.004)
        synth.place(out, pop * rng.uniform(0.1, 0.25), rng.uniform(0.0, 0.4))
    return out * np.clip((duration - t) / 0.2, 0, 1)
