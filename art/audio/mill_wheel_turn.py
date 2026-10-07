"""The water wheel taking the surge: water crashing over its paddles, then the old wheel groaning round on its axle,
wood creaking in a slow rhythm, water pouring off the paddles as they rise, slowing."""
VARIANTS = 1


def render(v, rng):
    duration = 4.0
    out = np.zeros(int(duration * synth.SR))
    crash = synth.bandpass(synth.white(1.0, rng), 1100, 0.7) * synth.envelope(1.0, 0.02, 0, curve=2.2)
    synth.place(out, crash / (np.max(np.abs(crash)) + 1e-9) * 0.55, 0.0)
    pour = synth.bandpass(synth.white(duration, rng), 1600, 1.4) * synth.smooth_random(duration, rng, rate=8.0, low=0.3, high=1.0)
    pour *= np.exp(-synth.seconds(duration) / 1.6) * np.clip(synth.seconds(duration) / 0.3, 0, 1)
    synth.place(out, pour / (np.max(np.abs(pour)) + 1e-9) * 0.3, 0.1)
    # The creaks: a groaning wooden squeal, each a little lower and further apart (it slows).
    at, gap = 0.35, 0.55
    for k in range(6):
        length = 0.32
        f = synth.glide(rng.uniform(150, 190) - k * 8, rng.uniform(110, 130) - k * 6, length)
        phase = 2 * np.pi * np.cumsum(f) / synth.SR
        creak = (np.sign(np.sin(phase)) * 0.4 + np.sin(phase)) * synth.envelope(length, 0.05, 0, curve=1.5)
        creak = synth.bandpass(creak, 600, 1.5) * (1 + 0.5 * synth.smooth_random(length, rng, rate=30.0))
        synth.place(out, creak / (np.max(np.abs(creak)) + 1e-9) * (0.32 - k * 0.04), at)
        at += gap
        gap *= 1.15
    return out * np.clip((duration - synth.seconds(duration)) / 0.5, 0, 1)
