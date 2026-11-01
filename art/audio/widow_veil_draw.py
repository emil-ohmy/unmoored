"""The Widow Hale gathering her veil (its tell, 0.9 s): heavy crape gathered up and drawn back, slowly: a silk-and-cloth
rustle (many small dry crackles of the weave over a soft swish) rising and quickening, a heavy fold of it shifting
low; and at her waist the chatelaine's stopped watches knocking together, a faint chime."""


def render(v, rng):
    duration = 0.95
    t = synth.seconds(duration)
    out = np.zeros_like(t)
    rise = synth.envelope(duration, 0.8, 0, curve=3.0, peak_at=0.84) * np.clip((duration - t) / 0.08, 0, 1)
    # The swish: cloth on cloth, its band rising as it's drawn faster.
    swish = synth.bandpass(synth.white(duration, rng), synth.glide(700, 2200, duration, 1.3), 1.1)
    swish *= 0.75 + 0.25 * synth.smooth_random(duration, rng, 6.0)
    out += swish / (np.max(np.abs(swish)) + 1e-9) * rise * 0.5
    # The weave crackling: tiny grains, denser as it goes.
    grains = np.zeros_like(t)
    density = 120 + 900 * (t / duration) ** 1.5
    hits = rng.random(len(t)) < density / synth.SR
    grains[hits] = rng.uniform(-1, 1, hits.sum()) * rng.uniform(0.2, 1.0, hits.sum())
    crackle = synth.bandpass(grains, 4200, 1.0) + 0.5 * synth.bandpass(grains, 2300, 1.5)
    out += crackle / (np.max(np.abs(crackle)) + 1e-9) * rise * 0.35
    # The heavy folds shifting: a low soft whump or two.
    for at, g in ((0.12, 0.5), (0.5, 0.8)):
        d = 0.3
        fold = synth.lowpass(synth.white(d, rng), 380) * synth.envelope(d, 0.08, 0, curve=2.5)
        synth.place(out, fold / (np.max(np.abs(fold)) + 1e-9) * 0.3 * g, at)
    # The chatelaine: small watches knocking, a faint chime (E and B, high and thin), late in the gather.
    for at, name, g in ((0.46, "B6", 0.6), (0.58, "E7", 0.45), (0.66, "G6", 0.5), (0.74, "B6", 0.35)):
        d = duration - at
        tt = synth.seconds(d)
        f = synth.note(name) * rng.uniform(0.995, 1.005)
        ping = (np.sin(2 * np.pi * f * tt) + 0.4 * np.sin(2 * np.pi * f * 2.76 * tt) * np.exp(-tt / 0.03)) * np.exp(-tt / 0.14)
        click = synth.bandpass(synth.white(d, rng), 5000, 2.0) * np.exp(-tt / 0.003) * 0.6
        synth.place(out, (ping + click) * np.clip(tt / 0.001, 0, 1) * 0.15 * g, at)
    fade = int(0.06 * synth.SR)
    out[-fade:] *= np.linspace(1, 0, fade)
    return out
