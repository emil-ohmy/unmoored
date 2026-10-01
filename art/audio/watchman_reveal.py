"""The Watchman found out: his stake splinters as he tears free of it (cracks, a long rending of wood), a great dry
whoosh of straw thrown out of his coat, and under it a hollow breath, as from a sack."""
VARIANTS = 1


def render(v, rng):
    duration = 1.6
    out = np.zeros(int(duration * synth.SR))
    # The stake splintering: a handful of sharp cracks, then a short rending.
    t = 0.0
    for k in range(6):
        crack = synth.bandpass(synth.white(0.06, rng), rng.uniform(1300, 2600), 2.5) * synth.decay(0.06, 0.012)
        crack += 0.6 * synth.bandpass(synth.white(0.06, rng), 600, 3.0) * synth.decay(0.06, 0.02)
        synth.place(out, crack / (np.max(np.abs(crack)) + 1e-9) * rng.uniform(0.5, 0.85), t)
        t += rng.uniform(0.02, 0.06)
    rend = synth.bandpass(synth.white(0.35, rng), synth.glide(1800, 700, 0.35), 3.0) * synth.envelope(0.35, 0.03, 0, curve=2.0)
    rend *= 1 + 0.7 * np.sign(np.sin(2 * np.pi * synth.glide(60, 25, 0.35) * synth.seconds(0.35)))  # (fibres giving, one by one)
    synth.place(out, rend / (np.max(np.abs(rend)) + 1e-9) * 0.5, 0.12)
    # The straw thrown out: a dry whoosh falling away.
    whoosh = synth.bandpass(synth.white(1.1, rng), synth.glide(4200, 1500, 1.1, 0.6), 0.9) * synth.envelope(1.1, 0.1, 0, curve=2.2)
    synth.place(out, whoosh / (np.max(np.abs(whoosh)) + 1e-9) * 0.45, 0.15)
    # A hollow breath in the sack.
    breath = synth.lowpass(synth.brown(0.9, rng), 320) * synth.envelope(0.9, 0.25, 0, curve=1.8)
    synth.place(out, breath / (np.max(np.abs(breath)) + 1e-9) * 0.6, 0.3)
    return out
