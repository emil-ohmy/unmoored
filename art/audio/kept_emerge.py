"""One of the Kept tearing free of the Hold's living wall as you pass, 1.6 s. The stone gives first (a soft, clay-like
grind: hold_stone_press's code), then the tar stretches (a sticky, tightening crackle of tiny wet pops and a slow
stick-slip creak that quickens as it pulls), snaps (a wet, dull "thwup" bending down, a spatter of pops), a small
gasping breath as it comes loose (nearly a child's), and it drops onto the stone (a soft wet pat), a few sticky
pops settling after. Close and small: something pitiful, not a monster.
Variants: 1 plain, 2 quick (an arm comes loose first, then the body: two snaps), 3 slow and heavy, the longest pull.
The Kept's other sounds (kept_*) use this file's code: their tar, breath and voice, thumps, bubbles, scratches.
`synth` and `np` are injected by tools/audio/build_sound.py."""
VARIANTS = 3
PEAK = 0.75  # noticed as it comes (a warning), but small

SR = synth.SR


def norm(x):
    return x / (np.max(np.abs(x)) + 1e-9)


def load(name):
    """Another script in this folder, as a module (the Hold's stone)."""
    import importlib.util
    import os
    spec = importlib.util.spec_from_file_location(name, os.path.join(os.path.dirname(__file__), name + ".py"))
    module = importlib.util.module_from_spec(spec)
    module.synth, module.np = synth, np
    spec.loader.exec_module(module)
    return module


def hump(d, start, length, power=1.5):
    """0 -> 1 -> 0 over [start, start + length], per sample over d."""
    t = synth.seconds(d)
    return np.sin(np.clip((t - start) / max(length, 1e-4), 0, 1) * np.pi) ** power


# ---------------------------------------------------------------- tar

def pop(rng, low, high, length=0.02, tau=(0.0015, 0.004), rise=(0.2, 0.6)):
    """One wet pop (or, slower and lower, one bubble): a tiny sine chirp rising as it bursts."""
    t = synth.seconds(length)
    f = rng.uniform(low, high) * (1 + rng.uniform(*rise) * np.clip(t / (length * 0.4), 0, 1))
    return np.sin(2 * np.pi * np.cumsum(f) / SR) * np.exp(-t / rng.uniform(*tau)) * np.clip(t / 0.0006, 0, 1)


def pops(d, count, rng, low=700, high=2600, density=lambda u: 1.0, length=0.02, tau=(0.0015, 0.004), rise=(0.2, 0.6),
         pitch=lambda u: 1.0):
    """Many pops scattered over d, their times picked by `density` (of the fraction through), pitched by `pitch`."""
    out = np.zeros(int(d * SR) + int(length * SR) + 1)
    for _ in range(count):
        while True:
            u = rng.random()
            if rng.random() < density(u):
                break
        p = pop(rng, low * pitch(u), high * pitch(u), length, tau, rise)
        synth.place(out, p * rng.uniform(0.15, 1.0) ** 1.5, u * d)
    return out[:int(d * SR)]


def stretch(d, rng, tension):
    """Tar pulled until it gives: a sticky crackle (tiny dark pops, denser as it tightens), a stick-slip creak
    (soft clicks whose rate quickens with the pull, through a low band that rises), a thick hush under it.
    `tension` is per sample, 0 -> 1."""
    n = int(d * SR)
    tension = tension[:n]
    crackle = pops(d, int(d * 120), rng, 600, 2200, density=lambda u: 0.1 + 0.9 * np.interp(u, np.linspace(0, 1, n), tension))
    rate = (18 + 70 * tension) * (1 + 0.35 * synth.smooth_random(d, rng, 9.0, -1, 1)[:n])
    phase = np.cumsum(rate) / SR
    ticks = np.diff(np.floor(phase), prepend=0.0) * rng.uniform(0.4, 1.0, n)
    grain = np.hanning(int(0.003 * SR)) * synth.white(0.003, rng)[:int(0.003 * SR)]
    creak = synth.fft_convolve(ticks, grain)[:n]
    creak = synth.bandpass(creak, 320 + 600 * tension, 2.5)
    hush = synth.lowpass(synth.bandpass(synth.white(d, rng), 300 + 900 * tension, 1.2), 2500)
    return (norm(crackle) * 0.55 + norm(creak) * 0.6 * tension + norm(hush) * 0.3 * tension ** 1.5)


def snap(rng, size=1.0):
    """Tar letting go: a wet, dull "thwup" bending down fast, a muddy burst, a spatter of pops after.
    size > 1 is smaller (higher, lighter)."""
    d = 0.18
    t = synth.seconds(d)
    f = synth.glide(rng.uniform(280, 340) * size, rng.uniform(95, 120) * size, d, 0.25)
    thwup = synth.sine(f, d) * np.exp(-t / 0.032)
    burst = synth.lowpass(synth.white(d, rng), 900 * size) * np.exp(-t / 0.014)
    spatter = pops(0.14, 9, rng, 900, 3000, density=lambda u: 1 - u)
    out = norm(thwup) + 0.5 * norm(burst)
    synth.place(out, norm(spatter) * 0.35, 0.006)
    return out * np.clip(t / 0.001, 0, 1)


def gurgle(d, rng, count, low=150, high=520, density=lambda u: 1.0, pitch=lambda u: 1.0):
    """Bubbles in something thick: low, slow sine chirps rising as each bursts."""
    return pops(d, count, rng, low, high, density, length=0.07, tau=(0.01, 0.03), rise=(0.3, 0.9), pitch=pitch)


