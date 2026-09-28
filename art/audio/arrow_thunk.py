"""An arrow strikes wood or stone and sticks: a short, hard thunk and a little quiver of the shaft."""
VARIANTS = 2


def render(v, rng):
    duration = 0.35
    out = np.zeros(int(duration * synth.SR))
    thunk = synth.bandpass(synth.white(0.06, rng), rng.uniform(500, 700), 2.5) * synth.decay(0.06, 0.012)
    body = synth.sine(synth.glide(220, 120, 0.08), 0.08) * synth.decay(0.08, 0.02)
    quiver = synth.sine(rng.uniform(95, 115), 0.3) * synth.decay(0.3, 0.07) * (1 + 0.5 * np.sin(2 * np.pi * 28 * synth.seconds(0.3)))
    synth.place(out, thunk / (np.max(np.abs(thunk)) + 1e-9) * 0.8, 0.0)
    synth.place(out, body * 0.6, 0.0)
    synth.place(out, quiver * 0.25, 0.01)
    return out
