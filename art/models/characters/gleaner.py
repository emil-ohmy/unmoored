"""A Gleaner (the Orchard): a farmhand who went on gleaning after the harvest failed, walking the rows at night with a
lantern on a long pole, picking the field over for grain that was never there. Thin and stooped in a long linen smock,
hooded, a gleaning sack slung on its left shoulder, its eyes the Soured's green; in both hands the pole, taller than
it is, its lantern (fire, not the Soured's green) hung from a crook at the tip. And, like every Soured, a small green
lantern at its belt.

Clips: Idle (stooped, breathing, peering at the ground), Walk (a quick shuffle, the lantern bobbing), Hop (0.4 s: a
light skip backwards, the smock flaring), Windup (0.9 s: the pole drawn back over its head, the lantern behind it,
flaring: the tell), Fling (0.35 s: the pole swept forward and down, the fire flung out at you), Recover (0.8 s),
Stagger, Death (it folds down, the pole falling from its hands).

How it's animated (like the Laden): each bone keyed on its own timing (kit.bake: tracks). The body leads (hips, spine,
chest), the head follows; the pole is keyed where it is (by its grip, in the world), not through the arms, and both
hands are bent onto it every frame (kit.reach); the lantern hangs plumb from the crook and swings after the pole (its
keys are its swing away from hanging straight down, in the world); the skirt of the smock, the sack on its back and the
belt lantern swing after the body.

The bones: "PoleShaft" is the pole; "Pole" is the lantern hung from its crook. (The game hangs the lantern's light on
the bone named "Pole", and throws the fire from that light: so the light and the fire follow the swinging lantern.)
"""
import math

from mathutils import Euler, Vector

M, part = kit.M, kit.part  # `kit` is injected by build_model.py

SHOULDER_L, ELBOW_L, HAND_L = (-0.2, -0.06, 1.31), (-0.27, -0.1, 1.05), (-0.22, -0.36, 0.96)
SHOULDER_R, ELBOW_R, HAND_R = (0.2, -0.06, 1.31), (0.29, -0.1, 1.05), (0.27, -0.36, 0.96)
BUTT, TIP = (0.28, -0.22, 0.45), (0.28, -0.5, 2.5)       # the pole's ends, at rest
SHAFT_HEAD = (0.28, -0.29, 0.96)                          # where the right hand holds it (on its axis)
GRIP_R, GRIP_L = SHAFT_HEAD, (0.28, -0.312, 1.13)          # the hands' grips on the pole (the left above the right)
HOOK = (0.28, -0.68, 2.5)                                 # the end of the crook, the lantern's cord hung from it
BONES = {
    "Root": ((0, 0, 0), (0, 0, 0.35), None),
    "Hips": ((0, 0, 0.35), (0, 0, 0.6), "Root"),
    "Hem": ((0, -0.01, 0.62), (0, 0.03, 0.2), "Hips"),            # the smock's skirt, hanging from the waist
    "Spine": ((0, 0, 0.6), (0, -0.06, 1.1), "Hips"),
    "Chest": ((0, -0.06, 1.1), (0, -0.1, 1.34), "Spine"),
    "Head": ((0, -0.1, 1.34), (0, -0.22, 1.62), "Chest"),
    "ArmL": (SHOULDER_L, ELBOW_L, "Chest"),
    "ForeL": (ELBOW_L, HAND_L, "ArmL"),
    "ArmR": (SHOULDER_R, ELBOW_R, "Chest"),
    "ForeR": (ELBOW_R, HAND_R, "ArmR"),
    "Sack": ((-0.18, 0.12, 1.3), (-0.16, 0.2, 0.72), "Chest"),     # the gleaning sack, hung on its back
    "BeltLantern": ((0.3, 0.02, 0.69), (0.3, 0.02, 0.53), "Spine"),  # the green lantern at its belt
    "PoleShaft": (SHAFT_HEAD, TIP, "Root"),                       # the pole (keyed where it is: the hands follow it)
    "Pole": (HOOK, (0.28, -0.68, 2.22), "PoleShaft"),              # the pole's lantern, hung from the crook
}

