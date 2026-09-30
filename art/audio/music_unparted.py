"""The Unparted: miniboss music, a 40 s seamless loop in F minor, a slow waltz (3/4 at 72 bpm: a bar is 2.5 s), in two
variants that share one loop so the game crossfades them in step. 1: both halves fighting. 2: one of them scattered:
the same music, torn (a quicker pulse, a keening wail over it, the heartbeat doubled).

A duet: a low, heavy voice (Hold) and a thin high one (Fast) circling each other, one answering the other a beat late,
never quite together; a plucked bass on each bar's first beat, soft brushed ticks on two and three, a heartbeat drum,
and the Drowned City's drone under it all. Chords: Fm - Db - Bbm - C - Fm - Ab - Db - C7 (two bars each).
`synth` and `np` are injected by tools/audio/build_sound.py.
"""
LOOP = True
FOLDER = "Music"
PEAK = 0.8
VARIANTS = 2

BAR = 2.5
BEAT = BAR / 3
CHORD_TIME = 2 * BAR
LENGTH = 8 * CHORD_TIME   # 40 s
FADE = 4.0
CHORDS = [
    (["F3", "Ab3", "C4"], "F2"), (["Db3", "F3", "Ab3"], "Db2"), (["Bb2", "Db3", "F3"], "Bb1"), (["C3", "E3", "G3"], "C2"),
    (["F3", "Ab3", "C4"], "F2"), (["Ab2", "C3", "Eb3"], "Ab1"), (["Db3", "F3", "Ab3"], "Db2"), (["C3", "E3", "Bb3"], "C2"),
]
# The duet, one note per bar (two per chord): Hold low, Fast high, a beat behind it.
HOLD = ["F3", "Eb3", "Db3", "F3", "Bb2", "Db3", "C3", "E3", "F3", "G3", "Ab3", "Eb3", "Db3", "C3", "Bb2", "C3"]
FAST = ["C5", "Ab4", "Ab4", "F4", "F4", "Db5", "G4", "E4", "C5", "Eb5", "C5", "Ab4", "F4", "Ab4", "E4", "G4"]


def rms(x):
    return np.sqrt(np.mean(np.asarray(x) ** 2)) + 1e-12


def level(x, db):
    return x / rms(x) * 10 ** (db / 20)


def swell(duration, attack, release):
    t = synth.seconds(duration)
    return np.sin(np.minimum(np.clip(t / attack, 0, 1), np.clip((duration - t) / release, 0, 1)) * np.pi / 2) ** 2


def events(total, times, sound):
    out = np.zeros(int(total * synth.SR))
    for at in times:
        if at < total:
            synth.place(out, sound(at), at)
    return out


def tail(x, share=0.3):
    """Fades the end of a sound to nothing, so it never stops with a click."""
    n = int(len(x) * share)
    x = np.array(x, dtype=float)
    x[-n:] *= np.linspace(1, 0, n) ** 2
    return x


def heartbeat(rng, strength=1.0):
    d = 0.9
    lub = tail(synth.sine(synth.glide(70, 40, 0.4, 0.5), 0.4) * synth.decay(0.4, 0.09))
    dub = tail(synth.sine(synth.glide(60, 36, 0.4, 0.5), 0.4) * synth.decay(0.4, 0.08)) * 0.7
    out = np.zeros(int(d * synth.SR))
    synth.place(out, lub, 0.0)
    synth.place(out, dub, 0.22)
    return out * strength


def pluck(f, rng):
    d = 1.4
    t = synth.seconds(d)
    tone = sum(np.sin(2 * np.pi * f * k * t + k) * (0.6 ** k) for k in range(1, 7))
    return tail(synth.lowpass(tone, 900) * synth.decay(d, 0.35))


def tick(rng):
    d = 0.12
    return tail(synth.lowpass(synth.bandpass(synth.white(d, rng), 3200, 1.5), 6000) * synth.envelope(d, 0.006, 0, curve=3))


def drone(total, rng):
    t = synth.seconds(total)
    out = np.zeros_like(t)
    for freq, amp in ((43.65, 0.8), (87.31, 1.0), (130.8, 0.35)):
        for detune in (-0.12, 0.12):
            out += amp * np.sin(2 * np.pi * (freq + detune) * t + rng.uniform(0, 6))
    return out


