"""The Watchman drawing breath over his lantern (his tell: face him, ready to turn it back): a long hollow inhale
through a sack, straw crackling with it, and the lantern's flame roaring up under it, swelling to the top."""
VARIANTS = 2


def render(v, rng):
    duration = 1.1
    out = np.zeros(int(duration * synth.SR))
    inhale = synth.bandpass(synth.white(1.0, rng), synth.glide(500, 1400, 1.0, 1.2), 1.3) * synth.envelope(1.0, 0.85, 0, curve=4.0, peak_at=0.9)
    synth.place(out, inhale / (np.max(np.abs(inhale)) + 1e-9) * 0.45, 0.0)
    t = synth.seconds(1.0)
    roar = synth.lowpass(synth.brown(1.0, rng), synth.glide(250, 1600, 1.0, 1.6)) * (t / 1.0) ** 2
    roar *= 1 + 0.35 * synth.smooth_random(1.0, rng, 9.0, -1.0, 1.0)[:len(roar)]
    synth.place(out, roar / (np.max(np.abs(roar)) + 1e-9) * 0.55, 0.05)
    crackle = np.zeros(int(1.0 * synth.SR))
    for _ in range(30):
        at = rng.uniform(0.1, 0.95)
        c = synth.bandpass(synth.white(0.012, rng), rng.uniform(2500, 5000), 3.0)
        synth.place(crackle, c * rng.uniform(0.3, 1.0) * at, at)
    synth.place(out, crackle / (np.max(np.abs(crackle)) + 1e-9) * 0.2, 0.0)
    fade = int(0.05 * synth.SR)
    out[-fade:] *= np.linspace(1, 0, fade)
    return out
