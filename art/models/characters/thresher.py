"""A Thresher (the Orchard): one of the orchard's old threshing machines, iron and oak on iron wheels, a reel of flails
at the front under an iron cowl, a boiler amidships with its stack, and in the driver's seat at the back, the man who
drove it, grown into its levers: a hunched, hooded Soured, its eyes green. Like every Soured, a green lantern hung
from its frame. About 2.6 m long. The reel spins in code (faster as it revs and charges): the clips only add to it
(a whirl in the sweep, a jam in the crash).

Clips (each plays stretched to the time the code gives it, so the long ones are made at that length: 1x):
Idle (16 frames, the chase loop too, and played once over 0.55-0.8 s: the vent's hiss, the charge's recovery, a brake
at a brink), Rev (1.1 s: the tell before a charge), Charge (2.2 s, the run, cut short by a crash), Crash (3.5 s: the
whole stall, the impact to the restart), Stalled (slumped, steaming: looped), SweepWindup / Sweep / Recover (the
front swung aside and back across: the flails), Vent (a buck: steam blown from it), Stagger (a perfect block's stun,
played slowed to 1.8 s), Death (1.2 s: a wheel gives, it lists over, the driver slumping off the seat).

How it's animated (as the Laden: each bone keyed on its own timing, kit.bake): the machine (Root: where it stands,
wheels and all; tipped about a wheel's contact, never through the floor) leads; the frame rocks on its springs over the
wheels (Body: a bob, a pitch, a roll: an engine's beat, bumps, slams); the driver grown into the seat is the loose part
and follows a frame or two late, the head after him, the hood after that; the lantern swings on its post. His hands
stay on the levers (kit.reach bends his arms every frame), and the levers move: he hauls on them to rev, throws them
to charge, yanks them to steer. The wheels roll when it moves (the run, the turn of a sweep, a roll back off a crash).
"""
import math
import random

from mathutils import Euler, Quaternion, Vector

M, part = kit.M, kit.part  # `kit` is injected by build_model.py

REEL_Y, REEL_Z = -1.42, 0.72
SHOULDER, ELBOW, HAND = (0.2, 0.85, 1.52), (0.3, 0.72, 1.34), (0.22, 0.53, 1.43)  # (the right arm; the left mirrors it)
LEVER_FOOT, LEVER_TOP = (0.22, 0.55, 0.78), (0.22, 0.5, 1.45)
REAR_HUB, FRONT_HUB = (0.78, 0.72, 0.62), (0.68, -0.68, 0.38)


def side(p, s):
    return (s * p[0], p[1], p[2])


BONES = {
    "Root": ((0, 0, 0), (0, 0, 0.4), None),                     # the machine where it stands (wheels and all)
    "Body": ((0, 0.9, 0.7), (0, -0.9, 0.7), "Root"),            # the frame on its springs: boiler, stack, cowl, seat
    "Reel": ((-0.72, REEL_Y, REEL_Z), (0.72, REEL_Y, REEL_Z), "Body"),  # the flail reel (spun by the code too)
    "WheelBL": (side(REAR_HUB, -1), (-0.95, 0.72, 0.62), "Root"),
    "WheelBR": (REAR_HUB, (0.95, 0.72, 0.62), "Root"),
    "WheelFL": (side(FRONT_HUB, -1), (-0.85, -0.68, 0.38), "Root"),
    "WheelFR": (FRONT_HUB, (0.85, -0.68, 0.38), "Root"),
    "LeverL": (side(LEVER_FOOT, -1), side(LEVER_TOP, -1), "Body"),
    "LeverR": (LEVER_FOOT, LEVER_TOP, "Body"),
    "Lantern": ((0.62, 1.05, 1.36), (0.62, 1.05, 1.05), "Body"),  # on its post by the seat
    "Driver": ((0, 0.95, 1.12), (0, 0.85, 1.6), "Body"),        # grown into the seat: bends from there
    "Head": ((0, 0.85, 1.6), (0, 0.72, 1.86), "Driver"),
    "Hood": ((0, 0.74, 1.67), (0, 0.94, 1.97), "Head"),          # its pointed hood: flops after the head
    "ArmL": (side(SHOULDER, -1), side(ELBOW, -1), "Driver"),
    "ForeL": (side(ELBOW, -1), side(HAND, -1), "ArmL"),
    "ArmR": (SHOULDER, ELBOW, "Driver"),
    "ForeR": (ELBOW, HAND, "ArmR"),
}

WHEELS = ("WheelBL", "WheelBR", "WheelFL", "WheelFR")
REAR, FRONT = (0, 0.72, 0), (0, -0.68, 0)  # where the rear and front wheels stand (their axles' middles, on the floor)
SPRUNG = (0, 0, 0.66)                      # what the frame rocks about on its springs


def about(rot, pivot, head, lift=(0, 0, 0)):
    """A bone (its head at `head`) turned `rot` about `pivot` instead of its head, then moved `lift`: as a key."""
    turn = Euler([math.radians(a) for a in rot]).to_matrix()
    d = Vector(pivot) - Vector(head)
    off = d - turn @ d + Vector(lift)
    return (tuple(rot), tuple(round(v, 4) for v in off))


def root(pitch=0, roll=0, yaw=0, at=(0, 0, 0), lift=(0, 0, 0)):
    """The whole machine pitched (+: nose down), rolled (+: over to its right), turned (+: its front to its right), about
    `at` on the floor (a wheel's contact, so the rest don't sink), and moved `lift`."""
    return about((pitch, roll, yaw), at, (0, 0, 0), lift)


def body(pitch=0, roll=0, yaw=0, bob=0):
    """The frame rocked on its springs (about its middle) and bobbed up or down."""
    return about((pitch, roll, yaw), SPRUNG, BONES["Body"][0], (0, 0, bob))


def tipped(angle):
    """The whole machine tipped `angle` about the line between its back-left and front-right wheels' contacts (what's
    left standing when the front-left wheel gives): the front-left corner drops, the back-right wheel lifts."""
    a, b = Vector((-REAR_HUB[0], REAR_HUB[1], 0)), Vector((FRONT_HUB[0], FRONT_HUB[1], 0))
    rot = [math.degrees(v) for v in Quaternion((b - a).normalized(), math.radians(angle)).to_euler("XYZ")]
    return about(rot, a, (0, 0, 0))


def buckled(tilt, lift):
    """The front-left wheel splayed out (its top falling outward), broken off its axle, `lift` up into the frame."""
    return about((0, -tilt, 0), side(FRONT_HUB, -1), side(FRONT_HUB, -1), lift)


