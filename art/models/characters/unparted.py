"""Hold, the greater half of the Unparted (the Drowned City's miniboss): two souls who never let each other go. Hold
is a Clinger grown vast, grey and hunched, twice the height, with arms that sweep the ground; Fast, the smaller, rides
its back (an ordinary Clinger, placed on the Back bone by the game), and a faded red ribbon ties Hold's wrist.

Clips: Idle, Walk (a lumbering knuckle-walk), SweepWindup (0.9 s: twisting away, the right arm drawn back wide),
Sweep (0.3 s: across the front), SweepRecover (0.9 s), SlamWindup (1.1 s: rearing up, both arms high), Slam (0.25 s:
both down in front), SlamRecover (1.2 s), Howl (1.2 s: Fast is gone), Stagger, Death.

How it's animated (the Laden's way): each bone keyed on its own timing (kit.bake: tracks), so nothing moves all at
once. The hips (Pivot) lead, the chest follows, the head comes last and keeps its eyes on you. It walks on its
knuckles: the hands aren't keyed by their arms but by where they are (planted on the floor while the body travels over
them; in the sweep, aimed from the shoulder so the swing draws an arc), and every frame the arms (now in two, with an
elbow) are bent onto them (kit.reach), a hand lifted onto its claws rather than through the floor; a build reports
any clip whose hands miss. The lower robe (Hem) stays on the floor when the body sinks
or rears (it crumples, it doesn't sink), and is skinned smoothly into the body. The lantern on its belt and the end of
the ribbon on its wrist hang free: each a damped pendulum, simulated frame by frame from how the body moves them (they
lag, swing out and settle). Back, where Fast rides, is carried by the chest and turned against it a little, so Fast
rides upright-ish while Hold rears and slams.
"""
import math

from mathutils import Quaternion, Vector

M, part = kit.M, kit.part  # `kit` is injected by build_model.py

S = 2.0  # twice a Clinger
SHOULDER_Z = 0.78 * S


def s(p):
    return tuple(c * S for c in p)


# The arm in two: shoulder to elbow (ArmL/ArmR, as before by name), elbow to hand (ForeL/ForeR).
SHOULDER = {-1: s((-0.17, -0.22, 0.78)), 1: s((0.17, -0.22, 0.78))}
ELBOW = {-1: s((-0.22, -0.4, 0.46)), 1: s((0.22, -0.4, 0.46))}
HAND = {-1: s((-0.21, -0.5, 0.12)), 1: s((0.21, -0.5, 0.12))}
WRIST = s((0.215, -0.47, 0.2))
RIBBON_TIP = (WRIST[0] + 0.02, WRIST[1] + 0.2, WRIST[2] - 0.34)
LANTERN_HOOK = (0.375, 0.04, 1.1)   # where its cord is tied to the belt
LANTERN_FOOT = (0.42, 0.04, 0.6)
CHEST_HEAD = (0, -0.2, 1.08)

BONES = {
    "Root": ((0, 0, 0), (0, 0, 0.25 * S), None),
    "Pivot": ((0, 0, 0.35 * S), (0, 0, 0.45 * S), "Root"),           # the hips: the whole body leans and turns here
    "Spine": ((0, 0, 0.3 * S), (0, -0.22 * S, SHOULDER_Z), "Pivot"),  # the belly (its lantern hangs here)
    "Chest": (CHEST_HEAD, (0, -0.22 * S, SHOULDER_Z), "Spine"),       # the hunch: shoulders, hump, the shroud
    "Head": ((0, -0.22 * S, SHOULDER_Z), (0, -0.36 * S, 0.95 * S), "Chest"),
    "ArmL": (SHOULDER[-1], ELBOW[-1], "Chest"),
    "ForeL": (ELBOW[-1], HAND[-1], "ArmL"),
    "ArmR": (SHOULDER[1], ELBOW[1], "Chest"),
    "ForeR": (ELBOW[1], HAND[1], "ArmR"),
    "Back": ((0, 0.1 * S, 0.74 * S), (0, 0.1 * S, 0.84 * S), "Chest"),  # where Fast rides
    "Hem": ((0, -0.08, 0.75), (0, -0.08, 0.05), "Root"),             # the lower robe, kept on the floor
    "Lantern": (LANTERN_HOOK, LANTERN_FOOT, "Spine"),                 # its belt lantern, on a cord (a pendulum)
    "Ribbon": (WRIST, RIBBON_TIP, "ForeR"),                           # the ribbon's loose end (a pendulum)
}


def hand(x, y, z):
    """A hand target (armature space) as a track value."""
    return ((0, 0, 0), (x, y, z))


FLOOR_HAND = 0.2   # a hand's height when its knuckles are on the floor
PLANT = {-1: (-0.46, -1.0, FLOOR_HAND), 1: (0.46, -1.0, FLOOR_HAND)}
POLE = {-1: (-1, 0.35, 0.0), 1: (1, 0.35, 0.0)}  # the elbows bow out and a little back (spidery, from above)


# ---------------------------------------------------------------- the pendulums (lantern, ribbon end)

