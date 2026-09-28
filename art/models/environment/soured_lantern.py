"""A Soured's belt lantern, on its own: dropped where the Soured fell (the game adds the green point light).
Same parts and sizes as the lantern on the Laden's belt (art/models/characters/laden.py), origin at its base."""
M, part = kit.M, kit.part  # `kit` is injected by build_model.py


def build():
    metal, glass = kit.mat("Metal", "#2c2b31"), kit.mat("SouredLantern_Glow", "#b6f06a")
    parts = [
        part("Base", kit.cube(), metal, M((0, 0, 0.01), scale=(0.12, 0.12, 0.02))),
        part("Glass", kit.cube(), glass, M((0, 0, 0.085), scale=(0.1, 0.1, 0.13))),
        part("Top", kit.cone(4, 0.08, 0.01, 0.07), metal, M((0, 0, 0.185), rot=(0, 0, 45))),
        part("Ring", kit.cube(), metal, M((0, 0, 0.245), scale=(0.015, 0.06, 0.03))),
    ]
    return [kit.join(parts, "SouredLantern")]
