"""The One Who Waits: the Hour (the Hall of the Last Hour's Boss). The Hall's last hour, which never struck, given a
body (after the Clockwork God of the old games, hung dead from his own contraption: the motifs, not the man). It hangs
from cables over the middle of its chamber, behind the great dial: a robed upper body, hooded, its face a full mask
that is a clock's dial (enamel, brass-rimmed, twelve marks, its two hands its only expression: they turn), long
jointed arms like a clock's levers (gear joints at shoulder and elbow), the left ending in a great silver hammer, the
right in a silver crescent blade like the hall's pendulums. Below the robe there's no body: the works, a brass cage of
rods and stacked gears with a pendulum inside, trailing down to a point a metre off the floor, a small green lantern
(it's a Soured, as they all are) hung from its foot. Origin on the floor under it; 9.5 m up, the cables go on into its
contraption (the chamber's). Front -Y.

Its rest pose: the hammer down on the floor in front of it (4.6 m out, on its centre line: where the slam lands), the
blade arm hanging forward and right, the blade low (0.6 m: a sweep passes under someone vaulting).

Clips (30 fps): Idle (poised, hung, swaying), Limp (time stopped: a husk dangling, head and arms hanging), Wake (from
limp to poised), TwitchDraw / TwitchSwing (limp: one arm jerked up, weakly, and let fall), HammerRaise / HammerSlam /
HammerRecover, BladeDraw / BladeSweep / BladeRecover (from its right, round in front, to its left, low), GrindDraw /
Grind (close under it: its works spin), StopCast (both arms flung up, the mask turned on you), Rewind (shuddering, drawn
in), Chime (the quarter strikes: head thrown back, arms wide), Stagger, Death (the cables let go of its weight: it
slumps, head and arms falling). The mask's hands are bones (DialHour, DialMinute): the game turns them.
"""
import math

M, part = kit.M, kit.part  # `kit` is injected by build_model.py

TOP = 9.5            # the cables' anchor
WAIST = 4.4          # where the robe ends and the works begin
SHOULDER = 5.75
MASK = (0, -0.42, 6.45)
ELBOW_L, HAND_L = (-1.35, -1.7, 4.15), (-0.25, -4.25, 1.05)   # the hammer arm (its head on the floor at y -4.6)
ELBOW_R, HAND_R = (1.55, -1.5, 4.2), (2.2, -3.6, 1.0)          # the blade arm (the blade low)
BONES = {
    "Root": ((0, 0, 0), (0, 0, 0.5), None),
    "Hang": ((0, 0, TOP), (0, 0, 7.2), "Root"),
    "Torso": ((0, 0, WAIST), (0, 0, SHOULDER + 0.3), "Hang"),
    "Head": ((0, 0, SHOULDER + 0.3), (0, 0, 7.0), "Torso"),
    "DialHour": (MASK, (MASK[0], MASK[1], MASK[2] + 0.3), "Head"),
    "DialMinute": ((MASK[0], MASK[1] - 0.01, MASK[2]), (MASK[0], MASK[1] - 0.01, MASK[2] + 0.42), "Head"),
    "DialTip": ((MASK[0], MASK[1] - 0.3, MASK[2]), (MASK[0], MASK[1] - 0.4, MASK[2]), "Head"),  # (out of the face: the game turns the hands about it)
    "Works": ((0, 0, WAIST), (0, 0, 1.0), "Hang"),
    "ArmL": ((-0.85, 0, SHOULDER), ELBOW_L, "Torso"),
    "ForeL": (ELBOW_L, HAND_L, "ArmL"),
    "ArmR": ((0.85, 0, SHOULDER), ELBOW_R, "Torso"),
    "ForeR": (ELBOW_R, HAND_R, "ArmR"),
}


def with_(pose, **changes):
    merged = dict(pose)
    merged.update(changes)
    return merged


