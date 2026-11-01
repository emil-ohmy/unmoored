"""The Laden archer: a Laden variant that let most of its heap go and kept a bow. A small bundle of belongings and
a quiver ride on its back; it holds a bow across its body, drawing it level at whatever it's watching. Faded ochre
where the Laden is violet, so the two read apart at a glance. Same sickly green eyes and belt lantern.

Clips: Idle and Walk (bow held low), Draw (0.9 s telegraph, 1.3 s for a volley: the bow comes up level and is drawn,
leaning back), Release (0.25 s: the snap of the shot; replayed for each arrow of a volley), ShotRecover (0.6 s; 1 s
after a volley), BashWindup / Bash / BashRecover (the jab it falls back on up close: the bow swung across like a club),
Death, Stagger.

How it's animated (the Laden's way, its kin): each bone keyed on its own timing (kit.bake: tracks), so nothing moves
all at once. The body leads (hips, then spine, then chest); the head follows a few frames late; the pack on its back
(bundle, kettle, quiver) and the lantern at its belt lag and overshoot; the skirt of its robe swings after the body and
stays on the floor. The bow is keyed where it is (by its grip, in the world); the arms aren't keyed: every frame they're
bent (kit.reach) so the left hand keeps the bow and the right hand keeps the bow or the string. The string is drawn
(a "Nock" track: how far back its middle is), the limbs bend to keep it its length, and the two halves of the string
follow the nock: a V you can read from above.
"""
import math

from mathutils import Euler, Vector

M, part = kit.M, kit.part  # `kit` is injected by build_model.py

# The arms (in two, an elbow knob between), built holding the bow level before the chest (the aim).
SHOULDER_L, ELBOW_L, HAND_L = (-0.3, -0.1, 1.06), (-0.33, -0.37, 0.93), (-0.12, -0.58, 1.06)
SHOULDER_R, ELBOW_R, HAND_R = (0.3, -0.1, 1.06), (0.33, -0.37, 0.93), (0.12, -0.58, 1.06)
# The bow, level across the chest: its grip (the Bow bone's head), the limbs' pivots by the hands, the tips. (The right
# limb is a centimetre longer, as bows are uneven: its tip is the bow's far end, which the bow-jab's swoosh follows.)
GRIP = (0.0, -0.63, 1.03)
PIVOT_L, PIVOT_R = (-0.1, -0.611, 1.03), (0.1, -0.611, 1.03)
TIP_L, TIP_R = (-0.52, -0.44, 1.03), (0.535, -0.44, 1.03)
STRING_Y = -0.44   # the string's middle, at rest
DRAW = 0.2         # how far back the string's middle comes at full draw (Nock 1)

BONES = {
    "Root": ((0, 0, 0), (0, 0, 0.3), None),
    "Hips": ((0, 0, 0.3), (0, 0, 0.6), "Root"),
    "Hem": ((0, 0, 0.6), (0, 0.05, 0.05), "Hips"),             # the robe's skirt, hanging from the waist
    "Spine": ((0, 0, 0.6), (0, -0.06, 0.92), "Hips"),
    "Chest": ((0, -0.06, 0.92), (0, -0.12, 1.1), "Spine"),
    "Head": ((0, -0.12, 1.1), (0, -0.26, 1.4), "Chest"),
    "ArmL": (SHOULDER_L, ELBOW_L, "Chest"),
    "ForeL": (ELBOW_L, HAND_L, "ArmL"),
    "ArmR": (SHOULDER_R, ELBOW_R, "Chest"),
    "ForeR": (ELBOW_R, HAND_R, "ArmR"),
    "Pack": ((0, 0.24, 1.25), (0, 0.32, 0.95), "Chest"),        # bundle, kettle and quiver, hung from the shoulders
    "Lantern": ((0.47, 0, 0.85), (0.48, 0, 0.5), "Spine"),     # its lantern, on a cord at its belt
    "Bow": (GRIP, (0, -0.9, 1.03), "Root"),                     # keyed where it is (the hands follow it)
    "LimbL": (PIVOT_L, TIP_L, "Bow"),
    "LimbR": (PIVOT_R, TIP_R, "Bow"),
    "StringL": (TIP_L, (0, STRING_Y, 1.03), "LimbL"),
    "StringR": (TIP_R, (0, STRING_Y, 1.03), "LimbR"),
}
# (Not bones: tracks the hands and string are worked out from, every frame.)
#   Nock  (d, 0, 0): the string's middle drawn back d (1: full draw; below 0: snapped forward past rest).
#   Pull  (w, p, 0): the right hand from the bow (w 0) to the string (w 1), at the string drawn back p.
#   Flick ((0, 0, 0), offset): the right hand thrown off its place (the loose's follow-through), in metres.
#   LetGo (h, 0, 0): the right hand let go of the bow (1: the arm hangs from the shoulder, swung by the Flick).
EXTRA = ("Nock", "Pull", "Flick", "LetGo")
HANG_R = (0.36, -0.16, 0.5)  # (where the right hand hangs when it lets go, on the chest)


