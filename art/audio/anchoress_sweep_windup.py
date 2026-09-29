"""The Chain Sweep's tell (0.85 s): the chain paying out through her hands in a fast ratcheting rattle, and the
anchor starting to swing at her side (a low whir rising and falling). Lighter and faster than the Drop's."""
TELEGRAPH = 0.85


def render(v, rng):
    import importlib.util, os
    spec = importlib.util.spec_from_file_location("kit", os.path.join(os.path.dirname(__file__), "anchoress_drop_windup.py"))
    kit = importlib.util.module_from_spec(spec)
    kit.synth, kit.np = synth, np
    spec.loader.exec_module(kit)
    duration = TELEGRAPH + 0.1
    t = synth.seconds(duration)
    swell = np.clip(t / TELEGRAPH, 0, 1) ** 1.1 * np.clip((duration - t) / 0.1, 0, 1)
    chain = kit.links(duration, rng, 40, 110, lambda u: 0.7 + 0.3 * u, low=2400, high=6000)
    whir = synth.bandpass(synth.white(duration, rng), synth.glide(220, 420, duration), 1.5) * (0.5 + 0.5 * np.sin(2 * np.pi * np.cumsum(synth.glide(1.5, 3.5, duration)) / synth.SR))
    return (kit.norm(chain) * 0.6 + kit.norm(whir) * 0.5) * swell