# ---------------------------------------------------------------- breath and voice

def throat(d, rng, f0, centre, voiced=0.5, rasp=0.5, vowel="ah"):
    """A wordless breath-voice through tar: breath through two moving formants (`centre`, per sample, the first),
    and a thin voice on `f0` (per sample) roughened by an irregular fast flutter (a throat full of something thick),
    dulled at the top. Nearly a person; not quite."""
    n = int(d * SR)
    breath = synth.bandpass(synth.white(d, rng), centre, 2.2) + 0.45 * synth.bandpass(synth.white(d, rng), centre * 2.4, 3.5)
    voice = synth.voice(f0, d, rng, vowel, vibrato=0.025, max_freq=4000)
    flutter = synth.smooth_random(d, rng, 70.0, 0.0, 1.0)[:n]
    voice = voice * ((1 - rasp) + rasp * 2.2 * flutter ** 2)
    out = norm(breath) * (1 - voiced) + norm(voice) * voiced
    return synth.lowpass(out, 3800)


def gasp(rng, d=0.24, f0=430, voiced=0.3):
    """A small gasping breath drawn in (coming loose): breath rising, a faint voice in it, cut off at the top."""
    t = synth.seconds(d)
    x = throat(d, rng, synth.glide(f0 * 0.9, f0 * 1.18, d), synth.glide(750, 1500, d), voiced, 0.45)
    shape = np.clip(t / (d * 0.7), 0, 1) ** 1.3 * np.clip((d - t) / 0.05, 0, 1)
    return x * shape


# ---------------------------------------------------------------- body and claws

def thump(rng, low=95, d=0.25):
    """A muffled, wet body hit: a low tone bending down, mud, a faint smack on top."""
    t = synth.seconds(d)
    body = synth.sine(synth.glide(low * 1.7, low * 0.7, d, 0.25), d) * np.exp(-t / 0.05)
    mud = synth.lowpass(synth.lowpass(synth.white(d, rng), 380), 520) * np.exp(-t / 0.03)
    smack = synth.bandpass(synth.white(d, rng), rng.uniform(800, 1100), 1.2) * np.exp(-t / 0.007)
    return (norm(body) + 0.7 * norm(mud) + 0.22 * norm(smack)) * np.clip(t / 0.0015, 0, 1)


def scrape(d, rng, centre, density=500):
    """Something hard and thin dragged over stone or through air: a dense granular rasp in a band."""
    n = int(d * SR)
    grains = (rng.random(n) < density / SR) * rng.uniform(0.2, 1.0, n)
    g = np.hanning(int(0.0015 * SR)) * synth.white(0.0015, rng)[:int(0.0015 * SR)]
    x = synth.fft_convolve(grains, g)[:n] + 0.3 * synth.white(d, rng)
    return synth.bandpass(x, centre, 2.5)


def room(x, rng, amount=0.15):
    """A breath of the cave round it: close, a dark short tail."""
    ir = synth.reverb_ir(0.8, 0.7, rng, predelay=0.008, brightness=0.22)[0]
    wet = synth.fft_convolve(x, ir)[:len(x)]
    return x + norm(wet) * np.max(np.abs(x)) * amount


# ---------------------------------------------------------------- the emerging

# (snap at, gasp pitch Hz, drop at, an arm first, stone colour Hz)
SHAPES = [(0.72, 430, 1.08, False, 480), (0.62, 470, 0.98, True, 560), (0.86, 390, 1.2, False, 400)]


def render(v, rng):
    d = 1.6
    snap_at, f0, drop_at, arm, colour = SHAPES[v]
    n = int(d * SR)
    t = synth.seconds(d)
    out = np.zeros(n)
    # the stone gives: a soft grind under the pull
    stone = load("hold_stone_press")
    speed = hump(d, 0.0, snap_at + 0.2, 1.3) * 0.8 + 0.05
    synth.place(out, norm(stone.clay(d, speed, colour, rng)) * 0.35 * np.clip((snap_at + 0.35 - t) / 0.2, 0, 1), 0.0)
    # the tar stretching, tighter and tighter, until it snaps
    pull = snap_at + 0.01
    tension = np.clip(synth.seconds(pull) / pull, 0, 1) ** (1.6 if v == 2 else 1.3)
    s = stretch(pull, rng, tension) * (0.25 + 0.75 * tension) * np.clip(synth.seconds(pull) / 0.08, 0, 1) * np.clip((pull - synth.seconds(pull)) / 0.012, 0, 1)
    synth.place(out, norm(s) * 0.5, 0.0)
    if arm:
        synth.place(out, norm(snap(rng, 1.35)) * 0.45, snap_at - 0.24)
    synth.place(out, norm(snap(rng, 0.9 if v == 2 else 1.0)) * 0.95, snap_at)
    # loose: a small gasp
    synth.place(out, norm(gasp(rng, 0.24, f0)) * 0.42, snap_at + 0.05)
    # it drops onto the stone: a soft wet pat, small and light
    synth.place(out, norm(thump(rng, 140 if v != 2 else 120, 0.22)) * 0.4, drop_at)
    synth.place(out, norm(pops(0.45, 10, rng, 700, 2200, density=lambda u: (1 - u) ** 2)) * 0.25, drop_at + 0.02)
    out = synth.highpass(out, 40)
    out = synth.lowpass(out, 7000)
    out = room(out, rng, 0.16)
    return out * np.clip((d - t) / 0.12, 0, 1) ** 2
