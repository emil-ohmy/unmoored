"""The Anchoress: Boss music, a 64 s seamless loop in D minor at 60 bpm, in two variants that share one loop, so
the game crossfades between them in step. 1: her first phase. 2: her second (the floor breaking): the same music,
heavier. Built from the Drowned Dark's world (its choir, its drone, its chords) turned into a slow, grinding march.

Layers (both): a deep drum on beats 1 and 3, chain rattles on the off-beats, a low bowed bass pulsing on every
beat, the drone, and the ghost choir chanting Dm - Bb - Gm - A - Dm - F - Gm - A7(b9) (8 s each).
Added in 2: the drum on every beat with a doubled hit before each bar, faster chain, a high drowned wail over the
choir, and water surging up every other chord.
`synth` and `np` are injected by tools/audio/build_sound.py.
"""
LOOP = True
FOLDER = "Music"
PEAK = 0.8
VARIANTS = 2

LENGTH = 64.0
FADE = 4.0
BEAT = 1.0
CHORD_TIME = 8.0
CHORDS = [
    (["D3", "A3", "F4"], "oh", "D2"),
    (["Bb2", "F3", "D4"], "oo", "Bb1"),
    (["G2", "D3", "Bb3"], "oh", "G1"),
    (["A2", "E3", "C#4"], "ah", "A1"),
    (["D3", "A3", "F4"], "oh", "D2"),
    (["F2", "C3", "A3"], "oo", "F1"),
    (["G2", "D3", "Bb3"], "oh", "G1"),
    (["A2", "E3", "C#4", "Bb4"], "ah", "A1"),
]
WAIL = [("A4", "D5"), ("F4", "Bb4"), ("D5", "G4"), ("E5", "C#5"), ("A4", "F5"), ("C5", "A4"), ("Bb4", "D5"), ("C#5", "A4")]


def rms(x):
    return np.sqrt(np.mean(np.asarray(x) ** 2)) + 1e-12


def level(x, db):
    return x / rms(x) * 10 ** (db / 20)


def norm(x):
    return x / (np.max(np.abs(x)) + 1e-9)


def swell(duration, attack, release):
    t = synth.seconds(duration)
    return np.sin(np.minimum(np.clip(t / attack, 0, 1), np.clip((duration - t) / release, 0, 1)) * np.pi / 2) ** 2


def drum(rng, strength=1.0):
    d = 1.2
    body = synth.sine(synth.glide(95, 42, d, 0.4), d) * synth.decay(d, 0.35)
    skin = synth.lowpass(synth.white(0.08, rng), 900) * synth.decay(0.08, 0.02)
    out = norm(body) + 0.35 * np.pad(norm(skin), (0, len(body) - len(skin)))
    return out * strength


def chain(rng, duration, count):
    out = np.zeros(int(duration * synth.SR))
    for _ in range(count):
        d = rng.uniform(0.006, 0.014)
        f = rng.uniform(2000, 5000)
        clink = synth.sine(f, d, rng.uniform(0, 6)) * synth.decay(d, d / 3)
        synth.place(out, clink * rng.uniform(0.4, 1.0), rng.uniform(0, duration * 0.8))
    return out


def events(total, times, sound):
    out = np.zeros(int(total * synth.SR))
    for at in times:
        if at < total:
            synth.place(out, sound(at), at)
    return out


def bass(total, rng):
    """A low bowed string pulsing on every beat: the root of each chord."""
    out = np.zeros(int(total * synth.SR))
    for i in range(int(total / BEAT)):
        root = CHORDS[int(i * BEAT / CHORD_TIME) % len(CHORDS)][2]
        f = synth.note(root)
        d = 0.9
        t = synth.seconds(d)
        saw = sum(np.sin(2 * np.pi * f * k * t + k) / k for k in range(1, 9))
        note = synth.lowpass(saw, 500) * synth.envelope(d, 0.06, 0, curve=1.8)
        synth.place(out, note * (1.0 if i % 4 == 0 else 0.7), i * BEAT)
    return out


