"""The Watchman, of the Unreaped Rows (the Orchard's miniboss), and the scarecrows he stands among (the same figure):
a coat stuffed with straw, a hat, a sack for a face with two stitched eyes, arms out along the crossbar, a reaping hook
in the right hand, a lantern (dark) hanging from the left. No legs: the coat hangs to the knees, straw poking out at
the cuffs and hem (the pole it stands on is a prop of its own, so he can tear free of it). Origin at the ground.

Clips: Idle (still, arms out, the faintest creaking sway), Lurch (a jerky hop forward, the coat dragging), Windup
(0.6 s: the hook drawn back high, the body twisting away), Swing (0.3 s: the hook swept round across the front),
Recover (0.9 s), Reveal (1.2 s: a jolt as he tears free, arms dropping, then up, the head snapping round), Thrash
(alight: flailing, looping), Breathe (1 s: leaning back, his lantern raised to his face, drawing breath over its flame),
Hold (1.9 s: the same, held, trembling: the feint), Spew (0.4 s: thrown forward, the lantern thrust out), Stagger, Death
(he slumps down into his own straw).
"""
import math

M, part = kit.M, kit.part  # `kit` is injected by build_model.py

SHOULDER = 1.52
BONES = {
    "Root": ((0, 0, 0), (0, 0, 0.3), None),
    "Body": ((0, 0, 0.75), (0, 0, SHOULDER), "Root"),
    "Head": ((0, 0, SHOULDER), (0, 0, 2.0), "Body"),
    "ArmL": ((-0.16, 0, SHOULDER - 0.04), (-0.82, 0, SHOULDER - 0.08), "Body"),
    "ArmR": ((0.16, 0, SHOULDER - 0.04), (0.82, 0, SHOULDER - 0.08), "Body"),
}

REST = {}
# Arms stand out to the sides (a scarecrow's T): +Y on ArmR (and -Y on ArmL) drops it; Z swings it forward/back.
SWAY = {"Body": (0, 2, 0), "Head": (0, 0, 4)}
LEAN = {"Body": (22, 0, 0), "Head": (-14, 0, 0), "ArmL": (0, 18, 10), "ArmR": (0, -18, -10), "Root": ((0, 0, 0), (0, -0.12, 0.08))}
DRAWN = {"Body": (-8, 0, 40), "Head": (0, 0, -30), "ArmR": (0, -40, 70), "ArmL": (0, 30, 0)}
SWEPT = {"Body": (12, 0, -45), "Head": (-6, 0, 30), "ArmR": (0, 20, -95), "ArmL": (0, 40, 0), "Root": ((0, 0, 0), (0, -0.2, 0))}
DRAWING = {"Body": (-14, 0, 0), "Head": (-24, 0, 0), "ArmL": (0, 12, 85), "ArmR": (0, -10, 0)}
SPEWING = {"Body": (24, 0, 0), "Head": (20, 0, 0), "ArmL": (0, 0, 95), "ArmR": (0, 10, 0), "Root": ((0, 0, 0), (0, -0.12, 0))}
SLUMPED = {"Body": (40, 0, 8), "Head": (50, 0, 20), "ArmL": (0, 70, 0), "ArmR": (0, -70, 0), "Root": ((0, 0, 0), (0, 0, -0.55))}

