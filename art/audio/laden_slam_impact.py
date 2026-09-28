"""The heap crashes down on the stones: a low boom, the crack of the blow, splintering wood and debris skittering
away, in a small stone reverb. Plays when the slam lands, hit or miss: a miss should still feel like it nearly had you."""
VARIANTS = 2


def render(v, rng):
    duration = 1.3
    out = np.zeros(int(duration * synth.SR))
    boom = synth.sine(synth.glide(78, 36, 0.6, 0.5), 0.6) * synth.decay(0.6, 0.2)
    crack = synth.highpass(synth.white(0.03, rng), 1500) * synth.decay(0.03, 0.008)
    crunch = synth.bandpass(synth.white(0.2, rng), 620, 2.0) * synth.decay(0.2, 0.05)
    stone = synth.bandpass(synth.white(0.25, rng), 190, 1.0) * synth.decay(0.25, 0.08)
    for part, gain in ((boom, 1.0), (crack, 0.5), (crunch, 0.55), (stone, 0.6)):
        synth.place(out, part / (np.max(np.abs(part)) + 1e-9) * gain, 0.0)
    for _ in range(12):  # debris skittering off
        d = rng.uniform(0.005, 0.015)
        click = synth.bandpass(synth.white(d, rng), rng.uniform(1200, 3500), 2.5) * synth.decay(d, d / 3)
        synth.place(out, click / (np.max(np.abs(click)) + 1e-9) * rng.uniform(0.05, 0.2), rng.uniform(0.05, 0.6))
    room = synth.reverb_ir(1.0, 0.9, rng, brightness=0.3)[0]
    wet = synth.fft_convolve(out, room)[:len(out)]
    return out + wet / (np.max(np.abs(wet)) + 1e-9) * 0.25
