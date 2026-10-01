"""The oar going through the Watchman (he's straw: no harm): a soft dull thump into stuffing, a dry rustle out."""
VARIANTS = 3


def render(v, rng):
    duration = 0.45
    out = np.zeros(int(duration * synth.SR))
    t = synth.seconds(0.15)
    thump = synth.lowpass(synth.brown(0.15, rng), 350) * np.exp(-t / 0.03)
    synth.place(out, thump / (np.max(np.abs(thump)) + 1e-9) * 0.6, 0.0)
    rustle = synth.bandpass(synth.white(0.4, rng), rng.uniform(2800, 3800), 1.1) * synth.envelope(0.4, 0.04, 0, curve=2.5)
    synth.place(out, rustle / (np.max(np.abs(rustle)) + 1e-9) * 0.4, 0.01)
    return out
