"""The Hold, That Would Not Let Go: the first Boss's music, a 60 s seamless stereo loop in C minor (the Hold's own C
drone) at 64 bpm (16 bars of 4), in two variants sharing one loop, so the game crossfades them in step.
Built from the Hold's world (music_hold: its deep breathing drone, its stone, its far voices) but driven: menacing,
not frantic (it's the first Boss).

1, the hands: a great heart beating in the rock on every beat (lub-dub, very low, felt); a deep stone drum on each
bar's first beat (a slab struck: a deep boom, a dull stony knock, grit), stones knocked together on the third; the
Hold's deep drone on C, its two strings drifting; a low groaning bass voice on each bar's root (rough, many throats);
a low wordless choir of men's voices holding Cm - Ab - Fm - Db/C (four bars each; the Db over the C drone pulling
back down to C); the cave breathing in and out every two bars under it all; far off, twice, the Hold's sigh of many
voices and the rock groaning.
2, the face joins (at half health): the same loop more urgent: the heart doubled (racing, a beat every half beat),
the stone drum on one and three with a doubled hit before each bar, stones knocking on two and four, a higher
keening voice above the choir (wordless, sliding, crying up and sighing down), and stone grinding up into every
bar's downbeat.

Everything is placed circularly over exactly one loop (events wrap round its end, the drone's frequencies make whole
cycles, the cave reverb is convolved circularly), so the seam is exact: nothing is crossfaded.
Uses the_hold_wake's kit (grind, the souls' voice), music_hold (the cave's reverb, the sigh), hold_creak (groans).
`synth` and `np` are injected by tools/audio/build_sound.py.
"""
LOOP = True
FOLDER = "Music"
VARIANTS = 2
PEAK = [0.8, 0.86]

SR = synth.SR
BEAT = 60 / 64               # 64 bpm: a great heart at rest
BAR = 4 * BEAT
SPAN = 4 * BAR               # a chord
LENGTH = 16 * BAR            # 60 s
N = int(round(LENGTH * SR))
CHORDS = [  # (bass root, choir notes, vowel)
    ("C2", ["C3", "Eb3", "G3"], "oh"),
    ("Ab1", ["C3", "Eb3", "Ab3"], "oo"),
    ("F1", ["C3", "F3", "Ab3"], "oh"),
    ("Db2", ["Db3", "F3", "Ab3"], "oo"),
]
# The keening voice (2): one phrase every two bars, (from, to): it slides from one to the other and sighs off.
KEEN = [("G4", "Eb4"), ("Eb5", "C5"), ("C5", "Ab4"), ("Eb5", "G4"),
        ("Ab4", "F4"), ("F5", "C5"), ("Ab4", "Db5"), ("C5", "C5")]
SIGHS = [(17.0, -0.45), (47.0, 0.4)]
GROANS = [(8.0, 0.55, 0), (38.5, -0.6, 1)]


def rms(x):
    return np.sqrt(np.mean(np.asarray(x) ** 2)) + 1e-12


def level(x, db):
    return x / rms(x) * 10 ** (db / 20)


def peak(x, db):
    return x / (np.max(np.abs(x)) + 1e-9) * 10 ** (db / 20)


def blank():
    return np.zeros((2, N))


def load(name):
    import importlib.util, os
    spec = importlib.util.spec_from_file_location(name, os.path.join(os.path.dirname(__file__), name + ".py"))
    module = importlib.util.module_from_spec(spec)
    module.synth, module.np = synth, np
    spec.loader.exec_module(module)
    return module


def shape(x, gain):
    """Filter circularly by a gain curve over frequency: periodic in, periodic out."""
    freqs = np.fft.rfftfreq(x.shape[-1], 1 / SR)
    return np.fft.irfft(np.fft.rfft(x, axis=-1) * gain(freqs), x.shape[-1], axis=-1)


def cycles(f):
    """A frequency with a whole number of cycles in the loop."""
    return round(f * LENGTH) / LENGTH


def put(out, tone, at, position=0.0, gain=1.0):
    """Add a sound into the loop at `at` seconds, panned (mono) or as is (stereo); what runs past the end wraps."""
    placed = synth.pan(tone * gain, position) if tone.ndim == 1 else tone * gain
    s = int(round(at * SR)) % N
    k = 0
    while k < placed.shape[1]:
        m = min(placed.shape[1] - k, N - s)
        out[:, s:s + m] += placed[:, k:k + m]
        k += m
        s = 0