def bow(rot, x, y, z):
    """The bow turned `rot` (degrees, world axes, about its grip), its grip at (x, y, z)."""
    return (tuple(rot), (round(x - GRIP[0], 4), round(y - GRIP[1], 4), round(z - GRIP[2], 4)))


def flex(d, side):
    """A limb's bend (degrees about Z, in the bow's own plane) that keeps the string its length with its middle drawn
    back d, and the string half's turn from there to the nock."""
    pivot, tip = (Vector(PIVOT_L), Vector(TIP_L)) if side < 0 else (Vector(PIVOT_R), Vector(TIP_R))
    half = abs(tip.x)
    nock = Vector((0, STRING_Y + DRAW * d, tip.z))

    def tip_at(a):
        r = Euler((0, 0, math.radians(a))).to_matrix()
        return r @ (tip - pivot) + pivot
    lo, hi = -30.0, 40.0   # (bent towards the string's pull: a limb tip comes back and in)
    for _ in range(40):
        mid = (lo + hi) / 2
        if (tip_at(side * mid) - nock).length > half:
            lo = mid
        else:
            hi = mid
    bend = side * (lo + hi) / 2
    t = tip_at(bend)
    to = nock - t
    # the string half's rest direction is along X (towards the middle); its turn on top of the limb's
    rest = math.degrees(math.atan2(0, -side))
    aim = math.degrees(math.atan2(to.y, to.x))
    turn = (aim - rest + 180) % 360 - 180 - bend
    return bend, turn


def grip(pose):
    """The bow strung as drawn; the left hand on the bow, the right on the bow or the string; the elbows bowed out and
    down (the drawing elbow out and up); the skirt left on the floor when the body crouches or rises."""
    pose = dict(pose)
    d = pose.get("Nock", ((0, 0, 0), (0, 0, 0)))[0][0]
    w, p = pose.get("Pull", ((0, 0, 0), (0, 0, 0)))[0][:2]
    flick = Vector(pose.get("Flick", ((0, 0, 0), (0, 0, 0)))[1])
    let_go = pose.get("LetGo", ((0, 0, 0), (0, 0, 0)))[0][0]
    for side, limb, string in ((-1, "LimbL", "StringL"), (1, "LimbR", "StringR")):
        bend, turn = flex(d, side)
        pose[limb] = (0, 0, bend)
        pose[string] = (0, 0, turn)
    pose = kit.reach(BONES, pose, "ArmL", "ForeL", lambda m: m["Bow"] @ Vector(HAND_L), (-1, 0.3, -0.8))
    on_string = Vector((0.03, STRING_Y + DRAW * p + 0.035, 1.05))
    held_at = Vector(HAND_R).lerp(on_string, w)
    pole = Vector((1, 0.3, -0.8)).lerp(Vector((1, 0.6, 0.5)), w)
    holding = kit.reach(BONES, pose, "ArmR", "ForeR", lambda m: m["Bow"] @ held_at + (flick if let_go <= 0 else Vector()), tuple(pole))
    if let_go > 0:
        # (let go: the arm hangs from the shoulder, swinging by the Flick)
        hanging = kit.reach(BONES, pose, "ArmR", "ForeR", lambda m: m["Chest"] @ Vector(HANG_R) + flick, (1, 0.5, 0.2))
        for bone in ("ArmR", "ForeR"):
            a, b = (Euler([math.radians(v) for v in holding[bone]]).to_quaternion(),
                    Euler([math.radians(v) for v in hanging[bone]]).to_quaternion())
            holding[bone] = tuple(math.degrees(v) for v in a.slerp(b, min(1.0, let_go)).to_euler("XYZ"))
    pose = holding
    hips = pose.get("Hips", ((0, 0, 0), (0, 0, 0)))
    hem = pose.get("Hem", ((0, 0, 0), (0, 0, 0)))
    hem_rot = hem[0] if isinstance(hem[0], (tuple, list)) else hem
    pose["Hem"] = (tuple(hem_rot), (0, 0, -hips[1][2]))
    m = kit.pose_matrices(BONES, pose)["Hem"]
    low = min((m @ p).z for p in HEM_RIM)
    if low < 0.002:  # (swung or tipped, it would dip into the floor: it rides up instead)
        pose["Hem"] = (tuple(hem_rot), (0, 0, -hips[1][2] + 0.002 - low))
    return pose


# The skirt's hem at rest (its lowest ring), to keep it off the floor.
HEM_RIM = [M((0, 0.031, 0.374), rot=(5, 0, 0)) @ Vector((0.5 * math.cos(a), 0.5 * math.sin(a), -0.33))
           for a in (2 * math.pi * k / 9 for k in range(9))]


REACH = {}  # (the build's report: how far each clip's hands come off what they hold, at worst)


