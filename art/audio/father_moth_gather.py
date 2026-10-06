"""Father Moth gathering (phase 4's ultimate): a great breath drawn in that never ends, wind rushing inward from every
side and rising, a storm of dry leaves swirling, the hollow's roar deepening under it all, till it's almost too much."""
VARIANTS = 1


def render(v, rng):
    duration = 4.6
    out = np.zeros(int(duration * synth.SR))
    n = int(duration * synth.SR)
    # The wind drawn in: noise swept upward, swelling (a breath in, enormous).
    wind = synth.bandpass(synth.white(duration, rng), synth.glide(250, 1400, duration, 1.6), 0.8)
    wind *= np.linspace(0.15, 1.0, n) ** 1.5
    wind *= 1 + 0.4 * np.sin(2 * np.pi * synth.glide(0.8, 3.2, duration) * synth.seconds(duration))  # (swirling)
    synth.place(out, wind / (np.max(np.abs(wind)) + 1e-9) * 0.6, 0.0)
    # The leaves, swirling in.
    leaves = synth.bandpass(synth.white(duration, rng), 3300, 0.9) * np.linspace(0.1, 1.0, n)
    leaves *= np.clip(synth.smooth_random(duration, rng, rate=45, low=-0.2, high=1.4), 0, None)
    synth.place(out, leaves / (np.max(np.abs(leaves)) + 1e-9) * 0.3, 0.0)
    # The hollow's roar, deepening.
    roar = synth.lowpass(synth.brown(duration, rng), synth.glide(500, 160, duration)) * np.linspace(0.2, 1.0, n) ** 2
    synth.place(out, roar / (np.max(np.abs(roar)) + 1e-9) * 0.55, 0.0)
    fade = np.ones(n)
    fade[-int(0.08 * synth.SR):] = np.linspace(1, 0.3, int(0.08 * synth.SR))  # (cut near-short: the release takes over)
    return out * fade