def swell(duration, attack, release):
    t = synth.seconds(duration)
    return np.sin(np.minimum(np.clip(t / attack, 0, 1), np.clip((duration - t) / release, 0, 1)) * np.pi / 2) ** 2


def norm(x):
    return x / (np.max(np.abs(x)) + 1e-9)


# ---------------------------------------------------------------- the pulse

def heartbeat(rng, gain=1.0):
    """One beat of a great heart deep in the rock: a soft, very low thump bending down (lub) and a fainter second
    just behind (dub); a swell more than a hit, through stone. Its overtones rounded up so small speakers carry it."""
    d = 0.75
    out = np.zeros(int(d * SR))
    for offset, g, f_hi, f_lo in ((0.0, 1.0, 72, 44), (0.24, 0.55, 80, 50)):
        bd = 0.5
        t = synth.seconds(bd)
        f = f_lo + (f_hi - f_lo) * np.exp(-t / 0.035)
        thump = np.sin(2 * np.pi * np.cumsum(f) / SR) * np.exp(-t / 0.1)
        thump += 0.4 * synth.lowpass(synth.white(bd, rng), 160) * np.exp(-t / 0.05)
        thump *= np.sin(np.clip(t / 0.012, 0, 1) * np.pi / 2) ** 2
        synth.place(out, thump * g, offset)
    out = np.tanh(2.2 * norm(out))
    return synth.lowpass(out, 380) * gain


def stone_drum(rng, gain=1.0):
    """A slab of stone struck like a drum: a deep boom bending down, a dull stony knock (no ring), a hard edge,
    a little grit falling off."""
    d = 1.6
    t = synth.seconds(d)
    f = 36 * (1 + 1.4 * np.exp(-t / 0.05))
    boom = np.sin(2 * np.pi * np.cumsum(f) / SR) * np.exp(-t / 0.4)
    knock = synth.bandpass(synth.white(d, rng), 170, 1.2) * np.exp(-t / 0.05)
    edge = synth.lowpass(synth.white(d, rng), 2500) * np.exp(-t / 0.004)
    grit = np.zeros_like(t)
    for _ in range(8):
        g = synth.bandpass(synth.white(0.012, rng), rng.uniform(1500, 3500), 2.0) * synth.decay(0.012, 0.0025)
        synth.place(grit, g * rng.uniform(0.2, 1.0), rng.uniform(0.08, 0.7))
    out = norm(boom) + 0.6 * norm(knock) + 0.25 * norm(edge) + 0.08 * norm(grit)
    out = np.tanh(1.8 * norm(out)) * np.clip(t / 0.0008, 0, 1)
    return out * gain


def stones(rng, gain=1.0):
    """Two stones knocked together, hard and dull: a hollow knock with a dry click, a second fainter just after."""
    d = 0.35
    out = np.zeros(int(d * SR))
    for offset, g in ((0.0, 1.0), (rng.uniform(0.015, 0.03), 0.45)):
        bd = 0.25
        t = synth.seconds(bd)
        fr = rng.uniform(290, 340)
        k = np.sin(2 * np.pi * fr * (1 + 0.3 * np.exp(-t / 0.004)) * t) * np.exp(-t / 0.025)
        k += 0.4 * np.sin(2 * np.pi * fr * 2.4 * t) * np.exp(-t / 0.01)
        k += 0.5 * synth.bandpass(synth.white(bd, rng), 2200, 1.0) * np.exp(-t / 0.003)
        synth.place(out, k * g * np.clip(t / 0.0004, 0, 1), offset)
    return out * gain


# ---------------------------------------------------------------- the cave

def drone(rng):
    """The Hold's drone (music_hold): two deep strings on C1 a hair apart, drifting in and out of tune, an octave
    and a twelfth faint above for small speakers; swelling and sinking once every chord. Exactly periodic."""
    t = np.arange(N) / SR
    f0 = cycles(32.7)
    breath = 0.7 + 0.3 * (0.5 - 0.5 * np.cos(2 * np.pi * cycles(4 / LENGTH) * t))
    out = np.zeros((2, N))
    for ch in range(2):
        detune = 0.4 * np.sin(2 * np.pi * cycles(2 / LENGTH) * t + rng.uniform(0, 6)) \
            + 0.2 * np.sin(2 * np.pi * cycles(5 / LENGTH) * t + rng.uniform(0, 6))
        drift = 2 * np.pi * np.cumsum(detune) / SR
        a = 2 * np.pi * f0 * t
        low = np.sin(a) + 0.85 * np.sin(a + drift + 0.3 * ch)
        upper = 0.2 * (np.sin(2 * a + 1.0) + 0.8 * np.sin(2 * (a + drift) + 2.0)) \
            + 0.09 * (np.sin(3 * a + 0.4) + 0.8 * np.sin(3 * (a + drift) + 1.3 + 0.5 * ch))
        out[ch] = (low + upper) * breath
    return out


