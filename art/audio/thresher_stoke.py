"""A Thresher stoked by fire: a deep whoomph as the firebox catches, the boiler's whistle shrieking up, a roar from
the stack. (Fire doesn't hurt it: it makes it worse.)"""
VARIANTS = 1


def render(v, rng):
    duration = 1.8
    out = np.zeros(int(duration * synth.SR))
    t = synth.seconds(0.4)
    whoomph = synth.lowpass(synth.brown(0.4, rng), 400) * np.exp(-t / 0.1)
    synth.place(out, whoomph / (np.max(np.abs(whoomph)) + 1e-9) * 0.7, 0.0)
    wt = synth.seconds(1.2)
    whistle = (np.sin(2 * np.pi * np.cumsum(synth.glide(900, 1250, 1.2, 0.5)) / synth.SR) + 0.4 * np.sin(2 * np.pi * np.cumsum(synth.glide(1360, 1880, 1.2, 0.5)) / synth.SR))
    whistle *= synth.envelope(1.2, 0.2, 0, curve=1.5) * (1 + 0.1 * np.sin(2 * np.pi * 6 * wt))
    whistle += synth.bandpass(synth.white(1.2, rng), 1200, 4.0) * 0.4
    synth.place(out, whistle / (np.max(np.abs(whistle)) + 1e-9) * 0.4, 0.15)
    roar = synth.bandpass(synth.white(1.4, rng), 600, 0.8) * synth.envelope(1.4, 0.2, 0, curve=2.0)
    synth.place(out, roar / (np.max(np.abs(roar)) + 1e-9) * 0.35, 0.1)
    return out
