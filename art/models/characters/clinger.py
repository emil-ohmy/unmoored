"""The Clinger: a Soured that couldn't let go of a person. Thin, grey and hunched, its face lost in its hood, with arms
far too long, knuckles on the ground. They come in pairs. It skitters in quick bursts, crouches (the tell), leaps, and
if it catches you it clings to your back, arms wrapped round your shoulders. Pale grey-blue, where the Laden is violet
and the archer ochre; the same sickly green eyes and belt lantern as every Soured.

Clips: Idle, Skitter (the run), Crouch (0.35 s telegraph: coiled low, arms back), Leap (0.35 s: stretched out, arms
reaching), Land (0.7 s: sprawled where it came down, then up: the punish window), Cling (loop: on your back, arms
round you), Thrown (0.5 s: flung off, tumbling), Stagger, Death.
"""
import math

M, part = kit.M, kit.part  # `kit` is injected by build_model.py

SHOULDER_Z = 0.78
BONES = {
    "Root": ((0, 0, 0), (0, 0, 0.25), None),
    "Pivot": ((0, 0, 0.35), (0, 0, 0.45), "Root"),  # whole-body tumbles (thrown, the leap) turn about here
    "Spine": ((0, 0, 0.3), (0, -0.22, SHOULDER_Z), "Pivot"),
    "Head": ((0, -0.22, SHOULDER_Z), (0, -0.36, 0.95), "Spine"),
    "ArmL": ((-0.17, -0.22, SHOULDER_Z), (-0.21, -0.5, 0.12), "Spine"),
    "ArmR": ((0.17, -0.22, SHOULDER_Z), (0.21, -0.5, 0.12), "Spine"),
}


def with_(pose, **changes):
    merged = dict(pose)
    merged.update(changes)
    return merged


REST = {}
# Arms: +X pitch swings an arm back (it hangs forward-down), -X raises it forward. Whole-body leans on Pivot.
CROUCH = {"Pivot": (22, 0, 0), "Spine": (10, 0, 0), "Head": (-24, 0, 0), "ArmL": (40, 0, -12), "ArmR": (40, 0, 12),
          "Root": ((0, 0, 0), (0, 0.08, -0.14))}
LEAP = {"Pivot": (58, 0, 0), "Spine": (-6, 0, 0), "Head": (-40, 0, 0), "ArmL": (-95, 0, -18), "ArmR": (-95, 0, 18),
        "Root": ((0, 0, 0), (0, 0, 0.3))}
SPRAWL = {"Pivot": (40, 0, 0), "Spine": (12, 0, 0), "Head": (-30, 0, 0), "ArmL": (-10, 0, -40), "ArmR": (-10, 0, 40),
          "Root": ((0, 0, 0), (0, -0.05, -0.18))}
# On the Apprentice's back: pressed close, arms wrapped forward round their shoulders, legs tucked (the game sets it
# behind and above their shoulders).
CLING = {"Pivot": (14, 0, 0), "Spine": (6, 0, 0), "Head": (10, 0, 0), "ArmL": ((-78, 0, 32), (0.06, 0, 0)), "ArmR": ((-78, 0, -32), (-0.06, 0, 0))}

ACTIONS = {
    "Idle": ("BEZIER", [
        (1, REST),
        (16, {"Spine": (4, 0, 3), "Head": (-6, 0, 8), "ArmL": (6, 0, 0), "ArmR": (-4, 0, 0)}),
        (32, {"Spine": (2, 0, -3), "Head": (-2, 0, -10), "ArmL": (-4, 0, 0), "ArmR": (6, 0, 0)}),
        (48, REST),
    ]),
    # A quick scrabble on all fours: arms reaching forward in turn, the body bobbing low (12 frames).
    "Skitter": ("BEZIER", [
        (1, {"Pivot": (18, 0, 0), "ArmL": (-30, 0, 0), "ArmR": (25, 0, 0), "Head": (-16, 0, 0), "Root": ((0, 0, 0), (0, 0, -0.04))}),
        (4, {"Pivot": (20, 0, 4), "ArmL": (0, 0, 0), "ArmR": (0, 0, 0), "Head": (-18, 0, 0), "Root": ((0, 0, 0), (0, 0, 0.04))}),
        (7, {"Pivot": (18, 0, 0), "ArmL": (25, 0, 0), "ArmR": (-30, 0, 0), "Head": (-16, 0, 0), "Root": ((0, 0, 0), (0, 0, -0.04))}),
        (10, {"Pivot": (20, 0, -4), "ArmL": (0, 0, 0), "ArmR": (0, 0, 0), "Head": (-18, 0, 0), "Root": ((0, 0, 0), (0, 0, 0.04))}),
        (13, {"Pivot": (18, 0, 0), "ArmL": (-30, 0, 0), "ArmR": (25, 0, 0), "Head": (-16, 0, 0), "Root": ((0, 0, 0), (0, 0, -0.04))}),
    ]),
    "Crouch": ("BEZIER", [(1, REST), (6, CROUCH), (9, with_(CROUCH, Root=((0, 0, 0), (0, 0.1, -0.16))))]),
    "Leap": ("BEZIER", [(1, CROUCH), (3, LEAP), (9, with_(LEAP, Pivot=(64, 0, 0), Root=((0, 0, 0), (0, 0, 0.15))))]),
    "Land": ("BEZIER", [(1, SPRAWL), (8, with_(SPRAWL, Head=(-20, 0, 0))), (17, REST)]),
    "Cling": ("BEZIER", [
        (1, CLING),
        (12, with_(CLING, Pivot=(18, 0, 3), Head=(16, 0, 6))),
        (24, CLING),
    ]),
    "Thrown": ("BEZIER", [
        (1, CLING),
        (4, {"Pivot": (-60, 0, 30), "ArmL": (-120, 0, -40), "ArmR": (-120, 0, 40), "Head": (-20, 0, 0), "Root": ((0, 0, 0), (0, 0.2, 0.3))}),
        (8, {"Pivot": (-150, 0, 50), "ArmL": (-60, 0, -60), "ArmR": (-60, 0, 60), "Root": ((0, 0, 0), (0, 0.3, 0.25))}),
        (12, SPRAWL),
    ]),
    "Stagger": ("BEZIER", [
        (1, REST),
        (4, {"Pivot": (-20, 0, 8), "Head": (-18, 0, 0), "ArmL": (30, 0, -20), "ArmR": (30, 0, 20), "Root": ((0, 0, 0), (0, 0.1, 0))}),
        (14, {"Pivot": (-8, 0, -4), "ArmL": (15, 0, -8), "ArmR": (15, 0, 8)}),
        (24, REST),
    ]),
    "Death": ("BEZIER", [  # a jolt, the arms thrown up, then it folds down onto the ground
        (1, REST),
        (5, {"Pivot": (-24, 0, 0), "Head": (-30, 0, 0), "ArmL": (-110, 0, -30), "ArmR": (-110, 0, 30), "Root": ((0, 0, 0), (0, 0.08, 0.02))}),
        (13, {"Pivot": (55, 0, 6), "Head": (20, 0, 0), "ArmL": (-20, 0, -50), "ArmR": (-20, 0, 50), "Root": ((0, 0, 0), (0, -0.05, -0.2))}),
        (20, {"Pivot": (70, 0, 8), "Head": (25, 0, 0), "ArmL": (-10, 0, -55), "ArmR": (-10, 0, 55), "Root": ((0, 0, 0), (0, -0.08, -0.26))}),
    ]),
}


