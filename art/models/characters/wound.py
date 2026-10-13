"""A Wound (the Hall of the Last Hour): one of the household's hounds, given more time a little at a time. A lean grey
sighthound, its flanks opened on brass wheels where the ribs were, iron legs; a brass collar, and hung from it, like
every Soured's, a small green lantern; the Soured's green eyes. In its back a wind-up key (a toy's, two round wings),
on its own bone (Key), which the game turns: spinning as it runs, slowing as it runs down, cranked round as it winds.

Clips (30 fps): Idle (2 s, standing, head turning), Run (0.5 s: a gallop), Walk (1 s: a trot), Crouch (0.3 s: the bite's
tell, head low, haunches gathered), Bite (0.2 s: lunge and snap), BiteRecover (0.3 s), Gather (0.45 s: the pounce's
tell, flat to the floor), Pounce (0.45 s: stretched out in the air), PounceRecover (0.7 s), RunDown (1 s: it sags, head
dropping, onto its haunches), Wind (2.4 s: sat, shuddering as the key's cranked round), Stagger, Death (onto its side).
"""
import math

M, part = kit.M, kit.part  # `kit` is injected by build_model.py

BACK = 0.6
BONES = {
    "Root": ((0, 0, 0), (0, 0, 0.2), None),
    "Body": ((0, 0.38, BACK - 0.04), (0, -0.33, BACK), "Root"),
    "Neck": ((0, -0.33, BACK + 0.02), (0, -0.5, BACK + 0.22), "Body"),
    "Head": ((0, -0.5, BACK + 0.22), (0, -0.82, BACK + 0.16), "Neck"),
    "Jaw": ((0, -0.56, BACK + 0.15), (0, -0.8, BACK + 0.09), "Head"),
    "LegFL": ((-0.09, -0.27, BACK - 0.08), (-0.09, -0.3, 0.03), "Body"),
    "LegFR": ((0.09, -0.27, BACK - 0.08), (0.09, -0.3, 0.03), "Body"),
    "LegBL": ((-0.09, 0.33, BACK - 0.1), (-0.09, 0.38, 0.03), "Body"),
    "LegBR": ((0.09, 0.33, BACK - 0.1), (0.09, 0.38, 0.03), "Body"),
    "Tail": ((0, 0.42, BACK - 0.02), (0, 0.78, BACK - 0.12), "Body"),
    "Key": ((0, 0.04, BACK + 0.12), (0, 0.04, BACK + 0.42), "Body"),
}


def with_(pose, **changes):
    merged = dict(pose)
    merged.update(changes)
    return merged


STAND = {"Tail": (-10, 0, 0)}
LOOK_L = with_(STAND, Head=(0, 0, 18), Neck=(0, 0, 8))
LOOK_R = with_(STAND, Head=(0, 0, -18), Neck=(0, 0, -8))
REACH = {"LegFL": (-38, 0, 0), "LegFR": (-30, 0, 0), "LegBL": (34, 0, 0), "LegBR": (40, 0, 0), "Body": (-4, 0, 0),
         "Head": (6, 0, 0), "Tail": (-20, 0, 0), "Root": ((0, 0, 0), (0, 0, 0.06))}
GATHER = {"LegFL": (30, 0, 0), "LegFR": (36, 0, 0), "LegBL": (-34, 0, 0), "LegBR": (-28, 0, 0), "Body": (5, 0, 0),
          "Head": (-4, 0, 0), "Tail": (-5, 0, 0), "Root": ((0, 0, 0), (0, 0, -0.03))}
TROT_A = {"LegFL": (-20, 0, 0), "LegBR": (-18, 0, 0), "LegFR": (16, 0, 0), "LegBL": (16, 0, 0), "Tail": (-12, 0, 0)}
TROT_B = {"LegFR": (-20, 0, 0), "LegBL": (-18, 0, 0), "LegFL": (16, 0, 0), "LegBR": (16, 0, 0), "Tail": (-12, 0, 0),
          "Root": ((0, 0, 0), (0, 0, 0.02))}
