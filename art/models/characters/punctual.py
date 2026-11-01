"""A Punctual (the Hall of the Last Hour): one of the household, still keeping its hours. A footman in a faded claret
livery coat with long tails, a cream waistcoat, white gloves, a high collar; stiffly upright, its head bowed; its eyes
the Soured's green. Where its heart was, a pocket watch has grown into its chest: a brass case, a pale face, one black
hand (on its own bone, WatchHand, which the game turns a tick each beat, and spins when it's lost count). In its right
hand a great brass winding key, held like a cudgel. And, like every Soured, a small green lantern at its belt.

Clips (30 fps; the Hall's beat is a second, 30 frames): Idle (at attention, head bowed), Walk (2 s: a stiff march, one
step a beat), Windup (1.85 s: the Count: the key raised overhead, a jolt on the beat between), Strike (0.3 s: brought
straight down), Recover (1.2 s), Warn (0.85 s: the Quarters' tell, the key drawn back to its right), Quarters (3.3 s:
four blows a beat apart: forehand, backhand, forehand, then overhead), QuartersRecover (1.4 s), Lost (2 s loop: head
cocked, listening for the beat), Stagger, Death (it stops, then topples stiffly forward).

How it's animated (the Laden's way, kit.bake: each bone on its own timing): it moves like clockwork, in ticks: a move
snaps to its place (the body first, the head and the key a frame or two after), overshoots a hair, settles, and holds,
dead still but for its coat tails and lantern swinging on after it. Its blows land on the beat (Punctual.cs times them:
see each clip). The key is keyed where it is and the right arm bent onto it every frame (kit.reach), and the left hand
too when it takes the key in both hands; the feet are keyed where they stand and the legs bent onto them (so it never
sinks or slides through the floor, and a lowered body bends its knees). The coat tails and the lantern hang: they keep
to the floor's down when the body bows, lagging and swinging after it.
"""
import math

from mathutils import Euler, Quaternion, Vector

M, part = kit.M, kit.part  # `kit` is injected by build_model.py

SHOULDER = 1.5
HIP = 0.95
WATCH = (0, -0.205, 1.27)
L, R = -1, 1
SH = {s: (s * 0.24, 0, SHOULDER - 0.05) for s in (L, R)}
EL = {s: (s * 0.255, -0.01, 1.19) for s in (L, R)}
HA = {s: (s * 0.27, -0.02, 0.93) for s in (L, R)}
HP = {s: (s * 0.11, 0, HIP) for s in (L, R)}
KN = {s: (s * 0.11, -0.015, 0.52) for s in (L, R)}
AN = {s: (s * 0.11, 0, 0.1) for s in (L, R)}
KEY_TIP = (0.27, -0.3, 0.2)
SIDE = {L: "L", R: "R"}
BONES = {
    "Root": ((0, 0, 0), (0, 0, 0.3), None),
    "Hips": ((0, 0, HIP), (0, 0, HIP - 0.15), "Root"),        # the pelvis: the legs hang from it, the body stands on it
    "Spine": ((0, 0, HIP), (0, 0, SHOULDER), "Hips"),        # the whole stiff torso: it bows from the waist
    "Head": ((0, 0, SHOULDER), (0, -0.02, 1.86), "Spine"),
    "ArmL": (SH[L], EL[L], "Spine"),
    "ForeL": (EL[L], HA[L], "ArmL"),
    "ArmR": (SH[R], EL[R], "Spine"),
    "ForeR": (EL[R], HA[R], "ArmR"),
    "Key": (HA[R], KEY_TIP, "Root"),                          # keyed where it is: the right hand follows it
    "LegL": (HP[L], KN[L], "Hips"),
    "ShinL": (KN[L], AN[L], "LegL"),
    "FootL": (AN[L], (-0.11, -0.17, 0.05), "Root"),           # keyed where it stands: the leg follows it
    "LegR": (HP[R], KN[R], "Hips"),
    "ShinR": (KN[R], AN[R], "LegR"),
    "FootR": (AN[R], (0.11, -0.17, 0.05), "Root"),
    "TailL": ((-0.09, 0.12, 0.99), (-0.09, 0.19, 0.42), "Spine"),  # the coat's tails, hanging behind
    "TailR": ((0.09, 0.12, 0.99), (0.09, 0.19, 0.42), "Spine"),
    "Lantern": ((-0.25, 0.02, 0.955), (-0.25, 0.02, 0.79), "Spine"),  # on its belt, swinging
    "WatchHand": (WATCH, (0, -0.3, WATCH[2]), "Spine"),       # (never keyed: the game turns it)
}


def rad(v):
    return [math.radians(a) for a in v]


def deg(e):
    return tuple(math.degrees(a) for a in e)


# ---------------------------------------------------------------- what's keyed where it is

# The key, by where its bow is (the right hand's grip) and which way it points: `elev` its angle above the horizontal
# (past 90 it points back over the head), `yaw` turned about the vertical (+ towards its right, +X), `tilt` rolled
# about the forward axis.
KEY_DIR = (Vector(KEY_TIP) - Vector(HA[R])).normalized()
KEY_ELEV0 = math.degrees(math.atan2(KEY_DIR.z, -KEY_DIR.y))
KEY_LEFT = Vector(HA[R]) - KEY_DIR * 0.11  # (where the left hand takes it: on the bow, beyond the right hand)


def key(at, elev, yaw=0.0, tilt=0.0):
    return ((-(elev - KEY_ELEV0), tilt, yaw), tuple(Vector(at) - Vector(HA[R])))


# Points on the key (its bow, shaft, bit) to find its lowest when it lies on the floor.
KEY_POINTS = ([Vector((0.27, -0.06 + 0.112 * math.sin(a), 0.93 + 0.112 * math.cos(a))) for a in (2 * math.pi * k / 16 for k in range(16))]
              + [Vector((0.27 + dx, -0.06 + 0.09 * math.sin(a), 0.93 + 0.09 * math.cos(a))) for dx in (-0.022, 0.022) for a in (2 * math.pi * k / 8 for k in range(8))]
              + [Vector((0.27 + dx, -0.06 - 0.24 * f + dy, 0.88 - 0.68 * f)) for dx in (-0.03, 0.03) for dy in (-0.03, 0.03) for f in (0.0, 0.5, 1.0)]
              + [Vector((0.27 + dx, -0.3 + dy, 0.2 + dz)) for dx in (-0.01, 0.11) for dy in (-0.03, 0.03) for dz in (0.0, 0.1)])


def key_lying(rot, x, y, bottom=0.0):
    """The key dropped: turned `rot`, its bow over (x, y), its lowest point `bottom` above the floor."""
    turn = Euler(rad(rot)).to_matrix()
    head = Vector(HA[R])
    low = min((turn @ (p - head) + head).z for p in KEY_POINTS)
    return (tuple(rot), (x - head.x, y - head.y, round(bottom - low, 4)))


# A foot, by where its ankle stands (y, and dx aside from its own place), its sole's lowest point `lift` above the
# floor, pitched (+ toe down) and yawed (+ toe towards +X). `toe=` / `heel=`: placed so that end of the sole is there.
SHOE = [Vector((x, y, z)) for x in (-0.055, 0.055) for y in (-0.175, 0.075) for z in (-0.1, 0.0)]


def foot(y=0.0, lift=0.0, pitch=0.0, yaw=0.0, dx=0.0, toe=None, heel=None):
    turn = Euler(rad((pitch, 0, yaw))).to_matrix()
    low = min((turn @ c).z for c in SHOE)
    if toe is not None:
        y = toe - (turn @ Vector((0, -0.175, -0.1))).y
    if heel is not None:
        y = heel - (turn @ Vector((0, 0.075, -0.1))).y
    return ((pitch, 0, yaw), (dx, round(y, 4), round(lift - 0.1 - low, 4)))


def shoulder(body, s=R):
    return kit.pose_matrices(BONES, body)["Spine"] @ Vector(SH[s])


def out(body, az, el, r, s=R):
    """A point `r` from the shoulder in `body`: `az` round from straight ahead (+ towards +X), `el` up."""
    a, e = math.radians(az), math.radians(el)
    return tuple(shoulder(body, s) + Vector((math.sin(a) * math.cos(e), -math.cos(a) * math.cos(e), math.sin(e))) * r)


# ---------------------------------------------------------------- solving a pose

def upright(q):
    """Only the turn about the vertical of a rotation (what hangs keeps that, and the floor's down)."""
    t = Quaternion((q.w, 0.0, 0.0, q.z))
    return t.normalized() if t.magnitude > 1e-6 else Quaternion()


def hang(pose, bone, parent, keep):
    """`bone` hangs: its keyed rotation is its swing about the floor's down; it keeps `keep` of its parent's tilt."""
    m = kit.pose_matrices(BONES, pose)
    pq = m[parent].to_quaternion()
    swing = Euler(rad(pose[bone][0])).to_quaternion()
    world = upright(pq).slerp(pq, keep) @ swing
    pose[bone] = (deg((pq.inverted() @ world).to_euler("XYZ")), (0, 0, 0))


def arm(pose, s, goal_fn, w, pole):
    """The arm on side `s` bent to `goal_fn(matrices)` (weight `w` of the way from where its own keys put the hand)."""
    upper, lower = "Arm" + SIDE[s], "Fore" + SIDE[s]
    if w <= 0.0:
        return pose
    m = kit.pose_matrices(BONES, pose)
    sh = m["Spine"] @ Vector(SH[s])
    fk_hand = m[lower] @ Vector(HA[s])
    fk_elbow = m[upper] @ Vector(EL[s])
    goal = fk_hand.lerp(goal_fn(m), min(w, 1.0))
    fk_pole = fk_elbow - (sh + fk_hand) / 2
    p = Vector(pole).normalized()
    if fk_pole.length > 1e-4:
        p = fk_pole.normalized().lerp(p, min(w, 1.0))
    return kit.reach(BONES, pose, upper, lower, lambda _: goal, p)


