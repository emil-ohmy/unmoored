"""A seed pod bursting (Father Moth's, phase 2): a dry husk swelling with a rising creak, then a sharp papery pop and a
scatter of leaves flung out."""
VARIANTS = 2


def render(v, rng):
    duration = 0.8
    out = np.zeros(int(duration * synth.SR))
    pop = synth.bandpass(synth.white(0.06, rng), rng.uniform(1600, 2200), 1.5) * synth.decay(0.06, 0.012)
    pop += 0.7 * synth.sine(synth.glide(260, 120, 0.06), 0.06) * synth.decay(0.06, 0.02)
    synth.place(out, pop / (np.max(np.abs(pop)) + 1e-9) * 0.8, 0.0)
    scatter = synth.bandpass(synth.white(0.6, rng), 3400, 0.9) * synth.envelope(0.6, 0.02, 0, curve=2.4)
    scatter *= np.clip(synth.smooth_random(0.6, rng, rate=50, low=-0.2, high=1.4), 0, None)
    synth.place(out, scatter / (np.max(np.abs(scatter)) + 1e-9) * 0.35, 0.02)
    return out
