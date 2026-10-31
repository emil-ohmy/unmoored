"""The Hall of the Last Hour's hour bell, tolling as time starts again (its central engine set going): the clock's
works catching (a heavy iron clunk and a ratchet's run), then one great bronze toll on E2, the Hall's key, its hum
and its ring going on a long while through the stone, a cold hall's tail."""
PEAK = 0.9


def render(v, rng):
    duration = 10.0
    t = synth.seconds(duration)
    out = np.zeros_like(t)
    # The works catching: a clunk, then a quick ratchet.
    clunk = synth.lowpass(synth.white(0.12, rng), 700) * synth.decay(0.12, 0.03)
    synth.place(out, clunk / (np.max(np.abs(clunk)) + 1e-9) * 0.5, 0.0)
    for k in range(7):
        c = synth.bandpass(synth.white(0.02, rng), 2600, 3.0) * synth.decay(0.02, 0.004)
        synth.place(out, c * 0.25, 0.08 + k * 0.035)
    # The toll: inharmonic church-bell partials, the hum longest.
    strike_at = 0.45
    d = duration - strike_at
    tt = synth.seconds(d)
    base = synth.note("E2")
    bell = np.zeros_like(tt)
    for ratio, amp, tau in ((0.5, 0.9, 6.5), (1.0, 1.0, 4.5), (1.19, 0.55, 3.2), (1.5, 0.35, 2.4), (2.0, 0.5, 2.0),
                            (2.52, 0.22, 1.3), (3.0, 0.14, 0.9), (4.16, 0.08, 0.5)):
        f = base * ratio * rng.uniform(0.998, 1.002)
        beat = 1 + 0.08 * np.sin(2 * np.pi * rng.uniform(0.6, 1.4) * tt)  # (a slow beat in each partial)
        bell += amp * synth.sine(f, d, rng.uniform(0, 6)) * np.exp(-tt / tau) * beat
    strike = synth.lowpass(synth.white(0.05, rng), 2200) * synth.decay(0.05, 0.012)
    synth.place(bell, strike * 0.7, 0.0)
    bell *= np.clip(tt / 0.003, 0, 1)
    synth.place(out, bell / (np.max(np.abs(bell)) + 1e-9), strike_at)
    hall = synth.reverb_ir(6.0, 5.0, rng, brightness=0.3)[0]
    wet = synth.fft_convolve(out, hall)[:len(out)]
    out = out * 0.7 + wet / (np.max(np.abs(wet)) + 1e-9) * 0.5
    fade = int(0.5 * synth.SR)
    out[-fade:] *= np.linspace(1, 0, fade)
    return out
