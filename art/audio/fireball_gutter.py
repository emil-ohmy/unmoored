"""A ball of the Watchman's fire striking something: a soft burst, a spit of sparks, a hiss as it gutters out."""
VARIANTS = 2


def render(v, rng):
    duration = 0.8
    out = np.zeros(int(duration * synth.SR))
    t = synth.seconds(0.2)
    burst = synth.lowpass(synth.brown(0.2, rng), 900) * np.exp(-t / 0.04)
    synth.place(out, burst / (np.max(np.abs(burst)) + 1e-9) * 0.7, 0.0)
    hiss = synth.highpass(synth.white(0.75, rng), 2500) * synth.envelope(0.75, 0.02, 0, curve=2.5)
    synth.place(out, hiss / (np.max(np.abs(hiss)) + 1e-9) * 0.3, 0.02)
    for _ in range(10):
        at = rng.uniform(0.01, 0.3)
        c = synth.bandpass(synth.white(0.01, rng), rng.uniform(3000, 6000), 3.0)
        synth.place(out, c / (np.max(np.abs(c)) + 1e-9) * rng.uniform(0.15, 0.35), at)
    return out
