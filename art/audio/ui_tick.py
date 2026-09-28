"""Moving through a menu: a tiny, soft wooden tick."""


def render(v, rng):
    d = 0.05
    tick = synth.bandpass(synth.white(d, rng), 1300, 4.0) * synth.decay(d, 0.006)
    return tick + 0.4 * synth.sine(900, d) * synth.decay(d, 0.008)
