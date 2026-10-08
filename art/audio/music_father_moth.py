"""Father Moth, Who Gathered Autumn: Boss music, Last Autumn grown huge. A 72 s seamless loop, a waltz in A minor (3/4
at 80 bpm: 32 bars, four to a chord, the Orchard's own chords: Am - Fmaj7 - C - G - Am - Dm7 - E7 - Am), in four
variants sharing one loop so the game crossfades them in step, one per phase.

1 (his leaves): the Orchard's waltz as a full dance: the reed organ, the harp keeping the waltz with leaf-fall runs
  down its strings, the music box carrying the tune clear and strong, a deep drum on every first beat (his wingbeats),
  the low bowed hum under it.
2 (more, faster): the same, busier: strings trembling on the chords, a reed voice chasing the tune a beat behind and a
  third below, leaves shaken on the off-beats, the drum doubling into each bar.
3 (afoot): the waltz breaks. A stamping drum on every beat, shears snipping between them, a bass sawing the roots in
  quavers, a fiddle hacking the tune out note by repeated note; the organ thin and far off.
4 (the dark): almost nothing. A low drone on A, a heartbeat each bar, the music box's tune slowed to a note every two
  bars and flat, wind. His charge carries the rest.
`synth` and `np` are injected by tools/audio/build_sound.py.
"""
LOOP = True
FOLDER = "Music"
PEAK = [0.8, 0.8, 0.95, 0.4]  # (the dark falls away: half as loud, his charge carrying it)
VARIANTS = 4

BEAT = 60 / 80               # 80 bpm
BAR = 3 * BEAT
LENGTH = 32 * BAR            # 72 s
FADE = 4.0
CHORDS = [
    ["A2", "E3", "A3", "C4", "E4"],    # Am
    ["F2", "C3", "A3", "E4"],          # Fmaj7
    ["C3", "G3", "C4", "E4"],          # C
    ["G2", "D3", "B3", "D4"],          # G
    ["A2", "E3", "C4", "E4"],          # Am
    ["D3", "A3", "C4", "F4"],          # Dm7
    ["E2", "B2", "G#3", "D4"],         # E7
    ["A2", "E3", "A3", "C4"],          # Am
]
# Last Autumn's tune (the Orchard's music box): (bar from 1, beat from 1, note). Bars 17-24 its middle, sung by the
# harp in 1 and the reed voice in 2.
TUNE = [
    (1, 1, "E5"), (1, 2, "C5"), (1, 3, "A4"), (2, 1, "B4"), (2, 2.5, "C5"), (3, 1, "E5"), (3, 2, "D5"), (3, 3, "C5"), (4, 1, "A4"),
    (5, 1, "C5"), (5, 2, "F5"), (5, 3, "E5"), (6, 1, "D5"), (6, 2.5, "C5"), (7, 1, "A4"), (7, 2, "C5"), (7, 3, "D5"), (8, 1, "C5"),
    (9, 1, "E5"), (9, 2, "G5"), (9, 3, "E5"), (10, 1, "D5"), (10, 2.5, "C5"), (11, 1, "G4"), (11, 2, "C5"), (11, 3, "E5"), (12, 1, "D5"),
    (13, 1, "D5"), (13, 2, "B4"), (13, 3, "G4"), (14, 1, "A4"), (14, 2.5, "B4"), (15, 1, "D5"), (15, 2, "C5"), (15, 3, "B4"), (16, 1, "A4"),
    (25, 1, "B4"), (25, 2, "G#4"), (25, 3, "E4"), (26, 1, "F5"), (26, 2.5, "E5"), (27, 1, "D5"), (27, 2, "C5"), (27, 3, "B4"), (28, 1, "G#4"),
    (29, 1, "A4"), (29, 2, "C5"), (29, 3, "E5"), (30, 1, "D5"), (30, 2.5, "C5"), (31, 1, "B4"), (31, 2, "A4"), (32, 2, "G#4"), (32, 3, "B4"),
]
# Its middle (bars 17-24): the opening phrase again, a step down (for the harp, then the reed voice).
MIDDLE = [(bar + 16, beat, note) for bar, beat, note in TUNE if bar <= 8]


def rms(x):
    return np.sqrt(np.mean(np.asarray(x) ** 2)) + 1e-12


def level(x, db):
    return x / rms(x) * 10 ** (db / 20)


def swell(duration, attack, release):
    t = synth.seconds(duration)
    return np.sin(np.minimum(np.clip(t / attack, 0, 1), np.clip((duration - t) / release, 0, 1)) * np.pi / 2) ** 2


def at(bar, beat):
    return (bar - 1) * BAR + (beat - 1) * BEAT


