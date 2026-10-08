"""The Stroke from the boat: the oar dug in and hauled through, a heavy plunge and a long tearing rush of water past
the hull as it's driven on, a cold glass note riding it (the Drowned Moon)."""
VARIANTS = 2


def render(v, rng):
    duration = 2.0
    out = np.zeros(int(duration * synth.SR))
    plunge = synth.lowpass(synth.brown(0.35, rng), 300) * synth.envelope(0.35, 0.01, 0, curve=2.0)
    synth.place(out, plunge / (np.max(np.abs(plunge)) + 1e-9) * 0.7, 0.0)
    rush_len = 1.6
    rush = synth.bandpass(synth.white(rush_len, rng), 900 + 200 * v, 0.7) * synth.envelope(rush_len, 0.08, 0, curve=1.6)
    rush *= synth.smooth_random(rush_len, rng, rate=7.0, low=0.6, high=1.0)
    synth.place(out, rush / (np.max(np.abs(rush)) + 1e-9) * 0.55, 0.05)
    note = synth.glass(synth.note(["E5", "A5"][v]), 1.6, rng) * 0.12
    note *= np.clip((1.6 - synth.seconds(1.6)) / 0.5, 0, 1)
    synth.place(out, note, 0.05)
    return out * np.clip((duration - synth.seconds(duration)) / 0.3, 0, 1)
