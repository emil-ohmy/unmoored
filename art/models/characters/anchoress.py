"""The Antediluvian Anchoress: a Laden who would not let the Ferryman go. She hauled his ferry's anchor up from the
riverbed and chained it to herself, so he would have to come back for it (her lantern hung on it, for him to see by), and kept vigil for him in the drowned chapel
until she Soured around it. Half again a Laden's size, veiled in old linen, the anchor held upright in front of her
like a shield (a cross over a crescent). Its chain is fastened at her waist with the Ferryman's hook, stolen off the
anchor line: the game draws the chain (from the hook to the anchor's ring) and flies the anchor itself when she lets
it out on the chain (the Anchor bone, which carries nothing but the anchor and its lantern).

Clips: Vigil (kneeling over the anchor, head bowed: before the fight), Idle, Walk (the anchor dragged), GuardUp /
GuardWalk / Block (the anchor lifted in front of her face), Windup / Strike / Recover (the Anchor Drop: raised high,
brought down in front), SweepWindup / Sweep / SweepRecover (the Chain Sweep: the anchor let out and swung round),
CastWindup / Cast / Haul (Cast and Haul: hefted back, thrown, then hauled in hand over hand), Reel (pulling it home),
Stagger (the anchor slips from her hands), Death. Phase two: Rise (the anchor lifted to the sky and planted: the floor
starts to break), YankWindup / Yank / YankRecover (the Hook Yank: the anchor planted, the Ferryman's hook whirled on its
line, thrown, and hauled back with whoever it caught). UndertowWindup / Undertow / UndertowRecover (down on her knees
over the anchor, praying while the water rises, and up again).

How it's animated (as the Laden): each bone keyed on its own timing (kit.bake: tracks). The body leads (hips, spine,
then chest), the head follows a few frames late. The anchor is keyed where it is in the world (by its grip, or by its
lowest point on the floor), not through her: it's the heavy thing, and she moves round it (it lags behind her heaves,
drags her after it, lands with a thud and a bounce). The arms aren't keyed: every frame they're bent (kit.reach) onto
what the hands hold: the anchor's shank, the chain (from the hook at her waist towards where the game flies the
anchor), or nothing (free, where the clip puts them). The loose things are simulated, not keyed: the lantern swings
on its cord from the anchor's stock (a pendulum), the Ferryman's hook swings at her waist, the veil's drape trails
on her back (springs); so they lag, overshoot and settle on their own. The skirt of her robe stays on the floor when
she crouches or kneels (it pools; the body sinks into it).
"""
import math
import os

from mathutils import Euler, Matrix, Vector

M, part = kit.M, kit.part  # `kit` is injected by build_model.py


def _elbow(shoulder, hand, pole, length):
    s, h = Vector(shoulder), Vector(hand)
    n = (h - s).normalized()
    p = Vector(pole) - n * Vector(pole).dot(n)
    p.normalize()
    half = (h - s).length / 2
    e = (s + h) / 2 + p * math.sqrt(length * length - half * half)
    return tuple(round(c, 4) for c in e)


ARM = 0.45  # upper arm and forearm, each
SHOULDER_L, HAND_L = (-0.5, -0.1, 1.42), (-0.2, -0.72, 1.2)
SHOULDER_R, HAND_R = (0.5, -0.1, 1.42), (0.2, -0.72, 1.2)
ELBOW_L = _elbow(SHOULDER_L, HAND_L, (-1, 0.4, -0.8), ARM)
ELBOW_R = _elbow(SHOULDER_R, HAND_R, (1, 0.4, -0.8), ARM)
CLAW_TIP_L, CLAW_TIP_R = (-0.1, -0.84, 1.18), (0.1, -0.84, 1.18)

BONES = {
    "Root": ((0, 0, 0), (0, 0, 0.5), None),                      # (stays put: the anchor is keyed from it)
    "Hips": ((0, 0, 0.3), (0, 0, 0.5), "Root"),
    "Hem": ((0, 0, 0.6), (0, 0.05, 0.05), "Hips"),               # her robe's skirt, kept on the floor
    "Spine": ((0, 0, 0.5), (0, -0.1, 1.15), "Hips"),             # the robe, the waist chain
    "Chest": ((0, -0.1, 1.15), (0, -0.2, 1.5), "Spine"),         # the hump, the shoulders
    "Head": ((0, -0.2, 1.5), (0, -0.35, 1.95), "Chest"),
    "Drape": ((0, 0.02, 1.74), (0, 0.3, 0.96), "Chest"),         # the veil's drape down her back (simulated)
    "Arms": ((0, -0.1, 1.42), (0, -0.62, 1.2), "Chest"),         # (no mesh: follows her hands, where the hook's line leaves them)
    "ArmL": (SHOULDER_L, ELBOW_L, "Chest"),
    "ForeL": (ELBOW_L, HAND_L, "ArmL"),
    "ArmR": (SHOULDER_R, ELBOW_R, "Chest"),
    "ForeR": (ELBOW_R, HAND_R, "ArmR"),
    "Anchor": ((0, -0.8, 1.2), (0, -0.8, 0.3), "Root"),          # the anchor, pivoting at her grip, keyed where it is
    "Lantern": ((0.56, -0.82, 1.39), (0.56, -0.82, 1.08), "Anchor"),  # on its cord from the stock (simulated)
    "Hook": ((0, -0.62, 1.0), (0, -0.62, 0.8), "Spine"),         # the Ferryman's hook at her waist (simulated; hidden while thrown)
}

# ------------------------------------------------------------------ the anchor, keyed where it is

ANCHOR_HEAD = Vector((0, -0.8, 1.2))
ANCHOR_CROWN = Vector((0, -0.8, 0.13))


def _anchor_points():
    y = -0.8
    pts = []
    for i in range(12):
        a = 2 * math.pi * i / 12
        for dy in (-0.035, 0.035):
            pts.append(Vector((0.165 * math.cos(a), y + dy, 1.66 + 0.165 * math.sin(a))))
    for sx in (-1, 1):
        for dy in (-0.05, 0.05):
            for dz in (-0.055, 0.055):
                pts.append(Vector((sx * 0.5, y + dy, 1.44 + dz)))
        c = Vector((sx * 0.52, y, 1.44))
        pts += [c + Vector(d) for d in ((0.075, 0, 0), (-0.075, 0, 0), (0, 0.075, 0), (0, -0.075, 0), (0, 0, 0.075), (0, 0, -0.075))]
        fluke = M((sx * 0.7, y, 0.74), rot=(0, sx * 32, 0))
        for dx in (-0.1, 0.1):
            for dy in (-0.03, 0.03):
                for dz in (-0.15, 0.15):
                    pts.append(fluke @ Vector((dx, dy, dz)))
    for x, z, r in ((-0.66, 0.66, 0.06), (-0.52, 0.36, 0.075), (-0.3, 0.18, 0.085), (0, 0.12, 0.095), (0.3, 0.18, 0.085),
                    (0.52, 0.36, 0.075), (0.66, 0.66, 0.06)):
        pts += [Vector((x, y + r, z)), Vector((x, y - r, z)), Vector((x, y, z + r)), Vector((x, y, z - r))]
    for z in (0.05, 1.55):
        pts += [Vector((0.075, y, z)), Vector((-0.075, y, z)), Vector((0, y + 0.075, z)), Vector((0, y - 0.075, z))]
    c = ANCHOR_CROWN
    pts += [c + Vector(d) for d in ((0.144, 0, 0), (-0.144, 0, 0), (0, 0.096, 0), (0, -0.096, 0), (0, 0, -0.096))]
    for dx in (-0.07, 0.07):
        for dy in (-0.07, 0.07):
            pts.append(Vector((0.56 + dx, -0.82 + dy, 1.045)))
    return pts


ANCHOR_POINTS = _anchor_points()


def _turn(rot):
    return Euler([math.radians(a) for a in rot]).to_matrix()


def grip_at(rot, at):
    """The anchor turned `rot` (degrees about world X, Y, Z: +X tips its crown back towards her, its top away),
    with her grip on its shank at `at`."""
    return (tuple(rot), tuple(Vector(at) - ANCHOR_HEAD))


def on_floor(rot, x=0.0, y=0.0, bottom=0.0, crown=None):
    """The anchor turned `rot`, moved (x, y) (or with its crown over the point `crown`), its lowest point `bottom`
    above the floor."""
    r = _turn(rot)
    low = min((r @ (p - ANCHOR_HEAD) + ANCHOR_HEAD).z for p in ANCHOR_POINTS)
    if crown is not None:
        at = r @ (ANCHOR_CROWN - ANCHOR_HEAD) + ANCHOR_HEAD
        x, y = crown[0] - at.x, crown[1] - at.y
    return (tuple(rot), (round(x, 4), round(y, 4), round(bottom - low, 4)))


# ------------------------------------------------------------------ the hands

# Pseudo-tracks (keyed and baked like bones, never exported):
#   GripL / GripR: ((on the anchor, on the chain, 0), _): how much each hand holds what (the rest: free).
#   Chain: ((yaw, elevation, 0), (left, right, 0)): the chain's way from the hook at her waist (degrees, + to her
#          right / up), and where along it each hand holds it (m, out from the reach of it nearest that shoulder).
#   FreeL / FreeR: (_, point): where a free hand goes, in her chest's rest coordinates.
#   HookHand: ((weight, 0, 0), _): the Arms bone (where the game ties the hook's line) from between her hands
#          (0) to her right hand (1).
CHAIN_START = Vector((0, -0.63, 0.82))  # (the hook's centre, on her spine: where the game starts the chain)
CHAIN_LIFT = 0.2  # (her hands lift the chain a little where they hold it, just out from the hook)
CLAW_MID_L = (Vector(HAND_L) + Vector(CLAW_TIP_L)) / 2
CLAW_MID_R = (Vector(HAND_R) + Vector(CLAW_TIP_R)) / 2
ZERO = ((0, 0, 0), (0, 0, 0))


def _value(pose, name, default):
    v = pose.get(name)
    if v is None:
        return default
    return (tuple(v[0]), tuple(v[1])) if isinstance(v[0], (tuple, list)) else (tuple(v), (0, 0, 0))


def chain_dir(yaw, elevation):
    y, e = math.radians(yaw), math.radians(elevation)
    return Vector((math.cos(e) * math.sin(y), -math.cos(e) * math.cos(y), math.sin(e)))


def _euler(q):
    return tuple(round(math.degrees(v), 4) for v in q.to_euler("XYZ"))


# The skirt's lowest ring (rest coordinates), and how low it rests: kept there.
SKIRT_SCALE = 1.03
ROBE_AT = M((0, 0.02, 0.78), rot=(10, 0, 0))
HEM_RIM = [ROBE_AT @ Vector((0.74 * SKIRT_SCALE * math.cos(a), 0.74 * SKIRT_SCALE * math.sin(a), -0.75))
           for a in (2 * math.pi * i / 11 for i in range(11))]
