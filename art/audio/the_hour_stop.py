"""The Hour about to stop you: its ticking racing faster and faster and rising in pitch, a held breath of air
drawing in, to the edge of a stop."""
VARIANTS = 1


def render(v, rng):
    duration = 1.2
    t = synth.seconds(duration)
    out = np.zeros_like(t)
    at, gap, k = 0.0, 0.14, 0
    while at < duration - 0.05:
        f0 = 1100 + 1600 * (at / duration)
        click = synth.bandpass(synth.white(0.025, rng), f0, 4.0) * synth.decay(0.025, 0.006)
        synth.place(out, click * (0.3 + 0.4 * at / duration), at)
        at += gap
        gap = max(0.025, gap * 0.85)
        k += 1
    air = synth.bandpass(synth.white(duration, rng), 2400, 1.0)[:len(t)] * (t / duration) ** 2
    out += air / (np.max(np.abs(air)) + 1e-9) * 0.25
    return out * np.clip((duration - t) / 0.02, 0, 1)
