"""The Hold: the bed for the game's opening area, played as its music. A 108 s seamless stereo loop, no melody, no pulse.

A flooded undercroft far beneath the great jetty, a forest of pilings in black water. Layers: a very deep drone (about
33 Hz, felt more than heard, a quiet octave and twelfth above it so small speakers carry a hint) slowly breathing, its
two strings drifting in and out of tune; the room's own low air; water lapping softly against the pilings all round
(a hushed wash and irregular laps, now and then a hollow clop in a gap); sparse, irregular, far-off sounds: drips,
a timber groaning under the weight far above, a chain shifting, a hollow knock; twice a faint, distant, wordless sigh
of many voices. Long stretches of almost nothing. A damp, low, wooden room: close early reflections off the planking
above, a long dark tail.

Everything is built circularly over exactly one loop (noise shaped by FFT over the loop, events wrapped round its end,
the reverb convolved circularly), so the seam is exact: nothing is crossfaded.
`synth` and `np` are injected by tools/audio/build_sound.py.
"""
LOOP = True
FOLDER = "Music"
PEAK = 0.35  # sets the loudness: about -26 dB RMS (the drone carries most of it)

SR = synth.SR
LENGTH = 108.0
N = int(LENGTH * SR)

# (time, position) of the sparse things; positions -1 (left) .. +1 (right). Irregular on purpose.
DRIPS = [(3.1, 0.55), (9.8, -0.7), (17.4, 0.2), (21.0, 0.75), (33.6, -0.35), (41.9, 0.6), (44.2, 0.62),
         (58.7, -0.8), (66.0, 0.1), (79.5, -0.5), (88.3, 0.85), (96.9, -0.2), (104.6, 0.4)]
CREAKS = [(12.5, -0.2, 4.2), (49.0, 0.25, 3.4), (71.5, -0.05, 4.8), (99.0, 0.3, 3.0)]  # (time, position, length)
CHAINS = [(27.5, 0.7), (84.0, -0.65)]
KNOCKS = [(6.8, -0.6, 2), (38.4, 0.5, 1), (62.2, -0.3, 2), (92.0, 0.65, 1)]  # (time, position, hits)
SIGHS = [(29.0, -0.3), (81.0, 0.35)]


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


# ---------------------------------------------------------------- the room

def drone(rng):
    """Two deep strings a hair apart, drifting in and out of tune (the beating slows to nothing and quickens again),
    their octave and twelfth faint above; the whole breathing slowly. Exactly periodic over the loop."""
    t = np.arange(N) / SR
    f0 = cycles(32.7)  # about C1
    breath = 0.5 + 0.5 * (0.5 - 0.5 * np.cos(2 * np.pi * cycles(1 / 15.4) * t + rng.uniform(0, 6)))
    breath *= slow(rng, 0.05, 0.6, 1.0)
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
    """The room's own low air: a dark rumble under the drone, and the faintest damp hiss."""
    rumble = shape(rng.normal(0, 1, (2, N)), lambda f: 1 / (1 + (f / 55) ** 4) * (f / 18) ** 2 / (1 + (f / 18) ** 2))
    hush = shape(rng.normal(0, 1, (2, N)), lambda f: np.exp(-0.5 * (np.log(np.maximum(f, 1) / 1400) / 0.7) ** 2))
    return level(rumble, -38) + level(hush * slow(rng, 0.08, 0.5, 1.0), -56)


# ---------------------------------------------------------------- the water

def wash(rng):
    """Water moving under it all: a low hushed slosh swelling with the slow swell (24 waves to the loop)."""
    t = np.arange(N) / SR
    swl = 0.5 - 0.5 * np.cos(2 * np.pi * cycles(24 / LENGTH) * t)
    swl = swl ** 1.5 * slow(rng, 0.04, 0.4, 1.0)
    noise = shape(rng.normal(0, 1, (2, N)), lambda f: np.exp(-0.5 * (np.log(np.maximum(f, 1) / 380) / 0.55) ** 2))
    return noise * (0.25 + 0.75 * swl)