HEM_LOW = min(p.z for p in HEM_RIM)


def _solve(pose):
    """One frame: the hands put where they hold (and how far they miss), the Arms bone between them, the skirt
    kept on the floor."""
    pose = dict(pose)
    pose.setdefault("Hem", (0, 0, 0))
    m = kit.pose_matrices(BONES, pose)
    (yaw, elev, _), (dl, dr, _) = _value(pose, "Chain", ((0, -10, 0), (0.0, 0.2, 0)))
    start = m["Spine"] @ CHAIN_START
    along = chain_dir(yaw, elev)
    lift = Vector((0, 0, CHAIN_LIFT))
    chest = m["Chest"].to_3x3()
    goals = {}
    for side, arm, fore, hand, grip, free, dist in ((-1, "ArmL", "ForeL", HAND_L, "GripL", "FreeL", dl),
                                                     (1, "ArmR", "ForeR", HAND_R, "GripR", "FreeR", dr)):
        (wa, wc, _), _ = _value(pose, grip, ((1, 0, 0), (0, 0, 0)))
        _, spot = _value(pose, free, ((0, 0, 0), hand))
        near = max(0.05, (m["Chest"] @ Vector(BONES[arm][0]) - start).dot(along))  # (the reach of the chain nearest her shoulder)
        on_chain = start + along * max(0.05, near + dist) + lift
        goal = (m["Anchor"] @ Vector(hand)) * wa + on_chain * wc + (m["Chest"] @ Vector(spot)) * (1 - wa - wc)
        goals[fore] = (goal, wa, wc)
        # The elbow bowed out and down, or (when the hand comes up in line with that: overhead, or tucked in close)
        # down and back, or out and forward: each way weighed by how well it bends the arm, so it never flips.
        n = (goal - m["Chest"] @ Vector(BONES[arm][0])).normalized()
        pole = Vector((0, 0, 0))
        for way in ((side * 1.0, 0.35, -0.8), (side * 0.5, 0.5, -1.0), (side * 1.0, -0.4, 0.1)):
            w = chest @ Vector(way)
            perp = w - n * w.dot(n)
            pole += perp * perp.length
        pose = kit.reach(BONES, pose, arm, fore, lambda _m, g=goal: g, pole)
    m = kit.pose_matrices(BONES, pose)
    miss = [0.0, 0.0]  # (off the anchor, off the chain)
    for fore, hand in (("ForeL", HAND_L), ("ForeR", HAND_R)):
        goal, wa, wc = goals[fore]
        off = (m[fore] @ Vector(hand) - goal).length
        if wa > 0.9:
            miss[0] = max(miss[0], off)
        if wc > 0.9:
            miss[1] = max(miss[1], off)
    # The Arms bone: carried so the point between her claws (or her right claw) is where it is.
    (w, _, _), _ = _value(pose, "HookHand", ZERO)
    rest_point = (CLAW_MID_L + CLAW_MID_R) / 2
    posed = (m["ForeL"] @ CLAW_MID_L).lerp(m["ForeR"] @ CLAW_MID_R, 0.5 + 0.5 * w)
    head = Vector(BONES["Arms"][0])
    local = m["Chest"].inverted() @ posed
    turn = (rest_point - head).rotation_difference(local - head)
    offset = local - (head + turn @ (rest_point - head))
    pose["Arms"] = (_euler(turn), tuple(round(c, 4) for c in offset))
    # The skirt: its hem kept where it rests on the floor (it pools as she sinks, it doesn't sink with her).
    rot, _ = _value(pose, "Hem", ZERO)
    dz = 0.0
    for _ in range(2):
        pose["Hem"] = (rot, (0, 0, dz))
        hm = kit.pose_matrices(BONES, pose)["Hem"]
        dz += HEM_LOW - min((hm @ p).z for p in HEM_RIM)
    pose["Hem"] = (rot, (0, 0, round(dz, 4)))
    return pose, miss


# ------------------------------------------------------------------ the loose things, simulated

# k: spring back to its place on the body (1/s^2); g: gravity (x 9.8); c: damping; limit: how far (degrees) it may
# swing from its place; keep: +1 never swings back into her (+Y), -1 never forward into her (-Y); floor: the lowest
# its tip may go (it rests on the floor, it doesn't sink into it).
LOOSE = {
    "Drape": dict(k=90.0, g=0.15, c=9.0, limit=22.0, keep=-1, floor=0.3),
    "Hook": dict(k=26.0, g=0.7, c=4.0, limit=38.0, keep=1, floor=0.26),
    "Lantern": dict(k=0.0, g=1.0, c=4.0, limit=70.0, keep=0, floor=0.05),
}


def _loose(seq, bone, prm, record_from):
    """Simulate one loose bone through `seq` [(pose, dt)], returning its rotation for each frame from `record_from`."""
    head, tail, parent = BONES[bone]
    head, tail = Vector(head), Vector(tail)
    length = (tail - head).length
    rest_dir = (tail - head).normalized()
    infos = []
    for pose, dt in seq:
        m = kit.pose_matrices(BONES, pose)
        pm = m[parent]
        infos.append((pm @ head, pm @ tail, pm, dt))
    x = infos[0][1].copy()
    v = Vector((0, 0, 0))
    out = []
    steps = 8
    gravity = Vector((0, 0, -9.8 * prm["g"]))
    limit = math.radians(prm["limit"])
    for i, (pivot, target, pm, dt) in enumerate(infos):
        if i > 0:
            p0, t0 = infos[i - 1][0], infos[i - 1][1]
            h = max(dt, 1e-4) / steps
            tv = (target - t0) / max(dt, 1e-4)
            for s in range(1, steps + 1):
                u = s / steps
                piv, tgt = p0.lerp(pivot, u), t0.lerp(target, u)
                acc = (tgt - x) * prm["k"] - (v - tv) * prm["c"] + gravity
                vn = v + acc * h
                xn = x + vn * h
                d = xn - piv
                if d.length < 1e-6:
                    d = tgt - piv
                d = d.normalized()
                want = (tgt - piv).normalized()
                ang = d.angle(want, 0.0)
                if ang > limit:
                    d = want.slerp(d, limit / ang) if ang < math.pi - 1e-3 else want
                if prm["keep"]:
                    inv = pm.to_3x3().inverted()
                    dl, wl = inv @ d, inv @ want
                    if prm["keep"] * (dl.y - wl.y) > 0:
                        dl.y = wl.y
                        d = (pm.to_3x3() @ dl).normalized()
                xn = piv + d * length
                for _ in range(2):
                    if xn.z < prm["floor"]:
                        xn.z = prm["floor"]
                        xn = piv + (xn - piv).normalized() * length
                v = (xn - x) / h
                x = xn
        if i >= record_from:
            local = pm.to_3x3().inverted() @ (x - pivot)
            out.append(_euler(rest_dir.rotation_difference(local.normalized())))
    return out


SOLVED = {}  # name: (poses, seconds a frame), for the clip after it to start its loose things from
REACH = {}   # name: how far the hands come off what they hold, at worst


def clip(name, length, tracks, secs=None, periodic=False, after=None):
    """A clip `length` frames long from per-bone tracks, baked to every frame; the hands put on what they hold, the
    skirt on the floor, the loose things simulated. `secs`: how long the game plays it (the simulation's clock);
    `after`: the clip it follows in play (its loose things carry on from there)."""
    dt = (secs or length / 30.0) / length
    solved = [_solve(p) for _, p in kit.bake(tracks, length, periodic)]
    poses = [p for p, _ in solved]
    REACH[name] = (max(m[0] for _, m in solved), max(m[1] for _, m in solved))
    if os.environ.get("ANCHORESS_DEBUG") == name:
        for i, (_, m) in enumerate(solved):
            print(f"[debug] {name} f{i + 1}: anchor {m[0] * 100:.0f} cm, chain {m[1] * 100:.0f} cm")
    if periodic:
        seq = [(p, dt) for p in poses[:-1]] * 4 + [(poses[-1], dt)]
        start = len(seq) - len(poses)
    else:
        before = []
        if after in SOLVED:
            prev, pdt = SOLVED[after]
            before = [(p, pdt) for p in prev]
        else:
            before = [(poses[0], 0.05)] * 30  # (settled in its first pose)
        seq = before + [(p, dt) for p in poses]
        start = len(before)
    for bone, prm in LOOSE.items():
        for pose, rot in zip(poses, _loose(seq, bone, prm, start)):
            pose[bone] = rot
    SOLVED[name] = (poses, dt)
    return ("LINEAR", [(i + 1, p) for i, p in enumerate(poses)])


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


def both(track_l, track_r, ks):
    return {track_l: list(ks), track_r: list(ks)}


def grips(*ks):
    """Both hands' grips keyed alike: (frame, on the anchor, on the chain)."""
    return both("GripL", "GripR", [(f, ((a, c, 0), (0, 0, 0))) for f, a, c in ks])


def G(a, c=0.0):
    return ((a, c, 0), (0, 0, 0))


def F(x, y, z):
    return ((0, 0, 0), (x, y, z))


def C(yaw, elev, dl, dr):
    return ((yaw, elev, 0), (dl, dr, 0))


def H(x, y, z, rx=0.0, ry=0.0, rz=0.0):
    return ((rx, ry, rz), (x, y, z))


# ------------------------------------------------------------------ poses

# OPEN (unaware, walking): the anchor stood on its crown in front of her, leaning back into her hands; her face
# shows over the stock.
OPEN = {"Hips": H(0, 0, 0), "Hem": (0, 0, 0), "Spine": (5, 0, 0), "Chest": (3, 0, 0), "Head": (-3, 0, 0),
        "Anchor": on_floor((-6, 0, 0), 0, 0.04, 0.0)}
# GUARD (alert): crouched, the anchor lifted off the floor in front of her face, her head ducked behind it.
GUARD = {"Hips": H(0, 0.02, -0.06), "Hem": (0, 0, 0), "Spine": (-2, 0, 0), "Chest": (-5, 0, 0), "Head": (12, 0, 0),
         "Anchor": on_floor((3, 0, 0), 0, 0.06, 0.3)}
# The Drop's top: the anchor swung up and over, crown to the sky (a little behind her), up on her toes.
RAISED = {"Hips": H(0, 0.04, 0.05), "Spine": (-3, 0, 0), "Chest": (-6, 0, 0), "Head": (-18, 0, 0),
          "Anchor": grip_at((130, 0, 0), (0, -0.6, 1.95))}
# The Drop's end: the crown buried in the floor a long stride ahead, she folded over it.
DROPPED_HIPS = H(0, -0.24, -0.12)
DROPPED_ANCHOR = on_floor((-56, 0, 0), 0, -0.55, -0.05)
DROPPED = {"Hips": DROPPED_HIPS, "Spine": (30, 0, 0), "Chest": (14, 0, 0), "Head": (8, 0, 0), "Anchor": DROPPED_ANCHOR}
# Kneeling over the anchor stood before her, her hands on it, head bowed (the Vigil, the Undertow).
KNEEL_HIPS = H(0, 0.04, -0.33)
KNEEL = {"Hips": KNEEL_HIPS, "Hem": (0, 0, 0), "Spine": (8, 0, 0), "Chest": (9, 0, 0), "Head": (26, 0, 0),
         "Anchor": on_floor((-9, 0, 0), 0, -0.04, 0.0)}