def value(pose, name, default):
    v = pose.get(name)
    return Vector(v[0]) if v is not None else Vector(default)


def solve(pose):
    """The tails and lantern hung, the legs bent onto the feet, the hands onto the key."""
    pose = dict(pose)
    hang(pose, "TailL", "Spine", 0.55)
    hang(pose, "TailR", "Spine", 0.55)
    hang(pose, "Lantern", "Spine", 0.0)
    m = kit.pose_matrices(BONES, pose)
    hips_fwd = m["Hips"].to_3x3() @ Vector((0, -1, 0))
    for s in (L, R):
        leg, shin, ft = "Leg" + SIDE[s], "Shin" + SIDE[s], "Foot" + SIDE[s]
        toe_fwd = m[ft].to_3x3() @ Vector((0, -1, 0))
        pole = (hips_fwd + toe_fwd).normalized() + Vector((s * 0.12, 0, 0))
        pose = kit.reach(BONES, pose, leg, shin, lambda mm, ft=ft, s=s: mm[ft] @ Vector(AN[s]), pole)
    hold = value(pose, "_holdR", (1, 0, 0)).x
    pose = arm(pose, R, lambda mm: mm["Key"] @ Vector(HA[R]), hold, value(pose, "_poleR", (0.3, 1, -0.1)))
    grip = value(pose, "_gripL", (0, 0, 0))
    if grip.x > 0:
        pose = arm(pose, L, lambda mm: mm["Key"] @ KEY_LEFT, grip.x, value(pose, "_poleL", (-0.3, 1, -0.1)))
    elif grip.y > 0:
        at = value(pose, "_handL", (0, 0, 0))
        pose = arm(pose, L, lambda mm: at, grip.y, value(pose, "_poleL", (-0.3, 1, -0.1)))
    return pose


REPORT = {}  # (the build's report: hands off the key, feet the legs can't reach, wrists bent too far)


def clip(name, length, tracks, periodic=False):
    """A clip `length` frames long from per-bone tracks, baked to every frame and solved."""
    frames = []
    hand = foot_off = wrist = 0.0
    for f, p in kit.bake(tracks, length, periodic):
        p = solve(p)
        m = kit.pose_matrices(BONES, p)
        if value(p, "_holdR", (1, 0, 0)).x >= 0.999:
            hand = max(hand, (m["ForeR"] @ Vector(HA[R]) - m["Key"] @ Vector(HA[R])).length)
            fore = (m["ForeR"] @ Vector(HA[R]) - m["ForeR"] @ Vector(EL[R])).normalized()
            wrist = max(wrist, math.degrees(fore.angle(m["Key"].to_3x3() @ KEY_DIR)))
        if value(p, "_gripL", (0, 0, 0)).x >= 0.999:
            hand = max(hand, (m["ForeL"] @ Vector(HA[L]) - m["Key"] @ KEY_LEFT).length)
        for s in (L, R):
            foot_off = max(foot_off, (m["Shin" + SIDE[s]] @ Vector(AN[s]) - m["Foot" + SIDE[s]] @ Vector(AN[s])).length)
        frames.append((f, {b: v for b, v in p.items() if not b.startswith("_")}))
    REPORT[name] = (hand, foot_off, wrist)
    return ("LINEAR", frames)


def held(pose, frame):
    """A whole pose as a key at `frame` for every bone (to start or end a clip on it)."""
    return {bone: [(frame, v)] for bone, v in pose.items()}


def keys(*parts):
    """Tracks merged: each a {bone: [(frame, value)]}."""
    merged = {}
    for p in parts:
        for bone, ks in p.items():
            merged.setdefault(bone, {}).update(dict(ks))  # (a later key at the same frame replaces an earlier)
    return {bone: sorted(ks.items()) for bone, ks in merged.items()}


def mix(a, b, t):
    """Between two keyed values (rotation and offset) by `t` (past 1: overshooting b)."""
    return tuple(tuple(x + (y - x) * t for x, y in zip(u, v)) for u, v in zip(a, b))


def with_(pose, **changes):
    merged = dict(pose)
    merged.update(changes)
    return merged


# ---------------------------------------------------------------- poses

# At attention: heels together, toes out, stiffly upright, the head bowed; the key held point-down at its side like a
# cane; the left arm straight at its side.
ATTN = {
    "Root": (0, 0, 0), "Hips": ((0, 0, 0), (0, 0, -0.006)), "Spine": (-1, 0, 0), "Head": (15, 0, 0),
    "ArmL": (-2, 3, 0), "ForeL": (-8, 0, 0), "ArmR": (0, 0, 0), "ForeR": (0, 0, 0),
    "FootL": foot(yaw=-8), "FootR": foot(yaw=8), "TailL": (0, 0, 0), "TailR": (0, 0, 0), "Lantern": (0, 0, 0),
    "_poleR": (0.3, 1, -0.1), "_poleL": (-0.3, 1, -0.1), "_holdR": (1, 0, 0), "_gripL": (0, 0, 0), "_handL": (0, 0, 0),
}
ATTN["Key"] = key((0.28, -0.05, 0.945), -76)


def body(base=ATTN, **bones):
    return with_(base, **bones)


# The Count's key overhead in both hands (raised on the beat; a notch further back on the next; then strained).
def raised(elev, lean, rise, grip_el=60, az=-55, r=0.46):
    b = body(Hips=((0, 0, 0), (0, 0.03, -0.035 + rise)), Spine=(lean, 0, 0), Head=(4, 0, 0),
             FootL=foot(-0.2, yaw=-6, dx=-0.02), FootR=foot(0.12, yaw=10, dx=0.02),
             _poleR=(1, -0.3, 0.3), _poleL=(-1, -0.3, 0.3), _gripL=(1, 0, 0))
    b["Key"] = key(out(b, az, grip_el, r), elev, yaw=-4)
    return b


RAISED = raised(118, -6, 0.0)
JOLTED = raised(134, -9, 0.0)
RAISED_HIGH = raised(146, -12, -0.012, grip_el=64)
RAISED_HIGH["Head"] = (0, 0, 0)
RAISED_HIGH["FootR"] = foot(heel=0.12 + 0.075, pitch=16, yaw=10, dx=0.02)  # (up on its back toes, straining)


def bowed(spine, hips_y, hips_z, elev, az=-40, el=-46, r=0.48, lead=-0.48, back=0.3):
    b = body(Hips=((0, 0, 0), (0, hips_y, hips_z)), Spine=(spine, 0, 0), Head=(22, 0, 0),
             FootL=foot(lead, yaw=-6, dx=-0.03), FootR=foot(heel=back + 0.075, pitch=24, yaw=12, dx=0.03),
             _poleR=(1, 0.3, -0.6), _poleL=(-1, 0.3, -0.6), _gripL=(1, 0, 0))
    b["Key"] = key(out(b, az, el, r), elev, yaw=-6)
    return b


SMASH = bowed(32, -0.14, -0.12, -58)          # the Count's blow: bowed rigidly from the waist, the key down before it

# ---------------------------------------------------------------- Walk

H = 0.32  # half a stride: a step is 0.64 m


def ease(t):
    """How far the body has gone through a beat at `t` (the code moves it as sin^2: still on the tick)."""
    return t - math.sin(2 * math.pi * t) / (2 * math.pi)


def walk_tracks():
    """Two beats, a step each: ticks (both feet down, standing) at frames 16 and 46, upright passing poses at 1 and 31."""
    tracks = {}

    def add(bone, frame, v):
        tracks.setdefault(bone, []).append((((frame - 1) % 60) + 1, v))

    for s, plant in ((L, 16), (R, 46)):
        ft, side = "Foot" + SIDE[s], s
        yaw = s * 6
        # On the floor from its plant to the next tick, carried back under it as the body goes on (as the game moves
        # it: through the middle of the beat, standing on the tick).
        for k in range(6):
            t = k / 5
            add(ft, plant + 6 * k, foot(-H + 2 * H * ease(t), yaw=yaw))
        # Then the swing: the heel peels (the toe still), off, the knee lifted high past the other (a march), the leg
        # thrown straight out, the heel down a frame before the tick and the sole slapped flat on it.
        lift = plant + 30
        add(ft, lift + 3, foot(toe=H - 0.175, pitch=22, yaw=yaw))
        add(ft, lift + 7, foot(H - 0.1, lift=0.04, pitch=34, yaw=yaw))
        add(ft, lift + 11, foot(0.12, lift=0.12, pitch=16, yaw=yaw))
        add(ft, lift + 15, foot(-0.02, lift=0.17, pitch=0, yaw=yaw))
        add(ft, lift + 20, foot(-H + 0.02, lift=0.1, pitch=-18, yaw=yaw))
        add(ft, lift + 24, foot(-H - 0.01, lift=0.03, pitch=-22, yaw=yaw))
        add(ft, lift + 28, foot(heel=-H + 0.075, pitch=-12, yaw=yaw))
        # The body over it: dropped onto the plant (lowest just after, a rebound), up again through the passing pose;
        # rolling onto the leg it stands on, turning with the leg thrown forward, the shoulders the other way after.
        add("Hips", plant, ((0, 0, -s * 5), (0, 0, -0.078)))
        add("Hips", plant + 3, ((0, -s * 1, -s * 5), (s * 0.006, 0, -0.088)))
        add("Hips", plant + 7, ((0, -s * 2, -s * 3), (s * 0.014, 0, -0.07)))
        add("Hips", plant + 11, ((0, -s * 3, -s * 1), (s * 0.02, 0, -0.035)))
        add("Hips", plant + 15, ((0, -s * 3, s * 0.5), (s * 0.022, 0, -0.006)))
        add("Spine", plant + 2, (3, s * 1, s * 8))
        add("Spine", plant + 5, (5, s * 1.5, s * 8))
        add("Spine", plant + 9, (3, s * 1, s * 6))
        add("Spine", plant + 17, (2, -s * 0.5, s * 0))
        add("Head", plant + 3, (11, -s * 2, -s * 3))
        add("Head", plant + 6, (15, -s * 2, -s * 3))
        add("Head", plant + 10, (12, -s * 1, -s * 2))
        add("Head", plant + 18, (10, s * 1, 0))
        # The arms swing stiffly with the opposite legs, reach their ends on the tick and hold there a moment; the key
        # in the right hand swings less (it's heavy) and its point trails.
        if s == L:  # (the left leg planted forward: the left arm back)
            add("ArmL", plant, (18, 4, 0))
            add("ArmL", plant + 5, (18, 4, 0))
            add("ForeL", plant + 2, (-2, 0, 0))
            add("ForeL", plant + 7, (-6, 0, 0))
            add("Key", plant + 1, key((0.29, -0.15, 0.96), -72))
            add("Key", plant + 5, key((0.29, -0.14, 0.96), -68))
            add("_poleR", plant, (0.3, 1, -0.3))
        else:       # (the right leg forward: the left arm forward, the key back)
            add("ArmL", plant, (-30, 4, 0))
            add("ArmL", plant + 5, (-30, 4, 0))
            add("ForeL", plant + 2, (-22, 0, 0))
            add("ForeL", plant + 7, (-16, 0, 0))
            add("Key", plant + 1, key((0.29, 0.05, 0.94), -82, yaw=0))
            add("Key", plant + 5, key((0.29, 0.04, 0.94), -88))
            add("_poleR", plant, (0.3, 1, 0.1))
        add("Tail" + SIDE[s], plant + 4, (14, 0, 0))
        add("Tail" + SIDE[s], plant + 19, (-4, 0, 0))
        add("Tail" + SIDE[-s], plant + 4, (-2, 0, s * 3))
        add("Tail" + SIDE[-s], plant + 12, (6, 0, 0))
        add("Lantern", plant + 5, (s * 10, 0, 0))
        add("Lantern", plant + 13, (-s * 4, s * 3, 0))
    return tracks


