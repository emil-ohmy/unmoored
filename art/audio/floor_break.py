"""A section of the chapel floor giving way: a huge splitting crack, the deep boom of it dropping, stone grinding
and tumbling, and rubble clattering and splashing down after it. The water's own splash plays with it."""
VARIANTS = 2


def render(v, rng):
    duration = 2.0
    out = np.zeros(int(duration * synth.SR))
    crack = synth.highpass(synth.white(0.06, rng), 700) * synth.decay(0.06, 0.012)
    boom = synth.sine(synth.glide(rng.uniform(70, 85), 32, 0.9, 0.5), 0.9) * synth.decay(0.9, 0.3)
    thud = synth.lowpass(synth.white(0.3, rng), 300) * synth.decay(0.3, 0.07)
    grind = synth.lowpass(synth.brown(1.2, rng), 420) * synth.envelope(1.2, 0.02, 0, curve=2.0)
    for part, gain, at in ((crack, 0.9, 0.0), (boom, 1.0, 0.01), (thud, 0.7, 0.0), (grind, 0.5, 0.05)):
        synth.place(out, part / (np.max(np.abs(part)) + 1e-9) * gain, at)
    for _ in range(16):  # rubble: knocks of stone on stone, and plops into the water
        d = rng.uniform(0.03, 0.08)
        f = rng.uniform(250, 700)
        knock = synth.bandpass(synth.white(d, rng), f, 2.5) * synth.decay(d, d * 0.3)
        synth.place(out, knock / (np.max(np.abs(knock)) + 1e-9) * rng.uniform(0.1, 0.3), rng.uniform(0.08, 1.2))
    for _ in range(6):
        d = rng.uniform(0.05, 0.1)
        f0 = rng.uniform(250, 450)
        plop = synth.sine(synth.glide(f0, f0 * 1.9, d, 0.6), d) * synth.envelope(d, 0.004, 0)
        synth.place(out, plop * rng.uniform(0.1, 0.25), rng.uniform(0.4, 1.6))
    hall = synth.reverb_ir(1.5, 1.2, rng, brightness=0.3)[0]
    wet = synth.fft_convolve(out, hall)[:len(out)]
    return out * 0.85 + wet / (np.max(np.abs(wet)) + 1e-9) * 0.25
