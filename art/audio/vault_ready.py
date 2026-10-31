"""The slam's moment opening, at the top of the Vault: a quick glint of the oar's moonlight, a small glassy chime
(E6 over B5, bright and short) with a breath of shimmer: now."""
VARIANTS = 1


def render(v, rng):
    duration = 0.6
    out = np.zeros(int(duration * synth.SR))
    for note, amp in (("E6", 1.0), ("B5", 0.55)):
        tone = synth.glass(synth.note(note), duration, rng)[:len(out)]
        out[:len(tone)] += tone * amp * np.exp(-synth.seconds(duration)[:len(tone)] / 0.18)
    shimmer = synth.bandpass(synth.white(0.25, rng), 7000, 2.0) * synth.envelope(0.25, 0.01, 0.24, curve=3.0)
    synth.place(out, shimmer / (np.max(np.abs(shimmer)) + 1e-9) * 0.12, 0.0)
    fade = int(0.03 * synth.SR)
    out[-fade:] *= np.linspace(1, 0, fade)
    return out
