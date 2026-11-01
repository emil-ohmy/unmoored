"""The Laden: a Soured who couldn't let go of things. Clutches a towering heap of belongings in front of it,
which is its shield (blocks frontal hits) and its weapon (lifts it overhead, slams it down).

Clips: Idle and Walk (open: heap low on the hip, face showing), GuardUp (0.35 s: heap lifted in front of the
face), GuardWalk (braced shuffle),
Block (0.3 s recoil when a frontal hit is blocked), Windup (0.9 s telegraph: heap raised high), Strike (0.3 s slam),
Recover (1.2 s, heap on the ground: the punish window), SweepWindup / Sweep / SweepRecover (the second attack),
Stagger (also used, slowed, when a perfect block stuns it), Death.

How it's animated (the template for the rest): each bone keyed on its own timing (kit.bake: tracks), so nothing moves
all at once. The body leads (hips, then spine, then chest); the head follows a few frames late; the heap is carried
by the chest and its top (the chair, the clock, the bundle) and the picture frame on it lag and overshoot like loose
junk; the skirt of its robe and the lantern at its hip swing after the body. Big moves are prepared (a dip before a
lift, a wind before a swing) and land with weight (an overshoot, a rebound, a settle). The arms aren't keyed: every
frame they're bent (kit.reach) so the hands keep their grip on the heap wherever the heap goes.
"""
import math

from mathutils import Euler, Vector

M, part = kit.M, kit.part  # `kit` is injected by build_model.py

SHOULDER_L, ELBOW_L, HAND_L = (-0.38, -0.1, 1.05), (-0.6, -0.18, 0.74), (-0.44, -0.48, 0.86)
SHOULDER_R, ELBOW_R, HAND_R = (0.38, -0.1, 1.05), (0.6, -0.18, 0.74), (0.44, -0.48, 0.86)
BONES = {
    "Root": ((0, 0, 0), (0, 0, 0.3), None),
    "Hips": ((0, 0, 0.3), (0, 0, 0.62), "Root"),
    "Hem": ((0, 0, 0.62), (0, 0.05, 0.05), "Hips"),            # the robe's skirt, hanging from the waist
    "Spine": ((0, 0, 0.62), (0, -0.06, 0.95), "Hips"),
    "Chest": ((0, -0.06, 0.95), (0, -0.12, 1.15), "Spine"),
    "Head": ((0, -0.12, 1.15), (0, -0.26, 1.45), "Chest"),
    "ArmL": (SHOULDER_L, ELBOW_L, "Chest"),
    "ForeL": (ELBOW_L, HAND_L, "ArmL"),
    "ArmR": (SHOULDER_R, ELBOW_R, "Chest"),
    "ForeR": (ELBOW_R, HAND_R, "ArmR"),
    "Heap": ((0, -0.55, 0.7), (0, -0.55, 0.92), "Root"),      # the heap's chest (keyed where it is: the hands follow it)
    "HeapTop": ((0, -0.56, 0.95), (0, -0.56, 1.4), "Heap"),    # what's piled on it: chair, clock, bundle
    "Frame": ((0.2, -0.62, 1.45), (0.22, -0.62, 1.8), "HeapTop"),  # the picture frame on top
    "Lantern": ((0.5, 0, 0.88), (0.52, 0, 0.54), "Hips"),      # its lantern, on a cord at its hip
}


# The heap's chest (its lowest part), corner by corner, at rest: where it sits is keyed by where its bottom is.
HEAP_HEAD = Vector((0, -0.55, 0.7))
HEAP_CORNERS = [Vector((x, y, z)) for x in (-0.37, 0.37) for y in (-0.82, -0.34) for z in (0.54, 0.945)]


def heap(rot, x, y, bottom):
    """The heap turned `rot`, moved (x, y), its lowest corner `bottom` above the floor."""
    turn = Euler([math.radians(a) for a in rot]).to_matrix()
    low = min((turn @ (c - HEAP_HEAD) + HEAP_HEAD).z for c in HEAP_CORNERS)
    return (tuple(rot), (x, y, round(bottom - low, 4)))


def grip(pose):
    """The hands on the heap, the elbows bowed out and down, whatever the heap's doing; the skirt left on the floor
    when the body crouches or rises (it crumples, it doesn't sink)."""
    pose = kit.reach(BONES, pose, "ArmL", "ForeL", lambda m: m["Heap"] @ Vector(HAND_L), (-1, 0.5, -0.8))
    pose = kit.reach(BONES, pose, "ArmR", "ForeR", lambda m: m["Heap"] @ Vector(HAND_R), (1, 0.5, -0.8))
    hips = pose.get("Hips", ((0, 0, 0), (0, 0, 0)))
    hem = pose.get("Hem", ((0, 0, 0), (0, 0, 0)))
    pose["Hem"] = (tuple(hem[0]), (0, 0, -hips[1][2]))
    return pose


