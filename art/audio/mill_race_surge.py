"""A surge running down the mill-race: water shoved along a stone channel, a hollow rushing slap that travels and
gathers, gurgling against the walls."""
VARIANTS = 1


def render(v, rng):
    duration = 1.6
    t = synth.seconds(duration)
    rush = synth.bandpass(synth.white(duration, rng), 900, 1.0) * synth.envelope(duration, 0.08, 0, curve=1.5)
    gurgle = synth.smooth_random(duration, rng, rate=11.0, low=0.4, high=1.0) ** 2
    hollow = synth.lowpass(synth.white(duration, rng), 300) * synth.envelope(duration, 0.05, 0, curve=2.0)
    out = rush / (np.max(np.abs(rush)) + 1e-9) * gurgle * 0.5 + hollow / (np.max(np.abs(hollow)) + 1e-9) * 0.4
    slap = synth.lowpass(synth.white(0.15, rng), 1200) * synth.decay(0.15, 0.03)
    synth.place(out, slap / (np.max(np.abs(slap)) + 1e-9) * 0.5, 0.0)
    return out
