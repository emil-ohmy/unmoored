"""A clock jack winding its hammer back up: a slow, heavy ratchet (a click a step), a spring tightening under it, a
creak of its arms at the top."""
VARIANTS = 1


def render(v, rng):
    duration = 1.9
    out = np.zeros(int(duration * synth.SR))
    at, k = 0.0, 0
    while at < 1.5:
        click = synth.bandpass(synth.white(0.03, rng), 1500 + 40 * k, 3.0) * synth.decay(0.03, 0.007)
        knock = synth.lowpass(synth.brown(0.06, rng), 350) * synth.decay(0.06, 0.015)
        synth.place(out, click * 0.5, at)
        synth.place(out, knock / (np.max(np.abs(knock)) + 1e-9) * 0.25, at)
        at += 0.13
        k += 1
    spring_len = 1.5
    f = synth.glide(300, 520, spring_len, shape=0.8)
    phase = 2 * np.pi * np.cumsum(f) / synth.SR
    spring = synth.bandpass(np.sin(phase) * (0.5 + 0.5 * np.sign(np.sin(phase * 0.13))), 600, 1.5) * synth.envelope(spring_len, 0.4, 0.4, curve=1.5)
    synth.place(out, spring / (np.max(np.abs(spring)) + 1e-9) * 0.12, 0.0)
    creak = synth.bandpass(synth.brown(0.3, rng), 330, 4.0) * synth.envelope(0.3, 0.1, 0.2, curve=2)
    synth.place(out, creak / (np.max(np.abs(creak)) + 1e-9) * 0.2, 1.5)
    return out * np.clip((duration - synth.seconds(duration)) / 0.15, 0, 1)
