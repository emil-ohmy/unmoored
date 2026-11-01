"""The Ferryman's Apprentice: small, cloaked, bone-white mask, oversized oar (blade up). Rigged and animated.

Clip timings match the Middle combat tuning (the controller rescales them if tuning changes):
light 0.13 s windup / 0.10 s active / 0.28 s recovery; heavy 0.5 / 0.12 / 0.5; dash 0.38 s; heal 0.7 s.
RecoilHeavy / RecoilLight: bouncing off a Laden's heap (0.35 s). HookAim (loop), HookThrow (0.15 s), HookPull, Pickup
(0.6 s), RaiseItem (2.4 s: a key item held up). VaultPlant (0.12 s), Vault (0.55 s), VaultLand (0.2 s): the Vault.

How it's animated (the Laden's way): each bone keyed on its own timing (kit.bake: tracks), so nothing moves all at
once: the body leads (Pivot, the whole-body lean; Body, the twist and bend), the head follows a few frames late. The
oar is keyed where it is (its direction and where its grip is, in the clip's frame: not through the body that carries
it), and every frame the arms are bent onto it (kit.reach) so the hands keep their grip, both hands where both hold it.
A free hand is keyed where it is (in the body's frame). The loose things aren't keyed at all: the skirt of the cloak,
the tip of the hood, the bow lantern on its cord and the Hook slung at the back swing after the body on springs
(simulated every frame, with the game's own movement of the Apprentice felt as wind and as a shove: a dash streams
them back, a landing flings them down), and the skirt is kept off the floor when the Apprentice crouches (it bunches
up, it doesn't sink). The feet are keyed where they stand, so a crouch doesn't push them through the floor.

Timing that the game depends on (the frame where things happen, as the clip is played):
  Light1/Light2 (0.51 s): the oar straight ahead at f5.7 (the hit is checked there); the swoosh f3.9-f8.7.
  Heavy (1.12 s): the windup ends at f16.2 (the oar starts down), the blade meets its target at f19.3.
  Stroke (0.62 s): the wave leaves the blade at f5.7, the oar straight ahead, low.
  Toss (0.4 s): the pot let go at f5. Pickup (0.6 s): taken at the bottom, f7.5. RaiseItem/RaiseOar (2.4 s): the flash
  at the top, f17. Death (2.2 s): kneeling by f25 (the lantern gutters out). Vault (0.55 s): its top at f6.6.
"""
import math

import bmesh
from mathutils import Euler, Matrix, Vector

import oars

M, part = kit.M, kit.part  # `kit` is injected by build_model.py

# Oar placement at rest: foot of the oar near the ground, leaning out to the right and slightly forward.
OAR = M((0.33, -0.08, 0.02), rot=(6, 8, 0))
OAR_ROT = OAR.to_3x3()
HAND = OAR @ Vector((0, 0, 0.6))  # the Oar bone's head (its grip, where the swing's trail is measured from)

# The arms (the cloak's sleeves): upper and fore, 0.2 m each, hanging at rest. The hand is the fore's tail.
SHOULDER_L, ELBOW_L, WRIST_L = (-0.15, -0.03, 0.6), (-0.21, -0.04, 0.41), (-0.22, -0.13, 0.23)
SHOULDER_R, ELBOW_R, WRIST_R = (0.15, -0.03, 0.6), (0.21, -0.04, 0.41), (0.22, -0.13, 0.23)
HOOK_HEAD = Vector((0.22, 0.16, 0.5))

BONES = {
    "Root": ((0, 0, 0), (0, 0, 0.2), None),
    "Pivot": ((0, 0, 0.45), (0, 0, 0.55), "Root"),  # centre of mass: whole-body leans (dash, death) pivot here
    "Body": ((0, 0, 0.1), (0, 0, 0.62), "Pivot"),
    "Head": ((0, 0, 0.62), (0, 0, 1.05), "Body"),
    "HoodTip": ((0, 0.02, 0.94), (0, 0.28, 1.13), "Head"),  # the hood's point: it flops after the head
    "Hem": ((0, 0, 0.42), (0, 0, 0.0), "Body"),  # the cloak's skirt (skinned smoothly into the body): it swings
    "ArmL": (SHOULDER_L, ELBOW_L, "Body"),
    "ForeL": (ELBOW_L, WRIST_L, "ArmL"),
    "ArmR": (SHOULDER_R, ELBOW_R, "Body"),
    "ForeR": (ELBOW_R, WRIST_R, "ArmR"),
    "Oar": (tuple(HAND), tuple(OAR @ Vector((0, 0, 0.9))), "Root"),  # keyed where it is; pivots at the grip
    "Hook": (tuple(HOOK_HEAD), (0.22, 0.16, 0.7), "Body"),  # the Hook, hung at the back of the hip (hidden until found)
    # The bow lantern at the left hip: its own bone, so the game can hide it while it hangs on the boat's bow.
    "Lantern": ((-0.36, -0.06, 0.52), (-0.36, -0.06, 0.62), "Body"),
    "FootL": ((-0.11, -0.1, 0.03), (-0.11, -0.26, 0.03), "Root"),
    "FootR": ((0.11, -0.1, 0.03), (0.11, -0.26, 0.03), "Root"),
}
BODY_BONES = ("Root", "Pivot", "Body", "Head", "Hem", "HoodTip", "Lantern", "Hook", "FootL", "FootR")


# ---------------------------------------------------------------- the oar and the hands

def turn(pitch, yaw, roll=0.0):
    """The oar's turn: upright (blade up, its face to the front), turned `roll` about its shaft, tipped `pitch` (+:
    forward, 90 level, more: down), then swung `yaw` (+: to the Apprentice's right)."""
    return (Matrix.Rotation(math.radians(yaw), 3, "Z") @ Matrix.Rotation(math.radians(pitch), 3, "X")
            @ Matrix.Rotation(math.radians(roll), 3, "Z"))


def oar(pitch, yaw, roll, at, s=0.6):
    """The oar's key: turned (pitch, yaw, roll), the point `s` metres up its shaft (from the butt) at `at` (the clip's
    frame). Every key holds the grip's place (s=0.6, the bone's head)."""
    grip = Vector(at) + turn(pitch, yaw, roll) @ Vector((0, 0, 0.6 - s))
    return ((pitch, yaw, roll), tuple(round(c, 4) for c in grip))


def planted(pitch, yaw, roll=-50.0, butt=(0.33, -0.08, 0.02)):
    """The oar stood on its foot."""
    return oar(pitch, yaw, roll, butt, s=0.0)


def hand(s, on, x, y, z):
    """A hand: `on` 1 holding the oar `s` metres up its shaft, 0 free at (x, y, z) (in the body's frame, as at rest),
    between: on its way."""
    return ((s, on, 0), (x, y, z))


def on_hook(w, x, y, z):
    """A hand on the Hook (w 1) at (x, y, z) on it (as it hangs at rest), instead of where its free key says."""
    return ((w, 0, 0), (x, y, z))


STAND = planted(10, 50)
FREE_L = (-0.21, -0.1, 0.31)      # the free hand, hanging by the lantern
FREE_R = (0.21, -0.1, 0.31)
POLE_L, POLE_R = (-1, 0.5, -0.9), (1, 0.5, -0.9)  # elbows: out, back and down

REST = {
    "Root": ((0, 0, 0), (0, 0, 0)), "Pivot": (0, 0, 0), "Body": (2, 0, 0), "Head": (-2, 0, 0),
    "OAR": STAND, "HandR": hand(0.6, 1, *FREE_R), "HandL": hand(0.4, 0, *FREE_L),
}


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


def pose_of(base, **changes):
    p = dict(base)
    p.update(changes)
    return p


# ---------------------------------------------------------------- the loose things (springs)

# Each swings about `pivot` (a rest point on its parent), a mass at `mass` (a rest point on itself): pulled back to
# where its parent carries it (k), towards the floor (grav), slowed (damp), blown back by the Apprentice's movement in
# the game (drag) and thrown by its starts and stops (inertia).
SPRINGS = {
    "Hem": dict(pivot=(0, 0, 0.42), mass=(0, 0, 0.0), k=170, damp=7, grav=1.0, drag=1.0, inertia=0.3, most=24),
    "HoodTip": dict(pivot=(0, 0.02, 0.94), mass=(0, 0.28, 1.13), k=210, damp=7, grav=0.5, drag=1.4, inertia=0.6, most=35),
    "Lantern": dict(pivot=(-0.36, -0.06, 0.52), mass=(-0.36, -0.06, 0.38), k=0, damp=2.4, grav=1.0, drag=0.5, inertia=1.0, most=60),
    "Hook": dict(pivot=(0.22, 0.16, 0.95), mass=(0.22, 0.16, 0.5), k=260, damp=8, grav=0.4, drag=0.5, inertia=0.6, most=25),
}
GRAVITY = 9.8
FLOOR = -0.02
# The skirt's rim (rest points round its foot): kept above the floor.
RIM = [Vector((0.41 * math.cos(a), 0.41 * math.sin(a), 0.0)) for a in (2 * math.pi * k / 16 for k in range(16))]


def local_matrix(name, value):
    head = Vector(BONES[name][0])
    rot, offset = value if isinstance(value[0], (tuple, list)) else (value, (0, 0, 0))
    r = Euler([math.radians(a) for a in rot]).to_matrix().to_4x4()
    return Matrix.Translation(head + Vector(offset)) @ r @ Matrix.Translation(-head)


def as_value(name, local):
    """A local matrix (rotation about anything) as animate()'s (rotation about the bone's head, offset)."""
    head = Vector(BONES[name][0])
    rot = local.to_3x3().normalized().to_euler("XYZ")
    offset = local @ head - head
    return (tuple(math.degrees(a) for a in rot), tuple(offset))


def parent_matrix(name, mats):
    parent = BONES[name][2]
    return mats[parent] if parent else Matrix.Identity(4)


def springs(frames, periodic, motion, skip, hang):
    """Swing the loose things after the body, frame by frame. The skirt's rest hangs `hang(frame)` of the way from
    where the body carries it towards straight down (cloth hangs: a lean bends it at the waist)."""
    n = len(frames)
    dt = 1.0 / 30
    sub = 6
    vel = [Vector(motion(f)) for f, _ in frames]
    for name, c in SPRINGS.items():
        if name in skip:
            continue
        pivots, targets, carried_at = [], [], []
        for (f, pose) in frames:
            mats = kit.pose_matrices(BONES, pose)
            carried = parent_matrix(name, mats) @ local_matrix(name, pose.get(name, (0, 0, 0)))
            pv, tg = carried @ Vector(c["pivot"]), carried @ Vector(c["mass"])
            carried_at.append(tg)
            if name == "Hem":
                down = Vector((0, 0, -(tg - pv).length))
                tg = pv + (tg - pv).lerp(down, hang(f)).normalized() * (tg - pv).length
            pivots.append(pv)
            targets.append(tg)
        length = (Vector(c["mass"]) - Vector(c["pivot"])).length
        x, v = targets[0].copy(), Vector((0, 0, 0))

        def step(i0, i1, t0, t1):
            nonlocal x, v
            for s in range(sub):
                u = (s + 0.5) / sub
                p = pivots[i0].lerp(pivots[i1], u)
                tgt = targets[i0].lerp(targets[i1], u)
                wind = vel[i0].lerp(vel[i1], u)
                accel = (vel[i1] - vel[i0]) / dt if i1 != i0 else Vector((0, 0, 0))
                a = c["k"] * (tgt - x) + Vector((0, 0, -GRAVITY * c["grav"])) - c["damp"] * v - c["drag"] * (v + wind) - c["inertia"] * accel
                v = v + a * (dt / sub)
                x = x + v * (dt / sub)
                d = x - p
                d = d.normalized() if d.length > 1e-6 else Vector((0, 0, -1))
                # (no further from its rest than `most` degrees)
                rest = (tgt - p).normalized()
                if rest.angle(d, 0.0) > math.radians(c["most"]):
                    axis = rest.cross(d)
                    if axis.length > 1e-6:
                        d = Matrix.Rotation(math.radians(c["most"]), 3, axis.normalized()) @ rest
                x = p + d * length
                v = v - d * v.dot(d)

        out = [None] * n
        if periodic:
            for _ in range(5):
                for i in range(n - 1):
                    step(i, i + 1, 0, 0)
            for i in range(n - 1):
                out[i] = x.copy()
                step(i, i + 1, 0, 0)
            out[n - 1] = out[0]
        else:
            for _ in range(40):  # (settled into where frame 1 holds it, moving as it is then)
                step(0, 0, 0, 0)
            for i in range(n):
                out[i] = x.copy()
                if i + 1 < n:
                    step(i, i + 1, 0, 0)
        for (f, pose), at, p, tgt in zip(frames, out, pivots, carried_at):
            mats = kit.pose_matrices(BONES, pose)
            parent = parent_matrix(name, mats)
            swing = (tgt - p).rotation_difference(at - p).to_matrix().to_4x4()
            world = Matrix.Translation(p) @ swing @ Matrix.Translation(-p)
            carried = parent @ local_matrix(name, pose.get(name, (0, 0, 0)))
            pose[name] = as_value(name, parent.inverted() @ world @ carried)


