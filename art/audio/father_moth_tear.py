"""Father Moth tearing a lamp down: its chain wrenched taut, iron links snapping and rattling loose."""
VARIANTS = 2


def render(v, rng):
    duration = 1.0
    out = np.zeros(int(duration * synth.SR))
    strain = synth.bandpass(synth.white(0.3, rng), synth.glide(700, 1200, 0.3), 6.0) * synth.envelope(0.3, 0.25, 0, curve=1.2, peak_at=0.28)
    synth.place(out, strain / (np.max(np.abs(strain)) + 1e-9) * 0.4, 0.0)
    snap = synth.bandpass(synth.white(0.08, rng), 3000, 3.0) * synth.decay(0.08, 0.012)
    synth.place(out, snap / (np.max(np.abs(snap)) + 1e-9) * 0.8, 0.3)
    t = 0.34
    for _ in range(9):
        link = synth.bandpass(synth.white(0.05, rng), rng.uniform(2800, 4500), 10.0) * synth.decay(0.05, 0.01)
        synth.place(out, link / (np.max(np.abs(link)) + 1e-9) * rng.uniform(0.2, 0.45), t)
        t += rng.uniform(0.03, 0.08)
    return out
