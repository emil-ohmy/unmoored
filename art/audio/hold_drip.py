"""A single drop in the Hold, a vast damp space: into still water (1-4: a tiny impact and the bubble's rising plip, a
small resonant tail; pitches from high and small to low and fat) or onto wet wood (5) and wet stone (6): a dull
tick, a few beads spattering. Each rings on a little in the room. Under 1.5 s.
`synth` and `np` are injected by tools/audio/build_sound.py."""
VARIANTS = 6
PEAK = 0.8

SR = synth.SR
PLIPS = [  # (bubble pitch Hz, its ring in s, how far it rises)
    (1900, 0.018, 0.55),
    (1400, 0.024, 0.6),
    (1050, 0.03, 0.7),
    (760, 0.04, 0.5),   # a fat one
]


def plip(f0, tau, rise, rng):
    d = 0.3
    t = synth.seconds(d)
    f = f0 * rng.uniform(0.95, 1.05) * (1 + rise * np.clip(t / 0.025, 0, 1) ** 0.7)
    bubble = np.sin(2 * np.pi * np.cumsum(f) / SR) * np.exp(-t / tau) * np.clip(t / 0.0012, 0, 1)
    tick = synth.highpass(synth.white(0.005, rng), 2500) * synth.decay(0.005, 0.001) * 0.3
    out = bubble
    out[:len(tick)] += tick
    # the splash it throws: a fainter second bubble just after, higher
    t2 = synth.seconds(0.12)
    f2 = f0 * rng.uniform(1.5, 1.9) * (1 + 0.4 * np.clip(t2 / 0.02, 0, 1))
    second = np.sin(2 * np.pi * np.cumsum(f2) / SR) * np.exp(-t2 / (tau * 0.6)) * np.clip(t2 / 0.001, 0, 1)
    synth.place(out, second * 0.18, rng.uniform(0.035, 0.07))
    return out


def on_wood(rng):
    """Onto a wet plank: a soft dull tick, the wood barely sounding, a wet smack."""
    d = 0.25
    t = synth.seconds(d)
    smack = synth.bandpass(synth.white(d, rng), 2200, 1.2) * np.exp(-t / 0.006)
    body = np.sin(2 * np.pi * rng.uniform(480, 560) * t) * np.exp(-t / 0.018) * 0.5 \
        + np.sin(2 * np.pi * rng.uniform(1150, 1300) * t) * np.exp(-t / 0.009) * 0.25
    return (smack + body) * np.clip(t / 0.0008, 0, 1)


def on_stone(rng):
    """Onto wet stone: a sharp little tick and a few beads flung off it."""
    d = 0.3
    t = synth.seconds(d)
    out = synth.bandpass(synth.white(d, rng), 4200, 1.0) * np.exp(-t / 0.003) * np.clip(t / 0.0005, 0, 1)
    for _ in range(rng.integers(3, 6)):
        bd = 0.02
        bead = synth.bandpass(synth.white(bd, rng), rng.uniform(3500, 6500), 3.0) * synth.decay(bd, 0.003)
        synth.place(out, bead * rng.uniform(0.15, 0.35), rng.uniform(0.012, 0.07))
    return out


def room(x, rng):
    """The vast damp undercroft: a short cluster off the planking above and a dark tail; the drop rings in it."""
    ir = synth.reverb_ir(1.3, 1.1, rng, predelay=0.012, brightness=0.2)[0]
    wet = synth.fft_convolve(x, ir)[:len(x)]
    return x + wet / (np.max(np.abs(wet)) + 1e-9) * np.max(np.abs(x)) * 0.32


def render(v, rng):
    total = 1.4
    out = np.zeros(int(total * SR))
    if v < 4:
        dry = plip(*PLIPS[v], rng)
    elif v == 4:
        dry = on_wood(rng)
    else:
        dry = on_stone(rng)
    synth.place(out, dry / (np.max(np.abs(dry)) + 1e-9), 0.0)
    out = synth.lowpass(out, 7000)
    return room(out, rng)
