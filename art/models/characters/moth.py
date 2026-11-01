"""A Moth: a Soured that couldn't let go of a memory. Big, as moths go (half a metre across the wings), pale and
dusty, the wings marked with faded eye-spots and softly glowing along their edges; the same sickly green eyes as
every Soured. They come in flocks, and they come to light. Its origin is its body's centre (it flies: the game holds
it up, never lower than 0.3 m off the ground in a dive, so nothing here reaches more than about 0.25 m below it).

Clips: Fly (a wingbeat, looping; also played once, slowed to half a second, as it pulls away after a dive), Glide
(wings held out, drifting, one stroke to stay up), Stall (0.5 s: the tell, it rears and flings its wings wide,
quivering, then cocks them), Dive (0.7 s: wings swept back, a dart, then it brakes and pulls up), Stagger, Death
(the wings fold and it crumples).

How it's animated (after the Laden): each bone keyed on its own timing (kit.bake: tracks). The wings lead; their tips
(paper, hinged half-way out) lag a beat behind and whip past; the body bobs on each stroke and pitches after it; the
abdomen and the antennae trail the body. The beat is a downstroke with power (fast, the wing pressing forward and
leading edge down) and an upstroke that lags at the tips (the tips trail low, then flick up past the top).
"""
import math

M, part = kit.M, kit.part  # `kit` is injected by build_model.py

HINGE = 0.16  # how far out the wing tips are hinged (x, either side)
BONES = {
    "Root": ((0, 0, -0.05), (0, 0, 0.05), None),
    "Body": ((0, 0.08, 0), (0, -0.1, 0), "Root"),
    "WingL": ((-0.03, 0, 0.02), (-0.28, 0, 0.02), "Body"),
    "WingR": ((0.03, 0, 0.02), (0.28, 0, 0.02), "Body"),
    "WingTipL": ((-HINGE, 0, 0.02), (-0.33, 0, 0.02), "WingL"),   # the outer half of each wing: it lags and flexes
    "WingTipR": ((HINGE, 0, 0.02), (0.33, 0, 0.02), "WingR"),
    "HindL": ((-0.035, 0.045, 0.02), (-0.22, 0.2, 0.02), "WingL"),  # the hindwings: they follow the forewings, a
    "HindR": ((0.035, 0.045, 0.02), (0.22, 0.2, 0.02), "WingR"),    # little late, and fold in under them
    "Abdomen": ((0, 0.05, -0.01), (0, 0.22, -0.01), "Body"),     # trails the body
    "AntennaL": ((-0.01, -0.08, 0.03), (-0.08, -0.17, 0.07), "Body"),
    "AntennaR": ((0.01, -0.08, 0.03), (0.08, -0.17, 0.07), "Body"),
}

# Axes (rotations in degrees about world axes, around each bone's head; a child's composes on its parent's):
#   Wing*/WingTip*/Hind*, as pair() takes them (the same for either side): (twist, lift, sweep): +twist tips the leading
#     edge down, +lift raises the wing (a tip: bends it up from the wing), +sweep sweeps it back.
#   Body, Root, Abdomen: +X pitches the nose down (the abdomen: its tail up); Root +Y rolls it onto its right wing.
#   Antenna* (pair(), (droop, 0, back)): +X droops them, -X raises them (towards lying back); +back swings them back.
#   Root's offset moves the whole moth (metres): the bob of each beat, a lunge, a drop.


def pair(bone, ks, left=None):
    """Tracks for a left/right pair from keys (frame, (twist, lift, sweep)), as the axes above say; turned into
    rotations about X, Y, Z for each side (about Y, a right wing rises the other way to a left one; about Z, it sweeps
    back the other way). `left`: the left's own keys when the two differ."""
    return {bone + "R": [(f, (v[0], -v[1], v[2])) for f, v in ks],
            bone + "L": [(f, (v[0], v[1], -v[2])) for f, v in (left if left is not None else ks)]}


