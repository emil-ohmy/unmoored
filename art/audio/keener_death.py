"""A Keener falls: a long sigh letting go (a voice sinking a fifth, then only breath), and its stopped watch going
again at last: a tick and a tock, quiet, as the grief is let go."""


def render(v, rng):
    duration = 2.6
    out = np.zeros(int(duration * synth.SR))
    d = 1.3
    sigh = synth.voice(synth.glide(synth.note("B4"), synth.note("E4"), d, 0.8), d, rng, vowel="oh", vibrato=0.01)
    sigh *= synth.envelope(d, 0.04, 0, curve=2.0, peak_at=0.08)
    synth.place(out, sigh / (np.max(np.abs(sigh)) + 1e-9) * 0.4, 0.0)
    breath = synth.bandpass(synth.white(1.2, rng), synth.glide(1200, 400, 1.2), 1.0) * synth.envelope(1.2, 0.1, 0, curve=2.5, peak_at=0.15)
    synth.place(out, breath / (np.max(np.abs(breath)) + 1e-9) * 0.3, 0.6)
    # The watch going again: tick (higher), tock (lower), a second apart's half.
    for at, centre in ((1.75, 4200), (2.25, 2900)):
        tick = synth.bandpass(synth.white(0.025, rng), centre, 5.0) * synth.decay(0.025, 0.004)
        synth.place(out, tick / (np.max(np.abs(tick)) + 1e-9) * 0.35, at)
    return out
