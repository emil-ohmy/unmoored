"""Picking something up: a soft cloth rustle as the Apprentice stoops, and a small bright tick as the hand closes on it."""


def render(v, rng):
    duration = 0.7
    out = np.zeros(int(duration * synth.SR))
    rustle = synth.bandpass(synth.white(0.3, rng), 1900, 0.8) * synth.envelope(0.3, 0.08, 0, curve=2.0)
    synth.place(out, rustle / (np.max(np.abs(rustle)) + 1e-9) * 0.3, 0.0)
    synth.place(out, synth.glass(synth.note("A5"), 0.4, rng) * 0.35, 0.26)
    tick = synth.bandpass(synth.white(0.01, rng), 3000, 3.0) * synth.decay(0.01, 0.003)
    synth.place(out, tick / (np.max(np.abs(tick)) + 1e-9) * 0.4, 0.25)
    return out
