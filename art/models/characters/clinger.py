"""The Clinger: a Soured that couldn't let go of a person. Thin, grey and hunched, its face lost in its hood, with arms
far too long, knuckles on the ground. They come in pairs. It skitters in quick bursts, crouches (the tell), leaps, and
if it catches you it clings to your back, arms wrapped round your shoulders. Pale grey-blue, where the Laden is violet
and the archer ochre; the same sickly green eyes and belt lantern as every Soured.

Clips: Idle, Skitter (the run), Crouch (0.35 s telegraph: coiled low, arms back), Leap (0.35 s: stretched out, arms
reaching), Land (0.7 s: sprawled where it came down, then up: the punish window), Cling (on your back, arms round you),
Thrown (0.45 s: flung off, tumbling), Stagger, Death.

How it's animated (the Laden's way, in a quick, wiry, twitchy key): each bone keyed on its own timing (kit.bake), the
body leading (pivot, spine, chest), the head and the hood's point after it, the skirt of its robe, its lantern and its
claws trailing. Its moves are snaps: a few frames to get there, past it, and a tiny settle. The long arms aren't keyed
as rotations: each hand is keyed where it is (planted on the ground, reaching out in a leap, gripping your shoulders)
and the arm is bent onto it every frame (kit.reach), elbows jutting up and out like a spider's. The claws, the skirt
and the lantern are keyed by how they hang in the world (they dangle, whatever the body does), the skirt kept on the
floor when the body sinks (it bunches, it doesn't sink).

The game: Crouch is the wind-up (eased in over 0.15 s) and, stretched to 3.2 s, its weeping where its other half fell;
Leap is the strike (it catches hold at 0.8 of it, f7.4) and the hop down off the Unparted's back; Land the recovery
(then Skitter or Idle) and, cut to 0.25 s, the landing of that hop; Cling is played once over an hour (it holds its
first frame on your back) and posed at its last on the character screen: both are its clinging pose; Thrown flings it
off your back into Stagger.
"""
import math

from mathutils import Euler, Matrix, Vector

M, part = kit.M, kit.part  # `kit` is injected by build_model.py

SHOULDER_Z = 0.78
SHOULDER = {-1: (-0.17, -0.22, SHOULDER_Z), 1: (0.17, -0.22, SHOULDER_Z)}
ELBOW = {-1: (-0.22, -0.4, 0.46), 1: (0.22, -0.4, 0.46)}
HAND = {-1: (-0.21, -0.5, 0.12), 1: (0.21, -0.5, 0.12)}
CLAW_DIR = Vector((0, -0.1, -0.08)).normalized()
HOOD_AT, HOOD_TILT, HOOD_LEN = Vector((0, -0.27, 0.95)), -48, 0.34
HOOD_AXIS = Vector((0, math.sin(math.radians(48)), math.cos(math.radians(48))))
LANTERN_HOOK = (0.2, 0.02, 0.555)

BONES = {
    "Root": ((0, 0, 0), (0, 0, 0.25), None),
    "Pivot": ((0, 0, 0.35), (0, 0, 0.45), "Root"),  # the hips: whole-body leans and tumbles turn about here
    "Spine": ((0, 0, 0.3), (0, -0.08, 0.52), "Pivot"),  # the belly (its lantern hangs here: the game lights it here)
    "Chest": ((0, -0.08, 0.52), (0, -0.22, SHOULDER_Z), "Spine"),  # the hunch: shoulders, the hump
    "Head": ((0, -0.22, SHOULDER_Z), (0, -0.36, 0.95), "Chest"),
    "HoodTip": (tuple(HOOD_AT), tuple(HOOD_AT + HOOD_AXIS * 0.17), "Head"),  # the hood's point: it flicks after the head
    "ArmL": (SHOULDER[-1], ELBOW[-1], "Chest"),
    "ForeL": (ELBOW[-1], HAND[-1], "ArmL"),
    "HandL": (HAND[-1], tuple(Vector(HAND[-1]) + CLAW_DIR * 0.12), "ForeL"),  # the claws
    "ArmR": (SHOULDER[1], ELBOW[1], "Chest"),
    "ForeR": (ELBOW[1], HAND[1], "ArmR"),
    "HandR": (HAND[1], tuple(Vector(HAND[1]) + CLAW_DIR * 0.12), "ForeR"),
    "Hem": ((0, 0.0, 0.34), (0, 0.04, 0.04), "Pivot"),  # the robe's skirt (skinned smoothly into the body): it dangles
    "Lantern": (LANTERN_HOOK, (0.21, 0.02, 0.3), "Spine"),  # its belt lantern on its cord (a pendulum)
}

# The robe's bottom edge at rest (the skirt is lifted when it would sink through the floor).
_ROBE = M((0, -0.04, 0.33), rot=(16, 0, 0))
HEM_RIM = [_ROBE @ Vector((0.26 * math.cos(a), 0.26 * math.sin(a), -0.31)) for a in (2 * math.pi * k / 8 for k in range(8))]

# Tracks that aren't bones. GripL/GripR: where each hand is (an offset, metres; the arm is bent onto it), PoleL/PoleR:
# which way its elbow bows (a direction). "<bone>@": a bone keyed by how it hangs (its rotation in the world, or in the
# clip's space), whatever its parent does: the claws, the skirt, the lantern.
ABSOLUTE = ("HandL", "HandR", "Hem", "Lantern")


def g(x, y, z):
    """A position (a grip, a pole) as a track value."""
    return ((0, 0, 0), (x, y, z))


def _rot(degrees):
    return Euler([math.radians(a) for a in degrees]).to_matrix()


def _deg(matrix):
    return tuple(math.degrees(a) for a in matrix.to_euler("XYZ"))


