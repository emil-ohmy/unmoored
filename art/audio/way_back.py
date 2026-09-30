"""The Way Back: a knot pulled loose, then a long breath of wind drawn backwards (swelling, then cut off) and a far,
soft bell of the Mooring post as the black closes."""


def render(v, rng):
    duration = 2.4
    out = np.zeros(int(duration * synth.SR))
    knot = synth.bandpass(synth.white(0.06, rng), 1300, 2.0) * synth.decay(0.06, 0.015)
    synth.place(out, knot * 0.5, 0.0)
    wind = synth.bandpass(synth.white(1.3, rng), synth.glide(400, 1800, 1.3, 1.5), 1.1)
    wind *= synth.envelope(1.3, 0, 0, curve=2.5, peak_at=1.25)
    synth.place(out, wind / (np.max(np.abs(wind)) + 1e-9) * 0.45, 0.05)
    bell = synth.glass(synth.note("A4"), 1.1, rng) * 0.25 + synth.glass(synth.note("E5"), 1.1, rng) * 0.12
    hall = synth.reverb_ir(1.5, 1.4, rng, brightness=0.3)[0]
    wet = synth.fft_convolve(bell, hall)[:len(bell)]
    synth.place(out, bell * 0.6 + wet / (np.max(np.abs(wet)) + 1e-9) * 0.3, 1.3)
    return out