def keys(*parts):
    """Tracks merged: each a {bone: [(frame, value)]}."""
    merged = {}
    for p in parts:
        for bone, ks in p.items():
            merged.setdefault(bone, []).extend(ks)
    return merged


def held(pose, frame):
    """A whole pose as a key at `frame` for every bone (to start or end a clip on it)."""
    return {bone: [(frame, value)] for bone, value in pose.items()}


def clip(length, tracks, periodic=False):
    """A clip `length` frames long (30 a second) from per-bone tracks, baked to every frame."""
    return ("LINEAR", kit.bake(tracks, length, periodic))


# Fly (4 frames, 7.5 beats a second; the clip every other one starts from or comes back to). Frame 1 is the top of
# the stroke (wings up in a V, the tips still flicking up): it's what a dive's pull-up ends on, and what the slow
# one-off beat after a dive starts from. Then the downstroke, with power: the wings press down and forward, leading
# edges down, in a frame and a half, the tips bent up behind them; the body lifts on it (a bob of 2 cm) and its nose
# rises. At the bottom the tips whip on down; the upstroke is slower, the wings drawn up and back, leading edges up,
# the tips trailing low; the body sinks and its nose dips. The abdomen and antennae swing after the bob.
FLY = clip(4, keys(
    pair("Wing", [(1, (-6, 44, 4)), (2, (14, -6, -6)), (3, (4, -34, -3)), (4, (-16, 10, 8))]),
    pair("WingTip", [(1, (0, 8, 0)), (2, (0, 22, 0)), (3, (0, -12, 0)), (4, (0, -24, 0))]),
    pair("Hind", [(1, (0, -10, 0)), (2, (0, 6, -2)), (3, (0, 0, 0)), (4, (0, -12, 2))]),
    {"Root": [(1.5, ((0, 0, 0), (0, 0, -0.011))), (3.5, ((0, 0, 0), (0, 0, 0.011)))],
     "Body": [(2.5, (-6, 0, 0)), (4.5, (-1, 0, 0))],
     "Abdomen": [(1.5, (5, 0, 0)), (3.5, (-6, 0, 0))]},
    pair("Antenna", [(1.5, (-4, 0, 0)), (3.5, (6, 0, 0))]),
), periodic=True)
TOP = FLY[1][0][1]  # (the pose at its frame 1, every bone)

