"""Round 3: a small fireball of the Watchman's knocked aside, and his breath with it. Three variants, one per
knock in a row, rising: the music-box notes of an A minor chord out of Last Autumn (A5, C6, E6), each a little
fuller; the third has the octave below under it, a finish."""
VARIANTS = 3
NOTES = ["A5", "C6", "E6"]


def tine(f, duration, rng):
    t = synth.seconds(duration)
    out = synth.glass(f, duration, rng) + np.sin(2 * np.pi * f * 2.76 * t) * np.exp(-t * 6.0) * 0.25
    click = synth.highpass(synth.white(0.01, rng), 4000) * 0.2
    out[:len(click)] += click
    return out


def render(v, rng):
    duration = 2.2
    f = synth.note(NOTES[v])
    out = tine(f, duration, rng)
    if v >= 1:
        out += 0.4 * tine(f * 2 ** (-5 / 12) if v == 1 else f / 2, duration, rng)  # (fuller as it climbs)
    if v == 2:
        out += 0.35 * tine(synth.note("A5"), duration, rng) + 0.3 * tine(synth.note("C6"), duration, rng)
    fade = int(0.3 * synth.SR)
    out[-fade:] *= np.linspace(1, 0, fade) ** 2
    return out
