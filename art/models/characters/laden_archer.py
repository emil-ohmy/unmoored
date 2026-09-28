"""The Laden archer: a Laden variant that let most of its heap go and kept a bow. A small bundle of belongings and
a quiver ride on its back; it holds a bow across its body, drawing it level at whatever it's watching. Faded ochre
where the Laden is violet, so the two read apart at a glance. Same sickly green eyes and belt lantern.

Clips: Idle and Walk (bow held low), Draw (0.9 s telegraph: the bow comes up level and is drawn, leaning back),
Release (0.25 s: the snap of the shot), ShotRecover (0.6 s), BashWindup / Bash / BashRecover (the jab it falls back
on up close: the bow swung across), Death, Stagger.
"""
import math

M, part = kit.M, kit.part  # `kit` is injected by build_model.py

BONES = {
    "Root": ((0, 0, 0), (0, 0, 0.35), None),
    "Spine": ((0, 0, 0.35), (0, -0.12, 1.1), "Root"),
    "Head": ((0, -0.12, 1.1), (0, -0.26, 1.4), "Spine"),
    "Bow": ((0, -0.1, 1.06), (0, -0.55, 1.06), "Spine"),  # the arms and the bow, pivoting at the shoulders
}

# The mesh is built in the aiming pose (arms level, bow across the chest). +X on the Bow bone lowers the arms.
LOW = {"Bow": (58, 0, 8), "Spine": (6, 0, 0), "Head": (-4, 0, 0)}
DRAWN = {"Bow": (0, 0, 0), "Spine": (-8, 0, 0), "Head": (8, 0, 0), "Root": ((0, 0, 0), (0, 0.04, 0))}
BASH_BACK = {"Bow": ((24, 0, 45), (0.05, 0.16, 0)), "Spine": (0, 0, 32), "Head": (0, 0, 12), "Root": ((0, 0, 0), (0, 0.06, 0))}
BASH_THROUGH = {"Bow": ((10, 0, -35), (0, -0.28, 0)), "Spine": (12, 0, -28), "Head": (6, 0, -10), "Root": ((0, 0, 0), (0, -0.12, 0))}


def with_(pose, **changes):
    merged = dict(pose)
    merged.update(changes)
    return merged


ACTIONS = {
    "Idle": ("BEZIER", [
        (1, LOW),
        (24, with_(LOW, Spine=(9, 0, 0), Bow=(62, 0, 8), Root=((0, 0, 0), (0, 0, -0.02)))),
        (48, LOW),
    ]),
    "Walk": ("BEZIER", [
        (1, with_(LOW, Spine=(6, 6, 0))),
        (9, with_(LOW, Root=((0, 0, 0), (0, 0, 0.04)))),
        (17, with_(LOW, Spine=(6, -6, 0))),
        (25, with_(LOW, Root=((0, 0, 0), (0, 0, 0.04)))),
        (33, with_(LOW, Spine=(6, 6, 0))),
    ]),
    "Draw": ("BEZIER", [  # the bow comes up level, then it leans back into the draw
        (1, LOW),
        (10, with_(DRAWN, Bow=(18, 0, 0), Spine=(0, 0, 0))),
        (20, DRAWN),
        (22, with_(DRAWN, Spine=(-10, 0, 0))),
    ]),
    "Release": ("LINEAR", [
        (1, with_(DRAWN, Spine=(-10, 0, 0))),
        (3, with_(DRAWN, Bow=((-6, 0, 0), (0, 0.07, 0)), Spine=(-14, 0, 0), Head=(0, 0, 0))),
        (6, with_(DRAWN, Spine=(-6, 0, 0))),
    ]),
    "ShotRecover": ("BEZIER", [(1, with_(DRAWN, Spine=(-6, 0, 0))), (15, LOW)]),
    "BashWindup": ("BEZIER", [(1, LOW), (11, BASH_BACK)]),
    "Bash": ("LINEAR", [(1, BASH_BACK), (5, BASH_THROUGH)]),
    "BashRecover": ("BEZIER", [(1, BASH_THROUGH), (15, LOW)]),
    "Death": ("BEZIER", [  # a jolt back, the bow falls from its hands, and it slumps forward
        (1, LOW),
        (6, {"Spine": (-16, 0, 6), "Head": (-20, 0, 0), "Bow": ((40, 0, 10), (0, 0.1, 0.05)), "Root": ((0, 0, 0), (0, 0.1, 0))}),
        (15, {"Spine": (38, 0, -8), "Head": (35, 0, 0), "Bow": ((85, 0, 30), (0.1, -0.2, -0.55)), "Root": ((0, 0, 0), (0, -0.05, -0.22))}),
        (25, {"Spine": (42, 0, -10), "Head": (40, 0, 0), "Bow": ((88, 0, 32), (0.1, -0.22, -0.6)), "Root": ((0, 0, 0), (0, -0.06, -0.26))}),
    ]),
    "Stagger": ("BEZIER", [
        (1, LOW),
        (5, {"Spine": (-18, 0, 6), "Head": (-12, 0, 0), "Bow": (30, 0, 20), "Root": ((0, 0, 0), (0, 0.12, 0))}),
        (12, {"Spine": (-10, 0, -5), "Bow": (45, 0, 10)}),
        (19, LOW),
    ]),
}