REACH = {}  # (the build's report: how far each clip's hands come off the heap, at worst)


def clip(length, tracks, periodic=False, name=None):
    """A clip `length` frames long (30 a second) from per-bone tracks, baked to every frame, the hands kept on the heap."""
    frames = [(f, grip(p)) for f, p in kit.bake(tracks, length, periodic)]
    worst = 0.0
    for f, p in frames:
        m = kit.pose_matrices(BONES, p)
        for fore, hand in (("ForeL", HAND_L), ("ForeR", HAND_R)):
            worst = max(worst, (m[fore] @ Vector(hand) - m["Heap"] @ Vector(hand)).length)
    REACH[len(REACH)] = worst
    return ("LINEAR", frames)


# The two carries everything starts from or comes back to.
# OPEN  (unaware): heap low on the right hip, tipped away, face and eyes showing over it. Does not block.
# GUARD (alert):   crouched, heap lifted before the face, head tucked behind it, peering over. Blocks.
OPEN = {"Hips": (0, 0, 0), "Spine": (6, 0, -4), "Chest": (4, 0, -4), "Head": (-10, 0, 6),
        "Heap": heap((22, 0, -14), 0.14, 0.1, 0.16), "HeapTop": (2, 0, 0), "Frame": (0, 0, 0), "Hem": (0, 0, 0), "Lantern": (0, 0, 0)}
GUARD = {"Hips": ((0, 0, 0), (0, 0.02, -0.05)), "Spine": (-3, 0, 0), "Chest": (-5, 0, 0), "Head": (16, 0, 0),
         "Heap": heap((-8, 0, 0), 0, -0.06, 0.9), "HeapTop": (-3, 0, 0), "Frame": (-4, 0, 0), "Hem": (2, 0, 0), "Lantern": (0, 0, 0)}


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


LIFTED_HIPS = ((0, 0, 0), (0, 0.08, 0.05))
LIFTED_HEAP = heap((-22, 0, 0), 0, 0.14, 1.3)
SLAM_HIPS = ((0, 0, 0), (0, -0.3, -0.09))
SLAM_HEAP = heap((36, 0, 0), 0, -0.32, 0.0)
WOUND_HIPS = ((0, 0, 20), (0, 0.06, -0.06))
WOUND_HEAP = heap((12, 0, 58), 0.5, 0.4, 0.3)
THROUGH_HIPS = ((0, 0, -24), (0, -0.04, -0.08))
THROUGH_HEAP = heap((16, 0, -70), -0.55, 0.2, 0.26)

