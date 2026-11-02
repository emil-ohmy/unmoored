"""One of the Kept's shuffling footfall, very short (0.2-0.3 s with its peel): a soft, sticky pat on stone (a dull small
thud of something soft and wet, no heel, no click), then the foot peeling off (two or three tiny sticky pops,
a faint wet tear). 4-6 add a knuckle dragging: a small dull tap and a short soft scrape over the stone. Quiet:
heard only near.
Uses kept_emerge's code. `synth` and `np` are injected by tools/audio/build_sound.py."""
VARIANTS = 6
PEAK = 0.45  # quiet: heard only near

SR = synth.SR
# (pat colour Hz, pat body Hz, peel pops, drag)
SHAPES = [(520, 165, 3, False), (600, 180, 2, False), (460, 150, 3, False), (540, 170, 2, True), (480, 155, 3, True),
          (620, 185, 2, True)]


def render(v, rng):
    import importlib.util, os
    spec = importlib.util.spec_from_file_location("kept", os.path.join(os.path.dirname(__file__), "kept_emerge.py"))
    kit = importlib.util.module_from_spec(spec)
    kit.synth, kit.np = synth, np
    spec.loader.exec_module(kit)

    colour, body, peel, drag = SHAPES[v]
    d = 0.3 if drag else 0.22
    n = int(d * SR)
    t = synth.seconds(d)
    out = np.zeros(n)
    pd = 0.08
    pt = synth.seconds(pd)
    pat = synth.lowpass(synth.bandpass(synth.white(pd, rng), colour, 1.0), 1400) * np.exp(-pt / 0.012)
    thud = synth.sine(synth.glide(body * 1.4, body * 0.8, pd, 0.3), pd) * np.exp(-pt / 0.018)
    synth.place(out, (kit.norm(pat) + 0.8 * kit.norm(thud)) * np.clip(pt / 0.002, 0, 1), 0.0)
    # the foot peeling off: tiny sticky pops, a faint wet tear
    pops = kit.pops(0.09, peel, rng, 900, 2400, length=0.015)
    synth.place(out, kit.norm(pops) * 0.32, 0.05)
    td = 0.05
    tear = synth.bandpass(synth.white(td, rng), rng.uniform(1300, 1800), 2.0) * np.sin(np.linspace(0, np.pi, int(td * SR))) ** 2
    synth.place(out, kit.norm(tear) * 0.1, 0.06)
    if drag:
        kd = 0.04
        kt = synth.seconds(kd)
        knock = synth.bandpass(synth.white(kd, rng), rng.uniform(1500, 2000), 3.0) * np.exp(-kt / 0.004) \
            + 0.5 * np.sin(2 * np.pi * rng.uniform(380, 460) * kt) * np.exp(-kt / 0.008)
        synth.place(out, kit.norm(knock) * 0.35, 0.08)
        sd = rng.uniform(0.12, 0.16)
        sc = kit.scrape(sd, rng, rng.uniform(1100, 1500), density=700)
        sc = synth.lowpass(sc, 2500) * np.sin(np.linspace(0, np.pi, int(sd * SR))) ** 1.5
        synth.place(out, kit.norm(sc) * 0.38, 0.09)
    out = synth.highpass(out, 60)
    out = kit.room(out, rng, 0.08)
    return out * np.clip((d - t) / 0.04, 0, 1) ** 2
