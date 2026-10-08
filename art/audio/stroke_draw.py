"""The Stroke drawn back: water pulled away as a tide pulls it, a low rushing swell rising and held, a cold glass tone
climbing under it (the Drowned Moon in the blade, gathering). Long enough to hold through a long draw; it's cut off
when the Stroke is let go."""
VARIANTS = 1


def render(v, rng):
    duration = 3.2
    t = synth.seconds(duration)
    swell = np.clip(t / 0.9, 0, 1) ** 1.5 * (0.85 + 0.15 * np.sin(2 * np.pi * 0.7 * t))
    rush = synth.lowpass(synth.white(duration, rng), 500) + 0.35 * synth.lowpass(synth.bandpass(synth.white(duration, rng), 1000, 1.2), 2200)
    rush *= synth.smooth_random(duration, rng, rate=3.0, low=0.6, high=1.0) * swell
    out = rush / (np.max(np.abs(rush)) + 1e-9) * 0.5
    # The glass, rising a fifth over the first second, then held (a slow shimmer).
    f = synth.glide(synth.note("A4"), synth.note("E5"), duration, shape=0.35)
    f = np.minimum(f, synth.note("E5"))
    phase = 2 * np.pi * np.cumsum(f) / synth.SR
    tone = (np.sin(phase) + 0.3 * np.sin(2 * phase + 0.4) + 0.12 * np.sin(3.01 * phase)) * np.clip(t / 0.6, 0, 1)
    tone *= 1 + 0.08 * np.sin(2 * np.pi * 5.2 * t)
    out += tone * 0.14
    out *= np.clip((duration - t) / 0.3, 0, 1)  # (a soft end, if it's held that long)
    return out
