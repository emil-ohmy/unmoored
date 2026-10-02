"""A Thresher charging: iron wheels rumbling over the ground, the flails threshing the air in a fast rattle, the
boiler roaring, all of it coming at you."""
VARIANTS = 2


def render(v, rng):
    duration = 2.2
    t = synth.seconds(duration)
    rumble = synth.lowpass(synth.brown(duration, rng), 180) * (1 + 0.3 * np.sin(2 * np.pi * 7 * t))
    rate = 30 + rng.uniform(-2, 2)
    clicks = np.diff(np.floor(np.cumsum(np.full(len(t), rate) / synth.SR)), prepend=0.0)
    whirr = synth.bandpass(clicks, 1500, 2.5) + 0.5 * synth.bandpass(synth.white(duration, rng), 900, 1.5) * 0.05
    roar = synth.bandpass(synth.white(duration, rng), 700, 0.8)
    out = rumble / (np.max(np.abs(rumble)) + 1e-9) * 0.6 + whirr / (np.max(np.abs(whirr)) + 1e-9) * 0.4 + roar / (np.max(np.abs(roar)) + 1e-9) * 0.2
    return out * synth.envelope(duration, 0.1, 0, curve=0.8, peak_at=0.3)
