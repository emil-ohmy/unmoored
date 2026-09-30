"""The Provisions, as found lying about (and the Pitch Pot as thrown): "pitch_pot" (a squat clay pot, a rag stuffed in
its neck, pitch dribbled down its side), "ember_tar" (a little tin of tar, its lid off, embers glowing in it),
"way_back" (a coil of knotted cord, pale, a bone toggle at one end) and "warm_broth" (a stoppered flask in a woven
sleeve, a warm glow in its glass). Sized up a little to read from the camera. Origin at the base."""
import math

M, part = kit.M, kit.part  # `kit` is injected by build_model.py

VARIANTS = ["pitch_pot", "ember_tar", "way_back", "warm_broth"]


def build(variant):
    if variant == "pitch_pot":
        clay, pitch, rag = kit.mat("PotClay", "#8a5a3c"), kit.mat("Pitch", "#1c1714"), kit.mat("PotRag", "#b8a98a")
        parts = [
            part("Pot", kit.sphere(10, 7, 0.14), clay, M((0, 0, 0.13), scale=(1, 1, 0.9)), smooth=True),
            part("Neck", kit.cylinder(8, 0.055, 0.07), clay, M((0, 0, 0.27))),
            part("Rim", kit.cylinder(8, 0.07, 0.02), clay, M((0, 0, 0.31))),
            part("Rag", kit.cone(6, 0.045, 0.015, 0.12), rag, M((0.01, 0, 0.37), rot=(0, 14, 0)), jitter=0.006),
            part("Dribble", kit.cube(), pitch, M((0.1, -0.08, 0.17), rot=(0, 20, -40), scale=(0.035, 0.012, 0.12))),
            part("Band", kit.cylinder(10, 0.142, 0.03), pitch, M((0, 0, 0.14))),
        ]
    elif variant == "ember_tar":
        tin, tar, ember = kit.mat("TarTin_Metal", "#6f737d"), kit.mat("Tar", "#1c1714"), kit.mat("TarEmber_Glow", "#ff7a2a")
        parts = [
            part("Tin", kit.cylinder(10, 0.12, 0.13), tin, M((0, 0, 0.065))),
            part("Tar", kit.cylinder(10, 0.105, 0.01), tar, M((0, 0, 0.128))),
            part("Lid", kit.cylinder(10, 0.125, 0.015), tin, M((0.15, 0.05, 0.02), rot=(0, 18, 0))),
        ]
        for k, (x, y) in enumerate(((0.03, 0.02), (-0.04, 0.01), (0.0, -0.05), (0.05, -0.03))):
            parts.append(part("Ember", kit.ico(1, 0.018 + 0.004 * k), ember, M((x, y, 0.135))))
    elif variant == "way_back":
        cord, bone = kit.mat("Cord", "#cfc2a4"), kit.mat("Toggle", "#e8dcc0")
        pts = [(0.12 * math.cos(a) * (1 - 0.06 * a / 6.28), 0.12 * math.sin(a) * (1 - 0.06 * a / 6.28), 0.02 + 0.012 * a / 6.28)
               for a in (k * 0.35 for k in range(38))]
        parts = [part("Coil", kit.tube(pts, [0.017] * len(pts), seg=5), cord)]
        for k in range(0, len(pts), 6):
            parts.append(part("Knot", kit.ico(1, 0.028), cord, M(pts[k])))
        parts.append(part("Toggle", kit.cylinder(6, 0.018, 0.1), bone, M((0.16, -0.02, 0.03), rot=(0, 90, 30))))
    else:
        glass, sleeve, cork = kit.mat("BrothGlass_Glow", "#ffb86a"), kit.mat("Sleeve", "#7a6248"), kit.mat("PieceCork", "#8a6a4a")
        parts = [
            part("Flask", kit.sphere(10, 7, 0.1), glass, M((0, 0, 0.12), scale=(1, 1, 1.15)), smooth=True),
            part("Sleeve", kit.cylinder(10, 0.104, 0.12), sleeve, M((0, 0, 0.1))),
            part("Neck", kit.cylinder(8, 0.035, 0.08), sleeve, M((0, 0, 0.25))),
            part("Cork", kit.cylinder(8, 0.04, 0.05), cork, M((0, 0, 0.31))),
        ]
    return [kit.join(parts, f"Provision_{variant}")]
