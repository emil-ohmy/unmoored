"""Last Autumn: music for the Orchard of Last Autumn. A 96 s seamless loop, a slow waltz in A minor (3/4 at 60 bpm:
32 bars, four to a chord), the key the Moths' notes are in.

Layers: a reed-organ pad on the chords (Am - Fmaj7 - C - G - Am - Dm7 - E7 - Am), a plucked harp keeping the waltz
under it (the root, then the chord twice), a low bowed hum on each chord's root, and the tune on an old music box a
few cents out of tune: it plays the first half, falls quiet through the middle (only the waltz going on), and comes back
for the last phrase, leaning on the G# so the loop pulls round to A. Under it all: leaves rustling in a breath of
wind, and now and then a distant chime. Nostalgic, almost cosy, and not quite right: the harvest that never came.
`synth` and `np` are injected by tools/audio/build_sound.py.
"""
LOOP = True
FOLDER = "Music"
PEAK = 0.8

BEAT = 1.0                   # 60 bpm
BAR = 3 * BEAT
LENGTH = 32 * BAR            # 96 s
FADE = 6.0
CHORDS = [
    ["A2", "E3", "A3", "C4", "E4"],    # Am
    ["F2", "C3", "A3", "E4"],          # Fmaj7
    ["C3", "G3", "C4", "E4"],          # C
    ["G2", "D3", "B3", "D4"],          # G
    ["A2", "E3", "C4", "E4"],          # Am
    ["D3", "A3", "C4", "F4"],          # Dm7
    ["E2", "B2", "G#3", "D4"],         # E7: the G# pulls back to A
    ["A2", "E3", "A3", "C4"],          # Am
]
# The music box's tune: (bar from 1, beat from 1, note). Bars 17-24 left to the waltz alone.
TUNE = [
    (1, 1, "E5"), (1, 2, "C5"), (1, 3, "A4"), (2, 1, "B4"), (2, 2.5, "C5"), (3, 1, "E5"), (3, 2, "D5"), (3, 3, "C5"), (4, 1, "A4"),
    (5, 1, "C5"), (5, 2, "F5"), (5, 3, "E5"), (6, 1, "D5"), (6, 2.5, "C5"), (7, 1, "A4"), (7, 2, "C5"), (7, 3, "D5"), (8, 1, "C5"),
    (9, 1, "E5"), (9, 2, "G5"), (9, 3, "E5"), (10, 1, "D5"), (10, 2.5, "C5"), (11, 1, "G4"), (11, 2, "C5"), (11, 3, "E5"), (12, 1, "D5"),
    (13, 1, "D5"), (13, 2, "B4"), (13, 3, "G4"), (14, 1, "A4"), (14, 2.5, "B4"), (15, 1, "D5"), (15, 2, "C5"), (15, 3, "B4"), (16, 1, "A4"),
    (25, 1, "B4"), (25, 2, "G#4"), (25, 3, "E4"), (26, 1, "F5"), (26, 2.5, "E5"), (27, 1, "D5"), (27, 2, "C5"), (27, 3, "B4"), (28, 1, "G#4"),
    (29, 1, "A4"), (29, 2, "C5"), (29, 3, "E5"), (30, 1, "D5"), (30, 2.5, "C5"), (31, 1, "B4"), (31, 2, "A4"), (32, 2, "G#4"), (32, 3, "B4"),
]
CHIMES = ["A5", "C6", "D6", "E6", "G6", "A6"]


def rms(x):
    return np.sqrt(np.mean(np.asarray(x) ** 2)) + 1e-12


def level(x, db):
    return x / rms(x) * 10 ** (db / 20)


def swell(duration, attack, release):
    t = synth.seconds(duration)
    up = np.clip(t / attack, 0, 1)
    down = np.clip((duration - t) / release, 0, 1)
    return np.sin(np.minimum(up, down) * np.pi / 2) ** 2


def wrapped(events):
    """Events near the start again a loop later, so the crossfaded tail carries them."""
    return events + [(t + LENGTH, *rest) for t, *rest in events if t < FADE]


def reed(f, duration, rng):
    """A reed organ: odd-leaning harmonics, a slow breath in the bellows, two reeds a hair apart."""
    t = synth.seconds(duration)
    out = np.zeros_like(t)
    breath = 1 + 0.04 * np.sin(2 * np.pi * 0.35 * t + rng.uniform(0, 6))
    for detune in (0.999, 1.0015):
        for k in range(1, 9):
            a = (1.0 / k) * (1.0 if k % 2 else 0.55)
            out += a * np.sin(2 * np.pi * f * detune * k * t + rng.uniform(0, 6))
    return synth.lowpass(out * breath, 2200)


def pluck(f, duration, rng):
    """A harp string: bright, then mellowing as it rings, a breath of finger at the start."""
    t = synth.seconds(duration)
    out = np.zeros_like(t)
    for k in range(1, 7):
        out += (1.0 / k ** 1.2) * np.sin(2 * np.pi * f * k * t + rng.uniform(0, 6)) * np.exp(-t * (1.1 + 0.9 * k))
    finger = synth.bandpass(synth.white(duration, rng), f * 3, 2.0) * synth.decay(duration, 0.006)
    return out + 0.15 * finger


def tine(f, duration, rng):
    """A music box's tine: glassy, a slightly off-true second partial, a few cents flat or sharp."""
    f *= 2 ** (rng.uniform(-10, 10) / 1200)
    t = synth.seconds(duration)
    body = synth.glass(f, duration, rng)
    upper = np.sin(2 * np.pi * f * 2.76 * t) * np.exp(-t * 6.0) * 0.25  # (the tine's own clang)
    click = synth.highpass(synth.white(0.01, rng), 4000) * 0.2
    out = body + upper
    out[:len(click)] += click
    return out


