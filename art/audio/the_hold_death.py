"""The Hold, That Would Not Let Go, letting go: 8 s. A long, falling, exhausted sigh of the whole cave (the souls of
the wall as one, a voice nearly all breath, sinking more than an octave, and the cave's breath going out with it,
slow and huge, the cracks whistling down into silence); stone settling as the face sags to the floor (slow heavy
grinding, dull cracks deep in the rock, grit trickling, a last soft thud as it comes to rest); then the mouth opening
wide on the way out (a long slow grind of the jaw, its catches dragging); and at the end, faintly, wind from outside
through the opening (soft gusts and a thin whistle at the crack, daylight's air, fading in as the rest falls quiet).
Mono. One variant. Uses the_hold_wake's kit, hold_breath_in and hold_creak.
`synth` and `np` are injected by tools/audio/build_sound.py."""
VARIANTS = 1
PEAK = 0.85

SR = synth.SR


def kit():
    import importlib.util, os
    spec = importlib.util.spec_from_file_location("the_hold_wake", os.path.join(os.path.dirname(__file__), "the_hold_wake.py"))
    k = importlib.util.module_from_spec(spec)
    k.synth, k.np = synth, np
    spec.loader.exec_module(k)
    return k


def wind(d, rng):
    """Wind from outside through the opening: soft slow gusts, a thin whistle at the crack rising with them."""
    n = int(d * SR)
    gusts = 0.35 + 0.65 * synth.smooth_random(d, rng, 0.7, 0, 1)[:n] ** 1.5

    def mag(times, freqs):
        g = np.interp(times * SR, np.arange(n), gusts)[:, None]
        f = np.maximum(freqs[None, :], 1)
        body = np.exp(-0.5 * (np.log(f / (450 + 350 * g)) / 0.9) ** 2)
        whistle = 0.5 * g ** 2 * np.exp(-0.5 * ((f - (1150 + 120 * g)) / 18) ** 2)
        return g * (body + whistle) * (f / 80) ** 2 / (1 + (f / 80) ** 2)
    return synth.spectral_noise(d, rng, mag, n_fft=8192, hop=512)


def render(v, rng):
    k = kit()
    breath_kit = k.load("hold_breath_in")
    creak = k.load("hold_creak")
    d = 8.0
    n = int(d * SR)
    t = synth.seconds(d)
    out = np.zeros(n)
    # the sigh: the souls' voice, nearly all breath, falling; the cave's breath going out with it
    sd = 5.2
    sigh_env = k.curve(sd, [(0, 0), (0.5, 0.85), (1.0, 1.0), (2.5, 0.6), (4.2, 0.15), (sd, 0)], 0.15)
    f0 = k.curve(sd, [(0, 62), (1.0, 56), (3.0, 38), (sd, 29)], 0.2)
    sigh = k.souls(sd, rng, f0, 8, "ah", rasp=0.55, breath=1.0, dark=1200)
    synth.place(out, k.norm(sigh) * sigh_env * 0.55, 0.0)
    flow = k.curve(sd, [(0, 0), (0.6, 0.9), (1.2, 1.0), (3.0, 0.45), (sd, 0)], 0.2)
    synth.place(out, k.norm(k.gust_air(sd, flow, rng, inward=False, size=1.7)) * 0.55, 0.0)
    synth.place(out, k.norm(breath_kit.cracks(sd, flow, False, rng, 30)) * 0.05, 0.0)
    synth.place(out, k.norm(breath_kit.mass(sd, flow, False, rng)) * 0.4, 0.0)
    # the face sagging down: slow heavy grinding, dull cracks deep in the rock, grit, a soft last thud
    speed = k.curve(d, [(0, 0), (0.8, 0.2), (1.8, 0.65), (2.6, 0.5), (3.2, 0.15), (3.5, 0.05), (d, 0)], 0.15)
    out += k.norm(k.grind(d, speed, rng, 150)) * 0.45 * np.clip(speed * 3, 0, 1)
    for at, gain in ((1.6, 0.4), (2.5, 0.5)):
        synth.place(out, k.norm(creak.settle(rng, 1.6)) * gain, at)
    synth.place(out, k.norm(k.impact(rng, 36, 1.0, 0.8, 0.3)) * 0.55, 3.15)
    out += k.norm(k.rubble(d, rng, 40, 3.2, 1.6, 0.6, pour=0.3)) * 0.18
    # the mouth opening wide: a long slow grind of the jaw, its catches dragging
    md = 2.4
    mspeed = k.curve(md, [(0, 0), (0.5, 0.5), (1.4, 0.4), (2.0, 0.15), (md, 0)], 0.1)
    synth.place(out, k.norm(k.grind(md, mspeed, rng, 210)) * 0.4 * np.clip(mspeed * 2.5, 0, 1), 3.7)
    synth.place(out, k.norm(creak.groan(md, 4, 13, 3, 0.35, 1.0, rng)) * 0.35, 3.7)
    synth.place(out, k.norm(creak.settle(rng, 1.2)) * 0.2, 5.7)
    # wind from outside, faintly, through the opening
    wd = 4.0
    w = k.norm(wind(wd, rng)) * k.curve(wd, [(0, 0), (1.6, 0.8), (2.6, 1.0), (wd, 0.0)], 0.3)
    synth.place(out, w * 0.2, d - wd)
    out = synth.highpass(out, 20)
    out *= np.clip(t / 0.4, 0, 1)
    out = k.chamber(out, rng, 2.4, 0.4, tail=False)
    return out * np.clip((d - t) / 0.6, 0, 1) ** 2
