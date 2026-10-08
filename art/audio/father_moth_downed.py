"""Father Moth brought down (his dive turned on the oar, or dragged down on the Hook): a great heavy crash onto the
stone, iron shears clattering, a wide spill of crumpling leaves, and a long hollow exhalation."""
VARIANTS = 1


def render(v, rng):
    duration = 1.8
    out = np.zeros(int(duration * synth.SR))
    crash = synth.sine(synth.glide(110, 40, 0.5), 0.5) * synth.decay(0.5, 0.12)
    crash += 0.6 * synth.lowpass(synth.white(0.5, rng), 900) * synth.decay(0.5, 0.06)
    synth.place(out, crash / (np.max(np.abs(crash)) + 1e-9) * 0.9, 0.0)
    for k in range(4):
        clang = synth.bandpass(synth.white(0.12, rng), rng.uniform(2400, 3800), 8.0) * synth.decay(0.12, 0.03)
        synth.place(out, clang / (np.max(np.abs(clang)) + 1e-9) * 0.25, 0.05 + k * rng.uniform(0.05, 0.1))
    leaves = synth.bandpass(synth.white(1.0, rng), 3000, 0.8) * synth.envelope(1.0, 0.05, 0, curve=2.2)
    synth.place(out, leaves / (np.max(np.abs(leaves)) + 1e-9) * 0.35, 0.05)
    breath = synth.lowpass(synth.brown(1.0, rng), 300) * synth.envelope(1.0, 0.3, 0, curve=1.6)
    synth.place(out, breath / (np.max(np.abs(breath)) + 1e-9) * 0.45, 0.6)
    return out
