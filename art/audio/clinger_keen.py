"""A Clinger whose other half has just scattered: a thin, rising, wavering keen, a voice with no words in it, cut
off short as it turns on you (frantic now). 1.2 s. Sincere: grief, not a monster's roar."""
VARIANTS = 2


def render(v, rng):
    d = 1.2
    f = rng.uniform(420, 470)
    pitch = synth.glide(f, f * 1.6, d, 0.6) * (1 + 0.03 * np.sin(2 * np.pi * 6.5 * synth.seconds(d)))
    cry = synth.voice(pitch, d, rng, vowel="ah", vibrato=0.02, max_freq=4500)
    cry *= synth.envelope(d, 0.15, 0, curve=1.3, peak_at=0.75)
    cut = np.ones_like(cry)
    cut[int(0.92 * len(cut)):] = np.linspace(1, 0, len(cut) - int(0.92 * len(cut)))  # cut off short
    breath = synth.bandpass(synth.white(d, rng), 2500, 1.0) * synth.envelope(d, 0.1, 0, curve=1.5, peak_at=0.7)
    out = cry * cut / (np.max(np.abs(cry)) + 1e-9) * 0.6 + breath * 0.08
    hall = synth.reverb_ir(1.4, 1.1, rng, brightness=0.3)[0]
    wet = synth.fft_convolve(out, hall)[:len(out)]
    out = out * 0.8 + wet / (np.max(np.abs(wet)) + 1e-9) * 0.25
    n = int(0.08 * synth.SR)
    out[-n:] *= np.linspace(1, 0, n)
    return out
