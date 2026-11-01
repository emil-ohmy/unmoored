"""The Drowned: a Soured that couldn't let go of life. It waits under the water at the edges, and rises to drag the
living down with it. Seen from the waist up (its origin is at the waterline: below it, the rags of its clothes trail
off into the dark): bloated and grey-green, long lank hair like waterweed hanging over its face, a mouth hanging open,
arms too long, pale swollen hands. The same sickly green eyes as every Soured, glowing through the hair, and its belt
lantern, drowned, still faintly lit.

Clips: Idle (lurking under, adrift), Swim (gliding under, arms trailing), Rise (0.85 s telegraph: bursting up, arms
flung high, the head drawn back, gathering), Spit (0.3 s: the head thrust forward, the hands slapping down onto the
edge), Clutch (loop: up at the edge, hanging on and looking about; after a spit, and at the shore), Sink (0.6 s, or
2 s knocked under by the Stroke: back under), Stranded (loop: hooked out onto dry ground, flopping on its belly), Crawl
(loop: hauling itself back to the water), Stagger (a jolt back, reeling, flopped on its belly), Death.

Under the water and on the surface (Idle, Swim, Rise, Spit, Clutch, Sink) it reaches below its origin: the rags, and in
Sink the whole of it, are under the surface. Out of it (Stranded, Crawl, and Stagger and Death, which can come on dry
ground after the Hook) it rests on the ground: every frame of those is set down so its lowest point is at 0.

How it's animated (as the Laden): each bone keyed on its own timing (kit.bake: tracks). Everything about it moves as in
water, slowly, with drag: the body leads, the head follows, the arms trail it (the upper arm, then the forearm, then the
hand, each a few frames later), the hair (three locks: left, right, and the fringe over the face), the rags (in two)
and the lantern trail further and overshoot. Where its hands hold something (the edge it clutches, the ground it
crawls on) they're keyed where they are and the arms are bent onto them every frame (kit.reach), the palms laid flat.
"""
import math

import bmesh
from mathutils import Euler, Vector

M, part = kit.M, kit.part  # `kit` is injected by build_model.py

SHOULDER_Z = 0.66
SHOULDER_L, ELBOW_L, WRIST_L = (-0.24, -0.02, SHOULDER_Z), (-0.28, -0.05, 0.34), (-0.3, -0.06, 0.06)
SHOULDER_R, ELBOW_R, WRIST_R = (0.24, -0.02, SHOULDER_Z), (0.28, -0.05, 0.34), (0.3, -0.06, 0.06)
BONES = {
    "Root": ((0, 0, 0), (0, 0, 0.2), None),
    "Pivot": ((0, 0, 0.05), (0, 0, 0.15), "Root"),  # whole-body lunges and flops turn about the waterline
    "Rags": ((0, 0, 0.02), (0, 0.01, -0.4), "Pivot"),        # the rags below the waist, in two: they trail
    "RagsTip": ((0, 0.01, -0.4), (0, 0.02, -0.86), "Rags"),
    "Spine": ((0, 0, 0.05), (0, -0.04, SHOULDER_Z), "Pivot"),
    "Chest": ((0, -0.02, 0.36), (0, -0.04, SHOULDER_Z), "Spine"),  # the upper body (the torso bends between)
    "Head": ((0, -0.04, SHOULDER_Z), (0, -0.08, 1.02), "Chest"),
    "HairL": ((-0.06, -0.04, 0.95), (-0.15, 0.0, 0.4), "Head"),    # its hair, in three locks
    "HairR": ((0.06, -0.04, 0.95), (0.15, 0.0, 0.4), "Head"),
    "Fringe": ((0, -0.15, 0.94), (0, -0.21, 0.55), "Head"),        # (over its face)
    "ArmL": (SHOULDER_L, ELBOW_L, "Chest"),
    "ForeL": (ELBOW_L, WRIST_L, "ArmL"),
    "HandL": (WRIST_L, (-0.3, -0.08, -0.12), "ForeL"),
    "ArmR": (SHOULDER_R, ELBOW_R, "Chest"),
    "ForeR": (ELBOW_R, WRIST_R, "ArmR"),
    "HandR": (WRIST_R, (0.3, -0.08, -0.12), "ForeR"),
    "Lantern": ((0.24, 0.0, 0.19), (0.25, 0.0, -0.07), "Spine"),   # its belt lantern, on its cord
}
WRIST = {"L": WRIST_L, "R": WRIST_R}


def T(x, y, z):
    """An offset (or where a hand goes: a target)."""
    return ((0, 0, 0), (x, y, z))


def W(w):
    """How far a hand holds its target (0: the arm keyed as it is; 1: on the target)."""
    return ((w, 0, 0), (0, 0, 0))


def held(pose, frame):
    """A whole pose as a key at `frame` for every bone (to start or end a clip on it)."""
    return {bone: [(frame, value)] for bone, value in pose.items()}


def keys(*parts):
    """Tracks merged: each a {bone: [(frame, value)]} (a later key on the same frame replaces an earlier one)."""
    merged = {}
    for p in parts:
        for bone, ks in p.items():
            merged.setdefault(bone, {}).update(dict(ks))
    return {bone: sorted(ks.items()) for bone, ks in merged.items()}


def over(pose, tracks):
    """`tracks`, and every bone they don't key held in `pose` (a loop's resting pose)."""
    return keys(held({bone: v for bone, v in pose.items() if bone not in tracks}, 1), tracks)


def mirror(rot):
    """A left arm's rotation for the right arm."""
    return (rot[0], -rot[1], -rot[2])


def arms(track_l, track_r=None):
    """Tracks for the left arm's ArmL/ForeL/HandL ({"Arm": [...], ...}) and the right's (mirrored, if not given)."""
    out = {}
    for part_name, ks in track_l.items():
        out[part_name + "L"] = ks
    for part_name, ks in (track_r or {k: [(f, mirror(v)) for f, v in ks] for k, ks in track_l.items()}).items():
        out[part_name + "R"] = ks
    return out


VERTS = []   # the mesh's rest vertices and their bones (filled by build(): to set it down on the ground)
REACH = {}   # (the build's report: how far each clip's hands come off what they hold, at worst)


def lowest(pose):
    """The lowest point of the mesh in `pose`."""
    m = kit.pose_matrices(BONES, pose)
    low = 9.0
    for co, ws in VERTS:
        if len(ws) == 1:
            z = (m[ws[0][0]] @ co).z
        else:
            z = sum((m[b] @ co).z * w for b, w in ws)
        low = min(low, z)
    return low


