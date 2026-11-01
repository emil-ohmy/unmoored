"""A great timber beam far above (the jetty's) groaning under the weight on it: slow, deep, distant. Stick-slip, so
slow you almost hear each catch, rung through the beam's low resonances; under it a deep groan of the wood as the
load shifts; heard through the dark: no highs, a long damp room after it. 2.5-4 s.
Variants: 1 a long low groan, 2 two strains with a breath between, 3 a short heavy one ending in a knock as something
settles, 4 the slowest, deepest.
`synth` and `np` are injected by tools/audio/build_sound.py."""
VARIANTS = 4
PEAK = 0.8

SR = synth.SR
# (length of the strain, slip rates: start, peak, end in Hz, where it peaks (fraction), timber pitch scale)
STRAINS = [
    [(2.6, 10, 34, 14, 0.55, 1.0)],
    [(1.2, 14, 30, 18, 0.5, 1.08), (1.5, 12, 38, 12, 0.4, 1.0)],
    [(1.7, 16, 42, 20, 0.45, 1.12)],
    [(3.0, 6, 22, 8, 0.6, 0.85)],
]


def strain(d, start, top, end, peak_at, scale, rng):
    n = int(d * SR)
    u = np.linspace(0, 1, n)
    rate = np.where(u < peak_at, start + (top - start) * np.sin(np.clip(u / peak_at, 0, 1) * np.pi / 2),
                    end + (top - end) * np.cos(np.clip((u - peak_at) / (1 - peak_at), 0, 1) * np.pi / 2))
    rate *= 1 + 0.3 * synth.smooth_random(d, rng, 5.0, -1.0, 1.0)[:n]
    phase = np.cumsum(rate / SR)
    clicks = np.diff(np.floor(phase), prepend=0.0) * rng.uniform(0.4, 1.0, n)
    wood = synth.bandpass(clicks, 120 * scale, 8.0) + 0.8 * synth.bandpass(clicks, 265 * scale, 7.0) \
        + 0.35 * synth.bandpass(clicks, 540 * scale, 6.0) + 0.12 * synth.bandpass(clicks, 1100 * scale, 5.0)
    loud = (rate - rate.min()) / (rate.max() - rate.min() + 1e-9)
    groan = synth.bandpass(synth.white(d, rng), 80 * scale, 3.0) * (0.3 + 0.7 * loud)
    out = wood / (np.max(np.abs(wood)) + 1e-9) + 0.5 * groan / (np.max(np.abs(groan)) + 1e-9)
    t = synth.seconds(d)
    fade = np.sin(np.clip(np.minimum(t / (d * 0.3), (d - t) / (d * 0.35)), 0, 1) * np.pi / 2) ** 2
    return out * (0.35 + 0.65 * loud) * fade


def settle(rng):
    """Something shifting into place as the strain lets go: a dull wooden knock and a whisper of grit falling."""
    d = 0.8
    t = synth.seconds(d)
    knock = np.sin(2 * np.pi * 85 * t) * np.exp(-t / 0.12) + 0.35 * np.sin(2 * np.pi * 190 * t) * np.exp(-t / 0.05)
    knock *= np.clip(t / 0.003, 0, 1)
    grit = synth.bandpass(synth.white(d, rng), 2500, 1.0) * synth.envelope(d, 0, 0, curve=2.0, peak_at=0.1) * 0.08
    return knock + grit


def render(v, rng):
    total = [3.8, 3.6, 3.2, 4.0][v]
    out = np.zeros(int(total * SR))
    at = 0.05
    for d, start, top, end, peak_at, scale in STRAINS[v]:
        s = strain(d, start, top, end, peak_at, scale * rng.uniform(0.95, 1.05), rng)
        synth.place(out, s, at)
        at += d - 0.25 + 0.35  # a breath between strains
    if v == 2:
        synth.place(out, settle(rng) * 0.6, at - 0.25)
    out = synth.lowpass(synth.lowpass(out, 1100), 1600)  # far above: no highs
    out = synth.highpass(out, 35)
    ir = synth.reverb_ir(2.5, 2.2, rng, predelay=0.02, brightness=0.15)[0]
    wet = synth.fft_convolve(out, ir)[:len(out)]
    wet = wet / (np.max(np.abs(wet)) + 1e-9) * np.max(np.abs(out))
    mix = out * 0.6 + wet * 0.6
    t = synth.seconds(total)
    return mix * np.clip((total - t) / 0.6, 0, 1) ** 2  # the room's tail let down, not cut