def drone(total, rng):
    t = synth.seconds(total)
    out = np.zeros_like(t)
    for freq, amp in ((36.71, 0.8), (73.42, 1.0), (110.0, 0.4)):
        for detune in (-0.15, 0.15):
            out += amp * np.sin(2 * np.pi * (freq + detune) * t + rng.uniform(0, 6))
    return out


def choir(total, rng):
    out = np.zeros((2, int(total * synth.SR)))
    duration = CHORD_TIME + 3.0
    for i in range(len(CHORDS) + 1):
        notes, vowel, _ = CHORDS[i % len(CHORDS)]
        start = max(0.0, i * CHORD_TIME - 1.5)
        env = swell(duration, 2.0, 3.0)
        for j, name in enumerate(notes):
            f = synth.note(name)
            sung = synth.voice(f, duration, rng, vowel, max_freq=3000) + 0.7 * synth.voice(f * 1.004, duration, rng, vowel, max_freq=3000)
            placed = synth.pan(sung * env / (1 + j * 0.2), -0.5 + j / max(1, len(notes) - 1))
            for ch in range(2):
                synth.place(out[ch], placed[ch], start)
    return out


def wail(total, rng):
    out = np.zeros((2, int(total * synth.SR)))
    for i in range(len(WAIL) + 1):
        a, b = WAIL[i % len(WAIL)]
        d = CHORD_TIME * 0.9
        pitch = synth.glide(synth.note(a), synth.note(b), d, 1.6)
        v = synth.voice(pitch, d, rng, "ah", vibrato=0.02, max_freq=3500) * swell(d, 1.5, 2.5)
        placed = synth.pan(v, 0.3 if i % 2 else -0.3)
        for ch in range(2):
            synth.place(out[ch], placed[ch], i * CHORD_TIME + 0.5)
    return out


def surges(total, rng):
    out = np.zeros(int(total * synth.SR))
    for i in range(0, int(total / CHORD_TIME) + 1, 2):
        d = 5.0
        rush = synth.bandpass(synth.white(d, rng), synth.glide(250, 900, d, 0.7), 0.8) * swell(d, 3.5, 1.2)
        synth.place(out, rush, i * CHORD_TIME + 3.0)
    return out


def render(v, rng):
    base = np.random.default_rng(20260929)  # the layers both variants share: identical, so they crossfade in step
    heavy = v >= 1  # (variants count from 0: the second is 1)
    total = LENGTH + FADE
    bars = np.arange(0, total, 4 * BEAT)
    dry = synth.pan(level(events(total, [b for b in bars] + [b + 2 * BEAT for b in bars], lambda at: drum(base)), -18), 0.0)
    dry += synth.pan(level(events(total, [b + 0.5 + k * BEAT for b in bars for k in range(4)], lambda at: chain(base, 0.3, 6)), -34), 0.3)
    dry += synth.pan(level(bass(total, base), -26), 0.0)
    dry += synth.pan(level(drone(total, base), -30), 0.0)
    wet = level(choir(total, base), -23)
    if heavy:
        extra = np.random.default_rng(7)
        dry += synth.pan(level(events(total, [b + k * BEAT for b in bars for k in (1, 3)] + [b + 3.75 for b in bars],
                                      lambda at: drum(extra, 0.7)), -21), 0.0)
        dry += synth.pan(level(events(total, [b + 0.25 + k * 0.5 for b in bars for k in range(8)], lambda at: chain(extra, 0.2, 4)), -33), -0.3)
        wet += level(wail(total, extra), -27)
        wet += synth.pan(level(surges(total, extra), -30), 0.0)

    dry = synth.loop_crossfade(dry, FADE)
    wet = synth.loop_crossfade(wet, FADE)
    n = wet.shape[1]
    hall = synth.reverb_ir(4.0, 3.5, np.random.default_rng(11))
    for ch in range(2):
        ir = np.zeros(n)
        ir[:len(hall[ch])] = hall[ch]
        reverberant = np.fft.irfft(np.fft.rfft(wet[ch]) * np.fft.rfft(ir), n)
        dry[ch] += 0.3 * wet[ch] + 0.8 * level(reverberant, 20 * np.log10(rms(wet[ch])))
    return dry
