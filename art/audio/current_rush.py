"""Fast water: a river running hard through a stone channel, a steady deep rush with a churning, gurgling surface over
it, the water tearing at the walls. A seamless loop."""
LOOP = True
PEAK = 0.6


def render(v, rng):
    duration = 6.0
    fade = 1.0
    total = duration + fade
    t = synth.seconds(total)
    body = synth.lowpass(synth.white(total, rng), 450) * (0.8 + 0.2 * synth.smooth_random(total, rng, rate=0.7))
    churn = synth.bandpass(synth.white(total, rng), 1300, 0.9) * synth.smooth_random(total, rng, rate=9.0, low=0.3, high=1.0) ** 2
    hiss = synth.highpass(synth.white(total, rng), 3500) * synth.smooth_random(total, rng, rate=2.0, low=0.4, high=0.8)
    out = body / (np.max(np.abs(body)) + 1e-9) * 0.6 + churn / (np.max(np.abs(churn)) + 1e-9) * 0.35 \
        + hiss / (np.max(np.abs(hiss)) + 1e-9) * 0.08
    return synth.loop_crossfade(out, fade)
