"""The Hour winding itself back: its own ticking run backwards, faster and faster, a reversed swell (sound sucked in),
a ratchet whirring in reverse."""
VARIANTS = 1


def render(v, rng):
    duration = 1.6
    t = synth.seconds(duration)
    out = np.zeros_like(t)
    at, gap = 0.0, 0.16
    k = 0
    while at < duration - 0.1:
        click = synth.bandpass(synth.white(0.03, rng), 1500 if k % 2 else 1150, 4.0)
        click = click * np.linspace(0, 1, len(click)) ** 3  # (reversed: rising into the click)
        synth.place(out, click * 0.5, at)
        at += gap
        gap = max(0.03, gap * 0.86)
        k += 1
    swell = synth.bandpass(synth.white(duration, rng), 900, 0.8)[:len(t)] * (t / duration) ** 3
    out += swell / (np.max(np.abs(swell)) + 1e-9) * 0.5
    f = synth.glide(900, 160, duration, shape=0.6)
    phase = 2 * np.pi * np.cumsum(f) / synth.SR
    whirr = synth.bandpass(np.sign(np.sin(phase * 3)) * 0.3 + np.sin(phase), 700, 1.2) * synth.envelope(duration, 0.3, 0.2, curve=1.2)
    out += whirr / (np.max(np.abs(whirr)) + 1e-9) * 0.2
    return out * np.clip((duration - t) / 0.05, 0, 1)