# Planted: stood on its crown before her, her left hand on it (the Hook Yank).
PLANTED_ANCHOR = on_floor((-4, 0, -8), -0.12, -0.08, 0.0)

# The Chain Sweep's anchor, out on the chain (as the game flies it): from 115 degrees to her right to 115 to her left.
SWEEP_FROM, SWEEP_TO = 115.0, -115.0


def sweep_angle(t):
    t = max(0.0, min(1.0, t))
    return SWEEP_FROM + (SWEEP_TO - SWEEP_FROM) * t * t * (3 - 2 * t)


# Out of her hands, the game puts the anchor where the fight says (by its ring); the clips key it there too, as near as
# they can, so the loose lantern swings as it will in play (and the films and the gallery show it).
ANCHOR_RING = Vector((0, -0.8, 1.66))


def ring_at(rot, at):
    """The anchor turned `rot` with its ring's centre at `at`."""
    return grip_at(rot, Vector(at) - _turn(rot) @ (ANCHOR_RING - ANCHOR_HEAD))


def swung(angle, reach=3.1, height=0.9):
    """Out on the chain at `angle` degrees round her (+ to her right), flat, its ring towards her (as the game swings it)."""
    a = math.radians(angle)
    radial = Vector((math.sin(a), -math.cos(a), 0))
    return ring_at((90, 0, angle + 180), radial * (reach - 0.75) + Vector((0, 0, height)))


def lying(distance, rot=(270, 0, 0)):
    """Lying flat on the floor `distance` ahead, its ring towards her (as the game lays it after a cast)."""
    return ring_at(rot, (0, -distance, 0.12))


OPEN_ANCHOR_354 = on_floor((354, 0, 0), 0, 0.04, 0.0)  # (OPEN's, turned the long way round: from flat on the floor)
OPEN_CROWN_Y = (_turn((354, 0, 0)) @ (ANCHOR_CROWN - ANCHOR_HEAD) + ANCHOR_HEAD).y + 0.04


def stood_up(f0, f1, ring_from):
    """Frame by frame from lying flat (its ring `ring_from` ahead of her) up onto its crown in her hands (OPEN): it
    pivots up on its crown as she drags it in, its lowest point kept on the floor all the way."""
    crown_from = -(ring_from + (ANCHOR_RING - ANCHOR_CROWN).length)
    out = []
    for f in range(f0, f1 + 1):
        t = (f - f0) / (f1 - f0)
        e = t * t * (3 - 2 * t)
        out.append((f, on_floor((270 + 84 * e, 0, 0), crown=(0, crown_from + (OPEN_CROWN_Y - crown_from) * e))))
    return out


ACTIONS = {}

# Vigil (2.6 s loop, before the fight): kneeling over the anchor, her hands on its shank, head bowed. A slow breath in
# (her back rising, the head lifting a little, as if she heard oars), held, and let go in a sigh: the shoulders sink
# and the head bows lower; a shiver runs through her shoulders; then still again.
ACTIONS["Vigil"] = clip("Vigil", 79, {
    "Hips": [(1, KNEEL_HIPS), (26, H(0, 0.04, -0.315)), (48, H(0, 0.045, -0.335)), (66, H(0, 0.04, -0.33))],
    "Hem": [(1, (0, 0, 0)), (40, (1, 0, 0))],
    "Spine": [(1, (8, 0, 0)), (24, (4, 0, 0)), (34, (4, 0, 0)), (50, (11, 0, 1)), (64, (9, 0, 0))],
    "Chest": [(1, (9, 0, 0)), (26, (4, 0, 0)), (36, (4, 0, 0)), (52, (12, 0, 1)), (58, (10, 1, 0)), (60, (12, -1, 0)),
              (62, (10, 1, 0)), (64, (11, 0, 0)), (70, (9, 0, 0))],
    "Head": [(1, (26, 0, 0)), (20, (26, 0, 0)), (31, (14, 0, 3)), (40, (15, 0, 2)), (54, (32, 0, -2)), (62, (31, 1, 0)),
             (72, (27, 0, 0))],
    "Anchor": [(1, KNEEL["Anchor"]), (30, on_floor((-8, 0, 0), 0, -0.045, 0.0)), (56, on_floor((-10, 0, 0), 0, -0.035, 0.0))],
}, periodic=True)

# Idle (2 s loop): the anchor stood before her, leaning into her hands. A heavy breath (the back rises, the shoulders
# after it, the anchor rocked back a touch as she leans on it), then the sigh out; her weight shifts onto her left;
# she looks round to her right, slowly, the head trailing the body, and back with a twitch.
ACTIONS["Idle"] = clip("Idle", 59, {
    "Hips": [(1, H(0, 0, 0)), (18, H(0.012, 0, 0.018, 0, -1)), (34, H(0.02, 0, -0.01, 0, -1.5)), (50, H(0.006, 0, 0.004, 0, -0.5))],
    "Hem": [(4, (0, 0, 0)), (24, (0, 1, 0)), (40, (0, 1.5, 0)), (56, (0, 0.5, 0))],
    "Spine": [(1, (5, 0, 0)), (17, (2, 1, 2)), (34, (8, 1.5, 3)), (50, (6, 0.5, 1))],
    "Chest": [(1, (3, 0, 0)), (19, (-1, 1, 3)), (36, (7, 1.5, 4)), (52, (5, 0.5, 1))],
    "Head": [(1, (-3, 0, 0)), (14, (-4, 0, 3)), (28, (-1, 0, 22)), (38, (2, 0, 20)), (44, (-3, 0, 6)), (47, (-6, 0, 9)),
             (54, (-3, 0, 2))],
    "Anchor": [(1, OPEN["Anchor"]), (21, on_floor((-8, 0, 0), 0.0, 0.05, 0.0)), (38, on_floor((-5, 1, 1), 0.01, 0.035, 0.0)),
               (52, on_floor((-6, 0, 0), 0.005, 0.04, 0.0))],
}, periodic=True)

# Walk (1.6 s loop, two heavy steps): upright on frame 1 (the passing pose: it joins what's before and after), she drops
# onto her right (frame 8) and rolls over it, rises (frame 25, passing), drops onto her left (frame 32). The chest
# rolls after the hips, the head bobs after the chest; the anchor drags on its crown before her, hitching each step
# (it catches, tips back into her hands, then slides on); the skirt swings after the hips.
_STEP = 24
ACTIONS["Walk"] = clip("Walk", 48, {
    "Hips": [(1, H(0, 0, 0.02)), (8, H(0.035, 0, -0.055, 0, 7, 5)), (11, H(0.035, 0, -0.065, 0, 6, 4)), (18, H(0.01, 0, 0.0, 0, 1, 1)),
             (25, H(0, 0, 0.02)), (32, H(-0.035, 0, -0.055, 0, -7, -5)), (35, H(-0.035, 0, -0.065, 0, -6, -4)), (42, H(-0.01, 0, 0.0, 0, -1, -1))],
    "Hem": [(3, (0, -2, 0)), (12, (-3, 4, 2)), (27, (0, 2, 0)), (36, (-3, -4, -2))],
    "Spine": [(1, (6, 0, 0)), (10, (9, 5, 3)), (25, (6, 0, 0)), (34, (9, -5, -3))],
    "Chest": [(3, (3, 0, 0)), (12, (7, 6, 4)), (27, (3, 0, 0)), (36, (7, -6, -4))],
    "Head": [(5, (-6, 0, 0)), (14, (-1, -3, -4)), (29, (-6, 0, 0)), (38, (-1, 3, 4))],
    "Anchor": [(1, on_floor((-6, 0, 0), 0, 0.04, 0.0)), (9, on_floor((-11, 2, 2), 0.02, 0.05, 0.0)), (13, on_floor((-4, 1, 1), 0.02, 0.0, 0.0)),
               (25, on_floor((-6, 0, 0), 0, 0.04, 0.0)), (33, on_floor((-11, -2, -2), -0.02, 0.05, 0.0)), (37, on_floor((-4, -1, -1), -0.02, 0.0, 0.0))],
}, periodic=True)

# GuardUp (0.4 s): a dip to get under it, the anchor heaved up before her face (overshooting, a settle), the head
# ducking in behind it last.
ACTIONS["GuardUp"] = clip("GuardUp", 11, keys(held(OPEN, 1), {
    "Hips": [(3, H(0, 0, -0.05)), (8, H(0, 0.02, -0.04)), (12, GUARD["Hips"])],
    "Spine": [(3, (8, 0, 0)), (8, (-4, 0, 0)), (12, GUARD["Spine"])],
    "Chest": [(4, (6, 0, 0)), (9, (-7, 0, 0)), (12, GUARD["Chest"])],
    "Head": [(5, (-6, 0, 0)), (10, (15, 0, 0)), (12, GUARD["Head"])],
    "Anchor": [(3, on_floor((-8, 0, 0), 0, 0.05, 0.0)), (8, on_floor((5, 0, 0), 0, 0.06, 0.36)), (12, GUARD["Anchor"])],
}), secs=0.4, after="Walk")

# GuardWalk (2.4 s loop, two braced shuffles, upright at frame 1): low, the anchor held steady before her face (a
# wall: it barely moves), the body rocking under it, the head peering over.
ACTIONS["GuardWalk"] = clip("GuardWalk", 72, {
    "Hips": [(1, H(0, 0.02, -0.05)), (14, H(0.02, 0.02, -0.08, 0, 3, 2)), (22, H(0.015, 0.02, -0.085, 0, 2, 1)),
             (37, H(0, 0.02, -0.05)), (50, H(-0.02, 0.02, -0.08, 0, -3, -2)), (58, H(-0.015, 0.02, -0.085, 0, -2, -1))],
    "Hem": [(5, (0, -1, 0)), (20, (-2, 3, 1)), (41, (0, 1, 0)), (56, (-2, -3, -1))],
    "Spine": [(1, (-2, 0, 0)), (17, (-1, 2, 2)), (37, (-2, 0, 0)), (53, (-1, -2, -2))],
    "Chest": [(3, (-5, 0, 0)), (20, (-4, 2, 2)), (39, (-5, 0, 0)), (56, (-4, -2, -2))],
    "Head": [(6, (12, 0, 0)), (23, (14, -2, -3)), (42, (12, 0, 0)), (59, (14, 2, 3))],
    "Anchor": [(1, GUARD["Anchor"]), (19, on_floor((2, 1, 1), 0.01, 0.06, 0.28)), (37, GUARD["Anchor"]),
               (55, on_floor((2, -1, -1), -0.01, 0.06, 0.28))],
}, periodic=True)

