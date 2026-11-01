"""The Widow Hale letting the minute go (with her death; 2.5 s of it matter, the tail rings on): silence; then one clock
tick... and a second later its tock at last (the tock the Hall never gets, lower and fuller); a long released sigh (a
low voice sinking B4 to E4, then only breath); the great bell's hum coming up and fading away; and very quietly, under
it, a soft E minor chord on a flue organ that turns to E major (G to G#: a Picardy third) and fades."""


def tick(rng, pitch=1.0):
    """As the Hall's music ticks: a hard click, a small metal ping, a little body."""
    d = 0.3
    t = synth.seconds(d)
    click = synth.lowpass(synth.highpass(synth.white(d, rng), 2500), 7000) * np.exp(-t / 0.004)
    ping = np.sin(2 * np.pi * 3100 * pitch * t) * np.exp(-t / 0.03) * 0.5 \
        + np.sin(2 * np.pi * 5230 * pitch * t) * np.exp(-t / 0.015) * 0.25
    body = synth.bandpass(synth.white(d, rng), 900 * pitch, 2.0) * np.exp(-t / (0.02 / pitch)) * 0.6
    return (click + ping + body) * np.clip(t / 0.0008, 0, 1)


def flue(f, duration, rng):
    t = synth.seconds(duration)
    out = sum(a * np.sin(2 * np.pi * f * k * t + rng.uniform(0, 6)) for k, a in ((1, 1.0), (2, 0.3), (3, 0.1)))
    return out


def render(v, rng):
    duration = 7.0
    t = synth.seconds(duration)
    out = np.zeros_like(t)
    tick_at, tock_at = 0.35, 1.35
    synth.place(out, tick(rng) * 0.7, tick_at)
    synth.place(out, tick(rng, pitch=0.62) * 0.8, tock_at)  # (the tock: at last)
    # The sigh, released after the tock.
    s = 1.5
    d = 2.2
    sigh = synth.voice(synth.glide(synth.note("B4"), synth.note("E4"), d, 0.7), d, rng, vowel="oh", vibrato=0.009)
    sigh = synth.lowpass(sigh, 2400) * synth.envelope(d, 0.1, 0, curve=2.0, peak_at=0.18)
    synth.place(out, sigh / (np.max(np.abs(sigh)) + 1e-9) * 0.3, s)
    d = 1.8
    breath = synth.bandpass(synth.white(d, rng), synth.glide(1100, 350, d), 0.9) * synth.envelope(d, 0.2, 0, curve=2.2, peak_at=0.3)
    synth.place(out, breath / (np.max(np.abs(breath)) + 1e-9) * 0.14, s + 0.9)
    # The great bell's hum: rising softly from the tock, fading over the rest.
    s = tock_at
    d = duration - s
    tt = synth.seconds(d)
    e = synth.note("E2")
    hum = sum(a * np.sin(2 * np.pi * e * r * tt + rng.uniform(0, 6)) * np.exp(-tt / tau)
              for r, a, tau in ((0.5, 1.0, 3.2), (1.0, 0.6, 2.2), (1.19, 0.25, 1.6), (2.0, 0.2, 1.2)))
    hum *= np.clip(tt / 0.6, 0, 1) ** 2 * (1 + 0.1 * np.sin(2 * np.pi * 0.8 * tt))
    synth.place(out, hum / (np.max(np.abs(hum)) + 1e-9) * 0.4, s)
    # The chord: E minor, then the G lifts to G# (E major), very quiet.
    s = 1.7
    d = duration - s
    tt = synth.seconds(d)
    env = np.clip(tt / 1.4, 0, 1) ** 2 * np.clip((d - tt) / 2.2, 0, 1) ** 1.5
    chord = np.zeros_like(tt)
    for name in ("E3", "B3", "E4"):
        chord += flue(synth.note(name), d, rng)
    turn = 1.6  # (when the third lifts, from the chord's start)
    third = np.where(tt < turn, synth.note("G3"),
                     synth.note("G3") + (synth.note("G#3") - synth.note("G3")) * np.clip((tt - turn) / 0.35, 0, 1))
    chord += sum(a * np.sin(2 * np.pi * k * np.cumsum(third) / synth.SR + rng.uniform(0, 6)) for k, a in ((1, 1.0), (2, 0.3), (3, 0.1)))
    chord = synth.lowpass(chord, 1500) * env
    synth.place(out, chord / (np.max(np.abs(chord)) + 1e-9) * 0.17, s)
    hall = synth.reverb_ir(4.0, 3.8, rng, brightness=0.25)[0]
    wet = synth.fft_convolve(out, hall)[:len(out)]
    out = out * 0.75 + wet / (np.max(np.abs(wet)) + 1e-9) * 0.35
    fade = int(1.0 * synth.SR)
    out[-fade:] *= np.linspace(1, 0, fade) ** 2
    return out