def hem_off_floor(pose, floor):
    """The skirt lifted so its rim stays on the floor (it bunches up)."""
    mats = kit.pose_matrices(BONES, pose)
    low = min((mats["Hem"] @ p).z for p in RIM)
    if low >= floor:
        return
    rot, offset = pose["Hem"]
    lift = parent_matrix("Hem", mats).to_3x3().inverted() @ Vector((0, 0, floor - low))
    pose["Hem"] = (rot, tuple(Vector(offset) + lift))


# ---------------------------------------------------------------- a clip

REACH = {}  # (the build's report: how far each clip's hands come off what they hold, at worst)


def clip(last, tracks, periodic=False, free=None, motion=None, floor=FLOOR, hook_hand=None, custom=None, poles=None, hang=0.85):
    """A clip of frames 1..`last` (30 a second) from per-track keys, baked to every frame. Tracks: the bones (Root,
    Pivot, Body, Head, the feet, and the loose ones to add to their swing), "OAR" (oar()), "HandL"/"HandR" (hand()),
    "HookL"/"HookR" (on_hook()). Unkeyed tracks hold REST.
    free: {"L"/"R": bone, or None for the clip's frame} the frame a free hand's point is in (default the body's).
    motion: [(frame, (x, y, z))] how fast the game moves the Apprentice meanwhile (m/s, its own axes: -y ahead).
    floor: the skirt kept above it (None: sat in the boat). hang: how much the skirt hangs straight (or hang(frame)). hook_hand: "L"/"R", the Hook (out on its tether) in that hand.
    custom(frame, pose): last changes to a frame's tracks before it's solved."""
    tracks = {track: sorted(dict(ks).items()) for track, ks in tracks.items()}  # (a later key on the same frame wins)
    for track, value in REST.items():
        tracks.setdefault(track, [(1, value)])
    free = free or {}
    poles = poles or {}
    baked = kit.bake(tracks, last - 1, periodic)
    frames, worst, prev = [], (0.0, "", 0), {}
    for f, p in baked:
        if custom:
            custom(f, p)
        frames.append((f, solve(p, free, hook_hand, poles, prev)))
        d, side = prev.pop("_off")
        if d > worst[0]:
            worst = (d, side, f)
    if motion:
        mk = sorted(motion)

        def moving(f):
            if f <= mk[0][0]:
                return mk[0][1]
            for (f0, v0), (f1, v1) in zip(mk, mk[1:]):
                if f <= f1:
                    u = (f - f0) / (f1 - f0)
                    return tuple(a + (b - a) * u for a, b in zip(v0, v1))
            return mk[-1][1]
    else:
        def moving(f):
            return (0, 0, 0)
    springs(frames, periodic, moving, skip=[t for t in ("Hook",) if t in tracks or hook_hand],
            hang=hang if callable(hang) else (lambda f: hang))
    for f, pose in frames:
        if floor is not None:
            hem_off_floor(pose, floor)
    # (every rotation kept continuous from frame to frame: no flips between equal turns)
    eulers = {}
    for f, pose in frames:
        for bone, value in list(pose.items()):
            rot, offset = value if isinstance(value[0], (tuple, list)) else (value, (0, 0, 0))
            e = Euler([math.radians(a) for a in rot]).to_matrix().to_euler("XYZ", eulers[bone]) if bone in eulers \
                else Euler([math.radians(a) for a in rot])
            eulers[bone] = e
            pose[bone] = (tuple(math.degrees(a) for a in e), tuple(offset))
    REACH[len(REACH)] = worst
    return ("LINEAR", frames)


def solve(p, free, hook_hand, poles, prev):
    """One frame's tracks to bone poses: the feet and the oar placed where they're keyed, the arms bent onto it."""
    out = {b: p[b] for b in BODY_BONES if b in p}
    root = Vector(p["Root"][1])
    for foot in ("FootL", "FootR"):
        rot, offset = out.get(foot, ((0, 0, 0), (0, 0, 0)))
        out[foot] = (rot, tuple(Vector(offset) - root))  # (keyed where they stand, not carried by the Root)
    (pitch, yaw, roll), grip = p["OAR"]
    rk = turn(pitch, yaw, roll) @ OAR_ROT.inverted()
    out["Oar"] = (tuple(math.degrees(a) for a in rk.to_euler("XYZ")), tuple(Vector(grip) - HAND - root))
    off = (0.0, "")
    for side, arm, fore, wrist, pole in (("L", "ArmL", "ForeL", WRIST_L, POLE_L), ("R", "ArmR", "ForeR", WRIST_R, POLE_R)):
        mats = kit.pose_matrices(BONES, out)
        (s, on, _), point = p["Hand" + side]
        on = max(0.0, min(1.0, on))
        frame = free.get(side, "Body")
        target = (mats[frame] @ Vector(point)) if frame else Vector(point)
        if "Hook" + side in p:
            (w, _, _), at = p["Hook" + side]
            target = target.lerp(mats["Hook"] @ Vector(at), max(0.0, min(1.0, w)))
        grip = mats["Oar"] @ (OAR @ Vector((0, 0, s)))
        target = target.lerp(grip, on)
        bend = mats["Body"].to_3x3() @ Vector(p["Pole" + side][0] if "Pole" + side in p else poles.get(side, pole))
        out = kit.reach(BONES, out, arm, fore, lambda m, t=target: t, bend)
        tip = kit.pose_matrices(BONES, out)[fore] @ Vector(wrist)
        held_on = max(on, p["Hook" + side][0][0] if "Hook" + side in p else 0.0)
        if held_on > 0.5 and (tip - target).length > off[0]:
            off = ((tip - target).length, side)
    if hook_hand:
        mats = kit.pose_matrices(BONES, out)
        tip = mats["Fore" + hook_hand] @ Vector(WRIST_L if hook_hand == "L" else WRIST_R)
        rot = out.get("Hook", ((0, 0, 0), (0, 0, 0)))
        rot = rot[0] if isinstance(rot[0], (tuple, list)) else rot
        out["Hook"] = (tuple(rot), tuple(mats["Body"].inverted() @ tip - HOOK_HEAD))  # (the tether's end, in the hand)
    prev["_off"] = off
    return out


# ---------------------------------------------------------------- poses

def swing(phi, pitch=86.0, lift=0.0, reach=0.0, roll=0.0):
    """The two-handed swing at `phi` (degrees: 0 the oar straight ahead, + to the right): the hands (right above
    left, the oar's lower half) ride a small arc in front of the body, a little behind the oar's turn, and the oar
    points out along `phi`."""
    a = math.radians(max(-46.0, min(46.0, 0.6 * phi)))
    r = 0.3 + reach
    return oar(pitch, phi, roll, (r * math.sin(a), -r * math.cos(a), 0.5 + lift), s=0.45)


SWING_R, SWING_L = 0.58, 0.32  # where the hands hold the oar for a swing (metres up the shaft)


def two(on=1):
    return {"HandR": hand(SWING_R, on, *FREE_R), "HandL": hand(SWING_L, on, *FREE_L)}


# The light swings (two-handed, swung from the body: the torso turns into it, the head counter-turns so the mask
# keeps facing the target). Light1 a forehand from the right across to the left, wrapping round over the left
# shoulder; Light2 the backhand from there back across to the right.
LIGHT_HIT = swing(0, 87, 0.02, 0.06)            # the contact pose: the oar straight ahead (f5.7 in both)
L1_WRAP = swing(-125, 62, 0.14, -0.08)          # Light1's follow-through, wrapped round over the left shoulder
L2_WRAP = swing(128, 64, 0.1, -0.08)            # Light2's, over the right
# The heavy: a two-handed overhead chop, hands close at the oar's foot.
HEAVY_R, HEAVY_L = 0.5, 0.22
HEAVY_RAISED = oar(-62, 30, 0, (0.16, 0.04, 0.78), s=0.36)
HEAVY_HIT = oar(70, -12, 0, (-0.03, -0.33, 0.55), s=0.36)
HEAVY_HIT_BODY = {"Pivot": (3, 0, 0), "Body": (15, 0, -12), "Head": (-12, 0, 8), "Root": ((0, 0, 0), (0, -0.06, -0.06))}
# The Stroke: drawn back low round the oar's side, crouched, the blade turned down to water that isn't there.
STROKE_R, STROKE_L = 0.62, 0.3
STROKE_DRAWN = oar(96, 130, 90, (0.26, 0.14, 0.38), s=0.46)
STROKE_STRAIN = oar(98, 135, 90, (0.27, 0.16, 0.37), s=0.46)
STROKE_BODY = {"Pivot": (5, 0, 0), "Body": (13, 0, 44), "Head": (-6, 0, -36), "Root": ((0, 0, 0), (0, 0.02, -0.1))}
# The block: the oar held across the front in both hands, the blade's face to the foe, braced.
BLOCK_OAR = oar(88, -90, 90, (0.0, -0.3, 0.56), s=0.62)
BLOCK_R, BLOCK_L = 0.42, 0.84
BLOCK = {"Root": ((0, 0, 0), (0, 0.02, -0.045)), "Pivot": (-3, 0, 0), "Body": (-3, 0, 0), "Head": (8, 0, 0),
         "OAR": BLOCK_OAR, "HandR": hand(BLOCK_R, 1, *FREE_R), "HandL": hand(BLOCK_L, 1, *FREE_L),
         "FootL": ((0, 0, 0), (-0.02, -0.06, 0)), "FootR": ((0, 0, 0), (0.02, 0.05, 0))}
# Running, the oar carried in the right hand, trailing (tipped back: from the game's camera a forward-tipped oar
# foreshortens and reads as "at the feet").
RUN_OAR = oar(-30, 24, 0, (0.33, 0.04, 0.47), s=0.45)
RUN = {"Root": ((0, 0, 0), (0, 0, 0.01)), "Pivot": (2, 0, 0), "Body": (12, 0, -1), "Head": (-12, 0, 2),
       "OAR": RUN_OAR, "HandR": hand(0.45, 1, *FREE_R), "HandL": hand(0.4, 0, -0.2, -0.05, 0.34)}

# Sat in the boat (facing the bow), the hem below the seat in the hull.
SIT = (0, 0, -0.16)
SAT_FEET = {"FootL": ((-20, 0, 0), (0, -0.12, 0.12)), "FootR": ((-20, 0, 0), (0, -0.12, 0.12))}
ROW_REST = oar(90, 90, 90, (0.05, -0.12, 0.22), s=0.6)  # laid across the boat, blade out to the right, hands in the lap
ROW_IDLE = dict(SAT_FEET, Root=((0, 0, 0), SIT), Pivot=(6, 0, 0), Body=(4, 0, 0), Head=(2, 0, 0), OAR=ROW_REST,
                HandR=hand(0.66, 1, *FREE_R), HandL=hand(0.4, 1, *FREE_L))
ROW_DRAW = oar(12, 90, 0, (0.2, -0.06, 0.7), s=0.32)  # the oar up high beside the head in both hands, blade high
ROW_STRAIN = oar(16, 92, 0, (0.21, -0.05, 0.72), s=0.32)
ROW_DRAW_POSE = dict(SAT_FEET, Root=((0, 0, 0), SIT), Pivot=(-4, 0, 0), Body=(-8, -3, 0), Head=(-20, 0, 10),
                     OAR=ROW_DRAW, HandR=hand(0.44, 1, *FREE_R), HandL=hand(0.2, 1, *FREE_L))

