"""The Widow Hale: miniboss music, a 64 s seamless loop in E minor at 60 bpm (the Hall's clock: a second to the beat;
16 bars of 4, four bars to a chord), in two variants sharing one loop so the game crossfades them in step.

1, her lament: a slow funeral procession through the stopped Hall. A muffled bell (her bell, E2, its clapper bound) on
the first beat of every bar; crape-muffled timpani on one and three; a deep bowed drone on E; the Last Hour's stopped
chords held on its soft flue organ (Em(add9) - Cmaj7 - Am9 - B7(b9)); and above them a wordless keening voice, a
soprano's, sliding between its notes, falling in sighs (B to G, the Keeners' third) and crying up to G5 once a turn.
Under it a clock ticks on one and three and never tocks. A cold, very wet stone hall.

2, her last third: the same loop, more urgent: the timpani on every beat, the clock ticking every beat (still never
a tock), a bowed bass trembling in quavers on the roots, the Keeners keening back at her (falling thirds, all round),
a smaller bell answering hers on the third beat, the voice trembling.

Chords: Em(add9) - Cmaj7 - Am9 - B7(b9), back round to E.
`synth` and `np` are injected by tools/audio/build_sound.py.
"""
LOOP = True
FOLDER = "Music"
VARIANTS = 2
PEAK = [0.8, 0.86]

BEAT = 1.0                   # 60 bpm: the Hall's second
BAR = 4 * BEAT
SPAN = 4 * BAR               # a chord
LENGTH = 16 * BAR            # 64 s
FADE = 6.0
CHORDS = [
    ["E2", "B2", "E3", "G3", "B3", "F#4"],   # Em(add9)
    ["C2", "G2", "E3", "G3", "B3"],          # Cmaj7
    ["A1", "E2", "C3", "G3", "B3"],          # Am9
    ["B1", "F#2", "D#3", "A3", "C4"],        # B7(b9)
]
# The keening voice: (note or None for a rest, beats); 16 beats a chord.
MELODY = [
    ("E4", 2), ("B4", 4), ("C5", 1), ("B4", 1), ("A4", 2), ("G4", 4), ("F#4", 2),            # Em(add9)
    ("E5", 5), ("D5", 1), ("C5", 2), ("B4", 4), ("G4", 2), (None, 2),                      # Cmaj7
    (None, 1), ("A4", 1), ("C5", 2), ("E5", 2), ("G5", 4), ("F#5", 1), ("E5", 1), ("B4", 4),  # Am9: the cry
    ("C5", 3), ("B4", 3), ("A4", 2), ("F#4", 2), ("D#4", 4), (None, 2),                    # B7(b9): to E again
]


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
    return events + [(t + LENGTH, *rest) for t, *rest in events if t < FADE + 1]


def put(out, tone, at, position=0.0, gain=1.0):
    placed = synth.pan(tone * gain, position)
    for ch in range(2):
        synth.place(out[ch], placed[ch], max(0.0, at))


def blank(total):
    return np.zeros((2, int(total * synth.SR)))


def mono_to_stereo(x, width=0.0):
    return synth.stereo(x, x) if not width else synth.pan(x, width)


# ---- voices ----

def flue(f, duration, rng):
    """The Hall's soft flue pipe (as in its music): near-sine, a quiet octave and twelfth, a breath of wind."""
    t = synth.seconds(duration)
    out = np.zeros_like(t)
    for k, a in ((1, 1.0), (2, 0.35), (3, 0.12), (4, 0.06)):
        out += a * np.sin(2 * np.pi * f * k * t + rng.uniform(0, 6))
    wind = synth.bandpass(synth.white(duration, rng), f * 2, 3.0) * 0.03
    return synth.lowpass(out + wind, 1800)