# Block (0.3 s): a blow on the anchor shoves it back into her; she rocks back under it, the head snapping back after,
# then she braces again (a little past, and settles).
ACTIONS["Block"] = clip("Block", 9, keys(held(GUARD, 1), held(GUARD, 10), {
    "Hips": [(3, H(0, 0.07, -0.05))],
    "Spine": [(3, (-9, 0, 0)), (7, (1, 0, 0))],
    "Chest": [(4, (-13, 0, 0)), (7, (-3, 0, 0))],
    "Head": [(5, (-2, 0, 0)), (8, (15, 0, 0))],
    "Anchor": [(3, on_floor((-9, 0, 0), 0, 0.16, 0.26)), (7, on_floor((6, 0, 0), 0, 0.03, 0.31))],
}), secs=0.3, after="GuardWalk")

# Windup (1.1 s, the Anchor Drop's tell): she sinks over it and tips it forward onto her grip (getting under the
# weight), then heaves: up onto her toes, the anchor swung up and over until it lies cocked over her head, crown back
# to the sky (it swings past, then settles back), the head following it up last; held there at the top, straining
# (it sways).
ACTIONS["Windup"] = clip("Windup", 33, keys(held(GUARD, 1), {
    "Hips": [(8, H(0, 0.03, -0.12)), (18, H(0, 0.04, 0.02)), (24, H(0, 0.05, 0.065)), (29, RAISED["Hips"]), (34, H(0, 0.045, 0.045))],
    "Hem": [(10, (-2, 0, 0)), (22, (3, 0, 0)), (34, (1, 0, 0))],
    "Spine": [(8, (14, 0, 0)), (17, (2, 0, 0)), (24, (-6, 0, 0)), (29, (-2, 0, 0)), (34, (-3, 0, 1))],
    "Chest": [(9, (12, 0, 0)), (18, (0, 0, 0)), (25, (-9, 0, 0)), (30, (-5, 0, 0)), (34, (-7, 0, 1))],
    "Head": [(10, (20, 0, 0)), (20, (-4, 0, 0)), (27, (-22, 0, 0)), (31, (-16, 0, 0)), (34, (-18, 0, 0))],
    "Anchor": [(8, on_floor((-22, 0, 0), 0, -0.1, 0.05)), (14, grip_at((40, 0, 0), (0, -0.85, 1.45))),
               (19, grip_at((100, 0, 0), (0, -0.72, 1.82))), (24, grip_at((144, 0, 0), (0, -0.56, 2.0))),
               (28, grip_at((126, 0, 0), (0, -0.61, 1.93))), (31, grip_at((133, 0, 2), (0, -0.6, 1.96))),
               (34, grip_at((130, 0, -1), (0, -0.6, 1.95)))],
}), secs=1.1, after="GuardWalk")

# Strike (0.35 s, the blow lands at 3/4): brought over the top and down, her body folding after it, the hips driving
# forward; the crown buried in the floor at frame 9 (the extreme: deeper than it rests), a bounce, and it settles;
# the head whips down after the impact.
ACTIONS["Strike"] = clip("Strike", 10, {
    "Hips": [(1, H(0, 0.045, 0.045)), (4, H(0, -0.05, 0.0)), (8, H(0, -0.26, -0.14)), (9, H(0, -0.27, -0.16)), (11, DROPPED_HIPS)],
    "Hem": [(1, (1, 0, 0)), (8, (-6, 0, 0)), (11, (-3, 0, 0))],
    "Spine": [(1, (-3, 0, 1)), (3, (2, 0, 0)), (6, (22, 0, 0)), (9, (35, 0, 0)), (11, DROPPED["Spine"])],
    "Chest": [(1, (-7, 0, 1)), (4, (-4, 0, 0)), (7, (14, 0, 0)), (9, (20, 0, 0)), (11, DROPPED["Chest"])],
    "Head": [(1, (-18, 0, 0)), (5, (-10, 0, 0)), (8, (4, 0, 0)), (10, (18, 0, 0)), (11, DROPPED["Head"])],
    "Anchor": [(1, grip_at((130, 0, -1), (0, -0.6, 1.95))), (3, grip_at((172, 0, 0), (0, -0.7, 2.0))),
               (5, grip_at((232, 0, 0), (0, -0.98, 1.66))), (7, grip_at((280, 0, 0), (0, -1.2, 1.1))),
               # (on through 280 to 302, not back round to -58: the same pose, turned the short way)
               (9, on_floor((302, 0, 0), 0, -0.6, -0.09)), (10, on_floor((306, 0, 0), 0, -0.57, -0.01)),
               (11, on_floor((304, 0, 0), 0, -0.55, -0.05))],
}, secs=0.35, after="Windup")

# Recover (1.5 s, the punish window): bowed over the buried anchor, heaving for breath (twice), the head hanging;
# then she digs in, levers it out of the floor and hauls it back up into her hands (it rocks back past upright, and
# settles on its crown).
ACTIONS["Recover"] = clip("Recover", 44, {
    "Hips": [(1, DROPPED_HIPS), (8, H(0, -0.23, -0.1)), (16, H(0, -0.24, -0.13)), (24, H(0, -0.23, -0.1)), (29, H(0, -0.2, -0.15)),
             (36, H(0, -0.04, 0.02)), (40, H(0, 0, -0.01)), (45, OPEN["Hips"])],
    "Hem": [(1, (-3, 0, 0)), (12, (-2, 0, 0)), (32, (-4, 0, 0)), (40, (2, 0, 0)), (45, (0, 0, 0))],
    "Spine": [(1, (30, 0, 0)), (8, (27, 0, 0)), (16, (32, 0, 0)), (24, (27, 0, 0)), (29, (33, 0, 0)), (36, (6, 0, 0)), (40, (3, 0, 0)), (45, OPEN["Spine"])],
    "Chest": [(1, (14, 0, 0)), (9, (10, 0, 0)), (17, (16, 0, 0)), (25, (10, 0, 0)), (30, (18, 0, 0)), (37, (4, 0, 0)), (41, (1, 0, 0)), (45, OPEN["Chest"])],
    "Head": [(1, (8, 0, 0)), (6, (22, 0, 0)), (14, (14, 0, 3)), (20, (24, 0, -2)), (28, (14, 0, 0)), (32, (24, 0, 0)), (39, (-8, 0, 0)), (45, OPEN["Head"])],
    "Anchor": [(1, DROPPED_ANCHOR), (26, DROPPED_ANCHOR), (30, on_floor((-50, 0, 0), 0, -0.52, 0.0)),
               (36, on_floor((-2, 0, 0), 0, 0.02, 0.08)), (40, on_floor((-10, 0, 0), 0, 0.05, 0.0)), (45, OPEN["Anchor"])],
}, secs=1.5, after="Strike")

# SweepWindup (0.85 s, the Chain Sweep's tell): a twist to her left first, then she heaves the anchor round to her
# right and flings it out (the game lets it out on the chain from frame 14); her hands slide onto the chain and she
# leans away from its pull, wound up to her right, the head staying on you.
_SW_OUT = 14
ACTIONS["SweepWindup"] = clip("SweepWindup", 23, keys(held(GUARD, 1), {
    "Hips": [(5, H(0, 0.02, -0.08, 0, 0, -8)), (12, H(0.03, 0.02, -0.07, 0, 0, 16)), (17, H(-0.03, 0.03, -0.08, 0, -3, 22)), (24, H(-0.04, 0.03, -0.09, 0, -4, 20))],
    "Hem": [(7, (0, 0, -6)), (15, (0, 3, 8)), (24, (0, 2, 4))],
    "Spine": [(5, (4, 0, -10)), (12, (2, 3, 16)), (17, (-4, -5, 20)), (24, (-5, -6, 18))],
    "Chest": [(6, (2, 0, -10)), (13, (0, 3, 12)), (18, (-4, -5, 14)), (24, (-4, -5, 12))],
    "Head": [(7, (12, 0, 8)), (14, (6, 0, -18)), (19, (4, 0, -32)), (24, (4, 0, -34))],
    "Anchor": [(5, on_floor((0, 0, -12), -0.2, 0.04, 0.22)), (10, grip_at((10, 20, 40), (0.35, -0.68, 1.25))),
               (_SW_OUT, grip_at((0, -55, 20), (0.62, -0.42, 1.25))), (22, swung(SWEEP_FROM - 3)), (24, swung(SWEEP_FROM + 3))],
    "GripL": [(1, G(1)), (_SW_OUT - 1, G(1)), (_SW_OUT + 4, G(0, 1))],
    "GripR": [(1, G(1)), (_SW_OUT, G(1)), (_SW_OUT + 5, G(0, 1))],
    "Chain": [(_SW_OUT, C(80, 8, -0.05, 0.15)), (_SW_OUT + 6, C(SWEEP_FROM, 2, -0.05, 0.15)), (24, C(SWEEP_FROM - 4, 2, -0.05, 0.15))],
}), secs=0.85, after="GuardWalk")

# Sweep (0.45 s): the anchor swung round on its chain from her right, in front of her, to her left (the game flies
# it, this angle exactly); her hips lead the turn, the chest follows, the hands drag on the chain after the anchor, the
# head comes last; she leans back against its pull as it passes in front.
_sweep_chain = [(f, C(sweep_angle((f - 1) / 13.0), 2 - 4 * math.sin(math.pi * (f - 1) / 13.0), -0.05, 0.15)) for f in range(1, 15)]
ACTIONS["Sweep"] = clip("Sweep", 13, {
    "Hips": [(1, H(-0.04, 0.03, -0.09, 0, -4, 20)), (4, H(-0.02, 0.0, -0.1, 0, -2, 8)), (8, H(0.03, -0.02, -0.1, 0, 3, -22)), (14, H(0.05, -0.02, -0.08, 0, 4, -30))],
    "Hem": [(1, (0, 2, 4)), (6, (0, 0, 6)), (14, (0, -3, -10))],
    "Spine": [(1, (-5, -6, 18)), (5, (-8, -2, 4)), (9, (-5, 5, -22)), (14, (-2, 6, -30))],
    "Chest": [(1, (-4, -5, 12)), (6, (-8, -2, 6)), (10, (-5, 5, -16)), (14, (-3, 6, -22))],
    "Head": [(1, (4, 0, -34)), (5, (6, 0, -16)), (9, (8, 0, 12)), (14, (6, 0, 28))],
    "Anchor": [(f, swung(sweep_angle((f - 1) / 13.0))) for f in range(1, 15)],
    "GripL": [(1, G(0, 1))], "GripR": [(1, G(0, 1))],
    "Chain": _sweep_chain,
}, secs=0.45, after="SweepWindup")