CROUCH = {"Body": (8, 0, 0), "Neck": (18, 0, 0), "Head": (10, 0, 0), "LegBL": (-30, 0, 0), "LegBR": (-30, 0, 0),
          "LegFL": (12, 0, 0), "LegFR": (12, 0, 0), "Jaw": (10, 0, 0), "Tail": (5, 0, 0), "Root": ((0, 0, 0), (0, 0.06, -0.08))}
SNAP_OPEN = {"Body": (-6, 0, 0), "Neck": (-10, 0, 0), "Head": (-6, 0, 0), "Jaw": (35, 0, 0), "LegFL": (-40, 0, 0),
             "LegFR": (-36, 0, 0), "LegBL": (30, 0, 0), "LegBR": (30, 0, 0), "Root": ((0, 0, 0), (0, -0.25, 0.04))}
SNAPPED = with_(SNAP_OPEN, Jaw=(0, 0, 0), Head=(8, 0, 0))
FLAT = {"Body": (4, 0, 0), "Neck": (26, 0, 0), "Head": (14, 0, 0), "LegBL": (-45, 0, 0), "LegBR": (-45, 0, 0),
        "LegFL": (30, 0, 0), "LegFR": (30, 0, 0), "Tail": (15, 0, 0), "Root": ((0, 0, 0), (0, 0.1, -0.16))}
STRETCHED = {"Body": (-8, 0, 0), "Neck": (-14, 0, 0), "Head": (-4, 0, 0), "Jaw": (30, 0, 0), "LegFL": (-70, 0, 0),
             "LegFR": (-64, 0, 0), "LegBL": (60, 0, 0), "LegBR": (66, 0, 0), "Tail": (-30, 0, 0), "Root": ((0, 0, 0), (0, -0.2, 0.3))}
SAT = {"Body": (-28, 0, 0), "Neck": (-6, 0, 0), "Head": (20, 0, 0), "LegBL": (-80, 0, 0), "LegBR": (-80, 0, 0),
       "LegFL": (22, 0, 0), "LegFR": (22, 0, 0), "Tail": (30, 0, 0), "Root": ((0, 0, 0), (0, 0.1, -0.16))}
SAGGED = with_(SAT, Head=(48, 0, 0), Neck=(20, 0, 0), Body=(-20, 0, 0))

ACTIONS = {
    "Idle": ("BEZIER", [(1, STAND), (20, LOOK_L), (40, STAND), (50, LOOK_R), (61, STAND)]),
    "Run": ("BEZIER", [(1, REACH), (8, GATHER), (16, REACH)]),
    "Walk": ("BEZIER", [(1, TROT_A), (16, TROT_B), (31, TROT_A)]),
    "Crouch": ("BEZIER", [(1, STAND), (10, CROUCH)]),
    "Bite": ("LINEAR", [(1, CROUCH), (4, SNAP_OPEN), (7, SNAPPED)]),
    "BiteRecover": ("BEZIER", [(1, SNAPPED), (10, STAND)]),
    "Gather": ("BEZIER", [(1, STAND), (12, FLAT), (14, with_(FLAT, Root=((0, 0, 0), (0, 0.12, -0.18))))]),
    "Pounce": ("BEZIER", [(1, FLAT), (6, STRETCHED), (12, with_(STRETCHED, Jaw=(0, 0, 0), Root=((0, 0, 0), (0, -0.2, 0.05))))]),
    "PounceRecover": ("BEZIER", [(1, with_(STRETCHED, Jaw=(0, 0, 0), Root=((0, 0, 0), (0, -0.2, 0.0)))), (8, CROUCH), (21, STAND)]),
    "RunDown": ("BEZIER", [(1, STAND), (12, with_(STAND, Head=(30, 0, 0), Neck=(14, 0, 0))), (30, SAGGED)]),
    "Wind": ("BEZIER", [(1, SAGGED), (10, SAT), (20, with_(SAT, Body=(-30, 0, 2))), (30, SAT), (40, with_(SAT, Body=(-30, 0, -2))),
                        (50, SAT), (60, with_(SAT, Head=(10, 0, 0))), (72, STAND)]),
    "Stagger": ("BEZIER", [(1, STAND), (5, {"Body": (-10, 0, 8), "Head": (-20, 0, 0), "Root": ((0, 0, 0), (0, 0.15, 0.02))}), (14, STAND)]),
    "Death": ("BEZIER", [
        (1, STAND),
        (6, {"Head": (-20, 0, 0), "Root": ((0, 0, 0), (0, 0.1, 0.03))}),
        (18, {"Root": ((0, 75, 0), (0.25, 0, 0.05)), "Head": (20, 0, 0), "LegFL": (20, 0, 0), "LegBL": (-20, 0, 0)}),
        (24, {"Root": ((0, 85, 0), (0.3, 0, 0.0)), "Head": (24, 0, 0), "LegFL": (24, 0, 0), "LegBL": (-24, 0, 0)}),
    ]),
}


