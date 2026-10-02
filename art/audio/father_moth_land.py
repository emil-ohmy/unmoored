"""Father Moth landing by a bed: a last heavy back-beat of the wings, his weight coming down on stone and water with a
dull thud and a splash, leaves settling."""
VARIANTS = 1


def render(v, rng):
    duration = 1.2
    out = np.zeros(int(duration * synth.SR))
    beat = synth.lowpass(synth.brown(0.4, rng), synth.glide(420, 140, 0.4)) * synth.envelope(0.4, 0.08, 0, curve=2.0)
    synth.place(out, beat / (np.max(np.abs(beat)) + 1e-9) * 0.7, 0.0)
    thud = synth.sine(synth.glide(90, 50, 0.3), 0.3) * synth.decay(0.3, 0.07)
    synth.place(out, thud * 0.8, 0.42)
    splash = synth.lowpass(synth.white(0.3, rng), 2600) * synth.decay(0.3, 0.06)
    synth.place(out, splash / (np.max(np.abs(splash)) + 1e-9) * 0.35, 0.43)
    settle = synth.bandpass(synth.white(0.6, rng), 3000, 1.0) * synth.envelope(0.6, 0.05, 0, curve=2.5)
    synth.place(out, settle / (np.max(np.abs(settle)) + 1e-9) * 0.2, 0.5)
    return out
