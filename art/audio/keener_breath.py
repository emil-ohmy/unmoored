"""A Keener drawing breath (the knell's tell, 1.2 s): a long ragged inhale through crape that catches twice in a sob,
and under it a low bell's hum swelling as if the stone itself drew breath with it (E2, the Hall's key), cut off at
the top."""
VARIANTS = 2


def render(v, rng):
    duration = 1.25
    out = np.zeros(int(duration * synth.SR))
    # The inhale: breath through cloth, rising, broken twice (the sob's catches).
    d = 1.15
    t = synth.seconds(d)
    inhale = synth.bandpass(synth.white(d, rng), synth.glide(450, 1500, d, 1.3), 1.1)
    gate = np.ones_like(t)
    for at in (0.32 + 0.05 * v, 0.62 + 0.04 * v):
        gate *= 1 - 0.85 * np.exp(-0.5 * ((t - at) / 0.03) ** 2)
    inhale *= synth.envelope(d, 0.9, 0, curve=3.0, peak_at=0.95) * gate * np.clip((d - t) / 0.04, 0, 1)
    synth.place(out, inhale / (np.max(np.abs(inhale)) + 1e-9) * 0.55, 0.0)
    # Two little voiced catches, a breath's sob.
    for at in (0.32 + 0.05 * v, 0.62 + 0.04 * v):
        sob = synth.voice(synth.glide(330, 290, 0.07), 0.07, rng, vowel="ah") * synth.envelope(0.07, 0.01, 0.05)
        synth.place(out, sob / (np.max(np.abs(sob)) + 1e-9) * 0.2, at - 0.02)
    # The hum beneath: a bell's low partials swelling to the knell.
    t = synth.seconds(d)
    f = synth.note("E2")
    hum = sum(a * np.sin(2 * np.pi * f * r * t + rng.uniform(0, 6)) for r, a in ((1.0, 1.0), (2.0, 0.4), (2.4, 0.2), (3.0, 0.15)))
    hum *= (t / d) ** 2.5 * np.clip((d - t) / 0.04, 0, 1)  # (cut off at the top, not clicked off)
    synth.place(out, hum / (np.max(np.abs(hum)) + 1e-9) * 0.4, 0.05)
    fade = int(0.03 * synth.SR)
    out[-fade:] *= np.linspace(1, 0, fade)
    return out
