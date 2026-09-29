"""The Anchoress's Undertow: she kneels and prays, and sour water wells up round her. A low, drowned murmur of a
prayer (a wordless voice, muffled as if under water) over a swelling rush of water welling up, deep churning, and
bubbles breaking thicker and thicker; it all ebbs as she rises. 4 s: the kneel (0.9 s) and the prayer (2.6 s)."""


def render(v, rng):
    duration = 4.4
    out = np.zeros(int(duration * synth.SR))
    # The prayer: two low voices a fifth apart, muffled, rising and falling like murmured words.
    murmur = sum(synth.voice(synth.glide(f, f * 0.94, 3.6, 1.0), 3.6, rng, vowel="oo", vibrato=0.012) * a
                 for f, a in ((98.0, 0.6), (146.8, 0.35)))
    murmur *= 0.55 + 0.45 * synth.smooth_random(3.6, rng, rate=2.5)
    murmur = synth.lowpass(murmur, 900) * synth.envelope(3.6, 0.6, 0, curve=1.4, peak_at=2.4)
    # The water welling up: a rush and a deep churn, swelling to the full pool.
    rush = synth.bandpass(synth.white(3.8, rng), synth.glide(300, 900, 3.8, 0.8), 0.8) * synth.envelope(3.8, 0.8, 0, curve=1.5, peak_at=2.8)
    churn = synth.lowpass(synth.brown(3.8, rng), 180) * (0.6 + 0.4 * synth.smooth_random(3.8, rng, rate=5)) * synth.envelope(3.8, 0.8, 0, curve=1.5, peak_at=2.8)
    for part, gain, at in ((murmur, 0.55, 0.1), (rush, 0.3, 0.5), (churn, 0.55, 0.5)):
        synth.place(out, part / (np.max(np.abs(part)) + 1e-9) * gain, at)
    # Bubbles breaking, thicker as the water rises.
    for _ in range(90):
        at = 0.6 + 3.2 * rng.random() ** 0.7
        d = rng.uniform(0.03, 0.08)
        f0 = rng.uniform(250, 700)
        bubble = synth.sine(synth.glide(f0, f0 * 1.8, d, 0.6), d) * synth.envelope(d, 0.004, 0)
        synth.place(out, bubble * rng.uniform(0.04, 0.12), at)
    hall = synth.reverb_ir(2.0, 1.6, rng, brightness=0.25)[0]
    wet = synth.fft_convolve(out, hall)[:len(out)]
    return out * 0.8 + wet / (np.max(np.abs(wet)) + 1e-9) * 0.3
