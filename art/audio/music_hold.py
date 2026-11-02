"""The Hold: the bed for the game's opening area, played as its music. A 108 s seamless stereo loop, no melody, no pulse.

A vast cave whose walls are the souls that were kept there: dark, damp, close and long, dead still. Layers: a very
deep drone (about 33 Hz, felt more than heard, a quiet octave and twelfth above it so small speakers carry a hint),
swelling and sinking very slowly (far slower than the walls' own breaths, which the game plays separately in time
with them: they are not in here), its two strings drifting in and out of tune; the cave's own low air; still black
pools: drips falling into them and onto stone, far apart, each ringing on in the rock (one spot somewhere to the
left drips again and again, irregularly, as cave drips do); rare and far: the rock groaning under its own weight,
stone settling (a dull crack, grit trickling); twice a faint, distant, wordless sigh of many voices; once, deep in
the rock, something like a heartbeat (two soft, very low beats, then nothing for a long while). Long stretches of
almost nothing. A stone room: early reflections off near walls, a long dark tail.

Everything is built circularly over exactly one loop (noise shaped by FFT over the loop, events wrapped round its end,
the reverb convolved circularly), so the seam is exact: nothing is crossfaded.
The groans and settling come from hold_creak.py (groan, settle).
`synth` and `np` are injected by tools/audio/build_sound.py.
"""
LOOP = True
FOLDER = "Music"
PEAK = 0.24  # sets the loudness: about -26 dB RMS (the drone carries most of it)

SR = synth.SR
LENGTH = 108.0
N = int(LENGTH * SR)

# (time, position) of the sparse things; positions -1 (left) .. +1 (right). Irregular on purpose.
DRIP_SPOT = (-0.55, [2.4, 13.1, 22.9, 35.6, 44.8, 58.3, 69.0, 80.7, 93.5, 103.2])  # one place, dripping again and again
DRIPS = [(7.6, 0.6, "pool"), (18.2, 0.15, "stone"), (30.4, 0.8, "pool"), (49.7, -0.2, "pool"), (54.1, 0.35, "stone"),
         (74.9, 0.7, "pool"), (88.6, -0.85, "stone"), (98.4, 0.45, "pool")]
GROANS = [(10.5, 0.3, 0), (60.5, -0.35, 1), (95.0, 0.1, 2)]  # (time, position, kind)
SETTLES = [(39.0, 0.65), (84.0, -0.5)]
SIGHS = [(26.0, -0.3), (71.0, 0.35)]
HEARTBEAT = (50.0, 0.0)


def rms(x):
    return np.sqrt(np.mean(np.asarray(x) ** 2)) + 1e-12


def level(x, db):
    return x / rms(x) * 10 ** (db / 20)


def peak(x, db):
    return x / (np.max(np.abs(x)) + 1e-9) * 10 ** (db / 20)


def blank():
    return np.zeros((2, N))


def shape(x, gain):
    """Filter x circularly by a gain curve over frequency: gain(freqs) -> gains. Periodic in, periodic out."""
    freqs = np.fft.rfftfreq(x.shape[-1], 1 / SR)
    return np.fft.irfft(np.fft.rfft(x, axis=-1) * gain(freqs), x.shape[-1], axis=-1)


def slow(rng, rate, low=0.0, high=1.0):
    """A slowly wandering value (changes about `rate` times a second) that loops exactly."""
    x = shape(rng.normal(0, 1, N), lambda f: np.exp(-(f / rate) ** 2))
    x = (x - x.min()) / (x.max() - x.min() + 1e-12)
    return low + (high - low) * x


def cycles(k):
    """A frequency with a whole number of cycles in the loop."""
    return round(k * LENGTH) / LENGTH


def put(out, tone, at, position=0.0, gain=1.0):
    """Add a mono sound into the loop at `at` seconds, panned; what runs past the end wraps round to the start."""
    placed = synth.pan(tone * gain, position) if tone.ndim == 1 else tone * gain
    s = int(at * SR) % N
    k = 0
    while k < placed.shape[1]:
        m = min(placed.shape[1] - k, N - s)
        out[:, s:s + m] += placed[:, k:k + m]
        k += m
        s = 0


