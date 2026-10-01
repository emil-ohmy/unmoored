"""The Watchman's reaping hook drawn back (behind you): his coat creaking as he twists, and the blade's edge rasping
up through the straw, a thin ring of iron at the top of it. The tell: turn and face him."""
VARIANTS = 2


def render(v, rng):
    duration = 0.75
    out = np.zeros(int(duration * synth.SR))
    t = synth.seconds(0.6)
    rasp = synth.bandpass(synth.white(0.6, rng), synth.glide(1500, 4800, 0.6, 1.4), 3.5)
    rasp *= 1 + 0.5 * np.sin(2 * np.pi * 38 * t)  # (the edge catching the stalks)
    rasp *= synth.envelope(0.6, 0.5, 0, curve=3.0, peak_at=0.52)
    synth.place(out, rasp / (np.max(np.abs(rasp)) + 1e-9) * 0.45, 0.0)
    ring = np.sin(2 * np.pi * 3150 * rng.uniform(0.97, 1.03) * synth.seconds(0.3)) * synth.decay(0.3, 0.07)
    ring += 0.4 * np.sin(2 * np.pi * 5230 * synth.seconds(0.3)) * synth.decay(0.3, 0.04)
    synth.place(out, ring * 0.14, 0.5)
    groan = synth.bandpass(synth.brown(0.5, rng), 380, 4.0) * synth.envelope(0.5, 0.2, 0, curve=1.6)
    synth.place(out, groan / (np.max(np.abs(groan)) + 1e-9) * 0.3, 0.0)
    return out
