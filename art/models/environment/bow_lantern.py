"""The bow lantern hung on the boat's bow pole while rowing: the same cold, pale lantern the Apprentice carries at the
hip (hidden there meanwhile), a little larger so it reads from the camera. Origin at its hanging ring."""
M, part = kit.M, kit.part  # `kit` is injected by build_model.py


def build():
    metal, soul = kit.mat("LanternMetal", "#2c2b31"), kit.mat("SoulLantern_Glow", "#cfe6ff")
    k = 1.4
    parts = [
        part("Cord", kit.cube(), metal, M((0, 0, -0.06 * k), scale=(0.015 * k, 0.015 * k, 0.12 * k))),
        part("Top", kit.cone(4, 0.07 * k, 0.01 * k, 0.06 * k), metal, M((0, 0, -0.16 * k), rot=(0, 0, 45))),
        part("Glass", kit.cube(), soul, M((0, 0, -0.24 * k), scale=(0.08 * k, 0.08 * k, 0.11 * k))),
        part("Base", kit.cube(), metal, M((0, 0, -0.305 * k), scale=(0.1 * k, 0.1 * k, 0.02 * k))),
    ]
    return [kit.join(parts, "BowLantern")]
