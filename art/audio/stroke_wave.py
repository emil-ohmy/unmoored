"""The Stroke let go: the oar pulled through and a wave of force rolling out ahead, a deep thump of water, a broad rush
travelling away and breaking into spray (a hiss), a cold glass chime on top (the Drowned Moon's)."""
VARIANTS = 3


def render(v, rng):
    duration = 1.8
    out = np.zeros(int(duration * synth.SR))
    thump = synth.lowpass(synth.brown(0.4, rng), 160) * synth.envelope(0.4, 0.01, 0, curve=2.5)
    synth.place(out, thump / (np.max(np.abs(thump)) + 1e-9) * 0.8, 0.0)
    rush_len = 1.1
    cutoff_env = synth.glide(2200, 700, rush_len, shape=0.7)
    rush = synth.bandpass(synth.white(rush_len, rng), 1300 + 300 * v, 0.8)
    rush = rush * synth.envelope(rush_len, 0.06, 0, curve=1.8) * (cutoff_env / 2200)
    synth.place(out, rush / (np.max(np.abs(rush)) + 1e-9) * 0.55, 0.02)
    spray = synth.highpass(synth.white(0.9, rng), 4500) * synth.envelope(0.9, 0.15, 0, curve=2.2)
    synth.place(out, spray / (np.max(np.abs(spray)) + 1e-9) * 0.25, 0.18)
    for _ in range(10):
        drop = synth.sine(rng.uniform(1800, 3800), 0.05) * synth.decay(0.05, 0.012)
        synth.place(out, drop * rng.uniform(0.05, 0.12), rng.uniform(0.25, 1.0))
    chime = synth.glass(synth.note(["A5", "C6", "E6"][v]), 1.6, rng) * 0.14
    chime *= np.clip((1.6 - synth.seconds(1.6)) / 0.5, 0, 1)  # (faded out, not cut)
    synth.place(out, chime, 0.05)
    return out
