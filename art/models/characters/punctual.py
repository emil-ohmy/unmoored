"""A Punctual (the Hall of the Last Hour): one of the household, still keeping its hours. A footman in a faded claret
livery coat with long tails, a cream waistcoat, white gloves, a high collar; stiffly upright, its head bowed; its eyes
the Soured's green. Where its heart was, a pocket watch has grown into its chest: a brass case, a pale face, one black
hand (on its own bone, WatchHand, which the game turns a tick each beat, and spins when it's lost count). In its right
hand a great brass winding key, held like a cudgel. And, like every Soured, a small green lantern at its belt.

Clips (30 fps; the Hall's beat is a second, 30 frames): Idle (at attention, head bowed), Walk (2 s: a stiff march, one
step a beat), Windup (1.85 s: the Count: the key raised overhead, a jolt on the beat between), Strike (0.3 s: brought
straight down), Recover (1.2 s), Warn (0.85 s: the Quarters' tell, the key drawn back to its right), Quarters (3.3 s:
four blows a beat apart: forehand, backhand, forehand, then overhead), QuartersRecover (1.4 s), Lost (2 s loop: head
cocked, listening for the beat), Stagger, Death (it stops, then topples stiffly forward).
"""
import math

M, part = kit.M, kit.part  # `kit` is injected by build_model.py

SHOULDER = 1.5
HIP = 0.95
WATCH = (0, -0.205, 1.27)
BONES = {
    "Root": ((0, 0, 0), (0, 0, 0.3), None),
    "Spine": ((0, 0, HIP), (0, 0, SHOULDER), "Root"),
    "Head": ((0, 0, SHOULDER), (0, -0.02, 1.86), "Spine"),
    "ArmL": ((-0.24, 0, SHOULDER - 0.05), (-0.27, -0.02, 0.93), "Spine"),
    "ArmR": ((0.24, 0, SHOULDER - 0.05), (0.27, -0.02, 0.93), "Spine"),
    "Key": ((0.27, -0.04, 0.93), (0.27, -0.3, 0.2), "ArmR"),
    "LegL": ((-0.11, 0, HIP), (-0.11, 0, 0.06), "Root"),
    "LegR": ((0.11, 0, HIP), (0.11, 0, 0.06), "Root"),
    "WatchHand": (WATCH, (0, -0.3, WATCH[2]), "Spine"),
}


def with_(pose, **changes):
    merged = dict(pose)
    merged.update(changes)
    return merged


ATTN = {"Head": (14, 0, 0)}
BREATH = {"Head": (14, 0, 0), "Spine": (2, 0, 0)}
PASS = {"Head": (8, 0, 0), "Root": ((0, 0, 0), (0, 0, -0.02))}
STEP_L = {"Head": (8, 0, 0), "LegL": (-24, 0, 0), "LegR": (14, 0, 0), "ArmL": (14, 0, 0), "ArmR": (-6, 0, 0),
          "Spine": (3, 0, -3), "Root": ((0, 0, 0), (0, 0, 0.03))}
STEP_R = {"Head": (8, 0, 0), "LegR": (-24, 0, 0), "LegL": (14, 0, 0), "ArmL": (-14, 0, 0), "ArmR": (6, 0, 0),
          "Spine": (3, 0, 3), "Root": ((0, 0, 0), (0, 0, 0.03))}
RAISED = {"Spine": (-8, 0, 0), "Head": (2, 0, 0), "ArmR": (-165, 0, 8), "ArmL": (-25, 0, 0), "Key": (-10, 0, 0),
          "LegL": (-10, 0, 0), "LegR": (8, 0, 0)}
RAISED_HIGH = with_(RAISED, Spine=(-12, 0, 0), ArmR=(-178, 0, 6))
SMASH = {"Spine": (26, 0, 0), "Head": (12, 0, 0), "ArmR": (-55, 0, 0), "ArmL": (-10, 0, 0), "Key": (25, 0, 0),
         "LegL": (-22, 0, 0), "LegR": (16, 0, 0), "Root": ((0, 0, 0), (0, -0.12, -0.07))}
