"""The Stroke fully drawn: the Drowned Moon in the blade answering, one clear cold ring (a struck glass, high A over E,
bright at the start) with a single drop falling into still water under it. Short and unmistakable: let go now."""
VARIANTS = 1


def render(v, rng):
    duration = 1.4
    out = np.zeros(int(duration * synth.SR))
    t = synth.seconds(0.05)
    strike = synth.highpass(synth.white(0.05, rng), 5000) * np.exp(-t / 0.006)
    synth.place(out, strike * 0.25, 0.0)
    ring = synth.glass(synth.note("A6"), duration, rng) * 0.32 + synth.glass(synth.note("E6"), duration, rng) * 0.2
    ring *= np.clip((duration - synth.seconds(duration)) / 0.4, 0, 1)
    synth.place(out, ring, 0.0)
    # The drop: a quick upward pitch blip (a plink) and its small splash.
    plink_len = 0.09
    f = synth.glide(900, 2400, plink_len, shape=0.6)
    plink = np.sin(2 * np.pi * np.cumsum(f) / synth.SR) * synth.decay(plink_len, 0.025)
    synth.place(out, plink * 0.35, 0.03)
    splash = synth.bandpass(synth.white(0.12, rng), 2500, 2.0) * synth.decay(0.12, 0.02)
    splash *= np.clip((0.12 - synth.seconds(0.12)) / 0.04, 0, 1)  # (no click at its end)
    synth.place(out, splash / (np.max(np.abs(splash)) + 1e-9) * 0.12, 0.035)
    return out
