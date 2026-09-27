"""Village street lantern: iron post with a hanging glass lantern (lit; the game adds a point light)."""
M, part = kit.M, kit.part  # `kit` is injected by build_model.py


def build():
    metal, glow = kit.mat("Metal", "#2c2b31"), kit.mat("Lantern_Glow", "#ff9a3c")
    parts = [
        part("Foot", kit.cylinder(6, 0.16, 0.12), metal, M((0, 0, 0.06))),
        part("Pole", kit.cylinder(6, 0.05, 2.2), metal, M((0, 0, 1.15))),
        part("Arm", kit.cube(), metal, M((0.2, 0, 2.2), scale=(0.42, 0.04, 0.04))),
        part("Top", kit.cone(4, 0.14, 0.02, 0.12), metal, M((0.38, 0, 2.08), rot=(0, 0, 45))),
        part("Glass", kit.cube(), glow, M((0.38, 0, 1.93), scale=(0.15, 0.15, 0.2))),
        part("Base", kit.cube(), metal, M((0.38, 0, 1.81), scale=(0.18, 0.18, 0.03))),
    ]
    return [kit.join(parts, "StreetLantern")]