def as_rot(value):
    return tuple(value[0]) if isinstance(value[0], (tuple, list)) else tuple(value)


def lerp_rot(a, b, t):
    out = []
    for x, y in zip(a, b):
        while y - x > 180:
            y -= 360
        while x - y > 180:
            y += 360
        out.append(x + (y - x) * t)
    return tuple(out)


def hands(pose, worst):
    """The hands onto their targets (TargetL/R: where the wrist goes, PalmL/R: the hand's world rotation, ElbowL/R: which
    way the elbow bows, IkL/R: how far it holds on), the arms bent with kit.reach; blended with the arm as keyed."""
    pose = dict(pose)
    for side, s in (("L", -1), ("R", 1)):
        target = pose.pop("Target" + side, None)
        palm = pose.pop("Palm" + side, None)
        elbow = pose.pop("Elbow" + side, None)
        ik = pose.pop("Ik" + side, None)
        if target is None:
            continue
        w = max(0.0, min(1.0, as_rot(ik)[0])) if ik is not None else 1.0
        if w <= 0.001:
            continue
        goal = Vector(target[1])
        pole = tuple(elbow[1]) if elbow is not None else (s * 1.0, 0.2, -0.3)
        arm, fore, hand = "Arm" + side, "Fore" + side, "Hand" + side
        solved = kit.reach(BONES, pose, arm, fore, lambda m: goal, pole)
        if palm is not None:
            parent = kit.pose_matrices(BONES, solved)[fore].to_quaternion()
            world = Euler([math.radians(a) for a in as_rot(palm)]).to_quaternion()
            solved[hand] = tuple(math.degrees(a) for a in (parent.inverted() @ world).to_euler("XYZ"))
        for bone in (arm, fore) + ((hand,) if palm is not None else ()):
            pose[bone] = lerp_rot(as_rot(pose.get(bone, (0, 0, 0))), as_rot(solved[bone]), w)
        if w > 0.99:
            m = kit.pose_matrices(BONES, pose)
            worst[0] = max(worst[0], (m[fore] @ Vector(WRIST[side]) - goal).length)
    return pose


def clip(name, length, tracks, periodic=False, ground=False):
    """A clip `length` frames long (30 a second) from per-bone tracks, baked to every frame; the hands kept on what they
    hold; `ground`: out of the water, set down on the ground every frame (its lowest point at 0)."""
    frames = []
    worst = [0.0]
    for f, p in kit.bake(tracks, length, periodic):
        p = hands(p, worst)
        if ground:
            root = p.get("Root", ((0, 0, 0), (0, 0, 0)))
            rot, off = (root if isinstance(root[0], (tuple, list)) else (root, (0, 0, 0)))
            p["Root"] = (tuple(rot), (off[0], off[1], off[2] - lowest(p)))
        frames.append((f, p))
    REACH[name] = worst[0]
    return ("LINEAR", frames)


# The poses everything starts from or comes back to.
# LURK   (under, waiting): adrift, hunched, the arms hanging slack before it, the hair floating up and out.
LURK = {"Root": T(0, 0, 0), "Pivot": (12, 0, 0), "Spine": (6, 0, 0), "Chest": (6, 0, 0), "Head": (12, 0, 0),
        "ArmL": (-14, 10, 0), "ForeL": (-14, 0, 0), "HandL": (-8, 0, 0),
        "ArmR": (-14, -10, 0), "ForeR": (-14, 0, 0), "HandR": (-8, 0, 0),
        "HairL": (-6, 16, 0), "HairR": (-6, -16, 0), "Fringe": (-12, 0, 0),
        "Rags": (10, 0, 0), "RagsTip": (8, 0, 0), "Lantern": (0, 0, 0)}
# RISEN  (up, gathering to spit): leaning back, the arms high, the claws hooked forward, the head drawn far back; the
#        wet hair hanging down its back.
RISEN = {"Root": T(0, 0, 0.1), "Pivot": (-8, 0, 0), "Spine": (-4, 0, 0), "Chest": (-12, 0, 0), "Head": (-34, 0, 0),
         "ArmL": (-156, -22, 0), "ForeL": (24, 0, 0), "HandL": (34, 0, 0),
         "ArmR": (-156, 22, 0), "ForeR": (24, 0, 0), "HandR": (34, 0, 0),
         "HairL": (32, 12, 0), "HairR": (32, -12, 0), "Fringe": (36, 0, 0),
         "Rags": (-4, 0, 0), "RagsTip": (-4, 0, 0), "Lantern": (4, 0, 0)}
# CLUTCH (up at the edge): leaning on it, the hands flat on its top before it (a quay's edge: 0.5 m up, 0.45 m off),
#        the elbows out, the head raised, peering over.
EDGE_L, EDGE_R = T(-0.3, -0.72, 0.53), T(0.3, -0.72, 0.53)
CLUTCH = {"Root": T(0, -0.04, 0), "Pivot": (14, 0, 0), "Spine": (4, 0, 0), "Chest": (6, 0, 0), "Head": (-30, 0, 0),
          "HandL": (0, 0, 0), "HandR": (0, 0, 0),
          "HairL": (8, 10, 0), "HairR": (8, -10, 0), "Fringe": (10, 0, 0),
          "Rags": (4, 0, 0), "RagsTip": (2, 0, 0), "Lantern": (0, 0, 0),
          "TargetL": EDGE_L, "TargetR": EDGE_R, "PalmL": (-78, 0, -12), "PalmR": (-78, 0, 12),
          "ElbowL": T(-1, 0.1, -0.4), "ElbowR": T(1, 0.1, -0.4), "IkL": W(1), "IkR": W(1)}
# LYING  (out of the water, on its belly): the head raised, the rags along the ground behind, the hands flat before it.
GROUND_L, GROUND_R = T(-0.38, -0.6, 0.06), T(0.38, -0.6, 0.06)
LYING = {"Root": T(0, 0.3, 0), "Pivot": (74, 0, 0), "Spine": (4, 0, 0), "Chest": (-14, 0, 0), "Head": (-50, 0, 0),
         "HandL": (0, 0, 0), "HandR": (0, 0, 0),
         "HairL": (8, 14, 0), "HairR": (8, -14, 0), "Fringe": (2, 0, 0),
         "Rags": (14, 0, 0), "RagsTip": (6, 0, 0), "Lantern": (60, 0, 0),
         "TargetL": GROUND_L, "TargetR": GROUND_R, "PalmL": (-84, 0, -10), "PalmR": (-84, 0, 10),
         "ElbowL": T(-1, 0.4, 1), "ElbowR": T(1, 0.4, 1), "IkL": W(1), "IkR": W(1)}
