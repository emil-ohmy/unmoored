"""The Hour striking a quarter: a cascade of the Hall's quarter chime (G, F#, E, B) rung huge and fast from inside it,
the last note left to ring, shimmering, over a deep hum."""
VARIANTS = 1
PEAK = 0.85


def render(v, rng):
    duration = 5.0
    t = synth.seconds(duration)
    out = np.zeros_like(t)
    for i, name in enumerate(["G4", "F#4", "E4", "B3"]):
        f = synth.note(name)
        at = i * 0.3
        n = len(t) - int(at * synth.SR)
        tt = np.arange(n) / synth.SR
        ring = 2.2 if i == 3 else 0.9
        note = sum(a * np.sin(2 * np.pi * f * r * tt + rng.uniform(0, 6)) * np.exp(-tt / (tau * ring))
                   for r, a, tau in ((0.5, 0.3, 1.5), (1.0, 1.0, 1.2), (1.19, 0.4, 0.8), (2.0, 0.5, 0.7), (2.52, 0.15, 0.35)))
        strike = synth.bandpass(synth.white(0.03, rng), f * 4, 1.5) * synth.decay(0.03, 0.006)
        note[:len(strike)] += strike * 0.5
        synth.place(out, note * (0.8 if i < 3 else 1.0), at)
    hum = np.sin(2 * np.pi * synth.note("B1") * t) * synth.envelope(duration, 0.4, 3.5, curve=1.5)
    out += hum * 0.25
    return out * np.clip((duration - t) / 0.6, 0, 1)