def clip(length, tracks, periodic=False):
    """A clip `length` frames long (30 a second) from per-bone tracks, baked to every frame, the hands kept on the bow."""
    frames = []
    worst = 0.0
    for f, raw in kit.bake(tracks, length, periodic):
        p = grip(raw)
        m = kit.pose_matrices(BONES, p)
        worst = max(worst, (m["ForeL"] @ Vector(HAND_L) - m["Bow"] @ Vector(HAND_L)).length)
        w, pull = raw.get("Pull", ((0, 0, 0), (0, 0, 0)))[0][:2]
        if raw.get("LetGo", ((0, 0, 0), (0, 0, 0)))[0][0] <= 0:
            want = m["Bow"] @ Vector(HAND_R).lerp(Vector((0.03, STRING_Y + DRAW * pull + 0.035, 1.05)), w)
            want += Vector(raw.get("Flick", ((0, 0, 0), (0, 0, 0)))[1])
            worst = max(worst, (m["ForeR"] @ Vector(HAND_R) - want).length)
        frames.append((f, {k: v for k, v in p.items() if k not in EXTRA}))
    REACH[len(REACH)] = worst
    return ("LINEAR", frames)


def held(pose, frame):
    """A whole pose as a key at `frame` for every track (to start or end a clip on it)."""
    return {bone: [(frame, value)] for bone, value in pose.items()}


def keys(*parts):
    """Tracks merged: each a {bone: [(frame, value)]}."""
    merged = {}
    for p in parts:
        for bone, ks in p.items():
            merged.setdefault(bone, []).extend(ks)
    return merged


def with_(pose, **changes):
    merged = dict(pose)
    merged.update(changes)
    return merged


ZERO = ((0, 0, 0), (0, 0, 0))
LOW_ROT, LOW_GRIP = (32, 14, -12), (0.04, -0.56, 0.7)


def low(rot=(0, 0, 0), move=(0, 0, 0)):
    """The bow as carried low, turned `rot` more and moved `move` (it jostles in its hands)."""
    return bow([a + b for a, b in zip(LOW_ROT, rot)], *[a + b for a, b in zip(LOW_GRIP, move)])


# The poses everything starts from or comes back to.
# LOW   (carrying, watching): hunched like a Laden, the bow held low across its belly, tipped, both hands on it; the
#       head cocked up to watch over it.
# AIM   the bow level before its chest, its right hand on the string (undrawn); turned a little side-on.
# DRAWN full draw: leaning back, side-on (right shoulder back), the head straight down the arrow, the string's middle
#       at its chest, the drawing elbow out and up.
LOW = {"Hips": ZERO, "Spine": (7, 0, -3), "Chest": (5, 0, -3), "Head": (-12, 0, 5),
       "Bow": low(), "Pack": (0, 0, 0), "Hem": (0, 0, 0), "Lantern": (0, 0, 0),
       "Nock": (0, 0, 0), "Pull": (0, 0, 0), "Flick": ZERO, "LetGo": (0, 0, 0)}
AIM = {"Hips": ((0, 0, 5), (0, 0.02, -0.02)), "Spine": (-2, 0, 5), "Chest": (-3, 0, 4), "Head": (6, 0, -13),
       "Bow": bow((0, 0, 0), 0, -0.68, 1.03), "Pack": (1, 0, 0), "Hem": (0, 0, 0), "Lantern": (0, 0, 0),
       "Nock": (0, 0, 0), "Pull": (1, 0, 0), "Flick": ZERO, "LetGo": (0, 0, 0)}
DRAWN = {"Hips": ((0, 0, 8), (0, 0.035, -0.03)), "Spine": (-6, 0, 6), "Chest": (-7, 0, 5), "Head": (12, 0, -19),
         "Bow": bow((0, 0, 0), 0, -0.68, 1.03), "Pack": (3, 0, 0), "Hem": (0, 0, 0), "Lantern": (0, 0, 0),
         "Nock": (1, 0, 0), "Pull": (1, 1, 0), "Flick": ZERO, "LetGo": (0, 0, 0)}
# The bow-jab: wound round to its right, the bow held like a club (its right limb outwards), then swung across in front.
WOUND = {"Hips": ((0, 0, 16), (0.02, 0.05, -0.05)), "Spine": (2, 0, 20), "Chest": (-2, 0, 14), "Head": (4, 0, -42),
         "Bow": bow((0, -12, 30), 0.44, 0.02, 1.12)}
THROUGH = {"Hips": ((0, 0, -14), (-0.03, -0.09, -0.07)), "Spine": (14, 0, -16), "Chest": (10, 0, -10), "Head": (4, 0, 30),
           "Bow": bow((0, 18, -145), -0.28, -0.46, 0.84)}

