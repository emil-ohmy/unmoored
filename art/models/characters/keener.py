"""A Keener (the Hall of the Last Hour): one of the mourners who stopped the Hall's clocks at the minute of a death, and
never let the grief go. A figure in black crape to the floor (a wide bell of a skirt, a crape overskirt to the knee), a
weeping veil over the whole face so that only the Soured's green eyes show through it, its long fall down the back;
white weepers (mourning cuffs) at the wrists. In its left hand a silver watch, stopped at the minute (its hands at
twenty past four), a loop of chain hanging from it; in its right a white handkerchief. A small green lantern at its
belt, like every Soured's.

Clips (30 fps): Idle (3 s: head bowed over the watch held to its chest, swaying), Walk (2 s: a glide, its skirts
swinging), Breath (1.2 s: the knell's tell: it rises, head thrown back, the watch lifted high, the other arm flung
out), Toll (1.2 s: the knell: thrown forward, the watch held out, a shudder at each of its three rings, at 0.18 s,
0.38 s and 0.58 s, then held), Recover (1.4 s: spent, bowed low, arms hanging, then back to the watch), Stagger, Death (a last look up,
then it folds down into its crape and falls forward).
"""
import math

M, part = kit.M, kit.part  # `kit` is injected by build_model.py

SHOULDER = 1.42
HIP = 0.95
HAND_L, HAND_R = (-0.25, -0.02, 0.86), (0.25, -0.02, 0.86)
BONES = {
    "Root": ((0, 0, 0), (0, 0, 0.3), None),
    "Skirt": ((0, 0, HIP), (0, 0, 0.1), "Root"),
    "Spine": ((0, 0, HIP), (0, 0, SHOULDER), "Root"),
    "Head": ((0, 0, SHOULDER), (0, -0.02, 1.8), "Spine"),
    "ArmL": ((-0.21, 0, SHOULDER - 0.04), HAND_L, "Spine"),
    "ArmR": ((0.21, 0, SHOULDER - 0.04), HAND_R, "Spine"),
}


def with_(pose, **changes):
    merged = dict(pose)
    merged.update(changes)
    return merged


# Bowed over the watch, both hands at the chest.
MOURN = {"Head": (24, 0, 0), "Spine": (6, 0, 0), "ArmL": (-42, 0, 28), "ArmR": (-38, 0, -24)}
MOURN_SWAY_L = with_(MOURN, Spine=(7, 0, 4), Skirt=(0, -2, 0), Head=(26, 0, 6))
MOURN_SWAY_R = with_(MOURN, Spine=(7, 0, -4), Skirt=(0, 2, 0), Head=(26, 0, -6))
GLIDE_L = with_(MOURN, Spine=(9, 0, 3), Skirt=(-7, 0, 2), Root=((0, 0, 0), (0, 0, 0.02)))
GLIDE_M = with_(MOURN, Spine=(8, 0, 0), Skirt=(0, 0, 0), Root=((0, 0, 0), (0, 0, -0.01)))
GLIDE_R = with_(MOURN, Spine=(9, 0, -3), Skirt=(7, 0, -2), Root=((0, 0, 0), (0, 0, 0.02)))
# The breath: risen, head thrown back, the watch high, the other arm flung wide.
BREATH_IN = {"Root": ((0, 0, 0), (0, 0, 0.04)), "Spine": (-14, 0, 0), "Head": (-28, 0, 0), "Skirt": (4, 0, 0),
             "ArmL": (-150, 18, 0), "ArmR": (-40, -70, 0)}
BREATH_FULL = with_(BREATH_IN, Spine=(-18, 0, 0), Head=(-34, 0, 0), ArmL=(-160, 14, 0), ArmR=(-50, -78, 0),
                    Root=((0, 0, 0), (0, 0, 0.06)))
# The knell: thrown forward, the watch held out at you, the handkerchief pressed to the veil.
WAIL = {"Spine": (24, 0, 0), "Head": (20, 0, 0), "Skirt": (-6, 0, 0), "ArmL": (-118, 0, 10), "ArmR": (-112, 0, -48),
        "Root": ((0, 0, 0), (0, -0.05, -0.02))}
WAIL_DEEP = with_(WAIL, Spine=(32, 0, 0), Head=(26, 0, 0), ArmL=(-124, 0, 8), ArmR=(-120, 0, -50),
                  Root=((0, 0, 0), (0, -0.07, -0.04)))
# Spent: bowed low, arms hanging.
SPENT = {"Spine": (34, 0, 0), "Head": (36, 0, 0), "Skirt": (-4, 0, 0), "ArmL": (-14, 0, 6), "ArmR": (-10, 0, -6),
         "Root": ((0, 0, 0), (0, -0.04, -0.04))}

