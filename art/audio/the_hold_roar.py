"""The Hold's silent scream made audible, at half its health (the face joins the fight), 1.8 s with its tail: a
huge, deep, rising stone roar. The souls of the wall roaring as one (many low voices, rough, the vowel opening from
"oh" to "ah", climbing nearly an octave), a thin airy scream far above them (the same cry, voiceless, through the
cracks), the stone grinding with it, the cave's breath forced out through it all; and the cave shaking: a deep
trembling rumble, the chamber booming, rubble and grit pouring down everywhere. Mono. One variant: it is heard once.
Uses the_hold_wake's kit.
`synth` and `np` are injected by tools/audio/build_sound.py."""
VARIANTS = 1
PEAK = 0.89

SR = synth.SR


def kit():
    import importlib.util, os
    spec = importlib.util.spec_from_file_location("the_hold_wake", os.path.join(os.path.dirname(__file__), "the_hold_wake.py"))
    k = importlib.util.module_from_spec(spec)
    k.synth, k.np = synth, np
    spec.loader.exec_module(k)
    return k


def render(v, rng):
    k = kit()
    d = 1.6
    n = int(d * SR)
    t = synth.seconds(d)
    env = k.curve(d, [(0, 0.0), (0.12, 0.45), (0.5, 0.8), (1.05, 1.0), (1.3, 0.85), (d, 0.0)], 0.06)
    f0 = k.curve(d, [(0, 40), (0.9, 66), (1.3, 72), (d, 62)], 0.08)
    oh = k.souls(d, rng, f0, 9, "oh", rasp=0.65, breath=0.3, spread=0.035, dark=1500)
    ah = k.souls(d, rng, f0 * 1.003, 9, "ah", rasp=0.65, breath=0.3, spread=0.035, dark=2200)
    opening = np.clip(t / 0.9, 0, 1)
    roar = k.norm(oh) * (1 - opening) + k.norm(ah) * opening
    out = k.norm(roar) * env
    # the scream above: voiceless, through the cracks
    def mag(times, freqs):
        e = np.interp(times * SR, np.arange(n), env)[:, None]
        f = np.maximum(freqs[None, :], 1)
        ff = np.interp(times * SR, np.arange(n), f0)[:, None] * 22
        return e ** 2 * (np.exp(-0.5 * (np.log(f / ff) / 0.25) ** 2) + 0.5 * np.exp(-0.5 * (np.log(f / (ff * 1.6)) / 0.2) ** 2))
    out += k.norm(synth.spectral_noise(d, rng, mag, n_fft=2048, hop=256)) * 0.18
    out += k.norm(k.grind(d, env, rng, 200)) * 0.4 * env
    out += k.norm(k.gust_air(d, env, rng, inward=False, size=1.6)) * 0.3
    # the cave shaking: a trembling rumble, the chamber booming, rubble pouring
    tremble = 0.6 + 0.4 * np.sin(2 * np.pi * np.cumsum(7 + 4 * env) / SR) * env
    rumble = k.fshape(rng.normal(0, 1, n), lambda f: k.lows(f, 90, 2) * k.highs(f, 22, 1))
    out += k.norm(rumble) * env * tremble * 0.6
    out += k.norm(k.modes(d, rng, 1.0, 1.2)) * env * 0.25
    out += k.norm(k.rubble(d, rng, 90, 0.25, 1.3, 1.0, pour=0.5)) * 0.3
    out = k.saturate(synth.highpass(out, 20), 1.6)
    out = k.chamber(out, rng, 2.0, 0.4)
    return k.trim(out, -50, 0.4)
