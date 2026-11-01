"""The great gear rolling down the run: a deep rumble of iron on stone and its teeth clacking on the rails, quick and
heavy (played faster and louder the faster it goes). A seamless loop."""
LOOP = True
PEAK = 0.7


def render(v, rng):
    length = 1.2
    n = int(length * synth.SR)
    t = np.arange(n) / synth.SR
    rumble = synth.lowpass(np.resize(synth.brown(length + 0.1, rng), n), 160)
    rumble = rumble / (np.max(np.abs(rumble)) + 1e-9) * (0.8 + 0.2 * np.sin(2 * np.pi * t / length * 3))
    out = rumble * 0.7
    tail = np.zeros(n + int(0.2 * synth.SR))
    count = 12
    for k in range(count):  # the teeth (evenly: it loops)
        clack = synth.bandpass(synth.white(0.05, rng), 900 + 200 * (k % 3), 2.0) * synth.decay(0.05, 0.012)
        thump = synth.lowpass(synth.brown(0.09, rng), 220) * synth.decay(0.09, 0.025)
        synth.place(tail, clack / (np.max(np.abs(clack)) + 1e-9) * 0.35, k * length / count)
        synth.place(tail, thump / (np.max(np.abs(thump)) + 1e-9) * 0.4, k * length / count)
    looped = tail[:n].copy()
    looped[: len(tail) - n] += tail[n:]
    return out + looped
