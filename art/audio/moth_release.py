"""A Moth scattered (a memory let go): a dry puff of dust, and a single soft note, as from an old music box, a little
out of tune; each variant a different note of the same small tune (A minor), so a flock going up plays it."""
VARIANTS = 6
NOTES = ["A5", "C6", "D6", "E6", "G6", "A6"]


def render(v, rng):
    duration = 1.6
    out = np.zeros(int(duration * synth.SR))
    puff = synth.lowpass(synth.white(0.15, rng), 2600) * synth.decay(0.15, 0.04)
    synth.place(out, puff * 0.35, 0.0)
    f = synth.note(NOTES[v % len(NOTES)]) * rng.uniform(0.992, 1.006)
    tine = synth.glass(f, 1.4, rng) * 0.45 + synth.sine(f * 2.01, 1.4) * synth.decay(1.4, 0.25) * 0.08
    hall = synth.reverb_ir(1.4, 1.2, rng, brightness=0.35)[0]
    wet = synth.fft_convolve(tine, hall)[:len(tine)]
    synth.place(out, tine * 0.6 + wet / (np.max(np.abs(wet)) + 1e-9) * 0.2, 0.02)
    return out
