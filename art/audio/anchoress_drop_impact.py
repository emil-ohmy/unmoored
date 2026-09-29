"""The Anchor Drop lands: a huge dull clank of iron into stone, a deep boom, the chain crashing down after it, and
the water it throws out rushing away across the floor (the ring). Played lower for her Rise's slam."""


def render(v, rng):
    import importlib.util, os
    spec = importlib.util.spec_from_file_location("kit", os.path.join(os.path.dirname(__file__), "anchoress_drop_windup.py"))
    kit = importlib.util.module_from_spec(spec)
    kit.synth, kit.np = synth, np
    spec.loader.exec_module(kit)
    duration = 2.2
    out = np.zeros(int(duration * synth.SR))
    clank = kit.iron(1.4, rng, rng.uniform(95, 110), tau=0.35)
    boom = synth.sine(synth.glide(70, 34, 1.2, 0.6), 1.2) * synth.decay(1.2, 0.4)
    thud = synth.lowpass(synth.white(0.25, rng), 260) * synth.decay(0.25, 0.06)
    chain = kit.links(0.6, rng, 90, 20, lambda u: 1.0 - u)
    rush = synth.bandpass(synth.white(1.4, rng), synth.glide(1500, 400, 1.4), 1.0) * synth.envelope(1.4, 0.05, 0, curve=2.0)
    for part, gain, at in ((clank, 0.8, 0.0), (boom, 1.0, 0.0), (thud, 0.8, 0.0), (chain, 0.4, 0.04), (rush, 0.35, 0.06)):
        synth.place(out, kit.norm(part) * gain, at)
    hall = synth.reverb_ir(1.8, 1.5, rng, brightness=0.3)[0]
    wet = synth.fft_convolve(out, hall)[:len(out)]
    return out * 0.85 + kit.norm(wet) * 0.25
