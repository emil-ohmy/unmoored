"""A dock's bell rung: a small bronze bell struck twice, bright and carrying over the water, its hum dying slowly
(inharmonic partials, as a bell's are)."""
VARIANTS = 1


def render(v, rng):
    duration = 3.2
    out = np.zeros(int(duration * synth.SR))
    f0 = synth.note("E5")
    for at, amp in ((0.0, 1.0), (0.42, 0.8)):
        t = synth.seconds(duration - at)
        tone = np.zeros_like(t)
        for ratio, a, tau in ((0.5, 0.35, 2.2), (1.0, 1.0, 1.6), (1.19, 0.5, 1.2), (1.5, 0.45, 0.9), (2.0, 0.3, 0.7), (2.66, 0.2, 0.4)):
            tone += a * np.sin(2 * np.pi * f0 * ratio * t + rng.uniform(0, 6)) * np.exp(-t / tau)
        strike = synth.highpass(synth.white(0.02, rng), 3000) * synth.decay(0.02, 0.004)
        tone[:len(strike)] += strike * 2.0
        tone *= np.clip(t / 0.003, 0, 1) * np.clip((duration - at - t) / 0.4, 0, 1)
        synth.place(out, tone * amp * 0.22, at)
    return out
