"""A great bell, struck once as a beaten Boss's farewell comes up: a deep bronze toll (its hum, prime, minor third,
fifth and nominal, each ringing down at its own rate, a little out of tune with each other as real bells are), the
clang of the strike, and a long cathedral tail. 7 s. Solemn, not triumphant."""
VARIANTS = 1


def render(v, rng):
    d = 7.0
    t = synth.seconds(d)
    f = 98.0  # the strike note (G2)
    partials = [(0.5, 0.9, 2.8), (1.0, 1.0, 2.2), (1.183, 0.7, 1.7), (1.506, 0.45, 1.3), (2.0, 0.6, 1.1), (2.51, 0.3, 0.7),
                (2.66, 0.25, 0.6), (3.01, 0.2, 0.45), (4.07, 0.12, 0.3)]
    bell = np.zeros_like(t)
    for ratio, amp, tau in partials:
        for detune in (-0.0015, 0.0015):  # each partial a pair, beating slowly: the bell's shimmer
            bell += amp * np.sin(2 * np.pi * f * ratio * (1 + detune) * t + rng.uniform(0, 6)) * np.exp(-t / tau)
    strike = synth.bandpass(synth.white(0.08, rng), 2200, 1.0) * synth.decay(0.08, 0.015)
    out = bell / (np.max(np.abs(bell)) + 1e-9) * 0.8
    synth.place(out, strike / (np.max(np.abs(strike)) + 1e-9) * 0.25, 0.0)
    hall = synth.reverb_ir(6.5, 5.5, rng, predelay=0.06, brightness=0.3)[0]
    wet = synth.fft_convolve(out, hall)[:len(out)]
    out = out * 0.6 + wet / (np.max(np.abs(wet)) + 1e-9) * 0.55
    n = int(0.5 * synth.SR)
    out[-n:] *= np.linspace(1, 0, n)
    return out / (np.max(np.abs(out)) + 1e-9) * 0.9