ACTIONS = {
    "Fly": FLY,
    # Glide (23 frames, a loop): wings held out in a shallow V, drifting. It banks slowly to the right, sinking, the
    # nose dropping, the tips flexing in the air; draws its wings up and gives one stroke (the tips lag it), rising,
    # nose up; banks back the other way and levels. The abdomen sways after the bank, the antennae after the body.
    "Glide": clip(23, keys(
        pair("Wing", [(1, (0, 14, 6)), (6, (2, 12, 5)), (11, (-6, 22, 8)), (14, (14, -14, -6)), (17, (-8, 18, 6)), (20, (0, 12, 6))],
             left=[(1, (0, 14, 6)), (6, (2, 17, 5)), (11, (-6, 24, 8)), (14, (14, -12, -6)), (17, (-8, 16, 6)), (20, (0, 15, 6))]),
        pair("WingTip", [(1, (0, 4, 0)), (5, (0, 8, 0)), (9, (0, 2, 0)), (12, (0, -12, 0)), (14.5, (0, 20, 0)), (17, (0, -10, 0)), (20, (0, 6, 0)), (22, (0, 3, 0))],
             left=[(1, (0, 4, 0)), (5, (0, 1, 0)), (9, (0, 6, 0)), (12, (0, -12, 0)), (14.5, (0, 18, 0)), (17, (0, -8, 0)), (20, (0, 5, 0)), (22, (0, 4, 0))]),
        pair("Hind", [(1, (0, -4, 0)), (7, (0, -6, 2)), (12, (0, -10, 0)), (14.5, (0, 6, 0)), (17, (0, -8, 0)), (20, (0, -3, 0))]),
        {"Root": [(1, ((0, 0, 0), (0, 0, 0))), (7, ((0, 6, -3), (0, 0, -0.015))), (12, ((0, 2, -2), (0, 0, -0.03))),
                  (17, ((0, -5, 2), (0, 0, 0.012))), (21, ((0, -2, 1), (0, 0, 0.006)))],
         "Body": [(1, (-2, 0, 0)), (9, (3, 0, 0)), (15, (-6, 0, 0)), (20, (-1, 0, 0))],
         "Abdomen": [(1, (2, 0, 0)), (6, (0, 0, 3)), (12, (-3, 0, 1)), (18, (6, 0, -3)), (22, (3, 0, -1))]},
        pair("Antenna", [(1, (0, 0, 0)), (5, (-4, 0, 2)), (13, (5, 0, -2)), (18, (-6, 0, 0)), (22, (2, 0, 0))]),
    ), periodic=True),
    # Stall (0.5 s in play: the tell; from Fly): a hard braking stroke, down and forward, and it rears back, drawn
    # back and up; then the wings are flung out wide and flat (the eye-spots shown), the tips flaring up past them and
    # settling; held there, quivering (the tips shiver), the abdomen curled under, the antennae raised at its prey.
    # At the last it cocks: rears further, the wings drawn up and back, the tips trailing (the dive comes from here).
    "Stall": clip(11, keys(held(TOP, 1), {
        "Root": [(3, ((0, 0, 0), (0, 0.01, 0.01))), (6, ((0, 0, 0), (0, 0.03, 0.03))), (8, ((0, 0, 0), (0, 0.025, 0.022))),
                 (10, ((0, 0, 0), (0, 0.03, 0.026))), (12, ((0, 0, 0), (0, 0.05, 0.04)))],
        "Body": [(3, (-12, 0, 0)), (6, (-26, 0, 0)), (8, (-22, 0, 0)), (10, (-23, 0, 0)), (12, (-30, 0, 0))],
        "Abdomen": [(4, (8, 0, 0)), (7, (-14, 0, 0)), (9, (-10, 0, 0)), (12, (-16, 0, 0))],
    }, pair("Wing", [(3, (16, -20, -14)), (5, (26, 16, -10)), (7, (30, 4, -8)), (8, (30, 6, -8)), (9, (30, 3, -8)),
                     (10, (30, 6, -8)), (12, (-4, 30, 18))]),
        pair("Hind", [(3, (0, 10, 0)), (5, (0, -8, 10)), (7, (0, -3, 14)), (9, (0, -4, 13)), (12, (0, -10, 4))]),
        pair("WingTip", [(3, (0, 24, 0)), (4, (0, -10, 0)), (6, (0, 16, 0)), (7, (0, 5, 0)), (8, (0, 9, 0)), (9, (0, 3, 0)),
                         (10, (0, 9, 0)), (11, (0, 6, 0)), (12, (0, -14, 0))]),
        pair("Antenna", [(4, (6, 0, 0)), (7, (-22, 0, 0)), (9, (-18, 0, 0)), (12, (-26, 0, 4))]))),
    # Dive (0.7 s in play: it darts on through, striking whatever it passes): from the cocked stall, the nose snaps
    # down and the wings are swept hard back along its sides, the tips whipping after them; the abdomen lags, then
    # falls in line; the antennae lie back. Held as a dart (a wobble, the tips shivering in the wind) while it flies
    # through; then it brakes: the nose comes up, the wings swing forward and up (the tips dragging), and it ends at
    # the top of a stroke, as Fly begins (the slow beat that follows pulls it away).
    "Dive": clip(8, keys(held(TOP, 9), {
        "Root": [(1, ((0, 0, 0), (0, 0.05, 0.04))), (2, ((0, 0, 0), (0, -0.03, 0.01))), (3, ((0, 0, 0), (0, -0.05, -0.01))),
                 (4, ((0, 3, 0), (0, -0.04, -0.008))), (5, ((0, -3, 0), (0, -0.04, -0.01))), (6, ((0, 2, 0), (0, -0.04, -0.008))),
                 (8, ((0, 0, 0), (0, -0.01, 0.02)))],
        "Body": [(1, (-30, 0, 0)), (2, (10, 0, 0)), (3, (32, 0, 0)), (4, (27, 0, 0)), (6, (28, 0, 0)), (7, (4, 0, 0)), (8, (-12, 0, 0))],
        "Abdomen": [(1, (-16, 0, 0)), (2, (-24, 0, 0)), (4, (8, 0, 0)), (5, (2, 0, 0)), (6, (4, 0, 0)), (7, (14, 0, 0)), (8, (10, 0, 0))],
    }, pair("Wing", [(1, (-4, 30, 18)), (2, (4, 8, 40)), (3, (2, -12, 52)), (5, (2, -10, 48)), (6, (2, -12, 50)), (7, (-10, 18, 24)), (8, (-8, 40, 6))]),
        pair("WingTip", [(1, (0, -14, 0)), (2, (0, 10, -22)), (3, (0, -8, 18)), (4, (0, -4, 8)), (5, (0, -1, 12)), (6, (0, -5, 9)),
                         (7, (0, -12, 24)), (8, (0, -18, 10))]),
        pair("Hind", [(1, (0, -10, 4)), (2, (0, -2, -18)), (3, (0, -8, -36)), (6, (0, -7, -34)), (7, (0, -14, -12)), (8, (0, -12, 0))]),
        pair("Antenna", [(1, (-26, 0, 4)), (2, (-10, 0, 10)), (3, (-55, 0, 20)), (6, (-50, 0, 18)), (7, (-30, 0, 10)), (8, (6, 0, 0))]))),
    # Stagger (0.27 s as made): knocked tumbling, rolled onto its left and back, the wings flailing out of step (the
    # right flung up, the left swept down and back), the tips flapping after them; it beats twice to right itself
    # and comes back to the top of a stroke.
    "Stagger": clip(8, keys(held(TOP, 1), held(TOP, 9), {
        "Root": [(2, ((0, 30, 0), (0, 0.04, 0.03))), (3, ((-10, 42, 6), (0, 0.06, 0.035))), (5, ((4, -14, -4), (0, 0.04, 0.0))),
                 (7, ((0, 6, 0), (0, 0.015, 0.005)))],
        "Body": [(2, (-18, 0, 0)), (4, (-24, 0, 0)), (6, (8, 0, 0)), (8, (-4, 0, 0))],
        "Abdomen": [(2, (10, 0, -8)), (4, (-14, 0, 10)), (6, (6, 0, -4)), (8, (-2, 0, 0))],
    }, pair("Wing", [(2, (-10, 62, -10)), (4, (10, -8, 10)), (6, (-10, 40, 0)), (7, (10, -20, -4))],
            left=[(2, (10, -24, 20)), (3, (10, -30, 24)), (5, (-8, 40, 0)), (7, (10, -26, -4))]),
        pair("WingTip", [(2, (0, -26, 0)), (3, (0, -10, 0)), (4, (0, 24, 0)), (6, (0, -18, 0)), (7, (0, 16, 0))],
             left=[(2, (0, 24, 0)), (4, (0, -6, 0)), (5, (0, -20, 0)), (7, (0, 18, 0))]),
        pair("Hind", [(2, (0, -16, 0)), (4, (0, 12, 0)), (6, (0, -10, 0)), (7, (0, 8, 0))],
             left=[(2, (0, 14, 0)), (3, (0, 6, 0)), (5, (0, -14, 0)), (7, (0, 8, 0))]),
        pair("Antenna", [(2, (-30, 0, 10)), (4, (20, 0, -4)), (6, (-8, 0, 0))]))),
    # Death (0.45 s, then held while it dissolves): a jolt (thrown back, rolled to its left, the wings flung up high,
    # the paper tips snapping down behind them); one feeble half-beat; then the wings fold up together over its back,
    # the tips curling in like burnt paper, the abdomen curls under and the antennae droop, and it tumbles over onto
    # its right side, dropping; a last twitch of the tips, and it lies crumpled in the air.
    "Death": clip(13, keys(held(TOP, 1), {
        "Root": [(2, ((0, -14, 0), (0, 0.03, 0.03))), (4, ((0, -24, 0), (0, 0.05, 0.035))), (7, ((10, 20, -8), (0, 0.04, -0.03))),
                 (10, ((16, 62, -14), (0, 0.03, -0.06))), (12, ((18, 72, -16), (0, 0.03, -0.085))), (14, ((16, 70, -16), (0, 0.03, -0.08)))],
        "Body": [(2, (-22, 0, 0)), (4, (-30, 0, 0)), (7, (10, 0, 0)), (10, (32, 0, 0)), (12, (38, 0, 0)), (14, (35, 0, 0))],
        "Abdomen": [(2, (14, 0, 0)), (4, (10, 0, 0)), (7, (-20, 0, 0)), (10, (-38, 0, 6)), (12, (-44, 0, 8)), (14, (-40, 0, 8))],
    }, pair("Wing", [(2, (-14, 70, -6)), (4, (-10, 80, -4)), (6, (0, 58, 10)), (9, (6, 82, 22)), (11, (6, 90, 26)), (14, (6, 86, 24))],
            left=[(2, (-14, 66, -4)), (4, (-10, 76, -2)), (6, (0, 50, 6)), (9, (8, 78, 18)), (11, (8, 84, 20)), (14, (8, 80, 20))]),
        pair("WingTip", [(2, (0, -28, 0)), (4, (0, -10, 0)), (6, (0, 16, 0)), (9, (0, 14, 0)), (11, (0, 20, 4)), (12, (0, 12, 2)), (14, (0, 18, 4))],
             left=[(2, (0, -24, 0)), (4, (0, -12, 0)), (6, (0, 20, 0)), (9, (0, 18, 0)), (11, (0, 24, 4)), (12, (0, 16, 2)), (14, (0, 22, 4))]),
        pair("Hind", [(2, (0, -18, 0)), (4, (0, -6, 0)), (6, (0, 10, 0)), (9, (0, 4, -6)), (11, (0, 8, -8)), (14, (0, 6, -8))]),
        pair("Antenna", [(2, (-34, 0, 10)), (4, (-20, 0, 8)), (7, (10, 0, 0)), (10, (36, 0, -6)), (14, (42, 0, -8))]))),
}


