"""Father Moth's release (phase 4's ultimate): everything he gathered let go at once: a huge deep whoomph, a blast of
wind and leaves tearing outward through the glasshouse, its panes rattling in their iron."""
VARIANTS = 1


def render(v, rng):
    duration = 2.6
    out = np.zeros(int(duration * synth.SR))
    boom = synth.sine(synth.glide(90, 32, 0.9), 0.9) * synth.decay(0.9, 0.25)
    boom += 0.8 * synth.lowpass(synth.brown(0.9, rng), synth.glide(900, 120, 0.9)) * synth.decay(0.9, 0.18)
    synth.place(out, boom / (np.max(np.abs(boom)) + 1e-9) * 0.95, 0.0)
    blast = synth.bandpass(synth.white(2.2, rng), synth.glide(1600, 500, 2.2, 0.6), 0.7) * synth.envelope(2.2, 0.03, 0, curve=2.0)
    synth.place(out, blast / (np.max(np.abs(blast)) + 1e-9) * 0.6, 0.02)
    leaves = synth.bandpass(synth.white(2.0, rng), 3400, 0.9) * synth.envelope(2.0, 0.05, 0, curve=1.8)
    leaves *= np.clip(synth.smooth_random(2.0, rng, rate=60, low=-0.2, high=1.4), 0, None)
    synth.place(out, leaves / (np.max(np.abs(leaves)) + 1e-9) * 0.35, 0.05)
    for _ in range(10):  # (the panes rattling in their iron)
        f = rng.uniform(1800, 3600)
        rattle = synth.bandpass(synth.white(0.08, rng), f, 8.0) * synth.decay(0.08, 0.02)
        synth.place(out, rattle / (np.max(np.abs(rattle)) + 1e-9) * rng.uniform(0.08, 0.18), rng.uniform(0.15, 1.2))
    return out