# The smock: one long cone, cut at the waist into the skirt (on Hem) and the body (on Spine); a twine belt over the cut.
SMOCK = M((0, -0.03, 0.72), rot=(10, 0, 0))
SKIRT_M, BODY_M = SMOCK @ M((0, 0, -0.3025)), SMOCK @ M((0, 0, 0.1975))
SKIRT_RIM = [SKIRT_M @ Vector((0.36 * math.cos(a), 0.36 * math.sin(a), -0.2225)) for a in (2 * math.pi * k / 18 for k in range(18))]
FLOOR = 0.03  # (the skirt's hem never lower than this: it crumples rather than sinking)

# Where the hands go when it lets go of the pole (dying): hanging under the shoulders, in the chest's space.
LIMP_L, LIMP_R = Vector((-0.25, -0.2, 0.86)), Vector((0.27, -0.2, 0.86))


def shaft(rot, x, y, z):
    """The pole turned `rot`, its right-hand grip at (x, y, z) in the world."""
    return (tuple(rot), (round(x - SHAFT_HEAD[0], 4), round(y - SHAFT_HEAD[1], 4), round(z - SHAFT_HEAD[2], 4)))


def shaft_on_butt(rot, x, y, z):
    """The pole turned `rot`, its butt at (x, y, z) (standing on it, or lying on the ground)."""
    turn = Euler([math.radians(a) for a in rot]).to_matrix()
    grip = Vector((x, y, z)) + turn @ (Vector(SHAFT_HEAD) - Vector(BUTT))
    return shaft(rot, *grip)


def hang(pose, mats, bone, parent):
    """A hanging thing's key is its swing in the world (0: hanging straight down): turned here into its rotation on
    its parent, whatever the parent is doing."""
    swing = Euler([math.radians(a) for a in pose[bone][0]]).to_quaternion()
    local = mats[parent].to_quaternion().inverted() @ swing
    pose[bone] = tuple(math.degrees(a) for a in local.to_euler("XYZ"))


def eased(f, start, end):
    if f <= start:
        return 0.0
    if f >= end:
        return 1.0
    t = (f - start) / (end - start)
    return t * t * (3 - 2 * t)


def grip(pose, let_go=0.0):
    """The lanterns hung plumb (swinging by their keys), the skirt kept off the floor, the hands on the pole (or, as it
    lets go, dropping to hang at its sides)."""
    pose = dict(pose)
    mats = kit.pose_matrices(BONES, dict(pose, Pole=(0, 0, 0), BeltLantern=(0, 0, 0)))
    hang(pose, mats, "Pole", "PoleShaft")
    hang(pose, mats, "BeltLantern", "Spine")
    hem_rot, hem_off = pose["Hem"]
    low = min((mats["Hem"] @ p).z for p in SKIRT_RIM)
    if low < FLOOR:
        lift = mats["Hips"].to_quaternion().inverted() @ Vector((0, 0, FLOOR - low))
        pose["Hem"] = (hem_rot, tuple(Vector(hem_off) + lift))

    def target(point, limp):
        return lambda m: (m["PoleShaft"] @ Vector(point)).lerp(m["Chest"] @ limp, let_go)
    pose = kit.reach(BONES, pose, "ArmR", "ForeR", target(GRIP_R, LIMP_R), (0.6, 0.5, -1))
    pose = kit.reach(BONES, pose, "ArmL", "ForeL", target(GRIP_L, LIMP_L), (-0.8, 0.1, -1))
    return pose


REACH = {}  # (the build's report: how far each clip's hands come off the pole, at worst, while holding it)


def clip(length, tracks, periodic=False, let_go=None):
    """A clip `length` frames long (30 a second) from per-bone tracks, baked to every frame, the hands kept on the pole
    (`let_go`: (from, to) frames over which the hands come off it)."""
    frames, worst = [], (0.0, 0)
    for f, p in kit.bake(tracks, length, periodic):
        w = eased(f, *let_go) if let_go else 0.0
        p = grip(p, w)
        frames.append((f, p))
        if w == 0.0:
            m = kit.pose_matrices(BONES, p)
            for fore, hand, point in (("ForeL", HAND_L, GRIP_L), ("ForeR", HAND_R, GRIP_R)):
                worst = max(worst, ((m[fore] @ Vector(hand) - m["PoleShaft"] @ Vector(point)).length, f))
    REACH[len(REACH)] = worst
    return ("LINEAR", frames)


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


