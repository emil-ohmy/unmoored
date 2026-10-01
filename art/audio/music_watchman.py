"""The Watchman: miniboss music, a 48 s seamless loop in A minor (the Orchard's key), 3/4 at 60 bpm (16 bars of 3 s),
in two variants sharing one loop so the game crossfades them in step. 1: he's found. 2: half way, straw spilling:
the same music, worse (a heartbeat under it, the knocking doubled, the fiddle an octave up and trembling, his hook
rasping every four bars).

A child's game played in the dark ("who moved?"): a dry wooden knock on every beat like a stake in the earth, tick
tock tock; a lone fiddle, scratchy and a little out of tune, creeping through the opening of Last Autumn's tune
slowed to a bar a note, sliding between them; a low bowed hum on A under it all; straw rattling now and then; and
twice in the loop everything but the hum stops dead for two beats (as though someone turned round), then goes on.
The music box from Last Autumn comes back in fragments, slower and flat. Chords: Am - F - Dm - E, two bars each.
`synth` and `np` are injected by tools/audio/build_sound.py.
"""
LOOP = True
FOLDER = "Music"
PEAK = 0.8
VARIANTS = 2

BEAT = 1.0
BAR = 3 * BEAT
LENGTH = 16 * BAR   # 48 s
FADE = 4.0
CHORDS = [("A2", "E3", "C4"), ("F2", "C3", "A3"), ("D3", "A3", "F4"), ("E2", "B2", "G#3")] * 2  # two bars each
# The fiddle: one note a bar (None: it rests), sliding from the last.
FIDDLE = ["E4", "C4", "A3", "G#3", "A3", "F4", "E4", None, "E4", "C4", "B3", "A3", "F3", "E3", "G#3", None]
# Everything but the hum stops dead: (bar from 0, from beat, to beat).
FREEZES = [(7, 1, 3), (15, 1, 3)]
BOX = [(4, 0, "E5"), (4, 1.5, "C5"), (4, 2.5, "A4"), (12, 0, "B4"), (12, 1.5, "G#4"), (12, 2.5, "E4")]


def rms(x):
    return np.sqrt(np.mean(np.asarray(x) ** 2)) + 1e-12


def level(x, db):
    return x / rms(x) * 10 ** (db / 20)


def swell(duration, attack, release):
    t = synth.seconds(duration)
    return np.sin(np.minimum(np.clip(t / attack, 0, 1), np.clip((duration - t) / release, 0, 1)) * np.pi / 2) ** 2


def frozen(at):
    """Whether time `at` (in the loop) falls in a freeze."""
    at = at % LENGTH
    return any(bar * BAR + a * BEAT - 0.02 <= at < bar * BAR + b * BEAT for bar, a, b in FREEZES)


def gate(x):
    """Silences a stereo or mono layer through the freezes (a quick fade either side, no clicks)."""
    t = np.arange(x.shape[-1]) / synth.SR
    g = np.ones_like(t)
    for bar, a, b in FREEZES:
        for k in (0, 1):  # (and once round the loop, for the crossfaded tail)
            s, e = k * LENGTH + bar * BAR + a * BEAT, k * LENGTH + bar * BAR + b * BEAT
            g = np.minimum(g, np.clip(np.maximum((s - t) / 0.05, (t - e) / 0.12), 0, 1))
    return x * g


def knock(rng, low):
    """A hollow wooden knock (a stake struck): two damped wood modes and the click of it."""
    d = 0.35
    t = synth.seconds(d)
    f = (420 if low else 760) * rng.uniform(0.98, 1.02)
    out = np.sin(2 * np.pi * f * t) * np.exp(-t / 0.035) + 0.5 * np.sin(2 * np.pi * f * 2.31 * t) * np.exp(-t / 0.018)
    click = synth.bandpass(synth.white(d, rng), 2500, 1.5) * np.exp(-t / 0.004)
    return out + 0.4 * click


def rattle(rng, d=0.4):
    """Straw rattling: a dry papery shake."""
    x = synth.lowpass(synth.highpass(synth.white(d, rng), 2500), 7000) * synth.envelope(d, 0.12, 0, curve=2.0)
    return x * (1 + 0.6 * np.sin(2 * np.pi * rng.uniform(14, 20) * synth.seconds(d)))


def thump(rng):
    """A heartbeat: lub, dub."""
    d = 0.7
    out = np.zeros(int(d * synth.SR))
    for at, gain in ((0.0, 1.0), (0.22, 0.6)):
        n = int(0.3 * synth.SR)
        beat = synth.sine(synth.glide(68, 38, 0.3, 0.6), 0.3) * synth.decay(0.3, 0.07) * gain
        synth.place(out, beat[:n], at)
    return synth.lowpass(out, 400)


def events(total, times, sound):
    out = np.zeros(int(total * synth.SR))
    for at in times:
        if at < total and not frozen(at):
            synth.place(out, sound(at), at)
    return out


