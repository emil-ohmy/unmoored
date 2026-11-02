"""The rock groaning far off in the Hold: stone under great weight, 2-4 s, distant and deep. Not timber: no ringing
resonances, no creak: a slow, rough stick-slip of rock on rock rung through broad low resonances (the mass of the
walls), a grinding hush with it, a deep rumble under it all, a few tiny fractures; heard from far through the cave
(no highs, a long dark stone tail).
Variants: 1 a long low groan, 2 two shoves with a pause between, 3 a short heavy one that ends in a crack and a
trickle of grit settling, 4 the slowest and deepest, almost only rumble.
`synth` and `np` are injected by tools/audio/build_sound.py. music_hold uses groan() and settle() from here."""
VARIANTS = 4
PEAK = 0.8

SR = synth.SR
# (length of the shove, slip rates: start, peak, end in Hz, where it peaks (fraction), pitch scale)
SHOVES = [
    [(2.8, 5, 18, 6, 0.55, 1.0)],
    [(1.3, 6, 16, 7, 0.45, 1.05), (1.6, 5, 20, 5, 0.4, 0.95)],
    [(1.8, 7, 24, 10, 0.5, 1.1)],
    [(3.3, 3, 11, 3, 0.6, 0.78)],
]


def norm(x):
    return x / (np.max(np.abs(x)) + 1e-9)


def groan(d, start, top, end, peak_at, scale, rng):
    """One long shove of rock on rock: the slip quickens and slows as the weight shifts; each catch a dull, rough
    knock in broad low resonances (no ring); at the height of the push the slips run together into a low rough moan;
    a grinding hush and a deep rumble following the effort."""
    n = int(d * SR)
    u = np.linspace(0, 1, n)
    rate = np.where(u < peak_at, start + (top - start) * np.sin(np.clip(u / peak_at, 0, 1) * np.pi / 2),
                    end + (top - end) * np.cos(np.clip((u - peak_at) / (1 - peak_at), 0, 1) * np.pi / 2))
    rate *= 1 + 0.45 * synth.smooth_random(d, rng, 4.0, -1, 1)[:n]
    phase = np.cumsum(rate / SR)
    catches = np.diff(np.floor(phase), prepend=0.0) * rng.uniform(0.3, 1.0, n)
    # each catch a short burst of rough noise rather than a click: stone grinds, it doesn't snap
    burst = synth.lowpass(synth.white(0.03, rng), 900) * np.hanning(int(0.03 * SR))
    rough = synth.fft_convolve(catches, burst)[:n]
    rock = synth.bandpass(rough, 48 * scale, 2.0) + 0.9 * synth.bandpass(rough, 95 * scale, 2.2) \
        + 0.5 * synth.bandpass(rough, 185 * scale, 2.0) + 0.2 * synth.bandpass(rough, 370 * scale, 1.8)
    effort = (rate - rate.min()) / (rate.max() - rate.min() + 1e-9)
    # under the strongest push the slips run together into a low, rough moan: the groan itself
    moan_rate = rate * 4.2 * (1 + 0.08 * synth.smooth_random(d, rng, 25.0, -1, 1)[:n])
    pulses = np.diff(np.floor(np.cumsum(moan_rate / SR)), prepend=0.0) * rng.uniform(0.5, 1.0, n)
    moan = synth.bandpass(pulses, 70 * scale, 1.6) + 0.7 * synth.bandpass(pulses, 140 * scale, 1.8) \
        + 0.25 * synth.bandpass(pulses, 290 * scale, 1.8)
    moan = synth.lowpass(moan, 500) * effort ** 1.4
    grind = synth.bandpass(synth.white(d, rng), 260 * scale, 1.2) * (0.2 + 0.8 * effort) ** 1.5
    rumble = synth.lowpass(synth.lowpass(synth.white(d, rng), 70 * scale), 70 * scale) * (0.3 + 0.7 * effort)
    out = norm(rock) + 0.6 * norm(moan) + 0.35 * norm(grind) + 0.7 * norm(rumble)
    t = synth.seconds(d)
    fade = np.sin(np.clip(np.minimum(t / (d * 0.3), (d - t) / (d * 0.35)), 0, 1) * np.pi / 2) ** 2
    return out * (0.4 + 0.6 * effort) * fade


def settle(rng, d=1.4):
    """The weight let go: a single dull crack deep in the rock, then grit and small stones trickling down."""
    t = synth.seconds(d)
    crack = synth.lowpass(synth.white(d, rng), 1400) * np.exp(-t / 0.012) \
        + np.sin(2 * np.pi * rng.uniform(55, 70) * t) * np.exp(-t / 0.15) * 0.8
    crack *= np.clip(t / 0.002, 0, 1)
    trickle = np.zeros_like(t)
    for _ in range(rng.integers(14, 24)):
        at = 0.08 + rng.random() ** 1.6 * (d - 0.4)
        gd = 0.02
        g = synth.bandpass(synth.white(gd, rng), rng.uniform(900, 2600), 2.5) * synth.decay(gd, 0.003)
        synth.place(trickle, g * rng.uniform(0.2, 1.0) * (1 - at / d), at)
    for _ in range(rng.integers(1, 3)):  # a pebble or two, knocking down
        pd = 0.1
        pt = synth.seconds(pd)
        pebble = np.sin(2 * np.pi * rng.uniform(600, 1100) * pt) * np.exp(-pt / 0.01) * np.clip(pt / 0.0005, 0, 1)
        synth.place(trickle, pebble * rng.uniform(0.5, 0.9), rng.uniform(0.15, 0.6))
    return crack + 0.35 * norm(trickle)


def far(x, rng, length=3.5, wet=0.75):
    """Heard from far off in the vast stone cave: the highs gone, a long dark tail kept inside the sound."""
    x = synth.lowpass(synth.lowpass(x, 900), 1300)
    x = synth.highpass(x, 25)
    ir = synth.reverb_ir(length, length * 0.9, rng, predelay=0.035, brightness=0.14)[0]
    tail = synth.fft_convolve(x, ir)[:len(x)]
    return x * 0.55 + norm(tail) * np.max(np.abs(x)) * wet


def render(v, rng):
    total = [3.6, 3.8, 3.4, 4.0][v]
    out = np.zeros(int(total * SR))
    at = 0.05
    for d, start, top, end, peak_at, scale in SHOVES[v]:
        synth.place(out, groan(d, start, top, end, peak_at, scale * rng.uniform(0.95, 1.05), rng), at)
        at += d + 0.15  # a pause between shoves
    if v == 2:
        synth.place(out, settle(rng) * 1.3, at - 0.45)
    if v == 3:
        out = synth.lowpass(out, 400)
    out = far(out, rng)
    t = synth.seconds(total)
    return out * np.clip((total - t) / 0.6, 0, 1) ** 2  # the cave's tail let down, not cut