def pad(total, rng):
    out = np.zeros((2, int(total * synth.SR)))
    span = 4 * BAR
    for i in range(len(CHORDS) + 1):
        notes = CHORDS[i % len(CHORDS)]
        start = max(0.0, i * span - 1.5)
        duration = span + 3.0
        env = swell(duration, 2.5, 3.0)
        for j, name in enumerate(notes[1:]):
            tone = reed(synth.note(name), duration, rng) * env
            placed = synth.pan(tone, -0.5 + j / max(1, len(notes) - 2))
            for ch in range(2):
                synth.place(out[ch], placed[ch], start)
    return out


def harp(total, rng):
    out = np.zeros((2, int(total * synth.SR)))
    events = []
    for bar in range(32):
        notes = CHORDS[bar // 4]
        t0 = bar * BAR
        events.append((t0, notes[0], -0.2))                   # oom
        for beat, pick in ((1, (1, 2)), (2, (2, 3))):        # pah, pah
            for k in pick:
                events.append((t0 + beat * BEAT + rng.uniform(0, 0.03), notes[min(k, len(notes) - 1)], 0.3))
    for time, name, position in wrapped(events):
        tone = synth.pan(pluck(synth.note(name), 2.5, rng), position)
        for ch in range(2):
            synth.place(out[ch], tone[ch] * (0.6 if position < 0 else 0.5), time)  # (the oom soft: a waltz, not a march)
    return out


def hum(total, rng):
    """Low bowed notes on each chord's root (a cello-ish hum through a closed mouth)."""
    out = np.zeros((2, int(total * synth.SR)))
    span = 4 * BAR
    for i in range(len(CHORDS) + 1):
        root = CHORDS[i % len(CHORDS)][0]
        duration = span + 2.0
        tone = synth.voice(synth.note(root), duration, rng, "oh", vibrato=0.006, max_freq=1800) * swell(duration, 3.0, 2.5)
        placed = synth.pan(tone, 0.0)
        for ch in range(2):
            synth.place(out[ch], placed[ch], max(0.0, i * span - 1.0))
    return out


def music_box(total, rng):
    out = np.zeros((2, int(total * synth.SR)))
    events = [((bar - 1) * BAR + (beat - 1) * BEAT, name) for bar, beat, name in TUNE]
    for time, name in wrapped(events):
        tone = synth.pan(tine(synth.note(name), 3.0, rng), rng.uniform(-0.15, 0.25))
        for ch in range(2):
            synth.place(out[ch], tone[ch], max(0.0, time + rng.uniform(-0.02, 0.02)))
    return out


def chimes(total, rng):
    out = np.zeros((2, int(total * synth.SR)))
    t = 3.0
    events = []
    while t < LENGTH - 4:
        events.append((t, rng.choice(CHIMES)))
        t += rng.uniform(5.0, 11.0)
    for time, name in wrapped(events):
        tone = synth.pan(synth.glass(synth.note(name), 4.0, rng), rng.uniform(-0.8, 0.8))
        for ch in range(2):
            synth.place(out[ch], tone[ch], time)
    return out


def leaves(total, rng):
    """Leaves rustling as a breath of wind goes through them: a dry crackle in gusts, high and soft."""
    n = int(total * synth.SR)
    gust = synth.smooth_random(total, rng, 0.2, 0.1, 1.0) ** 2

    def rustle(times, freqs):
        i = np.clip((times * synth.SR).astype(int), 0, n - 1)
        lf = np.log(np.maximum(freqs, 1.0))[None, :]
        return gust[i][:, None] * np.exp(-0.5 * ((lf - np.log(3200)) / 0.6) ** 2)

    def breeze(times, freqs):
        i = np.clip((times * synth.SR).astype(int), 0, n - 1)
        lf = np.log(np.maximum(freqs, 1.0))[None, :]
        return (0.3 + 0.7 * gust[i][:, None]) * 0.4 * np.exp(-0.5 * ((lf - np.log(420)) / 0.5) ** 2)

    return synth.spectral_noise(total, rng, rustle) + synth.spectral_noise(total, rng, breeze)


def render(v, rng):
    total = LENGTH + FADE
    dry = synth.stereo(level(leaves(total, rng), -36), level(leaves(total, rng), -36))
    wet = level(pad(total, rng), -27) + level(harp(total, rng), -27) + level(hum(total, rng), -31)
    box = music_box(total, rng)
    wet += box / (np.max(np.abs(box)) + 1e-9) * 10 ** (-11 / 20)
    chime = chimes(total, rng)
    wet += chime / (np.max(np.abs(chime)) + 1e-9) * 10 ** (-26 / 20)

    dry = synth.loop_crossfade(dry, FADE)
    wet = synth.loop_crossfade(wet, FADE)
    # A warm, middling hall, applied circularly over the loop (its tail rings on into the start).
    n = wet.shape[1]
    hall = synth.reverb_ir(3.5, 2.8, rng, brightness=0.3)
    for ch in range(2):
        ir = np.zeros(n)
        ir[:len(hall[ch])] = hall[ch]
        reverberant = np.fft.irfft(np.fft.rfft(wet[ch]) * np.fft.rfft(ir), n)
        dry[ch] += 0.55 * wet[ch] + 0.7 * level(reverberant, 20 * np.log10(rms(wet[ch])))
    return dry