# The Vault: the oar's foot planted far ahead, the hands high on it, crouched.
VAULT_BUTT = (0.16, -1.1, 0.0)
VAULT_HANDS = 1.02  # (how far up the shaft the hands' middle is, planted)
VAULT_PLANT = {"Root": ((0, 0, 0), (0, 0.02, -0.11)), "Pivot": (6, 0, 0), "Body": (14, 0, 22), "Head": (-14, 0, 4),
               "HandR": hand(VAULT_HANDS - 0.12, 1, *FREE_R), "HandL": hand(VAULT_HANDS + 0.12, 1, *FREE_L),
               "FootL": ((0, 0, 0), (-0.01, -0.08, 0)), "FootR": ((20, 0, 0), (0.02, 0.1, 0.02))}


def vault_oar(hands_at):
    """The oar planted at VAULT_BUTT and leaning back to `hands_at` (where the hands' middle is)."""
    d = Vector(hands_at) - Vector(VAULT_BUTT)
    pitch = math.degrees(math.acos(max(-1, min(1, d.normalized().z))))
    yaw = math.degrees(math.atan2(d.x, -d.y))
    return oar(pitch, yaw, 0, VAULT_BUTT, s=0.0)


VAULT_PLANT["OAR"] = vault_oar((0.2, -0.3, 0.48))


# ---------------------------------------------------------------- the clips

