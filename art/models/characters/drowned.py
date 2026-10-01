"""The Drowned: a Soured that couldn't let go of life. It waits under the water at the edges, and rises to drag the
living down with it. Seen from the waist up (its origin is at the waterline: below it, the rags of its clothes trail
off into the dark): bloated and grey-green, long lank hair like waterweed hanging over its face, a mouth hanging open,
arms too long, pale swollen hands. The same sickly green eyes as every Soured, glowing through the hair, and its belt
lantern, drowned, still faintly lit.

Clips: Idle (lurking, a slow sway), Swim (gliding under, arms trailing), Rise (0.85 s telegraph: bursting up, arms
flung high, the head drawn back, gathering), Spit (0.3 s: the head thrust forward, arms flung back), Clutch (loop: up at
the edge, hanging on and looking about; after a spit, and at the shore), Sink (0.6 s: back under), Stranded (loop:
hooked out onto dry ground, flopping on its belly), Crawl (loop: hauling itself back to the water), Stagger, Death.
"""
import math

M, part = kit.M, kit.part  # `kit` is injected by build_model.py

SHOULDER_Z = 0.66
BONES = {
    "Root": ((0, 0, 0), (0, 0, 0.2), None),
    "Pivot": ((0, 0, 0.05), (0, 0, 0.15), "Root"),  # whole-body lunges and flops turn about the waterline
    "Spine": ((0, 0, 0.05), (0, -0.04, SHOULDER_Z), "Pivot"),
    "Head": ((0, -0.04, SHOULDER_Z), (0, -0.08, 1.02), "Spine"),
    "ArmL": ((-0.24, -0.02, SHOULDER_Z), (-0.3, -0.06, 0.02), "Spine"),
    "ArmR": ((0.24, -0.02, SHOULDER_Z), (0.3, -0.06, 0.02), "Spine"),
}


def with_(pose, **changes):
    merged = dict(pose)
    merged.update(changes)
    return merged


REST = {"Spine": (8, 0, 0), "Head": (6, 0, 0), "ArmL": (-8, 0, -6), "ArmR": (-8, 0, 6)}
# Arms hang at its sides: -X pitch raises one forward, +X swings it back. +X on Pivot leans the whole body forward.
LURK = {"Pivot": (12, 0, 0), "Spine": (14, 0, 0), "Head": (10, 0, 0), "ArmL": (-18, 0, -10), "ArmR": (-18, 0, 10)}
RISEN = {"Pivot": (-10, 0, 0), "Spine": (-8, 0, 0), "Head": (-26, 0, 0), "ArmL": ((-165, 0, -18), (0, 0, 0.04)),
         "ArmR": ((-165, 0, 18), (0, 0, 0.04)), "Root": ((0, 0, 0), (0, 0, 0.08))}
CLUTCH = {"Pivot": (62, 0, 0), "Spine": (10, 0, 0), "Head": (-34, 0, 0), "ArmL": ((-80, 0, -30), (0, -0.04, 0)),
          "ArmR": ((-80, 0, 30), (0, -0.04, 0)), "Root": ((0, 0, 0), (0, -0.3, 0.0))}
LYING = {"Pivot": (82, 0, 0), "Spine": (4, 0, 0), "Head": (-40, 0, 0), "ArmL": (-60, 0, -40), "ArmR": (-60, 0, 40),
         "Root": ((0, 0, 0), (0, 0, 0.12))}

