"""The Hold, That Would Not Let Go, waking: the chamber's wall becoming a face and its hands tearing out, 4.8 s.
A deep grinding swell of stone (the whole wall shifting, slow at first, quickening: a rough granular drag, the rock's
stick-slip catches deep in it, a rumble that grows), a vast breath drawn in through the stone with it (the Hold's own
breath, huge and close, the cracks whistling at its height) and under it the souls of the wall waking into one low
groan; then the hands tear out, left and right a moment apart: stone cracking open (a sharp split, a heavy knock,
a crackle of breaking running on), the black strands that held them stretching and snapping (the Kept's tar, much
bigger), rubble pouring off and bouncing on the floor; the round chamber ringing with it.
Mono (the Boss's own place). One variant: it is heard once.

This file is the Boss's sound kit: the other the_hold_* sounds and music_the_hold load it (stone grinding, tearing,
impacts, rubble, the souls' voice, the gust, the chamber). It builds on the Hold's own sounds: hold_breath_in (the
breath), kept_emerge (the tar's stretch and snap), hold_stone_press (stone giving like clay), hold_creak (settling).
`synth` and `np` are injected by tools/audio/build_sound.py."""
VARIANTS = 1
PEAK = 0.89

SR = synth.SR


# ---------------------------------------------------------------- helpers

def norm(x):
    return x / (np.max(np.abs(x)) + 1e-9)


def load(name):
    """Another script in this folder, as a module."""
    import importlib.util
    import os
    spec = importlib.util.spec_from_file_location(name, os.path.join(os.path.dirname(__file__), name + ".py"))
    module = importlib.util.module_from_spec(spec)
    module.synth, module.np = synth, np
    spec.loader.exec_module(module)
    return module


def curve(d, points, smooth=0.0):
    """A per-sample value through (time, value) points, linear between them; `smooth` seconds of rounding."""
    n = int(d * SR)
    ts, vs = zip(*points)
    x = np.interp(np.arange(n) / SR, ts, vs)
    if smooth:
        k = max(1, int(smooth * SR))
        w = np.hanning(2 * k + 1)
        x = synth.fft_convolve(np.pad(x, k, mode="edge"), w / w.sum())[2 * k:2 * k + n]
    return x


def fshape(x, gain, tail=0.5):
    """Filter by a gain curve over frequency (gain(freqs) -> gains), with room for the filter to ring out."""
    n = len(x)
    m = n + int(tail * SR)
    size = 1 << (m - 1).bit_length()
    freqs = np.fft.rfftfreq(size, 1 / SR)
    return np.fft.irfft(np.fft.rfft(x, size) * gain(freqs), size)[:n]


def band(f, centre, width):
    """A broad resonance: a bell over log-frequency, `width` in octaves-ish."""
    return np.exp(-0.5 * (np.log(np.maximum(f, 1) / centre) / width) ** 2)


def lows(f, cutoff, order=2):
    return 1 / (1 + (f / cutoff) ** (2 * order))


def highs(f, cutoff, order=1):
    r = (f / cutoff) ** (2 * order)
    return r / (1 + r)


def saturate(x, drive=2.0):
    """Round off the peaks (the boom's first swing) so the weight carries: a soft tanh, which also gives the deepest
    tones overtones that small speakers can play."""
    x = norm(x)
    return np.tanh(drive * x) / np.tanh(drive)


def fade_edges(x, fin=0.003, fout=0.05):
    n = len(x)
    a, b = int(fin * SR), int(fout * SR)
    if a:
        x[:a] *= np.linspace(0, 1, a)
    if b:
        x[-b:] *= np.linspace(1, 0, b) ** 2
    return x


# ---------------------------------------------------------------- stone

