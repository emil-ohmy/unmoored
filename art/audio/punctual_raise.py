"""The Count's tell (1.9 s): the winding key raised overhead, a ratchet's three clicks; on the next beat (1 s in) a firm
tick, the jolt; then the brass straining, a bowed-metal whine swelling till the blow."""
PEAK = 0.7


def render(v, rng):
    d = 1.9
    out = np.zeros(int(d * synth.SR))
    for k, at in enumerate((0.0, 0.07, 0.15)):
        c = synth.bandpass(synth.white(0.04, rng), 2200 + 200 * k, 3.0) * synth.decay(0.04, 0.005)
        synth.place(out, c * 0.7, at)
    t = synth.seconds(0.2)
    jolt = synth.bandpass(synth.white(0.2, rng), 3000, 4.0) * np.exp(-t / 0.006) + np.sin(2 * np.pi * 1800 * t) * np.exp(-t / 0.04) * 0.5
    synth.place(out, jolt / (np.max(np.abs(jolt)) + 1e-9) * 0.9, 1.0)
    w = 0.85
    tw = synth.seconds(w)
    f = 520 + 260 * (tw / w) ** 2
    phase = 2 * np.pi * np.cumsum(f) / synth.SR
    whine = (np.sin(phase) + 0.4 * np.sin(2.01 * phase) + 0.2 * np.sin(3.03 * phase)) * (tw / w) ** 2 * 0.35
    whine *= np.clip((w - tw) / 0.02, 0, 1)
    synth.place(out, whine, 1.03)
    return out
