"""Ember Tar smeared on the oar: a wet slap, then the tar catching with a hiss that swells into a low steady burn."""


def render(v, rng):
    duration = 1.6
    out = np.zeros(int(duration * synth.SR))
    slap = synth.lowpass(synth.white(0.1, rng), 1500) * synth.decay(0.1, 0.03)
    hiss = synth.highpass(synth.white(1.3, rng), synth.glide(2500, 5000, 1.3)) * synth.envelope(1.3, 0.25, 0, curve=1.8)
    burn = synth.lowpass(synth.brown(1.4, rng), 500) * synth.envelope(1.4, 0.3, 0, curve=1.5)
    synth.place(out, slap * 0.6, 0.0)
    synth.place(out, hiss / (np.max(np.abs(hiss)) + 1e-9) * 0.3, 0.12)
    synth.place(out, burn / (np.max(np.abs(burn)) + 1e-9) * 0.5, 0.15)
    return out
