"""A blow passing through the Widow Hale where time is stopped (it can't touch her): a hollow, airless, muffled whoosh,
as through a photograph (no hit at all: the air swells and is gone, dull, as down a tube), and a faint dry tick in the
middle of it. Quiet and short; it's heard often."""
VARIANTS = 3
PEAK = 0.55


def render(v, rng):
    duration = (0.4, 0.36, 0.44)[v]
    t = synth.seconds(duration)
    # The whoosh: dull noise, swelling and falling with no edge to it.
    air = synth.lowpass(synth.lowpass(synth.white(duration, rng), 800), 1100)  # (dull: no air above it)
    air = synth.highpass(air, 160)
    # Hollow: a few delayed copies (a comb), as down a short tube.
    delay = int(synth.SR / (230, 205, 255)[v])
    hollow = air.copy()
    for k in range(1, 7):
        hollow[k * delay:] += 0.72 ** k * air[:-k * delay]
    env = np.sin(np.pi * np.clip(t / duration, 0, 1)) ** 2.5
    out = hollow / (np.max(np.abs(hollow)) + 1e-9) * env
    # The faint dry tick, mid-pass.
    d = 0.03
    tt = synth.seconds(d)
    tick = synth.bandpass(synth.white(d, rng), (3200, 2900, 3500)[v], 5.0) * np.exp(-tt / 0.003) * np.clip(tt / 0.0006, 0, 1)
    synth.place(out, tick / (np.max(np.abs(tick)) + 1e-9) * 0.3, duration * 0.45)
    return out
