"""A Charm fitted (or taken off) at a Mooring post: a cloth rustle, a small click of a clasp, a low warm chime."""


def render(v, rng):
    duration = 1.4
    out = np.zeros(int(duration * synth.SR))
    rustle = synth.bandpass(synth.white(0.25, rng), 2200, 0.8) * synth.envelope(0.25, 0.06, 0, curve=2.0)
    click = synth.bandpass(synth.white(0.012, rng), 3500, 3.0) * synth.decay(0.012, 0.003)
    synth.place(out, rustle / (np.max(np.abs(rustle)) + 1e-9) * 0.3, 0.0)
    synth.place(out, click / (np.max(np.abs(click)) + 1e-9) * 0.6, 0.18)
    synth.place(out, synth.glass(synth.note("A4"), 1.2, rng) * 0.5, 0.19)
    synth.place(out, synth.glass(synth.note("E5"), 1.0, rng) * 0.25, 0.24)
    return out
