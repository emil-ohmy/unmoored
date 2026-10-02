"""Father Moth diving: a deep rushing tear of air rising in pitch as he comes, leaves streaming in it."""
VARIANTS = 2


def render(v, rng):
    duration = 1.0
    out = np.zeros(int(duration * synth.SR))
    rush = synth.bandpass(synth.white(0.9, rng), synth.glide(300, 1500, 0.9, 1.5), 1.1) * synth.envelope(0.9, 0.6, 0, curve=2.5, peak_at=0.65)
    synth.place(out, rush / (np.max(np.abs(rush)) + 1e-9) * 0.7, 0.0)
    leaves = synth.bandpass(synth.white(0.9, rng), 3800, 1.0) * synth.envelope(0.9, 0.5, 0, curve=2.0) * (1 + 0.8 * synth.smooth_random(0.9, rng, rate=40, low=-1, high=1))
    synth.place(out, leaves / (np.max(np.abs(leaves)) + 1e-9) * 0.25, 0.05)
    return out
