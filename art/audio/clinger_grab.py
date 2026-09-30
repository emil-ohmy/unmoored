"""A Clinger catching hold: cloth snatched and bunched in its fists (a rough, papery rustle), a soft bump against
the back, and a small, sated exhale. 0.5 s."""
VARIANTS = 2


def render(v, rng):
    d = 0.55
    out = np.zeros(int(d * synth.SR))
    rustle = synth.bandpass(synth.white(0.22, rng), synth.glide(2600, 1200, 0.22), 0.9) * synth.envelope(0.22, 0.01, 0, curve=2.5)
    rustle *= 0.6 + 0.4 * (synth.smooth_random(0.22, rng, rate=40) > 0.5)
    bump = synth.lowpass(synth.white(0.08, rng), 400) * synth.decay(0.08, 0.02) + synth.sine(95, 0.08) * synth.decay(0.08, 0.025)
    exhale = synth.lowpass(synth.bandpass(synth.white(0.3, rng), 700, 1.1), 1500) * synth.envelope(0.3, 0.05, 0, curve=2, peak_at=0.3)
    for part, gain, at in ((rustle, 0.6, 0.0), (bump, 0.7, 0.02), (exhale, 0.3, 0.2)):
        synth.place(out, part / (np.max(np.abs(part)) + 1e-9) * gain, at)
    return out