def swell(duration, attack, release):
    t = synth.seconds(duration)
    up = np.clip(t / attack, 0, 1)
    down = np.clip((duration - t) / release, 0, 1)
    return np.sin(np.minimum(up, down) * np.pi / 2) ** 2


def tail(x, seconds=0.02):
    """Ease out the last moment of a sound so nothing is cut off mid-ring."""
    n = int(seconds * SR)
    x[-n:] *= np.linspace(1, 0, n)
    return x


def creak_kit():
    import importlib.util, os
    spec = importlib.util.spec_from_file_location("creak", os.path.join(os.path.dirname(__file__), "hold_creak.py"))
    kit = importlib.util.module_from_spec(spec)
    kit.synth, kit.np = synth, np
    spec.loader.exec_module(kit)
    return kit


# ---------------------------------------------------------------- the cave

def drone(rng):
    """Two deep strings a hair apart, drifting in and out of tune (the beating slows to nothing and quickens again),
    their octave and twelfth faint above; swelling and sinking very slowly (three times to the loop, unevenly: not
    the walls' breath). Exactly periodic over the loop."""
    t = np.arange(N) / SR
    f0 = cycles(32.7)  # about C1
    breath = 0.65 + 0.35 * (0.5 - 0.5 * np.cos(2 * np.pi * cycles(3 / LENGTH) * t + rng.uniform(0, 6)))
    breath *= slow(rng, 0.05, 0.65, 1.0)
    out = np.zeros((2, N))
    for ch in range(2):
        # zero-mean detune, so the drifting string comes back round to the same phase at the loop's end
        detune = 0.42 * np.sin(2 * np.pi * cycles(2 / LENGTH) * t + rng.uniform(0, 6)) \
            + 0.18 * np.sin(2 * np.pi * cycles(5 / LENGTH) * t + rng.uniform(0, 6))
        drift = 2 * np.pi * np.cumsum(detune) / SR
        a = 2 * np.pi * f0 * t
        low = np.sin(a) + 0.85 * np.sin(a + drift + 0.3 * ch)
        upper = 0.16 * (np.sin(2 * a + 1.0) + 0.8 * np.sin(2 * (a + drift) + 2.0)) \
            + 0.07 * (np.sin(3 * a + 0.4) + 0.8 * np.sin(3 * (a + drift) + 1.3 + 0.5 * ch))
        out[ch] = (low + upper * (0.6 + 0.4 * breath)) * breath
    return out


def air(rng):
    """The cave's own low air: a dark rumble under the drone, and the faintest damp hiss (a little higher than
    in a wooden room: stone keeps more of it)."""
    rumble = shape(rng.normal(0, 1, (2, N)), lambda f: 1 / (1 + (f / 50) ** 4) * (f / 18) ** 2 / (1 + (f / 18) ** 2))
    hush = shape(rng.normal(0, 1, (2, N)), lambda f: np.exp(-0.5 * (np.log(np.maximum(f, 1) / 1700) / 0.75) ** 2))
    return level(rumble, -38) + level(hush * slow(rng, 0.06, 0.4, 1.0), -57)


# ---------------------------------------------------------------- still pools

