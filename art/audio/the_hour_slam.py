"""The Hour's hammer coming down on the marble floor: a colossal crack and boom, stone splitting, the silver head
ringing, grit falling."""
VARIANTS = 2


def render(v, rng):
    duration = 2.4
    t = synth.seconds(duration)
    out = np.zeros_like(t)
    boom = synth.sine(synth.glide(80 + 10 * v, 34, 0.7, shape=0.5), 0.7) * synth.decay(0.7, 0.2)
    synth.place(out, boom, 0.0)
    crack = synth.lowpass(synth.white(0.18, rng), 3500) * synth.decay(0.18, 0.025)
    synth.place(out, crack / (np.max(np.abs(crack)) + 1e-9) * 0.8, 0.0)
    ring = sum(a * np.sin(2 * np.pi * f * t + rng.uniform(0, 6)) * np.exp(-t / tau)
               for f, a, tau in ((420 + 30 * v, 0.4, 0.7), (911, 0.25, 0.4), (1530, 0.15, 0.25)))
    out += ring * 0.3
    for _ in range(30):
        g = synth.bandpass(synth.white(0.012, rng), rng.uniform(2500, 6000), 3.0) * synth.decay(0.012, 0.003)
        synth.place(out, g * rng.uniform(0.08, 0.25), rng.uniform(0.05, 1.3))
    return out * np.clip((duration - t) / 0.4, 0, 1)