ACTIONS = {
    "Idle": ("BEZIER", [(1, REST), (30, SWAY), (60, REST)]),
    "Lurch": ("BEZIER", [(1, REST), (3, LEAN), (6, dict(LEAN, Root=((0, 0, 0), (0, -0.05, 0.02)))), (9, REST)]),
    "Windup": ("BEZIER", [(1, REST), (8, DRAWN), (15, dict(DRAWN, Body=(-10, 0, 46)))]),
    "Swing": ("BEZIER", [(1, DRAWN), (4, SWEPT), (8, dict(SWEPT, Body=(14, 0, -52)))]),
    "Recover": ("BEZIER", [(1, SWEPT), (12, dict(SWEPT, Body=(6, 0, -20))), (22, REST)]),
    "Reveal": ("BEZIER", [(1, REST), (4, {"Root": ((0, 0, 0), (0, 0, 0.25)), "Body": (-10, 0, 0), "ArmL": (0, 60, 0), "ArmR": (0, -60, 0)}),
                          (12, {"ArmL": (0, 50, 0), "ArmR": (0, -50, 0), "Head": (20, 0, 0)}), (22, {"Head": (-10, 0, 40)}), (29, REST)]),
    "Thrash": ("BEZIER", [(1, {"ArmL": (0, -30, 40), "ArmR": (0, 40, -20), "Body": (0, 10, 10)}),
                          (5, {"ArmL": (0, 40, -20), "ArmR": (0, -30, 40), "Body": (0, -10, -10)}),
                          (9, {"ArmL": (0, -30, 40), "ArmR": (0, 40, -20), "Body": (0, 10, 10)})]),
    "Breathe": ("BEZIER", [(1, REST), (16, DRAWING), (24, dict(DRAWING, Body=(-18, 0, 0), Head=(-30, 0, 0)))]),
    "Hold": ("BEZIER", [(1, REST), (16, DRAWING), (24, dict(DRAWING, Body=(-18, 0, 0), Head=(-30, 0, 0))),
                        (32, dict(DRAWING, Body=(-17, 0, 2), Head=(-29, 0, -3))), (40, dict(DRAWING, Body=(-19, 0, -2), Head=(-31, 0, 3))),
                        (46, dict(DRAWING, Body=(-20, 0, 0), Head=(-32, 0, 0)))]),
    "Spew": ("BEZIER", [(1, dict(DRAWING, Body=(-18, 0, 0), Head=(-30, 0, 0))), (4, SPEWING), (10, dict(SPEWING, Body=(20, 0, 0)))]),
    "Stagger": ("BEZIER", [(1, REST), (4, {"Body": (-18, 0, 10), "Head": (-20, 0, 0), "ArmL": (0, -20, 0), "ArmR": (0, 20, 0)}), (16, REST)]),
    "Death": ("BEZIER", [(1, REST), (6, {"Body": (-15, 0, 0), "ArmL": (0, 40, 0), "ArmR": (0, -40, 0)}), (20, SLUMPED)]),
}