def drip(rng, onto="pool"):
    """A drop from the roof into a still pool (a tiny impact, the bubble's rising plip, sometimes a fainter
    second) or onto bare stone (a sharp little tick, a bead or two flung off)."""
    d = 0.3
    t = synth.seconds(d)
    out = np.zeros_like(t)
    if onto == "pool":
        f = rng.uniform(800, 1600) * (1 + 0.6 * np.clip(t / 0.03, 0, 1))
        out += np.sin(2 * np.pi * np.cumsum(f) / SR) * np.exp(-t / rng.uniform(0.02, 0.035)) * np.clip(t / 0.0015, 0, 1)
        if rng.random() < 0.5:
            f2 = f * rng.uniform(1.5, 1.8)
            second = np.sin(2 * np.pi * np.cumsum(f2) / SR) * np.exp(-t / 0.012) * np.clip(t / 0.001, 0, 1)
            synth.place(out, second[:int(0.1 * SR)] * 0.2, rng.uniform(0.04, 0.07))
        tick = synth.highpass(synth.white(0.006, rng), 2000) * synth.decay(0.006, 0.0015) * 0.25
        out[:len(tick)] += tick
    else:
        out += synth.bandpass(synth.white(d, rng), 3800, 1.0) * np.exp(-t / 0.003) * np.clip(t / 0.0005, 0, 1)
        for _ in range(rng.integers(1, 4)):
            bead = synth.bandpass(synth.white(0.02, rng), rng.uniform(3000, 5500), 3.0) * synth.decay(0.02, 0.003)
            synth.place(out, bead * rng.uniform(0.15, 0.35), rng.uniform(0.012, 0.07))
    return tail(synth.lowpass(out, 3200))


# ---------------------------------------------------------------- the rock

def groan(kit, rng, kind):
    """The rock far off under its own weight: one long shove (0), two (1), or one ending in a settle (2)."""
    if kind == 0:
        shoves = [(3.4, 4, 14, 4, 0.55, 0.85)]
    elif kind == 1:
        shoves = [(1.6, 5, 13, 6, 0.45, 0.9), (2.2, 4, 16, 4, 0.4, 0.82)]
    else:
        shoves = [(2.4, 5, 18, 7, 0.5, 0.95)]
    total = sum(s[0] for s in shoves) + 0.6 * len(shoves) + 1.5
    out = np.zeros(int(total * SR))
    at = 0.05
    for d, start, top, end, peak_at, scale in shoves:
        synth.place(out, kit.groan(d, start, top, end, peak_at, scale, rng), at)
        at += d + 0.5
    if kind == 2:
        synth.place(out, kit.settle(rng) * 0.6, at - 0.8)
    out = synth.lowpass(synth.lowpass(out, 650), 900)  # far: the highs gone before the cave takes it
    return tail(synth.highpass(out, 22), 0.3)


def settling(kit, rng):
    """Stone settling somewhere: a dull crack deep in it, grit and a pebble trickling down. Far, faint."""
    x = kit.settle(rng, 1.8)
    return tail(synth.lowpass(x, 1500), 0.2)


def heartbeat(rng):
    """Something like a heart, far inside the rock: two beats a little over a second apart (each a soft low thump
    with a fainter second just behind it), the second weaker. Very low: felt more than heard; through stone."""
    d = 3.0
    out = np.zeros(int(d * SR))
    for k, (at, gain) in enumerate(((0.05, 1.0), (1.15, 0.7))):
        for sub, (offset, g, f_hi, f_lo) in enumerate(((0.0, 1.0, 58, 40), (0.26, 0.45, 64, 46))):
            bd = 0.5
            t = synth.seconds(bd)
            f = f_lo + (f_hi - f_lo) * np.exp(-t / 0.04)
            thump = np.sin(2 * np.pi * np.cumsum(f) / SR) * np.exp(-t / 0.11) + 0.25 * np.sin(4 * np.pi * np.cumsum(f) / SR) * np.exp(-t / 0.06)
            thump *= np.sin(np.clip(t / 0.018, 0, 1) * np.pi / 2) ** 2  # soft onset: a swell, not a hit
            synth.place(out, thump * g * gain, at + offset)
    return tail(synth.lowpass(synth.lowpass(out, 140), 200), 0.3)


# ---------------------------------------------------------------- the souls