ACTIONS = {
    "Idle": ("BEZIER", [(1, MOURN_SWAY_L), (46, MOURN_SWAY_R), (91, MOURN_SWAY_L)]),
    "Walk": ("BEZIER", [(1, GLIDE_L), (16, GLIDE_M), (31, GLIDE_R), (46, GLIDE_M), (61, GLIDE_L)]),
    "Breath": ("BEZIER", [(1, MOURN), (14, with_(BREATH_IN, Head=(-10, 0, 0))), (30, BREATH_FULL), (37, BREATH_FULL)]),
    # A shudder at each ring of the band: frames 6, 12, 18 (0.18 s, 0.38 s, 0.58 s); then held, keening.
    "Toll": ("BEZIER", [(1, BREATH_FULL), (6, WAIL_DEEP), (9, WAIL), (12, WAIL_DEEP), (15, WAIL), (18, WAIL_DEEP),
                        (28, with_(WAIL_DEEP, Spine=(34, 0, 0))), (37, WAIL_DEEP)]),
    "Recover": ("BEZIER", [(1, WAIL_DEEP), (12, SPENT), (30, with_(SPENT, Spine=(30, 0, 0))), (43, MOURN)]),
    "Stagger": ("BEZIER", [(1, MOURN), (5, {"Spine": (-20, 0, 10), "Head": (-14, 0, 0), "ArmL": (-60, 0, 30),
                                            "ArmR": (-20, -40, 0), "Skirt": (8, 0, 0), "Root": ((0, 0, 0), (0, 0.12, 0))}),
                           (18, MOURN)]),
    "Death": ("BEZIER", [
        (1, MOURN),
        (8, with_(BREATH_IN, ArmR=(-20, -30, 0))),
        (18, with_(SPENT, Root=((0, 0, 0), (0, -0.05, -0.3)), Skirt=(0, 0, 0))),
        (30, {"Root": ((80, 0, 0), (0, -0.3, -0.45)), "Spine": (10, 0, 0), "Head": (20, 0, 0), "ArmL": (-170, 0, 10),
              "ArmR": (-160, -10, 0), "Skirt": (-30, 0, 0)}),
    ]),
}