def build():
    robe, dark = kit.mat("SouredOchre", "#8a6d3e"), kit.mat("SouredOchreDark", "#5e4a2c")
    eye, claw = kit.mat("SouredEye_Glow", "#c8f07a"), kit.mat("Claw", "#d6cdb8")
    bow_wood, string = kit.mat("BowWood", "#4a3424"), kit.mat("BowString", "#b9ae96")
    cloth, metal = kit.mat("HeapCloth", "#8c7a5c"), kit.mat("HeapMetal", "#34313a")
    quiver = kit.mat("Quiver", "#5a3e2a")

    def robe_shape(bm):
        kit.cone(9, 0.5, 0.24, 1.12)(bm)
        rim = sorted((v for v in bm.verts if v.co.z < -0.54 and v.co.length > 0.1), key=lambda v: math.atan2(v.co.y, v.co.x))
        for i, v in enumerate(rim):
            v.co.z += 0.1 if i % 2 else 0.0  # tattered hem

    parts = [
        part("Robe", robe_shape, robe, M((0, 0, 0.6), rot=(8, 0, 0)), smooth=True, group="Spine"),
        part("Hump", kit.sphere(10, 7), robe, M((0, 0.05, 1.02), scale=(0.38, 0.32, 0.28)), smooth=True, group="Spine"),
        part("Head", kit.sphere(10, 7, 0.21), dark, M((0, -0.22, 1.28), scale=(1, 1.05, 0.95)), smooth=True, group="Head"),
        part("Hood", kit.cone(8, 0.27, 0.05, 0.46), robe, M((0, -0.15, 1.41), rot=(-35, 0, 0)), smooth=True, group="Head"),
    ]
    for s in (-1, 1):
        parts.append(part("Eye", kit.sphere(8, 6, 0.04), eye, M((s * 0.08, -0.42, 1.28)), smooth=True, group="Head"))
        shoulder, hand = (s * 0.3, -0.1, 1.06), (s * 0.13, -0.56, 1.03)
        parts.append(part("Arm", kit.cone(6, 0.1, 0.055, 0.5), robe, kit.along(shoulder, hand), smooth=True, group="Bow"))
        parts.append(part("Claw", kit.cone(5, 0.035, 0.0, 0.1), claw, kit.along(hand, (s * 0.06, -0.64, 1.03)), group="Bow"))

    # The bow, held level across the body: limbs curving back towards it, the string between the tips.
    limb = [(-0.52, -0.44, 1.03), (-0.32, -0.57, 1.03), (0.0, -0.63, 1.03), (0.32, -0.57, 1.03), (0.52, -0.44, 1.03)]
    parts += [
        part("BowLimbs", kit.tube(limb, [0.018, 0.026, 0.032, 0.026, 0.018], seg=5), bow_wood, group="Bow"),
        part("BowGrip", kit.cylinder(6, 0.04, 0.12), cloth, M((0, -0.63, 1.03), rot=(0, 90, 0)), group="Bow"),
        part("BowString", kit.cube(), string, M((0, -0.44, 1.03), scale=(1.04, 0.01, 0.01)), group="Bow"),
    ]

    # On its back: a small bundle of belongings (all it kept of its heap) and a quiver of green-tipped arrows.
    parts += [
        part("Bundle", kit.sphere(8, 6), cloth, M((-0.06, 0.34, 1.04), scale=(0.26, 0.2, 0.24)), smooth=True, group="Spine"),
        part("Kettle", kit.cylinder(8, 0.1, 0.14), metal, M((-0.2, 0.36, 1.26), rot=(20, 0, 0)), group="Spine"),
        part("Quiver", kit.cylinder(7, 0.08, 0.6), quiver, M((0.2, 0.3, 1.18), rot=(-18, -16, 0)), group="Spine"),
    ]
    for i, (dx, dy) in enumerate(((0.0, 0.0), (0.04, 0.03), (-0.03, 0.04), (0.02, -0.03))):
        base = (0.2 + dx + 0.1, 0.3 + dy + 0.2, 1.5)
        parts.append(part("Arrow", kit.cylinder(4, 0.008, 0.2), bow_wood, M(base, rot=(-18, -16, 0)), group="Spine"))
        parts.append(part("ArrowTip", kit.cone(4, 0.022, 0.0, 0.05), eye, M((base[0] + 0.03, base[1] + 0.03, base[2] + 0.12), rot=(-18, -16, 0)), group="Spine"))

    # A lantern hung from its belt on the right, like every Soured's: green light in the dark = a Soured.
    lantern_glass = kit.mat("SouredLantern_Glow", "#b6f06a")
    parts += [
        part("BeltLanternCord", kit.cube(), metal, M((0.46, 0.0, 0.78), rot=(0, -15, 0), scale=(0.015, 0.015, 0.14)), group="Spine"),
        part("BeltLanternTop", kit.cone(4, 0.08, 0.01, 0.07), metal, M((0.48, 0.0, 0.68), rot=(0, 0, 45)), group="Spine"),
        part("BeltLanternGlass", kit.cube(), lantern_glass, M((0.48, 0.0, 0.59), scale=(0.1, 0.1, 0.13)), group="Spine"),
        part("BeltLanternBase", kit.cube(), metal, M((0.48, 0.0, 0.515), scale=(0.12, 0.12, 0.02)), group="Spine"),
    ]
    for p in parts:
        if not p.vertex_groups:
            p.vertex_groups.new(name="Spine").add(list(range(len(p.data.vertices))), 1.0, "REPLACE")

    mesh = kit.join(parts, "LadenArcher")
    armature = kit.rig("LadenArcherRig", BONES)
    kit.skin(mesh, armature)
    kit.animate(armature, ACTIONS)
    return [armature, mesh]
