"""The Last Hour: music for the Hall of the Last Hour. Two 96 s seamless loops in E minor sharing one loop (a clock's
second to the beat: 60 bpm, 24 bars of 4, three bars to a chord), so the one can crossfade into the other in step.

1, stopped (the Hall as it is: time held at a quarter past nine): long held chords on a soft flue organ, bowed glass
above them, a low drone; a clock's tick now and then that never gets its tock; twice in the loop the quarter chime
begins on the great bells and stops short (G, F#, E: the B that would finish it never comes), the last note left
ringing a long while. A cold stone hall, very wet. Still, melancholic, held breath.

2, awake (the central engine set going: time again, everywhere at once): the same chords, driven. The escapement's tick
and tock on every beat, ratchets clattering, a harpsichord turning the chords over in quavers like a train of gears, a
bowed bass on every beat, timpani, the organ pulsing, the glass trembling; the quarter chime rung through at last (G, F#,
E, B), answered halfway (B, E, F#, G), and the great bell on each chord's root. Relentless, not fast.

Chords: Em(add9) - Cmaj7 - Am9 - B7(b9) - Em - G6 - Cmaj7(#11) - B7.
`synth` and `np` are injected by tools/audio/build_sound.py.
"""
LOOP = True
FOLDER = "Music"
VARIANTS = 2
PEAK = [0.72, 0.85]

BEAT = 1.0                   # 60 bpm: a clock's second
BAR = 4 * BEAT
SPAN = 3 * BAR               # a chord
LENGTH = 24 * BAR            # 96 s
FADE = 6.0
CHORDS = [
    ["E2", "B2", "E3", "G3", "B3", "F#4"],   # Em(add9)
    ["C2", "G2", "E3", "G3", "B3"],          # Cmaj7
    ["A1", "E2", "C3", "G3", "B3"],          # Am9
    ["B1", "F#2", "D#3", "A3", "C4"],        # B7(b9)
    ["E2", "B2", "G3", "B3", "E4"],          # Em
    ["G2", "D3", "B3", "E4"],                # G6
    ["C2", "G2", "B3", "E4", "F#4"],         # Cmaj7(#11)
    ["B1", "F#2", "D#3", "A3"],              # B7: back round to E
]
QUARTER = ["G4", "F#4", "E4", "B3"]          # the first quarter's chime (Westminster's, in E minor)
ANSWER = ["B3", "E4", "F#4", "G4"]


def rms(x):
    return np.sqrt(np.mean(np.asarray(x) ** 2)) + 1e-12


def level(x, db):
    return x / rms(x) * 10 ** (db / 20)


def peak(x, db):
    return x / (np.max(np.abs(x)) + 1e-9) * 10 ** (db / 20)


def swell(duration, attack, release):
    t = synth.seconds(duration)
    up = np.clip(t / attack, 0, 1)
    down = np.clip((duration - t) / release, 0, 1)
    return np.sin(np.minimum(up, down) * np.pi / 2) ** 2


def wrapped(events):
    """Events near the start again a loop later, so the crossfaded tail carries them."""
    return events + [(t + LENGTH, *rest) for t, *rest in events if t < FADE]


def put(out, tone, at, position=0.0, gain=1.0):
    placed = synth.pan(tone * gain, position)
    for ch in range(2):
        synth.place(out[ch], placed[ch], max(0.0, at))


def blank(total):
    return np.zeros((2, int(total * synth.SR)))


# ---- voices ----

def flue(f, duration, rng):
    """A soft flue pipe: near-sine, a quiet octave and twelfth, a breath of wind at the mouth."""
    t = synth.seconds(duration)
    out = np.zeros_like(t)
    for k, a in ((1, 1.0), (2, 0.35), (3, 0.12), (4, 0.06)):
        out += a * np.sin(2 * np.pi * f * k * t + rng.uniform(0, 6))
    wind = synth.bandpass(synth.white(duration, rng), f * 2, 3.0) * 0.03
    return synth.lowpass(out + wind, 1800)