def build():
    rng = kit.rng
    coat, coat_dark = kit.mat("Coat", "#5e4a52"), kit.mat("CoatDark", "#45363c")
    sack, stitch, hat = kit.mat("Sack", "#a89068"), kit.mat("Ink", "#1a1622"), kit.mat("Hat", "#2e2622")
    straw, iron, wood = kit.mat("Straw", "#b08f52"), kit.mat("HookIron", "#3b3a42"), kit.mat("OrchardWood", "#6a5038")
    glass = kit.mat("WatchmanLanternGlass", "#4a4232")  # (dark: unlit until he's found)
    lantern_iron = kit.mat("LanternIron", "#2f2d33")

    def coat_shape(bm):
        kit.cone(9, 0.34, 0.2, 0.85)(bm)
        for v in bm.verts:
            if v.co.z < -0.3:
                v.co.z += rng.uniform(-0.06, 0.04)  # a ragged hem
    parts = [
        part("Coat", coat_shape, coat, M((0, 0, 1.12)), smooth=True, group="Body"),
        part("Collar", kit.cylinder(9, 0.22, 0.08), coat_dark, M((0, 0, SHOULDER + 0.02)), group="Body"),
        part("Head", kit.sphere(9, 7, 0.17), sack, M((0, 0, 1.76), scale=(1, 0.95, 1.05)), smooth=True, group="Head"),
        part("Tie", kit.cylinder(8, 0.1, 0.05), kit.mat("Rope", "#a38b62"), M((0, 0, 1.6)), group="Head"),
        part("HatBrim", kit.cylinder(12, 0.32, 0.03), hat, M((0, 0, 1.9), rot=(8, 0, 0)), group="Head"),
        part("HatCrown", kit.cone(9, 0.16, 0.12, 0.24), hat, M((0, 0.01, 2.03), rot=(8, 0, 0)), group="Head"),
    ]
    # The face: two crossed stitches for eyes, a stitched mouth.
    for s in (-1, 1):
        for a in (45, -45):
            parts.append(part("Stitch", kit.cube(), stitch, M((s * 0.06, -0.162, 1.8), rot=(0, a, 0), scale=(0.07, 0.01, 0.012)), group="Head"))
    for k in range(5):
        parts.append(part("Mouth", kit.cube(), stitch, M((-0.06 + k * 0.03, -0.165, 1.7), rot=(0, 90, 0), scale=(0.035, 0.01, 0.008)), group="Head"))
    # The arms: sleeves along the crossbar, straw out of the cuffs.
    for s, bone in ((-1, "ArmL"), (1, "ArmR")):
        parts.append(part("Sleeve", kit.tube([(s * 0.16, 0, SHOULDER - 0.04), (s * 0.5, 0, SHOULDER - 0.06), (s * 0.74, 0, SHOULDER - 0.08)],
                                             [0.11, 0.09, 0.08], seg=7), coat, group=bone))
        for k in range(4):
            a = math.radians(k * 90 + 20)
            tip = (s * 0.92, math.cos(a) * 0.06, SHOULDER - 0.08 + math.sin(a) * 0.06)
            parts.append(part("Straw", kit.cone(4, 0.025, 0.0, 0.2), straw, kit.along((s * 0.76, 0, SHOULDER - 0.08), tip), group=bone))
    for k in range(10):
        a = math.tau * k / 10
        base = (math.cos(a) * 0.3, math.sin(a) * 0.3, 0.7)
        tip = (math.cos(a) * 0.36, math.sin(a) * 0.36, 0.5 + rng.uniform(-0.05, 0.05))
        parts.append(part("Straw", kit.cone(4, 0.03, 0.0, 0.22), straw, kit.along(base, tip), group="Body"))
    # The reaping hook, in the right hand: a handle, and the curved blade forward and down.
    hand = (0.78, 0, SHOULDER - 0.08)
    parts.append(part("HookHandle", kit.cylinder(6, 0.025, 0.55), wood, M((0.8, -0.05, SHOULDER - 0.3), rot=(10, 0, 0)), group="ArmR"))
    blade = [(0.8, -0.08, SHOULDER - 0.55), (0.82, -0.3, SHOULDER - 0.6), (0.8, -0.48, SHOULDER - 0.5), (0.78, -0.55, SHOULDER - 0.32)]
    parts.append(part("HookBlade", kit.tube(blade, [0.035, 0.03, 0.022, 0.008], seg=4), iron, group="ArmR"))
    # The lantern, hanging from the left hand: dark.
    lx = -0.78
    parts += [
        part("LanternCord", kit.cylinder(4, 0.006, 0.2), kit.mat("Rope", "#a38b62"), M((lx, 0, SHOULDER - 0.2)), group="ArmL"),
        part("LanternTop", kit.cone(4, 0.08, 0.02, 0.06), lantern_iron, M((lx, 0, SHOULDER - 0.33), rot=(0, 0, 45)), group="ArmL"),
        part("LanternGlass", kit.cube(), glass, M((lx, 0, SHOULDER - 0.45), scale=(0.1, 0.1, 0.15)), group="ArmL"),
        part("LanternBase", kit.cube(), lantern_iron, M((lx, 0, SHOULDER - 0.54), scale=(0.12, 0.12, 0.02)), group="ArmL"),
    ]
    mesh = kit.join(parts, "Watchman")
    armature = kit.rig("WatchmanRig", BONES)
    kit.skin(mesh, armature)
    kit.animate(armature, ACTIONS)
    return [armature, mesh]