def grind(d, speed, rng, colour=240, grit=0.5):
    """Great stone dragging on stone: a rough granular drag (dense grains, denser and brighter as it moves faster),
    the rock's stick-slip catches deep in it (dull rough knocks through broad low resonances, quickening with the
    speed), sharp grit at the top, a rumble of the mass under it. `speed` is per sample, 0..1."""
    n = int(d * SR)
    speed = np.asarray(speed)[:n]
    density = 120 + 3200 * speed
    grains = (rng.random(n) < density / SR) * rng.uniform(0.15, 1.0, n)
    k = int(0.005 * SR)
    grain = np.hanning(k) * rng.uniform(-1, 1, k)
    rough = synth.fft_convolve(grains, grain)[:n] + 0.25 * rng.uniform(-1, 1, n) * speed
    body = synth.bandpass(rough, colour * (0.75 + 0.7 * speed), 0.8)
    body = synth.lowpass(body, 2600)
    # stick-slip catches: the weight jumping on
    rate = (5 + 32 * speed) * (1 + 0.45 * synth.smooth_random(d, rng, 6.0, -1, 1)[:n])
    catches = np.diff(np.floor(np.cumsum(rate / SR)), prepend=0.0) * rng.uniform(0.3, 1.0, n)
    burst = synth.lowpass(synth.white(0.03, rng), 900) * np.hanning(int(0.03 * SR))
    knocks = synth.fft_convolve(catches, burst)[:n]
    knocks = fshape(knocks, lambda f: band(f, 50, 0.35) + 0.9 * band(f, 100, 0.3) + 0.5 * band(f, 200, 0.3)
                    + 0.25 * band(f, 410, 0.3))
    # grit: tiny hard ticks, only when it moves fast
    gd = (rng.random(n) < (20 + 500 * speed ** 2) / SR) * rng.uniform(0.2, 1.0, n)
    gk = int(0.0015 * SR)
    grit_ = fshape(synth.fft_convolve(gd, np.hanning(gk) * rng.uniform(-1, 1, gk))[:n],
                   lambda f: band(f, 3200, 0.5))
    rumble = fshape(rng.normal(0, 1, n), lambda f: lows(f, 70, 2) * highs(f, 22, 1))
    return (norm(body) * speed + 0.75 * norm(knocks) * speed ** 0.8 + grit * 0.35 * norm(grit_)
            + 0.8 * norm(rumble) * speed ** 1.2)


def crack(rng, size=1.0, d=0.6):
    """Stone splitting: a sharp, dry split (a hard broadband snap, darker for bigger), a heavy low knock, and a
    short crackle of the break running on. size 1 is a hand tearing out; smaller is a chip."""
    t = synth.seconds(d)
    n = len(t)
    snap = fshape(rng.uniform(-1, 1, n), lambda f: lows(f, 5000 / size ** 0.5, 1) * highs(f, 150, 1)) \
        * np.exp(-t / (0.004 * size ** 0.5))
    f = synth.glide(150 / size ** 0.4, 55 / size ** 0.2, d, 0.3)
    knock = synth.sine(f, d) * np.exp(-t / (0.05 * size ** 0.5))
    knock += fshape(rng.uniform(-1, 1, n), lambda f: band(f, 120, 0.5)) * np.exp(-t / 0.03) * 0.6
    run = np.zeros(n)
    for _ in range(int(14 * size) + 3):
        at = rng.random() ** 1.8 * min(0.35 * size ** 0.5, d - 0.05)
        cd = 0.03
        c = synth.white(cd, rng) * synth.decay(cd, rng.uniform(0.0015, 0.004))
        synth.place(run, c * rng.uniform(0.2, 1.0) * (1 - at / d), at)
    run = fshape(run, lambda f: lows(f, 4500, 1) * highs(f, 300, 1))
    out = norm(snap) + 0.9 * norm(knock) + 0.45 * norm(run)
    return out * np.clip(t / 0.0004, 0, 1)


