"""The anchor thrown (0.7 s): her grunt as it leaves her hands, a heavy whoosh, the chain paying out behind it
fast then slowing."""


def render(v, rng):
    import importlib.util, os
    spec = importlib.util.spec_from_file_location("kit", os.path.join(os.path.dirname(__file__), "anchoress_drop_windup.py"))
    kit = importlib.util.module_from_spec(spec)
    kit.synth, kit.np = synth, np
    spec.loader.exec_module(kit)
    duration = 0.9
    out = np.zeros(int(duration * synth.SR))
    grunt = kit.groan(0.25, rng, 110, 80, "ah") * synth.envelope(0.25, 0.02, 0, curve=2)
    whoosh = synth.bandpass(synth.white(0.6, rng), synth.glide(900, 300, 0.6), 1.1) * synth.envelope(0.6, 0.05, 0, curve=2.5)
    chain = kit.links(0.8, rng, 120, 25, lambda u: 1.0 - 0.7 * u)
    for part, gain, at in ((grunt, 0.5, 0.0), (whoosh, 0.6, 0.05), (chain, 0.45, 0.08)):
        synth.place(out, kit.norm(part) * gain, at)
    return out