ACTIONS = {
    # Idle (47 frames, a loop): breathing, watching. A breath in (the chest rises and straightens, the bow riding it a
    # moment later, the pack after) and out (it sinks into its hunch); it looks round, to its right and then to its
    # left, the head trailing the breath; and its claws shift on the bow (a tip and a settle), the quiver rattling after.
    "Idle": clip(47, {
        "Hips": [(1, ZERO), (13, ((0, 0, 0), (0, 0, 0.022))), (27, ((0, 0, 0), (0, 0, -0.014))), (38, ((0, 1, 0), (0, 0, 0.006)))],
        "Spine": [(1, (7, 0, -3)), (13, (2, 0, -3)), (28, (10, 0, -2)), (39, (6, 1, -3))],
        "Chest": [(2, (5, 0, -3)), (15, (-1, 0, -4)), (29, (9, 0, -2)), (40, (4, 1, -3))],
        "Head": [(3, (-12, 0, 5)), (10, (-13, 0, 34)), (19, (-11, 0, 28)), (29, (-10, 0, -26)), (37, (-14, 0, -20)), (44, (-11, 0, 2))],
        "Bow": [(3, low()), (16, low((-4, 0, 0), (0, 0, 0.03))), (24, low((3, 5, -3), (0, 0.01, -0.01))),
                (28, low((5, -5, 3), (0.005, 0, -0.025))), (35, low((1, 0, 0), (0, 0, -0.01)))],
        "Pack": [(1, (0, 0, 0)), (18, (4, 1, 0)), (28, (-1, -2, 0)), (32, (5, 2, 1)), (41, (1, -1, 0))],
        "Hem": [(1, (0, 0, 0)), (24, (1, 0, 1)), (42, (-1, 0, -1))],
        "Lantern": [(1, (0, 0, 0)), (20, (0, 4, 2)), (31, (5, -2, 0)), (40, (-2, 1, 0))],
    }, periodic=True),
    # Walk (32 frames, two steps): the Laden's heavy waddle, lighter. It drops onto each step (frames 24 and 8), rolls
    # over onto that side and rises between (upright at frame 1: the loop starts and ends on its passing pose, so it
    # joins what's before and after); the skirt kicks out, the chest rolls after the hips, the head bobs after the
    # chest, the bow jostles in its hands after that; the pack bounces late and the lantern swings.
    "Walk": clip(32, {
        "Hips": [(24, ((0, -7, 5), (0.03, 0, -0.05))), (28, ((0, -6, 4), (0.025, 0, -0.06))), (1, ((0, 0, 0), (0, 0, 0.02))),
                 (8, ((0, 7, -5), (-0.03, 0, -0.05))), (12, ((0, 6, -4), (-0.025, 0, -0.06))), (17, ((0, 0, 0), (0, 0, 0.02)))],
        "Hem": [(25, (-9, 0, 5)), (1, (5, 4, 0)), (9, (-9, 0, -5)), (17, (5, -4, 0))],
        "Spine": [(24, (10, 5, -3)), (32, (7, 0, -3)), (8, (10, -5, -3)), (16, (7, 0, -3))],
        "Chest": [(26, (8, 5, -4)), (2, (4, 0, -3)), (10, (8, -5, -2)), (18, (4, 0, -3))],
        "Head": [(27, (-6, -4, 7)), (3, (-14, 0, 5)), (11, (-6, 4, 3)), (19, (-14, 0, 5))],
        "Bow": [(27, low((6, -5, 0), (0.02, 0, -0.045))), (3, low((-3, 0, 0), (0, 0, 0.02))),
                (11, low((6, 5, 4), (-0.02, 0, -0.045))), (19, low((-3, 0, 0), (0, 0, 0.02)))],
        "Pack": [(28, (10, -5, 0)), (4, (1, 0, 0)), (12, (10, 5, 0)), (20, (1, 0, 0))],
        "Lantern": [(29, (-12, 0, 0)), (5, (9, 0, 0)), (13, (-12, 0, 0)), (21, (9, 0, 0))],
    }, periodic=True),
    # Draw (0.9 s telegraph; 1.3 s for a volley): it sinks to gather itself (the bow dipping), then swings the bow up
    # level before its chest (overshooting high, settling), the head rising after it to sight along it; the right hand
    # leaves the bow for the string and draws it back to its chest, slowly, harder at the end, as it leans back and
    # turns side-on, the drawing elbow coming up and out; the limbs bend. Held at full draw, trembling.
    "Draw": clip(21, keys(held(LOW, 1), {
        "Hips": [(4, ((0, 0, -2), (0, 0.01, -0.05))), (9, ((0, 0, 3), (0, 0.01, 0.014))), (12, ((0, 0, 4), (0, 0.015, -0.01))), (19, DRAWN["Hips"]), (22, DRAWN["Hips"])],
        "Spine": [(4, (12, 0, -4)), (9, (-3, 0, 3)), (12, (-1, 0, 4)), (19, (-6, 0, 6)), (22, DRAWN["Spine"])],
        "Chest": [(5, (10, 0, -4)), (10, (-5, 0, 3)), (13, (-2, 0, 3)), (20, (-7, 0, 5)), (22, DRAWN["Chest"])],
        "Head": [(5, (-6, 0, 2)), (11, (9, 0, -10)), (14, (7, 0, -13)), (20, (12, 0, -19)), (22, DRAWN["Head"])],
        "Bow": [(4, low((8, 0, 0), (0, 0.02, -0.05))), (8, bow((-8, 0, 2), 0, -0.7, 1.09)), (11, bow((2, 0, 0), 0, -0.68, 1.02)),
                (13, bow((0, 0, 0), 0, -0.68, 1.03)), (19, bow((0.5, 0, 0), 0, -0.68, 1.03)), (20, bow((-0.7, 0, 0.6), 0, -0.68, 1.035)),
                (21, bow((0.7, 0, -0.5), 0, -0.68, 1.027)), (22, DRAWN["Bow"])],
        "Pull": [(7, (0, 0, 0)), (11, (1, 0, 0)), (17, (1, 0.78, 0)), (20, (1, 0.98, 0)), (21, (1, 0.96, 0)), (22, DRAWN["Pull"])],
        "Nock": [(11, (0, 0, 0)), (17, (0.78, 0, 0)), (20, (0.98, 0, 0)), (21, (0.96, 0, 0)), (22, DRAWN["Nock"])],
        "Pack": [(4, (-2, 0, 0)), (9, (11, 0, 1)), (14, (-1, 0, 0)), (18, (5, 0, -1)), (22, DRAWN["Pack"])],
        "Hem": [(5, (3, 0, 0)), (10, (-5, 0, 0)), (15, (2, 0, 1)), (22, DRAWN["Hem"])],
        "Lantern": [(5, (-6, 0, 0)), (10, (10, 0, 2)), (15, (-4, 0, -1)), (22, DRAWN["Lantern"])],
    })),
    # Release (0.25 s; replayed for each arrow of a volley): the loose at frame 2 (where the shot leaves). The string
    # snaps forward past rest and back, the limbs kicking; the bow jumps; the right hand flies back and out off the
    # string; it rocks back on its heels, the head jolting after; then the hand comes forward to the string again (the
    # next arrow) and the bow steadies level, aiming.
    "Release": clip(5, keys(held(DRAWN, 1), held(AIM, 6), {
        "Nock": [(2, (-0.4, 0, 0)), (3, (0.16, 0, 0)), (4, (-0.06, 0, 0)), (5, (0.02, 0, 0))],
        "Pull": [(2, (1, 1.1, 0)), (3, (1, 1.05, 0)), (4, (1, 0.6, 0)), (5, (1, 0.12, 0))],
        "Flick": [(2, ((0, 0, 0), (0.1, 0.07, 0.07))), (3, ((0, 0, 0), (0.15, 0.09, 0.11))), (4, ((0, 0, 0), (0.07, 0.04, 0.05))), (5, ((0, 0, 0), (0.015, 0, 0.01)))],
        "Bow": [(2, bow((-8, 0, 1), 0, -0.72, 1.055)), (3, bow((3, 0, -1), 0, -0.7, 1.03)), (4, bow((-1, 0, 0), 0, -0.685, 1.032))],
        "Hips": [(3, ((0, 0, 6), (0, 0.045, -0.025)))],
        "Spine": [(2, (-8, 0, 6)), (4, (-4, 0, 5))],
        "Chest": [(3, (-11, 0, 5)), (5, (-4, 0, 4))],
        "Head": [(3, (4, 0, -18)), (5, (8, 0, -14))],
        "Pack": [(3, (-2, 0, 0)), (5, (6, 0, 0))],
        "Lantern": [(3, (6, 0, 0)), (5, (-3, 0, 0))],
    })),
    # ShotRecover (0.6 s; 1 s after a volley): the right hand leaves the string for the bow, the bow comes down to its
    # belly (dropping past, settling), and it lets its breath go: the body sags back into its hunch, the head last; the
    # pack and lantern swing after.
    "ShotRecover": clip(14, keys(held(AIM, 1), held(LOW, 15), {
        "Pull": [(5, (0, 0, 0))],
        "Bow": [(4, bow((8, 2, -2), 0.01, -0.66, 1.0)), (9, low((6, 2, 0), (0, 0.01, -0.05))), (12, low((-2, 0, 0), (0, 0, 0.012)))],
        "Hips": [(5, ((0, 0, 2), (0, 0.01, -0.035))), (10, ((0, 0, 0), (0, 0, 0.012)))],
        "Spine": [(6, (10, 0, 0)), (11, (6, 0, -3))],
        "Chest": [(7, (9, 0, -1)), (12, (4, 0, -3))],
        "Head": [(4, (6, 0, -8)), (9, (-6, 0, 2)), (12, (-15, 0, 6))],
        "Pack": [(4, (-2, 0, 0)), (8, (9, 0, 0)), (12, (-1, 0, 0))],
        "Hem": [(6, (-4, 0, 0)), (11, (2, 0, 0))],
        "Lantern": [(6, (-8, 0, 0)), (11, (5, 0, 0))],
    })),
    # BashWindup (0.45 s): a twist the other way first (and a dip), then it hauls the bow round to its right and up,
    # holding it like a club, its right limb outwards; the hips turn with it, the head stays on you (turning against
    # the body); the pack drags behind the turn.
    "BashWindup": clip(10, keys(held(LOW, 1), {
        "Hips": [(3, ((0, 0, -5), (0, 0.01, -0.04))), (8, ((0, 0, 13), (0.02, 0.04, -0.04))), (11, WOUND["Hips"])],
        "Spine": [(3, (10, 0, -8)), (8, (3, 0, 17)), (11, WOUND["Spine"])],
        "Chest": [(4, (8, 0, -9)), (9, (-1, 0, 12)), (11, WOUND["Chest"])],
        "Head": [(4, (-10, 0, 10)), (9, (2, 0, -34)), (11, WOUND["Head"])],
        "Bow": [(3, low((10, -8, -4), (-0.1, 0.02, 0.02))), (6, bow((20, -6, 4), 0.25, -0.35, 0.92)), (9, bow((0, -14, 34), 0.45, 0.03, 1.14)), (11, WOUND["Bow"])],
        "Pack": [(4, (2, 0, 4)), (9, (4, 0, -12)), (11, (3, 0, -8))],
        "Hem": [(5, (0, 0, -5)), (11, (0, 0, 8))],
        "Lantern": [(5, (0, 6, 0)), (11, (0, -6, 0))],
    })),
    # Bash (0.2 s; the blow lands at frame 3): swung across in front and down, the hips leading, the chest after, the
    # bow last; a lunge onto its front foot; the head stays on you; the bow's far tip swats through at frame 3 and
    # carries on round to its left.
    "Bash": clip(4, {
        "Hips": [(1, WOUND["Hips"]), (3, ((0, 0, -6), (-0.01, -0.09, -0.07))), (5, THROUGH["Hips"])],
        "Spine": [(1, WOUND["Spine"]), (2, (6, 0, 8)), (3, (12, 0, -8)), (5, THROUGH["Spine"])],
        "Chest": [(1, WOUND["Chest"]), (2, (0, 0, 10)), (4, (8, 0, -6)), (5, THROUGH["Chest"])],
        "Head": [(1, WOUND["Head"]), (3, (4, 0, 2)), (5, THROUGH["Head"])],
        "Bow": [(1, WOUND["Bow"]), (2, bow((0, 4, -30), 0.3, -0.42, 1.02)), (3, bow((0, 16, -105), -0.04, -0.58, 0.9)), (5, THROUGH["Bow"])],
        "Pack": [(1, (3, 0, -8)), (3, (2, 0, 6)), (5, (5, 0, 12))],
        "Hem": [(1, (0, 0, 8)), (3, (-4, 0, 0)), (5, (-6, 0, -8))],
        "Lantern": [(1, (0, -6, 0)), (3, (4, 4, 0)), (5, (2, 10, 0))],
    }),
    # BashRecover (0.6 s): carried past by the swing (a stagger-step, the pack whipping back), then it gathers the bow
    # back across its belly and straightens into its hunch.
    "BashRecover": clip(14, keys(held(LOW, 15), {
        "Hips": [(1, THROUGH["Hips"]), (4, ((0, 0, -18), (-0.04, -0.08, -0.09))), (10, ((0, 0, -5), (-0.01, -0.02, -0.02)))],
        "Spine": [(1, THROUGH["Spine"]), (4, (16, 0, -20)), (10, (9, 0, -7))],
        "Chest": [(1, THROUGH["Chest"]), (5, (12, 0, -14)), (11, (6, 0, -5))],
        "Head": [(1, THROUGH["Head"]), (5, (6, 0, 30)), (11, (-8, 0, 10))],
        "Bow": [(1, THROUGH["Bow"]), (4, bow((6, 20, -155), -0.32, -0.42, 0.8)), (9, bow((26, 14, -50), -0.08, -0.54, 0.74)),
                (12, low((6, 0, 2), (0, 0.01, -0.03)))],
        "Pack": [(1, (5, 0, 12)), (5, (4, 0, -10)), (9, (3, 0, 5)), (13, (1, 0, -2))],
        "Hem": [(1, (-6, 0, -8)), (6, (0, 0, 6)), (11, (0, 0, -2))],
        "Lantern": [(1, (2, 10, 0)), (6, (0, -8, 0)), (11, (0, 3, 0))],
    })),
    # Stagger (0.6 s as made; a stun plays it slowed): knocked back, the bow flung up in its hands, the head snapping
    # back and the pack thrown about; a stumble back; then it gathers itself, the bow back across its belly.
    "Stagger": clip(18, keys(held(LOW, 1), held(LOW, 19), {
        "Hips": [(4, ((0, 0, 3), (0, 0.13, 0.0))), (9, ((0, 4, 0), (0.04, 0.16, -0.06))), (14, ((0, -2, 0), (0, 0.05, -0.03)))],
        "Spine": [(4, (-16, 0, 6)), (9, (-6, 4, 0)), (14, (9, -2, -4))],
        "Chest": [(4, (-14, 0, 8)), (10, (-4, 0, -4)), (15, (7, 0, -3))],
        "Head": [(3, (-26, 0, 10)), (8, (-8, 0, -6)), (13, (-6, 0, 6))],
        "Bow": [(4, bow((-10, 4, 12), 0.04, -0.42, 1.04)), (10, bow((22, 8, -6), 0.06, -0.5, 0.82)), (15, low((6, 0, 0), (0, 0.01, -0.03)))],
        "Pack": [(4, (-3, 0, 4)), (8, (16, 0, -6)), (13, (-2, 0, 3))],
        "Hem": [(6, (8, 4, 0)), (12, (-4, -2, 0))],
        "Lantern": [(5, (16, 0, 4)), (10, (-10, 0, -2)), (15, (4, 0, 0))],
    })),
    # Death (0.8 s; then it dissolves): a jolt back, its right hand losing the bow (the arm flops), then it folds:
    # down and forward over the bow, which hits the floor (a bounce) under its left hand; the pack tumbles over its
    # shoulders after it and the head hangs last.
    "Death": clip(24, keys(held(LOW, 1), {
        "Hips": [(5, ((0, 0, 4), (0, 0.1, 0.0))), (13, ((0, 0, 0), (0, -0.04, -0.17))), (19, ((0, 0, -4), (0, -0.06, -0.27))), (25, ((0, 0, -4), (0, -0.06, -0.29)))],
        "Spine": [(5, (-16, 0, 6)), (13, (24, 0, -4)), (19, (40, 0, -8)), (25, (43, 0, -8))],
        "Chest": [(5, (-12, 0, 4)), (14, (14, 0, -4)), (20, (22, 0, -6)), (25, (20, 0, -6))],
        "Head": [(4, (-24, 0, 4)), (11, (-8, 0, 4)), (18, (30, 0, -4)), (22, (48, 0, -10)), (25, (44, 0, -8))],
        "Bow": [(5, bow((24, 6, 8), 0.04, -0.42, 0.9)), (12, bow((60, 20, 20), -0.06, -0.62, 0.42)), (16, bow((88, 24, 26), -0.08, -0.6, 0.09)),
                (19, bow((80, 22, 24), -0.08, -0.6, 0.13)), (25, bow((86, 24, 26), -0.08, -0.6, 0.095))],
        "LetGo": [(4, (0, 0, 0)), (9, (1, 0, 0))],
        "Flick": [(6, ((0, 0, 0), (0.06, 0.1, 0.05))), (12, ((0, 0, 0), (0.02, -0.12, 0))), (18, ((0, 0, 0), (0.0, 0.08, 0))),
                  (22, ((0, 0, 0), (0.0, -0.03, 0))), (25, ((0, 0, 0), (0.0, 0.0, 0)))],
        "Pack": [(6, (-4, 0, 0)), (14, (12, 0, 6)), (20, (-6, 0, 2)), (25, (-2, 0, 0))],
        "Hem": [(8, (6, 0, 0)), (16, (-8, 0, 0)), (25, (-4, 0, 0))],
        "Lantern": [(6, (14, 0, 0)), (14, (-10, 0, 4)), (20, (6, 0, 0)), (25, (2, 0, 0))],
    })),
}


