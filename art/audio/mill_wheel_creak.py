"""The water wheel rocked by a wave that isn't the race's: a slap of water on its paddles, one groaning creak of the
old axle, a knock as it settles back."""
VARIANTS = 2


def render(v, rng):
    duration = 1.1
    out = np.zeros(int(duration * synth.SR))
    slap = synth.bandpass(synth.white(0.25, rng), 1000, 0.8) * synth.envelope(0.25, 0.01, 0, curve=2.5)
    synth.place(out, slap / (np.max(np.abs(slap)) + 1e-9) * 0.4, 0.0)
    length = 0.45
    f = synth.glide(rng.uniform(170, 200) + 15 * v, rng.uniform(115, 135), length)
    phase = 2 * np.pi * np.cumsum(f) / synth.SR
    creak = (np.sign(np.sin(phase)) * 0.4 + np.sin(phase)) * synth.envelope(length, 0.06, 0, curve=1.5)
    creak = synth.bandpass(creak, 600, 1.5) * (1 + 0.5 * synth.smooth_random(length, rng, rate=30.0))
    synth.place(out, creak / (np.max(np.abs(creak)) + 1e-9) * 0.35, 0.08)
    knock = synth.lowpass(synth.brown(0.2, rng), 220) * synth.decay(0.2, 0.04)
    synth.place(out, knock / (np.max(np.abs(knock)) + 1e-9) * 0.35, 0.65)
    return out * np.clip((duration - synth.seconds(duration)) / 0.2, 0, 1)
