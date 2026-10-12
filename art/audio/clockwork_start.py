"""Stopped clockwork set running: a heavy brass catch letting go (a clunk), a ratchet clattering as the train takes up,
a rising whirr of wheels gathering speed."""
VARIANTS = 1


def render(v, rng):
    duration = 1.4
    out = np.zeros(int(duration * synth.SR))
    clunk = synth.lowpass(synth.brown(0.3, rng), 300) * synth.decay(0.3, 0.06)
    synth.place(out, clunk / (np.max(np.abs(clunk)) + 1e-9) * 0.7, 0.0)
    ring = synth.sine(1240, 0.5) * synth.decay(0.5, 0.1) * np.clip((0.5 - synth.seconds(0.5)) / 0.15, 0, 1)
    synth.place(out, ring * 0.12, 0.0)
    gap = 0.07
    at = 0.12
    while at < 0.9:  # the ratchet, quickening
        tick = synth.bandpass(synth.white(0.02, rng), rng.uniform(2600, 3400), 3.0) * synth.decay(0.02, 0.005)
        synth.place(out, tick * 0.35, at)
        at += gap
        gap = max(0.025, gap * 0.88)
    whirr_len = 1.1
    f = synth.glide(180, 520, whirr_len, shape=0.7)
    phase = 2 * np.pi * np.cumsum(f) / synth.SR
    whirr = (np.sign(np.sin(phase * 6)) * 0.3 + np.sin(phase)) * synth.envelope(whirr_len, 0.5, 0, curve=1.2)
    whirr = synth.bandpass(whirr, 900, 1.2)
    synth.place(out, whirr / (np.max(np.abs(whirr)) + 1e-9) * 0.25, 0.25)
    return out * np.clip((duration - synth.seconds(duration)) / 0.2, 0, 1)
