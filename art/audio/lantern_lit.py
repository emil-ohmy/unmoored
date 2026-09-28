"""A Mooring post's lantern catches for the first time: a soft 'fwoomp' of flame taking, a few crackles, and a
warm three-note chime (D major over the music's D minor: the one safe, bright moment) ringing out."""


def render(v, rng):
    duration = 3.4
    out = np.zeros(int(duration * synth.SR))
    catch = 0.45
    fwoomp = synth.lowpass(synth.white(catch, rng), synth.glide(250, 3200, catch, 0.5))
    fwoomp *= synth.envelope(catch, 0.06, 0, curve=2.0)
    fwoomp /= np.max(np.abs(fwoomp)) + 1e-9
    body = synth.sine(synth.glide(110, 70, catch), catch) * synth.envelope(catch, 0.03, 0, curve=3.0)
    synth.place(out, fwoomp * 0.6 + body * 0.5, 0.0)
    for _ in range(9):
        d = rng.uniform(0.004, 0.012)
        crack = synth.bandpass(synth.white(d, rng), rng.uniform(1500, 4000), 2.0) * synth.decay(d, d / 3)
        synth.place(out, crack * rng.uniform(0.6, 1.4), rng.uniform(0.1, 1.2))
    for at, name, amp in ((0.12, "D5", 0.5), (0.28, "A5", 0.38), (0.44, "F#6", 0.26)):
        synth.place(out, synth.glass(synth.note(name), duration - at, rng) * amp, at)
    hall = synth.reverb_ir(3.0, 2.4, rng, brightness=0.3)[0]
    wet = synth.fft_convolve(out, hall)[:len(out)]
    return out * 0.8 + wet / (np.max(np.abs(wet)) + 1e-9) * 0.35
