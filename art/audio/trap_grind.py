"""The undercroft's floor gears turning: brass teeth grinding into each other at the mesh, a harsh rasp and a
squeal, the teeth knocking as they meet, a low drone of the train. A seamless loop."""
LOOP = True
PEAK = 0.6


def render(v, rng):
    length = 2.0
    n = int(length * synth.SR)
    t = np.arange(n) / synth.SR
    rasp = synth.bandpass(np.resize(synth.white(length + 0.1, rng), n), 1600, 1.3)
    rasp = rasp / (np.max(np.abs(rasp)) + 1e-9) * (0.55 + 0.45 * np.sin(2 * np.pi * t * 5) ** 2)
    squeal = np.sin(2 * np.pi * (2900 + 60 * np.sin(2 * np.pi * t * 1.5)) * t) * (0.5 + 0.5 * np.sin(2 * np.pi * t / length)) ** 4
    drone = np.sin(2 * np.pi * 55 * t) * 0.6 + np.sin(2 * np.pi * 110 * t) * 0.25
    tail = np.zeros(n + int(0.2 * synth.SR))
    for k in range(20):
        knock = synth.bandpass(synth.white(0.03, rng), 700, 2.0) * synth.decay(0.03, 0.008)
        synth.place(tail, knock / (np.max(np.abs(knock)) + 1e-9) * 0.4, k * length / 20)
    looped = tail[:n].copy()
    looped[: len(tail) - n] += tail[n:]
    return rasp * 0.35 + squeal * 0.05 + drone * 0.25 + looped
