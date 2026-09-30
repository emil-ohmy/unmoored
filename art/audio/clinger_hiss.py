"""The Clinger's tell as it crouches to leap: a short, sharp intake of breath turning into a hiss through the teeth,
rising. 0.35 s, like the wind-up it announces; clear over a fight, never loud."""
VARIANTS = 3


def render(v, rng):
    d = 0.38
    t = synth.seconds(d)
    breath = synth.bandpass(synth.white(d, rng), synth.glide(900, 2400, d, 0.7), 1.2) * synth.envelope(d, 0.12, 0, curve=1.6, peak_at=0.55)
    hiss = synth.highpass(synth.white(d, rng), 3500) * synth.envelope(d, 0.2, 0, curve=2.0, peak_at=0.8)
    rasp = synth.bandpass(synth.white(d, rng), 1300 + 200 * np.sin(2 * np.pi * rng.uniform(28, 36) * t), 5) * synth.envelope(d, 0.15, 0, curve=2, peak_at=0.7)
    out = breath * 0.5 + hiss * 0.35 + rasp * 0.25
    out = out / (np.max(np.abs(out)) + 1e-9) * 0.8
    n = int(0.05 * synth.SR)  # (it stops as it springs: short, not a click)
    out[-n:] *= np.linspace(1, 0, n)
    return out
