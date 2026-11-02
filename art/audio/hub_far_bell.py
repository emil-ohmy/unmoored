"""A bell on the Far shore, struck once, far off across still water: a big, low bronze bell (its strike note D4, its
hum D3 an octave under: the Hub's key), so far away its highs are gone and its strike is only a soft knock. It hums on
a long while, warbling slowly (each partial a pair a hair apart, beating, as a real bell's do), and the water carries
it: a faint second arrival a moment after (off the water), a long soft open tail.
About 14 s with the tail. Mono (a far sound has no place to come from but "out there"). Quiet: normalised low.
Variants: 1 plain, 2 struck softer and further off (duller, more of the tail, the knock nearly gone),
3 struck a little harder (the knock clearer, the warble wider), a few cents lower on its tail as it cools.
Its bell() is the Hub's bell: music_hub strikes it too. `synth` and `np` are injected by tools/audio/build_sound.py.
"""
VARIANTS = 3
PEAK = 0.6

SR = synth.SR
STRIKE = "D4"
# (ratio to the strike note, amplitude, decay seconds): hum, prime, tierce (minor third), quint, nominal, and above.
PARTIALS = ((0.5, 0.9, 6.0), (1.0, 1.0, 5.0), (1.19, 0.45, 3.4), (1.5, 0.3, 2.6), (2.0, 0.5, 2.8),
            (2.51, 0.16, 1.6), (2.99, 0.12, 1.2), (4.08, 0.06, 0.7))


def band(x, centre, width):
    """A soft band-pass by FFT (a bell curve over log-frequency): no per-sample loop."""
    freqs = np.fft.rfftfreq(len(x), 1 / SR)
    gain = np.exp(-0.5 * (np.log(np.maximum(freqs, 1) / centre) / width) ** 2)
    return np.fft.irfft(np.fft.rfft(x) * gain, len(x))


def bell(duration, rng, distance=1.0, strength=1.0, warble=0.35, sag=0.0):
    """The bell, struck once. `distance` dulls it (1: across the water, 1.5: further), `strength` brings the knock up,
    `warble` sets how fast its partial pairs beat (Hz), `sag` lowers its pitch by that many cents as it rings."""
    t = synth.seconds(duration)
    f0 = synth.note(STRIKE)
    cents = 1 - (1 - 2 ** (-sag / 1200)) * (1 - np.exp(-t / 3.0))
    out = np.zeros_like(t)
    cutoff = 1400 / distance
    for ratio, a, tau in PARTIALS:
        f = f0 * ratio
        far = 1 / (1 + (f / cutoff) ** 2)  # the air and the mist take the highs
        split = warble * rng.uniform(0.6, 1.4)
        for side, share in ((-0.5, 0.62), (0.5, 0.38)):  # the pair, a hair apart, one stronger: a warble, not a throb
            phase = 2 * np.pi * np.cumsum((f + side * split) * cents) / SR + rng.uniform(0, 6)
            out += share * a * far * np.sin(phase) * np.exp(-t / (tau * (0.9 + 0.2 * distance)))
    # the strike, far off: a soft dull knock, no clang
    knock = band(rng.normal(0, 1, int(0.08 * SR)), 500 / distance, 0.5) * np.exp(-synth.seconds(0.08) / 0.012)
    out[:len(knock)] += knock / (np.max(np.abs(knock)) + 1e-9) * 0.35 * strength / distance
    return out * np.sin(np.clip(t / 0.006, 0, 1) * np.pi / 2) ** 2  # (a few ms of onset: far things are never sharp)


def water(x, rng, distance=1.0):
    """What the water does to it: a faint second arrival off the surface, a little later and duller, and a long soft
    open tail (no walls: mist and water, the highs going first)."""
    n = len(x)
    out = x.copy()
    lag = int(rng.uniform(0.09, 0.13) * distance * SR)
    bounce = band(x, 380, 0.9)
    out[lag:] += bounce[:n - lag] * 0.3
    ir = synth.reverb_ir(6.0, 5.0, rng, predelay=0.06, brightness=0.15)[0]
    wet = synth.fft_convolve(x, ir)[:n]
    wet = wet / (np.max(np.abs(wet)) + 1e-9) * np.max(np.abs(x))
    return out * (1.0 - 0.25 * distance) + wet * 0.5 * distance


def render(v, rng):
    distance, strength, warble, sag = [(1.0, 1.0, 0.35, 0.0), (1.5, 0.5, 0.3, 0.0), (1.0, 1.6, 0.6, 4.0)][v]
    duration = 14.0
    t = synth.seconds(duration)
    x = bell(duration, rng, distance, strength, warble, sag)
    out = water(x, rng, distance)
    return out * np.sin(np.clip((duration - t) / 4.0, 0, 1) * np.pi / 2) ** 2  # (nearly nothing by then: eased out)