# ---------------------------------------------------------------- the Quarters

GLIDE = 3.5 / 8.0  # (the shove into each blow: 3.5 m/s, dying away at e^-8t: 44 cm, most of it in a fifth of a second)


def glide(after):
    """How far the shove has carried it `after` frames past the blow."""
    return GLIDE * (1 - math.exp(-8 * max(after, 0.0) / 30))


LAND, LEAD = -0.4, -0.4 + GLIDE - glide(3.5)  # (where a stepping foot lands, and where the glide leaves it)


def quarters_pose(kind, phase):
    """The Quarters' poses: for each kind of blow (fore, back, over) where it's drawn, where it lands (on the beat:
    the key crossing in front), where it's carried through to, and where it settles."""
    if kind == "fore":
        hz, sz, sx, lean, az, el, r, yaw, elev, arml, forel, pole = {
            "draw": (14, 24, 2, 0, 112, -6, 0.42, 120, 14, (-55, -8, 0), (-30, 0, 0), (0.1, 0.7, -1)),
            "hit": (-4, -8, 9, 0, 8, -14, 0.5, -16, -4, (-30, 6, 0), (-14, 0, 0), (0.1, 0.4, -1)),
            "through": (-16, -26, 8, 0, -72, -12, 0.46, -98, 0, (24, 30, 0), (-10, 0, 0), (0.0, 0.3, -1)),
            "settle": (-14, -24, 6, 0, -68, -10, 0.45, -92, 2, (16, 22, 0), (-14, 0, 0), (0.0, 0.3, -1)),
        }[phase]
    elif kind == "back":
        hz, sz, sx, lean, az, el, r, yaw, elev, arml, forel, pole = {
            "draw": (-16, -24, 4, 0, -62, 4, 0.38, -128, 18, (14, 26, 0), (-40, 0, 0), (0.4, -0.6, 0.7)),
            "hit": (4, 8, 9, 0, 18, -10, 0.5, 18, 0, (-6, 18, 0), (-20, 0, 0), (0.6, 0.2, -0.8)),
            "through": (16, 26, 7, 0, 96, -4, 0.5, 106, 6, (-40, -10, 0), (-24, 0, 0), (0.2, 0.6, -1)),
            "settle": (14, 24, 5, 0, 92, -4, 0.48, 100, 8, (-34, -6, 0), (-26, 0, 0), (0.2, 0.6, -1)),
        }[phase]
    if kind == "over":
        b = {"draw": raised(140, -10, 0.0), "hit": bowed(30, -0.1, -0.12, -76, el=-56, r=0.46, lead=LAND),
             "through": bowed(34, -0.11, -0.135, -80, el=-60, r=0.46, lead=LAND),
             "settle": bowed(33, -0.11, -0.13, -78, el=-58, r=0.46, lead=LAND)}[phase]
        return {k: b[k] for k in ("Spine", "Head", "Key", "_poleR", "_poleL", "_gripL", "ArmL", "ForeL")} | {"HipsRot": (0, 0, 0)}
    b = body(Hips=((0, 0, hz), (0, 0, -0.05)), Spine=(sx, 0, sz), Head=(12, 0, -(hz + sz) * 0.85),
             ArmL=arml, ForeL=forel, _poleR=pole)
    b["Key"] = key(out(b, az, el, r), elev, yaw=yaw)
    return {k: b[k] for k in ("Spine", "Head", "Key", "_poleR", "ArmL", "ForeL")} | {"HipsRot": (0, 0, hz), "_gripL": (0, 0, 0)}


BLOWS = ("fore", "back", "fore", "over")


def quarters_tracks():
    """Four blows, landing at frames 6, 36, 66, 96 (the code: the first 0.15 s in, then a beat apart)."""
    t = {}

    def add(bone, frame, v):
        t.setdefault(bone, []).append((frame, v))

    def hips(frame, rot, y, z):
        add("Hips", frame, ((0, 0, rot), (0, y, z)))

    # Feet: each blow, the back foot steps through (lifted six frames before, landing three after) while the other
    # stands; as the shove glides the body on (0.44 m), the standing foot stays where it is on the floor (so here it's
    # carried back under it), and the landed one too, once it's down.
    stance = {R: LEAD, L: LEAD + GLIDE}  # (as the Warn leaves them: the right foot forward, the left behind)
    stepping = L
    for k, kind in enumerate(BLOWS):
        b = 6 + 30 * k
        hit = b - 0.5  # (the blow itself: 5.5 frames in, then a beat apart)
        st, sw = -stepping, stepping
        yaw_st, yaw_sw = st * 8, sw * 8
        y0 = stance[st]
        for f in (b - 6, b - 1):
            add("Foot" + SIDE[st], f, foot(y0, yaw=yaw_st, dx=st * 0.03))
        for f in (b, b + 1, b + 2, b + 3, b + 5, b + 8, b + 12, b + 18):
            add("Foot" + SIDE[st], f, foot(y0 + glide(f - hit), yaw=yaw_st, dx=st * 0.03))
        y_back = stance[sw]
        ft = "Foot" + SIDE[sw]
        if k > 0:
            add(ft, b - 8, foot(y_back, yaw=yaw_sw, dx=sw * 0.03))
        add(ft, b - 5, foot(toe=y_back - 0.175, pitch=24, yaw=yaw_sw, dx=sw * 0.03))
        add(ft, b - 2, foot(0.0, lift=0.1, pitch=8, yaw=yaw_sw, dx=sw * 0.03))
        add(ft, b, foot(LAND + 0.08, lift=0.07, pitch=-14, yaw=yaw_sw, dx=sw * 0.04))
        add(ft, b + 2, foot(heel=LAND + 0.075, pitch=-14, yaw=yaw_sw, dx=sw * 0.04))
        for f in (b + 3, b + 5, b + 8, b + 12, b + 18):
            add(ft, f, foot(LAND + glide(f - hit) - glide(b + 3 - hit), yaw=yaw_sw, dx=sw * 0.04))
        stance = {st: y0 + GLIDE, sw: LEAD}
        stepping = st

        # The body: it rocks forward onto the standing foot as the other lifts, unwinds into the blow (the hips first,
        # then the chest, the key last and fastest), drops onto the landing foot (deeper than it rests: the weight),
        # and holds there, square on its stance, till the next.
        draw, hitp, through, settle = (quarters_pose(kind, p) for p in ("draw", "hit", "through", "settle"))
        low = -0.12 if kind == "over" else -0.085
        hips(b - 4, draw["HipsRot"][2] * 1.05, -0.08, -0.035)
        hips(b - 1, hitp["HipsRot"][2], -0.1, -0.04)
        hips(b + 1, through["HipsRot"][2], -0.08, -0.055)
        hips(b + 4, through["HipsRot"][2] * 1.05, -0.05, low)
        hips(b + 8, settle["HipsRot"][2], -0.02, low + 0.025)
        hips(b + 13, settle["HipsRot"][2], 0.0, low + 0.015)
        add("Spine", b - 3, draw["Spine"])
        add("Spine", b, hitp["Spine"])
        add("Spine", b + 2, through["Spine"])
        add("Spine", b + 5, tuple(a + (c - a) * 1.1 for a, c in zip(draw["Spine"], through["Spine"])) if kind != "over" else through["Spine"])
        add("Spine", b + 9, settle["Spine"])
        add("Key", b - 3, draw["Key"])
        if kind == "over":
            mid = body(Hips=((0, 0, 0), (0, -0.06, -0.08)), Spine=(12, 0, 0))
            add("Key", b - 2, mix(draw["Key"], hitp["Key"], 0.1))
            add("Key", b - 1, key(out(mid, -45, 32, 0.47), 40, yaw=-5))
        else:
            add("Key", b - 2, mix(draw["Key"], hitp["Key"], 0.14))
            add("Key", b - 1, mix(draw["Key"], hitp["Key"], 0.5))
        add("Key", b, hitp["Key"])
        add("Key", b + 2, through["Key"])
        add("Key", b + 4, mix(hitp["Key"], through["Key"], 0.88))
        add("Key", b + 7, settle["Key"])
        add("_poleR", b - 3, draw["_poleR"])
        add("_poleR", b, hitp["_poleR"])
        add("_poleR", b + 2, through["_poleR"])
        if kind == "over":
            add("_poleL", b - 3, draw["_poleL"])
            add("_poleL", b, hitp["_poleL"])
        add("_gripL", b - 3, draw["_gripL"])
        add("_gripL", b + 6, settle["_gripL"])
        add("Head", b - 2, draw["Head"])
        add("Head", b + 1, hitp["Head"])
        add("Head", b + 3, tuple(h + (2 if i == 0 else 0) for i, h in enumerate(through["Head"])))
        add("Head", b + 7, settle["Head"])
        add("ArmL", b - 1, draw["ArmL"])
        add("ArmL", b + 3, through["ArmL"])
        add("ArmL", b + 8, settle["ArmL"])
        add("ForeL", b, draw["ForeL"])
        add("ForeL", b + 5, through["ForeL"])
        add("ForeL", b + 10, settle["ForeL"])
        # The coat tails and lantern flung after the turn and the drop, and swinging back.
        turn = 1 if (through["HipsRot"][2] > 0) else -1
        for side in ("TailL", "TailR"):
            add(side, b + 1, (-6, 0, -turn * 4))
            add(side, b + 5, (16, -turn * 10, -turn * 10))
            add(side, b + 10, (-6, turn * 4, turn * 3))
            add(side, b + 16, (3, 0, 0))
        add("Lantern", b + 2, (-6, turn * 4, 0))
        add("Lantern", b + 6, (14, -turn * 14, 0))
        add("Lantern", b + 12, (-6, turn * 6, 0))
        add("Lantern", b + 19, (2, 0, 0))

        # Then, a beat's tick later, drawn for the next: snapped there, a hair past, settled, and held, poised.
        if k + 1 < len(BLOWS):
            nxt = quarters_pose(BLOWS[k + 1], "draw")
            nb = b + 30
            add("Key", nb - 16, settle["Key"])
            add("Key", nb - 10, mix(settle["Key"], nxt["Key"], 1.08))
            add("Key", nb - 7, nxt["Key"])
            add("_poleR", nb - 15, settle["_poleR"])
            add("_poleR", nb - 9, nxt["_poleR"])
            add("_gripL", nb - 15, settle["_gripL"])
            add("Spine", nb - 17, settle["Spine"])
            add("Spine", nb - 11, nxt["Spine"])
            add("Head", nb - 15, settle["Head"])
            add("Head", nb - 9, nxt["Head"])
            add("ArmL", nb - 15, settle["ArmL"])
            add("ForeL", nb - 13, settle["ForeL"])
            hips(nb - 18, settle["HipsRot"][2], 0.0, low + 0.015)
            hips(nb - 12, nxt["HipsRot"][2], 0.0, -0.06)
    return t


