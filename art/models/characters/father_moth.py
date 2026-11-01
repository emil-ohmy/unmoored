"""Father Moth, Who Gathered Autumn (the Gardener, the Orchard's Boss): a vast moth, its wings like dead autumn leaves
(veined, blotched, ragged at the edges: six metres across), and rising from its thorax where its head should be, the
stooped old gardener in his apron and flat cap, a moth's feathered antennae growing from his brow. His forearms have
grown into its forelegs, long and jointed, each ending in a pair of pruning shears; four thin moth legs beneath. In his
ribs, caged, what looks like a small lantern and isn't: the Drowned Moon, the harvest moon's reflection he lifted out of the
river, a cold blue-white light (it glows, rippling in the game; it flares with his tell). In his belly,
below the apron, the autumn hollow: a ragged glowing opening ringed with dead leaves, that his leaves pour out of. The same sickly green eyes
as every Soured. Origin at the ground, where he stands when he lands (in flight the game holds him up).

Clips (in the order of his fight): Perched (folded on his tree like a dead leaf, breathing, before he wakes), Wake
(2 s: the wings unfold, he lifts his head, the first beat), Fly (his slow wingbeat, looping; also played once as a
beat's breath after). His beats: GustDraw (the tell: the wings raised high over his back) then Gust (one great beat
forward), Spin (wings flat out, turning: looping), FlickL / FlickR (one wing snapped down to throw); Shake (a shudder:
the pods' tell) and Tear (reaching up to the vault: the fruit's tell). Spent: Falter (weak, uneven beats, sagging),
Collapse (down onto the floor), Downed (sprawled, heaving), Rise (up off the floor, onto the wing) or Rouse (up onto
his forelegs, rearing, wings flung wide: also his cry in the air). Afoot (phase 3): Scuttle (coming on, hobbling on
his shears like an old man on two sticks: looping), Land (afoot, panting, shears ready: looping, and his recovery
after every blow), ShearsDraw / Shears (a shear drawn back and swept across his front: snip and sweep), ThrustDraw /
Thrust (both shears raised, then driven forward: his lunge), Block (his shears turning a blow), Stagger (a strike
turned on a perfect block: reeling, open). The last: Gather (arched back, wings and shears flung wide: drawing
everything in, twelve seconds), Release (snapped forward and down: letting it all go), Death (he folds and falls).
Unused by the fight, kept for the gallery: Stall, Dive, SwoopDraw, Swoop, Tend, Recover, Beat.

How it's animated (the Laden's way): each bone keyed on its own timing (kit.bake: tracks), then the loose parts
follow on their own: every wing's outer half (its own bone, the leaf bending smoothly across the middle), the hind
wings a beat behind the fore, the antennae, the abdomen's tail, the old man's head and his arms each trail what carries
them like a damped spring (lag, overshoot, settle), so nothing moves all at once. The four moth legs aren't keyed:
every frame they're bent (kit.reach) to plant their feet on the floor (afoot, sprawled) or to tuck them under him (on
the wing), blended by a "Plant" track; walking, his shear-tips plant on the floor like sticks the same way.
"""
import math

import bmesh
from mathutils import Euler, Matrix, Vector

M, part = kit.M, kit.part  # `kit` is injected by build_model.py

THORAX = (0.0, 0.0, 1.5)
SHOULDER = (0.32, -0.8, 2.72)
ELBOW = (0.48, -1.1, 2.02)
JOINT = (0.56, -1.5, 1.45)
WRIST = (0.55, -1.8, 0.85)
TIP = (0.55, -2.12, 0.12)       # (the shears' points)
WING_Z = 1.9
LEGS = {"FL": (-1, -0.05), "FR": (1, -0.05), "BL": (-1, 0.35), "BR": (1, 0.35)}  # (side, y)


def mirror(p, s):
    return (s * p[0], p[1], p[2])


def leg_points(s, y):
    return (s * 0.22, y, 1.2), (s * 0.95, y + 0.1, 1.15), (s * 1.15, y + 0.25, 0.02)  # hip, knee, foot


BONES = {
    "Root": ((0, 0, 0), (0, 0, 0.4), None),
    "Body": ((0, 0.05, 1.5), (0, -0.45, 1.5), "Root"),             # the thorax (pitches and rolls about its middle)
    "Abdomen": ((0, 0.35, 1.42), (0, 1.4, 1.27), "Body"),
    "AbdomenTip": ((0, 1.4, 1.27), (0, 2.4, 0.95), "Abdomen"),      # its tail half (the abdomen bends between)
    "Legs": ((0, 0.1, 1.2), (0, 0.1, 0.7), "Body"),                # the four moth legs' hub
    "Torso": ((0, -0.35, 1.72), (0, -0.78, 2.75), "Body"),         # the gardener (his lantern's light hangs from it)
    "Head": ((0, -0.82, 2.8), (0, -1.0, 3.3), "Torso"),
    "AntennaL": ((-0.06, -1.1, 3.2), (-0.56, -1.4, 3.9), "Head"),
    "AntennaR": ((0.06, -1.1, 3.2), (0.56, -1.4, 3.9), "Head"),
    "ArmL": (mirror(SHOULDER, -1), mirror(ELBOW, -1), "Torso"),     # his shirtsleeve, shoulder to elbow
    "ForeL": (mirror(ELBOW, -1), mirror(TIP, -1), "ArmL"),          # the foreleg, its shears' fixed blade to the point
    "SnipL": (mirror(WRIST, -1), mirror(TIP, -1), "ForeL"),         # the shears' moving blade
    "ArmR": (SHOULDER, ELBOW, "Torso"),
    "ForeR": (ELBOW, TIP, "ArmR"),
    "SnipR": (WRIST, TIP, "ForeR"),
    "Hollow": ((0, -0.6, 1.22), (0, -0.85, 1.22), "Body"),         # (the game scales it: it throbs)
    "WingL": ((-0.35, -0.1, WING_Z), (-1.6, -0.3, WING_Z), "Body"),
    "WingTipL": ((-1.6, -0.3, WING_Z), (-3.0, -0.55, WING_Z), "WingL"),
    "WingR": ((0.35, -0.1, WING_Z), (1.6, -0.3, WING_Z), "Body"),
    "WingTipR": ((1.6, -0.3, WING_Z), (3.0, -0.55, WING_Z), "WingR"),
    "HindL": ((-0.3, 0.3, WING_Z - 0.1), (-1.1, 0.6, WING_Z - 0.1), "Body"),
    "HindTipL": ((-1.1, 0.6, WING_Z - 0.1), (-2.0, 1.2, WING_Z - 0.1), "HindL"),
    "HindR": ((0.3, 0.3, WING_Z - 0.1), (1.1, 0.6, WING_Z - 0.1), "Body"),
    "HindTipR": ((1.1, 0.6, WING_Z - 0.1), (2.0, 1.2, WING_Z - 0.1), "HindR"),
}
for _leg, (_s, _y) in LEGS.items():
    _hip, _knee, _foot = leg_points(_s, _y)
    BONES["Leg" + _leg] = (_hip, _knee, "Legs")
    BONES["Shin" + _leg] = (_knee, _foot, "Leg" + _leg)


# ---------------------------------------------------------------- poses
# Rotations are degrees about world axes around each bone's head (a child's on its parent's). Body: +X pitches the nose
# down, +Y rolls his right side down. Torso and Head: +X bows forward, +Z turns him to his right (+X: his right).
# Abdomen: -X lets the tail hang. Snip: opens the shears.

def P(*parts, **bones):
    """A pose: the parts merged, then the bones given."""
    out = {}
    for p in parts:
        out.update(p)
    out.update(bones)
    return out


def R(dz=0.0, dy=0.0, dx=0.0):
    """An offset (metres): the root's, a foot's."""
    return ((0, 0, 0), (dx, dy, dz))


def PL(p):
    """The Plant track: 0, the moth legs tucked under him; 1, their feet on the floor."""
    return ((p, 0, 0), (0, 0, 0))


def wing(lift, sweep=0.0, twist=0.0, curl=0.0, side=None, hind=False):
    """Fore (or hind) wings lifted `lift` degrees (down: negative), swept back `sweep`, the leading edge twisted down
    `twist`, the outer half curled down `curl` (up: negative). `side`: "L" or "R" for one."""
    out = {}
    for n, s in (("L", -1), ("R", 1)):
        if side and n != side:
            continue
        out[("Hind" if hind else "Wing") + n] = (twist, -s * lift, s * sweep)
        out[("HindTip" if hind else "WingTip") + n] = (0.0, s * curl, 0.0)
    return out


def arm(lift, out=0.0, turn=0.0, bend=0.0, open_=0.0, side=None):
    """The forelegs swung forward and up `lift` degrees from hanging, out to the side `out` (in: negative), turned out
    about the vertical `turn`; the foreleg bent up at the elbow `bend`; the shears open `open_`."""
    res = {}
    for n, s in (("L", -1), ("R", 1)):
        if side and n != side:
            continue
        res["Arm" + n] = (-lift, -s * out, s * turn)
        res["Fore" + n] = (-bend, 0.0, 0.0)
        res["Snip" + n] = (0.0, -s * 0.9 * open_, s * 0.4 * open_)
    return res


def body(root=0.0, pitch=0.0, torso=0.0, head=0.0, abdomen=0.0, plant=0.0, roll=0.0, twist=0.0, look=0.0, legs=0.0, dy=0.0):
    """The rest of him: the root's height (and forward/back), the thorax's pitch and roll, the gardener's bow and turn,
    his head's nod and look, the abdomen, the moth legs (Legs: swung back; plant: their feet on the floor)."""
    return {"Root": R(root, dy), "Body": (pitch, roll, 0.0), "Abdomen": (abdomen, 0.0, 0.0), "Torso": (torso, 0.0, twist),
            "Head": (head, 0.0, look), "Legs": (legs, 0.0, 0.0), "Plant": PL(plant)}


ARMS, WINGS = ("Arm", "Fore", "Snip"), ("Wing", "Hind")


def held(pose, frame):
    """A whole pose as a key at `frame` for every bone (to start or end a clip on it)."""
    return {bone: [(frame, value)] for bone, value in pose.items()}


def keys(*parts):
    """Tracks merged (a later key on the same frame wins): each a {bone: [(frame, value)]}."""
    merged = {}
    for p in parts:
        for bone, ks in p.items():
            d = dict(merged.get(bone, []))
            for f, v in ks:
                d[f] = v
            merged[bone] = sorted(d.items())
    return merged


def dense(length, periodic, fn, **channels):
    """Tracks keyed every frame from a few channels each keyed on its own timing: channels {name: [(frame, value)]}
    (eased like kit.bake), fn(**{name: value}) -> {bone: value} at each frame."""
    baked = kit.bake({n: [(f, (v, 0, 0)) for f, v in ks] for n, ks in channels.items()}, length, periodic)
    first, last = min(f for ks in channels.values() for f, _ in ks), max(f for ks in channels.values() for f, _ in ks)
    out = {}
    for f, p in baked:
        if not periodic and not first <= f <= last:
            continue  # (keyed only between its first and last keys: other keys may take over around them)
        for bone, value in fn(**{n: p[n][0][0] for n in channels}).items():
            out.setdefault(bone, []).append((f, value))
    return out


def jitter(base, frames, amount, bones):
    """A tremble: each of `bones` nudged by +-amount (rx, ry, rz) on alternate frames of `frames` round its `base` value."""
    out = {}
    for k, f in enumerate(frames):
        s = 1 if k % 2 else -1
        for bone, (ax, ay, az) in bones.items():
            v = base[bone]
            rot, off = (v if isinstance(v[0], (tuple, list)) else (v, (0, 0, 0)))
            out.setdefault(bone, []).append((f, (tuple(r + s * a for r, a in zip(rot, (ax, ay, az))), tuple(off))))
    return out