ACTIONS = {
    # Idle (3.2 s loop): breathing, heavily. A breath (the chest straightens and sinks, the heap riding it a moment
    # later, the junk settling after), then a sigh: a deeper breath in, and the shoulders drop, the head sags, the heap
    # sinks on its hip and the junk nods. It looks round slowly, the head trailing the breath.
    "Idle": clip(96, {
        "Hips": [(1, ((0, 0, 0), (0, 0, 0))), (22, ((0, 0, 0), (0, 0, 0.02))), (44, ((0, 0, 0), (0, 0, -0.012))),
                 (64, ((0, 0, 0), (0, 0, 0.032))), (80, ((0, 1, 0), (0, 0, -0.03)))],
        "Spine": [(1, (6, 0, -4)), (22, (3, 0, -4)), (46, (7, 0, -3)), (64, (1, 0, -4)), (82, (10, 1, -4))],
        "Chest": [(1, (4, 0, -4)), (24, (-1, 0, -4)), (47, (7, 0, -3)), (65, (-3, 0, -4)), (83, (11, 1, -3))],
        "Head": [(1, (-10, 0, 6)), (18, (-13, 0, 8)), (36, (-8, 0, 26)), (54, (-11, 0, 22)), (70, (-14, 0, -10)), (86, (2, 0, -6))],
        "Heap": [(1, heap((22, 0, -14), 0.14, 0.1, 0.16)), (27, heap((21, 0, -14), 0.14, 0.1, 0.19)),
                 (50, heap((23, 0, -13), 0.14, 0.1, 0.14)), (68, heap((20, 0, -14), 0.14, 0.1, 0.21)),
                 (86, heap((25, 0, -12), 0.14, 0.11, 0.1))],
        "HeapTop": [(1, (2, 0, 0)), (31, (-1, 0, 1)), (54, (4, 0, -1)), (72, (-2, 0, 0)), (90, (7, 0, 2))],
        "Frame": [(1, (0, 0, 0)), (34, (-3, 0, 3)), (57, (3, 0, -2)), (75, (-4, 0, 1)), (92, (8, 0, 3))],
        "Hem": [(1, (0, 0, 0)), (48, (1, 0, 1)), (84, (-2, 0, 0))],
        "Lantern": [(1, (0, 0, 0)), (40, (0, 4, 3)), (84, (5, -2, 0))],
    }, periodic=True),
    # Walk (32 frames, two steps): a heavy waddle. It drops onto each step (frames 24 and 8), rolls over onto that side
    # and rises between (upright at frame 1: the loop starts and ends on its passing pose, so it joins what's before and after); the skirt kicks out, the chest rolls after the hips, the head bobs after the chest, the heap
    # jostles on the hip and the junk on it after that; the lantern swings.
    "Walk": clip(32, {
        "Hips": [(24, ((0, -6, 5), (0.03, 0, -0.05))), (28, ((0, -5, 4), (0.025, 0, -0.065))), (1, ((0, 0, 0), (0, 0, 0.03))),
                 (8, ((0, 6, -5), (-0.03, 0, -0.05))), (12, ((0, 5, -4), (-0.025, 0, -0.065))), (17, ((0, 0, 0), (0, 0, 0.03)))],
        "Hem": [(25, (-9, 0, 5)), (1, (5, 4, 0)), (9, (-9, 0, -5)), (17, (5, -4, 0))],
        "Spine": [(24, (9, 4, -4)), (32, (6, 0, -4)), (8, (9, -4, -4)), (16, (6, 0, -4))],
        "Chest": [(26, (7, 4, -5)), (2, (3, 0, -4)), (10, (7, -4, -3)), (18, (3, 0, -4))],
        "Head": [(27, (-4, -3, 6)), (3, (-12, 0, 5)), (11, (-4, 3, 4)), (19, (-12, 0, 5))],
        "Heap": [(26, heap((26, 0, -14), 0.14, 0.1, 0.1)), (2, heap((19, 0, -14), 0.14, 0.1, 0.21)),
                 (10, heap((26, 0, -11), 0.14, 0.1, 0.1)), (18, heap((19, 0, -17), 0.14, 0.1, 0.21))],
        "HeapTop": [(28, (9, 0, -4)), (4, (-2, 0, 1)), (12, (9, 0, 4)), (20, (-2, 0, -1))],
        "Frame": [(30, (8, 0, -6)), (6, (-5, 0, 2)), (14, (8, 0, 6)), (22, (-5, 0, -2))],
        "Lantern": [(29, (-12, 0, 0)), (5, (9, 0, 0)), (13, (-12, 0, 0)), (21, (9, 0, 0))],
    }, periodic=True),
    # GuardUp (0.35 s): a dip to get under it, the heap heaved up before the face (overshooting, then settling), the
    # head ducking in behind it last; the frame on top flops back.
    "GuardUp": clip(10, keys(held(OPEN, 1), {
        "Hips": [(3, ((0, 0, 0), (0, 0, -0.03))), (8, ((0, 0, 0), (0, 0.02, -0.045))), (11, GUARD["Hips"])],
        "Spine": [(4, (9, 0, -2)), (8, (-5, 0, 0)), (11, GUARD["Spine"])],
        "Chest": [(4, (6, 0, -2)), (9, (-7, 0, 0)), (11, GUARD["Chest"])],
        "Head": [(5, (-12, 0, 4)), (10, (19, 0, 0)), (11, GUARD["Head"])],
        "Heap": [(4, heap((26, 0, -12), 0.12, 0.08, 0.1)), (8, heap((-12, 0, 0), 0, -0.06, 0.96)), (11, GUARD["Heap"])],
        "HeapTop": [(6, (8, 0, -4)), (9, (-8, 0, 0)), (11, GUARD["HeapTop"])],
        "Frame": [(7, (10, 0, -4)), (10, (-10, 0, 0)), (11, (-6, 0, 0))],
        "Hem": [(6, (3, 0, 0)), (11, GUARD["Hem"])],
        "Lantern": [(5, (6, 0, 0)), (11, (-3, 0, 0))],
    })),
    # GuardWalk (64 frames, two shuffles; upright at frame 1, as the walk): braced, low, the heap held steady before the face (it barely moves: it's a
    # wall), the body rocking under it, the junk on top nodding.
    "GuardWalk": clip(64, {
        "Hips": [(49, ((0, -2, 2), (0.01, 0.02, -0.06))), (57, ((0, -1, 1), (0.008, 0.02, -0.065))), (1, ((0, 0, 0), (0, 0.02, -0.04))),
                 (17, ((0, 2, -2), (-0.01, 0.02, -0.06))), (25, ((0, 1, -1), (-0.008, 0.02, -0.065))), (33, ((0, 0, 0), (0, 0.02, -0.04)))],
        "Spine": [(49, (-3, 1, 0)), (17, (-3, -1, 0))],
        "Chest": [(51, (-5, 1, 1)), (19, (-5, -1, -1))],
        "Head": [(53, (16, 0, 2)), (5, (14, 0, 0)), (21, (16, 0, -2)), (37, (14, 0, 0))],
        "Heap": [(51, heap((-8, 0, 1), 0, -0.06, 0.895)), (3, heap((-8, 0, 0), 0, -0.06, 0.91)),
                 (19, heap((-8, 0, -1), 0, -0.06, 0.895)), (35, heap((-8, 0, 0), 0, -0.06, 0.91))],
        "HeapTop": [(54, (-2, 0, -2)), (6, (-4, 0, 0)), (22, (-2, 0, 2)), (38, (-4, 0, 0))],
        "Frame": [(56, (-3, 0, -3)), (8, (-6, 0, 1)), (24, (-3, 0, 3)), (40, (-6, 0, -1))],
        "Hem": [(49, (-3, 0, 2)), (1, (2, 0, 0)), (17, (-3, 0, -2)), (33, (2, 0, 0))],
        "Lantern": [(52, (-4, 0, 0)), (4, (3, 0, 0)), (20, (-4, 0, 0)), (36, (3, 0, 0))],
    }, periodic=True),
    # Block (0.3 s): a blow on the heap shoves it back into its face; the head snaps back, the frame flops, and it
    # braces again.
    "Block": clip(9, keys(held(GUARD, 1), held(GUARD, 10), {
        "Hips": [(3, ((0, 0, 0), (0, 0.08, -0.05)))],
        "Spine": [(3, (-10, 0, 0)), (6, (-2, 0, 0))],
        "Chest": [(3, (-14, 0, 0)), (7, (-3, 0, 0))],
        "Head": [(4, (-6, 0, 0)), (8, (20, 0, 0))],
        "Heap": [(3, heap((-18, 0, 0), 0, 0.16, 0.84)), (7, heap((-5, 0, 0), 0, -0.08, 0.92))],
        "HeapTop": [(4, (-14, 0, 0)), (7, (4, 0, 0))],
        "Frame": [(5, (-18, 0, 4)), (8, (6, 0, -2))],
        "Hem": [(5, (6, 0, 0))],
        "Lantern": [(4, (10, 0, 0))],
    })),
    # Windup (0.9 s): it crouches over the heap (getting under it), then heaves it up overhead, rising onto its toes,
    # the head following it up last; the junk on top lags behind the heave and whips back at the top; held there,
    # straining.
    "Windup": clip(27, keys(held(GUARD, 1), {
        "Hips": [(7, ((0, 0, 0), (0, 0.02, -0.11))), (18, ((0, 0, 0), (0, 0.06, 0.03))), (23, ((0, 0, 0), (0, 0.08, 0.065))), (28, LIFTED_HIPS)],
        "Spine": [(7, (14, 0, 0)), (18, (-12, 0, 0)), (24, (-19, 0, 0)), (28, (-17, 0, 0))],
        "Chest": [(8, (12, 0, 0)), (19, (-8, 0, 0)), (25, (-13, 0, 0)), (28, (-11, 0, 1))],
        "Head": [(9, (22, 0, 0)), (20, (-6, 0, 0)), (26, (-18, 0, 0)), (28, (-16, 0, 0))],
        "Heap": [(7, heap((10, 0, 0), 0, -0.02, 0.62)), (17, heap((-12, 0, 0), 0, 0.06, 1.2)), (23, heap((-24, 0, 0), 0, 0.14, 1.36)), (28, LIFTED_HEAP)],
        "HeapTop": [(9, (6, 0, 0)), (19, (10, 0, 0)), (24, (-10, 0, 0)), (28, (-4, 0, 2))],
        "Frame": [(11, (8, 0, 0)), (21, (12, 0, 2)), (25, (-14, 0, 0)), (28, (-6, 0, 3))],
        "Hem": [(8, (-4, 0, 0)), (20, (6, 0, 0)), (28, (3, 0, 0))],
        "Lantern": [(9, (8, 0, 0)), (22, (-10, 0, 0)), (28, (-4, 0, 0))],
    })),
    # Strike (0.3 s): brought down, the body folding over after it, the impact deeper than where it rests (then a
    # rebound); the head whips down after the impact, the junk on top keeps going and the frame slaps forward.
    "Strike": clip(9, {
        "Hips": [(1, LIFTED_HIPS), (4, ((0, 0, 0), (0, -0.05, -0.02))), (6, ((0, 0, 0), (0, -0.32, -0.12))), (10, SLAM_HIPS)],
        "Spine": [(1, (-17, 0, 0)), (4, (12, 0, 0)), (6, (36, 0, 0)), (10, (32, 0, 0))],
        "Chest": [(1, (-11, 0, 1)), (3, (-4, 0, 0)), (5, (18, 0, 0)), (7, (14, 0, 0)), (10, (12, 0, 0))],
        "Head": [(1, (-16, 0, 0)), (4, (-4, 0, 0)), (6, (4, 0, 0)), (8, (20, 0, 0)), (10, (14, 0, 0))],
        "Heap": [(1, LIFTED_HEAP), (4, heap((8, 0, 0), 0, -0.02, 0.9)), (6, heap((38, 0, 0), 0, -0.3, -0.03)), (8, heap((35, 0, 0), 0, -0.3, 0.04)), (10, SLAM_HEAP)],
        "HeapTop": [(1, (-4, 0, 2)), (4, (-14, 0, 0)), (6, (-6, 0, 0)), (8, (16, 0, 4)), (10, (10, 0, 2))],
        "Frame": [(1, (-6, 0, 3)), (5, (-20, 0, 0)), (7, (4, 0, 0)), (9, (22, 0, 6)), (10, (18, 0, 5))],
        "Hem": [(1, (3, 0, 0)), (6, (-8, 0, 0)), (10, (-4, 0, 0))],
        "Lantern": [(1, (-4, 0, 0)), (6, (14, 0, 0)), (10, (8, 0, 0))],
    }),
    # Recover (1.2 s): bowed over the heap on the ground, heaving for breath (the punish window), the junk settling;
    # then it digs in and hauls the heap back up onto its hip (overshooting down, settling).
    "Recover": clip(36, {
        "Hips": [(1, SLAM_HIPS), (6, ((0, 0, 0), (0, -0.3, -0.1))), (14, ((0, 0, 0), (0, -0.29, -0.08))), (22, ((0, 0, 0), (0, -0.3, -0.1))),
                 (28, ((0, 0, 0), (0, -0.2, -0.12))), (34, ((0, 0, 0), (0, -0.04, 0.015))), (37, OPEN["Hips"])],
        "Spine": [(1, (32, 0, 0)), (14, (30, 0, 0)), (22, (33, 0, 0)), (28, (28, 0, -2)), (34, (4, 0, -4)), (37, OPEN["Spine"])],
        "Chest": [(1, (12, 0, 0)), (5, (15, 0, 0)), (14, (10, 0, 0)), (22, (14, 0, 0)), (29, (12, 0, -2)), (35, (2, 0, -4)), (37, OPEN["Chest"])],
        "Head": [(1, (14, 0, 0)), (6, (22, 0, 0)), (14, (12, 0, 4)), (22, (18, 0, -2)), (30, (10, 0, 0)), (36, (-12, 0, 6)), (37, OPEN["Head"])],
        "Heap": [(1, SLAM_HEAP), (6, heap((34, 0, 0), 0, -0.31, 0.01)), (22, SLAM_HEAP), (28, heap((32, 0, -2), 0, -0.28, 0.02)),
                 (33, heap((16, 0, -12), 0.1, 0.06, 0.34)), (35, heap((24, 0, -15), 0.15, 0.11, 0.12)), (37, OPEN["Heap"])],
        "HeapTop": [(1, (10, 0, 2)), (4, (-6, 0, -2)), (9, (4, 0, 1)), (14, (2, 0, 0)), (30, (4, 0, 0)), (34, (-8, 0, 4)), (37, OPEN["HeapTop"])],
        "Frame": [(1, (18, 0, 5)), (5, (-8, 0, -4)), (10, (6, 0, 2)), (16, (2, 0, 0)), (31, (4, 0, 0)), (35, (-12, 0, 4)), (37, OPEN["Frame"])],
        "Hem": [(1, (-4, 0, 0)), (8, (2, 0, 0)), (30, (0, 0, 0)), (34, (6, 0, 0)), (37, OPEN["Hem"])],
        "Lantern": [(1, (8, 0, 0)), (6, (-6, 0, 0)), (12, (3, 0, 0)), (20, (0, 0, 0)), (34, (-8, 0, 0)), (37, OPEN["Lantern"])],
    }),
    # SweepWindup (0.6 s): a twist the other way first, then the heap hauled round to its right and back, the hips
    # turning with it; the head stays on you (turning against the body); the junk drags behind the turn.
    "SweepWindup": clip(18, keys(held(GUARD, 1), {
        "Hips": [(5, ((0, 0, -6), (0, 0.02, -0.08))), (15, ((0, 0, 18), (0, 0.06, -0.05))), (19, WOUND_HIPS)],
        "Spine": [(5, (6, 0, -8)), (15, (4, 0, 32)), (19, (5, 0, 36))],
        "Chest": [(6, (4, 0, -10)), (16, (2, 0, 22)), (19, (2, 0, 26))],
        "Head": [(7, (14, 0, -6)), (14, (8, 0, -14)), (19, (6, 0, -20))],
        "Heap": [(5, heap((4, 0, -6), -0.05, -0.02, 0.76)), (15, heap((10, 0, 50), 0.45, 0.35, 0.32)), (19, WOUND_HEAP)],
        "HeapTop": [(8, (0, 0, -6)), (16, (2, 0, -14)), (19, (0, 0, -8))],
        "Frame": [(10, (0, 0, -8)), (17, (4, 0, -18)), (19, (2, 0, -12))],
        "Hem": [(9, (0, 0, -6)), (19, (0, 0, 8))],
        "Lantern": [(10, (0, 6, 0)), (19, (0, -4, 0))],
    })),
    # Sweep (0.25 s): swung round in front and across to its left, low; the hips lead, the chest follows, the heap
    # last; the junk is flung outward.
    "Sweep": clip(7, {
        "Hips": [(1, WOUND_HIPS), (4, ((0, 0, -6), (0, 0, -0.07))), (8, THROUGH_HIPS)],
        "Spine": [(1, (5, 0, 36)), (4, (7, 0, 0)), (6, (9, 0, -34)), (8, (10, 0, -44))],
        "Chest": [(1, (2, 0, 26)), (3, (3, 0, 18)), (5, (5, 0, -12)), (8, (6, 0, -24))],
        "Head": [(1, (6, 0, -20)), (3, (8, 0, -10)), (5, (8, 0, 10)), (8, (8, 0, 24))],
        "Heap": [(1, WOUND_HEAP), (4, heap((14, 0, 0), 0.02, -0.28, 0.26)), (6, heap((15, 0, -50), -0.4, 0.02, 0.25)), (8, THROUGH_HEAP)],
        "HeapTop": [(1, (0, 0, -8)), (4, (0, 0, 16)), (6, (2, 0, 18)), (8, (2, 0, 6))],
        "Frame": [(1, (2, 0, -12)), (4, (0, 0, 22)), (7, (4, 0, 26)), (8, (4, 0, 20))],
        "Hem": [(1, (0, 0, 8)), (5, (0, 0, -4)), (8, (-4, 0, -12))],
        "Lantern": [(1, (0, -4, 0)), (5, (0, 8, 0)), (8, (0, 10, 0))],
    }),
    # SweepRecover (0.7 s): carried past by the weight (a stagger-step of the hips), the junk whipping back, then it
    # gathers the heap back onto its hip.
    "SweepRecover": clip(21, {
        "Hips": [(1, THROUGH_HIPS), (5, ((0, 0, -28), (-0.04, -0.06, -0.1))), (13, ((0, 0, -12), (0, -0.02, -0.05))), (22, OPEN["Hips"])],
        "Spine": [(1, (10, 0, -44)), (5, (12, 0, -50)), (13, (10, 0, -24)), (22, OPEN["Spine"])],
        "Chest": [(1, (6, 0, -24)), (6, (8, 0, -30)), (14, (6, 0, -14)), (22, OPEN["Chest"])],
        "Head": [(1, (8, 0, 24)), (6, (14, 0, 18)), (15, (2, 0, 10)), (22, OPEN["Head"])],
        "Heap": [(1, THROUGH_HEAP), (5, heap((20, 0, -74), -0.58, 0.22, 0.2)), (13, heap((20, 0, -45), -0.3, 0.15, 0.14)), (22, OPEN["Heap"])],
        "HeapTop": [(1, (2, 0, 6)), (5, (4, 0, -10)), (11, (2, 0, 4)), (17, (4, 0, -2)), (22, OPEN["HeapTop"])],
        "Frame": [(1, (4, 0, 20)), (6, (6, 0, -14)), (12, (2, 0, 8)), (18, (0, 0, -3)), (22, OPEN["Frame"])],
        "Hem": [(1, (-4, 0, -12)), (7, (0, 0, 6)), (14, (0, 0, -3)), (22, OPEN["Hem"])],
        "Lantern": [(1, (0, 10, 0)), (7, (0, -8, 0)), (14, (0, 4, 0)), (22, OPEN["Lantern"])],
    }),
    # Stagger (0.6 s as made; a stun plays it slowed): knocked back off its guard, a stumble, the junk thrown about,
    # then it gathers itself, heap back on its hip.
    "Stagger": clip(18, keys(held(GUARD, 1), {
        "Hips": [(4, ((0, 0, 0), (0, 0.14, 0.0))), (9, ((0, 4, 0), (0.04, 0.16, -0.06))), (14, ((0, -2, 0), (0, 0.06, -0.03))), (19, OPEN["Hips"])],
        "Spine": [(4, (-16, 0, 6)), (9, (-8, 4, 0)), (14, (4, -2, -4)), (19, OPEN["Spine"])],
        "Chest": [(4, (-14, 0, 8)), (10, (-6, 0, -4)), (15, (6, 0, -4)), (19, OPEN["Chest"])],
        "Head": [(3, (-20, 0, 8)), (8, (-6, 0, -6)), (13, (6, 0, 4)), (19, OPEN["Head"])],
        "Heap": [(4, heap((-14, 0, 6), 0, 0.18, 0.62)), (10, heap((6, 0, -6), 0.05, 0.05, 0.32)), (15, heap((24, 0, -14), 0.14, 0.1, 0.12)), (19, OPEN["Heap"])],
        "HeapTop": [(5, (-16, 0, 8)), (9, (8, 0, -6)), (14, (-4, 0, 2)), (19, OPEN["HeapTop"])],
        "Frame": [(6, (-22, 0, 10)), (10, (12, 0, -8)), (15, (-6, 0, 4)), (19, OPEN["Frame"])],
        "Hem": [(6, (8, 4, 0)), (12, (-4, -2, 0)), (19, OPEN["Hem"])],
        "Lantern": [(5, (16, 0, 4)), (10, (-10, 0, -2)), (15, (4, 0, 0)), (19, OPEN["Lantern"])],
    })),
    # Death (0.85 s; then it dissolves): a jolt back, the heap slips from its arms and crashes down (a bounce), the junk
    # tumbling off it, and it slumps forward over it.
    "Death": clip(25, keys(held(GUARD, 1), {
        "Hips": [(5, ((0, 0, 0), (0, 0.1, 0.0))), (14, ((0, 0, 0), (0, -0.04, -0.18))), (20, ((0, 0, 0), (0, -0.06, -0.28))), (26, ((0, 0, 0), (0, -0.06, -0.3)))],
        "Spine": [(5, (-16, 0, 6)), (13, (24, 0, -6)), (20, (40, 0, -10)), (26, (44, 0, -10))],
        "Chest": [(5, (-12, 0, 4)), (14, (14, 0, -4)), (21, (20, 0, -6)), (26, (22, 0, -6))],
        "Head": [(4, (-22, 0, 0)), (11, (-8, 0, 4)), (18, (30, 0, 0)), (23, (46, 0, -6)), (26, (44, 0, -6))],
        "Heap": [(5, heap((-12, 0, 0), 0, 0.12, 0.66)), (12, heap((30, 0, 18), 0.1, -0.35, 0.3)), (17, heap((58, 0, 28), 0.15, -0.47, -0.02)),
                 (20, heap((54, 0, 27), 0.15, -0.46, 0.05)), (26, heap((58, 0, 28), 0.15, -0.47, 0.0))],
        "HeapTop": [(6, (-14, 0, 0)), (13, (10, 0, 6)), (17, (30, 0, 12)), (20, (20, 0, 8)), (26, (26, 0, 10))],
        "Frame": [(7, (-18, 0, 0)), (14, (14, 0, 8)), (18, (50, 0, 20)), (22, (40, 0, 16)), (26, (48, 0, 18))],
        "Hem": [(8, (6, 0, 0)), (16, (-8, 0, 0)), (26, (-4, 0, 0))],
        "Lantern": [(6, (14, 0, 0)), (14, (-10, 0, 4)), (20, (6, 0, 0)), (26, (2, 0, 0))],
    })),
}