# ---------------------------------------------------------------- the wings split at the hinge

def cut(a, b):
    t = (HINGE - a[0]) / (b[0] - a[0])
    return (HINGE, a[1] + t * (b[1] - a[1]))


def clip_outline(points, outer):
    """The part of a closed outline (right side, x > 0) inside or outside the hinge (Sutherland-Hodgman)."""
    keep = (lambda p: p[0] >= HINGE) if outer else (lambda p: p[0] <= HINGE)
    out = []
    for i, cur in enumerate(points):
        prev = points[i - 1]
        if keep(cur):
            if not keep(prev):
                out.append(cut(prev, cur))
            out.append(cur)
        elif keep(prev):
            out.append(cut(prev, cur))
    return [p for i, p in enumerate(out) if math.dist(p, out[i - 1]) > 1e-6]


def split_line(points):
    """An open polyline (right side) in runs either side of the hinge: [(outer?, points)]."""
    side = lambda p: 0 if abs(p[0] - HINGE) < 1e-9 else (1 if p[0] > HINGE else -1)
    runs, run, s = [], [points[0]], side(points[0])
    for p in points[1:]:
        sp = side(p)
        if s == 0:
            s = sp
        if sp == 0 or sp == s:
            run.append(p)
            continue
        if side(run[-1]) == 0:
            runs.append((s > 0, run))
            run = [run[-1], p]
        else:
            c = cut(run[-1], p)
            runs.append((s > 0, run + [c]))
            run = [c, p]
        s = sp
    runs.append((s > 0, run))
    return [(outer, r) for outer, r in runs if len(r) > 1]


