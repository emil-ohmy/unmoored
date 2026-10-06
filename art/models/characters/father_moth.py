"""Father Moth, Who Gathered Autumn (the Gardener, the Orchard's Boss): a vast moth, its wings like dead autumn leaves
(veined, blotched, ragged at the edges: six metres across), and rising from its thorax where its head should be, the
stooped old gardener in his apron and flat cap, a moth's feathered antennae growing from his brow. His forearms have
grown into its forelegs, long and jointed, each ending in a pair of pruning shears; four thin moth legs beneath. In his
ribs, caged, a small lantern: the last light he tended (it glows; it flares with his tell). The same sickly green eyes
as every Soured. Origin at the ground, where he stands when he lands (in flight the game holds him up).

Clips: Perched (still, wings folded down over him like a dead leaf: before he wakes), Wake (2 s: the wings unfold, he
lifts his head), Fly (a great slow wingbeat, looping), Stall (0.8 s: his tell; wings flung wide and up and held, rearing
back, shears thrown open), Dive (wings swept back, shears thrust ahead), SwoopDraw (0.7 s: dropping low, the wings snapped right
back: the swoop's tell), Swoop (level and low across the floor), Downed (stunned: sprawled on the floor, the
wings splayed), Rise (0.8 s: up off the floor), Land (0.8 s), Tend (on the ground, bent over a bed, snipping: looping),
ShearsDraw (0.7 s), Shears (0.3 s: swept across his front), Recover (1 s), GustDraw (0.9 s: the wings raised high over
his back), Gust (0.5 s: one great beat forward), Beat (1.2 s: hammering the water below), Shake (1 s: a shudder that
shakes Moths from his wings), Tear (1.3 s: reaching up, tearing down), Stagger, Death (he folds and falls), Spin (wings flat out wide, turning: looping),
FlickL / FlickR (one wing snapped down to throw), Falter (spent: weak, uneven beats, sagging), Collapse (down onto the
floor, sprawled).
"""
import math

M, part = kit.M, kit.part  # `kit` is injected by build_model.py

THORAX = (0.0, 0.0, 1.5)
SHOULDER = (0.32, -0.8, 2.72)
WRIST = (0.55, -1.8, 0.85)
WING_Z = 1.9

BONES = {
    "Root": ((0, 0, 0), (0, 0, 0.4), None),
    "Body": ((0, 0.6, 1.45), (0, -0.4, 1.5), "Root"),
    "Abdomen": ((0, 0.35, 1.42), (0, 2.4, 0.95), "Body"),
    "Legs": ((0, 0.1, 1.2), (0, 0.1, 0.7), "Body"),
    "Torso": ((0, -0.35, 1.72), (0, -0.78, 2.75), "Body"),
    "Head": ((0, -0.82, 2.8), (0, -1.0, 3.3), "Torso"),
    "ArmL": ((-SHOULDER[0], SHOULDER[1], SHOULDER[2]), (-WRIST[0], WRIST[1], WRIST[2]), "Torso"),
    "ArmR": (SHOULDER, WRIST, "Torso"),
    "SnipL": ((-WRIST[0], WRIST[1], WRIST[2]), (-WRIST[0], WRIST[1] - 0.3, 0.2), "ArmL"),
    "SnipR": (WRIST, (WRIST[0], WRIST[1] - 0.3, 0.2), "ArmR"),
    "WingL": ((-0.35, -0.1, WING_Z), (-3.0, -0.5, WING_Z), "Body"),
    "WingR": ((0.35, -0.1, WING_Z), (3.0, -0.5, WING_Z), "Body"),
    "HindL": ((-0.3, 0.3, WING_Z - 0.1), (-2.0, 1.0, WING_Z - 0.1), "Body"),
    "HindR": ((0.3, 0.3, WING_Z - 0.1), (2.0, 1.0, WING_Z - 0.1), "Body"),
}


def wings(fl, fr, hl, hr, **more):
    """A pose for the four wings ((rx, ry, rz) for the left fore, right fore, left hind, right hind) and anything else."""
    flip = lambda r: (r[0], -r[1], -r[2])  # (the poses are written lift/sweep-positive as below; Blender's axes run the other way)
    pose = {"WingL": flip(fl), "WingR": flip(fr), "HindL": flip(hl), "HindR": flip(hr)}
    pose.update(more)
    return pose


