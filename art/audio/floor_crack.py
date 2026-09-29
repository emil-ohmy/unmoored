"""A section of the chapel floor about to give way (the warning, 0.6 s): a deep grinding rumble swelling, stone
creaking and grit pattering, with sharp cracks coming quicker towards the end."""
VARIANTS = 2


def render(v, rng):
    duration = 0.9
    out = np.zeros(int(duration * synth.SR))
    rumble = synth.lowpass(synth.brown(0.75, rng), 160) * synth.envelope(0.75, 0.1, 0, curve=1.2, peak_at=0.6)
    grind = synth.bandpass(synth.white(0.7, rng), synth.glide(300, 520, 0.7), 3.0) * synth.envelope(0.7, 0.1, 0, curve=1.5, peak_at=0.6)
    synth.place(out, rumble / (np.max(np.abs(rumble)) + 1e-9) * 0.8, 0.0)
    synth.place(out, grind / (np.max(np.abs(grind)) + 1e-9) * 0.25, 0.02)
    at = 0.05
    while at < 0.62:  # cracks, quickening
        d = rng.uniform(0.015, 0.035)
        crack = synth.highpass(synth.white(d, rng), 1200) * synth.decay(d, d * 0.25)
        synth.place(out, crack * rng.uniform(0.25, 0.5) * (0.5 + at), at)
        at += rng.uniform(0.04, 0.14) * (1.2 - at)
    for _ in range(20):
        d = rng.uniform(0.004, 0.012)
        grit = synth.bandpass(synth.white(d, rng), rng.uniform(2500, 5000), 2.0) * synth.decay(d, d * 0.3)
        synth.place(out, grit * rng.uniform(0.05, 0.14), rng.uniform(0.1, 0.8))
    return out
