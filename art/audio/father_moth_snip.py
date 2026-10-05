"""Father Moth's shears, drawn back to sweep: the blades opened with a long iron scrape, and a sharp snip-snip."""
VARIANTS = 2


def render(v, rng):
    duration = 0.75
    out = np.zeros(int(duration * synth.SR))
    scrape = synth.bandpass(synth.white(0.4, rng), synth.glide(2200, 3400, 0.4), 7.0) * synth.envelope(0.4, 0.15, 0, curve=1.5)
    synth.place(out, scrape / (np.max(np.abs(scrape)) + 1e-9) * 0.45, 0.0)
    for at in (0.45, 0.58):
        snip = synth.bandpass(synth.white(0.05, rng), 4200, 5.0) * synth.decay(0.05, 0.008)
        snip += 0.5 * synth.sine(2900, 0.05) * synth.decay(0.05, 0.01)
        synth.place(out, snip / (np.max(np.abs(snip)) + 1e-9) * 0.7, at)
    return out
