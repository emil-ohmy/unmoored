"""Torch fire, a seamless 4 s loop: a low, breathing flame bed and hiss, with random crackles and the odd pop."""
LOOP = True


def render(v, rng):
    duration = 4.6  # 0.6 s is crossfaded into the start
    t = synth.seconds(duration)
    breathe = 0.6 + 0.25 * np.sin(2 * np.pi * 0.7 * t) + 0.15 * np.sin(2 * np.pi * 1.9 * t + 1.0)
    bed = synth.lowpass(synth.brown(duration, rng), 700) * breathe * 0.7
    hiss = synth.highpass(synth.white(duration, rng), 3000) * breathe * 0.04
    crackles = np.zeros_like(t)
    for _ in range(rng.poisson(9 * duration)):
        length = rng.uniform(0.002, 0.012)
        size = rng.uniform(0.2, 1.0) ** 3  # mostly small crackles, now and then a pop
        burst = synth.bandpass(synth.white(length + 0.03, rng), rng.uniform(1500, 5000), 3) * synth.decay(length + 0.03, length / 3)
        synth.place(crackles, burst * size * 6.0, rng.uniform(0, duration - 0.05))
    return synth.loop_crossfade(bed + hiss + crackles, 0.6)