def lift(a, sweep=0.0, lag=0.8):
    """Wings at `a` degrees up (negative: down), swept back `sweep` degrees; the hind wings a little behind."""
    return wings((0, -a, sweep), (0, a, -sweep), (0, -a * lag, sweep * 0.8), (0, a * lag, -sweep * 0.8))


# Conventions (world axes about each bone's head): +Y on WingR (-Y on WingL) lifts it, -Z on WingR (+Z on WingL) sweeps
# it back. Body: +X pitches the nose down. Torso and Head: +X bows forward. Arms (hanging forward and down): -X swings
# them forward and up, +Z on ArmR swings it out to the right. Snip: +Z on SnipR (-Z on SnipL) opens the shears.
# Legs: +X tucks them back up under the abdomen.
REST = {}
TUCK = {"Legs": (75, 0, 0), "ArmL": (-35, 0, 0), "ArmR": (-35, 0, 0), "Torso": (-8, 0, 0)}
UP = dict(lift(48), **TUCK)
DOWN = dict(lift(-38, 0.0, 1.1), **TUCK)
FOLD = dict(wings((0, 30, 80), (0, -30, -80), (0, 28, 72), (0, -28, -72)),
            Torso=(38, 0, 0), Head=(32, 0, 0), ArmL=(-8, 0, 10), ArmR=(-8, 0, -10), Abdomen=(8, 0, 0))
STALL = dict(wings((0, -24, -22), (0, 24, 22), (0, -18, -12), (0, 18, 12)),  # (flung wide and flat: from above, all of him at once)
             Body=(-30, 0, 0), Torso=(-26, 0, 0), Head=(-14, 0, 0), ArmL=(-85, 0, -35), ArmR=(-85, 0, 35),
             SnipL=(0, 0, -28), SnipR=(0, 0, 28), Legs=(80, 0, 0))
DIVE = dict(lift(8, 62), Body=(30, 0, 0), Torso=(12, 0, 0), Head=(-8, 0, 0), ArmL=(-72, 0, -6), ArmR=(-72, 0, 6),
            SnipL=(0, 0, -20), SnipR=(0, 0, 20), Legs=(85, 0, 0))
SWOOP = dict(lift(4, 70), Body=(4, 0, 0), Torso=(20, 0, 0), Head=(-12, 0, 0), ArmL=(-80, 0, -14), ArmR=(-80, 0, 14),
             SnipL=(0, 0, -24), SnipR=(0, 0, 24), Legs=(85, 0, 0))  # (level, low, wings swept right back, shears ahead)
DOWNED = dict(wings((0, 22, 12), (0, -22, -12), (0, 18, 10), (0, -18, -10)),
              Root=((0, 0, 0), (0, 0, -0.62)), Torso=(52, 0, 10), Head=(34, 0, -22), ArmL=(25, 0, -35), ArmR=(25, 0, 35),
              Abdomen=(-6, 0, 0))
TEND = dict(lift(30, 40), Torso=(42, 0, 0), Head=(22, 0, 0), ArmL=(-12, 0, 6), ArmR=(-22, 0, -8))
DRAWN = dict(lift(26, 30), Torso=(10, 0, 34), Head=(-6, 0, -20), ArmR=(-42, 0, 72), ArmL=(-25, 0, 10), SnipR=(0, 0, 30))
SWEPT = dict(lift(20, 30), Torso=(16, 0, -32), Head=(0, 0, 18), ArmR=(-52, 0, -52), ArmL=(-30, 0, -28))
RAISED = dict(wings((0, -82, 26), (0, 82, -26), (0, -70, 20), (0, 70, -20)), Body=(-20, 0, 0), Torso=(-16, 0, 0), **{"Legs": (75, 0, 0)})
BEAT = dict(wings((0, 52, -38), (0, -52, 38), (0, 42, -26), (0, -42, 26)), Body=(12, 0, 0), Torso=(6, 0, 0), **{"Legs": (75, 0, 0)})
REACH = dict(lift(40), Body=(-22, 0, 0), Torso=(-30, 0, 0), Head=(-28, 0, 0), ArmL=(-150, 0, -12), ArmR=(-150, 0, 12),
             SnipL=(0, 0, -25), SnipR=(0, 0, 25), Legs=(75, 0, 0))