def make_actions():
    A = {}

    # Idle (1.6 s loop): one breath (the body rises and straightens, the head after it, and sinks with a little nod),
    # a shift of weight onto the oar and back, a glance to its left. Leaning on the oar, stood on its foot at the
    # right; the free hand hangs by the lantern.
    A["Idle"] = clip(48, {
        "Root": [(1, ((0, 0, 0), (0, 0, 0))), (9, ((0, 0, 0), (0.008, 0, 0.004))), (18, ((0, 0, 0), (0.004, 0, 0.009))),
                 (34, ((0, 0, 0), (-0.005, 0, -0.006)))],
        "Pivot": [(1, (0, 0, 0)), (10, (0, 1.8, 0)), (34, (0, -1.0, 0))],
        "Body": [(1, (2, 0, 0)), (18, (-0.5, 0, 2)), (37, (4, 0, -1))],
        "Head": [(1, (-2, 0, 0)), (12, (-3, 0, -13)), (21, (-4, -2, -11)), (29, (0, 0, 3)), (41, (2, 0, 1))],
        "OAR": [(1, planted(10, 50)), (12, planted(12.5, 56)), (36, planted(8, 45))],
        "HandL": [(1, hand(0.4, 0, -0.21, -0.1, 0.31)), (20, hand(0.4, 0, -0.205, -0.085, 0.325)), (38, hand(0.4, 0, -0.215, -0.11, 0.305))],
    }, periodic=True)

    # Run (16 frames, two strides; frame 1 its passing pose, the left foot under it, so it joins what's before and
    # after): leaning in, a bound and a drop at each footfall (f5 the right, f13 the left), rolling onto the foot it
    # lands on, the shoulders twisting against the feet; the head steadied (it rides after the body); the oar carried
    # trailing in the right hand, bobbing a beat late; the left arm swinging against the feet.
    def foot(phase):
        ks = [(1, ((0, 0, 0), (0, 0.0, 0.06))), (3, ((-10, 0, 0), (0, -0.08, 0.045))), (5, ((-16, 0, 0), (0, -0.11, 0))),
              (7, ((0, 0, 0), (0, -0.04, 0))), (9, ((0, 0, 0), (0, 0.04, 0))), (11, ((24, 0, 0), (0, 0.1, 0.012))),
              (13, ((30, 0, 0), (0, 0.08, 0.06))), (15, ((8, 0, 0), (0, 0.04, 0.07)))]
        return [(1 + (f - 1 + phase) % 16, v) for f, v in ks]

    A["Run"] = clip(17, {
        "Root": [(1, ((0, 0, 0), (0, 0, 0.012))), (3, ((0, 0, 0), (0, 0, 0.045))), (5, ((0, 0, 0), (0, 0, 0.012))),
                 (6.5, ((0, 0, 0), (0, 0, -0.03))), (9, ((0, 0, 0), (0, 0, 0.012))), (11, ((0, 0, 0), (0, 0, 0.045))),
                 (13, ((0, 0, 0), (0, 0, 0.012))), (14.5, ((0, 0, 0), (0, 0, -0.03)))],
        "Pivot": [(1, (2, 0, 0)), (6.5, (3.5, 3, 0)), (10, (1.5, 0, 0)), (14.5, (3.5, -3, 0))],
        "Body": [(1, (12, 0, -1)), (5, (12.5, 0, -7)), (9, (12, 0, 0)), (13, (12.5, 0, 5))],
        "Head": [(2, (-12, 0, 4)), (8, (-15, -2, 4)), (10, (-12, 0, -2)), (16, (-15, 2, -2))],
        "FootR": foot(0), "FootL": foot(8),
        "OAR": [(2, oar(-28, 24, 0, (0.33, 0.1, 0.5), s=0.45)), (8, oar(-33, 22, 0, (0.33, 0.06, 0.45), s=0.45)),
                (10, oar(-30, 26, 0, (0.33, -0.02, 0.49), s=0.45)), (16, oar(-33, 24, 0, (0.33, 0.0, 0.45), s=0.45))],
        "HandR": [(1, hand(0.45, 1, *FREE_R))],
        "HandL": [(5, hand(0.4, 0, -0.19, -0.24, 0.4)), (13, hand(0.4, 0, -0.2, 0.12, 0.33))],
    }, periodic=True, motion=[(1, (0, -5, 0))], poles={"R": (0.4, 1, 0.1)})

    # Dash (0.38 s; then held 0.12 s): no wind-up (it has to answer at once): flung low and forward into it, the oar
    # swept back to trail behind, the free arm thrown back, the head up and ahead; the cloak, the hood and the lantern
    # streamed back after; at the end it comes up into the run.
    A["Dash"] = clip(12, keys(held(REST, 1), {
        "Root": [(3, ((0, 0, 0), (0, 0, -0.07))), (8, ((0, 0, 0), (0, 0, -0.06))), (12, ((0, 0, 0), (0, 0, 0.005)))],
        "Pivot": [(3, (9, 0, 0)), (8, (8, 0, 0)), (11, (3, 0, 0)), (12, (2, 0, 0))],
        "Body": [(2, (16, 0, 6)), (3.5, (24, 0, 5)), (8, (22, 0, 4)), (11, (13, 0, 0)), (12, (12, 0, -1))],
        "Head": [(2, (-8, 0, -4)), (4, (-26, 0, -4)), (9, (-24, 0, 0)), (12, (-12, 0, 2))],
        "OAR": [(3, oar(-82, 14, 0, (0.3, 0.16, 0.42), s=0.45)), (8, oar(-78, 12, 0, (0.3, 0.14, 0.4), s=0.45)), (12, RUN_OAR)],
        "HandR": [(1, hand(0.6, 1, *FREE_R)), (3, hand(0.45, 1, *FREE_R))],
        "HandL": [(3, hand(0.4, 0, -0.22, 0.2, 0.42)), (8, hand(0.4, 0, -0.22, 0.18, 0.4)), (12, hand(0.4, 0, -0.2, -0.05, 0.34))],
        "FootL": [(2, ((20, 0, 0), (0, 0.12, 0.03))), (4, ((30, 0, 0), (0, 0.24, 0.08))), (9, ((20, 0, 0), (0, 0.2, 0.06))), (12, ((0, 0, 0), (0, 0, 0.06)))],
        "FootR": [(3, ((10, 0, 0), (0, 0.1, 0.0))), (8, ((10, 0, 0), (0, 0.06, 0.02))), (12, ((-10, 0, 0), (0, -0.08, 0.04)))],
    }), motion=[(1, (0, 0, 0)), (2.5, (0, -7.4, 0)), (10, (0, -7.4, 0)), (12, (0, -2, 0))], hang=0.3)

    # Light1 (0.51 s): the forehand. A snap round to the right (the body first), the left hand onto the oar, cocked over
    # the right shoulder; the body unwinds and the oar whips round after it, level, straight ahead at f5.7 (the hit),
    # on across to the left and wrapping round over the left shoulder (an overshoot; the head nods with the blow), then
    # back onto its foot at the right (by way of the front, tipped out ahead), the left hand letting go.
    light1_arc = [(3.2, swing(118, 58, 0.16, -0.06)), (4.2, swing(100, 74, 0.08, -0.02)), (5.0, swing(48, 84, 0.03, 0.04)),
                  (5.7, LIGHT_HIT), (6.5, swing(-58, 87, 0.02, 0.04)), (7.3, swing(-100, 80, 0.06, -0.02)),
                  (8.4, swing(-130, 60, 0.15, -0.09)), (10, L1_WRAP)]
    A["Light1"] = clip(15, keys(held(REST, 1), {
        "OAR": light1_arc + [(12.6, oar(56, -24, 0, (-0.04, -0.28, 0.46), s=0.45)), (15, STAND)],
        "HandR": [(2, hand(0.6, 1, *FREE_R)), (3.2, hand(SWING_R, 1, *FREE_R)), (12, hand(SWING_R, 1, *FREE_R)), (15, hand(0.6, 1, *FREE_R))],
        "HandL": [(1.5, hand(SWING_L, 0, -0.1, -0.2, 0.42)), (3.2, hand(SWING_L, 1, -0.1, -0.2, 0.42)), (11, hand(SWING_L, 1, -0.2, -0.1, 0.4)),
                  (13.5, hand(SWING_L, 0, -0.2, -0.1, 0.33)), (15, REST["HandL"])],
        "Body": [(2.6, (-2, 0, 42)), (4.0, (2, 0, 26)), (5.0, (5, 0, -4)), (6.2, (6, 0, -40)), (7.6, (5, 0, -52)), (10, (4, 0, -46)), (15, REST["Body"])],
        "Pivot": [(3, (-3, 0, 0)), (6, (8, -2, 0)), (10, (5, 0, 0)), (15, (0, 0, 0))],
        "Head": [(3.4, (-4, 0, -30)), (5.2, (-6, 0, -8)), (6.6, (4, 0, 22)), (8.6, (0, 0, 30)), (11, (-2, 0, 24)), (15, REST["Head"])],
        "Root": [(3, ((0, 0, 0), (0, 0.02, -0.02))), (6, ((0, 0, 0), (0, -0.07, -0.05))), (10, ((0, 0, 0), (0, -0.05, -0.03))), (15, REST["Root"])],
        "FootL": [(3.5, ((0, 0, 0), (0, -0.02, 0.04))), (5.5, ((0, 0, 0), (-0.02, -0.12, 0))), (12, ((0, 0, 0), (-0.02, -0.12, 0))), (14.5, ((0, 0, 0), (0, 0, 0)))],
        "FootR": [(5, ((0, 0, 0), (0, 0, 0))), (7, ((10, 0, 0), (0.02, 0.03, 0.01))), (13, ((0, 0, 0), (0, 0, 0)))],
    }), motion=[(4.6, (0, 0, 0)), (5.2, (0, -5, 0)), (7.0, (0, -5, 0)), (7.6, (0, 0, 0))])

    # Light2 (0.51 s): the backhand, from where Light1 left it (wrapped over the left shoulder): cocked a little
    # further, then whipped back across, the body unwinding first, straight ahead at f5.7, on round to the right and
    # wrapping over the right shoulder; then down onto its foot (the right side: home), the left hand letting go.
    L2_START = swing(-130, 60, 0.15, -0.09)
    light2_arc = [(1, L2_START), (3.2, swing(-136, 58, 0.17, -0.1)), (4.2, swing(-100, 76, 0.08, -0.1)), (5.0, swing(-48, 84, 0.03, -0.02)),
                  (5.7, LIGHT_HIT), (6.5, swing(58, 87, 0.02, 0.04)), (7.3, swing(100, 80, 0.06, -0.02)),
                  (8.4, swing(132, 62, 0.13, -0.09)), (10, L2_WRAP)]
    A["Light2"] = clip(15, {
        "OAR": light2_arc + [(15, STAND)],
        "HandR": [(1, hand(SWING_R, 1, *FREE_R)), (12, hand(SWING_R, 1, *FREE_R)), (15, hand(0.6, 1, *FREE_R))],
        "HandL": [(1, hand(SWING_L, 1, -0.2, -0.1, 0.4)), (11, hand(SWING_L, 1, -0.2, -0.1, 0.4)), (13.5, hand(SWING_L, 0, -0.2, -0.1, 0.33)), (15, REST["HandL"])],
        "Body": [(1, (4, 0, -46)), (2.8, (2, 0, -50)), (4.0, (3, 0, -26)), (5.0, (5, 0, 4)), (6.2, (6, 0, 40)), (7.6, (5, 0, 52)), (10, (4, 0, 46)), (15, REST["Body"])],
        "Pivot": [(1, (5, 0, 0)), (3, (-2, 0, 0)), (6, (8, 2, 0)), (10, (5, 0, 0)), (15, (0, 0, 0))],
        "Head": [(1, (-2, 0, 24)), (3.4, (-4, 0, 30)), (5.2, (-6, 0, 8)), (6.6, (4, 0, -22)), (8.6, (0, 0, -30)), (11, (-2, 0, -24)), (15, REST["Head"])],
        "Root": [(1, ((0, 0, 0), (0, -0.05, -0.03))), (3, ((0, 0, 0), (0, -0.02, -0.02))), (6, ((0, 0, 0), (0, -0.08, -0.05))), (10, ((0, 0, 0), (0, -0.05, -0.03))), (15, REST["Root"])],
        "FootL": [(1, ((0, 0, 0), (-0.02, -0.12, 0))), (15, ((0, 0, 0), (0, 0, 0)))],
        "FootR": [(3.5, ((0, 0, 0), (0, 0, 0.04))), (5.5, ((0, 0, 0), (0.02, -0.1, 0))), (12, ((0, 0, 0), (0.02, -0.1, 0))), (14.5, ((0, 0, 0), (0, 0, 0)))],
    }, motion=[(4.6, (0, 0, 0)), (5.2, (0, -5, 0)), (7.0, (0, -5, 0)), (7.6, (0, 0, 0))])

    # Heavy (1.12 s): the overhead chop. The left hand comes onto the oar's foot as it's swung up over the right
    # shoulder, the body rising and arching back after it, and on up and back (the blade low behind: a held breath at
    # the top); the windup's end (f16.2) lets it go: the hands drive down in front on a slight diagonal, the body
    # folding after them, and the blade meets its target at chest height at f19.3 (from the game's camera a chop that
    # lands on the ground foreshortens to nothing); on down, the blade knocking the ground and bouncing, the head
    # dipping after; held low (the recovery), then hauled back up onto its foot.
    hh = {"HandR": hand(HEAVY_R, 1, *FREE_R), "HandL": hand(HEAVY_L, 1, *FREE_L)}
    A["Heavy"] = clip(35, keys(held(REST, 1), {
        "OAR": [(5, oar(-10, 34, 0, (0.26, -0.04, 0.6), s=0.36)), (10, oar(-46, 34, 0, (0.18, -0.02, 0.72), s=0.36)), (14, HEAVY_RAISED),
                (16.2, oar(-70, 28, 0, (0.15, 0.06, 0.8), s=0.36)), (17.3, oar(-16, 16, 0, (0.13, -0.06, 0.84), s=0.36)),
                (18.3, oar(30, 0, 0, (0.04, -0.22, 0.72), s=0.36)), (19.3, HEAVY_HIT), (20.6, oar(98, -16, 0, (-0.04, -0.36, 0.46), s=0.36)),
                (22, oar(92, -17, 0, (-0.04, -0.35, 0.47), s=0.36)), (24, oar(96, -17, 0, (-0.04, -0.35, 0.45), s=0.36)),
                (28, oar(90, -14, 0, (-0.02, -0.33, 0.47), s=0.36)), (32, oar(30, 30, -20, (0.25, -0.18, 0.55), s=0.45)), (35, STAND)],
        "HandR": [(3, hand(0.6, 1, *FREE_R)), (6, hh["HandR"]), (29, hh["HandR"]), (33, hand(0.6, 1, *FREE_R))],
        "HandL": [(2, hand(HEAVY_L, 0, -0.14, -0.2, 0.42)), (6, hh["HandL"]), (29, hh["HandL"]), (32, hand(HEAVY_L, 0, -0.2, -0.12, 0.36)), (35, REST["HandL"])],
        "Root": [(4, ((0, 0, 0), (0, 0, -0.04))), (12, ((0, 0, 0), (0, 0.04, 0.03))), (16.2, ((0, 0, 0), (0, 0.05, 0.035))),
                 (19.3, HEAVY_HIT_BODY["Root"]), (21, ((0, 0, 0), (0, -0.07, -0.08))), (26, ((0, 0, 0), (0, -0.06, -0.065))), (35, REST["Root"])],
        "Pivot": [(4, (2, 0, 0)), (12, (-3, 0, 0)), (16.2, (-4, 0, 0)), (19.3, HEAVY_HIT_BODY["Pivot"]), (21, (5, 0, 0)), (27, (4, 0, 0)), (35, (0, 0, 0))],
        "Body": [(4, (5, 0, 10)), (11, (-6, 0, 22)), (15.5, (-9, 0, 26)), (18.6, (8, 0, -6)), (19.8, HEAVY_HIT_BODY["Body"]), (22, (19, 0, -14)), (28, (14, 0, -10)), (35, REST["Body"])],
        "Head": [(6, (2, 0, -4)), (12, (8, 0, -10)), (16.5, (12, 0, -12)), (19.6, (-14, 0, 6)), (21.3, (-8, 0, 10)), (24, (-12, 0, 8)), (35, REST["Head"])],
        "FootL": [(6, ((0, 0, 0), (0, 0, 0.03))), (9, ((0, 0, 0), (-0.02, -0.12, 0))), (30, ((0, 0, 0), (-0.02, -0.12, 0))), (34, ((0, 0, 0), (0, 0, 0)))],
        "FootR": [(14, ((0, 0, 0), (0, 0, 0))), (16, ((20, 0, 0), (0, 0.02, 0.02))), (19, ((0, 0, 0), (0, 0, 0)))],
    }), motion=[(16.2, (0, 0, 0)), (16.8, (0, -7.5, 0)), (19.0, (0, -7.5, 0)), (19.8, (0, 0, 0))])

    # StrokeDraw (loop, held as long as the heavy is): drawn back low round the oar's side, crouched, twisted, the blade
    # turned down to water that isn't there, the head turned back to the front; a strain in it (a tremble through the
    # oar and the arms, a breath held and let go).
    sd = {"HandR": hand(STROKE_R, 1, *FREE_R), "HandL": hand(STROKE_L, 1, *FREE_L)}
    A["StrokeDraw"] = clip(24, {
        "OAR": [(1, STROKE_DRAWN), (4, oar(96.6, 131, 90, (0.26, 0.145, 0.378), s=0.46)), (7, oar(96.3, 132, 90, (0.265, 0.145, 0.379), s=0.46)),
                (12, STROKE_STRAIN), (14, oar(97.6, 134, 90, (0.27, 0.155, 0.373), s=0.46)), (16, oar(98.4, 136, 90, (0.27, 0.165, 0.37), s=0.46)),
                (19, oar(97, 133, 90, (0.265, 0.15, 0.375), s=0.46))],
        "HandR": [(1, sd["HandR"])], "HandL": [(1, sd["HandL"])],
        "Root": [(1, STROKE_BODY["Root"]), (12, ((0, 0, 0), (0, 0.025, -0.11))), (20, ((0, 0, 0), (0, 0.02, -0.098)))],
        "Pivot": [(1, (5, 0, 0)), (12, (6, 0, 0)), (20, (5, 0, 0))],
        "Body": [(1, (13, 0, 44)), (6, (12.5, 0, 45)), (12, (14.5, 0, 47)), (15, (15, 0, 47.5)), (20, (13, 0, 45))],
        "Head": [(3, (-6, 0, -36)), (14, (-8, 0, -38)), (21, (-6, 0, -36))],
        "FootL": [(1, ((0, 0, 0), (-0.03, -0.12, 0)))], "FootR": [(1, ((14, 0, 0), (0.02, 0.1, 0.0)))],
    }, periodic=True)

    # Stroke (0.62 s): let go, a squeeze further back for a frame, then it sweeps low across the front like a paddle
    # pulled through the water, the body unwinding ahead of it, rising, the head last: straight ahead at f5.7 (the wave
    # leaves the blade), on round to the left and up; it comes back up out of the crouch and stands the oar on its foot.
    sw = stroke_at
    A["Stroke"] = clip(18, keys(held(pose_of(STROKE_BODY, OAR=STROKE_STRAIN, **sd), 1), {
        "OAR": [(2, oar(99, 138, 90, (0.27, 0.17, 0.36), s=0.46)), (3, sw(112, 95, -0.13)), (4.4, sw(60, 93, -0.13, 0.06)),
                (5.7, sw(0, 92, -0.12, 0.1)), (6.9, sw(-62, 92, -0.1, 0.06)), (8.2, sw(-104, 86, -0.04, -0.02)),
                (9.6, sw(-118, 76, 0.02, -0.06)), (11.5, sw(-112, 72, 0.03, -0.06)), (14.5, oar(58, -26, 0, (-0.04, -0.28, 0.44), s=0.46)), (18, STAND)],
        "HandR": [(1, sd["HandR"]), (12, sd["HandR"]), (16, hand(0.6, 1, *FREE_R))],
        "HandL": [(1, sd["HandL"]), (12, sd["HandL"]), (14.5, hand(STROKE_L, 0, -0.2, -0.12, 0.36)), (18, REST["HandL"])],
        "Root": [(2, ((0, 0, 0), (0, 0.03, -0.115))), (5.7, ((0, 0, 0), (0, -0.08, -0.11))), (9, ((0, 0, 0), (0, -0.08, -0.06))), (18, REST["Root"])],
        "Pivot": [(2, (6, 0, 0)), (5.7, (7, 0, 0)), (9, (4, 0, 0)), (18, (0, 0, 0))],
        "Body": [(2, (15, 0, 48)), (4.6, (17, 0, 14)), (6.2, (18, 0, -26)), (8, (14, 0, -44)), (11, (10, 0, -38)), (18, REST["Body"])],
        "Head": [(3, (-6, 0, -38)), (6, (-8, 0, -6)), (8, (-4, 0, 22)), (11, (-2, 0, 22)), (18, REST["Head"])],
        "FootL": [(1, ((0, 0, 0), (-0.03, -0.12, 0))), (14, ((0, 0, 0), (-0.03, -0.12, 0))), (17, ((0, 0, 0), (0, 0, 0)))],
        "FootR": [(1, ((14, 0, 0), (0.02, 0.1, 0.0))), (4, ((14, 0, 0), (0.02, 0.1, 0.0))), (6, ((0, 0, 0), (0.02, -0.02, 0.0))), (16, ((0, 0, 0), (0, 0, 0)))],
    }), motion=[(4, (0, 0, 0)), (5.5, (0, -1.5, 0)), (8, (0, 0, 0))])

    # RecoilHeavy (0.35 s): from the chop's contact (seen to land), the oar springs back up off the heap, jarring the
    # Apprentice back (the head snapping after), a wobble; it gathers the oar back onto its foot.
    A["RecoilHeavy"] = clip(10, keys(held(pose_of(REST, OAR=HEAVY_HIT, **hh, **HEAVY_HIT_BODY), 1), {
        "OAR": [(2.5, oar(40, 10, 0, (0.06, -0.16, 0.62), s=0.36)), (4.5, oar(50, 16, 0, (0.08, -0.2, 0.58), s=0.36)),
                (6.5, oar(30, 16, -14, (0.12, -0.2, 0.62), s=0.42)), (10, STAND)],
        "HandR": [(1, hh["HandR"]), (5, hh["HandR"]), (8, hand(0.6, 1, *FREE_R))],
        "HandL": [(1, hh["HandL"]), (5, hh["HandL"]), (8, hand(HEAVY_L, 0, -0.22, -0.06, 0.38)), (10, REST["HandL"])],
        "Root": [(2.5, ((0, 0, 0), (0, 0.06, -0.02))), (5, ((0, 0, 0), (0, 0.05, -0.03))), (10, REST["Root"])],
        "Pivot": [(2.5, (-10, 0, 0)), (5, (-3, 0, 0)), (7, (2, 0, 0)), (10, (0, 0, 0))],
        "Body": [(2.5, (-10, 0, -4)), (5.5, (-2, 0, 2)), (10, REST["Body"])],
        "Head": [(3.2, (-20, 0, 2)), (6, (4, 0, 0)), (10, REST["Head"])],
        "FootR": [(2, ((0, 0, 0), (0, 0, 0))), (3.5, ((0, 0, 0), (0, 0.06, 0.03))), (5, ((0, 0, 0), (0, 0.06, 0)))],
        "FootL": [(1, ((0, 0, 0), (-0.02, -0.12, 0))), (6, ((0, 0, 0), (-0.02, -0.1, 0.02))), (9, ((0, 0, 0), (0, 0, 0)))],
    }), motion=[(1, (0, 14, 0)), (5, (0, 3.6, 0)), (10, (0, 0.6, 0))])

    # RecoilLight (0.35 s): from the swing's contact, knocked back and up and round to the right, the body jolted after
    # it, the head after that; back onto its foot.
    lh = two()
    A["RecoilLight"] = clip(10, keys(held(pose_of(REST, OAR=LIGHT_HIT, Body=(5, 0, -4), Head=(-6, 0, -8), Pivot=(8, 0, 0),
                                                  Root=((0, 0, 0), (0, -0.07, -0.05)), **lh), 1), {
        "OAR": [(2.5, swing(46, 58, 0.16, -0.1)), (4.5, swing(66, 50, 0.14, -0.12)), (7, oar(30, 40, -30, (0.24, -0.12, 0.6), s=0.5)), (10, STAND)],
        "HandR": [(1, lh["HandR"]), (5, lh["HandR"]), (8, hand(0.6, 1, *FREE_R))],
        "HandL": [(1, lh["HandL"]), (5, lh["HandL"]), (8, hand(SWING_L, 0, -0.22, -0.06, 0.38)), (10, REST["HandL"])],
        "Root": [(2.5, ((0, 0, 0), (0, 0.04, -0.02))), (5, ((0, 0, 0), (0, 0.04, -0.02))), (10, REST["Root"])],
        "Pivot": [(2.5, (-8, 0, 0)), (5, (-2, 0, 0)), (10, (0, 0, 0))],
        "Body": [(2.5, (-6, 0, 18)), (5.5, (0, 0, 10)), (10, REST["Body"])],
        "Head": [(3.2, (-14, 0, -4)), (6, (2, 0, 2)), (10, REST["Head"])],
        "FootL": [(1, ((0, 0, 0), (-0.02, -0.12, 0))), (6, ((0, 0, 0), (-0.02, -0.06, 0.02))), (9, ((0, 0, 0), (0, 0, 0)))],
    }), motion=[(1, (0, 14, 0)), (5, (0, 3.6, 0)), (10, (0, 0.6, 0))])

    # HookAim (loop, one turn): the Hook whirled round on its chain overhead from the left hand (the wrist leading it
    # round), the body swaying with each turn; the oar held back out of the way in the right hand; the head on the
    # target.
    def whirl(f, p):
        th = 2 * math.pi * (f - 1) / 16
        centre = Vector((-0.22, -0.04, 0.95))
        n = Vector((-0.3, 0.25, 1)).normalized()
        u = n.cross(Vector((1, 0, 0))).normalized()
        v = n.cross(u)
        at = centre + 0.46 * (math.cos(th) * u + math.sin(th) * v)
        ahead = -math.sin(th) * u + math.cos(th) * v  # (the way it's going)
        chain = (centre - at).normalized().lerp(-ahead, 0.35).normalized()  # (dragged a little behind the turn)
        r = Vector((0, 0, 1)).rotation_difference(chain).to_euler("XYZ")
        p["Hook"] = (tuple(math.degrees(a) for a in r), tuple(at - HOOK_HEAD))
        p["HandL"] = hand(0.4, 0, *(centre + 0.035 * (math.cos(th - 1.2) * u + math.sin(th - 1.2) * v)))

    A["HookAim"] = clip(17, {
        "Hook": [(1, ((0, 0, 0), (0, 0, 0)))],
        "Root": [(1, ((0, 0, 0), (0, 0, 0))), (9, ((0, 0, 0), (0, 0, -0.012)))],
        "Pivot": [(1, (-3, 1.5, 0)), (9, (-3, -1.5, 0))],
        "Body": [(3, (-5, 0, 6)), (11, (-5, 0, -4))],
        "Head": [(1, (2, 0, -4)), (9, (3, 0, 3))],
        "OAR": [(1, oar(-24, 26, 0, (0.33, 0.06, 0.5), s=0.45)), (9, oar(-26, 24, 0, (0.33, 0.07, 0.49), s=0.45))],
        "HandR": [(1, hand(0.45, 1, *FREE_R))],
        "FootL": [(1, ((0, 0, 0), (-0.02, -0.06, 0)))], "FootR": [(1, ((0, 0, 0), (0.02, 0.05, 0)))],
    }, periodic=True, custom=whirl)

    # HookThrow (0.15 s; the Hook out on its tether, the tether from the left hand): from the whirl, flung forward
    # overarm, the body lunging after it, the arm out straight at the end.
    A["HookThrow"] = clip(4, {
        "HandL": [(1, hand(0.4, 0, -0.22, -0.04, 0.95)), (2, hand(0.4, 0, -0.2, 0.04, 0.97)), (4, hand(0.4, 0, -0.08, -0.4, 0.72))],
        "Root": [(1, ((0, 0, 0), (0, 0, 0))), (4, ((0, 0, 0), (0, -0.05, -0.02)))],
        "Pivot": [(1, (-3, 0, 0)), (2, (-6, 0, 0)), (4, (12, 0, 0))],
        "Body": [(1, (-5, 0, 0)), (2, (-8, 0, 12)), (4, (10, 0, -12))],
        "Head": [(1, (2, 0, 0)), (3, (-2, 0, -6)), (4, (-8, 0, 8))],
        "OAR": [(1, oar(-24, 26, 0, (0.33, 0.06, 0.5), s=0.45)), (4, oar(-30, 28, 0, (0.33, 0.1, 0.46), s=0.45))],
        "HandR": [(1, hand(0.45, 1, *FREE_R))],
        "FootL": [(1, ((0, 0, 0), (-0.02, -0.06, 0))), (4, ((0, 0, 0), (-0.02, -0.14, 0)))], "FootR": [(1, ((0, 0, 0), (0.02, 0.05, 0)))],
    }, hook_hand="L")

    # HookPull (loop, held for the pull): dragged along by the chain, leaning into it, the left arm out ahead on the
    # tether, the oar trailing in the right hand, the feet off the ground and trailing; everything loose streams back.
    A["HookPull"] = clip(8, {
        "HandL": [(1, hand(0.4, 0, -0.08, -0.4, 0.74)), (5, hand(0.4, 0, -0.07, -0.41, 0.76))],
        "Root": [(1, ((0, 0, 0), (0, 0, 0.08))), (5, ((0, 0, 0), (0, 0, 0.095)))],
        "Pivot": [(1, (28, 0, 0)), (5, (30, 1, 0))],
        "Body": [(1, (4, 0, -6)), (5, (5, 0, -5))],
        "Head": [(2, (-20, 0, 4)), (6, (-22, 0, 3))],
        "OAR": [(1, oar(-74, 16, 0, (0.3, 0.2, 0.5), s=0.45)), (5, oar(-76, 14, 0, (0.3, 0.21, 0.51), s=0.45))],
        "HandR": [(1, hand(0.45, 1, *FREE_R))],
        "FootL": [(1, ((40, 0, 0), (0, 0.16, 0.1))), (5, ((44, 0, 0), (0, 0.17, 0.11)))],
        "FootR": [(3, ((34, 0, 0), (0, 0.1, 0.06))), (7, ((38, 0, 0), (0, 0.11, 0.07)))],
    }, periodic=True, hook_hand="L", motion=[(1, (0, -13, 0))])

    # Pickup (0.6 s; the item's taken at the bottom, f7.5): a quick bob down to it, the head first (it looks), the left
    # hand reaching to the ground ahead, the oar leaning on its foot with the bow; the item gathered in to the chest on
    # the way up.
    A["Pickup"] = clip(14, keys(held(REST, 1), {
        "Head": [(3, (14, 0, -4)), (6, (24, 0, -6)), (9, (16, 0, -2)), (12, (-4, 0, 0)), (14, REST["Head"])],
        "Pivot": [(3, (8, 0, 0)), (6, (24, 0, -4)), (8.5, (23, 0, -4)), (11, (6, 0, 0)), (14, (0, 0, 0))],
        "Body": [(3, (8, 0, -2)), (6.5, (26, 0, -8)), (8.5, (25, 0, -8)), (11, (6, 0, -2)), (14, REST["Body"])],
        "Root": [(3, ((0, 0, 0), (0, -0.02, -0.06))), (6, ((0, 0, 0), (0, -0.04, -0.17))), (8.5, ((0, 0, 0), (0, -0.04, -0.16))), (11.5, ((0, 0, 0), (0, 0, 0.01))), (14, REST["Root"])],
        "OAR": [(3, planted(16, 30)), (6, planted(34, -10)), (8.5, planted(33, -8)), (11.5, planted(14, 40)), (14, STAND)],
        "HandR": [(1, hand(0.6, 1, *FREE_R)), (5, hand(0.5, 1, *FREE_R)), (10, hand(0.5, 1, *FREE_R)), (13, hand(0.6, 1, *FREE_R))],
        "HandL": [(3, hand(0.4, 0, -0.16, -0.25, 0.3)), (6, hand(0.4, 0, -0.06, -0.44, 0.12)), (8.5, hand(0.4, 0, -0.06, -0.42, 0.13)),
                  (11, hand(0.4, 0, -0.08, -0.2, 0.5)), (14, REST["HandL"])],
        "FootL": [(4, ((0, 0, 0), (0, 0, 0.03))), (6, ((0, 0, 0), (-0.01, -0.08, 0))), (11, ((0, 0, 0), (-0.01, -0.08, 0))), (13.5, ((0, 0, 0), (0, 0, 0)))],
    }), free={"L": None})

    # RaiseItem (2.4 s): a dip, the left hand taking the Hook from the hip (turned prongs up as it comes), then held
    # up high beside the head by its chain, the body rising onto its toes and leaning away under it, the head tipped
    # back to look up at it (the right hand keeps the oar); the light flares at the top (f17, the Hook's prongs: the
    # game's HeldPoint), a small push higher; held, breathing; then lowered and hung back at the hip.
    def hook_in(at, rot=(180, 0, 0)):
        """The Hook held by its chain's end at `at` (the body's frame, as at rest), turned `rot`."""
        turned = Euler([math.radians(a) for a in rot]).to_matrix()
        head = Vector(at) - turned @ Vector((0, 0, 0.43))
        return (rot, tuple(round(c, 4) for c in head - HOOK_HEAD))

    up_high = (-0.28, -0.08, 0.95)
    A["RaiseItem"] = clip(58, keys(held(REST, 1), {
        "Hook": [(4, ((0, 0, 0), (0, 0, 0))), (8, hook_in((-0.26, -0.16, 0.5))), (14, hook_in(up_high)), (16.5, hook_in((-0.285, -0.08, 0.98))),
                 (19, hook_in(up_high)), (30, hook_in((-0.28, -0.075, 0.945), (180, 0, 4))), (44, hook_in(up_high)),
                 (51, hook_in((-0.26, -0.16, 0.5))), (55, ((0, 0, 0), (0, 0, 0)))],
        "HookL": [(1, on_hook(0, 0.22, 0.16, 0.93)), (4.5, on_hook(0, 0.22, 0.16, 0.93)), (7.5, on_hook(1, 0.22, 0.16, 0.93)),
                  (51.5, on_hook(1, 0.22, 0.16, 0.93)), (55, on_hook(0, 0.22, 0.16, 0.93))],
        "HandL": [(1, REST["HandL"]), (4, hand(0.4, 0, -0.22, -0.16, 0.44)), (52, hand(0.4, 0, -0.22, -0.16, 0.44)), (58, REST["HandL"])],
        "OAR": [(1, STAND), (6, planted(8, 44)), (16, planted(12, 54)), (46, planted(11, 52)), (58, STAND)],
        "Root": [(5, ((0, 0, 0), (0, 0, -0.06))), (12, ((0, 0, 0), (0, 0, 0.03))), (16.5, ((0, 0, 0), (0, 0, 0.045))), (20, ((0, 0, 0), (0, 0, 0.03))),
                 (32, ((0, 0, 0), (0, 0, 0.024))), (44, ((0, 0, 0), (0, 0, 0.03))), (51, ((0, 0, 0), (0, 0, -0.04))), (58, REST["Root"])],
        "Pivot": [(5, (6, 0, 0)), (13, (-3, 4, 0)), (17, (-4, 5, 0)), (30, (-3, 4, 1)), (44, (-4, 4, 0)), (51, (5, 0, 0)), (58, (0, 0, 0))],
        "Body": [(6, (8, 0, -6)), (14, (-6, 0, 4)), (17, (-9, 0, 5)), (24, (-6, 0, 5)), (36, (-7, 0, 3)), (45, (-6, 0, 4)), (52, (6, 0, -4)), (58, REST["Body"])],
        "Head": [(7, (10, 0, -8)), (15, (-20, 0, -14)), (18, (-26, 0, -16)), (26, (-22, 0, -12)), (38, (-24, 0, -15)), (46, (-18, 0, -12)), (53, (8, 0, -4)), (58, REST["Head"])],
    }))

    # Toss (0.4 s; the pot let go at f5): the left arm wound back and up behind the shoulder, the body turning away
    # with it, then whipped round and over, let go out ahead and high at f5, following through down; the oar kept back
    # out of the way in the right hand.
    A["Toss"] = clip(10, keys(held(REST, 1), {
        "HandL": [(2.6, hand(0.4, 0, -0.24, 0.14, 0.78)), (4, hand(0.4, 0, -0.16, -0.1, 0.94)), (5, hand(0.4, 0, -0.06, -0.34, 0.86)),
                  (6.5, hand(0.4, 0, 0.0, -0.36, 0.52)), (10, REST["HandL"])],
        "Body": [(2.4, (-12, 0, -26)), (4, (0, 0, -4)), (5.2, (8, 0, 16)), (7, (6, 0, 12)), (10, REST["Body"])],
        "Pivot": [(2.4, (-5, -3, 0)), (5, (4, 2, 0)), (7, (3, 0, 0)), (10, (0, 0, 0))],
        "Head": [(3, (6, 0, 18)), (5, (-14, 0, -6)), (6.5, (-10, 0, -12)), (10, REST["Head"])],
        "Root": [(2.4, ((0, 0, 0), (0, 0.04, -0.03))), (5, ((0, 0, 0), (0, -0.05, -0.02))), (10, REST["Root"])],
        "OAR": [(3, planted(-2, 50, butt=(0.34, 0.0, 0.03))), (6, planted(-4, 52, butt=(0.34, 0.04, 0.03))), (10, STAND)],
        "FootL": [(2, ((0, 0, 0), (0, 0.04, 0.02))), (4.5, ((0, 0, 0), (-0.02, -0.12, 0))), (10, ((0, 0, 0), (0, 0, 0)))],
    }))

    # RaiseOar (2.4 s): a dip, the left hand onto the oar low, then hoisted up high beside the head in both hands,
    # leaning out a little, blade to the sky, the body rising onto its toes, the head tipped back to look up at it; a
    # pump at the top (its new look arrives there, f17); held, breathing; then lowered onto its foot again.
    up = oar(7, 90, 0, (0.24, -0.07, 0.92), s=0.4)
    A["RaiseOar"] = clip(58, keys(held(REST, 1), {
        "OAR": [(6, oar(4, 50, -40, (0.26, -0.1, 0.5), s=0.42)), (12, oar(6, 84, 0, (0.24, -0.07, 0.86), s=0.4)), (15, up),
                (17, oar(7, 90, 0, (0.245, -0.07, 0.945), s=0.4)), (20, up), (32, oar(8, 91, 2, (0.24, -0.065, 0.91), s=0.4)), (46, up),
                (52, oar(4, 50, -40, (0.26, -0.1, 0.52), s=0.42)), (58, STAND)],
        "HandR": [(1, hand(0.6, 1, *FREE_R)), (6, hand(0.46, 1, *FREE_R)), (11, hand(0.42, 1, *FREE_R)), (49, hand(0.42, 1, *FREE_R)), (53, hand(0.6, 1, *FREE_R))],
        "HandL": [(2, hand(0.3, 0, -0.1, -0.2, 0.42)), (6, hand(0.3, 1, *FREE_L)), (11, hand(0.17, 1, *FREE_L)), (49, hand(0.17, 1, *FREE_L)),
                  (54, hand(0.2, 0, -0.2, -0.12, 0.36)), (58, REST["HandL"])],
        "Root": [(5, ((0, 0, 0), (0, 0, -0.07))), (12, ((0, 0, 0), (0, 0, 0.03))), (17, ((0, 0, 0), (0, 0, 0.05))), (21, ((0, 0, 0), (0, 0, 0.03))),
                 (32, ((0, 0, 0), (0, 0, 0.022))), (46, ((0, 0, 0), (0, 0, 0.03))), (52, ((0, 0, 0), (0, 0, -0.04))), (58, REST["Root"])],
        "Pivot": [(5, (6, 0, 0)), (13, (-3, -3, 0)), (17, (-4, -4, 0)), (30, (-3, -3, 1)), (46, (-4, -3, 0)), (52, (4, 0, 0)), (58, (0, 0, 0))],
        "Body": [(6, (8, 0, 8)), (14, (-6, 0, -4)), (17, (-9, 0, -5)), (24, (-6, 0, -4)), (36, (-7, 0, -5)), (46, (-6, 0, -4)), (52, (6, 0, 4)), (58, REST["Body"])],
        "Head": [(7, (10, 0, 4)), (15, (-22, 0, 12)), (18, (-27, 0, 14)), (26, (-24, 0, 11)), (38, (-25, 0, 14)), (46, (-20, 0, 10)), (53, (8, 0, 2)), (58, REST["Head"])],
    }))

    # Heal (0.7 s): the free hand raised to the mask, the head bowing down to meet it, the body after; a breath of
    # relief (the shoulders rise and drop, slowly); the hand lowered, the head lifting last.
    A["Heal"] = clip(22, keys(held(REST, 1), {
        "HandL": [(3, hand(0.4, 0, -0.16, -0.2, 0.42)), (7, hand(0.4, 0, -0.05, -0.27, 0.66)), (15, hand(0.4, 0, -0.05, -0.27, 0.665)),
                  (18.5, hand(0.4, 0, -0.14, -0.2, 0.42)), (22, REST["HandL"])],
        "Head": [(3, (4, 0, 2)), (8, (15, 0, 6)), (13, (12, 0, 4)), (16, (15, 0, 4)), (21, (-4, 0, 0)), (22, REST["Head"])],
        "Body": [(5, (6, 0, -2)), (9, (9, 0, -4)), (13, (5, 0, -3)), (16, (8, 0, -3)), (22, REST["Body"])],
        "Pivot": [(5, (3, 0, 0)), (12, (0, 0, 0)), (16, (4, 0, 0)), (22, (0, 0, 0))],
        "Root": [(5, ((0, 0, 0), (0, 0, -0.03))), (12, ((0, 0, 0), (0, 0, -0.012))), (16, ((0, 0, 0), (0, 0, -0.04))), (22, REST["Root"])],
        "OAR": [(6, planted(13, 46)), (16, planted(12, 47)), (22, STAND)],
    }))

    # Death (2.2 s): reeling back from the blow (the head snapped back after the body), a stagger forward, the knees
    # going (it drops, a bounce at the bottom), the oar slipping from the hand to clatter down beside it; kneeling, the
    # head bowed, the breath going out of it (the bow lantern gutters out, from f25); then it slumps forward to lie
    # still, the arms going limp.
    lying = planted(90, 74, 90, butt=(0.4, -0.1, 0.05))
    A["Death"] = clip(66, keys(held(REST, 1), {
        "Root": [(4, ((0, 0, 0), (0, 0.1, 0.02))), (8, ((0, 0, 0), (0, 0.06, -0.06))), (12, ((0, 0, 0), (0, 0.05, -0.22))), (14, ((0, 0, 0), (0, 0.05, -0.19))),
                 (17, ((0, 0, 0), (0, 0.05, -0.21))), (46, ((0, 0, 0), (0, 0.05, -0.22))), (56, ((0, 0, 0), (0, -0.1, -0.24))),
                 (61, ((0, 0, 0), (0, -0.24, -0.15))), (63, ((0, 0, 0), (0, -0.24, -0.135))), (66, ((0, 0, 0), (0, -0.24, -0.145)))],
        "Pivot": [(4, (-24, 0, 8)), (8, (6, -4, 4)), (12, (22, 0, 0)), (15, (18, 0, 2)), (20, (22, 0, 2)), (30, (24, 0, 4)), (46, (27, 0, -3)),
                  (55, (60, 0, 8)), (61, (84, 0, 12)), (63, (80, 0, 12)), (66, (82, 0, 12))],
        "Body": [(5, (-8, 0, 6)), (10, (8, 0, -2)), (16, (6, 0, 0)), (40, (9, 0, 0)), (54, (4, 0, 0)), (66, (2, 0, 0))],
        "Head": [(3, (-6, 0, 0)), (5, (-26, 0, 0)), (9, (-6, 0, 4)), (14, (22, 0, 0)), (17, (18, 0, 0)), (28, (32, 0, 0)), (46, (38, 0, -4)),
                 (56, (24, 0, 0)), (62, (16, 0, 0)), (66, (20, 0, 0))],
        "OAR": [(4, oar(-16, 46, -40, (0.42, 0.0, 0.62), s=0.6)), (8, planted(8, 60, -40, butt=(0.37, -0.06, 0.03))),
                (11, planted(40, 70, 0, butt=(0.39, -0.08, 0.03))), (14, planted(78, 74, 60, butt=(0.4, -0.09, 0.04))),
                (15.5, planted(89, 74, 84, butt=(0.4, -0.1, 0.05))), (17.5, planted(85, 75, 88, butt=(0.4, -0.1, 0.08))), (19.5, lying)],
        "HandR": [(1, hand(0.6, 1, *FREE_R)), (8, hand(0.6, 1, *FREE_R)), (11, hand(0.6, 0, 0.24, -0.1, 0.36)), (40, hand(0.6, 0, 0.23, -0.1, 0.33)),
                  (56, hand(0.6, 0, 0.24, -0.16, 0.3)), (61, hand(0.6, 0, 0.26, -0.12, 0.26))],
        "HandL": [(4, hand(0.4, 0, -0.26, -0.02, 0.44)), (9, hand(0.4, 0, -0.2, -0.16, 0.38)), (14, hand(0.4, 0, -0.18, -0.2, 0.3)),
                  (40, hand(0.4, 0, -0.17, -0.2, 0.3)), (56, hand(0.4, 0, -0.24, -0.18, 0.3)), (61, hand(0.4, 0, -0.27, -0.12, 0.26))],
        "FootL": [(8, ((0, 0, 0), (0, 0, 0))), (12, ((40, 0, 0), (0, 0.16, 0.02))), (56, ((40, 0, 0), (0, 0.16, 0.02))), (61, ((60, 0, 0), (0, 0.06, 0.02)))],
        "FootR": [(6, ((0, 0, 0), (0, 0.08, 0.02))), (8, ((0, 0, 0), (0, 0.06, 0))), (12, ((40, 0, 0), (0, 0.18, 0.02))), (56, ((40, 0, 0), (0, 0.18, 0.02))), (61, ((60, 0, 0), (0, 0.08, 0.02)))],
    }), motion=[(1, (0, 3, 0)), (6, (0, 0.5, 0)), (10, (0, 0, 0))])

    # Block (loop, held): the oar across the front in both hands, the blade's face to the foe, braced low behind it,
    # the head tucked; breathing, and the guard shifting a little in the hands.
    A["Block"] = clip(48, {
        "OAR": [(1, BLOCK_OAR), (14, oar(87, -88, 90, (0.0, -0.3, 0.565), s=0.62)), (26, oar(88.5, -91, 90, (0.0, -0.305, 0.555), s=0.62)),
                (38, oar(87.5, -89, 90, (0.0, -0.3, 0.562), s=0.62))],
        "HandR": [(1, BLOCK["HandR"])], "HandL": [(1, BLOCK["HandL"])],
        "Root": [(1, BLOCK["Root"]), (16, ((0, 0, 0), (0, 0.02, -0.038))), (34, ((0, 0, 0), (0, 0.02, -0.05)))],
        "Pivot": [(1, (-3, 0, 0)), (12, (-3, 1, 0)), (36, (-3, -1, 0))],
        "Body": [(1, (-3, 0, 0)), (18, (-4.5, 0, 1)), (36, (-2.5, 0, -1))],
        "Head": [(3, (8, 0, 0)), (20, (6, 0, 2)), (38, (9, 0, -2))],
        "FootL": [(1, BLOCK["FootL"])], "FootR": [(1, BLOCK["FootR"])],
    }, periodic=True)

    # Parry (0.3 s; a perfect block): the guard snapped out to meet the blow, the body driving in behind it, the head
    # after; then drawn back into the block.
    A["Parry"] = clip(10, keys(held(BLOCK, 1), held(BLOCK, 10), {
        "OAR": [(2.5, oar(78, -72, 90, (-0.02, -0.48, 0.66), s=0.62)), (5, oar(85, -82, 90, (0.0, -0.4, 0.6), s=0.62))],
        "Root": [(2.5, ((0, 0, 0), (0, -0.06, -0.03))), (6, ((0, 0, 0), (0, -0.03, -0.04)))],
        "Pivot": [(2.5, (10, 0, 0)), (6, (3, 0, 0))],
        "Body": [(2.5, (6, 0, 10)), (6, (0, 0, 3))],
        "Head": [(3.5, (-6, 0, -6)), (7, (8, 0, 0))],
        "FootL": [(2, ((0, 0, 0), (-0.02, -0.12, 0)))],
    }))

    # Row (loop, two strokes: right then left; the game sets the pace): sat facing the bow, a paddle stroke. The blade
    # dipped in ahead (the catch), the body leaning in after it; pulled back along the side, the body pulling back
    # (the hands' grips swap as it goes over); lifted out and swung over to the other side.
    def row(pitch, yaw, centre):
        return oar(pitch, yaw, 90, centre, s=0.42)  # (the hands' middle: 0.42 up the shaft)

    A["Row"] = clip(49, {
        "Root": [(1, ((0, 0, 0), SIT)), (9, ((0, 0, 0), (0, 0, -0.17))), (25, ((0, 0, 0), SIT)), (33, ((0, 0, 0), (0, 0, -0.17)))],
        "OAR": [(1, row(128, 46, (0.12, -0.16, 0.42))), (9, row(124, 128, (0.14, 0.1, 0.38))), (14, row(100, 118, (0.12, 0.08, 0.5))),
                (19, row(72, 0, (0.0, -0.24, 0.4))),
                (25, row(128, -46, (-0.12, -0.16, 0.42))), (33, row(124, -128, (-0.14, 0.1, 0.38))), (38, row(100, -118, (-0.12, 0.08, 0.5))),
                (43, row(72, 0, (0.0, -0.24, 0.4)))],
        "HandL": [(1, hand(0.12, 1, *FREE_L)), (14, hand(0.12, 1, *FREE_L)), (19, hand(0.3, 1, *FREE_L)), (23, hand(0.72, 1, *FREE_L)),
                  (38, hand(0.72, 1, *FREE_L)), (43, hand(0.56, 1, *FREE_L)), (47, hand(0.12, 1, *FREE_L))],
        "HandR": [(1, hand(0.72, 1, *FREE_R)), (14, hand(0.72, 1, *FREE_R)), (19, hand(0.56, 1, *FREE_R)), (23, hand(0.12, 1, *FREE_R)),
                  (38, hand(0.12, 1, *FREE_R)), (43, hand(0.3, 1, *FREE_R)), (47, hand(0.72, 1, *FREE_R))],
        "Pivot": [(1, (16, 0, 0)), (9, (-6, 0, 0)), (17, (6, 0, 0)), (25, (16, 0, 0)), (33, (-6, 0, 0)), (41, (6, 0, 0))],
        "Body": [(1, (2, 6, 15)), (9, (0, -2, -12)), (15, (0, 0, -6)), (25, (2, -6, -15)), (33, (0, 2, 12)), (39, (0, 0, 6))],
        "Head": [(3, (-12, 0, -12)), (11, (4, 0, 9)), (18, (-2, 0, 0)), (27, (-12, 0, 12)), (35, (4, 0, -9)), (42, (-2, 0, 0))],
        "FootL": [(1, SAT_FEET["FootL"])], "FootR": [(1, SAT_FEET["FootR"])],
    }, periodic=True, floor=None, poles={"L": (-1, 0.2, -0.4), "R": (1, 0.2, -0.4)})

    # RowDraw (loop, held): the Stroke from the boat: the oar raised straight up overhead in both hands, blade high (its
    # glow seen, gathering); a strain in it.
    A["RowDraw"] = clip(24, {
        "OAR": [(1, ROW_DRAW), (5, oar(13, 91, 1, (0.2, -0.055, 0.705), s=0.32)), (12, ROW_STRAIN), (15, oar(17, 92, -1, (0.21, -0.05, 0.72), s=0.32)), (19, oar(14, 90, 0, (0.205, -0.06, 0.71), s=0.32))],
        "HandR": [(1, ROW_DRAW_POSE["HandR"])], "HandL": [(1, ROW_DRAW_POSE["HandL"])],
        "Root": [(1, ((0, 0, 0), SIT)), (12, ((0, 0, 0), (0, 0, -0.15))), (20, ((0, 0, 0), SIT))],
        "Pivot": [(1, (-4, 0, 0)), (12, (-6, 0, 1)), (20, (-4, 0, 0))],
        "Body": [(1, (-8, -3, 0)), (12, (-11, -4, 2)), (16, (-11.5, -4, 2.5)), (20, (-8, -3, 0))],
        "Head": [(3, (-20, 0, 10)), (14, (-24, 0, 12)), (21, (-20, 0, 10))],
        "FootL": [(1, SAT_FEET["FootL"])], "FootR": [(1, SAT_FEET["FootR"])],
    }, periodic=True, floor=None)

    # RowSurge (0.7 s): let go: thrashed back over the right shoulder and down into the water behind the boat (the water
    # driven back, the boat on), hauled through with the whole body, then brought round to rest across the boat.
    A["RowSurge"] = clip(21, keys(held(pose_of(ROW_DRAW_POSE, OAR=ROW_STRAIN, Body=(-11, -4, 2), Head=(-24, 0, 12)), 1), {
        "OAR": [(3, oar(56, 202, 180, (0.16, 0.06, 0.6), s=0.32)), (5, oar(128, 200, -90, (0.16, 0.08, 0.42), s=0.4)), (9, oar(146, 204, -90, (0.18, 0.14, 0.34), s=0.4)),
                (13, oar(146, 206, -90, (0.18, 0.15, 0.34), s=0.4)), (16.5, oar(112, 70, -90, (0.08, -0.04, 0.36), s=0.45)), (21, ROW_REST)],
        "HandR": [(1, ROW_DRAW_POSE["HandR"]), (5, hand(0.55, 1, *FREE_R)), (21, ROW_IDLE["HandR"])],
        "HandL": [(1, ROW_DRAW_POSE["HandL"]), (3, hand(0.24, 1, *FREE_L)), (5, hand(0.3, 1, *FREE_L)), (13, hand(0.3, 1, *FREE_L)), (16.5, hand(0.32, 1, *FREE_L)), (21, ROW_IDLE["HandL"])],
        "Root": [(1, ((0, 0, 0), SIT)), (5, ((0, 0, 0), (0, 0.03, -0.17))), (21, ROW_IDLE["Root"])],
        "Pivot": [(2.5, (-10, 0, 0)), (5, (-14, 0, 4)), (9, (12, 0, 4)), (14, (10, 0, 2)), (21, ROW_IDLE["Pivot"])],
        "Body": [(3, (-20, 0, 10)), (6, (-6, 0, 20)), (10, (10, 0, 12)), (15, (6, 0, 6)), (21, ROW_IDLE["Body"])],
        "Head": [(4, (-16, 0, -6)), (7, (2, 0, -10)), (12, (-6, 0, -6)), (21, ROW_IDLE["Head"])],
        "FootL": [(1, SAT_FEET["FootL"])], "FootR": [(1, SAT_FEET["FootR"])],
    }), floor=None, motion=[(4, (0, 0, 0)), (6, (0, -4, 0)), (21, (0, -2, 0))])

    # RowIdle (loop): resting on the oar laid across the boat, the hands in the lap: a slow breath, the boat's sway in
    # the body, a look about over the water.
    A["RowIdle"] = clip(60, {
        "OAR": [(1, ROW_REST), (20, oar(90, 92, 90, (0.05, -0.12, 0.225), s=0.6)), (42, oar(90, 88, 90, (0.05, -0.125, 0.215), s=0.6))],
        "HandR": [(1, ROW_IDLE["HandR"])], "HandL": [(1, ROW_IDLE["HandL"])],
        "Root": [(1, ROW_IDLE["Root"]), (22, ((0, 0, 0), (0, 0, -0.151))), (44, ((0, 0, 0), (0, 0, -0.168)))],
        "Pivot": [(1, (6, 0, 0)), (16, (5, 2, 0)), (46, (7, -2, 0))],
        "Body": [(1, (4, 0, 0)), (24, (2, 0, 1)), (46, (6, 0, -1))],
        "Head": [(4, (2, 0, 0)), (16, (-4, 0, -24)), (26, (-3, 0, -20)), (38, (-2, 0, 18)), (48, (0, 0, 16)), (56, (2, 0, 2))],
        "FootL": [(1, SAT_FEET["FootL"])], "FootR": [(1, SAT_FEET["FootR"])],
    }, periodic=True, floor=None)

    # Hit (0.35 s; slowed for a broken guard or a ducking): jolted back, the head snapped back after the body, the
    # free arm flung out, the oar wobbling in the hand; then caught, and back.
    A["Hit"] = clip(10, keys(held(REST, 1), {
        "Root": [(2.5, ((0, 0, 0), (0, 0.06, -0.025))), (5, ((0, 0, 0), (0, 0.04, -0.04))), (10, REST["Root"])],
        "Pivot": [(2.5, (-16, 2, 4)), (5, (4, 0, 0)), (7.5, (-2, 0, 0)), (10, (0, 0, 0))],
        "Body": [(2.5, (-6, 0, 10)), (5.5, (5, 0, 2)), (10, REST["Body"])],
        "Head": [(2, (2, 0, 0)), (3.5, (-24, 0, -6)), (6, (8, 0, 0)), (8, (-3, 0, 0)), (10, REST["Head"])],
        "OAR": [(3, planted(-6, 60, butt=(0.36, 0.06, 0.05))), (5.5, planted(18, 44, butt=(0.34, -0.06, 0.02))), (8, planted(7, 52)), (10, STAND)],
        "HandL": [(2.5, hand(0.4, 0, -0.3, 0.02, 0.44)), (5.5, hand(0.4, 0, -0.18, -0.16, 0.34)), (10, REST["HandL"])],
        "FootL": [(2, ((0, 0, 0), (0, 0, 0.03))), (4, ((0, 0, 0), (0, 0.08, 0))), (10, ((0, 0, 0), (0, 0.04, 0)))],
    }), motion=[(1, (0, 4, 0)), (6, (0, 1, 0)), (10, (0, 0, 0))])

    # VaultPlant (0.12 s): the oar swung down and its foot driven into the ground far ahead, both hands high on it,
    # crouching onto it.
    A["VaultPlant"] = clip(4, keys(held(REST, 1), held(VAULT_PLANT, 4), {
        "HandL": [(2.5, hand(VAULT_HANDS + 0.12, 0.3, -0.1, -0.25, 0.45))],
        "Root": [(2.5, ((0, 0, 0), (0, 0, -0.08)))], "Pivot": [(2.5, (10, 0, 0))], "Head": [(2.5, (-8, 0, 2))],
    }))

    # Vault (0.55 s; the top at f6.6): pushed up off the oar, its foot staying put in the ground as the Apprentice rises
    # past it (the hands sliding up the shaft as it goes); let go of below and swung up and back behind with the right
    # hand, tipped forward over the top, feet tucked; then down, the oar brought forward over, the feet reaching for the
    # ground.
    def vault(f, p):
        u = (f - 1) / 16
        if u > 0.22:
            return
        h = 1.75 * (lambda t: t * t * (3 - 2 * t))(min(1.0, u / 0.35))
        butt = Vector(VAULT_BUTT) + Vector((0, 4 * u, -h))
        mats = kit.pose_matrices(BONES, {b: p[b] for b in ("Root", "Pivot", "Body") if b in p})
        hands_at = mats["Body"] @ Vector((0.08, -0.24, 0.6))
        d = hands_at - butt
        s = d.length
        pitch = math.degrees(math.acos(max(-1, min(1, d.normalized().z))))
        yaw = math.degrees(math.atan2(d.x, -d.y))
        planted_oar = oar(pitch, yaw, 0, butt, s=0.0)
        w = max(0.0, min(1.0, (u - 0.13) / 0.09))  # (let go of, over the last of it)
        rot = tuple(a + (b - a) * w for a, b in zip(planted_oar[0], p["OAR"][0]))
        grip = tuple(a + (b - a) * w for a, b in zip(planted_oar[1], p["OAR"][1]))
        p["OAR"] = (rot, grip)
        (sr, onr, _), pr = p["HandR"]
        (sl, onl, _), pl = p["HandL"]
        p["HandR"] = ((s - 0.12 + (sr - s + 0.12) * w, onr, 0), pr)
        p["HandL"] = ((s + 0.12 + (sl - s - 0.12) * w, onl, 0), pl)

    A["Vault"] = clip(17, keys(held(VAULT_PLANT, 1), {
        "OAR": [(1, VAULT_PLANT["OAR"]), (4.5, oar(-40, 10, 0, (0.3, 0.06, 0.62), s=0.6)), (7, oar(-96, 10, 0, (0.3, 0.2, 0.55), s=0.6)),
                (11, oar(-70, 14, 0, (0.32, 0.12, 0.5), s=0.6)), (15, oar(-30, 20, 0, (0.33, 0.02, 0.5), s=0.6)), (17, oar(-18, 24, 0, (0.33, -0.02, 0.6), s=0.6))],
        "HandR": [(1, VAULT_PLANT["HandR"]), (4.5, hand(0.6, 1, *FREE_R))],
        "HandL": [(1, VAULT_PLANT["HandL"]), (2.6, hand(VAULT_HANDS + 0.12, 1, -0.1, -0.2, 0.7)), (5, hand(0.4, 0, -0.24, -0.1, 0.58)),
                  (10, hand(0.4, 0, -0.26, -0.02, 0.5)), (16, hand(0.4, 0, -0.24, -0.12, 0.42))],
        "Root": [(3, ((0, 0, 0), (0, 0, 0.02))), (7, ((0, 0, 0), (0, 0, 0.06))), (12, ((0, 0, 0), (0, 0, 0.04))), (17, ((0, 0, 0), (0, 0, 0)))],
        "Pivot": [(3, (24, 0, 0)), (6.6, (40, 0, 0)), (11, (20, 0, 0)), (17, (4, 0, 0))],
        "Body": [(3, (8, 0, 14)), (5, (8, 0, 4)), (7, (10, 0, 0)), (12, (4, 0, 2)), (17, (0, 0, 0))],
        "Head": [(3, (-18, 0, 2)), (7, (-30, 0, 0)), (12, (-16, 0, 0)), (17, (-6, 0, 0))],
        "FootL": [(3, ((30, 0, 0), (0, 0.1, 0.12))), (7, ((40, 0, 0), (0, 0.14, 0.2))), (12, ((10, 0, 0), (0, 0.0, 0.1))), (17, ((-10, 0, 0), (0, -0.06, 0.02)))],
        "FootR": [(2, ((30, 0, 0), (0, 0.12, 0.1))), (7, ((40, 0, 0), (0, 0.14, 0.18))), (12, ((10, 0, 0), (0, 0.02, 0.1))), (17, ((-6, 0, 0), (0, 0.02, 0.01)))],
    }), custom=vault, motion=[(1, (0, -7.3, 0.0)), (4, (0, -7.3, 6.5)), (6.6, (0, -7.3, 0.0)), (12, (0, -7.3, -5.0)), (17, (0, -7.3, -9.0))])

    # VaultLand (0.2 s, straight into whatever's next): down, a dip to take it (the cloak, the hood and the lantern
    # thrown down after), and up.
    A["VaultLand"] = clip(7, keys(held(REST, 7), {
        "Root": [(1, ((0, 0, 0), (0, 0, 0))), (3, ((0, 0, 0), (0, -0.02, -0.11))), (7, REST["Root"])],
        "Pivot": [(1, (4, 0, 0)), (3, (7, 0, 0)), (7, (0, 0, 0))],
        "Body": [(1, (0, 0, 0)), (3.5, (14, 0, 0))],
        "Head": [(1, (-6, 0, 0)), (4, (6, 0, 0))],
        "OAR": [(1, oar(-18, 24, 0, (0.33, -0.02, 0.6), s=0.6)), (3, planted(-4, 30, -20, butt=(0.33, -0.04, 0.03)))],
        "HandR": [(1, hand(0.6, 1, *FREE_R))],
        "HandL": [(1, hand(0.4, 0, -0.24, -0.12, 0.42)), (3, hand(0.4, 0, -0.24, -0.16, 0.34))],
        "FootL": [(1, ((-10, 0, 0), (0, -0.06, 0.02))), (2, ((0, 0, 0), (0, -0.06, 0))), (6, ((0, 0, 0), (0, 0, 0)))],
        "FootR": [(1, ((-6, 0, 0), (0, 0.02, 0.01))), (2, ((0, 0, 0), (0, 0.02, 0)))],
    }), motion=[(1, (0, -3.3, -7)), (2.5, (0, -1, 0)), (7, (0, 0, 0))])

    return A