def sigh(rng):
    """Many voices, far off, sighing together, wordless: each falls a little, breathier than sung."""
    d = 6.5
    out = np.zeros((2, int(d * SR)))
    for _ in range(12):
        f = rng.uniform(100, 250)
        vd = d - rng.uniform(0.0, 1.2)
        pitch = synth.glide(f, f * rng.uniform(0.8, 0.9), vd, 0.8)
        vowel = ["ah", "oh", "oo"][rng.integers(0, 3)]
        sung = synth.voice(pitch, vd, rng, vowel, vibrato=0.008, max_freq=1800)
        freqs = np.fft.rfftfreq(int(vd * SR), 1 / SR)
        breath = np.fft.irfft(np.fft.rfft(synth.white(vd, rng)) * synth.formants(freqs, vowel), int(vd * SR))
        v = level(sung, -20) * 0.5 + level(breath, -20)
        v *= swell(vd, rng.uniform(1.6, 2.6), rng.uniform(2.5, 3.5))
        placed = synth.pan(v, rng.uniform(-0.8, 0.8))
        start = int(rng.uniform(0, d - vd) * SR)
        out[:, start:start + placed.shape[1]] += placed
    return shape(out, lambda f: 1 / (1 + (f / 900) ** 2))


# ---------------------------------------------------------------- the cave's sound

def cave_ir(rng):
    """A vast stone cave, close and long: a scatter of early reflections off near walls (6-90 ms, thinning), then a
    long dark tail. Damp stone: the highs fade, but it keeps more of them than wood."""
    pair = synth.reverb_ir(7.0, 6.0, rng, predelay=0.012, brightness=0.2)
    out = []
    for ir in pair:
        early = np.zeros(int(0.1 * SR))
        for _ in range(22):
            at = 0.006 + rng.random() ** 1.4 * 0.085
            early[int(at * SR)] += rng.uniform(-1, 1) * np.exp(-at / 0.035) * 0.3
        full = np.zeros(max(len(ir), len(early)))
        full[:len(ir)] += ir
        full[:len(early)] += synth.lowpass(early, 3500)
        out.append(full)
    return out


def cave(wet, rng):
    """The cave applied circularly over the loop: the tail of the end rings on into the start."""
    pair = cave_ir(rng)
    reverberant = np.zeros_like(wet)
    for ch in range(2):
        ir = np.zeros(N)
        ir[:len(pair[ch])] = pair[ch]
        reverberant[ch] = np.fft.irfft(np.fft.rfft(wet[ch]) * np.fft.rfft(ir), N)
    # stone colours it: a little hollow low-mid body, the highs going
    reverberant = shape(reverberant, lambda f: (1 + 0.4 * np.exp(-0.5 * (np.log(np.maximum(f, 1) / 140) / 0.6) ** 2))
                        / (1 + (f / 3000) ** 2))
    return level(reverberant, 20 * np.log10(rms(wet)))


def render(v, rng):
    kit = creak_kit()
    dry = level(drone(rng), -26) + air(rng)

    drips = blank()
    position, times = DRIP_SPOT
    for time in times:  # the same place each time: the same note, near enough
        spot = np.random.default_rng(7)
        put(drips, drip(spot, "pool"), time, position, rng.uniform(0.5, 0.8))
    for time, position, onto in DRIPS:
        put(drips, drip(rng, onto), time, position, rng.uniform(0.4, 1.0))

    rock = blank()
    for time, position, kind in GROANS:
        put(rock, peak(groan(kit, rng, kind), 0), time, position, rng.uniform(0.75, 1.0))
    for time, position in SETTLES:
        put(rock, peak(settling(kit, rng), 0), time, position, 0.35)
    beat = blank()
    put(beat, peak(heartbeat(rng), 0), HEARTBEAT[0], HEARTBEAT[1])
    voices = blank()
    for time, position in SIGHS:
        put(voices, sigh(rng), time, position)

    wet = peak(drips, -20) + peak(rock, -17) + peak(voices, -24)
    return dry + 0.25 * wet + 1.15 * cave(wet, rng) + peak(beat, -18) + 0.4 * cave(peak(beat, -18), rng)
