"""The field's gates slamming shut behind you (the Watchman found): two heavy plank leaves banging to against each
other, the latch dropping with an iron clank, a rattle of the old wood settling."""
VARIANTS = 1


def render(v, rng):
    duration = 1.3
    out = np.zeros(int(duration * synth.SR))
    for at, gain in ((0.0, 1.0), (0.05, 0.75)):  # (one leaf, then the other)
        t = synth.seconds(0.5)
        bang = synth.lowpass(synth.brown(0.5, rng), 600) * np.exp(-t / 0.06)
        bang += 0.6 * synth.bandpass(synth.white(0.5, rng), 900, 1.5) * np.exp(-t / 0.025)
        bang += 0.5 * np.sin(2 * np.pi * 95 * t) * np.exp(-t / 0.12)
        synth.place(out, bang / (np.max(np.abs(bang)) + 1e-9) * gain, at)
    t = synth.seconds(0.4)
    clank = sum(np.sin(2 * np.pi * f * t) * np.exp(-t / tau) for f, tau in ((1850, 0.08), (2970, 0.05), (4410, 0.03)))
    synth.place(out, clank / (np.max(np.abs(clank)) + 1e-9) * 0.35, 0.18)
    rattle = synth.bandpass(synth.white(0.7, rng), 1400, 2.0) * synth.envelope(0.7, 0.02, 0, curve=3.0)
    rattle *= 1 + 0.9 * np.sign(np.sin(2 * np.pi * synth.glide(26, 9, 0.7) * synth.seconds(0.7)))
    synth.place(out, rattle / (np.max(np.abs(rattle)) + 1e-9) * 0.25, 0.12)
    return out
