"""Heavy oar swing: a slower, deeper whoosh with a low 'whoom' under it so the weight is felt."""
VARIANTS = 3


def render(v, rng):
    duration = 0.5 * rng.uniform(0.92, 1.08)
    k = rng.uniform(0.92, 1.08)
    peak = duration * 0.45
    air = synth.bandpass(synth.white(duration, rng), synth.arc(220 * k, 1100 * k, duration, 0.45), 1.1)
    body = synth.lowpass(synth.white(duration, rng), synth.arc(90, 420, duration, 0.45)) * 1.1
    whoosh = (air + body) * synth.envelope(duration, 0, 0, curve=2.2, peak_at=peak)
    whoom = synth.sine(synth.glide(85 * k, 52 * k, 0.4), 0.4) * synth.envelope(0.4, 0.03, 0, curve=3.0) * 0.55
    return synth.place(whoosh, whoom, peak - 0.05)
