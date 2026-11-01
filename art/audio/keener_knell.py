"""A Keener's knell, one per ring of sound it sends across the floor (three, about half a second apart): a muffled
funeral bell on E3, struck through crape (its highs held down), a keening voice above it falling a third, and a cold
stone hall's tail. Each variant a little apart (the bell's strike, the voice's vowel)."""
VARIANTS = 3
PEAK = 0.8


def render(v, rng):
    duration = 3.2
    t = synth.seconds(duration)
    base = synth.note("E3")
    bell = np.zeros_like(t)
    # Inharmonic church-bell partials: hum, prime, minor third, fifth, nominal, and above.
    for ratio, amp, tau in ((0.5, 0.9, 2.2), (1.0, 1.0, 1.6), (1.19, 0.6, 1.2), (1.5, 0.3, 0.9), (2.0, 0.45, 0.8),
                            (2.61, 0.18, 0.5), (3.37, 0.1, 0.3)):
        bell += amp * synth.sine(base * ratio * rng.uniform(0.998, 1.002), duration, rng.uniform(0, 6)) * np.exp(-t / tau)
    strike = synth.lowpass(synth.white(0.04, rng), 900) * synth.decay(0.04, 0.01)
    synth.place(bell, strike * 0.8, 0.0)
    bell = synth.lowpass(bell, 1400)  # (through the crape: muffled)
    bell *= np.clip(t / 0.004, 0, 1)
    out = bell / (np.max(np.abs(bell)) + 1e-9)
    # The keen: a voice above the bell, B4 falling to G4, fading under it.
    d = 1.4
    f0 = synth.glide(synth.note("B4"), synth.note("G4"), d, 0.7)
    keen = synth.voice(f0, d, rng, vowel=("ah", "oh", "oo")[v], vibrato=0.012)
    keen *= synth.envelope(d, 0.05, 0, curve=2.5, peak_at=0.12)
    synth.place(out, keen / (np.max(np.abs(keen)) + 1e-9) * 0.3, 0.03)
    hall = synth.reverb_ir(3.0, 2.6, rng, brightness=0.25)[0]
    wet = synth.fft_convolve(out, hall)[:len(out)]
    return out * 0.75 + wet / (np.max(np.abs(wet)) + 1e-9) * 0.4
