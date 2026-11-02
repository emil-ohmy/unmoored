"""An eye in the Hold's wall closing behind you: a soft wet seal, 0.3-0.6 s, very close and small. The lid slides
down (a short moist hush, falling), meets the lower lip with a soft wet "tup" (a little suction, a dull low pop
bending down), and a few sticky pops settle after it. Uses hold_eye_open's code.
Variants: 1 plain, 2 quick and light, 3 slow, a long wet seal, 4 squeezed shut (a double tup).
`synth` and `np` are injected by tools/audio/build_sound.py."""
VARIANTS = 4
PEAK = 0.6  # small and close

SR = synth.SR
# (length, slide length, seal at, sticky pops after)
SHAPES = [(0.45, 0.2, 0.18, 8), (0.32, 0.12, 0.11, 5), (0.6, 0.32, 0.29, 12), (0.52, 0.18, 0.16, 9)]


def seal(rng, f0=None):
    """The wet "tup": a dull low pop bending down (two moist surfaces meeting and sucking together), a soft
    breath of noise in it."""
    d = 0.1
    t = synth.seconds(d)
    f = (f0 or rng.uniform(210, 290)) * (1 - 0.45 * np.clip(t / 0.03, 0, 1))
    body = np.sin(2 * np.pi * np.cumsum(f) / SR) * np.exp(-t / 0.018)
    smack = synth.bandpass(synth.white(d, rng), rng.uniform(900, 1400), 1.5) * np.exp(-t / 0.005) * 0.5
    return (body + smack) * np.clip(t / 0.004, 0, 1)


def render(v, rng):
    import importlib.util, os
    spec = importlib.util.spec_from_file_location("eye", os.path.join(os.path.dirname(__file__), "hold_eye_open.py"))
    kit = importlib.util.module_from_spec(spec)
    kit.synth, kit.np = synth, np
    spec.loader.exec_module(kit)
    d, glide_d, seal_at, after = SHAPES[v]
    out = np.zeros(int(d * SR))
    synth.place(out, kit.norm(kit.slide(glide_d, rng, rng.uniform(800, 1100), falling=True)) * 0.35, 0.01)
    synth.place(out, kit.norm(seal(rng)) * 0.8, seal_at)
    if v == 3:  # squeezed: pressed shut a second time
        synth.place(out, kit.norm(seal(rng, rng.uniform(170, 210))) * 0.5, seal_at + 0.09)
    tail = d - seal_at - 0.03
    late = kit.pops(tail, after, rng, 1100, 3200, density=lambda u: (1 - u) ** 2)
    synth.place(out, kit.norm(late) * 0.4, seal_at + 0.015)
    out = synth.lowpass(out, 6000)
    out = kit.close_room(out, rng)
    t = synth.seconds(d)
    return out * np.clip((d - t) / 0.06, 0, 1) ** 2
