"""Something heavy goes into the water: a slap on the surface, a deep hollow 'bloop' as it goes under, the rush
of water closing over it, and a few bubbles. For a Soured knocked in, or the Apprentice falling off."""
VARIANTS = 2


def render(v, rng):
    duration = 1.4
    out = np.zeros(int(duration * synth.SR))
    slap = synth.lowpass(synth.white(0.12, rng), 2600) * synth.decay(0.12, 0.025)
    bloop = synth.sine(synth.glide(rng.uniform(170, 210), 60, 0.25, 0.5), 0.25) * synth.decay(0.25, 0.08)
    rush = synth.bandpass(synth.white(0.9, rng), synth.glide(1400, 350, 0.9), 1.2) * synth.envelope(0.9, 0.03, 0, curve=2.5)
    for part, gain, at in ((slap, 0.8, 0.0), (bloop, 0.9, 0.02), (rush, 0.45, 0.03)):
        synth.place(out, part / (np.max(np.abs(part)) + 1e-9) * gain, at)
    for _ in range(8):
        d = rng.uniform(0.03, 0.07)
        f0 = rng.uniform(350, 800)
        bubble = synth.sine(synth.glide(f0, f0 * 1.8, d, 0.6), d) * synth.envelope(d, 0.003, 0)
        synth.place(out, bubble * rng.uniform(0.08, 0.2), rng.uniform(0.15, 0.9))
    return out