WARN = None  # (the Quarters' first frame, filled below from the Quarters' own tracks)

# ---------------------------------------------------------------- Lost, Stagger, Death

LOST = body(Hips=((0, 2, 0), (-0.015, 0.0, -0.01)), Spine=(1, -2, 0), Head=(8, -16, -14),
            FootR=foot(0.12, yaw=12), _gripL=(0, 1, 0), _poleL=(-1, 0.2, -0.6), _handL=(-0.2, -0.12, 1.66))
LOST["Key"] = key((0.29, -0.02, 0.93), -84)


def topple(angle, lift=0.0):
    """Stiff as a board, toppling forward about its toes: the hips and both feet turned together."""
    pivot = Vector((0, -0.18, 0))
    turn = Euler(rad((angle, 0, 0))).to_matrix()

    def about(head):
        return tuple((pivot - Vector(head)) - turn @ (pivot - Vector(head)) + Vector((0, 0, lift)))
    out_ = {"Hips": ((angle, 0, 0), about((0, 0, HIP)))}
    for s in (L, R):
        f = foot(yaw=s * 8)
        out_["Foot" + SIDE[s]] = ((angle, 0, s * 8), tuple(Vector(about(AN[s])) + Vector(f[1])))
    return out_


def build_actions():
    quarters = quarters_tracks()
    q_baked = kit.bake(quarters, 99)
    q1 = q_baked[0][1]  # (the Quarters' first pose and its last, as baked: the Warn ends on one, its recovery starts on the other)
    qe = with_(ATTN, **q_baked[-1][1])
    warn_end = with_(ATTN, **{b: v for b, v in q1.items()})

    actions = {}
    # Idle (3 s loop): at attention, breathing small and stiff; it keeps time: on one beat its head ticks round to
    # look (a snap, a hair past, held), on the next back the other way; on the third the key's point is lifted and
    # tapped down on the floor (the tails and lantern twitch after it); then it's back at attention.
    a = ATTN
    actions["Idle"] = clip("Idle", 89, keys({
        "Root": [(1, (0, 0, 0))],
        "Hips": [(1, a["Hips"]), (24, ((0, 0, 0), (0, 0, -0.004))), (46, ((0, 0.5, 0), (0.003, 0, -0.008))),
                 (70, ((0, 0, 0), (0, 0, -0.004)))],
        "Spine": [(1, (-1, 0, 0)), (22, (-2.2, 0, 0)), (44, (-0.6, 0, 0)), (60, (-1.6, 0, 0)), (63, (-0.8, 0, 0)), (78, (-2.0, 0, 0))],
        "Head": [(1, (15, 0, 0)), (12, (15, 0, 0)), (14, (13, 0, 22)), (15, (13, 0, 24.5)), (18, (13.5, 0, 22)),
                 (40, (13.5, 0, 22)), (42, (14, 0, -16)), (43, (14, 0, -18.5)), (46, (14, 0, -16)), (59, (14, 0, -10)),
                 (62, (17, 0, -2)), (64, (15.5, 0, 0.5)), (68, (16, 0, 0)), (82, (15, 0, 0))],
        "Key": [(1, a["Key"]), (55, a["Key"]), (59, key((0.29, -0.1, 0.975), -56)), (61, key((0.28, -0.05, 0.94), -77)),
                (62, key((0.28, -0.05, 0.937), -76)), (64, key((0.28, -0.05, 0.945), -76.3)), (68, a["Key"])],
        "ArmL": [(1, (-2, 3, 0)), (16, (-1.2, 3, 0)), (45, (-2.4, 3, 0)), (62, (-1, 3.5, 0)), (66, (-2.2, 3, 0))],
        "ForeL": [(1, (-8, 0, 0)), (18, (-7, 0, 0)), (47, (-9, 0, 0)), (64, (-6, 0, 0)), (70, (-8, 0, 0))],
        "TailL": [(1, (0, 0, 0)), (17, (0, 0, -2)), (21, (0, 0, 1)), (46, (0, 0, 2)), (50, (0, 0, -1)), (63, (3, 0, 0)),
                  (68, (-1.5, 0, 0)), (74, (0, 0, 0))],
        "TailR": [(1, (0, 0, 0)), (18, (0, 0, -2)), (22, (0, 0, 1)), (47, (0, 0, 2)), (51, (0, 0, -1)), (63, (4, 0, 0)),
                  (69, (-2, 0, 0)), (75, (0, 0, 0))],
        "Lantern": [(1, (0, 0, 0)), (19, (0, 3, 2)), (26, (0, -1.5, 0)), (48, (0, -3, -2)), (55, (0, 1.5, 0)), (65, (4, 0, 0)),
                    (72, (-2, 0, 0)), (80, (0, 0, 0))],
    }, {b: [(1, v)] for b, v in a.items() if b in ("ArmR", "ForeR", "FootL", "FootR", "_poleR", "_poleL", "_holdR", "_gripL", "_handL")}),
        periodic=True)

    # Walk (2 s, two steps, a step a beat): a stiff march: the knee lifted, the leg thrown out straight and the sole
    # slapped down on the tick; it stands there (both feet down) a moment, then on. It starts on the upright passing
    # pose (the left foot lifted past the right), so it joins what's before and after.
    w = walk_tracks()
    w.update({b: [(1, v)] for b, v in ATTN.items() if b in ("Root", "ArmR", "ForeR", "_poleL", "_holdR", "_gripL", "_handL")})
    actions["Walk"] = clip("Walk", 60, w, periodic=True)
    W = dict(kit.bake(w, 60, periodic=True)[0][1])  # (the march's first pose: what a recovery hands over to)

    # Windup (the Count, 1.85 s, begun on a beat: the beat after falls about frame 30; the blow, the Strike, on the
    # one after that): a dip, and the key swung up overhead into both hands on the beat, the left foot stepping
    # forward under it (snapped there, a hair past, held); on the next beat a jolt: it ticks a notch further back and
    # the body with it; then it strains up, the back heel lifting, the key back as far as it goes, and holds.
    actions["Windup"] = clip("Windup", 55, keys(held(ATTN, 1), {
        "Hips": [(3, ((0, 0, 0), (0, 0.01, -0.035))), (8, ((0, 0, 0), (0, 0.02, -0.03))), (11, ((0, 0, 0), (0, 0.03, -0.045))),
                 (14, RAISED["Hips"]), (28, RAISED["Hips"]), (30, ((0, 0, 0), (0, 0.035, -0.045))), (33, ((0, 0, 0), (0, 0.03, -0.032))),
                 (36, JOLTED["Hips"]), (42, JOLTED["Hips"]), (50, RAISED_HIGH["Hips"])],
        "Spine": [(3, (3, 0, 0)), (8, (-8, 0, 0)), (12, RAISED["Spine"]), (29, RAISED["Spine"]), (31, (-11, 0, 0)),
                  (34, JOLTED["Spine"]), (42, JOLTED["Spine"]), (51, RAISED_HIGH["Spine"])],
        "Head": [(4, (18, 0, 0)), (10, (0, 0, 0)), (14, RAISED["Head"]), (30, RAISED["Head"]), (32, (-2, 0, 0)), (35, (3, 0, 0)),
                 (44, (3, 0, 0)), (52, (0, 0, 0))],
        "Key": [(3, key((0.29, 0.0, 0.92), -84)), (5, key((0.3, -0.1, 0.97), -58)), (7, key(out(RAISED, 15, -5, 0.47), 8, yaw=-2)),
                (9, key(out(RAISED, -25, 40, 0.47), 88, yaw=-3)), (11, key(out(RAISED, -50, 64, 0.46), 127, yaw=-4)), (14, RAISED["Key"]), (29, RAISED["Key"]),
                (31, key(out(JOLTED, -55, 62, 0.46), 138, yaw=-4)), (34, JOLTED["Key"]), (42, JOLTED["Key"]),
                (51, RAISED_HIGH["Key"]), (53, key(out(RAISED_HIGH, -55, 65, 0.46), 147.5, yaw=-4)), (56, RAISED_HIGH["Key"])],
        "_poleR": [(5, (0.6, 0.6, -0.5)), (10, RAISED["_poleR"])],
        "_gripL": [(8, (0, 0, 0)), (12, (1, 0, 0))],
        "_poleL": [(8, (-0.6, -0.2, -0.6)), (12, RAISED["_poleL"])],
        "ArmL": [(4, (-10, 4, 0)), (10, (-90, 10, 0))],
        "ForeL": [(5, (-20, 0, 0)), (11, (-60, 0, 0))],
        "FootL": [(3, ATTN["FootL"]), (5, foot(-0.08, lift=0.06, pitch=-6, yaw=-6, dx=-0.02)), (7, foot(heel=-0.2 + 0.075, pitch=-8, yaw=-6, dx=-0.02)),
                  (8, RAISED["FootL"])],
        "FootR": [(4, ATTN["FootR"]), (9, RAISED["FootR"]), (42, RAISED["FootR"]), (50, RAISED_HIGH["FootR"])],
        "TailL": [(5, (-4, 0, 0)), (10, (10, 0, 2)), (16, (-3, 0, 0)), (22, (1, 0, 0)), (32, (6, 0, 0)), (37, (-2, 0, 0)), (46, (2, 0, 0)), (55, (0, 0, 0))],
        "TailR": [(5, (-4, 0, 0)), (11, (12, 0, -2)), (17, (-4, 0, 0)), (23, (1, 0, 0)), (32, (7, 0, 0)), (38, (-2, 0, 0)), (47, (2, 0, 0)), (55, (0, 0, 0))],
        "Lantern": [(5, (-6, 0, 0)), (11, (10, 0, 0)), (18, (-4, 0, 0)), (26, (1, 0, 0)), (33, (6, 0, 0)), (39, (-3, 0, 0)), (48, (1, 0, 0))],
    }))

    # Strike (0.3 s; the blow lands at frame 6, half way, on the beat): brought straight down in both hands, the body
    # bowing rigidly after it from the waist, the left foot stamped forward (the code lunges it on); the impact the
    # deepest (the key low before it, the back heel up), then a rebound and the head whipping down after.
    sm = SMASH
    actions["Strike"] = clip("Strike", 9, keys(held(RAISED_HIGH, 1), {
        "Hips": [(3, ((0, 0, 0), (0, -0.02, -0.01))), (5, ((0, 0, 0), (0, -0.1, -0.08))), (6, ((0, 0, 0), (0, -0.14, -0.13))),
                 (8, ((0, 0, 0), (0, -0.14, -0.11))), (10, sm["Hips"])],
        "Spine": [(2, (-10, 0, 0)), (4, (8, 0, 0)), (6, (35, 0, 0)), (8, (29, 0, 0)), (10, sm["Spine"])],
        "Head": [(3, (-4, 0, 0)), (5, (4, 0, 0)), (7, (30, 0, 0)), (10, sm["Head"])],
        "Key": [(2, key(out(RAISED_HIGH, -52, 66, 0.46), 140, yaw=-4)),
                (4, key(out(with_(RAISED_HIGH, Spine=(8, 0, 0), Hips=((0, 0, 0), (0, -0.05, -0.04))), -45, 45, 0.48), 66, yaw=-5)),
                (5, key(out(with_(sm, Spine=(22, 0, 0), Hips=((0, 0, 0), (0, -0.1, -0.08))), -35, 0, 0.48), -6, yaw=-6)),
                (6, key(out(with_(sm, Spine=(35, 0, 0), Hips=((0, 0, 0), (0, -0.14, -0.13))), -40, -50, 0.48), -64, yaw=-6)),
                (8, key(out(with_(sm, Spine=(29, 0, 0), Hips=((0, 0, 0), (0, -0.14, -0.11))), -40, -44, 0.48), -54, yaw=-6)),
                (10, sm["Key"])],
        "_poleR": [(3, (1, -0.2, 0.2)), (6, sm["_poleR"])],
        "_poleL": [(3, (-1, -0.2, 0.2)), (6, sm["_poleL"])],
        "FootL": [(2, foot(-0.24, lift=0.05, pitch=-4, yaw=-6, dx=-0.03)), (5, foot(heel=-0.48 + 0.075, pitch=-10, yaw=-6, dx=-0.03)),
                  (6, sm["FootL"])],
        "FootR": [(2, RAISED_HIGH["FootR"]), (6, sm["FootR"])],
        "TailL": [(3, (8, 0, 0)), (6, (-12, 0, 0)), (8, (14, 0, 0)), (10, (8, 0, 0))],
        "TailR": [(3, (8, 0, 0)), (6, (-12, 0, 0)), (8, (15, 0, 0)), (10, (9, 0, 0))],
        "Lantern": [(3, (6, 0, 0)), (7, (-16, 0, 0)), (10, (-8, 0, 0))],
    }))

    # Recover (1.2 s, the punish window): it stays down in its bow, the key's point low, breathing; its head ticks up
    # twice (counting). Its back foot draws up under it; then on the beat (about frame 27) it snaps upright, a hair
    # past (leaning back), the key swung down to its side, and draws its forward foot back, lifting it: it ends on
    # the march's first pose (the chase follows), stepping off.
    actions["Recover"] = clip("Recover", 36, keys(held(sm, 1), held(W, 37), {
        "Hips": [(5, ((0, 0, 0), (0, -0.14, -0.125))), (12, ((0, 0, 0), (0, -0.14, -0.115))), (18, ((0, 0, 0), (0, -0.14, -0.125))),
                 (23, ((0, 0, 0), (0, -0.12, -0.105))), (27, ((0, 0, 0), (0, -0.07, -0.03))), (30, ((0, 0, 0), (0, -0.02, -0.012))),
                 (33, ((0, 0, 0), (0.01, 0, -0.02)))],
        "Spine": [(5, (33, 0, 0)), (12, (31, 0, 0)), (19, (33, 0, 0)), (25, (30, 0, 0)), (28, (-4, 0, 0)), (31, (-1, 0, 0)), (34, (1.5, 0, 0))],
        "Head": [(4, (26, 0, 0)), (9, (24, 0, 0)), (10, (18, 0, 0)), (12, (19, 0, 0)), (16, (19, 0, 0)), (17, (13, 0, 0)), (19, (14, 0, 0)),
                 (26, (14, 0, 0)), (29, (22, 0, 0)), (32, (9, 0, 0)), (35, (11, 0, 0))],
        "Key": [(6, key(out(sm, -40, -50, 0.48), -64, yaw=-6)), (22, key(out(with_(sm, Spine=(30, 0, 0)), -40, -50, 0.48), -66, yaw=-6)),
                (27, key(out(with_(ATTN, Spine=(8, 0, 0), Hips=((0, 0, 0), (0, -0.07, -0.03))), 4, -64, 0.46), -76, yaw=-2)),
                (30, key((0.29, -0.07, 0.95), -70))],
        "_gripL": [(24, (1, 0, 0)), (28, (0, 0, 0))],
        "_poleR": [(24, sm["_poleR"]), (29, ATTN["_poleR"])],
        "_poleL": [(24, sm["_poleL"]), (28, ATTN["_poleL"])],
        "ArmL": [(24, (-50, 6, 0)), (30, (4, 3, 0))],
        "ForeL": [(25, (-40, 0, 0)), (31, (-4, 0, 0))],
        "FootR": [(12, sm["FootR"]), (15, foot(0.2, lift=0.05, pitch=10, yaw=10, dx=0.02)), (18, foot(0.0, yaw=6))],
        "FootL": [(25, sm["FootL"]), (28, foot(-0.3, lift=0.06, pitch=-6, yaw=-6)), (32, foot(-0.1, lift=0.13, pitch=6, yaw=-6))],
        "TailL": [(1, (8, 0, 0)), (3, (10, 0, 0)), (8, (6, 0, 0)), (20, (6, 0, 0)), (28, (-8, 0, 0)), (31, (10, 0, 0)), (34, (-3, 0, 0))],
        "TailR": [(1, (9, 0, 0)), (3, (11, 0, 0)), (8, (6, 0, 0)), (20, (6, 0, 0)), (28, (-8, 0, 0)), (32, (10, 0, 0)), (35, (-3, 0, 0))],
        "Lantern": [(1, (-8, 0, 0)), (3, (-9, 0, 0)), (9, (-3, 0, 0)), (26, (-3, 0, 0)), (29, (12, 0, 0)), (32, (-6, 0, 0))],
    }))

    # Warn (the Quarters' tell, 0.85 s from a beat to just before the next): a small check the other way, then the
    # key snapped back to its right at shoulder height, the body wound round after it, the left hand thrown forward
    # at you; held, poised; the right foot set forward, the left behind on its toes; a last notch back as the beat
    # comes.
    wd = warn_end
    actions["Warn"] = clip("Warn", 25, keys(held(ATTN, 1), held(wd, 26), {
        "Hips": [(3, ((0, 0, -3), (0, 0, -0.02))), (8, ((0, 0, 15), (-0.0, -0.01, -0.035))), (12, ((0, 0, 13), (0, -0.02, -0.04))),
                 (20, ((0, 0, 13), (0, -0.02, -0.04)))],
        "Spine": [(3, (2, 0, -4)), (9, (2, 0, 26)), (13, (2, 0, 22)), (21, (2, 0, 22))],
        "Head": [(4, (16, 0, 3)), (10, (12, 0, -38)), (14, (12, 0, -31)), (22, (12, 0, -31))],
        "Key": [(3, key((0.27, -0.12, 0.95), -70, yaw=-6)), (5, key((0.42, -0.12, 1.08), -10, yaw=60)), (8, tuple(tuple(1.06 * c for c in v) for v in wd["Key"])),
                (11, wd["Key"]), (21, wd["Key"])],
        "_poleR": [(4, (0.3, 0.5, -0.6)), (9, wd["_poleR"])],
        "ArmL": [(4, (-4, 4, 0)), (10, (-62, -10, 0)), (14, (-52, -8, 0)), (22, (-55, -8, 0))],
        "ForeL": [(5, (-10, 0, 0)), (11, (-36, 0, 0)), (15, (-28, 0, 0)), (23, (-30, 0, 0))],
        "FootR": [(4, ATTN["FootR"]), (7, foot(-0.12, lift=0.05, pitch=-6, yaw=8, dx=0.03)), (10, wd["FootR"])],
        "FootL": [(6, ATTN["FootL"]), (10, foot(heel=wd["FootL"][1][1] + 0.075, pitch=0, yaw=-8, dx=-0.03)), (14, wd["FootL"])],
        "TailL": [(4, (0, 0, 2)), (10, (4, -8, -8)), (15, (-2, 4, 3)), (21, (0, 0, 0))],
        "TailR": [(4, (0, 0, 2)), (10, (5, -9, -8)), (16, (-2, 4, 3)), (22, (0, 0, 0))],
        "Lantern": [(5, (0, 3, 0)), (11, (6, -12, 0)), (17, (-2, 5, 0)), (23, (0, 0, 0))],
    }))

    # Quarters (3.3 s): four blows a beat apart, landing on frames 6, 36, 66 and 96 (forehand, backhand, forehand,
    # then overhead in both hands, the heaviest). Each: the back foot steps through, the hips unwind, then the chest,
    # then the key whips across (crossing in front on the beat), carried through past the far shoulder, a hair back,
    # held; the body drops onto the landing foot. A tick later the key is snapped to where the next is drawn from, a
    # hair past, settled, held, poised. The head stays on you throughout. (The code shoves it on 0.44 m with each
    # blow: the feet down are keyed where they stand in the world as it glides, so here they slide back under it.)
    q = quarters
    q.update({b: [(1, v)] for b, v in ATTN.items() if b not in q})
    actions["Quarters"] = clip("Quarters", 99, q)

    # QuartersRecover (1.4 s, the punish window): the last foot lands (the glide still carrying it), it's bowed over
    # the key's point, breathing; its back foot steps up; on the beat (about frame 27) it snaps upright, a hair past,
    # the key swung down to its side, the forward foot stepped back, and the other lifted: it ends on the march's first pose
    # (the chase follows), stepping off.
    over_hit = quarters_pose("over", "settle")
    actions["QuartersRecover"] = clip("QuartersRecover", 42, keys(held(W, 43), {
        "Hips": [(1, qe["Hips"]), (6, ((0, 0, 0), (0, -0.02, -0.13))), (14, ((0, 0, 0), (0, -0.04, -0.115))), (21, ((0, 0, 0), (0, -0.05, -0.125))),
                 (25, ((0, 0, 0), (0, -0.05, -0.09))), (29, ((0, 0, 0), (0, -0.03, -0.02))), (33, ((0, 0, 0), (0, 0, -0.002))), (37, ((0, 0, 0), (0.008, 0, -0.012)))],
        "Spine": [(1, qe["Spine"]), (6, (34, 0, 0)), (14, (31, 0, 0)), (22, (33, 0, 0)), (26, (30, 0, 0)), (30, (-5, 0, 0)), (33, (-3, 0, 0)), (37, (-1, 0, 0))],
        "Head": [(1, qe["Head"]), (4, (28, 0, 0)), (10, (24, 0, 0)), (12, (17, 0, 0)), (14, (18, 0, 0)), (19, (18, 0, 0)), (20, (12, 0, 0)), (22, (13, 0, 0)),
                 (28, (13, 0, 0)), (31, (23, 0, 0)), (35, (9, 0, 0)), (39, (11, 0, 0))],
        "Key": [(1, qe["Key"]), (4, key(out(with_(over_hit, Hips=((0, 0, 0), (0, -0.01, -0.13)), Spine=(34, 0, 0)), -40, -58, 0.48), -80, yaw=-6)),
                (24, key(out(with_(over_hit, Hips=((0, 0, 0), (0, -0.05, -0.12)), Spine=(31, 0, 0)), -40, -58, 0.48), -80, yaw=-6)),
                (29, key(out(with_(ATTN, Spine=(8, 0, 0), Hips=((0, 0, 0), (0, -0.04, -0.03))), 4, -64, 0.46), -76, yaw=-2)),
                (33, key((0.29, -0.07, 0.95), -70))],
        "_gripL": [(1, qe["_gripL"]), (26, (1, 0, 0)), (30, (0, 0, 0))],
        "_poleR": [(1, qe["_poleR"]), (26, qe["_poleR"]), (31, ATTN["_poleR"])],
        "_poleL": [(1, qe["_poleL"]), (26, qe["_poleL"]), (30, ATTN["_poleL"])],
        "ArmL": [(1, qe["ArmL"]), (26, (-50, 6, 0)), (32, (4, 3, 0))],
        "ForeL": [(1, qe["ForeL"]), (27, (-40, 0, 0)), (33, (-4, 0, 0))],
        # (the glide goes on into it: the last blow was 3.5 frames before it began)
        "FootR": [(1, qe["FootR"]), (3, foot(LAND + glide(7.5) - glide(3.5), yaw=8, dx=0.04)), (6, foot(LAND + glide(10.5) - glide(3.5), yaw=8, dx=0.04)),
                  (12, foot(LEAD, yaw=8, dx=0.04)), (26, foot(LEAD, yaw=8, dx=0.04)), (29, foot(-0.14, lift=0.06, pitch=-4, yaw=7)),
                  (32, foot(0.0, yaw=6))],
        "FootL": [(1, qe["FootL"]), (3, foot(LEAD + glide(7.5), yaw=-8, dx=-0.03)), (6, foot(LEAD + glide(10.5), yaw=-8, dx=-0.03)),
                  (12, foot(LEAD + GLIDE, yaw=-8, dx=-0.03)), (15, foot(toe=LEAD + GLIDE - 0.175, pitch=20, yaw=-8, dx=-0.03)),
                  (18, foot(0.05, lift=0.05, pitch=4, yaw=-7)), (21, foot(0.0, yaw=-6)),
                  (35, foot(0.0, yaw=-6)), (39, foot(toe=-0.175 + 0.02, pitch=16, yaw=-6))],
        "TailL": [(1, qe["TailL"]), (5, (12, 0, 0)), (10, (5, 0, 0)), (22, (6, 0, 0)), (30, (-8, 0, 0)), (33, (10, 0, 0)), (37, (-3, 0, 0)), (40, (0, 0, 0))],
        "TailR": [(1, qe["TailR"]), (5, (13, 0, 0)), (10, (5, 0, 0)), (22, (6, 0, 0)), (30, (-8, 0, 0)), (34, (10, 0, 0)), (38, (-3, 0, 0))],
        "Lantern": [(1, qe["Lantern"]), (5, (-8, 0, 0)), (11, (-3, 0, 0)), (28, (-3, 0, 0)), (31, (12, 0, 0)), (34, (-6, 0, 0)), (37, (2, 0, 0)), (40, (0, 0, 0))],
    }))

    # Lost (2 s loop, it's lost count; the code leaves it standing): it starts as the march does, a foot lifted to step
    # off, and stops: the foot hovers, and is set down, unsure. Its head cocks, its left hand comes up cupped to its ear,
    # listening; out of time now, its head jerks at odd moments and its key's point taps the floor twice, late, and
    # once again, nowhere near a beat. The hand comes down and it lifts the foot to try again (so it begins and ends on
    # the march's first pose: from a stagger, and back into the march when it finds the beat).
    lo = with_(LOST, FootL=W["FootL"], FootR=W["FootR"])
    hl = lo["_handL"]
    actions["Lost"] = clip("Lost", 60, keys(held(W, 1), {
        "Hips": [(1, W["Hips"]), (5, W["Hips"]), (11, ((0, 2, 0), (-0.012, 0.0, -0.014))), (14, ((0, 1.5, 0), (-0.015, 0.0, -0.008))),
                 (24, ((0, 1, 0), (-0.01, 0.005, -0.012))), (34, ((0, 3, 0), (-0.018, -0.004, -0.008))), (46, ((0, 1.5, 0), (-0.012, 0.002, -0.012))),
                 (52, ((0, 0, 0), (0.0, 0.0, -0.012)))],
        "Spine": [(1, W["Spine"]), (6, W["Spine"]), (12, (1, -2, 0)), (24, (2, -1, 1)), (35, (0, -3, -1)), (46, (1.5, -1.5, 0)), (54, (2, 0, 0))],
        "Head": [(1, W["Head"]), (5, W["Head"]), (8, (12, -4, -6)), (10, (11, -4, -7)), (14, (8, -16, -14)), (16, (7, -17, -15)),
                 (19, (8, -16, -14)), (22, (7, -18, -20)), (24, (7, -17, -18)), (26, (10, -8, -2)), (28, (9, -9, -4)),
                 (31, (9, -9, -4)), (32, (10, 10, 18)), (34, (9, 7, 12)), (38, (9, 7, 12)), (40, (8, -6, -4)), (42, (8, -6, -4)),
                 (43, (6, -16, -16)), (46, (7, -15, -14)), (50, (9, -8, -6)), (55, (11, 0, -1))],
        "Key": [(1, W["Key"]), (6, W["Key"]), (12, lo["Key"]), (19, lo["Key"]), (21, key((0.29, -0.09, 0.975), -60)), (23, key((0.29, -0.03, 0.925), -85)),
                (25, key((0.29, -0.08, 0.97), -63)), (27, key((0.29, -0.03, 0.925), -85)), (29, lo["Key"]), (38, lo["Key"]),
                (41, key((0.29, -0.1, 0.98), -56)), (44, key((0.29, -0.03, 0.925), -85)), (47, lo["Key"]), (52, lo["Key"])],
        "_poleR": [(1, W["_poleR"]), (8, ATTN["_poleR"]), (52, ATTN["_poleR"])],
        "ArmL": [(1, W["ArmL"]), (6, W["ArmL"]), (12, (-4, 6, 0)), (50, (-4, 6, 0)), (56, W["ArmL"])],
        "ForeL": [(1, W["ForeL"]), (7, W["ForeL"]), (13, (-10, 0, 0)), (51, (-10, 0, 0)), (57, W["ForeL"])],
        "_gripL": [(1, (0, 0, 0)), (12, (0, 0, 0)), (18, (0, 1, 0)), (43, (0, 1, 0)), (49, (0, 0, 0))],
        "_handL": [(1, (-0.3, -0.06, 1.0)), (12, (-0.3, -0.06, 1.0)), (16, (-0.25, -0.16, 1.5)), (18, hl), (21, (-0.21, -0.12, 1.67)),
                   (28, (-0.2, -0.11, 1.66)), (36, (-0.19, -0.13, 1.64)), (43, hl), (46, (-0.25, -0.16, 1.45)), (49, (-0.3, -0.06, 1.0))],
        "_poleL": [(1, lo["_poleL"])],
        "FootL": [(1, W["FootL"]), (5, foot(-0.03, lift=0.16, pitch=-2, yaw=-6)), (9, foot(-0.02, lift=0.03, pitch=-4, yaw=-6)), (11, foot(0.0, yaw=-6)),
                  (48, foot(0.0, yaw=-6)), (52, foot(toe=-0.175 + 0.02, pitch=16, yaw=-6)), (56, foot(-0.02, lift=0.1, pitch=8, yaw=-6))],
        "TailL": [(1, W["TailL"]), (7, (0, 0, 0)), (12, (3, 0, -2)), (18, (0, 0, 1)), (30, (0, 2, 2)), (40, (0, -1, 0)), (48, (0, 0, -2))],
        "TailR": [(1, W["TailR"]), (7, (0, 0, 0)), (13, (3, 0, -2)), (19, (0, 0, 1)), (31, (0, 2, 2)), (41, (0, -1, 0)), (49, (0, 0, -2))],
        "Lantern": [(1, W["Lantern"]), (8, (0, 0, 0)), (13, (3, 3, 0)), (22, (3, -1, 0)), (30, (0, -4, 0)), (45, (-2, 2, 0))],
    }), periodic=True)

    # Stagger (0.57 s as made; the code plays it over a second, or 1.6 s when a perfect block stuns it): knocked off
    # its stance, it reels back (the head snapping back after the chest, the key flung out wide, the left arm thrown
    # up), its right foot stumbling back to catch it; it rocks forward, stiffens upright, steps the foot back under it and
    # lifts the other: it ends on the march's first pose (the chase follows: Lost, or the march if it has the beat).
    actions["Stagger"] = clip("Stagger", 17, keys(held(ATTN, 1), held(W, 18), {
        "Hips": [(3, ((0, 0, 4), (0, 0.1, -0.01))), (6, ((0, -3, 6), (0.02, 0.16, -0.06))), (9, ((0, 3, 2), (0.0, 0.08, -0.05))),
                 (12, ((0, 2, 0), (-0.01, 0.01, -0.02))), (15, ((0, 2, 0), (-0.015, -0.005, -0.006)))],
        "Spine": [(3, (-16, 2, 6)), (6, (-12, -2, 4)), (9, (6, -3, 0)), (12, (-1, -2, 0)), (15, (2, -2, 0))],
        "Head": [(4, (-18, 4, 6)), (7, (-4, -6, -4)), (10, (16, -10, -8)), (13, (12, -6, -8)), (16, (11, -2, -3))],
        "Key": [(3, key((0.36, 0.04, 1.0), -45, yaw=40)), (5, key((0.46, 0.08, 1.1), 0, yaw=80)), (7, key((0.45, 0.06, 1.08), -6, yaw=76)), (10, key((0.34, -0.02, 0.97), -58, yaw=24)),
                (12, key((0.29, -0.03, 0.93), -88))],
        "_poleR": [(3, (0.4, 0.4, -1)), (9, (0.3, 1, -0.2))],
        "ArmL": [(3, (-30, 30, 0)), (6, (-55, 40, 0)), (10, (-10, 10, 0))],
        "ForeL": [(4, (-30, 0, 0)), (7, (-50, 0, 0)), (11, (-20, 0, 0))],
        "FootR": [(2, ATTN["FootR"]), (4, foot(0.12, lift=0.06, pitch=4, yaw=10)), (6, foot(heel=0.24 + 0.075, pitch=-10, yaw=12)),
                  (7, foot(0.24, yaw=12)), (11, foot(0.24, yaw=12)), (13, foot(0.13, lift=0.06, pitch=6, yaw=9)), (15, foot(0.0, yaw=6))],
        "FootL": [(5, ATTN["FootL"]), (6, foot(toe=-0.175, pitch=14, yaw=-8)), (8, foot(yaw=-8)), (14, foot(yaw=-7))],
        "TailL": [(3, (-10, 0, 0)), (7, (18, 4, 0)), (11, (-6, 0, 0)), (15, (2, 0, 0))],
        "TailR": [(3, (-10, 0, 0)), (7, (20, 4, 0)), (12, (-6, 0, 0)), (16, (2, 0, 0))],
        "Lantern": [(3, (-12, 0, 0)), (8, (16, 4, 0)), (12, (-8, 0, 0)), (16, (2, 0, 0))],
    }))

    # Death (0.83 s, then it dissolves): a jolt, and it stops dead, stiff (the watch has stopped); the key slips from
    # its fingers and clatters down, its point first, then flat; it tips forward on its toes, rigid as a board, slowly
    # and then faster, and slams down on its face (a bounce), the head and arms knocked loose last.
    def fall(f, angle, lift=0.0):
        return {b: [(f, v)] for b, v in topple(angle, lift).items()}

    actions["Death"] = clip("Death", 25, keys(held(ATTN, 1), fall(10, 0), fall(13, 2), fall(16, 14),
                                              fall(19, 40), fall(22, 74, 0.01), fall(23, 80, 0.04), fall(24, 75, 0.06), fall(26, 79, 0.045), {
        "Spine": [(3, (-8, 0, 0)), (6, (-6, 0, 0)), (12, (-5, 0, 0)), (22, (-3, 0, 0)), (24, (-6, 0, 0)), (26, (-4, 0, 0))],
        "Head": [(3, (-6, 0, 0)), (6, (2, 0, 0)), (12, (2, 0, 0)), (20, (-6, 0, 0)), (23, (-10, 0, 0)), (24, (6, 0, 14)), (26, (-2, 0, 18))],
        "_holdR": [(8, (1, 0, 0)), (11, (0, 0, 0))],
        "Key": [(8, ATTN["Key"]), (11, key((0.29, -0.06, 0.84), -78, yaw=4)), (13, key_lying((-9, 4, 6), 0.32, -0.07)),
                (15, key_lying((-36, 34, 9), 0.38, -0.05, 0.03)), (17, key_lying((-58, 72, 12), 0.44, -0.02, 0.005)),
                (19, key_lying((-67, 90, 14), 0.48, 0.0)), (21, key_lying((-64, 82, 14), 0.48, 0.0, 0.015)),
                (23, key_lying((-67, 90, 14), 0.48, 0.0))],
        "ArmR": [(10, (0, 0, 0)), (14, (-4, -2, 0)), (22, (6, -2, 0)), (24, (-40, -10, 0)), (26, (-30, -14, 0))],
        "ForeR": [(12, (-6, 0, 0)), (22, (0, 0, 0)), (25, (-20, 0, 0)), (26, (-16, 0, 0))],
        "ArmL": [(6, (-2, 3, 0)), (22, (6, 3, 0)), (24, (-36, 12, 0)), (26, (-28, 16, 0))],
        "ForeL": [(12, (-8, 0, 0)), (22, (-2, 0, 0)), (25, (-24, 0, 0)), (26, (-18, 0, 0))],
        "TailL": [(4, (-6, 0, 0)), (9, (4, 0, 0)), (14, (0, 0, 0)), (21, (-10, 0, 0)), (24, (20, 0, 0)), (26, (14, 0, 0))],
        "TailR": [(4, (-6, 0, 0)), (9, (4, 0, 0)), (14, (0, 0, 0)), (21, (-12, 0, 0)), (24, (22, 0, 0)), (26, (16, 0, 0))],
        "Lantern": [(4, (-8, 0, 0)), (9, (5, 0, 0)), (14, (-1, 0, 0)), (22, (-20, 0, 0)), (24, (10, 0, 0)), (26, (6, 0, 0))],
    }))
    return actions


