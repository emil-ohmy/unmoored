"""Father Moth's tell before he dives: his wings flung wide with a hard crack of air, held, a dry trembling rattle of
leaves, and the lantern in his ribs flaring up with a soft rising roar."""
VARIANTS = 2


def render(v, rng):
    duration = 0.95
    out = np.zeros(int(duration * synth.SR))
    crack = synth.lowpass(synth.white(0.12, rng), 1800) * synth.decay(0.12, 0.03)
    synth.place(out, crack / (np.max(np.abs(crack)) + 1e-9) * 0.7, 0.0)
    rattle = synth.bandpass(synth.white(0.8, rng), 2800, 1.5) * synth.envelope(0.8, 0.1, 0, curve=1.4)
    rattle *= 1 + 0.9 * np.sin(2 * np.pi * 38 * synth.seconds(0.8))
    synth.place(out, rattle / (np.max(np.abs(rattle)) + 1e-9) * 0.25, 0.05)
    flare = synth.bandpass(synth.white(0.85, rng), synth.glide(300, 900, 0.85, 1.4), 1.0) * synth.envelope(0.85, 0.75, 0, curve=1.0, peak_at=0.8)
    synth.place(out, flare / (np.max(np.abs(flare)) + 1e-9) * 0.5, 0.08)
    return out