# THROWN (a blow, a hard landing): jolted back, the head snapped back, the arms flung out, the rags swept out behind.
THROWN = {"Root": T(0, 0.1, 0), "Pivot": (-12, 0, 0), "Spine": (-4, 0, 0), "Chest": (-8, 0, 0), "Head": (-18, 0, 0),
          "ArmL": (-20, 100, 0), "ForeL": (-30, 0, 0), "HandL": (-20, 0, 0),
          "ArmR": (-20, -100, 0), "ForeR": (-30, 0, 0), "HandR": (-20, 0, 0),
          "HairL": (24, 20, 0), "HairR": (24, -20, 0), "Fringe": (20, 0, 0),
          "Rags": (95, 0, 0), "RagsTip": (0, 0, 0), "Lantern": (40, 0, 0),
          "TargetL": GROUND_L, "TargetR": GROUND_R, "IkL": W(0), "IkR": W(0)}

# STAGGERED (the end of a stagger): the Clutch, but its rags folded back behind it (so it never sinks into dry ground).
STAGGERED = dict(CLUTCH, Rags=(98, 0, 0), RagsTip=(8, 0, 0))


# The Swim's tracks (its first frame is where the Sink ends: it swims off from there).
SWIM = {
    "Root": [(1, T(0, 0, 0)), (6, T(0, -0.03, 0.01)), (13, T(0, 0, 0.03))],
    "Pivot": [(1, (46, 0, 0)), (12, (56, 0, 0))],
    "Spine": [(3, (8, 0, 0)), (14, (2, 0, 0))],
    "Chest": [(4, (2, 0, 0)), (15, (9, 0, 0))],
    "Head": [(2, (-34, 0, 0)), (13, (-41, 0, 0))],
    "ArmL": [(3, (-6, 12, 0)), (14, (8, 6, 0))],
    "ForeL": [(7, (2, 0, 0)), (18, (14, 0, 0))],
    "HandL": [(10, (8, 0, 0)), (21, (24, 0, 0))],
    "ArmR": [(5, (-8, -12, 0)), (16, (8, -6, 0))],
    "ForeR": [(9, (2, 0, 0)), (20, (14, 0, 0))],
    "HandR": [(12, (8, 0, 0)), (23, (24, 0, 0))],
    "HairL": [(5, (26, 10, 0)), (16, (38, 14, 0))],
    "HairR": [(6, (26, -10, 0)), (17, (38, -14, 0))],
    "Fringe": [(5, (6, 0, 0)), (16, (16, 0, 0))],
    "Rags": [(6, (4, 0, 0)), (17, (24, 0, 0))],
    "RagsTip": [(9, (-8, 0, 0)), (20, (30, 0, 0))],
    "Lantern": [(8, (30, 0, 0)), (19, (44, 0, 0))],
}