PULLED = dict(lift(-20), Body=(10, 0, 0), Torso=(20, 0, 0), ArmL=(-60, 0, -6), ArmR=(-60, 0, 6), Legs=(75, 0, 0))
FALLEN = dict(wings((0, -40, 70), (0, 40, -70), (0, -30, 60), (0, 30, -60)),
              Root=((0, 0, 0), (0, 0, -0.75)), Torso=(72, 0, 0), Head=(40, 0, 10), ArmL=(40, 0, -40), ArmR=(40, 0, 40),
              Abdomen=(-8, 0, 0))


SPREAD = dict(lift(2, -6), Body=(0, 0, 0), Torso=(-6, 0, 0), ArmL=(-40, 0, -50), ArmR=(-40, 0, 50), Legs=(80, 0, 0))  # (spinning: flat out, wide)


def flick(side):
    """One wing snapped down and forward (throwing a leaf), the other held up."""
    if side < 0:
        return dict(wings((0, -50, -25), (0, 30, 0), (0, -38, -15), (0, 24, 0)), Body=(4, -8, 0), Torso=(0, 0, 10), Legs=(75, 0, 0))
    return dict(wings((0, 30, 0), (0, -50, 25), (0, 24, 0), (0, -38, 15)), Body=(4, 8, 0), Torso=(0, 0, -10), Legs=(75, 0, 0))


def falter(k):
    """A weak, uneven beat, sagging (more with each k)."""
    a = 30 - k * 6
    return dict(lift(a if k % 2 == 0 else -a * 0.5, k * 4), Body=(-6 + k * 4, (5 if k % 2 else -5) * (1 + k * 0.3), 0),
                Torso=(k * 6, 0, 0), Head=(k * 7, 0, 0), ArmL=(-35 + k * 8, 0, 0), ArmR=(-35 + k * 8, 0, 0), Legs=(max(0, 75 - k * 18), 0, 0))


def shudder(k):
    return dict(lift(14 + (6 if k % 2 else -6)), Body=(0, 7 if k % 2 else -7, 0), Torso=(-6, 0, 4 if k % 2 else -4), **{"Legs": (75, 0, 0)})