def dangle(frames, bone, freq, damping, hang, periodic, floor=None, settle=6):
    """Swing `bone` (no keyed rotation of its own) as a damped pendulum through the baked `frames`: its tip follows
    where the body carries it (it lags, overshoots, settles). freq: its swing (cycles a second); damping: share of its
    speed lost a frame; hang: how much it keeps hanging the way it hangs at rest when the body tips (0: rigid with its
    parent, 1: always straight down as at rest); floor: its tip kept above this; settle: a one-off's last frames ease
    it back to hanging still (so the clip ends where the next begins)."""
    head, tail, parent = BONES[bone]
    rest = Vector(tail) - Vector(head)
    length = rest.length
    rest.normalize()
    heads, aims, parents = [], [], []
    for _, p in frames:
        m = kit.pose_matrices(BONES, dict(p, **{bone: (0, 0, 0)}))
        heads.append(m[bone] @ Vector(head))
        rigid = (m[parent].to_3x3() @ rest).normalized()
        aims.append((rigid * (1 - hang) + rest * hang).normalized())
        parents.append(m[parent].to_quaternion())
    steps = 4
    k = (2 * math.pi * freq / kit.FPS) ** 2
    keep = (1 - damping) ** (1 / steps)
    n = len(frames)
    tip = heads[0] + aims[0] * length  # (at rest on its first pose; a loop goes round until it repeats itself)
    if floor is not None and tip.z < floor:
        tip.z = floor
    prev = tip.copy()
    tips = [None] * n
    passes = 6 if periodic else 1
    span = n - 1 if periodic else n  # (a loop's last frame is its first again)
    for _ in range(passes):
        for i in range(span):
            if i == 0 and not periodic:
                tips[0] = tip.copy()
                continue
            j = i - 1 if i > 0 else span - 1
            for sub in range(1, steps + 1):
                t = sub / steps
                h = heads[j].lerp(heads[i], t)
                goal = h + aims[j].lerp(aims[i], t).normalized() * length
                acc = (goal - tip) * k
                new = tip + (tip - prev) * keep + acc / (steps * steps)
                new = h + (new - h).normalized() * length
                if floor is not None and new.z < floor:
                    new.z = floor
                prev, tip = tip, new
            tips[i] = tip.copy()
    if periodic:
        tips[n - 1] = tips[0]
    out = []
    for i, ((f, p), h, q, t) in enumerate(zip(frames, heads, parents, tips)):
        d = (t - h).normalized()
        if not periodic and settle and i > n - 1 - settle:  # (settled by the end: it joins what follows)
            u = (i - (n - 1 - settle)) / settle
            d = d.lerp(aims[i], u * u * (3 - 2 * u)).normalized()
            if floor is not None and (h + d * length).z < floor:
                d = (h + d * length + Vector((0, 0, floor - (h + d * length).z)) - h).normalized()
        spec = rest.rotation_difference(q.inverted() @ d)
        p = dict(p)
        p[bone] = tuple(math.degrees(v) for v in spec.to_euler("XYZ"))
        out.append((f, p))
    return out


# ---------------------------------------------------------------- clips

CLAWS = {side: [Vector(HAND[side]) + Vector((k * 0.07, -0.2, -0.16)) for k in (-1, 0, 1)] for side in (-1, 1)}  # (tips)
ARM = math.dist(SHOULDER[1], ELBOW[1]) + math.dist(ELBOW[1], HAND[1])
REACH = {}  # (the build's report: how far each clip's hands miss their targets, at worst)
FLOOR = {}  # (and how far below the floor a claw goes)


def carry(pose):
    """One frame: the arms bent onto their hands' targets, the lower robe left on the floor, Back turned against
    the chest so Fast rides upright-ish."""
    pose = dict(pose)
    targets = {}
    for side, upper, lower, key in ((-1, "ArmL", "ForeL", "_HL"), (1, "ArmR", "ForeR", "_HR")):
        target = Vector(pose.pop(key, ((0, 0, 0), PLANT[side]))[1])
        pole = pose.pop("_P" + key[-1], ((0, 0, 0), POLE[side]))[1]
        aim = pose.pop("_A" + key[-1], None)
        weight = pose.pop("_W" + key[-1], ((0, 0, 0), (0, 0, 0)))[0][0]
        if aim is not None and weight > 0:  # (the hand where the arm's aimed, from the shoulder: an arc in a swing)
            yaw, pitch, ext = (math.radians(aim[0][0]), math.radians(aim[0][1]), aim[0][2])
            shoulder = kit.pose_matrices(BONES, pose)["Chest"] @ Vector(SHOULDER[side])
            aimed = shoulder + Vector((math.sin(yaw) * math.cos(pitch), -math.cos(yaw) * math.cos(pitch), math.sin(pitch))) * ext * ARM
            target = target.lerp(aimed, min(1.0, weight))
        for _ in range(3):  # (and never through the floor: a hand that would put its claws in it is lifted onto them)
            pose = kit.reach(BONES, pose, upper, lower, lambda m, t=target: t, pole)
            m = kit.pose_matrices(BONES, pose)
            sunk = 0.01 - min((m[lower] @ claw).z for claw in CLAWS[side])
            if sunk <= 0.001:
                break
            target = target + Vector((0, 0, sunk))
        targets[lower] = target
    root = pose.get("Root", ((0, 0, 0), (0, 0, 0)))
    hem = pose.get("Hem", ((0, 0, 0), (0, 0, 0)))
    hem_rot, hem_off = (hem, (0, 0, 0)) if not isinstance(hem[0], (tuple, list)) else hem
    pose["Hem"] = (tuple(hem_rot), (hem_off[0], hem_off[1], hem_off[2] - root[1][2]))
    m = kit.pose_matrices(BONES, dict(pose, Back=(0, 0, 0)))
    turned = m["Chest"].to_quaternion()
    back = Quaternion().slerp(turned.inverted(), 0.6)
    pose["Back"] = tuple(math.degrees(v) for v in back.to_euler("XYZ"))
    return pose, targets


def clip(length, tracks, periodic=False):
    """A clip `length` frames long (30 a second) from per-bone tracks (and the hands' targets: _HL/_HR, or the arms'
    aims _AL/_AR weighted by _WL/_WR; the elbows' poles _PL/_PR), baked to every frame, the arms bent onto the hands,
    the lantern and ribbon swung."""
    frames, worst, low = [], (0.0, 0), (0.0, 0)
    for f, p in kit.bake(tracks, length, periodic):
        p, targets = carry(p)
        m = kit.pose_matrices(BONES, p)
        for fore, t in targets.items():
            worst = max(worst, ((m[fore] @ Vector(BONES[fore][1]) - t).length, f))
            side = -1 if fore == "ForeL" else 1
            for claw in CLAWS[side]:
                low = min(low, ((m[fore] @ claw).z, f))
        frames.append((f, p))
    frames = dangle(frames, "Lantern", 1.1, 0.06, 0.55, periodic)
    frames = dangle(frames, "Ribbon", 1.5, 0.1, 0.7, periodic, floor=0.02)
    REACH[len(REACH)] = worst
    FLOOR[len(FLOOR)] = low
    return ("LINEAR", frames)


