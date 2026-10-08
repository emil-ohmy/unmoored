"""Something heavy and wooden shoved across stone: a rough dragging scrape, a knock as it stops."""
VARIANTS = 2


def render(v, rng):
    duration = 0.9
    t = synth.seconds(duration)
    drag = synth.bandpass(synth.white(duration, rng), 700 + 200 * v, 1.2) * synth.smooth_random(duration, rng, rate=25.0, low=0.3, high=1.0)
    drag *= np.clip(t / 0.02, 0, 1) * np.exp(-t / 0.35)
    out = drag / (np.max(np.abs(drag)) + 1e-9) * 0.5
    knock = synth.lowpass(synth.brown(0.2, rng), 250) * synth.decay(0.2, 0.04)
    synth.place(out, knock / (np.max(np.abs(knock)) + 1e-9) * 0.4, 0.55)
    return out * np.clip((duration - t) / 0.1, 0, 1)
