"""A Drowned's grab: a heavy wet slap of arms on stone, and the water heaving as it lunges."""
VARIANTS = 2


def render(v, rng):
    duration = 0.9
    out = np.zeros(int(duration * synth.SR))
    slap = synth.lowpass(synth.white(0.1, rng), 2200) * synth.decay(0.1, 0.02)
    thud = synth.sine(synth.glide(rng.uniform(110, 130), 55, 0.2, 0.5), 0.2) * synth.decay(0.2, 0.06)
    heave = synth.bandpass(synth.white(0.7, rng), synth.glide(1200, 400, 0.7), 1.0) * synth.envelope(0.7, 0.02, 0, curve=2.0)
    synth.place(out, slap * 0.8, 0.0)
    synth.place(out, thud * 0.8, 0.005)
    synth.place(out, heave / (np.max(np.abs(heave)) + 1e-9) * 0.4, 0.03)
    return out