def mourning_bell(f, duration, rng):
    """Her bell, muffled: the clapper bound in leather (a padded thud, a soft front), the hum and prime long, the
    upper partials smothered quick. (The crape's lowpass goes over the whole layer.)"""
    t = synth.seconds(duration)
    out = np.zeros_like(t)
    for ratio, a, tau in ((0.5, 0.25, 5.0), (1.0, 1.0, 3.4), (1.19, 0.55, 2.2), (1.5, 0.3, 1.5), (2.0, 0.5, 1.4),
                          (2.52, 0.22, 0.8), (3.0, 0.14, 0.55), (4.16, 0.07, 0.3)):
        beat = 1 + 0.08 * np.sin(2 * np.pi * rng.uniform(0.5, 1.3) * t + rng.uniform(0, 6))
        phase = 0.0 if ratio <= 1.0 else rng.uniform(0, 6)  # (hum and prime in phase with the drum: never cancelling it)
        out += a * np.sin(2 * np.pi * f * ratio * rng.uniform(0.998, 1.002) * t + phase) * np.exp(-t / tau) * beat
    out *= np.clip(t / 0.012, 0, 1)
    d = 0.18
    thud = synth.lowpass(synth.brown(d, rng), 320) * synth.decay(d, 0.04) * np.clip(synth.seconds(d) / 0.004, 0, 1)
    out[:len(thud)] += thud / (np.max(np.abs(thud)) + 1e-9) * 0.5
    return out


def timpani(f, duration, rng):
    """A kettledrum with crape over its head: short, dull, all thud. (Lowpassed with the layer.)"""
    t = synth.seconds(duration)
    out = np.zeros_like(t)
    for ratio, a, tau in ((1.0, 1.0, 0.45), (1.5, 0.35, 0.25), (1.98, 0.2, 0.18)):
        out += a * np.sin(2 * np.pi * f * ratio * t) * np.exp(-t / tau)  # (from rest: the head pushed down)
    thud = synth.lowpass(synth.white(0.08, rng), 300) * synth.decay(0.08, 0.02)
    out[:len(thud)] += thud / (np.max(np.abs(thud)) + 1e-9) * 0.7
    return out * np.clip(t / 0.004, 0, 1)


def tick(rng, gain=1.0):
    """The Hall's escapement letting a tooth go (its music's tick): a hard click and a small metal ping."""
    d = 0.25
    t = synth.seconds(d)
    click = synth.lowpass(synth.highpass(synth.white(d, rng), 2500), 7000) * np.exp(-t / 0.004)
    ping = np.sin(2 * np.pi * 3100 * t) * np.exp(-t / 0.03) * 0.5 + np.sin(2 * np.pi * 5230 * t) * np.exp(-t / 0.015) * 0.25
    body = synth.bandpass(synth.white(d, rng), 900, 2.0) * np.exp(-t / 0.02) * 0.6
    return (click + ping + body) * np.clip(t / 0.0008, 0, 1) * gain


def bass_bow(f, duration, rng):
    """A bowed bass note, short: a saw's harmonics, a scrape at the front. (Lowpassed with the layer.)"""
    t = synth.seconds(duration)
    out = np.zeros_like(t)
    for k in range(1, 12):
        out += (1.0 / k ** 1.3) * np.sin(2 * np.pi * f * k * t + rng.uniform(0, 6))
    scrape = synth.white(duration, rng) * np.exp(-t / 0.03) * 0.15
    return (out + scrape) * swell(duration, 0.03, 0.12)


# ---- layers ----

def drone(total, rng):
    """A deep bowed drone on E: E2 bowed (the bow's pressure wandering), E1 under it, the open fifth faint."""
    t = synth.seconds(total)
    out = np.zeros_like(t)
    for name, gain in (("E1", 0.5), ("E2", 1.0), ("B2", 0.25)):
        f = synth.note(name)
        tone = np.zeros_like(t)
        for k in range(1, 10):
            tone += np.sin(2 * np.pi * f * k * (1 + 0.0007 * (k % 3)) * t + rng.uniform(0, 6)) / k ** 1.4
        tone *= 0.75 + 0.25 * synth.smooth_random(total, rng, 0.3)
        out += tone * gain
    return mono_to_stereo(out)


def organ(total, rng):
    """The stopped chords, held on the flue organ, each swelling into the next."""
    out = blank(total)
    for i in range(int(total / SPAN) + 1):
        notes = CHORDS[i % len(CHORDS)]
        start = i * SPAN - 2.0
        duration = SPAN + 4.0
        env = swell(duration, 3.0, 3.5)
        for j, name in enumerate(notes):
            put(out, flue(synth.note(name), duration, rng) * env, start, -0.5 + 1.0 * j / (len(notes) - 1), 0.8 if j else 1.0)
    return out


