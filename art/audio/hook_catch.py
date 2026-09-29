"""The Hook catches the ring: a hard iron clank, and the tether's taut thrum as the pull begins."""


def render(v, rng):
    duration = 0.6
    out = np.zeros(int(duration * synth.SR))
    t = synth.seconds(duration)
    clank = sum(a * np.sin(2 * np.pi * f * t + rng.uniform(0, 6)) * np.exp(-t / tau)
                for f, a, tau in ((820, 1.0, 0.05), (1370, 0.6, 0.035), (2210, 0.4, 0.02), (3400, 0.2, 0.012)))
    hit = synth.bandpass(synth.white(0.02, rng), 1800, 2.0) * synth.decay(0.02, 0.004)
    thrum = synth.sine(synth.glide(140, 190, duration, 0.8), duration) * synth.envelope(duration, 0.05, 0, curve=2.0)
    synth.place(out, clank / (np.max(np.abs(clank)) + 1e-9) * 0.6, 0.0)
    synth.place(out, hit / (np.max(np.abs(hit)) + 1e-9) * 0.5, 0.0)
    synth.place(out, thrum * 0.3, 0.02)
    return out