# ---------------------------------------------------------------- the loose parts, the legs

RX, RY, RZ, DX, DY, DZ = range(6)
DEG = 57.2958

# (child, channel, drivers [(bone, channel, factor)], period (frames), damping ratio, gain): the child trails the sum of
# its drivers like a damped spring (lag, overshoot, settle); its keyed value plus gain * (where the spring is - where the
# drivers are). Group names (the first word) can be scaled per clip.
SECONDARY = []
for _n, _s in (("L", -1), ("R", 1)):
    SECONDARY += [
        ("Arm" + _n, RX, [("Body", RX, 1), ("Torso", RX, 1), ("Root", DZ, -DEG / 2.2), ("Root", DY, DEG / 2.2)], 11, 0.35, 0.5),
        ("Arm" + _n, RZ, [("Body", RZ, 1), ("Torso", RZ, 1)], 11, 0.35, 0.4),
        ("Arm" + _n, RY, [("Body", RY, 1), ("Torso", RY, 1)], 11, 0.35, 0.3),
        ("Fore" + _n, RX, [("Arm" + _n, RX, 1), ("Torso", RX, 1), ("Body", RX, 1)], 8, 0.35, 0.3),
        ("WingTip" + _n, RY, [("Wing" + _n, RY, 1), ("Body", RY, 1), ("Root", DZ, -_s * DEG / 1.0)], 7, 0.3, 0.75),
        ("WingTip" + _n, RX, [("Wing" + _n, RX, 1), ("Body", RX, 1)], 7, 0.3, 0.5),
        ("WingTip" + _n, RZ, [("Wing" + _n, RZ, 1), ("Body", RZ, 1)], 8, 0.35, 0.5),
        ("HindTip" + _n, RY, [("Hind" + _n, RY, 1), ("Body", RY, 1), ("Root", DZ, -_s * DEG / 0.8)], 6, 0.3, 0.7),
        ("HindTip" + _n, RZ, [("Hind" + _n, RZ, 1), ("Body", RZ, 1)], 7, 0.35, 0.5),
    ]
SECONDARY += [
    ("Head", RX, [("Body", RX, 1), ("Torso", RX, 1), ("Root", DZ, -DEG / 2.5)], 9, 0.45, 0.45),
    ("Head", RZ, [("Body", RZ, 1), ("Torso", RZ, 1)], 9, 0.45, 0.4),
    ("Head", RY, [("Body", RY, 1), ("Torso", RY, 1)], 9, 0.45, 0.4),
    ("Abdomen", RX, [("Body", RX, 1), ("Root", DZ, DEG / 1.6)], 12, 0.45, 0.35),
    ("Abdomen", RZ, [("Body", RZ, 1), ("Body", RY, 0.5)], 12, 0.45, 0.35),
    ("AbdomenTip", RX, [("Abdomen", RX, 1), ("Body", RX, 1), ("Root", DZ, DEG / 1.2)], 10, 0.4, 0.6),
    ("AbdomenTip", RZ, [("Abdomen", RZ, 1), ("Body", RZ, 1)], 10, 0.4, 0.5),
    ("Legs", RX, [("Body", RX, 1), ("Root", DZ, DEG / 1.5)], 10, 0.4, 0.5),
]
for _n in ("L", "R"):
    SECONDARY += [
        ("Antenna" + _n, RX, [("Body", RX, 1), ("Torso", RX, 1), ("Head", RX, 1), ("Root", DZ, -DEG / 0.7)], 6, 0.25, 0.8),
        ("Antenna" + _n, RZ, [("Body", RZ, 1), ("Torso", RZ, 1), ("Head", RZ, 1)], 6, 0.25, 0.8),
        ("Antenna" + _n, RY, [("Body", RY, 1), ("Torso", RY, 1), ("Head", RY, 1)], 6, 0.25, 0.6),
    ]


def spring(drive, period, zeta, periodic):
    """How far a damped spring following `drive` (one value a frame) is from it, each frame."""
    w = 2 * math.pi / period
    seq = drive[:-1] if periodic else drive

    def run(x, v):
        out = []
        for d in seq:
            for _ in range(4):
                v += (w * w * (d - x) - 2 * zeta * w * v) * 0.25
                x += v * 0.25
            out.append(x)
        return out, x, v
    x, v = drive[0], 0.0
    if periodic:
        for _ in range(6):
            _, x, v = run(x, v)
        out, _, _ = run(x, v)
        return [o - d for o, d in zip(out, seq)] + [out[0] - seq[0]]
    out, x, v = drive[0], 0.0, 0.0
    res = []
    for d in drive:
        for _ in range(4):
            v += (w * w * (d - out) - 2 * zeta * w * v) * 0.25
            out += v * 0.25
        res.append(out - d)
    res[0] = 0.0
    return res


