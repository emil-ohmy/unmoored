"""The Hold struck (a hand or the face), 0.5 s: a dull stone knock (a heavy low thud with a hard edge, a chip of
stone cracking off) and under it, a beat behind, a deep pained groan of the souls in the wall (many low voices as
one, rough, falling). Variants: 1 plain, 2 the groan higher, an "ah", 3 lower and heavier, 4 a sharper knock, the
groan cut short. Uses the_hold_wake's kit.
`synth` and `np` are injected by tools/audio/build_sound.py."""
VARIANTS = 4
PEAK = 0.7  # heard on every blow: kept below the attacks

SR = synth.SR
# (thud Hz, chip size, groan from, groan to, vowel, groan length)
SHAPES = [(58, 0.5, 58, 46, "oh", 0.45), (64, 0.45, 70, 55, "ah", 0.45), (50, 0.6, 50, 40, "oh", 0.5),
          (66, 0.4, 62, 52, "ah", 0.32)]


def kit():
    import importlib.util, os
    spec = importlib.util.spec_from_file_location("the_hold_wake", os.path.join(os.path.dirname(__file__), "the_hold_wake.py"))
    k = importlib.util.module_from_spec(spec)
    k.synth, k.np = synth, np
    spec.loader.exec_module(k)
    return k


def render(v, rng):
    k = kit()
    low, chip, f_a, f_b, vowel, gl = SHAPES[v]
    d = 0.55
    n = int(d * SR)
    out = np.zeros(n)
    out += k.norm(k.impact(rng, low, d, 0.4, 0.8)) * 0.8
    out += k.norm(k.crack(rng, chip, d)) * 0.5
    gt = synth.seconds(gl)
    groan = k.souls(gl, rng, k.curve(gl, [(0, f_a), (gl, f_b)]), 6, vowel, rasp=0.55, breath=0.3, dark=1300)
    groan *= np.sin(np.clip(gt / 0.07, 0, 1) * np.pi / 2) ** 2 * np.clip((gl - gt) / (gl * 0.6), 0, 1) ** 1.5
    synth.place(out, k.norm(groan) * 0.55, 0.04)
    out = k.saturate(synth.highpass(out, 25), 1.5)
    out = k.chamber(out, rng, 0.8, 0.22)
    return k.trim(out, -48, 0.15)
