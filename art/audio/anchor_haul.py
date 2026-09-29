"""The anchor hauled home (0.9 s, gathering speed): iron grinding and juddering over stone, faster and louder, the
chain rattling in over her hands."""


def render(v, rng):
    import importlib.util, os
    spec = importlib.util.spec_from_file_location("kit", os.path.join(os.path.dirname(__file__), "anchoress_drop_windup.py"))
    kit = importlib.util.module_from_spec(spec)
    kit.synth, kit.np = synth, np
    spec.loader.exec_module(kit)
    duration = 1.1
    t = synth.seconds(duration)
    rise = np.clip(t / 0.9, 0, 1) ** 1.5 * np.clip((duration - t) / 0.2, 0, 1)
    grind = kit.scrape(duration, rng, 650)
    chain = kit.links(duration, rng, 20, 80, lambda u: 0.5 + 0.5 * u)
    return (kit.norm(grind) * 0.7 + kit.norm(chain) * 0.4) * (0.25 + 0.75 * rise)
