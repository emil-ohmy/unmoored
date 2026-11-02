"""The dead waiting in the queue to cross, now and then: small, tired, patient sounds from one soul or another along
the shore and the jetty. Soft, breathy, wordless, warm (low-mid, no hiss, no doubling): a long wait, not a haunting.
Mono one-shots, out in the open air over still water (a faint short misty tail), played now and then from souls in
the queue. Quiet on purpose, all about as loud as one another.
Variants:
  1 a low tired "hmm", falling, through a closed mouth (0.9 s)
  2 a long sigh: a breath let out, a little voice at the top of it, sinking (1.7 s)
  3 a soul humming to themself, half-remembered: the Hub's tune ("A, B A F#") low and slow, trailing off (2.6 s)
  4 a small, polite cough into a hand (two, the second smaller) and a throat settled with a soft "hm" (1.2 s)
  5 feet shifting on the planks: two soft scuffs, a board's small creak under the weight, a heel set down (1.4 s)
  6 two souls: one asks "mm?" (rising), the other answers "mm" (falling, lower), a moment apart (1.6 s)
`synth` and `np` are injected by tools/audio/build_sound.py."""
VARIANTS = 6
PEAK = [0.27, 0.45, 0.28, 0.5, 0.6, 0.28]  # matched by loudness: the voices near -21 LUFS, the cough and feet a little under

SR = synth.SR


def norm(x):
    return x / (np.max(np.abs(x)) + 1e-9)


def shape(x, gain):
    freqs = np.fft.rfftfreq(len(x), 1 / SR)
    return np.fft.irfft(np.fft.rfft(x) * gain(freqs), len(x))


def band(x, centre, width):
    return shape(x, lambda f: np.exp(-0.5 * (np.log(np.maximum(f, 1) / centre) / width) ** 2))


def soften(x, cutoff):
    """A gentle low-pass by FFT (no per-sample loop)."""
    return shape(x, lambda f: 1 / (1 + (f / cutoff) ** 2))


def ease(x, seconds):
    n = int(seconds * SR)
    x[-n:] *= np.cos(np.linspace(0, np.pi / 2, n)) ** 2
    return x


def swell(d, attack, release):
    t = synth.seconds(d)
    return np.sin(np.minimum(np.clip(t / attack, 0, 1), np.clip((d - t) / release, 0, 1)) * np.pi / 2) ** 2


def breath(d, rng, vowel="ah"):
    """Air through a mouth shaped to a vowel (breathy, not sung)."""
    freqs = np.fft.rfftfreq(int(d * SR), 1 / SR)
    x = np.fft.irfft(np.fft.rfft(rng.normal(0, 1, int(d * SR))) * synth.formants(freqs, vowel), int(d * SR))
    return soften(x, 2200)


def hum(pitch, d, rng, vowel="oo", cutoff=900, vibrato=0.006):
    """A voice through a closed or nearly closed mouth: warm, the highs gone, a little unsteady (an old voice)."""
    v = synth.voice(pitch, d, rng, vowel, vibrato=vibrato, max_freq=2200)
    return soften(v, cutoff)


def path(points, d):
    """A pitch path through (fraction of d, Hz) points, eased between them."""
    n = int(d * SR)
    u = np.linspace(0, 1, n)
    xs, ys = zip(*points)
    k = np.clip(np.searchsorted(xs, u, side="right") - 1, 0, len(xs) - 2)
    a, b = np.array(xs)[k], np.array(xs)[k + 1]
    w = np.clip((u - a) / np.maximum(b - a, 1e-9), 0, 1)
    w = (1 - np.cos(w * np.pi)) / 2
    return np.array(ys)[k] * (1 - w) + np.array(ys)[k + 1] * w


# ---------------------------------------------------------------- the variants

def hmm(rng, f0=None, fall=0.88, d=0.9):
    f0 = f0 or rng.uniform(120, 140)
    pitch = path([(0, f0 * 1.03), (0.3, f0), (1, f0 * fall)], d)
    x = norm(hum(pitch, d, rng, "oo", 800)) * swell(d, 0.12, 0.45)
    air = norm(breath(d, rng, "oo")) * swell(d, 0.05, 0.6) * 0.12
    return x + air


def sigh(rng):
    d = 1.7
    f0 = rng.uniform(150, 170)
    pitch = path([(0, f0), (0.15, f0 * 1.06), (1, f0 * 0.72)], d)
    voice = norm(hum(pitch, d, rng, "ah", 1300, 0.01)) * swell(d, 0.15, 0.2) ** 1.0
    voice *= np.exp(-synth.seconds(d) / 0.35)  # the voice only at the top of it; then just air
    air = norm(breath(d, rng, "ah")) * swell(d, 0.25, 1.1) * 0.55
    return voice * 0.6 + air


