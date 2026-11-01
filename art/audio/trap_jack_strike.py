"""A clock jack's hammer coming down on the bronze strip in the floor: a hard strike and a thud through the boards,
then the strip ringing out like a bell, a low E (the Hall's chime, its deepest), long."""
VARIANTS = 1
PEAK = 0.85


def render(v, rng):
    f = synth.note("E3")
    duration = 4.5
    t = synth.seconds(duration)
    out = np.zeros_like(t)
    for ratio, a, tau in ((0.5, 0.3, 1.8), (1.0, 1.0, 1.5), (1.19, 0.45, 1.0), (1.5, 0.3, 0.8), (2.0, 0.55, 0.9),
                          (2.52, 0.2, 0.45), (3.01, 0.15, 0.3), (4.2, 0.1, 0.18)):
        out += a * np.sin(2 * np.pi * f * ratio * t + rng.uniform(0, 6)) * np.exp(-t / tau)
    strike = synth.bandpass(synth.white(0.04, rng), f * 6, 1.2) * synth.decay(0.04, 0.008)
    out[:len(strike)] += strike * 0.8
    thud = synth.sine(synth.glide(85, 45, 0.3), 0.3) * synth.decay(0.3, 0.07)
    out[:len(thud)] += thud * 0.9
    return out * np.clip(t / 0.002, 0, 1) * np.clip((duration - t) / 0.6, 0, 1)
