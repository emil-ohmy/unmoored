"""The Hold drawing a breath: the cave's walls (the souls kept there) breathing in, 3.5 s. Not a person: a mountain
asleep. Air drawn in through countless cracks (a broad dark hush that brightens as the draw quickens, and, at its
height, the faintest whistles of a few dozen cracks, each one beginning to sing only once the flow is strong enough
for it); the rock's mass in it (a deep rumble and a sub tone of the whole cave, like a vast bottle, swelling with
the flow; a slow stony strain far inside); a little grit loosened. No attack: the flow eases up from nothing,
peaks late and turns, falling back to nothing at the very end, so it runs straight into hold_breath_out (which
shares this code: breath(kind, v, rng)). Mono, quiet.
`synth` and `np` are injected by tools/audio/build_sound.py."""
VARIANTS = 2
PEAK = 0.5  # quiet: the game plays these under everything

SR = synth.SR
IN, OUT = 3.5, 4.5


def flow_curve(d, peak_at, softness=1.6):
    """The breath's flow: 0 at both ends (with zero slope), one smooth hump peaking at `peak_at` (fraction)."""
    u = np.linspace(0, 1, int(d * SR))
    p = np.log(0.5) / np.log(peak_at)
    return np.sin(np.pi * u ** p) ** softness


def at_frames(curve, times):
    return np.interp(times * SR, np.arange(len(curve)), curve)


def norm(x):
    return x / (np.max(np.abs(x)) + 1e-9)


def hush(d, flow, inward, rng):
    """Air through the cracks, all of them at once: a broad dark noise whose centre rises with the flow (higher on
    the draw, lower and softer on the letting go); a thin hiss only at the height."""
    def mag(times, freqs):
        fl = at_frames(flow, times)[:, None]
        f = np.maximum(freqs[None, :], 1)
        centre = (380 + (850 if inward else 420) * fl)
        body = np.exp(-0.5 * (np.log(f / centre) / 1.05) ** 2)
        hiss = 0.12 * fl ** 2 * np.exp(-0.5 * (np.log(f / (2600 if inward else 1800)) / 0.6) ** 2)
        low_cut = (f / 60) ** 2 / (1 + (f / 60) ** 2)
        return (fl ** 1.2 * body + hiss) * low_cut / (1 + (f / 4500) ** 4)
    return synth.spectral_noise(d, rng, mag, n_fft=2048, hop=256)


def cracks(d, flow, inward, rng, count=34):
    """The cracks singing: narrow whistles at scattered pitches, each waking only above its own share of the flow
    (the narrow ones need the strongest draw), drifting up as the air quickens. Very faint: a texture, not tones."""
    freqs_k = np.exp(rng.uniform(np.log(260), np.log(2600), count))
    thresholds = rng.uniform(0.35, 0.85, count)
    amps = rng.uniform(0.2, 1.0, count) ** 2 * (freqs_k / 600) ** -0.4

    def mag(times, freqs):
        fl = at_frames(flow, times)[:, None]
        out = np.zeros((len(times), len(freqs)))
        for fk, th, a in zip(freqs_k, thresholds, amps):
            awake = np.clip((fl - th) / (1 - th), 0, 1) ** 1.5
            centre = fk * (1 + (0.05 if inward else 0.035) * fl)
            width = max(fk * 0.012, 6.0)
            out += a * awake * np.exp(-0.5 * ((freqs[None, :] - centre) / width) ** 2)
        return out
    return synth.spectral_noise(d, rng, mag, n_fft=8192, hop=512)


