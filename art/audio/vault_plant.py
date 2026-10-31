"""The Vault: the oar's iron-shod foot planted on stone (a hard, bright knock and a short scrape as it bites), the ash
shaft flexing under the weight (a low wooden creak), and the cloak's whump as you go up."""
VARIANTS = 2


def render(v, rng):
    duration = 0.5
    out = np.zeros(int(duration * synth.SR))
    knock = synth.bandpass(synth.white(0.05, rng), 2400 + 300 * v, 2.5) * synth.decay(0.05, 0.008)
    ring = synth.sine(1850 + 140 * v, 0.12) * synth.decay(0.12, 0.03) * 0.25  # (the ferrule's iron)
    synth.place(out, knock / (np.max(np.abs(knock)) + 1e-9) * 0.8, 0.0)
    synth.place(out, ring, 0.0)
    scrape = synth.bandpass(synth.white(0.08, rng), 3500, 1.5) * synth.envelope(0.08, 0.005, 0.07) * 0.25
    synth.place(out, scrape, 0.01)
    d = 0.22
    t = synth.seconds(d)
    creak = np.sin(2 * np.pi * np.cumsum(synth.glide(180, 120, d)) / synth.SR) * synth.bandpass(synth.white(d, rng), 600, 1.2) * 3
    creak *= np.sin(np.pi * t / d) ** 2
    synth.place(out, creak / (np.max(np.abs(creak)) + 1e-9) * 0.3, 0.03)
    whump = synth.lowpass(synth.white(0.25, rng), synth.glide(900, 300, 0.25)) * synth.envelope(0.25, 0.06, 0.19, curve=2.0)
    synth.place(out, whump / (np.max(np.abs(whump)) + 1e-9) * 0.45, 0.1)
    fade = int(0.02 * synth.SR)
    out[-fade:] *= np.linspace(1, 0, fade)
    return out
