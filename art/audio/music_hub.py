"""The Ferryman's jetty: the Hub's music, and the game's home theme. A 72 s seamless stereo loop in D major (the warm
D of the Mooring posts' chime, under the Drowned Dark's D minor), a slow barcarolle: 6/8 at 40 to the dotted crotchet
(a bar every 3 s, 24 bars), rocking like a boat at its mooring.

Layers: an old reed organ (a harmonium, a little worn) holding the chords softly, its two reeds a hair apart; a warm
drone on D and A under everything; a plucked string keeping the boat's rock (the root, then the fifth, a note more now
and then); and the tune on the harmonium's one solo reed: "A, B A F#, E" (a question, then its answer falling), a
second phrase climbing to B and sinking, then a long rest; it comes back once through G to G minor (the B gone flat:
the one ache in it), rests again, and a last phrase leads back round. One of its reeds is worn: its B speaks a few
cents flat, every time. At each phrase the bellows wheeze softly as they're pumped. Twice in the loop, in the rests,
the Far shore's bell, far off across the water (hub_far_bell's bell). Open air over still water: a soft, misty tail.

Chords (two bars each): D - Bm - G - A | D - Bm - G - Gm | Bm - G - Em7 - Asus4, A.
Everything is placed circularly over exactly one loop (notes and their tails wrap round its end, the drone makes
whole cycles, the reverb is convolved circularly), so the seam is exact: nothing is crossfaded.
`synth` and `np` are injected by tools/audio/build_sound.py.
"""
LOOP = True
FOLDER = "Music"
PEAK = 0.5  # sets the loudness: quiet, out of the way (about -18 LUFS; the Areas' themes are -14 to -16)

SR = synth.SR
EIGHTH = 0.5                 # 6/8 at 40 to the dotted crotchet
BAR = 6 * EIGHTH             # 3 s
LENGTH = 24 * BAR            # 72 s
N = int(round(LENGTH * SR))

# name: (pad notes, pluck notes: root, fifth, octave, third)
CHORDS = {
    "D": (["F#3", "A3", "D4"], ["D3", "A3", "D4", "F#3"]),
    "Bm": (["F#3", "B3", "D4"], ["B2", "F#3", "B3", "D3"]),
    "G": (["G3", "B3", "D4"], ["G2", "D3", "G3", "B2"]),
    "A": (["E3", "A3", "C#4"], ["A2", "E3", "A3", "C#3"]),
    "Gm": (["G3", "Bb3", "D4"], ["G2", "D3", "G3", "Bb2"]),
    "Em7": (["G3", "B3", "D4"], ["E2", "B2", "E3", "G2"]),
    "Asus": (["E3", "A3", "D4"], ["A2", "E3", "A3", "D3"]),
}
BARS = ["D", "D", "Bm", "Bm", "G", "G", "A", "A",
        "D", "D", "Bm", "Bm", "G", "G", "Gm", "Gm",
        "Bm", "Bm", "G", "G", "Em7", "Em7", "Asus", "A"]
# The tune: bar (from 1) -> [(eighth from 0, note, length in eighths)]. Bars 9-12 and 17-20 rest.
TUNE = {
    1: [(0, "A4", 3), (3, "B4", 1), (4, "A4", 1), (5, "F#4", 1)],
    2: [(0, "E4", 4)],
    3: [(0, "F#4", 3), (3, "G4", 1), (4, "F#4", 1), (5, "D4", 1)],
    4: [(0, "B3", 5)],
    5: [(0, "D4", 1), (1, "G4", 1), (2, "A4", 1), (3, "B4", 3)],
    6: [(0, "A4", 2), (2, "G4", 4)],
    7: [(0, "E4", 1), (1, "F#4", 1), (2, "G4", 1), (3, "F#4", 2), (5, "D4", 1)],
    8: [(0, "E4", 5)],
    13: [(0, "D4", 1), (1, "G4", 1), (2, "A4", 1), (3, "B4", 3)],
    14: [(0, "A4", 2), (2, "G4", 4)],
    15: [(0, "D4", 1), (1, "G4", 1), (2, "A4", 1), (3, "Bb4", 3)],
    16: [(0, "A4", 2), (2, "G4", 1), (3, "D4", 3)],
    21: [(0, "G4", 3), (3, "F#4", 1), (4, "E4", 1), (5, "D4", 1)],
    22: [(0, "E4", 4)],
    23: [(0, "E4", 3), (3, "D4", 1), (4, "C#4", 1), (5, "D4", 1)],
    24: [(0, "C#4", 4), (5, "E4", 1)],  # (the E lifts into the A at the top of the loop)
}
WORN = {"B4": -9.0}          # the worn reed, in cents: the same every time
PHRASES = [1, 13, 21]        # the bellows wheeze just before these bars
BELLS = [(9, -0.55), (17, -0.45)]  # (bar, position): the Far shore's bell, in the rests


