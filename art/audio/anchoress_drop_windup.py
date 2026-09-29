"""The Anchoress raises the anchor for the Drop (1.1 s, the wind-up): its chain rattling faster and faster as it
climbs, the iron groaning under its own weight, and her strained, drowned groan, all swelling to the moment it
falls. Also the Anchoress's sound kit (links, iron, groan, scrape), used by her other sounds."""
TELEGRAPH = 1.1


def norm(x):
    return x / (np.max(np.abs(x)) + 1e-9)


def links(duration, rng, rate_from, rate_to, gain=lambda u: 1.0, low=1800, high=5200):
    """A chain: iron links knocking together, a train of small metallic clinks at a changing rate."""
    out = np.zeros(int(duration * synth.SR))
    t = 0.0
    while t < duration:
        u = t / duration
        rate = rate_from + (rate_to - rate_from) * u
        d = rng.uniform(0.006, 0.016)
        f = rng.uniform(low, high)
        clink = synth.sine(f, d, rng.uniform(0, 6)) * synth.decay(d, d / 3) + 0.4 * synth.bandpass(synth.white(d, rng), f * 0.6, 4.0) * synth.decay(d, d / 5)
        synth.place(out, clink * rng.uniform(0.4, 1.0) * gain(u), t)
        t += (1.0 / max(rate, 1.0)) * rng.uniform(0.6, 1.4)
    return out


def iron(duration, rng, f0, tau=0.5):
    """Struck heavy iron: a few inharmonic partials, the low ones ringing longest (not a bell: short, dull, clanky)."""
    out = np.zeros(int(duration * synth.SR))
    for ratio, amp, decay in ((1.0, 1.0, 1.0), (2.41, 0.6, 0.55), (3.89, 0.4, 0.35), (5.63, 0.25, 0.22), (7.9, 0.15, 0.15)):
        out += amp * synth.sine(f0 * ratio * rng.uniform(0.99, 1.01), duration, rng.uniform(0, 6)) * synth.decay(duration, tau * decay)
    knock = synth.lowpass(synth.white(0.03, rng), 1800) * synth.decay(0.03, 0.006)
    synth.place(out, norm(knock) * 0.8, 0.0)
    return out


def groan(duration, rng, f_from, f_to, vowel="oh"):
    """Her voice: low, strained, as if heard under water."""
    v = synth.voice(synth.glide(f_from, f_to, duration, 1.2), duration, rng, vowel, vibrato=0.03, max_freq=1400)
    return synth.lowpass(norm(v), 700)


def scrape(duration, rng, centre=700):
    """Iron dragged over stone: a grinding, juddering noise."""
    judder = 0.5 + 0.5 * np.abs(np.sin(2 * np.pi * np.cumsum(synth.glide(9, 16, duration)) / synth.SR))
    grind = synth.bandpass(synth.white(duration, rng), centre, 1.4) * judder
    return norm(grind) + 0.5 * norm(synth.lowpass(synth.brown(duration, rng), 220))


def render(v, rng):
    duration = TELEGRAPH + 0.15
    t = synth.seconds(duration)
    swell = np.clip(t / TELEGRAPH, 0, 1) ** 1.5 * np.clip((duration - t) / 0.15, 0, 1)
    chain = links(duration, rng, 8, 60, lambda u: 0.4 + 0.6 * u)
    strain = synth.sine(synth.glide(48, 62, duration, 1.3), duration) * 0.6 + synth.lowpass(synth.brown(duration, rng), 150)
    out = norm(chain) * 0.55 + norm(strain) * 0.45 + groan(duration, rng, 73, 92) * 0.45
    return out * swell