def bells(total, rng, on_beats=(0,), small=False):
    """Her muffled bell on each bar's first beat (a little harder on a new chord); `small`: a smaller bell (E3)
    answering on the third."""
    out = blank(total)
    events = []
    for b in range(int(LENGTH / BAR)):
        for beat in on_beats:
            events.append((b * BAR + beat * BEAT, 1.0 if b % 4 == 0 else 0.8))
    f = synth.note("E3" if small else "E2")
    for time, g in wrapped(events):
        put(out, mourning_bell(f, 9.0 if not small else 6.0, rng), time, 0.25 if small else -0.1, g)
    for ch in range(2):
        out[ch] = synth.lowpass(out[ch], 1100 if small else 850)  # (through the crape)
    return out


def drums(total, rng, between=False):
    """The crape-muffled timpani: E on one, B under it on three; a soft pickup into each chord. `between`: the
    strokes on two and four that make it every beat (urgent), and a second pickup."""
    out = blank(total)
    events = []
    e, b = synth.note("E2"), synth.note("B1")
    for bar in range(int(LENGTH / BAR)):
        t0 = bar * BAR
        if between:
            events.append((t0 + 1 * BEAT, e, 0.5))
            events.append((t0 + 3 * BEAT, b, 0.55))
            if bar % 4 == 3:
                events.append((t0 + 3.75 * BEAT, b, 0.6))
            continue
        events.append((t0, e, 1.0))
        events.append((t0 + 2 * BEAT, b, 0.65))
        if bar % 4 == 3:
            events.append((t0 + 3.5 * BEAT, b, 0.4))
    for time, f, g in wrapped(events):
        put(out, timpani(f, 1.6, rng), time, -0.05, g)
    for ch in range(2):
        out[ch] = synth.lowpass(out[ch], 260)
    return out


def ticks(total, rng, between=False):
    """The clock that ticks and never tocks: a tick on one and three (where the tocks would fall, nothing).
    `between`: ticks on two and four too (urgent: a tick every beat, still never a tock)."""
    out = blank(total)
    events = [(b * BEAT, rng.uniform(-0.05, 0.05)) for b in range(1 if between else 0, int(LENGTH / BEAT), 2)]
    for time, position in wrapped(events):
        put(out, tick(rng), time + rng.uniform(-0.003, 0.003), 0.35 + position)
    return out