# SweepRecover (0.9 s): carried on round by the weight (a stagger-step to her left), then she hauls it home hand over
# hand (the game reels it in by frame 14) and catches it on her shank, swings it down onto its crown with a thud and
# straightens.
ACTIONS["SweepRecover"] = clip("SweepRecover", 27, {
    "Hips": [(1, H(0.05, -0.02, -0.08, 0, 4, -30)), (5, H(0.0, -0.04, -0.11, 0, 2, -36)), (10, H(-0.02, 0.06, -0.1, 0, 0, -20)),
             (15, H(0, 0.02, -0.06, 0, 0, -4)), (19, H(0, -0.02, -0.07)), (28, OPEN["Hips"])],
    "Hem": [(1, (0, -3, -10)), (7, (0, -2, -12)), (14, (0, 1, 2)), (21, (-2, 0, 0)), (28, (0, 0, 0))],
    "Spine": [(1, (-2, 6, -30)), (5, (2, 5, -36)), (10, (-2, 0, -22)), (14, (-4, 0, -6)), (19, (12, 0, 0)), (23, (3, 0, 0)), (28, OPEN["Spine"])],
    "Chest": [(1, (-3, 6, -22)), (6, (0, 5, -28)), (11, (-3, 0, -18)), (15, (-4, 0, -4)), (20, (10, 0, 0)), (24, (1, 0, 0)), (28, OPEN["Chest"])],
    "Head": [(1, (6, 0, 28)), (7, (10, 0, 24)), (13, (6, 0, 6)), (19, (14, 0, 0)), (24, (-6, 0, 0)), (28, OPEN["Head"])],
    "Anchor": [(1, swung(SWEEP_TO)), (5, swung(-104, 2.7, 0.85)), (9, ring_at((60, 0, 20), (-0.75, -1.25, 0.9))),
               (14, grip_at((14, 0, -6), (-0.05, -0.85, 1.3))), (19, on_floor((-12, 0, 0), 0, -0.02, 0.0)),
               (22, on_floor((-3, 0, 0), 0, 0.03, 0.04)), (28, OPEN["Anchor"])],
    "GripL": [(1, G(0, 1)), (10, G(0, 1)), (14, G(1))],
    "GripR": [(1, G(0, 1)), (11, G(0, 1)), (15, G(1))],
    "Chain": [(1, C(SWEEP_TO, 2, -0.05, 0.15)), (4, C(-108, 0, 0.1, -0.05)), (7, C(-90, -4, -0.05, 0.1)),
              (10, C(-55, -8, 0.1, -0.05)), (13, C(-20, -10, -0.05, 0.1))],
}, secs=0.9, after="Sweep")

# CastWindup (1.0 s, the cast's tell): a breath and a dip, the anchor drawn in; then hefted up and back over her
# right shoulder (it swings up behind her and she turns into it, leaning back under the weight); the last coil.
ACTIONS["CastWindup"] = clip("CastWindup", 29, keys(held(OPEN, 1), {
    "Hips": [(7, H(0, 0.02, -0.1)), (17, H(0, 0.06, 0.0, 0, 0, 10)), (24, H(0, 0.08, -0.03, 0, 0, 14)), (30, H(0, 0.1, -0.05, 0, 0, 17))],
    "Hem": [(9, (-2, 0, 0)), (20, (2, 0, 6)), (30, (2, 0, 8))],
    "Spine": [(7, (14, 0, 0)), (17, (-4, 3, 10)), (24, (-5, 4, 14)), (30, (-6, 4, 16))],
    "Chest": [(8, (12, 0, 0)), (18, (-4, 3, 8)), (25, (-4, 4, 11)), (30, (-5, 4, 13))],
    "Head": [(9, (16, 0, 0)), (20, (0, 0, -8)), (27, (-4, 0, -18)), (30, (-4, 0, -22))],
    "Anchor": [(7, on_floor((-16, 0, 0), 0, -0.05, 0.08)), (13, grip_at((60, 0, -6), (0.1, -0.8, 1.6))),
               (19, grip_at((105, 0, -22), (0.26, -0.32, 1.9))), (25, grip_at((116, 0, -28), (0.33, -0.12, 1.86))),
               (30, grip_at((122, 0, -32), (0.36, -0.04, 1.82)))],
}), secs=1.0, after="Walk")

# Cast (0.7 s, let go at frame 3): she uncoils (the hips first), the anchor whipped over her shoulder and flung
# forward; her hands follow it out along the chain as it flies (up, then down where it lands), and she's pulled
# forward after it onto her front foot.
_RELEASE = grip_at((212, 0, 0), (0.06, -0.7, 1.82))
_RELEASE_RING = Vector(_RELEASE[1]) + ANCHOR_HEAD + _turn((212, 0, 0)) @ (ANCHOR_RING - ANCHOR_HEAD)
_CAST_TO = Vector((0, -5.25, 0.12))  # (where a cast at about 6 m lands)
_flight = [(f, ring_at((212 + 58 * min(1.0, (f - 3) / 5.8), 0, 0),
                       _RELEASE_RING.lerp(_CAST_TO, min(1.0, (f - 3) / 5.8)) + Vector((0, 0, 2.2 * 4 * min(1.0, (f - 3) / 5.8) * (1 - min(1.0, (f - 3) / 5.8))))))
           for f in range(4, 11)]
ACTIONS["Cast"] = clip("Cast", 9, {
    "Hips": [(1, H(0, 0.1, -0.05, 0, 0, 17)), (2, H(0, 0.04, -0.06, 0, 0, 8)), (4, H(0, -0.16, -0.1, 0, 0, -8)), (7, H(0, -0.22, -0.12, 0, 0, -10)), (10, H(0, -0.2, -0.11, 0, 0, -6))],
    "Hem": [(1, (2, 0, 8)), (5, (-6, 0, -4)), (10, (-4, 0, -4))],
    "Spine": [(1, (-6, 4, 16)), (3, (0, 2, 6)), (5, (22, 0, -8)), (8, (26, 0, -10)), (10, (24, 0, -8))],
    "Chest": [(1, (-5, 4, 13)), (3, (-6, 2, 8)), (6, (16, 0, -6)), (9, (12, 0, -6)), (10, (12, 0, -6))],
    "Head": [(1, (-4, 0, -22)), (4, (-14, 0, -6)), (7, (8, 0, -4)), (10, (6, 0, -4))],
    "Anchor": [(1, grip_at((122, 0, -32), (0.36, -0.04, 1.82))), (2, grip_at((160, 0, -14), (0.22, -0.36, 1.9))),
               (3, grip_at((212, 0, 0), (0.06, -0.7, 1.82)))] + _flight,
    "GripL": [(1, G(1)), (3, G(1)), (4, G(0)), (5, G(0)), (8, G(0, 1))],
    "GripR": [(1, G(1)), (3, G(1)), (4, G(0)), (6, G(0)), (9, G(0, 1))],
    "FreeL": [(3, F(-0.2, -1.0, 1.75)), (6, F(-0.2, -0.95, 1.45))],
    "FreeR": [(3, F(0.2, -1.0, 1.75)), (7, F(0.2, -0.95, 1.4))],
    "Chain": [(3, C(0, 35, 0.25, 0.45)), (6, C(-2, 22, 0.2, 0.4)), (10, C(-2, -8, 0.1, 0.3))],
}, secs=0.7, after="CastWindup")

# Haul (the cast's recovery, 1.9 s, played once): the anchor lies out there, the chain slack: she gathers it and
# braces (leaning back); then she hauls it in, hand over hand, two great heaves, each one rocking her back; it comes
# home (the game reels it in over the last quarter) and she catches it on its shank and sets it on its crown.
ACTIONS["Haul"] = clip("Haul", 16, {
    "Hips": [(1, H(0, -0.2, -0.11, 0, 0, -6)), (5, H(0, 0.02, -0.1)), (7, H(0, 0.09, -0.14, 0, 0, 6)), (9, H(0, 0.02, -0.1)),
             (11, H(0, 0.1, -0.15, 0, 0, -6)), (13, H(0, 0.02, -0.08)), (15, H(0, -0.02, -0.03)), (17, OPEN["Hips"])],
    "Hem": [(1, (-4, 0, -4)), (6, (2, 0, 0)), (10, (-2, 0, 0)), (14, (2, 0, 0)), (17, (0, 0, 0))],
    "Spine": [(1, (24, 0, -8)), (5, (8, 0, 0)), (7, (-10, 0, 8)), (9, (6, 0, 0)), (11, (-11, 0, -8)), (13, (6, 0, 0)), (15, (10, 0, 0)), (17, OPEN["Spine"])],
    "Chest": [(1, (12, 0, -6)), (5, (6, 0, 0)), (8, (-9, 0, 8)), (10, (4, 0, 0)), (12, (-9, 0, -8)), (14, (6, 0, 0)), (17, OPEN["Chest"])],
    "Head": [(1, (6, 0, -4)), (5, (10, 0, 0)), (8, (2, 0, -4)), (10, (10, 0, 0)), (12, (0, 0, 4)), (15, (12, 0, 0)), (17, OPEN["Head"])],
    "Anchor": [(1, lying(5.25)), (5, lying(5.25)), (7, lying(4.9)), (9, lying(4.0)), (11, lying(2.6))] + stood_up(13, 17, 1.3),
    "GripL": [(1, G(0, 1)), (13, G(0, 1)), (16, G(1))],
    "GripR": [(1, G(0, 1)), (14, G(0, 1)), (17, G(1))],
    "Chain": [(1, C(-2, -8, 0.1, 0.3)), (5, C(0, -14, -0.05, 0.2)), (7, C(0, -14, 0.18, -0.08)), (9, C(0, -16, -0.08, 0.18)),
              (11, C(0, -18, 0.18, -0.08)), (13, C(0, -24, -0.05, 0.15))],
}, secs=1.9, after="Cast")

# Reel (0.5 s, the chase while she pulls a dropped anchor home): two quick hauls hand over hand, then it's in her
# hands (the game brings it home by frame 14) and set on its crown.
ACTIONS["Reel"] = clip("Reel", 15, {
    "Hips": [(1, H(0, 0.04, -0.06)), (4, H(0, 0.1, -0.1)), (7, H(0, 0.03, -0.07)), (10, H(0, 0.1, -0.1)), (13, H(0, 0.0, -0.03)), (16, OPEN["Hips"])],
    "Spine": [(1, (6, 0, 0)), (4, (-4, 0, 5)), (7, (6, 0, 0)), (10, (-4, 0, -5)), (13, (8, 0, 0)), (16, OPEN["Spine"])],
    "Chest": [(1, (4, 0, 0)), (5, (-8, 0, 5)), (8, (4, 0, 0)), (11, (-8, 0, -5)), (14, (6, 0, 0)), (16, OPEN["Chest"])],
    "Head": [(1, (8, 0, 0)), (5, (2, 0, 0)), (8, (10, 0, 0)), (11, (2, 0, 0)), (14, (8, 0, 0)), (16, OPEN["Head"])],
    "Anchor": stood_up(1, 14, 2.2) + [(16, OPEN_ANCHOR_354)],
    "GripL": [(1, G(0, 1)), (11, G(0, 1)), (14, G(1))],
    "GripR": [(1, G(0, 1)), (12, G(0, 1)), (15, G(1))],
    "Chain": [(1, C(0, -16, -0.05, 0.2)), (4, C(0, -16, 0.18, -0.08)), (7, C(0, -18, -0.08, 0.18)), (10, C(0, -20, 0.18, -0.08)), (13, C(0, -24, -0.05, 0.15))],
}, secs=0.5, after="Stagger")