ACTIONS = {
    "Idle": ("BEZIER", [
        (1, LURK),
        (24, with_(LURK, Spine=(18, 0, 4), Head=(14, 0, -8), ArmL=(-10, 0, -14), ArmR=(-24, 0, 8))),
        (48, LURK),
    ]),
    "Swim": ("BEZIER", [
        (1, {"Pivot": (40, 0, 0), "Spine": (10, 0, 0), "Head": (-20, 0, 0), "ArmL": (30, 0, -10), "ArmR": (20, 0, 10)}),
        (12, {"Pivot": (40, 0, 4), "Spine": (10, 0, 0), "Head": (-20, 0, 0), "ArmL": (18, 0, -10), "ArmR": (34, 0, 10)}),
        (24, {"Pivot": (40, 0, 0), "Spine": (10, 0, 0), "Head": (-20, 0, 0), "ArmL": (30, 0, -10), "ArmR": (20, 0, 10)}),
    ]),
    "Rise": ("BEZIER", [(1, LURK), (6, with_(LURK, Pivot=(24, 0, 0), Root=((0, 0, 0), (0, 0, -0.1)))), (12, RISEN),
                        (17, with_(RISEN, Head=(-32, 0, 0)))]),
    "Spit": ("BEZIER", [(1, RISEN), (3, {"Pivot": (26, 0, 0), "Spine": (18, 0, 0), "Head": (30, 0, 0), "ArmL": (40, 0, -30),
                                        "ArmR": (40, 0, 30), "Root": ((0, 0, 0), (0, -0.08, 0.04))}),
                        (8, with_(LURK, Pivot=(4, 0, 0), Head=(0, 0, 0)))]),
    "Clutch": ("BEZIER", [
        (1, CLUTCH),
        (9, with_(CLUTCH, ArmL=((-92, 0, -24), (0, -0.08, 0)), Head=(-40, 0, 6))),
        (18, with_(CLUTCH, ArmR=((-92, 0, 24), (0, -0.08, 0)), Head=(-38, 0, -6))),
        (27, with_(CLUTCH, ArmL=((-92, 0, -24), (0, -0.08, 0)), Head=(-40, 0, 4))),
        (36, CLUTCH),
    ]),
    "Sink": ("BEZIER", [(1, CLUTCH), (8, with_(LURK, Head=(20, 0, 0))), (15, LURK)]),
    "Stranded": ("BEZIER", [
        (1, LYING),
        (6, with_(LYING, Pivot=(76, 0, 8), ArmL=(-100, 0, -30), ArmR=(-30, 0, 50), Head=(-48, 0, 0))),
        (12, with_(LYING, Pivot=(76, 0, -8), ArmL=(-30, 0, -50), ArmR=(-100, 0, 30), Head=(-48, 0, 0))),
        (18, LYING),
    ]),
    "Crawl": ("BEZIER", [
        (1, with_(LYING, ArmL=(-130, 0, -12), ArmR=(-20, 0, 20))),
        (10, with_(LYING, ArmL=(-20, 0, -20), ArmR=(-130, 0, 12), Root=((0, 0, 0), (0, -0.05, 0.12)))),
        (19, with_(LYING, ArmL=(-130, 0, -12), ArmR=(-20, 0, 20))),
    ]),
    "Stagger": ("BEZIER", [(1, REST), (4, {"Pivot": (-18, 0, 8), "Spine": (-10, 0, 0), "Head": (-30, 0, 10), "ArmL": (-60, 0, -40),
                                          "ArmR": (-40, 0, 50)}), (18, REST)]),
    "Death": ("BEZIER", [(1, REST), (5, {"Pivot": (-24, 0, 0), "Head": (-40, 0, 0), "ArmL": (-150, 0, -20), "ArmR": (-150, 0, 20)}),
                         (20, {"Pivot": (-60, 0, 10), "Head": (-20, 0, 0), "ArmL": (-120, 0, -50), "ArmR": (-120, 0, 50),
                               "Root": ((0, 0, 0), (0, 0.2, -0.4))})]),
}