def fiddle(total, rng, octave=1.0, tremble=0.0):
    """The fiddle: a bowed string (bright, a little nasal, a bow's scratch in it), each note a bar, sliding from the
    last; out of tune by a few cents, and drifting."""
    n = int(total * synth.SR)
    pitch = np.zeros(n)
    amp = np.zeros(n)
    prev = None
    for i in range(int(total / BAR) + 1):
        name = FIDDLE[i % len(FIDDLE)]
        s, e = int(i * BAR * synth.SR), min(n, int((i + 1) * BAR * synth.SR))
        if s >= n:
            break
        if name is None:
            prev = None
            continue
        f = synth.note(name) * octave * 2 ** (rng.uniform(-14, 6) / 1200)
        seg = e - s
        slide = int(0.35 * synth.SR) if prev is not None else 0
        p = np.full(seg, f)
        if slide:
            p[:slide] = np.geomspace(prev, f, slide)
        pitch[s:e] = p
        env = np.ones(seg)
        a, r = int(0.25 * synth.SR), int(0.6 * synth.SR)
        env[:a] = np.linspace(0.3 if slide else 0, 1, a)
        if FIDDLE[(i + 1) % len(FIDDLE)] is None or (i + 1) * BAR >= total:
            env[-r:] *= np.linspace(1, 0, r) ** 2
        amp[s:e] = env * (0.85 + 0.15 * np.sin(np.linspace(0, np.pi, seg)))
        prev = f
    t = np.arange(n) / synth.SR
    vib = 1 + 0.006 * np.sin(2 * np.pi * 5.2 * t) * np.clip(synth.smooth_random(total, rng, 0.4)[:n] * 1.5, 0, 1)
    phase = 2 * np.pi * np.cumsum(np.maximum(pitch, 1.0) * vib) / synth.SR
    tone = np.zeros(n)
    for k in range(1, 14):
        tone += np.sin(k * phase) / k * (1.0 if k % 2 else 0.8)
    body = synth.bandpass(tone, 600, 1.2) + 0.7 * synth.bandpass(tone, 2800, 1.5) + 0.25 * tone
    bow = synth.bandpass(synth.white(total, rng)[:n], 3500, 1.0) * 0.08
    out = (body + bow) * amp
    if tremble:
        out *= 1 - tremble * (0.5 + 0.5 * np.sin(2 * np.pi * 11 * t))
    return out


def hum(total, rng):
    """A low bowed hum on A, never stopping (the one thing that doesn't freeze)."""
    out = np.zeros(int(total * synth.SR))
    for name, gain in (("A1", 1.0), ("E2", 0.5)):
        tone = synth.voice(synth.note(name), total, rng, "oo", vibrato=0.004, max_freq=1200) * gain
        out += tone * (0.8 + 0.2 * np.sin(2 * np.pi * 0.11 * synth.seconds(total)))
    return out


def chords(total, rng):
    """The chords, low and soft, on a reed organ gone damp."""
    out = np.zeros(int(total * synth.SR))
    span = 2 * BAR
    for i in range(int(total / span) + 1):
        d = span + 1.0
        for name in CHORDS[i % len(CHORDS)]:
            t = synth.seconds(d)
            f = synth.note(name)
            tone = sum(np.sin(2 * np.pi * f * k * t + rng.uniform(0, 6)) / k for k in range(1, 6))
            synth.place(out, synth.lowpass(tone, 1500) * swell(d, 1.2, 1.5), max(0.0, i * span - 0.5))
    return out


def music_box(total, rng):
    out = np.zeros(int(total * synth.SR))
    for bar, beat, name in BOX:
        for k in (0, 1):
            at = k * LENGTH + bar * BAR + beat * BEAT
            if at < total:
                synth.place(out, synth.glass(synth.note(name) * 2 ** (-40 / 1200), 3.0, rng), at)
    return out


def rasp(rng):
    """His hook drawn back, in the music: a rising scrape."""
    d = 1.2
    x = synth.bandpass(synth.white(d, rng), synth.glide(1200, 4200, d, 1.5), 3.0) * synth.envelope(d, 1.0, 0, curve=3.0, peak_at=1.05)
    return x


def render(v, rng):
    base = np.random.default_rng(20261002)  # shared by both variants: identical layers, so they crossfade in step
    worse = v >= 1  # (variants count from 0: the second is 1)
    total = LENGTH + FADE
    beats = [b * BEAT for b in range(int(total / BEAT) + 1)]
    knocks = events(total, beats, lambda at: knock(base, round(at / BEAT) % 3 == 0))
    dry = synth.pan(level(knocks, -24), 0.15)
    straw_at = [at + 0.5 for at in beats if base.random() < 0.18]
    dry += synth.pan(level(events(total, straw_at, lambda at: rattle(base)), -44), -0.4)
    dry += synth.pan(level(hum(total, base), -28), 0.0)
    wet = synth.pan(level(gate(fiddle(total, base)), -21), -0.2)
    wet += synth.pan(level(gate(chords(total, base)), -31), 0.2)
    wet += synth.pan(level(music_box(total, base), -30), 0.35)
    if worse:
        extra = np.random.default_rng(14)
        dry += synth.pan(level(events(total, [b * BAR for b in range(int(total / BAR) + 1)], lambda at: thump(extra)), -19), 0.0)
        dry += synth.pan(level(events(total, [at + 0.5 for at in beats], lambda at: knock(extra, False)), -31), -0.25)
        dry += synth.pan(level(events(total, [at + 0.5 for at in beats if extra.random() < 0.4], lambda at: rattle(extra, 0.3)), -42), 0.4)
        wet += synth.pan(level(gate(fiddle(total, extra, octave=2.0, tremble=0.6)), -27), 0.3)
        wet += synth.pan(level(events(total, [b * 4 * BAR + 1.8 * BAR for b in range(5)], lambda at: rasp(extra)), -30), 0.0)
    dry = synth.loop_crossfade(dry, FADE)
    wet = synth.loop_crossfade(wet, FADE)
    n = wet.shape[1]
    hall = synth.reverb_ir(3.0, 2.4, np.random.default_rng(18), brightness=0.3)  # (open air: a field, a wall round it)
    for ch in range(2):
        ir = np.zeros(n)
        ir[:len(hall[ch])] = hall[ch]
        reverberant = np.fft.irfft(np.fft.rfft(wet[ch]) * np.fft.rfft(ir), n)
        dry[ch] += 0.5 * wet[ch] + 0.6 * level(reverberant, 20 * np.log10(rms(wet[ch])))
    return dry
