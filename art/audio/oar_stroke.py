"""A paddle stroke while rowing: the blade slipping into the water (a soft, small plunge, no slap), the pull (a
gurgling swirl of water drawn past the blade), and the drip as it lifts out. Calm, close, quiet: it repeats every
stroke, so it must never tire the ear. 0.9 s; variants differ in the swirl and the drips."""
VARIANTS = 4


def render(v, rng):
    duration = 0.95
    out = np.zeros(int(duration * synth.SR))
    # The dip: a short, dull plunge (low-passed noise and a tiny hollow bloop).
    dip = synth.lowpass(synth.white(0.09, rng), 1500) * synth.envelope(0.09, 0.012, 0, curve=2.5)
    f = rng.uniform(260, 330)
    bloop = synth.sine(synth.glide(f, f * 0.55, 0.12, 0.6), 0.12) * synth.decay(0.12, 0.04)
    # The pull: water swirling past the blade, a slow wobble in the band.
    d = 0.45
    wobble = 700 + 250 * synth.smooth_random(d, rng, rate=9, low=-1, high=1)
    swirl = synth.bandpass(synth.white(d, rng), wobble, 1.6) * synth.envelope(d, 0.08, 0, curve=1.8, peak_at=0.3)
    swirl = synth.lowpass(swirl, 2200)
    for part, gain, at in ((dip, 0.45, 0.0), (bloop, 0.35, 0.01), (swirl, 0.4, 0.06)):
        synth.place(out, part / (np.max(np.abs(part)) + 1e-9) * gain, at)
    for _ in range(rng.integers(3, 6)):  # drips off the blade as it lifts
        dd = rng.uniform(0.02, 0.04)
        f0 = rng.uniform(900, 1600)
        drip = synth.sine(synth.glide(f0, f0 * 1.5, dd, 0.5), dd) * synth.envelope(dd, 0.002, 0)
        synth.place(out, drip * rng.uniform(0.05, 0.12), rng.uniform(0.5, 0.85))
    return out
