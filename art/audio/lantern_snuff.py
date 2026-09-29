"""The Apprentice's bow lantern going out, as they fall: a flare (a bright breath of air), then the flame fluttering
faster and fainter, guttering, and a last soft puff. Under it a deep, cold swell that sinks away. 2.4 s: the
lantern's dying in the death sequence, before the phrase comes up."""


def render(v, rng):
    duration = 2.6
    out = np.zeros(int(duration * synth.SR))
    t = np.arange(int(1.9 * synth.SR)) / synth.SR
    # The flame: breathy noise, its flutter quickening and growing ragged as it dies.
    flutter_rate = 6 + 18 * (t / t[-1]) ** 1.5
    flutter = 0.55 + 0.45 * np.sin(2 * np.pi * np.cumsum(flutter_rate) / synth.SR + synth.smooth_random(1.9, rng, rate=8, low=-2, high=2))
    flame = synth.bandpass(synth.white(1.9, rng), synth.glide(1800, 500, 1.9, 0.7), 1.3) * flutter
    flame *= synth.envelope(1.9, 0.04, 0, curve=1.6)
    flare = synth.bandpass(synth.white(0.35, rng), synth.glide(900, 2600, 0.35, 0.6), 1.0) * synth.envelope(0.35, 0.02, 0, curve=3)
    puff = synth.lowpass(synth.white(0.3, rng), 900) * synth.envelope(0.3, 0.01, 0, curve=3)
    # The cold swell under it: two low sines sinking, a fifth apart.
    swell = sum(synth.sine(synth.glide(f, f * 0.7, 2.4, 0.8), 2.4) * a for f, a in ((73.4, 0.6), (110.0, 0.3)))
    swell *= synth.envelope(2.4, 0.5, 0, curve=2.2, peak_at=0.7)
    for part, gain, at in ((flare, 0.5, 0.0), (flame, 0.55, 0.12), (puff, 0.4, 1.95), (swell, 0.5, 0.0)):
        synth.place(out, part / (np.max(np.abs(part)) + 1e-9) * gain, at)
    hall = synth.reverb_ir(2.2, 1.8, rng, brightness=0.25)[0]
    wet = synth.fft_convolve(out, hall)[:len(out)]
    return out * 0.8 + wet / (np.max(np.abs(wet)) + 1e-9) * 0.3
