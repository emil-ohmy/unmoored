"""What a Clinger whispers while it clings to your back, barely there: "don't go", "stay", "please". Breath shaped
into the words' vowels and consonants (no pitch: a whisper), close to the ear. Sincere, never a joke.
Variants: 1 "don't go", 2 "stay", 3 "please", 4 "don't go" (softer)."""
VARIANTS = 4

# Whisper vowel formants (Hz): the resonances that make breath sound like a vowel.
VOWELS = {"oh": (500, 850, 2400), "ay": (450, 1900, 2600), "ee": (300, 2300, 3000), "o": (570, 840, 2410)}


def vowel(d, name, rng):
    out = np.zeros(int(d * synth.SR))
    for f, gain in zip(VOWELS[name], (1.0, 0.6, 0.25)):
        out += synth.bandpass(synth.white(d, rng), f, 6.0) * gain
    return out * synth.envelope(d, 0.04, 0, curve=1.5, peak_at=0.35)


def consonant(d, low, high, rng):
    return synth.bandpass(synth.white(d, rng), (low + high) / 2, 2.0) * synth.envelope(d, 0.005, 0, curve=2.5)


def render(v, rng):
    total = 1.0
    out = np.zeros(int(total * synth.SR))
    t = 0.02
    words = {0: [("c", 0.04, 1500, 3000), ("v", 0.22, "oh"), ("c", 0.03, 2500, 5000), ("gap", 0.06), ("c", 0.03, 800, 2000), ("v", 0.3, "o")],
             1: [("c", 0.16, 5000, 9000), ("c", 0.03, 2500, 5000), ("v", 0.36, "ay")],
             2: [("c", 0.03, 700, 1800), ("c", 0.05, 2000, 4000), ("v", 0.26, "ee"), ("c", 0.14, 4000, 8000)],
             3: [("c", 0.04, 1500, 3000), ("v", 0.2, "oh"), ("c", 0.03, 2500, 5000), ("gap", 0.08), ("c", 0.03, 800, 2000), ("v", 0.26, "o")]}[v]
    for piece in words:
        if piece[0] == "gap":
            t += piece[1]
            continue
        if piece[0] == "c":
            sound = consonant(piece[1], piece[2], piece[3], rng) * 0.5
        else:
            sound = vowel(piece[1], piece[2], rng)
        synth.place(out, sound / (np.max(np.abs(sound)) + 1e-9) * (0.7 if piece[0] == "v" else 0.45), t)
        t += piece[1] * 0.9
    out = synth.lowpass(out, 6000)
    return out * (0.55 if v == 3 else 0.8)
