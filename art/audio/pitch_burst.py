"""A Pitch Pot bursting: clay cracking apart, a deep 'whumpf' as the pitch catches, then fire roaring up and settling
into a crackle (the pool burning on)."""
VARIANTS = 2


def render(v, rng):
    duration = 2.2
    out = np.zeros(int(duration * synth.SR))
    crack = synth.highpass(synth.white(0.08, rng), 1800) * synth.decay(0.08, 0.015)
    shards = [synth.bandpass(synth.white(0.05, rng), rng.uniform(2500, 5000), 3.0) * synth.decay(0.05, 0.01) for _ in range(5)]
    whumpf = synth.lowpass(synth.brown(0.5, rng), synth.glide(900, 120, 0.5)) * synth.envelope(0.5, 0.01, 0, curve=3.0)
    roar = synth.bandpass(synth.white(1.9, rng), synth.glide(700, 1500, 1.9, 0.6), 0.8) * synth.envelope(1.9, 0.08, 0, curve=1.6)
    synth.place(out, crack * 0.7, 0.0)
    for k, s in enumerate(shards):
        synth.place(out, s * 0.35, 0.01 + k * 0.03 + rng.uniform(0, 0.02))
    synth.place(out, whumpf / (np.max(np.abs(whumpf)) + 1e-9) * 0.9, 0.02)
    synth.place(out, roar / (np.max(np.abs(roar)) + 1e-9) * 0.35, 0.06)
    for _ in range(26):  # crackle
        at = rng.uniform(0.2, 2.0)
        pop = synth.bandpass(synth.white(0.02, rng), rng.uniform(1500, 4500), 2.0) * synth.decay(0.02, 0.004)
        synth.place(out, pop * rng.uniform(0.15, 0.4) * (1.0 - at / 2.2), at)
    return out