def lap(rng, far):
    """Water lapping once against a piling: a soft slosh falling in pitch, sometimes a hollow clop in a gap."""
    d = rng.uniform(0.45, 0.9)
    slosh = synth.bandpass(synth.white(d, rng), synth.glide(rng.uniform(550, 900), rng.uniform(220, 360), d, 0.7), 1.3)
    slosh *= synth.envelope(d, 0, 0, curve=2.6, peak_at=rng.uniform(0.05, 0.12))
    out = slosh / (np.max(np.abs(slosh)) + 1e-9)
    if rng.random() < 0.4:  # water knocking hollow in a gap between the planks
        cd = 0.18
        ct = synth.seconds(cd)
        f = rng.uniform(170, 300) * (1 + 0.5 * np.clip(ct / 0.05, 0, 1))
        clop = np.sin(2 * np.pi * np.cumsum(f) / SR) * np.exp(-ct / 0.035) * np.clip(ct / 0.004, 0, 1)
        synth.place(out, clop * rng.uniform(0.35, 0.6), rng.uniform(0.03, 0.12))
    for _ in range(rng.integers(1, 4)):  # a bubble or two as it drains back
        bd = 0.06
        bt = synth.seconds(bd)
        f = rng.uniform(500, 1000) * (1 + 0.8 * bt / bd)
        bub = np.sin(2 * np.pi * np.cumsum(f) / SR) * np.exp(-bt / 0.012) * np.clip(bt / 0.002, 0, 1)
        synth.place(out, bub * rng.uniform(0.08, 0.2), rng.uniform(0.1, d * 0.8))
    cutoff = 1200 if far else 2400
    return tail(synth.lowpass(out, cutoff)) * (0.5 if far else 1.0)


def laps(rng):
    """Laps all round, gathered on the swell's crests: two to four a wave, from different pilings; some waves
    hardly reach the wood at all."""
    near, far = blank(), blank()
    period = LENGTH / 24
    for w in range(24):
        crest = w * period + period * 0.5
        if rng.random() < 0.2:
            continue
        for _ in range(rng.integers(1, 4)):
            is_far = rng.random() < 0.5
            put(far if is_far else near, lap(rng, is_far), crest + rng.uniform(-0.8, 1.2), rng.uniform(-0.95, 0.95),
                rng.uniform(0.35, 1.0))
    return near, far


# ---------------------------------------------------------------- the far-off things

def drip(rng):
    """A drop from the planking far above into still water: a tiny impact and the bubble's rising plip."""
    d = 0.25
    t = synth.seconds(d)
    f = rng.uniform(900, 1700) * (1 + 0.6 * np.clip(t / 0.03, 0, 1))
    plip = np.sin(2 * np.pi * np.cumsum(f) / SR) * np.exp(-t / rng.uniform(0.02, 0.035)) * np.clip(t / 0.0015, 0, 1)
    tick = synth.highpass(synth.white(0.006, rng), 2000) * synth.decay(0.006, 0.0015) * 0.25
    plip[:len(tick)] += tick
    return synth.lowpass(plip, 3000)


def creak(rng, d):
    """A great beam far above groaning under weight: slow stick-slip, rung through the timber's low resonances;
    the slipping quickens and slows as the load shifts. Distant: no highs."""
    n = int(d * SR)
    rate = synth.arc(rng.uniform(9, 14), rng.uniform(28, 42), d, rng.uniform(0.3, 0.6))[:n]
    rate *= 1 + 0.25 * synth.smooth_random(d, rng, 6.0, -1.0, 1.0)[:n]
    phase = np.cumsum(rate / SR)
    clicks = np.diff(np.floor(phase), prepend=0.0) * rng.uniform(0.5, 1.0, n)
    wood = synth.bandpass(clicks, 140 * rng.uniform(0.9, 1.1), 7.0) + 0.7 * synth.bandpass(clicks, 310 * rng.uniform(0.9, 1.1), 6.0) \
        + 0.3 * synth.bandpass(clicks, 620 * rng.uniform(0.9, 1.1), 5.0)
    groan = synth.bandpass(synth.white(d, rng), 95, 4.0) * 0.6
    out = (wood + groan * (rate / rate.max())) * swell(d, d * 0.35, d * 0.4)
    return tail(synth.lowpass(out, 900))


