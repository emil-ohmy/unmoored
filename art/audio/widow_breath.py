"""The Widow Hale drawing breath (her knell's tell, 1.2 s): the Keener's breath, grown: a long deep inhale through
heavy crape, shuddering as it rises (a grief held down, the chest catching in small tremors), a low chest under it, and
a faint high ringing building beneath it all, the great bell's rim singing before it's struck (its nominal and
upper partials on E, swelling), the hum of E1 coming up from the stone; cut off at the top."""


def render(v, rng):
    duration = 1.25
    out = np.zeros(int(duration * synth.SR))
    d = 1.17
    t = synth.seconds(d)
    # The shudder: a tremor in the chest, irregular, stronger in the middle of the breath.
    rate = 9.0 + 3.0 * synth.smooth_random(d, rng, 3.0)
    tremor = 0.5 + 0.5 * np.sin(2 * np.pi * np.cumsum(rate) / synth.SR)
    depth = 0.55 * np.sin(np.clip(t / d, 0, 1) * np.pi) ** 0.7
    shudder = 1 - depth * tremor ** 2
    # Two deeper catches, the sob caught in the throat.
    for at in (0.38, 0.71):
        shudder *= 1 - 0.7 * np.exp(-0.5 * ((t - at) / 0.035) ** 2)
    rise = synth.envelope(d, 0.9, 0, curve=3.0, peak_at=0.97) * np.clip((d - t) / 0.03, 0, 1)
    # The inhale through crape: lower and fuller than a Keener's, its band rising.
    air = synth.bandpass(synth.white(d, rng), synth.glide(260, 950, d, 1.2), 0.9)
    air += 0.4 * synth.bandpass(synth.white(d, rng), synth.glide(1400, 2600, d, 1.4), 2.0)  # (the cloth's hiss)
    air = air / (np.max(np.abs(air)) + 1e-9) * rise * shudder
    synth.place(out, air * 0.6, 0.0)
    # The chest: a low hollow resonance of the breath, rising a little.
    chest = synth.bandpass(synth.white(d, rng), synth.glide(140, 230, d), 3.0) * rise * shudder
    synth.place(out, chest / (np.max(np.abs(chest)) + 1e-9) * 0.35, 0.0)
    # Small voiced catches at the sob's breaks, low (a contralto's, not a Keener's).
    for at in (0.38, 0.71):
        sob = synth.voice(synth.glide(220, 196, 0.09), 0.09, rng, vowel="oh") * synth.envelope(0.09, 0.015, 0.07)
        synth.place(out, sob / (np.max(np.abs(sob)) + 1e-9) * 0.16, at - 0.03)
    # The bell's hum, low, swelling from the stone.
    e1 = synth.note("E1")
    hum = sum(a * np.sin(2 * np.pi * e1 * r * t + rng.uniform(0, 6)) for r, a in ((1.0, 0.7), (2.0, 1.0), (4.0, 0.35), (4.76, 0.2)))
    hum *= (t / d) ** 2.2 * np.clip((d - t) / 0.03, 0, 1)
    synth.place(out, hum / (np.max(np.abs(hum)) + 1e-9) * 0.42, 0.03)
    # The rim singing: a faint high ringing, a hair off true (beating), building under the breath.
    ring = np.zeros_like(t)
    for f, a in ((synth.note("E6"), 1.0), (synth.note("E6") * 1.004, 0.8), (synth.note("B6") * 1.003, 0.4),
                 (synth.note("E7") * 0.997, 0.15)):
        ring += a * np.sin(2 * np.pi * f * t + rng.uniform(0, 6))
    ring *= (t / d) ** 2.2 * np.clip((d - t) / 0.03, 0, 1)
    synth.place(out, ring / (np.max(np.abs(ring)) + 1e-9) * 0.12, 0.0)
    fade = int(0.03 * synth.SR)
    out[-fade:] *= np.linspace(1, 0, fade)
    return out