def cave_breath(rng, kit):
    """The cave breathing every two bars (in over one, out over the next), a dark hush of air and the rock's mass."""
    out = blank()
    breath_kit = load("hold_breath_in")
    for b in range(0, 16, 2):
        d = 2 * BAR
        flow = breath_kit.flow_curve(d, 0.45, 1.6)
        air = kit.gust_air(d, flow, rng, inward=True, size=2.0)
        put(out, synth.lowpass(air, 1400), b * BAR - 0.2 * BAR, rng.uniform(-0.3, 0.3))
    return out


# ---------------------------------------------------------------- voices

def choir(rng):
    """The low wordless choir: three or four men to each note, a hair apart, holding each chord for four bars,
    swelling in a little before it and fading into the next."""
    out = blank()
    for i, (_, notes, vowel) in enumerate(CHORDS):
        d = SPAN + 3.0
        env = swell(d, 3.0, 3.5)
        for j, name in enumerate(notes):
            f = synth.note(name)
            sung = np.zeros(int(d * SR))
            for _ in range(3):
                sung += synth.voice(f * rng.uniform(0.996, 1.004), d, rng, vowel, vibrato=0.006, max_freq=2600)
            put(out, sung * env / (1 + 0.15 * j), i * SPAN - 1.5, -0.55 + 0.55 * j)
    return shape(out, lambda f: 1 / (1 + (f / 2200) ** 2))