def held(pose, frame):
    """A whole pose as a key at `frame` for every bone (to start or end a clip on it)."""
    return {bone: [(frame, value)] for bone, value in pose.items()}


def keys(*parts):
    """Tracks merged: each a {bone: [(frame, value)]} (a later part's key on the same frame replaces an earlier's)."""
    merged = {}
    for p in parts:
        for bone, ks in p.items():
            merged.setdefault(bone, {}).update(dict(ks))
    return {bone: sorted(ks.items()) for bone, ks in merged.items()}


def every(f0, f1, fn):
    """Keys on every frame from f0 to f1 (inclusive) of fn(frame): a path drawn exactly (a planted hand)."""
    return [(f, fn(f)) for f in range(f0, f1 + 1)]


def aim(yaw, pitch, ext=0.96):
    """An arm's aim as a track value (_AL/_AR): yaw from straight ahead (degrees, + to its right), pitch above the
    level (degrees), how far it's stretched (1: straight). The hand goes there from the shoulder; _WL/_WR (0..1) say
    how much the aim rules the hand rather than its target (_HL/_HR)."""
    return ((yaw, pitch, ext), (0, 0, 0))


def w(value):
    return ((value, 0, 0), (0, 0, 0))


def up(dz=0.0, dy=0.0):
    return ((0, 0, 0), (0, dy, dz))


# Neutral: hunched over its knuckles, leaning a little, its head raised to look at you. Everything starts from or
# comes back to it (the walk's passing pose is near it, the recoveries end on it).
NEUTRAL = {"Root": up(), "Pivot": (8, 0, 0), "Spine": (0, 0, 0), "Chest": (4, 0, 0), "Head": (-12, 0, 0),
           "Hem": (0, 0, 0), "_HL": hand(*PLANT[-1]), "_HR": hand(*PLANT[1])}

# The sweep: wound (twisted away to its right, the right arm out wide and back, braced on its left knuckles)...
WOUND = {"Root": up(-0.06, 0.04), "Pivot": (10, -5, 20), "Spine": (0, 0, 4), "Chest": (2, 0, 16), "Head": (-8, 4, -32),
         "Hem": (0, 0, -8), "_HL": hand(-0.36, -1.02, FLOOR_HAND), "_HR": hand(1.48, 0.42, 0.86), "_AR": aim(120, -17), "_WR": w(1)}
# ...and through (unwound to its left, the arm flung across).
THROUGH = {"Root": up(-0.07, -0.04), "Pivot": (12, 4, -20), "Spine": (0, 0, -4), "Chest": (6, 0, -22), "Head": (-6, -2, 26),
           "Hem": (0, 0, 8), "_HL": hand(-0.36, -1.02, FLOOR_HAND), "_HR": hand(-1.32, -0.3, 0.72), "_AR": aim(-105, -22, 0.95), "_WR": w(1)}

# The slam: reared up, both arms high and back over its head...
REARED = {"Root": up(0.1, 0.1), "Pivot": (-24, 0, 0), "Spine": (0, 0, 0), "Chest": (-18, 0, 0), "Head": (-16, 0, 0),
          "Hem": (0, 0, 0), "_HL": hand(-0.64, 0.36, 2.98), "_HR": hand(0.64, 0.36, 2.98),
          "_PL": hand(-1, 0.2, -0.6), "_PR": hand(1, 0.2, -0.6)}
# ...and down: folded over, both fists on the floor far in front.
SLAMMED = {"Root": up(-0.1, -0.14), "Pivot": (28, 0, 0), "Spine": (0, 0, 0), "Chest": (12, 0, 0), "Head": (-52, 0, 0),
           "Hem": (-4, 0, 0), "_HL": hand(-0.4, -2.0, FLOOR_HAND), "_HR": hand(0.4, -2.0, FLOOR_HAND)}

ARMS_POLE = {"_PL": hand(*POLE[-1]), "_PR": hand(*POLE[1])}


def walk_pole(side, f):
    """The walk's elbows: out while the hand's planted, swung back as it's lifted through (an ape's, not a crab's)."""
    plant = 19 if side < 0 else 7
    u = ((f - plant) % 24) / 24.0
    swing = math.sin(math.pi * (u - 0.5) / 0.5) if u > 0.5 else 0.0
    return hand(*(Vector(POLE[side]) * (1 - swing) + Vector((side * 0.8, 0.8, 0.2)) * swing))


def walk_hand(side, f):
    """The knuckle-walk (24 frames): each hand planted for half the stride (sliding back under the body as it goes
    over it, at the speed it walks) and swung forward for the other half (lifted, reaching past, slapped down)."""
    plant = 19 if side < 0 else 7  # (the left hand comes down at frame 19, the right at 7)
    u = ((f - plant) % 24) / 24.0
    x, front, back = PLANT[side][0], -1.17, -0.45
    if u <= 0.5:  # planted: from in front to under the shoulder
        v = u / 0.5
        return hand(x, front + (back - front) * v, FLOOR_HAND)
    v = (u - 0.5) / 0.5  # swinging: lifted off behind, forward through the air, reaching past, down
    h00, h10, h01, h11 = 2 * v ** 3 - 3 * v ** 2 + 1, v ** 3 - 2 * v ** 2 + v, -2 * v ** 3 + 3 * v ** 2, v ** 3 - v ** 2
    speed = (back - front)  # (in stride units: it leaves and lands moving back, as the floor does under it)
    y = h00 * back + h10 * speed * 0.6 + h01 * front + h11 * speed * 0.6
    z = FLOOR_HAND + 0.24 * math.sin(math.pi * v ** 0.8)
    return hand(x - side * 0.05 * math.sin(math.pi * v), y, z)


