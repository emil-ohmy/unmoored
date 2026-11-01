"""The Widow Hale's veil cast out low along the floor (0.6 s, the blow landing ~0.27 s in): a long heavy whip of crape
(a sweep that swells to the strike and snaps there, the cloth fluttering hard as it straightens), a low airy whoosh under
it, and the lace hem hissing across the stone after, trailing away."""
VARIANTS = 3


def render(v, rng):
    duration = 0.72
    t = synth.seconds(duration)
    out = np.zeros_like(t)
    hit = 0.27 + (0.0, -0.01, 0.01)[v]
    # The sweep: band-passed noise whose centre rises to the strike and falls after.
    centre = np.where(t < hit, 500 + 1300 * (t / hit) ** 2, 1800 * np.exp(-(t - hit) / 0.18) + 400)
    sweep = synth.bandpass(synth.white(duration, rng), centre, 1.0)
    swell = np.where(t < hit, (t / hit) ** 2.5, np.exp(-(t - hit) / 0.09))
    out += sweep / (np.max(np.abs(sweep)) + 1e-9) * swell * 0.6
    # The low whoosh: air moved by a great weight of cloth.
    whoosh = synth.lowpass(synth.white(duration, rng), np.where(t < hit, 250 + 300 * t / hit, 550))
    lswell = np.where(t < hit, (t / hit) ** 1.8, np.exp(-(t - hit) / 0.14))
    out += whoosh / (np.max(np.abs(whoosh)) + 1e-9) * lswell * 0.55
    # The snap: the crape straightening at the strike, fluttering hard (a few quick flaps).
    d = 0.12
    tt = synth.seconds(d)
    flap = synth.bandpass(synth.white(d, rng), 1300, 0.8) * np.exp(-tt / 0.03)
    flap *= 0.4 + 0.6 * (0.5 + 0.5 * np.sin(2 * np.pi * rng.uniform(34, 44) * tt)) ** 2
    flap *= np.clip(tt / 0.002, 0, 1)
    synth.place(out, flap / (np.max(np.abs(flap)) + 1e-9) * 0.85, hit - 0.01)
    # The hem hissing across the stone: a high scrape with grit in it, from the strike, trailing.
    d = duration - hit + 0.05
    tt = synth.seconds(d)
    hiss = synth.highpass(synth.white(d, rng), 3500) * (0.7 + 0.3 * synth.smooth_random(d, rng, 25.0))
    grit = np.zeros_like(tt)
    hits = rng.random(len(tt)) < 500 / synth.SR
    grit[hits] = rng.uniform(-1, 1, hits.sum())
    hiss = hiss + 1.5 * synth.highpass(grit, 2500)
    henv = np.clip(tt / 0.06, 0, 1) * np.exp(-tt / 0.16)
    synth.place(out, hiss / (np.max(np.abs(hiss)) + 1e-9) * henv * 0.3, hit - 0.05)
    fade = int(0.08 * synth.SR)
    out[-fade:] *= np.linspace(1, 0, fade) ** 2
    return out