# Stagger (2.0 s; also the stun): knocked back off her guard, the anchor slips from her hands, tips and crashes down
# on its face before her (a bounce); she sags over it, arms hanging, swaying, gasping; then she stoops, takes hold of
# it again and drags it back up onto its crown (it rocks, and settles).
_LYING = on_floor((84, 0, 8), 0.1, -0.2, 0.0)
ACTIONS["Stagger"] = clip("Stagger", 51, keys(held(GUARD, 1), {
    "Hips": [(4, H(0, 0.12, -0.02)), (9, H(0.02, 0.1, -0.08, 0, 3, 0)), (14, H(0, 0.0, -0.14)), (24, H(-0.02, 0.02, -0.12, 0, -2, 0)),
             (32, H(0, -0.04, -0.16)), (38, H(0, -0.06, -0.18)), (45, H(0, 0.0, 0.0)), (49, H(0, 0, -0.015)), (52, OPEN["Hips"])],
    "Hem": [(6, (4, 0, 0)), (16, (-3, 0, 0)), (26, (1, 2, 0)), (40, (-3, 0, 0)), (47, (2, 0, 0)), (52, (0, 0, 0))],
    "Spine": [(4, (-9, 0, 4)), (9, (-6, 3, 0)), (15, (24, 0, -4)), (22, (20, -3, 3)), (30, (26, 2, -2)), (37, (40, 0, 0)), (45, (6, 0, 0)), (49, (3, 0, 0)), (52, OPEN["Spine"])],
    "Chest": [(4, (-10, 0, 6)), (10, (-4, 4, 0)), (16, (18, 0, -4)), (23, (14, -3, 4)), (31, (20, 2, -2)), (38, (22, 0, 0)), (46, (2, 0, 0)), (50, (2, 0, 0)), (52, OPEN["Chest"])],
    "Head": [(3, (-20, 0, 6)), (8, (-10, 0, -6)), (13, (6, 0, 0)), (17, (34, 0, 4)), (25, (30, 0, -6)), (33, (24, 0, 3)), (39, (20, 0, 0)), (47, (-8, 0, 0)), (52, OPEN["Head"])],
    "Anchor": [(4, on_floor((-12, 0, 4), 0, 0.17, 0.34)), (7, on_floor((12, 0, 6), 0.02, -0.02, 0.04)), (10, on_floor((48, 0, 8), 0.06, -0.14, 0.0)),
               (12, on_floor((86, 0, 8), 0.1, -0.21, 0.0)), (14, on_floor((78, 0, 8), 0.1, -0.2, 0.0)), (16, _LYING),
               (36, _LYING), (42, on_floor((30, 0, 4), 0.05, -0.1, 0.0)), (46, on_floor((-10, 0, 0), 0, 0.06, 0.03)),
               (49, on_floor((-3, 0, 0), 0, 0.03, 0.0)), (52, OPEN["Anchor"])],
    "GripL": [(4, G(1)), (6, G(0)), (33, G(0)), (37, G(1))],
    "GripR": [(5, G(1)), (7, G(0)), (34, G(0)), (38, G(1))],
    "FreeL": [(7, F(-0.55, -0.45, 1.55)), (12, F(-0.4, -0.6, 1.05)), (20, F(-0.3, -0.62, 0.85)), (28, F(-0.34, -0.6, 0.9)), (33, F(-0.3, -0.7, 0.8))],
    "FreeR": [(8, F(0.6, -0.4, 1.6)), (13, F(0.42, -0.6, 1.05)), (21, F(0.3, -0.62, 0.86)), (29, F(0.33, -0.6, 0.92)), (34, F(0.3, -0.7, 0.8))],
}), secs=2.0, after="GuardWalk")