def keening(total, rng):
    """The soprano's wordless lament: sliding between notes, a scoop up into each, breathing in the rests."""
    n = int(total * synth.SR)
    pitch = np.zeros(n)
    target = np.zeros(n)
    onsets = []
    at, prev = 0.0, synth.note("E4")
    k = 0
    while at < total:
        name, beats = MELODY[k % len(MELODY)]
        s, e = int(at * synth.SR), min(n, int((at + beats * BEAT) * synth.SR))
        if name is None:
            pitch[s:e] = prev
        else:
            f = synth.note(name)
            pitch[s:e] = f
            target[s:e] = 1.0
            onsets.append(at)
            prev = f
        at += beats * BEAT
        k += 1
    # Slide between notes: smooth the pitch (in log) over ~0.12 s.
    kernel = np.hanning(int(0.12 * synth.SR))
    logp = np.log(pitch)
    pad = len(kernel)
    logp = synth.fft_convolve(np.pad(logp, pad, mode="edge"), kernel / kernel.sum())[pad + len(kernel) // 2:pad + len(kernel) // 2 + n]
    t = np.arange(n) / synth.SR
    scoop = np.zeros(n)
    for o in onsets:
        s = int(o * synth.SR)
        m = min(n - s, int(0.3 * synth.SR))
        scoop[s:s + m] += -(45 / 1200) * np.log(2) * np.exp(-t[:m] / 0.06)
    f0 = np.exp(logp + scoop)
    # The breath: phrases swell in, each note leaning a little, and fall away into the rests.
    kernel = np.hanning(int(0.35 * synth.SR))
    pad = len(kernel)
    amp = synth.fft_convolve(np.pad(target, pad, mode="edge"), kernel / kernel.sum())[pad + len(kernel) // 2:pad + len(kernel) // 2 + n]
    lean = np.ones(n)
    for o in onsets:
        s = int(o * synth.SR)
        m = min(n - s, int(1.5 * synth.SR))
        lean[s:s + m] += 0.25 * np.exp(-t[:m] / 0.5) * np.clip(t[:m] / 0.15, 0, 1)
    amp = np.clip(amp, 0, 1) * lean
    sung = synth.voice(f0, total, rng, vowel="ah", vibrato=0.014, max_freq=4500)
    sung += 0.5 * synth.voice(f0 * 1.003, total, rng, vowel="oo", vibrato=0.011, max_freq=4500)  # (a second breath in it)
    return synth.pan(sung[:n] * amp, 0.05)


def keeners(total, rng):
    """Urgent: the Keeners keening back, falling thirds (B4 to G4) here and there round the hall, on the third beat."""
    out = blank(total)
    events = []
    for bar in range(int(LENGTH / BAR)):
        if bar % 2 == 1 or rng.random() < 0.4:
            events.append((bar * BAR + 2 * BEAT + rng.uniform(-0.1, 0.2), rng.uniform(-0.9, 0.9), int(rng.integers(0, 3))))
    for time, position, vowel in wrapped(events):
        d = 1.6
        f0 = synth.glide(synth.note("B4"), synth.note("G4"), d, 0.7) * 2 ** (rng.uniform(-15, 15) / 1200)
        keen = synth.voice(f0, d, rng, vowel=("ah", "oh", "oo")[vowel], vibrato=0.016)
        keen *= synth.envelope(d, 0.1, 0, curve=2.2, peak_at=0.2)
        put(out, keen, time, position, rng.uniform(0.6, 1.0))
    return out


def tremolo_bass(total, rng):
    """Urgent: a bowed bass trembling in quavers on each chord's root."""
    out = blank(total)
    events = []
    for q in range(int(LENGTH / (BEAT / 2))):
        at = q * BEAT / 2
        root = CHORDS[int(at // SPAN) % len(CHORDS)][0]
        f = synth.note(root)
        while f < 55:
            f *= 2
        events.append((at, f, 1.0 if q % 2 == 0 else 0.7))
    for time, f, g in wrapped(events):
        put(out, bass_bow(f, 0.48, rng), time, -0.15, g)
    for ch in range(2):
        out[ch] = synth.lowpass(out[ch], 700)
    return out


def limit(x, db):
    """Brought to `db` RMS, its few hard peaks (the ticks, the strikes) rounded off rather than setting the level."""
    x = level(x, db)
    knee = 0.5
    over = np.abs(x) > knee
    x[over] = np.sign(x[over]) * (knee + (1 - knee) * np.tanh((np.abs(x[over]) - knee) / (1 - knee)))
    return x


def hall(dry, wet, decay, mix, rng):
    """A cold stone hall, applied circularly over the loop (its tail rings on into the start)."""
    n = wet.shape[1]
    ir_pair = synth.reverb_ir(decay + 1.0, decay, rng, brightness=0.22)
    for ch in range(2):
        ir = np.zeros(n)
        ir[:len(ir_pair[ch])] = ir_pair[ch]
        reverberant = np.fft.irfft(np.fft.rfft(wet[ch]) * np.fft.rfft(ir), n)
        dry[ch] += (1 - mix) * wet[ch] + mix * 1.4 * level(reverberant, 20 * np.log10(rms(wet[ch])))
    return dry


def render(v, rng):
    base = np.random.default_rng(20261101)  # shared by both variants: identical layers, so they crossfade in step
    urgent = v >= 1  # (variants count from 0: the second is 1)
    total = LENGTH + FADE
    dry = level(drone(total, base), -35)
    dry += peak(ticks(total, base), -19)
    wet = level(organ(total, base), -31)
    # The procession (bell and drum) goes to the hall and, closer, straight to us, so its strokes stay clear of the wash.
    procession = peak(bells(total, base), -9) + level(drums(total, base), -26)
    voice = keening(total, base)
    if urgent:
        extra = np.random.default_rng(1101)
        t = synth.seconds(total)
        voice = voice * (1 - 0.3 * (0.5 + 0.5 * np.sin(2 * np.pi * 6.5 * t)))  # (the same line, trembling)
        dry += peak(ticks(total, extra, between=True), -19)
        procession += level(drums(total, extra, between=True), -28) + peak(bells(total, extra, on_beats=(2,), small=True), -14)
        wet += level(tremolo_bass(total, extra), -28)
        wet += level(keeners(total, extra), -31)
    wet += level(voice, -19) + procession
    dry += procession * 0.7
    dry = synth.loop_crossfade(dry, FADE)
    wet = synth.loop_crossfade(wet, FADE)
    out = hall(dry, wet, 5.5, 0.55, np.random.default_rng(1102))
    return limit(out, -15.5 if urgent else -17)