def bass(rng, kit):
    """A low groaning bass voice on each bar's root: many rough throats as one (the souls' voice), swelling on the
    downbeat and sinking through the bar."""
    out = blank()
    for b in range(16):
        root = synth.note(CHORDS[b // 4][0])
        d = BAR + 0.6
        t = synth.seconds(d)
        v = kit.souls(d, rng, root, 4, "oo", rasp=0.35, breath=0.15, spread=0.006, max_freq=1400, dark=900)
        env = np.clip(t / 0.18, 0, 1) ** 1.5 * (0.55 + 0.45 * np.exp(-t / 1.0)) * np.clip((d - t) / 0.6, 0, 1)
        put(out, v * env * (1.0 if b % 4 == 0 else 0.8), b * BAR)
    return out


def keening(rng):
    """The face's voice (2): a higher wordless keening above the choir, one phrase every two bars, sliding from one
    note to the next (late, slowly) and sighing off; a wide, trembling vibrato; breathy."""
    out = blank()
    for i, (a, b) in enumerate(KEEN):
        d = 2 * BAR + 0.4
        fa, fb = synth.note(a), synth.note(b)
        u = np.linspace(0, 1, int(d * SR))
        slide = np.clip((u - 0.45) / 0.25, 0, 1)
        slide = slide * slide * (3 - 2 * slide)
        pitch = fa * (fb / fa) ** slide * (1 - 0.03 * np.clip((u - 0.85) / 0.15, 0, 1))  # sighing off at the end
        v = synth.voice(pitch, d, rng, "ah", vibrato=0.018, max_freq=4500)
        v2 = synth.voice(pitch * 1.006, d, rng, "ah", vibrato=0.02, max_freq=4500)
        freqs = np.fft.rfftfreq(len(u), 1 / SR)
        air = np.fft.irfft(np.fft.rfft(synth.white(d, rng)) * synth.formants(freqs * 0.55, "ah"), len(u))
        line = norm(v + 0.7 * v2) + 0.25 * norm(air)
        line *= swell(d, 1.2, 2.2)
        put(out, line, i * 2 * BAR + 0.1, 0.25 if i % 2 else -0.25)
    return shape(out, lambda f: 1 / (1 + (f / 3500) ** 2) * (f / 200) ** 2 / (1 + (f / 200) ** 2))


# ---------------------------------------------------------------- the pulse laid out

def hearts(rng, between=False):
    """The heart on every beat (each bar's first a little stronger), or (2) only between the beats, doubling it."""
    out = blank()
    for i in range(16 * 4):
        at = i * BEAT + (BEAT / 2 if between else 0.0)
        put(out, heartbeat(rng, (0.75 if between else 1.0) * (1.0 if i % 4 == 0 else 0.8)), at)
    return out


def drums(rng, extra=False):
    """The stone drum on each bar's first beat; or (2) the hits it adds: on the third, and doubled before each bar."""
    out = blank()
    for b in range(16):
        if not extra:
            put(out, stone_drum(rng, 1.0), b * BAR)
        else:
            put(out, stone_drum(rng, 0.7), b * BAR + 2 * BEAT)
            put(out, stone_drum(rng, 0.5), b * BAR + 3.5 * BEAT)
    return out


def knocks(rng, beats):
    out = blank()
    for b in range(16):
        for k in beats:
            put(out, stones(rng, 1.0 if k == 2 else 0.8), b * BAR + k * BEAT, rng.uniform(-0.4, 0.4))
    return out


def grinding(rng, kit):
    """(2) Stone grinding up into each bar's downbeat: a drag swelling over beats three and four, cut by the drum."""
    out = blank()
    for b in range(16):
        d = 2 * BEAT
        speed = np.clip(synth.seconds(d) / d, 0, 1) ** 1.6
        g = kit.grind(d, speed, rng, 230, grit=0.6) * np.clip((d - synth.seconds(d)) / 0.03, 0, 1)
        put(out, g, b * BAR + 2 * BEAT, 0.45 if b % 2 else -0.45)
    return out


# ---------------------------------------------------------------- the cave's sound

def cave(wet, rng):
    """music_hold's cave, applied circularly over the loop: the tail of the end rings on into the start."""
    pair = load("music_hold").cave_ir(rng)
    reverberant = np.zeros_like(wet)
    for ch in range(2):
        ir = np.zeros(N)
        m = min(N, len(pair[ch]))
        ir[:m] = pair[ch][:m] * np.linspace(1, 0, m) ** 0.5  # (a 6 s tail: kept short of the next phrase's wash)
        reverberant[ch] = np.fft.irfft(np.fft.rfft(wet[ch]) * np.fft.rfft(ir), N)
    reverberant = shape(reverberant, lambda f: (1 + 0.4 * np.exp(-0.5 * (np.log(np.maximum(f, 1) / 140) / 0.6) ** 2))
                        / (1 + (f / 3000) ** 2))
    return level(reverberant, 20 * np.log10(rms(wet)))


def limit(x, db):
    """Brought to `db` RMS, its few hard peaks (the drum's first swing) rounded off rather than setting the level."""
    x = level(x, db)
    knee = 0.5
    over = np.abs(x) > knee
    x[over] = np.sign(x[over]) * (knee + (1 - knee) * np.tanh((np.abs(x[over]) - knee) / (1 - knee)))
    return x


def render(v, rng):
    kit = load("the_hold_wake")
    hold = load("music_hold")
    creak = load("hold_creak")
    base = np.random.default_rng(20261102)  # shared by both variants: identical layers, so they crossfade in step
    urgent = v >= 1  # (variants count from 0: the second is 1)

    dry = level(drone(base), -27)
    dry += level(cave_breath(base, kit), -36)
    pulse = level(hearts(base), -24)
    drum = peak(drums(base), -8)
    knock = peak(knocks(base, (2,)), -21)
    wet = level(choir(base), -25) + level(bass(base, kit), -28)
    far = blank()
    for time, position in SIGHS:
        put(far, hold.sigh(base), time, position)
    for time, position, kind in GROANS:
        put(far, peak(hold.groan(creak, base, kind), 0), time, position, 0.8)
    wet += peak(far, -24)

    if urgent:  # added on top: the shared layers stay as they are
        extra = np.random.default_rng(1102)
        pulse += level(hearts(extra, between=True), -26)
        drum += peak(drums(extra, extra=True), -10)
        knock += peak(knocks(extra, (1, 3)), -23)
        wet += level(keening(extra), -24)
        wet += level(grinding(extra, kit), -29)

    dry += pulse + drum * 0.8 + knock
    wet += drum * 0.5 + knock * 0.6 + pulse * 0.3
    out = dry + 0.3 * wet + 0.9 * cave(wet, np.random.default_rng(1103))
    out = shape(out, lambda f: (f / 18) ** 4 / (1 + (f / 18) ** 4))
    return limit(out, -15 if urgent else -16.5)