def rubble(d, rng, count, start=0.0, length=1.0, size=1.0, pour=0.0):
    """Stones and grit falling off and bouncing on the floor: stones (a dull knock with a stony click), pebbles
    (a small hard tock, bouncing two or three times, quicker each time), grit ticks; times crowded early.
    `pour` adds a hiss of sand pouring with them."""
    n = int(d * SR)
    out = np.zeros(n)
    for _ in range(count):
        at = start + rng.random() ** 1.7 * length
        kind = rng.random()
        if kind < 0.25 * size:  # a stone
            sd = 0.12
            st = synth.seconds(sd)
            f = rng.uniform(140, 260)
            s = synth.sine(f * (1 + 0.3 * np.exp(-st / 0.005)), sd) * np.exp(-st / 0.022)
            s += synth.white(sd, rng) * np.exp(-st / 0.003) * 0.5
            hits, gain = [at], rng.uniform(0.5, 1.0)
        elif kind < 0.65:  # a pebble, bouncing
            sd = 0.05
            st = synth.seconds(sd)
            f = rng.uniform(500, 1400)
            s = np.sin(2 * np.pi * f * st) * np.exp(-st / rng.uniform(0.006, 0.012)) + synth.white(sd, rng) * np.exp(-st / 0.0015) * 0.6
            gap = rng.uniform(0.07, 0.16)
            hits, gain = [], rng.uniform(0.2, 0.6)
            a = at
            for _ in range(rng.integers(1, 4)):
                hits.append(a)
                a += gap
                gap *= 0.6
        else:  # grit
            sd = 0.012
            s = synth.white(sd, rng) * synth.decay(sd, 0.0025)
            hits, gain = [at], rng.uniform(0.1, 0.35)
        for k, h in enumerate(hits):
            if h < d:
                synth.place(out, s * gain * 0.55 ** k, h)
    out = fshape(out, lambda f: lows(f, 6000, 1) * highs(f, 90, 1))
    if pour:
        t = synth.seconds(d)
        env = np.clip((t - start) / 0.05, 0, 1) * np.exp(-np.maximum(t - start, 0) / (length * 0.5))
        sand = fshape(rng.uniform(-1, 1, n), lambda f: band(f, 2200, 0.6) + 0.5 * band(f, 700, 0.5)) * env
        out = norm(out) + pour * norm(sand)
    return out


def impact(rng, low=34, d=1.2, weight=1.0, slap=0.6):
    """A colossal stone mass coming down flat on stone: a very deep thud bending down (felt), the body of it (dark
    rumbling noise), a short hard crack on contact, a broad flat slap (a palm, not a fist)."""
    t = synth.seconds(d)
    n = len(t)
    f = low * (1 + 1.6 * np.exp(-t / 0.04))
    boom = synth.sine(f, d) * np.exp(-t / (0.28 * weight)) + 0.35 * synth.sine(2 * f, d, 0.7) * np.exp(-t / 0.12)
    body = fshape(rng.normal(0, 1, n), lambda f: lows(f, 180, 2) * highs(f, 25, 1)) * np.exp(-t / (0.12 * weight))
    hit = fshape(rng.uniform(-1, 1, n), lambda f: lows(f, 3500, 1) * highs(f, 200, 1)) * np.exp(-t / 0.006)
    flat = fshape(rng.uniform(-1, 1, n), lambda f: band(f, 520, 0.6)) * np.exp(-t / 0.018)
    out = norm(boom) + 0.7 * norm(body) + 0.5 * norm(hit) + slap * norm(flat)
    return out * np.clip(t / 0.0008, 0, 1)


def modes(d, rng, scale=1.0, tau=0.7):
    """The round chamber ringing a moment after a blow: its lowest resonances (a hollow, inharmonic stone boom,
    not a bell), dying fast."""
    t = synth.seconds(d)
    out = np.zeros_like(t)
    for ratio, a, ta in ((1.0, 1.0, 1.0), (1.47, 0.7, 0.8), (2.09, 0.45, 0.6), (2.83, 0.3, 0.45), (3.71, 0.15, 0.3)):
        fr = 46 * scale * ratio * rng.uniform(0.98, 1.02)
        out += a * np.sin(2 * np.pi * fr * t + rng.uniform(0, 6)) * np.exp(-t / (tau * ta)) \
            * (1 + 0.2 * np.sin(2 * np.pi * rng.uniform(1, 3) * t))
    return out * np.clip(t / 0.03, 0, 1)


# ---------------------------------------------------------------- the souls' voice

