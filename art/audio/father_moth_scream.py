"""Father Moth's scream, as he takes wing for the last time and the lamps come down: an old man's hoarse cry torn
ragged, rising into a dry rasping shriek of wings and leaves, the glasshouse's iron ringing under it."""
VARIANTS = 1


def render(v, rng):
    duration = 3.0
    out = np.zeros(int(duration * synth.SR))
    # The cry: a hoarse voice, rising and breaking.
    cry = synth.voice(synth.glide(150, 290, 2.2, 0.7), 2.2, rng, vowel="ah", vibrato=0.03)
    cry = synth.bandpass(cry + 0.5 * synth.white(2.2, rng) * synth.envelope(2.2, 0.4, 0, curve=1.5), 900, 0.7)
    cry *= synth.envelope(2.2, 0.35, 0, curve=1.6, peak_at=1.2)
    synth.place(out, cry / (np.max(np.abs(cry)) + 1e-9) * 0.75, 0.0)
    # The shriek: dry wings and leaves, rasping.
    rasp = synth.bandpass(synth.white(2.4, rng), synth.glide(1800, 4200, 2.4, 1.2), 1.4) * synth.envelope(2.4, 1.0, 0, curve=1.4, peak_at=1.4)
    rasp *= 1 + 0.9 * np.sin(2 * np.pi * synth.glide(30, 55, 2.4) * synth.seconds(2.4))
    synth.place(out, rasp / (np.max(np.abs(rasp)) + 1e-9) * 0.4, 0.3)
    # The iron of the glasshouse ringing under it.
    for f, at in ((233.0, 0.6), (311.0, 0.9), (415.0, 1.2)):
        ring = synth.glass(f, 1.6, rng) * 0.12
        synth.place(out, ring, at)
    return out
