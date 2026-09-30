"""A soul beginning a line: a soft, wordless, breathy murmur ("hm", "mm-hm", a sigh with a voice in it), low and
old, far off in a little hall. No words: the text says them. 0.6-0.9 s. Variants differ in contour."""

VARIANTS = 4
CONTOURS = [  # (start, end) pitch in Hz, vowel, length in s
    (196, 174, "oo", 0.55),   # a falling hm
    (174, 207, "oh", 0.6),    # a rising, questioning mm
    (185, 165, "ah", 0.7),    # a sigh
    (165, 185, "oo", 0.5),    # a small hum
]


def render(v, rng):
    f_start, f_end, vowel, d = CONTOURS[v % len(CONTOURS)]
    total = d + 0.4
    out = np.zeros(int(total * synth.SR))
    # The voice: low, soft, a slow wavering; the breath of it: filtered air over the top.
    pitch = synth.glide(f_start, f_end, d, 0.8) * rng.uniform(0.97, 1.03)
    hum = synth.voice(pitch, d, rng, vowel=vowel, vibrato=0.012, max_freq=2800)
    hum = synth.lowpass(hum, 1600) * synth.envelope(d, 0.09, 0, curve=1.8, peak_at=0.35)
    air = synth.lowpass(synth.bandpass(synth.white(d, rng), synth.glide(900, 600, d), 1.4), 2200) * synth.envelope(d, 0.05, 0, curve=2.2, peak_at=0.3)
    synth.place(out, hum / (np.max(np.abs(hum)) + 1e-9) * 0.55, 0.0)
    synth.place(out, air / (np.max(np.abs(air)) + 1e-9) * 0.18, 0.0)
    hall = synth.reverb_ir(1.2, 0.9, rng, brightness=0.2)[0]
    wet = synth.fft_convolve(out, hall)[:len(out)]
    return out * 0.8 + wet / (np.max(np.abs(wet)) + 1e-9) * 0.22