def actions():
    swim_start = dict(kit.bake(SWIM, 23, periodic=True)[0][1])
    return {
        # Idle (1.6 s loop; under the water, waiting): adrift. It bobs, slowly; the body sways after it, the chest heaves
        # (a gulp of water); it turns its head to listen off to its left, holds, and twitches back. The arms hang and
        # drift after the body, each joint later than the last; the hair floats, swaying after the head; the rags
        # undulate below.
        "Idle": clip("Idle", 47, {
            "Root": [(1, T(0, 0, 0)), (14, T(0, 0.01, 0.035)), (30, T(0, -0.01, -0.015))],
            "Pivot": [(1, (12, 0, 0)), (18, (15, 2, 0)), (34, (10, -2, 0))],
            "Spine": [(1, (6, 0, 0)), (20, (9, 1, 0)), (36, (4, -1, 0))],
            "Chest": [(1, (6, 0, 0)), (10, (2, 0, 0)), (22, (11, 0, 0)), (38, (5, 0, 0))],
            "Head": [(1, (12, 0, 0)), (12, (6, 0, -18)), (24, (-2, -4, -30)), (29, (-4, -4, -32)), (32, (14, 6, 10)),
                     (36, (10, 4, 12)), (44, (13, 0, 2))],
            "ArmL": [(1, (-14, 10, 0)), (19, (-30, 16, 0)), (37, (-8, 8, 0))],
            "ForeL": [(1, (-14, 0, 0)), (9, (-10, 0, 0)), (25, (-32, 0, 0)), (42, (-8, 0, 0))],
            "HandL": [(1, (-8, 0, 0)), (13, (0, 0, 0)), (29, (-20, 0, 0)), (45, (-4, 0, 0))],
            "ArmR": [(1, (-14, -10, 0)), (10, (-8, -8, 0)), (28, (-30, -16, 0))],
            "ForeR": [(1, (-14, 0, 0)), (16, (-6, 0, 0)), (34, (-32, 0, 0))],
            "HandR": [(1, (-8, 0, 0)), (20, (0, 0, 0)), (39, (-20, 0, 0))],
            "HairL": [(1, (-6, 16, 0)), (16, (-2, 20, 0)), (30, (-10, 10, 0)), (35, (4, 26, 0)), (40, (-4, 18, 0))],
            "HairR": [(1, (-6, -16, 0)), (18, (-2, -12, 0)), (32, (-8, -24, 0)), (36, (2, -8, 0)), (42, (-6, -14, 0))],
            "Fringe": [(1, (-12, 0, 0)), (14, (-16, 0, 6)), (30, (-8, 0, 10)), (35, (-20, 0, -8)), (41, (-12, 0, -2))],
            "Rags": [(1, (10, 0, 0)), (16, (4, 0, 3)), (34, (14, 0, -3))],
            "RagsTip": [(1, (8, 0, 0)), (8, (14, 0, 0)), (24, (2, 0, 5)), (42, (14, 0, -4))],
            "Lantern": [(1, (0, 0, 0)), (20, (4, 6, 0)), (38, (-4, -4, 0))],
        }, periodic=True),
        # Swim (23 frames; under, gliding after someone): a slow kick, like an eel's, that runs down the body from the
        # chest to the rags' ends; the head held level, looking ahead; the arms trail along its sides, dragged, and the
        # hair and the lantern stream back.
        "Swim": clip("Swim", 23, SWIM, periodic=True),
        # Rise (0.85 s, the telegraph; the game lifts it to the surface in its first quarter): it coils (curling down,
        # the arms drawn back), bursts up out of the water, leaning back, the arms flung up overhead (overshooting, the
        # forearms and hands whipping after), the hair and the rags lagging the burst; then, risen, it gathers: the
        # chest swells and the head draws further and further back, the claws hooked over it.
        "Rise": clip("Rise", 16, keys(held(LURK, 1), {
            "Root": [(5, T(0, 0.02, -0.12)), (9, T(0, 0, 0.17)), (12, T(0, 0, 0.07)), (17, RISEN["Root"])],
            "Pivot": [(5, (20, 0, 0)), (9, (-16, 0, 0)), (13, (-5, 0, 0)), (17, RISEN["Pivot"])],
            "Spine": [(5, (10, 0, 0)), (10, (-10, 0, 0)), (14, (-2, 0, 0)), (17, RISEN["Spine"])],
            "Chest": [(6, (10, 0, 0)), (10, (-14, 0, 0)), (13, (-6, 0, 0)), (17, RISEN["Chest"])],
            "Head": [(6, (22, 0, 0)), (9, (10, 0, 0)), (11, (-26, 0, 0)), (14, (-20, 0, 0)), (17, RISEN["Head"])],
            **arms({
                "Arm": [(5, (26, 14, 0)), (10, (-172, -26, 0)), (13, (-148, -18, 0)), (17, RISEN["ArmL"])],
                "Fore": [(6, (20, 0, 0)), (11, (-14, 0, 0)), (14, (32, 0, 0)), (17, RISEN["ForeL"])],
                "Hand": [(7, (30, 0, 0)), (12, (-30, 0, 0)), (15, (42, 0, 0)), (17, RISEN["HandL"])],
            }),
            "HairL": [(5, (-30, 30, 0)), (9, (-14, 10, 0)), (12, (50, 8, 0)), (15, (26, 12, 0)), (17, RISEN["HairL"])],
            "HairR": [(5, (-30, -30, 0)), (9, (-14, -10, 0)), (12, (50, -8, 0)), (15, (26, -12, 0)), (17, RISEN["HairR"])],
            "Fringe": [(5, (-30, 0, 0)), (10, (-10, 0, 0)), (12, (50, 0, 0)), (15, (30, 0, 0)), (17, RISEN["Fringe"])],
            "Rags": [(6, (-10, 0, 0)), (10, (20, 0, 0)), (17, RISEN["Rags"])],
            "RagsTip": [(8, (-20, 0, 0)), (12, (26, 0, 0)), (17, RISEN["RagsTip"])],
            "Lantern": [(6, (-10, 0, 0)), (10, (24, 0, 0)), (14, (-8, 0, 0)), (17, RISEN["Lantern"])],
        })),
        # Spit (0.3 s; the glob leaves at 35%, between frames 3 and 4): the head draws back a last time, then the chest
        # whips forward and the head thrusts out after it (furthest at frame 4); the arms come down after the body and
        # the hands slap onto the edge before it (from there on, they hold it), the hair flung forward over the face;
        # it comes to rest leaning on the edge (the Clutch, which follows).
        "Spit": clip("Spit", 7, keys(held(RISEN, 1), held(CLUTCH, 8), {
            "Root": [(3, T(0, -0.05, 0.08)), (5, T(0, -0.1, 0.02))],
            "Pivot": [(3, (2, 0, 0)), (5, (11, 0, 0))],
            "Spine": [(3, (4, 0, 0)), (5, (7, 0, 0))],
            "Chest": [(2, (-16, 0, 0)), (4, (5, 0, 0)), (6, (3, 0, 0))],
            "Head": [(2, (-40, 0, 0)), (4, (-2, 0, 0)), (6, (-20, 0, 0))],
            **arms({
                "Arm": [(1, RISEN["ArmL"]), (3, (-140, -18, 0)), (5, (-70, 8, 0))],
                "Fore": [(1, RISEN["ForeL"]), (3, (-20, 0, 0)), (5, (40, 0, 0))],
                "Hand": [(1, RISEN["HandL"]), (4, (-30, 0, 0)), (6, (20, 0, 0))],
            }),
            "IkL": [(1, W(0)), (4, W(0)), (6, W(1))], "IkR": [(1, W(0)), (4, W(0)), (6, W(1))],
            "TargetL": [(1, EDGE_L)], "TargetR": [(1, EDGE_R)],
            "HairL": [(3, (38, 14, 0)), (5, (-24, 8, 0)), (7, (14, 12, 0))],
            "HairR": [(3, (38, -14, 0)), (5, (-24, -8, 0)), (7, (14, -12, 0))],
            "Fringe": [(3, (40, 0, 0)), (5, (-40, 0, 0)), (7, (18, 0, 0))],
            "Rags": [(4, (-14, 0, 0)), (7, (10, 0, 0))],
            "RagsTip": [(5, (-20, 0, 0))],
            "Lantern": [(4, (-20, 0, 0)), (6, (16, 0, 0))],
        })),
        # Clutch (1.2 s loop; the recover after a spit, played once, and at the shore, looking for someone hiding):
        # hanging on the edge, bobbing in the water, the hands flat on it. It peers off to its left, a twitch, then
        # off to its right, and back before you (the spit's recover sinks it from about frame 29: it's facing you by
        # then); its chest heaves; the left hand lifts and slaps down again, then the right (claws drumming); the hair
        # swings after its looks.
        "Clutch": clip("Clutch", 35, keys(held({k: v for k, v in CLUTCH.items() if not k.startswith(("Target", "Palm"))}, 1), {
            "Root": [(1, CLUTCH["Root"]), (9, T(0, -0.05, 0.03)), (18, T(0, -0.03, -0.015)), (27, T(0, -0.05, 0.025))],
            "Pivot": [(1, (14, 0, 0)), (10, (17, -2, 0)), (20, (12, 2, 0)), (29, (16, 0, 0))],
            "Spine": [(1, (4, 0, 0)), (12, (6, -2, -3)), (24, (3, 2, 4))],
            "Chest": [(1, (6, 0, 0)), (6, (10, 0, 0)), (14, (3, 0, -4)), (22, (9, 0, 0)), (30, (4, 0, 2))],
            "Head": [(1, (-30, 0, 0)), (6, (-28, 0, -34)), (11, (-33, 2, -38)), (14, (-22, -10, -10)), (19, (-32, 0, 34)),
                     (23, (-30, -3, 38)), (28, (-30, 0, 0))],
            "HairL": [(1, (8, 10, 0)), (9, (4, 18, 10)), (14, (14, 4, 14)), (17, (2, 16, -6)), (24, (10, 6, -14)),
                      (31, (6, 12, 4))],
            "HairR": [(1, (8, -10, 0)), (10, (2, -6, 12)), (15, (14, -18, 8)), (18, (4, -8, -8)), (25, (12, -16, -12)),
                      (32, (6, -10, 2))],
            "Fringe": [(1, (10, 0, 0)), (8, (8, 0, 12)), (15, (2, 0, -6)), (21, (12, 0, -10)), (26, (6, 0, -14)), (32, (12, 0, 2))],
            "TargetL": [(1, EDGE_L), (11, EDGE_L), (14, T(-0.34, -0.66, 0.76)), (17, T(-0.31, -0.73, 0.53))],
            "PalmL": [(1, CLUTCH["PalmL"]), (11, CLUTCH["PalmL"]), (14, (-20, 0, -12)), (17, (-82, 0, -12)), (20, CLUTCH["PalmL"])],
            "TargetR": [(1, EDGE_R), (18, EDGE_R), (21, T(0.34, -0.66, 0.76)), (24, T(0.3, -0.72, 0.53))],
            "PalmR": [(1, CLUTCH["PalmR"]), (18, CLUTCH["PalmR"]), (21, (-20, 0, 12)), (24, (-82, 0, 12)), (27, CLUTCH["PalmR"])],
            "Rags": [(1, (4, 0, 0)), (12, (-4, 0, 4)), (26, (10, 0, -4))],
            "RagsTip": [(1, (2, 0, 0)), (16, (-8, 0, 6)), (30, (12, 0, -4))],
            "Lantern": [(1, (0, 0, 0)), (11, (6, 4, 0)), (20, (-4, -6, 0)), (29, (2, 2, 0))],
        }), periodic=True),
        # Sink (0.6 s, or 2 s knocked under; the game takes it under in the first half): it lets go of the edge and
        # falls back, the face turned up, the mouth to the sky; it goes down and its arms are left trailing above it,
        # the hair floating up about the head; then it rolls forward under the water into the Swim's glide (the Swim
        # follows it in play: after a spit, knocked under, back from the shore).
        "Sink": clip("Sink", 14, keys(held({k: v for k, v in CLUTCH.items() if not k.startswith("Ik")}, 1), held(swim_start, 15), {
            "IkL": [(1, W(1)), (4, W(0))], "IkR": [(1, W(1)), (4, W(0))],
            "Root": [(3, T(0, 0.06, 0.04)), (7, T(0, 0.1, -0.1))],
            "Pivot": [(3, (6, 0, 0)), (7, (-16, 0, 0)), (11, (22, 0, 0))],
            "Spine": [(4, (0, 0, 0)), (8, (-8, 0, 0))],
            "Chest": [(4, (0, 0, 0)), (8, (-10, 0, 0))],
            "Head": [(3, (-36, 0, 0)), (7, (-50, 0, 0)), (11, (-30, 0, 0))],
            **arms({
                "Arm": [(1, (-80, 10, 0)), (4, (-110, -6, 0)), (8, (-152, -24, 0)), (12, (-40, 16, 0))],
                "Fore": [(1, (60, 0, 0)), (5, (10, 0, 0)), (9, (-30, 0, 0)), (13, (10, 0, 0))],
                "Hand": [(1, (0, 0, 0)), (6, (30, 0, 0)), (10, (-40, 0, 0)), (14, (0, 0, 0))],
            }),
            "HairL": [(4, (0, 20, 0)), (8, (-30, 30, 0)), (12, (6, 20, 0))],
            "HairR": [(4, (0, -20, 0)), (8, (-30, -30, 0)), (12, (6, -20, 0))],
            "Fringe": [(4, (10, 0, 0)), (8, (-46, 0, 0)), (12, (-6, 0, 0))],
            "Rags": [(5, (-20, 0, 0)), (10, (20, 0, 0))],
            "RagsTip": [(7, (-24, 0, 0)), (12, (20, 0, 0))],
            "Lantern": [(5, (-20, 0, 0)), (9, (14, 0, 0))],
        })),
        # Stranded (0.57 s loop; hooked out onto dry ground): flopping on its belly like a landed fish. It arches (the
        # head and the shoulders up, the rags' ends lifting off the ground), the hands lifting, then slaps down (the
        # hands slapping flat, the head knocked down after), and lies there a moment, the rags settling.
        "Stranded": clip("Stranded", 17, keys(held({k: v for k, v in LYING.items() if not k.startswith(("Target", "Palm"))}, 1), {
            "Root": [(1, LYING["Root"])],
            "Pivot": [(1, (76, 0, 0)), (6, (68, 0, -4)), (10, (80, 0, 3)), (13, (75, 0, 0))],
            "Spine": [(1, (4, 0, 0)), (6, (-8, 0, 0)), (10, (6, 0, 0)), (14, (3, 0, 0))],
            "Chest": [(1, (-14, 0, 0)), (7, (-26, 0, 0)), (11, (-6, 0, 0)), (15, (-12, 0, 0))],
            "Head": [(1, (-50, 0, 0)), (7, (-64, 0, 6)), (11, (-36, 0, -4)), (14, (-52, 0, 0))],
            "Rags": [(1, (14, 0, 0)), (6, (34, 0, 0)), (10, (6, 0, 0)), (14, (18, 0, 0))],
            "RagsTip": [(1, (6, 0, 0)), (8, (30, 0, 6)), (12, (-4, 0, -4)), (16, (10, 0, 0))],
            "TargetL": [(1, GROUND_L), (6, T(-0.44, -0.56, 0.3)), (10, T(-0.4, -0.64, 0.06)), (17, GROUND_L)],
            "TargetR": [(2, GROUND_R), (7, T(0.45, -0.55, 0.32)), (10, T(0.41, -0.65, 0.06)), (17, GROUND_R)],
            "PalmL": [(1, LYING["PalmL"]), (6, (-40, 0, -10)), (10, (-88, 0, -10)), (17, LYING["PalmL"])],
            "PalmR": [(2, LYING["PalmR"]), (7, (-40, 0, 10)), (10, (-88, 0, 10)), (17, LYING["PalmR"])],
            "HairL": [(1, (8, 14, 0)), (8, (26, 20, 0)), (12, (-8, 10, 0)), (15, (12, 16, 0))],
            "HairR": [(1, (8, -14, 0)), (8, (26, -20, 0)), (12, (-8, -10, 0)), (15, (12, -16, 0))],
            "Fringe": [(1, (2, 0, 0)), (8, (18, 0, 0)), (12, (-12, 0, 0)), (15, (6, 0, 0))],
            "Lantern": [(1, (60, 0, 0)), (9, (70, 0, 10)), (13, (54, 0, -6))],
        }), periodic=True, ground=True),
        # Crawl (0.6 s loop; it moves 0.9 m/s): hauling itself along on its belly, hand over hand: a hand reaches
        # forward, plants, and pulls (it stays where it's planted as the body slides up to it) while the other swings
        # forward; the shoulders roll into each pull, the head bobs after it, the rags drag and swish behind.
        "Crawl": clip("Crawl", 18, over(LYING, {
            "Root": [(1, T(0, 0.3, 0)), (6, T(0, 0.26, 0)), (10, T(0, 0.3, 0)), (15, T(0, 0.26, 0))],
            "Pivot": [(2, (78, 0, 0)), (6, (72, 0, 3)), (11, (78, 0, 0)), (15, (72, 0, -3))],
            "Spine": [(3, (4, -6, 0)), (12, (4, 6, 0))],
            "Chest": [(4, (-14, -6, -4)), (13, (-14, 6, 4))],
            "Head": [(4, (-44, 0, 4)), (8, (-56, 0, 0)), (13, (-44, 0, -4)), (17, (-56, 0, 0))],
            "TargetL": [(1, T(-0.36, -0.66, 0.06)), (4, T(-0.36, -0.57, 0.06)), (7, T(-0.36, -0.48, 0.06)), (10, T(-0.36, -0.39, 0.06)),
                        (14, T(-0.4, -0.56, 0.26))],
            "TargetR": [(10, T(0.36, -0.66, 0.06)), (13, T(0.36, -0.57, 0.06)), (16, T(0.36, -0.48, 0.06)), (1, T(0.36, -0.39, 0.06)),
                        (5, T(0.4, -0.56, 0.26))],
            "PalmL": [(1, (-86, 0, -10)), (10, (-80, 0, -10)), (13, (-30, 0, -10)), (17, (-70, 0, -10))],
            "PalmR": [(10, (-86, 0, 10)), (1, (-80, 0, 10)), (4, (-30, 0, 10)), (8, (-70, 0, 10))],
            "HairL": [(5, (14, 18, 4)), (14, (4, 12, -4))],
            "HairR": [(6, (4, -12, 4)), (15, (14, -18, -4))],
            "Fringe": [(6, (8, 0, 4)), (15, (-4, 0, -4))],
            "Rags": [(4, (14, 0, 8)), (13, (14, 0, -8))],
            "RagsTip": [(7, (6, 0, 14)), (16, (6, 0, -14))],
            "Lantern": [(5, (60, 0, 10)), (14, (60, 0, -10))],
        }), periodic=True, ground=True),
        # Stagger (1 s for a blow; hooked, it's played over the flight and again, slowed to 3 s, once it's landed): a
        # jolt back (the head snapped back, the arms flung out); it reels, swaying to one side and the other, the head
        # lolling after; then it lurches forward and catches itself, the hands slapping down onto the edge before it
        # (the head knocked down), rebounds, and hangs on: it ends on the Clutch's pose, which follows a blow in the
        # water (at the shore it goes on clutching; out in the water it sinks, and the Sink starts there too). Its rags
        # stay folded back behind it (under the water, unseen; on dry ground, so it never sinks into it): on land the
        # Crawl takes over from there.
        "Stagger": clip("Stagger", 17, keys(held(THROWN, 1), held({k: v for k, v in STAGGERED.items() if not k.startswith("Ik")}, 18), {
            "Root": [(8, T(0, 0.12, 0)), (13, T(0, -0.06, 0))],
            "Pivot": [(4, (-18, 4, 0)), (8, (-4, -12, 6)), (11, (36, 4, 0)), (13, (26, 0, 0)), (15, (9, 0, 0))],
            "Spine": [(3, (-8, 0, 0)), (7, (-2, 8, 0)), (11, (10, 0, 0)), (14, (1, 0, 0))],
            "Chest": [(3, (-12, 0, 0)), (8, (4, -8, 0)), (12, (12, 0, 0)), (15, (2, 0, 0))],
            "Head": [(3, (-32, 0, 10)), (6, (-6, 8, -14)), (9, (-24, -10, 16)), (12, (6, 0, 0)), (15, (-38, 0, 0))],
            **arms({
                "Arm": [(1, THROWN["ArmL"]), (4, (-30, 126, 0)), (8, (-90, 60, 0)), (11, (-110, 30, 0))],
                "Fore": [(1, THROWN["ForeL"]), (5, (-50, 0, 0)), (9, (20, 0, 0)), (12, (0, 0, 0))],
                "Hand": [(1, THROWN["HandL"]), (6, (-40, 0, 0)), (10, (30, 0, 0)), (13, (0, 0, 0))],
            }, {
                "Arm": [(1, THROWN["ArmR"]), (5, (-40, -110, 0)), (8, (-60, -90, 0)), (11, (-110, -30, 0))],
                "Fore": [(1, THROWN["ForeR"]), (6, (-40, 0, 0)), (9, (10, 0, 0)), (12, (0, 0, 0))],
                "Hand": [(1, THROWN["HandR"]), (7, (-30, 0, 0)), (10, (30, 0, 0)), (13, (0, 0, 0))],
            }),
            "IkL": [(9, W(0)), (11, W(1))], "IkR": [(10, W(0)), (12, W(1))],
            "TargetL": [(9, T(-0.42, -0.62, 1.0)), (12, T(-0.31, -0.74, 0.5)), (14, T(-0.3, -0.73, 0.55))],
            "TargetR": [(10, T(0.42, -0.6, 1.04)), (13, T(0.31, -0.74, 0.5)), (15, T(0.3, -0.73, 0.55))],
            "PalmL": [(9, (-20, 0, -10)), (12, (-84, 0, -12))], "PalmR": [(10, (-20, 0, 10)), (13, (-84, 0, 12))],
            "HairL": [(4, (44, 26, 0)), (8, (14, 10, 0)), (12, (34, 20, 0)), (15, (-12, 6, 0))],
            "HairR": [(4, (44, -26, 0)), (8, (30, -30, 0)), (12, (34, -10, 0)), (15, (-12, -6, 0))],
            "Fringe": [(4, (40, 0, 0)), (8, (10, 0, 10)), (12, (36, 0, -10)), (15, (-14, 0, 0))],
            "Rags": [(5, (80, 0, 10)), (9, (70, 0, -10)), (11, (58, 0, 0)), (13, (68, 0, 0)), (15, (88, 0, 0))],
            "RagsTip": [(5, (-10, 0, 0)), (9, (20, 0, 10)), (12, (30, 0, 0)), (15, (-6, 0, 0))],
            "Lantern": [(4, (20, 0, 0)), (9, (60, 0, 20)), (13, (-14, 0, 0))],
        }), ground=True),
        # Death (0.8 s; then it dissolves): a jolt back, a convulsion (the back arching, the claws clutching up at the
        # air, the head thrown back), and it collapses forward, face down, limp: the arms flung out flat, the head
        # rolling aside, the hair fanned out, a rebound, still. (On the water: floating face down.)
        "Death": clip("Death", 19, keys(held(THROWN, 1), {
            "Root": [(6, T(0, 0.1, 0)), (14, T(0, 0.34, 0)), (20, T(0, 0.34, 0))],
            "Pivot": [(4, (-16, 0, 0)), (7, (-6, 0, 4)), (12, (86, 0, 8)), (14, (93, 0, 8)), (16, (86, 0, 6)), (20, (90, 0, 6))],
            "Spine": [(4, (-10, 0, 0)), (9, (10, 0, 0)), (13, (6, 0, 0)), (20, (2, 0, 0))],
            "Chest": [(3, (-12, 0, 0)), (6, (-16, 0, 0)), (10, (12, 0, 0)), (14, (-4, 0, 0)), (20, (0, 0, 0))],
            "Head": [(3, (-30, 0, 0)), (6, (-36, 0, 8)), (10, (-10, 0, 0)), (13, (20, 0, 0)), (16, (-6, 8, 30)), (20, (-2, 10, 34))],
            **arms({
                "Arm": [(3, (-130, 20, 0)), (6, (-142, 10, 0)), (10, (-70, 80, 0)), (13, (-30, 100, 0))],
                "Fore": [(4, (40, 0, 0)), (7, (60, 0, 0)), (11, (-20, 0, 0)), (14, (0, 0, 0))],
                "Hand": [(5, (50, 0, 0)), (8, (60, 0, 0)), (12, (-30, 0, 0)), (15, (0, 0, 0))],
            }),
            "IkL": [(9, W(0)), (11, W(1))], "IkR": [(9, W(0)), (11, W(1))],
            "TargetL": [(9, T(-0.42, -0.62, 0.9)), (11, T(-0.62, -0.6, 0.4)), (14, T(-0.7, -0.5, 0.05))],
            "TargetR": [(9, T(0.42, -0.6, 0.86)), (11, T(0.64, -0.56, 0.42)), (14, T(0.72, -0.44, 0.05))],
            "PalmL": [(9, (-20, 0, -30)), (14, (-90, 0, -50))], "PalmR": [(9, (-20, 0, 30)), (14, (-90, 0, 46))],
            "ElbowL": [(1, T(-1, 0.4, 0.6))], "ElbowR": [(1, T(1, 0.4, 0.6))],
            "HairL": [(4, (40, 10, 0)), (8, (-20, 20, 0)), (13, (-20, 30, 0)), (16, (6, 44, 0)), (20, (-4, 40, 0))],
            "HairR": [(4, (40, -10, 0)), (8, (-20, -20, 0)), (13, (-20, -30, 0)), (16, (6, -44, 0)), (20, (-4, -40, 0))],
            "Fringe": [(4, (40, 0, 0)), (8, (-20, 0, 0)), (13, (-24, 0, 0)), (16, (6, 0, 0)), (20, (-6, 0, 0))],
            "Rags": [(5, (80, 0, 0)), (10, (60, 0, 0)), (14, (4, 0, 6)), (17, (-10, 0, 8)), (20, (-6, 0, 8))],
            "RagsTip": [(6, (-10, 0, 0)), (12, (10, 0, 0)), (16, (8, 0, 10)), (20, (-4, 0, 12))],
            "Lantern": [(5, (30, 0, 0)), (13, (90, 0, 20)), (17, (70, 0, 30)), (20, (76, 0, 30))],
        }), ground=True),
    }