def rms(x):
    return np.sqrt(np.mean(np.asarray(x) ** 2)) + 1e-12


def level(x, db):
    return x / rms(x) * 10 ** (db / 20)


def peak(x, db):
    return x / (np.max(np.abs(x)) + 1e-9) * 10 ** (db / 20)


def blank():
    return np.zeros((2, N))


def norm(x):
    return x / (np.max(np.abs(x)) + 1e-9)


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


def band(x, centre, width):
    return shape(x, lambda f: np.exp(-0.5 * (np.log(np.maximum(f, 1) / centre) / width) ** 2))


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


def when(bar, eighth=0.0):
    return (bar - 1) * BAR + eighth * EIGHTH


# ---------------------------------------------------------------- the instruments

def reed(f, length, rng, solo=True, attack=0.08, release=0.35):
    """A harmonium reed held for `length` s: it speaks a hair flat and settles (a worn reed, slow to speak), the
    bellows' pressure sagging a little over a long note and wavering; two reeds a hair apart (a slow beat). The solo
    stop is reedy (odd harmonics strong, a nasal bump near 1.1 kHz); the chord stop soft and round."""
    d = length + release
    t = synth.seconds(d)
    bend = 1 - 0.005 * np.exp(-t / 0.06)
    phase = 2 * np.pi * np.cumsum(f * bend) / SR
    out = np.zeros_like(t)
    top = 10 if solo else 6
    for detune, share in ((1.0, 1.0), (1.0011, 0.5)):  # the second reed, about 2 cents sharp
        for k in range(1, top + 1):
            if k * f > 5000:
                break
            a = (1.0 / k ** (1.3 if solo else 1.7)) * (1.0 if k % 2 else (0.6 if solo else 0.8))
            if solo:
                a *= 1 + 0.6 * np.exp(-0.5 * (np.log(k * f / 1100) / 0.35) ** 2)
            out += share * a * np.sin(k * detune * phase + rng.uniform(0, 6))
    bellows = (1 - 0.1 * np.clip(t / max(length, 0.1), 0, 1)) * (1 + 0.025 * np.sin(2 * np.pi * rng.uniform(0.25, 0.45) * t + rng.uniform(0, 6)))
    up = np.sin(np.clip(t / attack, 0, 1) * np.pi / 2) ** 2
    down = np.cos(np.clip((t - length) / release, 0, 1) * np.pi / 2) ** 2
    out *= bellows * up * down
    # the air through the reed: a little breath, with the note
    air = band(rng.normal(0, 1, len(t)), min(f * 3, 3500), 0.5) * up * down
    return out + norm(air) * np.max(np.abs(out)) * (0.035 if solo else 0.02)


def pluck(f, rng, gain=1.0):
    """A plucked string, gut, soft: a round tone mellowing as it rings (the highs die first), its partials a hair
    stretched (a real string's stiffness), a little thump of the body and the fingertip at the start."""
    d = 3.2
    t = synth.seconds(d)
    out = np.zeros_like(t)
    for k in range(1, 9):
        fk = f * k * np.sqrt(1 + 0.0004 * k * k)
        if fk > 6000:
            break
        out += (1.0 / k ** 1.4) * np.sin(2 * np.pi * fk * t + rng.uniform(0, 6)) * np.exp(-t * (0.9 + 0.75 * k))
    finger = band(rng.normal(0, 1, len(t)), f * 4, 0.6) * np.exp(-t / 0.008)
    body = np.sin(2 * np.pi * 110 * t) * np.exp(-t / 0.05)
    out = out + 0.08 * norm(finger) + 0.12 * body
    out *= np.sin(np.clip(t / 0.003, 0, 1) * np.pi / 2) ** 2
    return out * np.cos(np.clip((t - (d - 0.4)) / 0.4, 0, 1) * np.pi / 2) ** 2 * gain


def wheeze(rng):
    """The bellows pumped: a soft wheeze of air (a wooden creak of the treadle in it, very faint)."""
    d = 0.7
    t = synth.seconds(d)
    air = band(rng.normal(0, 1, len(t)), 900, 0.6) * np.sin(np.pi * np.clip(t / d, 0, 1)) ** 2
    creak = np.zeros_like(t)
    at = 0.18
    while at < 0.42:
        i = int(at * SR)
        creak[i:i + 40] += np.hanning(40) * rng.uniform(0.5, 1.0)
        at += rng.uniform(0.012, 0.02)
    creak = band(creak, 600, 0.3)
    return norm(air) + 0.25 * norm(creak)


# ---------------------------------------------------------------- the layers

