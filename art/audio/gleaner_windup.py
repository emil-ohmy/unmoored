"""A Gleaner drawing its pole back to throw (its tell): the long pole creaking as it swings up, the lantern's flame
gulping air and roaring up, a breath drawn in through the hood."""
VARIANTS = 2


def render(v, rng):
    duration = 1.0
    out = np.zeros(int(duration * synth.SR))
    n = int(0.5 * synth.SR)
    rate = synth.glide(rng.uniform(60, 80), rng.uniform(30, 45), 0.5)
    clicks = np.diff(np.floor(np.cumsum(rate / synth.SR)), prepend=0.0)
    creak = synth.bandpass(clicks, 520 * rng.uniform(0.9, 1.1), 6.0) + 0.5 * synth.bandpass(clicks, 1300, 5.0)
    creak *= synth.envelope(0.5, 0.1, 0, curve=1.5)
    synth.place(out, creak / (np.max(np.abs(creak)) + 1e-9) * 0.4, 0.0)
    t = synth.seconds(0.85)
    roar = synth.lowpass(synth.brown(0.85, rng), synth.glide(300, 1700, 0.85, 1.5)) * (t / 0.85) ** 2
    synth.place(out, roar / (np.max(np.abs(roar)) + 1e-9) * 0.5, 0.1)
    breath = synth.bandpass(synth.white(0.6, rng), synth.glide(600, 1200, 0.6), 1.2) * synth.envelope(0.6, 0.45, 0, curve=3.0, peak_at=0.5)
    synth.place(out, breath / (np.max(np.abs(breath)) + 1e-9) * 0.25, 0.25)
    fade = int(0.05 * synth.SR)
    out[-fade:] *= np.linspace(1, 0, fade)
    return out