def build():
    skin, dark = kit.mat("DrownedSkin", "#7d8c82"), kit.mat("DrownedDark", "#4f5d56")
    rags, weed = kit.mat("DrownedRags", "#3e4a45"), kit.mat("DrownedWeed", "#2c3a2a")
    mouth, eye, hand = kit.mat("Ink", "#1a1622"), kit.mat("SouredEye_Glow", "#b6f06a"), kit.mat("DrownedHand", "#a7b3a6")
    metal = kit.mat("SouredLanternMetal", "#2c2b31")
    rng = kit.rng

    def weigh(ob, fn):
        """Skin `ob` vertex by vertex: fn(index, rest position) -> {bone: weight}."""
        groups = {}
        for v in ob.data.vertices:
            for bone, w in fn(v.index, v.co).items():
                if w > 1e-4:
                    if bone not in groups:
                        groups[bone] = ob.vertex_groups.new(name=bone)
                    groups[bone].add([v.index], w, "REPLACE")
        return ob

    def ramp(x, a, b):
        return max(0.0, min(1.0, (x - a) / (b - a)))

    def bloated(bm):
        kit.sphere(10, 8)(bm)
        for v in bm.verts:
            v.co *= 1.0 + rng.uniform(-0.04, 0.05)

    def rag_skirt(bm):
        kit.cone(10, 0.3, 0.2, 0.9)(bm)
        for v in bm.verts:
            if v.co.z < -0.3:
                v.co.x *= 1 + rng.uniform(-0.15, 0.15)
                v.co.y *= 1 + rng.uniform(-0.15, 0.15)
                v.co.z += rng.uniform(-0.1, 0.08)
        # (cut into rings down its length, so it bends where its two bones meet; the shape is the same)
        bmesh.ops.subdivide_edges(bm, edges=[e for e in bm.edges if abs(e.verts[0].co.z - e.verts[1].co.z) > 0.3],
                                  cuts=7, use_grid_fill=False)

    def rag_weights(i, co):
        # the waist with the body, then the rags in two, bending smoothly between
        t = ramp(-co.z, -0.02, 0.14)
        tip = ramp(-co.z, 0.32, 0.52)
        return {"Pivot": 1 - t, "Rags": t * (1 - tip), "RagsTip": t * tip}

    parts = [
        weigh(part("Rags", rag_skirt, rags, M((0, 0, -0.42)), smooth=True), rag_weights),
        weigh(part("Torso", bloated, skin, M((0, -0.01, 0.36), scale=(0.29, 0.25, 0.36)), smooth=True),
              lambda i, co: {"Spine": 1 - ramp(co.z, 0.26, 0.5), "Chest": ramp(co.z, 0.26, 0.5)}),
        part("Shoulders", bloated, dark, M((0, 0.03, 0.6), scale=(0.3, 0.17, 0.1)), smooth=True, group="Chest"),
        part("Neck", kit.cylinder(8, 0.08, 0.14), dark, M((0, -0.04, 0.7)), smooth=True, group="Head"),
        part("Head", bloated, skin, M((0, -0.07, 0.84), scale=(0.15, 0.15, 0.17)), smooth=True, group="Head"),
        part("Mouth", kit.sphere(8, 6), mouth, M((0, -0.2, 0.78), scale=(0.05, 0.03, 0.07)), smooth=True, group="Head"),
    ]
    for s in (-1, 1):
        parts.append(part("Eye", kit.sphere(8, 6, 0.024), eye, M((s * 0.055, -0.205, 0.87)), smooth=True, group="Head"))
    # Long lank hair, like waterweed: strands from the crown, hanging over the face and down the back. Each strand's
    # root is on the head; down its length it goes over to its lock (left, right, or the fringe over the face).
    parts.append(part("Scalp", kit.sphere(10, 7), weed, M((0, -0.05, 0.9), scale=(0.155, 0.155, 0.12)), smooth=True, group="Head"))
    for k in range(22):
        a = math.pi * 2 * k / 22
        front = math.sin(a) < -0.3
        top = (0.11 * math.cos(a), -0.06 + 0.11 * math.sin(a), 0.95)
        drop = 0.4 if front else 0.62  # (a little shorter over the face, parted: the eyes glow through)
        side = 0.035 if front else 0.0
        mid = (0.16 * math.cos(a) + math.copysign(side, math.cos(a)), -0.07 + 0.17 * math.sin(a), 0.9 - drop * 0.35)
        end = (0.15 * math.cos(a) + rng.uniform(-0.03, 0.03), -0.07 + 0.19 * math.sin(a) + rng.uniform(-0.02, 0.02), 0.95 - drop)
        lock = "Fringe" if front else ("HairL" if math.cos(a) < 0 else "HairR")
        parts.append(weigh(part("Weed", kit.tube([top, mid, end], [0.034, 0.028, 0.012], seg=5), weed),
                           lambda i, co, lock=lock, drop=drop: {"Head": 1 - ramp((0.95 - co.z) / drop, 0.08, 0.6),
                                                                lock: ramp((0.95 - co.z) / drop, 0.08, 0.6)}))
    for s, side in ((-1, "L"), (1, "R")):
        shoulder, elbow, wrist = (s * 0.24, -0.02, SHOULDER_Z), (s * 0.28, -0.05, 0.34), (s * 0.3, -0.06, 0.06)
        # The arm as before (a tube shoulder, elbow, wrist), in more rings, so it bends soft at the elbow.
        path, radii = [], []
        for (a, ra), (b, rb) in (((shoulder, 0.07), (elbow, 0.055)), ((elbow, 0.055), (wrist, 0.045))):
            for k in range(3):
                path.append(tuple(a[c] + (b[c] - a[c]) * k / 3 for c in range(3)))
                radii.append(ra + (rb - ra) * k / 3)
        path.append(wrist)
        radii.append(0.045)
        parts.append(weigh(part("Arm", kit.tube(path, radii, seg=7), skin),
                           lambda i, co, side=side: {"Arm" + side: 1 - ramp(i // 7, 2, 4), "Fore" + side: ramp(i // 7, 2, 4)}))
        parts.append(part("Hand", bloated, hand, M((s * 0.3, -0.07, 0.0), scale=(0.07, 0.05, 0.09)), smooth=True, group="Hand" + side))
        for k in (-1, 0, 1):
            base = (s * 0.3 + k * 0.03, -0.08, -0.06)
            parts.append(part("Finger", kit.cone(4, 0.016, 0.006, 0.11), hand, kit.along(base, (base[0], base[1] - 0.02, base[2] - 0.1)),
                              group="Hand" + side))
    # Its belt lantern, drowned: still faintly lit, at the waterline.
    lantern_glass = kit.mat("SouredLantern_Glow", "#b6f06a")
    parts += [
        part("BeltLanternCord", kit.cube(), metal, M((0.24, 0.0, 0.14), rot=(0, -15, 0), scale=(0.012, 0.012, 0.1)), group="Lantern"),
        part("BeltLanternTop", kit.cone(4, 0.06, 0.01, 0.05), metal, M((0.25, 0.0, 0.07), rot=(0, 0, 45)), group="Lantern"),
        part("BeltLanternGlass", kit.cube(), lantern_glass, M((0.25, 0.0, 0.0), scale=(0.075, 0.075, 0.1)), group="Lantern"),
        part("BeltLanternBase", kit.cube(), metal, M((0.25, 0.0, -0.06), scale=(0.09, 0.09, 0.015)), group="Lantern"),
    ]
    mesh = kit.join(parts, "Drowned")
    names = {g.index: g.name for g in mesh.vertex_groups}
    for v in mesh.data.vertices:
        ws = [(names[g.group], g.weight) for g in v.groups if g.weight > 1e-4]
        total = sum(w for _, w in ws)
        VERTS.append((v.co.copy(), [(b, w / total) for b, w in ws]))
    armature = kit.rig("DrownedRig", BONES)
    kit.skin(mesh, armature)
    kit.animate(armature, actions())
    for name, worst in REACH.items():
        if worst > 0.02:
            print(f"[drowned] {name}: the hands come off what they hold by up to {worst * 100:.0f} cm")
    return [armature, mesh]
