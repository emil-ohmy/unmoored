"""Still water at the Hub's jetty, close: barely moving, lapping softly against the wooden piles and the shingle of the
shore. Not waves: a slow, uneven breath of the surface (a low soft wash rising and settling), and on it little sounds
now and then: a lap against a pile (a soft wet slap, the hollow knock of the timber under it, sometimes a deep little
"glunk" where the water fills a hollow between piles), a gurgle of a few bubbles under the planks, the shingle
drawing back (a fine trickling hiss of wet pebbles, a few ticking over each other), a drip off a beam into the water.
Long quiet stretches between. A 24 s seamless mono loop, for a sound source at the water's edge.
Built circularly over the loop (noise shaped by FFT over exactly one loop, events wrapped round its end): nothing is
crossfaded, the seam is exact. `synth` and `np` are injected by tools/audio/build_sound.py."""
LOOP = True
PEAK = 0.7

SR = synth.SR
LENGTH = 24.0
N = int(LENGTH * SR)
# The surface's slow rise and settle: (time of the lift, piles it laps: (offset, gain, kind), shingle after?, gurgle?)
# Irregular on purpose; kinds: "slap" (a soft slap on a pile), "glunk" (a hollow filling), "plip" (a small one).
SWELLS = [
    (0.9, [(0.0, 1.0, "slap"), (0.55, 0.5, "plip")], True, False),
    (6.6, [(0.0, 0.7, "glunk"), (0.4, 0.45, "slap")], False, True),
    (11.4, [(0.0, 0.55, "plip")], True, False),
    (15.2, [(0.0, 0.9, "slap"), (0.3, 0.6, "glunk"), (1.05, 0.35, "plip")], False, True),
    (20.3, [(0.0, 0.6, "slap")], True, False),
]
DRIPS = [4.1, 9.8, 13.3, 18.0, 22.6]


def rms(x):
    return np.sqrt(np.mean(x ** 2)) + 1e-12


def level(x, db):
    return x / rms(x) * 10 ** (db / 20)


def norm(x):
    return x / (np.max(np.abs(x)) + 1e-9)


def shape(x, gain):
    freqs = np.fft.rfftfreq(len(x), 1 / SR)
    return np.fft.irfft(np.fft.rfft(x) * gain(freqs), len(x))


def bump(f, centre, width, gain=1.0):
    return gain * np.exp(-0.5 * (np.log(np.maximum(f, 1) / centre) / width) ** 2)


def band(x, centre, width):
    return shape(x, lambda f: bump(f, centre, width))


def slow(rng, rate, low=0.0, high=1.0):
    x = shape(rng.normal(0, 1, N), lambda f: np.exp(-(f / rate) ** 2))
    x = (x - x.min()) / (x.max() - x.min() + 1e-12)
    return low + (high - low) * x


def put(out, tone, at, gain=1.0):
    s = int(at * SR) % N
    k = 0
    while k < len(tone):
        m = min(len(tone) - k, N - s)
        out[s:s + m] += tone[k:k + m] * gain
        k += m
        s = 0


def ease(x, seconds):
    """Ease out the last moment so nothing stops mid-ring."""
    n = int(seconds * SR)
    x[-n:] *= np.cos(np.linspace(0, np.pi / 2, n)) ** 2
    return x


def onset(d, seconds):
    return np.sin(np.clip(synth.seconds(d) / seconds, 0, 1) * np.pi / 2) ** 2


def bubble(f, d, rng):
    """One small bubble (a Minnaert ring): a sine rising in pitch as it closes, dying fast."""
    t = synth.seconds(d)
    sweep = f * (1 + rng.uniform(0.3, 0.8) * np.clip(t / d, 0, 1))
    return np.sin(2 * np.pi * np.cumsum(sweep) / SR) * np.exp(-t / (d / 3.5)) * np.sin(np.clip(t / 0.003, 0, 1) * np.pi / 2) ** 2


# ---------------------------------------------------------------- the little sounds

def lap(rng, kind):
    d = 0.6
    t = synth.seconds(d)
    out = np.zeros_like(t)
    if kind in ("slap", "plip"):
        # the slap: a soft wet splat of water meeting wood (no hard edge: still water)
        big = kind == "slap"
        splat = band(rng.normal(0, 1, len(t)), rng.uniform(700, 1100) if big else rng.uniform(1200, 1800), 0.55)
        splat *= np.exp(-t / (0.035 if big else 0.018)) * np.sin(np.clip(t / 0.004, 0, 1) * np.pi / 2) ** 2
        out += norm(splat) * (0.8 if big else 0.6)
        if big:  # the timber under it: a short hollow knock
            f = rng.uniform(170, 240)
            out += 0.35 * np.sin(2 * np.pi * f * t) * np.exp(-t / 0.035) * np.clip(t / 0.003, 0, 1)
        for _ in range(rng.integers(1, 3)):  # a bubble or two caught by it
            b = bubble(rng.uniform(500, 1100), rng.uniform(0.03, 0.06), rng)
            synth.place(out, b * rng.uniform(0.15, 0.3), rng.uniform(0.01, 0.06))
    else:  # glunk: water filling a hollow between the piles, a deep little cavity note rising, then settling
        f = rng.uniform(180, 260)
        cav = bubble(f, 0.16, rng) * 0.9
        out[:len(cav)] += cav
        wash = band(rng.normal(0, 1, len(t)), 500, 0.5) * np.exp(-t / 0.06) * np.clip(t / 0.01, 0, 1)
        out += 0.35 * norm(wash)
        b = bubble(f * rng.uniform(2.1, 2.6), 0.05, rng)
        synth.place(out, b * 0.25, rng.uniform(0.09, 0.14))
    return ease(out, 0.1)


