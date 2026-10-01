"""The Watchman struck by his own fire, turned back: the fire taking his straw with a great rushing roar, a crack
of the coat's dry seams going, and under it a hollow cry through the sack, as of breath driven out."""
VARIANTS = 1


def render(v, rng):
    duration = 1.8
    out = np.zeros(int(duration * synth.SR))
    t = synth.seconds(0.4)
    hit = synth.lowpass(synth.brown(0.4, rng), 600) * np.exp(-t / 0.08)
    synth.place(out, hit / (np.max(np.abs(hit)) + 1e-9) * 0.9, 0.0)
    roar = synth.bandpass(synth.white(1.6, rng), synth.glide(900, 2600, 1.6, 0.5), 0.7) * synth.envelope(1.6, 0.15, 0, curve=2.2)
    synth.place(out, roar / (np.max(np.abs(roar)) + 1e-9) * 0.55, 0.02)
    cry = synth.voice(synth.glide(190, 120, 1.0, 0.8), 1.0, rng, "oo", vibrato=0.03, max_freq=1400) * synth.envelope(1.0, 0.15, 0, curve=1.8)
    synth.place(out, synth.lowpass(cry, 900) / (np.max(np.abs(cry)) + 1e-9) * 0.4, 0.12)
    return out
