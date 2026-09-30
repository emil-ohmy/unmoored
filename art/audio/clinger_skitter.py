"""A Clinger skittering over stone on its knuckles and claws: a quick scrabble of light, dry taps and clicks, a
little cloth drag under it. One burst, 0.35 s (played as it darts)."""
VARIANTS = 4


def render(v, rng):
    d = 0.4
    out = np.zeros(int(d * synth.SR))
    n = rng.integers(5, 9)
    for i in range(n):
        at = 0.02 + i * rng.uniform(0.035, 0.05)
        tap = synth.bandpass(synth.white(0.02, rng), rng.uniform(1800, 3200), 3.0) * synth.decay(0.02, 0.004)
        click = synth.pad(synth.sine(rng.uniform(2500, 4000), 0.012) * synth.decay(0.012, 0.002), 0.02)
        synth.place(out, (tap / (np.max(np.abs(tap)) + 1e-9) + 0.3 * click) * rng.uniform(0.35, 0.7), at)
    drag = synth.bandpass(synth.white(d, rng), 900, 1.2) * synth.envelope(d, 0.05, 0, curve=2, peak_at=0.4)
    synth.place(out, drag / (np.max(np.abs(drag)) + 1e-9) * 0.12, 0.0)
    n = int(0.06 * synth.SR)
    out[-n:] *= np.linspace(1, 0, n)
    return out
