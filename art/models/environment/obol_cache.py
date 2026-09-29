"""An Obol cache: a small iron-bound box, lid ajar, spilling a few Obols. Found behind a secret. Origin at its base."""
M, part = kit.M, kit.part  # `kit` is injected by build_model.py


def build():
    wood, iron, silver = kit.mat("CacheWood", "#5a4331"), kit.mat("HookIron", "#3b3a42"), kit.mat("ObolSilver", "#b8b2a2")
    parts = [
        part("Box", kit.cube(), wood, M((0, 0, 0.14), scale=(0.46, 0.32, 0.28))),
        part("Band", kit.cube(), iron, M((0, 0, 0.14), scale=(0.48, 0.34, 0.04))),
        part("Lid", kit.cube(), wood, M((0, 0.1, 0.33), rot=(-28, 0, 0), scale=(0.47, 0.33, 0.06))),
    ]
    rng = kit.rng
    for i in range(7):
        parts.append(part("Obol", kit.cylinder(8, 0.035, 0.01), silver,
                          M((rng.uniform(-0.18, 0.18), rng.uniform(-0.3, -0.18), 0.005 + 0.01 * (i % 2)), rot=(rng.uniform(-10, 10), rng.uniform(-10, 10), 0))))
    return [kit.join(parts, "ObolCache")]
