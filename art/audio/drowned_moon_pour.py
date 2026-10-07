"""The Drowned Moon pouring out of Father Moth: his rib-cage cracking (a dry snap, a glassy split), then the moonlight
running out of it like water, a thin burbling stream onto the stone that spreads and slows; over it, cold glass tones
in A minor coming in one by one as the pool fills, ringing on in the glasshouse after the stream stops."""
VARIANTS = 1


def render(v, rng):
    duration = 4.6
    out = np.zeros(int(duration * synth.SR))
    # The cage cracking: a dry snap and a split of glass.
    snap = synth.bandpass(synth.white(0.12, rng), 1800, 3.0) * synth.decay(0.12, 0.018)
    synth.place(out, snap / (np.max(np.abs(snap)) + 1e-9) * 0.55, 0.0)
    split = synth.highpass(synth.white(0.3, rng), 4200) * synth.decay(0.3, 0.05)
    synth.place(out, split / (np.max(np.abs(split)) + 1e-9) * 0.3, 0.01)
    for _ in range(5):
        tink = synth.sine(rng.uniform(4200, 6500), 0.08) * synth.decay(0.08, 0.015)
        synth.place(out, tink * rng.uniform(0.06, 0.14), rng.uniform(0.02, 0.25))
    # The stream: water-like noise in a narrow band, burbling (fast, uneven swells), falling and slowing as it pools.
    pour_len = 2.6
    stream = synth.bandpass(synth.white(pour_len, rng), 1400, 1.6)
    burble = synth.smooth_random(pour_len, rng, rate=14.0, low=0.25, high=1.0) ** 2
    body = synth.lowpass(synth.white(pour_len, rng), 600) * 0.6
    shape = synth.envelope(pour_len, 0.25, 0, curve=1.6)
    pour = (stream * burble + body * synth.smooth_random(pour_len, rng, rate=5.0, low=0.4, high=1.0)) * shape
    synth.place(out, pour / (np.max(np.abs(pour)) + 1e-9) * 0.42, 0.18)
    # Cold glass as it fills: A, E, C, A, one after another, ringing on.
    for i, (name, at) in enumerate((("A5", 0.35), ("E6", 0.9), ("C6", 1.5), ("A6", 2.2))):
        tone = synth.glass(synth.note(name), duration - at, rng)
        synth.place(out, tone * (0.16 - 0.02 * i), at)
    ir_l, _ = synth.reverb_ir(2.4, 2.2, rng, brightness=0.5)
    wet = synth.fft_convolve(out, ir_l)[:len(out)]
    return out * 0.75 + wet * 0.45