def drone(rng):
    """D and A low under everything, warm: two strings each, drifting in and out of tune very slowly, swelling a
    little with the phrases. Exactly periodic over the loop."""
    t = np.arange(N) / SR
    out = np.zeros((2, N))
    swell = 0.75 + 0.25 * (0.5 - 0.5 * np.cos(2 * np.pi * cycles(4 / LENGTH) * t + 0.4))
    for ch in range(2):
        for name, g in (("D2", 1.0), ("A2", 0.45)):
            f0 = cycles(synth.note(name))
            detune = 0.25 * np.sin(2 * np.pi * cycles(3 / LENGTH) * t + rng.uniform(0, 6))  # zero-mean: back in phase
            drift = 2 * np.pi * np.cumsum(detune) / SR
            a = 2 * np.pi * f0 * t
            for k, ak in ((1, 1.0), (2, 0.35), (3, 0.14), (4, 0.06)):
                out[ch] += g * ak * (np.sin(k * a + rng.uniform(0, 6)) + 0.8 * np.sin(k * (a + drift) + rng.uniform(0, 6)))
    return out * swell


def pad(rng):
    """The chords on the harmonium's soft stop, each run of a chord held and breathed in and out."""
    out = blank()
    runs = []
    for i, name in enumerate(BARS):
        if runs and runs[-1][0] == name and i % 2 == 1:
            runs[-1][2] += 1
        else:
            runs.append([name, i, 1])
    for name, start, bars in runs:
        length = bars * BAR + 0.6
        for j, note in enumerate(CHORDS[name][0]):
            tone = reed(synth.note(note), length, rng, solo=False, attack=0.9, release=1.6)
            put(out, tone, start * BAR - 0.3 + rng.uniform(0, 0.08), -0.45 + 0.45 * j, 0.9 if j else 1.0)
    return out


def strings(rng):
    """The boat's rock: the root on the bar, the fifth on its second half; every other bar a soft note more; in the
    tune's rests, the octave too."""
    out = blank()
    resting = {b for b in range(1, 25) if b not in TUNE}
    for i, name in enumerate(BARS):
        bar = i + 1
        root, fifth, octave, third = CHORDS[name][1]
        notes = [(0, root, 1.0), (3, fifth, 0.6)]
        if bar % 2 == 0:
            notes.append((4, third, 0.35))
        if bar in resting:
            notes.append((2, octave, 0.3))
            if bar % 2 == 1:
                notes.append((5, third, 0.25))
        for eighth, note, g in notes:
            put(out, pluck(synth.note(note), rng, g * rng.uniform(0.85, 1.0)),
                when(bar, eighth) + rng.uniform(0.0, 0.025), -0.3 + 0.1 * eighth / 5)
    return out


def tune(rng):
    """The solo reed: a held breath on each note (legato, its release overlapping the next a little)."""
    out = blank()
    for bar, notes in TUNE.items():
        for k, (eighth, name, length) in enumerate(notes):
            f = synth.note(name) * 2 ** ((WORN.get(name, 0.0) + rng.uniform(-2.5, 2.5)) / 1200)
            accent = 1.0 if eighth in (0, 3) else 0.82
            tone = reed(f, length * EIGHTH * 0.96, rng, solo=True, attack=0.07, release=0.3)
            put(out, tone, when(bar, eighth) + rng.uniform(-0.012, 0.018), 0.18, accent * rng.uniform(0.9, 1.0))
    return out


def bellows(rng):
    out = blank()
    for bar in PHRASES:
        put(out, wheeze(rng), when(bar) - 0.85, 0.2, rng.uniform(0.8, 1.0))
    return out


def far_bell(rng):
    kit = load("hub_far_bell")
    out = blank()
    for bar, position in BELLS:
        x = kit.bell(12.0, rng, distance=1.4, strength=0.6)
        x *= np.sin(np.clip((12.0 - synth.seconds(12.0)) / 3.0, 0, 1) * np.pi / 2) ** 2
        put(out, x, when(bar, 0.5), position)
    return out


def air(rng):
    """Barely there: the open night over the water (a soft low hush, so the rests are never digital silence)."""
    hush = shape(rng.normal(0, 1, (2, N)), lambda f: 1 / (1 + (f / 600) ** 2) * (f / 60) ** 2 / (1 + (f / 60) ** 2))
    return level(hush, -52)


def mist(wet, rng):
    """Open air over still water in mist: no walls, a soft diffuse tail (about 3.5 s), dark, applied circularly."""
    pair = synth.reverb_ir(4.5, 3.5, rng, predelay=0.035, brightness=0.22)
    out = np.zeros_like(wet)
    for ch in range(2):
        ir = np.zeros(N)
        ir[:len(pair[ch])] = pair[ch]
        out[ch] = np.fft.irfft(np.fft.rfft(wet[ch]) * np.fft.rfft(ir), N)
    return level(out, 20 * np.log10(rms(wet)))


def render(v, rng):
    dry = level(drone(rng), -33) + air(rng)
    wet = level(pad(rng), -29) + level(strings(rng), -31) + peak(tune(rng), -9) + peak(bellows(rng), -36)
    bell = peak(far_bell(rng), -17)
    return dry + 0.6 * wet + 0.75 * mist(wet, rng) + 0.3 * bell + 1.0 * mist(bell, rng)