def build():
    crape, crape_sheen = kit.mat("KeenerCrape", "#1d1b21"), kit.mat("KeenerCrapeSheen", "#2e2b35")
    weeper, linen = kit.mat("KeenerWeeper", "#e8e4dc"), kit.mat("KeenerHandkerchief", "#ece8df")
    glove = kit.mat("KeenerGlove", "#141317")
    silver, face = kit.mat("KeenerSilver_Metal", "#c4c8cc"), kit.mat("KeenerWatchFace", "#efe9d8")
    black, eye = kit.mat("KeenerWatchHand", "#141316"), kit.mat("SouredEye_Glow", "#c8f07a")
    iron = kit.mat("KeenerIron", "#2f2d33")

    parts = []
    # The skirts: a wide bell of crape to the floor, a crape overskirt to the knee, a dark hem.
    parts += [
        part("Skirt", kit.cone(12, 0.42, 0.19, HIP - 0.02), crape, M((0, 0.02, HIP / 2 + 0.01), scale=(1, 0.92, 1)), smooth=True, group="Skirt"),
        part("Overskirt", kit.cone(12, 0.36, 0.21, 0.42), crape_sheen, M((0, 0.01, HIP - 0.2), scale=(1, 0.92, 1)), smooth=True, group="Skirt"),
        part("Hem", kit.cylinder(12, 0.43, 0.05), crape_sheen, M((0, 0.02, 0.035), scale=(1, 0.92, 1)), smooth=True, group="Skirt"),
    ]
    # The bodice, buttoned high; a white band at the throat.
    parts += [
        part("Bodice", kit.cone(10, 0.2, 0.22, SHOULDER - HIP + 0.04), crape, M((0, 0.0, (HIP + SHOULDER) / 2), scale=(1, 0.74, 1)), smooth=True, group="Spine"),
        part("Waist", kit.cylinder(10, 0.2, 0.06), crape_sheen, M((0, 0.0, HIP + 0.03), scale=(1, 0.78, 1)), smooth=True, group="Spine"),
        part("Shoulders", kit.sphere(10, 6), crape, M((0, 0, SHOULDER - 0.05), scale=(0.27, 0.17, 0.09)), smooth=True, group="Spine"),
        part("Band", kit.cylinder(9, 0.075, 0.04), weeper, M((0, -0.005, SHOULDER + 0.03)), smooth=True, group="Spine"),
        # The veil's fall: down the back from the head to the waist.
        part("VeilFall", kit.cone(9, 0.25, 0.14, SHOULDER - HIP + 0.1), crape_sheen, M((0, 0.07, (HIP + SHOULDER) / 2 + 0.1), scale=(1, 0.6, 1)), smooth=True, group="Spine"),
    ]
    # The head under its weeping veil: no face, the eyes' green through the crape; a small round cap at the crown.
    parts += [
        part("Veil", kit.cone(10, 0.2, 0.09, 0.32), crape_sheen, M((0, -0.01, 1.6)), smooth=True, group="Head"),
        part("VeilCrown", kit.sphere(10, 7, 0.135), crape, M((0, -0.02, 1.74), scale=(1.0, 1.0, 1.05)), smooth=True, group="Head"),
        part("Cap", kit.cylinder(10, 0.1, 0.05), crape_sheen, M((0, 0.0, 1.86)), smooth=True, group="Head"),
    ]
    for s in (-1, 1):
        parts.append(part("Eye", kit.sphere(8, 6, 0.024), eye, M((s * 0.05, -0.15, 1.72), scale=(1.2, 0.6, 0.8)), smooth=True, group="Head"))
    # Arms in crape sleeves, the white weepers at the wrists, black gloves.
    for s, bone, hand in ((-1, "ArmL", HAND_L), (1, "ArmR", HAND_R)):
        shoulder = (s * 0.21, 0, SHOULDER - 0.04)
        wrist = (hand[0], hand[1], hand[2] + 0.08)
        parts.append(part("Sleeve", kit.cone(7, 0.07, 0.055, math.dist(shoulder, wrist)), crape, kit.along(wrist, shoulder), smooth=True, group=bone))
        parts.append(part("Weeper", kit.cone(8, 0.075, 0.06, 0.12), weeper, kit.along(hand, (hand[0], hand[1], hand[2] + 0.13)), smooth=True, group=bone))
        parts.append(part("Glove", kit.sphere(7, 5, 0.055), glove, M((hand[0], hand[1] - 0.01, hand[2] - 0.04)), smooth=True, group=bone))
    # The stopped watch, in the left hand, held out: a silver case, a pale face, its hands at twenty past four; the
    # chain looped down from it.
    wx, wy, wz = HAND_L[0], HAND_L[1] - 0.08, HAND_L[2] - 0.06
    parts += [
        part("WatchCase", kit.cylinder(16, 0.075, 0.035), silver, M((wx, wy + 0.01, wz), rot=(90, 0, 0)), smooth=True, group="ArmL"),
        part("WatchFace", kit.cylinder(16, 0.062, 0.01), face, M((wx, wy - 0.005, wz), rot=(90, 0, 0)), group="ArmL"),
        part("WatchBow", kit.cylinder(8, 0.018, 0.025), silver, M((wx, wy + 0.01, wz + 0.088)), group="ArmL"),
    ]
    for angle, length in ((-120, 0.035), (110, 0.05)):  # the hour hand towards four, the minute hand at twenty past
        a = math.radians(angle)
        parts.append(part("WatchHand", kit.cube(), black, M((wx + math.sin(a) * length / 2, wy - 0.012, wz + math.cos(a) * length / 2),
                                                           rot=(0, -angle, 0), scale=(0.008, 0.004, length)), group="ArmL"))
    chain = [(wx + 0.01 * math.sin(t * math.pi), wy + 0.01, wz + 0.09 - 0.22 * math.sin(t * math.pi / 2) ** 0.8) for t in (k / 8 for k in range(9))]
    parts.append(part("WatchChain", kit.tube(chain, [0.008] * len(chain), seg=4), silver, group="ArmL"))
    # The handkerchief, hanging from the right hand.
    parts.append(part("Handkerchief", kit.cube(), linen, M((HAND_R[0] + 0.01, HAND_R[1] - 0.03, HAND_R[2] - 0.17), rot=(4, 0, 8), scale=(0.11, 0.015, 0.22)), group="ArmR"))
    # A lantern hung from its belt, like every Soured's.
    lantern_glass = kit.mat("SouredLantern_Glow", "#b6f06a")
    parts += [
        part("BeltLanternTop", kit.cone(4, 0.06, 0.01, 0.05), iron, M((-0.24, -0.04, 0.92), rot=(0, 0, 45)), group="Spine"),
        part("BeltLanternGlass", kit.cube(), lantern_glass, M((-0.24, -0.04, 0.85), scale=(0.08, 0.08, 0.1)), group="Spine"),
        part("BeltLanternBase", kit.cube(), iron, M((-0.24, -0.04, 0.795), scale=(0.1, 0.1, 0.015)), group="Spine"),
    ]
    mesh = kit.join(parts, "Keener")
    armature = kit.rig("KeenerRig", BONES)
    kit.skin(mesh, armature)
    kit.animate(armature, ACTIONS)
    return [armature, mesh]
