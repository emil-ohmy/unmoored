"""A Clinger thrown off: cloth tearing loose (a quick rip), the whoosh of it flung through the air, and the dull
thump and scrabble as it hits the ground and rolls. 0.7 s."""
VARIANTS = 2


def render(v, rng):
    d = 0.75
    out = np.zeros(int(d * synth.SR))
    rip = synth.bandpass(synth.white(0.1, rng), 3000, 0.8) * synth.envelope(0.1, 0.005, 0, curve=3)
    whoosh = synth.bandpass(synth.white(0.3, rng), synth.glide(500, 1400, 0.3, 0.5), 1.4) * synth.envelope(0.3, 0.1, 0, curve=2, peak_at=0.5)
    thump = synth.lowpass(synth.white(0.12, rng), 300) * synth.decay(0.12, 0.03) + 0.8 * synth.sine(synth.glide(110, 60, 0.12), 0.12) * synth.decay(0.12, 0.04)
    scrabble = synth.bandpass(synth.white(0.25, rng), 1800, 2.0) * (synth.smooth_random(0.25, rng, rate=30) > 0.55) * synth.envelope(0.25, 0.01, 0, curve=2)
    for part, gain, at in ((rip, 0.5, 0.0), (whoosh, 0.45, 0.03), (thump, 0.8, 0.36), (scrabble, 0.3, 0.42)):
        synth.place(out, part / (np.max(np.abs(part)) + 1e-9) * gain, at)
    return out