def grip(pose):
    """His hands kept on the levers, the elbows bowed out and down, whatever he and the levers are doing."""
    m = kit.pose_matrices(BONES, pose)
    for s, arm, fore, lever in ((-1, "ArmL", "ForeL", "LeverL"), (1, "ArmR", "ForeR", "LeverR")):
        hand = Vector(side(HAND, s))
        pole = m["Driver"].to_3x3() @ Vector((s, 0.3, -0.8))
        pose = kit.reach(BONES, pose, arm, fore, lambda mm, lever=lever, hand=hand: mm[lever] @ hand, pole)
    return pose


REACH = {}  # (the build's report: how far each clip's hands come off the levers, at worst)


def clip(length, tracks, periodic=False):
    """A clip `length` frames long (30 a second) from per-bone tracks, baked to every frame, the hands on the levers."""
    frames = [(f, grip(p)) for f, p in kit.bake(tracks, length, periodic)]
    worst = 0.0
    for f, p in frames:
        m = kit.pose_matrices(BONES, p)
        for s, fore, lever in ((-1, "ForeL", "LeverL"), (1, "ForeR", "LeverR")):
            hand = Vector(side(HAND, s))
            worst = max(worst, (m[fore] @ hand - m[lever] @ hand).length)
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


def rolled(length, rate, turns=2):
    """A wheel rolling, keyed every frame: `rate(f)` degrees a frame (+: forward), the whole scaled to `turns` whole
    turns (two: it ends exactly where it began, so whatever follows takes it up without a flick)."""
    angles = [0.0]
    for f in range(2, length + 2):
        angles.append(angles[-1] + rate(f))
    k = turns * 360.0 / angles[-1]
    return [(f, (round(a * k, 3), 0, 0)) for f, a in zip(range(1, length + 2), angles)]


def turning(yaw):
    """How far the rear wheels roll (left, right) when the machine turns `yaw` about the middle of its rear axle."""
    a = REAR_HUB[0] / REAR_HUB[2] * yaw
    return (a, 0, 0), (-a, 0, 0)


# What everything starts from and comes back to: at rest on its wheels, the driver a little hunched over his levers.
BASE = {"Root": root(), "Body": body(), "Reel": (0, 0, 0), "Driver": (6, 0, 0), "Head": (-4, 0, 0), "Hood": (0, 0, 0),
        "LeverL": (0, 0, 0), "LeverR": (0, 0, 0), "Lantern": (0, 0, 0), **{w: (0, 0, 0) for w in WHEELS}}
# Revved (the end of the tell): reared on its back wheels, the nose and the reel up, the driver hunched forward, the
# levers thrown.
REVVED = dict(BASE, Root=root(-2, at=REAR), Body=body(-4, 0, 0, -0.01), Driver=((18, -14, 0), (0, 0, 0.04)),
              Head=(-16, 8, 6), Hood=(-12, 0, 0), LeverL=(4.8, 0, 0), LeverR=(4.8, 0, 0), Lantern=(8, 0, 0))
# Running (the charge, as a crash finds it): nose down, the driver hunched low, the hood and the lantern blown back.
RUNNING = dict(BASE, Body=body(2.5, 0, 0, 0), Driver=(16, 0, 0), Head=(-18, 0, 0), Hood=(-20, 0, 0),
               LeverL=(4.8, 0, 0), LeverR=(4.8, 0, 0), Lantern=(14, 0, 0))
# Wound (the sweep's wind-up): swung round on its back wheels, its front to its right, the nose up; the driver
# steering (one lever forward, one back), his head turned back on you.
WOUND = dict(BASE, Root=root(yaw=32, at=REAR), Body=body(-3, -2, 0, -0.01), Driver=(12, 6, -16), Head=(-6, 0, -18),
             Hood=(-8, 0, 10), LeverL=(4.2, 0, 0), LeverR=(-16, 0, 0), Lantern=(4, 8, 0), Reel=(30, 0, 0))
WOUND["WheelBL"], WOUND["WheelBR"] = turning(32)
# Swept (the sweep's end): swung right across to its left, nose down, leaning out; the driver thrown the other way.
SWEPT = dict(BASE, Root=root(yaw=-36, at=REAR), Body=body(3, 2.5, 0, -0.006), Driver=(20, -8, 16), Head=(2, 0, 22),
             Hood=(14, 0, -12), LeverL=(-16, 0, 0), LeverR=(4.8, 0, 0), Lantern=(2, -4, 0), Reel=(-140, 0, 0))
SWEPT["WheelBL"], SWEPT["WheelBR"] = turning(-36)
# Slumped (stalled): low on its springs, the nose down a little; the driver fallen forward over his levers, his head
# lolled to the side, the hood hanging.
SLUMPED = dict(BASE, Body=body(2, 0, 0, -0.026), Driver=(31, 20, 10), Head=(40, 10, 24), Hood=(16, 0, 8),
               LeverL=(4.8, 0, 0), LeverR=(4.8, 0, 0))


def run(first, last, seed=7):
    """The run's rattle, from `first` to about `last`: bumps every 4 or 5 frames, each a kick of the frame (up, nose
    up, over to one side, the other side the next), the wheels hopping; the driver jolted a frame later, his head a frame
    after him, the hood and the lantern flapping after that."""
    rnd = random.Random(seed)
    tr = {b: [] for b in ("Root", "Body", "Driver", "Head", "Hood", "LeverL", "LeverR", "Lantern")}
    f, s = first, 1
    while f <= last:
        a = rnd.uniform(0.55, 1.0)
        tr["Root"] += [(f, root(0, s * 0.5 * a, 0, at=(s * 0.8, 0, 0), lift=(0, 0, 0.012 * a))), (f + 2, root())]
        tr["Body"] += [(f, body(2.5 - 1.4 * a, s * 1.5 * a, 0, 0.014 * a)), (f + 2, body(2.5 + 0.8 * a, -s * 0.5 * a, 0, -0.008 * a))]
        tr["Driver"] += [(f + 1, (16 + 5 * a, s * 3 * a, s * 1.5 * a))]
        tr["Head"] += [(f + 2, (-18 + 8 * a, 0, s * 3 * a))]
        tr["Hood"] += [(f + 3, (-20 - 9 * a, 0, -s * 5 * a))]
        tr["LeverL"] += [(f + 1, (4.5 + 1.2 * a * s, 0, 0))]
        tr["LeverR"] += [(f + 2, (4.5 - 1.2 * a * s, 0, 0))]
        tr["Lantern"] += [(f + 3, (14 + 5 * a * s, 3 * a * s, 0))]
        f += rnd.choice((4, 5))
        s = -s
    return tr