def follow(poses, periodic, loose):
    """The hind wings a beat behind the fore (unless keyed); then every SECONDARY spring added on."""
    n = len(poses)
    zero = [0.0] * 6
    if "HindL" not in poses[0]:
        for i in range(n):
            j = (i - 2) % (n - 1) if periodic else max(0, i - min(2, n - 1 - i))  # (a one-off ends with them together)
            for side in ("L", "R"):
                w, t = poses[j].get("Wing" + side, zero), poses[j].get("WingTip" + side, zero)
                poses[i]["Hind" + side] = [w[0] * 0.6, w[1] * 0.85, w[2] * 0.85, 0, 0, 0]
                poses[i]["HindTip" + side] = [t[0] * 0.8, t[1] * 0.8, t[2] * 0.8, 0, 0, 0]
    for child, c, drivers, period, zeta, gain in SECONDARY:
        g = gain * loose.get(child.rstrip("LR") if child[-1] in "LR" and child not in ("Legs",) else child, 1.0)
        if g == 0:
            continue
        drive = [sum(f * p.get(b, zero)[ch] for b, ch, f in drivers) for p in poses]
        lag = spring(drive, period, zeta, periodic)
        if not periodic:  # (a one-off ends on its keyed pose: what follows it in play starts there)
            settle = max(1, min(8, n // 3))
            ramp = lambda u: u * u * (3 - 2 * u)
            lag = [l * ramp(min(1.0, (n - 1 - i) / settle)) for i, l in enumerate(lag)]
        for p, l in zip(poses, lag):
            v = p.setdefault(child, [0.0] * 6)
            v[c] += g * l


TUCKED = {leg: (s * 0.7, y + 1.3, 0.6) for leg, (s, y) in LEGS.items()}   # (the feet tucked back under him, flying)
FLOOR = {leg: leg_points(s, y)[2] for leg, (s, y) in LEGS.items()}        # (the feet where they stand)
REACH = []  # (the build's report: how far planted feet and shear-points come off their marks, at worst, per clip)


def floored(pose, n, s, clear=0.06):
    """On the floor, a foreleg that would go through it swung up about the shoulder instead, just clear of it (its
    points, wrist and knuckle), keeping its bend: it skids along the floor."""
    for _ in range(10):
        m = kit.pose_matrices(BONES, pose)
        fore = m["Fore" + n]
        pts = [fore @ Vector(mirror(p, s)) for p in (TIP, WRIST, JOINT)] + [m["Snip" + n] @ Vector(mirror(TIP, s))]
        low = min(pts, key=lambda p: p.z)
        if low.z >= clear - 0.005:
            break
        shoulder = m["Torso"] @ Vector(mirror(SHOULDER, s))
        out = low - shoulder
        flat = Vector((out.x, out.y, 0.0)) + Vector((s * 0.4, -0.8, 0.0))  # (it swings up forward and out: steady even straight down)
        axis = flat.normalized().cross(Vector((0, 0, 1)))  # (turning about it lifts the point)
        rate = axis.cross(out).z  # (how fast the point rises, a radian turned)
        if abs(rate) < 1e-3:
            break
        lift = Matrix.Rotation(max(-0.5, min(0.5, (clear - low.z) / rate)), 3, axis)
        parent = m["Torso"].to_3x3().normalized()
        rot, off = pose["Arm" + n] if isinstance(pose["Arm" + n][0], (tuple, list)) else (pose["Arm" + n], (0, 0, 0))
        local = Euler([math.radians(a) for a in rot]).to_matrix()
        new = (parent.inverted() @ lift @ parent @ local).to_euler("XYZ")
        pose["Arm" + n] = (tuple(math.degrees(a) for a in new), tuple(off))
    return pose


WING_EDGE = {"Wing": [(3.05, -0.3), (2.95, -0.72), (2.6, 0.12), (2.2, -0.98)], "Hind": [(2.2, 1.32), (1.6, 1.76), (2.0, 0.76)]}


def grounded(pose, clear=0.03):
    """On the floor, a wing whose outer half would go through it curled up off it instead (it lies on the floor)."""
    for kind, z0 in (("Wing", WING_Z - 0.015), ("Hind", WING_Z - 0.115)):
        for n, s in (("L", -1), ("R", 1)):
            tip = kind + "Tip" + n
            for _ in range(3):
                m = kit.pose_matrices(BONES, pose)
                low = min((m[tip] @ Vector((s * x, y, z0))).z for x, y in WING_EDGE[kind])
                if low >= clear:
                    break
                up = math.degrees(math.asin(min(1.0, (clear - low) / (1.5 if kind == "Wing" else 1.0)))) + 0.5
                rot, off = pose[tip] if tip in pose else ((0, 0, 0), (0, 0, 0))
                pose[tip] = ((rot[0], rot[1] + s * -up, rot[2]), off)
    return pose


def limbs(pose):
    """The moth legs bent to their feet (tucked or on the floor, by Plant); the shears to their points (TipL/TipR:
    ((1, 0, 0), where)) when a clip plants them. Returns the pose and how far, at worst, a planted foot or point misses."""
    plant = pose.get("Plant", PL(0))[0][0]
    targets = {}
    for leg, (s, y) in LEGS.items():
        off = Vector(pose.get("Foot" + leg, R())[1])

        def at(m, leg=leg, off=off):
            return (m["Legs"] @ Vector(TUCKED[leg])).lerp(Vector(FLOOR[leg]) + off, plant)
        pose = kit.reach(BONES, pose, "Leg" + leg, "Shin" + leg, at, (s, 0.0, 1.0))
        if plant > 0.99:
            targets["Shin" + leg] = Vector(FLOOR[leg]) + off
    for n, s in (("L", -1), ("R", 1)):
        tip = pose.get("Tip" + n)
        if tip and tip[0][0] > 0.5:
            pose = kit.reach(BONES, pose, "Arm" + n, "Fore" + n, lambda m, t=tip: Vector(t[1]), (s, 0.8, 0.0))
            targets["Fore" + n] = Vector(tip[1])
        elif plant > 0.5:
            pose = floored(pose, n, s)
    if plant > 0.5:
        pose = grounded(pose)
    worst = 0.0
    if targets:
        m = kit.pose_matrices(BONES, pose)
        for bone, t in targets.items():
            worst = max(worst, (m[bone] @ Vector(BONES[bone][1]) - t).length)
    return pose, worst


def clip(length, tracks, periodic=False, loose=None):
    """A clip `length` frames long (30 a second) from per-bone tracks, baked to every frame; the loose parts trailing
    (`loose`: a group's springs scaled, e.g. {"Arm": 0.3}); the legs (and planted shear-points) bent to their marks."""
    if periodic:  # (a loop's frame length+1 is its frame 1 again)
        tracks = {b: [(f, v) for f, v in ks if f <= length] for b, ks in tracks.items()}
    baked = kit.bake(tracks, length, periodic)
    poses = [{b: list(v[0]) + list(v[1]) for b, v in p.items()} for _, p in baked]
    follow(poses, periodic, loose or {})
    frames, worst = [], 0.0
    for (f, _), p in zip(baked, poses):
        pose, miss = limbs({b: (tuple(v[:3]), tuple(v[3:])) for b, v in p.items()})
        worst = max(worst, miss)
        frames.append((f, {b: v for b, v in pose.items() if b in BONES}))
    REACH.append(worst)
    return ("LINEAR", frames)


# ---------------------------------------------------------------- the poses he comes back to

# On the wing, the bottom of a beat (Fly starts here; every beat ends here): wings down, the gardener upright over
# them, looking down at you, his forelegs hanging forward, the moth legs tucked.
HOVER = P(body(0.04, -4, -6, 12, -4), wing(-30, 6, 6), arm(25, 4, 0, 10, 8))
# Tired (sinking): sagging over his wings.
TIRED = P(HOVER, body(0.0, -2, 0, 16, -8))
# The beat's tell: wings raised high over his back, rearing, the shears up and open before him.
RAISED = P(body(0.1, -14, -14, 6, -10), wing(60, 26, 22, -6), arm(54, 34, 0, 28, 28))
# A flick's start: the bottom of a beat (a flick comes after a flick, or out of his beat).
HALF = HOVER
# Sprawled on the floor, spent: the wings splayed flat, the gardener bowed face down, his shears flung out to the
# sides along the floor, the moth legs splayed.
SPRAWL_FEET = {"Foot" + leg: [(1, R(0.0, 0.1 if y > 0 else -0.25, s * 0.35))] for leg, (s, y) in LEGS.items()}
DOWNED = P(body(-0.56, 2, 50, 34, -4, plant=1, look=-22), wing(-20, 14, 0, 14), arm(0, 62, 0, -14, 6),
           {k: v[0][1] for k, v in SPRAWL_FEET.items()})
# Dead on the floor: further over, the wings swept back and limp, the shears fallen open.
FALLEN = P(body(-0.6, 2, 56, 40, -6, plant=1, look=12), wing(-24, 38, 0, 18), arm(0, 66, 0, -12, 30),
           {k: v[0][1] for k, v in SPRAWL_FEET.items()})
# Gathering: arched back, wings cupped forward, shears flung wide.
GATHER = P(body(0.0, -14, -36, -30, -6), wing(8, -22, 22, -20), arm(108, 64, 0, 10, 34))
# Folded on his tree (Perched): the wings swept right back and down over him like a dead leaf, their tips curled
# round, the gardener bowed asleep, the shears hanging, the legs half gripping the branch.
FOLD = P(body(0.0, 0, 38, 32, 6, plant=0.35, legs=-20), wing(-28, 82, 0, 24), arm(-4, -8, 0, -4, 0))


def scuttle_tracks(length=24):
    """Afoot, coming on (4 m/s, 3.4 m a loop): his shear-points planted like sticks by turns (each for a third of the
    loop, sliding back with the floor), the moth legs walking in pairs; he dips onto each stick, the gardener lurching
    after it, wings bouncing."""
    tracks = {}
    speed = 3.36 / length
    # The moth legs, in two pairs (front left with back right, then the others): each on the floor 13 frames (sliding
    # back 1.5 m), 11 lifting forward; at frame 1 all four are down (one pair just planted, the other about to lift),
    # so the stance he stands in (READY) is this frame.
    for leg, ph in (("FL", 0), ("BR", 0), ("FR", 12), ("BL", 12)):
        s = LEGS[leg][0]
        ks = []
        for f in range(1, length + 2):
            t = ((f - 1 - ph) % length) / length
            st = 13 / length
            if t <= st:
                dy, dz = -0.75 + 1.5 * (t / st), 0.0
            else:
                u = (t - st) / (1 - st)
                e = u * u * (3 - 2 * u)
                dy, dz = 0.75 - 1.5 * e, 0.28 * math.sin(math.pi * u)
            ks.append((f, R(dz, dy, s * 0.05)))
        tracks["Foot" + leg] = ks
    # The shear-points: each on the floor 8 frames, then swung forward in an arc (opening as they go, shut as they bite).
    for n, s, ph in (("L", -1, 0), ("R", 1, 12)):
        ks, snip = [], []
        for f in range(1, length + 2):
            t = ((f - 1 - ph) % length) / length
            st = 8 / length
            if t < st:
                y, z, o = -2.45 + 8 * speed * (t / st), 0.03, 4.0
            else:
                u = (t - st) / (1 - st)
                e = u * u * (3 - 2 * u)
                y, z, o = -2.45 + 8 * speed * (1 - e), 0.03 + 0.55 * math.sin(math.pi * u) ** 1.5, 4 + 22 * math.sin(math.pi * u)
            ks.append((f, ((1, 0, 0), (s * 0.62, y, z))))
            snip.append((f, (0.0, -s * 0.9 * o, s * 0.4 * o)))
        tracks["Tip" + n] = ks
        tracks["Snip" + n] = snip
    return tracks


# Afoot, ready (Land and Scuttle start here; every blow and every reaction ends here): stooped low over his shears like
# an old man over two sticks, the left planted before him, the right just lifted behind (exactly where his hobble's
# first frame has them, so standing and coming on join), wings half raised and back, the moth legs on the floor.
READY_BODY = P(body(-0.1, 8, 17, -14, -2, plant=1), wing(28, 42, 0, 6))


def canes(pose, frame=1):
    """The forelegs and feet as they are at `frame` of the hobble (the shear-points and feet where Scuttle plants them)."""
    tracks = scuttle_tracks()
    at = dict(pose)
    for n in ("L", "R"):
        at["Tip" + n] = dict(tracks["Tip" + n])[frame]
        at["Snip" + n] = dict(tracks["Snip" + n])[frame]
    feet = {"Foot" + leg: dict(tracks["Foot" + leg])[frame] for leg in LEGS}
    bent, _ = limbs({b: (v if isinstance(v[0], (tuple, list)) else (tuple(v), (0, 0, 0))) for b, v in P(at, feet).items()})
    return P({b: bent[b] for b in ("ArmL", "ForeL", "SnipL", "ArmR", "ForeR", "SnipR")}, feet)


READY = P(READY_BODY, canes(READY_BODY))


ACTIONS = {
    # Perched (2.6 s loop): folded on his tree like a dead leaf, the gardener bowed asleep over his folded shears. A slow
    # breath (the thorax rising, the abdomen pumping after it), the old man's head sinking lower and lifting; once, the
    # wings shiver like a leaf in a draught (the right first, then the left), an antenna twitching after.
    "Perched": clip(79, keys(held(FOLD, 1), {
        "Root": [(1, R(0.0)), (24, R(0.012)), (50, R(-0.004))],
        "Body": [(1, (0, 0, 0)), (26, (-1.5, 0, 0)), (52, (0.5, 0, 0))],
        "Abdomen": [(1, (6, 0, 0)), (30, (2, 0, 0)), (56, (7, 0, 0))],
        "Torso": [(1, (38, 0, 0)), (28, (36, 0, 1)), (54, (39, 0, -1))],
        "Head": [(1, (32, 0, 0)), (20, (36, 0, 2)), (44, (38, 0, 4)), (62, (30, 0, -2))],
    }, dense(79, True, lambda r, l: P(wing(-28 + r * 0.4, 82 - r, 0, 24 - r, side="R"), wing(-28 + l * 0.4, 82 - l, 0, 24 - l, side="L")),
             r=[(1, 0), (56, 0), (59, 5), (61, -1), (63, 3), (66, 0)], l=[(1, 0), (58, 0), (61, 4), (63, -1), (65, 2), (69, 0)])),
        periodic=True, loose={"Arm": 0.3}),
    # Wake (2 s): a stir (the antennae twitch), the old man lifts his head and straightens; the wings shiver, then unfold:
    # swept forward and lifted, the outer halves unfurling last; raised high, then the first great beat (the body lifting
    # off the branch, the legs letting go), ending at the bottom of a beat (Fly's start).
    "Wake": clip(47, keys(held(FOLD, 1), {
        "Root": [(18, R(-0.04)), (30, R(-0.08)), (38, R(0.14)), (44, R(0.08)), (48, HOVER["Root"])],
        "Body": [(10, (2, 0, 0)), (22, (-6, 0, 0)), (32, (-12, 0, 0)), (40, (4, 0, 0)), (48, HOVER["Body"])],
        "Abdomen": [(14, (8, 0, 0)), (30, (-6, 0, 0)), (48, HOVER["Abdomen"])],
        "Torso": [(8, (38, 0, 0)), (20, (14, 0, 0)), (30, (-12, 0, 0)), (40, (-2, 0, 0)), (48, HOVER["Torso"])],
        "Head": [(10, (34, 0, 0)), (16, (8, 0, 0)), (22, (-14, 0, 2)), (30, (-8, 0, 0)), (40, (16, 0, 0)), (48, HOVER["Head"])],
        "Legs": [(26, (-20, 0, 0)), (36, (6, 0, 0)), (48, (0, 0, 0))],
        "Plant": [(24, PL(0.35)), (36, PL(0.0))],
    }, {k: [(12, v)] for k, v in arm(-4, -8, 0, -4, 0).items()},
        {k: [(26, v)] for k, v in arm(40, 16, 0, 26, 24).items()},
        {k: [(36, v)] for k, v in arm(14, 6, 0, 4, 4).items()},
        {k: [(48, v)] for k, v in arm(25, 4, 0, 10, 8).items()},
        dense(47, False, lambda lift, sweep, curl, twist: wing(lift, sweep, twist, curl),
              lift=[(1, -28), (12, -28), (14, -22), (16, -30), (22, -6), (31, 72), (34, 76), (42, -36), (48, -30)],
              sweep=[(1, 82), (12, 82), (24, 30), (31, 22), (40, 8), (48, 6)],
              curl=[(1, 24), (20, 22), (30, -10), (36, -4), (48, 0)],
              twist=[(1, 0), (28, 16), (34, 14), (38, 12), (48, 6)])),
        loose={"Arm": 0.8}),
    # Fly (0.63 s loop; slower as he tires): a great slow wingbeat from the bottom: up (slower, the wings feathered, the
    # outer halves trailing down), then down (the power stroke, the tips bending up after it and whipping through at the
    # bottom); the hind wings a beat behind. The body heaves up on each downstroke, the gardener and his hanging shears
    # bobbing after it, his head after that; the antennae streaming.
    "Fly": clip(19, keys(held(HOVER, 1), {
        "Root": [(3, R(0.07)), (13, R(-0.06))],
        "Body": [(4, (-6, 0, 0)), (14, (-2, 0, 0))],
        "Abdomen": [(6, (-8, 0, 0)), (16, (-1, 0, 0))],
        "Torso": [(6, (-8, 0, 0)), (16, (-4, 0, 0))],
        "Head": [(8, (12, 0, 4)), (18, (10, 0, -3))],
    }, dense(19, True, lambda lift, sweep, twist: wing(lift, sweep, twist),
             lift=[(1, -30), (11, 55)], sweep=[(1, 6), (11, -4)], twist=[(1, 6), (5, -10), (11, -6), (15, 12)])),
        periodic=True),
    # GustDraw (the beat's tell, played over 0.5-0.6 s): a dip (the wings drop, he crouches over them), then the wings
    # heaved up high over his back and swept back in a great V (their faces turned to you: from above they loom), rearing,
    # the shears raised and opening before him, his head last; held there, trembling (the lantern flares).
    "GustDraw": clip(21, keys(held(HOVER, 1), {
        "Root": [(4, R(-0.04)), (14, R(0.12)), (18, R(0.09)), (22, RAISED["Root"])],
        "Body": [(4, (2, 0, 0)), (13, (-18, 0, 0)), (17, (-15, 0, 0)), (22, RAISED["Body"])],
        "Torso": [(5, (2, 0, 0)), (14, (-19, 0, 0)), (18, (-15, 0, 0)), (22, RAISED["Torso"])],
        "Head": [(6, (16, 0, 0)), (15, (0, 0, 0)), (22, RAISED["Head"])],
        "Abdomen": [(5, (-2, 0, 0)), (14, (-12, 0, 0)), (22, RAISED["Abdomen"])],
    }, {k: [(5, v)] for k, v in arm(18, 2, 0, 4, 4).items()},
        {k: [(15, v)] for k, v in arm(76, 32, 0, 34, 32).items()},
        {k: [(22, v)] for k, v in RAISED.items() if k.startswith(ARMS)},
        dense(21, False, lambda lift, sweep, twist: wing(lift, sweep, twist),
              lift=[(1, -30), (4, -36), (13, 66), (16, 58), (19, 61), (21, 59), (22, 60)],
              sweep=[(1, 6), (4, 4), (14, 28), (22, 26)], twist=[(1, 6), (5, 8), (12, 26), (22, 22)]))),
    # Gust (0.4 s; the leaves go at once): one great beat forward: the wings slammed down and forward, the body thrown
    # forward and up by it, the shears flung down; the tips whip through; then up, to the bottom of a beat (Fly's start).
    "Gust": clip(11, keys(held(RAISED, 1), {
        "Root": [(4, R(0.2)), (8, R(0.1)), (12, HOVER["Root"])],
        "Body": [(4, (12, 0, 0)), (7, (8, 0, 0)), (12, HOVER["Body"])],
        "Torso": [(3, (-10, 0, 0)), (5, (10, 0, 0)), (8, (2, 0, 0)), (12, HOVER["Torso"])],
        "Head": [(4, (0, 0, 0)), (6, (20, 0, 0)), (12, HOVER["Head"])],
        "Abdomen": [(4, (6, 0, 0)), (8, (-10, 0, 0)), (12, HOVER["Abdomen"])],
    }, {k: [(5, v)] for k, v in arm(40, 10, 0, 6, 6).items()},
        {k: [(12, v)] for k, v in HOVER.items() if k.startswith(ARMS)},
        dense(11, False, lambda lift, sweep, twist: wing(lift, sweep, twist),
              lift=[(1, 60), (4, -48), (7, -38), (12, -30)], sweep=[(1, 26), (4, -10), (8, 8), (12, 6)],
              twist=[(1, 22), (3, 16), (7, 4), (12, 6)]))),
}

# Spin (0.63 s loop: the game turns him): wings flat out and wide, fluttering (the tips trailing), the shears flung out
# before him; a leaf off each wingtip as it sweeps round.
SPREAD = P(body(0.0, 0, -6, 8, -4), wing(10, -2, 0), arm(36, 30, 0, 10, 16))
ACTIONS["Spin"] = clip(19, keys(held(SPREAD, 1), {
    "Root": [(5, R(0.04)), (15, R(-0.03))],
    "Body": [(1, (0, 2, 0)), (10, (0, -2, 0))],
    "Torso": [(4, (-8, 0, 2)), (14, (-4, 0, -2))],
    "Head": [(6, (10, 0, 0)), (16, (6, 0, 0))],
}, dense(19, True, lambda lift, twist: wing(lift, -2, twist), lift=[(1, 10), (10, -16)], twist=[(1, -4), (10, 6)])),
    periodic=True)


def flick(side):
    """FlickL / FlickR (0.3 s; from the bottom of a beat, where a flick or a beat ends): one wing flicked up and snapped
    down and forward to throw a leaf (the body rolling into it, the gardener turning after it, the shear on that side
    flung out), the other lifting a little; then both at the bottom of a beat again (Fly's start)."""
    s = -1 if side == "L" else 1
    o = "R" if side == "L" else "L"
    return clip(7, keys(held(HALF, 1), {
        "Root": [(2, R(0.02)), (4, R(0.1)), (8, HOVER["Root"])],
        "Body": [(2, (-6, -s * 3, 0)), (4, (4, s * 8, 0)), (6, (2, s * 5, 0)), (8, HOVER["Body"])],
        "Torso": [(4, (-2, 0, s * 12)), (8, HOVER["Torso"])],
        "Head": [(5, (14, 0, s * 10)), (8, HOVER["Head"])],
    }, {k: [(4, v)] for k, v in arm(50, 34, 10, 0, 26, side=side).items()},
        {k: [(8, v)] for k, v in HOVER.items() if k.startswith(ARMS)},
        dense(7, False, lambda lift, sweep, twist: wing(lift, sweep, twist, side=side), lift=[(1, -30), (2, 26), (4, -54), (6, -40), (8, -30)],
              sweep=[(1, 6), (2, 8), (4, -14), (8, 6)], twist=[(1, 6), (2, -10), (4, 16), (8, 6)]),
        dense(7, False, lambda lift: wing(lift, 6, 6, side=o), lift=[(1, -30), (3, -10), (5, -14), (8, -30)])))


ACTIONS["FlickL"] = flick("L")
ACTIONS["FlickR"] = flick("R")

# Shake (the pods' tell, 0.5-0.6 s): a shudder through him: the wings trembling, the body rocking, the shears rattling,
# the head shaking; ending half raised (a flick's start).
SHAKEN = P(HOVER, wing(18, 4, 0), arm(30, 12, 0, 12, 16))
ACTIONS["Shake"] = clip(25, keys(held(HOVER, 1), held(SHAKEN, 4),
                                 jitter(SHAKEN, range(5, 22, 2), 1, {"Body": (0, 6, 0), "Torso": (2, 0, 5), "Head": (3, 0, -8),
                                                                     "WingL": (0, 8, 0), "WingR": (0, 8, 0), "SnipL": (0, 14, -6), "SnipR": (0, 14, -6),
                                                                     "ArmL": (4, 0, 0), "ArmR": (4, 0, 0), "Root": (0, 0, 0)}),
                                 held(HALF, 26), {"Root": [(14, R(0.06)), (22, R(0.0)), (26, HALF["Root"])]}))

# Tear (the fruit's tell, 0.5-0.6 s): a crouch, then reaching up with both shears to the vault above (leaning back,
# looking up, the wings raised), the shears closing on it, a first tug; ending raised (the Gust tears it down).
ACTIONS["Tear"] = clip(31, keys(held(HOVER, 1), {
    "Root": [(6, R(-0.06)), (16, R(0.16)), (24, R(0.2)), (28, R(0.12)), (32, RAISED["Root"])],
    "Body": [(6, (4, 0, 0)), (16, (-22, 0, 0)), (24, (-24, 0, 0)), (28, (-18, 0, 0)), (32, RAISED["Body"])],
    "Torso": [(7, (6, 0, 0)), (17, (-30, 0, 0)), (24, (-32, 0, 0)), (28, (-22, 0, 0)), (32, RAISED["Torso"])],
    "Head": [(8, (18, 0, 0)), (18, (-28, 0, 0)), (25, (-30, 0, 0)), (29, (-12, 0, 0)), (32, RAISED["Head"])],
    "Abdomen": [(7, (0, 0, 0)), (18, (-14, 0, 0)), (32, RAISED["Abdomen"])],
}, {k: [(7, v)] for k, v in arm(10, 4, 0, 0, 6).items()},
    {k: [(17, v)] for k, v in arm(150, 10, 0, -6, 32).items()},
    {k: [(22, v)] for k, v in arm(146, 8, 0, -4, 0).items()},
    {k: [(27, v)] for k, v in arm(128, 10, 0, 10, 0).items()},
    {k: [(32, v)] for k, v in RAISED.items() if k.startswith(ARMS)},
    dense(31, False, lambda lift, sweep, twist: wing(lift, sweep, twist),
          lift=[(1, -30), (6, -36), (17, 60), (24, 64), (28, 60), (32, 60)], sweep=[(1, 6), (17, 22), (32, 26)], twist=[(1, 6), (16, 14), (32, 22)])))

# Falter (spent, 1.8 s: the game sinks him): weak, uneven beats, each lower and smaller, the body rolling (the game
# rocks him too), the gardener sagging over, his head drooping, the shears dangling, the moth legs letting down; ending
# drooped, the wings hanging (Collapse's start).
DROOP = P(body(0.0, 6, 30, 40, -14, legs=-50), wing(-38, 26, 0, 18), arm(4, 8, 0, 0, 4))
ACTIONS["Falter"] = clip(33, keys(held(TIRED, 1), {
    "Root": [(6, R(0.05)), (10, R(-0.05)), (13, R(0.02)), (17, R(-0.08)), (20, R(-0.02)), (25, R(-0.1)), (34, DROOP["Root"])],
    "Body": [(6, (-4, -5, 0)), (12, (0, 6, 0)), (19, (2, -7, 0)), (26, (4, 8, 0)), (34, DROOP["Body"])],
    "Torso": [(8, (4, 0, 2)), (14, (14, 0, -3)), (20, (12, 0, 3)), (27, (24, 0, -2)), (34, DROOP["Torso"])],
    "Head": [(9, (20, 0, 4)), (16, (30, 0, -6)), (22, (26, 0, 6)), (29, (40, 0, -4)), (34, DROOP["Head"])],
    "Abdomen": [(10, (-10, 0, 0)), (20, (-12, 0, 0)), (34, DROOP["Abdomen"])],
    "Legs": [(12, (-10, 0, 0)), (24, (-30, 0, 0)), (34, DROOP["Legs"])],
}, {k: [(18, v)] for k, v in arm(14, 8, 0, 4, 6).items()},
    {k: [(34, v)] for k, v in DROOP.items() if k.startswith(ARMS)},
    dense(33, False, lambda l, r, sweep, curl: P(wing(l, sweep, 0, curl, side="L"), wing(r, sweep, 0, curl, side="R")),
          l=[(1, -30), (5, 40), (9, -24), (12, 26), (16, -30), (19, 12), (24, -34), (28, 0), (34, -38)],
          r=[(1, -30), (6, 36), (10, -22), (13, 20), (17, -28), (21, 8), (25, -32), (30, -4), (34, -38)],
          sweep=[(1, 6), (20, 14), (34, 26)], curl=[(1, 0), (24, 8), (34, 18)])),
    loose={"Arm": 1.2})

# Collapse (0.5 s: the game drops him to the floor in its first tenth): falling, the wings flung up by the air, the
# shears flung up; he hits (the body squashing down past where it rests, the wings slapping flat, the gardener thrown
# down over his shears, the head whipping after), a bounce, and he's sprawled (Downed's start).
ACTIONS["Collapse"] = clip(11, keys(held(DROOP, 1), {
    "Root": [(2, R(-0.04)), (5, R(-0.6)), (7, R(-0.48)), (9, R(-0.58)), (12, DOWNED["Root"])],
    "Body": [(3, (-6, 0, 0)), (5, (8, 0, 0)), (8, (2, 0, 0)), (12, DOWNED["Body"])],
    "Torso": [(3, (12, 0, 0)), (5, (40, 0, 4)), (7, (58, 0, 8)), (9, (48, 0, 6)), (12, DOWNED["Torso"])],
    "Head": [(4, (20, 0, 0)), (7, (44, 0, -16)), (9, (28, 0, -24)), (12, DOWNED["Head"])],
    "Abdomen": [(4, (4, 0, 0)), (6, (-12, 0, 0)), (9, (0, 0, 0)), (12, DOWNED["Abdomen"])],
    "Legs": [(4, (-20, 0, 0)), (12, (0, 0, 0))],
    "Plant": [(3, PL(1.0))],
}, {k: [(3, v)] for k, v in arm(40, 18, 0, 10, 10).items()},
    {k: [(6, v)] for k, v in arm(0, 58, 0, -12, 4).items()},
    {k: [(12, v)] for k, v in DOWNED.items() if k.startswith(ARMS) or k.startswith("Foot")},
    dense(11, False, lambda lift, sweep, curl: wing(lift, sweep, 0, curl),
          lift=[(1, -38), (3, 30), (5, -24), (7, -12), (9, -22), (12, -20)], sweep=[(1, 26), (4, 20), (12, 14)],
          curl=[(1, 18), (3, -18), (5, 30), (7, 12), (9, 24), (12, 22)])),
    loose={"Arm": 1.4})

# Downed (spent on the floor; played over 5 s, or 1.2 s as he stirs): sprawled, heaving: two slow breaths (the thorax
# lifting, the gardener's back rising and falling after it, the abdomen pumping), his head lolling; a wing twitches; the
# loop ends where it starts (Rise and Rouse start there).
ACTIONS["Downed"] = clip(59, keys(held(DOWNED, 1), {
    "Root": [(14, R(-0.52)), (30, R(-0.57)), (44, R(-0.53))],
    "Body": [(14, (1, 0, 0)), (30, (5, 0, 0)), (44, (2, 1, 0))],
    "Torso": [(16, (43, 0, 2)), (31, (54, 0, 0)), (46, (45, 0, -2))],
    "Head": [(18, (30, 0, -18)), (33, (35, 0, -23)), (48, (31, 0, -16))],
    "Abdomen": [(17, (-9, 0, 0)), (32, (-3, 0, 0)), (47, (-8, 0, 0))],
}, dense(59, True, lambda l, r: P(wing(-20 + l, 14, 0, 22 - l * 1.5, side="L"), wing(-20 + r, 14, 0, 22 - r * 1.5, side="R")),
         l=[(1, 0), (36, 0), (39, 7), (42, 2), (45, 4), (50, 0)], r=[(1, 0), (14, 1), (30, 0), (46, 2)])),
    periodic=True)

# Rise (0.9 s): up off the floor: the gardener pushes himself up on his shears, the wings heave up high, a great beat
# lifts him off (the legs letting go of the floor, tucking), then up and down again to the bottom of a beat (Fly's start).
ACTIONS["Rise"] = clip(19, keys(held(DOWNED, 1), {
    "Root": [(4, R(-0.55)), (8, R(-0.3)), (11, R(0.16)), (15, R(0.12)), (20, HOVER["Root"])],
    "Body": [(4, (8, 0, 0)), (8, (-10, 0, 0)), (12, (4, 0, 0)), (20, HOVER["Body"])],
    "Torso": [(4, (36, 0, 0)), (8, (2, 0, 0)), (12, (-10, 0, 0)), (20, HOVER["Torso"])],
    "Head": [(5, (30, 0, -8)), (9, (-6, 0, 0)), (14, (16, 0, 0)), (20, HOVER["Head"])],
    "Abdomen": [(5, (-10, 0, 0)), (11, (6, 0, 0)), (20, HOVER["Abdomen"])],
    "Plant": [(8, PL(1.0)), (12, PL(0.0))],
    "Legs": [(10, (10, 0, 0)), (20, (0, 0, 0))],
}, {k: [(4, v)] for k, v in arm(0, 50, 0, -10, 4).items()},
    {k: [(10, v)] for k, v in arm(36, 10, 0, 16, 14).items()},
    {k: [(20, v)] for k, v in HOVER.items() if k.startswith(ARMS)},
    {k: [(8, R())] for k in SPRAWL_FEET},
    dense(19, False, lambda lift, sweep, curl, twist: wing(lift, sweep, twist, curl),
          lift=[(1, -20), (3, -24), (8, 68), (11, -30), (15, 50), (20, -30)], sweep=[(1, 14), (8, 24), (12, 8), (20, 6)],
          curl=[(1, 22), (4, 26), (8, -6), (11, 6), (20, 0)], twist=[(1, 0), (7, -10), (10, 12), (14, -8), (20, 6)])))

# Rouse (1.5 s; also his cry in the air, 1.1-2.2 s): up off the floor onto his forelegs (pushing up on the shears),
# then rearing: wings flung wide and up, the shears flung up and open, the head thrown back (the cry), held there,
# trembling; then down onto his forelegs, ready (Land's start).
REAR = P(body(0.22, -20, -26, -22, 30, plant=1), wing(36, -14, 10, -12), arm(92, 62, 22, 30, 44))
ACTIONS["Rouse"] = clip(39, keys(held(DOWNED, 1), {
    "Root": [(5, R(-0.5)), (10, R(-0.12)), (15, R(0.26)), (18, R(0.2))],
    "Body": [(5, (10, 0, 0)), (10, (2, 0, 0)), (15, (-22, 0, 0)), (18, (-18, 0, 0))],
    "Torso": [(5, (38, 0, 0)), (10, (8, 0, 0)), (16, (-30, 0, 0)), (19, (-24, 0, 0))],
    "Head": [(6, (36, 0, -10)), (11, (6, 0, 0)), (17, (-28, 0, 0)), (20, (-20, 0, 0))],
    "Abdomen": [(6, (-8, 0, 0)), (16, (-14, 0, 0))],
    "Plant": [(1, PL(1.0))],
}, {k: [(5, v)] for k, v in arm(0, 50, 0, -10, 4).items()},
    {k: [(10, v)] for k, v in arm(18, 14, 0, 6, 10).items()},
    {k: [(15, v)] for k, v in arm(96, 66, 24, 30, 46).items()},
    {k: [(10, R()), (33, R()), (39, READY[k])] for k in SPRAWL_FEET},
    dense(39, False, lambda lift, sweep, curl: wing(lift, sweep, 10, curl),
          lift=[(1, -20), (5, -26), (10, 14), (15, 46), (18, 33), (20, 36)], sweep=[(1, 14), (10, 10), (15, -16), (20, -14)],
          curl=[(1, 22), (6, 26), (12, 4), (15, -14), (18, -8), (20, -12)]),
    jitter(REAR, range(21, 33, 2), 1, {"Torso": (2, 0, 2), "Head": (3, 0, 4), "WingL": (0, 5, 0), "WingR": (0, 5, 0),
                                       "SnipL": (0, 12, -5), "SnipR": (0, 12, -5), "Body": (1, 1, 0)}),
    {"Root": [(32, REAR["Root"]), (36, R(-0.14)), (40, READY["Root"])], "Body": [(32, REAR["Body"]), (37, (9, 0, 0)), (40, READY["Body"])],
     "Torso": [(33, (-24, 0, 0)), (37, (20, 0, -6)), (40, READY["Torso"])], "Head": [(34, (-20, 0, 0)), (38, (-4, 0, 6)), (40, READY["Head"])],
     "Abdomen": [(33, REAR["Abdomen"]), (40, READY["Abdomen"])], "Legs": [(1, (0, 0, 0))]},
    {k: [(32, REAR[k]), (37, v), (40, v)] for k, v in READY.items() if k.startswith(WINGS)},
    {k: [(31, REAR[k]), (40, v)] for k, v in READY.items() if k.startswith(ARMS)},
    {k: [(36, v)] for k, v in arm(36, 12, 0, 14, 10).items()}),
    loose={"Arm": 0.7})


ACTIONS.update({
    # Land (afoot, 0.63 s loop: close to you, ready; and his recovery after every blow): stooped over his shears,
    # panting: a heave in (rising, the wings lifting with it), a sag out; the shears give a little snip (an old habit);
    # his head on you.
    "Land": clip(19, keys(held(READY, 1), {
        "Root": [(8, R(-0.03)), (15, R(-0.11))],
        "Body": [(8, (3, 0, 0)), (15, (8, 0, 0))],
        "Torso": [(9, (12, 0, 2)), (16, (21, 0, -2))],
        "Head": [(10, (-17, 0, -3)), (17, (-12, 0, 4))],
        "Abdomen": [(10, (-5, 0, 0)), (17, (0, 0, 0))],
        "SnipL": [(9, arm(0, open_=24, side="L")["SnipL"]), (12, arm(0, open_=4, side="L")["SnipL"]), (15, arm(0, open_=14, side="L")["SnipL"])],
    }, dense(19, True, lambda lift, curl: wing(lift, 42, 0, curl), lift=[(1, 28), (9, 34), (16, 26)], curl=[(1, 6), (11, 2), (17, 9)])),
        periodic=True),
    # Scuttle (afoot, coming on: a 0.8 s loop): see scuttle_tracks.
    "Scuttle": clip(24, keys(held(READY, 1), {
        "Root": [(2, R(-0.17)), (8, R(-0.08)), (14, R(-0.17)), (20, R(-0.08))],
        "Body": [(3, (10, -3, 0)), (9, (6, 0, 0)), (15, (10, 3, 0)), (21, (6, 0, 0))],
        "Torso": [(3, (22, 0, 4)), (9, (15, 0, -1)), (15, (22, 0, -4)), (21, (15, 0, 1))],
        "Head": [(5, (-18, 0, -6)), (11, (-12, 0, 0)), (17, (-18, 0, 6)), (23, (-12, 0, 0))],
        "Abdomen": [(6, (-6, 0, 4)), (12, (-2, 0, 0)), (18, (-6, 0, -4)), (24, (-2, 0, 0))],
        "Legs": [(1, (0, 0, 0))],
    }, dense(24, True, lambda lift, curl: wing(lift, 42, 0, curl), lift=[(4, 21), (10, 34), (16, 21), (22, 34)],
             curl=[(6, 10), (12, 2), (18, 10), (24, 2)]),
        scuttle_tracks()), periodic=True),
})

# ShearsDraw (the snip's and the sweep's tell, 0.45-0.65 s): a little wind the other way, then the right shear drawn
# back out wide to his right and opened, the body turning after it, the left shear raised to guard; the head stays on
# you; the right wing lifting with the draw; held, creeping further.
DRAWN = P(body(-0.06, 2, 10, -10, -4, plant=1, twist=30, look=-26), wing(32, 34, 0, 4, side="L"), wing(50, 28, -6, -4, side="R"),
          arm(18, 76, 40, 30, 42, side="R"), arm(50, -6, -10, 40, 10, side="L"))
SWEPT = P(body(-0.14, 10, 22, -12, 0, plant=1, twist=-30, look=24), wing(34, 46, 6, 8, side="L"), wing(18, 50, 8, 10, side="R"),
          arm(40, -52, -50, 16, 0, side="R"), arm(22, 20, 6, 10, 8, side="L"))
ACTIONS["ShearsDraw"] = clip(16, keys(held(READY, 1), {
    "Root": [(4, R(-0.1)), (12, R(-0.05)), (17, DRAWN["Root"])],
    "Body": [(4, (8, 0, -4)), (12, (3, 0, 0)), (17, DRAWN["Body"])],
    "Torso": [(4, (18, 0, -14)), (12, (11, 0, 28)), (17, DRAWN["Torso"])],
    "Head": [(5, (-14, 0, 12)), (13, (-10, 0, -22)), (17, DRAWN["Head"])],
    "Abdomen": [(6, (-2, 0, 4)), (14, (-4, 0, -6)), (17, DRAWN["Abdomen"])],
}, {k: [(4, v)] for k, v in arm(18, -30, -16, 12, 4, side="R").items()},
    {k: [(12, v)] for k, v in arm(20, 72, 38, 28, 40, side="R").items()},
    {k: [(12, v)] for k, v in arm(48, -4, -8, 38, 10, side="L").items()},
    {k: [(17, v)] for k, v in DRAWN.items() if k.startswith(ARMS + WINGS)},
    {k: [(8, v)] for k, v in wing(36, 38, -4, 0, side="R").items()}))
# Shears (0.22-0.3 s; the blow lands at frame 4): the body turns, the right shear sweeps round across his front at your
# chest and snaps shut there (the extreme), carried on past to his left low, the left shear thrown back for balance;
# settling into the ready stance (Land's start), the wings flung after.
ACTIONS["Shears"] = clip(7, keys(held(DRAWN, 1), {k: [(1, v)] for k, v in READY.items() if k.startswith("Foot")}, {
    "Root": [(3, R(-0.1)), (5, R(-0.16)), (8, READY["Root"])],
    "Body": [(3, (6, 0, 0)), (5, (11, 0, 0)), (8, READY["Body"])],
    "Torso": [(3, (14, 0, 6)), (4, (18, 0, -22)), (6, (23, 0, -32)), (8, READY["Torso"])],
    "Head": [(3, (-12, 0, -10)), (5, (-12, 0, 18)), (8, READY["Head"])],
    "Abdomen": [(4, (0, 0, 6)), (6, (2, 0, 8)), (8, READY["Abdomen"])],
}, {k: [(3, v)] for k, v in arm(40, 44, 16, 22, 44, side="R").items()},
    {k: [(4, v)] for k, v in arm(56, -24, -28, 16, 0, side="R").items()},
    {k: [(6, v)] for k, v in SWEPT.items() if k.startswith(ARMS + WINGS)},
    {k: [(8, v)] for k, v in READY.items() if k.startswith(ARMS + WINGS)}),
    loose={"Arm": 0.3, "Fore": 0.5})

# ThrustDraw (the lunge's tell, 0.7 s): a crouch, then rearing back on his legs, both shears raised high and opened,
# the wings lifted to drive him; held, straining.
THRUST_DRAWN = P(body(0.06, -14, -24, -6, -10, plant=1, dy=0.12), wing(56, 12, -8, -6), arm(118, 18, 0, 34, 40))
THRUST = P(body(-0.16, 12, 26, -4, 4, plant=1, dy=-0.25), wing(-12, 52, 10, 8), arm(96, -10, -4, -14, 0))
ACTIONS["ThrustDraw"] = clip(16, keys(held(READY, 1), {
    "Root": [(5, R(-0.2)), (12, R(0.1, 0.12)), (17, THRUST_DRAWN["Root"])],
    "Body": [(5, (12, 0, 0)), (12, (-16, 0, 0)), (17, THRUST_DRAWN["Body"])],
    "Torso": [(5, (24, 0, -4)), (12, (-27, 0, 0)), (17, THRUST_DRAWN["Torso"])],
    "Head": [(6, (-16, 0, 4)), (13, (-4, 0, 0)), (17, THRUST_DRAWN["Head"])],
    "Abdomen": [(6, (2, 0, 0)), (13, (-12, 0, 0)), (17, THRUST_DRAWN["Abdomen"])],
}, {k: [(5, v)] for k, v in arm(12, 0, 0, 10, 8).items()},
    {k: [(12, v)] for k, v in arm(124, 20, 0, 36, 42).items()},
    {k: [(17, v)] for k, v in THRUST_DRAWN.items() if k.startswith(ARMS)},
    dense(16, False, lambda lift, sweep: wing(lift, sweep, -6, -4), lift=[(1, 28), (5, 18), (12, 60), (17, 56)], sweep=[(1, 42), (6, 36), (12, 12), (17, 12)])))
# Thrust (0.4 s; the game carries him across the floor in its first half; the blow at frame 5): driven forward and
# low, the wings beating back, both shears thrust out together at your chest, snapping shut (the extreme); carried on
# down by it, then settling into the ready stance (Land's start).
ACTIONS["Thrust"] = clip(9, keys(held(THRUST_DRAWN, 1), {k: [(1, v)] for k, v in READY.items() if k.startswith("Foot")}, {
    "Root": [(3, R(-0.06, -0.1)), (5, THRUST["Root"]), (7, R(-0.2, -0.18)), (10, READY["Root"])],
    "Body": [(3, (2, 0, 0)), (5, (14, 0, 0)), (7, (16, 0, 0)), (10, READY["Body"])],
    "Torso": [(3, (6, 0, 0)), (5, (26, 0, 0)), (7, (34, 0, -4)), (10, READY["Torso"])],
    "Head": [(3, (-12, 0, 0)), (6, (-6, 0, 0)), (8, (-2, 0, 4)), (10, READY["Head"])],
    "Abdomen": [(4, (6, 0, 0)), (7, (-4, 0, 0)), (10, READY["Abdomen"])],
}, {k: [(3, v)] for k, v in arm(100, 6, 0, 10, 44).items()},
    {k: [(5, v)] for k, v in THRUST.items() if k.startswith(ARMS)},
    {k: [(7, v)] for k, v in arm(70, -8, -4, -4, 0).items()},
    {k: [(10, v)] for k, v in READY.items() if k.startswith(ARMS)},
    dense(9, False, lambda lift, sweep, twist: wing(lift, sweep, twist), lift=[(1, 56), (4, -16), (6, -10), (10, 28)],
          sweep=[(1, 12), (4, 50), (7, 54), (10, 42)], twist=[(1, -8), (4, 12), (10, 0)])),
    loose={"Arm": 0.3, "Fore": 0.5})

# Block (0.3 s, afoot: his shears turn your blow): the shears snapped up crossed before his face, the blow shoving him
# back a step (the head snapping back after), then braced again (Land's start).
GUARDED = P(body(-0.1, -4, 2, -24, -6, plant=1, dy=0.14), wing(40, 30, 0, -4), arm(58, -26, -14, 62, 8))
ACTIONS["Block"] = clip(9, keys(held(READY, 1), {
    "Root": [(3, GUARDED["Root"]), (6, R(-0.12, 0.06)), (10, READY["Root"])],
    "Body": [(3, GUARDED["Body"]), (6, (4, 0, 0)), (10, READY["Body"])],
    "Torso": [(3, (0, 0, -2)), (6, (12, 0, -6)), (10, READY["Torso"])],
    "Head": [(4, (-28, 0, 0)), (7, (-10, 0, 6)), (10, READY["Head"])],
}, {k: [(3, v)] for k, v in GUARDED.items() if k.startswith(ARMS + WINGS)},
    {k: [(6, v)] for k, v in arm(46, -20, -12, 40, 8).items()},
    {k: [(10, v)] for k, v in READY.items() if k.startswith(ARMS + WINGS)}))

# Stagger (a strike turned on a perfect block: played over 1.6 s): knocked back off his feet (the shears thrown up and
# apart, the wings flaring, the head snapped back), reeling (a stumble back, sagging, the shears dropping), hanging
# there dazed, open; then gathering himself, ready (Land's start).
ACTIONS["Stagger"] = clip(15, keys(held(READY, 1), {
    "Root": [(3, R(0.04, 0.22)), (7, R(-0.18, 0.3)), (11, R(-0.2, 0.26)), (16, READY["Root"])],
    "Body": [(3, (-12, -4, 0)), (7, (8, 4, 0)), (11, (10, 2, 0)), (16, READY["Body"])],
    "Torso": [(3, (-28, 0, 6)), (7, (-8, 0, -6)), (11, (26, 0, 8)), (16, READY["Torso"])],
    "Head": [(2, (-34, 0, 4)), (6, (-12, 0, -12)), (10, (28, 0, 14)), (13, (14, 0, 8)), (16, READY["Head"])],
    "Abdomen": [(4, (8, 0, 0)), (8, (-10, 0, 0)), (16, READY["Abdomen"])],
}, {k: [(3, v)] for k, v in arm(76, 44, 10, 20, 40).items()},
    {k: [(7, v)] for k, v in arm(26, 32, 0, 0, 20).items()},
    {k: [(11, v)] for k, v in arm(30, 12, 0, 22, 10).items()},
    {k: [(16, v)] for k, v in READY.items() if k.startswith(ARMS)},
    dense(15, False, lambda lift, sweep, curl: wing(lift, sweep, 0, curl), lift=[(1, 28), (3, 64), (7, 6), (11, 14), (16, 28)],
          sweep=[(1, 42), (3, 20), (8, 46), (16, 42)], curl=[(1, 6), (3, -14), (7, 16), (11, 8), (16, 6)])),
    loose={"Arm": 1.2})

# Gather (twelve seconds: the clip crawls): from where his cry left him, arching back, the wings opening and cupping
# forward, the shears flung wide, his head thrown back; straining further and further, shuddering, the legs letting go.
ACTIONS["Gather"] = clip(11, keys(held(READY, 1), {
    "Plant": [(1, PL(1.0)), (3, PL(0.0))],
}, {k: [(3, v)] for k, v in P(GATHER, body(0.0, -10, -24, -18, -4), wing(18, -10, 12, -10), arm(96, 54, 0, 14, 26)).items() if k != "Plant"},
    {k: [(6, v)] for k, v in P(GATHER, body(0.02, -12, -31, -26, -5), wing(12, -18, 18, -16), arm(104, 60, 0, 12, 30)).items() if k != "Plant"},
    {k: [(9, v)] for k, v in P(GATHER, body(0.0, -13, -34, -28, -6), wing(10, -21, 20, -18), arm(106, 63, 0, 10, 32)).items() if k != "Plant"},
    {k: [(12, v)] for k, v in GATHER.items() if k != "Plant"},
    jitter(GATHER, [7, 8, 10, 11], 1, {"Torso": (1.5, 0, 1.5), "WingL": (0, 3, 0), "WingR": (0, 3, 0)})),
    loose={"Arm": 0.4, "WingTip": 0.4, "HindTip": 0.4, "Antenna": 0.4, "Head": 0.4})

# Release (0.7 s: the blast): snapped forward and down, the wings slammed down and back, the shears flung down; the
# tips and the head whip through; then he hangs there, emptied, the wings drooping (he sinks: Falter).
RELEASE = P(body(0.12, 24, 36, 20, 6), wing(-50, 40, 14, 10), arm(40, -20, 0, 0, 0))
ACTIONS["Release"] = clip(13, keys(held(GATHER, 1), {
    "Root": [(3, R(0.16)), (6, R(0.06)), (14, R(0.0))],
    "Body": [(3, (18, 0, 0)), (5, (24, 0, 0)), (9, (12, 0, 0)), (14, TIRED["Body"])],
    "Torso": [(3, (24, 0, 0)), (6, (42, 0, 0)), (10, (18, 0, 0)), (14, TIRED["Torso"])],
    "Head": [(4, (6, 0, 0)), (7, (34, 0, 0)), (11, (22, 0, 4)), (14, TIRED["Head"])],
    "Abdomen": [(4, (10, 0, 0)), (8, (-12, 0, 0)), (14, (-8, 0, 0))],
    "Legs": [(1, GATHER["Legs"])],
}, {k: [(3, v)] for k, v in RELEASE.items() if k.startswith(ARMS)},
    {k: [(7, v)] for k, v in arm(30, -16, 0, -4, 0).items()},
    {k: [(14, v)] for k, v in TIRED.items() if k.startswith(ARMS)},
    dense(13, False, lambda lift, sweep, curl, twist: wing(lift, sweep, twist, curl),
          lift=[(1, 8), (3, -52), (6, -42), (10, -26), (14, -30)], sweep=[(1, -22), (3, 30), (6, 40), (14, 6)],
          curl=[(1, -20), (3, 10), (6, 20), (10, 8), (14, 0)], twist=[(1, 22), (3, 16), (8, 6), (14, 6)])))

# Death (2 s; he's finished on the floor, sprawled, after his blast): a last heave up onto his shears, rearing, the
# wings flung up, the head thrown back (the cage in his ribs cracks: the moon pours out of him), then he folds: the
# wings closing up over him, the gardener crumpling down over his ribs, the shears giving way; he hits the floor (a
# bounce, the wings slapping down and splaying), a last twitch of a wing, the head sinking; still.
ACTIONS["Death"] = clip(35, keys(held(DOWNED, 1), {
    "Root": [(4, R(-0.4)), (8, R(-0.2)), (11, R(-0.26)), (15, R(-0.59)), (18, R(-0.54)), (22, R(-0.62)), (36, FALLEN["Root"])],
    "Body": [(4, (-8, 0, 0)), (8, (-16, 0, 0)), (12, (0, 0, 4)), (15, (8, 0, 4)), (19, (2, 0, 2)), (36, FALLEN["Body"])],
    "Torso": [(4, (20, 0, 0)), (8, (-14, 0, 0)), (12, (22, 0, 6)), (15, (58, 0, 8)), (18, (46, 0, 6)), (24, (56, 0, 4)), (36, FALLEN["Torso"])],
    "Head": [(4, (10, 0, -6)), (8, (-34, 0, 0)), (11, (-24, 0, 0)), (14, (30, 0, 8)), (17, (44, 0, 18)), (21, (32, 0, 10)), (30, (38, 0, 14)), (36, FALLEN["Head"])],
    "Abdomen": [(5, (0, 0, 0)), (9, (8, 0, 0)), (14, (-6, 0, 0)), (17, (2, 0, 0)), (36, FALLEN["Abdomen"])],
}, {k: [(4, v)] for k, v in arm(0, 50, 0, -10, 6).items()},
    {k: [(8, v)] for k, v in arm(60, 36, 0, 20, 40).items()},
    {k: [(12, v)] for k, v in arm(24, 20, 0, 6, 20).items()},
    {k: [(16, v)] for k, v in arm(0, 62, 0, -12, 26).items()},
    {k: [(36, v)] for k, v in FALLEN.items() if k.startswith(ARMS + ("Foot",))},
    dense(35, False, lambda l, r, sweep, curl: P(wing(l, sweep, 0, curl, side="L"), wing(r, sweep, 0, curl, side="R")),
          l=[(1, -20), (4, 10), (8, 70), (11, 78), (15, -24), (18, -12), (22, -24), (27, -24), (29, -14), (32, -24), (36, -24)],
          r=[(1, -20), (4, 6), (8, 66), (11, 76), (15, -22), (19, -14), (23, -24), (36, -24)],
          sweep=[(1, 14), (8, 26), (12, 30), (15, 36), (36, 38)], curl=[(1, 14), (5, 4), (8, -12), (12, 10), (15, 26), (18, 14), (22, 20), (36, 18)])),
    loose={"Arm": 1.2})

# ---- unused by the fight (the gallery shows them)
STALL = P(body(0.1, -30, -26, -14, -10), wing(24, -18, -8, -8), arm(85, 35, 0, 10, 28))
DIVE = P(body(-0.1, 30, 12, -8, 6), wing(8, 62, 6, 10), arm(72, 6, 0, -6, 20))
SWOOP = P(body(-0.1, 4, 20, -12, 0), wing(4, 70, 4, 6), arm(80, 14, 0, -6, 24))
ACTIONS.update({
    # Stall (0.63 s): rearing back in the air, wings flung wide, the shears thrown open; held, straining.
    "Stall": clip(19, keys(held(HOVER, 1), held(STALL, 7), {"Body": [(10, (-28, 0, 0)), (20, (-34, 0, 0))], "Torso": [(11, (-22, 0, 0)), (20, (-30, 0, 0))],
                                                          "Head": [(8, (-20, 0, 0)), (20, (-14, 0, 0))]},
                           jitter(STALL, range(12, 20, 2), 1, {"WingL": (0, 4, 0), "WingR": (0, 4, 0), "SnipL": (0, 8, 0), "SnipR": (0, 8, 0)}))),
    # Dive (0.63 s): pitching over, wings swept back, shears thrust ahead.
    "Dive": clip(19, keys(held(STALL, 1), {"Root": [(5, DIVE["Root"])], "Body": [(4, (10, 0, 0)), (8, (32, 0, 0)), (20, (34, 0, 0))],
                                           "Torso": [(6, (16, 0, 0)), (20, (12, 0, 0))], "Head": [(7, (-14, 0, 0)), (20, (-8, 0, 0))]},
                          {k: [(6, v)] for k, v in DIVE.items() if k not in ("Root", "Body", "Torso", "Head")})),
    # SwoopDraw (0.5 s): dropping low, the wings snapped right back.
    "SwoopDraw": clip(15, keys(held(HOVER, 1), held(P(SWOOP, Body=(-10, 0, 0)), 7), held(SWOOP, 16))),
    # Swoop (0.63 s loop): level and low, the swept wings fluttering.
    "Swoop": clip(19, keys(held(SWOOP, 1), {"Body": [(6, (6, 0, 0)), (16, (2, 0, 0))], "Root": [(5, R(-0.06)), (15, R(-0.13))]},
                           dense(19, True, lambda lift: wing(lift, 70, 4, 6), lift=[(1, 10), (10, -2)])), periodic=True),
    # Tend (1.3 s loop): on the ground, bent over a bed, snipping: right, then left.
    "Tend": clip(39, keys(held(P(READY, body(-0.12, 10, 40, 20, -2, plant=1)), 1),
                          {k: [(1, v), (8, v2), (11, v3), (16, v)] for (k, v), v2, v3 in zip(
                              arm(36, 0, 0, 50, 4, side="R").items(), arm(36, 4, 0, 44, 30, side="R").values(), arm(34, 4, 0, 46, 0, side="R").values())},
                          {k: [(1, v), (26, v), (29, v2), (32, v3), (37, v)] for (k, v), v2, v3 in zip(
                              arm(36, 0, 0, 50, 4, side="L").items(), arm(36, 4, 0, 44, 30, side="L").values(), arm(34, 4, 0, 46, 0, side="L").values())},
                          {"Torso": [(1, (40, 0, 0)), (10, (42, 0, 6)), (30, (42, 0, -6))], "Head": [(12, (22, 0, 8)), (31, (22, 0, -8))]},
                          dense(39, True, lambda lift: wing(lift, 40, 0, 6), lift=[(1, 30), (20, 36)])), periodic=True),
    # Recover (0.77 s): bowed after a blow, heaving, then back up ready.
    "Recover": clip(23, keys(held(READY, 1), {"Torso": [(8, (28, 0, -6)), (16, (24, 0, -6)), (24, READY["Torso"])],
                                              "Head": [(9, (10, 0, 6)), (24, READY["Head"])], "Root": [(8, R(-0.16)), (24, READY["Root"])]},
                             {k: [(9, v)] for k, v in arm(30, -10, -6, 24, 4).items()}, held(READY, 24))),
    # Beat (0.93 s): hammering the water below: great beats, one after another.
    "Beat": clip(28, keys(held(HOVER, 1), held(RAISED, 6), {"Body": [(9, (12, 0, 0)), (16, (12, 0, 0)), (23, (12, 0, 0))]},
                          dense(28, False, lambda lift: wing(lift, 6, 0), lift=[(1, -30), (6, 80), (9, -46), (13, 76), (16, -46), (20, 76), (23, -46), (29, -30)]),
                          {"Root": [(9, R(0.16)), (13, R(0.06)), (16, R(0.18)), (20, R(0.06)), (23, R(0.18)), (29, HOVER["Root"])]}, held(P(HOVER, Body=(4, 0, 0)), 29))),
})

# (the order the gallery lists them in)
ORDER = ["Perched", "Wake", "Fly", "GustDraw", "Gust", "Spin", "FlickL", "FlickR", "Shake", "Tear", "Falter", "Collapse",
         "Downed", "Rise", "Rouse", "Land", "Scuttle", "ShearsDraw", "Shears", "ThrustDraw", "Thrust", "Block", "Stagger",
         "Gather", "Release", "Death", "Stall", "Dive", "SwoopDraw", "Swoop", "Tend", "Recover", "Beat"]
REPORT = dict(zip(ACTIONS, REACH))
ACTIONS = {name: ACTIONS[name] for name in ORDER}


# ---------------------------------------------------------------- the model

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


def sliced(build, xs):
    """A flat part cut across at each x (so it can bend smoothly between its bones)."""
    def b(bm):
        build(bm)
        for x in xs:
            bmesh.ops.bisect_plane(bm, geom=bm.verts[:] + bm.edges[:] + bm.faces[:], plane_co=(x, 0, 0), plane_no=(1, 0, 0))
    return b


def along_line(points, radii, step):
    """A polyline (and its radii) with points added every `step` along each run: the same shape, more rings."""
    pts, rs = [points[0]], [radii[0]]
    for (a, ra), (b, rb) in zip(zip(points, radii), zip(points[1:], radii[1:])):
        n = max(1, int(math.dist(a, b) / step + 0.5))
        for k in range(1, n + 1):
            t = k / n
            pts.append(tuple(a[j] + (b[j] - a[j]) * t for j in range(3)))
            rs.append(ra + (rb - ra) * t)
    return pts, rs


def blend(ob, inner, outer, weight):
    """Skin a part across two bones: weight(co) of each vertex to `outer`, the rest to `inner` (a smooth bend)."""
    gi = ob.vertex_groups[inner]
    go = ob.vertex_groups.get(outer) or ob.vertex_groups.new(name=outer)
    for v in ob.data.vertices:
        w = max(0.0, min(1.0, weight(v.co)))
        w = w * w * (3 - 2 * w)
        gi.add([v.index], 1.0 - w, "REPLACE")
        go.add([v.index], w, "REPLACE")
    return ob


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
    light = kit.mat("FatherMothLantern_Glow", "#cfe3ff")  # (the Drowned Moon: cold)
    shear = kit.mat("ShearIron_Metal", "#6a6870")
    # (each shear's blades their own material, the same iron: a swipe trail finds them by name)
    blades = {"L": kit.mat("ShearBladeL_Metal", "#6a6870"), "R": kit.mat("ShearBladeR_Metal", "#6a6870")}

    # The abdomen bends smoothly between its two bones (Abdomen, AbdomenTip) round y 1.0-1.85.
    tail = lambda co: (co.y - 0.95) / 0.9
    abd_pts, abd_r = along_line([(0, 0.35, 1.45), (0, 1.0, 1.36), (0, 1.75, 1.18), (0, 2.4, 0.96)], [0.44, 0.42, 0.3, 0.07], 0.15)
    parts = [
        # The moth: thorax, furred; the abdomen behind, banded.
        part("Thorax", kit.sphere(12, 9, 0.52), fur, M(THORAX, scale=(1.0, 1.2, 0.88)), smooth=True, group="Body"),
        part("Collar", kit.sphere(12, 8, 0.4), fur_dark, M((0, -0.38, 1.72), scale=(1.0, 0.9, 0.55)), smooth=True, group="Body"),
        blend(part("Abdomen", kit.tube(abd_pts, abd_r, seg=10), fur, smooth=True, group="Abdomen"), "Abdomen", "AbdomenTip", tail),
    ]
    for k, (y, z, r) in enumerate([(0.75, 1.41, 0.435), (1.15, 1.33, 0.405), (1.55, 1.24, 0.35), (1.95, 1.12, 0.26)]):
        parts.append(blend(part("Band", kit.cylinder(10, r, 0.09), fur_dark, M((0, y, z), rot=(90 - 12 - k * 4, 0, 0)), smooth=True, group="Abdomen"),
                           "Abdomen", "AbdomenTip", tail))
    # The autumn hollow in his belly, low on the front of the moth below the gardener's apron, between his forelegs: a
    # ragged opening ringed with dead leaves curling back from it, an ember glow deep in it (the leaves he throws come
    # out of it; it throbs, and swells before a throw).
    parts += [
        part("HollowRim", kit.cylinder(14, 0.36, 0.14), kit.mat("HollowDark", "#1e140e"), M((0, -0.56, 1.22), rot=(80, 0, 0)), group="Hollow"),
        part("HollowGlow", kit.cylinder(14, 0.26, 0.04), kit.mat("FatherMothHollow_Glow", "#ff9a40"), M((0, -0.64, 1.23), rot=(80, 0, 0)), group="Hollow"),
    ]
    rim_leaf = kit.mat("HollowLeaf", "#8a4a20")
    for k in range(9):
        a = math.tau * k / 9 + 0.2
        base = (math.cos(a) * 0.32, -0.63, 1.22 + math.sin(a) * 0.32)
        tip = (math.cos(a) * 0.6, -0.7 - 0.05 * (k % 2), 1.22 + math.sin(a) * 0.6)
        parts.append(part("HollowLeaf", kit.cone(4, 0.09, 0.0, math.dist(base, tip)), rim_leaf, kit.along(base, tip), group="Hollow"))
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
    for s, n in ((-1, "L"), (1, "R")):
        parts.append(part("Eye", kit.sphere(6, 4, 0.03), eye, M((s * 0.075, -1.16, 3.08)), smooth=True, group="Head"))
        for k in range(3):
            base = (s * 0.17, -0.9 + k * 0.05, 3.0 - k * 0.04)
            parts.append(part("Hair", kit.cone(4, 0.035, 0.0, 0.14), hair, kit.along(base, (s * 0.25, -0.86 + k * 0.06, 2.92 - k * 0.05)), group="Head"))
        # The antennae, feathered, out of his brow (each on its own bone: they trail).
        stem = [(s * 0.06, -1.1, 3.2), (s * 0.18, -1.24, 3.52), (s * 0.36, -1.36, 3.78), (s * 0.56, -1.4, 3.9)]
        parts.append(part("Antenna", kit.tube(stem, [0.018, 0.014, 0.01, 0.005], seg=4), chitin, group="Antenna" + n))
        for k in range(1, 8):
            t = k / 8
            i = min(2, int(t * 3))
            u = t * 3 - i
            p = [stem[i][j] + (stem[i + 1][j] - stem[i][j]) * u for j in range(3)]
            for side in (-1, 1):
                parts.append(part("Barb", kit.cone(3, 0.008, 0.0, 0.12 * (1 - t * 0.5)), chitin,
                                  kit.along(tuple(p), (p[0] + side * 0.06, p[1] - 0.05, p[2] + 0.06)), group="Antenna" + n))
    # The caged "lantern" in his ribs (the Drowned Moon): ribs curving round it, a little cage, its light.
    lantern_at = (0, -1.1, 2.5)  # (out in front of his shirt: his chest is open, the ribs round it)
    parts.append(part("Lantern", kit.sphere(10, 8, 0.13), light, M(lantern_at), smooth=True, group="Torso"))
    for k in range(6):
        a = math.tau * k / 6
        parts.append(part("CageBar", kit.cylinder(4, 0.01, 0.32), cage, M((lantern_at[0] + math.cos(a) * 0.17, lantern_at[1] + math.sin(a) * 0.17, lantern_at[2])), group="Torso"))
    for z in (-0.16, 0.16):
        parts.append(part("CageRing", kit.cylinder(10, 0.18, 0.02), cage, M((lantern_at[0], lantern_at[1], lantern_at[2] + z)), group="Torso"))
    for k, z in enumerate((2.36, 2.5, 2.64)):
        for s in (-1, 1):
            arc = [(s * 0.06, -1.3, z), (s * 0.22, -1.24, z + 0.01), (s * 0.31, -1.06, z - 0.02), (s * 0.29, -0.86, z - 0.04)]
            parts.append(part("Rib", kit.tube(arc, [0.02, 0.022, 0.02, 0.015], seg=4), rib, group="Torso"))
    # The arms: shirtsleeves rolled to the elbow (Arm); from there the forelegs, long and jointed, each ending in shears
    # (Fore: its fixed blade; Snip: the blade that opens). The cuff hides the elbow's joint.
    for s, n in ((-1, "L"), (1, "R")):
        arm_b, fore_b, snip_b = "Arm" + n, "Fore" + n, "Snip" + n
        sh, elbow, joint, wrist = (mirror(p, s) for p in (SHOULDER, ELBOW, JOINT, WRIST))
        parts.append(part("Sleeve", kit.tube([sh, ((sh[0] + elbow[0]) / 2, (sh[1] + elbow[1]) / 2, (sh[2] + elbow[2]) / 2), elbow], [0.1, 0.095, 0.085], seg=7),
                          shirt, smooth=True, group=arm_b))
        parts.append(part("Cuff", kit.sphere(8, 6, 0.1), shirt, M(elbow, scale=(1, 1, 0.8)), group=arm_b))
        parts.append(part("Foreleg", kit.tube([elbow, joint], [0.075, 0.06], seg=6), chitin, group=fore_b))
        parts.append(part("Knuckle", kit.sphere(6, 5, 0.07), chitin, M(joint), group=fore_b))
        parts.append(part("Foreleg", kit.tube([joint, wrist], [0.058, 0.045], seg=6), chitin, group=fore_b))
        for k in range(3):
            t = 0.25 + k * 0.25
            p = tuple(joint[j] + (wrist[j] - joint[j]) * t for j in range(3))
            parts.append(part("Spur", kit.cone(3, 0.02, 0.0, 0.12), chitin, kit.along(p, (p[0] + s * 0.04, p[1] + 0.07, p[2] + 0.06)), group=fore_b))
        # The shears: a pivot at the wrist, a fixed blade, and one that opens.
        tip = (wrist[0], wrist[1] - 0.32, 0.12)
        parts.append(part("Pivot", kit.cylinder(8, 0.05, 0.05), shear, M(wrist, rot=(0, 90, 0)), group=fore_b))
        for bone, off in ((fore_b, -0.02), (snip_b, 0.02)):
            a = (wrist[0] + s * off, wrist[1], wrist[2])
            b = (tip[0] + s * off, tip[1], tip[2])
            parts.append(part("Blade", kit.tube([a, ((a[0] + b[0]) / 2, (a[1] + b[1]) / 2 - 0.03, (a[2] + b[2]) / 2), b], [0.04, 0.032, 0.006], seg=3),
                              blades[n], group=bone))
    # Four thin moth legs beneath (tucked in flight), each in two (thigh, shin) with a knee between.
    for leg, (s, y) in LEGS.items():
        hip, knee, foot = leg_points(s, y)
        parts.append(part("Leg", kit.tube([hip, knee], [0.045, 0.035], seg=5), chitin, group="Leg" + leg))
        parts.append(part("Knee", kit.sphere(6, 4, 0.038), chitin, M(knee), group="Leg" + leg))
        parts.append(part("Leg", kit.tube([knee, foot], [0.035, 0.015], seg=5), chitin, group="Shin" + leg))
    # The wings: dead leaves, veined, blotched, ragged; a dim edge (barely there in the dark). Each bends smoothly across
    # its middle (its inner bone to its outer: the leaf cut across there, the veins given more points).
    fore = [(0.35, -0.38), (1.2, -0.78), (2.2, -0.98), (2.95, -0.72), (3.05, -0.3), (2.6, 0.12), (1.6, 0.42), (0.7, 0.38), (0.32, 0.08)]
    hind = [(0.3, 0.2), (1.1, 0.34), (2.0, 0.76), (2.2, 1.32), (1.6, 1.76), (0.8, 1.62), (0.3, 0.92)]
    for s, n in ((-1, "L"), (1, "R")):
        for outline, material, inner, outer, z0, rib_to, n_veins, (a0, a1) in (
                (fore, leaf, "Wing" + n, "WingTip" + n, WING_Z - 0.015, (2.92, -0.55), 6, (1.1, 2.1)),
                (hind, leaf_hind, "Hind" + n, "HindTip" + n, WING_Z - 0.115, (1.85, 1.35), 4, (0.75, 1.45))):
            bend = lambda co, a0=a0, a1=a1: (abs(co.x) - a0) / (a1 - a0)
            cuts = [s * (0.55 + 0.2 * k) for k in range(13)]
            group = []
            edge_pts = ragged(outline, rng)
            pts = [(s * x, y) for x, y in edge_pts]
            if s < 0:
                pts = list(reversed(pts))
            group.append(part("Wing", sliced(kit.prism(pts, 0.03), cuts), material, M((0, 0, z0)), group=inner))
            loop = [(s * x, y, z0 + 0.034) for x, y in edge_pts]
            group.append(part("WingEdge", kit.tube(loop + [loop[0]], [0.018] * (len(loop) + 1), seg=3), edge, group=inner))
            top = z0 + 0.03
            root = (s * (outline[0][0] + 0.05), (outline[0][1] + outline[-1][1]) / 2, top + 0.012)
            tip = (s * rib_to[0], rib_to[1], top + 0.012)
            group.append(part("Midrib", kit.tube(*along_line([root, tip], [0.03, 0.01], 0.15), seg=4), vein, group=inner))
            for k in range(1, n_veins + 1):
                t = k / (n_veins + 1)
                p = tuple(root[j] + (tip[j] - root[j]) * t for j in range(3))
                for side in (-1, 1):
                    end = (p[0] + s * 0.32 * (1 - t * 0.4), p[1] + side * 0.42 * (1 - t * 0.5) + 0.18, top + 0.01)
                    group.append(part("Vein", kit.tube(*along_line([p, end], [0.016, 0.004], 0.12), seg=3), vein, group=inner))
            for k in range(5):
                cx = s * rng.uniform(0.9, (rib_to[0] - 0.3))
                cy = rng.uniform(min(y for _, y in outline) + 0.25, max(y for _, y in outline) - 0.25)
                r = rng.uniform(0.12, 0.26)
                group.append(part("Blotch", kit.cylinder(9, r, 0.004), blotch if k % 3 else rot,
                                  M((cx, cy, top + 0.004 + 0.002 * (k + 1)), scale=(1, rng.uniform(0.6, 1.0), 1)), jitter=0.01, group=inner))
            parts += [blend(ob, inner, outer, bend) for ob in group]
    mesh = kit.join(parts, "FatherMoth")
    armature = kit.rig("FatherMothRig", BONES)
    kit.skin(mesh, armature)
    kit.animate(armature, ACTIONS)
    for name, worst in REPORT.items():
        if worst > 0.03:
            print(f"[father_moth] {name}: a planted foot or shear-point misses its mark by up to {worst * 100:.0f} cm")
    return [armature, mesh]