def stroke_at(phi, pitch, lift, reach=0.0):
    """The Stroke's sweep at `phi`: low, the hands near the floor, the blade turned to push water."""
    a = math.radians(max(-46.0, min(46.0, 0.6 * phi)))
    r = 0.3 + reach
    return oar(pitch, phi, 90, (r * math.sin(a), -r * math.cos(a), 0.5 + lift), s=0.46)


def build():
    cloak, cloak_dark = kit.mat("Cloak", "#9c3b2b"), kit.mat("CloakDark", "#74291f")
    mask, ink, mark = kit.mat("Mask", "#ece4d0"), kit.mat("Ink", "#1a1622"), kit.mat("MaskMark", "#b8352b")
    rng = kit.rng

    def cloak_body(bm):
        kit.cone(10, 0.40, 0.13, 0.72)(bm)
        for v in bm.verts:
            if v.co.z < -0.3:  # ragged hem
                s = 1 + rng.uniform(-0.06, 0.06)
                v.co.x *= s
                v.co.y *= s
                v.co.z += rng.uniform(-0.025, 0.02)
        # (rings up its height, so the skirt can bend from the body)
        sides = [e for e in bm.edges if abs(e.verts[0].co.z - e.verts[1].co.z) > 0.5]
        bmesh.ops.subdivide_edges(bm, edges=sides, cuts=5, use_grid_fill=True)

    # The cloak, skinned smoothly: the body above, the skirt below (it swings after the body, and bunches on the floor).
    body = part("Cloak", cloak_body, cloak, M((0, 0, 0.36)), smooth=True)
    upper, skirt = body.vertex_groups.new(name="Body"), body.vertex_groups.new(name="Hem")
    for vert in body.data.vertices:
        t = max(0.0, min(1.0, (0.55 - vert.co.z) / 0.45))
        w = t * t * (3 - 2 * t)
        if w < 1:
            upper.add([vert.index], 1 - w, "REPLACE")
        if w > 0:
            skirt.add([vert.index], w, "REPLACE")
    parts = [
        body,
        part("Hood", kit.sphere(10, 7, 0.25), cloak, M((0, 0.02, 0.84), scale=(1, 1, 1.05)), smooth=True, group="Head"),
        part("HoodTip", kit.cone(8, 0.11, 0.0, 0.34), cloak, M((0, 0.14, 1.03), rot=(-55, 0, 0)), smooth=True, group="HoodTip"),
        part("Mask", kit.sphere(12, 8), mask, M((0, -0.2, 0.83), scale=(0.17, 0.06, 0.21)), smooth=True, group="Head"),
        part("Mark", kit.cube(), mark, M((0, -0.262, 0.765), scale=(0.018, 0.012, 0.07)), group="Head"),
    ]
    # The arms: a sleeve to the elbow (a knob of cloak at the shoulder and the elbow hides the joints), a forearm, and
    # the dark hand (gloved) that holds the oar.
    for arm, fore, shoulder, elbow, wrist in (("ArmL", "ForeL", SHOULDER_L, ELBOW_L, WRIST_L), ("ArmR", "ForeR", SHOULDER_R, ELBOW_R, WRIST_R)):
        parts += [
            part("Shoulder", kit.sphere(8, 6, 0.062), cloak, M(shoulder), smooth=True, group=arm),
            part("Sleeve", kit.cone(7, 0.06, 0.048, math.dist(shoulder, elbow) + 0.03), cloak, kit.along(shoulder, elbow), smooth=True, group=arm),
            part("Elbow", kit.sphere(8, 6, 0.05), cloak, M(elbow), smooth=True, group=fore),
            part("Forearm", kit.cone(7, 0.048, 0.04, math.dist(elbow, wrist)), cloak, kit.along(elbow, wrist), smooth=True, group=fore),
            part("Hand", kit.sphere(8, 6, 0.058), cloak_dark, M(wrist), smooth=True, group=fore),
        ]
    # The oar at each of its five steps, all carried; the game shows the one it's at (tools/blender/oars.py, OarLook.cs).
    for step in range(5):
        parts += oars.build(step, OAR, group="Oar")
    # The bow lantern, carried at the left hip on land (it hangs on the boat's bow when rowing). Cold, pale light.
    metal, soul = kit.mat("LanternMetal", "#2c2b31"), kit.mat("SoulLantern_Glow", "#cfe6ff")
    lx, ly, lz = -0.36, -0.06, 0.38
    parts += [
        part("LanternCord", kit.cube(), metal, M((lx + 0.02, ly, lz + 0.14), rot=(0, 18, 0), scale=(0.015, 0.015, 0.12)), group="Lantern"),
        part("LanternTop", kit.cone(4, 0.07, 0.01, 0.06), metal, M((lx, ly, lz + 0.08), rot=(0, 0, 45)), group="Lantern"),
        part("LanternGlass", kit.cube(), soul, M((lx, ly, lz), scale=(0.08, 0.08, 0.11)), group="Lantern"),
        part("LanternBase", kit.cube(), metal, M((lx, ly, lz - 0.065), scale=(0.1, 0.1, 0.02)), group="Lantern"),
    ]
    # The Hook (the Ferryman's, taken from his relic): a small iron grappling hook with a few links of chain, hung at the
    # back of the hip. Same dark iron as the relic and the hook posts' rings.
    iron = kit.mat("HookIron", "#3b3a42")
    hx, hy, hz, k = 0.22, 0.16, 0.5, 1.5  # k: sized up to read from the game's camera

    def at(dx, dy, dz):
        return (hx + dx * k, hy + dy * k, hz + dz * k)

    parts += [
        part("HookShank", kit.cylinder(5, 0.024 * k, 0.24 * k), iron, M(at(0, 0, 0.08)), group="Hook"),
        part("HookProngL", kit.tube([at(0, 0, -0.04), at(-0.07, 0, -0.06), at(-0.1, 0, 0.0)], [0.022 * k, 0.017 * k, 0.01 * k], seg=5), iron, group="Hook"),
        part("HookProngR", kit.tube([at(0, 0, -0.04), at(0.07, 0, -0.06), at(0.1, 0, 0.0)], [0.022 * k, 0.017 * k, 0.01 * k], seg=5), iron, group="Hook"),
        part("HookProngB", kit.tube([at(0, 0, -0.04), at(0, 0.07, -0.06), at(0, 0.1, 0.0)], [0.022 * k, 0.017 * k, 0.01 * k], seg=5), iron, group="Hook"),
        part("HookEye", kit.cylinder(6, 0.032 * k, 0.02 * k), iron, M(at(0, 0, 0.21), rot=(90, 0, 0)), group="Hook"),
    ]
    for i in range(3):
        parts.append(part("HookLink", kit.cylinder(5, 0.024 * k, 0.013 * k), iron, M(at(0, 0, 0.25 + 0.04 * i), rot=(90 * (i % 2), 0, 90 * (i % 2))), group="Hook"))
    for s, foot in ((-1, "FootL"), (1, "FootR")):
        parts.append(part("Eye", kit.sphere(8, 6), ink, M((s * 0.065, -0.255, 0.87), scale=(0.032, 0.02, 0.046)), smooth=True, group="Head"))
        parts.append(part("Foot", kit.sphere(8, 6), ink, M((s * 0.11, -0.17, 0.035), scale=(0.075, 0.09, 0.045)), smooth=True, group=foot))

    mesh = kit.join(parts, "Apprentice")
    armature = kit.rig("ApprenticeRig", BONES)
    kit.skin(mesh, armature)
    actions = make_actions()
    kit.animate(armature, actions)
    for (name, _), (worst, side, frame) in zip(actions.items(), REACH.values()):
        if worst > 0.02:
            print(f"[apprentice] {name}: the hands come off what they hold by up to {worst * 100:.0f} cm ({side}, f{frame})")
    return [armature, mesh]