ACTIONS = {
    "Perched": ("BEZIER", [(1, FOLD), (40, dict(FOLD, Head=(34, 0, 3))), (80, FOLD)]),
    "Wake": ("BEZIER", [(1, FOLD), (14, dict(FOLD, Head=(-10, 0, 0), Torso=(10, 0, 0))), (30, dict(lift(70), Torso=(-20, 0, 0), Head=(-12, 0, 0))),
                        (40, UP), (48, DOWN)]),
    "Fly": ("BEZIER", [(1, UP), (10, DOWN), (20, UP)]),
    "Stall": ("BEZIER", [(1, UP), (6, STALL), (20, dict(STALL, Body=(-34, 0, 0), Torso=(-30, 0, 0)))]),
    "Dive": ("BEZIER", [(1, STALL), (4, DIVE), (20, dict(DIVE, Body=(34, 0, 0)))]),
    "SwoopDraw": ("BEZIER", [(1, UP), (6, dict(SWOOP, Body=(-10, 0, 0))), (16, SWOOP)]),
    "Swoop": ("BEZIER", [(1, SWOOP), (10, dict(SWOOP, Body=(6, 0, 0))), (20, SWOOP)]),
    "Downed": ("BEZIER", [(1, DOWNED), (15, dict(DOWNED, Torso=(40, 0, 6), Head=(22, 0, -18), Abdomen=(-12, 0, 0),
                                             **wings((0, 16, 8), (0, -16, -8), (0, 14, 8), (0, -14, -8)))),
                          (30, DOWNED), (45, dict(DOWNED, Torso=(41, 0, -4), Head=(24, 0, -14), Abdomen=(-12, 0, 0))), (60, DOWNED)]),  # (heaving, spent)
    "Spin": ("LINEAR", [(1, SPREAD), (10, dict(SPREAD, **lift(8, -6))), (20, SPREAD)]),
    "FlickL": ("BEZIER", [(1, UP), (3, flick(-1)), (8, UP)]),
    "FlickR": ("BEZIER", [(1, UP), (3, flick(1)), (8, UP)]),
    "Falter": ("BEZIER", [(1, UP)] + [(4 + k * 5, falter(k)) for k in range(6)] + [(34, dict(falter(5), **lift(-30, 30)))]),
    "Collapse": ("BEZIER", [(1, dict(falter(5), **lift(-30, 30))), (5, dict(DOWNED, Root=((0, 0, 0), (0, 0, -0.3)), Torso=(70, 0, 10))), (12, DOWNED)]),
    "Rise": ("BEZIER", [(1, DOWNED), (8, dict(lift(70), Torso=(-10, 0, 0))), (14, DOWN), (20, UP)]),
    "Land": ("BEZIER", [(1, UP), (8, dict(lift(60, 0), Body=(-24, 0, 0))), (14, dict(lift(-20), Body=(-10, 0, 0))), (20, REST)]),
    "Tend": ("BEZIER", [(1, TEND), (10, dict(TEND, ArmR=(-30, 0, -14), SnipR=(0, 0, 26))), (20, dict(TEND, ArmR=(-24, 0, -6))),
                        (30, dict(TEND, ArmL=(-24, 0, 10), SnipL=(0, 0, -26))), (40, TEND)]),
    "ShearsDraw": ("BEZIER", [(1, REST), (10, DRAWN), (17, dict(DRAWN, Torso=(10, 0, 40)))]),
    "Shears": ("BEZIER", [(1, DRAWN), (4, SWEPT), (8, dict(SWEPT, Torso=(18, 0, -38)))]),
    "Recover": ("BEZIER", [(1, SWEPT), (14, dict(SWEPT, Torso=(10, 0, -12))), (24, REST)]),
    "GustDraw": ("BEZIER", [(1, UP), (12, RAISED), (22, dict(RAISED, Body=(-24, 0, 0)))]),
    "Gust": ("BEZIER", [(1, RAISED), (4, BEAT), (12, dict(BEAT, Body=(8, 0, 0)))]),
    "Beat": ("BEZIER", [(1, UP), (4, BEAT), (8, RAISED), (11, BEAT), (15, RAISED), (18, BEAT), (22, RAISED), (29, BEAT)]),
    "Shake": ("BEZIER", [(1, UP)] + [(3 + k * 2, shudder(k)) for k in range(11)] + [(26, UP)]),
    "Tear": ("BEZIER", [(1, UP), (12, REACH), (20, dict(REACH, ArmL=(-140, 0, -12), ArmR=(-140, 0, 12))), (26, PULLED), (32, UP)]),
    "Stagger": ("BEZIER", [(1, UP), (4, dict(lift(60, 0), Body=(-8, 22, 0), Torso=(-24, 0, 0))), (16, UP)]),
    "Death": ("BEZIER", [(1, UP), (8, dict(lift(70), Torso=(-30, 0, 0), Head=(-30, 0, 0))), (22, dict(FALLEN, Root=((0, 0, 0), (0, 0, -0.4)))), (36, FALLEN)]),
}


def ragged(points, rng, every=0.35, depth=0.07):
    """A wing's outline, its edges broken up: each side split into short runs, each nudged in or out a little."""
    out = []
    n = len(points)
    cx = sum(p[0] for p in points) / n
    cy = sum(p[1] for p in points) / n
    for i in range(n):
        a, b = points[i], points[(i + 1) % n]
        steps = max(1, int(math.dist(a, b) / every))
        for k in range(steps):
            t = k / steps
            x, y = a[0] + (b[0] - a[0]) * t, a[1] + (b[1] - a[1]) * t
            if k > 0 or i > 0:
                dx, dy = x - cx, y - cy
                d = math.hypot(dx, dy) or 1.0
                j = rng.uniform(-depth, depth * 0.5)
                x, y = x + dx / d * j, y + dy / d * j
            out.append((x, y))
    return out


