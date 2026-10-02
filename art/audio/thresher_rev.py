"""A Thresher revving to charge (its tell): the boiler's hiss building, the flail reel clattering up to speed, the iron
frame rattling, a deep chuff from the stack quickening."""
VARIANTS = 2


def render(v, rng):
    duration = 1.2
    out = np.zeros(int(duration * synth.SR))
    hiss = synth.highpass(synth.white(1.1, rng), 2500) * synth.envelope(1.1, 1.0, 0, curve=3.0, peak_at=1.0)
    synth.place(out, hiss / (np.max(np.abs(hiss)) + 1e-9) * 0.35, 0.0)
    # The reel: a clatter whose rate climbs.
    t = synth.seconds(1.1)
    rate = synth.glide(6, 28, 1.1, 1.3)
    clicks = np.diff(np.floor(np.cumsum(rate / synth.SR)), prepend=0.0)
    clatter = synth.bandpass(clicks, 1400, 3.0) + 0.6 * synth.bandpass(clicks, 3200, 3.0)
    synth.place(out, clatter / (np.max(np.abs(clatter)) + 1e-9) * 0.5, 0.0)
    # Chuffs from the stack, quickening.
    at = 0.0
    gap = 0.3
    while at < 1.05:
        c = synth.lowpass(synth.brown(0.15, rng), 400) * synth.decay(0.15, 0.04)
        synth.place(out, c / (np.max(np.abs(c)) + 1e-9) * 0.6, at)
        at += gap
        gap = max(0.09, gap * 0.78)
    fade = int(0.05 * synth.SR)
    out[-fade:] *= np.linspace(1, 0, fade)
    return out