def pad(total, rng):
    out = np.zeros((2, int(total * synth.SR)))
    duration = CHORD_TIME + 2.5
    for i in range(len(CHORDS) + 1):
        notes, _ = CHORDS[i % len(CHORDS)]
        start = max(0.0, i * CHORD_TIME - 1.2)
        env = swell(duration, 1.8, 2.5)
        for j, name in enumerate(notes):
            f = synth.note(name)
            sung = synth.voice(f, duration, rng, "oo", max_freq=2400)
            placed = synth.pan(sung * env / (1 + j * 0.25), -0.4 + 0.4 * j)
            for ch in range(2):
                synth.place(out[ch], placed[ch], start)
    return out


def duet(total, rng, late):
    """Hold's line, low and heavy, and Fast's, thin and high, a beat behind; each note a bar long, swelling."""
    out = np.zeros((2, int(total * synth.SR)))
    bars = int(LENGTH / BAR)
    for i in range(bars + 1):
        for line, vowel, pan, delay, gain in ((HOLD, "oh", -0.35, 0.0, 1.0), (FAST, "ah", 0.35, BEAT * late, 0.55)):
            name = line[i % len(line)]
            d = BAR * 1.1
            f = synth.note(name)
            v = synth.voice(f, d, rng, vowel, vibrato=0.012 if line is HOLD else 0.022, max_freq=3200) * swell(d, 0.5, 1.2) * gain
            placed = synth.pan(v, pan)
            for ch in range(2):
                synth.place(out[ch], placed[ch], i * BAR + delay)
    return out


def wail(total, rng):
    out = np.zeros((2, int(total * synth.SR)))
    for i in range(len(CHORDS) + 1):
        d = CHORD_TIME * 0.8
        a = synth.note(FAST[(2 * i) % len(FAST)]) * 2
        pitch = synth.glide(a, a * 0.8, d, 1.4)
        v = synth.voice(pitch, d, rng, "ah", vibrato=0.03, max_freq=5000) * swell(d, 1.0, 2.0)
        placed = synth.pan(v, 0.5 if i % 2 else -0.5)
        for ch in range(2):
            synth.place(out[ch], placed[ch], i * CHORD_TIME + 1.0)
    return out


def render(v, rng):
    base = np.random.default_rng(20261001)  # shared by both variants: identical layers, so they crossfade in step
    torn = v >= 2
    total = LENGTH + FADE
    bars = np.arange(0, total, BAR)
    dry = synth.pan(level(events(total, list(bars), lambda at: heartbeat(base)), -20), 0.0)
    dry += synth.pan(level(events(total, list(bars), lambda at: pluck(synth.note(CHORDS[int(at / CHORD_TIME) % len(CHORDS)][1]), base)), -24), 0.0)
    dry += synth.pan(level(events(total, [b + k * BEAT for b in bars for k in (1, 2)], lambda at: tick(base)), -36), 0.25)
    dry += synth.pan(level(drone(total, base), -31), 0.0)
    wet = level(pad(total, base), -27) + level(duet(total, base, 1.0), -22)
    if torn:
        extra = np.random.default_rng(13)
        dry += synth.pan(level(events(total, [b + BAR / 2 for b in bars], lambda at: heartbeat(extra, 0.8)), -22), 0.0)
        dry += synth.pan(level(events(total, [b + k * BEAT / 2 for b in bars for k in range(1, 6)], lambda at: tick(extra)), -34), -0.25)
        wet += level(wail(total, extra), -27)
    dry = synth.loop_crossfade(dry, FADE)
    wet = synth.loop_crossfade(wet, FADE)
    n = wet.shape[1]
    hall = synth.reverb_ir(4.0, 3.2, np.random.default_rng(17))
    for ch in range(2):
        ir = np.zeros(n)
        ir[:len(hall[ch])] = hall[ch]
        reverberant = np.fft.irfft(np.fft.rfft(wet[ch]) * np.fft.rfft(ir), n)
        dry[ch] += 0.3 * wet[ch] + 0.8 * level(reverberant, 20 * np.log10(rms(wet[ch])))
    return dry
