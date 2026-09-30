"""Hesper's son's boat-lantern, the keepsake: a small ship's lantern of blackened iron, a ring to hang it by, and
glass he painted himself, crooked (a warm glow: it's still burning). On the islet north of the yard, where she saw it
from the landing; in her lap once it's brought to her. Origin at its base."""
M, part = kit.M, kit.part  # `kit` is injected by build_model.py


def build():
    iron = kit.mat("LanternIron", "#2e2c33")
    glass = kit.mat("BoatLantern_Glow", "#ffae5c")
    paint = kit.mat("GlassPaint", "#6b2d22")
    parts = [
        part("Base", kit.cylinder(8, 0.1, 0.04), iron, M((0, 0, 0.02))),
        part("Glass", kit.cylinder(8, 0.075, 0.2), glass, M((0, 0, 0.14))),
        part("Cap", kit.cone(8, 0.1, 0.03, 0.08), iron, M((0, 0, 0.28))),
        part("Chimney", kit.cylinder(6, 0.025, 0.05), iron, M((0, 0, 0.34))),
        part("Ring", kit.tube([(-0.05, 0, 0.36), (0, 0, 0.43), (0.05, 0, 0.36)], [0.01, 0.01, 0.01], seg=4), iron),
    ]
    for i in range(4):  # the cage around the glass
        import math
        a = math.pi / 4 + i * math.pi / 2
        parts.append(part("Bar", kit.cube(), iron, M((0.085 * math.cos(a), 0.085 * math.sin(a), 0.14), scale=(0.012, 0.012, 0.2))))
    # His painted glass: a crooked stripe and a little boat, not quite straight.
    parts.append(part("Stripe", kit.cube(), paint, M((0, -0.077, 0.12), rot=(0, 9, 0), scale=(0.1, 0.004, 0.018))))
    parts.append(part("Boat", kit.cube(), paint, M((0.01, -0.077, 0.17), rot=(0, -6, 0), scale=(0.05, 0.004, 0.014))))
    return [kit.join(parts, "BoatLantern")]