def bowed_glass(f, duration, rng, tremolo=0.0):
    """Glass bowed, not struck: a held tone, its partials a hair off true, shimmering; `tremolo` trembles it."""
    t = synth.seconds(duration)
    out = np.zeros_like(t)
    for detune in (0.9993, 1.0007):
        for ratio, a in ((1.0, 1.0), (2.003, 0.22), (3.01, 0.07)):
            out += a * np.sin(2 * np.pi * f * detune * ratio * t + rng.uniform(0, 6))
    shimmer = 1 + 0.05 * np.sin(2 * np.pi * rng.uniform(0.15, 0.3) * t + rng.uniform(0, 6))
    if tremolo:
        shimmer *= 1 - 0.35 * (0.5 + 0.5 * np.sin(2 * np.pi * tremolo * t))
    return out * shimmer


def bell(f, duration, rng, ring=1.0):
    """A great bell (a carillon's): the hum an octave under, the minor-third tierce, the quint, the nominal; struck.
    `ring` lengthens it (a note left hanging)."""
    t = synth.seconds(duration)
    out = np.zeros_like(t)
    for ratio, a, tau in ((0.5, 0.55, 6.0), (1.0, 1.0, 4.0), (1.19, 0.45, 2.6), (1.5, 0.28, 2.0), (2.0, 0.6, 2.4),
                          (2.52, 0.18, 1.2), (3.0, 0.14, 0.9), (4.07, 0.07, 0.5)):
        out += a * np.sin(2 * np.pi * f * ratio * t + rng.uniform(0, 6)) * np.exp(-t / (tau * ring))
    strike = synth.bandpass(synth.white(0.05, rng), f * 4, 1.5) * synth.decay(0.05, 0.01) * 0.4
    out[:len(strike)] += strike
    return out * np.clip(t / 0.003, 0, 1)


def tick(rng, pitch=1.0, gain=1.0):
    """The escapement letting a tooth go: a hard click and a small metal ping."""
    d = 0.25
    t = synth.seconds(d)
    click = synth.lowpass(synth.highpass(synth.white(d, rng), 2500), 7000) * np.exp(-t / 0.004)
    ping = np.sin(2 * np.pi * 3100 * pitch * t) * np.exp(-t / 0.03) * 0.5 \
        + np.sin(2 * np.pi * 5230 * pitch * t) * np.exp(-t / 0.015) * 0.25
    body = synth.bandpass(synth.white(d, rng), 900 * pitch, 2.0) * np.exp(-t / 0.02) * 0.6
    return (click + ping + body) * np.clip(t / 0.0008, 0, 1) * gain  # (a hair of attack: hard, but not a pop)


def ratchet(rng, teeth=3, gap=0.045):
    """A pawl over a ratchet's teeth: a few quick dry clicks."""
    d = teeth * gap + 0.1
    out = np.zeros(int(d * synth.SR))
    for k in range(teeth):
        c = synth.bandpass(synth.white(0.03, rng), rng.uniform(1800, 2600), 3.0) * synth.decay(0.03, 0.004)
        synth.place(out, c * rng.uniform(0.6, 1.0), k * gap * rng.uniform(0.9, 1.1))
    return out


def harpsichord(f, duration, rng):
    """A plucked quill: bright, buzzing, quick to fade."""
    t = synth.seconds(duration)
    out = np.zeros_like(t)
    for k in range(1, 12):
        out += (1.0 / k ** 0.9) * np.sin(2 * np.pi * f * k * t + rng.uniform(0, 6)) * np.exp(-t * (2.0 + 0.6 * k))
    quill = synth.lowpass(synth.highpass(synth.white(0.01, rng), 3000), 8000) * 0.3
    out[:len(quill)] += quill
    return out * np.clip(t / 0.001, 0, 1)


def bass_bow(f, duration, rng):
    """A bowed bass note, short and dug in: a lowpassed saw with a scrape at the front."""
    t = synth.seconds(duration)
    out = np.zeros_like(t)
    for k in range(1, 16):
        out += (1.0 / k) * np.sin(2 * np.pi * f * k * t + rng.uniform(0, 6))
    scrape = synth.bandpass(synth.white(duration, rng), 1200, 1.2) * np.exp(-t / 0.05) * 0.4
    return synth.lowpass(out, 900) * synth.envelope(duration, 0.02, duration - 0.02, curve=2.5) + scrape


def timpani(f, duration, rng):
    t = synth.seconds(duration)
    out = np.zeros_like(t)
    for ratio, a, tau in ((1.0, 1.0, 0.9), (1.5, 0.45, 0.6), (1.98, 0.3, 0.45), (2.44, 0.15, 0.3)):
        out += a * np.sin(2 * np.pi * f * ratio * t + rng.uniform(0, 6)) * np.exp(-t / tau)
    thud = synth.lowpass(synth.white(0.08, rng), 400) * synth.decay(0.08, 0.02) * 0.8
    out[:len(thud)] += thud
    return out