# Poised: hung upright, leaning a little, its dial face turned down on the floor (on you), the hammer raised to the middle, the blade ready at its side.
POISED = {"Torso": (6, 0, 0), "Head": (30, 0, 0), "ArmL": (-50, 0, 8), "ForeL": (-35, 0, 0),
          "ArmR": (-25, 0, -15), "ForeR": (-15, 0, 0)}
POISED_L = with_(POISED, Hang=(2, 0, 2), Head=(30, 0, 4))
POISED_R = with_(POISED, Hang=(-2, 0, -2), Head=(30, 0, -4))
# Limp: a husk on its cables, slumped forward, head hanging, arms hanging straight down.
LIMP = {"Hang": (5, 0, 0), "Torso": (30, 0, 6), "Head": (25, 0, 8), "ArmL": (17, 0, -4), "ForeL": (-8, 0, 0),
        "ArmR": (14, 0, 6), "ForeR": (-11, 0, 0), "Works": (-4, 0, 0)}  # (the arms' own: straight down, the torso's bow taken off)
LIMP_SWAY = with_(LIMP, Hang=(7, 0, -3), Torso=(31, 0, 9), Head=(27, 0, 4))
# The hammer: raised high and back over the head; brought down on the floor in front.
HAMMER_UP = {"Torso": (-14, 0, 0), "Head": (-6, 0, 0), "ArmL": (-150, 0, 10), "ForeL": (-35, 0, 0),
             "ArmR": (-30, 0, -25), "ForeR": (-20, 0, 0), "Hang": (-3, 0, 0)}
HAMMER_DOWN = {"Torso": (8, 0, 0), "Head": (16, 0, 0), "ArmL": (-6, 0, 0), "ForeL": (0, 0, 0),
               "ArmR": (-20, 0, -20), "ForeR": (-10, 0, 0), "Hang": (3, 0, 0)}
# The blade: drawn back to its right (behind it); swept round in front, low, to its left.
BLADE_BACK = {"Torso": (0, 0, 28), "Head": (8, 0, 10), "ArmR": (0, 0, 105), "ForeR": (0, 0, 10),
              "ArmL": (-45, 0, 10), "ForeL": (-30, 0, 0)}
BLADE_ACROSS = {"Torso": (0, 0, -24), "Head": (8, 0, -12), "ArmR": (0, 0, -95), "ForeR": (0, 0, -15),
                "ArmL": (-45, 0, 25), "ForeL": (-30, 0, 0)}
# The grind: drawn up into itself, then its works spin.
GRIND_DRAW = with_(POISED, Torso=(-10, 0, 0), Works=(0, 0, 0), Hang=((0, 0, 0), (0, 0, 0.25)))
GRIND_A = with_(POISED, Works=(0, 0, 120), Hang=((0, 0, 0), (0, 0, -0.1)))
GRIND_B = with_(POISED, Works=(0, 0, 240), Hang=((0, 0, 0), (0, 0, -0.1)))
GRIND_C = with_(POISED, Works=(0, 0, 359), Hang=((0, 0, 0), (0, 0, -0.1)))
# Stop: both arms flung up, the mask turned full on you.
STOP = {"Torso": (-10, 0, 0), "Head": (-12, 0, 0), "ArmL": (-125, 0, 35), "ForeL": (-20, 0, 0),
        "ArmR": (-125, 0, -35), "ForeR": (-20, 0, 0), "Hang": (-2, 0, 0)}
# Rewind: drawn in on itself, shuddering.
REWIND_A = {"Torso": (14, 0, 0), "Head": (18, 0, 0), "ArmL": (-25, 0, -20), "ForeL": (-60, 0, 0),
            "ArmR": (-25, 0, 20), "ForeR": (-60, 0, 0), "Hang": (-3, 0, 2)}
REWIND_B = with_(REWIND_A, Hang=(-1, 0, -2), Torso=(12, 0, 3))
# The quarter strikes: head thrown back, arms wide.
CHIME = {"Torso": (-20, 0, 0), "Head": (-34, 0, 0), "ArmL": (-80, 0, 60), "ForeL": (-10, 0, 0),
         "ArmR": (-80, 0, -60), "ForeR": (-10, 0, 0), "Hang": (-4, 0, 0)}