def charge_rate(f):
    """The wheels' roll in the charge (degrees a frame): up to speed as it lurches off, slowing as it brakes."""
    return 13 * min(1.0, (f - 1) / 6, max(0.0, (67 - f) / 12))


def crash_wheels(back):
    """The wheels rolled back with the machine (`back` metres) off what it ran into: (rear, front) rotations."""
    return (-math.degrees(back / REAR_HUB[2]), 0, 0), (-math.degrees(back / FRONT_HUB[2]), 0, 0)


def crash_root(frames_back):
    """Root keys for the crash, each (frame, pitch, pivot, back): tipped about a wheel's contact, rolled `back`."""
    return [(f, root(p, at=at, lift=(0, back, 0))) for f, p, at, back in frames_back]


CRASH_BACK = [(3, 0), (7, 0.05), (12, 0.12), (16, 0.13), (90, 0.13), (95, 0.1), (100, 0.03), (104, 0.0)]

ACTIONS = {
    # Idle (16 frames, looped; also played once, stretched to 0.55-0.8 s): an old engine ticking over, two beats a loop.
    # Each beat kicks the frame up on its springs, nose up and over to one side (the right, then the left), and it
    # settles; the driver is jolted a frame later, his head nods a frame after him (turning a little, side to side), the
    # hood flops after that; the levers shiver, the lantern jiggles on its post.
    "Idle": clip(16, keys(held(BASE, 1), {
        "Body": [(2, body(-1.0, 0.9, 0, 0.016)), (4, body(0.4, -0.3, 0, -0.005)), (6, body(-0.2, 0.2, 0, 0.003)), (8, body()),
                 (9, body(-0.9, -0.9, 0, 0.014)), (11, body(0.4, 0.3, 0, -0.005)), (13, body(-0.2, -0.2, 0, 0.003))],
        "Driver": [(3, (9.5, 1.2, 0)), (5, (4, 0.4, 0)), (7, (6.5, 0, 0)), (10, (9.5, -1.2, 0)), (12, (4, -0.4, 0)), (14, (6.5, 0, 0))],
        "Head": [(4, (2, 0, 2.5)), (6, (-7, 0, 3.5)), (8, (-4, 0, 1.5)), (11, (2, 0, -2.5)), (13, (-7, 0, -3.5)), (15, (-4, 0, -1.5))],
        "Hood": [(5, (-7, 0, -2)), (7, (5, 0, 1)), (9, (0, 0, 0)), (12, (-7, 0, 2)), (14, (5, 0, -1)), (16, (0, 0, 0))],
        "LeverL": [(2, (0.8, 0, 0)), (4, (-1.5, 0, 0)), (6, (0.2, 0, 0)), (8, (0, 0, 0)), (9, (0.8, 0, 0)), (11, (-1.5, 0, 0)), (13, (0.2, 0, 0))],
        "LeverR": [(3, (0.8, 0, 0)), (5, (-1.5, 0, 0)), (7, (0.2, 0, 0)), (10, (0.8, 0, 0)), (12, (-1.5, 0, 0)), (14, (0.2, 0, 0))],
        "Lantern": [(3, (6, 4, 0)), (6, (-3, 0, 0)), (10, (6, -2, 0)), (13, (-3, 2, 0)), (16, (1, 0, 0))],
    }), periodic=True),
    # Rev (1.1 s: the tell). The driver hauls the levers back and leans back, the frame dips its nose under the load;
    # then the engine catches with a bang (the frame kicks up, the driver and his head jolted back, the reel jerks), and
    # it shakes, faster and faster, rearing up on its back wheels (the front wheels and the reel lifting), the driver
    # hunching forward over the levers, his head up and on you; at the last he throws the levers forward.
    "Rev": clip(26, keys(held(BASE, 1), held(REVVED, 27), {
        "Root": [(17, root()), (22, root(-2.5, at=REAR)), (25, root(-1.6, at=REAR))],
        "Body": [(4, body(1.5, 0, 0, -0.012)), (7, body(-3.5, 1.2, 0, 0.02)), (10, body(-2.5, -1.3, 0, 0.004)),
                 (13, body(-4, 1.5, 0, 0.012)), (15, body(-3.5, -1.5, 0, 0.0)), (17, body(-4.5, 1.7, 0, 0.01)),
                 (19, body(-4, -2.2, 0, 0)), (20, body(-4.5, 2.4, 0, 0.008)), (21, body(-4, -2.6, 0, 0)),
                 (22, body(-4.6, 2.8, 0, 0.008)), (23, body(-4, -2.8, 0, 0)), (24, body(-4.6, 2.8, 0, 0.008)), (25, body(-4, -2, 0, 0))],
        "Driver": [(4, (-6, 0, 0)), (6, (-8, 0, 0)), (9, (-2, 1.5, 0)), (13, ((10, -6, 0), (0, 0, 0.02))),
                   (18, ((15, -12, 0), (0, 0, 0.04))), (21, ((19, -13, 0), (0, 0, 0.04))), (23, ((16, -15, 0), (0, 0, 0.04))),
                   (25, ((20, -13, 0), (0, 0, 0.04)))],
        "Head": [(5, (6, 0, 0)), (8, (-2, 0, 0)), (10, (-14, 0, 0)), (15, (-16, 4, 4)), (20, (-18, 9, 8)), (22, (-14, 7, 4)), (24, (-18, 9, 8))],
        "Hood": [(6, (8, 0, 0)), (9, (2, 0, 0)), (11, (-12, 0, 0)), (15, (-6, 0, 0)), (19, (-14, 0, 3)), (22, (-10, 0, -3)), (25, (-14, 0, 3))],
        "LeverL": [(4, (-14, 0, 0)), (8, (-10, 0, 0)), (12, (-14, 0, 0)), (20, (-14, 0, 0)), (23, (-16, 0, 0)), (25, (5, 0, 0))],
        "LeverR": [(5, (-14, 0, 0)), (9, (-10, 0, 0)), (13, (-14, 0, 0)), (21, (-14, 0, 0)), (24, (-16, 0, 0)), (26, (5, 0, 0))],
        "Lantern": [(5, (-6, 0, 0)), (9, (10, 2, 0)), (13, (-4, 0, 0)), (17, (6, 3, 0)), (20, (2, -2, 0)), (22, (7, 3, 0)), (24, (3, -2, 0))],
        "Reel": [(6, (8, 0, 0)), (9, (-14, 0, 0)), (14, (0, 0, 0))],
    })),
    # Charge (2.2 s, the run; a crash cuts it short). Let go, it lurches off: the front slams down (the frame dives and
    # rebounds), the driver is thrown back and whips forward again, his head snapping back, the hood and the lantern
    # flung back; then the run, rattling over the ground (the frame kicking side to side every few frames, the wheels
    # hopping, the driver jolted after each bump, the hood flapping), the wheels rolling. At the end of its run he hauls
    # the levers back: it brakes, the nose diving, him thrown forward, and it rocks back.
    "Charge": clip(66, keys(held({b: v for b, v in REVVED.items() if b not in WHEELS}, 1), run(9, 52), {
        "Root": [(3, root()), (56, root()), (67, root())],
        "Body": [(3, body(4.5, 0, 0, -0.025)), (5, body(1.5, 0, 0, 0.01)), (7, body(3, 0, 0, -0.005)), (56, body(2.5, 0, 0, 0)),
                 (60, body(5.5, 0, 0, -0.02)), (63, body(-0.8, 0, 0, 0.006)), (67, body(0.5, 0, 0, 0))],
        "Driver": [(3, ((2, -6, 0), (0, 0, 0.02))), (6, (22, 2, 0)), (9, (16, 0, 0)), (57, (16, 0, 0)), (61, (19, 6, 0)), (64, (6, 0, 0)), (67, (7, 0, 0))],
        "Head": [(4, (-34, 0, 0)), (7, (-10, 0, 0)), (10, (-18, 0, 0)), (58, (-18, 0, 0)), (62, (-7, 0, 0)), (65, (-10, 0, 0)), (67, (-5, 0, 0))],
        "Hood": [(5, (-30, 0, 0)), (9, (-16, 0, 0)), (59, (-18, 0, 0)), (63, (2, 0, 0)), (67, (0, 0, 0))],
        "LeverL": [(3, (5, 0, 0)), (56, (4.8, 0, 0)), (59, (-8, 0, 0)), (63, (-4, 0, 0)), (67, (0, 0, 0))],
        "LeverR": [(3, (5, 0, 0)), (57, (4.8, 0, 0)), (60, (-8, 0, 0)), (63, (-4, 0, 0)), (67, (0, 0, 0))],
        "Lantern": [(5, (22, 0, 0)), (9, (12, 0, 0)), (58, (14, 0, 0)), (62, (-14, 0, 0)), (65, (6, 0, 0)), (67, (0, 0, 0))],
        "WheelBL": rolled(66, charge_rate), "WheelBR": rolled(66, charge_rate),
        "WheelFL": rolled(66, charge_rate), "WheelFR": rolled(66, charge_rate),
    })),
    # Crash (3.5 s: the whole stall). It hits: the front stops dead and the rear bucks up over it, the frame flung on its
    # springs, the reel jammed back, the levers knocked forward; the driver is thrown forward over the levers, his head
    # whipping down and back, the hood flopping, the lantern flung forward. The rear slams down, hops once more, and it
    # rolls back a little off what it hit. The engine dies in a few weakening coughs, sagging on its springs; the driver
    # slumps over the levers, head lolled (open: the punish window), breathing; once a hiss shudders it. Then he comes to:
    # his head lifts, he hauls himself up and yanks the levers, the engine catches (a kick, another), and it lurches
    # forward back onto its idle.
    "Crash": clip(105, keys(held(RUNNING, 1), held(BASE, 106), {
        "Root": crash_root([(3, 8, FRONT, 0), (4, 9, FRONT, 0.01), (7, -1.2, REAR, 0.05), (9, 1.5, FRONT, 0.08),
                            (11, -0.4, REAR, 0.11), (14, 0, FRONT, 0.125), (16, 0, FRONT, 0.13), (90, 0, FRONT, 0.13),
                            (95, 0, FRONT, 0.1), (100, 0, FRONT, 0.03), (104, 0, FRONT, 0.0)]),
        "Body": [(2, body(4, 0, 0, -0.01)), (4, body(-3, 1.5, 0, 0.03)), (7, body(3.5, -1, 0, -0.035)), (10, body(-1, 0.6, 0, 0.012)),
                 (13, body(1.5, -0.3, 0, -0.012)), (17, body(0.8, 0, 0, -0.004)), (21, body(0.2, 0.5, 0, 0.008)),
                 (24, body(1.5, 0, 0, -0.016)), (29, body(0.6, -0.3, 0, 0.004)), (33, body(1.8, 0, 0, -0.022)),
                 (40, SLUMPED["Body"]), (56, SLUMPED["Body"]), (58, body(1.2, 0.6, 0, -0.012)), (60, body(2.2, -0.3, 0, -0.028)),
                 (64, SLUMPED["Body"]), (74, SLUMPED["Body"]), (76, body(1.6, -0.4, 0, -0.018)),
                 (78, body(2.1, 0.2, 0, -0.027)), (84, SLUMPED["Body"]), (88, body(2.4, 0, 0, -0.03)), (92, body(-1.2, 0.8, 0, 0.022)),
                 (95, body(1, -0.4, 0, -0.008)), (99, body(-0.8, -0.7, 0, 0.014)), (102, body(0.4, 0.2, 0, -0.004))],
        "Driver": [(3, (23, 12, 4)), (5, (26, 19, 6)), (8, (16, 6, 2)), (11, (25, 18, 8)), (15, (27, 19, 8)), (20, (30, 20, 10)),
                   (40, SLUMPED["Driver"]), (50, (28, 20, 10)), (59, (33, 21, 10)), (61, (34, 22, 11)), (70, (28, 20, 10)),
                   (77, (33, 21, 11)), (82, (30, 20, 10)), (86, (27, 14, 8)), (92, ((20, 0, 2), (0, 0, 0.02))), (96, (2, -1, 0)), (101, (8, 0, 0))],
        "Head": [(4, (12, 4, 0)), (6, (22, 8, 6)), (9, (-6, 0, 0)), (13, (26, 8, 14)), (18, (38, 9, 22)), (40, SLUMPED["Head"]),
                 (55, (38, 10, 22)), (62, (42, 11, 26)), (70, (41, 10, 25)), (82, (40, 10, 24)), (88, (30, 0, 18)), (93, (-8, 0, 4)),
                 (97, (-14, 0, -6)), (102, (-6, 0, -2))],
        "Hood": [(5, (6, 0, 2)), (8, (8, 0, 4)), (11, (-14, 0, 0)), (15, (18, 0, 6)), (20, (14, 0, 6)), (40, SLUMPED["Hood"]),
                 (62, (20, 0, 10)), (66, (15, 0, 7)), (84, SLUMPED["Hood"]), (93, (24, 0, 0)), (97, (-10, 0, 0)), (101, (4, 0, 0))],
        "LeverL": [(2, (5, 0, 0)), (5, (-6, 0, 0)), (8, (5, 0, 0)), (12, (4.2, 0, 0)), (20, (4.8, 0, 0)), (60, (4.8, 0, 0)),
                   (62, (3.3, 0, 0)), (65, (4.8, 0, 0)), (88, (4.8, 0, 0)), (90, (-14, 0, 0)), (94, (-10, 0, 0)), (98, (1.2, 0, 0))],
        "LeverR": [(2, (5, 0, 0)), (6, (-6, 0, 0)), (9, (5, 0, 0)), (13, (4.2, 0, 0)), (20, (4.8, 0, 0)), (89, (4.8, 0, 0)),
                   (91, (-14, 0, 0)), (95, (-10, 0, 0)), (99, (1.2, 0, 0))],
        "Lantern": [(3, (-28, 0, 0)), (7, (20, 4, 0)), (11, (-14, -2, 0)), (15, (9, 2, 0)), (20, (-5, 0, 0)), (25, (3, 0, 0)),
                    (31, (-1, 0, 0)), (38, (0, 0, 0)), (59, (3, 2, 0)), (63, (-2, 0, 0)), (68, (0, 0, 0)), (90, (0, 0, 0)),
                    (93, (6, 3, 0)), (97, (-4, -2, 0)), (101, (3, 1, 0))],
        "Reel": [(3, (14, 0, 0)), (6, (-6, 0, 0)), (9, (3, 0, 0)), (12, (0, 0, 0)), (90, (0, 0, 0)), (93, (-20, 0, 0)), (98, (0, 0, 0))],
        **{w: [(f, crash_wheels(back)[0 if w.startswith("WheelB") else 1]) for f, back in CRASH_BACK] for w in WHEELS},
    })),
    # Stalled (looped): slumped and steaming, as the crash leaves it: it sags on its springs and lifts, the driver heaving
    # over the levers, his head lolled; once a loop a hiss shudders it (the frame twitches, a lever jerks, the hood swings).
    "Stalled": clip(39, keys(held(SLUMPED, 1), {
        "Body": [(20, body(2.2, 0, 0, -0.029)), (28, body(1.4, 0.5, 0, -0.016)), (30, body(2.3, -0.2, 0, -0.028)), (34, SLUMPED["Body"])],
        "Driver": [(12, (28, 20, 10)), (24, (33, 21, 10)), (30, (34, 22, 11)), (36, (31, 20, 10))],
        "Head": [(14, (37, 9, 22)), (26, (41, 10, 25)), (31, (43, 11, 27)), (37, (39, 10, 24))],
        "Hood": [(16, (13, 0, 7)), (29, (17, 0, 9)), (33, (20, 0, 10)), (38, (15, 0, 8))],
        "LeverL": [(28, (4.8, 0, 0)), (30, (3.6, 0, 0)), (33, (4.8, 0, 0))],
        "Lantern": [(29, (3, 2, 0)), (33, (-2, 0, 0)), (37, (0.5, 0, 0))],
    }), periodic=True),
    # SweepWindup (0.7 s): a twitch the other way first (the driver steering left, the front dipping), then he yanks the
    # levers over and swings its front round to its right on its back wheels (they roll against each other), rearing a
    # little, the frame leaning out; his head stays on you, turning against the machine; the reel wound back, cocked.
    "SweepWindup": clip(14, keys(held(BASE, 1), held(WOUND, 15), {
        "Root": [(4, root(yaw=-6, at=REAR)), (11, root(yaw=29, at=REAR)), (13, root(yaw=34, at=REAR))],
        "Body": [(4, body(1, 1.2, 0, -0.008)), (9, body(-2, -2.5, 0, 0.004)), (13, body(-3.5, -1.5, 0, -0.012))],
        "Driver": [(5, (8, -3, 6)), (9, (14, 8, -8)), (13, (12, 5, -18))],
        "Head": [(5, (-4, 0, 6)), (10, (-2, 0, -8)), (14, (-7, 0, -20))],
        "Hood": [(6, (-3, 0, -4)), (11, (-6, 0, 12))],
        "LeverL": [(4, (-10, 0, 0)), (8, (4.8, 0, 0))],
        "LeverR": [(4, (2.4, 0, 0)), (8, (-18, 0, 0))],
        "Lantern": [(7, (0, -3, 0)), (12, (4, 10, 0))],
        "Reel": [(6, (40, 0, 0))],
        "WheelBL": [(4, turning(-6)[0]), (11, turning(29)[0]), (13, turning(34)[0])],
        "WheelBR": [(4, turning(-6)[1]), (11, turning(29)[1]), (13, turning(34)[1])],
    })),
    # Sweep (0.3 s; the flails strike 0.4 in, as the front crosses in front of it): whipped round across to its left,
    # the nose slamming down so the flails go through at knee height, the reel whirling; the driver thrown after it, his
    # head still on you, the hood and the lantern flung out.
    "Sweep": clip(7, keys(held(WOUND, 1), held(SWEPT, 8), {
        "Root": [(2, root(yaw=24, at=REAR)), (3, root(yaw=8, at=REAR)), (4, root(yaw=-10, at=REAR)), (5, root(yaw=-26, at=REAR)),
                 (6, root(yaw=-34, at=REAR)), (7, root(yaw=-38, at=REAR))],
        "Body": [(3, body(3.5, 0, 0, -0.015)), (5, body(4, 2.5, 0, -0.01))],
        "Driver": [(3, (14, 2, -4)), (5, (18, -8, 12))],
        "Head": [(4, (-4, 0, 0)), (6, (0, 0, 18))],
        "Hood": [(4, (-6, 0, 6)), (6, (8, 0, -10))],
        "LeverL": [(3, (-16, 0, 0))],
        "LeverR": [(3, (5, 0, 0))],
        "Lantern": [(4, (2, 12, 0)), (6, (0, -5, 0))],
        "Reel": [(2, (12, 0, 0)), (3, (-14, 0, 0)), (4, (-42, 0, 0)), (5, (-70, 0, 0)), (6, (-96, 0, 0)), (7, (-120, 0, 0))],
        "WheelBL": [(3, turning(8)[0]), (5, turning(-26)[0]), (7, turning(-38)[0])],
        "WheelBR": [(3, turning(8)[1]), (5, turning(-26)[1]), (7, turning(-38)[1])],
    })),
    # Recover (0.75 s, after the sweep): carried on past by the weight, then it rocks back the other way; the driver
    # hauls the levers over and steers it round to face you again (a little past, and back), the reel spinning down,
    # the driver, his head and the hood swinging back after it.
    "Recover": clip(17, keys(held(SWEPT, 1), held(dict(BASE, Reel=(-360, 0, 0)), 18), {
        "Root": [(4, root(yaw=-41, at=REAR)), (8, root(yaw=-30, at=REAR)), (13, root(yaw=-2, at=REAR)), (15, root(yaw=2.5, at=REAR))],
        "Body": [(4, body(1, 3.5, 0, 0)), (7, body(-1, -1.5, 0, 0.008)), (11, body(0.5, 1, 0, -0.004)), (15, body(-0.5, -1.2, 0, 0.004))],
        "Driver": [(5, (18, -10, 18)), (9, (10, 4, 6)), (13, (8, -3, -2)), (16, (5, 1, 0))],
        "Head": [(6, (6, 0, 26)), (10, (-6, 0, 8)), (14, (-4, 0, -4))],
        "Hood": [(6, (8, 0, -16)), (11, (-8, 0, 4)), (15, (4, 0, -2))],
        "LeverL": [(6, (-16, 0, 0)), (8, (4.2, 0, 0)), (14, (1.2, 0, 0))],
        "LeverR": [(6, (4.8, 0, 0)), (8, (-14, 0, 0)), (14, (-4, 0, 0))],
        "Lantern": [(5, (0, -5, 0)), (9, (3, 10, 0)), (13, (-2, -3, 0)), (16, (1, 2, 0))],
        "Reel": [(5, (-200, 0, 0)), (10, (-262, 0, 0)), (14, (-310, 0, 0))],
        "WheelBL": [(4, turning(-41)[0]), (8, turning(-30)[0]), (13, turning(-2)[0]), (15, turning(2.5)[0])],
        "WheelBR": [(4, turning(-41)[1]), (8, turning(-30)[1]), (13, turning(-2)[1]), (15, turning(2.5)[1])],
    })),
    # Vent (0.5 s; the steam bursts as it starts): it bucks right off its wheels, the frame flung up on its springs, the
    # driver yanking the valve lever and ducking, his hood blown up; it lands hard (compressed, the driver slumped by it),
    # hops once more, and settles.
    "Vent": clip(11, keys(held(BASE, 1), held(BASE, 12), {
        "Root": [(2, root(lift=(0, 0, 0.05))), (3, root(-1.5, lift=(0, 0, 0.13))), (4, root(-1, lift=(0, 0, 0.12))),
                 (6, root(0.5, at=FRONT)), (8, root(lift=(0, 0, 0.025))), (9, root())],
        "Body": [(2, body(1, 0, 0, -0.02)), (4, body(-2, 1.2, 0, 0.02)), (6, body(2.5, -1, 0, -0.035)), (8, body(-1, 0.6, 0, 0.01)),
                 (10, body(0.5, -0.2, 0, -0.006))],
        "Driver": [(2, ((10, 0, 0), (0, 0, -0.02))), (3, (17, 4, 0)), (5, (-6, 2, 0)), (7, (15, -4, 0)), (9, (2, 1, 0))],
        "Head": [(4, (22, 0, 0)), (6, (-20, 0, 0)), (8, (14, 0, 0)), (10, (-8, 0, 0))],
        "Hood": [(3, (-30, 0, 0)), (5, (-36, 0, 4)), (7, (10, 0, -4)), (9, (-6, 0, 0))],
        "LeverL": [(2, (-12, 0, 0)), (5, (-8, 0, 0)), (8, (1.2, 0, 0))],
        "LeverR": [(3, (-12, 0, 0)), (6, (-6, 0, 0)), (9, (0.9, 0, 0))],
        "Lantern": [(4, (6, 8, 0)), (6, (-16, -4, 0)), (8, (10, 6, 0)), (10, (-4, -1, 0))],
    })),
    # Stagger (made 0.43 s; a perfect block's stun plays it slowed to 1.8 s): the flails knocked back, the front thrown
    # up off its wheels, the reel kicked back, the levers kicked; the driver thrown back, his head whipping back; the
    # front slams down, the driver lurches forward; it coughs and wobbles, the driver shaking his head, and settles.
    "Stagger": clip(13, keys(held(BASE, 1), held(BASE, 14), {
        "Root": [(2, root(-5, at=REAR)), (4, root(1, at=FRONT)), (6, root(-1, at=REAR)), (8, root())],
        "Body": [(2, body(-2.5, 1.5, 0, 0.02)), (4, body(3.5, -1.5, 0, -0.03)), (6, body(-1, 1, 0, 0.01)), (8, body(1, -0.5, 0, -0.008)),
                 (10, body(-0.6, 0.8, 0, 0.008)), (12, body(0.4, -0.3, 0, -0.004))],
        "Driver": [(3, (-18, 3, 0)), (5, (20, -6, 0)), (8, (8, 4, 6)), (11, (10, -2, -4))],
        "Head": [(3, (-6, 0, 0)), (4, (-28, 0, 0)), (6, (16, 0, 0)), (8, (-6, 0, 14)), (10, (-2, 0, -12)), (12, (-6, 0, 4))],
        "Hood": [(4, (18, 0, 0)), (6, (-20, 0, 0)), (8, (8, 0, -10)), (10, (-4, 0, 8)), (13, (2, 0, 0))],
        "LeverL": [(2, (-20, 0, 0)), (5, (3, 0, 0)), (8, (-4, 0, 0)), (11, (0.6, 0, 0))],
        "LeverR": [(3, (-18, 0, 0)), (6, (2.4, 0, 0)), (9, (-3, 0, 0))],
        "Lantern": [(3, (-20, 0, 0)), (5, (18, 6, 0)), (7, (-10, -3, 0)), (9, (6, 2, 0)), (11, (-3, 0, 0))],
        "Reel": [(2, (35, 0, 0)), (5, (-10, 0, 0)), (8, (4, 0, 0)), (11, (0, 0, 0))],
    })),
    # Death (made 0.7 s, played over 1.2 s; then it dissolves): a last gasp (the frame bucks, the driver jerks upright,
    # head thrown back), then the front-left wheel buckles out from under it and it lists over onto that corner, nose
    # down, sagging on its springs, the reel turning its last turns; the driver falls forward and slumps off the seat to
    # the left, his head lolling, the hood flopping, the lantern swinging and hanging.
    "Death": clip(21, keys(held(BASE, 1), {
        "Root": [(3, root(-2, at=REAR)), (5, root()), (7, root()), (10, tipped(6.5)), (13, tipped(4.5)), (16, tipped(6)), (22, tipped(5.6))],
        "Body": [(3, body(-4, 0, 0, 0.03)), (5, body(2, 0, 0, -0.02)), (7, body(-1, -1, 0, 0.01)), (10, body(0.4, -1.2, 0, -0.012)),
                 (13, body(-0.2, -0.4, 0, 0.0)), (16, body(0.4, -1, 0, -0.012)), (22, body(0.3, -0.8, 0, -0.01))],
        "WheelFL": [(7, (0, 0, 0)), (10, buckled(36, (0.03, 0, 0.05))), (13, buckled(26, (0.02, 0, 0.05))), (16, buckled(32, (0.03, 0, 0.06))),
                    (22, buckled(30, (0.03, 0, 0.055)))],
        "Driver": [(3, ((-14, 0, 0), (0, 0, 0.03))), (5, (-18, 0, 0)), (8, (6, -14, 0)), (12, (18, -30, 6)), (16, (22, -40, 8)),
                   (19, (19, -36, 8)), (22, (21, -38, 8))],
        "Head": [(4, (-30, 0, 0)), (7, (-34, 0, 0)), (11, (10, 0, -6)), (15, (32, -14, -14)), (18, (26, -10, -12)), (22, (33, -16, -16))],
        "Hood": [(5, (24, 0, 0)), (9, (-20, 0, 0)), (13, (8, 0, 6)), (17, (16, 0, 10)), (20, (8, 0, 8)), (22, (12, 0, 9))],
        "LeverL": [(3, (-16, 0, 0)), (8, (-10, 0, 0)), (12, (4.2, 0, 0)), (22, (5, 0, 0))],
        "LeverR": [(4, (-14, 0, 0)), (9, (-6, 0, 0)), (14, (1.8, 0, 0)), (22, (2.4, 0, 0))],
        "Lantern": [(4, (8, 4, 0)), (8, (-10, -2, 0)), (11, (6, 10, 0)), (14, (-2, 2, 0)), (17, (4, 7, 0)), (20, (1, 4, 0)), (22, (2, 5, 0))],
        "Reel": [(6, (-50, 0, 0)), (12, (-90, 0, 0)), (18, (-104, 0, 0)), (22, (-106, 0, 0))],
    })),
}


