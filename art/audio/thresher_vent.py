"""Steam blown from a Thresher all round it (whoever's at its back is scalded): a sudden roaring burst of steam."""
VARIANTS = 2


def render(v, rng):
    duration = 1.2
    burst = synth.bandpass(synth.white(duration, rng), synth.glide(3500, 1500, duration, 0.5), 0.6) * synth.envelope(duration, 0.03, 0, curve=2.2)
    thump = synth.lowpass(synth.brown(0.3, rng), 300) * synth.decay(0.3, 0.06)
    out = burst / (np.max(np.abs(burst)) + 1e-9) * 0.7
    out[:len(thump)] += thump / (np.max(np.abs(thump)) + 1e-9) * 0.5
    return out
