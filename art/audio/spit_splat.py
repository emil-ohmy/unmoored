"""A Drowned's spit striking: a thick wet splat, and a small fizzing hiss as the poison eats at what it hit."""
VARIANTS = 2


def render(v, rng):
    duration = 0.8
    out = np.zeros(int(duration * synth.SR))
    splat = synth.lowpass(synth.white(0.12, rng), 1800) * synth.decay(0.12, 0.03)
    blob = synth.sine(synth.glide(rng.uniform(260, 320), 120, 0.1, 0.6), 0.1) * synth.decay(0.1, 0.03)
    fizz = synth.highpass(synth.white(0.6, rng), 3500) * synth.envelope(0.6, 0.03, 0, curve=2.2)
    synth.place(out, splat * 0.7, 0.0)
    synth.place(out, blob * 0.5, 0.0)
    synth.place(out, fizz / (np.max(np.abs(fizz)) + 1e-9) * 0.2, 0.04)
    return out
