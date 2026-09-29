"""The thrown anchor lands: a heavy clank on stone, a thud, grit skittering, the chain slapping down after it."""
VARIANTS = 2


def render(v, rng):
    import importlib.util, os
    spec = importlib.util.spec_from_file_location("kit", os.path.join(os.path.dirname(__file__), "anchoress_drop_windup.py"))
    kit = importlib.util.module_from_spec(spec)
    kit.synth, kit.np = synth, np
    spec.loader.exec_module(kit)
    duration = 1.2
    out = np.zeros(int(duration * synth.SR))
    clank = kit.iron(0.9, rng, rng.uniform(130, 150), tau=0.25)
    thud = synth.lowpass(synth.white(0.2, rng), 300) * synth.decay(0.2, 0.05)
    boom = synth.sine(synth.glide(80, 45, 0.5, 0.6), 0.5) * synth.decay(0.5, 0.15)
    chain = kit.links(0.4, rng, 60, 15, lambda u: 1.0 - u)
    for part, gain, at in ((clank, 0.75, 0.0), (thud, 0.8, 0.0), (boom, 0.7, 0.0), (chain, 0.35, 0.12)):
        synth.place(out, kit.norm(part) * gain, at)
    return out
