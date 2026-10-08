"""Father Moth beaten: the music box plays Last Autumn's last phrase, slow, as if winding down, and this once it doesn't
lean on the G# to pull round again: it comes home to A, a soft reed chord swelling under it, and rings away.
`synth` and `np` are injected by tools/audio/build_sound.py.
"""
FOLDER = "Music"
PEAK = 0.7

# (seconds, note): the tune's last phrase, slower and slower (a box winding down), then home.
PHRASE = [(0.0, "A4"), (0.75, "C5"), (1.5, "E5"), (2.4, "D5"), (3.2, "C5"), (4.2, "B4"), (5.3, "A4"), (6.6, "C5"), (8.2, "A4")]


def tine(f, duration, rng):
    t = synth.seconds(duration)
    out = synth.glass(f * 2 ** (rng.uniform(-6, 6) / 1200), duration, rng) + np.sin(2 * np.pi * f * 2.76 * t) * np.exp(-t * 6.0) * 0.25
    click = synth.highpass(synth.white(0.01, rng), 4000) * 0.2
    out[:len(click)] += click
    return out


def reed(f, duration, rng):
    t = synth.seconds(duration)
    out = np.zeros_like(t)
    for detune in (0.999, 1.0015):
        for k in range(1, 9):
            out += (1.0 / k) * (1.0 if k % 2 else 0.55) * np.sin(2 * np.pi * f * detune * k * t + rng.uniform(0, 6))
    return synth.lowpass(out, 1800)


def render(v, rng):
    duration = 14.0
    n = int(duration * synth.SR)
    wet = np.zeros(n)
    for at, name in PHRASE:
        synth.place(wet, tine(synth.note(name), 4.5, rng) * (1.2 if at == PHRASE[-1][0] else 1.0), at)
    synth.place(wet, tine(synth.note("A5"), 5.0, rng) * 0.5, PHRASE[-1][0])  # (home: the octave with it)
    # The reed chord under the last of it: A minor, swelling, then let go.
    chord_at, chord_d = 6.0, 7.5
    env = np.sin(np.clip(synth.seconds(chord_d) / 2.5, 0, 1) * np.pi / 2) ** 2 * np.clip((chord_d - synth.seconds(chord_d)) / 3.5, 0, 1)
    for name, gain in (("A2", 0.6), ("E3", 0.4), ("A3", 0.4), ("C4", 0.35), ("E4", 0.3)):
        synth.place(wet, reed(synth.note(name), chord_d, rng) * env * gain * 0.25, chord_at)
    hall = synth.reverb_ir(4.0, 3.2, rng, brightness=0.3)
    left = wet + 0.8 * np.convolve(wet, hall[0])[:n] * 0.3
    right = wet + 0.8 * np.convolve(wet, hall[1])[:n] * 0.3
    return synth.stereo(left, right)