def build():
    robe, dark = kit.mat("SouredOchre", "#8a6d3e"), kit.mat("SouredOchreDark", "#5e4a2c")
    eye, claw = kit.mat("SouredEye_Glow", "#c8f07a"), kit.mat("Claw", "#d6cdb8")
    bow_wood, string = kit.mat("BowWood", "#4a3424"), kit.mat("BowString", "#b9ae96")
    cloth, metal = kit.mat("HeapCloth", "#8c7a5c"), kit.mat("HeapMetal", "#34313a")
    quiver = kit.mat("Quiver", "#5a3e2a")
    # (the arrows in the quiver: the bow's wood, under a name of their own, so "BowWood" is the bow alone: the game
    # finds the bow-jab's swoosh by the BowWood farthest from the grip)
    shaft = kit.mat("ArrowShaft", "#4a3424")

    def tattered(bm, below):
        rim = sorted((v for v in bm.verts if v.co.z < below and v.co.length > 0.1), key=lambda v: math.atan2(v.co.y, v.co.x))
        for i, v in enumerate(rim):
            v.co.z += 0.1 if i % 2 else 0.0  # tattered hem

    def skirt(bm):
        kit.cone(9, 0.5, 0.3, 0.66)(bm)
        tattered(bm, -0.3)

    # The robe in two (the old robe's cone, cut at the waist): the skirt hangs from the waist (it swings after the body,
    # its hem on the floor), the upper robe moves with the body; a cord belt hides where they meet.
    parts = [
        part("Skirt", skirt, robe, M((0, 0.031, 0.374), rot=(5, 0, 0)), smooth=True, group="Hem"),
        part("Robe", kit.cone(9, 0.384, 0.24, 0.62), robe, M((0, -0.035, 0.848), rot=(8, 0, 0)), smooth=True, group="Spine"),
        part("Belt", kit.cylinder(12, 0.39, 0.06), dark, M((0, 0.0, 0.62), rot=(7, 0, 0)), smooth=True, group="Spine"),
        part("Hump", kit.sphere(10, 7), robe, M((0, 0.05, 1.02), scale=(0.38, 0.32, 0.28)), smooth=True, group="Chest"),
        part("Head", kit.sphere(10, 7, 0.21), dark, M((0, -0.22, 1.28), scale=(1, 1.05, 0.95)), smooth=True, group="Head"),
        part("Hood", kit.cone(8, 0.27, 0.05, 0.46), robe, M((0, -0.15, 1.41), rot=(-35, 0, 0)), smooth=True, group="Head"),
    ]
    for s, arm, fore, shoulder, elbow, hand in ((-1, "ArmL", "ForeL", SHOULDER_L, ELBOW_L, HAND_L), (1, "ArmR", "ForeR", SHOULDER_R, ELBOW_R, HAND_R)):
        parts.append(part("Eye", kit.sphere(8, 6, 0.04), eye, M((s * 0.08, -0.42, 1.28)), smooth=True, group="Head"))
        # The arm in two (a sleeve to the elbow, a forearm to the claws), the elbow a knob of robe.
        parts.append(part("Sleeve", kit.cone(7, 0.1, 0.08, math.dist(shoulder, elbow) + 0.04), robe, kit.along(shoulder, elbow), smooth=True, group=arm))
        parts.append(part("Elbow", kit.sphere(8, 6, 0.08), robe, M(elbow), smooth=True, group=arm))
        parts.append(part("Forearm", kit.cone(7, 0.075, 0.05, math.dist(elbow, hand)), robe, kit.along(elbow, hand), smooth=True, group=fore))
        parts.append(part("Claw", kit.cone(5, 0.035, 0.0, 0.1), claw, kit.along(hand, (s * 0.08, -0.67, 1.03)), group=fore))

    # The bow, held level across the body: limbs curving back towards it, the string between the tips. In five: the
    # middle (and its cloth grip) on the Bow, each limb from beside the grip to its tip (they bend when it's drawn), and
    # the string in two halves (they follow the nock).
    def along_box(a, b, thick):
        return kit.along(a, b) @ M(scale=(thick, thick, math.dist(a, b)))
    z = GRIP[2]
    parts += [
        part("BowLimbs", kit.tube([(-0.13, -0.606, z), (0.0, -0.63, z), (0.13, -0.606, z)], [0.03, 0.032, 0.03], seg=5), bow_wood, group="Bow"),
        part("BowLimbs", kit.tube([(-0.1, -0.611, z), (-0.32, -0.57, z), TIP_L], [0.03, 0.026, 0.018], seg=5), bow_wood, group="LimbL"),
        part("BowLimbs", kit.tube([(0.1, -0.611, z), (0.325, -0.57, z), TIP_R], [0.03, 0.026, 0.018], seg=5), bow_wood, group="LimbR"),
        part("BowGrip", kit.cylinder(6, 0.04, 0.12), cloth, M((0, -0.63, z), rot=(0, 90, 0)), group="Bow"),
        part("BowString", kit.cube(), string, along_box(TIP_L, (0.005, STRING_Y, z), 0.01), group="StringL"),
        part("BowString", kit.cube(), string, along_box(TIP_R, (-0.005, STRING_Y, z), 0.01), group="StringR"),
    ]

    # On its back: a small bundle of belongings (all it kept of its heap) and a quiver of green-tipped arrows.
    parts += [
        part("Bundle", kit.sphere(8, 6), cloth, M((-0.06, 0.34, 1.04), scale=(0.26, 0.2, 0.24)), smooth=True, group="Pack"),
        part("Kettle", kit.cylinder(8, 0.1, 0.14), metal, M((-0.2, 0.36, 1.26), rot=(20, 0, 0)), group="Pack"),
        part("Quiver", kit.cylinder(7, 0.08, 0.6), quiver, M((0.2, 0.3, 1.18), rot=(-18, -16, 0)), group="Pack"),
    ]
    # (the arrows stand in the quiver's mouth, along its tilt: a few centimetres in, the rest out, the tips glowing)
    tilt = M(rot=(-18, -16, 0))
    mouth = Vector((0.2, 0.3, 1.18)) + tilt @ Vector((0, 0, 0.3))
    for dx, dy in ((0.0, 0.0), (0.04, 0.03), (-0.03, 0.04), (0.02, -0.03)):
        at = mouth + tilt @ Vector((dx, dy, 0))
        parts.append(part("Arrow", kit.cylinder(4, 0.008, 0.2), shaft, M(at + tilt @ Vector((0, 0, 0.06)), rot=(-18, -16, 0)), group="Pack"))
        parts.append(part("ArrowTip", kit.cone(4, 0.022, 0.0, 0.05), eye, M(at + tilt @ Vector((0, 0, 0.18)), rot=(-18, -16, 0)), group="Pack"))

    # A lantern hung from its belt on the right, like every Soured's: green light in the dark = a Soured.
    lantern_glass = kit.mat("SouredLantern_Glow", "#b6f06a")
    parts += [
        part("BeltLanternCord", kit.cube(), metal, M((0.46, 0.0, 0.78), rot=(0, -15, 0), scale=(0.015, 0.015, 0.14)), group="Lantern"),
        part("BeltLanternTop", kit.cone(4, 0.08, 0.01, 0.07), metal, M((0.48, 0.0, 0.68), rot=(0, 0, 45)), group="Lantern"),
        part("BeltLanternGlass", kit.cube(), lantern_glass, M((0.48, 0.0, 0.59), scale=(0.1, 0.1, 0.13)), group="Lantern"),
        part("BeltLanternBase", kit.cube(), metal, M((0.48, 0.0, 0.515), scale=(0.12, 0.12, 0.02)), group="Lantern"),
    ]

    mesh = kit.join(parts, "LadenArcher")
    armature = kit.rig("LadenArcherRig", BONES)
    kit.skin(mesh, armature)
    kit.animate(armature, ACTIONS)
    for (name, _), worst in zip(ACTIONS.items(), REACH.values()):
        if worst > 0.02:
            print(f"[laden_archer] {name}: the hands come off the bow by up to {worst * 100:.0f} cm")
    return [armature, mesh]
