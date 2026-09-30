"""Hold's tell as it winds up to strike: a deep, hollow groan with a breath through it, sliding down and then up, like
effort and grief at once. ~0.9 s (a sweep's wind-up; the slam's is longer and uses the same). Never loud."""
VARIANTS = 3


def render(v, rng):
    d = 1.0
    f = rng.uniform(78, 92)
    pitch = synth.glide(f, f * 0.82, d * 0.6, 0.7)
    pitch = np.concatenate([pitch, synth.glide(f * 0.82, f * 1.05, d * 0.4, 0.5)])[:int(d * synth.SR)]
    moan = synth.voice(pitch, d, rng, vowel="oh", vibrato=0.015, max_freq=2200)
    moan *= synth.envelope(d, 0.12, 0, curve=1.4, peak_at=0.6)
    breath = synth.lowpass(synth.bandpass(synth.white(d, rng), 500, 0.9), 1400) * synth.envelope(d, 0.1, 0, curve=1.5, peak_at=0.5)
    out = moan / (np.max(np.abs(moan)) + 1e-9) * 0.7 + breath / (np.max(np.abs(breath)) + 1e-9) * 0.2
    hall = synth.reverb_ir(1.3, 1.0, rng, brightness=0.2)[0]
    wet = synth.fft_convolve(out, hall)[:len(out)]
    out = out * 0.8 + wet / (np.max(np.abs(wet)) + 1e-9) * 0.25
    n = int(0.08 * synth.SR)
    out[-n:] *= np.linspace(1, 0, n)
    return out
