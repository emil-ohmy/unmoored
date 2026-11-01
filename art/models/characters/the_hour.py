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
slumps, head and arms falling). The mask's hands are bones (DialHour, DialMinute, turned about DialTip): the game turns
them, so no clip ever keys them (they stay at rest on the Head).

How it's animated (the Laden's way: see laden.py): each bone keyed on its own timing (kit.bake: tracks), so nothing
moves at once. It's a body hung on cables: the Hang swings it like a pendulum, the torso follows a few frames late and
the head after that; its levers move like a clock's, in ticks (a hold, a quick step, a hold), and swing with weight. A
bone named "Torso+..." is a layer added onto the Torso's track (a tick, a shudder, on its own timing). Its loose parts
aren't keyed but swung after the body every frame (damped springs, `swing`): the works below the robe (a pendulum from
the waist, its keyed turn kept: the grind's spin), the green lantern on its chain at the works' point (a true pendulum,
resting on the floor if it gets there), and the cables (their middles bow after the shoulders they hold; their tops are
fixed in the contraption, their feet on the shoulders). Where a weapon rests on the floor (limp, the slam's landing,
dead) it's keyed where it lies ("_SpotL"/"_SpotR": its middle and its lowest point, how much by "_LayL"/"_LayR") and the
arm is bent onto it every frame (kit.reach), so it stays put however the body sways; the build reports how far any
clip's weapon comes off its spot, and how low each weapon and the lantern go.
"""
import math

from mathutils import Euler, Vector

M, part = kit.M, kit.part  # `kit` is injected by build_model.py

TOP = 9.5            # the cables' anchor
WAIST = 4.4          # where the robe ends and the works begin
SHOULDER = 5.75
MASK = (0, -0.42, 6.45)
ELBOW_L, HAND_L = (-1.35, -1.7, 4.15), (-0.25, -4.25, 1.05)   # the hammer arm (its head on the floor at y -4.6)
ELBOW_R, HAND_R = (1.55, -1.5, 4.2), (2.2, -3.6, 1.0)          # the blade arm (the blade low)
CABLE_TOP, CABLE_MID, CABLE_FOOT = (0, 0.1, TOP + 1.2), (0, 0.23, 8.0), (0, 0.35, SHOULDER - 0.3)
LANTERN_PIVOT = (0, 0, 0.87)
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
    "Cables": (CABLE_TOP, CABLE_MID, "Root"),          # the cables' middles (swung after the shoulders they hold)
    "Lantern": (LANTERN_PIVOT, (0, 0, 0.44), "Works"),  # the green lantern on its chain at the works' point
}

# ---------------------------------------------------------------- the weapons, where they are (for laying them down)

HAMMER_MID = Vector((HAND_L[0] + 0.25, HAND_L[1] - 0.35, 0.45))   # the hammer head's middle (radius 0.45, 1.3 long, along x)
HAMMER_PTS = [HAMMER_MID + Vector((dx, r * math.cos(a), r * math.sin(a)))
              for dx, r in ((-0.65, 0.45), (-0.6, 0.48), (0, 0.45), (0.6, 0.48), (0.65, 0.45))
              for a in (2 * math.pi * k / 16 for k in range(16))]
BLADE_CENTRE, BLADE_R = (HAND_R[0] + 0.2, HAND_R[1] - 0.6, 0.6), 1.25


def blade_outer(t):
    a = math.radians(-150 + 120 * t)
    return (BLADE_CENTRE[0] + math.cos(a) * BLADE_R, BLADE_CENTRE[1] + math.sin(a) * BLADE_R)


def blade_inner(t):
    a = math.radians(-138 + 96 * t)
    return (BLADE_CENTRE[0] + math.cos(a) * 0.98, BLADE_CENTRE[1] + 0.32 + math.sin(a) * 0.98)


BLADE_PTS = [Vector((x, y, z)) for t in (i / 8 for i in range(9)) for (x, y) in (blade_outer(t), blade_inner(t))
             for z in (BLADE_CENTRE[2] - 0.04, BLADE_CENTRE[2] + 0.04)]
BLADE_MID = Vector((BLADE_CENTRE[0], BLADE_CENTRE[1] - 0.95, BLADE_CENTRE[2]))   # the crescent's middle
LANTERN_LOW = Vector((0, 0, 0.43))
SIDES = {"L": ("ArmL", "ForeL", HAMMER_PTS, HAMMER_MID), "R": ("ArmR", "ForeR", BLADE_PTS, BLADE_MID)}


def rest_pole(upper, fore):
    """The way the elbow bows at rest (a world direction off the shoulder-hand line)."""
    s, e, h = Vector(BONES[upper][0]), Vector(BONES[upper][1]), Vector(BONES[fore][1])
    d = (h - s).normalized()
    p = (e - s) - d * (e - s).dot(d)
    return p.normalized()


POLE = {"L": rest_pole("ArmL", "ForeL"), "R": rest_pole("ArmR", "ForeR")}


def mats(pose):
    return kit.pose_matrices(BONES, pose)


def lowest(m, bone, pts):
    return min((m[bone] @ p).z for p in pts)


def lay(side, pose, spot):
    """The pose with one arm bent (kit.reach) so its weapon lies at `spot`: (x, y) its middle, z its lowest point."""
    upper, fore, pts, mid = SIDES[side]
    hand = Vector(BONES[fore][1])
    torso = mats(pose)["Torso"].to_quaternion()
    pole = torso @ POLE[side]
    target = hand + Vector((spot[0] - mid.x, spot[1] - mid.y, spot[2] - min(p.z for p in pts)))
    for _ in range(5):
        laid = kit.reach(BONES, pose, upper, fore, lambda m, t=target.copy(): t, pole)
        m = mats(laid)
        c = m[fore] @ mid
        target += Vector((spot[0] - c.x, spot[1] - c.y, spot[2] - lowest(m, fore, pts)))
    return laid


def laid_arm(side, pose, spot):
    """The two arm rotations that lay a weapon at `spot` with the body in `pose` (to key an arm's FK onto it)."""
    upper, fore, _, _ = SIDES[side]
    laid = lay(side, pose, spot)
    return tuple(round(v, 3) for v in laid[upper]), tuple(round(v, 3) for v in laid[fore])


# ---------------------------------------------------------------- keys and tracks

def rot_of(value):
    return tuple(value[0]) if isinstance(value[0], (tuple, list)) else tuple(value)


def add(a, b):
    """Two values (a rotation, or (rotation, offset)) added component by component."""
    if isinstance(a[0], (tuple, list)) or isinstance(b[0], (tuple, list)):
        ra, oa = (a if isinstance(a[0], (tuple, list)) else (a, (0, 0, 0)))
        rb, ob = (b if isinstance(b[0], (tuple, list)) else (b, (0, 0, 0)))
        return (tuple(x + y for x, y in zip(ra, rb)), tuple(x + y for x, y in zip(oa, ob)))
    return tuple(x + y for x, y in zip(a, b))


def mix(a, b, t):
    """Between two values (rotations, or (rotation, offset)), `t` of the way."""
    if isinstance(a[0], (tuple, list)):
        return (tuple(x + (y - x) * t for x, y in zip(a[0], b[0])), tuple(x + (y - x) * t for x, y in zip(a[1], b[1])))
    return tuple(x + (y - x) * t for x, y in zip(a, b))


def held(pose, frame):
    """A whole pose as a key at `frame` for every bone (to start or end a clip on it)."""
    return {bone: [(frame, value)] for bone, value in pose.items()}


def keys(*parts):
    """Tracks merged: each a {bone: [(frame, value)]}."""
    merged = {}
    for p in parts:
        for bone, ks in p.items():
            merged.setdefault(bone, []).extend(ks)
    return merged


def ratchet(start, end, a, b, steps, step=2):
    """A lever moved like a clock's: from `a` (held from frame `start`) to `b` (reached at `end`) in `steps` ticks, each a
    quick move `step` frames long, then a hold."""
    out = [(start, a)]
    span = (end - start) / steps
    for i in range(steps):
        go = start + round(span * (i + 1)) - step
        out += [(go, mix(a, b, i / steps)), (go + step, mix(a, b, (i + 1) / steps))]
    return out


def with_(pose, **changes):
    merged = dict(pose)
    merged.update(changes)
    return merged


def weight(*ks):
    """A pseudo-track's keys (how much a weapon is laid on its spot), from (frame, w) pairs."""
    return [(f, (w, 0, 0)) for f, w in ks]


def spot(*ks):
    """A weapon's spot over time, from (frame, (x, y, lowest)) pairs."""
    return [(f, ((0, 0, 0), tuple(s))) for f, s in ks]


# ---------------------------------------------------------------- loose parts: swung after the body

FPS = 30


def follow(pivots, targets, length, omega, zeta, periodic, floor=None, lag=0.5):
    """A point hung `length` from a moving pivot, pulled towards a moving target by a damped spring (`omega` its
    stiffness in rad/s, `zeta` its damping; `lag`: how much of the target's own speed it ignores, so it trails a move
    and overshoots its stop). Frame by frame (substeps between); a loop's run several times round so it joins up."""
    n = len(pivots)
    count = n - 1 if periodic else n
    b, v = targets[0].copy(), Vector((0, 0, 0))
    sub, dt = 10, 1.0 / FPS / 10
    out = []
    for _ in range(5 if periodic else 1):
        out = []
        for i in range(count):
            out.append(b.copy())
            j = (i + 1) % count if periodic else min(i + 1, n - 1)
            tv = (targets[j] - targets[i]) * FPS * (1 - lag)
            for s in range(sub):
                t = (s + 1) / sub
                p, tg = pivots[i].lerp(pivots[j], t), targets[i].lerp(targets[j], t)
                prev = b.copy()
                v += (omega * omega * (tg - b) - 2 * zeta * omega * (v - tv)) * dt
                b += v * dt
                b = p + (b - p).normalized() * length
                if floor is not None and b.z < floor:
                    b.z = floor
                v = (b - prev) / dt
    if periodic:
        out.append(out[0].copy())
    return out


def swing(frames, bone, periodic, omega, zeta, target=None, floor=None, lag=0.5):
    """Swing `bone` after the body: its tip follows (`follow`) where its keys put it (or `target(m)`), lagging, and
    its rotation is turned onto it (its keyed twist kept)."""
    head, tail, parent = BONES[bone]
    head, tail = Vector(head), Vector(tail)
    ms = [mats(p) for _, p in frames]
    pivots = [m[bone] @ head for m in ms]
    tips = [m[bone] @ tail for m in ms]
    targets = [target(m, pv) if target else tp for m, pv, tp in zip(ms, pivots, tips)]
    bobs = follow(pivots, targets, (tail - head).length, omega, zeta, periodic, floor, lag)
    out, prev = [], None
    for (f, p), m, pv, tp, bob in zip(frames, ms, pivots, tips, bobs):
        keyed = Euler([math.radians(a) for a in rot_of(p.get(bone, (0, 0, 0)))]).to_quaternion()
        up = m[parent].to_quaternion() if parent else Euler((0, 0, 0)).to_quaternion()
        turn = (tp - pv).rotation_difference(bob - pv)
        q = up.inverted() @ turn @ up @ keyed
        e = q.to_euler("XYZ", prev) if prev is not None else q.to_euler("XYZ")
        if prev is None:  # (keep a keyed spin's whole turns: start from the keyed angles)
            k = Euler([math.radians(a) for a in rot_of(p.get(bone, (0, 0, 0)))])
            e = q.to_euler("XYZ", k)
        prev = e
        value = p.get(bone, (0, 0, 0))
        offset = tuple(value[1]) if isinstance(value[0], (tuple, list)) else (0, 0, 0)
        p = dict(p)
        p[bone] = (tuple(math.degrees(a) for a in e), offset)
        out.append((f, p))
    return out


def hang_down(m, pivot):
    return pivot + Vector((0, 0, -(Vector(BONES["Lantern"][0]) - Vector(BONES["Lantern"][1])).length))


def cable_line(m, pivot):
    """Where the cables' middle would be, drawn straight from the contraption to the shoulders."""
    foot = m["Torso"] @ Vector(CABLE_FOOT)
    t = (CABLE_TOP[2] - CABLE_MID[2]) / (CABLE_TOP[2] - CABLE_FOOT[2])
    return pivot.lerp(foot, t)


# ---------------------------------------------------------------- clips

REPORT = {}   # (the build's: per clip, how low each weapon and the lantern go, how far a laid weapon is off its spot)


def blend(a, b, w):
    qa = Euler([math.radians(x) for x in a]).to_quaternion()
    qb = Euler([math.radians(x) for x in b]).to_quaternion()
    e = qa.slerp(qb, w).to_euler("XYZ", Euler([math.radians(x) for x in a]))
    return tuple(math.degrees(x) for x in e)


def clip(length, tracks, periodic=False, name=None):
    """A clip `length` frames long (30 a second) from per-bone tracks, baked to every frame: layers added, weapons laid
    where they're keyed to lie, and the loose parts swung after the body."""
    frames = []
    off = 0.0
    tracks = {bone: sorted(dict(ks).items()) for bone, ks in tracks.items()}  # (a frame keyed twice: the later key)
    for f, p in kit.bake(tracks, length, periodic):
        p = dict(p)
        for bone in [b for b in p if "+" in b]:
            base = bone.split("+")[0]
            p[base] = add(p.get(base, ((0, 0, 0), (0, 0, 0))), p.pop(bone))
        for side in "LR":
            w = p.pop("_Lay" + side, ((0, 0, 0), (0, 0, 0)))[0][0]
            at = p.pop("_Spot" + side, None)
            if w > 1e-3 and at is not None:
                upper, fore, pts, mid = SIDES[side]
                laid = lay(side, p, at[1])
                for b in (upper, fore):
                    p[b] = (blend(rot_of(p.get(b, (0, 0, 0))), rot_of(laid[b]), min(1.0, w)), (0, 0, 0))
                if w > 0.999:
                    m = mats(p)
                    c = m[fore] @ mid
                    off = max(off, math.hypot(c.x - at[1][0], c.y - at[1][1]), abs(lowest(m, fore, pts) - at[1][2]))
        frames.append((f, p))
    frames = swing(frames, "Works", periodic, omega=4.2, zeta=0.32)
    frames = swing(frames, "Lantern", periodic, omega=5.3, zeta=0.2, target=hang_down, floor=0.12, lag=1.0)
    frames = swing(frames, "Cables", periodic, omega=6.0, zeta=0.25, target=cable_line, lag=0.3)
    low = {"hammer": 99.0, "blade": 99.0, "lantern": 99.0}
    for f, p in frames:
        m = mats(p)
        low["hammer"] = min(low["hammer"], lowest(m, "ForeL", HAMMER_PTS))
        low["blade"] = min(low["blade"], lowest(m, "ForeR", BLADE_PTS))
        low["lantern"] = min(low["lantern"], (m["Lantern"] @ LANTERN_LOW).z)
    REPORT[len(REPORT)] = (low, off, frames)
    return ("LINEAR", frames)


# ---------------------------------------------------------------- poses

# Poised (awake, time running): hung upright, leaning a little, its dial face turned down on the floor (on you), the
# hammer raised to the middle, the blade ready low at its side. Every fight clip starts or ends here (Idle's frame 1).
POISED = {"Hang": ((0, 0, 0), (0, 0, 0)), "Torso": (6, 0, 0), "Head": (30, 0, 0), "ArmL": (-50, 0, -10), "ForeL": (-35, 0, 0),
          "ArmR": (-25, 0, -15), "ForeR": (-15, 0, 0), "Works": (0, 0, 0)}
# Limp (time stopped): a husk on its cables, slumped forward, the head hung on its chest, its weapons lying on the floor
# where they fell (the arms bent slack onto them). Limp's frame 1 (the twitch and the wake start from it, end on it).
LIMP_BODY = {"Hang": ((0, 0, -4), (0, 0, 0)), "Torso": (28, 0, 6), "Head": (40, 0, 10), "Works": (0, 0, 0)}
LIMP_SPOT_L, LIMP_SPOT_R = (-1.65, -4.75, 0.02), (2.7, -5.1, 0.03)
LIMP_L, LIMP_R = laid_arm("L", LIMP_BODY, LIMP_SPOT_L), laid_arm("R", LIMP_BODY, LIMP_SPOT_R)
LIMP = with_(LIMP_BODY, ArmL=LIMP_L[0], ForeL=LIMP_L[1], ArmR=LIMP_R[0], ForeR=LIMP_R[1])
LAID_LIMP = {"_LayL": weight((1, 1)), "_LayR": weight((1, 1)), "_SpotL": spot((1, LIMP_SPOT_L)), "_SpotR": spot((1, LIMP_SPOT_R))}

# The hammer: raised high and back over the head (the body arched back under it); brought down on the floor in front
# (4.6 m out, its middle on the centre line), the body thrown over after it.
HAMMER_UP = {"Hang": ((-1.5, 0, 0), (0, 0, 0.06)), "Torso": (-12, 0, 2), "Head": (26, 0, 0), "ArmL": (-152, 0, 6), "ForeL": (-38, 0, 0),
             "ArmR": (-34, 0, -6), "ForeR": (-22, 0, 0), "Works": (0, 0, 0)}
SLAM_SPOT = (0.0, -4.6, 0.0)
SLAM_BODY = {"Hang": ((-2.5, 0, 0), (0, 0, -0.08)), "Torso": (15, 0, 0)}
SLAM_ARM = laid_arm("L", SLAM_BODY, SLAM_SPOT)
SLAM_DEEP = {"Hang": ((-1.0, 0, 0), (0, 0, -0.14)), "Torso": (19, 0, -1)}   # (the body carried on over it, a frame later)
# The blade: drawn round to its right and back (the body wound round after it, the head kept on you); swept round in
# front, low, to its left.
BLADE_BACK = {"Hang": ((0, 0, 6), (0, 0, 0.04)), "Torso": (1, 0, 34), "Head": (26, 0, -28), "ArmR": (-14, 0, 96), "ForeR": (-6, 0, 14),
              "ArmL": (-80, 0, -72), "ForeL": (-30, 0, 0), "Works": (0, 0, 0)}
BLADE_ACROSS = {"Hang": ((0, 0, -6), (0, 0, -0.03)), "Torso": (2, 0, -30), "Head": (30, 0, 16), "ArmR": (-3, 0, -64), "ForeR": (0, 0, -16),
                "ArmL": (-60, 0, -10), "ForeL": (-30, 0, 0), "Works": (0, 0, 0)}
# The grind: drawn up into itself, its works wound back; then dropped, the works spun round.
GRIND_UP = {"Hang": ((0, 0, 0), (0, 0, 0.32)), "Torso": (-6, 0, 0), "Head": (44, 0, 0), "ArmL": (-80, 0, -24), "ForeL": (-60, 0, 0),
            "ArmR": (-62, 0, 26), "ForeR": (-50, 0, 0), "Works": (0, 0, -45)}
# Stop: both arms flung up and wide, the mask thrust down on you.
STOP = {"Hang": ((-2.5, 0, 0), (0, 0, 0.12)), "Torso": (-6, 0, 0), "Head": (46, 0, 0), "ArmL": (-128, 0, 50), "ForeL": (-14, 0, 0),
        "ArmR": (-128, 0, -50), "ForeR": (-14, 0, 0), "Works": (0, 0, 0)}
STOP_GATHER = {"Hang": ((1, 0, 0), (0, 0, -0.08)), "Torso": (18, 0, 0), "Head": (50, 0, 0), "ArmL": (-30, 0, 30), "ForeL": (-58, 0, 0),
               "ArmR": (-30, 0, -30), "ForeR": (-58, 0, 0)}
# Rewind: drawn in on itself, the levers folded.
REWIND = {"Hang": ((1, 0, 0), (0, 0, 0.1)), "Torso": (16, 0, 0), "Head": (22, 0, 0), "ArmL": (-30, 0, 24), "ForeL": (-62, 0, 0),
          "ArmR": (-30, 0, -24), "ForeR": (-62, 0, 0), "Works": (0, 0, 0)}
# The quarter strikes: thrown open like a struck bell, head back, arms wide.
CHIME = {"Hang": ((-2, 0, 0), (0, 0, 0.22)), "Torso": (-16, 0, 0), "Head": (-24, 0, 0), "ArmL": (-70, 0, -48), "ForeL": (-8, 0, 0),
         "ArmR": (-70, 0, 48), "ForeR": (-8, 0, 0), "Works": (0, 0, 0)}
CHIME_GATHER = {"Hang": ((1, 0, 0), (0, 0, -0.1)), "Torso": (16, 0, 0), "Head": (44, 0, 0), "ArmL": (-36, 0, 16), "ForeL": (-56, 0, 0),
                "ArmR": (-30, 0, -16), "ForeR": (-48, 0, 0)}
# Dead: the cables let it down, slumped over, everything hanging; its weapons lie where they fell.
DEAD_BODY = {"Hang": ((2, 0, 10), (0, 0, -0.42)), "Torso": (44, 0, 10), "Head": (52, 0, 14), "Works": (0, 0, 0)}
DEAD_SPOT_L, DEAD_SPOT_R = (-1.6, -5.6, 0.0), (2.9, -5.6, 0.0)
DEAD_L, DEAD_R = laid_arm("L", DEAD_BODY, DEAD_SPOT_L), laid_arm("R", DEAD_BODY, DEAD_SPOT_R)


def arms(left=None, right=None):
    """Arm rotations as a pose's piece: left/right = (upper, fore)."""
    out = {}
    if left:
        out.update(ArmL=left[0], ForeL=left[1])
    if right:
        out.update(ArmR=right[0], ForeR=right[1])
    return out


ACTIONS = {
    # Idle (3 s loop): hung and poised, alive the way a clock is. It sways slowly on its cables (side to side, and a
    # little round), the torso a few frames behind; it winds itself up (the shoulders rise, slowly, the hammer creeping
    # up with them) and lets go with a tick (it drops a notch, the hammer jolts, the head nods after); the head looks
    # round in ticks (a hold, a quick turn, a hold); the blade's lever clicks out and creeps back. The works and the
    # lantern swing after the sway; the cables bow.
    "Idle": clip(90, {
        "Hang": [(1, ((0, 0, 0), (0, 0, 0))), (23, ((0.5, 1.4, 0.8), (0, 0, 0.01))), (46, ((0, 0, 0), (0, 0, 0))),
                 (69, ((-0.5, -1.4, -0.8), (0, 0, -0.01)))],
        "Torso": [(1, (6, 0, 0)), (30, (4, -0.5, 0.4)), (39, (3, -0.4, 0.2)), (42, (8, 0.2, 0)), (46, (5.5, 0.4, -0.2)),
                  (51, (6.4, 0.5, -0.4)), (76, (6.2, 0.6, -0.4))],
        "Head": [(1, (30, 0, 0)), (12, (30, 0, 0)), (14, (29, 0, 8)), (26, (29, 0, 8)), (28, (32, 0, 13)), (41, (31, 0, 13)),
                 (44, (36, 0, 4)), (49, (33, 0, 3)), (57, (33, 0, 3)), (59, (31, 0, -8)), (71, (31, 0, -8)), (73, (30, 0, -12)),
                 (84, (30, 0, -12)), (87, (30, 0, -1))],
        "ArmL": [(1, (-50, 0, -10)), (13, (-50.5, 0, -9)), (39, (-53, 0, -10)), (43, (-47, 0, -10)), (47, (-50.5, 0, -10)), (52, (-49.5, 0, -10)),
                 (74, (-49.5, 0, -11))],
        "ForeL": [(1, (-35, 0, 0)), (16, (-35.5, 0, 0.6)), (41, (-37, 0, 0)), (45, (-31, 0, 0)), (49, (-37, 0, 0)), (54, (-34.5, 0, 0)),
                  (78, (-35, 0, -0.6))],
        "ArmR": [(1, (-25, 0, -15)), (26, (-26, 0, -14)), (41, (-25.5, 0, -14.5)), (44, (-22.5, 0, -15)), (49, (-25.5, 0, -15.5)),
                 (72, (-25, 0, -16))],
        "ForeR": [(1, (-15, 0, 0)), (43, (-15, 0, 0)), (46, (-11, 0, 0)), (51, (-16, 0, 0)), (55, (-15, 0, 0)), (61, (-15, 0, 0)),
                  (63, (-15, 0, -8)), (67, (-15, 0, -6)), (76, (-15, 0, -6.5)), (86, (-15, 0, -0.5))],
    }, periodic=True),
    # Limp (4 s loop; time stopped): a dead husk dangling. It sways on its cables and turns slowly on them, one way and
    # back (a hung thing does); the slumped torso and the hanging head roll after the sway, late, loose; its weapons
    # lie on the floor where they fell, the slack arms bent onto them whatever the body does; once round, something in
    # it slips (the head drops a notch, wrongly, and slowly sags back).
    "Limp": clip(119, keys(LAID_LIMP, {
        "Hang": [(1, ((0, 0, -4), (0, 0, 0))), (31, ((0.8, 2.0, 0), (0, 0, 0))), (61, ((0, 0, 4), (0, 0, 0))), (91, ((-0.8, -2.0, 0), (0, 0, 0)))],
        "Torso": [(1, (28, 0, 6)), (39, (28.6, -1.2, 6.5)), (69, (28, 0, 6)), (99, (27.4, 1.2, 5.5))],
        "Head": [(1, (40, 0, 10)), (44, (41, -4, 13)), (72, (40, 0, 8)), (79, (40, 1, 7)), (81, (44, 2, 6)), (87, (42.5, 2.5, 7)),
                 (104, (40, 3, 9))],
    }), periodic=True),
    # Wake (2 s; time running, you came near): the clock starts. A jolt runs through the husk (the cables yank it up,
    # the head whips down after); the head comes up in ticks, the torso straightening with it; the weapons are dragged
    # off the floor and the levers ratchet up and out, the body rising on its cables; it opens, arms wide, the mask
    # thrown back (the hour, remembering itself); then it drops into its poise, the arms swinging down past it and
    # settling, the mask coming down on you last.
    "Wake": clip(59, keys(held(LIMP, 1), {
        "_LayL": weight((1, 1), (14, 1), (19, 0)), "_LayR": weight((1, 1), (12, 1), (17, 0)),
        "_SpotL": spot((1, LIMP_SPOT_L), (14, LIMP_SPOT_L), (19, (-1.0, -3.6, 0.3))),
        "_SpotR": spot((1, LIMP_SPOT_R), (12, LIMP_SPOT_R), (17, (1.9, -3.4, 0.3))),
        "Hang": [(4, ((-1, 0, -4), (0, 0, 0.14))), (8, ((0.8, 0, -3), (0, 0, 0.04))), (13, ((0, 0, -2.5), (0, 0, 0.08))),
                 (30, ((-1.2, 0, -0.5), (0, 0, 0.16))), (41, ((-2, 0, 0), (0, 0, 0.24))), (49, ((1.4, 0, 0), (0, 0, -0.05))),
                 (55, ((-0.4, 0, 0), (0, 0, 0.02))), (60, ((0, 0, 0), (0, 0, 0)))],
        "Torso": [(5, (22, 0, 4)), (9, (27, 0, 5)), (15, (26, 0, 5)), (17, (20, 0, 3)), (22, (19, 0, 3)), (24, (12, 0, 1)),
                  (29, (11, 0, 1)), (34, (0, 0, 0)), (41, (-11, 0, 0)), (45, (-10, 0, 0)), (51, (10, 0, 0)), (56, (5, 0, 0)), (60, (6, 0, 0))],
        "Head": [(6, (50, 0, 12)), (12, (42, 0, 10)), (18, (42, 0, 10)), (20, (30, 0, 6)), (25, (30, 0, 6)), (27, (16, 0, 2)), (31, (16, 0, 2)),
                 (33, (4, 0, 0)), (37, (0, 0, 0)), (44, (-20, 0, 0)), (48, (-18, 0, 0)), (54, (37, 0, 0)), (57, (28.5, 0, 0)), (60, (30, 0, 0))],
        "ArmL": ratchet(14, 34, LIMP_L[0], (-62, 0, -30), 3) + [(41, (-72, 0, -46)), (46, (-70, 0, -44)), (52, (-44, 0, -8)), (56, (-52, 0, -11)), (60, (-50, 0, -10))],
        "ForeL": [(14, LIMP_L[1]), (24, (-30, 0, 0)), (36, (-16, 0, 0)), (43, (-6, 0, 0)), (49, (-14, 0, 0)), (54, (-42, 0, 0)), (58, (-33, 0, 0)), (60, (-35, 0, 0))],
        "ArmR": ratchet(12, 33, LIMP_R[0], (-62, 0, 30), 3) + [(41, (-72, 0, 46)), (46, (-70, 0, 44)), (53, (-20, 0, -16)), (57, (-27, 0, -15)), (60, (-25, 0, -15))],
        "ForeR": [(12, LIMP_R[1]), (23, (-30, 0, 0)), (36, (-16, 0, 0)), (43, (-6, 0, 0)), (49, (-12, 0, 0)), (55, (-21, 0, 0)), (58, (-14, 0, 0)), (60, (-15, 0, 0))],
    })),
}

# The twitch (time stopped): the blade arm jerked up off the floor, weakly, in fits (it falls back between), hauled up
# in failing ticks; then let fall across in front of it, slapping the floor (the blow), dragged back to where it lay.
TW_U, TW_F = LIMP_R
TW_UP = (add(TW_U, (-58, 0, 22)), add(TW_F, (-8, 0, 0)))
TW_HIT_SPOT = (0.5, -4.3, 0.0)
TW_HIT_BODY = {"Hang": ((-1.2, 0, -5), (0, 0, 0)), "Torso": (31, -1.5, 1), "Head": (43, 1, 7)}
TW_HIT = laid_arm("R", TW_HIT_BODY, TW_HIT_SPOT)

ACTIONS.update({
    # TwitchDraw (1.6 s tell): dead still a moment; then a fit (the arm jerks off the floor and falls back short), a
    # second, bigger (the right shoulder hitching, the head lolling after each), then it hauls the blade up in three
    # failing ticks, sagging between, the forearm flopping after the upper arm; held up, shaking.
    "TwitchDraw": clip(47, keys(held(LIMP, 1), {
        "_LayR": weight((1, 1), (6, 1), (8, 0)), "_SpotR": spot((1, LIMP_SPOT_R)),
        "_LayL": weight((1, 1)), "_SpotL": spot((1, LIMP_SPOT_L)),
        "Hang": [(10, ((0.4, 0, -4.4), (0, 0, 0))), (21, ((-0.6, 0, -3.4), (0, 0, 0))), (36, ((0.4, -0.8, -4.6), (0, 0, 0.02))), (48, ((-0.3, -1.2, -5), (0, 0, 0.03)))],
        "Torso": [(6, (28, 0, 6)), (8, (26, -3, 7)), (12, (28, -0.5, 6)), (18, (28, -0.5, 6)), (20, (25, -4.5, 8)), (25, (27, -2, 7)),
                  (29, (26, -3.5, 8)), (34, (27, -2.5, 7)), (36, (25, -4.5, 9)), (40, (26, -3.5, 8)), (42, (24, -5.5, 10)), (48, (24.5, -5, 10))],
        "Head": [(8, (40, 0, 10)), (10, (45, 3, 8)), (15, (41, 0, 10)), (21, (40, 0, 10)), (23, (47, 4, 6)), (28, (43, 2, 9)),
                 (38, (44, 3, 8)), (44, (46, 4, 7)), (48, (45, 4, 7))],
        "ArmR": [(1, TW_U), (6, TW_U), (8, add(TW_U, (-13, 0, 4))), (13, add(TW_U, (-3, 0, 1))), (17, add(TW_U, (-3, 0, 1))),
                 (19, add(TW_U, (-26, 0, 9))), (24, add(TW_U, (-17, 0, 6))), (27, add(TW_U, (-18, 0, 6)))]
                + ratchet(27, 44, add(TW_U, (-18, 0, 6)), add(TW_U, (-60, 0, 22)), 3)
                + [(46, add(TW_U, (-56, 0, 21))), (48, TW_UP[0])],
        "ForeR": [(1, TW_F), (8, TW_F), (10, add(TW_F, (9, 0, 0))), (15, add(TW_F, (-2, 0, 0))), (19, TW_F), (21, add(TW_F, (16, 0, 0))),
                  (26, add(TW_F, (2, 0, 0))), (31, add(TW_F, (10, 0, 0))), (35, add(TW_F, (-4, 0, 0))), (37, add(TW_F, (12, 0, 0))),
                  (41, add(TW_F, (-6, 0, 0))), (43, add(TW_F, (8, 0, 0))), (46, add(TW_F, (-10, 0, 0))), (48, TW_UP[1])],
        "Works": [(1, (0, 0, 0)), (20, (0, 0, -6)), (48, (0, 0, -10))],
    })),
    # TwitchSwing (1 s; the blow at its middle): a last hitch up, then it lets the arm fall across in front of it, the
    # forearm trailing, faster as it falls, the body lurching after it; the blade slaps the floor in front (the blow)
    # and the arm goes slack on it; the head lolls late; the blade is dragged back to where it lay, the body swinging
    # back into its dangle.
    "TwitchSwing": clip(29, {
        "_LayR": weight((1, 0), (13, 0), (15, 1)), "_SpotR": spot((1, TW_HIT_SPOT), (17, TW_HIT_SPOT), (28, LIMP_SPOT_R)),
        "_LayL": weight((1, 1)), "_SpotL": spot((1, LIMP_SPOT_L)),
        "Hang": [(1, ((-0.3, -1.2, -5), (0, 0, 0.03))), (12, ((-1.2, 0, -5.5), (0, 0, 0))), (20, ((0.7, 0.6, -3.4), (0, 0, 0))), (30, ((0, 0, -4), (0, 0, 0)))],
        "Torso": [(1, (24.5, -5, 10)), (4, (23.5, -6, 11)), (11, (30, -1.5, 2)), (16, (31.5, -1, 0)), (22, (28, 0.5, 5)), (30, (28, 0, 6))],
        "Head": [(1, (45, 4, 7)), (6, (42, 5, 9)), (14, (44, 1, 6)), (18, (48, -2, 8)), (24, (39, 0, 11)), (30, (40, 0, 10))],
        "ArmR": [(1, TW_UP[0]), (4, add(TW_UP[0], (-4, 0, 2))), (13, add(TW_HIT[0], (-8, 0, 2))), (15, TW_HIT[0])],
        "ForeR": [(1, TW_UP[1]), (5, add(TW_UP[1], (-6, 0, 0))), (12, add(TW_HIT[1], (-14, 0, 0))), (15, TW_HIT[1])],
        "Works": [(1, (0, 0, -10)), (14, (0, 0, -2)), (30, (0, 0, 0))],
    }),
})

ACTIONS.update({
    # HammerRaise (1.1 s tell; 0.45 s when it's stopped you): a heave first (the hammer dipped forward and down, the
    # body leaning into it), then swung up and back over its head, the upper arm leading and the forearm whipping up
    # after it, the body arching back under the weight and rising on its cables, the head (the mask) kept on you;
    # held at the top, straining, the lever clicking up a last notch.
    "HammerRaise": clip(29, keys(held(POISED, 1), {
        "Hang": [(6, ((-1, 0, 0), (0, 0, -0.04))), (18, ((0.5, 0, 0), (0, 0, 0.04))), (24, ((-2, 0, 0), (0, 0, 0.07))), (30, HAMMER_UP["Hang"])],
        "Torso": [(6, (12, 0, -1)), (17, (-6, 0, 1)), (23, (-14, 0, 2)), (30, HAMMER_UP["Torso"])],
        "Head": [(8, (36, 0, 0)), (19, (24, 0, 0)), (25, (28, 0, 0)), (30, HAMMER_UP["Head"])],
        "ArmL": [(6, (-38, 0, -10)), (13, (-100, 0, -2)), (19, (-146, 0, 6)), (22, (-155, 0, 6)), (26, (-150, 0, 6)), (28, (-154, 0, 6)), (30, HAMMER_UP["ArmL"])],
        "ForeL": [(7, (-24, 0, 0)), (14, (-12, 0, 0)), (20, (-44, 0, 0)), (24, (-36, 0, 0)), (27, (-37, 0, 0)), (30, HAMMER_UP["ForeL"])],
        "ArmR": [(8, (-20, 0, -14)), (20, (-36, 0, -8)), (30, HAMMER_UP["ArmR"])],
        "ForeR": [(10, (-12, 0, 0)), (22, (-26, 0, 0)), (30, HAMMER_UP["ForeR"])],
    })),
    # HammerSlam (0.3 s): brought down, the upper arm leading, the forearm whipping after; the body thrown forward over
    # it; the hammer lands on the floor 4.6 m in front at frame 7 (stopping you, the code lands it at 0.6 of the strike;
    # otherwise at 0.85: either side of that frame) and stays there, the body carried on over it, the head snapping down.
    "HammerSlam": clip(8, {
        "_LayL": weight((1, 0), (6, 0), (7, 1)), "_SpotL": spot((1, SLAM_SPOT)),
        "Hang": [(1, HAMMER_UP["Hang"]), (4, ((-3, 0, 0), (0, 0, 0.02))), (7, SLAM_BODY["Hang"]), (9, SLAM_DEEP["Hang"])],
        "Torso": [(1, HAMMER_UP["Torso"]), (3, (-6, 0, 1)), (5, (8, 0, 0)), (7, SLAM_BODY["Torso"]), (9, SLAM_DEEP["Torso"])],
        "Head": [(1, HAMMER_UP["Head"]), (4, (22, 0, 0)), (7, (30, 0, 0)), (9, (44, 0, -2))],
        "ArmL": [(1, HAMMER_UP["ArmL"]), (3, (-120, 0, 6)), (5, (-58, 0, 3)), (7, SLAM_ARM[0])],
        "ForeL": [(1, HAMMER_UP["ForeL"]), (3, (-48, 0, 0)), (5, (-26, 0, 0)), (7, SLAM_ARM[1])],
        "ArmR": [(1, HAMMER_UP["ArmR"]), (4, (-31, 0, 2)), (8, (-28, 0, 16)), (9, (-29, 0, 14))],
        "ForeR": [(1, HAMMER_UP["ForeR"]), (5, (-14, 0, 0)), (8, (-26, 0, 0)), (9, (-24, 0, 0))],
    }),
    # HammerRecover (1.3 s): the hammer stays planted while the body bounces on its cables over it (the weight still
    # in it), the head hanging; then it hauls the lever up in clicks, the hammer dragged off the floor, and swings it
    # up to the middle, past it and back (settling), the body straightening under it, the mask coming up on you.
    "HammerRecover": clip(35, {
        "_LayL": weight((1, 1), (13, 1), (17, 0)), "_SpotL": spot((1, SLAM_SPOT), (13, SLAM_SPOT), (17, (0.1, -4.3, 0.35))),
        "Hang": [(1, SLAM_DEEP["Hang"]), (5, ((1.2, 0, 0), (0, 0, -0.04))), (10, ((-0.8, 0, 0), (0, 0, -0.12))), (16, ((0.4, 0, 0), (0, 0, -0.06))),
                 (26, ((-0.6, 0, 0), (0, 0, 0.03))), (31, ((0.3, 0, 0), (0, 0, -0.01))), (36, ((0, 0, 0), (0, 0, 0)))],
        "Torso": [(1, SLAM_DEEP["Torso"]), (5, (15, 0, 0)), (10, (18, 0, -1)), (16, (15, 0, 0)), (24, (4, 0, 0)), (30, (7, 0, 0)), (36, (6, 0, 0))],
        "Head": [(1, (44, 0, -2)), (7, (40, 0, 0)), (12, (46, 0, 0)), (19, (42, 0, 0)), (26, (25, 0, 0)), (31, (32, 0, 0)), (36, (30, 0, 0))],
        "ArmL": [(1, SLAM_ARM[0]), (13, SLAM_ARM[0])] + ratchet(13, 24, add(SLAM_ARM[0], (-4, 0, 0)), (-40, 0, -6), 2)
                + [(28, (-55, 0, -11)), (32, (-48, 0, -10)), (36, (-50, 0, -10))],
        "ForeL": [(1, SLAM_ARM[1]), (13, SLAM_ARM[1]), (19, (-14, 0, 0)), (25, (-26, 0, 0)), (30, (-40, 0, 0)), (33, (-33, 0, 0)), (36, (-35, 0, 0))],
        "ArmR": [(1, (-29, 0, 14)), (6, (-26, 0, 17)), (14, (-30, 0, 10)), (26, (-27, 0, -16)), (31, (-24, 0, -14.5)), (36, (-25, 0, -15))],
        "ForeR": [(1, (-24, 0, 0)), (6, (-18, 0, 0)), (14, (-24, 0, 0)), (27, (-17, 0, 0)), (32, (-14, 0, 0)), (36, (-15, 0, 0))],
    }),
    # BladeDraw (0.95 s tell): a wind the other way first (the blade eased across to its left, the body turning with
    # it), then hauled round to its right and back, the torso winding round after the arm, the forearm trailing, the
    # body turning on its cables; the head stays on you (turning against the body); held there, coiled, quivering.
    "BladeDraw": clip(26, keys(held(POISED, 1), {
        "Hang": [(6, ((0, 0, -2), (0, 0, -0.02))), (18, ((0, 0, 5), (0, 0, 0.03))), (23, ((0, 0, 7), (0, 0, 0.045))), (27, BLADE_BACK["Hang"])],
        "Torso": [(6, (7, 0, -8)), (19, (3, 0, 26)), (23, (2, 0, 33)), (27, BLADE_BACK["Torso"])],
        "Head": [(7, (31, 0, 7)), (16, (28, 0, -14)), (22, (26, 0, -27)), (27, BLADE_BACK["Head"])],
        "ArmR": [(5, (-24, 0, -26)), (16, (-16, 0, 82)), (21, (-14, 0, 102)), (24, (-15, 0, 97)), (27, BLADE_BACK["ArmR"])],
        "ForeR": [(7, (-14, 0, -10)), (17, (-8, 0, 2)), (22, (-6, 0, 20)), (25, (-7, 0, 12)), (27, BLADE_BACK["ForeR"])],
        "ArmL": [(8, (-48, 0, -14)), (20, (-78, 0, -64)), (24, (-81, 0, -75)), (27, BLADE_BACK["ArmL"])],
        "ForeL": [(9, (-34, 0, 0)), (21, (-40, 0, 0)), (27, BLADE_BACK["ForeL"])],
    })),
    # BladeSweep (0.5 s; the blow at its middle): the torso unwinds first, the arm after it, the forearm whipping round
    # last; the blade drops low (0.6 m) and comes round from its right, crossing in front of it at frame 8 (the blow),
    # and on round to its left, the body carried round after it on its cables.
    "BladeSweep": clip(14, {
        "Hang": [(1, BLADE_BACK["Hang"]), (6, ((0, 0, 3), (0, 0, 0.0))), (10, ((0, 0, -3), (0, 0, -0.03))), (15, BLADE_ACROSS["Hang"])],
        "Torso": [(1, BLADE_BACK["Torso"]), (4, (1, 0, 23)), (8, (1, 0, -6)), (11, (1.5, 0, -24)), (15, BLADE_ACROSS["Torso"])],
        "Head": [(1, BLADE_BACK["Head"]), (5, (27, 0, -18)), (9, (30, 0, 0)), (13, (31, 0, 13)), (15, BLADE_ACROSS["Head"])],
        "ArmR": [(1, BLADE_BACK["ArmR"]), (3, (-13, 0, 92)), (6, (-5, 0, 44)), (8, (-1.5, 0, -8)), (11, (-2, 0, -48)), (15, BLADE_ACROSS["ArmR"])],
        "ForeR": [(1, BLADE_BACK["ForeR"]), (4, (-5, 0, 20)), (7, (-2, 0, 12)), (9, (0, 0, -2)), (12, (0, 0, -14)), (15, BLADE_ACROSS["ForeR"])],
        "ArmL": [(1, BLADE_BACK["ArmL"]), (7, (-74, 0, -44)), (15, BLADE_ACROSS["ArmL"])],
        "ForeL": [(1, BLADE_BACK["ForeL"]), (8, (-36, 0, 0)), (15, BLADE_ACROSS["ForeL"])],
    }),
    # BladeRecover (1 s): carried on past by the blade's weight (the forearm swinging on, the body turning on its
    # cables), then it unwinds back, lifting the blade round to its side, a swing past and back, settling poised.
    "BladeRecover": clip(29, {
        "Hang": [(1, BLADE_ACROSS["Hang"]), (6, ((0, 0, -10), (0, 0, -0.04))), (16, ((0, 0, -2), (0, 0, 0.01))), (23, ((0, 0, 1.5), (0, 0, 0))), (30, ((0, 0, 0), (0, 0, 0)))],
        "Torso": [(1, BLADE_ACROSS["Torso"]), (5, (9, 0, -34)), (15, (7, 0, -8)), (22, (5.5, 0, 3)), (30, (6, 0, 0))],
        "Head": [(1, BLADE_ACROSS["Head"]), (6, (31, 0, 18)), (14, (32, 0, 4)), (21, (29, 0, -3)), (30, (30, 0, 0))],
        "ArmR": [(1, BLADE_ACROSS["ArmR"]), (5, (-3, 0, -72)), (14, (-36, 0, -46)), (21, (-32, 0, -12)), (25, (-23.5, 0, -16.5)), (30, (-25, 0, -15))],
        "ForeR": [(1, BLADE_ACROSS["ForeR"]), (6, (0, 0, -30)), (15, (-10, 0, -8)), (22, (-18, 0, 3)), (26, (-14, 0, -2)), (30, (-15, 0, 0))],
        "ArmL": [(1, BLADE_ACROSS["ArmL"]), (7, (-58, 0, -6)), (18, (-52, 0, -8)), (24, (-49, 0, -11)), (30, (-50, 0, -10))],
        "ForeL": [(1, BLADE_ACROSS["ForeL"]), (8, (-28, 0, 0)), (19, (-38, 0, 0)), (30, (-35, 0, 0))],
    }),
    # GrindDraw (0.7 s tell): a sag first, then it draws itself up on its cables, the levers folded in and up (the
    # weapons off the floor), the head bowed to look under itself; its works wound back against their spin.
    "GrindDraw": clip(19, keys(held(POISED, 1), {
        "Hang": [(5, ((0.5, 0, 0), (0, 0, -0.08))), (15, ((0, 0, 0), (0, 0, 0.3))), (20, GRIND_UP["Hang"])],
        "Torso": [(5, (10, 0, 0)), (15, (-4, 0, 0)), (20, GRIND_UP["Torso"])],
        "Head": [(7, (36, 0, 0)), (16, (46, 0, 0)), (20, GRIND_UP["Head"])],
        "ArmL": [(6, (-44, 0, -8)), (15, (-82, 0, -22)), (20, GRIND_UP["ArmL"])],
        "ForeL": [(8, (-30, 0, 0)), (17, (-63, 0, 0)), (20, GRIND_UP["ForeL"])],
        "ArmR": [(6, (-22, 0, -12)), (15, (-64, 0, 24)), (20, GRIND_UP["ArmR"])],
        "ForeR": [(8, (-12, 0, 0)), (17, (-53, 0, 0)), (20, GRIND_UP["ForeR"])],
        "Works": [(4, (0, 0, 6)), (17, (0, 0, -48)), (20, GRIND_UP["Works"])],
    })),
    # Grind (0.6 s; the blow at 0.4): it drops on its cables and its works spin round under it (a whole turn and the
    # wind back, fastest as it lands), the levers flung out by the spin, the torso shuddering round; then it rises back
    # into its poise as the works run down.
    "Grind": clip(17, {
        "Hang": [(1, GRIND_UP["Hang"]), (4, ((0, 0, 4), (0, 0, 0.1))), (8, ((0, 0, 8), (0, 0, -0.22))), (11, ((0, 0, 5), (0, 0, -0.1))),
                 (14, ((0, 0, 1), (0, 0, 0.03))), (18, ((0, 0, 0), (0, 0, 0)))],
        "Torso": [(1, GRIND_UP["Torso"]), (5, (2, 0, -6)), (8, (10, 0, 8)), (11, (8, 0, -4)), (14, (6.5, 0, 2)), (18, (6, 0, 0))],
        "Head": [(1, GRIND_UP["Head"]), (6, (40, 0, 6)), (9, (48, 0, -6)), (12, (36, 0, 4)), (15, (29, 0, -1)), (18, (30, 0, 0))],
        "ArmL": [(1, GRIND_UP["ArmL"]), (5, (-72, 0, -40)), (9, (-58, 0, -46)), (13, (-52, 0, -14)), (16, (-51, 0, -8)), (18, (-50, 0, -10))],
        "ForeL": [(1, GRIND_UP["ForeL"]), (6, (-40, 0, 0)), (10, (-20, 0, 0)), (14, (-40, 0, 0)), (18, (-35, 0, 0))],
        "ArmR": [(1, GRIND_UP["ArmR"]), (5, (-52, 0, 42)), (9, (-40, 0, 46)), (13, (-27, 0, -12)), (16, (-24, 0, -17)), (18, (-25, 0, -15))],
        "ForeR": [(1, GRIND_UP["ForeR"]), (6, (-30, 0, 0)), (10, (-8, 0, 0)), (14, (-20, 0, 0)), (18, (-15, 0, 0))],
        "Works": [(1, GRIND_UP["Works"]), (4, (0, 0, 40)), (8, (0, 0, 200)), (12, (0, 0, 320)), (15, (0, 0, 352)), (18, (0, 0, 360))],
    }),
    # StopCast (1.2 s tell, then replayed in 0.2 s as the cast): it gathers (hunched, the levers drawn in and crossed
    # before it, the mask bowed); then flings both arms up and wide, rising on its cables, the forearms whipping after
    # the uppers, the mask thrust down on you last (the code spins its hands); held there, trembling; then dropped back
    # towards its poise in the last moment (the tell's end: dodge now), so the cast (the whole clip again, fast) is a
    # second fling.
    "StopCast": clip(35, keys(held(POISED, 1), {
        "Hang": [(8, STOP_GATHER["Hang"]), (16, ((-3, 0, 0), (0, 0, 0.16))), (20, STOP["Hang"]), (24, ((-2, 0, 0), (0, 0, 0.1))), (28, ((-2.5, 0, 0), (0, 0, 0.12))),
                 (33, ((0.6, 0, 0), (0, 0, -0.02))), (36, ((0, 0, 0), (0, 0, 0)))],
        "Torso": [(8, STOP_GATHER["Torso"]), (15, (-9, 0, 0)), (19, STOP["Torso"]), (28, (-7, 0, 0)), (33, (8, 0, 0)), (36, (6, 0, 0))],
        "Head": [(10, STOP_GATHER["Head"]), (17, (14, 0, 0)), (21, (52, 0, 0)), (24, STOP["Head"]), (30, (47, 0, 0)), (34, (32, 0, 0)), (36, (30, 0, 0))],
        "Head+tremble": [(1, (0, 0, 0)), (22, (0, 0, 0)), (24, (1.5, 0, 1)), (26, (-1, 0, -1)), (28, (1.5, 0, 0.5)), (30, (0, 0, 0))],
        "ArmL": [(9, STOP_GATHER["ArmL"]), (16, (-120, 0, 40)), (19, (-134, 0, 54)), (22, STOP["ArmL"]), (28, (-127, 0, 49)), (33, (-62, 0, -4)), (36, (-50, 0, -10))],
        "ForeL": [(10, STOP_GATHER["ForeL"]), (16, (-40, 0, 0)), (20, (-6, 0, 0)), (23, STOP["ForeL"]), (29, (-14, 0, 0)), (34, (-30, 0, 0)), (36, (-35, 0, 0))],
        "ArmR": [(9, STOP_GATHER["ArmR"]), (16, (-120, 0, -40)), (19, (-134, 0, -54)), (22, STOP["ArmR"]), (28, (-127, 0, -49)), (33, (-36, 0, -13)), (36, (-25, 0, -15))],
        "ForeR": [(10, STOP_GATHER["ForeR"]), (16, (-40, 0, 0)), (20, (-6, 0, 0)), (23, STOP["ForeR"]), (29, (-14, 0, 0)), (34, (-18, 0, 0)), (36, (-15, 0, 0))],
        "ArmL+tremble": [(1, (0, 0, 0)), (23, (0, 0, 0)), (25, (-2, 0, 1)), (27, (1, 0, -1)), (29, (-1.5, 0, 0)), (31, (0, 0, 0))],
        "ArmR+tremble": [(1, (0, 0, 0)), (23, (0, 0, 0)), (25, (-1, 0, -1)), (27, (2, 0, 1)), (29, (-1, 0, 0)), (31, (0, 0, 0))],
    })),
    # Rewind (0.8 s as made; played over 1.4 s): drawn in on itself, the levers folded, the head bowed, and it shudders
    # (quick little jolts through everything, each part a frame after the last) while the whole body turns back on its
    # cables, winding; then it opens back into its poise, mended.
    "Rewind": clip(24, keys(held(POISED, 1), {
        "Hang": [(5, REWIND["Hang"]), (19, ((0.5, 0, -14), (0, 0, 0.08))), (25, ((0, 0, 0), (0, 0, 0)))],
        "Torso": [(5, REWIND["Torso"]), (19, (15, 0, 0)), (25, (6, 0, 0))],
        "Head": [(6, REWIND["Head"]), (19, (24, 0, 0)), (25, (30, 0, 0))],
        "ArmL": [(5, REWIND["ArmL"]), (18, (-32, 0, 22)), (23, (-54, 0, -9)), (25, (-50, 0, -10))],
        "ForeL": [(6, REWIND["ForeL"]), (19, (-60, 0, 0)), (24, (-33, 0, 0)), (25, (-35, 0, 0))],
        "ArmR": [(5, REWIND["ArmR"]), (18, (-32, 0, -22)), (23, (-23, 0, -16)), (25, (-25, 0, -15))],
        "ForeR": [(6, REWIND["ForeR"]), (19, (-60, 0, 0)), (24, (-14, 0, 0)), (25, (-15, 0, 0))],
        "Torso+shudder": [(1, (0, 0, 0)), (5, (0, 0, 0)), (7, (-3, 1.5, 2)), (9, (2, -1, -2)), (11, (-3, -1.5, 1)), (13, (2.5, 1, -2)),
                          (15, (-2, 1, 2)), (17, (2, -1, -1)), (19, (0, 0, 0)), (25, (0, 0, 0))],
        "Head+shudder": [(1, (0, 0, 0)), (6, (0, 0, 0)), (8, (-5, 2, 4)), (10, (4, -2, -4)), (12, (-5, -2, 3)), (14, (4, 2, -3)),
                         (16, (-3, 1, 3)), (18, (3, -1, -2)), (20, (0, 0, 0)), (25, (0, 0, 0))],
        "ArmL+shudder": [(1, (0, 0, 0)), (6, (0, 0, 0)), (8, (-4, 0, 3)), (10, (3, 0, -3)), (12, (-4, 0, -2)), (14, (3, 0, 3)),
                         (16, (-2, 0, -2)), (18, (2, 0, 1)), (20, (0, 0, 0)), (25, (0, 0, 0))],
        "ArmR+shudder": [(1, (0, 0, 0)), (6, (0, 0, 0)), (8, (3, 0, -3)), (10, (-4, 0, 3)), (12, (3, 0, 2)), (14, (-4, 0, -3)),
                         (16, (2, 0, 2)), (18, (-2, 0, -1)), (20, (0, 0, 0)), (25, (0, 0, 0))],
    })),
    # Chime (2 s as made; 2.2 s in play): the quarter strikes. It gathers itself (hunched, levers drawn in, the mask
    # bowed), then is thrown open like a struck bell: jolted up on its cables, arms flung wide, the head thrown back;
    # and it rings: the body shivering back and forth on the cables, dying away, struck twice more (smaller), the
    # forearms and the head ringing a beat after the body; then it settles back into its poise.
    "Chime": clip(59, keys(held(POISED, 1), {
        "Hang": [(9, CHIME_GATHER["Hang"]), (13, ((-3, 0, 0), (0, 0, 0.3))), (17, ((1.5, 0, 0), (0, 0, 0.16))), (21, ((-2.5, 0, 0), (0, 0, 0.24))),
                 (25, ((0, 0, 0), (0, 0, 0.18))), (29, ((-2.5, 0, 0), (0, 0, 0.26))), (33, ((-0.5, 0, 0), (0, 0, 0.19))), (37, ((-1.8, 0, 0), (0, 0, 0.23))),
                 (41, ((-2.2, 0, 0), (0, 0, 0.25))), (45, ((-1.2, 0, 0), (0, 0, 0.2))), (51, ((1, 0, 0), (0, 0, -0.03))), (56, ((-0.3, 0, 0), (0, 0, 0.01))), (60, ((0, 0, 0), (0, 0, 0)))],
        "Torso": [(9, CHIME_GATHER["Torso"]), (14, (-18, 0, 0)), (18, (-10, 0, 0)), (22, (-17, 0, 0)), (26, (-12, 0, 0)), (30, (-18, 0, 0)), (34, (-13, 0, 0)),
                  (38, (-16, 0, 0)), (42, (-17, 0, 0)), (46, (-13, 0, 0)), (52, (9, 0, 0)), (56, (5, 0, 0)), (60, (6, 0, 0))],
        "Head": [(10, CHIME_GATHER["Head"]), (16, (-30, 0, 0)), (20, (-18, 0, 0)), (24, (-27, 0, 0)), (28, (-20, 0, 0)), (32, (-28, 0, 0)), (36, (-22, 0, 0)),
                 (40, (-25, 0, 0)), (45, (-24, 0, 0)), (51, (14, 0, 0)), (55, (35, 0, 0)), (60, (30, 0, 0))],
        "ArmL": [(9, CHIME_GATHER["ArmL"]), (14, (-74, 0, -52)), (18, (-66, 0, -44)), (22, (-72, 0, -50)), (26, (-68, 0, -46)), (30, (-73, 0, -51)),
                 (34, (-69, 0, -47)), (38, (-71, 0, -49)), (45, CHIME["ArmL"]), (52, (-46, 0, -6)), (56, (-52, 0, -11)), (60, (-50, 0, -10))],
        "ForeL": [(10, CHIME_GATHER["ForeL"]), (15, (-2, 0, 0)), (19, (-14, 0, 0)), (23, (-4, 0, 0)), (27, (-12, 0, 0)), (31, (-4, 0, 0)), (35, (-11, 0, 0)),
                  (40, (-7, 0, 0)), (46, (-8, 0, 0)), (53, (-40, 0, 0)), (57, (-33, 0, 0)), (60, (-35, 0, 0))],
        "ArmR": [(9, CHIME_GATHER["ArmR"]), (14, (-74, 0, 52)), (18, (-66, 0, 44)), (22, (-72, 0, 50)), (26, (-68, 0, 46)), (30, (-73, 0, 51)),
                 (34, (-69, 0, 47)), (38, (-71, 0, 49)), (45, CHIME["ArmR"]), (52, (-21, 0, -18)), (56, (-27, 0, -14)), (60, (-25, 0, -15))],
        "ForeR": [(10, CHIME_GATHER["ForeR"]), (15, (-2, 0, 0)), (19, (-14, 0, 0)), (23, (-4, 0, 0)), (27, (-12, 0, 0)), (31, (-4, 0, 0)), (35, (-11, 0, 0)),
                  (40, (-7, 0, 0)), (46, (-8, 0, 0)), (53, (-19, 0, 0)), (57, (-14, 0, 0)), (60, (-15, 0, 0))],
    })),
    # Stagger (1 s as made; 1.6 s in play): knocked back on its cables (the body swung back, the torso jolting back
    # after it, the head snapping back last), the levers flung loose, the forearms flopping; it swings forward past its
    # poise and back, the swings dying away, and gathers itself.
    "Stagger": clip(29, keys(held(POISED, 1), {
        "Hang": [(4, ((5, 0, 1.5), (0, 0, 0.05))), (11, ((-3.5, 0, -1), (0, 0, -0.04))), (17, ((2, 0, 0.5), (0, 0, 0.02))), (23, ((-0.8, 0, 0), (0, 0, 0))), (30, ((0, 0, 0), (0, 0, 0)))],
        "Torso": [(5, (-14, 2, 3)), (12, (14, -1, -1)), (18, (2, 0.5, 0.5)), (24, (7, 0, 0)), (30, (6, 0, 0))],
        "Head": [(3, (34, 0, 0)), (7, (-8, 4, 6)), (14, (44, -2, -2)), (20, (24, 1, 1)), (26, (32, 0, 0)), (30, (30, 0, 0))],
        "ArmL": [(5, (-66, 0, -30)), (12, (-36, 0, -4)), (19, (-54, 0, -12)), (25, (-49, 0, -10)), (30, (-50, 0, -10))],
        "ForeL": [(6, (-14, 0, 0)), (13, (-50, 0, 0)), (20, (-30, 0, 0)), (26, (-36, 0, 0)), (30, (-35, 0, 0))],
        "ArmR": [(5, (-44, 0, -2)), (12, (-14, 0, -24)), (19, (-28, 0, -13)), (25, (-24, 0, -15.5)), (30, (-25, 0, -15))],
        "ForeR": [(6, (-2, 0, 0)), (13, (-30, 0, 0)), (20, (-10, 0, 0)), (26, (-16, 0, 0)), (30, (-15, 0, 0))],
    })),
    # Death (2.2 s): the hour strikes at last: a last jolt (thrown open, the mask up), then the cables give: it drops,
    # caught short with a bounce, slumping over, the head falling on its chest after the body; the levers fall, the
    # weapons crashing to the floor where they lie (the arms slack on them); it sags, turning slowly on its cables, and
    # hangs still.
    "Death": clip(59, keys(held(POISED, 1), {
        "_LayL": weight((1, 0), (19, 0), (24, 1)), "_SpotL": spot((1, add(DEAD_SPOT_L, (0, -0.6, 2.0))), (19, add(DEAD_SPOT_L, (0, -0.6, 2.0))), (24, DEAD_SPOT_L),
                                                                 (27, add(DEAD_SPOT_L, (0, 0, 0.24))), (31, DEAD_SPOT_L)),
        "_LayR": weight((1, 0), (17, 0), (22, 1)), "_SpotR": spot((1, add(DEAD_SPOT_R, (0.6, -0.4, 2.2))), (17, add(DEAD_SPOT_R, (0.6, -0.4, 2.2))), (22, DEAD_SPOT_R),
                                                                 (25, add(DEAD_SPOT_R, (0, 0, 0.2))), (29, DEAD_SPOT_R)),
        "Hang": [(5, ((-2.5, 0, 0), (0, 0, 0.18))), (11, ((-1.5, 0, 1), (0, 0, 0.12))), (20, ((2, 0, 4), (0, 0, -0.5))), (25, ((1, 0, 6), (0, 0, -0.33))),
                 (31, ((2.5, 0, 7.5), (0, 0, -0.46))), (38, ((1.8, 0, 9), (0, 0, -0.4))), (46, ((2.2, 0, 10), (0, 0, -0.43))), (60, DEAD_BODY["Hang"])],
        "Torso": [(5, (-14, 0, 0)), (11, (-10, 0, 0)), (19, (30, 0, 4)), (24, (48, 0, 8)), (30, (40, 0, 9)), (37, (45, 0, 10)), (60, DEAD_BODY["Torso"])],
        "Head": [(6, (-22, 0, 0)), (12, (-16, 0, 0)), (22, (24, 0, 6)), (27, (60, 0, 14)), (33, (48, 0, 12)), (40, (54, 0, 15)), (60, DEAD_BODY["Head"])],
        "ArmL": [(5, (-78, 0, -40)), (12, (-74, 0, -38)), (24, DEAD_L[0])],
        "ForeL": [(6, (-10, 0, 0)), (13, (-12, 0, 0)), (20, add(DEAD_L[1], (-14, 0, 0))), (24, DEAD_L[1])],
        "ArmR": [(5, (-78, 0, 40)), (12, (-74, 0, 38)), (22, DEAD_R[0])],
        "ForeR": [(6, (-10, 0, 0)), (13, (-12, 0, 0)), (18, add(DEAD_R[1], (-14, 0, 0))), (22, DEAD_R[1])],
        "Works": [(1, (0, 0, 0)), (20, (0, 0, 0)), (60, (0, 0, -12))],
    })),
})


ACTIONS.update({
    # LimpDeath (2.2 s; killed with time stopped: the game plays it instead of Death): no hour strikes, the husk just
    # gives out. From Limp's first pose, a moment's stillness; then the cables let go and it drops (falling faster as it
    # goes), caught short with a small sag and bounce, slumping lower over itself; the head sinks the last notch after
    # the body; its weapons stay where they lie (slumping a little further out), the slack arms bent onto them; it hangs
    # turning slowly on its cables, and is still.
    "LimpDeath": clip(59, keys(held(LIMP, 1), {
        "_LayL": weight((1, 1)), "_LayR": weight((1, 1)),
        "_SpotL": spot((1, LIMP_SPOT_L), (16, LIMP_SPOT_L), (24, add(LIMP_SPOT_L, (-0.08, -0.12, -0.02)))),
        "_SpotR": spot((1, LIMP_SPOT_R), (18, LIMP_SPOT_R), (27, add(LIMP_SPOT_R, (0.1, -0.1, -0.03)))),
        "Hang": [(6, ((0, 0, -4), (0, 0, 0))), (10, ((0.3, 0, -3.8), (0, 0, -0.06))), (16, ((1.2, 0.4, -3.2), (0, 0, -0.42))),
                 (21, ((0.4, 0.8, -2), (0, 0, -0.34))), (27, ((1.4, 0.4, 0), (0, 0, -0.43))), (35, ((0.8, -0.4, 3), (0, 0, -0.4))),
                 (47, ((1.2, -0.6, 6), (0, 0, -0.42))), (60, ((1, -0.3, 8), (0, 0, -0.42)))],
        "Torso": [(8, (28, 0, 6)), (17, (36, 0, 7)), (22, (42, 0, 8)), (28, (39, 0, 8)), (38, (44, 0, 9)), (60, (43, 0, 9))],
        "Head": [(10, (40, 0, 10)), (14, (38, 0, 10)), (21, (46, -1, 11)), (26, (58, -2, 13)), (32, (53, -1, 12)), (42, (57, -2, 14)), (60, (56, -2, 14))],
    })),
})

def gear(parts, material, centre, radius, teeth, thick, rot, group):
    """A brass gear (a disc and its teeth), turned by `rot` (degrees) from lying flat."""
    at = M(centre, rot=rot)
    parts.append(part("Gear", kit.cylinder(20, radius, thick), material, at, group=group))
    for k in range(teeth):
        a = 2 * math.pi * k / teeth
        parts.append(part("Cog", kit.cube(), material, at @ M((math.cos(a) * (radius + 0.06), math.sin(a) * (radius + 0.06), 0),
                                                             rot=(0, 0, math.degrees(a)), scale=(0.13, 0.1, thick)), group=group))


def cable(points, material):
    """A cable, its foot on the shoulders (the Torso), the rest of it on the Cables (bowing after them), blended."""
    ob = part("Cable", kit.tube(points, [0.05, 0.045, 0.04], seg=5), material)
    torso, cables = ob.vertex_groups.new(name="Torso"), ob.vertex_groups.new(name="Cables")
    low, high = CABLE_FOOT[2], 8.6
    for v in ob.data.vertices:
        t = min(1.0, max(0.0, (high - v.co.z) / (high - low)))
        t = t * t * (3 - 2 * t)
        torso.add([v.index], t, "REPLACE")
        cables.add([v.index], 1.0 - t, "REPLACE")
    return ob


def build():
    robe, robe_dark = kit.mat("HourRobe", "#262b36"), kit.mat("HourRobeShadow", "#1a1d25")
    trim, brass = kit.mat("HourGilt_Metal", "#d9b45a"), kit.mat("HourBrass_Metal", "#b8913f")
    iron, steel = kit.mat("HourIron_Metal", "#3a3a42"), kit.mat("HourSilver_Metal", "#c9d3e2")
    enamel, mark = kit.mat("HourEnamel", "#ece4cc"), kit.mat("HourMark", "#1c1a18")
    eye = kit.mat("SouredEye_Glow", "#c8f07a")
    lantern_glass = kit.mat("SouredLantern_Glow", "#b6f06a")

    parts = []
    # The cables up into its contraption, from its back and shoulders (their tops fixed up there, their feet on its
    # shoulders, their middles bowing after them).
    for x, y in ((-0.5, 0.25), (0.5, 0.25), (-0.2, 0.45), (0.2, 0.45)):
        parts.append(cable([(x, y, SHOULDER - 0.3), (x * 0.6, y * 0.7, 8.0), (x * 0.3, y * 0.3, TOP + 1.2)], iron))
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
    centre = BLADE_CENTRE
    steps = [i / 14 for i in range(15)]

    def blade(bm):
        import bmesh
        rows = []
        for t in steps:
            (ox, oy), (ix, iy) = blade_outer(t), blade_inner(t)
            rows.append([bm.verts.new(p) for p in ((ox, oy, centre[2] - 0.04), (ox, oy, centre[2] + 0.04),
                                                   (ix, iy, centre[2] + 0.04), (ix, iy, centre[2] - 0.04))])
        for a, b in zip(rows, rows[1:]):
            for q in range(4):
                bm.faces.new((a[q], b[q], b[(q + 1) % 4], a[(q + 1) % 4]))
        bm.faces.new(rows[0])
        bm.faces.new(list(reversed(rows[-1])))
        bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    outer_pts = [blade_outer(t) for t in steps]
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
    # Its green lantern, hung from the point on its chain (swinging after the works): a Soured, after all.
    parts += [
        part("LanternChain", kit.cylinder(4, 0.015, 0.25), iron, M((0, 0, 0.75)), group="Lantern"),
        part("LanternTop", kit.cone(4, 0.09, 0.02, 0.07), iron, M((0, 0, 0.62), rot=(0, 0, 45)), group="Lantern"),
        part("LanternGlass", kit.cube(), lantern_glass, M((0, 0, 0.52), scale=(0.12, 0.12, 0.14)), group="Lantern"),
        part("LanternBase", kit.cube(), iron, M((0, 0, 0.44), scale=(0.14, 0.14, 0.02)), group="Lantern"),
    ]
    mesh = kit.join(parts, "TheHour")
    armature = kit.rig("TheHourRig", BONES)
    kit.skin(mesh, armature)
    kit.animate(armature, ACTIONS)
    for name, (low, off, _) in zip(ACTIONS, REPORT.values()):
        notes = []
        if low["hammer"] < -0.02 or low["blade"] < -0.02 or low["lantern"] < 0.0:
            notes.append(f"lowest: hammer {low['hammer']:+.2f} m, blade {low['blade']:+.2f} m, lantern {low['lantern']:+.2f} m")
        if off > 0.05:
            notes.append(f"a laid weapon comes off its spot by up to {off * 100:.0f} cm")
        for n in notes:
            print(f"[the_hour] {name}: {n}")
    return [armature, mesh]
