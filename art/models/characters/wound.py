"""A Wound (the Hall of the Last Hour): one of the household's hounds, given more time a little at a time. A lean grey
sighthound, its flanks opened on brass wheels where the ribs were, iron legs; a brass collar, and hung from it, like
every Soured's, a small green lantern; the Soured's green eyes. In its back a wind-up key (a toy's, two round wings),
on its own bone (Key), which the game turns: spinning as it runs, slowing as it runs down, cranked round as it winds.

Clips (30 fps): Idle (2 s, standing, looking round), Run (0.5 s: a gallop), Walk (1 s: a trot, two strides), Crouch
(0.3 s: the bite's tell, head low, haunches gathered), Bite (0.2 s: lunge and snap), BiteRecover (0.3 s), Gather (0.45 s:
the pounce's tell, flat to the floor), Pounce (0.45 s: stretched out in the air), PounceRecover (0.7 s), RunDown (1 s: it
sags, head dropping, onto its haunches), Wind (2.4 s: sat, shuddering as the key's cranked round), Stagger, Death (onto
its side).

How it's animated (the Laden's way: see laden.py): each bone keyed on its own timing (kit.bake: tracks), so nothing moves
all at once. The body leads (its front half, Body, and its haunches, Hips, flexing about the waist: the spine arches
and stretches in the gallop); the neck and head follow a few frames late; the ears, the tail (two bones), the jaw and
the lantern on its collar lag and overshoot. The legs aren't keyed: each paw is keyed where it is on the floor (a Foot
track: its place and its tilt), and every frame the leg is bent onto it (kit.reach), so planted paws stay planted; in
the gallop and the trot the paws are placed by the gait (gait()), moving back under the body at the speed the game moves
it, so they don't slide. It's clockwork: it moves in small ticks and holds (the head turns in steps, the key turns in
jerks as it runs down, it shudders as it's wound), but it's a hound.
"""
import math

from mathutils import Euler, Vector

M, part = kit.M, kit.part  # `kit` is injected by build_model.py

BACK = 0.6
WAIST = (0, 0.05, BACK - 0.03)  # where the spine bends: the front half (Body) and the haunches (Hips) turn about it
LEGS = ("FL", "FR", "BL", "BR")
# Each leg: its top, its joint (the knuckle), its ankle (on the floor at rest), its toe.
LEG = {}
for _leg, _x, _y0, _y1 in (("FL", -0.09, -0.27, -0.3), ("FR", 0.09, -0.27, -0.3), ("BL", -0.09, 0.33, 0.38), ("BR", 0.09, 0.33, 0.38)):
    _top, _foot = (_x, _y0, BACK - 0.09), (_x, _y1, 0.04)
    _knee = (_x, (_y0 + _y1) / 2 + (0.03 if _y0 > 0 else -0.02), 0.3)
    LEG[_leg] = (_top, _knee, _foot, (_x, _y1 - 0.08, 0.03))
FOOT = {leg: LEG[leg][2] for leg in LEGS}
POLE = {"FL": -1.0, "FR": -1.0, "BL": 1.0, "BR": 1.0}  # the joint bows forward on a foreleg (a wrist), back on a hind (a hock)

BONES = {
    "Root": ((0, 0, 0), (0, 0, 0.2), None),
    "Body": (WAIST, (0, -0.33, BACK), "Root"),                 # the front half: chest, wheels, shoulders
    "Hips": (WAIST, (0, 0.42, BACK - 0.04), "Body"),           # the haunches (the spine flexes between)
    "Neck": ((0, -0.33, BACK + 0.02), (0, -0.5, BACK + 0.22), "Body"),
    "Head": ((0, -0.5, BACK + 0.22), (0, -0.82, BACK + 0.16), "Neck"),
    "Jaw": ((0, -0.56, BACK + 0.15), (0, -0.8, BACK + 0.09), "Head"),
    "EarL": ((-0.04, -0.5, BACK + 0.28), (-0.07, -0.4, BACK + 0.31), "Head"),
    "EarR": ((0.04, -0.5, BACK + 0.28), (0.07, -0.4, BACK + 0.31), "Head"),
    "Lantern": ((0, -0.44, BACK - 0.0), (0, -0.44, BACK - 0.13), "Neck"),  # on its collar, swinging
    "Tail": ((0, 0.42, BACK - 0.02), (0, 0.6, BACK - 0.04), "Hips"),
    "Tail2": ((0, 0.6, BACK - 0.04), (0, 0.78, BACK - 0.14), "Tail"),
    "Key": ((0, 0.04, BACK + 0.12), (0, 0.04, BACK + 0.42), "Body"),
}
for _leg in LEGS:
    _top, _knee, _foot, _toe = LEG[_leg]
    BONES["Leg" + _leg] = (_top, _knee, "Body" if _leg[0] == "F" else "Hips")
    BONES["Shin" + _leg] = (_knee, _foot, "Leg" + _leg)
    BONES["Paw" + _leg] = (_foot, _toe, "Shin" + _leg)


# ---------------------------------------------------------------- paws on the floor

def foot(leg, dy=0.0, dz=0.0, pitch=0.0, dx=0.0, pole=None, carry=0.0):
    """A paw's key: where its ankle is (moved from where it stands at rest), the paw tilted `pitch` (+: toes down, the
    heel up; a foreleg's paw flicked back is ~+120), the leg's joint bowed `pole` (-1 forward, +1 back). On the floor
    (`carry` 0) or, off it, carried by the body (1: the move is from where the paw hangs from the shoulder or hip as the
    body goes: a leap's legs reaching and trailing)."""
    x, y, z = FOOT[leg]
    return ((pitch, POLE[leg] if pole is None else pole, carry), (x + dx, y + dy, z + dz))


def air(leg, dy=0.0, dz=0.0, pitch=0.0, dx=0.0, pole=None):
    """A paw's key off the floor, carried by the body (see foot)."""
    return foot(leg, dy, dz, pitch, dx, pole, 1.0)


def feet(**moved):
    """Every paw's key: planted where it stands, unless moved (leg=(dy, dz, pitch[, dx[, pole]]))."""
    return {"Foot" + leg: foot(leg, *moved.get(leg, ())) for leg in LEGS}


def paw_clear(pitch):
    """How high the ankle must be for the paw (tilted `pitch`) to clear the floor."""
    t = math.radians(pitch)
    cz = -0.03 * math.sin(t) - 0.01 * math.cos(t)
    return -cz + math.sqrt((0.06 * math.sin(t)) ** 2 + (0.03 * math.cos(t)) ** 2)


def ease(points, s):
    """A smooth curve through (s, value) points."""
    for (s0, v0), (s1, v1) in zip(points, points[1:]):
        if s <= s1:
            t = (s - s0) / max(1e-6, s1 - s0)
            return v0 + (v1 - v0) * (0.5 - 0.5 * math.cos(math.pi * t))
    return points[-1][1]


FLICK_FORE = [(0, 0), (0.15, 70), (0.4, 125), (0.7, 40), (0.88, -12), (1, 0)]   # a foreleg's paw curled back under, then reaching
FLICK_HIND = [(0, 0), (0.2, 55), (0.45, 70), (0.75, 12), (0.9, -8), (1, 0)]


def gait(length, cycle, speed, plan):
    """The paws of a loop, keyed every frame: each paw planted from `touchdown` to `liftoff` (frames; the floor moving
    back under the body at `speed` metres a frame, the game's speed, so it doesn't slide), then swung forward (lifted
    `lift`, the paw flicked) to land `ahead` of where it stands at rest. plan: {leg: (touchdown, liftoff, ahead, lift)}."""
    tracks = {}
    for leg, (td, lo, ahead, lift) in plan.items():
        x0, y0, z0 = FOOT[leg]
        stance, swing = lo - td, cycle - (lo - td)
        y_td = y0 - ahead
        y_lo = y_td + speed * stance
        m0, m1 = 0.5 * speed * swing, 0.7 * speed * swing  # (leaving and landing going with the floor, near enough)
        keys = []
        for f in range(1, length + 1):
            u = (f - td) % cycle
            if u <= stance:
                keys.append((f, foot(leg, y_td + speed * u - y0)))
                continue
            s = (u - stance) / swing
            y = ((2 * s ** 3 - 3 * s ** 2 + 1) * y_lo + (s ** 3 - 2 * s ** 2 + s) * m0 + (-2 * s ** 3 + 3 * s ** 2) * y_td
                 + (s ** 3 - s ** 2) * m1)
            z = lift * math.sin(math.pi * s ** 0.8)
            keys.append((f, foot(leg, y - y0, z, ease(FLICK_FORE if leg[0] == "F" else FLICK_HIND, s))))
        tracks["Foot" + leg] = keys
    return tracks