def build():
    robe, dark = kit.mat("Soured", "#6c5c84"), kit.mat("SouredDark", "#4c4060")
    eye, claw = kit.mat("SouredEye_Glow", "#c8f07a"), kit.mat("Claw", "#d6cdb8")
    wood, wood_dark = kit.mat("HeapWood", "#7a5538"), kit.mat("HeapWoodDark", "#56392a")
    metal, cloth = kit.mat("HeapMetal", "#34313a"), kit.mat("HeapCloth", "#8c7a5c")
    clock_face, frame_gold = kit.mat("ClockFace", "#e8dcc0"), kit.mat("FrameGold", "#b08d3e")
    rope = kit.mat("SouredRope", "#3e3450")

    def skirt(bm):
        kit.cone(9, 0.55, 0.4, 0.62)(bm)
        rim = sorted((v for v in bm.verts if v.co.z < -0.25 and v.co.length > 0.1), key=lambda v: math.atan2(v.co.y, v.co.x))
        for i, v in enumerate(rim):
            v.co.z += 0.1 if i % 2 else 0.0  # tattered hem

    parts = [
        # The robe in two: the skirt hangs from the waist (it swings after the body), the upper robe moves with it;
        # a rope belt hides where they meet.
        part("Skirt", skirt, robe, M((0, 0.02, 0.38), rot=(4, 0, 0)), smooth=True, group="Hem"),
        part("Robe", kit.cone(9, 0.42, 0.26, 0.6), robe, M((0, -0.03, 0.9), rot=(10, 0, 0)), smooth=True, group="Spine"),
        part("Belt", kit.cylinder(12, 0.43, 0.07), rope, M((0, -0.01, 0.63), rot=(8, 0, 0)), smooth=True, group="Spine"),
        part("Hump", kit.sphere(10, 7), robe, M((0, 0.05, 1.05), scale=(0.42, 0.34, 0.3)), smooth=True, group="Chest"),
        part("Head", kit.sphere(10, 7, 0.22), dark, M((0, -0.24, 1.3), scale=(1, 1.05, 0.95)), smooth=True, group="Head"),
        part("Hood", kit.cone(8, 0.28, 0.06, 0.44), robe, M((0, -0.17, 1.42), rot=(-35, 0, 0)), smooth=True, group="Head"),
    ]
    for s, arm, fore, shoulder, elbow, hand in ((-1, "ArmL", "ForeL", SHOULDER_L, ELBOW_L, HAND_L), (1, "ArmR", "ForeR", SHOULDER_R, ELBOW_R, HAND_R)):
        parts.append(part("Eye", kit.sphere(8, 6, 0.04), eye, M((s * 0.08, -0.45, 1.3)), smooth=True, group="Head"))
        # The arm in two (a sleeve to the elbow, a forearm to the claws on the heap), the elbow a knob of robe.
        parts.append(part("Sleeve", kit.cone(7, 0.13, 0.1, math.dist(shoulder, elbow) + 0.04), robe, kit.along(shoulder, elbow), smooth=True, group=arm))
        parts.append(part("Elbow", kit.sphere(8, 6, 0.1), robe, M(elbow), smooth=True, group=arm))
        parts.append(part("Forearm", kit.cone(7, 0.095, 0.075, math.dist(elbow, hand)), robe, kit.along(elbow, hand), smooth=True, group=fore))
        parts.append(part("Claw", kit.cone(5, 0.04, 0.0, 0.14), claw, kit.along(hand, (s * 0.36, -0.62, 0.84)), group=fore))

    # A lantern hung from its belt on the right, on a cord: sickly green, like its eyes. Green light in the dark = a Soured.
    lantern_glass = kit.mat("SouredLantern_Glow", "#b6f06a")
    parts += [
        part("BeltLanternCord", kit.cube(), metal, M((0.505, 0.0, 0.8), rot=(0, -6, 0), scale=(0.015, 0.015, 0.16)), group="Lantern"),
        part("BeltLanternTop", kit.cone(4, 0.08, 0.01, 0.07), metal, M((0.515, 0.0, 0.7), rot=(0, 0, 45)), group="Lantern"),
        part("BeltLanternGlass", kit.cube(), lantern_glass, M((0.515, 0.0, 0.61), scale=(0.1, 0.1, 0.13)), group="Lantern"),
        part("BeltLanternBase", kit.cube(), metal, M((0.515, 0.0, 0.535), scale=(0.12, 0.12, 0.02)), group="Lantern"),
    ]

    # The heap: a chest; on it a chair, a bundle and a wall clock; a picture frame on top of all.
    y = -0.58
    parts += [
        part("Chest", kit.cube(), wood, M((0, y, 0.72), rot=(0, 0, 4), scale=(0.72, 0.46, 0.36)), group="Heap"),
        part("ChestBand", kit.cube(), metal, M((0, y, 0.72), rot=(0, 0, 4), scale=(0.74, 0.48, 0.05)), group="Heap"),
        part("ChestLid", kit.cube(), wood_dark, M((0, y, 0.92), rot=(0, 0, 4), scale=(0.74, 0.48, 0.05)), group="Heap"),
        part("Seat", kit.cube(), wood_dark, M((0.05, y, 1.12), rot=(0, 8, -10), scale=(0.44, 0.4, 0.05)), group="HeapTop"),
        part("ChairBack", kit.cube(), wood_dark, M((0.08, y + 0.18, 1.36), rot=(-8, 8, -10), scale=(0.42, 0.05, 0.45)), group="HeapTop"),
        part("Bundle", kit.sphere(8, 6), cloth, M((-0.3, y, 1.18), scale=(0.2, 0.2, 0.16)), smooth=True, group="HeapTop"),
        part("Clock", kit.cylinder(10, 0.17, 0.07), metal, M((-0.12, y - 0.12, 1.5), rot=(90, 0, 12)), group="HeapTop"),
        part("ClockFace", kit.cylinder(10, 0.14, 0.02), clock_face, M((-0.12, y - 0.165, 1.5), rot=(90, 0, 12)), group="HeapTop"),
        part("Frame", kit.cube(), frame_gold, M((0.22, y - 0.05, 1.62), rot=(0, -14, 8), scale=(0.3, 0.04, 0.38)), group="Frame"),
        part("Picture", kit.cube(), dark, M((0.22, y - 0.075, 1.62), rot=(0, -14, 8), scale=(0.22, 0.02, 0.3)), group="Frame"),
    ]
    for dx, dy in ((-0.17, -0.15), (0.2, -0.15), (-0.17, 0.15), (0.2, 0.15)):
        parts.append(part("ChairLeg", kit.cube(), wood_dark, M((0.05 + dx, y + dy, 1.02), rot=(0, 8, -10), scale=(0.04, 0.04, 0.2)), group="HeapTop"))

    mesh = kit.join(parts, "Laden")
    armature = kit.rig("LadenRig", BONES)
    kit.skin(mesh, armature)
    kit.animate(armature, ACTIONS)
    for (name, _), worst in zip(ACTIONS.items(), REACH.values()):
        if worst > 0.02:
            print(f"[laden] {name}: the hands come off the heap by up to {worst * 100:.0f} cm")
    return [armature, mesh]