def hips(rot=(0, 0, 0), off=(0, 0, 0)):
    return (tuple(rot), tuple(off))


# CARRY: what everything starts from and comes back to: stooped, the pole held upright before its right side in both
# hands (the chest turned a little towards it, the head turned back to look ahead), the lantern hanging still.
CARRY = {"Hips": hips(), "Hem": (0, 0, 0), "Spine": (8, 0, 0), "Chest": (5, 0, 12), "Head": (-8, 0, -10),
         "Sack": (0, 0, 0), "BeltLantern": (0, 0, 0), "PoleShaft": shaft((3, 4, 0), 0.2, -0.34, 0.95), "Pole": (0, 0, 0)}
# DRAWN (the end of the wind-up): leaning back and turned, the pole drawn back over its head, the lantern behind it.
DRAWN = {"Hips": hips((-4, 0, 10), (0, 0.03, 0.01)), "Hem": (-2, 0, -3), "Spine": (-8, 0, 0), "Chest": (-8, 0, 28),
         "Head": (10, 0, -32), "Sack": (2, 0, 0), "BeltLantern": (0, 0, 0), "PoleShaft": shaft((-54, 20, 0), 0.22, -0.04, 1.4),
         "Pole": (3, 0, 0)}
# FLUNG (the end of the fling): lunged forward and bent over, the pole swept down before it, the lantern swung out.
FLUNG = {"Hips": hips((6, 0, 6), (0, -0.12, -0.05)), "Hem": (-4, 0, 0), "Spine": (14, 0, 0), "Chest": (8, 0, 16),
         "Head": (-24, 0, 6), "Sack": (-4, 0, 0), "BeltLantern": (-6, 0, 0), "PoleShaft": shaft((62, 4, 0), 0.12, -0.74, 1.04),
         "Pole": (-24, 0, 0)}

