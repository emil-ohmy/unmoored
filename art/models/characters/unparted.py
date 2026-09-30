"""Hold, the greater half of the Unparted (the Drowned City's miniboss): two souls who never let each other go. Hold
is a Clinger grown vast, grey and hunched, twice the height, with arms that sweep the ground; Fast, the smaller, rides
its back (an ordinary Clinger, placed on the Back bone by the game), and a faded red ribbon ties Hold's wrist.

Clips: Idle, Walk (a lumbering knuckle-walk), SweepWindup (0.9 s: twisting away, the right arm drawn back wide),
Sweep (0.3 s: across the front), SweepRecover (0.9 s), SlamWindup (1.1 s: rearing up, both arms high), Slam (0.25 s:
both down in front), SlamRecover (1.2 s), Howl (1.2 s: Fast is gone), Stagger, Death.
"""
import math

M, part = kit.M, kit.part  # `kit` is injected by build_model.py

S = 2.0  # twice a Clinger
SHOULDER_Z = 0.78 * S
BONES = {
    "Root": ((0, 0, 0), (0, 0, 0.25 * S), None),
    "Pivot": ((0, 0, 0.35 * S), (0, 0, 0.45 * S), "Root"),
    "Spine": ((0, 0, 0.3 * S), (0, -0.22 * S, SHOULDER_Z), "Pivot"),
    "Head": ((0, -0.22 * S, SHOULDER_Z), (0, -0.36 * S, 0.95 * S), "Spine"),
    "ArmL": ((-0.17 * S, -0.22 * S, SHOULDER_Z), (-0.21 * S, -0.5 * S, 0.12 * S), "Spine"),
    "ArmR": ((0.17 * S, -0.22 * S, SHOULDER_Z), (0.21 * S, -0.5 * S, 0.12 * S), "Spine"),
    "Back": ((0, 0.1 * S, 0.74 * S), (0, 0.1 * S, 0.84 * S), "Spine"),  # where Fast rides
}


def with_(pose, **changes):
    merged = dict(pose)
    merged.update(changes)
    return merged


REST = {}
SWEEP_BACK = {"Spine": (4, 0, 34), "Head": (-4, 0, -20), "ArmR": (-30, 0, 85), "ArmL": (10, 0, 0), "Pivot": (4, 0, 0)}
SWEEP_THROUGH = {"Spine": (8, 0, -30), "Head": (-6, 0, 18), "ArmR": (-45, 0, -75), "ArmL": (0, 0, -10), "Pivot": (10, 0, 0)}
REARED = {"Pivot": (-18, 0, 0), "Spine": (-14, 0, 0), "Head": (-24, 0, 0), "ArmL": (-165, 0, -12), "ArmR": (-165, 0, 12),
          "Root": ((0, 0, 0), (0, 0.08, 0.1))}
SLAMMED = {"Pivot": (34, 0, 0), "Spine": (12, 0, 0), "Head": (-30, 0, 0), "ArmL": (-62, 0, -8), "ArmR": (-62, 0, 8),
           "Root": ((0, 0, 0), (0, -0.1, -0.12))}

