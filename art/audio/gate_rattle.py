"""A barred gate shaken from the wrong side: two or three wooden knocks against the barring plank and a short
rattle of loose bars. It won't open from here."""


def render(v, rng):
    duration = 0.7
    out = np.zeros(int(duration * synth.SR))
    for at in (0.0, 0.13, 0.24):
        d = 0.07
        knock = synth.bandpass(synth.white(d, rng), rng.uniform(380, 520), 2.5) * synth.decay(d, 0.018)
        synth.place(out, knock / (np.max(np.abs(knock)) + 1e-9) * rng.uniform(0.6, 0.9), at)
        for _ in range(4):
            dd = rng.uniform(0.004, 0.01)
            click = synth.bandpass(synth.white(dd, rng), rng.uniform(1200, 2500), 3.0) * synth.decay(dd, dd / 3)
            synth.place(out, click * 0.3, at + rng.uniform(0.01, 0.08))
    return out
