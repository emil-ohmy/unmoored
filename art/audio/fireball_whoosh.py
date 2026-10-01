"""The Watchman's fire breathed out: a gout of flame leaving the lantern with a deep whumph and a roar trailing off."""
VARIANTS = 2


def render(v, rng):
    duration = 0.9
    out = np.zeros(int(duration * synth.SR))
    t = synth.seconds(0.25)
    whumph = synth.lowpass(synth.brown(0.25, rng), 400) * np.exp(-t / 0.06) + 0.6 * np.sin(2 * np.pi * synth.glide(110, 50, 0.25) * t) * np.exp(-t / 0.08)
    synth.place(out, whumph / (np.max(np.abs(whumph)) + 1e-9) * 0.8, 0.0)
    roar = synth.bandpass(synth.white(0.85, rng), synth.glide(1800, 600, 0.85, 0.7), 0.8) * synth.envelope(0.85, 0.04, 0, curve=2.0)
    roar *= 1 + 0.4 * synth.smooth_random(0.85, rng, 14.0, -1.0, 1.0)[:len(roar)]
    synth.place(out, roar / (np.max(np.abs(roar)) + 1e-9) * 0.5, 0.01)
    return out