def gurgle(rng):
    """Under the planks: a few bubbles in a row, close together, muffled by the boards above them."""
    d = 0.8
    out = np.zeros(int(d * SR))
    at = 0.0
    f = rng.uniform(380, 600)
    for _ in range(rng.integers(3, 6)):
        bd = rng.uniform(0.03, 0.07)
        synth.place(out, bubble(f * rng.uniform(0.85, 1.3), bd, rng) * rng.uniform(0.5, 1.0), at)
        at += rng.uniform(0.05, 0.13)
    out = shape(out, lambda fr: 1 / (1 + (fr / 1400) ** 2))
    return ease(out, 0.1)


def shingle(rng):
    """The water drawing back off the shore: a fine trickling hiss through wet pebbles, swelling and thinning, and a
    few pebbles ticking over one another in it."""
    d = 1.8
    t = synth.seconds(d)
    env = np.sin(np.pi * np.clip(t / d, 0, 1)) ** 2 * np.exp(-t / 1.2)
    grit = np.zeros_like(t)
    count = int(rng.integers(140, 220))
    for at in np.sort(rng.random(count) ** 0.8 * d * 0.9):  # grains: dense first, thinning
        i = int(at * SR)
        m = int(rng.uniform(0.0008, 0.002) * SR)
        if i + m < len(grit):
            grit[i:i + m] += np.hanning(m) * rng.uniform(-1, 1)
    grit = band(grit, 3600, 0.5)
    hiss = band(rng.normal(0, 1, len(t)), 2600, 0.6)
    out = (norm(grit) * 0.7 + norm(hiss) * 0.25) * env
    for _ in range(rng.integers(2, 5)):  # a pebble or two turning over
        pd = 0.03
        tick = band(rng.normal(0, 1, int(pd * SR)), rng.uniform(2500, 4200), 0.25) * np.exp(-synth.seconds(pd) / 0.004) * onset(pd, 0.0015)
        synth.place(out, norm(tick) * rng.uniform(0.15, 0.3), rng.uniform(0.1, d * 0.6))
    return ease(out, 0.2)


def drip(rng):
    """A drop off a beam into the water: a small rising plip."""
    out = np.zeros(int(0.25 * SR))
    b = bubble(rng.uniform(900, 1500), 0.045, rng)
    out[:len(b)] += b
    tick = band(rng.normal(0, 1, int(0.006 * SR)), 3000, 0.5) * np.exp(-synth.seconds(0.006) / 0.0015) * onset(0.006, 0.001)
    out[:len(tick)] += norm(tick) * 0.15
    return ease(out, 0.05)


# ---------------------------------------------------------------- the water

def surface(rng):
    """The slow rise and settle of still water: a low, soft wash, lifting a little at each swell (each a different
    size), and the faintest high trickle all along the shore."""
    t = np.arange(N) / SR
    lift = np.zeros(N)
    for time, laps, _, _ in SWELLS:
        u = ((t - time + 0.8) % LENGTH)  # wrapped: the swell before the loop's end rises into its start
        lift += max(g for _, g, _ in laps) * np.exp(-0.5 * ((u - 0.8) / 0.7) ** 2)
    lift = 0.55 + 0.45 * lift / lift.max()
    lift *= slow(rng, 0.25, 0.7, 1.0)
    wash = shape(rng.normal(0, 1, N), lambda f: bump(f, 320, 0.6) + bump(f, 900, 0.5, 0.35)) * lift
    trickle = shape(rng.normal(0, 1, N), lambda f: bump(f, 3000, 0.5)) * slow(rng, 0.4, 0.2, 1.0) ** 2
    return level(wash, -45) + level(trickle, -58)


def render(v, rng):
    events = np.zeros(N)
    for time, laps, draws, gurgles in SWELLS:
        for offset, g, kind in laps:
            put(events, lap(rng, kind), time + offset + rng.uniform(-0.05, 0.05), g)
        if draws:
            put(events, shingle(rng), time + rng.uniform(1.0, 1.4), 0.5)
        if gurgles:
            put(events, gurgle(rng), time + rng.uniform(0.7, 1.1), 0.45)
    for time in DRIPS:
        put(events, drip(rng), time, rng.uniform(0.12, 0.25))
    out = surface(rng) + events * 10 ** (-14 / 20)
    # close by the jetty: a little of the planks' and piles' short reflection (applied circularly)
    ir = np.zeros(N)
    for _ in range(14):
        at = rng.uniform(0.004, 0.06)
        ir[int(at * SR)] += rng.uniform(-1, 1) * np.exp(-at / 0.025) * 0.25
    ir[0] = 1.0
    out = np.fft.irfft(np.fft.rfft(out) * np.fft.rfft(ir), N)
    return shape(out, lambda f: f ** 2 / (f ** 2 + 40 ** 2))  # nothing below the floor of hearing
