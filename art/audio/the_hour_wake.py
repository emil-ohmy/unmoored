"""The Hour waking (time running): its cables drawing taut with a groan, a heavy ratchet as its contraption takes its
weight, and a single low bell-stroke from inside it, felt more than heard."""
VARIANTS = 1


def render(v, rng):
    duration = 4.0
    t = synth.seconds(duration)
    out = np.zeros_like(t)
    groan = synth.bandpass(synth.brown(1.6, rng), 140, 2.5) * synth.envelope(1.6, 0.6, 0.9, curve=2)
    synth.place(out, groan / (np.max(np.abs(groan)) + 1e-9) * 0.5, 0.0)
    at, gap = 0.5, 0.18
    while at < 1.5:
        tick = synth.bandpass(synth.white(0.04, rng), 1300, 3.0) * synth.decay(0.04, 0.01)
        knock = synth.lowpass(synth.brown(0.1, rng), 260) * synth.decay(0.1, 0.03)
        synth.place(out, tick * 0.5, at)
        synth.place(out, knock / (np.max(np.abs(knock)) + 1e-9) * 0.4, at)
        at += gap
        gap = max(0.07, gap * 0.85)
    f = synth.note("A1")
    n = len(t) - int(1.6 * synth.SR)
    tt = np.arange(n) / synth.SR
    bell = sum(a * np.sin(2 * np.pi * f * r * tt + rng.uniform(0, 6)) * np.exp(-tt / tau)
               for r, a, tau in ((1.0, 1.0, 1.4), (2.0, 0.5, 0.9), (2.4, 0.3, 0.6), (3.0, 0.2, 0.4)))
    synth.place(out, bell * 0.6, 1.6)
    return out * np.clip((duration - t) / 0.5, 0, 1)
