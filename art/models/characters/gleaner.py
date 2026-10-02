"""A Gleaner (the Orchard): a farmhand who went on gleaning after the harvest failed, walking the rows at night with a
lantern on a long pole, picking the field over for grain that was never there. Thin and stooped in a long linen smock,
hooded, a gleaning sack slung on its left shoulder, its eyes the Soured's green; in its right hand the pole, taller
than it is, its lantern (fire, not the Soured's green) hung from a crook at the tip. And, like every Soured, a small
green lantern at its belt.

Clips: Idle (stooped, the pole swaying), Walk (a shuffle, the lantern bobbing), Hop (0.4 s: a light skip backwards,
the smock flaring), Windup (0.9 s: the pole drawn back over its head, the lantern behind it, flaring: the tell),
Fling (0.35 s: the pole swept forward and down, the lantern thrown out at you), Recover (0.8 s), Stagger, Death (it
folds down, the pole falling across it).
"""
import math

M, part = kit.M, kit.part  # `kit` is injected by build_model.py

SHOULDER = 1.36
BONES = {
    "Root": ((0, 0, 0), (0, 0, 0.35), None),
    "Spine": ((0, 0, 0.35), (0, -0.1, SHOULDER), "Root"),
    "Head": ((0, -0.1, SHOULDER), (0, -0.22, 1.62), "Spine"),
    "ArmL": ((-0.2, -0.06, SHOULDER - 0.04), (-0.24, -0.32, 0.98), "Spine"),
    "ArmR": ((0.2, -0.06, SHOULDER - 0.04), (0.27, -0.3, 0.98), "Spine"),
    "Pole": ((0.28, -0.3, 0.96), (0.28, -0.48, 2.45), "ArmR"),
}

REST = {}
IDLE = {"Spine": (4, 0, 0), "Pole": (6, 0, 0)}
SHUFFLE_A = {"Spine": (8, 0, 4), "Root": ((0, 0, 0), (0, 0, 0.03)), "Pole": (10, 0, 4)}
SHUFFLE_B = {"Spine": (8, 0, -4), "Root": ((0, 0, 0), (0, 0, 0.0)), "Pole": (2, 0, -4)}
HOPPED = {"Spine": (-14, 0, 0), "Head": (-10, 0, 0), "Root": ((0, 0, 0), (0, 0.3, 0.25)), "ArmL": (0, 30, 0), "Pole": (-15, 0, 0)}
DRAWN = {"Spine": (-16, 0, 12), "Head": (-12, 0, 0), "ArmR": (-60, 0, -10), "ArmL": (-30, 25, 0), "Pole": (-28, 0, 0)}
FLUNG = {"Spine": (18, 0, -8), "Head": (10, 0, 0), "ArmR": (25, 0, 5), "ArmL": (10, 0, 0), "Pole": (38, 0, 0), "Root": ((0, 0, 0), (0, -0.12, 0))}

ACTIONS = {
    "Idle": ("BEZIER", [(1, REST), (30, IDLE), (60, REST)]),
    "Walk": ("BEZIER", [(1, SHUFFLE_A), (9, SHUFFLE_B), (17, SHUFFLE_A)]),
    "Hop": ("BEZIER", [(1, REST), (4, dict(HOPPED, Root=((0, 0, 0), (0, 0.15, 0.2)))), (8, HOPPED), (10, dict(REST, Root=((0, 0, 0), (0, 0.0, -0.05))))]),
    "Windup": ("BEZIER", [(1, REST), (14, DRAWN), (22, dict(DRAWN, Spine=(-24, 0, 14)))]),
    "Fling": ("BEZIER", [(1, DRAWN), (4, FLUNG), (9, dict(FLUNG, Spine=(28, 0, -10)))]),
    "Recover": ("BEZIER", [(1, FLUNG), (20, REST)]),
    "Stagger": ("BEZIER", [(1, REST), (5, {"Spine": (-20, 0, 8), "Head": (-15, 0, 0), "Pole": (-25, 0, 10), "Root": ((0, 0, 0), (0, 0.12, 0))}), (16, REST)]),
    "Death": ("BEZIER", [
        (1, REST),
        (6, {"Spine": (-16, 0, 6), "Head": (-20, 0, 0), "Pole": (-30, 0, 20)}),
        (18, {"Spine": (45, 0, -8), "Head": (35, 0, 0), "Pole": (85, 0, 40), "ArmR": (40, 0, 0), "Root": ((0, 0, 0), (0, -0.05, -0.3))}),
        (26, {"Spine": (50, 0, -10), "Head": (40, 0, 0), "Pole": (88, 0, 42), "ArmR": (45, 0, 0), "Root": ((0, 0, 0), (0, -0.06, -0.34))}),
    ]),
}


