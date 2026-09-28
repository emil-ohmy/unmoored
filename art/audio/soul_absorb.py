"""A defeated Soured's fare streaming into the Apprentice's lantern: a rising, hollow whoosh with climbing shimmer
tones and a soft chime as the lantern takes it."""
def render(v, rng):
    duration = 1.4
    t = synth.seconds(duration)
    rise = synth.bandpass(synth.white(duration, rng), synth.glide(300, 2200, duration, 1.6), 3.0)
    rise *= synth.envelope(duration, 0, 0, curve=6.0, peak_at=1.0)
    shimmer = np.zeros_like(t)
    for base in (660.0, 880.0, 1320.0):
        shimmer += synth.sine(synth.glide(base, base * 2, duration, 1.3), duration, rng.uniform(0, 6))
    shimmer *= synth.envelope(duration, 0, 0, curve=5.0, peak_at=1.05) * 0.18
    chime = synth.glass(1760.0, 0.6, rng) * 0.35
    return synth.place(rise + shimmer, chime, 1.0)
