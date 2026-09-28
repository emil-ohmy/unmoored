"""The Laden: a Soured who couldn't let go of things. Clutches a towering heap of belongings in front of it,
which is its shield (blocks frontal hits) and its weapon (lifts it overhead, slams it down).

Clips: Idle and Walk (open: heap low on the hip, face showing), GuardUp (0.35 s: heap lifted in front of the
face), GuardWalk (braced shuffle),
Block (0.3 s recoil when a frontal hit is blocked), Windup (0.9 s telegraph: heap raised high), Strike (0.3 s slam),
Recover (1.2 s, heap on the ground: the punish window), Stagger.
"""
import math

M, part = kit.M, kit.part  # `kit` is injected by build_model.py

BONES = {
    "Root": ((0, 0, 0), (0, 0, 0.35), None),
    "Spine": ((0, 0, 0.35), (0, -0.12, 1.15), "Root"),
    "Head": ((0, -0.12, 1.15), (0, -0.26, 1.45), "Spine"),
    "Heap": ((0, -0.55, 0.7), (0, -0.55, 1.4), "Spine"),  # the arms are weighted here, so they carry it
}

# Guard states, readable from the game camera:
# OPEN  (unaware): heap lowered and tipped away, face and eyes showing above it. Does not block.
# GUARD (alert):   heap lifted in front of the face. Blocks frontal hits.
# OPEN is carried low on one hip (asymmetric, relaxed); GUARD is planted and braced, heap high over the face.
OPEN = {"Spine": (8, 0, -6), "Head": (-6, 0, 0), "Heap": ((20, 0, -14), (0.14, 0.1, -0.5))}
GUARD = {"Spine": (-6, 0, 0), "Head": (14, 0, 0), "Heap": ((-8, 0, 0), (0, -0.06, 0.36)), "Root": ((0, 0, 0), (0, 0, -0.05))}
HEAP_UP = ((-20, 0, 0), (0, 0.1, 0.6))
HEAP_DOWN = ((35, 0, 0), (0, -0.3, -0.55))
LIFTED = {"Spine": (-20, 0, 0), "Head": (-16, 0, 0), "Heap": ((-22, 0, 0), (0, 0.12, 0.62)), "Root": ((0, 0, 0), (0, 0.09, 0.06))}
SLAMMED = {"Spine": (32, 0, 0), "Head": (14, 0, 0), "Heap": ((36, 0, 0), (0, -0.32, -0.57)), "Root": ((0, 0, 0), (0, -0.32, -0.09))}


def with_(pose, **changes):
    merged = dict(pose)
    merged.update(changes)
    return merged


