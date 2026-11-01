"""The great gear crashing into the buffers at the run's foot: a huge hollow boom of iron into oak, the gear ringing
like a struck bell, timber splintering-groan, grit raining, the ring dying away."""
VARIANTS = 1


def render(v, rng):
    duration = 3.2
    out = np.zeros(int(duration * synth.SR))
    t = synth.seconds(duration)
    boom = synth.sine(synth.glide(95, 38, 0.6, shape=0.5), 0.6) * synth.decay(0.6, 0.18)
    synth.place(out, boom * 0.9, 0.0)
    slam = synth.lowpass(synth.white(0.25, rng), 1800) * synth.decay(0.25, 0.04)
    synth.place(out, slam / (np.max(np.abs(slam)) + 1e-9) * 0.7, 0.0)
    ring = np.zeros_like(t)
    for f, a, tau in ((196, 0.5, 1.4), (311, 0.4, 1.0), (467, 0.35, 0.8), (689, 0.25, 0.5), (1043, 0.15, 0.3)):
        ring += a * np.sin(2 * np.pi * f * t + rng.uniform(0, 6)) * np.exp(-t / tau)
    synth.place(out, ring * 0.35, 0.0)
    groan = synth.bandpass(synth.brown(0.7, rng), 190, 2.5) * synth.envelope(0.7, 0.05, 0.6, curve=2)
    synth.place(out, groan / (np.max(np.abs(groan)) + 1e-9) * 0.3, 0.06)
    for _ in range(26):  # grit
        g = synth.bandpass(synth.white(0.012, rng), rng.uniform(2500, 6000), 3.0) * synth.decay(0.012, 0.003)
        synth.place(out, g * rng.uniform(0.08, 0.25), rng.uniform(0.1, 1.2))
    return out * np.clip((duration - t) / 0.5, 0, 1)