def build():
    robe, dark = kit.mat("SouredGrey", "#7c8291"), kit.mat("SouredGreyDark", "#50545f")
    eye, claw = kit.mat("SouredEye_Glow", "#c8f07a"), kit.mat("Claw", "#d6cdb8")
    metal = kit.mat("HeapMetal", "#34313a")
    rng = kit.rng

    def robe_shape(bm):
        kit.cone(8, 0.26, 0.12, 0.62)(bm)
        rim = sorted((v for v in bm.verts if v.co.z < -0.29 and v.co.length > 0.05), key=lambda v: math.atan2(v.co.y, v.co.x))
        for i, v in enumerate(rim):
            v.co.z += 0.07 if i % 2 else 0.0  # tattered hem

    parts = [
        part("Robe", robe_shape, robe, M((0, -0.04, 0.33), rot=(16, 0, 0)), smooth=True, group="Spine"),
        part("Back", kit.sphere(9, 6), robe, M((0, -0.12, 0.68), rot=(30, 0, 0), scale=(0.21, 0.19, 0.2)), smooth=True, group="Spine"),
        part("Head", kit.sphere(9, 6, 0.13), dark, M((0, -0.33, 0.86)), smooth=True, group="Head"),
        part("Hood", kit.cone(8, 0.17, 0.03, 0.34), robe, M((0, -0.27, 0.95), rot=(-48, 0, 0)), smooth=True, group="Head"),
    ]
    for s, bone in ((-1, "ArmL"), (1, "ArmR")):
        parts.append(part("Eye", kit.sphere(8, 6, 0.025), eye, M((s * 0.05, -0.45, 0.85)), smooth=True, group="Head"))
        shoulder, elbow, hand = (s * 0.17, -0.22, SHOULDER_Z), (s * 0.22, -0.4, 0.46), (s * 0.21, -0.5, 0.12)
        parts.append(part("Arm", kit.tube([shoulder, elbow, hand], [0.05, 0.036, 0.03], seg=6), robe, group=bone))
        for k in (-1, 0, 1):  # three long claws
            tip = (hand[0] + k * 0.035, hand[1] - 0.1, hand[2] - 0.08)
            parts.append(part("Claw", kit.cone(4, 0.016, 0.0, 0.12), claw, kit.along(hand, tip), group=bone))
    # A small lantern hung from its belt, like every Soured's.
    lantern_glass = kit.mat("SouredLantern_Glow", "#b6f06a")
    parts += [
        part("BeltLanternCord", kit.cube(), metal, M((0.2, 0.02, 0.5), rot=(0, -15, 0), scale=(0.012, 0.012, 0.1)), group="Spine"),
        part("BeltLanternTop", kit.cone(4, 0.06, 0.01, 0.05), metal, M((0.21, 0.02, 0.43), rot=(0, 0, 45)), group="Spine"),
        part("BeltLanternGlass", kit.cube(), lantern_glass, M((0.21, 0.02, 0.36), scale=(0.075, 0.075, 0.1)), group="Spine"),
        part("BeltLanternBase", kit.cube(), metal, M((0.21, 0.02, 0.3), scale=(0.09, 0.09, 0.015)), group="Spine"),
    ]
    mesh = kit.join(parts, "Clinger")
    armature = kit.rig("ClingerRig", BONES)
    kit.skin(mesh, armature)
    kit.animate(armature, ACTIONS)
    return [armature, mesh]
