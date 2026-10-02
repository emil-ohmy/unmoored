"""Father Moth waking on the dead orange tree: a long dry crackle of leaves unfolding (wings like dead leaves, opening),
the tree's branches creaking under him, then two great slow wingbeats as he lifts."""
VARIANTS = 1


def render(v, rng):
    duration = 2.6
    out = np.zeros(int(duration * synth.SR))
    crackle = synth.bandpass(synth.white(1.4, rng), 3200, 0.8) * synth.envelope(1.4, 0.6, 0, curve=1.6)
    crackle *= np.clip(synth.smooth_random(1.4, rng, rate=60, low=-0.4, high=1.4), 0, None)
    synth.place(out, crackle / (np.max(np.abs(crackle)) + 1e-9) * 0.4, 0.0)
    for k in range(3):
        creak = synth.bandpass(synth.white(0.35, rng), synth.glide(rng.uniform(420, 520), rng.uniform(300, 380), 0.35), 9.0) * synth.envelope(0.35, 0.1, 0, curve=1.5)
        synth.place(out, creak / (np.max(np.abs(creak)) + 1e-9) * 0.35, 0.2 + k * 0.45)
    for at in (1.3, 1.95):
        whump = synth.lowpass(synth.brown(0.5, rng), synth.glide(420, 120, 0.5)) * synth.envelope(0.5, 0.1, 0, curve=2.0)
        synth.place(out, whump / (np.max(np.abs(whump)) + 1e-9) * 0.85, at)
    return out