def souls(d, rng, f0, count=6, vowel="oh", rasp=0.45, breath=0.35, spread=0.03, max_freq=2400, dark=1600):
    """The wall's voice: many souls as one, deep, through stone. `count` low wordless voices on f0 (a number or per
    sample), each a little off the others in pitch and time, roughened by an irregular flutter (throats full of
    stone), with breath through the same vowel; an octave below, a faint sub. Dulled: it comes through rock."""
    n = int(d * SR)
    base = np.broadcast_to(np.asarray(f0, dtype=float), (n,)).copy()
    out = np.zeros(n)
    for i in range(count):
        detune = 1 + rng.uniform(-spread, spread)
        drift = 1 + 0.012 * synth.smooth_random(d, rng, 1.5, -1, 1)[:n]
        v = synth.voice(base * detune * drift, d, rng, vowel, vibrato=0.01, max_freq=max_freq)
        flutter = synth.smooth_random(d, rng, rng.uniform(35, 60), 0.0, 1.0)[:n]
        v *= (1 - rasp) + rasp * 2.4 * flutter ** 2
        out += norm(v) * rng.uniform(0.6, 1.0)
    freqs = np.fft.rfftfreq(n, 1 / SR)
    air = np.fft.irfft(np.fft.rfft(rng.uniform(-1, 1, n)) * synth.formants(freqs, vowel), n)
    sub = synth.sine(base * 0.5, d) * 0.5
    out = norm(out) + breath * norm(air) + 0.35 * sub
    return fshape(out, lambda f: lows(f, dark, 1) * highs(f, 25, 1))


# ---------------------------------------------------------------- breath

def gust_air(d, flow, rng, inward=False, size=1.0):
    """The cave's breath, huge and close: a broad roar of air through stone, its centre rising with the flow (a
    hiss at the top of it), darker for a bigger breath."""
    def mag(times, freqs):
        fl = np.interp(times * SR, np.arange(len(flow)), flow)[:, None]
        f = np.maximum(freqs[None, :], 1)
        centre = (260 + (900 if inward else 1100) * fl) / size
        body = np.exp(-0.5 * (np.log(f / centre) / 1.1) ** 2)
        hiss = 0.18 * fl ** 2 * np.exp(-0.5 * (np.log(f / (2800 if inward else 2200)) / 0.55) ** 2)
        return (fl ** 1.1 * body + hiss) * (f / 40) ** 2 / (1 + (f / 40) ** 2) / (1 + (f / 6000) ** 4)
    return synth.spectral_noise(d, rng, mag, n_fft=2048, hop=256)


def sand(d, flow, rng, density=2500):
    """Grit carried in the wind: countless tiny grains ticking off stone, thick where the gust is strongest."""
    n = int(d * SR)
    fl = np.asarray(flow)[:n]
    grains = (rng.random(n) < density * fl ** 1.5 / SR) * rng.uniform(0.1, 1.0, n)
    k = int(0.0012 * SR)
    x = synth.fft_convolve(grains, np.hanning(k) * rng.uniform(-1, 1, k))[:n]
    return fshape(x, lambda f: band(f, 3000, 0.55) + 0.4 * band(f, 1200, 0.4))


# ---------------------------------------------------------------- the round chamber

def chamber(x, rng, length=2.5, wet=0.4, tail=True):
    """The round stone chamber at the way out: early reflections off its curved wall, a long dark stone tail.
    With `tail`, the sound is lengthened so the tail rings out instead of being cut."""
    ir = synth.reverb_ir(length, length * 0.85, rng, predelay=0.018, brightness=0.2)[0]
    early = np.zeros(int(0.12 * SR))
    for _ in range(18):
        at = 0.012 + rng.random() ** 1.3 * 0.1
        early[int(at * SR)] += rng.uniform(-1, 1) * np.exp(-at / 0.05) * 0.35
    full = np.zeros(max(len(ir), len(early)))
    full[:len(ir)] += ir
    full[:len(early)] += synth.lowpass(early, 3000)
    if tail:
        x = np.concatenate([x, np.zeros(int(length * SR))])
    reverberant = synth.fft_convolve(x, full)[:len(x)]
    reverberant = fshape(reverberant, lambda f: (1 + 0.35 * band(f, 130, 0.6)) * lows(f, 3200, 1))
    return x + norm(reverberant) * np.max(np.abs(x)) * wet


