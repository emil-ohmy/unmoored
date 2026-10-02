"""The Provisions, as found lying about (and the Pitch Pot as thrown): "pitch_pot" (a squat clay pot, a rag stuffed in
its neck, pitch dribbled down its side), "ember_tar" (a little tin of tar, its lid off, embers glowing in it),
"way_back" (a coil of knotted cord, pale, a bone toggle at one end), "warm_broth" (a stoppered flask in a woven
sleeve, a warm glow in its glass) and "bitter_root" (a gnarled forked root, twine round its neck, earth still on it) and "sodden_sack" (old sacking
soaked through, folded over on itself, its neck tied, a little puddle under it). Sized up a little to read from the camera. Origin at the base."""
import math

M, part = kit.M, kit.part  # `kit` is injected by build_model.py

VARIANTS = ["pitch_pot", "ember_tar", "way_back", "warm_broth", "bitter_root", "sodden_sack"]


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
    elif variant == "bitter_root":
        root, twine, earth = kit.mat("Root", "#8a7148"), kit.mat("Twine", "#a38b62"), kit.mat("PieceAsh", "#3d3935")
        rng = kit.rng
        stem = [(0.0, 0.0, 0.03), (0.02, 0.01, 0.12), (-0.01, 0.0, 0.22), (0.015, -0.01, 0.32)]
        parts = [part("Root", kit.tube(stem, [0.045, 0.05, 0.04, 0.025], seg=6), root, jitter=0.004)]
        for k, (dx, dy) in enumerate(((0.1, 0.03), (-0.09, 0.05), (0.02, -0.1), (-0.05, -0.08))):
            parts.append(part("Fork", kit.tube([(0.0, 0.0, 0.05), (dx * 0.6, dy * 0.6, 0.02), (dx, dy, 0.0)], [0.028, 0.02, 0.01], seg=5), root))
        for k in range(3):
            parts.append(part("Twine", kit.cylinder(8, 0.047, 0.012), twine, M((0.0, 0.0, 0.24 + k * 0.016))))
        parts.append(part("Earth", kit.cylinder(7, 0.07, 0.015), earth, M((0.0, 0.0, 0.007))))
    elif variant == "sodden_sack":
        sack, dark, twine, wet = kit.mat("WetSack", "#5c4b37"), kit.mat("WetSackDark", "#45382a"), kit.mat("Twine", "#a38b62"), kit.mat("Puddle", "#2a3640")
        parts = [
            part("Puddle", kit.cylinder(10, 0.2, 0.006), wet, M((0.02, 0.01, 0.003), scale=(1, 0.8, 1))),
            part("Sack", kit.sphere(10, 7, 0.16), sack, M((0, 0, 0.1), scale=(1.1, 0.85, 0.6)), smooth=True, jitter=0.012),
            part("Fold", kit.sphere(8, 6, 0.12), dark, M((-0.04, 0.02, 0.17), scale=(0.9, 0.8, 0.45), rot=(0, -12, 20)), smooth=True, jitter=0.01),
            part("Neck", kit.cone(7, 0.05, 0.03, 0.09), sack, M((0.15, 0.0, 0.12), rot=(0, 70, 0)), jitter=0.006),
            part("Tie", kit.cylinder(8, 0.045, 0.02), twine, M((0.17, 0.0, 0.125), rot=(0, 70, 0))),
            part("Ear", kit.cone(6, 0.03, 0.0, 0.07), sack, M((0.22, 0.0, 0.14), rot=(0, 75, 0))),
        ]
        for k, (x, y) in enumerate(((0.12, -0.08), (-0.1, 0.09), (0.04, 0.12))):
            parts.append(part("Drip", kit.sphere(5, 4, 0.012), wet, M((x, y, 0.012))))
    else:
        glass, sleeve, cork = kit.mat("BrothGlass_Glow", "#ffb86a"), kit.mat("Sleeve", "#7a6248"), kit.mat("PieceCork", "#8a6a4a")
        parts = [
            part("Flask", kit.sphere(10, 7, 0.1), glass, M((0, 0, 0.12), scale=(1, 1, 1.15)), smooth=True),
            part("Sleeve", kit.cylinder(10, 0.104, 0.12), sleeve, M((0, 0, 0.1))),
            part("Neck", kit.cylinder(8, 0.035, 0.08), sleeve, M((0, 0, 0.25))),
            part("Cork", kit.cylinder(8, 0.04, 0.05), cork, M((0, 0, 0.31))),
        ]
    return [kit.join(parts, f"Provision_{variant}")]