def wrapped(events):
    """Events near the start again a loop later, so the crossfaded tail carries them."""
    return events + [(t + LENGTH, *rest) for t, *rest in events if t < FADE]


def place(total, events, make, spread=0.0):
    """Stereo: each (time, ...) event's sound, panned (spread: random either side)."""
    out = np.zeros((2, int(total * synth.SR)))
    rng = np.random.default_rng(7)
    for time, *args in wrapped(events):
        if time < 0 or time >= total:
            continue
        tone = synth.pan(make(*args), rng.uniform(-spread, spread))
        for ch in range(2):
            synth.place(out[ch], tone[ch], time)
    return out


# ---- instruments

def reed(f, duration, rng):
    """A reed organ: odd-leaning harmonics, a slow breath in the bellows, two reeds a hair apart."""
    t = synth.seconds(duration)
    out = np.zeros_like(t)
    breath = 1 + 0.04 * np.sin(2 * np.pi * 0.35 * t + rng.uniform(0, 6))
    for detune in (0.999, 1.0015):
        for k in range(1, 9):
            out += (1.0 / k) * (1.0 if k % 2 else 0.55) * np.sin(2 * np.pi * f * detune * k * t + rng.uniform(0, 6))
    return synth.lowpass(out * breath, 2200)


def pluck(f, duration, rng):
    """A harp string: bright, then mellowing as it rings."""
    t = synth.seconds(duration)
    out = np.zeros_like(t)
    for k in range(1, 7):
        out += (1.0 / k ** 1.2) * np.sin(2 * np.pi * f * k * t + rng.uniform(0, 6)) * np.exp(-t * (1.1 + 0.9 * k))
    return out + 0.15 * synth.bandpass(synth.white(duration, rng), f * 3, 2.0) * synth.decay(duration, 0.006)


def tine(f, duration, rng, cents=0.0):
    """A music box's tine: glassy, its own clang above it, a few cents out of true."""
    f *= 2 ** ((rng.uniform(-8, 8) + cents) / 1200)
    t = synth.seconds(duration)
    out = synth.glass(f, duration, rng) + np.sin(2 * np.pi * f * 2.76 * t) * np.exp(-t * 6.0) * 0.25
    click = synth.highpass(synth.white(0.01, rng), 4000) * 0.2
    out[:len(click)] += click
    return out


def bowed(f, duration, rng, bright=1.0, tremolo=0.0):
    """A bowed string (a sawtooth's harmonics through a body), a slow vibrato; trembling (tremolo: bowed fast)."""
    t = synth.seconds(duration)
    vib = 1 + 0.004 * np.sin(2 * np.pi * 5.3 * t + rng.uniform(0, 6))
    phase = 2 * np.pi * np.cumsum(np.full(len(t), f) * vib) / synth.SR
    tone = sum(np.sin(k * phase) / k for k in range(1, 12))
    body = synth.bandpass(tone, 650, 1.1) + bright * 0.6 * synth.bandpass(tone, 2600, 1.4) + 0.2 * tone
    if tremolo:
        body *= 0.55 + 0.45 * np.abs(np.sin(2 * np.pi * tremolo * t))
    return body


def drum(rng, deep=True):
    """A deep drum (a wingbeat in the music): a falling boom, a skin's slap."""
    d = 1.2 if deep else 0.5
    t = synth.seconds(d)
    f0 = 55 if deep else 90
    boom = np.sin(2 * np.pi * np.cumsum(f0 * (1 + 1.4 * np.exp(-t / 0.05))) / synth.SR) * np.exp(-t / (0.35 if deep else 0.15))
    slap = synth.lowpass(synth.white(d, rng), 1200) * np.exp(-t / 0.02)
    return boom + 0.35 * slap


def stamp(rng):
    """A stamping drum and a wooden knock in it (phase 3: afoot)."""
    d = 0.45
    t = synth.seconds(d)
    boom = np.sin(2 * np.pi * np.cumsum(70 * (1 + 1.0 * np.exp(-t / 0.03))) / synth.SR) * np.exp(-t / 0.12)
    knock = np.sin(2 * np.pi * 380 * t) * np.exp(-t / 0.03) + 0.4 * synth.bandpass(synth.white(d, rng), 2200, 1.5) * np.exp(-t / 0.005)
    return boom + 0.4 * knock


def snip(rng):
    """Shears snipping, as percussion: a bright metal tick, a little ring."""
    d = 0.18
    t = synth.seconds(d)
    tick = synth.bandpass(synth.white(d, rng), rng.uniform(3800, 4800), 5.0) * np.exp(-t / 0.008)
    ring = np.sin(2 * np.pi * 2900 * t) * np.exp(-t / 0.03) * 0.4
    return tick + ring


