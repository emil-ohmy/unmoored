"""The Gear Run's pawl lifting: a heavy iron catch knocked back (a clunk), the great gear's teeth clicking free over
it, a groan of iron as its weight comes on. The warning: it's coming."""
VARIANTS = 1


def render(v, rng):
    duration = 1.2
    out = np.zeros(int(duration * synth.SR))
    clunk = synth.lowpass(synth.brown(0.35, rng), 260) * synth.decay(0.35, 0.07)
    synth.place(out, clunk / (np.max(np.abs(clunk)) + 1e-9) * 0.9, 0.0)
    ring = (synth.sine(780, 0.6) * 0.6 + synth.sine(1310, 0.6) * 0.4) * synth.decay(0.6, 0.12)
    synth.place(out, ring * 0.16, 0.0)
    at = 0.18
    for k in range(4):  # teeth clicking free over the pawl, slow then quicker
        tick = synth.bandpass(synth.white(0.03, rng), rng.uniform(1700, 2200), 3.0) * synth.decay(0.03, 0.007)
        synth.place(out, tick * 0.5, at)
        knock = synth.lowpass(synth.brown(0.08, rng), 400) * synth.decay(0.08, 0.02)
        synth.place(out, knock / (np.max(np.abs(knock)) + 1e-9) * 0.3, at)
        at += 0.16 - 0.03 * k
    groan_len = 0.8
    f = synth.glide(70, 52, groan_len)
    phase = 2 * np.pi * np.cumsum(f) / synth.SR
    groan = (np.sin(phase) + 0.4 * np.sin(phase * 2.03) + 0.2 * np.sign(np.sin(phase * 3.01))) * synth.envelope(groan_len, 0.25, 0.5, curve=1.5)
    synth.place(out, synth.lowpass(groan, 600) * 0.35, 0.25)
    return out * np.clip((duration - synth.seconds(duration)) / 0.15, 0, 1)
