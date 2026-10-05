"""Father Moth's swoop, its tell and its pass: his wings snapped back with a hard dry clap, then a low rushing roar of
air building as he comes in level across the floor, leaves torn up in his wake."""
VARIANTS = 2


def render(v, rng):
    duration = 1.6
    out = np.zeros(int(duration * synth.SR))
    clap = synth.bandpass(synth.white(0.1, rng), 1600, 1.2) * synth.decay(0.1, 0.02)
    synth.place(out, clap / (np.max(np.abs(clap)) + 1e-9) * 0.75, 0.0)
    roar = synth.lowpass(synth.brown(1.4, rng), synth.glide(250, 900, 1.4, 1.6)) * synth.envelope(1.4, 1.0, 0, curve=2.5, peak_at=1.0)
    synth.place(out, roar / (np.max(np.abs(roar)) + 1e-9) * 0.7, 0.12)
    leaves = synth.bandpass(synth.white(0.8, rng), 3300, 0.9) * synth.envelope(0.8, 0.3, 0, curve=2.0)
    leaves *= np.clip(synth.smooth_random(0.8, rng, rate=45, low=-0.2, high=1.4), 0, None)
    synth.place(out, leaves / (np.max(np.abs(leaves)) + 1e-9) * 0.3, 0.75)
    return out