# Rise (2.2 s, phase two; planted at 2/3, frame 44): she heaves the anchor up and lifts it to the sky, upright above her,
# rising onto her toes, her face turned up to it; held there trembling; then driven down with all her weight onto its
# crown before her (the floor breaks: she drops with it, a jolt, a bounce), and she straightens over it.
_TREMBLE = [(f, grip_at((-3 + (1.5 if (f // 2) % 2 else -1.5), (1 if (f // 3) % 2 else -1), 0), (0.0, -0.56 + (0.01 if (f // 2) % 2 else -0.01), 2.1)))
            for f in range(22, 38, 2)]
ACTIONS["Rise"] = clip("Rise", 65, keys(held(GUARD, 1), {
    "Hips": [(7, H(0, 0.02, -0.12)), (16, H(0, 0.02, 0.04)), (22, H(0, 0.03, 0.06)), (36, H(0, 0.03, 0.065)), (40, H(0, 0.0, 0.04)),
             (44, H(0, -0.04, -0.17)), (46, H(0, -0.04, -0.2)), (49, H(0, -0.04, -0.16)), (56, H(0, 0, -0.04)), (66, OPEN["Hips"])],
    "Hem": [(9, (-2, 0, 0)), (20, (3, 0, 0)), (40, (2, 0, 0)), (46, (-5, 0, 0)), (54, (2, 0, 0)), (66, (0, 0, 0))],
    "Spine": [(7, (14, 0, 0)), (16, (0, 0, 0)), (22, (-4, 0, 0)), (36, (-5, 0, 1)), (40, (-2, 0, 0)), (44, (22, 0, 0)), (47, (26, 0, 0)), (54, (12, 0, 0)), (66, OPEN["Spine"])],
    "Chest": [(8, (12, 0, 0)), (17, (-2, 0, 0)), (23, (-6, 0, 0)), (28, (-5, 1, 0)), (32, (-7, -1, 0)), (37, (-6, 0, 0)), (41, (-2, 0, 0)), (45, (16, 0, 0)), (48, (18, 0, 0)), (56, (6, 0, 0)), (66, OPEN["Chest"])],
    "Head": [(9, (18, 0, 0)), (19, (-14, 0, 0)), (25, (-32, 0, 0)), (37, (-34, 0, 0)), (42, (-20, 0, 0)), (46, (10, 0, 0)), (49, (22, 0, 0)), (58, (2, 0, 0)), (66, OPEN["Head"])],
    "Anchor": [(7, on_floor((-6, 0, 0), 0, 0.04, 0.12)), (13, grip_at((-2, 0, 0), (0, -0.7, 1.6))), (20, grip_at((-4, 0, 0), (0, -0.57, 2.12)))]
              + _TREMBLE + [(38, grip_at((-3, 0, 0), (0, -0.56, 2.1))), (41, grip_at((-3, 0, 0), (0, -0.75, 1.75))),
                            (44, on_floor((-4, 0, 0), 0, -0.06, -0.06)), (46, on_floor((-6, 0, 0), 0, -0.05, -0.03)),
                            (49, on_floor((-5, 0, 0), 0, -0.04, -0.04)), (58, on_floor((-6, 0, 0), 0, 0.02, 0.0)), (66, OPEN["Anchor"])],
}), secs=2.2, after="GuardWalk")

# YankWindup (1.1 s, phase two): she sets the anchor down on its crown before her (a thud), her left hand stays on it;
# her right hand goes up and whirls the Ferryman's hook round over her head on its line (the game whirls the hook; the
# line leaves her right hand), faster as it goes; she turns a little into the whirl, eyes on you.
_whirl = [(f, F(0.42 + 0.14 * min(1.0, (f - 10) / 8) * math.cos(2 * math.pi * (f - 10) / 13.0),
                -0.3 + 0.14 * min(1.0, (f - 10) / 8) * math.sin(2 * math.pi * (f - 10) / 13.0), 2.3)) for f in range(10, 34)]
ACTIONS["YankWindup"] = clip("YankWindup", 32, keys(held(GUARD, 1), {
    "Hips": [(5, H(0, 0.0, -0.1)), (8, H(0, 0.0, -0.08)), (14, H(0.02, 0.04, -0.05, 0, 0, 10)), (33, H(0.02, 0.04, -0.05, 0, 0, 12))],
    "Hem": [(8, (-3, 0, 0)), (16, (1, 0, 3)), (33, (0, 0, 2))],
    "Spine": [(5, (16, 0, 0)), (9, (8, 0, 2)), (15, (-6, -3, 10)), (24, (-7, -4, 12)), (33, (-8, -4, 14))],
    "Chest": [(6, (12, 0, 0)), (10, (6, 0, 2)), (16, (-6, -4, 10)), (25, (-7, -5, 12)), (33, (-8, -5, 12))],
    "Head": [(7, (16, 0, 0)), (12, (6, 0, -4)), (20, (-4, 0, -18)), (33, (-4, 0, -20))],
    "Anchor": [(5, on_floor((-2, 0, -4), -0.06, -0.05, 0.02)), (7, PLANTED_ANCHOR), (8, on_floor((-6, 0, -8), -0.12, -0.08, 0.01)), (11, PLANTED_ANCHOR)],
    "GripL": [(1, G(1))],
    "GripR": [(4, G(1)), (9, G(0))],
    "FreeR": [(1, F(0.2, -0.72, 1.2)), (4, F(0.3, -0.6, 1.4)), (7, F(0.38, -0.45, 1.85))] + _whirl,
    "HookHand": [(1, ((0, 0, 0), (0, 0, 0))), (6, ((0, 0, 0), (0, 0, 0))), (10, ((1, 0, 0), (0, 0, 0)))],
}), secs=1.1, after="GuardWalk")

# Yank (0.6 s): the throw: her right arm whips over and forward (the body twisting after it, the hips first), flung
# out along the line as the hook flies; she leans after it, her left hand still on the planted anchor.
ACTIONS["Yank"] = clip("Yank", 17, {
    "Hips": [(1, H(0.02, 0.04, -0.05, 0, 0, 12)), (3, H(0.0, -0.02, -0.07, 0, 0, 0)), (6, H(-0.02, -0.1, -0.1, 0, 0, -12)), (18, H(-0.02, -0.09, -0.09, 0, 0, -10))],
    "Hem": [(1, (0, 0, 2)), (6, (-5, 0, -6)), (12, (-2, 0, -2)), (18, (-3, 0, -3))],
    "Spine": [(1, (-8, -4, 14)), (3, (2, 0, 4)), (6, (20, 2, -14)), (10, (18, 1, -12)), (18, (18, 1, -12))],
    "Chest": [(1, (-8, -5, 12)), (4, (0, 0, 4)), (7, (16, 2, -14)), (11, (12, 1, -10)), (18, (13, 1, -10))],
    "Head": [(1, (-4, 0, -20)), (5, (-8, 0, -8)), (8, (2, 0, 8)), (18, (0, 0, 8))],
    "Anchor": [(1, PLANTED_ANCHOR)],
    "GripL": [(1, G(1))],
    "GripR": [(1, G(0))],
    "FreeR": [(1, F(0.42, -0.16, 2.3)), (3, F(0.4, -0.7, 2.0)), (5, F(0.22, -1.0, 1.45)), (8, F(0.12, -1.05, 1.3)), (18, F(0.14, -1.0, 1.32))],
    "HookHand": [(1, ((1, 0, 0), (0, 0, 0)))],
}, secs=0.6, after="YankWindup")

# YankRecover (0.9 s): she hauls back on the line, a great heave with both hands (the hook comes home, and whoever it
# caught), then takes up the anchor again from where it stands, swings it into her hands and sets it on its crown.
ACTIONS["YankRecover"] = clip("YankRecover", 26, {
    "Hips": [(1, H(-0.02, -0.09, -0.09, 0, 0, -10)), (4, H(0, 0.1, -0.12, 0, 0, 4)), (8, H(0, 0.12, -0.1, 0, 0, 6)), (13, H(0, 0.0, -0.1)),
             (18, H(0, -0.02, -0.05)), (22, H(0, 0, 0.01)), (27, OPEN["Hips"])],
    "Hem": [(1, (-3, 0, -3)), (5, (3, 0, 2)), (12, (-2, 0, 0)), (20, (2, 0, 0)), (27, (0, 0, 0))],
    "Spine": [(1, (18, 1, -12)), (4, (-8, 0, 2)), (8, (-10, 0, 4)), (13, (14, 0, 0)), (18, (10, 0, 0)), (22, (3, 0, 0)), (27, OPEN["Spine"])],
    "Chest": [(1, (13, 1, -10)), (5, (-9, 0, 2)), (9, (-8, 0, 4)), (14, (12, 0, 0)), (19, (6, 0, 0)), (23, (1, 0, 0)), (27, OPEN["Chest"])],
    "Head": [(1, (0, 0, 8)), (4, (8, 0, 4)), (8, (4, 0, 0)), (14, (16, 0, 0)), (20, (2, 0, 0)), (24, (-6, 0, 0)), (27, OPEN["Head"])],
    "Anchor": [(1, PLANTED_ANCHOR), (13, PLANTED_ANCHOR), (17, on_floor((-2, 0, -2), -0.04, 0.0, 0.1)), (21, on_floor((-8, 0, 0), 0, 0.05, 0.0)),
               (24, on_floor((-4, 0, 0), 0, 0.03, 0.02)), (27, OPEN["Anchor"])],
    "GripL": [(1, G(0)), (2, G(0)), (4, G(0, 1)), (9, G(0, 1)), (13, G(1))],
    "GripR": [(1, G(0)), (3, G(0, 1)), (9, G(0, 1)), (14, G(1))],
    "FreeL": [(1, F(-0.2, -0.72, 1.2))],
    "FreeR": [(1, F(0.14, -1.0, 1.32))],
    "Chain": [(1, C(0, 12, 0.3, 0.5)), (4, C(0, 14, -0.05, 0.2)), (9, C(0, 6, 0.2, -0.05))],
    "HookHand": [(1, ((1, 0, 0), (0, 0, 0))), (4, ((0.5, 0, 0), (0, 0, 0))), (12, ((0, 0, 0), (0, 0, 0)))],
}, secs=0.9, after="Yank")

# UndertowWindup (1.6 s): she sets the anchor down before her (a thud) and sinks onto her knees behind it, slowly, a
# bump as they touch the floor; her head bows and her shoulders fold in over her hands.
ACTIONS["UndertowWindup"] = clip("UndertowWindup", 26, keys(held(GUARD, 1), {
    "Hips": [(4, H(0, 0.02, -0.1)), (6, H(0, 0.02, -0.09)), (12, H(0, 0.03, -0.22)), (18, H(0, 0.04, -0.35)), (20, H(0, 0.04, -0.32)), (27, KNEEL_HIPS)],
    "Hem": [(8, (2, 0, 0)), (19, (-3, 0, 0)), (27, (0, 0, 0))],
    "Spine": [(4, (12, 0, 0)), (12, (4, 0, 0)), (19, (14, 0, 0)), (27, KNEEL["Spine"])],
    "Chest": [(5, (10, 0, 0)), (13, (4, 0, 0)), (20, (12, 0, 0)), (27, KNEEL["Chest"])],
    "Head": [(6, (16, 0, 0)), (14, (8, 0, 0)), (21, (30, 0, 0)), (27, KNEEL["Head"])],
    "Anchor": [(4, on_floor((-2, 0, 0), 0, 0.04, 0.02)), (6, on_floor((-5, 0, 0), 0, 0.05, 0.0)), (10, on_floor((-5, 0, 0), 0, 0.06, 0.0)),
               (20, on_floor((-10, 0, 0), 0, -0.03, 0.0)), (27, KNEEL["Anchor"])],
}), secs=1.6, after="GuardWalk")

# Undertow (2.6 s): kneeling over the anchor, praying while the black water rises round her: she rocks forward and
# back over it (deeper each time), and her shoulders shake.
_shake = [(f, (9 + 3 * math.sin(2 * math.pi * (f - 1) / 26.0) + (1.6 if f % 2 else -1.6) * (1 if 20 < f < 60 else 0), (0.8 if f % 4 < 2 else -0.8) * (1 if 20 < f < 60 else 0), 0))
          for f in range(1, 80)]
ACTIONS["Undertow"] = clip("Undertow", 78, {
    "Hips": [(1, KNEEL_HIPS), (14, H(0, 0.0, -0.34)), (27, H(0, 0.05, -0.325)), (40, H(0, -0.01, -0.345)), (53, H(0, 0.05, -0.325)), (66, H(0, -0.01, -0.345)), (79, KNEEL_HIPS)],
    "Hem": [(1, (0, 0, 0)), (40, (1, 0, 0)), (79, (0, 0, 0))],
    "Spine": [(1, (8, 0, 0)), (14, (13, 0, 1)), (27, (4, 0, -1)), (40, (14, 0, 1)), (53, (4, 0, -1)), (66, (15, 0, 1)), (79, (8, 0, 0))],
    "Chest": _shake,
    "Head": [(1, (26, 0, 0)), (16, (32, 0, 2)), (29, (20, 0, -2)), (42, (33, 0, 2)), (55, (20, 0, -2)), (68, (34, 0, 2)), (79, (26, 0, 0))],
    "Anchor": [(1, KNEEL["Anchor"]), (79, KNEEL["Anchor"])],
}, secs=2.6, after="UndertowWindup")

# UndertowRecover (1.3 s): she leans her weight onto the anchor and pushes up off her knees (a lurch, a stagger
# back as she rises), the head last; then lifts the anchor and sets it on its crown before her.
ACTIONS["UndertowRecover"] = clip("UndertowRecover", 39, keys(held(KNEEL, 1), {
    "Hips": [(8, H(0, -0.04, -0.31)), (18, H(0, 0.02, -0.12)), (24, H(0, 0.07, -0.02)), (30, H(0, 0.02, -0.04)), (35, H(0, 0, 0.01)), (40, OPEN["Hips"])],
    "Hem": [(10, (-2, 0, 0)), (22, (3, 0, 0)), (32, (-1, 0, 0)), (40, (0, 0, 0))],
    "Spine": [(8, (22, 0, 0)), (18, (14, 0, 0)), (24, (-6, 0, 0)), (30, (8, 0, 0)), (35, (4, 0, 0)), (40, OPEN["Spine"])],
    "Chest": [(9, (16, 0, 0)), (19, (12, 0, 0)), (25, (-6, 0, 0)), (31, (6, 0, 0)), (36, (2, 0, 0)), (40, OPEN["Chest"])],
    "Head": [(10, (30, 0, 0)), (20, (26, 0, 0)), (27, (-6, 0, 0)), (33, (2, 0, 0)), (40, OPEN["Head"])],
    "Anchor": [(8, on_floor((-8, 0, 0), 0, -0.02, 0.0)), (20, on_floor((-6, 0, 0), 0, 0.04, 0.0)), (28, on_floor((-2, 0, 0), 0, 0.02, 0.1)),
               (33, on_floor((-9, 0, 0), 0, 0.05, 0.0)), (36, on_floor((-5, 0, 0), 0, 0.035, 0.01)), (40, OPEN["Anchor"])],
}), secs=1.3, after="Undertow")

# Death (2.8 s, slowed): a jolt back; she sinks to her knees (hitting the floor at 35%, when the chain snaps), bowed
# over the anchor, clinging to it; then her hands open and slide off it, and it topples away from her onto its face
# (a thud), and she folds forward over the place it stood. She let go.
ACTIONS["Death"] = clip("Death", 25, keys(held(GUARD, 1), {
    "Hips": [(3, H(0, 0.1, 0.0)), (7, H(0, 0.06, -0.2)), (10, H(0, 0.04, -0.36)), (11, H(0, 0.04, -0.33)), (14, H(0, 0.04, -0.34)),
             (20, H(0, 0.02, -0.32)), (26, H(0, 0.0, -0.3))],
    "Hem": [(4, (4, 0, 0)), (10, (-4, 0, 0)), (14, (1, 0, 0)), (26, (-2, 0, 0))],
    "Spine": [(3, (-9, 0, 4)), (7, (2, 0, 0)), (11, (16, 0, -2)), (17, (14, 0, -2)), (21, (20, 0, -4)), (26, (24, 0, -6))],
    "Chest": [(3, (-9, 0, 4)), (8, (0, 0, 0)), (12, (16, 0, -2)), (18, (14, 0, -2)), (22, (26, 0, -4)), (26, (32, 0, -4))],
    "Head": [(2, (-22, 0, 0)), (6, (-14, 0, 2)), (12, (26, 0, 0)), (18, (28, 0, 0)), (23, (44, 0, -4)), (26, (48, 0, -6))],
    "Anchor": [(3, on_floor((-8, 0, 0), 0, 0.15, 0.26)), (8, on_floor((-6, 0, 0), 0, 0.08, 0.04)), (10, on_floor((-10, 0, 0), 0, 0.0, 0.0)),
               (17, on_floor((-9, 0, 0), 0, 0.0, 0.0)), (20, on_floor((6, 0, 2), 0, 0.02, 0.0)), (23, on_floor((70, 0, 4), 0.02, -0.14, 0.0)),
               (24, on_floor((82, 0, 4), 0.02, -0.17, 0.0)), (25, on_floor((76, 0, 4), 0.02, -0.16, 0.0)), (26, on_floor((82, 0, 4), 0.02, -0.17, 0.0))],
    "GripL": [(1, G(1)), (16, G(1)), (21, G(0))],
    "GripR": [(1, G(1)), (17, G(1)), (22, G(0))],
    "FreeL": [(1, F(-0.3, -0.5, 1.15)), (26, F(-0.34, -0.45, 1.1))],
    "FreeR": [(1, F(0.3, -0.5, 1.15)), (26, F(0.34, -0.45, 1.1))],
}), secs=2.8, after="GuardWalk")


def build():
    rng = kit.rng
    robe, dark = kit.mat("Soured", "#5e5074"), kit.mat("SouredDark", "#3e3450")
    linen, linen_dark = kit.mat("VeilLinen", "#aca38f"), kit.mat("VeilLinenDark", "#7d7666")
    eye, claw = kit.mat("SouredEye_Glow", "#c8f07a"), kit.mat("Claw", "#d6cdb8")
    iron, rust = kit.mat("AnchorIron", "#6c6b75"), kit.mat("AnchorRust", "#7a4a32")
    weed, barnacle = kit.mat("RiverWeed", "#4d5c38"), kit.mat("Barnacle", "#9d9786")
    hook = kit.mat("HookIron", "#3b3a42")
    # (the ring and the arms get their own materials, the same iron, so the game can find them: the chain ties to the
    # ring; the arms' lowest point is the crown that hits the floor)
    ring_iron, arms_iron = kit.mat("AnchorRing", "#6c6b75"), kit.mat("AnchorArms", "#6c6b75")

    # The robe (a cone to the floor) in two: the skirt below the knees stays on the floor (the body sinks into it
    # when she kneels), the robe above moves with her; the skirt a touch wider, so it sits over the robe's foot.
    def r_at(z):
        return 0.74 + (0.34 - 0.74) * (z + 0.75) / 1.5

    def section(z0, z1, scale, tattered):
        def build_it(bm):
            kit.cone(11, r_at(z0) * scale, r_at(z1) * scale, z1 - z0)(bm)
            for v in bm.verts:
                v.co.z += (z0 + z1) / 2
            if tattered:
                rim = sorted((v for v in bm.verts if v.co.z < z0 + 0.05 and v.co.length > 0.1), key=lambda v: math.atan2(v.co.y, v.co.x))
                for i, v in enumerate(rim):
                    v.co.z += 0.12 if i % 2 else 0.0  # tattered, silted hem
        return build_it

    parts = [
        part("Skirt", section(-0.75, -0.1, SKIRT_SCALE, True), robe, ROBE_AT, smooth=True, group="Hem"),
        part("Robe", section(-0.22, 0.75, 1.0, False), robe, ROBE_AT, smooth=True, group="Spine"),
        part("Hump", kit.sphere(10, 7), robe, M((0, 0.06, 1.38), scale=(0.56, 0.46, 0.38)), smooth=True, group="Chest"),
        part("Head", kit.sphere(10, 7, 0.26), dark, M((0, -0.3, 1.7), scale=(1, 1.05, 0.95)), smooth=True, group="Head"),
        # The veil: a tall pointed hood of old linen with a wimple band at the brow, and a long drape down her back.
        part("Veil", kit.sphere(10, 7), linen, M((0, -0.16, 1.8), scale=(0.34, 0.34, 0.37)), smooth=True, group="Head"),
        part("VeilPeak", kit.cone(8, 0.2, 0.03, 0.26), linen, M((0, -0.1, 2.12), rot=(-28, 0, 0)), smooth=True, group="Head"),
        part("Wimple", kit.cylinder(12, 0.29, 0.08), linen_dark, M((0, -0.32, 1.84), rot=(-22, 0, 0)), smooth=True, group="Head"),
        part("VeilDrape", kit.cone(9, 0.26, 0.44, 0.85), linen_dark, M((0, 0.16, 1.36), rot=(20, 0, 0)), smooth=True, group="Drape"),
    ]
    for s, arm, fore, shoulder, elbow, hand, tip in ((-1, "ArmL", "ForeL", SHOULDER_L, ELBOW_L, HAND_L, CLAW_TIP_L),
                                                     (1, "ArmR", "ForeR", SHOULDER_R, ELBOW_R, HAND_R, CLAW_TIP_R)):
        parts.append(part("Eye", kit.sphere(8, 6, 0.045), eye, M((s * 0.09, -0.54, 1.7)), smooth=True, group="Head"))
        # The arm in two (a sleeve to the elbow, a forearm to the claws), the elbow a knob of robe.
        parts.append(part("Arm", kit.cone(6, 0.14, 0.11, math.dist(shoulder, elbow) + 0.04), robe, kit.along(shoulder, elbow), smooth=True, group=arm))
        parts.append(part("Elbow", kit.sphere(8, 6, 0.112), robe, M(elbow), smooth=True, group=arm))
        parts.append(part("Forearm", kit.cone(6, 0.105, 0.08, math.dist(elbow, hand)), robe, kit.along(elbow, hand), smooth=True, group=fore))
        parts.append(part("Claw", kit.cone(5, 0.05, 0.0, 0.16), claw, kit.along(hand, tip), group=fore))

    # The chain round her waist (the anchor's chain is fastened to it with the Ferryman's hook, at the front).
    links = 22
    for i in range(links):
        a = 2 * math.pi * i / links
        x, y = 0.6 * math.cos(a), 0.56 * math.sin(a)
        parts.append(part("WaistLink", kit.cube(), iron, M((x, y, 0.98 + 0.03 * math.sin(3 * a)), rot=(90 * (i % 2), 0, math.degrees(a) + 90),
                                                            scale=(0.16, 0.05, 0.07)), group="Spine"))
    # The Ferryman's hook, closed over the waist chain at her belly: where the anchor's chain starts.
    k = 1.8
    hook_at = (0.0, -0.62, 1.0)

    def h(dx, dy, dz):
        return (hook_at[0] + dx * k, hook_at[1] + dy * k, hook_at[2] + dz * k)

    parts += [
        part("FerrymansHookShank", kit.cylinder(5, 0.024 * k, 0.2 * k), hook, M(h(0, 0, -0.1), rot=(0, 0, 0)), group="Hook"),
        part("FerrymansHookProngL", kit.tube([h(0, 0, -0.2), h(-0.07, 0, -0.23), h(-0.1, -0.02, -0.16)], [0.02 * k, 0.016 * k, 0.01 * k], seg=5), hook, group="Hook"),
        part("FerrymansHookProngR", kit.tube([h(0, 0, -0.2), h(0.07, 0, -0.23), h(0.1, -0.02, -0.16)], [0.02 * k, 0.016 * k, 0.01 * k], seg=5), hook, group="Hook"),
        part("FerrymansHookEye", kit.cylinder(6, 0.032 * k, 0.02 * k), hook, M(h(0, 0, 0.0), rot=(90, 0, 0)), group="Hook"),
    ]

    # The anchor, upright in front of her: ring on top, the stock across (a cross), the shank down to the crown, and
    # the two arms curving up either side into flukes (the crescent). River weed and barnacles on it; rust in the seams.
    y = -0.8
    ring = [(0.13 * math.cos(a), y, 1.66 + 0.13 * math.sin(a)) for a in (2 * math.pi * i / 12 for i in range(13))]
    arm = [(-0.66, y, 0.66), (-0.52, y, 0.36), (-0.3, y, 0.18), (0, y, 0.12), (0.3, y, 0.18), (0.52, y, 0.36), (0.66, y, 0.66)]
    anchor = [
        part("AnchorRing", kit.tube(ring, [0.035] * len(ring), seg=5), ring_iron),
        part("AnchorShank", kit.cylinder(7, 0.075, 1.5), iron, M((0, y, 0.8))),
        part("AnchorStock", kit.cube(), iron, M((0, y, 1.44), scale=(1.0, 0.1, 0.11))),
        part("AnchorStockEnd", kit.sphere(6, 5, 0.075), iron, M((-0.52, y, 1.44))),
        part("AnchorStockEnd", kit.sphere(6, 5, 0.075), iron, M((0.52, y, 1.44))),
        part("AnchorArms", kit.tube(arm, [0.06, 0.075, 0.085, 0.095, 0.085, 0.075, 0.06], seg=7), arms_iron),
        part("AnchorCrown", kit.sphere(8, 6, 0.12), rust, M((0, y, 0.13), scale=(1.2, 0.8, 0.8))),
        part("AnchorRust", kit.cube(), rust, M((0, y, 1.36), scale=(0.2, 0.11, 0.04))),
    ]
    for s in (-1, 1):
        anchor.append(part("AnchorFluke", kit.cube(), iron, M((s * 0.7, y, 0.74), rot=(0, s * 32, 0), scale=(0.2, 0.06, 0.3))))
        anchor.append(part("AnchorFlukeRust", kit.cube(), rust, M((s * 0.71, y - 0.01, 0.72), rot=(0, s * 32, 0), scale=(0.1, 0.06, 0.12))))
    for x, z, length in ((-0.34, 1.38, 0.34), (0.2, 1.38, 0.24), (0.42, 1.38, 0.3), (-0.05, 0.2, 0.18)):
        anchor.append(part("Weed", kit.cone(5, 0.04, 0.005, length), weed, M((x, y - 0.03, z - length / 2), rot=(180 + rng.uniform(-10, 10), 0, 0))))
    for i in range(9):
        t = rng.uniform(0.1, 0.9)
        x = -0.6 + 1.2 * t
        z = 0.12 + 0.5 * (x / 0.66) ** 2 + 0.02
        anchor.append(part("Barnacle", kit.cone(5, 0.035, 0.012, 0.03), barnacle, M((x, y - 0.07, z), rot=(90, 0, 0))))
    for p in anchor:
        p.vertex_groups.new(name="Anchor").add(list(range(len(p.data.vertices))), 1.0, "REPLACE")
    parts += anchor
    # A Soured's lantern, sickly green like her eyes, hung on a cord from the stock's end: a light for the ferry to see
    # by. It swings on its own (the Lantern bone, under the Anchor's).
    lantern_glass = kit.mat("SouredLantern_Glow", "#b6f06a")
    lx = 0.56
    parts += [
        part("BeltLanternCord", kit.cube(), iron, M((lx, y - 0.02, 1.33), scale=(0.015, 0.015, 0.12)), group="Lantern"),
        part("BeltLanternTop", kit.cone(4, 0.1, 0.01, 0.08), iron, M((lx, y - 0.02, 1.25), rot=(0, 0, 45)), group="Lantern"),
        part("BeltLanternGlass", kit.cube(), lantern_glass, M((lx, y - 0.02, 1.14), scale=(0.12, 0.12, 0.15)), group="Lantern"),
        part("BeltLanternBase", kit.cube(), iron, M((lx, y - 0.02, 1.055), scale=(0.14, 0.14, 0.02)), group="Lantern"),
    ]

    mesh = kit.join(parts, "Anchoress")
    armature = kit.rig("AnchoressRig", BONES)
    kit.skin(mesh, armature)
    kit.animate(armature, ACTIONS)
    for name, (anchor_off, chain_off) in REACH.items():
        if anchor_off > 0.02:
            print(f"[anchoress] {name}: the hands come off the anchor by up to {anchor_off * 100:.0f} cm")
        if chain_off > 0.12:
            print(f"[anchoress] {name}: the hands fall short of the chain by up to {chain_off * 100:.0f} cm")
    return [armature, mesh]