def finish(raw, space=None, floor=True):
    """One frame's tracks made a pose: the arms bent onto their grips, the claws, skirt and lantern turned to hang as
    keyed, the skirt lifted off the floor. `space`: grips, poles and hangs are keyed in that bone's frame (a tumble),
    else in the world (the armature)."""
    pose = {k: v for k, v in raw.items() if k in BONES}
    m = kit.pose_matrices(BONES, pose)
    frame = m[space] if space else Matrix.Identity(4)
    turn = frame.to_3x3()
    targets = {}
    for side, s in (("L", -1), ("R", 1)):
        if "Grip" + side not in raw:
            continue
        target = frame @ Vector(raw["Grip" + side][1])
        pole = turn @ Vector(raw["Pole" + side][1] if "Pole" + side in raw else (s, 0.5, 0.8))
        pose = kit.reach(BONES, pose, "Arm" + side, "Fore" + side, lambda _m, t=target: t, pole)
        targets[side] = target
    m = kit.pose_matrices(BONES, pose)
    for bone in ABSOLUTE:
        if bone + "@" in raw:
            rot, offset = raw[bone + "@"]
            parent = m[BONES[bone][2]].to_3x3()
            pose[bone] = (_deg(parent.transposed() @ turn @ _rot(rot)), tuple(offset))
    if floor:
        m = kit.pose_matrices(BONES, pose)
        low = min((m["Hem"] @ p).z for p in HEM_RIM)
        if low < 0.004:
            parent = m[BONES["Hem"][2]].to_3x3()
            rot, offset = pose.get("Hem", Z)
            lift = parent.transposed() @ Vector((0, 0, 0.004 - low))
            pose["Hem"] = (tuple(rot), tuple(Vector(offset) + lift))
    return pose, targets


REACH = {}  # (the build's report: how far each clip's hands come off what they hold, at worst)


def clip(length, tracks, periodic=False, space=None, floor=True):
    """A clip `length` frames long (30 a second) from per-bone tracks, baked to every frame, the arms bent onto the grips."""
    frames, worst = [], 0.0
    for f, raw in kit.bake(tracks, length, periodic):
        pose, targets = finish(raw, space, floor)
        m = kit.pose_matrices(BONES, pose)
        for side, s in (("L", -1), ("R", 1)):
            if side in targets:
                worst = max(worst, (m["Fore" + side] @ Vector(HAND[s]) - targets[side]).length)
        frames.append((f, pose))
    REACH[len(REACH)] = worst
    return ("LINEAR", frames)


def held(pose, frame):
    """A whole pose as a key at `frame` for every track (to start or end a clip on it)."""
    return {track: [(frame, value)] for track, value in pose.items()}


def keys(*parts):
    """Tracks merged: each a {track: [(frame, value)]}."""
    merged = {}
    for p in parts:
        for track, ks in p.items():
            merged.setdefault(track, []).extend(ks)
    return merged


def in_space(pose, space):
    """A world-keyed pose re-keyed in `space` (a bone's frame), so a tumbling clip can start or end on it."""
    m = kit.pose_matrices(BONES, {k: v for k, v in pose.items() if k in BONES})
    inv = m[space].inverted()
    turn = inv.to_3x3()
    out = dict(pose)
    for side in ("L", "R"):
        if "Grip" + side in pose:
            out["Grip" + side] = g(*(inv @ Vector(pose["Grip" + side][1])))
        if "Pole" + side in pose:
            out["Pole" + side] = g(*(turn @ Vector(pose["Pole" + side][1])))
    for bone in ABSOLUTE:
        if bone + "@" in pose:
            rot, offset = pose[bone + "@"]
            out[bone + "@"] = (_deg(turn @ _rot(rot)), offset)
    return out


def at(rot, offset=(0, 0, 0)):
    """A hanging bone's value (a rotation, an offset)."""
    return (tuple(rot), tuple(offset))


Z = ((0, 0, 0), (0, 0, 0))

# The stances everything starts from or comes back to.
# NEUTRAL: hunched on its knuckles, the body low and forward, the head craned up to watch you, the elbows jutting.
NEUTRAL = {
    "Root": Z, "Pivot": (8, 0, 0), "Spine": (4, 0, 0), "Chest": (10, 0, 0), "Head": (-24, 0, 0), "HoodTip": (0, 0, 0),
    "GripL": g(-0.25, -0.5, 0.068), "GripR": g(0.25, -0.5, 0.068), "PoleL": g(-1, 0.5, 0.8), "PoleR": g(1, 0.5, 0.8),
    "HandL@": at((-8, 0, -8)), "HandR@": at((-8, 0, 8)), "Hem@": at((0, 0, 0)), "Lantern@": at((0, 0, 0)),
}
# COIL (the tell, the leap's start): sunk low and back on its haunches, the hands drawn in under it and wide, the
# elbows high, the head low and craned up at you.
COIL = {
    "Root": ((0, 0, 0), (0, 0.06, -0.11)), "Pivot": (20, 0, 0), "Spine": (6, 0, 0), "Chest": (12, 0, 0),
    "Head": (-52, 0, 0), "HoodTip": (-8, 0, 0),
    "GripL": g(-0.34, -0.42, 0.068), "GripR": g(0.34, -0.42, 0.068), "PoleL": g(-0.8, 0.5, 1.2), "PoleR": g(0.8, 0.5, 1.2),
    "HandL@": at((-4, 0, -24)), "HandR@": at((-4, 0, 24)), "Hem@": at((-12, 0, 0)), "Lantern@": at((-6, 0, 0)),
}
# LEAP_END (the leap's last frame, the landing's first): still in the air, arms closed round what it caught.
LEAP_END = {
    "Root": ((0, 0, 0), (0, -0.08, 0.2)), "Pivot": (46, 0, 0), "Spine": (4, 0, 0), "Chest": (14, 0, 0),
    "Head": (-30, 0, 0), "HoodTip": (14, 0, 0),
    "GripL": g(-0.1, -0.88, 0.4), "GripR": g(0.1, -0.88, 0.4), "PoleL": g(-1, 0, 0.6), "PoleR": g(1, 0, 0.6),
    "HandL@": at((60, 0, 50)), "HandR@": at((60, 0, -50)), "Hem@": at((64, 0, 0)), "Lantern@": at((60, 0, 0)),
}
# CLING (on your back; the game sets it behind you and 0.25 m up): pressed to your back, its arms over your shoulders
# and round your chest, claws hooked in, its head craned over your right shoulder to whisper; its skirt tucked up behind.
CLING = {
    "Root": ((0, 0, 0), (-0.04, 0.14, -0.06)), "Pivot": (-4, 20, 0), "Spine": (4, 0, 0), "Chest": (6, 14, 8),
    "Head": (-6, -16, -6), "HoodTip": (6, 0, 0),
    "GripL": g(-0.14, -0.65, 0.22), "GripR": g(0.15, -0.64, 0.19), "PoleL": g(-1, -0.1, 0.2), "PoleR": g(1, -0.1, 0.2),
    "HandL@": at((40, 0, 70)), "HandR@": at((40, 0, -70)), "Hem@": at((38, 0, 0)), "Lantern@": at((12, 0, 0)),
}
# LANDED (thrown, coming down): caught on its hands, low, splayed (the stagger takes it from here).
LANDED = {
    "Root": ((0, 0, 0), (0, 0, -0.05)), "Pivot": (16, 0, 0), "Spine": (4, 0, 0), "Chest": (12, 0, 0), "Head": (-30, 0, 0),
    "HoodTip": (6, 0, 0), "GripL": g(-0.32, -0.52, 0.068), "GripR": g(0.3, -0.5, 0.068),
    "HandL@": at((-8, 0, -14)), "HandR@": at((-8, 0, 14)), "Hem@": at((-6, 0, 0)), "Lantern@": at((-12, 0, 0)),
}