ACTIONS = {
    "Idle": ("BEZIER", [
        (1, REST),
        (24, {"Spine": (4, 0, 2), "Head": (-5, 0, 6), "ArmL": (5, 0, 0), "ArmR": (-3, 0, 0), "Root": ((0, 0, 0), (0, 0, -0.03))}),
        (48, REST),
    ]),
    "Walk": ("BEZIER", [
        (1, {"Pivot": (10, 0, 3), "ArmL": (-22, 0, 0), "ArmR": (18, 0, 0), "Head": (-10, 0, 0)}),
        (7, {"Pivot": (12, 0, 0), "Head": (-10, 0, 0), "Root": ((0, 0, 0), (0, 0, 0.06))}),
        (13, {"Pivot": (10, 0, -3), "ArmL": (18, 0, 0), "ArmR": (-22, 0, 0), "Head": (-10, 0, 0)}),
        (19, {"Pivot": (12, 0, 0), "Head": (-10, 0, 0), "Root": ((0, 0, 0), (0, 0, 0.06))}),
        (25, {"Pivot": (10, 0, 3), "ArmL": (-22, 0, 0), "ArmR": (18, 0, 0), "Head": (-10, 0, 0)}),
    ]),
    "SweepWindup": ("BEZIER", [(1, REST), (16, SWEEP_BACK), (22, with_(SWEEP_BACK, Spine=(4, 0, 38)))]),
    "Sweep": ("LINEAR", [(1, with_(SWEEP_BACK, Spine=(4, 0, 38))), (4, {"Spine": (6, 0, 0), "ArmR": (-55, 0, 5), "Pivot": (8, 0, 0)}), (8, SWEEP_THROUGH)]),
    "SweepRecover": ("BEZIER", [(1, SWEEP_THROUGH), (10, with_(SWEEP_THROUGH, ArmR=(-20, 0, -60))), (22, REST)]),
    "SlamWindup": ("BEZIER", [(1, REST), (14, REARED), (27, with_(REARED, Pivot=(-22, 0, 0)))]),
    "Slam": ("LINEAR", [(1, with_(REARED, Pivot=(-22, 0, 0))), (3, {"Pivot": (20, 0, 0), "ArmL": (-100, 0, -8), "ArmR": (-100, 0, 8)}), (7, SLAMMED)]),
    "SlamRecover": ("BEZIER", [(1, SLAMMED), (16, SLAMMED), (29, REST)]),
    "Howl": ("BEZIER", [
        (1, REST),
        (8, {"Pivot": (-26, 0, 0), "Spine": (-10, 0, 0), "Head": (-45, 0, 0), "ArmL": (-60, 0, -70), "ArmR": (-60, 0, 70), "Root": ((0, 0, 0), (0, 0.05, 0.08))}),
        (22, {"Pivot": (-28, 0, 0), "Spine": (-12, 0, 0), "Head": (-50, 0, 0), "ArmL": (-65, 0, -75), "ArmR": (-65, 0, 75), "Root": ((0, 0, 0), (0, 0.05, 0.1))}),
        (29, REST),
    ]),
    "Stagger": ("BEZIER", [
        (1, REST),
        (5, {"Pivot": (-18, 0, 8), "Head": (-16, 0, 0), "ArmL": (30, 0, -20), "ArmR": (30, 0, 20), "Root": ((0, 0, 0), (0, 0.15, 0))}),
        (16, {"Pivot": (-8, 0, -4), "ArmL": (15, 0, -8), "ArmR": (15, 0, 8)}),
        (28, REST),
    ]),
    "Death": ("BEZIER", [  # a long reach for what isn't there, then it folds down onto the stones
        (1, REST),
        (8, {"Pivot": (-14, 0, 0), "Head": (-30, 0, 0), "ArmL": (-100, 0, -20), "ArmR": (-100, 0, 20)}),
        (18, {"Pivot": (48, 0, 6), "Head": (15, 0, 0), "ArmL": (-50, 0, -30), "ArmR": (-50, 0, 30), "Root": ((0, 0, 0), (0, -0.05, -0.3))}),
        (26, {"Pivot": (66, 0, 8), "Head": (22, 0, 0), "ArmL": (-20, 0, -45), "ArmR": (-20, 0, 45), "Root": ((0, 0, 0), (0, -0.1, -0.5))}),
    ]),
}