def build():
    smock, dark = kit.mat("GleanerSmock", "#a59a80"), kit.mat("GleanerHood", "#5e4c38")
    eye, claw = kit.mat("SouredEye_Glow", "#c8f07a"), kit.mat("Claw", "#d6cdb8")
    sack, twine = kit.mat("GleanerSack", "#8a7552"), kit.mat("Twine", "#a38b62")
    pole, iron = kit.mat("GleanerPole", "#5b4430"), kit.mat("GleanerIron", "#2f2d33")
    flame = kit.mat("GleanerLantern_Glow", "#ffb05a")
    face = kit.mat("SouredOchreDark", "#5e4a2c")

    def smock_shape(bm):
        kit.cone(9, 0.36, 0.2, 1.05)(bm)
        rim = sorted((v for v in bm.verts if v.co.z < -0.5 and v.co.length > 0.1), key=lambda v: math.atan2(v.co.y, v.co.x))
        for i, v in enumerate(rim):
            v.co.z += 0.07 if i % 2 else 0.0  # ragged hem

    parts = [
        part("Smock", smock_shape, smock, M((0, -0.03, 0.72), rot=(10, 0, 0)), smooth=True, group="Spine"),
        part("Shoulders", kit.sphere(9, 6), smock, M((0, -0.06, SHOULDER - 0.08), scale=(0.3, 0.24, 0.16)), smooth=True, group="Spine"),
        part("Head", kit.sphere(10, 7, 0.17), face, M((0, -0.2, 1.5), scale=(1, 1.05, 0.95)), smooth=True, group="Head"),
        part("Hood", kit.cone(8, 0.23, 0.05, 0.42), dark, M((0, -0.13, 1.6), rot=(-38, 0, 0)), smooth=True, group="Head"),
        part("Cape", kit.cone(9, 0.32, 0.2, 0.22), dark, M((0, -0.05, SHOULDER - 0.03)), smooth=True, group="Spine"),
    ]
    for s in (-1, 1):
        parts.append(part("Eye", kit.sphere(8, 6, 0.033), eye, M((s * 0.065, -0.36, 1.5)), smooth=True, group="Head"))
    # Arms: the left hangs (gleaning), the right holds the pole.
    for s, bone, hand in ((-1, "ArmL", (-0.24, -0.32, 0.98)), (1, "ArmR", (0.27, -0.3, 0.98))):
        shoulder = (s * 0.2, -0.06, SHOULDER - 0.04)
        parts.append(part("Sleeve", kit.cone(6, 0.085, 0.05, 0.45), smock, kit.along(shoulder, hand), smooth=True, group=bone))
        parts.append(part("Claw", kit.cone(5, 0.03, 0.0, 0.09), claw, kit.along(hand, (hand[0], hand[1] - 0.08, hand[2] - 0.04)), group=bone))
    # The gleaning sack on its left shoulder, hanging down its back, tied at the neck.
    parts += [
        part("Sack", kit.sphere(9, 7), sack, M((-0.16, 0.2, 1.0), scale=(0.2, 0.17, 0.3)), smooth=True, jitter=0.01, group="Spine"),
        part("SackTie", kit.cylinder(7, 0.06, 0.03), twine, M((-0.18, 0.12, 1.3)), group="Spine"),
        part("Strap", kit.cube(), twine, M((-0.05, -0.02, 1.12), rot=(0, 40, 0), scale=(0.03, 0.42, 0.03)), group="Spine"),
    ]
    # The pole: from below its hand to well over its head, a crook at the tip, the lantern hung from it.
    butt, tip = (0.28, -0.22, 0.45), (0.28, -0.5, 2.5)
    parts.append(part("Pole", kit.cylinder(6, 0.024, math.dist(butt, tip)), pole, kit.along(butt, tip), group="Pole"))
    crook = [(0.28, -0.5, 2.5), (0.28, -0.6, 2.56), (0.28, -0.68, 2.5)]
    parts.append(part("Crook", kit.tube(crook, [0.02, 0.018, 0.015], seg=5), iron, group="Pole"))
    lx, ly, lz = 0.28, -0.68, 2.3
    parts += [
        part("LanternCord", kit.cylinder(4, 0.006, 0.16), iron, M((lx, ly, 2.43)), group="Pole"),
        part("LanternTop", kit.cone(4, 0.075, 0.02, 0.06), iron, M((lx, ly, lz + 0.11), rot=(0, 0, 45)), group="Pole"),
        part("LanternGlass", kit.cube(), flame, M((lx, ly, lz), scale=(0.1, 0.1, 0.15)), group="Pole"),
        part("LanternBase", kit.cube(), iron, M((lx, ly, lz - 0.09), scale=(0.12, 0.12, 0.02)), group="Pole"),
    ]
    # A lantern hung from its belt, like every Soured's: green light in the dark = a Soured.
    lantern_glass = kit.mat("SouredLantern_Glow", "#b6f06a")
    parts += [
        part("BeltLanternTop", kit.cone(4, 0.06, 0.01, 0.05), iron, M((0.3, 0.02, 0.66), rot=(0, 0, 45)), group="Spine"),
        part("BeltLanternGlass", kit.cube(), lantern_glass, M((0.3, 0.02, 0.59), scale=(0.08, 0.08, 0.1)), group="Spine"),
        part("BeltLanternBase", kit.cube(), iron, M((0.3, 0.02, 0.535), scale=(0.1, 0.1, 0.015)), group="Spine"),
    ]
    mesh = kit.join(parts, "Gleaner")
    armature = kit.rig("GleanerRig", BONES)
    kit.skin(mesh, armature)
    kit.animate(armature, ACTIONS)
    return [armature, mesh]