ACTIONS = {
    # Idle (47 frames, a loop): crouched on its knuckles, panting fast and shallow (three breaths: the chest heaves,
    # the hood's point nodding after), never still: the head snaps round to its right (past, and a settle), cocks to
    # listen, snaps back to its left; the right hand lifts and taps its claws twice; the weight shifts under it, the
    # skirt and lantern sway after.
    "Idle": clip(47, keys(
        {"Root": [(1, Z), (9, ((0, 0, 0), (0.008, 0, 0.012))), (17, ((0, 0, 0), (0.012, 0, -0.004))),
                  (24, ((0, 0, 0), (0.004, 0, 0.012))), (32, ((0, 0, 0), (-0.01, 0, -0.004))), (40, ((0, 0, 0), (-0.008, 0, 0.012)))],
         "Pivot": [(1, (8, 0, 0)), (14, (8, 2, 0)), (30, (8, -2, 0))],
         "Spine": [(1, (4, 0, 0)), (9, (2, 0, 0)), (17, (5, 0, 0)), (24, (2, 0, 0)), (32, (5, 0, 0)), (40, (2, 0, 0))],
         "Chest": [(2, (10, 0, 0)), (10, (6, 0, 0)), (18, (11, 0, 1)), (25, (6, 0, 1)), (33, (11, 0, 0)), (41, (6, 0, -1))],
         # the snaps: two frames to get there and past, two to settle back, then held (a few small drifts)
         "Head": [(1, (-24, 0, -6)), (8, (-23, 0, -8)), (10, (-30, 0, 30)), (12, (-26, 0, 22)), (14, (-27, 0, 24)),
                  (20, (-25, 0, 22)), (22, (-18, 16, 14)), (24, (-21, 12, 16)), (29, (-20, 13, 15)),
                  (31, (-30, -4, -26)), (33, (-24, 0, -18)), (35, (-25, 0, -20)), (42, (-23, 0, -14)), (45, (-24, 0, -7))],
         "HoodTip": [(3, (2, 0, 4)), (10, (4, 0, 6)), (12, (-8, 0, -14)), (15, (6, 0, 6)), (18, (0, 0, 0)), (24, (-4, -8, -6)),
                     (27, (4, 4, 2)), (33, (-6, 0, 14)), (36, (5, 0, -6)), (40, (0, 0, 0))],
         "GripL": [(1, NEUTRAL["GripL"])],
         "GripR": [(1, NEUTRAL["GripR"]), (25, NEUTRAL["GripR"]), (27, g(0.26, -0.53, 0.15)), (28, g(0.25, -0.52, 0.075)),
                   (29, g(0.25, -0.515, 0.068)), (30, g(0.255, -0.52, 0.13)), (31, g(0.25, -0.51, 0.068)),
                   (39, g(0.25, -0.51, 0.068)), (41, g(0.25, -0.505, 0.11)), (43, NEUTRAL["GripR"])],
         "HandL@": [(1, at((-8, 0, -8))), (20, at((-8, 0, -8))), (22, at((4, 0, -14))), (24, at((-8, 0, -8)))],
         "HandR@": [(1, at((-8, 0, 8))), (25, at((-8, 0, 8))), (27, at((-30, 0, 14))), (28, at((-2, 0, 8))), (29, at((-10, 0, 8))),
                    (30, at((-24, 0, 10))), (31, at((-2, 0, 8))), (33, at((-6, 0, 8))), (41, at((-14, 0, 10))), (43, at((-8, 0, 8)))],
         "Hem@": [(4, at((1, -1, 0))), (18, at((-1, 1, 0))), (34, at((1, 2, 0)))],
         "Lantern@": [(6, at((2, -3, 0))), (20, at((-2, 3, 0))), (36, at((3, 2, 0)))],
         }), periodic=True),
    # Skitter (12 frames, a loop): a bounding scramble on its knuckles. Both hands come down far ahead (the left, then
    # the right two frames after), the body drops onto them and is hauled over them as they rake back, then it springs
    # off (the skirt kicking back like hind legs) and the arms swing through, high-elbowed, to reach ahead again. The
    # head rides level, a frame behind; the hood's point and the lantern trail.
    "Skitter": clip(12, {
        "Root": [(1, ((0, 0, 0), (0, 0, 0.0))), (3, ((0, 0, 0), (0, 0, -0.035))), (5, ((0, 0, 0), (0, 0, -0.02))),
                 (8, ((0, 0, 0), (0, 0, 0.05))), (11, ((0, 0, 0), (0, 0, 0.03)))],
        "Pivot": [(1, (22, 0, 0)), (3, (28, 2, 0)), (6, (24, 0, 0)), (8, (16, -2, 0)), (11, (18, 0, 0))],
        "Spine": [(2, (4, 0, 0)), (4, (8, 0, 0)), (8, (0, 0, 0)), (11, (2, 0, 0))],
        "Chest": [(2, (8, 0, -2)), (4, (14, 0, 2)), (7, (10, 0, 3)), (9, (4, 0, -2)), (12, (6, 0, -3))],
        "Head": [(3, (-50, 0, 2)), (5, (-58, 0, 0)), (8, (-46, 0, -2)), (10, (-42, 0, 0))],
        "HoodTip": [(4, (-6, 0, 0)), (7, (10, 0, 0)), (10, (-14, 0, 0))],
        # each hand: planted ahead, raked back under the body (8 cm a frame), lifted and swung through high
        "GripL": [(1, g(-0.26, -0.74, 0.068)), (6, g(-0.27, -0.34, 0.068)), (8, g(-0.3, -0.42, 0.2)), (10, g(-0.28, -0.7, 0.22)),
                  (12, g(-0.26, -0.78, 0.1))],
        "GripR": [(3, g(0.26, -0.72, 0.068)), (8, g(0.27, -0.32, 0.068)), (10, g(0.3, -0.42, 0.2)), (12, g(0.28, -0.68, 0.22)),
                  (2, g(0.26, -0.76, 0.1))],
        "PoleL": [(1, g(-0.8, 0.6, 1))], "PoleR": [(1, g(0.8, 0.6, 1))],
        "HandL@": [(1, at((-6, 0, -10))), (6, at((-4, 0, -10))), (8, at((40, 0, -6))), (11, at((-40, 0, -14)))],
        "HandR@": [(3, at((-6, 0, 10))), (8, at((-4, 0, 10))), (10, at((40, 0, 6))), (1, at((-40, 0, 14)))],
        "Hem@": [(2, at((-14, 0, 0))), (5, at((-2, 0, 0))), (9, at((26, 0, 0)))],
        "Lantern@": [(3, at((-10, 0, 0))), (7, at((10, 0, 0))), (11, at((22, 0, 0)))],
    }, periodic=True),
    # Crouch (8 frames, the tell: 0.35 s with its hiss): the head jerks up (the hiss), then it drops low and back onto
    # its haunches all at once, hands drawn in wide, elbows up; the head craned up at you, the hood's point and the
    # lantern settling after; held there, quivering.
    "Crouch": clip(8, keys(held(NEUTRAL, 1), held(COIL, 9), {
        "Root": [(3, ((0, 0, 0), (0, 0.0, 0.02))), (6, ((0, 0, 0), (0, 0.07, -0.125))), (7, ((0, 0, 0), (0, 0.06, -0.105)))],
        "Pivot": [(3, (2, 0, 0)), (6, (22, 0, 0)), (7, (19, 0, 0)), (8, (20.5, 0, 0))],
        "Spine": [(3, (0, 0, 0)), (6, (7, 0, 0))],
        "Chest": [(3, (2, 0, 0)), (6, (14, 0, 0)), (7, (11, 0, 1)), (8, (12.5, 0, -1))],
        "Head": [(3, (-46, 0, 0)), (5, (-36, 0, 0)), (7, (-56, 0, 2)), (8, (-50, 0, -2))],
        "HoodTip": [(4, (16, 0, 0)), (6, (-14, 0, 0)), (8, (-4, 0, 0))],
        "GripL": [(3, g(-0.27, -0.5, 0.12)), (5, g(-0.34, -0.42, 0.068))],
        "GripR": [(3, g(0.27, -0.5, 0.12)), (5, g(0.34, -0.42, 0.068))],
        "PoleL": [(3, g(-1, 0.5, 0.8))], "PoleR": [(3, g(1, 0.5, 0.8))],
        "HandL@": [(3, at((-24, 0, -14))), (6, at((2, 0, -26)))],
        "HandR@": [(3, at((-24, 0, 14))), (6, at((2, 0, 26)))],
        "Hem@": [(4, at((6, 0, 0))), (7, at((-16, 0, 0)))],
        "Lantern@": [(4, at((8, 0, 0))), (7, at((-12, 0, 0)))],
    })),
    # Leap (8 frames, 0.35 s; it catches hold at f7.4, and the trail's on the claws): from the coil it uncurls all at
    # once, the arms sweeping up from the ground and out ahead, claws spread; stretched out flat in the air, the skirt
    # and lantern streaming behind; at the catch the arms snap shut round you, claws hooking in, the body curling in
    # after them, the head last.
    "Leap": clip(8, keys(held(COIL, 1), held(LEAP_END, 9), {
        "Root": [(2, ((0, 0, 0), (0, -0.02, 0.06))), (4, ((0, 0, 0), (0, -0.1, 0.3))), (6, ((0, 0, 0), (0, -0.11, 0.32))),
                 (7, ((0, 0, 0), (0, -0.1, 0.28)))],
        "Pivot": [(2, (46, 0, 0)), (4, (64, 0, 0)), (6, (62, 0, 0)), (8, (50, 0, 0))],
        "Spine": [(2, (0, 0, 0)), (4, (-4, 0, 0)), (7, (2, 0, 0))],
        "Chest": [(2, (-4, 0, 0)), (4, (-12, 0, 0)), (6, (-10, 0, 0)), (8, (18, 0, 0))],
        "Head": [(2, (-50, 0, 0)), (4, (-56, 0, 0)), (6, (-54, 0, 0)), (7, (-46, 0, 0)), (8, (-24, 0, 0))],
        "HoodTip": [(3, (24, 0, 0)), (5, (30, 0, 0)), (8, (6, 0, 0))],
        "GripL": [(2, g(-0.32, -0.66, 0.22)), (4, g(-0.34, -1.14, 0.6)), (6, g(-0.33, -1.18, 0.62)), (7, g(-0.17, -1.06, 0.52)),
                  (8, g(-0.08, -0.94, 0.44))],
        "GripR": [(2, g(0.32, -0.66, 0.22)), (4, g(0.34, -1.14, 0.6)), (6, g(0.33, -1.18, 0.62)), (7, g(0.17, -1.06, 0.52)),
                  (8, g(0.08, -0.94, 0.44))],
        "PoleL": [(2, g(-1, 0.4, 0.6)), (6, g(-1, 0.2, 0.6))], "PoleR": [(2, g(1, 0.4, 0.6)), (6, g(1, 0.2, 0.6))],
        "HandL@": [(2, at((-40, 0, -20))), (4, at((-50, 0, -30))), (6, at((-46, 0, -28))), (7, at((20, 0, 30))), (8, at((66, 0, 56)))],
        "HandR@": [(2, at((-40, 0, 20))), (4, at((-50, 0, 30))), (6, at((-46, 0, 28))), (7, at((20, 0, -30))), (8, at((66, 0, -56)))],
        "Hem@": [(2, at((-4, 0, 0))), (4, at((70, 0, 0))), (6, at((100, 0, 0))), (8, at((80, 0, 0)))],
        "Lantern@": [(3, at((6, 0, 0))), (5, at((70, 0, 0))), (7, at((84, 0, 0)))],
    })),
    # Land (16 frames; the recovery, 0.7 s: the punish window): it comes down where it missed, hands slapping out wide,
    # the body crashing onto them (a bounce), sprawled flat; it shakes its head (quick snaps, the hood's point
    # flicking); then it gathers its hands in, pushes itself up past its stance and settles into it (Skitter or Idle
    # follow).
    "Land": clip(16, keys(held(LEAP_END, 1), held(NEUTRAL, 17), {
        "Root": [(2, ((0, 0, 0), (0, -0.1, 0.05))), (3, ((0, 0, 0), (0, -0.12, -0.05))), (5, ((0, 0, 0), (0, -0.12, -0.02))),
                 (7, ((0, 0, 0), (0, -0.12, -0.045))), (11, ((0, 0, 0), (0, -0.11, -0.04))), (13, ((0, 0, 0), (0, -0.05, -0.02))),
                 (15, ((0, 0, 0), (0, 0, 0.02)))],
        "Pivot": [(2, (52, 0, 0)), (3, (56, 0, 0)), (5, (48, 0, 0)), (7, (53, 0, 0)), (11, (52, 0, 0)), (13, (32, 0, 0)),
                  (15, (4, 0, 0))],
        "Spine": [(3, (8, 0, 0)), (6, (6, 0, 0)), (12, (6, 0, 0)), (14, (0, 0, 0))],
        "Chest": [(2, (14, 0, 0)), (4, (18, 0, 0)), (6, (12, 0, 0)), (8, (15, 0, 0)), (12, (14, 0, 0)), (14, (2, 0, 0))],
        "Head": [(2, (-30, 0, 0)), (4, (-22, 0, 0)), (6, (-50, 0, 0)), (7, (-44, 0, 0)), (8, (-46, 0, 22)), (9, (-46, 0, -22)),
                 (10, (-48, 0, 16)), (11, (-48, 0, -6)), (13, (-54, 0, 0)), (15, (-20, 0, 0))],
        "HoodTip": [(3, (6, 0, 0)), (5, (24, 0, 0)), (7, (-8, 0, 0)), (9, (0, 0, 22)), (10, (0, 0, -24)), (11, (0, 0, 16)),
                    (12, (0, 0, -8)), (14, (16, 0, 0)), (16, (-6, 0, 0))],
        "GripL": [(2, g(-0.36, -0.86, 0.12)), (3, g(-0.44, -0.74, 0.068)), (11, g(-0.44, -0.74, 0.068)), (13, g(-0.36, -0.6, 0.14)),
                  (15, g(-0.26, -0.5, 0.068))],
        "GripR": [(2, g(0.36, -0.86, 0.12)), (3, g(0.44, -0.74, 0.068)), (11, g(0.44, -0.74, 0.068)), (13, g(0.36, -0.6, 0.14)),
                  (15, g(0.26, -0.5, 0.068))],
        "PoleL": [(3, g(-1, 0.4, 0.8))], "PoleR": [(3, g(1, 0.4, 0.8))],
        "HandL@": [(2, at((-30, 0, -20))), (3, at((-6, 0, -30))), (5, at((-14, 0, -26))), (11, at((-8, 0, -24))), (13, at((-30, 0, -10)))],
        "HandR@": [(2, at((-30, 0, 20))), (3, at((-6, 0, 30))), (5, at((-14, 0, 26))), (11, at((-8, 0, 24))), (13, at((-30, 0, 10)))],
        "Hem@": [(3, at((86, 0, 0))), (5, at((56, 0, 0))), (8, at((74, 0, 0))), (12, at((68, 0, 0))), (14, at((-12, 0, 0)))],
        "Lantern@": [(3, at((50, 0, 0))), (5, at((-20, 0, 0))), (8, at((14, 0, 0))), (12, at((4, 0, 0))), (15, at((-10, 0, 0)))],
    })),
    # Cling (23 frames; the game holds its first frame while it clings and poses its last on the character screen: both
    # the clinging pose): on your back, hugging tight; it hitches itself up and squeezes (the claws digging in), leans
    # in to whisper (the head nuzzling in at your ear, a twitch), and the tucked-up skirt kicks.
    "Cling": clip(23, keys(held(CLING, 1), {
        "Root": [(6, ((0, 0, 0), (-0.04, 0.13, -0.04))), (12, ((0, 0, 0), (-0.04, 0.15, -0.07)))],
        "Pivot": [(6, (-7, 19, 0)), (12, (-2, 22, -2)), (18, (-4, 20, 1))],
        "Chest": [(7, (2, 14, 8)), (13, (8, 16, 10)), (19, (6, 14, 7))],
        "Head": [(8, (-6, -16, -6)), (10, (8, -26, -28)), (12, (4, -22, -20)), (15, (5, -23, -21)), (17, (-10, -12, 4)), (19, (-5, -16, -7))],
        "HoodTip": [(9, (0, 0, 0)), (11, (14, 4, -10)), (14, (2, 0, 4)), (18, (-6, 0, 0))],
        "GripL": [(6, g(-0.12, -0.62, 0.24)), (11, g(-0.13, -0.65, 0.23))],
        "GripR": [(6, g(0.13, -0.61, 0.21)), (11, g(0.14, -0.64, 0.2))],
        "HandL@": [(6, at((50, 0, 76))), (12, at((38, 0, 68)))],
        "HandR@": [(6, at((50, 0, -76))), (12, at((38, 0, -68)))],
        "Hem@": [(5, at((44, 0, 4))), (10, at((30, 0, -6))), (15, at((44, 0, 2))), (20, at((34, 0, 0)))],
        "Lantern@": [(6, at((18, 0, 4))), (13, at((6, 0, -4))), (19, at((16, 0, 0)))],
    }), periodic=True, floor=False),
    # Thrown (11 frames, 0.45 s; the game flings it 2.4 m away in an arc): torn off your back, its arms dragged out
    # ahead of it, then flung over backwards, arms and skirt flailing, on its back in the air; it twists over (a cat's
    # righting) and comes down on its hands, low and splayed (the stagger follows). Keyed in its own frame (Pivot).
    "Thrown": clip(11, keys(held(in_space(CLING, "Pivot"), 1), held(in_space(LANDED, "Pivot"), 12), {
        "Root": [(2, ((0, 0, 0), (0, -0.04, -0.02))), (5, ((0, 0, 0), (0, 0.08, 0.26))), (8, ((0, 0, 0), (0, 0.06, 0.28))),
                 (10, ((0, 0, 0), (0, 0.02, 0.08))), (11, ((0, 0, 0), (0, 0, -0.03)))],
        "Pivot": [(2, (26, 0, 0)), (4, (-50, 0, 6)), (6, (-120, 8, 14)), (8, (-110, 40, 10)), (10, (-20, 14, 0)), (11, (19, 0, 0))],
        "Spine": [(2, (8, 0, 0)), (5, (-14, 0, 0)), (8, (10, 0, 0)), (11, (6, 0, 0))],
        "Chest": [(2, (20, 0, 0)), (4, (-20, 0, 4)), (6, (-24, 0, -6)), (8, (14, 0, 0)), (10, (18, 0, 0))],
        "Head": [(2, (24, 0, 0)), (4, (-50, 0, 0)), (6, (-40, 0, 20)), (8, (-10, 0, -10)), (10, (-50, 0, 0)), (11, (-20, 0, 0))],
        "HoodTip": [(3, (30, 0, 0)), (5, (-30, 0, 10)), (7, (20, 0, -20)), (9, (-20, 0, 10)), (11, (14, 0, 0))],
        "GripL": [(2, g(-0.16, -0.9, 0.6)), (4, g(-0.42, -0.3, 1.0)), (6, g(-0.48, -0.05, 0.8)), (8, g(-0.45, -0.45, 0.25))],
        "GripR": [(2, g(0.16, -0.9, 0.56)), (4, g(0.46, -0.45, 0.85)), (6, g(0.4, -0.55, 0.92)), (8, g(0.46, -0.2, 0.5))],
        "PoleL": [(2, g(-1, 0, 0.4)), (6, g(-1, 0.4, 0)), (10, g(-0.8, 0.5, 1))],
        "PoleR": [(2, g(1, 0, 0.4)), (6, g(1, 0.4, 0)), (10, g(0.8, 0.5, 1))],
        "HandL@": [(2, at((40, 0, 30))), (4, at((-60, 0, -40))), (7, at((-20, 0, -60))), (10, at((-20, 0, -20)))],
        "HandR@": [(2, at((40, 0, -30))), (5, at((-70, 0, 30))), (8, at((-10, 0, 50))), (10, at((-20, 0, 20)))],
        "Hem@": [(3, at((60, 0, 0))), (5, at((-40, 0, 0))), (7, at((-70, 0, 20))), (9, at((10, 0, -10))), (11, at((-14, 0, 0)))],
        "Lantern@": [(3, at((50, 0, 0))), (6, at((-50, 0, 10))), (9, at((20, 0, 0))), (11, at((-20, 0, 0)))],
    }), space="Pivot"),
    # Stagger (23 frames; stretched to the stagger's length): a blow knocks it back on its haunches, the head snapping
    # back, the arms flung up and out; it scrabbles its hands back down (one, then the other), and shakes its head
    # like a wet dog (three snaps, the hood's point whipping after), then gathers itself into its stance.
    "Stagger": clip(23, keys(held(NEUTRAL, 1), held(NEUTRAL, 24), {
        "Root": [(3, ((0, 0, 0), (0, 0.12, 0.04))), (6, ((0, 0, 0), (0.02, 0.1, -0.07))), (8, ((0, 0, 0), (0, 0.08, -0.04))),
                 (16, ((0, 0, 0), (0, 0.06, -0.05))), (20, ((0, 0, 0), (0, 0.01, 0.01)))],
        "Pivot": [(3, (-20, 0, 4)), (6, (22, -4, 0)), (8, (14, 2, 0)), (16, (16, 0, 0)), (20, (6, 0, 0))],
        "Spine": [(3, (-6, 0, 0)), (6, (8, 0, 0)), (10, (5, 0, 0))],
        "Chest": [(3, (-14, 0, 6)), (5, (-4, 0, 0)), (7, (16, 0, -4)), (10, (12, 0, 0)), (17, (13, 0, 0)), (20, (8, 0, 0))],
        "Head": [(2, (-20, 0, 0)), (4, (-56, 0, -10)), (6, (-28, 0, 6)), (8, (-44, 0, 0)), (10, (-44, 0, 28)), (11, (-44, 0, -28)),
                 (12, (-46, 0, 24)), (13, (-46, 0, -16)), (14, (-46, 0, 6)), (16, (-44, 0, 0)), (19, (-24, 0, 0))],
        "HoodTip": [(3, (20, 0, 0)), (5, (-24, 0, 0)), (7, (16, 0, 0)), (11, (0, 0, 26)), (12, (0, 0, -26)), (13, (0, 0, 22)),
                    (14, (0, 0, -16)), (15, (0, 0, 8)), (18, (0, 0, 0)), (21, (-4, 0, 0))],
        "GripL": [(3, g(-0.5, -0.26, 0.66)), (5, g(-0.42, -0.46, 0.3)), (7, g(-0.3, -0.46, 0.068)), (19, g(-0.3, -0.46, 0.068)),
                  (21, g(-0.27, -0.49, 0.12))],
        "GripR": [(3, g(0.52, -0.22, 0.6)), (6, g(0.38, -0.5, 0.16)), (8, g(0.32, -0.5, 0.068)), (17, g(0.32, -0.5, 0.068)),
                  (19, g(0.27, -0.5, 0.12))],
        "PoleL": [(3, g(-1, 0.6, -0.2)), (7, g(-1, 0.5, 0.8))], "PoleR": [(3, g(1, 0.6, -0.2)), (8, g(1, 0.5, 0.8))],
        "HandL@": [(3, at((-60, 0, -40))), (6, at((-30, 0, -20))), (7, at((-4, 0, -14))), (19, at((-4, 0, -14))), (21, at((-24, 0, -8)))],
        "HandR@": [(3, at((-60, 0, 40))), (7, at((-30, 0, 20))), (8, at((-4, 0, 14))), (17, at((-4, 0, 14))), (19, at((-24, 0, 8)))],
        "Hem@": [(3, at((-30, 0, 0))), (6, at((20, 0, 0))), (9, at((-8, 0, 0))), (13, at((4, 0, 6))), (16, at((0, 0, -4)))],
        "Lantern@": [(4, at((-30, 0, 0))), (7, at((24, 0, 0))), (10, at((-10, 0, 0))), (14, at((6, -8, 0))), (18, at((0, 4, 0)))],
    })),
    # Death (19 frames; then it dissolves): a jolt: it rears up rigid, arms flung up and out, claws splayed, the head
    # thrown back; a shudder; then it pitches forward onto its face (a bounce), arms flopping out to the sides, the
    # skirt and lantern settling after; a last twitch of the claws.
    "Death": clip(19, keys(held(NEUTRAL, 1), {
        "Root": [(3, ((0, 0, 0), (0, 0.06, 0.05))), (6, ((0, 0, 0), (0, 0.07, 0.04))), (11, ((0, 0, 0), (0, -0.1, -0.06))),
                 (13, ((0, 0, 0), (0, -0.1, -0.025))), (15, ((0, 0, 0), (0, -0.1, -0.035))), (20, ((0, 0, 0), (0, -0.1, -0.035)))],
        "Pivot": [(3, (-22, 0, 0)), (5, (-18, 0, 3)), (6, (-21, 0, -2)), (11, (60, 0, 6)), (13, (54, 0, 5)), (15, (60, 0, 6)), (20, (60, 0, 6))],
        "Spine": [(3, (-8, 0, 0)), (11, (10, 0, 0)), (20, (10, 0, 0))],
        "Chest": [(3, (-16, 0, 0)), (5, (-12, 0, 2)), (6, (-15, 0, -2)), (11, (10, 0, 0)), (13, (4, 0, 0)), (16, (10, 0, 0)), (20, (10, 0, 0))],
        "Head": [(2, (-24, 0, 0)), (4, (-60, 0, 0)), (6, (-56, 0, 6)), (9, (-40, 0, 0)), (12, (-24, 24, -24)), (14, (-40, 16, -30)),
                 (16, (-26, 28, -34)), (20, (-28, 28, -34))],
        "HoodTip": [(4, (24, 0, 0)), (7, (-10, 0, 0)), (11, (-20, 0, 0)), (13, (24, 0, 6)), (16, (6, 0, -6)), (20, (10, 0, -4))],
        "GripL": [(3, g(-0.5, -0.2, 1.12)), (6, g(-0.48, -0.18, 1.08)), (9, g(-0.5, -0.6, 0.4)), (12, g(-0.52, -0.62, 0.08)),
                  (20, g(-0.52, -0.62, 0.08))],
        "GripR": [(3, g(0.5, -0.2, 1.12)), (6, g(0.5, -0.14, 1.06)), (10, g(0.5, -0.56, 0.34)), (13, g(0.5, -0.56, 0.08)),
                  (20, g(0.5, -0.56, 0.08))],
        "PoleL": [(3, g(-1, 0.4, -0.4)), (9, g(-1, 0.3, 0.6)), (14, g(-1, 0.2, -0.1))], "PoleR": [(3, g(1, 0.4, -0.4)), (10, g(1, 0.3, 0.6)), (15, g(1, 0.2, -0.1))],
        "HandL@": [(3, at((-70, 0, -40))), (5, at((-64, 0, -46))), (12, at((0, 0, -50))), (20, at((0, 0, -50)))],
        "HandR@": [(3, at((-70, 0, 40))), (5, at((-76, 0, 34))), (13, at((0, 0, 40))), (16, at((-40, 0, 46))), (17, at((-4, 0, 40))),
                   (19, at((-8, 0, 40))), (20, at((-6, 0, 40)))],
        "Hem@": [(4, at((-26, 0, 0))), (12, at((40, 0, 0))), (15, at((22, 0, 0))), (20, at((28, 0, 0)))],
        "Lantern@": [(5, at((-30, 0, 0))), (12, at((40, 0, 0))), (15, at((-10, 0, 0))), (18, at((8, 0, 0))), (20, at((4, 0, 0)))],
    })),
}


