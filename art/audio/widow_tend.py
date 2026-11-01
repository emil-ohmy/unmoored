"""The Widow Hale laying her hands on a running machine to stop it (about 2.5 s, from when she starts pressing): her
palms meeting the brass (a soft muted knock, the metal's ring smothered under them), the machine's ticking slowing and
stretching (each tick further from the last, lower, longer, smeared), a low strained hum under her hands rising in
effort, and at the end one held, dead-still note (no vibrato, no beating): time stopping. The machine's own stop
clunk plays apart, at the end."""


def render(v, rng):
    duration = 2.75
    t = synth.seconds(duration)
    out = np.zeros_like(t)
    # The palms on the brass: a muted knock, the ring smothered at once.
    d = 0.5
    tt = synth.seconds(d)
    brass = sum(a * np.sin(2 * np.pi * f * tt + rng.uniform(0, 6)) * np.exp(-tt / tau)
                for f, a, tau in ((310, 1.0, 0.09), (742, 0.5, 0.05), (1310, 0.3, 0.03), (2120, 0.15, 0.02)))
    palm = synth.lowpass(synth.lowpass(synth.white(d, rng), 600), 600) * np.exp(-tt / 0.02)
    knock = (brass + 0.8 * palm / (np.max(np.abs(palm)) + 1e-9)) * np.clip(tt / 0.008, 0, 1) ** 2
    synth.place(out, knock / (np.max(np.abs(knock)) + 1e-9) * 0.5, 0.0)
    synth.place(out, knock / (np.max(np.abs(knock)) + 1e-9) * 0.3, 0.09)  # (the second hand)
    # The ticking slowing and stretching.
    at, gap, k = 0.14, 0.16, 0
    while at < 1.95:
        u = at / 1.95
        stretch = 1 + 4 * u ** 1.5  # each tick longer and duller
        d = 0.06 * stretch
        tt = synth.seconds(d)
        f0 = (1500 if k % 2 == 0 else 1150) * (1 - 0.45 * u)
        click = synth.bandpass(synth.white(d, rng), f0, 4.0) * np.exp(-tt / (0.008 * stretch))
        click *= np.clip(tt / (0.0008 * stretch ** 2), 0, 1)  # (the attack smears too)
        synth.place(out, click / (np.max(np.abs(click)) + 1e-9) * 0.4 * (1 - 0.4 * u), at)
        at += gap
        gap *= 1.25
        k += 1
    # The strain: a low hum under her hands, a cluster on E with a rough edge, rising in effort, then held.
    d = 2.2
    tt = synth.seconds(d)
    effort = np.clip(tt / 1.6, 0, 1) ** 1.5
    f = synth.note("E2") * (1 + 0.02 * effort)  # (pressed sharp, a little)
    strain = np.zeros_like(tt)
    for ratio, a in ((1.0, 1.0), (1.012, 0.7), (2.0, 0.5), (3.0, 0.3 * effort + 0.05), (4.0, 0.2 * effort), (5.0, 0.12 * effort)):
        strain += a * np.sin(2 * np.pi * np.cumsum(f * ratio) / synth.SR + rng.uniform(0, 6))
    strain *= 1 + 0.25 * effort * np.sin(2 * np.pi * np.cumsum(5 + 9 * effort) / synth.SR)  # (trembling with it)
    strain *= synth.envelope(d, 1.6, 0, curve=1.5, peak_at=1.65)
    synth.place(out, strain / (np.max(np.abs(strain)) + 1e-9) * 0.33, 0.05)
    # Time stopping: one pure held note (B4 with its octave, perfectly still), coming in as the ticks give out.
    start = 1.75
    d = duration - start
    tt = synth.seconds(d)
    still = np.sin(2 * np.pi * synth.note("B4") * tt) + 0.35 * np.sin(2 * np.pi * synth.note("B5") * tt) \
        + 0.12 * np.sin(2 * np.pi * synth.note("E4") * tt)
    still *= np.clip(tt / 0.4, 0, 1) ** 2 * np.clip((d - tt) / 0.25, 0, 1)
    synth.place(out, still / (np.max(np.abs(still)) + 1e-9) * 0.2, start)
    return out