ACTIONS = {
    # Idle (47 frames, a loop): a slow heavy breath (the hunch lifts, the shoulders rise, then it all sags back), its
    # weight rolling onto its right knuckles and back; its head turns to its right shoulder, towards Fast riding
    # behind, and back to you; the left claws lift and set down again, fretting. The lantern and ribbon sway after.
    "Idle": clip(47, {
        "Root": [(1, up()), (16, up(0.025)), (32, up(-0.012)), (42, up(-0.004))],
        "Pivot": [(1, (8, 0, 0)), (18, (6, 2.5, 1)), (34, (9.5, 1, 0)), (44, (8.5, 0, -0.5))],
        "Spine": [(1, (0, 0, 0)), (20, (0, 1, 0)), (38, (0, 0, 0))],
        "Chest": [(1, (4, 0, 0)), (17, (-1, 0.5, 2)), (33, (7, 0, 0)), (43, (5, 0, -1))],
        "Head": [(1, (-12, 0, 0)), (10, (-11, -1, 6)), (21, (-4, -4, 30)), (29, (-6, -3, 27)), (38, (-13, 1, -4)), (44, (-12, 0, -1))],
        "Hem": [(1, (0, 0, 0)), (22, (0, 1.5, 1)), (40, (0, -0.5, 0))],
        "_HL": [(1, hand(*PLANT[-1])), (24, hand(*PLANT[-1])), (28, hand(-0.5, -1.0, 0.31)), (31, hand(-0.48, -1.02, 0.25)),
                (34, hand(-0.46, -1.0, FLOOR_HAND)), (47, hand(*PLANT[-1]))],
        "_HR": [(1, hand(*PLANT[1])), (47, hand(*PLANT[1]))],
    }, periodic=True),
    # Walk (24 frames, a loop: two hand-falls): it walks on its knuckles, each hand reaching forward and slapped down
    # in turn (the left at 19, the right at 7), the body vaulting over the planted arm: highest when that arm is
    # upright (frames 1 and 13: the loop starts on its passing pose), dropping hard after each hand-fall, rolling onto
    # the planted side, the shoulders turning with the reaching arm; the head bobs after the body and keeps on you;
    # the robe's hem sways after the hips.
    "Walk": clip(24, {
        "Root": [(1, up(0.03)), (9, up(-0.045)), (13, up(0.03)), (21, up(-0.045))],
        "Pivot": [(1, (9, 0, 0)), (8, (12, 3, -2)), (13, (9, 0, 0)), (20, (12, -3, 2))],
        "Spine": [(2, (0, 0, 0)), (10, (0, 2, 0)), (14, (0, 0, 0)), (22, (0, -2, 0))],
        "Chest": [(1, (4, 0, 1)), (7, (6, 2, -7)), (13, (4, 0, -1)), (19, (6, -2, 7))],
        "Head": [(3, (-13, 0, 0)), (11, (-7, -2, 6)), (15, (-13, 0, 0)), (23, (-7, 2, -6))],
        "Hem": [(3, (0, -2, 2)), (11, (2, 3, -2)), (15, (0, 2, -2)), (23, (2, -3, 2))],
        "_HL": every(1, 24, lambda f: walk_hand(-1, f)),
        "_HR": every(1, 24, lambda f: walk_hand(1, f)),
        "_PL": every(1, 24, lambda f: walk_pole(-1, f)),
        "_PR": every(1, 24, lambda f: walk_pole(1, f)),
    }, periodic=True),
    # SweepWindup (21 frames; 0.9 s): a hitch the other way first (a lean onto its right, the left hand shuffled in
    # to brace), then it twists away to its right, hips first, the chest after, the right arm hauled out wide and
    # back (elbow leading, the hand dragging); its weight settles onto its left knuckles; the head stays on you
    # (turning against the body). Held there, straining, the twist creeping on.
    "SweepWindup": clip(21, keys(held(NEUTRAL, 1), held(ARMS_POLE, 1), {
        "Root": [(5, up(-0.03, 0.0)), (15, up(-0.05, 0.03)), (22, WOUND["Root"])],
        "Pivot": [(5, (10, 3, -5)), (14, (10, -4, 16)), (22, WOUND["Pivot"])],
        "Spine": [(6, (0, 0, -2)), (15, (0, 0, 4)), (22, WOUND["Spine"])],
        "Chest": [(6, (6, 1, -7)), (16, (2, 0, 12)), (22, WOUND["Chest"])],
        "Head": [(7, (-10, -2, 8)), (17, (-8, 4, -28)), (22, WOUND["Head"])],
        "Hem": [(7, (0, 0, 3)), (17, (0, 0, -6)), (22, WOUND["Hem"])],
        "_HL": [(3, hand(*PLANT[-1])), (5, hand(-0.42, -1.04, 0.32)), (8, WOUND["_HL"])],
        "_HR": [(4, hand(0.5, -0.92, 0.26)), (9, hand(1.0, -0.55, 0.62)), (15, hand(1.42, 0.3, 0.9)), (22, WOUND["_HR"])],
        "_AR": [(5, aim(70, -62, 0.72)), (10, aim(92, -50, 0.75)), (15, aim(108, -26, 0.92)), (18, aim(115, -19, 0.96)), (22, WOUND["_AR"])],
        "_WR": [(1, w(0)), (5, w(0)), (10, w(1)), (22, w(1))],
        "_PR": [(4, hand(*POLE[1])), (12, hand(0.6, 0.6, -0.6)), (22, hand(0.2, 0.5, -1))],
    })),
    # Sweep (7 frames; 0.3 s, the blow lands at 0.55: frame 4.85): the hips unwind first, the chest after, the arm
    # last, whipped round low across the front: straight ahead at the blow, and flung on round to its left, the
    # body thrown after it; the head swings with it late.
    "Sweep": clip(7, keys(held(WOUND, 1), {
        "Root": [(4, up(-0.08, -0.02)), (8, THROUGH["Root"])],
        "Pivot": [(3, (11, 0, 6)), (5, (12, 3, -12)), (8, THROUGH["Pivot"])],
        "Spine": [(4, (0, 0, 0)), (8, THROUGH["Spine"])],
        "Chest": [(2, (2, 0, 14)), (4, (4, 0, 0)), (6, (6, 0, -18)), (8, THROUGH["Chest"])],
        "Head": [(3, (-8, 2, -22)), (5, (-6, 0, 0)), (8, THROUGH["Head"])],
        "Hem": [(4, (0, 0, -4)), (8, THROUGH["Hem"])],
        "_AR": [(2, aim(113, -18, 0.97)), (3, aim(80, -20, 0.98)), (4, aim(38, -22, 0.98)), (5, aim(-4, -24, 0.98)),
                (6, aim(-48, -24, 0.97)), (7, aim(-84, -23, 0.96)), (8, THROUGH["_AR"])],
        "_WR": [(8, w(1))],
        "_PR": [(1, hand(0.2, 0.5, -1)), (4, hand(0, 1, -0.2)), (8, hand(-0.2, 1, 0.2))],
    })),
    # SweepRecover (21 frames; 0.9 s): carried on past by the weight of the arm (the body drags round after it),
    # the hand slapping down onto the floor at its left; then it hauls the arm back round in front, lifted, and plants
    # it under its right shoulder; the body untwists past centre and settles; the left hand shuffles back out.
    "SweepRecover": clip(21, keys(held(THROUGH, 1), {
        "Root": [(4, up(-0.09, -0.05)), (12, up(-0.04, -0.02)), (22, NEUTRAL["Root"])],
        "Pivot": [(4, (13, 6, -26)), (11, (12, 2, -10)), (17, (8, -1, 4)), (22, NEUTRAL["Pivot"])],
        "Spine": [(5, (0, 0, -6)), (18, (0, 0, 2)), (22, NEUTRAL["Spine"])],
        "Chest": [(5, (8, 0, -28)), (12, (6, 0, -8)), (18, (3, 0, 4)), (22, NEUTRAL["Chest"])],
        "Head": [(4, (-4, -4, 30)), (8, (2, -2, 14)), (14, (-10, 0, -6)), (22, NEUTRAL["Head"])],
        "Hem": [(6, (0, 0, 12)), (13, (0, 0, -3)), (22, NEUTRAL["Hem"])],
        "_HL": [(14, THROUGH["_HL"]), (17, hand(-0.44, -1.02, 0.3)), (20, hand(*PLANT[-1])), (22, hand(*PLANT[-1]))],
        "_AR": [(4, aim(-122, -40, 0.97)), (6, aim(-118, -60, 0.98)), (9, aim(-106, -60, 0.95)), (13, aim(-60, -55, 0.8))],
        "_WR": [(9, w(1)), (13, w(0)), (22, w(0))],
        "_HR": [(9, hand(-0.9, -0.6, 0.25)), (13, hand(-0.3, -1.15, 0.46)), (15, hand(0.44, -1.18, 0.4)), (17, hand(0.48, -1.04, FLOOR_HAND)), (22, hand(*PLANT[1]))],
        "_PR": [(1, hand(-0.2, 0.5, 1)), (6, hand(0.2, 0.8, 0.6)), (14, hand(1, 0.35, 0)), (22, hand(*POLE[1]))],
    })),
    # SlamWindup (26 frames; 1.1 s): it sinks onto its knuckles first (a crouch to spring from, its head low), then
    # rears up: the hips lift and tip back, the chest after, the arms torn up off the floor and swung high over its
    # head (the hands trailing, the elbows out); the head thrown back last. Held, straining, arching further back
    # (Fast clinging on behind).
    "SlamWindup": clip(26, keys(held(NEUTRAL, 1), held(ARMS_POLE, 1), {
        "Root": [(6, up(-0.09, 0.02)), (13, up(0.04, 0.06)), (18, up(0.11, 0.1)), (27, REARED["Root"])],
        "Pivot": [(6, (16, 0, 0)), (13, (-6, 0, 0)), (18, (-22, 0, 0)), (23, (-21, 0, 0)), (27, (-24, 0, 0))],
        "Spine": [(7, (0, 0, 0)), (27, (0, 0, 0))],
        "Chest": [(7, (10, 0, 0)), (14, (-6, 0, 0)), (19, (-17, 0, 0)), (23, (-15, 0, 0)), (27, (-18, 0, 0))],
        "Head": [(8, (-8, 0, 0)), (15, (-12, 0, 0)), (20, (-20, 0, 0)), (27, (-16, 0, 0))],
        "Hem": [(8, (2, 0, 0)), (16, (-3, 0, 0)), (27, (0, 0, 0))],
        "_HL": [(4, hand(-0.44, -0.96, FLOOR_HAND)), (8, hand(-0.46, -1.02, 0.3)), (12, hand(-0.72, -1.0, 1.35)),
                (16, hand(-0.7, -0.35, 2.55)), (20, hand(-0.62, 0.2, 3.02)), (27, hand(-0.64, 0.36, 2.98))],
        "_HR": [(4, hand(0.44, -0.96, FLOOR_HAND)), (8, hand(0.46, -1.02, 0.3)), (12, hand(0.72, -1.0, 1.35)),
                (16, hand(0.7, -0.35, 2.55)), (20, hand(0.62, 0.2, 3.02)), (27, hand(0.64, 0.36, 2.98))],
        "_PL": [(8, hand(*POLE[-1])), (14, hand(-1, 0, -0.3)), (27, REARED["_PL"])],
        "_PR": [(8, hand(*POLE[1])), (14, hand(1, 0, -0.3)), (27, REARED["_PR"])],
    })),
    # Slam (6 frames; 0.25 s, the blow lands at 0.7: frame 5.2): the hips throw forward first, the chest folds after,
    # the arms come over last (whipped), both fists brought down on the floor far in front at frame 5; the body
    # sinks deepest just after (6), the head snapping down, then a small rebound.
    "Slam": clip(6, keys(held(REARED, 1), {
        "Pivot": [(1, (-24, 0, 0)), (2, (-12, 0, 0)), (3, (10, 0, 0)), (4, (24, 0, 0)), (5, (30, 0, 0)), (6, (32, 0, 0)), (7, SLAMMED["Pivot"])],
        "Chest": [(1, (-18, 0, 0)), (2, (-18, 0, 0)), (3, (-6, 0, 0)), (4, (6, 0, 0)), (5, (13, 0, 0)), (6, (16, 0, 0)), (7, SLAMMED["Chest"])],
        "Head": [(1, (-16, 0, 0)), (3, (-26, 0, 0)), (5, (-58, 0, 0)), (6, (-44, 0, 0)), (7, SLAMMED["Head"])],
        "Root": [(1, REARED["Root"]), (3, up(0.04, -0.04)), (5, up(-0.11, -0.14)), (6, up(-0.14, -0.16)), (7, SLAMMED["Root"])],
        "Hem": [(1, (0, 0, 0)), (5, (-6, 0, 0)), (7, SLAMMED["Hem"])],
        "_HL": [(1, hand(-0.64, 0.36, 2.98)), (2, hand(-0.66, 0.36, 3.0)), (3, hand(-0.6, -0.55, 2.75)), (4, hand(-0.48, -1.55, 1.5)),
                (5, SLAMMED["_HL"]), (7, SLAMMED["_HL"])],
        "_HR": [(1, hand(0.64, 0.36, 2.98)), (2, hand(0.66, 0.36, 3.0)), (3, hand(0.6, -0.55, 2.75)), (4, hand(0.48, -1.55, 1.5)),
                (5, SLAMMED["_HR"]), (7, SLAMMED["_HR"])],
        "_PL": [(1, REARED["_PL"]), (4, hand(-1, 0.3, 0.2)), (7, hand(*POLE[-1]))],
        "_PR": [(1, REARED["_PR"]), (4, hand(1, 0.3, 0.2)), (7, hand(*POLE[1]))],
    })),
    # SlamRecover (28 frames; 1.2 s: the punish window): bowed over its fists, heaving for breath twice, shaking its
    # head; then it pushes itself back up (the hips first, overshooting back, settling), dragging its knuckles back
    # in and walking them home, right then left.
    "SlamRecover": clip(28, keys(held(SLAMMED, 1), {
        "Root": [(4, up(-0.12, -0.15)), (8, up(-0.07, -0.13)), (12, up(-0.12, -0.15)), (16, up(-0.09, -0.13)),
                 (21, up(-0.03, -0.06)), (24, up(0.015)), (29, NEUTRAL["Root"])],
        "Pivot": [(4, (29, 0, 0)), (8, (25, 0, 0)), (12, (29, 0, 0)), (16, (25, 0, 0)), (20, (16, 0, 0)), (23, (5, 0, 0)), (26, (3, 0, 0)), (29, NEUTRAL["Pivot"])],
        "Chest": [(4, (14, 0, 0)), (8, (9, 0, 0)), (12, (14, 0, 0)), (17, (11, 0, 0)), (22, (2, 0, 0)), (26, (6, 0, 0)), (29, NEUTRAL["Chest"])],
        "Head": [(3, (-48, 0, 0)), (7, (-48, 0, 14)), (10, (-50, 0, -14)), (13, (-46, 0, 8)), (16, (-50, 0, 0)),
                 (22, (-6, 0, 0)), (26, (-15, 0, 0)), (29, NEUTRAL["Head"])],
        "Hem": [(6, (-2, 0, 0)), (20, (-4, 0, 0)), (24, (3, 0, 0)), (29, NEUTRAL["Hem"])],
        "_HR": [(16, SLAMMED["_HR"]), (18, hand(0.44, -1.78, 0.4)), (20, hand(0.46, -1.42, FLOOR_HAND)), (22, hand(0.46, -1.3, FLOOR_HAND)),
                (24, hand(0.47, -1.12, 0.34)), (26, hand(0.46, -1.0, FLOOR_HAND)), (29, hand(*PLANT[1]))],
        "_HL": [(16, SLAMMED["_HL"]), (18, hand(-0.42, -1.9, FLOOR_HAND)), (20, hand(-0.44, -1.62, 0.4)), (22, hand(-0.46, -1.24, FLOOR_HAND)), (24, hand(-0.46, -1.13, FLOOR_HAND)),
                (26, hand(-0.47, -1.12, 0.34)), (28, hand(-0.46, -1.0, FLOOR_HAND)), (29, hand(*PLANT[-1]))],
    })),
    # Howl (28 frames; 1.2 s, when Fast is gone): it turns to look over its right shoulder for what's no longer
    # there, gathers itself (hunched, arms drawn in), then rears up and howls: arms flung wide, head thrown back,
    # trembling; and comes down hard on its knuckles, berserk.
    "Howl": clip(28, keys(held(NEUTRAL, 1), held(ARMS_POLE, 1), {
        "Root": [(5, up(0.01, 0.02)), (9, up(-0.07, 0.02)), (13, up(0.1, 0.08)), (17, up(0.08, 0.08)), (21, up(0.1, 0.08)),
                 (25, up(-0.06, -0.04)), (29, NEUTRAL["Root"])],
        "Pivot": [(5, (6, -2, 10)), (9, (16, 0, 2)), (13, (-22, 0, 0)), (17, (-19, 0, 0)), (21, (-23, 0, 0)), (25, (16, 0, 0)), (29, NEUTRAL["Pivot"])],
        "Spine": [(1, (0, 0, 0)), (29, (0, 0, 0))],
        "Chest": [(5, (4, -2, 14)), (10, (14, 0, 2)), (14, (-14, 0, 0)), (16, (-11, 0, 0)), (18, (-15, 0, 0)), (20, (-11, 0, 0)), (22, (-14, 0, 0)),
                  (26, (10, 0, 0)), (29, NEUTRAL["Chest"])],
        "Head": [(4, (-8, -6, 42)), (7, (-6, -6, 46)), (10, (10, 0, 6)), (15, (-44, 0, 0)), (17, (-40, 0, 2)), (19, (-46, 0, -2)), (21, (-41, 0, 1)),
                 (23, (-45, 0, 0)), (27, (-6, 0, 0)), (29, NEUTRAL["Head"])],
        "Hem": [(10, (2, 0, 0)), (15, (-3, 0, 0)), (26, (3, 0, 0)), (29, NEUTRAL["Hem"])],
        "_HL": [(7, hand(*PLANT[-1])), (10, hand(-0.3, -0.85, 0.5)), (14, hand(-1.2, -0.42, 2.4)), (18, hand(-1.24, -0.36, 2.48)),
                (22, hand(-1.18, -0.44, 2.42)), (25, hand(-0.5, -1.1, 0.5)), (26, hand(-0.48, -1.06, FLOOR_HAND)), (29, hand(*PLANT[-1]))],
        "_HR": [(5, hand(*PLANT[1])), (10, hand(0.3, -0.85, 0.5)), (14, hand(1.2, -0.42, 2.4)), (18, hand(1.24, -0.36, 2.48)),
                (22, hand(1.18, -0.44, 2.42)), (25, hand(0.5, -1.1, 0.5)), (26, hand(0.48, -1.06, FLOOR_HAND)), (29, hand(*PLANT[1]))],
        "_PL": [(10, hand(-1, 0.6, 0)), (14, hand(-0.4, 0.3, -1)), (22, hand(-0.4, 0.3, -1)), (26, hand(*POLE[-1]))],
        "_PR": [(10, hand(1, 0.6, 0)), (14, hand(0.4, 0.3, -1)), (22, hand(0.4, 0.3, -1)), (26, hand(*POLE[1]))],
    })),
    # Stagger (27 frames; played over 1.4 s, and over other times): knocked back, the head whipped back, the arms
    # flung up loose; it reels over to its right, its right hand slapped down behind to catch itself, lurches back
    # forward (overshooting), and gathers its knuckles back under it.
    "Stagger": clip(27, keys(held(NEUTRAL, 1), held(ARMS_POLE, 1), {
        "Root": [(4, up(0.04, 0.16)), (9, up(-0.06, 0.18)), (14, up(-0.08, 0.02)), (20, up(-0.02)), (28, NEUTRAL["Root"])],
        "Pivot": [(4, (-16, 6, 0)), (9, (-6, 10, -8)), (14, (16, -2, 2)), (20, (6, 0, 0)), (28, NEUTRAL["Pivot"])],
        "Spine": [(1, (0, 0, 0)), (28, (0, 0, 0))],
        "Chest": [(3, (-6, 0, 0)), (6, (-14, 4, 4)), (11, (-4, 6, -6)), (16, (12, -2, 2)), (21, (3, 0, 0)), (28, NEUTRAL["Chest"])],
        "Head": [(3, (-30, 0, 0)), (7, (-20, 6, 10)), (12, (-2, 4, -12)), (17, (-20, 0, 4)), (22, (-10, 0, 0)), (28, NEUTRAL["Head"])],
        "Hem": [(6, (4, 4, 0)), (12, (-3, -3, 0)), (18, (2, 0, 0)), (28, NEUTRAL["Hem"])],
        "_HL": [(4, hand(-0.55, -0.95, 1.0)), (8, hand(-0.7, -0.95, 0.75)), (13, hand(-0.52, -1.18, 0.32)), (15, hand(-0.5, -1.15, FLOOR_HAND)),
                (21, hand(-0.48, -1.1, FLOOR_HAND)), (24, hand(-0.47, -1.04, 0.3)), (27, hand(*PLANT[-1])), (28, hand(*PLANT[-1]))],
        "_HR": [(4, hand(0.58, -0.92, 1.05)), (7, hand(0.92, -0.32, 0.4)), (9, hand(0.98, -0.12, FLOOR_HAND)), (14, hand(0.98, -0.12, FLOOR_HAND)),
                (18, hand(0.6, -0.7, 0.42)), (21, hand(0.48, -1.04, FLOOR_HAND)), (28, hand(*PLANT[1]))],
    })),
    # Death (25 frames; 1.1 s, then it dissolves): a jolt; its right hand reaches up and back over its shoulder for
    # what used to ride there, and finds nothing; then it folds: the body sinks, tips forward and crashes down onto its
    # front (a bounce), the arms falling slack either side, the head last.
    "Death": clip(25, keys(held(NEUTRAL, 1), held(ARMS_POLE, 1), {
        "Root": [(4, up(0.03, 0.05)), (9, up(0.05, 0.05)), (13, up(-0.1, -0.02)), (17, up(-0.27, -0.16)), (20, up(-0.22, -0.15)), (26, up(-0.27, -0.16))],
        "Pivot": [(4, (-6, 0, 0)), (9, (-10, -4, 12)), (13, (18, 3, 4)), (17, (52, 5, 0)), (20, (45, 4, 0)), (26, (52, 5, 0))],
        "Spine": [(1, (0, 0, 0)), (26, (0, 0, 0))],
        "Chest": [(4, (-8, 0, 0)), (9, (-12, -6, 16)), (14, (12, 2, 4)), (18, (20, 4, 0)), (21, (12, 4, 0)), (26, (20, 4, 0))],
        "Head": [(3, (-26, 0, 0)), (9, (-16, -10, 48)), (14, (-6, 0, 20)), (18, (6, 0, 10)), (21, (-14, 0, 10)), (26, (2, 4, 16))],
        "Hem": [(8, (2, 0, 0)), (13, (-2, 0, 0)), (17, (16, 0, 0)), (20, (12, 0, 0)), (26, (18, 0, 0))],
        "_HL": [(3, hand(-0.48, -0.92, 0.36)), (9, hand(-0.6, -0.92, 0.5)), (13, hand(-0.85, -1.05, 0.45)), (17, hand(-1.25, -1.35, FLOOR_HAND)),
                (20, hand(-1.25, -1.35, 0.28)), (26, hand(-1.27, -1.37, FLOOR_HAND))],
        "_HR": [(3, hand(0.48, -0.85, 0.5)), (6, hand(0.66, -0.45, 1.5)), (10, hand(0.55, 0.3, 2.1)), (13, hand(0.9, -0.45, 1.05)),
                (17, hand(1.3, -1.15, FLOOR_HAND)), (20, hand(1.3, -1.15, 0.28)), (26, hand(1.32, -1.17, FLOOR_HAND))],
        "_PR": [(3, hand(*POLE[1])), (8, hand(1, -0.2, -0.6)), (13, hand(1, 0.3, 0)), (26, hand(1, 0.3, 0))],
    })),
}