WARN = {"Spine": (-4, 0, 32), "Head": (10, 0, -20), "ArmR": (-95, 0, 55), "ArmL": (-20, 0, -10), "Key": (0, 0, 30),
        "LegL": (-12, 0, 0), "LegR": (8, 0, 0)}
FORE = {"Spine": (10, 0, -32), "Head": (10, 0, 14), "ArmR": (-85, 0, -60), "ArmL": (-10, 0, 10), "Key": (0, 0, -25),
        "LegR": (-18, 0, 0), "LegL": (12, 0, 0), "Root": ((0, 0, 0), (0, -0.1, -0.03))}
BACK_DRAWN = {"Spine": (2, 0, -38), "Head": (10, 0, 16), "ArmR": (-100, 0, -95), "ArmL": (-10, 0, 10), "Key": (0, 0, -30),
              "LegR": (-10, 0, 0), "LegL": (8, 0, 0)}
BACK = {"Spine": (10, 0, 30), "Head": (10, 0, -14), "ArmR": (-85, 0, 50), "ArmL": (-15, 0, -10), "Key": (0, 0, 25),
        "LegL": (-18, 0, 0), "LegR": (12, 0, 0), "Root": ((0, 0, 0), (0, -0.1, -0.03))}

ACTIONS = {
    "Idle": ("BEZIER", [(1, ATTN), (45, BREATH), (90, ATTN)]),
    "Walk": ("BEZIER", [(1, PASS), (15, STEP_L), (31, PASS), (45, STEP_R), (61, PASS)]),
    # The Count: the key goes up on the beat, a jolt on the next (tick), and comes down on the third.
    "Windup": ("BEZIER", [(1, ATTN), (16, RAISED), (29, RAISED), (31, with_(RAISED, Spine=(-4, 0, 0))), (34, RAISED),
                          (56, RAISED_HIGH)]),
    "Strike": ("LINEAR", [(1, RAISED_HIGH), (5, SMASH), (10, with_(SMASH, Spine=(30, 0, 0)))]),
    "Recover": ("BEZIER", [(1, SMASH), (14, with_(SMASH, Spine=(28, 0, 0))), (37, ATTN)]),
    # The Quarters: drawn back (the warning), then four blows on four beats, a step into each.
    "Warn": ("BEZIER", [(1, ATTN), (18, WARN), (26, with_(WARN, Spine=(-6, 0, 36)))]),
    "Quarters": ("BEZIER", [
        (1, WARN), (5, FORE), (22, FORE), (30, BACK_DRAWN),
        (36, BACK), (52, BACK), (60, WARN),
        (66, FORE), (80, FORE), (90, RAISED_HIGH),
        (96, SMASH), (100, with_(SMASH, Spine=(30, 0, 0))),
    ]),
    "QuartersRecover": ("BEZIER", [(1, SMASH), (18, with_(SMASH, Spine=(30, 0, 0), Head=(18, 0, 0))), (43, ATTN)]),
    "Lost": ("BEZIER", [(1, with_(ATTN, Head=(6, 0, 22))), (30, with_(ATTN, Head=(10, 0, -22))), (61, with_(ATTN, Head=(6, 0, 22)))]),
    "Stagger": ("BEZIER", [(1, ATTN), (5, {"Spine": (-16, 0, 8), "Head": (-12, 0, 0), "ArmR": (-30, 0, 20),
                                           "Root": ((0, 0, 0), (0, 0.12, 0))}), (18, ATTN)]),
    "Death": ("BEZIER", [
        (1, ATTN),
        (8, with_(ATTN, Head=(-6, 0, 0), ArmR=(-20, 0, 10))),
        (20, {"Root": ((40, 0, 0), (0, -0.2, -0.05)), "Head": (20, 0, 0), "ArmR": (20, 0, 20)}),
        (26, {"Root": ((80, 0, 0), (0, -0.55, 0.05)), "Head": (25, 0, 0), "ArmR": (40, 0, 30), "ArmL": (30, 0, 0)}),
    ]),
}