REACH = {}  # (the build's report: how far each clip's planted paws come off the floor where they're keyed, at worst)


def clip(name, length, tracks, periodic=False):
    """A clip `length` frames long (30 a second) from per-bone tracks, baked to every frame, every leg bent onto its paw's
    key (in the Root's frame: the floor, unless the whole hound is rolled over), the paw tilted as keyed; the lantern
    keyed by how it swings in the world (0: hanging straight down)."""
    frames = []
    worst = 0.0
    for f, pose in kit.bake(tracks, length, periodic):
        targets = {leg: pose.pop("Foot" + leg) for leg in LEGS if "Foot" + leg in pose}
        body = kit.pose_matrices(BONES, pose)
        root = body["Root"]
        for leg, ((pitch, pole, carry), at) in targets.items():
            at = Vector(at)
            at.z = max(at.z, paw_clear(pitch))
            hip = body["Body" if leg[0] == "F" else "Hips"]
            goal = (root @ at).lerp(hip @ at, carry)
            turn = root.to_quaternion().slerp(hip.to_quaternion(), carry)
            # (a joint bowing forward turned to bow back swings round the outside, so it never snaps through)
            pole = max(-1.0, min(1.0, pole))
            bend = turn @ Vector((math.copysign(math.sqrt(1 - pole * pole), FOOT[leg][0]), pole, 0))
            pose = kit.reach(BONES, pose, "Leg" + leg, "Shin" + leg, lambda m, g=goal: g, tuple(bend))
            mats = kit.pose_matrices(BONES, pose)
            shin = mats["Shin" + leg].to_quaternion()
            want = turn @ Euler((math.radians(pitch), 0, 0)).to_quaternion()
            pose["Paw" + leg] = tuple(math.degrees(a) for a in (shin.inverted() @ want).to_euler("XYZ"))
            if carry < 0.5 and at.z <= paw_clear(pitch) + 0.01:  # (a paw on the floor: one in the air may stretch its leg straight)
                worst = max(worst, (mats["Shin" + leg] @ Vector(FOOT[leg]) - goal).length)
        if "Lantern" in pose:
            # (keyed as it swings in the world, hanging straight down at 0: turned here into a turn from the collar)
            swing = pose["Lantern"][0]
            neck = kit.pose_matrices(BONES, pose)["Neck"].to_quaternion()
            want = Euler([math.radians(a) for a in swing]).to_quaternion()
            pose["Lantern"] = tuple(math.degrees(a) for a in (neck.inverted() @ want).to_euler("XYZ"))
        frames.append((f, pose))
    REACH[name] = worst
    return ("LINEAR", frames)


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


def ears(frame, lift, out=0.0, droop=0.0):
    """Both ears keyed at `frame`: lifted (+) or laid flat (-), turned out to the sides, drooped (rolled down)."""
    return {"EarL": [(frame, (lift, droop, out))], "EarR": [(frame, (lift, -droop, -out))]}


def mirror(value):
    """A rotation (or rotation and offset) mirrored left to right."""
    if isinstance(value[0], (tuple, list)):
        (rx, ry, rz), (dx, dy, dz) = value
        return ((rx, -ry, -rz), (-dx, dy, dz))
    rx, ry, rz = value
    return (rx, -ry, -rz)


# ---------------------------------------------------------------- the poses it starts from and comes back to

# STAND (unaware): standing easy, head up, ears half up, tail hanging.
STAND = {"Root": (0, 0, 0), "Body": ((0, 0, 0), (0, 0, -0.015)), "Hips": (0, 0, 0), "Neck": (0, 0, 0), "Head": (0, 0, 0),
         "Jaw": (0, 0, 0), "EarL": (14, 0, 0), "EarR": (14, 0, 0), "Lantern": (0, 0, 0), "Tail": (-12, 0, 0), "Tail2": (-6, 0, 0),
         "Key": (0, 0, 0), **feet()}
# READY (hunting): a little lower, neck out, ears back, tail level: what the recoveries end on, what the tells start from
# (the gallop's first frame is close: its legs gathered under it).
READY = {"Root": (0, 0, 0), "Body": ((1, 0, 0), (0, 0, -0.035)), "Hips": (-3, 0, 0), "Neck": (16, 0, 0), "Head": (-8, 0, 0),
         "Jaw": (3, 0, 0), "EarL": (-8, 0, 4), "EarR": (-8, 0, -4), "Lantern": (0, 0, 0), "Tail": (4, 0, 0), "Tail2": (-4, 0, 0),
         "Key": (0, 0, 0), **feet()}
# CROUCHED (the bite's tell): sunk back on its haunches, chest low, head low and forward, lips drawn, ears flat.
CROUCHED = {"Root": (0, 0, 0), "Body": ((7, 0, 0), (0, 0.07, -0.1)), "Hips": (-12, 0, 0), "Neck": (28, 0, 0), "Head": (-10, 0, 0),
            "Jaw": (14, 0, 0), "EarL": (-24, 0, 6), "EarR": (-24, 0, -6), "Lantern": (-6, 0, 0), "Tail": (-26, 0, 0), "Tail2": (-10, 0, 0),
            "Key": (0, 0, 0), **feet(FL=(0, 0, 0, 0, 1), FR=(0, 0, 0, 0, 1))}
# BITTEN (the bite's end): stretched forward, low, jaws shut on you, the forepaws landed ahead.
BITTEN = {"Root": (0, 0, 0), "Body": ((4, 0, 0), (0, -0.15, -0.075)), "Hips": (-2, 0, 0), "Neck": (8, 0, 0), "Head": (2, 0, 0),
          "Jaw": (0, 0, 0), "EarL": (-26, 0, 4), "EarR": (-26, 0, -4), "Lantern": (-10, 0, 0), "Tail": (-6, 0, 0), "Tail2": (-2, 0, 0),
          "Key": (0, 0, 0), **feet(FL=(-0.2,), FR=(-0.22,), BL=(-0.04,), BR=(-0.02,))}
# FLAT (the pounce's tell): flat to the floor, elbows out behind, haunches bunched, head low, eyes on you.
FLAT = {"Root": (0, 0, 0), "Body": ((5, 0, 0), (0, 0.1, -0.2)), "Hips": (-10, 0, 0), "Neck": (30, 0, 0), "Head": (-10, 0, 0),
        "Jaw": (6, 0, 0), "EarL": (-28, 0, 4), "EarR": (-28, 0, -4), "Lantern": (-6, 0, 0), "Tail": (-24, 0, 0), "Tail2": (-6, 0, 0),
        "Key": (0, 0, 0), **feet(FL=(0, 0, 0, 0, 1), FR=(0, 0, 0, 0, 1))}
# LANDED (the pounce's end): come down on its forepaws, chest low, the haunches still up behind, jaws shut.
LANDED = {"Root": (0, 0, 0), "Body": ((10, 0, 0), (0, -0.18, -0.12)), "Hips": (-2, 0, 0), "Neck": (12, 0, 0), "Head": (0, 0, 0),
          "Jaw": (0, 0, 0), "EarL": (-20, 0, 6), "EarR": (-20, 0, -6), "Lantern": (-14, 0, 0), "Tail": (4, 0, 0), "Tail2": (8, 0, 0),
          "Key": (0, 0, 0), **feet(FL=(-0.36,), FR=(-0.32,), BL=(-0.02,), BR=(0.02,))}
# SAGGED (run down): sat on its haunches, forelegs braced, head hanging, ears down, tail on the floor; the key stopped.
SAGGED = {"Root": (0, 0, 0), "Body": ((-24, 0, 0), (0, 0.1, -0.15)), "Hips": (-16, 0, 0), "Neck": (26, 0, 0), "Head": (32, 0, 0),
          "Jaw": (8, 0, 0), "EarL": (-14, 18, -6), "EarR": (-14, -18, 6), "Lantern": (4, 0, 0), "Tail": (36, 0, 26), "Tail2": (2, 0, 24),
          "Key": (0, 0, 180), **feet(FL=(0.1,), FR=(0.1,), BL=(-0.14,), BR=(-0.14,))}
SAT_UP = dict(SAGGED, Neck=(14, 0, 0), Head=(0, 0, 0), Jaw=(0, 0, 0), EarL=(4, 0, 0), EarR=(4, 0, 0), Key=(0, 0, 0))


def jolt(frame, base, up, settle):
    """A clockwork jolt of the body at `frame`: from `base` up a tick in one frame, settling to `settle` two frames on."""
    return [(frame, base), (frame + 1, up), (frame + 3, settle)]


def roll(tracks, by, length):
    """A loop's tracks turned to start `by` frames later (its frame 1+by becomes frame 1)."""
    return {bone: [((f - 1 - by) % length + 1, v) for f, v in ks] for bone, ks in tracks.items()}