def build():
    skin, dark = kit.mat("DrownedSkin", "#7d8c82"), kit.mat("DrownedDark", "#4f5d56")
    rags, weed = kit.mat("DrownedRags", "#3e4a45"), kit.mat("DrownedWeed", "#2c3a2a")
    mouth, eye, hand = kit.mat("Ink", "#1a1622"), kit.mat("SouredEye_Glow", "#b6f06a"), kit.mat("DrownedHand", "#a7b3a6")
    metal = kit.mat("SouredLanternMetal", "#2c2b31")
    rng = kit.rng

    def bloated(bm):
        kit.sphere(10, 8)(bm)
        for v in bm.verts:
            v.co *= 1.0 + rng.uniform(-0.04, 0.05)

    def rag_skirt(bm):
        kit.cone(10, 0.3, 0.2, 0.9)(bm)
        for v in bm.verts:
            if v.co.z < -0.3:
                v.co.x *= 1 + rng.uniform(-0.15, 0.15)
                v.co.y *= 1 + rng.uniform(-0.15, 0.15)
                v.co.z += rng.uniform(-0.1, 0.08)

    parts = [
        part("Rags", rag_skirt, rags, M((0, 0, -0.42)), smooth=True, group="Pivot"),
        part("Torso", bloated, skin, M((0, -0.01, 0.36), scale=(0.29, 0.25, 0.36)), smooth=True, group="Spine"),
        part("Shoulders", bloated, dark, M((0, 0.03, 0.6), scale=(0.3, 0.17, 0.1)), smooth=True, group="Spine"),
        part("Neck", kit.cylinder(8, 0.08, 0.14), dark, M((0, -0.04, 0.7)), smooth=True, group="Head"),
        part("Head", bloated, skin, M((0, -0.07, 0.84), scale=(0.15, 0.15, 0.17)), smooth=True, group="Head"),
        part("Mouth", kit.sphere(8, 6), mouth, M((0, -0.2, 0.78), scale=(0.05, 0.03, 0.07)), smooth=True, group="Head"),
    ]
    for s in (-1, 1):
        parts.append(part("Eye", kit.sphere(8, 6, 0.024), eye, M((s * 0.055, -0.205, 0.87)), smooth=True, group="Head"))
    # Long lank hair, like waterweed: strands from the crown, hanging over the face and down the back.
    parts.append(part("Scalp", kit.sphere(10, 7), weed, M((0, -0.05, 0.9), scale=(0.155, 0.155, 0.12)), smooth=True, group="Head"))
    for k in range(22):
        a = math.pi * 2 * k / 22
        front = math.sin(a) < -0.3
        top = (0.11 * math.cos(a), -0.06 + 0.11 * math.sin(a), 0.95)
        drop = 0.4 if front else 0.62  # (a little shorter over the face, parted: the eyes glow through)
        side = 0.035 if front else 0.0
        mid = (0.16 * math.cos(a) + math.copysign(side, math.cos(a)), -0.07 + 0.17 * math.sin(a), 0.9 - drop * 0.35)
        end = (0.15 * math.cos(a) + rng.uniform(-0.03, 0.03), -0.07 + 0.19 * math.sin(a) + rng.uniform(-0.02, 0.02), 0.95 - drop)
        parts.append(part("Weed", kit.tube([top, mid, end], [0.034, 0.028, 0.012], seg=5), weed, group="Head"))
    for s, bone in ((-1, "ArmL"), (1, "ArmR")):
        shoulder, elbow, wrist = (s * 0.24, -0.02, SHOULDER_Z), (s * 0.28, -0.05, 0.34), (s * 0.3, -0.06, 0.06)
        parts.append(part("Arm", kit.tube([shoulder, elbow, wrist], [0.07, 0.055, 0.045], seg=7), skin, group=bone))
        parts.append(part("Hand", bloated, hand, M((s * 0.3, -0.07, 0.0), scale=(0.07, 0.05, 0.09)), smooth=True, group=bone))
        for k in (-1, 0, 1):
            base = (s * 0.3 + k * 0.03, -0.08, -0.06)
            parts.append(part("Finger", kit.cone(4, 0.016, 0.006, 0.11), hand, kit.along(base, (base[0], base[1] - 0.02, base[2] - 0.1)), group=bone))
    # Its belt lantern, drowned: still faintly lit, at the waterline.
    lantern_glass = kit.mat("SouredLantern_Glow", "#b6f06a")
    parts += [
        part("BeltLanternCord", kit.cube(), metal, M((0.24, 0.0, 0.14), rot=(0, -15, 0), scale=(0.012, 0.012, 0.1)), group="Spine"),
        part("BeltLanternTop", kit.cone(4, 0.06, 0.01, 0.05), metal, M((0.25, 0.0, 0.07), rot=(0, 0, 45)), group="Spine"),
        part("BeltLanternGlass", kit.cube(), lantern_glass, M((0.25, 0.0, 0.0), scale=(0.075, 0.075, 0.1)), group="Spine"),
        part("BeltLanternBase", kit.cube(), metal, M((0.25, 0.0, -0.06), scale=(0.09, 0.09, 0.015)), group="Spine"),
    ]
    mesh = kit.join(parts, "Drowned")
    armature = kit.rig("DrownedRig", BONES)
    kit.skin(mesh, armature)
    kit.animate(armature, ACTIONS)
    return [armature, mesh]
