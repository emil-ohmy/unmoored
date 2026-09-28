"""The Apprentice falls: one deep, muffled bell toll that sinks as if dragged under water (its highs close off),
a low thump, a descending sinking whoosh and a few rising bubbles, all in a long dark hall."""


def render(v, rng):
    duration = 6.0
    t = synth.seconds(duration)
    base = synth.note("D2")
    bell = np.zeros_like(t)
    # Inharmonic church-bell partials: hum, prime, minor third, fifth, nominal, and a couple above.
    for ratio, amp, tau in ((0.5, 0.8, 3.5), (1.0, 1.0, 2.8), (1.19, 0.55, 2.0), (1.5, 0.35, 1.6),
                            (2.0, 0.5, 1.4), (2.61, 0.25, 0.9), (3.37, 0.15, 0.6), (4.1, 0.1, 0.4)):
        f = base * ratio * rng.uniform(0.998, 1.002)
        bell += amp * synth.sine(f * (1 - 0.01 * t / duration), duration, rng.uniform(0, 6)) * np.exp(-t / tau)
    strike = synth.lowpass(synth.white(0.03, rng), 1500) * synth.decay(0.03, 0.008)
    synth.place(bell, strike * 0.6, 0.0)
    bell = synth.lowpass(bell, synth.glide(2600, 260, duration, 0.45))  # sinking: the water closes over it
    bell *= np.clip(t / 0.004, 0, 1)

    thump = synth.sine(synth.glide(62, 34, 0.6), 0.6) * synth.decay(0.6, 0.18)
    sink = synth.bandpass(synth.brown(2.2, rng), synth.glide(900, 110, 2.2, 0.7), 1.4)
    sink *= synth.envelope(2.2, 0, 0, curve=2.5, peak_at=0.35)
    sink /= np.max(np.abs(sink)) + 1e-9

    out = bell / (np.max(np.abs(bell)) + 1e-9)
    synth.place(out, thump * 0.9, 0.0)
    synth.place(out, sink * 0.25, 0.05)
    for _ in range(7):  # bubbles rising past the listener
        d = rng.uniform(0.04, 0.09)
        f0 = rng.uniform(260, 520)
        bubble = synth.sine(synth.glide(f0, f0 * 2.2, d, 0.6), d) * synth.envelope(d, 0.004, 0)
        synth.place(out, bubble * rng.uniform(0.05, 0.12), rng.uniform(0.3, 1.8))

    hall = synth.reverb_ir(5.0, 4.0, rng, brightness=0.2)[0]
    wet = synth.fft_convolve(out, hall)[:len(out)]
    return out * 0.7 + wet / (np.max(np.abs(wet)) + 1e-9) * 0.55
