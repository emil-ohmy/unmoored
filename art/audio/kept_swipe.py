"""One of the Kept's claws through the air, ~0.35 s: a short, soft, wet swish (a dull whoosh rising and falling, flecks of
tar coming off in a few small sticky pops), a dry scratch at its end (three or four thin claws rasping, a moment
apart). Soft: a frail thing's blow, every strike, hit or miss.
Variants differ in speed, colour and where the claws scratch.
Uses kept_emerge's code. `synth` and `np` are injected by tools/audio/build_sound.py."""
VARIANTS = 4
PEAK = 0.75

SR = synth.SR
# (length, swish top Hz, its peak (fraction), scratch at, scratch band Hz, claws)
SHAPES = [(0.35, 1500, 0.45, 0.22, 3400, 3), (0.32, 1800, 0.4, 0.19, 4000, 4), (0.38, 1300, 0.5, 0.25, 3000, 3),
          (0.35, 1600, 0.42, 0.21, 3700, 4)]


def render(v, rng):
    import importlib.util, os
    spec = importlib.util.spec_from_file_location("kept", os.path.join(os.path.dirname(__file__), "kept_emerge.py"))
    kit = importlib.util.module_from_spec(spec)
    kit.synth, kit.np = synth, np
    spec.loader.exec_module(kit)

    d, top, peak, scratch_at, band, claws = SHAPES[v]
    n = int(d * SR)
    t = synth.seconds(d)
    out = np.zeros(n)
    sd = scratch_at + 0.05
    st = synth.seconds(sd)
    swish = synth.bandpass(synth.white(sd, rng), synth.arc(380, top, sd, peak), 0.9)
    swish = synth.lowpass(swish, 3000) * np.sin(np.clip(st / sd, 0, 1) * np.pi) ** 2
    synth.place(out, kit.norm(swish) * 0.8, 0.0)
    flecks = kit.pops(sd * 0.6, 7, rng, 600, 1900, density=lambda u: np.sin(u * np.pi))
    synth.place(out, kit.norm(flecks) * 0.28, sd * 0.25)
    for k in range(claws):
        cd = rng.uniform(0.035, 0.06)
        c = kit.scrape(cd, rng, band * rng.uniform(0.85, 1.2), density=rng.uniform(500, 900))
        c *= np.sin(np.linspace(0, np.pi, len(c))) ** 0.7 * np.exp(-synth.seconds(cd) / (cd * 0.6))
        synth.place(out, kit.norm(c) * rng.uniform(0.45, 0.6), scratch_at + k * rng.uniform(0.008, 0.016))
    out = synth.highpass(out, 150)
    out = kit.room(out, rng, 0.1)
    return out * np.clip((d - t) / 0.06, 0, 1) ** 2
