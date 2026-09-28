"""The Drowned Dark: ambient music for the Drowned Village. An 80 s seamless loop in D minor.

Layers: wind (a howl whose resonances wander and swell in gusts, plus a thin cold hiss), a low beating drone,
a wordless ghost choir drifting through Dm - Bb - Gm - A (the A's C# pulls back to D, so the loop resolves),
sparse glass notes that lean on that C#, and faint distant wails, all in a large, dark hall reverb.
The reverb is applied circularly over the loop, so the tail of the end already rings at the start.
`synth` and `np` are injected by tools/audio/build_sound.py.
"""
LOOP = True
FOLDER = "Music"
PEAK = 0.8

LENGTH = 80.0  # the loop
FADE = 6.0     # extra material crossfaded into the start
CHORD_TIME = 10.0
CHORDS = [  # (notes, vowel)
    (["D3", "A3", "F4", "E5"], "oo"),   # Dm(add9)
    (["Bb2", "F3", "D4", "A4"], "oh"),  # Bbmaj7
    (["G2", "D3", "Bb3", "E4"], "oo"),  # Gm6
    (["A2", "E3", "C#4", "G4"], "ah"),  # A7: the leading tone pulls back to D
    (["D3", "A3", "F4", "D5"], "oo"),   # Dm
    (["F2", "C4", "A3", "E4"], "oh"),   # Fmaj7
    (["G2", "D3", "Bb3", "D4"], "oo"),  # Gm
    (["A2", "E3", "C#4", "Bb4"], "ah"),  # A7(b9): the ghastliest moment, just before the loop comes round
]
GLASS = [(4.0, "A5"), (5.4, "F5"), (6.9, "E5"),
         (22.0, "D5"), (23.3, "C#5"), (24.7, "A4"),
         (44.0, "F5"), (45.5, "E5"), (46.8, "D5"), (48.4, "C#5"),
         (63.0, "A5"), (64.6, "Bb5"), (66.3, "A5")]
WAILS = [(13.0, 0.7, ("A3", "E4", "D3")), (37.0, -0.6, ("D4", "F4", "C#4")),
         (58.0, 0.4, ("F3", "A3", "E3")), (72.5, -0.3, ("A3", "Bb3", "A3"))]


def rms(x):
    return np.sqrt(np.mean(np.asarray(x) ** 2)) + 1e-12


def level(x, db):
    """Scale to an RMS level in dBFS."""
    return x / rms(x) * 10 ** (db / 20)


def swell(duration, attack, release):
    t = synth.seconds(duration)
    up = np.clip(t / attack, 0, 1)
    down = np.clip((duration - t) / release, 0, 1)
    return np.sin(np.minimum(up, down) * np.pi / 2) ** 2


def wind(total, rng):
    n = int(total * synth.SR)
    gust = synth.smooth_random(total, rng, 0.15, 0.2, 1.0) ** 2
    centre = synth.smooth_random(total, rng, 0.12, np.log(260), np.log(720))

    def frame(times):
        return np.clip((times * synth.SR).astype(int), 0, n - 1)

    def howl(times, freqs):
        i = frame(times)
        lf = np.log(np.maximum(freqs, 1.0))[None, :]
        c = centre[i][:, None]
        band = np.exp(-0.5 * ((lf - c) / 0.12) ** 2) + 0.5 * np.exp(-0.5 * ((lf - c - np.log(1.9)) / 0.1) ** 2)
        rumble = 0.18 * np.exp(-0.5 * ((lf - np.log(90)) / 0.6) ** 2)
        return gust[i][:, None] * (band + rumble)

    def hiss(times, freqs):
        i = frame(times)
        lf = np.log(np.maximum(freqs, 1.0))[None, :]
        return (0.25 + 0.75 * gust[i][:, None]) * 0.12 * np.exp(-0.5 * ((lf - np.log(4000)) / 0.5) ** 2)

    return synth.spectral_noise(total, rng, howl) + synth.spectral_noise(total, rng, hiss)


def drone(total, rng):
    t = synth.seconds(total)
    out = np.zeros_like(t)
    for freq, amp in ((73.42, 1.0), (110.0, 0.5), (36.71, 0.6)):  # D2, A2, D1
        for detune in (-0.12, 0.12):  # two copies a hair apart: a slow, uneasy beating
            for k, a in ((1, 1.0), (2, 0.35), (3, 0.15)):
                out += amp * a * np.sin(2 * np.pi * (freq + detune) * k * t + rng.uniform(0, 6))
    return out * (0.75 + 0.25 * synth.smooth_random(total, rng, 0.1))


def choir(total, rng):
    out = np.zeros((2, int(total * synth.SR)))
    duration = CHORD_TIME + 4.0
    for i in range(len(CHORDS) + 1):  # one extra: the first chord again, so the end matches the start
        notes, vowel = CHORDS[i % len(CHORDS)]
        start = max(0.0, i * CHORD_TIME - 2.0)
        env = swell(duration, 3.5, 4.5)
        for j, name in enumerate(notes):
            f = synth.note(name)
            sung = synth.voice(f, duration, rng, vowel, max_freq=3500)
            sung += 0.7 * synth.voice(f * 1.003, duration, rng, vowel, max_freq=3500)  # a second singer, a little sharp
            position = -0.6 + 1.2 * j / (len(notes) - 1)
            placed = synth.pan(sung * env / (1 + j * 0.15), position)
            for ch in range(2):
                synth.place(out[ch], placed[ch], start)
    return out


def glass_notes(total, rng):
    out = np.zeros((2, int(total * synth.SR)))
    for time, name in GLASS + [(t + LENGTH, n) for t, n in GLASS if t < FADE]:
        tone = synth.pan(synth.glass(synth.note(name), 6.0, rng), rng.uniform(-0.5, 0.5))
        for ch in range(2):
            synth.place(out[ch], tone[ch], time)
    return out


def wails(total, rng):
    out = np.zeros((2, int(total * synth.SR)))
    for time, position, (a, b, c) in WAILS:
        duration = 5.0
        pitch = np.concatenate([synth.glide(synth.note(a), synth.note(b), duration / 2, 1.5),
                                synth.glide(synth.note(b), synth.note(c), duration / 2, 0.7)])
        wail = synth.voice(pitch, duration, rng, "oo", vibrato=0.015, max_freq=2500) * swell(duration, 2.0, 2.5)
        placed = synth.pan(wail, position)
        for ch in range(2):
            synth.place(out[ch], placed[ch], time)
    return out


def render(v, rng):
    total = LENGTH + FADE
    dry = synth.stereo(level(wind(total, rng), -21), level(wind(total, rng), -21))
    dry += synth.pan(level(drone(total, rng), -29), 0.0)
    wet = level(choir(total, rng), -24) + level(wails(total, rng), -33)
    glass = glass_notes(total, rng)
    wet += glass / (np.max(np.abs(glass)) + 1e-9) * 10 ** (-12 / 20)

    dry = synth.loop_crossfade(dry, FADE)
    wet = synth.loop_crossfade(wet, FADE)
    # Circular reverb: convolve over exactly one loop length so the tail wraps round into the start.
    n = wet.shape[1]
    hall = synth.reverb_ir(6.0, 5.5, rng)
    for ch in range(2):
        ir = np.zeros(n)
        ir[:len(hall[ch])] = hall[ch]
        reverberant = np.fft.irfft(np.fft.rfft(wet[ch]) * np.fft.rfft(ir), n)
        dry[ch] += 0.25 * wet[ch] + 0.9 * level(reverberant, 20 * np.log10(rms(wet[ch])))
    return dry