# Run (0.5 s loop, the game moving it 5 m/s): a double-suspension gallop. Keyed from the gathered flight (legs bunched
# under it, its back arched: frame 1 as keyed); the hind paws land (left, then right: frames 2 and 3), drive, and it
# springs into the stretched flight (frames 7-8: back flat, forelegs reaching, hinds trailing); the forepaws land (right,
# left) and pull it over them, the back arching again. The body drops onto each pair of paws and lifts off them; the
# head stays steady on its long neck (it counters the body's pitch, a little late); the ears flap after each landing;
# the tail streams behind, rising and falling after the haunches; the jaw opens a little on each stride; the lantern
# swings. Then turned (roll) so the clip starts on the hind paws driving, the forepaws curled up off the floor (frame 3
# as keyed): how a hound breaks into a gallop from a stand, and the pose every clip that hands back to the chase ends on.
RUN = roll(keys(gait(15, 15, 5.0 / 30, {
    "BL": (1.8, 4.6, 0.2, 0.15), "BR": (2.8, 5.6, 0.17, 0.15), "FR": (8.3, 11.0, 0.22, 0.24), "FL": (9.3, 12.0, 0.2, 0.24),
    }), {
        "Body": [(1, ((3, 0, 0), (0, 0.0, -0.03))), (4, ((-5, 1, 0), (0, -0.015, -0.065))), (7, ((-6, 0, 0), (0, -0.01, -0.025))),
                 (9, ((-1, 0, 0), (0, 0.01, -0.03))), (11, ((7, -1, 0), (0, 0.02, -0.065))), (14, ((5, 0, 0), (0, 0.006, -0.035)))],
        "Hips": [(1, (-17, 0, 0)), (4, (-6, 0, 0)), (7, (13, 0, 0)), (9, (14, 0, 0)), (11, (3, 0, 0)), (13, (-13, 0, 0))],
        "Neck": [(2, (28, 0, 0)), (6, (22, 0, 0)), (9, (25, 0, 0)), (13, (31, 0, 0))],
        "Head": [(3, (-15, 0, 0)), (7, (-8, 0, 0)), (10, (-11, 0, 0)), (14, (-19, 0, 0))],
        "Jaw": [(4, (3, 0, 0)), (11, (12, 0, 0))],
        **keys(ears(5, -4, 5), ears(8, -18, 2), ears(12, -6, 5), ears(15, -20, 2)),
        "Lantern": [(4, (14, 0, 0)), (8, (2, 0, 0)), (12, (-16, 0, 0)), (15, (-4, 0, 0))],
        "Tail": [(3, (20, 0, 0)), (6, (14, 0, 3)), (10, (4, 0, 0)), (13, (12, 0, -3))],
        "Tail2": [(5, (10, 0, 0)), (8, (2, 0, 4)), (12, (-6, 0, 0)), (15, (4, 0, -4))],
    }), 2, 15)
RUN_START = dict(kit.bake(RUN, 15, True)[0][1])  # (its frame 1, every bone and paw: where the recoveries end)