def chain(rng):
    """A chain somewhere shifting: a short drag and a few dull links knocking, far off."""
    d = 1.8
    out = np.zeros(int(d * SR))
    drag = synth.bandpass(synth.white(0.7, rng), 1600, 1.5) * swell(0.7, 0.2, 0.4)
    synth.place(out, drag * 0.12, 0.1)
    at = 0.12
    for _ in range(rng.integers(4, 8)):
        ld = 0.3
        lt = synth.seconds(ld)
        f = rng.uniform(1300, 2200)
        link = np.zeros_like(lt)
        for ratio, a, tau in ((1.0, 1.0, 0.07), (2.71, 0.5, 0.04), (4.13, 0.3, 0.025)):
            link += a * np.sin(2 * np.pi * f * ratio * lt + rng.uniform(0, 6)) * np.exp(-lt / tau)
        link *= np.clip(lt / 0.001, 0, 1)
        synth.place(out, link * rng.uniform(0.3, 1.0), at)
        at += rng.uniform(0.06, 0.25)
    return tail(synth.lowpass(out, 2500))


def knock(rng, hits):
    """Something hollow bumping a piling (a drifting cask, a loose board): a low wooden knock, once or twice."""
    d = 0.6 + 0.5 * (hits - 1)
    out = np.zeros(int(d * SR))
    for h in range(hits):
        kd = 0.5
        kt = synth.seconds(kd)
        f = rng.uniform(95, 150)
        body = np.sin(2 * np.pi * f * kt) * np.exp(-kt / 0.09) + 0.4 * np.sin(2 * np.pi * f * 2.3 * kt) * np.exp(-kt / 0.04)
        thud = synth.lowpass(synth.white(0.03, rng), 600) * synth.decay(0.03, 0.006)
        body[:len(thud)] += thud
        body *= np.clip(kt / 0.002, 0, 1)
        synth.place(out, body * (1.0 if h == 0 else 0.55), h * rng.uniform(0.35, 0.5))
    return tail(synth.lowpass(out, 1200))


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


# ---------------------------------------------------------------- the room's sound

def room_ir(rng):
    """Close and damp: a cluster of early reflections off the planking above and the pilings near by (5-45 ms),
    then a long dark tail. Wood swallows the highs."""
    pair = synth.reverb_ir(5.0, 4.2, rng, predelay=0.014, brightness=0.15)
    out = []
    for ir in pair:
        early = np.zeros(int(0.05 * SR))
        for _ in range(14):
            at = rng.uniform(0.004, 0.045)
            early[int(at * SR)] += rng.uniform(-1, 1) * np.exp(-at / 0.02) * 0.35
        full = np.zeros(max(len(ir), len(early)))
        full[:len(ir)] += ir
        full[:len(early)] += synth.lowpass(early, 2500)
        out.append(full)
    return out


def room(wet, rng):
    """The room applied circularly over the loop: the tail of the end rings on into the start."""
    pair = room_ir(rng)
    reverberant = np.zeros_like(wet)
    for ch in range(2):
        ir = np.zeros(N)
        ir[:len(pair[ch])] = pair[ch]
        reverberant[ch] = np.fft.irfft(np.fft.rfft(wet[ch]) * np.fft.rfft(ir), N)
    # timber colours it: a broad warmth about 250 Hz, the highs gone
    reverberant = shape(reverberant, lambda f: (1 + 0.6 * np.exp(-0.5 * (np.log(np.maximum(f, 1) / 250) / 0.5) ** 2))
                        / (1 + (f / 2200) ** 2))
    return level(reverberant, 20 * np.log10(rms(wet)))


def render(v, rng):
    dry = level(drone(rng), -26) + air(rng)

    near, far = laps(rng)
    water = level(wash(rng), -47)
    dry += level(near, -42) * 0.6 + water * 0.7

    things = blank()
    for time, position in DRIPS:
        put(things, drip(rng), time, position, rng.uniform(0.4, 1.0))
    for time, position, d in CREAKS:
        put(things, peak(creak(rng, d), 0), time, position, rng.uniform(0.6, 1.0))
    for time, position in CHAINS:
        put(things, peak(chain(rng), 0), time, position, 0.55)
    for time, position, hits in KNOCKS:
        put(things, peak(knock(rng, hits), 0), time, position, rng.uniform(0.6, 0.9))
    voices = blank()
    for time, position in SIGHS:
        put(voices, sigh(rng), time, position)

    wet = level(near, -42) + level(far, -44) + water * 0.5 \
        + peak(things, -18) + peak(voices, -24)
    return dry + 0.3 * wet + 1.1 * room(wet, rng)
