"""The Widow Hale's knell, played once with its first ring (four go out over 0.6 s): the Keener's knell's mother. A great
funeral bell an octave under theirs (E2), a muffled mourning toll: its clapper bound in leather (a dull, padded thud of a
strike, no clang), the bell's highs held down by crape, its hum long; and a low keen in it, a contralto falling a third
(G4 to E4) where the Keeners fall B4 to G4. A cold stone hall's tail. Each variant a little apart (the strike, the
vowel, the bell's beating)."""
VARIANTS = 3
PEAK = 0.82


def render(v, rng):
    duration = 5.5
    t = synth.seconds(duration)
    base = synth.note("E2") * (1.0, 0.997, 1.003)[v]
    bell = np.zeros_like(t)
    # Inharmonic church-bell partials, the hum longest; the upper ones damped quick (the muffle).
    for ratio, amp, tau in ((0.5, 0.75, 4.5), (1.0, 1.0, 3.0), (1.19, 0.6, 2.0), (1.5, 0.35, 1.4), (2.0, 0.5, 1.3),
                            (2.52, 0.25, 0.8), (3.0, 0.16, 0.55), (4.16, 0.08, 0.3), (5.43, 0.04, 0.18)):
        beat = 1 + 0.1 * np.sin(2 * np.pi * rng.uniform(0.5, 1.3) * t + rng.uniform(0, 6))  # (a slow beat in each)
        bell += amp * synth.sine(base * ratio * rng.uniform(0.998, 1.002), duration, rng.uniform(0, 6)) * np.exp(-t / tau) * beat
    bell = synth.lowpass(bell, 900)  # (through the crape)
    bell *= np.clip(t / 0.012, 0, 1)  # (a padded clapper: a soft front, no click)
    # The bound clapper: a dull heavy thud, leather on bronze.
    d = 0.18
    thud = synth.lowpass(synth.brown(d, rng), 320) * synth.decay(d, 0.04)
    thud += 0.5 * np.sin(2 * np.pi * 70 * synth.seconds(d)) * synth.decay(d, 0.05)
    thud *= np.clip(synth.seconds(d) / 0.004, 0, 1)
    out = bell / (np.max(np.abs(bell)) + 1e-9)
    synth.place(out, thud / (np.max(np.abs(thud)) + 1e-9) * (0.55, 0.65, 0.5)[v], 0.0)
    # The crape's edge: a soft breath of cloth stirred by the stroke.
    edge = synth.bandpass(synth.white(0.5, rng), 1800, 0.8) * synth.envelope(0.5, 0.03, 0, curve=3.0)
    synth.place(out, edge / (np.max(np.abs(edge)) + 1e-9) * 0.07, 0.0)
    # The keen: low, a contralto, G4 falling to E4, under the bell.
    d = 1.9
    f0 = synth.glide(synth.note("G4"), synth.note("E4"), d, 0.6)
    keen = synth.voice(f0, d, rng, vowel=("oh", "ah", "oo")[v], vibrato=0.014)
    keen = synth.lowpass(keen, 2200)  # (through the veil)
    keen *= synth.envelope(d, 0.08, 0, curve=2.2, peak_at=0.18)
    synth.place(out, keen / (np.max(np.abs(keen)) + 1e-9) * 0.24, 0.05)
    hall = synth.reverb_ir(4.0, 3.6, rng, brightness=0.22)[0]
    wet = synth.fft_convolve(out, hall)[:len(out)]
    out = out * 0.7 + wet / (np.max(np.abs(wet)) + 1e-9) * 0.45
    fade = int(0.8 * synth.SR)
    out[-fade:] *= np.linspace(1, 0, fade) ** 2
    return out