# ---- layers ----

def organ(total, rng, pulse=False):
    """The chords held on the flue organ; awake, it breathes in on every bar."""
    out = blank(total)
    for i in range(len(CHORDS) + 1):
        notes = CHORDS[i % len(CHORDS)]
        start = i * SPAN - 2.0
        duration = SPAN + 4.0
        env = swell(duration, 3.0, 3.5)
        if pulse:
            t = synth.seconds(duration)
            env = env * (0.55 + 0.45 * np.exp(-((t - 2.0) % BAR) / 1.2))
        for j, name in enumerate(notes[:4]):
            put(out, flue(synth.note(name), duration, rng) * env, start, -0.4 + 0.8 * j / 3)
    return out


def glass_choir(total, rng, tremolo=0.0):
    """Bowed glass on the chords' upper notes."""
    out = blank(total)
    for i in range(len(CHORDS) + 1):
        notes = CHORDS[i % len(CHORDS)]
        start = i * SPAN - 3.0
        duration = SPAN + 6.0
        env = swell(duration, 4.5, 4.5)
        for j, name in enumerate(notes[-2:]):
            f = synth.note(name) * 2
            put(out, bowed_glass(f, duration, rng, tremolo) * env, start, -0.6 + 1.2 * j)
    return out


def drone(total, rng):
    """E low under it all, two strings a hair apart, and the stone's own rumble."""
    t = synth.seconds(total)
    e1 = synth.note("E1")
    low = np.sin(2 * np.pi * e1 * t) + 0.7 * np.sin(2 * np.pi * e1 * 1.002 * t + 1.0) + 0.3 * np.sin(2 * np.pi * e1 * 3 * t)
    rumble = synth.lowpass(synth.brown(total, rng), 120) * 1.2
    mono = low + rumble
    return synth.stereo(mono, mono)


def lone_ticks(total, rng):
    """A clock's tick now and then, its tock never coming."""
    out = blank(total)
    events = []
    t = 4.0
    while t < LENGTH - 2:
        events.append((t, rng.uniform(-0.6, 0.6)))
        t += rng.uniform(8.0, 15.0)
    for time, position in wrapped(events):
        put(out, tick(rng, pitch=rng.uniform(0.95, 1.05)), time, position)
    return out


def chime(total, rng, notes_at, ring_last=1.0, gain=1.0, position=0.25):
    """The great bells: (time, notes) phrases, a beat apart; the last note of each rung on `ring_last` times longer."""
    out = blank(total)
    events = []
    for start, notes in notes_at:
        for k, name in enumerate(notes):
            last = k == len(notes) - 1
            events.append((start + k * BEAT, name, ring_last if last else 1.0))
    for time, name, ring in wrapped(events):
        put(out, bell(synth.note(name), 6.0 * ring, rng, ring), time, position + rng.uniform(-0.1, 0.1), gain)
    return out


def great_bell(total, rng):
    """Awake: the great bell on each chord's root, low."""
    out = blank(total)
    events = []
    for i, notes in enumerate(CHORDS):
        root = notes[0]
        f = synth.note(root)
        while f < 70:
            f *= 2
        events.append((i * SPAN, f))
    for time, f in wrapped(events):
        put(out, bell(f, 9.0, rng, 1.4), time, -0.2)
    return out


def escapement(total, rng):
    """Awake: tick and tock on every beat, the tock lower and duller."""
    out = blank(total)
    events = [(b * BEAT, b % 2) for b in range(int(LENGTH / BEAT))]
    for time, tock in wrapped(events):
        put(out, tick(rng, pitch=0.62 if tock else 1.0, gain=0.75 if tock else 1.0), time + rng.uniform(-0.004, 0.004),
            0.15 if tock else -0.15)
    return out


def ratchets(total, rng):
    """Awake: the trains of gears catching on the off-beats, here and there."""
    out = blank(total)
    events = []
    for b in range(int(LENGTH / BEAT)):
        if rng.random() < 0.55:
            events.append((b * BEAT + 0.5, rng.uniform(-0.8, 0.8), int(rng.integers(2, 5))))
    for time, position, teeth in wrapped(events):
        put(out, ratchet(rng, teeth), time, position)
    return out