ACTIONS = {
    # Idle (2 s loop): stood easy, breathing (the chest rises and sinks twice, the haunches after it), weight shifting
    # from one side to the other. It looks round to its left in three ticks of the head, the neck following each,
    # holds, flicks an ear, then snaps back past the front and settles; cocks its head; gives two stiff wags of the tail
    # like a pendulum, and clacks its jaw once.
    "Idle": clip("Idle", 60, keys(held(feet(), 1), {
        "Root": [(1, (0, 0, 0))],
        "Body": [(1, ((0, 0, 0), (0, 0, -0.015))), (14, ((-1, 0, 0), (-0.006, 0, -0.005))), (29, ((0, 1, 0), (0.008, 0, -0.018))),
                 (44, ((-1, 1, 0), (0.008, 0, -0.006))), (58, ((0, 0, 0), (-0.004, 0, -0.016)))],
        "Hips": [(1, (0, 0, 0)), (17, (1, 0, 0)), (32, (-1, 0, 0)), (47, (1, 0, 0))],
        "Neck": [(1, (0, 0, 0)), (8, (0, 0, 0)), (11, (0, 0, 4)), (14, (0, 0, 4)), (17, (0, 0, 8)), (20, (0, 0, 8)), (23, (-2, 0, 11)),
                 (32, (-2, 0, 11)), (36, (0, 0, -3)), (40, (0, 0, 0)), (52, (0, 0, 0))],
        "Head": [(1, (0, 0, 0)), (7, (0, 0, 0)), (9, (0, 0, 8)), (13, (0, 0, 8)), (15, (0, 0, 15)), (19, (0, 0, 15)), (21, (-4, 0, 22)),
                 (31, (-4, 0, 22)), (34, (2, 0, -6)), (37, (0, 0, 2)), (40, (0, 0, 0)), (42, (0, 0, 0)), (44, (0, 14, 0)), (49, (0, 14, 0)),
                 (53, (0, -2, 0)), (56, (0, 0, 0))],
        "Jaw": [(1, (0, 0, 0)), (45, (0, 0, 0)), (46, (10, 0, 0)), (48, (0, 0, 0))],
        "EarL": [(1, (14, 0, 0)), (10, (16, 0, 0)), (24, (14, 0, 0)), (26, (34, 0, 10)), (28, (12, 0, -2)), (31, (14, 0, 0)), (35, (6, 0, 0)),
                 (39, (16, 0, 0)), (46, (12, 0, 4)), (52, (14, 0, 0))],
        "EarR": [(1, (14, 0, 0)), (11, (12, 0, 0)), (25, (14, 0, 0)), (36, (6, 0, 0)), (40, (16, 0, 0)), (46, (18, 0, -2)), (52, (14, 0, 0))],
        "Lantern": [(1, (0, 0, 0)), (24, (0, 2, 0)), (36, (-5, -3, 0)), (40, (3, 1, 0)), (45, (0, 0, 0))],
        "Tail": [(1, (-12, 0, 0)), (4, (-11, 0, 0)), (16, (-13, 0, 0)), (38, (-12, 0, 0)), (40, (-8, 0, 14)), (43, (-8, 0, 14)), (45, (-8, 0, -14)),
                 (48, (-8, 0, -14)), (51, (-10, 0, 8)), (54, (-12, 0, 0))],
        "Tail2": [(1, (-6, 0, 0)), (39, (-6, 0, 0)), (41, (-6, 0, -8)), (44, (-4, 0, 10)), (46, (-6, 0, 8)), (49, (-4, 0, -10)), (52, (-6, 0, -4)),
                  (56, (-6, 0, 0))],
    }), periodic=True),
    # Run (0.5 s loop, the game moving it 5 m/s): see RUN.
    "Run": clip("Run", 15, RUN, periodic=True),
    # Walk (1 s loop: two strides of a trot, as at 1.5 m/s; the game doesn't use it: it always runs): the diagonal pairs
    # of paws together (the left fore with the right hind), frame 1 on its passing pose (one pair planted under it, the
    # other passing). It bounces down onto each pair, rolls a touch to the planted side; the head nods after the body, the
    # ears bob after the head, the tail sways from side to side a stride behind.
    "Walk": clip("Walk", 30, keys(gait(30, 15, 1.5 / 30, {
        "FL": (-2.75, 4.75, 0.19, 0.09), "BR": (-2.25, 5.25, 0.19, 0.07), "FR": (4.75, 12.25, 0.19, 0.09), "BL": (5.25, 12.75, 0.19, 0.07),
    }), {
        "Body": [(1, ((0, 1.5, 0), (0, 0, -0.045))), (5, ((-1, 0, 0), (0, 0, -0.03))), (9, ((0, -1.5, 0), (0, 0, -0.045))), (13, ((-1, 0, 0), (0, 0, -0.03))),
                 (16, ((0, 1.5, 0), (0, 0, -0.045))), (20, ((-1, 0, 0), (0, 0, -0.03))), (24, ((0, -1.5, 0), (0, 0, -0.045))), (28, ((-1, 0, 0), (0, 0, -0.03)))],
        "Hips": [(1, (0, -2, 0)), (9, (0, 2, 0)), (16, (0, -2, 0)), (24, (0, 2, 0))],
        "Neck": [(2, (10, 0, -2)), (6, (6, 0, 0)), (10, (10, 0, 2)), (14, (6, 0, 0)), (17, (10, 0, -2)), (21, (6, 0, 0)), (25, (10, 0, 2)), (29, (6, 0, 0))],
        "Head": [(3, (-2, 0, 0)), (7, (-7, 0, 0)), (11, (-2, 0, 0)), (14, (-7, 0, 0)), (18, (-2, 0, 0)), (22, (-7, 0, 0)), (26, (-2, 0, 0)), (29, (-7, 0, 0))],
        "Jaw": [(1, (2, 0, 0)), (16, (5, 0, 0))],
        **keys(ears(4, 4, 2), ears(8, 12, 0), ears(12, 4, 2), ears(15, 12, 0), ears(19, 4, 2), ears(23, 12, 0), ears(27, 4, 2), ears(30, 12, 0)),
        "Lantern": [(3, (6, 0, -3)), (10, (-4, 0, 3)), (18, (6, 0, -3)), (25, (-4, 0, 3))],
        "Tail": [(3, (-4, 0, 9)), (11, (-6, 0, -9)), (18, (-4, 0, 9)), (26, (-6, 0, -9))],
        "Tail2": [(5, (-4, 0, 10)), (13, (-4, 0, -10)), (20, (-4, 0, 10)), (28, (-4, 0, -10))],
    }), periodic=True),
    # Crouch (0.32 s: the bite's tell): it pricks up at you first (a lift of the head), then sinks back onto its haunches,
    # chest dropping, head down and forward, lips drawn back off its teeth, ears flattening last; a little too deep, then
    # held, trembling with the spring.
    "Crouch": clip("Crouch", 9, keys(held(READY, 1), held(CROUCHED, 10), {
        "Body": [(3, ((-2, 0, 0), (0, 0.0, -0.025))), (7, ((8, 0, 0), (0, 0.075, -0.11))), (8, ((7, 0, 0), (0.004, 0.072, -0.104))),
                 (9, ((7, 0, 0), (-0.003, 0.07, -0.1)))],
        "Hips": [(3, (-1, 0, 0)), (7, (-13, 0, 0))],
        "Neck": [(3, (8, 0, 0)), (8, (30, 0, 0))],
        "Head": [(4, (-14, 0, 0)), (8, (-8, 0, 0))],
        "Jaw": [(5, (2, 0, 0)), (8, (17, 0, 0))],
        **keys(ears(3, 10, 0), ears(6, -10, 4), ears(9, -26, 7)),
        "Tail": [(4, (6, 0, 0)), (8, (-28, 0, 0))],
        "Tail2": [(6, (6, 0, 0)), (9, (-12, 0, 0))],
        "Lantern": [(4, (4, 0, 0)), (8, (-6, 0, 0))],
    })),
    # Bite (0.2 s; the jaws shut at 0.6 of it): it springs off its haunches (the game lunges it forward meanwhile): the
    # hind paws drive and trail, the forepaws reach, the head lifts as the jaws gape; the jaws snap shut as the head drives
    # down onto you (the impact: frame 4.6), the forepaws landing; the ears and lantern flung after.
    "Bite": clip("Bite", 6, keys(held(CROUCHED, 1), held(BITTEN, 7), {
        "Body": [(2, ((2, 0, 0), (0, -0.03, -0.075))), (3, ((-6, 0, 0), (0, -0.12, -0.02))), (4, ((-2, 0, 0), (0, -0.16, -0.03))),
                 (5, ((6, 0, 0), (0, -0.19, -0.065))), (6, ((4, 0, 0), (0, -0.16, -0.08)))],
        "Hips": [(2, (-4, 0, 0)), (3, (10, 0, 0)), (5, (5, 0, 0))],
        "Neck": [(2, (20, 0, 0)), (4, (-6, 0, 0)), (5, (10, 0, 0))],
        "Head": [(3, (-18, 0, 0)), (4, (-14, 0, 0)), (5, (8, 0, 0)), (6, (0, 0, 0))],
        "Jaw": [(2, (24, 0, 0)), (4, (44, 0, 0)), (5, (0, 0, 0)), (6, (4, 0, 0))],
        **keys(ears(3, -32, 4), ears(5, -18, 8)),
        "Tail": [(3, (12, 0, 0)), (5, (2, 0, 0))],
        "Tail2": [(4, (14, 0, 0)), (6, (-4, 0, 0))],
        "Lantern": [(3, (24, 0, 0)), (6, (-16, 0, 0))],
        "FootBL": [(2, foot("BL", 0.12, 0, 35)), (3, air("BL", 0.2, 0.07, 65)), (4, air("BL", 0.14, 0.1, 45)), (5, air("BL", 0.06, 0.06, 20))],
        "FootBR": [(2, foot("BR", 0.12, 0, 35)), (3, air("BR", 0.21, 0.06, 65)), (4, air("BR", 0.16, 0.1, 45)), (5, air("BR", 0.08, 0.07, 20))],
        "FootFL": [(2, foot("FL", -0.03, 0.08, 50, 0, 0.3)), (3, air("FL", -0.12, 0.1, 10, 0, -1)), (4, foot("FL", -0.2, 0.07, -10)), (5, foot("FL", -0.2))],
        "FootFR": [(2, foot("FR", -0.02, 0.06, 45, 0, 0.3)), (3, air("FR", -0.11, 0.11, 15, 0, -1)), (4, foot("FR", -0.21, 0.09, -8)), (5, foot("FR", -0.22, 0.01))],
    })),
    # BiteRecover (0.3 s): it worries what it bit (two jerks of the head, the jaws clacking once more), then lets go and
    # backs off onto its feet, the forepaws stepping back under it, and springs off its hind paws into the gallop (it
    # ends on the Run's first frame: the chase follows).
    "BiteRecover": clip("BiteRecover", 9, keys(held(BITTEN, 1), held(RUN_START, 10), {
        "Body": [(3, ((7, 0, 4), (0, -0.15, -0.085))), (5, ((5, 0, -3), (0, -0.12, -0.075))), (8, ((0, 0, 0), (0, -0.02, -0.03)))],
        "Neck": [(2, (10, 6, 4)), (4, (10, -8, -4)), (6, (12, 2, 0))],
        "Head": [(2, (4, -12, 6)), (3, (4, 10, -6)), (5, (0, 0, 0)), (7, (-12, 0, 0))],
        "Jaw": [(4, (0, 0, 0)), (5, (12, 0, 0)), (6, (0, 0, 0)), (8, (6, 0, 0))],
        **keys(ears(3, -20, 12), ears(5, -30, 0), ears(8, -4, 6)),
        "Tail": [(3, (0, 0, 6)), (6, (8, 0, -4))],
        "Tail2": [(4, (2, 0, -8)), (8, (-8, 0, 4))],
        "Lantern": [(3, (-4, 8, 0)), (5, (4, -8, 0)), (8, (6, 0, 0))],
        "FootFL": [(4, foot("FL", -0.2)), (6, foot("FL", -0.08, 0.07, 40))],
        "FootFR": [(5, foot("FR", -0.22)), (7, foot("FR", -0.12, 0.08, 40))],
        "FootBL": [(5, foot("BL", -0.04)), (7, foot("BL", -0.02, 0.04, 20)), (9, foot("BL"))],
        "FootBR": [(4, foot("BR", -0.02)), (6, foot("BR")), (8, foot("BR", -0.08, 0.05, 20))],
    })),
    # Gather (0.45 s: the pounce's tell): a lift (it rears its head, ears up, fixing you), then down flat to the floor,
    # elbows out behind, the haunches bunching up; a touch too low, then it wags its rear end twice like a cat about to
    # spring (stiffly: ticks), the tail twitching after.
    "Gather": clip("Gather", 13, keys(held(READY, 1), held(FLAT, 14), {
        "Body": [(3, ((-4, 0, 0), (0, 0.01, -0.02))), (8, ((6, 0, 0), (0, 0.1, -0.215))), (10, ((5, 0, 0), (0, 0.1, -0.2))),
                 (12, ((5, 0, 0), (0, 0.1, -0.2)))],
        "Hips": [(3, (2, 0, 0)), (8, (-12, 0, 0)), (10, (-10, 0, 9)), (11, (-10, 0, 9)), (12, (-10, 0, -9)), (13, (-10, 0, -9))],
        "Neck": [(3, (2, 0, 0)), (9, (32, 0, 0))],
        "Head": [(4, (-16, 0, 0)), (9, (-8, 0, 0))],
        "Jaw": [(4, (0, 0, 0)), (9, (8, 0, 0))],
        **keys(ears(3, 22, 0), ears(5, 20, 0), ears(10, -30, 6)),
        "Tail": [(4, (10, 0, 0)), (9, (-26, 0, 0)), (11, (-24, 0, -10)), (13, (-24, 0, 10))],
        "Tail2": [(6, (8, 0, 0)), (10, (-8, 0, 0)), (12, (-6, 0, -16))],
        "Lantern": [(4, (6, 0, 0)), (9, (-8, 0, 0)), (11, (-4, 0, 0))],
        "FootFL": [(2, foot("FL")), (4, foot("FL", 0, 0, 0, 0, 1))],
        "FootFR": [(2, foot("FR")), (4, foot("FR", 0, 0, 0, 0, 1))],
    })),
    # Pounce (0.45 s; it lands on you at 0.7 of it): it explodes off the floor (the game flings it forward meanwhile),
    # stretched out flat in the air, forelegs reaching, hinds trailing, the tail streaming, jaws gaping wider all the way;
    # it comes down onto its forepaws and the jaws slam shut as the head drives down (the impact: frame 8.7); the chest
    # buckles over the forelegs and the haunches come down behind.
    "Pounce": clip("Pounce", 11, keys(held(FLAT, 1), held(LANDED, 12), {
        "Body": [(2, ((-10, 0, 0), (0, 0.02, -0.14))), (3, ((-7, 0, 0), (0, -0.1, 0.06))), (5, ((-3, 0, 0), (0, -0.17, 0.26))),
                 (6, ((-1, 0, 0), (0, -0.18, 0.27))), (8, ((7, 0, 0), (0, -0.2, 0.08))), (9, ((14, 0, 0), (0, -0.2, -0.07))),
                 (10, ((12, 0, 0), (0, -0.19, -0.13)))],
        "Hips": [(2, (6, 0, 0)), (4, (13, 0, 0)), (7, (12, 0, 0)), (9, (5, 0, 0))],
        "Neck": [(2, (14, 0, 0)), (4, (-10, 0, 0)), (7, (-6, 0, 0)), (9, (14, 0, 0)), (10, (10, 0, 0))],
        "Head": [(3, (-14, 0, 0)), (6, (-10, 0, 0)), (8, (-18, 0, 0)), (9, (6, 0, 0)), (10, (-2, 0, 0))],
        "Jaw": [(2, (10, 0, 0)), (5, (36, 0, 0)), (8, (50, 0, 0)), (9, (0, 0, 0)), (10, (5, 0, 0))],
        **keys(ears(3, -20, 2), ears(6, -36, 0), ears(9, -14, 10)),
        "Tail": [(3, (-6, 0, 0)), (5, (22, 0, 0)), (8, (18, 0, 0)), (10, (8, 0, 0))],
        "Tail2": [(4, (-8, 0, 0)), (7, (10, 0, 0)), (10, (18, 0, 0))],
        "Lantern": [(3, (30, 0, 0)), (7, (12, 0, 0)), (10, (-24, 0, 0))],
        "FootFL": [(2, foot("FL", -0.06, 0.1, 60, 0, 0.2)), (3, air("FL", -0.16, 0.1, 0, 0, -1)), (6, air("FL", -0.3, 0.2, -10)),
                   (8, foot("FL", -0.36, 0.07, -6)), (9, foot("FL", -0.36))],
        "FootFR": [(2, foot("FR", -0.05, 0.12, 60, 0, 0.2)), (3, air("FR", -0.15, 0.11, 0, 0, -1)), (6, air("FR", -0.29, 0.21, -10)),
                   (8, foot("FR", -0.32, 0.08, -6)), (9, foot("FR", -0.32, 0.01))],
        "FootBL": [(2, foot("BL", 0.14, 0, 45)), (3, air("BL", 0.2, 0.08, 70)), (6, air("BL", 0.3, 0.22, 60)), (8, air("BL", 0.22, 0.2, 40)),
                   (10, air("BL", 0.08, 0.1, 20)), (11, foot("BL", -0.01, 0.03, 10))],
        "FootBR": [(2, foot("BR", 0.14, 0, 45)), (3, air("BR", 0.21, 0.07, 70)), (6, air("BR", 0.31, 0.2, 60)), (8, air("BR", 0.23, 0.17, 40)),
                   (10, air("BR", 0.09, 0.1, 20)), (11, foot("BR", 0.03, 0.03, 10))],
    })),
    # PounceRecover (0.7 s): it skids on a little and rocks back, then shakes itself like a dog out of water (neck, then
    # ears flapping after, the tail wagging with it), steps its forepaws back under it, rises in two ticks, and springs
    # off its hind paws into the gallop (it ends on the Run's first frame: the chase follows).
    "PounceRecover": clip("PounceRecover", 20, keys(held(LANDED, 1), held(RUN_START, 21), {
        "Body": [(3, ((12, 0, 0), (0, -0.21, -0.14))), (6, ((6, 0, 0), (0, -0.15, -0.09))), (9, ((5, 4, 0), (0.01, -0.15, -0.08))),
                 (10, ((5, -4, 0), (-0.01, -0.15, -0.08))), (11, ((5, 3, 0), (0.008, -0.15, -0.08))), (13, ((4, 0, 0), (0, -0.12, -0.075))),
                 (16, ((2, 0, 0), (0, -0.04, -0.06))), (17, ((2, 0, 0), (0, -0.03, -0.06))), (19, ((1, 0, 0), (0, 0, -0.03)))],
        "Hips": [(4, (-4, 0, 0)), (8, (-4, 0, 0)), (12, (-6, 0, 0)), (17, (-3, 0, 0))],
        "Neck": [(3, (18, 0, 0)), (6, (10, 0, 0)), (8, (12, 18, 0)), (9, (12, -20, 0)), (10, (12, 16, 0)), (11, (12, -10, 0)),
                 (12, (14, 4, 0)), (14, (16, 0, 0))],
        "Head": [(3, (8, 0, 0)), (6, (-4, 0, 0)), (9, (0, 14, 0)), (10, (0, -16, 0)), (11, (0, 12, 0)), (12, (0, -6, 0)), (14, (-4, 0, 0)),
                 (17, (-12, 0, 0))],
        "Jaw": [(3, (0, 0, 0)), (6, (8, 0, 0)), (8, (2, 0, 0)), (9, (10, 0, 0)), (10, (0, 0, 0)), (11, (6, 0, 0)), (13, (0, 0, 0))],
        "EarL": [(3, (-24, 0, 6)), (6, (-6, 0, 2)), (9, (10, -30, 20)), (10, (-20, 20, 0)), (11, (6, -24, 16)), (12, (-12, 14, 4)),
                 (14, (4, 0, 2)), (17, (-12, 0, 4))],
        "EarR": [(3, (-24, 0, -6)), (6, (-6, 0, -2)), (9, (-20, -20, 0)), (10, (10, 30, -20)), (11, (-12, -14, -4)), (12, (6, 24, -16)),
                 (14, (4, 0, -2)), (17, (-12, 0, -4))],
        "Tail": [(4, (0, 0, 0)), (9, (6, 0, 16)), (10, (6, 0, -18)), (11, (6, 0, 14)), (12, (6, 0, -10)), (14, (6, 0, 4)), (17, (2, 0, 0))],
        "Tail2": [(5, (10, 0, 0)), (10, (2, 0, 20)), (11, (2, 0, -20)), (12, (2, 0, 16)), (13, (2, 0, -12)), (15, (0, 0, 4)), (18, (-6, 0, 0))],
        "Lantern": [(3, (-22, 0, 0)), (6, (10, 0, 0)), (9, (0, 14, 0)), (10, (0, -16, 0)), (11, (0, 12, 0)), (13, (-4, -4, 0)), (16, (2, 0, 0))],
        "FootFL": [(12, foot("FL", -0.36)), (14, foot("FL", -0.18, 0.08, 40)), (16, foot("FL")), (18, foot("FL"))],
        "FootFR": [(13, foot("FR", -0.32)), (15, foot("FR", -0.15, 0.08, 40)), (17, foot("FR")), (18, foot("FR"))],
        "FootBL": [(4, foot("BL", -0.02)), (6, foot("BL", -0.01, 0.04, 20)), (8, foot("BL"))],
        "FootBR": [(5, foot("BR", 0.02)), (7, foot("BR", 0.01, 0.04, 20)), (9, foot("BR")), (17, foot("BR")), (19, foot("BR", -0.07, 0.05, 20))],
    })),
    # RunDown (1 s): its spring gives out. A hitch in its stride, the key gives a turn; the head drops a tick; the key a
    # slower turn; the haunches give and it sits down hard, forelegs braced; the head sinks after, ears drooping; the key
    # a last, slowest turn; a shudder; still.
    "RunDown": clip("RunDown", 29, keys(held(READY, 1), held(SAGGED, 30), {
        "Body": [(3, ((3, 0, 0), (0, -0.01, -0.055))), (5, ((1, 0, 0), (0, 0, -0.04))), (10, ((1, 0, 0), (0, 0, -0.045))),
                 (13, ((-6, 0, 0), (0, 0.02, -0.07))), (15, ((-6, 0, 0), (0, 0.025, -0.075))), (20, ((-26, 0, 0), (0, 0.1, -0.16))),
                 (22, ((-23, 0, 0), (0, 0.1, -0.145))), (26, ((-24, 0, 0), (0, 0.1, -0.15))), (27, ((-24, 0, 2), (0.004, 0.1, -0.145))),
                 (28, ((-24, 0, -1), (-0.003, 0.1, -0.152)))],
        "Hips": [(5, (-3, 0, 0)), (12, (-8, 0, 0)), (20, (-18, 0, 0)), (23, (-15, 0, 0))],
        "Neck": [(4, (20, 0, 0)), (6, (20, 0, 0)), (8, (24, 0, 0)), (15, (24, 0, 0)), (22, (32, 0, 0)), (25, (26, 0, 0))],
        "Head": [(4, (-2, 0, 0)), (6, (-2, 0, 0)), (8, (8, 0, 0)), (14, (8, 0, 0)), (17, (14, 0, 0)), (23, (36, 0, 0)), (26, (30, 0, 0)),
                 (28, (33, 0, 0))],
        "Jaw": [(8, (3, 0, 0)), (16, (4, 0, 0)), (23, (12, 0, 0)), (26, (7, 0, 0))],
        "EarL": [(6, (-8, 0, 4)), (9, (-10, 6, 0)), (17, (-12, 10, -2)), (24, (-16, 22, -8))],
        "EarR": [(6, (-8, 0, -4)), (9, (-10, -6, 0)), (17, (-12, -10, 2)), (24, (-16, -22, 8))],
        "Tail": [(4, (0, 0, 0)), (12, (-6, 0, 0)), (15, (2, 0, 2)), (18, (30, 0, 12)), (21, (40, 0, 22)), (25, (34, 0, 27))],
        "Tail2": [(6, (-6, 0, 0)), (14, (-10, 0, 0)), (18, (6, 0, 8)), (23, (8, 0, 28)), (27, (0, 0, 22))],
        "Lantern": [(4, (-12, 0, 0)), (8, (6, 0, 0)), (14, (-4, 0, 0)), (20, (-14, 0, 0)), (24, (10, 0, 0)), (27, (2, 0, 0))],
        "Key": [(2, (0, 0, 0)), (4, (0, 0, 60)), (8, (0, 0, 60)), (11, (0, 0, 105)), (15, (0, 0, 105)), (19, (0, 0, 145)), (22, (0, 0, 145)),
                (27, (0, 0, 180))],
        "FootFL": [(17, foot("FL")), (19, foot("FL", 0.04, 0.04, 20)), (21, foot("FL", 0.1))],
        "FootFR": [(18, foot("FR")), (20, foot("FR", 0.04, 0.04, 20)), (22, foot("FR", 0.1))],
        "FootBL": [(12, foot("BL")), (16, foot("BL", -0.07, 0.05, 25)), (19, foot("BL", -0.14))],
        "FootBR": [(13, foot("BR")), (17, foot("BR", -0.07, 0.05, 25)), (20, foot("BR", -0.14))],
    })),
    # Wind (2.4 s; 4 when its key's knocked loose: the clip is stretched): sat, it winds itself: the game cranks the key
    # round, and the body shudders with each turn (a jolt up, a settle: clockwork), its head coming up a tick at a time,
    # ears lifting, the tail beginning to sweep. Then it heaves up onto its hind legs, a little too high, drops into
    # ready, wound tight, and springs off into the gallop (it ends on the Run's first frame: the chase follows).
    "Wind": clip("Wind", 71, keys(held(dict(SAGGED, Key=(0, 0, 0)), 1), held(RUN_START, 72), {
        "Body": [(1, SAGGED["Body"]), *jolt(6, ((-24, 0, 0), (0, 0.1, -0.15)), ((-25, 0, 1), (0, 0.1, -0.135)), ((-24, 0, 0), (0, 0.1, -0.148))),
                 *jolt(15, ((-24, 0, 0), (0, 0.1, -0.148)), ((-25, 0, -1), (0, 0.1, -0.133)), ((-24, 0, 0), (0, 0.1, -0.146))),
                 *jolt(24, ((-24, 0, 0), (0, 0.1, -0.146)), ((-25, 0, 1), (0, 0.1, -0.13)), ((-24, 0, 0), (0, 0.1, -0.144))),
                 *jolt(33, ((-24, 0, 0), (0, 0.1, -0.144)), ((-26, 0, -1), (0, 0.1, -0.128)), ((-24, 0, 0), (0, 0.1, -0.142))),
                 *jolt(42, ((-24, 0, 0), (0, 0.1, -0.142)), ((-26, 0, 1), (0, 0.1, -0.125)), ((-24, 0, 0), (0, 0.1, -0.14))),
                 (52, ((-24, 0, 0), (0, 0.1, -0.14))), (55, ((-24, 0, 0), (0, 0.11, -0.155))), (61, ((-2, 0, 0), (0, 0.0, -0.005))),
                 (65, ((2, 0, 0), (0, -0.01, -0.045))), (68, ((0, 0, 0), (0, 0, -0.03)))],
        "Hips": [(1, (-16, 0, 0)), (52, (-16, 0, 0)), (56, (-18, 0, 0)), (61, (2, 0, 0)), (66, (-5, 0, 0))],
        "Neck": [(1, (26, 0, 0)), (7, (26, 0, 0)), (8, (22, 0, 0)), (16, (22, 0, 0)), (17, (18, 0, 0)), (25, (18, 0, 0)), (26, (14, 0, 0)),
                 (43, (12, 0, 0)), (53, (14, 0, 0)), (57, (22, 0, 0)), (62, (8, 0, 0)), (67, (18, 0, 0))],
        "Head": [(1, (32, 0, 0)), (8, (32, 0, 0)), (9, (24, 0, 0)), (17, (24, 0, 0)), (18, (16, 0, 0)), (26, (16, 0, 0)), (27, (8, 0, 0)),
                 (35, (8, 0, 0)), (36, (2, 0, 0)), (44, (2, 0, 0)), (45, (-2, 0, 6)), (52, (-2, 0, 6)), (58, (6, 0, 0)), (63, (-14, 0, 0)),
                 (68, (-6, 0, 0))],
        "Jaw": [(1, (8, 0, 0)), (9, (8, 0, 0)), (10, (2, 0, 0)), (27, (0, 0, 0)), (58, (0, 0, 0)), (61, (6, 0, 0)), (66, (2, 0, 0))],
        "EarL": [(1, (-14, 18, -6)), (8, (-14, 18, -6)), (10, (-8, 10, -2)), (19, (-8, 10, -2)), (20, (0, 4, 0)), (28, (0, 4, 0)),
                 (29, (8, 0, 0)), (37, (8, 0, 0)), (38, (14, 0, 2)), (53, (14, 0, 2)), (59, (-6, 0, 2)), (64, (16, 0, 4)), (69, (-10, 0, 4))],
        "EarR": [(1, (-14, -18, 6)), (8, (-14, -18, 6)), (10, (-8, -10, 2)), (19, (-8, -10, 2)), (20, (0, -4, 0)), (28, (0, -4, 0)),
                 (29, (8, 0, 0)), (37, (8, 0, 0)), (38, (14, 0, -2)), (53, (14, 0, -2)), (59, (-6, 0, -2)), (64, (16, 0, -4)), (69, (-10, 0, -4))],
        "Tail": [(1, (36, 0, 26)), (30, (36, 0, 26)), (34, (37, 0, 8)), (38, (36, 0, 30)), (42, (37, 0, 6)), (46, (36, 0, 30)),
                 (50, (37, 0, 8)), (55, (38, 0, 12)), (61, (10, 0, 0)), (66, (2, 0, 0))],
        "Tail2": [(1, (2, 0, 24)), (32, (2, 0, 24)), (36, (4, 0, 2)), (40, (2, 0, 30)), (44, (4, 0, 2)), (48, (2, 0, 30)),
                  (52, (4, 0, 6)), (57, (-8, 0, 0)), (63, (8, 0, 0)), (68, (-6, 0, 0))],
        "Lantern": [(1, (4, 0, 0)), (7, (4, 0, 0)), (8, (-6, 0, 0)), (10, (5, 0, 0)), (16, (4, 0, 0)), (17, (-6, 0, 0)), (19, (4, 0, 0)),
                    (25, (3, 0, 0)), (26, (-5, 0, 0)), (28, (3, 0, 0)), (52, (2, 0, 0)), (59, (24, 0, 0)), (64, (-12, 0, 0)), (68, (4, 0, 0))],
        "FootFL": [(1, foot("FL", 0.1)), (56, foot("FL", 0.1)), (59, foot("FL", 0.03, 0.05, 25)), (62, foot("FL")), (68, foot("FL"))],
        "FootFR": [(1, foot("FR", 0.1)), (57, foot("FR", 0.1)), (60, foot("FR", 0.03, 0.05, 25)), (63, foot("FR")), (68, foot("FR"))],
        "FootBL": [(1, foot("BL", -0.14)), (55, foot("BL", -0.14)), (59, foot("BL", -0.06, 0.06, 30)), (62, foot("BL"))],
        "FootBR": [(1, foot("BR", -0.14)), (56, foot("BR", -0.14)), (60, foot("BR", -0.06, 0.06, 30)), (63, foot("BR")), (67, foot("BR")), (70, foot("BR", -0.07, 0.05, 20))],
    })),
    # Stagger (0.6 s as made; the game stretches it): struck, its head's flung back and the body thrown back and twisted,
    # the forepaws knocked off the floor; it comes down on them splayed, shakes its head (a stutter: clockwork jarred),
    # and gathers itself and springs back into the gallop (it ends on the Run's first frame: the chase follows).
    "Stagger": clip("Stagger", 13, keys(held(READY, 1), held(RUN_START, 14), {
        "Body": [(2, ((-6, -4, 6), (0, 0.06, -0.03))), (4, ((-12, -6, 10), (0, 0.11, -0.02))), (6, ((4, 3, 2), (0, 0.06, -0.075))),
                 (8, ((3, 5, 0), (0.01, 0.04, -0.065))), (9, ((3, -3, 0), (-0.008, 0.035, -0.06))), (11, ((2, 1, 0), (0, 0.01, -0.045)))],
        "Hips": [(3, (-8, 0, -4)), (6, (-6, 0, 2)), (10, (-3, 0, 0))],
        "Neck": [(2, (-14, 0, -8)), (4, (-8, 0, -12)), (6, (22, 0, 4)), (8, (18, 10, 0)), (9, (18, -10, 0)), (10, (18, 6, 0)), (12, (16, 0, 0))],
        "Head": [(2, (-20, 10, 0)), (5, (-6, 0, 6)), (7, (14, 0, 0)), (8, (10, 0, 14)), (9, (10, 0, -14)), (10, (8, 0, 8)), (12, (-8, 0, 0))],
        "Jaw": [(2, (22, 0, 0)), (5, (12, 0, 0)), (7, (2, 0, 0)), (9, (8, 0, 0)), (11, (2, 0, 0))],
        "EarL": [(3, (24, 0, -6)), (6, (-28, 0, 10)), (8, (2, -20, 14)), (9, (-14, 18, 0)), (11, (-4, 0, 6))],
        "EarR": [(3, (28, 0, 4)), (6, (-24, 0, -10)), (8, (-14, -18, 0)), (9, (2, 20, -14)), (11, (-4, 0, -6))],
        "Tail": [(3, (-6, 0, 10)), (6, (14, 0, -8)), (9, (2, 0, 8)), (11, (6, 0, -4))],
        "Tail2": [(4, (-10, 0, 14)), (7, (14, 0, -12)), (10, (-6, 0, 8)), (12, (-2, 0, -4))],
        "Lantern": [(3, (26, 0, 8)), (6, (-16, 0, -6)), (9, (8, 6, 0)), (12, (-2, 0, 0))],
        "FootFL": [(2, foot("FL", 0.02, 0.06, 40, -0.02)), (4, foot("FL", 0.02, 0.17, 50, -0.04)), (6, foot("FL", 0.05, 0, 0, -0.04)),
                   (10, foot("FL", 0.05, 0, 0, -0.04)), (12, foot("FL", 0.02, 0.06, 30))],
        "FootFR": [(2, foot("FR", 0.0, 0.03, 30)), (4, foot("FR", 0.04, 0.12, 50, 0.03)), (7, foot("FR", 0.08, 0, 0, 0.05)),
                   (9, foot("FR", 0.08, 0, 0, 0.05)), (11, foot("FR", 0.04, 0.06, 30, 0.02))],
        "FootBL": [(3, foot("BL")), (5, foot("BL", 0.05, 0.05, 25)), (7, foot("BL", 0.08)), (10, foot("BL", 0.08)), (12, foot("BL", 0.03, 0.04, 20))],
        "FootBR": [(4, foot("BR")), (6, foot("BR", 0.03, 0.04, 20)), (8, foot("BR", 0.05)), (11, foot("BR", 0.05)), (13, foot("BR", -0.08, 0.05, 20))],
    })),
    # Death (0.77 s; then it dissolves): its spring snaps: a jolt, the head flung up, and the key spins free, unwinding
    # (fast, then slower and slower). The legs give, it drops and topples onto its right side, stiff legs out; a bounce;
    # a hind leg kicks twice, weaker each time (the spring's last); the head comes down last, the jaw falls open.
    "Death": clip("Death", 23, keys(held(READY, 1), {
        "Root": [(5, ((0, 0, 0), (0, 0, 0))), (8, ((0, 12, 0), (0.02, 0, 0))), (11, ((0, 58, 0), (0.06, 0, 0.12))), (13, ((0, 90, 0), (0.04, 0, 0.17))),
                 (15, ((0, 86, 0), (0.04, 0, 0.19))), (17, ((0, 90, 0), (0.04, 0, 0.17))), (24, ((0, 90, 0), (0.04, 0, 0.17)))],
        "Body": [(3, ((-6, 0, 0), (0, 0, -0.015))), (6, ((4, 0, 0), (0, 0, -0.1))), (9, ((6, 0, 0), (0, 0, -0.13))), (13, ((2, 0, 0), (0, 0, -0.14))),
                 (15, ((0, 0, 0), (0, 0, -0.14))), (24, ((0, 0, 0), (0, 0, -0.14)))],
        "Hips": [(3, (6, 0, 0)), (7, (-8, 0, 0)), (13, (-4, 0, 0)), (16, (-6, 0, 0)), (24, (-5, 0, 0))],
        "Neck": [(3, (-20, 0, 0)), (6, (-12, 0, 0)), (11, (14, 0, 0)), (14, (10, 24, 0)), (17, (16, 20, 0)), (21, (20, 34, 0)), (24, (20, 32, 0))],
        "Head": [(3, (-22, 0, 0)), (6, (-10, 0, 0)), (11, (10, 0, 0)), (14, (2, 10, 0)), (17, (10, 6, 0)), (21, (14, 24, 0)), (24, (12, 22, 0))],
        "Jaw": [(2, (6, 0, 0)), (4, (34, 0, 0)), (8, (10, 0, 0)), (14, (4, 0, 0)), (20, (6, 0, 0)), (23, (20, 0, 0)), (24, (18, 0, 0))],
        "EarL": [(4, (30, 0, 0)), (8, (-10, 0, 0)), (13, (-30, 30, 0)), (15, (-10, -10, 0)), (18, (-24, 20, 0)), (24, (-20, 26, 0))],
        "EarR": [(4, (30, 0, 0)), (8, (-10, 0, 0)), (13, (-6, 0, 20)), (15, (-14, 0, 40)), (18, (-4, 0, 30)), (24, (-8, 0, 38))],
        "Tail": [(4, (24, 0, 0)), (9, (-10, 0, 0)), (13, (6, 0, 14)), (16, (-4, 0, 20)), (24, (-6, 0, 22))],
        "Tail2": [(6, (16, 0, 0)), (11, (-16, 0, 0)), (14, (10, 0, 16)), (17, (-6, 0, 10)), (24, (-4, 0, 14))],
        "Lantern": [(4, (20, 0, 0)), (9, (-14, 0, 0)), (12, (0, 30, 0)), (14, (0, 70, 0)), (16, (0, 60, 0)), (18, (0, 72, 0)), (24, (0, 70, 0))],
        "Key": [(3, (0, 0, 0)), (5, (0, 0, 110)), (8, (0, 0, 260)), (12, (0, 0, 400)), (17, (0, 0, 500)), (23, (0, 0, 540))],
        "FootFL": [(6, foot("FL", 0.03)), (10, foot("FL", -0.04, 0.03, 20)), (13, foot("FL", -0.16, 0.08, 10)), (16, foot("FL", -0.2, 0.1, 0)),
                   (24, foot("FL", -0.2, 0.1, 0))],
        "FootFR": [(6, foot("FR", 0.0)), (10, foot("FR", -0.03, 0.02, 20)), (13, foot("FR", -0.12, 0.06, 10)), (16, foot("FR", -0.16, 0.08, 0)),
                   (24, foot("FR", -0.16, 0.08, 0))],
        "FootBL": [(6, foot("BL", 0.0)), (10, foot("BL", 0.04, 0.02, 20)), (13, foot("BL", 0.14, 0.08, 20)), (16, foot("BL", 0.16, 0.1, 10)),
                   (18, foot("BL", 0.04, 0.18, 40)), (20, foot("BL", 0.2, 0.08, 10)), (22, foot("BL", 0.12, 0.13, 25)), (24, foot("BL", 0.18, 0.1, 10))],
        "FootBR": [(6, foot("BR", 0.02)), (10, foot("BR", 0.04, 0.02, 20)), (13, foot("BR", 0.12, 0.06, 20)), (16, foot("BR", 0.14, 0.07, 10)),
                   (24, foot("BR", 0.14, 0.07, 10))],
    })),
}


