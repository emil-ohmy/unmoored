"""Healing: a soft, rising, breathy shimmer while the charge is drawn in (0.7 s, the heal's length), then a warm
chord blooms as it takes hold (D major: the safe, warm colour, like the lantern), with a soft exhale of relief."""
HEAL = 0.7


def render(v, rng):
    duration = 2.0
    out = np.zeros(int(duration * synth.SR))
    rise = synth.bandpass(synth.white(HEAL, rng), synth.glide(500, 2400, HEAL, 1.4), 2.0)
    rise *= synth.envelope(HEAL, 0, 0, curve=1.0, peak_at=HEAL * 0.98)
    shimmer = sum(synth.sine(synth.glide(f, f * 2, HEAL, 1.3), HEAL, rng.uniform(0, 6)) for f in (440.0, 660.0))
    shimmer *= synth.envelope(HEAL, 0, 0, curve=1.0, peak_at=HEAL * 0.98) * 0.15
    synth.place(out, rise / (np.max(np.abs(rise)) + 1e-9) * 0.35 + shimmer, 0.0)
    for name, amp, at in (("D5", 0.45, 0.0), ("F#5", 0.32, 0.03), ("A5", 0.28, 0.06), ("D4", 0.3, 0.0)):
        synth.place(out, synth.glass(synth.note(name), duration - HEAL, rng) * amp, HEAL + at)
    exhale = synth.bandpass(synth.white(0.5, rng), 1100, 0.7) * synth.envelope(0.5, 0.05, 0, curve=2.0)
    synth.place(out, exhale / (np.max(np.abs(exhale)) + 1e-9) * 0.12, HEAL + 0.02)
    hall = synth.reverb_ir(2.0, 1.6, rng, brightness=0.3)[0]
    wet = synth.fft_convolve(out, hall)[:len(out)]
    return out * 0.85 + wet / (np.max(np.abs(wet)) + 1e-9) * 0.25
