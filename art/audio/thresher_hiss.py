"""A Thresher about to vent (its tell, close behind it): a sharp rising hiss of steam building in the pipes."""
VARIANTS = 1


def render(v, rng):
    duration = 0.6
    hiss = synth.highpass(synth.white(duration, rng), synth.glide(2000, 4000, duration)) * synth.envelope(duration, 0.55, 0, curve=4.0, peak_at=0.58)
    return hiss / (np.max(np.abs(hiss)) + 1e-9) * 0.6