ACTIONS = {
    "Idle": ("BEZIER", [
        (1, OPEN),
        (24, with_(OPEN, Spine=(11, 0, 0), Heap=((20, 0, 0), (0, 0.08, -0.48)), Root=((0, 0, 0), (0, 0, -0.02)))),
        (48, OPEN),
    ]),
    "Walk": ("BEZIER", [  # approaching, open: a lumbering walk with the heap low on the hip
        (1, with_(OPEN, Spine=(8, 6, -6))),
        (9, with_(OPEN, Root=((0, 0, 0), (0, 0, 0.04)))),
        (17, with_(OPEN, Spine=(8, -6, -6))),
        (25, with_(OPEN, Root=((0, 0, 0), (0, 0, 0.04)))),
        (33, with_(OPEN, Spine=(8, 6, -6))),
    ]),
    "GuardUp": ("BEZIER", [  # 0.35 s: the heap comes up in front of the face
        (1, OPEN),
        (8, with_(GUARD, Heap=((-10, 0, 0), (0, 0.0, 0.26)))),
        (11, GUARD),
    ]),
    "GuardWalk": ("BEZIER", [  # braced shuffle behind the raised heap
        (1, with_(GUARD, Spine=(-6, 3, 0))),
        (17, with_(GUARD, Root=((0, 0, 0), (0, 0, -0.03)))),
        (33, with_(GUARD, Spine=(-6, -3, 0))),
        (49, with_(GUARD, Root=((0, 0, 0), (0, 0, -0.03)))),
        (65, with_(GUARD, Spine=(-6, 3, 0))),
    ]),
    "Block": ("BEZIER", [  # a blocked hit shoves the heap back into its face
        (1, GUARD),
        (3, with_(GUARD, Spine=(-12, 0, 0), Heap=((-16, 0, 0), (0, 0.18, 0.24)), Root=((0, 0, 0), (0, 0.06, 0)))),
        (10, GUARD),
    ]),
    "Windup": ("BEZIER", [
        (1, GUARD),
        (8, {"Spine": (10, 0, 0), "Heap": ((5, 0, 0), (0, 0, -0.1)), "Root": ((0, 0, 0), (0, 0, -0.06))}),
        (24, {"Spine": (-18, 0, 0), "Head": (-15, 0, 0), "Heap": HEAP_UP, "Root": ((0, 0, 0), (0, 0.08, 0.05))}),
        (28, LIFTED),
    ]),
    "Strike": ("LINEAR", [
        (1, LIFTED),
        (5, {"Spine": (30, 0, 0), "Head": (12, 0, 0), "Heap": HEAP_DOWN, "Root": ((0, 0, 0), (0, -0.3, -0.08))}),
        (10, SLAMMED),
    ]),
    "Recover": ("BEZIER", [  # heap on the ground, then hauled back up to the open carry (guard comes after)
        (1, SLAMMED),
        (22, {"Spine": (30, 0, 0), "Head": (20, 0, 0), "Heap": HEAP_DOWN, "Root": ((0, 0, 0), (0, -0.3, -0.1))}),
        (37, OPEN),
    ]),
    "Stagger": ("BEZIER", [
        (1, GUARD),
        (5, {"Spine": (-16, 0, 6), "Head": (-12, 0, 0), "Heap": ((-10, 0, 0), (0, 0.15, -0.1)), "Root": ((0, 0, 0), (0, 0.12, 0))}),
        (12, {"Spine": (-10, 0, -5), "Heap": ((-6, 0, 0), (0, 0.1, -0.05))}),
        (19, OPEN),
    ]),
}


