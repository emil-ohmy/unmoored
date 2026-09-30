"""A Drowned rising (its tell, 0.7 s): water surging up and pouring off it, and its drowned groan swelling out of the
water, wet and choked."""
VARIANTS = 2


def render(v, rng):
    duration = 1.4
    out = np.zeros(int(duration * synth.SR))
    surge = synth.bandpass(synth.white(0.9, rng), synth.glide(300, 1600, 0.9, 0.7), 0.9) * synth.envelope(0.9, 0.35, 0, curve=2.0)
    pour = synth.lowpass(synth.white(0.8, rng), 1800) * synth.envelope(0.8, 0.05, 0, curve=1.5)
    groan = synth.voice(rng.uniform(80, 95), 0.9, rng, vowel="oo", vibrato=0.02)
    groan = synth.lowpass(groan, 900) * synth.envelope(0.9, 0.4, 0, curve=1.5)
    synth.place(out, surge / (np.max(np.abs(surge)) + 1e-9) * 0.6, 0.0)
    synth.place(out, pour / (np.max(np.abs(pour)) + 1e-9) * 0.3, 0.55)
    synth.place(out, groan / (np.max(np.abs(groan)) + 1e-9) * 0.5, 0.15)
    return out
