"""The Hour killed while time is stopped: its cables creaking as the weight goes slack, a dull dead clunk of brass, no
bell at all. The hour never comes."""
VARIANTS = 1


def render(v, rng):
    duration = 2.8
    t = synth.seconds(duration)
    out = np.zeros_like(t)
    creak = synth.bandpass(synth.brown(1.4, rng), 230, 5.0) * synth.envelope(1.4, 0.3, 1.0, curve=2)
    synth.place(out, creak / (np.max(np.abs(creak)) + 1e-9) * 0.4, 0.0)
    clunk = synth.lowpass(synth.brown(0.4, rng), 220) * synth.decay(0.4, 0.08)
    synth.place(out, clunk / (np.max(np.abs(clunk)) + 1e-9) * 0.8, 1.1)
    dull = np.sin(2 * np.pi * 180 * t) * np.exp(-np.clip(t - 1.1, 0, None) / 0.15) * (t > 1.1)
    out += dull * 0.15
    return out * np.clip((duration - t) / 0.4, 0, 1)
