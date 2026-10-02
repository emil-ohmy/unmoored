"""The Apprentice catching fire: a soft dry whumph as the cloak goes up, a rush of flame, a gasp through the mask."""
VARIANTS = 2


def render(v, rng):
    duration = 0.9
    out = np.zeros(int(duration * synth.SR))
    t = synth.seconds(0.3)
    whumph = synth.lowpass(synth.brown(0.3, rng), 500) * np.exp(-t / 0.07)
    synth.place(out, whumph / (np.max(np.abs(whumph)) + 1e-9) * 0.8, 0.0)
    rush = synth.bandpass(synth.white(0.8, rng), synth.glide(800, 2400, 0.8, 0.5), 0.8) * synth.envelope(0.8, 0.08, 0, curve=2.0)
    synth.place(out, rush / (np.max(np.abs(rush)) + 1e-9) * 0.5, 0.02)
    gasp = synth.bandpass(synth.white(0.25, rng), 1300, 2.0) * synth.envelope(0.25, 0.05, 0, curve=1.5)
    synth.place(out, synth.lowpass(gasp, 2500) / (np.max(np.abs(gasp)) + 1e-9) * 0.35, 0.1)
    return out