ACTIONS = build_actions()


def build():
    coat, coat_dark = kit.mat("PunctualLivery", "#6a2a30"), kit.mat("PunctualLiveryDark", "#4a1d22")
    vest, linen = kit.mat("PunctualWaistcoat", "#cdbf9c"), kit.mat("PunctualLinen", "#e6dfcd")
    trousers, shoe = kit.mat("PunctualTrousers", "#2b2930"), kit.mat("PunctualShoe", "#17161a")
    brass, face = kit.mat("PunctualBrass_Metal", "#c99a45"), kit.mat("PunctualWatchFace", "#ece4cc")
    black, eye = kit.mat("PunctualHand", "#141316"), kit.mat("SouredEye_Glow", "#c8f07a")
    skin, hair = kit.mat("SouredOchreDark", "#5e4a2c"), kit.mat("PunctualHair", "#2c2622")
    iron = kit.mat("PunctualIron", "#2f2d33")
    bit = kit.mat("PunctualKeyBit_Metal", "#c99a45")  # (the key's bit: brass like the rest, its own name for the swing trail's tip)

    parts = []
    # Legs: trousers to the shoe, in two at the knee (a knob of cloth hides the joint); the shoes on their own bones.
    for s in (L, R):
        hip, knee, ankle = (s * 0.11, 0, HIP - 0.02), KN[s], (s * 0.11, 0, 0.1)
        r_knee = 0.08 - 0.015 * (knee[2] - ankle[2]) / (hip[2] - ankle[2])
        parts.append(part("Trouser", kit.cone(7, r_knee, 0.065, math.dist(hip, knee)), trousers, kit.along(knee, hip), smooth=True, group="Leg" + SIDE[s]))
        parts.append(part("Trouser", kit.cone(7, 0.08, r_knee, math.dist(knee, ankle)), trousers, kit.along(ankle, knee), smooth=True, group="Shin" + SIDE[s]))
        parts.append(part("Knee", kit.sphere(8, 6, r_knee + 0.003), trousers, M(knee), smooth=True, group="Shin" + SIDE[s]))
        parts.append(part("Shoe", kit.cube(), shoe, M((s * 0.11, -0.05, 0.05), scale=(0.11, 0.25, 0.1)), group="Foot" + SIDE[s]))
    # The body: the coat over a cream waistcoat, its tails behind to the knees, brass buttons.
    parts += [
        part("Coat", kit.cone(9, 0.2, 0.26, SHOULDER - HIP + 0.05), coat, M((0, 0.01, (HIP + SHOULDER) / 2), scale=(1, 0.72, 1)), smooth=True, group="Spine"),
        part("Waistcoat", kit.cube(), vest, M((0, -0.15, 1.15), scale=(0.2, 0.06, 0.38)), group="Spine"),
        part("Shoulders", kit.sphere(9, 6), coat, M((0, 0, SHOULDER - 0.06), scale=(0.3, 0.2, 0.1)), smooth=True, group="Spine"),
        part("Collar", kit.cylinder(9, 0.09, 0.1), linen, M((0, -0.01, SHOULDER + 0.02)), smooth=True, group="Spine"),
    ]
    for s in (L, R):
        parts.append(part("Tail", kit.cube(), coat_dark, M((s * 0.09, 0.15, HIP - 0.25), rot=(-8, 0, s * 6), scale=(0.16, 0.04, 0.6)), group="Tail" + SIDE[s]))
        parts.append(part("Lapel", kit.cube(), coat_dark, M((s * 0.13, -0.16, 1.32), rot=(0, s * 12, 0), scale=(0.07, 0.03, 0.3)), group="Spine"))
    for z in (1.02, 1.12):
        for s in (L, R):
            parts.append(part("Button", kit.sphere(6, 4, 0.018), brass, M((s * 0.15, -0.18, z)), group="Spine"))
    # The watch where its heart was: a brass case grown into the chest, a pale face, a hand on its own bone.
    wx, wy, wz = WATCH
    parts += [
        part("WatchCase", kit.cylinder(16, 0.11, 0.05), brass, M((wx, wy + 0.02, wz), rot=(90, 0, 0)), smooth=True, group="Spine"),
        part("WatchFace", kit.cylinder(16, 0.09, 0.012), face, M((wx, wy - 0.006, wz), rot=(90, 0, 0)), group="Spine"),
        part("WatchBow", kit.cylinder(8, 0.025, 0.03), brass, M((wx, wy + 0.01, wz + 0.125)), group="Spine"),
        part("WatchHand", kit.cube(), black, M((wx, wy - 0.016, wz + 0.035), scale=(0.012, 0.006, 0.08)), group="WatchHand"),
        part("WatchPin", kit.cylinder(6, 0.012, 0.012), brass, M((wx, wy - 0.02, wz), rot=(90, 0, 0)), group="WatchHand"),
    ]
    for k in range(12):  # the hours, ticked round the face
        a = 2 * math.pi * k / 12
        parts.append(part("Hour", kit.cube(), black, M((wx + math.sin(a) * 0.075, wy - 0.013, wz + math.cos(a) * 0.075),
                                                       rot=(0, -math.degrees(a), 0), scale=(0.006, 0.004, 0.02 if k % 3 else 0.03)), group="Spine"))
    # The head, bowed: dark hair pressed flat, the green eyes.
    parts += [
        part("Head", kit.sphere(10, 7, 0.15), skin, M((0, -0.04, 1.68), scale=(0.95, 1.0, 1.08)), smooth=True, group="Head"),
        part("Hair", kit.sphere(10, 6, 0.155), hair, M((0, 0.0, 1.72), scale=(0.97, 1.0, 0.85)), smooth=True, group="Head"),
    ]
    for s in (L, R):
        parts.append(part("Eye", kit.sphere(8, 6, 0.026), eye, M((s * 0.055, -0.175, 1.68)), smooth=True, group="Head"))
    # Arms in livery sleeves, in two at the elbow (a knob of sleeve hides the joint), white gloves.
    for s in (L, R):
        shoulder_, elbow, hand = SH[s], EL[s], HA[s]
        parts.append(part("Sleeve", kit.cone(7, 0.0675, 0.06, math.dist(shoulder_, elbow)), coat, kit.along(elbow, shoulder_), smooth=True, group="Arm" + SIDE[s]))
        parts.append(part("Sleeve", kit.cone(7, 0.075, 0.0675, math.dist(elbow, hand)), coat, kit.along(hand, elbow), smooth=True, group="Fore" + SIDE[s]))
        parts.append(part("Elbow", kit.sphere(8, 6, 0.069), coat, M(elbow), smooth=True, group="Fore" + SIDE[s]))
        parts.append(part("Cuff", kit.cylinder(7, 0.065, 0.05), coat_dark, kit.along(hand, (hand[0], hand[1], hand[2] + 0.05)), group="Fore" + SIDE[s]))
        parts.append(part("Glove", kit.sphere(7, 5, 0.06), linen, M((hand[0], hand[1] - 0.02, hand[2] - 0.05)), smooth=True, group="Fore" + SIDE[s]))
    # The winding key: its bow in the hand, the shaft down and forward, the bit at the end.
    grip, tip = (0.27, -0.06, 0.88), KEY_TIP
    ring = [(0.27, -0.06 + 0.09 * math.sin(a), 0.93 + 0.09 * math.cos(a)) for a in (2 * math.pi * k / 12 for k in range(13))]
    parts += [
        part("KeyBow", kit.tube(ring, [0.022] * len(ring), seg=6), brass, group="Key"),
        part("KeyShaft", kit.cylinder(8, 0.03, math.dist(grip, tip)), brass, kit.along(grip, tip), smooth=True, group="Key"),
        part("KeyCollar", kit.cylinder(8, 0.05, 0.04), brass, kit.along(grip, (0.27, -0.072, 0.845)), group="Key"),
        part("KeyBit", kit.cube(), bit, kit.along((0.27, -0.27, 0.29), tip) @ M((0.05, 0, 0), scale=(0.12, 0.04, 0.1)), group="Key"),
    ]
    # A lantern hung from its belt, like every Soured's (swinging on its own bone; the game's light stays on Spine).
    lantern_glass = kit.mat("SouredLantern_Glow", "#b6f06a")
    parts += [
        part("BeltLanternTop", kit.cone(4, 0.06, 0.01, 0.05), iron, M((-0.25, 0.02, 0.92), rot=(0, 0, 45)), group="Lantern"),
        part("BeltLanternGlass", kit.cube(), lantern_glass, M((-0.25, 0.02, 0.85), scale=(0.08, 0.08, 0.1)), group="Lantern"),
        part("BeltLanternBase", kit.cube(), iron, M((-0.25, 0.02, 0.795), scale=(0.1, 0.1, 0.015)), group="Lantern"),
    ]
    mesh = kit.join(parts, "Punctual")
    armature = kit.rig("PunctualRig", BONES)
    kit.skin(mesh, armature)
    kit.animate(armature, ACTIONS)
    for name, (hand, foot_off, wrist) in REPORT.items():
        notes = []
        if hand > 0.02:
            notes.append(f"the hands come off the key by up to {hand * 100:.0f} cm")
        if foot_off > 0.02:
            notes.append(f"the legs fall short of the feet by up to {foot_off * 100:.0f} cm")
        if wrist > 100:
            notes.append(f"the wrist bends {wrist:.0f} deg")
        if notes:
            print(f"[punctual] {name}: " + "; ".join(notes))
    return [armature, mesh]
