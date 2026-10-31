"""The slam: the oar driven down into the stone from the leap. A deep, heavy impact (a sub drop), the blade's crack on
stone, a scatter of grit and rubble, and the ring of force going out from it: a silvery shimmer swelling and gone,
like the Stroke's wave (the oar's own)."""
VARIANTS = 2
PEAK = 0.9


def render(v, rng):
    duration = 1.4
    out = np.zeros(int(duration * synth.SR))
    boom = synth.sine(synth.glide(75 + 8 * v, 32, 0.5, 0.7), 0.5) * synth.decay(0.5, 0.16)
    synth.place(out, boom, 0.0)
    crack = synth.bandpass(synth.white(0.08, rng), 1800, 0.9) * synth.decay(0.08, 0.018)
    synth.place(out, crack / (np.max(np.abs(crack)) + 1e-9) * 0.75, 0.0)
    body = synth.lowpass(synth.brown(0.3, rng), 500) * synth.decay(0.3, 0.07)
    synth.place(out, body / (np.max(np.abs(body)) + 1e-9) * 0.5, 0.0)
    for _ in range(14):
        d = rng.uniform(0.01, 0.03)
        g = synth.bandpass(synth.white(d, rng), rng.uniform(1500, 5000), 2.5) * synth.decay(d, d / 4)
        synth.place(out, g * rng.uniform(0.1, 0.3), rng.uniform(0.03, 0.4))
    d = 0.9
    t = synth.seconds(d)
    shimmer = sum(np.sin(2 * np.pi * f * t + rng.uniform(0, 6)) for f in (synth.note("E5"), synth.note("B5"), synth.note("E6") * 1.003))
    shimmer *= synth.envelope(d, 0.12, 0, curve=3.0, peak_at=0.18) * (1 + 0.3 * np.sin(2 * np.pi * 9 * t))
    synth.place(out, shimmer / (np.max(np.abs(shimmer)) + 1e-9) * 0.16, 0.04)
    hall = synth.reverb_ir(1.2, 0.9, rng, brightness=0.3)[0]
    wet = synth.fft_convolve(out, hall)[:len(out)]
    out = out * 0.8 + wet / (np.max(np.abs(wet)) + 1e-9) * 0.25
    fade = int(0.05 * synth.SR)
    out[-fade:] *= np.linspace(1, 0, fade)
    return out
