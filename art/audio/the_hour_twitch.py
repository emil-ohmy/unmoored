"""The limp Hour's twitch: a cable jerking, a dry creak of its lever arm, a weak scrape."""
VARIANTS = 2


def render(v, rng):
    duration = 1.0
    t = synth.seconds(duration)
    out = np.zeros_like(t)
    jerk = synth.lowpass(synth.brown(0.12, rng), 300) * synth.decay(0.12, 0.03)
    synth.place(out, jerk / (np.max(np.abs(jerk)) + 1e-9) * 0.5, 0.0)
    creak = synth.bandpass(synth.brown(0.6, rng), 300 + 60 * v, 6.0) * synth.envelope(0.6, 0.1, 0.4, curve=2)
    synth.place(out, creak / (np.max(np.abs(creak)) + 1e-9) * 0.35, 0.1)
    return out * np.clip((duration - t) / 0.2, 0, 1)
