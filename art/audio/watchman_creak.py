"""The Watchman creeping (while you aren't looking): a stake creaking in the earth, slow stick-slip of dry wood, and a
whisper of straw shifting in his coat as it settles. Quiet: a sound you half hear behind you."""
VARIANTS = 3


def creak(duration, rng, rate_from, rate_to):
    """Stick-slip: a train of tiny clicks at a slowly changing rate, rung through the wood's resonances."""
    n = int(duration * synth.SR)
    rate = synth.glide(rate_from, rate_to, duration, 1.0) * (1 + 0.15 * synth.smooth_random(duration, rng, 8.0, -1.0, 1.0)[:n])
    phase = np.cumsum(rate / synth.SR)
    clicks = np.diff(np.floor(phase), prepend=0.0) * rng.uniform(0.6, 1.0, n)
    wood = synth.bandpass(clicks, 420 * rng.uniform(0.9, 1.1), 6.0) + 0.6 * synth.bandpass(clicks, 1150 * rng.uniform(0.9, 1.1), 5.0)
    return wood * synth.envelope(duration, 0.08, 0, curve=1.5, peak_at=duration * 0.4)


def render(v, rng):
    duration = 0.9
    out = np.zeros(int(duration * synth.SR))
    c = creak(0.6, rng, rng.uniform(70, 110), rng.uniform(40, 70))
    synth.place(out, c / (np.max(np.abs(c)) + 1e-9) * 0.5, 0.0)
    rustle = synth.bandpass(synth.white(0.5, rng), 3200, 1.2) * synth.envelope(0.5, 0.12, 0, curve=2.5)
    synth.place(out, rustle / (np.max(np.abs(rustle)) + 1e-9) * 0.16, 0.35)
    return out
