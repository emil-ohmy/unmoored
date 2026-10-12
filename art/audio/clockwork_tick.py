"""Clockwork running: a slow deep tick-tock of the escapement (a beat a second), wheels whirring faintly under it, a
soft ratchet now and then. A seamless loop of four beats."""
LOOP = True
PEAK = 0.6


def render(v, rng):
    beat = 1.0
    n = int(4 * beat * synth.SR)
    out = np.zeros(n + int(0.6 * synth.SR))
    for k in range(5):  # (the fifth wraps into the start)
        f0 = 1500 if k % 2 == 0 else 1150
        click = synth.bandpass(synth.white(0.04, rng), f0, 4.0) * synth.decay(0.04, 0.008)
        knock = synth.lowpass(synth.brown(0.12, rng), 260) * synth.decay(0.12, 0.03)
        synth.place(out, click / (np.max(np.abs(click)) + 1e-9) * 0.5, k * beat)
        synth.place(out, knock / (np.max(np.abs(knock)) + 1e-9) * 0.35, k * beat)
    t = np.arange(len(out)) / synth.SR
    whirr = synth.bandpass(np.resize(synth.white(len(out) / synth.SR + 0.01, rng), len(out)), 700, 2.0) * (0.6 + 0.4 * np.sin(2 * np.pi * 0.5 * t))
    out += whirr / (np.max(np.abs(whirr)) + 1e-9) * 0.06
    looped = out[:n].copy()
    looped[: len(out) - n] += out[n:]
    return looped