def build():
    rng = kit.rng
    fur, fur_dark = kit.mat("FatherMothFur", "#8a7658"), kit.mat("FatherMothFurDark", "#5e4e3a")
    chitin = kit.mat("FatherMothChitin", "#3a2e26")
    leaf, leaf_hind = kit.mat("DeadLeaf", "#9a5e2c"), kit.mat("DeadLeafHind", "#835028")
    blotch, rot, vein = kit.mat("LeafBlotch", "#6e3f1e"), kit.mat("LeafRot", "#2e2016"), kit.mat("LeafVein", "#b48a52")
    edge = kit.mat("FatherMothEdge_Glow", "#4e3c26")
    skin, hair = kit.mat("GardenerSkin", "#b8aa90"), kit.mat("GardenerHair", "#d8d2c4")
    shirt, apron, cap = kit.mat("GardenerShirt", "#7c7466"), kit.mat("GardenerApron", "#5a5040"), kit.mat("GardenerCap", "#3e3830")
    eye = kit.mat("SouredEye_Glow", "#b6f06a")
    rib, cage = kit.mat("GardenerRib", "#cfc3a6"), kit.mat("CageIron_Metal", "#2f2d33")
    light = kit.mat("FatherMothLantern_Glow", "#ffb860")
    shear = kit.mat("ShearIron_Metal", "#6a6870")

    parts = [
        # The moth: thorax, furred; the abdomen behind, banded.
        part("Thorax", kit.sphere(12, 9, 0.52), fur, M(THORAX, scale=(1.0, 1.2, 0.88)), smooth=True, group="Body"),
        part("Collar", kit.sphere(12, 8, 0.4), fur_dark, M((0, -0.38, 1.72), scale=(1.0, 0.9, 0.55)), smooth=True, group="Body"),
        part("Abdomen", kit.tube([(0, 0.35, 1.45), (0, 1.0, 1.36), (0, 1.75, 1.18), (0, 2.4, 0.96)], [0.44, 0.42, 0.3, 0.07], seg=10),
             fur, smooth=True, group="Abdomen"),
    ]
    for k, (y, z, r) in enumerate([(0.75, 1.41, 0.435), (1.15, 1.33, 0.405), (1.55, 1.24, 0.35), (1.95, 1.12, 0.26)]):
        parts.append(part("Band", kit.cylinder(10, r, 0.09), fur_dark, M((0, y, z), rot=(90 - 12 - k * 4, 0, 0)), smooth=True, group="Abdomen"))
    # The gardener, rising from it: stooped, in a shirt, his apron, his flat cap.
    parts += [
        part("Waist", kit.tube([(0, -0.3, 1.66), (0, -0.52, 2.1), (0, -0.7, 2.52), (0, -0.8, 2.78)], [0.3, 0.29, 0.31, 0.27], seg=10),
             shirt, smooth=True, group="Torso"),
        part("Apron", kit.cube(), apron, M((0, -0.69, 2.02), rot=(-26, 0, 0), scale=(0.56, 0.05, 0.78)), group="Torso"),
        part("ApronStrap", kit.tube([(-0.22, -0.82, 2.38), (-0.16, -0.86, 2.62), (0, -0.8, 2.86), (0.16, -0.86, 2.62), (0.22, -0.82, 2.38)],
                                    [0.025] * 5, seg=4), apron, group="Torso"),
        part("Neck", kit.cylinder(8, 0.1, 0.22), skin, M((0, -0.86, 2.86), rot=(-30, 0, 0)), group="Head"),
        part("Head", kit.sphere(10, 8, 0.21), skin, M((0, -0.97, 3.05), scale=(0.95, 1.0, 1.08)), smooth=True, group="Head"),
        part("Nose", kit.cone(6, 0.045, 0.01, 0.12), skin, M((0, -1.19, 3.02), rot=(90, 0, 0)), group="Head"),
        part("CapCrown", kit.cylinder(12, 0.23, 0.09), cap, M((0, -0.95, 3.22), rot=(-12, 0, 0), scale=(1, 1.08, 1)), group="Head"),
        part("CapPeak", kit.cube(), cap, M((0, -1.17, 3.2), rot=(-18, 0, 0), scale=(0.3, 0.16, 0.03)), group="Head"),
    ]
    for s in (-1, 1):
        parts.append(part("Eye", kit.sphere(6, 4, 0.03), eye, M((s * 0.075, -1.16, 3.08)), smooth=True, group="Head"))
        for k in range(3):
            base = (s * 0.17, -0.9 + k * 0.05, 3.0 - k * 0.04)
            parts.append(part("Hair", kit.cone(4, 0.035, 0.0, 0.14), hair, kit.along(base, (s * 0.25, -0.86 + k * 0.06, 2.92 - k * 0.05)), group="Head"))
        # The antennae, feathered, out of his brow.
        stem = [(s * 0.06, -1.1, 3.2), (s * 0.18, -1.24, 3.52), (s * 0.36, -1.36, 3.78), (s * 0.56, -1.4, 3.9)]
        parts.append(part("Antenna", kit.tube(stem, [0.018, 0.014, 0.01, 0.005], seg=4), chitin, group="Head"))
        for k in range(1, 8):
            t = k / 8
            i = min(2, int(t * 3))
            u = t * 3 - i
            p = [stem[i][j] + (stem[i + 1][j] - stem[i][j]) * u for j in range(3)]
            for side in (-1, 1):
                parts.append(part("Barb", kit.cone(3, 0.008, 0.0, 0.12 * (1 - t * 0.5)), chitin,
                                  kit.along(tuple(p), (p[0] + side * 0.06, p[1] - 0.05, p[2] + 0.06)), group="Head"))
    # The caged lantern in his ribs: ribs curving round it, a little cage, its light.
    lantern_at = (0, -0.86, 2.5)
    parts.append(part("Lantern", kit.sphere(8, 6, 0.085), light, M(lantern_at), smooth=True, group="Torso"))
    for k in range(6):
        a = math.tau * k / 6
        parts.append(part("CageBar", kit.cylinder(4, 0.009, 0.26), cage, M((lantern_at[0] + math.cos(a) * 0.12, lantern_at[1] + math.sin(a) * 0.12, lantern_at[2])), group="Torso"))
    for z in (-0.13, 0.13):
        parts.append(part("CageRing", kit.cylinder(10, 0.13, 0.02), cage, M((lantern_at[0], lantern_at[1], lantern_at[2] + z)), group="Torso"))
    for k, z in enumerate((2.36, 2.5, 2.64)):
        for s in (-1, 1):
            arc = [(s * 0.05, -0.98, z), (s * 0.2, -0.95, z + 0.01), (s * 0.3, -0.82, z - 0.02), (s * 0.29, -0.66, z - 0.04)]
            parts.append(part("Rib", kit.tube(arc, [0.02, 0.022, 0.02, 0.015], seg=4), rib, group="Torso"))
    # The arms: shirtsleeves rolled to the elbow; from there the forelegs, long and jointed, each ending in shears.
    for s, arm, snip in ((-1, "ArmL", "SnipL"), (1, "ArmR", "SnipR")):
        sh = (s * SHOULDER[0], SHOULDER[1], SHOULDER[2])
        elbow = (s * 0.48, -1.1, 2.02)
        joint = (s * 0.56, -1.5, 1.45)
        wrist = (s * WRIST[0], WRIST[1], WRIST[2])
        parts.append(part("Sleeve", kit.tube([sh, ((sh[0] + elbow[0]) / 2, (sh[1] + elbow[1]) / 2, (sh[2] + elbow[2]) / 2), elbow], [0.1, 0.095, 0.085], seg=7),
                          shirt, smooth=True, group=arm))
        parts.append(part("Cuff", kit.sphere(8, 6, 0.1), shirt, M(elbow, scale=(1, 1, 0.8)), group=arm))
        parts.append(part("Foreleg", kit.tube([elbow, joint], [0.075, 0.06], seg=6), chitin, group=arm))
        parts.append(part("Knuckle", kit.sphere(6, 5, 0.07), chitin, M(joint), group=arm))
        parts.append(part("Foreleg", kit.tube([joint, wrist], [0.058, 0.045], seg=6), chitin, group=arm))
        for k in range(3):
            t = 0.25 + k * 0.25
            p = tuple(joint[j] + (wrist[j] - joint[j]) * t for j in range(3))
            parts.append(part("Spur", kit.cone(3, 0.02, 0.0, 0.12), chitin, kit.along(p, (p[0] + s * 0.04, p[1] + 0.07, p[2] + 0.06)), group=arm))
        # The shears: a pivot at the wrist, a fixed blade, and one that opens (its own bone).
        tip = (wrist[0], wrist[1] - 0.32, 0.12)
        parts.append(part("Pivot", kit.cylinder(8, 0.05, 0.05), shear, M(wrist, rot=(0, 90, 0)), group=arm))
        for bone, off in ((arm, -0.02), (snip, 0.02)):
            a = (wrist[0] + s * off, wrist[1], wrist[2])
            b = (tip[0] + s * off, tip[1], tip[2])
            parts.append(part("Blade", kit.tube([a, ((a[0] + b[0]) / 2, (a[1] + b[1]) / 2 - 0.03, (a[2] + b[2]) / 2), b], [0.04, 0.032, 0.006], seg=3),
                              shear, group=bone))
    # Four thin moth legs beneath (tucked in flight).
    for s in (-1, 1):
        for y in (-0.05, 0.35):
            hip, knee, foot = (s * 0.22, y, 1.2), (s * 0.95, y + 0.1, 1.15), (s * 1.15, y + 0.25, 0.02)
            parts.append(part("Leg", kit.tube([hip, knee, foot], [0.045, 0.035, 0.015], seg=5), chitin, group="Legs"))
    # The wings: dead leaves, veined, blotched, ragged; a dim edge (barely there in the dark).
    fore = [(0.35, -0.38), (1.2, -0.78), (2.2, -0.98), (2.95, -0.72), (3.05, -0.3), (2.6, 0.12), (1.6, 0.42), (0.7, 0.38), (0.32, 0.08)]
    hind = [(0.3, 0.2), (1.1, 0.34), (2.0, 0.76), (2.2, 1.32), (1.6, 1.76), (0.8, 1.62), (0.3, 0.92)]
    for s, fbone, hbone in ((-1, "WingL", "HindL"), (1, "WingR", "HindR")):
        for outline, material, bone, z0, rib_to, n_veins in ((fore, leaf, fbone, WING_Z - 0.015, (2.92, -0.55), 6),
                                                              (hind, leaf_hind, hbone, WING_Z - 0.115, (1.85, 1.35), 4)):
            edge_pts = ragged(outline, rng)
            pts = [(s * x, y) for x, y in edge_pts]
            if s < 0:
                pts = list(reversed(pts))
            parts.append(part("Wing", kit.prism(pts, 0.03), material, M((0, 0, z0)), group=bone))
            loop = [(s * x, y, z0 + 0.034) for x, y in edge_pts]
            parts.append(part("WingEdge", kit.tube(loop + [loop[0]], [0.018] * (len(loop) + 1), seg=3), edge, group=bone))
            top = z0 + 0.03
            root = (s * (outline[0][0] + 0.05), (outline[0][1] + outline[-1][1]) / 2, top + 0.012)
            tip = (s * rib_to[0], rib_to[1], top + 0.012)
            parts.append(part("Midrib", kit.tube([root, tip], [0.03, 0.01], seg=4), vein, group=bone))
            for k in range(1, n_veins + 1):
                t = k / (n_veins + 1)
                p = tuple(root[j] + (tip[j] - root[j]) * t for j in range(3))
                for side in (-1, 1):
                    end = (p[0] + s * 0.32 * (1 - t * 0.4), p[1] + side * 0.42 * (1 - t * 0.5) + 0.18, top + 0.01)
                    parts.append(part("Vein", kit.tube([p, end], [0.016, 0.004], seg=3), vein, group=bone))
            for k in range(5):
                cx = s * rng.uniform(0.9, (rib_to[0] - 0.3))
                cy = rng.uniform(min(y for _, y in outline) + 0.25, max(y for _, y in outline) - 0.25)
                r = rng.uniform(0.12, 0.26)
                parts.append(part("Blotch", kit.cylinder(9, r, 0.004), blotch if k % 3 else rot,
                                  M((cx, cy, top + 0.002 * (k + 1)), scale=(1, rng.uniform(0.6, 1.0), 1)), jitter=0.01, group=bone))
    mesh = kit.join(parts, "FatherMoth")
    armature = kit.rig("FatherMothRig", BONES)
    kit.skin(mesh, armature)
    kit.animate(armature, ACTIONS)
    return [armature, mesh]