def shake(rng):
    """Dry leaves shaken (a soft rattle on the off-beats)."""
    d = 0.25
    x = synth.lowpass(synth.highpass(synth.white(d, rng), 2600), 7000) * synth.envelope(d, 0.04, 0, curve=2.5)
    return x


def thump(rng):
    """A heartbeat: lub, dub."""
    d = 0.8
    out = np.zeros(int(d * synth.SR))
    for at_, gain in ((0.0, 1.0), (0.24, 0.6)):
        beat = synth.sine(synth.glide(64, 36, 0.3, 0.6), 0.3) * synth.decay(0.3, 0.08) * gain
        synth.place(out, beat, at_)
    return synth.lowpass(out, 380)


# ---- layers

def pad(total, rng, thin=False):
    out = np.zeros((2, int(total * synth.SR)))
    span = 4 * BAR
    for i in range(len(CHORDS) + 1):
        notes = CHORDS[i % len(CHORDS)][1:]
        if thin:
            notes = notes[:2]
        start, duration = max(0.0, i * span - 1.0), span + 2.0
        for j, name in enumerate(notes):
            tone = synth.pan(reed(synth.note(name), duration, rng) * swell(duration, 1.8, 2.2), -0.5 + j / max(1, len(notes) - 1))
            for ch in range(2):
                synth.place(out[ch], tone[ch], start)
    return out


