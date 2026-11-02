"""A face or a hand pressing out of the Hold's wall and sinking back: stone moving like slow clay, close and quiet,
2.5-3.5 s. Out: a slow, thick grinding hush (stone that gives instead of breaking: a rough, granular drag, its grains
dense at first and smearing), swelling as the shape comes; a moment held, almost still; back: the drag again, lower,
as it sinks. Under it something soft: a muffled low pressure (a body against a sheet), a damp give, a faint wet
slither at the edges where the stone closes over.
Variants: 1 a face (slow out, held, slow back), 2 a hand (quicker out, longer held, a sticky slip back),
3 a deep, slow one, heaviest.
`synth` and `np` are injected by tools/audio/build_sound.py."""
VARIANTS = 3
PEAK = 0.6  # quiet and close

SR = synth.SR
# (length, rise out, hold, sink back, grind colour Hz)
SHAPES = [(3.1, 1.2, 0.35, 1.3, 520), (2.6, 0.75, 0.6, 1.0, 640), (3.5, 1.4, 0.3, 1.6, 400)]


def norm(x):
    return x / (np.max(np.abs(x)) + 1e-9)


def motion(d, rise, hold, sink):
    """How fast the stone is moving: a smooth hump out, nearly still while held, a smooth hump back."""
    t = synth.seconds(d)
    out_ = np.sin(np.clip((t - 0.05) / rise, 0, 1) * np.pi) ** 1.5
    back_at = 0.05 + rise + hold
    back = np.sin(np.clip((t - back_at) / sink, 0, 1) * np.pi) ** 1.5 * 0.85
    return np.maximum(out_, back) + 0.04 * np.sin(np.clip((t - 0.05 - rise) / max(hold, 1e-3), 0, 1) * np.pi)


def clay(d, speed, colour, rng):
    """Stone moving like slow clay: a granular drag. Many tiny rough grains (denser as it moves faster), smeared
    into a thick hush, through a low-mid band that brightens a little with speed."""
    n = int(d * SR)
    density = 60 + 900 * speed
    grains = (rng.random(n) < density / SR) * rng.uniform(0.2, 1.0, n)
    grain = synth.lowpass(synth.white(0.008, rng), 2500) * np.hanning(int(0.008 * SR))
    rough = synth.fft_convolve(grains, grain)[:n]
    hush = synth.white(d, rng) * speed
    x = norm(rough) + 0.5 * norm(hush)
    x = synth.bandpass(x, colour * (0.8 + 0.5 * speed), 0.9)
    return synth.lowpass(x, 2200) * speed


def soft(d, speed, rng):
    """Something soft under it: a muffled low pressure, a damp give (low noise, very dark, swelling with the
    motion; a faint low wavering underneath, like a muffled mass)."""
    n = int(d * SR)
    press = synth.lowpass(synth.lowpass(synth.white(d, rng), 180), 260) * speed ** 1.2
    f = rng.uniform(70, 90) * (1 + 0.05 * synth.smooth_random(d, rng, 2.0, -1, 1)[:n])
    mass = synth.lowpass(synth.sine(f, d) + 0.4 * synth.sine(2 * f, d, 1.0), 200) * speed ** 1.6
    return norm(press) + 0.3 * norm(mass)


def slither(d, rng, at, length=0.35):
    """The damp edge where stone closes over: a faint wet slither (a few tiny sticky pops in a soft hiss)."""
    out = np.zeros(int(d * SR))
    t = synth.seconds(length)
    hiss = synth.bandpass(synth.white(length, rng), 1500, 1.4) * np.sin(np.pi * t / length) ** 2 * 0.4
    synth.place(out, hiss, at)
    for _ in range(rng.integers(4, 9)):
        pd = 0.01
        pt = synth.seconds(pd)
        f = rng.uniform(1100, 2600)
        p = np.sin(2 * np.pi * f * pt) * np.exp(-pt / 0.002) * np.clip(pt / 0.0005, 0, 1)
        synth.place(out, p * rng.uniform(0.2, 0.7), at + rng.uniform(0, length))
    return out


def render(v, rng):
    d, rise, hold, sink, colour = SHAPES[v]
    speed = motion(d, rise, hold, sink)
    out = norm(clay(d, speed, colour, rng)) * 0.75 + norm(soft(d, speed, rng)) * (0.55 if v < 2 else 0.7)
    sl = slither(d, rng, 0.05 + rise + hold + sink * 0.55, 0.35 if v != 1 else 0.5)
    out += norm(sl) * (0.18 if v != 1 else 0.24)
    out = synth.highpass(out, 30)
    ir = synth.reverb_ir(0.9, 0.8, rng, predelay=0.008, brightness=0.2)[0]
    wet = synth.fft_convolve(out, ir)[:len(out)]
    out = out + norm(wet) * np.max(np.abs(out)) * 0.15
    t = synth.seconds(d)
    return out * np.clip((d - t) / 0.15, 0, 1) ** 2 * np.clip(t / 0.05, 0, 1)
