"""Round 3: one of the three not knocked aside, and the chain of them broken: a dull wooden thud and a music-box
note gone sour (two tines a semitone apart, flat, beating against each other)."""
VARIANTS = 1


def render(v, rng):
    duration = 1.6
    out = np.zeros(int(duration * synth.SR))
    t = synth.seconds(0.3)
    thud = synth.lowpass(synth.brown(0.3, rng), 300) * np.exp(-t / 0.05)
    synth.place(out, thud / (np.max(np.abs(thud)) + 1e-9) * 0.6, 0.0)
    for f in (synth.note("G#5") * 2 ** (-30 / 1200), synth.note("A5") * 2 ** (-25 / 1200)):
        synth.place(out, synth.glass(f, 1.5, rng) * 0.4, 0.02)
    fade = int(0.2 * synth.SR)
    out[-fade:] *= np.linspace(1, 0, fade) ** 2
    return out
