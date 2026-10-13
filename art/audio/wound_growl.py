"""A Wound's bite tell (0.32 s): a growl through gears: a low rasp, toothed, rising a little."""
VARIANTS = 2
PEAK = 0.7


def render(v, rng):
    d = 0.4
    t = synth.seconds(d)
    f = (95 + 15 * v) * (1 + 0.25 * t / d)
    phase = 2 * np.pi * np.cumsum(f) / synth.SR
    saw = sum(np.sin(k * phase) / k for k in range(1, 18))
    rasp = synth.bandpass(saw * (1 + 0.6 * np.sign(np.sin(2 * np.pi * 38 * t))), 700, 1.0)
    return rasp * synth.envelope(d, 0.06, d - 0.06, curve=2.0)