def build():
    robe, dark = kit.mat("SouredGrey", "#7c8291"), kit.mat("SouredGreyDark", "#50545f")
    eye, claw = kit.mat("SouredEye_Glow", "#c8f07a"), kit.mat("Claw", "#d6cdb8")
    ribbon = kit.mat("Ribbon", "#8a3a34")
    metal = kit.mat("HeapMetal", "#34313a")

    def s(p):
        return tuple(c * S for c in p)

    def robe_shape(bm):
        kit.cone(9, 0.26 * S, 0.12 * S, 0.62 * S)(bm)
        rim = sorted((v for v in bm.verts if v.co.z < -0.29 * S and v.co.length > 0.05), key=lambda v: math.atan2(v.co.y, v.co.x))
        for i, v in enumerate(rim):
            v.co.z += 0.07 * S if i % 2 else 0.0

    parts = [
        part("Robe", robe_shape, robe, M(s((0, -0.04, 0.33)), rot=(16, 0, 0)), smooth=True, group="Spine"),
        part("Back", kit.sphere(10, 7), robe, M(s((0, -0.12, 0.68)), rot=(30, 0, 0), scale=s((0.23, 0.2, 0.2))), smooth=True, group="Spine"),
        part("Head", kit.sphere(9, 6, 0.13 * S), dark, M(s((0, -0.33, 0.86))), smooth=True, group="Head"),
        part("Hood", kit.cone(9, 0.17 * S, 0.03 * S, 0.34 * S), robe, M(s((0, -0.27, 0.95)), rot=(-48, 0, 0)), smooth=True, group="Head"),
        # The shroud they share, draped over Hold's back where Fast rides.
        part("Shroud", kit.sphere(9, 6), dark, M(s((0, 0.08, 0.7)), rot=(10, 0, 0), scale=s((0.27, 0.16, 0.12))), smooth=True, jitter=0.02, group="Spine"),
    ]
    for side, bone in ((-1, "ArmL"), (1, "ArmR")):
        parts.append(part("Eye", kit.sphere(8, 6, 0.025 * S), eye, M(s((side * 0.05, -0.45, 0.85))), smooth=True, group="Head"))
        shoulder, elbow, hand = s((side * 0.17, -0.22, 0.78)), s((side * 0.22, -0.4, 0.46)), s((side * 0.21, -0.5, 0.12))
        parts.append(part("Arm", kit.tube([shoulder, elbow, hand], [0.05 * S, 0.036 * S, 0.03 * S], seg=7), robe, group=bone))
        for k in (-1, 0, 1):
            tip = (hand[0] + k * 0.035 * S, hand[1] - 0.1 * S, hand[2] - 0.08 * S)
            parts.append(part("Claw", kit.cone(4, 0.016 * S, 0.0, 0.12 * S), claw, kit.along(hand, tip), group=bone))
    # The ribbon round Hold's right wrist, its ends hanging (its other end was round Fast's).
    wrist = s((0.215, -0.47, 0.2))
    parts.append(part("RibbonBand", kit.cylinder(7, 0.04 * S, 0.03 * S), ribbon, M(wrist, rot=(20, 0, 0)), group="ArmR"))
    parts.append(part("RibbonEnd", kit.tube([wrist, (wrist[0] + 0.05, wrist[1] + 0.1, wrist[2] - 0.2), (wrist[0] + 0.02, wrist[1] + 0.2, wrist[2] - 0.34)],
                                            [0.02, 0.018, 0.012], seg=4), ribbon, group="ArmR"))
    # Its belt lantern, like every Soured's.
    lantern_glass = kit.mat("SouredLantern_Glow", "#b6f06a")
    parts += [
        part("BeltLanternCord", kit.cube(), metal, M(s((0.2, 0.02, 0.5)), rot=(0, -15, 0), scale=s((0.012, 0.012, 0.1))), group="Spine"),
        part("BeltLanternTop", kit.cone(4, 0.06 * S, 0.01 * S, 0.05 * S), metal, M(s((0.21, 0.02, 0.43)), rot=(0, 0, 45)), group="Spine"),
        part("BeltLanternGlass", kit.cube(), lantern_glass, M(s((0.21, 0.02, 0.36)), scale=s((0.075, 0.075, 0.1))), group="Spine"),
        part("BeltLanternBase", kit.cube(), metal, M(s((0.21, 0.02, 0.3)), scale=s((0.09, 0.09, 0.015))), group="Spine"),
    ]
    mesh = kit.join(parts, "Hold")
    armature = kit.rig("HoldRig", BONES)
    kit.skin(mesh, armature)
    kit.animate(armature, ACTIONS)
    return [armature, mesh]
