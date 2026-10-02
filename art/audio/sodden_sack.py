"""A Sodden Sack thrown over yourself: a heavy wet slap of sacking, then a long hiss of steam as the flames die."""
VARIANTS = 1


def render(v, rng):
    duration = 1.4
    out = np.zeros(int(duration * synth.SR))
    t = synth.seconds(0.25)
    slap = synth.lowpass(synth.white(0.25, rng), 1500) * np.exp(-t / 0.035)
    slap += 0.5 * synth.lowpass(synth.brown(0.25, rng), 300) * np.exp(-t / 0.06)
    synth.place(out, slap / (np.max(np.abs(slap)) + 1e-9) * 0.8, 0.0)
    hiss = synth.highpass(synth.white(1.2, rng), 3000) * synth.envelope(1.2, 0.05, 0, curve=2.0)
    synth.place(out, hiss / (np.max(np.abs(hiss)) + 1e-9) * 0.35, 0.08)
    drips = np.zeros(int(1.2 * synth.SR))
    for _ in range(6):
        at = rng.uniform(0.2, 1.0)
        d = np.sin(2 * np.pi * synth.glide(rng.uniform(900, 1400), 500, 0.05) * synth.seconds(0.05)) * synth.decay(0.05, 0.015)
        synth.place(drips, d, at)
    synth.place(out, drips * 0.25, 0.1)
    return out
