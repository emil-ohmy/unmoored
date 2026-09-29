"""The Chain Sweep itself (0.45 s): the anchor whirling round on its chain, a huge low whoosh passing, the chain
thrumming taut."""
VARIANTS = 2


def render(v, rng):
    import importlib.util, os
    spec = importlib.util.spec_from_file_location("kit", os.path.join(os.path.dirname(__file__), "anchoress_drop_windup.py"))
    kit = importlib.util.module_from_spec(spec)
    kit.synth, kit.np = synth, np
    spec.loader.exec_module(kit)
    duration = 0.8
    whoosh = synth.bandpass(synth.white(duration, rng), synth.arc(180, 700, duration, 0.4), 1.2) * synth.envelope(duration, 0.05, 0, curve=2.0, peak_at=0.3)
    whum = synth.sine(synth.arc(55, 85, duration, 0.4), duration) * synth.envelope(duration, 0.05, 0, curve=2.0, peak_at=0.3)
    thrum = kit.links(duration, rng, 70, 30, lambda u: 1.0 - u, low=1500, high=3500)
    return kit.norm(whoosh) * 0.6 + kit.norm(whum) * 0.5 + kit.norm(thrum) * 0.25
