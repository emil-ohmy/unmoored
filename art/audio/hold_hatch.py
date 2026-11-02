"""The hatch: the way out of the Hold, grey daylight above. Heard from below, through the jetty's planks: wind over
open water (a low muffled roar swelling in gusts, a thin whistle where it finds the cracks round the hatch) and the
waves lapping and slapping against the jetty's pilings above (a muffled slosh with each swell, a deep thump as one
hits the wood). A 16 s seamless mono loop for a sound source at the hatch.
Built circularly over the loop (noise shaped by FFT over exactly one loop, slaps wrapped round its end): nothing is
crossfaded, the seam is exact. `synth` and `np` are injected by tools/audio/build_sound.py."""
LOOP = True
PEAK = 0.7

SR = synth.SR
LENGTH = 16.0
N = int(LENGTH * SR)
WAVES = 4  # swells to the loop: one every 4 s


def rms(x):
    return np.sqrt(np.mean(x ** 2)) + 1e-12


def level(x, db):
    return x / rms(x) * 10 ** (db / 20)


def shape(x, gain):
    freqs = np.fft.rfftfreq(len(x), 1 / SR)
    return np.fft.irfft(np.fft.rfft(x) * gain(freqs), len(x))


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


def bump(f, centre, width, gain):
    return gain * np.exp(-0.5 * (np.log(np.maximum(f, 1) / centre) / width) ** 2)


def wind(rng):
    """Open-air wind heard through planks: a dark roar (the planks let the lows through), its gusts swelling; the
    planks' own hollow note a little; a thin whistle at the cracks that rises with each gust."""
    gust = slow(rng, 0.35, 0.25, 1.0) ** 1.5
    roar = shape(rng.normal(0, 1, N), lambda f: 1 / (1 + (f / 320) ** 3) * (f / 40) ** 2 / (1 + (f / 40) ** 2)
                 + bump(f, 170, 0.25, 0.6))  # the planking ringing faintly
    gap = shape(rng.normal(0, 1, N), lambda f: bump(f, 1500, 0.08, 1.0) + bump(f, 2300, 0.06, 0.4))
    return level(roar * (0.35 + 0.65 * gust), -20) + level(gap * gust ** 2, -42)


def slap(rng):
    """A wave against a piling above, muffled: a slosh rising and falling, a deep thump of water on wood."""
    d = rng.uniform(0.9, 1.4)
    t = synth.seconds(d)
    slosh = synth.bandpass(synth.white(d, rng), synth.glide(rng.uniform(700, 900), rng.uniform(250, 330), d, 0.6), 1.0)
    slosh *= synth.envelope(d, 0, 0, curve=2.2, peak_at=rng.uniform(0.08, 0.16))
    thump = np.sin(2 * np.pi * rng.uniform(55, 75) * t) * np.exp(-t / 0.12) * np.clip(t / 0.01, 0, 1)
    thump += synth.lowpass(synth.white(d, rng), 200) * np.exp(-t / 0.05) * np.clip(t / 0.006, 0, 1) * 0.6
    out = slosh / (np.max(np.abs(slosh)) + 1e-9) + 0.8 * thump / (np.max(np.abs(thump)) + 1e-9)
    out = synth.lowpass(synth.lowpass(out, 650), 900)  # through the planks
    return out * np.clip((d - t) / 0.03, 0, 1)


def water(rng):
    """The swell: a soft wash rising with each wave, and the slaps on its crest (two or three, from pilings near
    and further off)."""
    t = np.arange(N) / SR
    swl = (0.5 - 0.5 * np.cos(2 * np.pi * WAVES / LENGTH * t + 0.6)) ** 2
    wash = shape(rng.normal(0, 1, N), lambda f: bump(f, 400, 0.6, 1.0) / (1 + (f / 700) ** 4)) * (0.2 + 0.8 * swl)
    slaps = np.zeros(N)
    period = LENGTH / WAVES
    for w in range(WAVES):
        crest = w * period + period * (0.5 - 0.6 / (2 * np.pi)) - 0.3
        for k in range(rng.integers(2, 4)):
            put(slaps, slap(rng), crest + k * rng.uniform(0.35, 0.8) + rng.uniform(-0.15, 0.15), rng.uniform(0.4, 1.0) * (1 - 0.25 * k))
    return level(wash, -32) + level(slaps, -26)


def render(v, rng):
    out = wind(rng) + water(rng)
    return shape(out, lambda f: f ** 2 / (f ** 2 + 30 ** 2))  # nothing below the floor of hearing