def build():
    body, dust = kit.mat("MothBody", "#6a5e52"), kit.mat("MothWing", "#cbbf9f")
    spot, edge = kit.mat("MothSpot", "#5a4a3a"), kit.mat("MothEdge_Glow", "#e8dcb8")
    eye, fur = kit.mat("SouredEye_Glow", "#b6f06a"), kit.mat("MothFur", "#9a8c74")
    parts = [
        part("Thorax", kit.sphere(8, 6, 0.06), fur, M((0, 0.02, 0), scale=(1, 1.1, 0.9)), smooth=True, group="Body"),
        part("Abdomen", kit.cone(8, 0.05, 0.015, 0.18), body, M((0, 0.13, -0.01), rot=(90, 0, 0)), smooth=True, group="Abdomen"),
        part("Head", kit.sphere(8, 6, 0.035), fur, M((0, -0.06, 0.005)), smooth=True, group="Body"),
    ]
    for s, side in ((-1, "L"), (1, "R")):
        mirror = lambda pts: [(s * x, y) for x, y in pts]
        parts.append(part("Eye", kit.sphere(6, 4, 0.014), eye, M((s * 0.022, -0.09, 0.015)), smooth=True, group="Body"))
        parts.append(part("Antenna", kit.tube([(s * 0.01, -0.08, 0.03), (s * 0.05, -0.15, 0.08), (s * 0.08, -0.17, 0.07)], [0.004] * 3, seg=3),
                          body, group="Antenna" + side))
        # Fore and hind wings: flat fans out to the side, eye-spots, a glowing edge. The forewing cut at the hinge, its
        # outer half on the tip's bone; the hindwing on its own.
        fore = [(0.03, -0.035), (0.17, -0.1), (0.29, -0.11), (0.33, -0.02), (0.27, 0.06), (0.12, 0.07), (0.04, 0.04)]
        hind = [(0.03, 0.04), (0.16, 0.06), (0.25, 0.13), (0.22, 0.22), (0.12, 0.25), (0.04, 0.15)]
        spots = [(0.2, -0.03, 0.04), (0.15, 0.15, 0.03)]
        bone = lambda outer: ("WingTip" if outer else "Wing") + side
        for outer in (False, True):
            pts = mirror(clip_outline(fore, outer))
            parts.append(part("Forewing", kit.prism(pts if s > 0 else list(reversed(pts)), 0.006), dust, M((0, 0, 0.018)), group=bone(outer)))
        for outer, run in split_line(fore[1:]):
            loop = [(x, y, 0.024) for x, y in mirror(run)]
            parts.append(part("ForewingEdge", kit.tube(loop, [0.005] * len(loop), seg=3), edge, group=bone(outer)))
        pts = mirror(hind)
        parts.append(part("Hindwing", kit.prism(pts if s > 0 else list(reversed(pts)), 0.006), dust, M((0, 0, 0.018)), group="Hind" + side))
        loop = [(x, y, 0.024) for x, y in mirror(hind[1:])]
        parts.append(part("HindwingEdge", kit.tube(loop, [0.005] * len(loop), seg=3), edge, group="Hind" + side))
        for (cx, cy, r), on_hind in zip(spots, (False, True)):  # (octagons, as kit.cylinder(8) makes them)
            octagon = [(cx + r * math.cos(k * math.pi / 4), cy + r * math.sin(k * math.pi / 4)) for k in range(8)]
            for outer in ((None,) if on_hind else (False, True)):
                pts = octagon if on_hind else clip_outline(octagon, outer)
                if len(pts) < 3:
                    continue
                pts = mirror(pts)
                parts.append(part("EyeSpot", kit.prism(pts if s > 0 else list(reversed(pts)), 0.004), spot, M((0, 0, 0.024)),
                                  group="Hind" + side if on_hind else bone(outer)))
    mesh = kit.join(parts, "Moth")
    armature = kit.rig("MothRig", BONES)
    kit.skin(mesh, armature)
    kit.animate(armature, ACTIONS)
    return [armature, mesh]
