"""A Shade withdrawing from your light: what you hear as a pair of eyes in the dark draws back. A breathy, wordless
murmur (breath shaped into vowels that never make words, syllables that never make sense), doubled a moment apart by
a second voice a little higher, a little wrong; it recedes as it goes (quieter, duller, more of the room and less
of it). Barely there. 1-2 s.
Variants: 1 two voices murmuring away, 2 a quick intake of breath first, then the murmur, 3 three voices and the
ghost of a low hum under them, 4 short: hardly more than a sigh trailing off.
`synth` and `np` are injected by tools/audio/build_sound.py."""
VARIANTS = 4
PEAK = 0.5  # quiet in itself: the Shades are barely there

SR = synth.SR
VOWELS = [(300, 870, 2240), (500, 850, 2400), (730, 1100, 2450), (530, 1840, 2480), (300, 2300, 3000), (640, 1190, 2390)]
# (length, voices, intake first, hum under)
SHAPES = [(1.4, 2, False, False), (1.6, 2, True, False), (1.7, 3, False, True), (1.0, 2, False, False)]


def contour(d, rng, values, rates):
    """A per-sample path through `values`, one per syllable (lengths from `rates`), eased between them."""
    n = int(d * SR)
    out = np.empty(n)
    i, k = 0, 0
    while i < n:
        m = min(n - i, int(rates[k % len(rates)] * SR))
        a, b = values[k % len(values)], values[(k + 1) % len(values)]
        u = (1 - np.cos(np.linspace(0, np.pi, m))) / 2
        out[i:i + m] = a + (b - a) * u
        i += m
        k += 1
    return out


def murmur(d, rng, scale):
    """One whispering voice: breath through three moving formants, a syllable's swell and dip, a lisp of 's' now
    and then."""
    syllables = int(d / 0.12) + 2
    rates = rng.uniform(0.11, 0.22, syllables)
    targets = [VOWELS[i] for i in rng.integers(0, len(VOWELS), syllables + 1)]
    breath = synth.white(d, rng)
    out = np.zeros(int(d * SR))
    for j, gain in enumerate((1.0, 0.55, 0.22)):
        centre = contour(d, rng, [f[j] * scale for f in targets], rates)
        out += synth.bandpass(breath, centre, 5.0 + 2 * j) * gain
    # the syllables: a swell on each vowel, a dip between
    pulse = contour(d, rng, list(rng.uniform(0.25, 1.0, syllables + 1) * np.tile([1.0, 0.35], syllables)[:syllables + 1]), rates)
    out *= pulse
    for _ in range(rng.integers(0, 3)):
        sd = rng.uniform(0.12, 0.2)
        s = synth.bandpass(synth.white(sd, rng), rng.uniform(5000, 6500), 2.0) * np.sin(np.linspace(0, np.pi, int(sd * SR))) ** 2
        synth.place(out, s * 0.3, rng.uniform(0.1, d * 0.6))
    t = synth.seconds(d)
    return out * np.sin(np.clip(t / 0.09, 0, 1) * np.pi / 2) ** 2  # breathed in, not switched on


def intake(rng):
    """A quick soft breath drawn in (startled), rising."""
    d = 0.22
    t = synth.seconds(d)
    air = synth.bandpass(synth.white(d, rng), synth.glide(900, 1700, d), 1.5)
    return air * (t / d) ** 2 * np.clip((d - t) / 0.06, 0, 1)


def render(v, rng):
    d, voices, gasp, hum = SHAPES[v]
    total = d + 0.35
    n = int(total * SR)
    t = synth.seconds(total)
    dry = np.zeros(n)
    start = 0.0
    if gasp:
        synth.place(dry, intake(rng) * 0.5, 0.0)
        start = 0.2
    for k in range(voices):
        scale = [1.0, 1.17, 0.9][k]  # the second a little higher and smaller: not quite the same voice
        m = murmur(d - start, rng, scale * rng.uniform(0.97, 1.03))
        synth.place(dry, m / (np.max(np.abs(m)) + 1e-9) * [1.0, 0.7, 0.5][k], start + k * rng.uniform(0.03, 0.08))
    if hum:
        hd = d - start
        h = synth.voice(synth.glide(105, 88, hd), hd, rng, "oo", vibrato=0.02, max_freq=900)
        synth.place(dry, h / (np.max(np.abs(h)) + 1e-9) * 0.12 * synth.envelope(hd, 0.2, 0, curve=1.5, peak_at=0.3), start)
    # receding: quieter, duller as it goes
    go = np.clip((t - start) / (d - start), 0, 1)
    away = (1 - go) ** 1.6
    dry = synth.lowpass(dry * away, 5000 - 3800 * go)
    dry = synth.highpass(dry, 180)
    # the dark taking it: the room's share grows as it goes
    ir = synth.reverb_ir(1.6, 1.3, rng, predelay=0.015, brightness=0.18)[0]
    wet = synth.fft_convolve(dry, ir)[:n]
    wet = wet / (np.max(np.abs(wet)) + 1e-9) * np.max(np.abs(dry))
    out = dry * 0.85 + wet * 0.45
    return out * np.clip((total - t) / 0.3, 0, 1)
