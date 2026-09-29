"""A key item held up (an Ability found): a rising four-note motif in D, the minor third lifting to the major
(D - F - A - F#... resolving up to D) in glass and a warm low bell, a wordless choir swelling under it into a held
D major chord, and a shimmer at the top. The game's own, not borrowed: it rises the way the music rarely does."""


def render(v, rng):
    duration = 3.2
    out = np.zeros(int(duration * synth.SR))
    for at, name, amp in ((0.0, "D5", 0.5), (0.22, "F5", 0.45), (0.44, "A5", 0.5), (0.72, "F#5", 0.55), (1.0, "D6", 0.6)):
        synth.place(out, synth.glass(synth.note(name), duration - at, rng) * amp, at)
    bell = synth.glass(synth.note("D3"), duration, rng) + 0.5 * synth.glass(synth.note("A3"), duration, rng)
    synth.place(out, bell * 0.45, 0.0)
    chord_len = duration - 0.9
    choir = sum(synth.voice(synth.note(n), chord_len, rng, "ah", max_freq=3000) for n in ("D4", "F#4", "A4", "D5"))
    t = synth.seconds(chord_len)
    choir *= np.clip(t / 0.8, 0, 1) ** 1.5 * np.clip((chord_len - t) / 1.0, 0, 1)
    synth.place(out, choir / (np.max(np.abs(choir)) + 1e-9) * 0.35, 0.9)
    shimmer = synth.bandpass(synth.white(1.2, rng), 6000, 1.2) * synth.envelope(1.2, 0.3, 0, curve=2.0)
    synth.place(out, shimmer / (np.max(np.abs(shimmer)) + 1e-9) * 0.08, 1.0)
    hall = synth.reverb_ir(3.0, 2.6, rng, brightness=0.35)[0]
    wet = synth.fft_convolve(out, hall)[:len(out)]
    return out * 0.8 + wet / (np.max(np.abs(wet)) + 1e-9) * 0.3
