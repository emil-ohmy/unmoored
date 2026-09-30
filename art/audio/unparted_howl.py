"""Hold, when Fast is gone: a long, deep, anguished howl that cracks upward into a higher cry and breaks, with Fast's
own thin keen faintly inside it. 1.6 s. Grief, then fury: after it, Hold is berserk."""
VARIANTS = 2


def render(v, rng):
    d = 1.6
    t = synth.seconds(d)
    f = rng.uniform(70, 80)
    low = synth.voice(synth.glide(f, f * 1.9, d, 1.6), d, rng, vowel="ah", vibrato=0.03, max_freq=2600)
    low *= synth.envelope(d, 0.2, 0, curve=1.2, peak_at=0.7)
    crack = synth.voice(synth.glide(f * 4.5, f * 6.5, d, 0.8), d, rng, vowel="ah", vibrato=0.04, max_freq=5000)
    crack *= synth.envelope(d, 0.4, 0, curve=1.5, peak_at=0.8) * 0.3
    rasp = synth.bandpass(synth.white(d, rng), 900 + 300 * np.sin(2 * np.pi * 24 * t), 3) * synth.envelope(d, 0.3, 0, curve=1.5, peak_at=0.7)
    out = low / (np.max(np.abs(low)) + 1e-9) * 0.65 + crack + rasp / (np.max(np.abs(rasp)) + 1e-9) * 0.15
    hall = synth.reverb_ir(2.0, 1.6, rng, brightness=0.25)[0]
    wet = synth.fft_convolve(out, hall)[:len(out)]
    out = out * 0.75 + wet / (np.max(np.abs(wet)) + 1e-9) * 0.3
    n = int(0.12 * synth.SR)
    out[-n:] *= np.linspace(1, 0, n)
    return out / (np.max(np.abs(out)) + 1e-9) * 0.9
