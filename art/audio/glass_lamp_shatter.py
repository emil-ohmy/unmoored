"""A glasshouse lamp hitting the floor: an iron clank, its glass bursting, shards skittering over the stone, the flame
gone with a soft puff."""
VARIANTS = 2


def render(v, rng):
    duration = 1.4
    out = np.zeros(int(duration * synth.SR))
    clank = synth.bandpass(synth.white(0.25, rng), 900, 4.0) * synth.decay(0.25, 0.05)
    synth.place(out, clank / (np.max(np.abs(clank)) + 1e-9) * 0.6, 0.0)
    burst = synth.highpass(synth.white(0.4, rng), 3000) * synth.decay(0.4, 0.07)
    synth.place(out, burst / (np.max(np.abs(burst)) + 1e-9) * 0.6, 0.005)
    for _ in range(14):
        f = rng.uniform(3500, 7000)
        tink = synth.sine(f, 0.06) * synth.decay(0.06, 0.012)
        synth.place(out, tink * rng.uniform(0.08, 0.22), rng.uniform(0.05, 0.9))
    puff = synth.lowpass(synth.brown(0.3, rng), 500) * synth.envelope(0.3, 0.05, 0, curve=2.0)
    synth.place(out, puff / (np.max(np.abs(puff)) + 1e-9) * 0.3, 0.08)
    return out
