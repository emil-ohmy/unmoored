"""Stopped in time: a hard glassy snap, then a ringing high tone held dead still (the world's sound gone thin), a
dull thump of the heart under it."""
VARIANTS = 1


def render(v, rng):
    duration = 1.6
    t = synth.seconds(duration)
    out = np.zeros_like(t)
    snap = synth.bandpass(synth.white(0.05, rng), 4200, 2.0) * synth.decay(0.05, 0.008)
    synth.place(out, snap / (np.max(np.abs(snap)) + 1e-9) * 0.9, 0.0)
    held = (np.sin(2 * np.pi * 2637 * t) * 0.5 + np.sin(2 * np.pi * 3951 * t) * 0.3) * synth.envelope(duration, 0.02, 1.3, curve=1.2)
    out += held * 0.22
    for at in (0.3, 1.05):
        thump = synth.sine(synth.glide(70, 45, 0.2), 0.2) * synth.decay(0.2, 0.05)
        synth.place(out, thump * 0.5, at)
    return out * np.clip((duration - t) / 0.2, 0, 1)