def harp(total, rng):
    """The waltz: oom (the root), pah pah (the chord); every other bar's last beat, a run down the strings (leaves falling)."""
    events = []
    for bar in range(32):
        notes = CHORDS[bar // 4]
        t0 = bar * BAR
        events.append((t0, notes[0], 0.65))
        for beat, pick in ((1, (1, 2)), (2, (2, 3))):
            for k in pick:
                events.append((t0 + beat * BEAT + rng.uniform(0, 0.02), notes[min(k, len(notes) - 1)], 0.5))
        if bar % 2 == 1:  # (a run down: leaves falling)
            run = [synth.note(n) * 2 for n in sorted(notes[1:], key=synth.note, reverse=True)]
            for k, f in enumerate(run):
                events.append((t0 + 2 * BEAT + 0.4 * BEAT + k * BEAT / 6, f, 0.35))
    for bar, beat, name in MIDDLE:  # (the harp sings the middle, an octave down)
        events.append((at(bar, beat), synth.note(name) / 2, 0.8))
    return place(total, events, lambda n, g: pluck(n if isinstance(n, float) else synth.note(n), 2.2, rng) * g, 0.4)


def hum(total, rng):
    out = np.zeros((2, int(total * synth.SR)))
    span = 4 * BAR
    for i in range(len(CHORDS) + 1):
        duration = span + 2.0
        tone = synth.voice(synth.note(CHORDS[i % len(CHORDS)][0]), duration, rng, "oh", vibrato=0.006, max_freq=1800) * swell(duration, 2.0, 2.0)
        tone = synth.pan(tone, 0.0)
        for ch in range(2):
            synth.place(out[ch], tone[ch], max(0.0, i * span - 1.0))
    return out


def music_box(total, rng, cents=0.0, slow=False):
    if slow:  # (the dark: a note every two bars, the tune half-remembered)
        notes = [n for _, _, n in TUNE[:16]]
        events = [(k * 2 * BAR, n) for k, n in enumerate(notes)]
    else:
        events = [(at(bar, beat), n) for bar, beat, n in TUNE]
    return place(total, events, lambda n: tine(synth.note(n), 3.5 if slow else 2.8, rng, cents), 0.2)


def tremble(total, rng):
    """Strings trembling on the chords (phase 2)."""
    out = np.zeros((2, int(total * synth.SR)))
    span = 4 * BAR
    for i in range(len(CHORDS) + 1):
        start, duration = max(0.0, i * span - 0.5), span + 1.0
        for j, name in enumerate(CHORDS[i % len(CHORDS)][2:4]):
            tone = synth.pan(bowed(synth.note(name) * 2, duration, rng, tremolo=6.0) * swell(duration, 0.8, 1.0), -0.6 + 1.2 * j)
            for ch in range(2):
                synth.place(out[ch], tone[ch], start)
    return out


def chaser(total, rng):
    """A reed voice chasing the tune a beat behind and a third below, and singing the middle (phase 2)."""
    events = []
    for bar, beat, name in TUNE:
        f = synth.note(name) * 2 ** (-3 / 12)  # (a third below: a little sour, a little sweet)
        events.append((at(bar, beat) + BEAT, f))
    for bar, beat, name in MIDDLE:
        events.append((at(bar, beat), synth.note(name)))
    return place(total, events, lambda f: reed(f, 0.9, rng) * swell(0.9, 0.05, 0.5), 0.3)


def fiddle(total, rng):
    """A fiddle hacking the tune out, each note sawn three times (phase 3)."""
    events = []
    for bar, beat, name in TUNE + MIDDLE:
        for k in range(3):
            events.append((at(bar, beat) + k * BEAT / 3, synth.note(name) / 2))
    return place(total, events, lambda f: bowed(f, 0.22, rng, bright=1.4) * synth.envelope(0.22, 0.01, 0, curve=1.5), 0.2)


def ostinato(total, rng):
    """A bass sawing the roots in quavers (phase 3)."""
    events = []
    for bar in range(32):
        root = synth.note(CHORDS[bar // 4][0]) / 2
        for k in range(6):
            events.append((bar * BAR + k * BEAT / 2, root * (2 if k == 3 else 1)))
    return place(total, events, lambda f: bowed(f * 2, 0.3, rng, bright=0.6) * synth.envelope(0.3, 0.01, 0, curve=1.6))


def wind(total, rng):
    """Wind through the broken panes (phase 4)."""
    n = int(total * synth.SR)
    gust = synth.smooth_random(total, rng, 0.15, 0.2, 1.0)

    def shape(times, freqs):
        i = np.clip((times * synth.SR).astype(int), 0, n - 1)
        lf = np.log(np.maximum(freqs, 1.0))[None, :]
        return gust[i][:, None] * np.exp(-0.5 * ((lf - np.log(500)) / 0.6) ** 2)

    return synth.spectral_noise(total, rng, shape)


def drone(total, rng):
    out = np.zeros(int(total * synth.SR))
    for name, gain in (("A1", 1.0), ("E2", 0.5), ("A2", 0.25)):
        out += synth.voice(synth.note(name), total, rng, "oo", vibrato=0.003, max_freq=900) * gain
    return synth.pan(out * (0.85 + 0.15 * np.sin(2 * np.pi * 0.07 * synth.seconds(total))), 0.0)


def render(v, rng):
    base = np.random.default_rng(20261007)  # (shared layers identical in every variant: they crossfade in step)
    total = LENGTH + FADE
    beats = [b * BEAT for b in range(32 * 3)]
    bars = [b * BAR for b in range(32)]
    if v == 3:
        # The dark: a drone, a heartbeat, the music box half-remembered and flat, wind.
        out = level(drone(total, base), -31)
        out += level(place(total, [(t,) for t in bars], lambda: thump(base)), -25)
        wet = level(music_box(total, base, cents=-40, slow=True), -17)
        dry = out + synth.stereo(level(wind(total, base), -30), level(wind(total, base), -30))
    else:
        dry = level(hum(total, base), -33)
        if v < 2:
            wet = level(pad(total, base), -26) + level(harp(total, base), -24)
            box = music_box(total, base)
            wet += box / (np.max(np.abs(box)) + 1e-9) * 10 ** (-10 / 20)
            wings = place(total, [(t,) for t in bars], lambda: drum(base))
            dry += level(np.stack([synth.highpass(c, 45) for c in wings]), -29)  # (felt more than heard: his wingbeats)
            if v == 1:
                extra = np.random.default_rng(31)
                wet += level(tremble(total, extra), -29) + level(chaser(total, extra), -23)
                dry += level(place(total, [(t + 2 * BEAT,) for t in bars], lambda: drum(extra, deep=False)), -31)  # (doubling into each bar)
                dry += level(place(total, [(t + BEAT / 2,) for t in beats], lambda: shake(extra), 0.6), -36)
        else:
            # Afoot: the waltz broken.
            extra = np.random.default_rng(47)
            wet = level(pad(total, base, thin=True), -34) + level(fiddle(total, extra), -20) + level(ostinato(total, extra), -23)
            dry += level(place(total, [(t,) for t in beats], lambda: stamp(extra)), -25)
            dry += level(place(total, [(t + BEAT / 2,) for t in beats], lambda: snip(extra), 0.5), -28)
    dry = synth.loop_crossfade(dry, FADE)
    wet = synth.loop_crossfade(wet, FADE)
    n = wet.shape[1]
    hall = synth.reverb_ir(3.2, 2.6, np.random.default_rng(19), brightness=0.3)  # (the glasshouse: iron and glass)
    for ch in range(2):
        ir = np.zeros(n)
        ir[:len(hall[ch])] = hall[ch]
        reverberant = np.fft.irfft(np.fft.rfft(wet[ch]) * np.fft.rfft(ir), n)
        dry[ch] += 0.55 * wet[ch] + 0.7 * level(reverberant, 20 * np.log10(rms(wet[ch])))
    return dry
