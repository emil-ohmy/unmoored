"""A hand of the Hold tearing out of the wall again, 1.2 s: the stone straining (a tight grind), cracking open (a
sharp split, a heavy knock, a crackle of the break running on, a second split after), the black strands that held it
stretching and snapping one after another (the Kept's tar, much bigger), rubble pouring off and bouncing on the
floor. Variants: 1 plain, 2 slower, one big split. Uses the_hold_wake's kit.
`synth` and `np` are injected by tools/audio/build_sound.py."""
VARIANTS = 2
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
    kept = k.load("kept_emerge")
    d = 1.2
    n = int(d * SR)
    t = synth.seconds(d)
    first = [0.2, 0.28][v]
    speed = k.curve(d, [(0, 0.15), (first - 0.02, 0.6), (first + 0.03, 1.0), (first + 0.4, 0.5), (d, 0.0)], 0.02)
    out = k.norm(k.grind(d, speed, rng, 220)) * 0.45
    # the tar pulled tight before it goes
    pull = first + 0.02
    tension = np.clip(synth.seconds(pull) / pull, 0, 1) ** 1.3
    s = kept.stretch(pull, rng, tension) * tension
    synth.place(out, k.norm(synth.lowpass(s, 1500)) * 0.3, 0.0)
    splits = [(first, 1.0), (first + 0.17, 0.7)] if v == 0 else [(first, 1.15)]
    for at, size in splits:
        synth.place(out, k.norm(k.crack(rng, size, 0.7)) * 0.9 * size, at)
        synth.place(out, k.norm(k.impact(rng, 44, 0.6, 0.5, 0.2)) * 0.5 * size, at)
    synth.place(out, k.norm(k.strands(rng, 5, 0.35, 0.6)) * 0.45, first + 0.03)
    out += k.norm(k.rubble(d, rng, 40, first + 0.05, 0.8, 1.0, pour=0.3)) * 0.3
    out = synth.highpass(out, 22)
    out *= np.clip(t / 0.05, 0, 1)
    out = k.chamber(out, rng, 1.2, 0.3)
    return k.trim(out, -48, 0.25)