STAGGER = with_(POISED, Hang=(-9, 0, 0), Torso=(-16, 0, 0), Head=(-22, 0, 0), ArmL=(-30, 0, 20), ArmR=(-15, 0, -30))
DEAD = {"Hang": ((10, 0, 6), (0, 0, -0.6)), "Torso": (48, 0, 10), "Head": (35, 0, 12), "ArmL": (0, 0, -8),
        "ForeL": (-10, 0, 0), "ArmR": (-4, 0, 12), "ForeR": (-15, 0, 0), "Works": (-10, 0, 0)}

ACTIONS = {
    "Idle": ("BEZIER", [(1, POISED_L), (46, POISED_R), (91, POISED_L)]),
    "Limp": ("BEZIER", [(1, LIMP), (60, LIMP_SWAY), (120, LIMP)]),
    "Wake": ("BEZIER", [(1, LIMP), (20, with_(LIMP, Head=(10, 0, 0), Torso=(20, 0, 0))), (45, with_(CHIME, Head=(-20, 0, 0))), (60, POISED)]),
    "TwitchDraw": ("BEZIER", [(1, LIMP), (10, with_(LIMP, ArmR=(10, 0, 0))), (14, with_(LIMP, ArmR=(16, 0, 0))),
                              (30, with_(LIMP, ArmR=(-20, 0, -10), ForeR=(10, 0, 0), Head=(30, 0, 0))), (48, with_(LIMP, ArmR=(-35, 0, -10), Head=(28, 0, 0)))]),
    "TwitchSwing": ("BEZIER", [(1, with_(LIMP, ArmR=(-35, 0, -10), Head=(28, 0, 0))), (15, with_(LIMP, ArmR=(30, 0, 20), ForeR=(40, 0, 0))), (30, LIMP)]),
    "HammerRaise": ("BEZIER", [(1, POISED), (18, with_(HAMMER_UP, ArmL=(-120, 0, 10))), (30, HAMMER_UP)]),
    "HammerSlam": ("LINEAR", [(1, HAMMER_UP), (5, with_(HAMMER_DOWN, ArmL=(-60, 0, 5))), (9, HAMMER_DOWN)]),
    "HammerRecover": ("BEZIER", [(1, HAMMER_DOWN), (15, with_(HAMMER_DOWN, Torso=(10, 0, 2))), (36, POISED)]),
    "BladeDraw": ("BEZIER", [(1, POISED), (27, BLADE_BACK)]),
    "BladeSweep": ("LINEAR", [(1, BLADE_BACK), (8, with_(POISED, Torso=(0, 0, 0), ArmR=(0, 0, 5), ForeR=(0, 0, 0))), (15, BLADE_ACROSS)]),
    "BladeRecover": ("BEZIER", [(1, BLADE_ACROSS), (30, POISED)]),
    "GrindDraw": ("BEZIER", [(1, POISED), (20, GRIND_DRAW)]),
    "Grind": ("LINEAR", [(1, GRIND_DRAW), (6, GRIND_A), (12, GRIND_B), (18, GRIND_C)]),
    "StopCast": ("BEZIER", [(1, POISED), (20, STOP), (36, with_(STOP, Head=(-16, 0, 0)))]),
    "Rewind": ("BEZIER", [(1, REWIND_A), (7, REWIND_B), (13, REWIND_A), (19, REWIND_B), (25, REWIND_A)]),
    "Chime": ("BEZIER", [(1, POISED), (12, CHIME), (45, CHIME), (60, POISED)]),
    "Stagger": ("BEZIER", [(1, POISED), (6, STAGGER), (30, POISED)]),
    "Death": ("BEZIER", [(1, POISED), (12, CHIME), (30, with_(LIMP, Hang=((6, 0, 0), (0, 0, -0.2)))), (60, DEAD)]),
}


