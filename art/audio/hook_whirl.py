"""The Hook whirled on its chain while aiming: a soft whoosh each turn (one turn per 16 frames of the clip, 0.667 s)
and the links' faint rattle. A seamless loop of two turns."""
LOOP = True
PEAK = 0.7


def render(v, rng):
    turn = 16 / 24
    duration = 2 * turn
    n = int(duration * synth.SR)
    out = np.zeros(n + int(0.5 * synth.SR))
    for i in range(3):  # the third wraps round into the start
        d = 0.35
        whoosh = synth.bandpass(synth.white(d, rng), synth.arc(500, 1700, d, 0.5), 1.3) * synth.envelope(d, 0, 0, curve=2.0, peak_at=0.18)
        synth.place(out, whoosh / (np.max(np.abs(whoosh)) + 1e-9) * 0.5, i * turn)
        for _ in range(4):
            dd = rng.uniform(0.004, 0.009)
            link = synth.bandpass(synth.white(dd, rng), rng.uniform(2600, 4500), 4.0) * synth.decay(dd, dd / 3)
            synth.place(out, link / (np.max(np.abs(link)) + 1e-9) * rng.uniform(0.08, 0.18), i * turn + rng.uniform(0.05, 0.3))
    looped = out[:n].copy()
    looped[:len(out) - n] += out[n:]
    return looped