def robe_weights(ob):
    """The robe skinned smoothly: the lower robe on Hem, the middle on Spine, the top (under the hump) on Chest."""
    hem, spine, chest = (ob.vertex_groups.new(name=n) for n in ("Hem", "Spine", "Chest"))
    for v in ob.data.vertices:
        z = v.co.z
        low = min(1.0, max(0.0, (0.82 - z) / 0.5))    # (1 below 0.32, 0 above 0.82)
        high = min(1.0, max(0.0, (z - 0.98) / 0.26))  # (0 below 0.98, 1 above 1.24)
        mid = 1.0 - low - high
        for group, w in ((hem, low), (spine, mid), (chest, high)):
            if w > 1e-4:
                group.add([v.index], w, "REPLACE")


def build():
    robe, dark = kit.mat("SouredGrey", "#7c8291"), kit.mat("SouredGreyDark", "#50545f")
    eye, claw = kit.mat("SouredEye_Glow", "#c8f07a"), kit.mat("Claw", "#d6cdb8")
    ribbon = kit.mat("Ribbon", "#8a3a34")
    # (the right hand's claws, the same bone-white, by a name of their own: each hand's claws are then the far tip
    # of a swing trail on its forearm, the right's HoldClaw, the left's Claw)
    claws = {-1: claw, 1: kit.mat("HoldClaw", "#d6cdb8")}
    metal = kit.mat("HeapMetal", "#34313a")

    def robe_shape(bm):
        kit.cone(9, 0.26 * S, 0.12 * S, 0.62 * S)(bm)
        rim = sorted((v for v in bm.verts if v.co.z < -0.29 * S and v.co.length > 0.05), key=lambda v: math.atan2(v.co.y, v.co.x))
        for i, v in enumerate(rim):
            v.co.z += 0.07 * S if i % 2 else 0.0

    robe_part = part("Robe", robe_shape, robe, M(s((0, -0.04, 0.33)), rot=(16, 0, 0)), smooth=True)
    robe_weights(robe_part)
    parts = [
        robe_part,
        part("Back", kit.sphere(10, 7), robe, M(s((0, -0.12, 0.68)), rot=(30, 0, 0), scale=s((0.23, 0.2, 0.2))), smooth=True, group="Chest"),
        part("Head", kit.sphere(9, 6, 0.13 * S), dark, M(s((0, -0.33, 0.86))), smooth=True, group="Head"),
        part("Hood", kit.cone(9, 0.17 * S, 0.03 * S, 0.34 * S), robe, M(s((0, -0.27, 0.95)), rot=(-48, 0, 0)), smooth=True, group="Head"),
        # The shroud they share, draped over Hold's back where Fast rides.
        part("Shroud", kit.sphere(9, 6), dark, M(s((0, 0.08, 0.7)), rot=(10, 0, 0), scale=s((0.27, 0.16, 0.12))), smooth=True, jitter=0.02, group="Chest"),
    ]
    for side, arm, fore in ((-1, "ArmL", "ForeL"), (1, "ArmR", "ForeR")):
        parts.append(part("Eye", kit.sphere(8, 6, 0.025 * S), eye, M(s((side * 0.05, -0.45, 0.85))), smooth=True, group="Head"))
        shoulder, elbow, wrist_hand = SHOULDER[side], ELBOW[side], HAND[side]
        # The arm in two (shoulder to elbow, elbow to hand), the elbow a knob of robe over the joint.
        parts.append(part("Arm", kit.tube([shoulder, elbow], [0.05 * S, 0.036 * S], seg=7), robe, group=arm))
        parts.append(part("Elbow", kit.sphere(8, 6, 0.039 * S), robe, M(elbow), smooth=True, group=arm))
        parts.append(part("Arm", kit.tube([elbow, wrist_hand], [0.036 * S, 0.03 * S], seg=7), robe, group=fore))
        for k in (-1, 0, 1):
            tip = (wrist_hand[0] + k * 0.035 * S, wrist_hand[1] - 0.1 * S, wrist_hand[2] - 0.08 * S)
            parts.append(part("Claw", kit.cone(4, 0.016 * S, 0.0, 0.12 * S), claws[side], kit.along(wrist_hand, tip), group=fore))
    # The ribbon round Hold's right wrist, its ends hanging (its other end was round Fast's).
    parts.append(part("RibbonBand", kit.cylinder(7, 0.04 * S, 0.03 * S), ribbon, M(WRIST, rot=(20, 0, 0)), group="ForeR"))
    parts.append(part("RibbonEnd", kit.tube([WRIST, (WRIST[0] + 0.05, WRIST[1] + 0.1, WRIST[2] - 0.2), RIBBON_TIP],
                                            [0.02, 0.018, 0.012], seg=4), ribbon, group="Ribbon"))
    # Its belt lantern, like every Soured's (on a cord: it swings).
    lantern_glass = kit.mat("SouredLantern_Glow", "#b6f06a")
    parts += [
        part("BeltLanternCord", kit.cube(), metal, M(s((0.2, 0.02, 0.5)), rot=(0, -15, 0), scale=s((0.012, 0.012, 0.1))), group="Lantern"),
        part("BeltLanternTop", kit.cone(4, 0.06 * S, 0.01 * S, 0.05 * S), metal, M(s((0.21, 0.02, 0.43)), rot=(0, 0, 45)), group="Lantern"),
        part("BeltLanternGlass", kit.cube(), lantern_glass, M(s((0.21, 0.02, 0.36)), scale=s((0.075, 0.075, 0.1))), group="Lantern"),
        part("BeltLanternBase", kit.cube(), metal, M(s((0.21, 0.02, 0.3)), scale=s((0.09, 0.09, 0.015))), group="Lantern"),
    ]
    mesh = kit.join(parts, "Hold")
    armature = kit.rig("HoldRig", BONES)
    kit.skin(mesh, armature)
    kit.animate(armature, ACTIONS)
    for (name, _), worst, low in zip(ACTIONS.items(), REACH.values(), FLOOR.values()):
        if worst[0] > 0.02:
            print(f"[unparted] {name}: the hands miss their marks by up to {worst[0] * 100:.0f} cm (frame {worst[1]})")
        if low[0] < -0.02:
            print(f"[unparted] {name}: a claw goes {-low[0] * 100:.0f} cm into the floor (frame {low[1]})")
    return [armature, mesh]