def build():
    fur, fur_dark = kit.mat("WoundFur", "#b39670"), kit.mat("WoundFurDark", "#7d6547")  # (fawn: seen against the grey stone)
    brass, brass_web = kit.mat("WoundBrass_Metal", "#c99a45"), kit.mat("WoundBrassWeb_Metal", "#9c7a38")
    iron, eye = kit.mat("WoundIron_Metal", "#3a383f"), kit.mat("SouredEye_Glow", "#c8f07a")
    nose = kit.mat("WoundNose", "#1b1a1d")

    parts = [
        # The body: a deep chest, a tucked waist, haunches.
        part("Chest", kit.sphere(10, 8), fur, M((0, -0.2, BACK - 0.05), scale=(0.15, 0.2, 0.19)), smooth=True, group="Body"),
        part("Waist", kit.sphere(10, 8), fur, M((0, 0.1, BACK - 0.02), scale=(0.1, 0.22, 0.11)), smooth=True, group="Body"),
        part("Haunch", kit.sphere(10, 8), fur, M((0, 0.32, BACK - 0.04), scale=(0.13, 0.14, 0.14)), smooth=True, group="Body"),
        part("Spine", kit.cube(), fur_dark, M((0, 0.04, BACK + 0.1), scale=(0.05, 0.62, 0.03)), group="Body"),
        part("Neck", kit.cone(8, 0.08, 0.06, 0.3), fur, kit.along((0, -0.33, BACK + 0.02), (0, -0.52, BACK + 0.24)), smooth=True, group="Neck"),
        part("Collar", kit.cylinder(10, 0.085, 0.04), brass, kit.along((0, -0.38, BACK + 0.07), (0, -0.4, BACK + 0.1)), group="Neck"),
        # The head: long and narrow, the snout tapering, ears laid back.
        part("Skull", kit.sphere(9, 7), fur, M((0, -0.55, BACK + 0.22), scale=(0.08, 0.11, 0.08)), smooth=True, group="Head"),
        part("Snout", kit.cone(8, 0.06, 0.025, 0.24), fur, kit.along((0, -0.6, BACK + 0.2), (0, -0.84, BACK + 0.15)), smooth=True, group="Head"),
        part("Nose", kit.sphere(6, 4, 0.025), nose, M((0, -0.85, BACK + 0.15)), group="Head"),
        part("LowerJaw", kit.cone(6, 0.035, 0.015, 0.22), fur_dark, kit.along((0, -0.58, BACK + 0.14), (0, -0.8, BACK + 0.1)), group="Jaw"),
    ]
    for s in (-1, 1):
        parts.append(part("Eye", kit.sphere(6, 4, 0.02), eye, M((s * 0.045, -0.62, BACK + 0.26)), group="Head"))
        parts.append(part("Ear", kit.cone(4, 0.04, 0.005, 0.12), fur_dark, kit.along((s * 0.04, -0.5, BACK + 0.28), (s * 0.07, -0.4, BACK + 0.31)), group="Head"))
        # Its flanks opened on brass: a wheel where the ribs were, a smaller one meshing behind it.
        parts += [
            part("RibWheel", kit.cylinder(14, 0.11, 0.03), brass, M((s * 0.135, -0.14, BACK - 0.04), rot=(0, 90, 0)), group="Body"),
            part("RibWheelWeb", kit.cylinder(14, 0.08, 0.035), brass_web, M((s * 0.135, -0.14, BACK - 0.04), rot=(0, 90, 0)), group="Body"),
            part("Hub", kit.cylinder(8, 0.025, 0.05), iron, M((s * 0.14, -0.14, BACK - 0.04), rot=(0, 90, 0)), group="Body"),
            part("SmallWheel", kit.cylinder(10, 0.06, 0.03), brass, M((s * 0.12, 0.06, BACK - 0.02), rot=(0, 90, 0)), group="Body"),
        ]
        for k in range(10):
            a = 2 * math.pi * k / 10
            parts.append(part("Cog", kit.cube(), brass, M((s * 0.135, -0.14 + math.cos(a) * 0.12, BACK - 0.04 + math.sin(a) * 0.12),
                                                          rot=(math.degrees(a), 0, 0), scale=(0.03, 0.03, 0.03)), group="Body"))
    # The legs: iron, thin, a brass knuckle at each joint, paws.
    for bone, (x, y0, y1) in (("LegFL", (-0.09, -0.27, -0.3)), ("LegFR", (0.09, -0.27, -0.3)),
                              ("LegBL", (-0.09, 0.33, 0.38)), ("LegBR", (0.09, 0.33, 0.38))):
        top, knee, foot = (x, y0, BACK - 0.1), (x, (y0 + y1) / 2 + (0.03 if y0 > 0 else -0.02), 0.3), (x, y1, 0.04)
        parts += [
            part("Thigh", kit.cone(6, 0.045, 0.03, math.dist(top, knee)), fur if y0 > 0 else iron, kit.along(top, knee), smooth=True, group=bone),
            part("Shin", kit.cylinder(6, 0.022, math.dist(knee, foot)), iron, kit.along(knee, foot), group=bone),
            part("Knuckle", kit.sphere(6, 4, 0.032), brass, M(knee), group=bone),
            part("Paw", kit.sphere(6, 4), iron, M((x, y1 - 0.03, 0.03), scale=(0.04, 0.06, 0.03)), group=bone),
        ]
    # The tail: thin, curving down.
    tail = [(0, 0.42, BACK - 0.02), (0, 0.6, BACK - 0.04), (0, 0.78, BACK - 0.14)]
    parts.append(part("Tail", kit.tube(tail, [0.025, 0.018, 0.01], seg=5), fur_dark, group="Tail"))
    # The key in its back: a shaft and two round wings, a toy's.
    kx, ky, kz = 0, 0.04, BACK + 0.1
    parts += [
        part("KeyShaft", kit.cylinder(8, 0.02, 0.24), brass, M((kx, ky, kz + 0.1)), group="Key"),
        part("KeyCollar", kit.cylinder(8, 0.04, 0.03), brass, M((kx, ky, kz + 0.02)), group="Key"),
    ]
    for s in (-1, 1):
        ring = [(kx + s * (0.09 + 0.07 * math.cos(a)), ky, kz + 0.26 + 0.07 * math.sin(a)) for a in (2 * math.pi * k / 14 for k in range(15))]
        parts.append(part("KeyWing", kit.tube(ring, [0.016] * len(ring), seg=5), brass, group="Key"))
    # The lantern hung from its collar, like every Soured's.
    lantern_glass = kit.mat("SouredLantern_Glow", "#b6f06a")
    parts += [
        part("BeltLanternTop", kit.cone(4, 0.045, 0.01, 0.04), iron, M((0, -0.44, BACK - 0.02), rot=(0, 0, 45)), group="Neck"),
        part("BeltLanternGlass", kit.cube(), lantern_glass, M((0, -0.44, BACK - 0.08), scale=(0.06, 0.06, 0.08)), group="Neck"),
        part("BeltLanternBase", kit.cube(), iron, M((0, -0.44, BACK - 0.125), scale=(0.075, 0.075, 0.012)), group="Neck"),
    ]
    mesh = kit.join(parts, "Wound")
    armature = kit.rig("WoundRig", BONES)
    kit.skin(mesh, armature)
    kit.animate(armature, ACTIONS)
    return [armature, mesh]