def gear_train(total, rng):
    """Awake: the harpsichord turning each chord over in quavers, up and down, like teeth meshing."""
    out = blank(total)
    events = []
    for i, notes in enumerate(CHORDS):
        upper = [synth.note(n) * 2 for n in notes[1:]]
        pattern = upper + upper[-2:0:-1]
        for q in range(int(SPAN / (BEAT / 2))):
            events.append((i * SPAN + q * BEAT / 2, pattern[q % len(pattern)], (q % 8) / 7 - 0.5))
    for time, f, position in wrapped(events):
        put(out, harpsichord(f, 1.2, rng), time + rng.uniform(0, 0.01), position * 0.8)
    return out


def bass(total, rng):
    """Awake: a bowed bass dug in on every beat, the chord's root (the fifth on the fourth beat)."""
    out = blank(total)
    events = []
    for b in range(int(LENGTH / BEAT)):
        notes = CHORDS[int(b * BEAT // SPAN) % len(CHORDS)]
        name = notes[1] if b % 4 == 3 else notes[0]
        f = synth.note(name)
        while f < 50:
            f *= 2
        events.append((b * BEAT, f, 1.0 if b % 4 == 0 else 0.7))
    for time, f, g in wrapped(events):
        put(out, bass_bow(f, 0.7, rng), time, 0.0, g)
    return out


def drums(total, rng):
    """Awake: timpani on E and B, beats one and three; a pickup into each new chord."""
    out = blank(total)
    events = []
    e, b = synth.note("E2"), synth.note("B1")
    for bar in range(24):
        t0 = bar * BAR
        events.append((t0, e, 1.0))
        events.append((t0 + 2 * BEAT, b, 0.6))
        if bar % 3 == 2:
            for k, at in enumerate((3.0, 3.5, 3.75)):
                events.append((t0 + at * BEAT, b, 0.4 + 0.15 * k))
    for time, f, g in wrapped(events):
        put(out, timpani(f, 2.0, rng), time, -0.1, g)
    return out


def limit(x, db):
    """Brought to `db` RMS, its few hard peaks (the ticks, the strikes) rounded off rather than setting the level."""
    x = level(x, db)
    knee = 0.5
    over = np.abs(x) > knee
    x[over] = np.sign(x[over]) * (knee + (1 - knee) * np.tanh((np.abs(x[over]) - knee) / (1 - knee)))
    return x


def hall(dry, wet, decay, mix, rng):
    """A stone hall, applied circularly over the loop (its tail rings on into the start)."""
    n = wet.shape[1]
    ir_pair = synth.reverb_ir(decay + 1.0, decay, rng, brightness=0.25)
    for ch in range(2):
        ir = np.zeros(n)
        ir[:len(ir_pair[ch])] = ir_pair[ch]
        reverberant = np.fft.irfft(np.fft.rfft(wet[ch]) * np.fft.rfft(ir), n)
        dry[ch] += (1 - mix) * wet[ch] + mix * 1.4 * level(reverberant, 20 * np.log10(rms(wet[ch])))
    return dry


def render(v, rng):
    total = LENGTH + FADE
    dry = level(drone(total, rng), -34)
    if v == 0:  # stopped
        wet = level(organ(total, rng), -28) + level(glass_choir(total, rng), -33)
        wet += peak(chime(total, rng, [(0.0, QUARTER[:3]), (12 * BAR, QUARTER[:3])], ring_last=2.5), -14)
        wet += peak(lone_ticks(total, rng), -24)
        dry = synth.loop_crossfade(dry, FADE)
        wet = synth.loop_crossfade(wet, FADE)
        return hall(dry, wet, 5.5, 0.6, rng)
    # awake
    dry += level(escapement(total, rng), -31) + level(ratchets(total, rng), -38)
    wet = level(organ(total, rng, pulse=True), -27) + level(glass_choir(total, rng, tremolo=7.5), -32)
    wet += level(gear_train(total, rng), -27) + level(bass(total, rng), -26) + level(drums(total, rng), -24)
    wet += peak(chime(total, rng, [(0.0, QUARTER), (12 * BAR, ANSWER)], ring_last=1.5), -12)
    wet += peak(great_bell(total, rng), -16)
    dry = synth.loop_crossfade(dry, FADE)
    wet = synth.loop_crossfade(wet, FADE)
    return limit(hall(dry, wet, 3.5, 0.4, rng), -16)