ACTIONS = {
    # Idle (2 s loop): stooped, breathing (the chest lifts and sinks, the pole riding the breath a moment later, the
    # lantern swaying after it); its weight shifts from foot to foot; it peers down at the ground to its left (still
    # gleaning), then looks back up at you; a twitch of the head.
    "Idle": clip(59, {
        "Hips": [(1, hips()), (16, hips((0, 3, 0), (-0.025, 0.005, 0.012))), (34, hips((0, 0, 0), (0, 0, -0.01))), (50, hips((0, -3, 0), (0.02, 0.005, 0.006)))],
        "Spine": [(1, (8, 0, 0)), (20, (3, -2, 0)), (42, (13, 2, 0))],
        "Chest": [(1, (5, 0, 12)), (23, (-2, -2, 14)), (45, (10, 2, 10))],
        "Head": [(1, (-8, 0, -10)), (9, (-5, 0, -14)), (18, (14, -4, -36)), (27, (11, -2, -31)), (35, (-12, 0, -4)),
                 (45, (-9, 0, -10)), (47, (-3, 10, -15)), (51, (-10, -3, -8))],
        "PoleShaft": [(1, shaft((3, 4, 0), 0.2, -0.34, 0.95)), (24, shaft((-1, 2, 0), 0.195, -0.34, 0.975)), (46, shaft((8, 6, 0), 0.205, -0.33, 0.935))],
        "Pole": [(1, (0, 0, 0)), (12, (5, 2, 0)), (30, (-6, -2, 0)), (40, (3, 0, 0)), (54, (-5, 2, 0))],
        "Sack": [(1, (0, 0, 0)), (26, (-5, 0, 2)), (48, (5, 0, -2))],
        "Hem": [(1, (0, 0, 0)), (20, (1, -2, 0)), (40, (2, 2, 0))],
        "BeltLantern": [(1, (0, 0, 0)), (22, (0, 6, 3)), (48, (5, -4, 0))],
    }, periodic=True),
    # Walk (16 frames, two quick shuffling steps): it drops onto each step (frames 5 and 13), rolls over onto that side
    # and rises between (upright at frame 1: the loop starts and ends on its passing pose); the chest rolls after the
    # hips, the head bobs after the chest; the pole rocks with the steps, its lantern swinging after it; the skirt
    # kicks, the sack bounces on its back.
    "Walk": clip(16, {
        "Hips": [(1, hips((0, 0, 0), (0, 0, 0.025))), (5, hips((0, -7, 6), (0.035, 0, -0.04))),
                 (9, hips((0, 0, 0), (0, 0, 0.025))), (13, hips((0, 7, -6), (-0.035, 0, -0.04)))],
        "Spine": [(2, (9, 0, 0)), (6, (13, 5, -2)), (10, (9, 0, 0)), (14, (13, -5, 2))],
        "Chest": [(3, (4, 0, 12)), (7, (8, 4, 7)), (11, (4, 0, 12)), (15, (8, -4, 17))],
        "Head": [(4, (-12, 0, -10)), (8, (-3, -4, -6)), (12, (-12, 0, -10)), (16, (-3, 4, -14))],
        "PoleShaft": [(2, shaft((0, 4, 0), 0.2, -0.345, 0.97)), (6, shaft((8, 0, 0), 0.21, -0.34, 0.92)),
                      (10, shaft((0, 4, 0), 0.2, -0.345, 0.97)), (14, shaft((8, 8, 0), 0.19, -0.34, 0.92))],
        "Pole": [(1, (-9, -5, 0)), (5, (8, 0, 0)), (9, (-9, 5, 0)), (13, (8, 0, 0))],
        "Hem": [(3, (-10, 0, 6)), (7, (6, 5, 0)), (11, (-10, 0, -6)), (15, (6, -5, 0))],
        "Sack": [(4, (5, 0, 0)), (8, (-8, 0, 4)), (12, (5, 0, 0)), (16, (-8, 0, -4))],
        "BeltLantern": [(3, (9, -6, 0)), (7, (-6, 0, 0)), (11, (9, 6, 0)), (15, (-6, 0, 0))],
    }, periodic=True),
    # Hop (0.4 s; the game carries it back): a crouch, a light spring up and back (the body tipping back the way it
    # goes, the head staying on you), the skirt flaring forward and the lantern left swinging out in front; it lands
    # with a dip, and settles.
    "Hop": clip(9, keys(held(CARRY, 1), held(CARRY, 10), {
        "Hips": [(3, hips((4, 0, 0), (0, -0.01, -0.07))), (5, hips((-7, 0, 0), (0, 0.03, 0.19))), (6, hips((-6, 0, 0), (0, 0.03, 0.17))),
                 (8, hips((3, 0, 0), (0, 0, -0.06)))],
        "Spine": [(3, (14, 0, 0)), (5, (3, 0, 0)), (8, (14, 0, 0))],
        "Chest": [(4, (10, 0, 12)), (6, (-2, 0, 12)), (9, (9, 0, 12))],
        "Head": [(4, (-14, 0, -10)), (7, (2, 0, -10)), (9, (-12, 0, -10))],
        "PoleShaft": [(3, shaft((4, 4, 0), 0.2, -0.355, 0.885)), (5, shaft((10, 4, 0), 0.2, -0.22, 1.2)), (6, shaft((12, 4, 0), 0.2, -0.23, 1.18)),
                      (8, shaft((-6, 4, 0), 0.2, -0.32, 0.9))],
        "Pole": [(3, (5, 0, 0)), (6, (-16, 0, 0)), (8, (8, 0, 0))],
        "Hem": [(3, (2, 0, 0)), (5, (-22, 0, 0)), (7, (8, 0, 0)), (9, (-3, 0, 0))],
        "Sack": [(3, (4, 0, 0)), (6, (-10, 0, 2)), (8, (8, 0, 0))],
        "BeltLantern": [(4, (6, 0, 0)), (6, (-12, 0, 0)), (8, (8, 0, 0))],
    })),
    # Windup (0.9 s: the tell): it coils (a dip, the pole tipped forward and in, the chest turning away), then draws
    # the pole up and back over its head, rising and leaning back, the chest turned with it, the head staying on you;
    # the lantern trails the draw, swings behind it when it stops and settles; held there, straining, the pole
    # creeping further back.
    "Windup": clip(21, keys(held(CARRY, 1), {
        "Hips": [(5, hips((3, 0, -2), (0, -0.01, -0.05))), (13, hips((-4, 0, 8), (0, 0.02, 0.02))), (22, DRAWN["Hips"])],
        "Spine": [(5, (14, 0, 0)), (13, (-4, 0, 0)), (16, (-9, 0, 0)), (22, DRAWN["Spine"])],
        "Chest": [(6, (10, 0, 4)), (14, (-6, 0, 26)), (17, (-10, 0, 30)), (22, DRAWN["Chest"])],
        "Head": [(6, (-14, 0, -6)), (15, (6, 0, -28)), (18, (12, 0, -34)), (22, DRAWN["Head"])],
        "PoleShaft": [(5, shaft((10, 2, 0), 0.2, -0.38, 0.9)), (12, shaft((-34, 14, 0), 0.21, -0.16, 1.32)), (16, shaft((-52, 21, 0), 0.22, -0.05, 1.4)),
                      (22, DRAWN["PoleShaft"])],
        "Pole": [(4, (4, 0, 0)), (10, (-14, 0, 0)), (17, (16, 0, 0)), (20, (-4, 0, 0)), (22, DRAWN["Pole"])],
        "Sack": [(6, (-6, 0, 0)), (14, (10, 0, -4)), (18, (-4, 0, 0)), (22, DRAWN["Sack"])],
        "Hem": [(5, (3, 0, 0)), (14, (-4, 0, -5)), (22, DRAWN["Hem"])],
        "BeltLantern": [(6, (6, 0, 0)), (14, (-8, 2, 0)), (19, (4, 0, 0)), (22, DRAWN["BeltLantern"])],
    })),
    # Fling (0.35 s; the fire leaves the lantern at 0.45 of it, frame 4.6): the hips lunge forward first, the chest
    # turns through after them, the pole whips over and down (through upright at frame 3, out before it at the
    # release), the lantern dragging behind and then flung out over the end; bent over at the end of it, the lantern
    # swung out past the pole and the skirt kicked forward.
    "Fling": clip(8, keys(held(DRAWN, 1), {
        "Hips": [(3, hips((3, 0, 3), (0, -0.05, -0.02))), (5, hips((7, 0, 2), (0, -0.11, -0.04))), (7, hips((8, 0, 5), (0, -0.13, -0.06))), (9, FLUNG["Hips"])],
        "Spine": [(3, (2, 0, 0)), (5, (12, 0, 0)), (7, (17, 0, 0)), (9, FLUNG["Spine"])],
        "Chest": [(2, (-6, 0, 24)), (4, (4, 0, 12)), (6, (9, 0, 14)), (8, (10, 0, 17)), (9, FLUNG["Chest"])],
        "Head": [(3, (6, 0, -24)), (5, (-14, 0, 0)), (7, (-27, 0, 8)), (9, FLUNG["Head"])],
        "PoleShaft": [(3, shaft((-15, 12, 0), 0.2, -0.3, 1.5)), (5, shaft((40, 5, 0), 0.15, -0.66, 1.42)), (7, shaft((68, 3, 0), 0.12, -0.8, 1.02)),
                      (9, FLUNG["PoleShaft"])],
        "Pole": [(3, (18, 0, 0)), (5, (-10, 0, 0)), (7, (-35, 0, 0)), (9, FLUNG["Pole"])],
        "Hem": [(4, (6, 0, 0)), (7, (-8, 0, 2)), (9, FLUNG["Hem"])],
        "Sack": [(3, (14, 0, 0)), (5, (18, 0, -4)), (8, (-6, 0, 0)), (9, FLUNG["Sack"])],
        "BeltLantern": [(2, (0, 0, 0)), (4, (12, 0, 0)), (7, (-10, 0, 0)), (9, FLUNG["BeltLantern"])],
    })),
    # Recover (0.8 s: the punish window): bent over its spent throw, panting, the pole sagging lower and the lantern
    # swinging to and fro under it; then it straightens and hauls the pole back up (past upright, settling), the
    # lantern swinging after it, back to how it carries it.
    "Recover": clip(19, keys(held(FLUNG, 1), held(CARRY, 20), {
        "Hips": [(6, hips((8, 0, 6), (0, -0.11, -0.06))), (10, hips((6, 0, 5), (0, -0.09, -0.05))), (15, hips((-1, 0, 0), (0, 0, 0.02)))],
        "Spine": [(5, (18, 0, 0)), (8, (14, 0, 0)), (10, (16, 0, 0)), (15, (5, 0, 0))],
        "Chest": [(5, (11, 0, 16)), (8, (7, 0, 15)), (11, (8, 0, 14)), (16, (2, 0, 13))],
        "Head": [(6, (-8, 0, 4)), (9, (-16, 0, 2)), (13, (-6, 0, -4)), (17, (-12, 0, -12))],
        "PoleShaft": [(6, shaft((70, 4, 0), 0.12, -0.72, 0.98)), (9, shaft((66, 4, 0), 0.13, -0.7, 0.98)), (12, shaft((44, 4, 0), 0.16, -0.56, 1.03)),
                      (15, shaft((10, 4, 0), 0.19, -0.38, 1.0)), (17, shaft((-3, 4, 0), 0.2, -0.33, 0.98)), (19, shaft((5, 4, 0), 0.2, -0.34, 0.94))],
        "Pole": [(4, (8, 0, 0)), (8, (-10, 0, 0)), (10, (2, 0, 0)), (13, (-14, 0, 0)), (16, (10, 0, 0)), (19, (-3, 0, 0))],
        "Sack": [(5, (6, 0, 0)), (9, (-2, 0, 0)), (14, (-8, 0, 0)), (17, (4, 0, 0))],
        "Hem": [(6, (2, 0, 0)), (10, (0, 0, 0)), (14, (5, 0, 0)), (17, (-2, 0, 0))],
        "BeltLantern": [(5, (6, 0, 0)), (9, (-3, 0, 0)), (14, (7, 0, 0)), (17, (-3, 0, 0))],
    })),
    # Stagger (0.5 s as made; played over the stagger or the stun): knocked back, the head snapping back after the
    # body, the pole tipped back and the lantern thrown out in front; it stumbles forward over it, the pole wobbling
    # forward, the lantern swinging back; then it gathers itself.
    "Stagger": clip(15, keys(held(CARRY, 1), held(CARRY, 16), {
        "Hips": [(3, hips((-8, 0, 4), (0, 0.1, 0.02))), (7, hips((4, -4, -6), (0.03, 0.08, -0.06))), (11, hips((-1, 2, 2), (0, 0.02, 0.01)))],
        "Spine": [(3, (-8, 4, 0)), (7, (14, -4, 0)), (11, (6, 0, 0))],
        "Chest": [(4, (-6, 0, 18)), (8, (8, 0, 6)), (12, (4, 0, 13))],
        "Head": [(4, (-24, 0, -4)), (8, (6, 0, -14)), (12, (-12, 0, -8))],
        "PoleShaft": [(3, shaft((-10, 8, 0), 0.21, -0.04, 1.06)), (5, shaft((-18, 8, 0), 0.21, -0.1, 1.04)), (8, shaft((14, -2, 0), 0.2, -0.38, 0.9)), (12, shaft((0, 6, 0), 0.2, -0.33, 0.96))],
        "Pole": [(5, (-22, 0, 0)), (9, (18, -4, 0)), (12, (-8, 0, 0)), (15, (3, 0, 0))],
        "Hem": [(4, (-10, 0, 0)), (8, (8, 2, 0)), (12, (-3, 0, 0))],
        "Sack": [(4, (8, 0, 0)), (8, (-10, 0, 0)), (12, (4, 0, 0))],
        "BeltLantern": [(4, (-10, 0, 0)), (8, (10, 0, 0)), (12, (-4, 0, 0))],
    })),
    # Death (0.85 s; then it dissolves): a jolt back, then it folds down, bowing over; the pole slips through its
    # hands onto its butt and they let it go: it topples forward and out, the lantern striking the ground (a bounce)
    # and propping it there; the head drops last.
    "Death": clip(25, keys(held(CARRY, 1), {
        "Hips": [(4, hips((-6, 0, 0), (0, 0.08, 0.01))), (11, hips((5, 0, -4), (0, 0, -0.1))), (17, hips((10, 0, -8), (0, -0.04, -0.22))),
                 (20, hips((9, 0, -8), (0, -0.04, -0.19))), (26, hips((10, 0, -8), (0, -0.04, -0.22)))],
        "Spine": [(4, (-10, 0, 0)), (12, (16, 0, 0)), (18, (30, 0, 0)), (21, (27, 0, 0)), (26, (30, 0, 0))],
        "Chest": [(5, (-10, 0, 16)), (13, (12, 0, 6)), (19, (18, 0, 0)), (22, (15, 0, 0)), (26, (16, 0, 0))],
        "Head": [(5, (-28, 0, -6)), (12, (-6, 0, -8)), (19, (36, 6, -10)), (23, (28, 4, -10)), (26, (32, 6, -10))],
        "PoleShaft": [(4, shaft((-10, 6, 0), 0.22, -0.02, 1.05)), (8, shaft_on_butt((6, 8, 0), 0.24, -0.3, 0.02)),
                      (11, shaft_on_butt((22, 8, 20), 0.24, -0.3, 0.02)), (14, shaft_on_butt((50, 6, 40), 0.24, -0.3, 0.02)),
                      (16, shaft_on_butt((69, 4, 45), 0.24, -0.3, 0.02)), (19, shaft_on_butt((64, 4, 45), 0.24, -0.3, 0.02)),
                      (22, shaft_on_butt((69, 4, 45), 0.24, -0.3, 0.02)), (26, shaft_on_butt((69, 4, 45), 0.24, -0.3, 0.02))],
        "Pole": [(4, (-10, 0, 0)), (9, (6, 0, 0)), (14, (18, 0, 0)), (17, (-6, 0, 0)), (21, (3, 0, 0)), (26, (0, 0, 0))],
        "Hem": [(5, (-6, 0, 0)), (12, (6, 0, 0)), (18, (-4, 0, 0)), (26, (-2, 0, 0))],
        "Sack": [(5, (10, 0, 0)), (12, (-8, 0, 0)), (18, (12, 0, 0)), (22, (6, 0, 0)), (26, (8, 0, 0))],
        "BeltLantern": [(5, (-10, 0, 0)), (12, (8, 0, 0)), (18, (-6, 0, 0)), (26, (0, 0, 0))],
    }), let_go=(4, 9)),
}