def weigh(ob, weights):
    """Skin a part smoothly: `weights(co)` gives each vertex's {bone: weight}."""
    groups = {}
    for v in ob.data.vertices:
        for bone, w in weights(v.co).items():
            if w > 0:
                if bone not in groups:
                    groups[bone] = ob.vertex_groups.new(name=bone)
                groups[bone].add([v.index], w, "REPLACE")


def smooth(a, b, x):
    t = min(1.0, max(0.0, (x - a) / (b - a)))
    return t * t * (3 - 2 * t)


def build():
    robe, dark = kit.mat("SouredGrey", "#7c8291"), kit.mat("SouredGreyDark", "#50545f")
    eye, claw = kit.mat("SouredEye_Glow", "#c8f07a"), kit.mat("Claw", "#d6cdb8")
    metal = kit.mat("HeapMetal", "#34313a")

    def robe_shape(bm):
        kit.cone(8, 0.26, 0.12, 0.62)(bm)
        rim = sorted((v for v in bm.verts if v.co.z < -0.29 and v.co.length > 0.05), key=lambda v: math.atan2(v.co.y, v.co.x))
        for i, v in enumerate(rim):
            v.co.z += 0.07 if i % 2 else 0.0  # tattered hem

    # The robe bends: its skirt hangs from the hips (the Hem), the belly is the Spine, its top the Chest.
    robe_part = part("Robe", robe_shape, robe, _ROBE, smooth=True)

    def robe_weights(co):
        hem = 1 - smooth(0.14, 0.34, co.z)
        chest = smooth(0.46, 0.6, co.z)
        return {"Hem": hem, "Spine": max(0.0, 1 - hem - chest), "Chest": chest}
    weigh(robe_part, robe_weights)

    # The hood: its point (up and back) flicks after the head.
    hood = part("Hood", kit.cone(8, 0.17, 0.03, HOOD_LEN), robe, M(tuple(HOOD_AT), rot=(HOOD_TILT, 0, 0)), smooth=True)
    base = HOOD_AT - HOOD_AXIS * (HOOD_LEN / 2)

    def hood_weights(co):
        tip = smooth(0.35, 0.9, (Vector(co) - base).dot(HOOD_AXIS) / HOOD_LEN)
        return {"Head": 1 - tip, "HoodTip": tip}
    weigh(hood, hood_weights)

    parts = [
        robe_part,
        part("Back", kit.sphere(9, 6), robe, M((0, -0.12, 0.68), rot=(30, 0, 0), scale=(0.21, 0.19, 0.2)), smooth=True, group="Chest"),
        part("Head", kit.sphere(9, 6, 0.13), dark, M((0, -0.33, 0.86)), smooth=True, group="Head"),
        hood,
    ]
    for s, side in ((-1, "L"), (1, "R")):
        parts.append(part("Eye", kit.sphere(8, 6, 0.025), eye, M((s * 0.05, -0.45, 0.85)), smooth=True, group="Head"))
        shoulder, elbow, hand = SHOULDER[s], ELBOW[s], HAND[s]
        # The arm in two (upper, fore), a knob at the elbow hiding the joint.
        parts.append(part("Arm", kit.tube([shoulder, elbow], [0.05, 0.037], seg=6), robe, group="Arm" + side))
        parts.append(part("Elbow", kit.sphere(7, 5, 0.04), robe, M(elbow), smooth=True, group="Arm" + side))
        parts.append(part("Forearm", kit.tube([elbow, hand], [0.036, 0.03], seg=6), robe, group="Fore" + side))
        for k in (-1, 0, 1):  # three long claws
            tip = (hand[0] + k * 0.035, hand[1] - 0.1, hand[2] - 0.08)
            parts.append(part("Claw", kit.cone(4, 0.016, 0.0, 0.12), claw, kit.along(hand, tip), group="Hand" + side))
    # A small lantern hung from its belt, like every Soured's (swinging on its cord).
    lantern_glass = kit.mat("SouredLantern_Glow", "#b6f06a")
    parts += [
        part("BeltLanternCord", kit.cube(), metal, M((0.2, 0.02, 0.5), rot=(0, -15, 0), scale=(0.012, 0.012, 0.1)), group="Lantern"),
        part("BeltLanternTop", kit.cone(4, 0.06, 0.01, 0.05), metal, M((0.21, 0.02, 0.43), rot=(0, 0, 45)), group="Lantern"),
        part("BeltLanternGlass", kit.cube(), lantern_glass, M((0.21, 0.02, 0.36), scale=(0.075, 0.075, 0.1)), group="Lantern"),
        part("BeltLanternBase", kit.cube(), metal, M((0.21, 0.02, 0.3), scale=(0.09, 0.09, 0.015)), group="Lantern"),
    ]
    mesh = kit.join(parts, "Clinger")
    armature = kit.rig("ClingerRig", BONES)
    kit.skin(mesh, armature)
    kit.animate(armature, ACTIONS)
    for (name, _), worst in zip(ACTIONS.items(), REACH.values()):
        if worst > 0.02:
            print(f"[clinger] {name}: the hands come off their grips by up to {worst * 100:.0f} cm")
    return [armature, mesh]
