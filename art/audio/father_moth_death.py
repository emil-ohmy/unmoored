"""Father Moth, beaten: his wings folding with a long dry crackle, like a heap of dead leaves settling, a great soft
fall onto the stone, and a last slow breath going out of him; under it all, faintly, a music-box note (the Moths'
memories let go), low and unresolved."""
VARIANTS = 1


def render(v, rng):
    duration = 4.2
    out = np.zeros(int(duration * synth.SR))
    fold = synth.bandpass(synth.white(2.0, rng), 2800, 0.8) * synth.envelope(2.0, 0.4, 0, curve=1.4)
    fold *= np.clip(synth.smooth_random(2.0, rng, rate=40, low=-0.3, high=1.4), 0, None)
    synth.place(out, fold / (np.max(np.abs(fold)) + 1e-9) * 0.4, 0.0)
    fall = synth.sine(synth.glide(80, 38, 0.8), 0.8) * synth.decay(0.8, 0.2) + 0.4 * synth.lowpass(synth.brown(0.8, rng), 400) * synth.decay(0.8, 0.15)
    synth.place(out, fall / (np.max(np.abs(fall)) + 1e-9) * 0.8, 0.9)
    breath = synth.lowpass(synth.brown(2.2, rng), 280) * synth.envelope(2.2, 0.5, 0, curve=1.3)
    synth.place(out, breath / (np.max(np.abs(breath)) + 1e-9) * 0.4, 1.4)
    for name, at, gain in (("A5", 1.6, 0.18), ("E5", 2.4, 0.14), ("D5", 3.1, 0.1)):
        synth.place(out, synth.glass(synth.note(name), 1.0, rng) * gain, at)
    return out
