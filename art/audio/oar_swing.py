"""Light oar swing: a quick, airy whoosh that brightens as the blade moves fastest (the 0.1 s active window).
`synth` and `np` are injected by tools/audio/build_sound.py."""
VARIANTS = 4


def render(v, rng):
    duration = 0.3 * rng.uniform(0.9, 1.1)
    k = rng.uniform(0.9, 1.12)  # each variant pitched a little differently
    air = synth.bandpass(synth.white(duration, rng), synth.arc(380 * k, 1700 * k, duration, 0.42), 1.4)
    body = synth.lowpass(synth.white(duration, rng), synth.arc(150, 600, duration, 0.4)) * 0.6
    return (air + body) * synth.envelope(duration, 0, 0, curve=2.5, peak_at=duration * 0.42)