def build():
    smock, dark = kit.mat("GleanerSmock", "#a59a80"), kit.mat("GleanerHood", "#5e4c38")
    eye, claw = kit.mat("SouredEye_Glow", "#c8f07a"), kit.mat("Claw", "#d6cdb8")
    sack, twine = kit.mat("GleanerSack", "#8a7552"), kit.mat("Twine", "#a38b62")
    pole, iron = kit.mat("GleanerPole", "#5b4430"), kit.mat("GleanerIron", "#2f2d33")
    flame = kit.mat("GleanerLantern_Glow", "#ffb05a")
    face = kit.mat("SouredOchreDark", "#5e4a2c")

    def skirt_shape(bm):
        kit.cone(9, 0.36, 0.2922, 0.445)(bm)
        rim = sorted((v for v in bm.verts if v.co.z < -0.15), key=lambda v: math.atan2(v.co.y, v.co.x))
        for i, v in enumerate(rim):
            v.co.z += 0.07 if i % 2 else 0.0  # ragged hem

    parts = [
        # The smock in two: the skirt hangs from the waist (it swings after the body), the body of it moves with the
        # spine; a twine belt hides where they meet.
        part("Skirt", skirt_shape, smock, SKIRT_M, smooth=True, group="Hem"),
        part("Smock", kit.cone(9, 0.2998, 0.2, 0.655), smock, BODY_M, smooth=True, group="Spine"),
        part("Belt", kit.cylinder(12, 0.31, 0.05), twine, SMOCK @ M((0, 0, -0.1)), smooth=True, group="Spine"),
        part("Shoulders", kit.sphere(9, 6), smock, M((0, -0.06, 1.28), scale=(0.3, 0.24, 0.16)), smooth=True, group="Chest"),
        part("Head", kit.sphere(10, 7, 0.17), face, M((0, -0.2, 1.5), scale=(1, 1.05, 0.95)), smooth=True, group="Head"),
        part("Hood", kit.cone(8, 0.23, 0.05, 0.42), dark, M((0, -0.13, 1.6), rot=(-38, 0, 0)), smooth=True, group="Head"),
        part("Cape", kit.cone(9, 0.32, 0.2, 0.22), dark, M((0, -0.05, 1.33)), smooth=True, group="Chest"),
    ]
    for s in (-1, 1):
        parts.append(part("Eye", kit.sphere(8, 6, 0.033), eye, M((s * 0.065, -0.36, 1.5)), smooth=True, group="Head"))
    # Arms in two (a sleeve to the elbow, a forearm to the claws), the elbow a knob of smock; both hands on the pole.
    for arm, fore, shoulder, elbow, hand in (("ArmL", "ForeL", SHOULDER_L, ELBOW_L, HAND_L), ("ArmR", "ForeR", SHOULDER_R, ELBOW_R, HAND_R)):
        parts.append(part("Sleeve", kit.cone(6, 0.085, 0.065, math.dist(shoulder, elbow) + 0.04), smock, kit.along(shoulder, elbow), smooth=True, group=arm))
        parts.append(part("Elbow", kit.sphere(7, 5, 0.066), smock, M(elbow), smooth=True, group=arm))
        parts.append(part("Forearm", kit.cone(6, 0.065, 0.05, math.dist(elbow, hand)), smock, kit.along(elbow, hand), smooth=True, group=fore))
        tip = Vector(hand) + (Vector(hand) - Vector(elbow)).normalized() * 0.09
        parts.append(part("Claw", kit.cone(5, 0.03, 0.0, 0.09), claw, kit.along(hand, tuple(tip)), group=fore))
    # The gleaning sack on its left shoulder, hanging down its back, tied at the neck.
    parts += [
        part("Sack", kit.sphere(9, 7), sack, M((-0.16, 0.2, 1.0), scale=(0.2, 0.17, 0.3)), smooth=True, jitter=0.01, group="Sack"),
        part("SackTie", kit.cylinder(7, 0.06, 0.03), twine, M((-0.18, 0.12, 1.3)), group="Sack"),
        part("Strap", kit.cube(), twine, M((-0.05, -0.02, 1.12), rot=(0, 40, 0), scale=(0.03, 0.42, 0.03)), group="Chest"),
    ]
    # The pole: from below its hands to well over its head, a crook at the tip, the lantern hung from it.
    parts.append(part("Pole", kit.cylinder(6, 0.024, math.dist(BUTT, TIP)), pole, kit.along(BUTT, TIP), group="PoleShaft"))
    crook = [TIP, (0.28, -0.6, 2.56), HOOK]
    parts.append(part("Crook", kit.tube(crook, [0.02, 0.018, 0.015], seg=5), iron, group="PoleShaft"))
    lx, ly, lz = 0.28, -0.68, 2.3
    parts += [
        part("LanternCord", kit.cylinder(4, 0.006, 0.16), iron, M((lx, ly, 2.43)), group="Pole"),
        part("LanternTop", kit.cone(4, 0.075, 0.02, 0.06), iron, M((lx, ly, lz + 0.11), rot=(0, 0, 45)), group="Pole"),
        part("LanternGlass", kit.cube(), flame, M((lx, ly, lz), scale=(0.1, 0.1, 0.15)), group="Pole"),
        part("LanternBase", kit.cube(), iron, M((lx, ly, lz - 0.09), scale=(0.12, 0.12, 0.02)), group="Pole"),
    ]
    # A lantern hung from its belt, like every Soured's: green light in the dark = a Soured.
    lantern_glass = kit.mat("SouredLantern_Glow", "#b6f06a")
    parts += [
        part("BeltLanternTop", kit.cone(4, 0.06, 0.01, 0.05), iron, M((0.3, 0.02, 0.66), rot=(0, 0, 45)), group="BeltLantern"),
        part("BeltLanternGlass", kit.cube(), lantern_glass, M((0.3, 0.02, 0.59), scale=(0.08, 0.08, 0.1)), group="BeltLantern"),
        part("BeltLanternBase", kit.cube(), iron, M((0.3, 0.02, 0.535), scale=(0.1, 0.1, 0.015)), group="BeltLantern"),
    ]
    mesh = kit.join(parts, "Gleaner")
    armature = kit.rig("GleanerRig", BONES)
    kit.skin(mesh, armature)
    kit.animate(armature, ACTIONS)
    for (name, _), (worst, frame) in zip(ACTIONS.items(), REACH.values()):
        if worst > 0.02:
            print(f"[gleaner] {name}: the hands come off the pole by up to {worst * 100:.0f} cm (frame {frame})")
    return [armature, mesh]
