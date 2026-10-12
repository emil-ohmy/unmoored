"""Clockwork running down: the ticks slowing and slowing, the whirr sinking, a last weary tock and the catch dropping
home: still again."""
VARIANTS = 1


def render(v, rng):
    duration = 2.4
    out = np.zeros(int(duration * synth.SR))
    at, gap = 0.0, 0.5
    k = 0
    while at < 1.8:
        f0 = 1500 if k % 2 == 0 else 1150
        click = synth.bandpass(synth.white(0.04, rng), f0 * (1 - at * 0.15), 4.0) * synth.decay(0.04, 0.008)
        synth.place(out, click / (np.max(np.abs(click)) + 1e-9) * 0.45 * (1 - at * 0.3), at)
        at += gap
        gap *= 1.35
        k += 1
    whirr_len = 1.6
    f = synth.glide(480, 120, whirr_len, shape=0.8)
    phase = 2 * np.pi * np.cumsum(f) / synth.SR
    whirr = synth.bandpass(np.sin(phase) + 0.3 * np.sign(np.sin(phase * 6)), 700, 1.2) * synth.envelope(whirr_len, 0.02, 0, curve=1.6)
    synth.place(out, whirr / (np.max(np.abs(whirr)) + 1e-9) * 0.2, 0.0)
    clunk = synth.lowpass(synth.brown(0.3, rng), 280) * synth.decay(0.3, 0.06)
    synth.place(out, clunk / (np.max(np.abs(clunk)) + 1e-9) * 0.55, 1.95)
    return out * np.clip((duration - synth.seconds(duration)) / 0.15, 0, 1)