def gear(parts, material, centre, radius, teeth, thick, rot, group):
    """A brass gear (a disc and its teeth), turned by `rot` (degrees) from lying flat."""
    at = M(centre, rot=rot)
    parts.append(part("Gear", kit.cylinder(20, radius, thick), material, at, group=group))
    for k in range(teeth):
        a = 2 * math.pi * k / teeth
        parts.append(part("Cog", kit.cube(), material, at @ M((math.cos(a) * (radius + 0.06), math.sin(a) * (radius + 0.06), 0),
                                                             rot=(0, 0, math.degrees(a)), scale=(0.13, 0.1, thick)), group=group))


def build():
    robe, robe_dark = kit.mat("HourRobe", "#262b36"), kit.mat("HourRobeShadow", "#1a1d25")
    trim, brass = kit.mat("HourGilt_Metal", "#d9b45a"), kit.mat("HourBrass_Metal", "#b8913f")
    iron, steel = kit.mat("HourIron_Metal", "#3a3a42"), kit.mat("HourSilver_Metal", "#c9d3e2")
    enamel, mark = kit.mat("HourEnamel", "#ece4cc"), kit.mat("HourMark", "#1c1a18")
    eye = kit.mat("SouredEye_Glow", "#c8f07a")
    lantern_glass = kit.mat("SouredLantern_Glow", "#b6f06a")

    parts = []
    # The cables up into its contraption, from its back and shoulders.
    for x, y in ((-0.5, 0.25), (0.5, 0.25), (-0.2, 0.45), (0.2, 0.45)):
        parts.append(part("Cable", kit.tube([(x, y, SHOULDER - 0.3), (x * 0.6, y * 0.7, 8.0), (x * 0.3, y * 0.3, TOP + 1.2)], [0.05, 0.045, 0.04], seg=5), iron, group="Hang"))
    # A crown of the clock's train over the hood (a halo of gears), on the cables.
    for k, (r, z) in enumerate(((0.55, 7.35), (0.32, 7.8))):
        gear(parts, brass, (0, 0.15, z), r, 10 + 4 * (1 - k), 0.06, (90 if k else 75, 0, 0), "Head")
    # The robe: shoulders to the waist, flaring a little, hood round the mask; gilt hem and edges.
    parts += [
        part("Robe", kit.cone(14, 0.85, 0.55, SHOULDER - WAIST + 0.1), robe, M((0, 0.05, (WAIST + SHOULDER) / 2), scale=(1, 0.8, 1)), smooth=True, group="Torso"),
        part("RobeHem", kit.cylinder(14, 0.88, 0.08), trim, M((0, 0.05, WAIST + 0.02), scale=(1, 0.8, 1)), smooth=True, group="Torso"),
        part("Shoulders", kit.sphere(12, 7), robe, M((0, 0.05, SHOULDER), scale=(0.95, 0.55, 0.3)), smooth=True, group="Torso"),
        part("Stole", kit.cube(), trim, M((0, -0.42, (WAIST + SHOULDER) / 2), rot=(-6, 0, 0), scale=(0.22, 0.03, SHOULDER - WAIST)), group="Torso"),
        part("Hood", kit.sphere(12, 8), robe_dark, M((0, 0.06, 6.5), scale=(0.62, 0.6, 0.72)), smooth=True, group="Head"),
        part("HoodEdge", kit.cylinder(16, 0.6, 0.06), trim, M((0, -0.3, 6.47), rot=(90, 0, 0), scale=(1, 1.18, 1)), smooth=True, group="Head"),
    ]
    # The mask: a dial, full over the face, brass-rimmed, twelve marks; its hands (bones: the game turns them).
    mx, my, mz = MASK
    parts += [
        part("Mask", kit.cylinder(28, 0.5, 0.1), enamel, M((mx, my, mz), rot=(90, 0, 0)), smooth=True, group="Head"),
        part("MaskRim", kit.cylinder(28, 0.55, 0.07), brass, M((mx, my + 0.03, mz), rot=(90, 0, 0)), smooth=True, group="Head"),
        part("MaskBoss", kit.sphere(8, 6, 0.06), eye, M((mx, my - 0.07, mz)), group="Head"),
    ]
    for k in range(12):
        a = 2 * math.pi * k / 12
        long_ = 0.1 if k % 3 == 0 else 0.06
        parts.append(part("MaskMark", kit.cube(), mark, M((mx + math.sin(a) * 0.4, my - 0.055, mz + math.cos(a) * 0.4),
                                                          rot=(0, -math.degrees(a), 0), scale=(0.025, 0.01, long_)), group="Head"))
    parts.append(part("HandHour", kit.cube(), mark, M((mx, my - 0.07, mz + 0.13), scale=(0.045, 0.012, 0.28)), group="DialHour"))
    parts.append(part("HandMinute", kit.cube(), mark, M((mx, my - 0.08, mz + 0.18), scale=(0.03, 0.012, 0.38)), group="DialMinute"))
    # The arms: long levers of iron, gear joints at shoulder and elbow, gilt bands.
    for side, arm, fore, elbow, hand in ((-1, "ArmL", "ForeL", ELBOW_L, HAND_L), (1, "ArmR", "ForeR", ELBOW_R, HAND_R)):
        shoulder = (side * 0.85, 0, SHOULDER)
        gear(parts, brass, shoulder, 0.28, 10, 0.12, (0, 90, 0), arm)
        parts.append(part("Sleeve", kit.cone(10, 0.24, 0.15, 0.9), robe, kit.along(shoulder, (side * 1.05, -0.35, SHOULDER - 0.75)), smooth=True, group=arm))
        parts.append(part("Upper", kit.tube([shoulder, elbow], [0.13, 0.11], seg=6), iron, group=arm))
        parts.append(part("UpperBand", kit.tube([[s + (e - s) * 0.45 for s, e in zip(shoulder, elbow)], [s + (e - s) * 0.55 for s, e in zip(shoulder, elbow)]], [0.15, 0.15], seg=8), trim, group=arm))
        gear(parts, brass, elbow, 0.22, 9, 0.1, (0, 90, 0), fore)
        parts.append(part("Fore", kit.tube([elbow, hand], [0.11, 0.08], seg=6), iron, group=fore))
        for t in (0.3, 0.65):
            c = [s + (e - s) * t for s, e in zip(elbow, hand)]
            parts.append(part("ForeBand", kit.sphere(8, 6, 0.13), trim, M(tuple(c)), group=fore))
    # The hammer: a great silver head across the end of the left lever (along x), gilt bands.
    hx, hy, hz = HAND_L
    parts.append(part("HammerSocket", kit.cylinder(10, 0.14, 0.4), iron, kit.along(HAND_L, (hx, hy - 0.25, hz - 0.35)), group="ForeL"))
    head = (hx + 0.25, hy - 0.35, 0.45)
    parts.append(part("HammerHead", kit.cylinder(18, 0.45, 1.3), steel, M(head, rot=(0, 90, 0)), smooth=True, group="ForeL"))
    for dx in (-0.6, 0.6):
        parts.append(part("HammerBand", kit.cylinder(18, 0.48, 0.08), trim, M((head[0] + dx, head[1], head[2]), rot=(0, 90, 0)), smooth=True, group="ForeL"))
    parts.append(part("HammerHaft", kit.tube([HAND_L, (head[0], head[1], head[2] + 0.45)], [0.1, 0.1], seg=6), iron, group="ForeL"))
    # The blade: a silver crescent at the end of the right lever, lying flat and low (its edge out, away from it).
    bx, by, bz = HAND_R
    centre, r_out = (bx + 0.2, by - 0.6, 0.6), 1.25
    steps = [i / 14 for i in range(15)]

    def outer(t):
        a = math.radians(-150 + 120 * t)
        return (centre[0] + math.cos(a) * r_out, centre[1] + math.sin(a) * r_out)

    def inner(t):
        a = math.radians(-138 + 96 * t)
        return (centre[0] + math.cos(a) * 0.98, centre[1] + 0.32 + math.sin(a) * 0.98)

    def blade(bm):
        import bmesh
        rows = []
        for t in steps:
            (ox, oy), (ix, iy) = outer(t), inner(t)
            rows.append([bm.verts.new(p) for p in ((ox, oy, centre[2] - 0.04), (ox, oy, centre[2] + 0.04),
                                                   (ix, iy, centre[2] + 0.04), (ix, iy, centre[2] - 0.04))])
        for a, b in zip(rows, rows[1:]):
            for q in range(4):
                bm.faces.new((a[q], b[q], b[(q + 1) % 4], a[(q + 1) % 4]))
        bm.faces.new(rows[0])
        bm.faces.new(list(reversed(rows[-1])))
        bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    outer_pts = [outer(t) for t in steps]
    parts.append(part("Blade", blade, steel, group="ForeR"))
    parts.append(part("BladeEdge", kit.tube([(x, y, centre[2]) for x, y in outer_pts], [0.03] * len(outer_pts), seg=4), trim, group="ForeR"))
    parts.append(part("BladeHaft", kit.tube([HAND_R, (centre[0], centre[1] + 0.3, centre[2] + 0.05)], [0.09, 0.08], seg=6), iron, group="ForeR"))
    # The works below the robe: a cage of brass rods round stacked gears, a pendulum in it, down to a point.
    for k in range(8):
        a = 2 * math.pi * k / 8
        top_ = (math.cos(a) * 0.62, math.sin(a) * 0.62, WAIST)
        bottom = (math.cos(a) * 0.18, math.sin(a) * 0.18, 1.25)
        parts.append(part("CageRod", kit.tube([top_, (top_[0] * 0.85, top_[1] * 0.85, 2.8), bottom], [0.04, 0.04, 0.035], seg=4), brass, group="Works"))
    for z, r in ((3.9, 0.5), (3.3, 0.42), (2.7, 0.36), (2.1, 0.28), (1.6, 0.2)):
        parts.append(part("CageRing", kit.cylinder(16, r + 0.08, 0.05), trim, M((0, 0, z)), group="Works"))
        gear(parts, brass, (0, 0, z - 0.15), r - 0.08, 8, 0.07, (0, 0, 0), "Works")
    parts.append(part("Spindle", kit.cylinder(8, 0.06, WAIST - 1.0), iron, M((0, 0, (WAIST + 1.0) / 2)), group="Works"))
    parts.append(part("Bob", kit.cylinder(16, 0.22, 0.06), steel, M((0.1, 0, 2.4), rot=(90, 0, 0)), group="Works"))
    parts.append(part("Point", kit.cone(8, 0.18, 0.01, 0.4), trim, M((0, 0, 1.05)), group="Works"))
    # Its green lantern, hung from the point: a Soured, after all.
    parts += [
        part("LanternChain", kit.cylinder(4, 0.015, 0.25), iron, M((0, 0, 0.75)), group="Works"),
        part("LanternTop", kit.cone(4, 0.09, 0.02, 0.07), iron, M((0, 0, 0.62), rot=(0, 0, 45)), group="Works"),
        part("LanternGlass", kit.cube(), lantern_glass, M((0, 0, 0.52), scale=(0.12, 0.12, 0.14)), group="Works"),
        part("LanternBase", kit.cube(), iron, M((0, 0, 0.44), scale=(0.14, 0.14, 0.02)), group="Works"),
    ]
    mesh = kit.join(parts, "TheHour")
    armature = kit.rig("TheHourRig", BONES)
    kit.skin(mesh, armature)
    kit.animate(armature, ACTIONS)
    return [armature, mesh]
