"""Things to furnish a room with (the tower's, and anywhere else): "table" (a trestle table, a candle, a bowl and a
mug), "stool", "shelf" (against a wall: jars, a pot, folded cloth), "sacks" (three, slumped together), "rope" (a coil
and a boathook), "oar_rack" (the Ferryman's spare oars on a wall rack), "cot" (a low bed, a blanket), "lantern" (a
wall bracket with a hanging lantern) and "brazier" (an iron bowl of coals on legs). Glowing parts are *_Glow (the game
adds their light). Game directions (see masonry.py); origin at the floor, against a wall its back is to the north."""
import math

import masonry as m

VARIANTS = ["table", "stool", "shelf", "sacks", "rope", "oar_rack", "cot", "lantern", "brazier"]


def build(variant):
    kit = m.kit
    rng = kit.rng
    wood, dark = kit.mat("CrateWood", "#8a6a48"), kit.mat("CrateEdge", "#5f4731")
    timber, iron = m.mat(m.TIMBER), kit.mat("TowerIron", "#2c2b31")
    cloth, sack = kit.mat("Cloth", "#7a6f62"), kit.mat("Sack", "#9a8a6c")
    clay, glaze = kit.mat("Clay", "#9c6b4a"), kit.mat("Glaze", "#556b6a")
    rope = kit.mat("Rope", "#a58e62")
    box, part, M = m.box, kit.part, kit.M
    p = []
    if variant == "table":
        p.append(box("Top", wood, -0.65, 0.65, -0.38, 0.38, 0.7, 0.76))
        for e in (-0.5, 0.5):
            p.append(box("Trestle", dark, e - 0.04, e + 0.04, -0.3, 0.3, 0.0, 0.7))
            p.append(box("Foot", dark, e - 0.06, e + 0.06, -0.34, 0.34, 0.0, 0.06))
        p.append(box("Stretcher", dark, -0.5, 0.5, -0.03, 0.03, 0.25, 0.31))
        # A candle in a dish, a bowl, a mug.
        p.append(part("Dish", kit.cylinder(8, 0.07, 0.015), clay, M((-0.3, 0.1, 0.77))))
        p.append(part("Candle", kit.cylinder(6, 0.025, 0.12), kit.mat("Wax", "#e8dfc8"), M((-0.3, 0.1, 0.84))))
        p.append(part("Flame", kit.cone(5, 0.018, 0.0, 0.05), kit.mat("Candle_Glow", "#ffcf7a"), M((-0.3, 0.1, 0.925))))
        p.append(part("Bowl", kit.cone(8, 0.06, 0.1, 0.06), glaze, M((0.25, -0.05, 0.79))))
        p.append(part("Mug", kit.cylinder(7, 0.04, 0.09), clay, M((0.42, -0.2, 0.805))))
    elif variant == "stool":
        p.append(part("Seat", kit.cylinder(8, 0.18, 0.05), wood, M((0, 0, 0.45))))
        for k in range(3):
            a = k * 2 * math.pi / 3
            p.append(part("Leg", kit.cylinder(5, 0.025, 0.45), dark, M((0.12 * math.cos(a), 0.12 * math.sin(a), 0.22), rot=(8 * math.sin(a), -8 * math.cos(a), 0))))
    elif variant == "shelf":
        for e in (-0.6, 0.6):
            p.append(box("Upright", dark, e - 0.04, e + 0.04, -0.34, 0.0, 0.0, 1.7))
        for u in (0.05, 0.6, 1.15, 1.65):
            p.append(box("Board", wood, -0.62, 0.62, -0.34, 0.0, u, u + 0.04))
        for k, u in enumerate((0.64, 1.19)):
            e = -0.45
            while e < 0.45:
                h = rng.uniform(0.12, 0.28)
                p.append(part("Jar", kit.cylinder(7, rng.uniform(0.05, 0.08), h), rng.choice([clay, glaze]), M((-e, 0.17, u + h / 2))))
                e += rng.uniform(0.16, 0.26)
        p.append(box("FoldedCloth", cloth, -0.4, 0.3, -0.3, -0.04, 0.09, 0.2))
        p.append(part("Pot", kit.sphere(8, 6, 0.14), clay, M((-0.42, 0.17, 0.2))))
    elif variant == "sacks":
        for (e, n, s) in ((0.0, 0.0, 1.0), (0.45, 0.15, 0.85), (0.2, -0.35, 0.9)):
            p.append(part("Sack", kit.sphere(8, 6), sack, M((-e, -n, 0.26 * s), scale=(0.3 * s, 0.26 * s, 0.3 * s)), smooth=True, jitter=0.02))
            p.append(part("Tie", kit.cone(6, 0.07 * s, 0.02, 0.12), sack, M((-e, -n, 0.56 * s))))
    elif variant == "rope":
        for k in range(4):  # a coil, loop over loop
            ring = [(0.3 * math.cos(a) * (1 - 0.06 * k), 0.3 * math.sin(a) * (1 - 0.06 * k), 0.04 + k * 0.05)
                    for a in [i * math.pi / 8 for i in range(17)]]
            p.append(part("Coil", kit.tube(ring, [0.035] * len(ring), seg=5), rope))
        p.append(part("Boathook", kit.cylinder(6, 0.025, 2.2), timber, M((-0.6, 0.1, 0.05), rot=(0, 88, 20))))
        p.append(part("HookEnd", kit.cone(5, 0.04, 0.0, 0.18), iron, M((-1.65, 0.45, 0.08), rot=(0, 88, 20))))
    elif variant == "oar_rack":
        # Against a wall: two brackets, three spare oars (like the Apprentice's) leaning in them, blade up.
        p.append(box("Rail", timber, -0.8, 0.8, -0.14, -0.04, 1.35, 1.45))
        p.append(box("Rail", timber, -0.8, 0.8, -0.14, -0.04, 0.35, 0.45))
        blade = kit.mat("OarBlade", "#8f6d48")
        for k, e in enumerate((-0.5, 0.0, 0.5)):
            lean = rng.uniform(-4, 4)
            p.append(part("Shaft", kit.cylinder(6, 0.028, 2.0), kit.mat("OarWood", "#b08a5c"), M((-e, 0.2, 1.0), rot=(8, lean, 0))))
            p.append(part("Blade", kit.cube(), blade, M((-e, 0.26, 1.78), rot=(8, lean, 0), scale=(0.17, 0.035, 0.5))))
    elif variant == "cot":
        p.append(box("Frame", dark, -0.45, 0.45, -1.0, 1.0, 0.18, 0.32))
        for e in (-0.4, 0.4):
            for n in (-0.95, 0.95):
                p.append(box("Leg", dark, e - 0.04, e + 0.04, n - 0.04, n + 0.04, 0.0, 0.2))
        p.append(box("Mattress", sack, -0.42, 0.42, -0.97, 0.97, 0.32, 0.42))
        p.append(part("Blanket", kit.cube(), cloth, M((0, 0.25, 0.45), rot=(0, 0, 3), scale=(0.88, 1.3, 0.07))))
        p.append(part("Pillow", kit.sphere(7, 5), kit.mat("Linen", "#c9bfa8"), M((0, -0.72, 0.47), scale=(0.28, 0.14, 0.07)), smooth=True))
    elif variant == "lantern":
        # A wall bracket (its back to the north, against the wall) and the lantern hanging from it.
        p.append(box("Plate", iron, -0.08, 0.08, -0.04, 0.0, 1.8, 2.05))
        p.append(box("Arm", iron, -0.02, 0.02, -0.4, -0.02, 1.98, 2.02))
        p.append(box("Chain", iron, -0.01, 0.01, -0.39, -0.37, 1.72, 1.98))
        p.append(part("Top", kit.cone(6, 0.1, 0.02, 0.08), iron, M((0, 0.38, 1.68))))
        p.append(part("Glass", kit.cylinder(6, 0.075, 0.18), kit.mat("HangingLantern_Glow", "#ffc27a"), M((0, 0.38, 1.55))))
        p.append(part("Base", kit.cylinder(6, 0.09, 0.03), iron, M((0, 0.38, 1.45))))
    else:  # brazier
        for k in range(3):
            a = k * 2 * math.pi / 3
            p.append(part("Leg", kit.tube([(0.28 * math.cos(a), 0.28 * math.sin(a), 0.0), (0.2 * math.cos(a), 0.2 * math.sin(a), 0.55)], [0.025, 0.025], seg=5), iron))
        p.append(part("Bowl", kit.cone(10, 0.18, 0.34, 0.2), iron, M((0, 0, 0.62))))
        for k in range(7):
            p.append(part("Coal", kit.ico(1, rng.uniform(0.05, 0.08)), kit.mat("Coals_Glow", "#a8301a"),
                          M((rng.uniform(-0.17, 0.17), rng.uniform(-0.17, 0.17), 0.72)), jitter=0.01))
    return [kit.join(p, f"TowerProps_{variant}")]
