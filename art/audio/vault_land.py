"""Coming down from the Vault: soft boots on stone (a muffled thud), the cloak settling, a little grit."""
VARIANTS = 2


def render(v, rng):
    duration = 0.4
    out = np.zeros(int(duration * synth.SR))
    thud = synth.sine(synth.glide(110 + 15 * v, 55, 0.15), 0.15) * synth.decay(0.15, 0.04)
    synth.place(out, thud * 0.7, 0.0)
    slap = synth.lowpass(synth.white(0.04, rng), 1400) * synth.decay(0.04, 0.01)
    synth.place(out, slap / (np.max(np.abs(slap)) + 1e-9) * 0.4, 0.0)
    cloth = synth.bandpass(synth.white(0.25, rng), 1200, 0.8) * synth.envelope(0.25, 0.03, 0.22, curve=2.5)
    synth.place(out, cloth / (np.max(np.abs(cloth)) + 1e-9) * 0.18, 0.03)
    for _ in range(5):
        g = synth.bandpass(synth.white(0.01, rng), rng.uniform(3000, 6000), 3.0) * synth.decay(0.01, 0.002)
        synth.place(out, g * rng.uniform(0.1, 0.25), rng.uniform(0.02, 0.15))
    fade = int(0.02 * synth.SR)
    out[-fade:] *= np.linspace(1, 0, fade)
    return out