def build():
    coat, coat_dark = kit.mat("PunctualLivery", "#6a2a30"), kit.mat("PunctualLiveryDark", "#4a1d22")
    vest, linen = kit.mat("PunctualWaistcoat", "#cdbf9c"), kit.mat("PunctualLinen", "#e6dfcd")
    trousers, shoe = kit.mat("PunctualTrousers", "#2b2930"), kit.mat("PunctualShoe", "#17161a")
    brass, face = kit.mat("PunctualBrass_Metal", "#c99a45"), kit.mat("PunctualWatchFace", "#ece4cc")
    black, eye = kit.mat("PunctualHand", "#141316"), kit.mat("SouredEye_Glow", "#c8f07a")
    skin, hair = kit.mat("SouredOchreDark", "#5e4a2c"), kit.mat("PunctualHair", "#2c2622")
    iron = kit.mat("PunctualIron", "#2f2d33")

    parts = []
    # Legs: trousers to the shoe, rigid on their own bones.
    for s, bone in ((-1, "LegL"), (1, "LegR")):
        hip, ankle = (s * 0.11, 0, HIP - 0.02), (s * 0.11, 0, 0.1)
        parts.append(part("Trouser", kit.cone(7, 0.08, 0.065, math.dist(hip, ankle)), trousers, kit.along(ankle, hip), smooth=True, group=bone))
        parts.append(part("Shoe", kit.cube(), shoe, M((s * 0.11, -0.05, 0.05), scale=(0.11, 0.25, 0.1)), group=bone))
    # The body: the coat over a cream waistcoat, its tails behind to the knees, brass buttons.
    parts += [
        part("Coat", kit.cone(9, 0.2, 0.26, SHOULDER - HIP + 0.05), coat, M((0, 0.01, (HIP + SHOULDER) / 2), scale=(1, 0.72, 1)), smooth=True, group="Spine"),
        part("Waistcoat", kit.cube(), vest, M((0, -0.15, 1.15), scale=(0.2, 0.06, 0.38)), group="Spine"),
        part("Shoulders", kit.sphere(9, 6), coat, M((0, 0, SHOULDER - 0.06), scale=(0.3, 0.2, 0.1)), smooth=True, group="Spine"),
        part("Collar", kit.cylinder(9, 0.09, 0.1), linen, M((0, -0.01, SHOULDER + 0.02)), smooth=True, group="Spine"),
    ]
    for s in (-1, 1):
        parts.append(part("Tail", kit.cube(), coat_dark, M((s * 0.09, 0.15, HIP - 0.25), rot=(-8, 0, s * 6), scale=(0.16, 0.04, 0.6)), group="Spine"))
        parts.append(part("Lapel", kit.cube(), coat_dark, M((s * 0.13, -0.16, 1.32), rot=(0, s * 12, 0), scale=(0.07, 0.03, 0.3)), group="Spine"))
    for z in (1.02, 1.12):
        for s in (-1, 1):
            parts.append(part("Button", kit.sphere(6, 4, 0.018), brass, M((s * 0.15, -0.18, z)), group="Spine"))
    # The watch where its heart was: a brass case grown into the chest, a pale face, a hand on its own bone.
    wx, wy, wz = WATCH
    parts += [
        part("WatchCase", kit.cylinder(16, 0.11, 0.05), brass, M((wx, wy + 0.02, wz), rot=(90, 0, 0)), smooth=True, group="Spine"),
        part("WatchFace", kit.cylinder(16, 0.09, 0.012), face, M((wx, wy - 0.006, wz), rot=(90, 0, 0)), group="Spine"),
        part("WatchBow", kit.cylinder(8, 0.025, 0.03), brass, M((wx, wy + 0.01, wz + 0.125)), group="Spine"),
        part("WatchHand", kit.cube(), black, M((wx, wy - 0.016, wz + 0.035), scale=(0.012, 0.006, 0.08)), group="WatchHand"),
        part("WatchPin", kit.cylinder(6, 0.012, 0.012), brass, M((wx, wy - 0.02, wz), rot=(90, 0, 0)), group="WatchHand"),
    ]
    for k in range(12):  # the hours, ticked round the face
        a = 2 * math.pi * k / 12
        parts.append(part("Hour", kit.cube(), black, M((wx + math.sin(a) * 0.075, wy - 0.013, wz + math.cos(a) * 0.075),
                                                       rot=(0, -math.degrees(a), 0), scale=(0.006, 0.004, 0.02 if k % 3 else 0.03)), group="Spine"))
    # The head, bowed: dark hair pressed flat, the green eyes.
    parts += [
        part("Head", kit.sphere(10, 7, 0.15), skin, M((0, -0.04, 1.68), scale=(0.95, 1.0, 1.08)), smooth=True, group="Head"),
        part("Hair", kit.sphere(10, 6, 0.155), hair, M((0, 0.0, 1.72), scale=(0.97, 1.0, 0.85)), smooth=True, group="Head"),
    ]
    for s in (-1, 1):
        parts.append(part("Eye", kit.sphere(8, 6, 0.026), eye, M((s * 0.055, -0.175, 1.68)), smooth=True, group="Head"))
    # Arms in livery sleeves, white gloves.
    for s, bone, hand in ((-1, "ArmL", (-0.27, -0.02, 0.93)), (1, "ArmR", (0.27, -0.02, 0.93))):
        shoulder = (s * 0.24, 0, SHOULDER - 0.05)
        parts.append(part("Sleeve", kit.cone(7, 0.075, 0.06, math.dist(shoulder, hand)), coat, kit.along(hand, shoulder), smooth=True, group=bone))
        parts.append(part("Cuff", kit.cylinder(7, 0.065, 0.05), coat_dark, kit.along(hand, (hand[0], hand[1], hand[2] + 0.05)), group=bone))
        parts.append(part("Glove", kit.sphere(7, 5, 0.06), linen, M((hand[0], hand[1] - 0.02, hand[2] - 0.05)), smooth=True, group=bone))
    # The winding key: its bow in the hand, the shaft down and forward, the bit at the end.
    grip, tip = (0.27, -0.06, 0.88), (0.27, -0.3, 0.2)
    ring = [(0.27, -0.06 + 0.09 * math.sin(a), 0.93 + 0.09 * math.cos(a)) for a in (2 * math.pi * k / 12 for k in range(13))]
    parts += [
        part("KeyBow", kit.tube(ring, [0.022] * len(ring), seg=6), brass, group="Key"),
        part("KeyShaft", kit.cylinder(8, 0.03, math.dist(grip, tip)), brass, kit.along(grip, tip), smooth=True, group="Key"),
        part("KeyCollar", kit.cylinder(8, 0.05, 0.04), brass, kit.along(grip, (0.27, -0.072, 0.845)), group="Key"),
        part("KeyBit", kit.cube(), brass, kit.along((0.27, -0.27, 0.29), tip) @ M((0.05, 0, 0), scale=(0.12, 0.04, 0.1)), group="Key"),
    ]
    # A lantern hung from its belt, like every Soured's.
    lantern_glass = kit.mat("SouredLantern_Glow", "#b6f06a")
    parts += [
        part("BeltLanternTop", kit.cone(4, 0.06, 0.01, 0.05), iron, M((-0.25, 0.02, 0.92), rot=(0, 0, 45)), group="Spine"),
        part("BeltLanternGlass", kit.cube(), lantern_glass, M((-0.25, 0.02, 0.85), scale=(0.08, 0.08, 0.1)), group="Spine"),
        part("BeltLanternBase", kit.cube(), iron, M((-0.25, 0.02, 0.795), scale=(0.1, 0.1, 0.015)), group="Spine"),
    ]
    mesh = kit.join(parts, "Punctual")
    armature = kit.rig("PunctualRig", BONES)
    kit.skin(mesh, armature)
    kit.animate(armature, ACTIONS)
    return [armature, mesh]
