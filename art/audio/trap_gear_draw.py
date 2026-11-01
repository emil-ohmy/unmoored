"""The great gear drawn back up the run on its chain: the chain's links clattering over the drum, a ratchet ticking
behind them, the gear grumbling up the stone. A seamless loop."""
LOOP = True
PEAK = 0.55


def render(v, rng):
    length = 1.6
    n = int(length * synth.SR)
    tail = np.zeros(n + int(0.2 * synth.SR))
    links = 10
    for k in range(links):
        link = synth.bandpass(synth.white(0.04, rng), rng.uniform(1800, 2600), 2.5) * synth.decay(0.04, 0.01)
        synth.place(tail, link * 0.4, k * length / links + rng.uniform(0, 0.01))
        link2 = synth.bandpass(synth.white(0.03, rng), rng.uniform(3000, 3800), 3.0) * synth.decay(0.03, 0.006)
        synth.place(tail, link2 * 0.2, k * length / links + 0.035)
    for k in range(4):
        tick = synth.bandpass(synth.white(0.02, rng), 4200, 4.0) * synth.decay(0.02, 0.004)
        synth.place(tail, tick * 0.3, k * length / 4 + 0.07)
    looped = tail[:n].copy()
    looped[: len(tail) - n] += tail[n:]
    t = np.arange(n) / synth.SR
    grumble = synth.lowpass(np.resize(synth.brown(length + 0.1, rng), n), 140)
    grumble = grumble / (np.max(np.abs(grumble)) + 1e-9) * (0.7 + 0.3 * np.sin(2 * np.pi * t / length * 2))
    return looped + grumble * 0.25