def trim(x, threshold_db=-60, release=0.3):
    """Cut the silence the tail left at the end, easing the last moment out."""
    level_ = np.abs(x) / (np.max(np.abs(x)) + 1e-9)
    above = np.nonzero(level_ > 10 ** (threshold_db / 20))[0]
    end = min(len(x), (above[-1] if len(above) else len(x)) + int(0.05 * SR))
    x = x[:end].copy()
    k = min(int(release * SR), len(x))
    x[-k:] *= np.linspace(1, 0, k) ** 2
    return x


# ---------------------------------------------------------------- tar strands

def strands(rng, count=3, spread=0.25, size=0.6):
    """The black strands that held a hand in the wall stretching and snapping, one after another (the Kept's tar,
    much bigger: lower, heavier)."""
    kept = load("kept_emerge")
    d = spread + 0.4
    out = np.zeros(int(d * SR))
    for i in range(count):
        at = (i / max(1, count - 1)) * spread * rng.uniform(0.8, 1.0)
        synth.place(out, norm(kept.snap(rng, size * rng.uniform(0.85, 1.15))) * rng.uniform(0.5, 1.0), at)
    return out


# ---------------------------------------------------------------- the waking

def render(v, rng):
    d = 4.8
    n = int(d * SR)
    t = synth.seconds(d)
    out = np.zeros(n)
    tear_l, tear_r = 3.0, 3.38
    # the wall shifting: a grinding swell, slow at first, quickening to the tearing
    # (a moment's strain just before: the swell held, tight, then it gives)
    speed = curve(d, [(0, 0.0), (0.4, 0.15), (1.4, 0.35), (2.4, 0.75), (2.7, 0.55), (2.95, 0.3), (3.05, 1.0),
                      (3.6, 0.7), (4.3, 0.12), (4.8, 0.0)], 0.05)
    out += norm(grind(d, speed, rng, 190)) * 0.7
    # the breath drawn in through the stone, peaking just before the hands come
    breath_kit = load("hold_breath_in")
    flow = np.zeros(n)
    bd = 2.6
    synth.place(flow, breath_kit.flow_curve(bd, 0.78, 1.4), 0.45)
    air = gust_air(d, flow, rng, inward=True, size=1.4)
    sing = breath_kit.cracks(d, flow, True, rng, 30)
    out += norm(air) * 0.5 + norm(sing) * 0.05
    # the souls of the wall waking into one low groan, rising under it
    f0 = curve(d, [(0, 38), (2.9, 52), (3.6, 49), (4.8, 44)], 0.2)
    groan_env = curve(d, [(0, 0), (1.0, 0.25), (2.9, 0.8), (3.5, 0.6), (4.6, 0.0), (4.8, 0)], 0.15)
    out += norm(souls(d, rng, f0, 7, "oh", rasp=0.5, breath=0.3, dark=900)) * groan_env * 0.45
    # the hands tearing out, left then right
    for at, size in ((tear_l, 1.0), (tear_r, 0.9)):
        synth.place(out, norm(crack(rng, size, 0.8)) * 1.3, at)
        synth.place(out, norm(strands(rng, 4, 0.3, 0.55)) * 0.45, at + 0.04)
        synth.place(out, norm(impact(rng, 40, 0.8, 0.6, 0.2)) * 0.8, at)
    out += norm(rubble(d, rng, 55, tear_l + 0.05, 1.3, 1.0, pour=0.35)) * 0.4
    synth.place(out, norm(modes(1.6, rng, 1.0, 0.7)) * 0.25, tear_l + 0.02)
    out = synth.highpass(out, 20)
    out = fade_edges(out, 0.25, 0.02)
    out = chamber(out, rng, 2.4, 0.4)
    return trim(out, -55, 0.4)
