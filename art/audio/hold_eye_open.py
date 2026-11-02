"""An eye in the Hold's wall opening as you come near: very close and small, 0.4-0.8 s. The stone lips part, wet and
sticky (a soft slide of moist stone on stone, a fine crackle of tiny wet pops as the seal lets go, thinning out as
it opens), then the faint click of the lid settling open. Barely any room: it's right by you.
Variants: 1 slow and sticky, 2 quick with a sharper click, 3 a hesitant opening (it catches halfway), 4 the
widest and wettest. hold_eye_close uses this code (slide, pops, tick, close_room).
`synth` and `np` are injected by tools/audio/build_sound.py."""
VARIANTS = 4
PEAK = 0.6  # small and close

SR = synth.SR
# (length, how long the lips part, pop count, click at, click brightness Hz)
SHAPES = [(0.75, 0.48, 26, 0.55, 2300), (0.45, 0.22, 16, 0.27, 3200), (0.7, 0.5, 22, 0.56, 2600),
          (0.8, 0.5, 34, 0.6, 2100)]


def norm(x):
    return x / (np.max(np.abs(x)) + 1e-9)


def slide(d, rng, centre=900, falling=False):
    """Moist stone sliding on stone: a soft, dark, slightly rough hush (a low grind of stone under it) that swells and fades, its colour drifting."""
    n = int(d * SR)
    t = synth.seconds(d)
    sweep = synth.glide(centre * (1.25 if falling else 0.8), centre * (0.8 if falling else 1.25), d)[:n]
    noise = synth.lowpass(synth.bandpass(synth.white(d, rng), sweep, 1.1), 2200)
    grind = synth.lowpass(synth.bandpass(synth.white(d, rng), 220, 1.5), 600)  # stone under it, low and rough
    noise = noise / (np.max(np.abs(noise)) + 1e-9) + 0.5 * grind / (np.max(np.abs(grind)) + 1e-9)
    rough = 1 + 0.6 * synth.smooth_random(d, rng, 40.0, -1, 1)[:n]  # the grain of the stone
    shape = np.sin(np.clip(t / d, 0, 1) * np.pi) ** 1.5
    return noise * rough * shape


def pops(d, count, rng, low=1400, high=4200, density=lambda u: 1 - u):
    """Tiny wet pops as a moist seal lets go: short bright chirps, dense at first then thinning."""
    out = np.zeros(int(d * SR) + int(0.03 * SR))
    for _ in range(count):
        while True:  # a time picked by the density

            u = rng.random()
            if rng.random() < density(u):
                break
        pd = 0.012
        pt = synth.seconds(pd)
        f = rng.uniform(low, high) * (1 + rng.uniform(0.2, 0.6) * np.clip(pt / 0.004, 0, 1))
        p = np.sin(2 * np.pi * np.cumsum(f) / SR) * np.exp(-pt / rng.uniform(0.0015, 0.003)) * np.clip(pt / 0.0004, 0, 1)
        synth.place(out, p * rng.uniform(0.15, 1.0) ** 1.5, u * d)
    return out[:int(d * SR)]


def tick(rng, brightness, body=None):
    """The lid: a faint, small, dull click (soft tissue on stone, not hard), with a hint of a hollow body."""
    d = 0.06
    t = synth.seconds(d)
    click = synth.bandpass(synth.white(d, rng), brightness, 2.0) * np.exp(-t / 0.0025)
    b = body or rng.uniform(380, 520)
    hollow = np.sin(2 * np.pi * b * t) * np.exp(-t / 0.012) * 0.6
    return (click + hollow) * np.clip(t / 0.0004, 0, 1)


def close_room(x, rng):
    """A breath of the cave round it: very little, it's near."""
    ir = synth.reverb_ir(0.6, 0.5, rng, predelay=0.006, brightness=0.25)[0]
    wet = synth.fft_convolve(x, ir)[:len(x)]
    return x + norm(wet) * np.max(np.abs(x)) * 0.12


def render(v, rng):
    d, part, count, click_at, bright = SHAPES[v]
    out = np.zeros(int(d * SR))
    s = slide(part, rng, rng.uniform(700, 1000))
    synth.place(out, norm(s) * 0.3, 0.02)
    if v == 2:  # catches halfway: the pops come in two bursts
        synth.place(out, norm(pops(part * 0.4, count // 2, rng)) * 0.6, 0.03)
        synth.place(out, norm(pops(part * 0.45, count - count // 2, rng)) * 0.55, 0.03 + part * 0.55)
    else:
        synth.place(out, norm(pops(part, count, rng)) * (0.75 if v == 3 else 0.6), 0.03)
    synth.place(out, norm(tick(rng, bright)) * (0.4 if v == 1 else 0.28), click_at)
    out = synth.lowpass(out, 6500)
    out = close_room(out, rng)
    t = synth.seconds(d)
    return out * np.clip((d - t) / 0.08, 0, 1) ** 2