def mass(d, flow, inward, rng):
    """The rock's weight in it: a deep rumble under the air, and the whole cave's own low tone (like a vast
    bottle blown across) swelling with the flow, bending a little with it; its octave faint, for small speakers."""
    def mag(times, freqs):
        f = np.maximum(freqs[None, :], 1)
        fl = at_frames(flow, times)[:, None]
        return fl * (f / 22) ** 2 / (1 + (f / 22) ** 2) / (1 + (f / 85) ** 4)
    rumble = synth.spectral_noise(d, rng, mag, n_fft=4096, hop=512)
    f0 = rng.uniform(31, 35) * (1 + (0.06 if inward else -0.05) * flow)
    t = synth.seconds(d)
    tone = synth.sine(f0, d) + 0.25 * synth.sine(2 * f0, d, 1.0) + 0.08 * synth.sine(3.01 * f0, d, 2.0)
    tone *= flow ** 1.3 * (1 + 0.15 * synth.smooth_random(d, rng, 1.5, -1, 1))
    return norm(rumble) + 0.55 * norm(tone)


def strain(d, flow, rng):
    """Deep inside, the stone easing under the swell: a very slow, soft stick-slip rung low (no creak of wood:
    low, broad resonances), only where the flow is strong."""
    n = int(d * SR)
    rate = (4 + 9 * flow) * (1 + 0.4 * synth.smooth_random(d, rng, 3.0, -1, 1)[:n])
    phase = np.cumsum(rate / SR)
    clicks = np.diff(np.floor(phase), prepend=0.0) * rng.uniform(0.3, 1.0, n)
    rock = synth.bandpass(clicks, rng.uniform(70, 85), 2.5) + 0.6 * synth.bandpass(clicks, rng.uniform(150, 175), 2.0)
    return norm(synth.lowpass(rock, 300)) * flow ** 2


def grit(d, flow, rng):
    """A few grains loosened by the air and trickling down, here and there: tiny dull ticks, sparse."""
    out = np.zeros(int(d * SR))
    for _ in range(rng.integers(5, 10)):
        at = rng.uniform(0.2, d - 0.3)
        fl = flow[int(at * SR)]
        g = synth.bandpass(synth.white(0.01, rng), rng.uniform(1500, 3500), 2.0) * synth.decay(0.01, 0.002)
        synth.place(out, g * rng.uniform(0.3, 1.0) * fl, at)
    return synth.lowpass(out, 4000)


def cave(x, rng, length=1.6):
    """The breath heard in the cave itself: a short dark stone tail, kept within the sound's own length."""
    ir = synth.reverb_ir(length, length * 0.9, rng, predelay=0.02, brightness=0.18)[0]
    wet = synth.fft_convolve(x, ir)[:len(x)]
    return x * 0.75 + wet / (np.max(np.abs(wet)) + 1e-9) * np.max(np.abs(x)) * 0.5


def breath(kind, v, rng):
    """kind 'in' (3.5 s, the flow peaks late, at about 62%) or 'out' (4.5 s, it peaks early, about 30%, and sinks
    away slowly). v 1 is a shade deeper and darker than v 0."""
    inward = kind == "in"
    d = IN if inward else OUT
    flow = flow_curve(d, rng.uniform(0.6, 0.65) if inward else rng.uniform(0.28, 0.33), 1.6 if inward else 1.8)
    dark = 1.0 if v == 0 else 0.8
    air = norm(hush(d, flow, inward, rng))
    if v == 1:
        air = synth.lowpass(air, 1400)
    sing = norm(cracks(d, flow, inward, rng, 34 if v == 0 else 26))
    out = air * (1.0 if inward else 0.85) + sing * 0.07 * dark + norm(mass(d, flow, inward, rng)) * 0.55 \
        + strain(d, flow, rng) * (0.12 if inward else 0.18) + norm(grit(d, flow, rng)) * 0.04
    out = synth.highpass(out, 18)
    out = cave(out, rng)
    # the edges let down to nothing, smoothly: no attack, no cut tail
    t = synth.seconds(d)
    edge = np.sin(np.clip(np.minimum(t / 0.35, (d - t) / 0.35), 0, 1) * np.pi / 2) ** 2
    return out * edge


def render(v, rng):
    return breath("in", v, rng)
