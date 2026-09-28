"""The Apprentice takes a blow: a muffled body impact and a short, clipped grunt through the mask (a sung 'ah' or
'oh' falling in pitch, low-passed), with a breath of air. Small and muffled, so it never gets tiresome."""
VARIANTS = 3


def render(v, rng):
    duration = 0.42
    out = np.zeros(int(duration * synth.SR))
    impact = synth.sine(synth.glide(95, 44, 0.16, 0.6), 0.16) * synth.decay(0.16, 0.045)
    synth.place(impact, 0.5 * synth.lowpass(synth.white(0.05, rng), 1100) * synth.decay(0.05, 0.015), 0.0)
    g = 0.24
    f0 = rng.uniform(185, 235)
    env = synth.envelope(g, 0.015, 0, curve=2.2)
    grunt = synth.voice(synth.glide(f0, f0 * 0.72, g, 0.8), g, rng, ["ah", "oh", "ah"][v % 3], vibrato=0.01, max_freq=3000)
    grunt = synth.lowpass(grunt / (np.max(np.abs(grunt)) + 1e-9), 2100) * env
    breath = synth.bandpass(synth.white(g, rng), 1500, 0.9) * env
    synth.place(out, impact / (np.max(np.abs(impact)) + 1e-9) * 0.8, 0.0)
    synth.place(out, grunt / (np.max(np.abs(grunt)) + 1e-9) * 0.55, 0.02)
    synth.place(out, breath / (np.max(np.abs(breath)) + 1e-9) * 0.15, 0.02)
    return out * np.clip(synth.seconds(duration) / 0.001, 0, 1)