def build():
    fur, fur_dark = kit.mat("WoundFur", "#b39670"), kit.mat("WoundFurDark", "#7d6547")  # (fawn: seen against the grey stone)
    brass, brass_web = kit.mat("WoundBrass_Metal", "#c99a45"), kit.mat("WoundBrassWeb_Metal", "#9c7a38")
    iron, eye = kit.mat("WoundIron_Metal", "#3a383f"), kit.mat("SouredEye_Glow", "#c8f07a")
    nose = kit.mat("WoundNose", "#1b1a1d")

    def bend(ob, front, back, y0, y1):
        """Skin a part across a joint: on `front` ahead of y0, on `back` behind y1, shared smoothly between."""
        a, b = ob.vertex_groups[front], ob.vertex_groups.new(name=back)
        for v in ob.data.vertices:
            t = min(1.0, max(0.0, (v.co.y - y0) / (y1 - y0)))
            w = t * t * (3 - 2 * t)
            a.add([v.index], 1 - w, "REPLACE")
            b.add([v.index], w, "REPLACE")
        return ob

    parts = [
        # The body: a deep chest, a tucked waist, haunches (the waist and the spine bend between the front half and the haunches).
        part("Chest", kit.sphere(10, 8), fur, M((0, -0.2, BACK - 0.05), scale=(0.15, 0.2, 0.19)), smooth=True, group="Body"),
        bend(part("Waist", kit.sphere(10, 8), fur, M((0, 0.1, BACK - 0.02), scale=(0.1, 0.22, 0.11)), smooth=True, group="Body"), "Body", "Hips", -0.04, 0.16),
        part("Haunch", kit.sphere(10, 8), fur, M((0, 0.32, BACK - 0.04), scale=(0.13, 0.14, 0.14)), smooth=True, group="Hips"),
        bend(part("Spine", kit.cube(), fur_dark, M((0, 0.04, BACK + 0.1), scale=(0.05, 0.62, 0.03)), group="Body"), "Body", "Hips", -0.04, 0.16),
        part("Neck", kit.cone(8, 0.08, 0.06, 0.3), fur, kit.along((0, -0.33, BACK + 0.02), (0, -0.52, BACK + 0.24)), smooth=True, group="Neck"),
        part("Collar", kit.cylinder(10, 0.085, 0.04), brass, kit.along((0, -0.38, BACK + 0.07), (0, -0.4, BACK + 0.1)), group="Neck"),
        # The head: long and narrow, the snout tapering, ears laid back.
        part("Skull", kit.sphere(9, 7), fur, M((0, -0.55, BACK + 0.22), scale=(0.08, 0.11, 0.08)), smooth=True, group="Head"),
        part("Snout", kit.cone(8, 0.06, 0.025, 0.24), fur, kit.along((0, -0.6, BACK + 0.2), (0, -0.84, BACK + 0.15)), smooth=True, group="Head"),
        part("Nose", kit.sphere(6, 4, 0.025), nose, M((0, -0.85, BACK + 0.15)), group="Head"),
        part("LowerJaw", kit.cone(6, 0.035, 0.015, 0.22), fur_dark, kit.along((0, -0.58, BACK + 0.14), (0, -0.8, BACK + 0.1)), group="Jaw"),
    ]
    for s, ear in ((-1, "EarL"), (1, "EarR")):
        parts.append(part("Eye", kit.sphere(6, 4, 0.02), eye, M((s * 0.045, -0.62, BACK + 0.26)), group="Head"))
        parts.append(part("Ear", kit.cone(4, 0.04, 0.005, 0.12), fur_dark, kit.along((s * 0.04, -0.5, BACK + 0.28), (s * 0.07, -0.4, BACK + 0.31)), group=ear))
        # Its flanks opened on brass: a wheel where the ribs were, a smaller one meshing behind it.
        parts += [
            part("RibWheel", kit.cylinder(14, 0.11, 0.03), brass, M((s * 0.135, -0.14, BACK - 0.04), rot=(0, 90, 0)), group="Body"),
            part("RibWheelWeb", kit.cylinder(14, 0.08, 0.035), brass_web, M((s * 0.135, -0.14, BACK - 0.04), rot=(0, 90, 0)), group="Body"),
            part("Hub", kit.cylinder(8, 0.025, 0.05), iron, M((s * 0.14, -0.14, BACK - 0.04), rot=(0, 90, 0)), group="Body"),
            part("SmallWheel", kit.cylinder(10, 0.06, 0.03), brass, M((s * 0.12, 0.06, BACK - 0.02), rot=(0, 90, 0)), group="Body"),
        ]
        for k in range(10):
            a = 2 * math.pi * k / 10
            parts.append(part("Cog", kit.cube(), brass, M((s * 0.135, -0.14 + math.cos(a) * 0.12, BACK - 0.04 + math.sin(a) * 0.12),
                                                          rot=(math.degrees(a), 0, 0), scale=(0.03, 0.03, 0.03)), group="Body"))
    # The legs: iron, thin, a brass knuckle at each joint, paws. Each in three (thigh, shin, paw) so it bends: the
    # knuckle hides the joint.
    for leg in LEGS:
        top, knee, foot_at, _ = LEG[leg]
        x, y1 = foot_at[0], foot_at[1]
        hind = leg[0] == "B"
        parts += [
            part("Thigh", kit.cone(6, 0.045, 0.03, math.dist(top, knee)), fur if hind else iron, kit.along(top, knee), smooth=True, group="Leg" + leg),
            part("Shin", kit.cylinder(6, 0.022, math.dist(knee, foot_at)), iron, kit.along(knee, foot_at), group="Shin" + leg),
            part("Knuckle", kit.sphere(6, 4, 0.032), brass, M(knee), group="Shin" + leg),
            part("Paw", kit.sphere(6, 4), iron, M((x, y1 - 0.03, 0.03), scale=(0.04, 0.06, 0.03)), group="Paw" + leg),
        ]
    # The tail: thin, curving down (in two, bending between).
    tail = [(0, 0.42, BACK - 0.02), (0, 0.51, BACK - 0.03), (0, 0.6, BACK - 0.04), (0, 0.69, BACK - 0.09), (0, 0.78, BACK - 0.14)]
    parts.append(bend(part("Tail", kit.tube(tail, [0.025, 0.0215, 0.018, 0.014, 0.01], seg=5), fur_dark, group="Tail"), "Tail", "Tail2", 0.54, 0.66))
    # The key in its back: a shaft and two round wings, a toy's.
    kx, ky, kz = 0, 0.04, BACK + 0.1
    parts += [
        part("KeyShaft", kit.cylinder(8, 0.02, 0.24), brass, M((kx, ky, kz + 0.1)), group="Key"),
        part("KeyCollar", kit.cylinder(8, 0.04, 0.03), brass, M((kx, ky, kz + 0.02)), group="Key"),
    ]
    for s in (-1, 1):
        ring = [(kx + s * (0.09 + 0.07 * math.cos(a)), ky, kz + 0.26 + 0.07 * math.sin(a)) for a in (2 * math.pi * k / 14 for k in range(15))]
        parts.append(part("KeyWing", kit.tube(ring, [0.016] * len(ring), seg=5), brass, group="Key"))
    # The lantern hung from its collar, like every Soured's (on its own bone: it swings).
    lantern_glass = kit.mat("SouredLantern_Glow", "#b6f06a")
    parts += [
        part("BeltLanternTop", kit.cone(4, 0.045, 0.01, 0.04), iron, M((0, -0.44, BACK - 0.02), rot=(0, 0, 45)), group="Lantern"),
        part("BeltLanternGlass", kit.cube(), lantern_glass, M((0, -0.44, BACK - 0.08), scale=(0.06, 0.06, 0.08)), group="Lantern"),
        part("BeltLanternBase", kit.cube(), iron, M((0, -0.44, BACK - 0.125), scale=(0.075, 0.075, 0.012)), group="Lantern"),
    ]
    mesh = kit.join(parts, "Wound")
    armature = kit.rig("WoundRig", BONES)
    kit.skin(mesh, armature)
    kit.animate(armature, ACTIONS)
    for name, worst in REACH.items():
        if worst > 0.02:
            print(f"[wound] {name}: the paws come off where they're keyed by up to {worst * 100:.0f} cm")
    return [armature, mesh]