def build():
    oak, dark_oak = kit.mat("ThresherOak", "#6b4a30"), kit.mat("ThresherOakDark", "#4c3524")
    iron, cowl = kit.mat("ThresherIron_Metal", "#4a4a52"), kit.mat("ThresherCowl_Metal", "#5a5560")
    boiler, rust = kit.mat("ThresherBoiler_Metal", "#3d3a3f"), kit.mat("ThresherRust", "#7a4a2c")
    firebox = kit.mat("ThresherFire_Glow", "#ff8a3a")
    robe, hood = kit.mat("ThresherDriverRobe", "#5c6058"), kit.mat("ThresherDriverHood", "#41443e")
    eye, claw, belt = kit.mat("SouredEye_Glow", "#c8f07a"), kit.mat("Claw", "#d6cdb8"), kit.mat("ThresherChain", "#2c2a2e")
    lantern_glass = kit.mat("SouredLantern_Glow", "#b6f06a")
    flail = kit.mat("ThresherFlail_Metal", "#4a4a52")  # (the iron of the flails and their teeth alone: what the sweep's trail traces)
    parts = []
    B = "Body"
    # The frame: two long oak beams and cross-members, a floor of boards.
    for s in (-1, 1):
        parts.append(part("Beam", kit.cube(), oak, M((s * 0.6, 0.0, 0.66), scale=(0.12, 2.3, 0.16)), group=B))
    for y in (-1.0, -0.1, 0.8):
        parts.append(part("Cross", kit.cube(), dark_oak, M((0, y, 0.66), scale=(1.3, 0.12, 0.12)), group=B))
    parts.append(part("Boards", kit.cube(), dark_oak, M((0, 0.45, 0.76), scale=(1.1, 1.1, 0.05)), group=B))

    # Wheels: two great iron-tyred ones at the back, two small at the front (spoked), each on a bone of its own (they
    # roll; one buckles when it dies).
    def wheel(x, y, r, w, bone):
        out = [part("Tyre", kit.cylinder(14, r, w), iron, M((x, y, r), rot=(0, 90, 0)), group=bone),
               part("Hub", kit.cylinder(8, r * 0.22, w + 0.06), rust, M((x, y, r), rot=(0, 90, 0)), group=bone)]
        for k in range(6):
            a = k * 30
            out.append(part("Spoke", kit.cube(), oak, M((x, y, r), rot=(a, 0, 0), scale=(w * 0.6, 0.05, r * 1.8)), group=bone))
        return out
    for s, l in ((-1, "L"), (1, "R")):
        parts += wheel(s * 0.78, 0.72, 0.62, 0.14, "WheelB" + l)
        parts += wheel(s * 0.68, -0.68, 0.38, 0.1, "WheelF" + l)
    # The boiler amidships, its stack, the firebox glowing.
    parts += [
        part("Boiler", kit.cylinder(12, 0.38, 0.9), boiler, M((0, 0.0, 1.22)), group=B),
        part("BoilerCap", kit.sphere(10, 6, 0.38), boiler, M((0, 0.0, 1.67), scale=(1, 1, 0.4)), smooth=True, group=B),
        part("Band", kit.cylinder(12, 0.4, 0.05), rust, M((0, 0.0, 1.0)), group=B),
        part("Band", kit.cylinder(12, 0.4, 0.05), rust, M((0, 0.0, 1.45)), group=B),
        part("Stack", kit.cylinder(8, 0.09, 0.8), iron, M((0, -0.18, 2.05)), group=B),
        part("StackLip", kit.cylinder(8, 0.13, 0.08), iron, M((0, -0.18, 2.47)), group=B),
        part("Firebox", kit.cube(), firebox, M((0, -0.37, 0.98), scale=(0.22, 0.03, 0.14)), group=B),
    ]
    # The cowl over the reel, and the reel: an axle, two spoked ends and six toothed flail bars.
    # The cowl: an iron hood over the back of the reel (open over the front and below, where the flails come round).
    for k in range(5):
        a0 = math.radians(5 + k * 22)  # (0: straight back; 90: overhead: the front of the reel left bare, flails turning)
        y, z = REEL_Y + math.cos(a0) * 0.64 + 0.04, REEL_Z + math.sin(a0) * 0.64
        parts.append(part("Cowl", kit.cube(), cowl, M((0, y, z), rot=(math.degrees(a0) + 90, 0, 0), scale=(1.6, 0.26, 0.035)), group=B))
    parts.append(part("Axle", kit.cylinder(8, 0.05, 1.6), iron, M((0, REEL_Y, REEL_Z), rot=(0, 90, 0)), group="Reel"))
    for s in (-1, 1):
        parts.append(part("ReelEnd", kit.cylinder(10, 0.46, 0.04), rust, M((s * 0.7, REEL_Y, REEL_Z), rot=(0, 90, 0)), group="Reel"))
    for k in range(6):
        a = math.tau * k / 6
        y, z = REEL_Y + math.cos(a) * 0.44, REEL_Z + math.sin(a) * 0.44
        parts.append(part("Flail", kit.cube(), flail, M((0, y, z), rot=(math.degrees(a), 0, 0), scale=(1.42, 0.06, 0.1)), group="Reel"))
        for t in range(5):
            x = -0.56 + t * 0.28
            ty, tz = REEL_Y + math.cos(a) * 0.52, REEL_Z + math.sin(a) * 0.52
            parts.append(part("Tooth", kit.cone(4, 0.035, 0.0, 0.12), flail, kit.along((x, y, z), (x, ty, tz)), group="Reel"))
    # The drive chain: from the boiler down the right side to the reel (the Hook's to wrench off).
    chain = [(0.66, -0.05, 1.05), (0.7, -0.7, 0.95), (0.74, REEL_Y, REEL_Z)]
    parts.append(part("Chain", kit.tube(chain, [0.035, 0.035, 0.035], seg=5), belt, group=B))
    # The seat at the back, the levers (each on a bone: hauled back, thrown forward), and the driver grown into them.
    parts += [
        part("Seat", kit.cube(), oak, M((0, 0.98, 1.02), scale=(0.5, 0.4, 0.06)), group=B),
        part("SeatBack", kit.cube(), oak, M((0, 1.18, 1.25), rot=(-12, 0, 0), scale=(0.5, 0.05, 0.45)), group=B),
    ]
    for s, lever in ((-1, "LeverL"), (1, "LeverR")):
        parts.append(part("Lever", kit.cylinder(5, 0.02, 0.7), rust, kit.along(side(LEVER_FOOT, s), side(LEVER_TOP, s)), group=lever))
        parts.append(part("Knob", kit.sphere(6, 4, 0.04), oak, M((s * 0.22, 0.5, 1.46)), group=lever))
    D = "Driver"
    parts += [
        part("Robe", kit.cone(8, 0.3, 0.18, 0.55), robe, M((0, 0.95, 1.33), rot=(12, 0, 0)), smooth=True, group=D),
        part("Head", kit.sphere(9, 6, 0.15), hood, M((0, 0.78, 1.72)), smooth=True, group="Head"),
        part("Hood", kit.cone(8, 0.2, 0.04, 0.36), hood, M((0, 0.84, 1.82), rot=(-35, 0, 0)), smooth=True, group="Hood"),
        # (the roots he's grown into the levers by: on the frame, so they stay rooted whatever he does)
        part("Tendril", kit.tube([(0.12, 0.9, 1.15), (0.2, 0.72, 0.95), (0.22, 0.55, 0.8)], [0.03, 0.02, 0.012], seg=5), robe, group=B),
        part("Tendril", kit.tube([(-0.12, 0.9, 1.15), (-0.2, 0.75, 0.92), (-0.22, 0.56, 0.8)], [0.03, 0.02, 0.012], seg=5), robe, group=B),
    ]
    for s in (-1, 1):
        parts.append(part("Eye", kit.sphere(7, 5, 0.03), eye, M((s * 0.055, 0.64, 1.72)), smooth=True, group="Head"))
        arm, fore = ("ArmL", "ForeL") if s < 0 else ("ArmR", "ForeR")
        shoulder, elbow, hand = side(SHOULDER, s), side(ELBOW, s), side(HAND, s)
        # The arm in two (a sleeve to the elbow, a forearm to the claw on the lever's knob), the elbow a knob of robe.
        parts.append(part("Sleeve", kit.cone(6, 0.07, 0.055, math.dist(shoulder, elbow) + 0.03), robe, kit.along(shoulder, elbow), smooth=True, group=arm))
        parts.append(part("Elbow", kit.sphere(7, 5, 0.056), robe, M(elbow), smooth=True, group=arm))
        parts.append(part("Forearm", kit.cone(6, 0.052, 0.04, math.dist(elbow, hand)), robe, kit.along(elbow, hand), smooth=True, group=fore))
        parts.append(part("Claw", kit.cone(5, 0.03, 0.0, 0.08), claw, kit.along(hand, (s * 0.22, 0.47, 1.49)), group=fore))
    # Like every Soured, a green lantern: hung by the seat from an iron post on the frame, on a short cord (it swings).
    parts += [
        part("LanternPost", kit.cylinder(5, 0.018, 0.64), iron, M((0.62, 1.13, 1.06)), group=B),
        part("LanternCrook", kit.cylinder(5, 0.018, 0.1), iron, kit.along((0.62, 1.14, 1.37), (0.62, 1.04, 1.37)), group=B),
        part("LanternCord", kit.cylinder(4, 0.008, 0.14), iron, M((0.62, 1.05, 1.295)), group="Lantern"),
        part("BeltLanternTop", kit.cone(4, 0.07, 0.01, 0.05), iron, M((0.62, 1.05, 1.2), rot=(0, 0, 45)), group="Lantern"),
        part("BeltLanternGlass", kit.cube(), lantern_glass, M((0.62, 1.05, 1.12), scale=(0.09, 0.09, 0.12)), group="Lantern"),
        part("BeltLanternBase", kit.cube(), iron, M((0.62, 1.05, 1.05), scale=(0.11, 0.11, 0.015)), group="Lantern"),
    ]
    mesh = kit.join(parts, "Thresher")
    armature = kit.rig("ThresherRig", BONES)
    kit.skin(mesh, armature)
    kit.animate(armature, ACTIONS)
    for (name, _), worst in zip(ACTIONS.items(), REACH.values()):
        if worst > 0.02:
            print(f"[thresher] {name}: the hands come off the levers by up to {worst * 100:.0f} cm")
    return [armature, mesh]
