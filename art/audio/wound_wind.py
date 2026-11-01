"""A Wound winding itself (2.4 s): the key cranked round in turns, each a run of ratchet clicks and a creak, the spring
tightening (each turn a little higher)."""
PEAK = 0.7


def render(v, rng):
    d = 2.5
    out = np.zeros(int(d * synth.SR))
    for turn in range(4):
        start = turn * 0.58
        for k in range(6):
            c = synth.bandpass(synth.white(0.025, rng), 2300 + 180 * turn + 40 * k, 4.0) * synth.decay(0.025, 0.004)
            synth.place(out, c * 0.7, start + k * 0.06)
        w = 0.3
        t = synth.seconds(w)
        f = 360 + 90 * turn + 120 * t / w
        creak = np.sin(2 * np.pi * np.cumsum(f) / synth.SR) * synth.bandpass(synth.white(w, rng), 900, 0.8) * 2.0
        synth.place(out, creak * np.sin(np.pi * t / w) * 0.5, start + 0.05)
    return out