def humming(rng):
    """Hummed low and slow, a little out of tune, as if remembered from long ago; the last note trailing off."""
    notes = [("A3", 0.0, 0.55), ("B3", 0.55, 0.25), ("A3", 0.8, 0.25), ("F#3", 1.05, 1.2)]
    d = 2.6
    out = np.zeros(int(d * SR))
    drift = 2 ** (rng.uniform(-25, -10) / 1200)  # (flat: they don't quite have it)
    points = []
    for name, at, length in notes:
        f = synth.note(name) * drift
        points += [(at / d, f), ((at + length * 0.8) / d, f)]
    points[-1] = (1.0, points[-1][1] * 0.97)  # (sagging as it fades)
    pitch = path(points, d)
    x = norm(hum(pitch, d, rng, "oo", 700, 0.008))
    # each note leaned on a little, the hum dying away on the last
    env = 0.75 + 0.25 * sum(np.exp(-0.5 * ((synth.seconds(d) - at - 0.05) / 0.08) ** 2) for _, at, _ in notes)
    env *= swell(d, 0.1, 1.3)
    out += x * env
    return out + norm(breath(d, rng, "oo")) * env * 0.06


def cough(rng):
    """Into a hand: a short burst of air through an open throat (a catch of voice at its front), muffled."""
    d = 1.2
    out = np.zeros(int(d * SR))
    for at, g in ((0.03, 1.0), (0.27, 0.6)):
        cd = 0.28
        t = synth.seconds(cd)
        env = np.sin(np.clip(t / 0.008, 0, 1) * np.pi / 2) ** 2 * np.exp(-t / 0.07)
        burst = norm(breath(cd, rng, "ah")) * env
        catch = norm(hum(path([(0, 170), (1, 120)], cd), cd, rng, "ah", 1200, 0.0)) * env * np.exp(-t / 0.035)
        thump = np.sin(2 * np.pi * 95 * t) * np.exp(-t / 0.03) * np.clip(t / 0.004, 0, 1)
        synth.place(out, (burst + 0.5 * catch + 0.25 * thump) * g, at)
    out = soften(out, 1100)  # (behind a hand)
    synth.place(out, hmm(rng, rng.uniform(115, 125), 0.92, 0.45) * 0.4, 0.6)
    return out


def scuff(d, rng):
    """A foot sliding a little on dry boards: grainy friction swelling and fading."""
    t = synth.seconds(d)
    grain = np.abs(synth.smooth_random(d, rng, 60, -1, 1)) ** 2
    x = band(rng.normal(0, 1, len(t)), rng.uniform(1300, 1900), 0.6) * (0.4 + grain)
    return norm(x) * swell(d, d * 0.35, d * 0.55)


def shuffle(rng):
    import importlib.util, os
    spec = importlib.util.spec_from_file_location("slam", os.path.join(os.path.dirname(__file__), "laden_slam_windup.py"))
    kit = importlib.util.module_from_spec(spec)
    kit.synth, kit.np = synth, np
    spec.loader.exec_module(kit)
    d = 1.4
    out = np.zeros(int(d * SR))
    synth.place(out, scuff(0.22, rng) * 0.35, 0.02)
    synth.place(out, scuff(0.3, rng) * 0.28, 0.42)
    # the board under the weight: a short low creak, slow stick-slip
    cd = 0.35
    creak = kit.creak(cd, rng, 25, 40, [(380, 1.0, 0.012), (870, 0.5, 0.008), (1700, 0.2, 0.004)], lambda u: 1.0)
    synth.place(out, norm(creak) * swell(cd, 0.1, 0.2) * 0.35, 0.5)
    # a heel set down on hollow planks: a soft low knock
    t = synth.seconds(0.4)
    knock = np.sin(2 * np.pi * 120 * t) * np.exp(-t / 0.05) + 0.5 * np.sin(2 * np.pi * 240 * t) * np.exp(-t / 0.03)
    knock += 0.4 * norm(band(rng.normal(0, 1, len(t)), 600, 0.5)) * np.exp(-t / 0.012)
    knock *= np.sin(np.clip(t / 0.004, 0, 1) * np.pi / 2) ** 2
    synth.place(out, norm(knock) * 0.6, 0.86)
    return soften(out, 2500)


def exchange(rng):
    d = 1.6
    out = np.zeros(int(d * SR))
    ask = 0.55
    a = rng.uniform(150, 165)
    x = norm(hum(path([(0, a), (0.6, a * 1.02), (1, a * 1.22)], ask), ask, rng, "oo", 900)) * swell(ask, 0.08, 0.2)
    synth.place(out, x * 0.85, 0.02)
    answer = 0.7
    b = rng.uniform(110, 122)
    y = norm(hum(path([(0, b * 1.08), (0.3, b * 1.04), (1, b * 0.9)], answer), answer, rng, "oh", 750)) * swell(answer, 0.1, 0.35)
    synth.place(out, y, 0.82)
    return out + norm(breath(d, rng, "oo")) * 0.03 * swell(d, 0.1, 0.5)


def render(v, rng):
    dry = [hmm, sigh, humming, cough, shuffle, exchange][v](rng)
    total = len(dry) / SR + 0.5
    dry = synth.pad(dry, total)
    dry = shape(dry, lambda f: f ** 2 / (f ** 2 + 70 ** 2))  # (no rumble)
    ir = synth.reverb_ir(1.4, 1.0, rng, predelay=0.02, brightness=0.18)[0]
    wet = synth.fft_convolve(dry, ir)[:len(dry)]
    out = dry + norm(wet) * np.max(np.abs(dry)) * 0.18
    return ease(out, 0.25)
