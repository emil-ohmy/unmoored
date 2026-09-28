"""Dash: a fast whoosh with a soft push-off thump, and a thin ghostly shimmer to go with the ghost images
(the i-frames). Spectral rather than cloth-y."""
VARIANTS = 3


def render(v, rng):
    duration = 0.38
    k = rng.uniform(0.93, 1.08)
    whoosh = synth.bandpass(synth.white(duration, rng), synth.arc(600 * k, 2600 * k, duration, 0.25), 1.6)
    whoosh *= synth.envelope(duration, 0, 0, curve=2.0, peak_at=0.08)
    thump = synth.sine(synth.glide(95, 55, 0.16), 0.16) * synth.decay(0.16, 0.025) * 0.5  # long enough to die out: no click
    shimmer = np.zeros(int(duration * synth.SR))
    t = synth.seconds(duration)
    for freq in (1318.5, 1760.0, 2637.0):
        f = freq * k + rng.uniform(-4, 4)
        shimmer += synth.sine(f, duration) * (0.7 + 0.3 * np.sin(2 * np.pi * 9 * t + rng.uniform(0, 6)))
    shimmer *= synth.envelope(duration, 0.03, 0, curve=3.0) * 0.06
    return synth.place(whoosh + shimmer, thump, 0.0)