def build():
    robe, dark = kit.mat("Soured", "#6c5c84"), kit.mat("SouredDark", "#4c4060")
    eye, claw = kit.mat("SouredEye_Glow", "#c8f07a"), kit.mat("Claw", "#d6cdb8")
    wood, wood_dark = kit.mat("HeapWood", "#7a5538"), kit.mat("HeapWoodDark", "#56392a")
    metal, cloth = kit.mat("HeapMetal", "#34313a"), kit.mat("HeapCloth", "#8c7a5c")
    clock_face, frame_gold = kit.mat("ClockFace", "#e8dcc0"), kit.mat("FrameGold", "#b08d3e")

    def robe_shape(bm):
        kit.cone(9, 0.55, 0.26, 1.15)(bm)
        rim = sorted((v for v in bm.verts if v.co.z < -0.55 and v.co.length > 0.1), key=lambda v: math.atan2(v.co.y, v.co.x))
        for i, v in enumerate(rim):
            v.co.z += 0.1 if i % 2 else 0.0  # tattered hem

    parts = [
        part("Robe", robe_shape, robe, M((0, 0, 0.62), rot=(10, 0, 0)), smooth=True, group="Spine"),
        part("Hump", kit.sphere(10, 7), robe, M((0, 0.05, 1.05), scale=(0.42, 0.34, 0.3)), smooth=True, group="Spine"),
        part("Head", kit.sphere(10, 7, 0.22), dark, M((0, -0.24, 1.3), scale=(1, 1.05, 0.95)), smooth=True, group="Head"),
        part("Hood", kit.cone(8, 0.28, 0.06, 0.44), robe, M((0, -0.17, 1.42), rot=(-35, 0, 0)), smooth=True, group="Head"),
    ]
    for s in (-1, 1):
        parts.append(part("Eye", kit.sphere(8, 6, 0.04), eye, M((s * 0.08, -0.45, 1.3)), smooth=True, group="Head"))
        shoulder, hand = (s * 0.38, -0.1, 1.05), (s * 0.44, -0.48, 0.86)
        parts.append(part("Arm", kit.cone(6, 0.11, 0.06, 0.5), robe, kit.along(shoulder, hand), smooth=True, group="Heap"))
        parts.append(part("Claw", kit.cone(5, 0.04, 0.0, 0.14), claw, kit.along(hand, (s * 0.36, -0.62, 0.84)), group="Heap"))

    # A lantern hung from its belt on the right: sickly green, like its eyes. Green light in the dark = a Soured.
    lantern_glass = kit.mat("SouredLantern_Glow", "#b6f06a")
    parts += [
        part("BeltLanternCord", kit.cube(), metal, M((0.5, 0.0, 0.8), rot=(0, -15, 0), scale=(0.015, 0.015, 0.14)), group="Spine"),
        part("BeltLanternTop", kit.cone(4, 0.08, 0.01, 0.07), metal, M((0.52, 0.0, 0.7), rot=(0, 0, 45)), group="Spine"),
        part("BeltLanternGlass", kit.cube(), lantern_glass, M((0.52, 0.0, 0.61), scale=(0.1, 0.1, 0.13)), group="Spine"),
        part("BeltLanternBase", kit.cube(), metal, M((0.52, 0.0, 0.535), scale=(0.12, 0.12, 0.02)), group="Spine"),
    ]

    # The heap: a chest, a chair on top, a wall clock, a bundle and a picture frame, stacked in front.
    y = -0.58
    parts += [
        part("Chest", kit.cube(), wood, M((0, y, 0.72), rot=(0, 0, 4), scale=(0.72, 0.46, 0.36))),
        part("ChestBand", kit.cube(), metal, M((0, y, 0.72), rot=(0, 0, 4), scale=(0.74, 0.48, 0.05))),
        part("ChestLid", kit.cube(), wood_dark, M((0, y, 0.92), rot=(0, 0, 4), scale=(0.74, 0.48, 0.05))),
        part("Seat", kit.cube(), wood_dark, M((0.05, y, 1.12), rot=(0, 8, -10), scale=(0.44, 0.4, 0.05))),
        part("ChairBack", kit.cube(), wood_dark, M((0.08, y + 0.18, 1.36), rot=(-8, 8, -10), scale=(0.42, 0.05, 0.45))),
        part("Bundle", kit.sphere(8, 6), cloth, M((-0.3, y, 1.18), scale=(0.2, 0.2, 0.16)), smooth=True),
        part("Clock", kit.cylinder(10, 0.17, 0.07), metal, M((-0.12, y - 0.12, 1.5), rot=(90, 0, 12))),
        part("ClockFace", kit.cylinder(10, 0.14, 0.02), clock_face, M((-0.12, y - 0.165, 1.5), rot=(90, 0, 12))),
        part("Frame", kit.cube(), frame_gold, M((0.22, y - 0.05, 1.62), rot=(0, -14, 8), scale=(0.3, 0.04, 0.38))),
        part("Picture", kit.cube(), dark, M((0.22, y - 0.075, 1.62), rot=(0, -14, 8), scale=(0.22, 0.02, 0.3))),
    ]
    for dx, dy in ((-0.17, -0.15), (0.2, -0.15), (-0.17, 0.15), (0.2, 0.15)):
        parts.append(part("ChairLeg", kit.cube(), wood_dark, M((0.05 + dx, y + dy, 1.02), rot=(0, 8, -10), scale=(0.04, 0.04, 0.2))))
    for p in parts:
        if not p.vertex_groups:
            p.vertex_groups.new(name="Heap").add(list(range(len(p.data.vertices))), 1.0, "REPLACE")

    mesh = kit.join(parts, "Laden")
    armature = kit.rig("LadenRig", BONES)
    kit.skin(mesh, armature)
    kit.animate(armature, ACTIONS)
    return [armature, mesh]
