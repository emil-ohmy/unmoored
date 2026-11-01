"""The Widow Hale, Who Kept the Minute (the Hall of the Last Hour's miniboss): the chief of the Keeners, the widow of the
Hall, who stopped the great clock itself at the minute of the death. Tall (a head and more over a Keener: 2.45 m to the
top of her cap), gaunt and stately in full mourning: a high-necked gown of black crape to the floor, its long train
behind her; a widow's cap (a white frill round its peak); a long black veil falling from the cap over her face to below
her waist in front (a pale lace hem at its foot) and down her back; under it a second, short veil, so her face is never
seen. Long pale hands. At her waist a chatelaine of stopped watches on chains, and on it, like every Soured's, the
little green lantern. A mourning brooch on her breast.

Clips (30 fps; the game's code is written against these names and lengths):
  Idle (loop, 3 s): her vigil: head bowed, hands folded, a slow breath; once a slow look round to the stair, and back.
  Walk (loop, 2 s): a glide (no steps under the gown), slow and stately; the train drags, the veil and watches swing.
  Breath (1.2 s): the knell's tell: she gathers, then rises, arching back, arms wide and high, head thrown back last.
  Toll (1.2 s): the knell: four rings (frames 6.4, 12.4, 18.4, 24.4): at each she jolts down and forward, arms driving
    down, the head whipping after a frame late; after the fourth she sags.
  TollRecover (1 s): she draws herself up, folding her hands (to Idle).
  VeilDraw (0.9 s): the veil's tell: she takes the veil's edge in her right hand, twists away (her right shoulder back),
    drawing it back and out low behind her right side, a lean back, the head turned to you.
  Veil (0.6 s): the cast: a long low sweep along the floor in front of her, right to left, about 140 degrees, its lace
    hem (VeilTip) about 4 m out at the impact (frame 8.6-9.1); the body unwinds first, the veil lags.
  VeilRecover (1 s): she draws the veil in hand over hand, lifts it and lets it fall back over her face (to Idle).
  Tend (loop, 2 s): leaning over a machine (its top 1.2 m up, 0.6 m before her), both hands flat on it, pressing it
    still, trembling, head bowed over her hands, a shudder now and then.
  Stagger (0.8 s): struck in running time: a jolt back as if burnt, veil flung, arms up, a stumble, gathered (to Idle).
  Death (2.5 s): she lets the minute go: straightens, lifts the veil back off her face with both hands, a last breath,
    and sinks to her knees, the veil and train settling round her, head bowed; still.

How it's animated (as the Laden and the Keener): each bone keyed on its own timing (kit.bake: tracks); the body leads,
the head follows late. The arms aren't keyed: the wrists' places are (TgtL / TgtR: in the chest's frame or the
floor's, or on the veil they grip) and kit.reach bends the arms to them; the hands are keyed by how they're turned. The
loose things aren't keyed: they're swung every frame by a little simulation (the skirt's hem, the veil's fall down her
back, the lantern and the watches on their chains pendulums that lie on her skirt, the train towed along the floor),
and the long veil is a chain of five bones simulated hanging from her cap, or, while she holds it, keyed where it goes
(K1..K4: around her, in degrees, metres out and up) and blended in. The veil stretches: its bones are moved apart
(offsets), and its cloth is shared between them so it draws out with them; its lace hem is all on VeilTip.
"""
import math

import bmesh
import bpy
from mathutils import Euler, Matrix, Quaternion, Vector

M, part = kit.M, kit.part  # `kit` is injected by build_model.py
V = Vector

# ---------------------------------------------------------------- the rig

HIP, WAIST, MID, NECK = 1.12, 1.3, 1.6, 1.9
SHOULDER_L, SHOULDER_R = (-0.19, 0.01, 1.84), (0.19, 0.01, 1.84)
ELBOW_L, ELBOW_R = (-0.22, 0.03, 1.46), (0.22, 0.03, 1.46)
WRIST_L, WRIST_R = (-0.235, 0.0, 1.11), (0.235, 0.0, 1.11)
PALM_L, PALM_R = (-0.235, 0.0, 1.03), (0.235, 0.0, 1.03)
FINGER_L, FINGER_R = (-0.235, 0.0, 0.92), (0.235, 0.0, 0.92)
CLASP = (-0.11, -0.15, 1.27)                          # the chatelaine's clasp, at her waist (front, left)
# The long veil's chain, at rest: from the cap's peak, over the face, to below the waist in front (J5: the lace's foot).
JR = [V((0, -0.165, 2.27)), V((0, -0.2, 2.0)), V((0, -0.225, 1.68)), V((0, -0.3, 1.33)), V((0, -0.32, 0.99)),
      V((0, -0.32, 0.85))]
VEIL_BONES = ["Veil", "Veil1", "Veil2", "Veil3", "VeilTip"]
TIP = (JR[5] - JR[4]).length                           # (the lace: VeilTip's length, never stretched)
TRAIN_R = [V((0, 0.6, 0.02)), V((0, 1.1, 0.02)), V((0, 1.75, 0.02))]

BONES = {
    "Root": ((0, 0, 0), (0, 0, 0.3), None),
    "Hips": ((0, 0, HIP), (0, 0, WAIST), "Root"),
    "Spine": ((0, 0, WAIST), (0, 0, MID), "Hips"),
    "Chest": ((0, 0, MID), (0, 0, NECK), "Spine"),
    "Head": ((0, 0, NECK), (0, -0.02, 2.42), "Chest"),
    "Skirt": ((0, 0, WAIST), (0, 0, 0.6), "Hips"),                  # the gown's skirts, from the waist
    "Hem": ((0, 0, 0.6), (0, 0, 0.02), "Skirt"),                    # their lower half and hem (swung; kept on the floor)
    "Train": (tuple(TRAIN_R[0]), tuple(TRAIN_R[1]), "Root"),        # the train, towed along the floor
    "Train1": (tuple(TRAIN_R[1]), tuple(TRAIN_R[2]), "Train"),
    "ArmL": (SHOULDER_L, ELBOW_L, "Chest"),
    "ForeL": (ELBOW_L, WRIST_L, "ArmL"),
    "HandL": (WRIST_L, FINGER_L, "ForeL"),
    "ArmR": (SHOULDER_R, ELBOW_R, "Chest"),
    "ForeR": (ELBOW_R, WRIST_R, "ArmR"),
    "HandR": (WRIST_R, FINGER_R, "ForeR"),
    "Lantern": (CLASP, (CLASP[0] - 0.02, CLASP[1] - 0.01, 0.9), "Hips"),     # (swung)
    "WatchA": (CLASP, (CLASP[0] + 0.05, CLASP[1], 1.04), "Hips"),            # the chatelaine's watches (swung)
    "WatchB": (CLASP, (CLASP[0] + 0.01, CLASP[1] - 0.01, 0.95), "Hips"),
    "WatchC": (CLASP, (CLASP[0] - 0.06, CLASP[1] + 0.01, 1.0), "Hips"),
    "BackFall": ((0, 0.12, 2.15), (0, 0.2, 1.35), "Head"),                   # the veil's fall down her back (swung)
    "Veil": (tuple(JR[0]), tuple(JR[1]), "Head"),
    "Veil1": (tuple(JR[1]), tuple(JR[2]), "Veil"),
    "Veil2": (tuple(JR[2]), tuple(JR[3]), "Veil1"),
    "Veil3": (tuple(JR[3]), tuple(JR[4]), "Veil2"),
    "VeilTip": (tuple(JR[4]), tuple(JR[5]), "Veil3"),
}
POLE_L, POLE_R = (-1, 0.5, -0.4), (1, 0.5, -0.4)       # the elbows bow out and back
PSEUDO = ("TgtL", "TgtR", "RotL", "RotR", "K1", "K2", "K3", "K4", "VeilHeld", "Gust")

# ---------------------------------------------------------------- the body's shapes (rest; also its collision)

# (z, radius, y offset): the gown's bell (squashed 0.88 front to back, fuller behind) and the bodice (0.75).
GOWN = [(1.33, 0.14, 0.0), (1.24, 0.17, 0.005), (1.1, 0.22, 0.015), (0.95, 0.28, 0.03), (0.8, 0.34, 0.045),
        (0.6, 0.41, 0.065), (0.4, 0.47, 0.085), (0.2, 0.52, 0.1), (0.06, 0.555, 0.115), (0.012, 0.56, 0.12)]
GOWN_SQ = 0.88
BODICE = [(1.27, 0.145), (1.36, 0.145), (1.48, 0.155), (1.62, 0.17), (1.74, 0.175), (1.82, 0.17), (1.88, 0.14),
          (1.92, 0.08), (1.96, 0.066), (2.06, 0.064)]
BODICE_SQ = 0.75
HEAD_C, HEAD_R = (0, -0.005, 2.16), 0.155


def profile_at(prof, z):
    """A profile's (radius, y offset) at height z, or None outside it."""
    zs = [p[0] for p in prof]
    if z > max(zs) or z < min(zs):
        return None
    for a, b in zip(prof, prof[1:]):
        hi, lo = (a, b) if a[0] >= b[0] else (b, a)
        if lo[0] <= z <= hi[0]:
            u = (z - lo[0]) / max(1e-6, hi[0] - lo[0])
            return (lo[1] + (hi[1] - lo[1]) * u, (lo[2] + (hi[2] - lo[2]) * u) if len(lo) > 2 else 0.0)
    return None


def push_ellipse(pos, mat, prof, sq, zlo, zhi, margin):
    local = mat.inverted() @ pos
    if not (zlo <= local.z <= zhi):
        return pos
    at = profile_at(prof, local.z)
    if at is None:
        return pos
    r, y0 = at
    r += margin
    x, y = local.x, (local.y - y0) / sq
    d = math.hypot(x, y)
    if d >= r:
        return pos
    if d < 1e-6:
        x, y, d = 0.0, -1.0, 1.0
    return mat @ V((x * r / d, y * r / d * sq + y0, local.z))


def push_sphere(pos, centre, r):
    d = pos - centre
    if d.length >= r:
        return pos
    return centre + (d.normalized() if d.length > 1e-6 else V((0, -1, 0))) * r


def outside(pos, m, margin=0.02, head=True, hands=()):
    """`pos` pushed out of her (the gown's bell, the bodice, the head; hands if given) and above the floor."""
    pos = push_ellipse(pos, m["Skirt"], GOWN, GOWN_SQ, 0.7, 1.34, margin)
    pos = push_ellipse(pos, m["Hem"], GOWN, GOWN_SQ, 0.0, 0.7, margin)
    pos = push_ellipse(pos, m["Spine"], BODICE, BODICE_SQ, 1.27, 1.6, margin)
    pos = push_ellipse(pos, m["Chest"], BODICE, BODICE_SQ, 1.6, 2.06, margin)
    if head:
        pos = push_sphere(pos, m["Head"] @ V(HEAD_C), HEAD_R + margin)
    for c in hands:
        pos = push_sphere(pos, c, 0.075)
    if pos.z < 0.03:
        pos = V((pos.x, pos.y, 0.03))
    return pos


# ---------------------------------------------------------------- keying vocabulary

def frame_q(d, w):
    """The rotation whose columns are (w, d, w x d) (w made perpendicular to d)."""
    d = V(d).normalized()
    w = V(w) - d * V(w).dot(d)
    if w.length < 1e-6:
        w = V((1, 0, 0)) - d * d.x
    w.normalize()
    return Matrix((w, d, w.cross(d))).transposed().to_quaternion()


def hand_rot(d, n, side):
    """A hand turned to point along `d`, its palm facing `n` (degrees, as animate() takes them). At rest a hand hangs
    (down), its palm in to the body."""
    rest = frame_q((0, 0, -1), (0, side, 0))      # (columns: the palm's width, its length, its normal)
    want = frame_q(d, V(d).cross(V(n)))
    q = want @ rest.inverted()
    return tuple(math.degrees(a) for a in q.to_euler("XYZ"))


def tgt(pos, w=0.0, grip=0.0):
    """A wrist's place: in the chest's frame (w 0) or the floor's (w 1); `grip`: how much it holds the veil instead."""
    return ((w, grip, 0.0), tuple(pos))


def kp(phi, r, z):
    """A veil joint keyed round her: `phi` degrees from straight ahead (+ to her right), `r` metres out, `z` up."""
    return ((phi, r, z), (0.0, 0.0, 0.0))


def vh(h, space=0.0, width=0.0, drape=0.0):
    """How much the veil is held where K1..K4 put it (0: it hangs free), the space they're in (0: round her feet, 1: as
    if on her head), its cloth's width (0: laid flat across where it goes, 1: across her head); `drape`: hanging free,
    how much it hangs towards K1..K4 instead of over her face (thrown back over her head, it stays there)."""
    return ((h, space, width), (drape, 0.0, 0.0))


def gust(x, y, z):
    """A wind on the veil (m/s^2, in her frame): its sweep and flutter."""
    return ((0.0, 0.0, 0.0), (x, y, z))


def held(pose, frame):
    return {bone: [(frame, value)] for bone, value in pose.items()}


def keys(*parts):
    merged = {}
    for p in parts:
        for bone, ks in p.items():
            merged.setdefault(bone, []).extend(ks)
    return merged


def shake(base, amp, start, end, every=2, seed=1, axes=(1, 0, 1)):
    """Keys trembling about `base` (a rotation) from `start` to `end`, a key every `every` frames."""
    out, k, f = [], seed, start
    while f <= end:
        k = (k * 7919 + 13) % 101
        s = (1 if (f // every) % 2 else -1) * (0.6 + 0.4 * (k / 100))
        out.append((f, tuple(b + a * s * amp for b, a in zip(base, axes))))
        f += every
    return out


# ---------------------------------------------------------------- the clips' machinery

def num(pose, name, slot):
    v = pose.get(name)
    if v is None:
        return (0.0, 0.0, 0.0)
    return v[slot] if isinstance(v[0], (tuple, list)) else (v if slot == 0 else (0.0, 0.0, 0.0))


def polar(phi, r, z):
    a = math.radians(phi)
    return V((r * math.sin(a), -r * math.cos(a), z))


SPACE_W = [1.0, 1.0, 1.0, 0.55, 0.25]   # (how much of `space` each joint takes: the far ones stay round her feet)


def veil_keys(pose, m, grips=None):
    """K1..K5 in the world, the held weight, and the cloth's width rule. With `grips` (the arms already bent), the
    veil is put in the hands that hold it (K1 in the gripping palm; the hem's middle between both palms)."""
    h, space, width = num(pose, "VeilHeld", 0)
    ks = [None]
    for i in range(1, 5):
        p = polar(*num(pose, f"K{i}", 0))
        s = space * SPACE_W[i]
        k = (m["Root"] @ p).lerp(m["Head"] @ p, s)
        k.z = max(k.z, 0.05)   # (along the floor, never in it, however low she bends)
        ks.append(k)
    if grips is not None:
        gl, gr = num(pose, "TgtL", 0)[1], num(pose, "TgtR", 0)[1]
        pl, pr = m["HandL"] @ V(PALM_L), m["HandR"] @ V(PALM_R)
        if grips is grip_cast and gl + gr > 1e-4:
            ks[1] = ks[1].lerp((pl * gl + pr * gr) / (gl + gr), min(1.0, gl + gr))
        elif grips is grip_corners and min(gl, gr) > 1e-4:
            ks[4] = ks[4].lerp((pl + pr) / 2, min(gl, gr))
    d = ks[4] - ks[3]
    d = d.normalized() if d.length > 1e-6 else V((0, 0, -1))
    tip = ks[4] + d * TIP
    if tip.z < 0.05:
        tip.z = 0.05
        tip = ks[4] + (tip - ks[4]).normalized() * TIP
    ks.append(tip)
    return ks, h, width


def place_hands(pose, grips):
    """Both arms bent to their wrists' places (or the veil, as much as they grip it), the hands turned as keyed."""
    m = kit.pose_matrices(BONES, pose)
    ks, _, _ = veil_keys(pose, m)
    head_x = m["Head"].to_3x3() @ V((1, 0, 0))
    for side, arm, fore, hand, wrist, palm, pole in ((-1, "ArmL", "ForeL", "HandL", WRIST_L, PALM_L, POLE_L),
                                                     (1, "ArmR", "ForeR", "HandR", WRIST_R, PALM_R, POLE_R)):
        (w, grip, _), at = pose["Tgt" + ("L" if side < 0 else "R")]
        rot = Euler([math.radians(a) for a in num(pose, "Rot" + ("L" if side < 0 else "R"), 0)]).to_quaternion()
        want = (m["Chest"].to_quaternion() @ rot).slerp(m["Root"].to_quaternion() @ rot, w)
        target = (m["Chest"] @ V(at)).lerp(m["Root"] @ V(at), w)
        if grip > 0 and grips:
            g = grips(side, ks, head_x)
            if g is not None:
                target = target.lerp(g - want @ (V(palm) - V(wrist)), grip)
        pose = kit.reach(BONES, pose, arm, fore, lambda mm, t=target: t, pole)
        mm = kit.pose_matrices(BONES, pose)
        r = mm[fore].to_quaternion().inverted() @ want
        pose[hand] = tuple(math.degrees(a) for a in r.to_euler("XYZ"))
    return pose


def grip_cast(side, ks, head_x):
    return ks[1]  # (either hand that grips holds the veil at K1)


def grip_corners(side, ks, head_x):
    return ks[4] + head_x * (0.2 * side)  # (both hands on the hem's corners)


def chain_pose(pose, m, names, points, widths):
    """Bones `names` placed along the world points (bone i from points[i] to points[i+1], moved apart if they're
    further than at rest: the cloth stretches), the cloth's width along `widths`."""
    world = {}
    for i, name in enumerate(names):
        head, tail, parent = BONES[name]
        d = points[i + 1] - points[i]
        if d.length < 1e-5:
            d = V(tail) - V(head)
        q = frame_q(d, widths[i])
        rest = frame_q(V(tail) - V(head), REST_W[name])
        qw = q @ rest.inverted()
        pm = world[parent] if parent in world else m[parent]
        r = pm.to_quaternion().inverted() @ qw
        off = pm.inverted() @ points[i] - V(head)
        pose[name] = (tuple(math.degrees(a) for a in r.to_euler("XYZ")), tuple(off))
        world[name] = Matrix.Translation(points[i]) @ qw.to_matrix().to_4x4() @ Matrix.Translation(-V(head))
    return pose


REST_W = {n: V((1, 0, 0)) for n in VEIL_BONES}
REST_W.update({"Train": V((-1, 0, 0)), "Train1": V((-1, 0, 0))})


def veil_width(d, head_x, width):
    """The cloth's width across a veil bone pointing `d`: laid flat across it (horizontal), or across her head."""
    flat = V((-d.y, d.x, 0.0))
    across = head_x
    w = flat + V((head_x.x, head_x.y, 0.0)) * 0.35
    if w.length < 1e-4:
        w = head_x
    return w.normalized().lerp(across, width)


HEM_RING = [V((0.585 * math.cos(a), 0.12 + 0.585 * GOWN_SQ * math.sin(a), 0.006)) for a in (2 * math.pi * i / 20 for i in range(20))]


def floor_hem(pose):
    """The hem lifted where it would go through the floor; lifted far (she kneels), the skirt's lower half squashed
    down and spread out instead: it pools on the floor. Returns the Hem's scale (across, along it)."""
    m = kit.pose_matrices(BONES, pose)
    low = min((m["Hem"] @ v).z for v in HEM_RING)
    if low >= 0.016:
        return (1.0, 1.0)
    need = 0.016 - low
    along = 1.0 if need <= 0.06 else max(0.35, 1 - (need - 0.06) * 1.1)
    need -= 0.588 * (1 - along)
    rot, offset = pose["Hem"]
    lift = m["Skirt"].to_quaternion().inverted() @ V((0, 0, need))
    pose["Hem"] = (rot, tuple(V(offset) + lift))
    return (1 + (1 - along) * 0.6, along)


# bone: (stiffness towards where its parent carries it, damping, gravity (g), collide, gust taken)
LOOSE = {
    "Hem": (70.0, 6.5, 0.0, False, 0.0),
    "BackFall": (14.0, 4.5, 1.0, True, 0.5),
    "Lantern": (0.0, 4.0, 1.0, True, 0.3),
    "WatchA": (0.0, 5.0, 1.0, True, 0.3),
    "WatchB": (0.0, 4.5, 1.0, True, 0.3),
    "WatchC": (0.0, 5.5, 1.0, True, 0.3),
}
SUB = 6
DT = 1.0 / 30 / SUB


def run(n, periodic, step, start, settle):
    """Drive a simulation over the clip's frames: `step(a, b, state) -> state` (from frame a to b), from
    `start(frame)`. A loop runs three times round (its seam joins); a one-off settles at its first frame and eases to
    rest over its last (the next clip begins settled). Returns each frame's state (as `settle` lerps them)."""
    cycle = n - 1 if periodic else n
    order = list(range(cycle)) * (3 if periodic else 1)
    state = start(order[0])
    for _ in range(100):
        state = step(order[0], order[0], state)
    out = {order[0]: state}
    for a, b in zip(order, order[1:] + order[:1] if periodic else order[1:]):
        state = step(a, b, state)
        out[b] = state
    if periodic:
        out[n - 1] = out[0]
    else:
        rest = out[n - 1]
        for _ in range(150):
            rest = step(n - 1, n - 1, rest)
        ease = min(8, n // 3)
        for i in range(n - ease, n):
            u = (i - (n - 1 - ease)) / ease
            out[i] = settle(out[i], rest, u * u * (3 - 2 * u), i)
    return [out[i] for i in range(n)]


def lerp_points(a, b, u):
    return ([p.lerp(q, u) for p, q in zip(a[0], b[0])], a[1])


def loose(frames, periodic, bones):
    """Swing the loose bones by their weight (a point at each one's tail: a spring towards where its parent carries it,
    gravity, the gust; pushed out of her)."""
    mats = [kit.pose_matrices(BONES, p) for _, p in frames]
    gusts = [mats[i]["Root"].to_3x3() @ V(num(p, "Gust", 1)) for i, (_, p) in enumerate(frames)]
    n = len(frames)
    for bone in bones:
        k, c, g, collide, gk = LOOSE[bone]
        head, tail, parent = BONES[bone]
        length = (V(tail) - V(head)).length
        rest_dir = (V(tail) - V(head)).normalized()

        def at(i):
            return mats[i][bone] @ V(head), mats[i][bone] @ V(tail)

        def step(a, b, state):
            pos, vel = state
            ha, ta = at(a)
            hb, tb = at(b)
            for s in range(SUB):
                u = (s + 1) / SUB
                h, t = ha.lerp(hb, u), ta.lerp(tb, u)
                acc = (t - pos) * k - vel * c + V((0, 0, -9.81 * g)) + gusts[b] * gk
                new = pos + (vel + acc * DT) * DT
                d = new - h
                new = h + d.normalized() * length if d.length > 1e-6 else t
                if collide:
                    new = outside(new, mats[b], 0.025, head=bone == "BackFall")
                    if (new - pos).length > 0.012:   # (pushed out of her skirts, it slides over them; it doesn't pop)
                        new = pos + (new - pos).normalized() * 0.012
                        d = new - h
                        new = h + d.normalized() * length if d.length > 1e-6 else t
                vel = (new - pos) / DT
                pos = new
            return pos, vel

        states = run(n, periodic, step, lambda i: (at(i)[1], V()),
                     lambda s, r, u, i: (s[0].lerp(r[0], u), s[1]))
        for i, (_, p) in enumerate(frames):
            h = mats[i][bone] @ V(head)
            inward = mats[i][parent].to_quaternion().inverted() @ (states[i][0] - h)
            q = rest_dir.rotation_difference(inward.normalized())
            p[bone] = (tuple(math.degrees(a) for a in q.to_euler("XYZ")), (0.0, 0.0, 0.0))


def towed(frames, periodic):
    """The train: its front follows the hem's back, along the floor; each later joint towed after the one before (it
    drags, it doesn't spring), drawn back behind her a little at a time."""
    mats = [kit.pose_matrices(BONES, p) for _, p in frames]
    n = len(frames)
    lens = [(TRAIN_R[1] - TRAIN_R[0]).length, (TRAIN_R[2] - TRAIN_R[1]).length]

    def front(i):
        p = mats[i]["Hem"] @ TRAIN_R[0]
        return V((p.x, p.y, 0.02))

    def behind(i):
        d = mats[i]["Hips"].to_3x3() @ V((0, 1, 0))
        d.z = 0
        return d.normalized()

    def step(a, b, state):
        pts = list(state)
        for s in range(SUB):
            u = (s + 1) / SUB
            p0 = front(a).lerp(front(b), u)
            back = behind(a).lerp(behind(b), u).normalized()
            prev = p0
            for j in (1, 2):
                d = pts[j] - prev
                d.z = 0
                d = d.normalized() if d.length > 1e-6 else back
                d = d.lerp(back, 0.02 if j == 1 else 0.012).normalized()
                pts[j] = prev + d * lens[j - 1]
                pts[j].z = 0.02
                back = d
                prev = pts[j]
            pts[0] = p0
        return pts

    def start(i):
        p0, back = front(i), behind(i)
        return [p0, p0 + back * lens[0], p0 + back * (lens[0] + lens[1])]

    states = run(n, periodic, step, start, lambda s, r, u, i: [a.lerp(b, u) for a, b in zip(s, r)])
    for i, (_, p) in enumerate(frames):
        pts = states[i]
        widths = [V((-(pts[j + 1] - pts[j]).y, (pts[j + 1] - pts[j]).x, 0)) for j in (0, 1)]
        chain_pose(p, mats[i], ["Train", "Train1"], pts, widths)


VEIL_K = [0.0, 95.0, 70.0, 55.0, 45.0, 45.0]
VEIL_LEN = [(JR[i + 1] - JR[i]).length for i in range(5)]


def veil(frames, periodic):
    """The long veil: hanging from her cap (springs towards where it'd hang, a little gravity, the gust; inextensible;
    pushed out of her), blended into where K1..K5 put it as much as it's held (and fed back, so let go it falls from
    there)."""
    mats = [kit.pose_matrices(BONES, p) for _, p in frames]
    n = len(frames)
    info = []
    for i, (_, p) in enumerate(frames):
        m = mats[i]
        j0 = m["Head"] @ JR[0]
        fwd = m["Head"].to_3x3() @ V((0, -1, 0))
        yaw = math.atan2(fwd.x, -fwd.y) if math.hypot(fwd.x, fwd.y) > 1e-3 else 0.0
        turn = Matrix.Rotation(yaw, 3, "Z")
        targets = [j0]
        for k in range(1, 6):
            rigid = m["Head"] @ JR[k]
            plumb = j0 + turn @ (JR[k] - JR[0])
            targets.append(rigid.lerp(plumb, 0.65))
        ks, h, width = veil_keys(p, m, GRIPS[0])
        drape = num(p, "VeilHeld", 1)[0]
        targets = [targets[0]] + [targets[k].lerp(ks[k], drape) for k in range(1, 6)]
        palms = [m["HandL"] @ V(PALM_L), m["HandR"] @ V(PALM_R)]
        info.append((j0, targets, ks, h, m["Root"].to_3x3() @ V(num(p, "Gust", 1)), palms))

    def step(a, b, state):
        pos, vel = state
        pos, vel = list(pos), list(vel)
        A, B = info[a], info[b]
        for s in range(SUB):
            u = (s + 1) / SUB
            j0 = A[0].lerp(B[0], u)
            h = A[3] + (B[3] - A[3]) * u
            g = A[4].lerp(B[4], u)
            new = [j0]
            for k in range(1, 6):
                t = A[1][k].lerp(B[1][k], u)
                acc = (t - pos[k]) * VEIL_K[k] - vel[k] * 9.0 + V((0, 0, -9.81 * 0.5)) + g
                v = vel[k] + acc * DT
                if v.length > 5.0:   # (a free veil never flies faster than this: struck, it flutters, it doesn't fly off)
                    v = v.normalized() * 5.0
                q = pos[k] + v * DT
                d = q - new[k - 1]
                q = new[k - 1] + (d.normalized() if d.length > 1e-6 else V((0, 0, -1))) * VEIL_LEN[k - 1]
                q = outside(q, mats[b], 0.03, head=k <= 2, hands=B[5])
                new.append(q)
            for k in range(1, 6):
                kk = A[2][k].lerp(B[2][k], u)
                new[k] = new[k].lerp(kk, h)
                vel[k] = (new[k] - pos[k]) / DT
                pos[k] = new[k]
            pos[0] = j0
        return pos, vel

    states = run(n, periodic, step, lambda i: (list(info[i][1]), [V() for _ in range(6)]),
                 lambda s, r, u, i: ([a.lerp(b, u * (1 - info[i][3])) for a, b in zip(s[0], r[0])], s[1]))
    for i, (_, p) in enumerate(frames):
        pts = states[i][0]
        pts[0] = info[i][0]
        m = mats[i]
        head_x = m["Head"].to_3x3() @ V((1, 0, 0))
        _, h, width = veil_keys(p, m)
        widths = []
        for k in range(5):
            d = pts[k + 1] - pts[k]
            held_w = veil_width(d.normalized() if d.length > 1e-6 else V((0, 0, -1)), head_x, width)
            widths.append(head_x.lerp(held_w, min(1.0, h * 1.5)))
        chain_pose(p, m, VEIL_BONES, pts, widths)
        VEIL_PATH.setdefault(CURRENT[0], []).append([V(x) for x in pts])


import os
DEBUG = os.environ.get("WIDOW_DEBUG")
REACH = {}
VEIL_PATH = {}
CURRENT = [None]
HEM_SCALE = {}
GRIPS = [None]


def clip(name, length, tracks, periodic=False, grips=grip_cast):
    """A clip `length` frames long (30 a second; frames 1..length+1) from per-bone tracks, baked to every frame: the
    arms bent to their wrists' places, the loose things swung, the train towed, the veil hung or held, the hem kept off
    the floor."""
    CURRENT[0] = name
    GRIPS[0] = grips
    frames = kit.bake(tracks, length, periodic)
    for _, p in frames:
        for bone in ("TgtL", "TgtR"):
            p.setdefault(bone, tgt(WRIST_L if bone == "TgtL" else WRIST_R))
        p.update(place_hands(p, grips))
    loose(frames, periodic, ["Hem"])
    HEM_SCALE[name] = [floor_hem(p) for _, p in frames]
    loose(frames, periodic, ["BackFall", "Lantern", "WatchA", "WatchB", "WatchC"])
    towed(frames, periodic)
    veil(frames, periodic)
    worst = 0.0
    for f, p in frames:
        m = kit.pose_matrices(BONES, p)
        keyed, _, _ = veil_keys(p, m)
        ks, _, _ = veil_keys(p, m, grips)
        head_x = m["Head"].to_3x3() @ V((1, 0, 0))
        veil_at = {1: m["Veil1"] @ JR[1], 4: m["VeilTip"] @ JR[4]}
        for side, hand, palm, t in ((-1, "HandL", PALM_L, "TgtL"), (1, "HandR", PALM_R, "TgtR")):
            grip = p[t][0][1]
            if grip > 0.5 and grips and p["VeilHeld"][0][0] > 0.95:   # (once she has it, not while she takes it)
                at = m[hand] @ V(palm)
                short = (at - grips(side, keyed, head_x)).length
                if short > 0.05 and DEBUG:
                    print(f"[widow] {name} f{f} {hand} short of its keyed grip by {short * 100:.0f} cm")
                # (on the veil itself: K1 is the palm; the hem's corners either side of its middle)
                g = veil_at[1] if grips is grip_cast else veil_at[4] + head_x * (0.2 * side)
                worst = max(worst, (at - g).length)
                if DEBUG and (at - g).length > 0.05:
                    print(f"[widow] {name} f{f} {hand} palm {tuple(round(c, 2) for c in at)} veil {tuple(round(c, 2) for c in g)} K {tuple(round(c, 2) for c in ks[1])} h {p['VeilHeld'][0][0]:.2f}")
        for b in PSEUDO:
            p.pop(b, None)
    REACH[name] = worst
    return ("LINEAR", frames)


# ---------------------------------------------------------------- poses

FOLD_L = hand_rot((0.8, -0.25, -0.5), (0.0, 0.7, -0.3), -1)
FOLD_R = hand_rot((-0.8, -0.3, -0.45), (0.0, 0.7, -0.4), 1)
# Her vigil (Idle's first frame: Walk begins on it, the one-offs end on it): upright, the head bowed, the hands folded.
VIGIL = {"Root": ((0, 0, 0), (0, 0, 0)), "Hips": (0, 0, 0), "Spine": (2, 0, 0), "Chest": (4, 0, 0), "Head": (22, 0, 0),
         "Skirt": (0, 0, 0), "TgtL": tgt((-0.1, -0.2, 1.3)), "TgtR": tgt((0.09, -0.235, 1.33)),
         "RotL": FOLD_L, "RotR": FOLD_R, "VeilHeld": vh(0), "Gust": gust(0, 0, 0)}

ACTIONS = {}

# Idle (3 s loop): a slow breath (the chest lifting, the head with it a little later), twice; between them she turns
# her head to the stair (her right, over the shoulder: the body turning first, a little), holds, listening, and turns
# back, bowing again. The veil and the chatelaine stir after her, and with a breath of air.
ACTIONS["Idle"] = clip("Idle", 90, {
    "Root": [(1, ((0, 0, 0), (0, 0, 0))), (20, ((0, 0, 0), (0, 0, 0.012))), (42, ((0, 0, 0), (0, 0, -0.004))),
             (66, ((0, 0, 0), (0, 0, 0.01)))],
    "Hips": [(1, (0, 0, 0)), (40, (0, 0, 2)), (70, (0, 0, 0))],
    "Spine": [(1, (2, 0, 0)), (20, (0, 0, 0)), (40, (2, 0, 3)), (62, (1, 0, 3)), (78, (3, 0, 0))],
    "Chest": [(1, (4, 0, 0)), (22, (0, 0, 0)), (42, (5, 0, 5)), (64, (1, 0, 5)), (80, (5, 0, 0))],
    "Head": [(1, (22, 0, 0)), (16, (20, 0, 0)), (34, (16, 0, 14)), (44, (10, -2, 34)), (56, (9, -2, 36)),
             (68, (14, 0, 20)), (80, (24, 0, 2))],
    "Skirt": [(1, (0, 0, 0)), (46, (0, 0, 1)), (76, (0, 0, -0.5))],
    "TgtL": [(1, VIGIL["TgtL"]), (22, tgt((-0.1, -0.205, 1.31))), (50, tgt((-0.1, -0.2, 1.3))),
             (70, tgt((-0.1, -0.205, 1.305)))],
    "TgtR": [(1, VIGIL["TgtR"]), (24, tgt((0.09, -0.24, 1.335))), (52, tgt((0.09, -0.235, 1.33))),
             (72, tgt((0.09, -0.24, 1.335)))],
    "RotL": [(1, FOLD_L)], "RotR": [(1, FOLD_R)], "VeilHeld": [(1, vh(0))],
    "Gust": [(1, gust(0, 0, 0)), (24, gust(0.4, 0.6, 0.2)), (50, gust(-0.2, 0.2, 0)), (74, gust(0.3, -0.3, 0.1))],
}, periodic=True)

# Walk (2 s loop): a glide: no steps, the gown to the floor; she rises a little and settles twice a cycle, sways side to
# side with it (the chest after the hips, the head after the chest, keeping level); the air of her going streams the
# veil back; the train drags; the watches and the lantern swing. Frame 1 is her vigil.
ACTIONS["Walk"] = clip("Walk", 60, {
    "Root": [(1, ((0, 0, 0), (0, 0, 0))), (16, ((0, 2.0, 0), (0.018, 0, 0.026))), (31, ((0, 0, 0), (0, 0, 0))),
             (46, ((0, -2.0, 0), (-0.018, 0, 0.026)))],
    "Hips": [(1, (0, 0, 0)), (16, (0, 0, 5)), (31, (0, 0, 0)), (46, (0, 0, -5))],
    "Spine": [(1, (2, 0, 0)), (17, (3, -0.5, -1.5)), (31, (2, 0, 0)), (47, (3, 0.5, 1.5))],
    "Chest": [(1, (4, 0, 0)), (19, (5, -1.5, -3)), (31, (4, 0, 0)), (49, (5, 1.5, 3))],
    "Head": [(1, (22, 0, 0)), (21, (19, 0.8, -1.5)), (31, (22, 0, 0)), (51, (19, -0.8, 1.5))],
    "Skirt": [(4, (0, -1, 2)), (19, (1, 1, -3)), (34, (0, 1, -2)), (49, (1, -1, 3))],
    "TgtL": [(1, VIGIL["TgtL"]), (18, tgt((-0.1, -0.2, 1.305))), (31, VIGIL["TgtL"]), (48, tgt((-0.1, -0.2, 1.305)))],
    "TgtR": [(1, VIGIL["TgtR"]), (19, tgt((0.09, -0.235, 1.335))), (31, VIGIL["TgtR"]), (49, tgt((0.09, -0.235, 1.335)))],
    "RotL": [(1, FOLD_L)], "RotR": [(1, FOLD_R)], "VeilHeld": [(1, vh(0))],
    "Gust": [(1, gust(0, 1.2, 0.2)), (16, gust(0.4, 1.6, 0.3)), (31, gust(0, 1.2, 0.2)), (46, gust(-0.4, 1.6, 0.3))],
}, periodic=True)

# The breath drawn (Breath ends on it, Toll begins on it): risen, arched back, the arms wide and high (the palms up and
# open), the head thrown back.
WIDE_L = hand_rot((-0.4, 0.1, 0.9), (0.2, -0.6, 0.6), -1)
WIDE_R = hand_rot((0.4, 0.1, 0.9), (-0.2, -0.6, 0.6), 1)
BREATH_FULL = {"Root": ((0, 0, 0), (0, 0.03, 0.05)), "Hips": (-2, 0, 0), "Spine": (-7, 0, 0), "Chest": (-10, 0, 0),
               "Head": (-22, 0, 0), "Skirt": (2, 0, 0), "TgtL": tgt((-0.66, 0.02, 2.38), 1), "TgtR": tgt((0.66, 0.02, 2.38), 1),
               "RotL": WIDE_L, "RotR": WIDE_R, "VeilHeld": vh(0), "Gust": gust(0, 1.0, 1.0)}
TUCK_L = hand_rot((0.85, -0.2, -0.3), (0.0, 0.8, -0.2), -1)
TUCK_R = hand_rot((-0.85, -0.25, -0.3), (0.0, 0.8, -0.2), 1)

# Breath (1.2 s, the tell): she gathers (sinking, curling over her folded hands, which draw in tight: frame 8), then
# rises out of it, the chest opening and arching back, the arms sweeping out and up wide (the hands overshooting high
# at 25), the head thrown back last (28); the veil swept back off her by the breath; held at the top, trembling.
ACTIONS["Breath"] = clip("Breath", 36, keys(held(VIGIL, 1), {
    "Root": [(8, ((0, 0, 0), (0, -0.01, -0.045))), (20, ((0, 0, 0), (0, 0.02, 0.04))), (26, ((0, 0, 0), (0, 0.03, 0.065))),
             (31, ((0, 0, 0), (0, 0.03, 0.045))), (37, BREATH_FULL["Root"])],
    "Hips": [(9, (3, 0, 0)), (22, (-3, 0, 0)), (37, BREATH_FULL["Hips"])],
    "Spine": [(7, (12, 0, 0)), (19, (-4, 0, 0)), (25, (-9, 0, 0)), (37, BREATH_FULL["Spine"])],
    "Chest": [(8, (15, 0, 0)), (20, (-5, 0, 0)), (26, (-13, 0, 0)), (30, (-9, 0, 0))] + shake((-10, 0, 0), 0.8, 32, 37, 2, 3),
    "Head": [(9, (34, 0, 0)), (19, (14, 0, 0)), (24, (-8, 0, 0)), (28, (-27, 0, 0)), (31, (-20, 0, 0))] + shake((-22, 0, 0), 1.2, 33, 37, 2, 5),
    "Skirt": [(9, (-2, 0, 0)), (24, (3, 0, 0)), (37, BREATH_FULL["Skirt"])],
    "TgtL": [(8, tgt((-0.07, -0.17, 1.4))), (16, tgt((-0.38, -0.32, 1.6), 0.6)), (22, tgt((-0.62, -0.12, 2.2), 1)),
             (26, tgt((-0.68, 0.04, 2.44), 1)), (31, tgt((-0.65, 0.02, 2.36), 1)), (34, tgt((-0.66, 0.025, 2.39), 1)),
             (37, BREATH_FULL["TgtL"])],
    "TgtR": [(8, tgt((0.06, -0.2, 1.42))), (17, tgt((0.38, -0.32, 1.6), 0.6)), (23, tgt((0.62, -0.12, 2.2), 1)),
             (27, tgt((0.68, 0.04, 2.44), 1)), (32, tgt((0.65, 0.02, 2.36), 1)), (35, tgt((0.66, 0.025, 2.39), 1)),
             (37, BREATH_FULL["TgtR"])],
    "RotL": [(8, TUCK_L), (17, hand_rot((-0.6, -0.4, -0.5), (0.3, -0.5, 0.4), -1)), (26, WIDE_L), (37, WIDE_L)],
    "RotR": [(8, TUCK_R), (18, hand_rot((0.6, -0.4, -0.5), (-0.3, -0.5, 0.4), 1)), (27, WIDE_R), (37, WIDE_R)],
    "Gust": [(12, gust(0, 0, 0)), (20, gust(0, 3.0, 3.0)), (26, gust(0, 2.0, 1.5)), (37, BREATH_FULL["Gust"])],
}))

# Toll (1.2 s, the knell): four rings, at frames 6.4, 12.4, 18.4, 24.4 (0.18, 0.38, 0.58, 0.78 s). At each she jolts
# down and forward to her deepest (the body leads: the root, the spine, the chest), the arms driving down from on
# high to before her knees as if on a bell rope, the head whipping down a frame after; between, she rebounds and the
# arms come back up (less each time). After the fourth she sags, spent, the arms hanging.
RINGS = (6.4, 12.4, 18.4, 24.4)
DOWN_L = hand_rot((0.1, -0.5, -0.85), (0.6, 0.3, 0.2), -1)
DOWN_R = hand_rot((-0.1, -0.5, -0.85), (-0.6, 0.3, 0.2), 1)
HANG_L = hand_rot((0, -0.1, -1), (0.9, 0.3, 0), -1)
HANG_R = hand_rot((0, -0.1, -1), (-0.9, 0.3, 0), 1)
SAG = {"Root": ((0, 0, 0), (0, -0.04, -0.06)), "Hips": (3, 0, 0), "Spine": (12, 0, 0), "Chest": (14, 0, 0),
       "Head": (30, 0, 0), "Skirt": (-2, 0, 0), "TgtL": tgt((-0.24, -0.4, 1.16), 1), "TgtR": tgt((0.24, -0.4, 1.16), 1),
       "RotL": HANG_L, "RotR": HANG_R, "VeilHeld": vh(0), "Gust": gust(0, 0, 0)}


def ring(base, deep, up):
    """Keys for the four rings: `deep(i)` at each ring's frame, `up(i)` between (2.6 frames before the next)."""
    out = []
    for i, r in enumerate(RINGS):
        if i:
            out.append((r - 2.6, up(i)))
        out.append((r, deep(i)))
    return out


def rk(z, y=0.0):
    return ((0, 0, 0), (0, y, z))


ACTIONS["Toll"] = clip("Toll", 36, keys(held(BREATH_FULL, 1), {
    "Root": [(3, rk(0.065, 0.03))] + ring(None, lambda i: rk(-0.05 - 0.012 * i, -0.04 - 0.005 * i),
                                         lambda i: rk(0.01 - 0.008 * i, -0.02)) + [(27, rk(-0.04, -0.035)), (31, rk(-0.07, -0.045)), (37, SAG["Root"])],
    "Hips": [(3, (-3, 0, 0))] + ring(None, lambda i: (4, 0, 0), lambda i: (0, 0, 0)) + [(37, SAG["Hips"])],
    "Spine": [(3, (-11, 0, 0))] + ring(None, lambda i: (11 + 1.5 * i, 0, 0), lambda i: (1 + 2 * i, 0, 0)) +
             [(28, (9, 0, 0)), (32, (14, 0, 0)), (37, SAG["Spine"])],
    "Chest": [(3.5, (-14, 0, 0))] + [(r + 0.4, (13 + 1.5 * i, 0, (-1) ** i * 2)) for i, r in enumerate(RINGS)] +
             [(r - 2.2, (0 + 2 * i, 0, 0)) for i, r in enumerate(RINGS) if i] + [(28.5, (10, 0, 0)), (32.5, (16, 0, 0)), (37, SAG["Chest"])],
    "Head": [(4, (-30, 0, 0))] + [(r + 1, (16, 0, (-1) ** i * 4)) for i, r in enumerate(RINGS)] +
            [(r - 1.6, (-4 + 3 * i, 0, 0)) for i, r in enumerate(RINGS) if i] + [(29.5, (20, 0, 0)), (33.5, (32, 0, 0)), (37, SAG["Head"])],
    "Skirt": [(r + 1, (-3, 0, 0)) for r in RINGS] + [(r - 2, (1, 0, 0)) for r in RINGS[1:]] + [(37, SAG["Skirt"])],
    "TgtL": [(3, tgt((-0.6, -0.04, 2.4), 1))] + ring(None, lambda i: tgt((-0.28 - 0.01 * i, -0.5, 1.32 - 0.03 * i), 1),
                                                     lambda i: tgt((-0.4 + 0.02 * i, -0.4, 1.78 - 0.07 * i), 1)) +
            [(29, tgt((-0.25, -0.46, 1.2), 1)), (37, SAG["TgtL"])],
    "TgtR": [(3, tgt((0.6, -0.04, 2.4), 1))] + ring(None, lambda i: tgt((0.28 + 0.01 * i, -0.5, 1.32 - 0.03 * i), 1),
                                                    lambda i: tgt((0.4 - 0.02 * i, -0.4, 1.78 - 0.07 * i), 1)) +
            [(29, tgt((0.25, -0.46, 1.2), 1)), (37, SAG["TgtR"])],
    "RotL": [(3, WIDE_L)] + ring(None, lambda i: DOWN_L, lambda i: hand_rot((0, -0.6, 0.5), (0.7, 0.2, 0.2), -1)) + [(37, HANG_L)],
    "RotR": [(3, WIDE_R)] + ring(None, lambda i: DOWN_R, lambda i: hand_rot((0, -0.6, 0.5), (-0.7, 0.2, 0.2), 1)) + [(37, HANG_R)],
    "Gust": [(2, gust(0, -2.0, -3.0)), (6, gust(0, -6.0, -6.0)), (12, gust(0, -3.0, -4.0)), (20, gust(0, -2.0, -2.0)), (37, gust(0, 0, 0))],
}))

# TollRecover (1 s): spent, a heave for breath (the body dropping a last time, the head after it), then she draws
# herself up, the hands coming together and folding before her, the head bowing: her vigil.
ACTIONS["TollRecover"] = clip("TollRecover", 30, keys(held(SAG, 1), held(VIGIL, 31), {
    "Root": [(6, rk(-0.075, -0.045)), (12, rk(-0.05, -0.035)), (20, rk(0.012, -0.01)), (25, rk(0.005))],
    "Hips": [(8, (3, 0, 0)), (20, (0, 0, 0))],
    "Spine": [(6, (19, 0, 0)), (11, (15, 0, 0)), (20, (2, 0, 0)), (26, (1, 0, 0))],
    "Chest": [(7, (20, 0, 0)), (12, (15, 0, 0)), (21, (2, 0, 0)), (27, (3, 0, 0))],
    "Head": [(9, (40, 0, 0)), (14, (32, 0, 0)), (22, (14, 0, 0)), (28, (24, 0, 0))],
    "TgtL": [(7, tgt((-0.24, -0.4, 1.12), 1)), (15, tgt((-0.2, -0.32, 1.22), 0.6)), (22, tgt((-0.11, -0.21, 1.32))),
             (27, tgt((-0.1, -0.2, 1.295)))],
    "TgtR": [(7, tgt((0.24, -0.4, 1.12), 1)), (16, tgt((0.2, -0.32, 1.24), 0.6)), (23, tgt((0.1, -0.24, 1.34))),
             (28, tgt((0.09, -0.235, 1.325)))],
    "RotL": [(8, HANG_L), (20, FOLD_L)], "RotR": [(9, HANG_R), (21, FOLD_R)],
}))

# The veil's keys, as it hangs from her cap at her vigil (where her hand takes it), round her: (degrees, out, up).
HANG_K = {"K1": kp(0, 0.24, 1.98), "K2": kp(0, 0.29, 1.66), "K3": kp(0, 0.33, 1.32), "K4": kp(0, 0.34, 0.98)}
# Wound (VeilDraw ends on it, Veil begins on it): twisted away, her right shoulder back, leaning back, the veil drawn
# out low behind her right side and trailing on the floor behind, the head turned to you, the left arm across before
# her.
WOUND = {"Root": ((0, 0, 0), (0, 0.04, -0.03)), "Hips": (0, 0, 22), "Spine": (-6, 2, 18), "Chest": (-6, 4, 16),
         "Head": (8, 0, -44), "Skirt": (0, 0, -6),
         "TgtL": tgt((-0.14, -0.5, 1.42), 1), "TgtR": tgt((0.52, 0.5, 1.0), 1, 1),
         "RotL": hand_rot((0.3, -0.8, 0.2), (0.4, 0.2, -0.8), -1), "RotR": hand_rot((0.4, 0.6, -0.6), (0.6, -0.6, 0.2), 1),
         "K1": kp(134, 0.72, 1.0), "K2": kp(146, 1.4, 0.36), "K3": kp(156, 2.05, 0.06), "K4": kp(160, 2.7, 0.05),
         "VeilHeld": vh(1), "Gust": gust(0, 0, 0)}

# VeilDraw (0.9 s, the tell): the right hand rises to take the veil's edge before her chest (frame 8; the left hand
# lets go of it); then she twists away (the hips first, the spine, the chest), drawing it off her face, back and out
# low behind her right side, paying it out along the floor behind her, leaning back; the head turns to keep you.
ACTIONS["VeilDraw"] = clip("VeilDraw", 27, keys(held(VIGIL, 1), {
    "Root": [(8, rk(-0.012)), (20, rk(-0.04, 0.04)), (28, WOUND["Root"])],
    "Hips": [(8, (0, 0, -4)), (17, (0, 0, 18)), (23, (0, 0, 24)), (28, WOUND["Hips"])],
    "Spine": [(8, (5, 0, -4)), (18, (-4, 2, 15)), (24, (-7, 2, 20)), (28, WOUND["Spine"])],
    "Chest": [(9, (7, 0, -6)), (19, (-4, 3, 13)), (25, (-7, 4, 18)), (28, WOUND["Chest"])],
    "Head": [(9, (18, 0, 6)), (16, (12, 0, -20)), (22, (8, 0, -48)), (28, WOUND["Head"])],
    "Skirt": [(10, (0, 0, 2)), (22, (0, 0, -8)), (28, WOUND["Skirt"])],
    "TgtL": [(6, tgt((-0.12, -0.26, 1.36))), (14, tgt((-0.2, -0.42, 1.4), 1)), (28, WOUND["TgtL"])],
    "TgtR": [(4, tgt((0.12, -0.3, 1.45))), (8, tgt((0.05, -0.32, 1.64), 0, 1)), (14, tgt((0.4, -0.1, 1.4), 0.6, 1)),
             (20, tgt((0.58, 0.3, 1.08), 1, 1)), (24, tgt((0.54, 0.52, 0.98), 1, 1)), (28, WOUND["TgtR"])],
    "RotL": [(6, FOLD_L), (16, WOUND["RotL"]), (28, WOUND["RotL"])],
    "RotR": [(5, hand_rot((-0.3, -0.5, 0.6), (0.2, 0.7, 0.3), 1)), (9, hand_rot((-0.4, -0.3, -0.6), (0.3, 0.8, 0), 1)),
             (20, WOUND["RotR"]), (28, WOUND["RotR"])],
    "K1": [(1, HANG_K["K2"]), (8, kp(-8, 0.32, 1.64)), (14, kp(40, 0.45, 1.38)), (20, kp(112, 0.68, 1.06)), (24, kp(132, 0.73, 0.99)), (28, WOUND["K1"])],
    "K2": [(1, HANG_K["K3"]), (9, kp(0, 0.36, 1.3)), (16, kp(70, 0.75, 0.9)), (22, kp(132, 1.2, 0.42)), (26, kp(144, 1.4, 0.34)), (28, WOUND["K2"])],
    "K3": [(1, HANG_K["K4"]), (10, kp(0, 0.4, 1.0)), (18, kp(100, 1.0, 0.4)), (24, kp(146, 1.75, 0.1)), (28, WOUND["K3"])],
    "K4": [(1, kp(0, 0.36, 0.85)), (11, kp(0, 0.42, 0.75)), (20, kp(120, 1.5, 0.15)), (25, kp(154, 2.45, 0.05)), (28, WOUND["K4"])],
    "VeilHeld": [(5, vh(0)), (10, vh(1)), (28, vh(1))],
}))

# Through (Veil ends on it, VeilRecover begins on it): unwound past, turned to her left, bent forward, the veil
# lying out along the floor to her left.
THROUGH = {"Root": ((0, 0, 0), (0, -0.05, -0.06)), "Hips": (2, 0, -26), "Spine": (14, -2, -24), "Chest": (10, -2, -16),
           "Head": (18, 0, 14), "Skirt": (0, 0, 6),
           "TgtL": tgt((-0.45, 0.3, 1.2), 1), "TgtR": tgt((-0.58, -0.36, 0.98), 1, 1),
           "RotL": hand_rot((-0.3, 0.4, -0.8), (0.8, 0, 0.2), -1), "RotR": hand_rot((-0.8, 0.2, -0.5), (0.2, -0.4, -0.8), 1),
           "K1": kp(-122, 0.68, 0.98), "K2": kp(-120, 1.6, 0.12), "K3": kp(-112, 2.6, 0.05), "K4": kp(-104, 3.45, 0.05),
           "VeilHeld": vh(1), "Gust": gust(0, 0, 0)}

# Veil (0.6 s, the cast; it lands at 45%, frames 8.6-9.1): the body unwinds first (the hips, then the spine and the
# chest), bending forward and down into it; the arm whips the veil round low and out, right to left; the veil follows
# it, each part of it later than the one before, so it draws out straight and long along the floor: its lace hem
# sweeps an arc in front of her about 4 m out (from 70 degrees to her right to 70 to her left), level with the floor.
# Follow-through to her left, the veil lying out there; the head on it.
ACTIONS["Veil"] = clip("Veil", 18, keys(held(WOUND, 1), {
    "Root": [(4, rk(-0.02, 0.02)), (9, rk(-0.07, -0.04)), (13, rk(-0.075, -0.06)), (19, THROUGH["Root"])],
    "Hips": [(3, (0, 0, 14)), (7, (2, 0, -10)), (11, (2, 0, -28)), (14, (2, 0, -30)), (19, THROUGH["Hips"])],
    "Spine": [(4, (0, 1, 12)), (8, (12, -1, -8)), (12, (16, -2, -28)), (15, (15, -2, -28)), (19, THROUGH["Spine"])],
    "Chest": [(5, (-2, 2, 10)), (9, (12, 0, -6)), (13, (12, -2, -20)), (16, (11, -2, -19)), (19, THROUGH["Chest"])],
    "Head": [(4, (8, 0, -40)), (8, (16, 0, -16)), (12, (20, 0, 8)), (16, (18, 0, 16)), (19, THROUGH["Head"])],
    "Skirt": [(6, (0, 0, -2)), (12, (0, 0, 8)), (19, THROUGH["Skirt"])],
    "TgtL": [(5, tgt((-0.3, -0.3, 1.36), 1)), (10, tgt((-0.5, 0.15, 1.3), 1)), (19, THROUGH["TgtL"])],
    "TgtR": [(4, tgt((0.66, 0.06, 1.0), 1, 1)), (7, tgt((0.42, -0.62, 0.95), 1, 1)), (9, tgt((-0.1, -0.78, 0.92), 1, 1)),
             (11, tgt((-0.5, -0.6, 0.92), 1, 1)), (14, tgt((-0.64, -0.38, 0.96), 1, 1)), (19, THROUGH["TgtR"])],
    "RotL": [(8, hand_rot((-0.6, 0.4, -0.6), (0.6, 0, 0.4), -1)), (19, THROUGH["RotL"])],
    "RotR": [(5, hand_rot((0.4, -0.6, -0.6), (-0.6, 0.6, 0.4), 1)), (9, hand_rot((0, -1, -0.4), (0, 0.4, -0.9), 1)),
             (13, hand_rot((-0.9, -0.2, -0.4), (0.3, 0.2, -0.9), 1)), (19, THROUGH["RotR"])],
    # (each joint later than the last: the hand leads; at the impact the hem passes straight ahead, ~4 m out)
    "K1": [(4, kp(96, 0.67, 1.0)), (7, kp(36, 0.75, 0.95)), (9, kp(-8, 0.79, 0.92)), (11, kp(-50, 0.78, 0.92)),
           (14, kp(-82, 0.74, 0.96)), (19, THROUGH["K1"])],
    "K2": [(4, kp(136, 1.45, 0.3)), (6, kp(96, 1.55, 0.32)), (8, kp(32, 1.8, 0.3)), (10, kp(-22, 1.85, 0.26)),
           (12, kp(-62, 1.8, 0.2)), (15, kp(-98, 1.7, 0.14)), (19, THROUGH["K2"])],
    "K3": [(4, kp(150, 2.1, 0.08)), (6, kp(118, 2.3, 0.12)), (8, kp(44, 2.75, 0.12)), (10, kp(-10, 2.9, 0.1)),
           (12, kp(-48, 2.85, 0.08)), (15, kp(-86, 2.7, 0.06)), (19, THROUGH["K3"])],
    "K4": [(4, kp(158, 2.75, 0.05)), (6, kp(134, 3.0, 0.08)), (7.5, kp(70, 3.6, 0.08)), (9, kp(4, 3.92, 0.06)),
           (11, kp(-48, 3.85, 0.05)), (13, kp(-74, 3.7, 0.05)), (16, kp(-96, 3.55, 0.05)), (19, THROUGH["K4"])],
}))

# VeilRecover (1 s): she draws the veil in, hand over hand (the right hand pulls in, the left reaches out past it and
# takes it, pulls; the right again), turning back to the front as it comes; then gathers it up, lifts it over her
# head and lets it fall back over her face; the hands fold: her vigil.
ACTIONS["VeilRecover"] = clip("VeilRecover", 30, keys(held(THROUGH, 1), held(VIGIL, 31), {
    "Root": [(6, rk(-0.04, -0.03)), (14, rk(-0.02, -0.01)), (22, rk(0.02)), (27, rk(0.0))],
    "Hips": [(6, (2, 0, -20)), (14, (1, 0, -10)), (20, (0, 0, -2))],
    "Spine": [(6, (10, -1, -18)), (14, (6, 0, -8)), (20, (-4, 0, 0)), (26, (2, 0, 0))],
    "Chest": [(7, (6, -1, -12)), (15, (4, 0, -6)), (21, (-6, 0, 0)), (27, (4, 0, 0))],
    "Head": [(7, (16, 0, 10)), (15, (12, 0, 4)), (21, (-4, 0, 0)), (24, (6, 0, 0)), (28, (24, 0, 0))],
    "Skirt": [(10, (0, 0, 3)), (20, (0, 0, -1))],
    # (right pulls 1-6, left takes it at 7 and pulls to 12, right takes it at 13 and pulls to 17; both lift it 17-22)
    "TgtR": [(5, tgt((-0.12, -0.3, 1.25), 1, 1)), (7, tgt((-0.05, -0.28, 1.3), 1, 0.0)), (11, tgt((-0.2, -0.58, 1.1), 1, 0)),
             (13, tgt((-0.22, -0.6, 1.08), 1, 1)), (17, tgt((0.12, -0.28, 1.5), 1, 1)), (21, tgt((0.12, -0.24, 2.3), 1, 1)),
             (23, tgt((0.16, -0.22, 2.3), 1, 0)), (27, tgt((0.12, -0.26, 1.45)))],
    "TgtL": [(5, tgt((-0.62, -0.42, 1.0), 1, 0)), (7, tgt((-0.6, -0.46, 1.0), 1, 1)), (12, tgt((-0.14, -0.3, 1.3), 1, 1)),
             (13, tgt((-0.14, -0.3, 1.32), 1, 0)), (17, tgt((-0.12, -0.28, 1.5), 1, 0.6)), (21, tgt((-0.12, -0.24, 2.3), 1, 0.6)),
             (23, tgt((-0.16, -0.22, 2.3), 1, 0)), (27, tgt((-0.12, -0.24, 1.42)))],
    "RotR": [(5, hand_rot((-0.8, -0.3, -0.4), (0, 0.5, -0.8), 1)), (13, hand_rot((-0.4, -0.7, -0.5), (0, 0.4, -0.9), 1)),
             (21, hand_rot((-0.3, 0, 0.9), (0, 1, 0), 1)), (27, FOLD_R)],
    "RotL": [(5, hand_rot((-0.3, -0.7, -0.5), (0.4, 0.2, -0.9), -1)), (12, hand_rot((0.8, -0.3, -0.4), (0, 0.5, -0.8), -1)),
             (21, hand_rot((0.3, 0, 0.9), (0, 1, 0), -1)), (27, FOLD_L)],
    "K1": [(5, kp(-24, 0.33, 1.26)), (7, kp(-48, 0.75, 1.02)), (12, kp(-26, 0.34, 1.3)), (13, kp(-20, 0.64, 1.08)),
           (17, kp(14, 0.28, 1.5)), (21, kp(0, 0.24, 2.3)), (24, kp(0, 0.25, 2.1)), (29, HANG_K["K1"])],
    "K2": [(5, kp(-80, 1.2, 0.3)), (10, kp(-56, 0.95, 0.5)), (15, kp(-20, 0.7, 0.9)), (19, kp(0, 0.4, 1.4)), (22, kp(0, 0.36, 1.9)),
           (29, HANG_K["K2"])],
    "K3": [(5, kp(-100, 2.0, 0.06)), (10, kp(-74, 1.5, 0.08)), (15, kp(-36, 1.05, 0.3)), (19, kp(0, 0.6, 1.0)), (23, kp(0, 0.4, 1.5)),
           (29, HANG_K["K3"])],
    "K4": [(5, kp(-102, 2.9, 0.05)), (10, kp(-82, 2.2, 0.05)), (15, kp(-48, 1.45, 0.06)), (19, kp(-8, 0.8, 0.5)), (23, kp(0, 0.45, 1.1)),
           (29, HANG_K["K4"])],
    "VeilHeld": [(18, vh(1)), (24, vh(0.6)), (28, vh(0))],
}))

# Tend (2 s loop): leaning over the machine (its top 1.2 m up, 0.6 m before her), both hands laid flat on it and
# pressing down, the elbows out, the head bowed close over her hands; she trembles with the effort, the whole time,
# and twice a shudder runs through her (frames 18 and 44): the chest and the head jolt down and the shoulders hunch.
FLAT_L = hand_rot((0.15, -1, -0.12), (0, 0, -1), -1)
FLAT_R = hand_rot((-0.15, -1, -0.12), (0, 0, -1), 1)
TEND_L, TEND_R = (-0.14, -0.53, 1.235), (0.14, -0.53, 1.235)   # (the wrists, the palms flat 0.6 m out at 1.2 m)


def tremble(base, amp, seed):
    ks = shake(base, amp, 1, 60, 2, seed)
    return [k for k in ks if k[0] <= 59]


ACTIONS["Tend"] = clip("Tend", 60, {
    "Root": [(1, rk(-0.03, -0.04)), (17, rk(-0.032, -0.04)), (19, rk(-0.045, -0.05)), (24, rk(-0.032, -0.04)),
             (43, rk(-0.03, -0.04)), (45, rk(-0.045, -0.05)), (50, rk(-0.032, -0.04))],
    "Hips": [(1, (4, 0, 0))],
    "Spine": [(f, v) for f, v in tremble((12, 0, 0), 0.5, 2) if not 16 <= f <= 24 and not 42 <= f <= 50] +
             [(18, (16, 0, 1)), (22, (11, 0, 0)), (44, (16, 0, -1)), (48, (11, 0, 0))],
    "Chest": [(f, v) for f, v in tremble((14, 0, 0), 0.7, 3) if not 17 <= f <= 25 and not 43 <= f <= 51] +
             [(19, (20, 0, -1)), (23, (13, 0, 0)), (45, (20, 0, 1)), (49, (13, 0, 0))],
    "Head": [(f, v) for f, v in tremble((30, 0, 0), 1.0, 4) if not 18 <= f <= 26 and not 44 <= f <= 52] +
            [(20, (42, 0, 3)), (24, (30, 0, 0)), (46, (42, 0, -3)), (50, (30, 0, 0))],
    "Skirt": [(1, (-2, 0, 0)), (30, (-2, 0, 0.5))],
    "TgtL": [(1, tgt(TEND_L, 1))], "TgtR": [(1, tgt(TEND_R, 1))],
    "RotL": [(1, FLAT_L)], "RotR": [(1, FLAT_R)], "VeilHeld": [(1, vh(0))],
    "Gust": [(1, gust(0, 0, 0)), (20, gust(0, 0, 1.0)), (30, gust(0, 0, 0)), (46, gust(0, 0, 1.0))],
}, periodic=True)

# Stagger (0.8 s): struck inside running time: a jolt back as if burnt (the waist first, the head whipping back after),
# the arms flung up and out, the veil flung up off her; a stumble back, a sway forward past her rest, catching
# herself; she gathers herself: her vigil.
ACTIONS["Stagger"] = clip("Stagger", 24, keys(held(VIGIL, 1), held(VIGIL, 25), {
    "Root": [(3, ((-3, 0, 2), (0, 0.08, 0.02))), (8, ((-1, 2, 0), (0.02, 0.18, -0.02))), (12, ((2, 0, -1), (0.01, 0.16, -0.04))),
             (17, ((1, 0, 0), (0, 0.06, -0.01))), (21, rk(0.004))],
    "Hips": [(3, (-4, 0, 4)), (9, (2, 0, -2)), (16, (1, 0, 0))],
    "Spine": [(3, (-14, 0, 4)), (8, (-6, 2, 0)), (12, (10, 0, -2)), (17, (5, 0, 0))],
    "Chest": [(4, (-18, 0, 6)), (9, (-6, 0, -2)), (13, (12, 0, -2)), (18, (6, 0, 0))],
    "Head": [(2, (26, 0, 0)), (5, (-30, 0, 8)), (10, (-6, 0, -6)), (14, (30, 0, 2)), (19, (20, 0, 0))],
    "Skirt": [(4, (3, 0, -3)), (10, (-2, 0, 2)), (16, (1, 0, 0))],
    "TgtL": [(4, tgt((-0.5, -0.12, 2.15), 1)), (9, tgt((-0.42, -0.25, 1.75), 0.8)), (15, tgt((-0.16, -0.24, 1.36), 0.2)),
             (21, tgt((-0.1, -0.21, 1.31)))],
    "TgtR": [(4, tgt((0.52, -0.1, 2.1), 1)), (9, tgt((0.44, -0.24, 1.7), 0.8)), (16, tgt((0.14, -0.26, 1.38), 0.2)),
             (22, tgt((0.09, -0.24, 1.335)))],
    "RotL": [(4, hand_rot((-0.5, -0.2, 0.8), (0.3, -0.9, 0), -1)), (10, hand_rot((-0.2, -0.4, -0.8), (0.7, 0.3, 0), -1)), (18, FOLD_L)],
    "RotR": [(4, hand_rot((0.5, -0.2, 0.8), (-0.3, -0.9, 0), 1)), (10, hand_rot((0.2, -0.4, -0.8), (-0.7, 0.3, 0), 1)), (19, FOLD_R)],
    "Gust": [(2, gust(0, -9, 9)), (6, gust(0, -4, 5)), (12, gust(0, 0, 0))],
}))

# Death (2.5 s): she lets the minute go. She straightens (frames 1-12), takes the hem of her veil in both hands and
# lifts it, up before her face and over her head and back (12-36), her face still unseen (the short veil under it);
# lets it fall down her back; a last breath (the chest rising, 40-50, and out); then she sinks slowly to her knees
# (50-70), the gown pooling and the train lying round her on the floor, the hands coming to rest in her lap and the
# head bowing; a last settle, and still.
KNEEL = {"Root": ((0, 0, 0), (0, 0.02, -0.6)), "Hips": (-2, 0, 0), "Spine": (8, 0, 0), "Chest": (10, 0, 0),
         "Head": (40, 0, 0), "Skirt": (-2, 0, 0), "TgtL": tgt((-0.12, -0.3, 1.17), 1), "TgtR": tgt((0.11, -0.33, 1.2), 1),
         "RotL": FOLD_L, "RotR": FOLD_R,
         "K1": kp(180, 0.02, 2.44), "K2": kp(180, 0.2, 2.3), "K3": kp(180, 0.4, 1.8), "K4": kp(180, 0.6, 1.0),
         "VeilHeld": vh(0, 0.3, 1, 1), "Gust": gust(0, 0, 0)}
ACTIONS["Death"] = clip("Death", 75, keys(held(VIGIL, 1), held(KNEEL, 76), {
    "Root": [(10, rk(0.025)), (34, rk(0.03)), (42, rk(0.02)), (47, rk(0.045)), (52, rk(0.01)), (58, rk(-0.12, 0.01)),
             (64, rk(-0.38, 0.015)), (69, rk(-0.58, 0.02)), (72, rk(-0.615, 0.02)), (74, rk(-0.6, 0.02))],
    "Hips": [(12, (-2, 0, 0)), (50, (-2, 0, 0)), (60, (3, 0, 0)), (68, (-3, 0, 0))],
    "Spine": [(8, (-2, 0, 0)), (13, (9, 0, 0)), (20, (-2, 0, 0)), (30, (-6, 0, 0)), (42, (-2, 0, 0)), (48, (-8, 0, 0)), (54, (4, 0, 0)), (62, (10, 0, 0)), (68, (6, 0, 0))],
    "Chest": [(8, (-3, 0, 0)), (14, (8, 0, 0)), (21, (-3, 0, 0)), (30, (-8, 0, 0)), (42, (-2, 0, 0)), (49, (-12, 0, 0)), (55, (6, 0, 0)), (63, (12, 0, 0)), (69, (8, 0, 0))],
    "Head": [(8, (8, 0, 0)), (15, (24, 0, 0)), (22, (-6, 0, 0)), (30, (-16, 0, 0)), (40, (-4, 0, 0)), (50, (-14, 0, 0)), (57, (14, 0, 0)),
             (66, (34, 0, 0)), (71, (44, 0, 0)), (74, (40, 0, 0))],
    "TgtL": [(8, tgt((-0.12, -0.24, 1.3))), (13, tgt((-0.22, -0.4, 1.12), 1, 1)), (22, tgt((-0.22, -0.42, 1.95), 1, 1)),
             (28, tgt((-0.22, -0.26, 2.5), 1, 1)), (34, tgt((-0.24, 0.12, 2.42), 1, 1)), (38, tgt((-0.26, 0.2, 2.2), 1, 0)),
             (44, tgt((-0.22, -0.2, 1.65), 1)), (52, tgt((-0.12, -0.27, 1.42), 1)), (64, tgt((-0.12, -0.3, 1.2), 1))],
    "TgtR": [(8, tgt((0.1, -0.25, 1.32))), (14, tgt((0.22, -0.4, 1.12), 1, 1)), (22, tgt((0.22, -0.42, 1.95), 1, 1)),
             (28, tgt((0.22, -0.26, 2.5), 1, 1)), (34, tgt((0.24, 0.12, 2.42), 1, 1)), (38, tgt((0.26, 0.2, 2.2), 1, 0)),
             (45, tgt((0.22, -0.2, 1.65), 1)), (53, tgt((0.11, -0.3, 1.44), 1)), (65, tgt((0.11, -0.33, 1.22), 1))],
    "RotL": [(8, FOLD_L), (14, hand_rot((0.2, -0.3, 0.9), (0.2, 0.9, 0.2), -1)), (28, hand_rot((0.3, 0.3, 0.9), (0.2, 0.3, -0.9), -1)),
             (38, hand_rot((0.1, 0.6, -0.8), (0.9, 0, 0), -1)), (50, FOLD_L)],
    "RotR": [(8, FOLD_R), (15, hand_rot((-0.2, -0.3, 0.9), (-0.2, 0.9, 0.2), 1)), (28, hand_rot((-0.3, 0.3, 0.9), (-0.2, 0.3, -0.9), 1)),
             (38, hand_rot((-0.1, 0.6, -0.8), (-0.9, 0, 0), 1)), (51, FOLD_R)],
    # (the hem taken at 14, up before the face 22, over the head 28, behind it 34; let go down her back)
    "K1": [(1, HANG_K["K1"]), (12, HANG_K["K1"]), (20, kp(0, 0.3, 1.85)), (26, kp(0, 0.28, 2.3)), (32, kp(180, 0.02, 2.47)), (40, KNEEL["K1"])],
    "K2": [(1, HANG_K["K2"]), (12, HANG_K["K2"]), (20, kp(0, 0.38, 1.6)), (26, kp(0, 0.34, 2.3)), (32, kp(180, 0.12, 2.42)), (42, KNEEL["K2"])],
    "K3": [(1, HANG_K["K3"]), (12, HANG_K["K3"]), (20, kp(0, 0.42, 1.55)), (26, kp(0, 0.3, 2.42)), (32, kp(180, 0.2, 2.38)), (44, KNEEL["K3"])],
    "K4": [(1, HANG_K["K4"]), (10, HANG_K["K4"]), (14, kp(0, 0.4, 1.12)), (22, kp(0, 0.44, 1.98)), (28, kp(0, 0.28, 2.52)), (34, kp(180, 0.15, 2.44)),
           (40, kp(180, 0.32, 2.0)), (48, KNEEL["K4"])],
    "VeilHeld": [(10, vh(0)), (14, vh(1, 0, 1)), (30, vh(1, 0, 1)), (36, vh(1, 1, 1, 1)), (46, vh(0, 0.3, 1, 1))],
    "Gust": [(44, gust(0, 0, 0)), (48, gust(0, 0.5, 1.0)), (54, gust(0, 0, 0))],
}), grips=grip_corners)


# ---------------------------------------------------------------- the mesh

def lathe(prof, seg, sq, wave=None):
    """A surface of revolution through the profile's rings (z, radius, y offset), squashed `sq` front to back, capped;
    `wave(angle, z)` a radius factor (folds)."""
    def build(bm):
        rings = []
        for row in prof:
            z, r = row[0], row[1]
            y0 = row[2] if len(row) > 2 else 0.0
            ring = []
            for k in range(seg):
                a = 2 * math.pi * k / seg
                f = wave(a, z) if wave else 1.0
                ring.append(bm.verts.new((r * f * math.cos(a), y0 + r * f * sq * math.sin(a), z)))
            rings.append(ring)
        for a, b in zip(rings, rings[1:]):
            for k in range(seg):
                bm.faces.new((a[k], a[(k + 1) % seg], b[(k + 1) % seg], b[k]))
        bm.faces.new(rings[0])
        bm.faces.new(list(reversed(rings[-1])))
        import bmesh
        bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    return build


def sheet(rows, cols=7, thick=0.008):
    """A cloth with some thickness: `rows` of (centre, half width, bulge: the edges drawn back) across x, at rest."""
    def build(bm):
        import bmesh
        front, back = [], []
        for c, hw, bulge in rows:
            f, b = [], []
            for j in range(cols):
                u = -1 + 2 * j / (cols - 1)
                p = V(c) + V((u * hw, bulge * u * u, 0))
                f.append(bm.verts.new(p + V((0, -thick / 2, 0))))
                b.append(bm.verts.new(p + V((0, thick / 2, 0))))
            front.append(f)
            back.append(b)
        for i in range(len(rows) - 1):
            for j in range(cols - 1):
                bm.faces.new((front[i][j], front[i][j + 1], front[i + 1][j + 1], front[i + 1][j]))
                bm.faces.new((back[i][j], back[i + 1][j], back[i + 1][j + 1], back[i][j + 1]))
            for j in (0, cols - 1):
                bm.faces.new((front[i][j], front[i + 1][j], back[i + 1][j], back[i][j]))
        for i in (0, len(rows) - 1):
            for j in range(cols - 1):
                bm.faces.new((front[i][j], back[i][j], back[i][j + 1], front[i][j + 1]))
        bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    return build


def slab(rows, cols=7):
    """The train: rows of (y, half width, bottom, top) along the floor."""
    def build(bm):
        import bmesh
        top, bot = [], []
        for y, hw, z0, z1 in rows:
            t, b = [], []
            for j in range(cols):
                u = -1 + 2 * j / (cols - 1)
                curl = 0.025 * u ** 4
                t.append(bm.verts.new((u * hw, y, z1 + curl)))
                b.append(bm.verts.new((u * hw, y, z0 + curl)))
            top.append(t)
            bot.append(b)
        for i in range(len(rows) - 1):
            for j in range(cols - 1):
                bm.faces.new((top[i][j], top[i][j + 1], top[i + 1][j + 1], top[i + 1][j]))
                bm.faces.new((bot[i][j], bot[i + 1][j], bot[i + 1][j + 1], bot[i][j + 1]))
            for j in (0, cols - 1):
                bm.faces.new((top[i][j], top[i + 1][j], bot[i + 1][j], bot[i][j]))
        for i in (0, len(rows) - 1):
            for j in range(cols - 1):
                bm.faces.new((top[i][j], bot[i][j], bot[i][j + 1], top[i][j + 1]))
        bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    return build


def weigh(mesh, temp, fn):
    """The parts grouped `temp` weighted by fn(rest position) -> {bone: weight}."""
    groups = mesh.vertex_groups
    t = groups[temp]
    for v in [v for v in mesh.data.vertices if any(g.group == t.index for g in v.groups)]:
        for bone, w in fn(v.co).items():
            if w > 1e-4:
                (groups.get(bone) or groups.new(name=bone)).add([v.index], w, "REPLACE")
    groups.remove(t)


def by_height(points):
    """Weights shared between bones by height: points [(z, bone)] from low to high, eased between."""
    def fn(co):
        if co.z <= points[0][0]:
            return {points[0][1]: 1.0}
        for (z0, a), (z1, b) in zip(points, points[1:]):
            if co.z <= z1:
                s = (co.z - z0) / (z1 - z0)
                s = s * s * (3 - 2 * s)
                return {a: 1 - s, b: s} if a != b else {a: 1.0}
        return {points[-1][1]: 1.0}
    return fn


def veil_weights(co):
    """The long veil's cloth shared between its bones by where it is along them (so it stretches as they part)."""
    for i in range(4):
        z0, z1 = JR[i].z, JR[i + 1].z
        if co.z >= z1 - 1e-6:
            u = min(1.0, max(0.0, (z0 - co.z) / (z0 - z1)))
            return {VEIL_BONES[i]: 1 - u, VEIL_BONES[i + 1]: u}
    return {"VeilTip": 1.0}


def train_weights(co):
    y0, y1 = TRAIN_R[0].y, TRAIN_R[1].y
    if co.y <= y0:
        return {"Hem": 1.0}
    if co.y >= y1:
        return {"Train1": 1.0}
    u = (co.y - y0) / (y1 - y0)
    return {"Train": 1 - u, "Train1": u}


def build():
    import bmesh  # noqa: F401
    crape, dark = kit.mat("WidowCrape", "#1c1a20"), kit.mat("WidowCrapeDark", "#0e0d11")
    veil_mat = kit.mat("WidowVeil", "#24212a")
    skin, lace = kit.mat("WidowSkin", "#d9d6cc"), kit.mat("WidowLace", "#ddd8cb")
    frill = kit.mat("WidowFrill", "#e6e2d8")
    gold, silver = kit.mat("WidowBrooch_Metal", "#b8964a"), kit.mat("WidowWatch_Metal", "#c8c4b4")
    face, jet = kit.mat("WidowWatchFace", "#efe9d8"), kit.mat("WidowJet", "#111014")
    iron, glass = kit.mat("WidowIron", "#2f2d33"), kit.mat("SouredLantern_Glow", "#b6f06a")

    parts = []
    # The gown: a bell of crape to the floor (its lower half swings: shared with the Hem by height), falling in folds
    # towards the hem; a darker band at the hem; a sash at the waist.
    folds = lambda a, z: 1.0 + 0.035 * math.cos(9 * a) * max(0.0, 1 - z / 1.0)
    parts += [
        part("Gown", lathe(GOWN, 27, GOWN_SQ, folds), crape, smooth=True, group="GownBlend"),
        part("HemBand", lathe([(0.1, 0.546, 0.112), (0.006, 0.566, 0.12)], 27, GOWN_SQ,
                              lambda a, z: 1.0 + 0.035 * math.cos(9 * a) * (1 - z) + 0.006), dark, smooth=True, group="Hem"),
        part("Sash", lathe([(1.34, 0.152, 0.0), (1.26, 0.168, 0.005)], 16, 0.8), dark, smooth=True, group="Hips"),
    ]
    # The train: out from under the gown's back, along the floor, a little wider at its end, rounded.
    rows = []
    for k in range(15):
        y = 0.36 + (1.75 - 0.36) * k / 14
        t = (y - 0.36) / (1.75 - 0.36)
        hw = 0.33 + 0.12 * math.sin(min(1.0, t * 1.4) * math.pi / 2)
        if y > 1.5:
            hw *= math.sqrt(max(0.05, 1 - ((y - 1.5) / 0.25) ** 2))
        top = 0.032 + 0.28 * max(0.0, (0.75 - y) / 0.39) ** 1.5
        rows.append((y, hw, 0.012, top))
    parts.append(part("Train", slab(rows), crape, smooth=True, group="TrainCloth"))
    # The bodice, buttoned to a high collar (it bends between the hips, the waist and the chest; the collar with the
    # head); narrow sloping shoulders.
    parts += [
        part("Bodice", lathe(BODICE, 16, BODICE_SQ), crape, smooth=True, group="BodiceBlend"),
        part("Shoulders", kit.sphere(12, 7), crape, M((0, 0.005, 1.8), scale=(0.235, 0.135, 0.08)), smooth=True, group="Chest"),
    ]
    # The brooch on her breast: an oval of jet in a gold rim.
    parts += [
        part("BroochRim", kit.cylinder(12, 0.036, 0.012), gold, M((-0.085, -0.128, 1.72), rot=(90, 0, 0), scale=(0.8, 1, 1)), group="Chest"),
        part("Brooch", kit.cylinder(12, 0.026, 0.014), jet, M((-0.085, -0.133, 1.72), rot=(90, 0, 0), scale=(0.8, 1, 1)), group="Chest"),
    ]
    # The head: under a short veil of crape (no face to see), the widow's cap over it, peaked at the brow, a white
    # frill round its edge.
    frill_pts = [(0.148 * math.sin(a), -0.01 - 0.148 * math.cos(a), 2.245 + 0.04 * (1 - math.cos(a)) - 0.025 * math.cos(a) ** 8)
                 for a in (math.radians(-110 + 220 * k / 14) for k in range(15))]
    parts += [
        part("ShortVeil", kit.sphere(12, 9), dark, M(HEAD_C, scale=(0.118, 0.132, 0.15)), smooth=True, group="Head"),
        part("Cap", kit.sphere(12, 8), crape, M((0, 0.02, 2.28), scale=(0.145, 0.158, 0.165)), smooth=True, group="Head"),
        part("CapPeak", kit.cone(6, 0.035, 0.0, 0.07), crape, M((0, -0.145, 2.24), rot=(180, 0, 0)), smooth=True, group="Head"),
        part("CapFrill", kit.tube(frill_pts, [0.016] * len(frill_pts), seg=5), frill, smooth=True, group="Head"),
    ]
    # Long arms in tight crape sleeves, a black lace cuff at each wrist; long pale hands.
    for s, arm, fore, hand, sh, el, wr in ((-1, "ArmL", "ForeL", "HandL", SHOULDER_L, ELBOW_L, WRIST_L),
                                           (1, "ArmR", "ForeR", "HandR", SHOULDER_R, ELBOW_R, WRIST_R)):
        parts += [
            part("Sleeve", kit.cone(8, 0.05, 0.046, math.dist(sh, el) + 0.03), crape, kit.along(el, sh), smooth=True, group=arm),
            part("Elbow", kit.sphere(8, 6, 0.047), crape, M(el), smooth=True, group=arm),
            part("Forearm", kit.cone(8, 0.044, 0.04, math.dist(el, wr)), crape, kit.along(wr, el), smooth=True, group=fore),
            part("Cuff", kit.cone(8, 0.062, 0.04, 0.07), dark, kit.along(wr, (wr[0], wr[1], wr[2] + 0.07)), smooth=True, group=fore),
            part("Palm", kit.sphere(8, 6), skin, M((wr[0], wr[1], wr[2] - 0.07), scale=(0.019, 0.04, 0.07)), smooth=True, group=hand),
            part("Fingers", kit.cone(6, 0.034, 0.008, 0.11), skin, M((wr[0], wr[1], wr[2] - 0.175), scale=(0.42, 1, 1)), smooth=True, group=hand),
        ]
    # The long veil: from the cap's peak over the face, to below her waist in front, widening; its lace hem on VeilTip.
    vrows = []
    steps = 22
    for k in range(steps + 1):
        z = JR[0].z + (JR[4].z - JR[0].z) * k / steps
        i = min(3, max(i for i in range(4) if JR[i].z >= z - 1e-6))
        u = (JR[i].z - z) / (JR[i].z - JR[i + 1].z)
        c = JR[i].lerp(JR[i + 1], u)
        t = k / steps
        hw = 0.11 + 0.17 * min(1.0, t * 1.25)
        vrows.append((tuple(c), hw, 0.09 * max(0.0, 1 - t * 2.5)))
    parts.append(part("LongVeil", sheet(vrows, 9), veil_mat, smooth=True, group="VeilCloth"))
    lrows = []
    for k in range(4):
        u = k / 3
        c = JR[4].lerp(JR[5], u)
        lrows.append((tuple(c + V((0, -0.002, 0))), 0.28 + 0.01 * u, 0.0))
    lace_part = part("Lace", sheet(lrows, 9, 0.012), lace, smooth=True, group="VeilTip")
    for v in lace_part.data.vertices:   # (scalloped: its foot dips between points)
        if v.co.z < JR[5].z + 0.005:
            v.co.z -= 0.02 * abs(math.sin(v.co.x * 34))
    parts.append(lace_part)
    # The veil's fall down her back, from the cap to the waist.
    parts.append(part("BackFall", kit.cone(12, 0.2, 0.13, 0.9), veil_mat, M((0, 0.1, 1.79), scale=(1.0, 0.55, 1)), smooth=True, group="BackBlend"))
    # The chatelaine at her waist: a gold clasp, three stopped watches on chains, and the lantern.
    cx, cy, cz = CLASP
    parts.append(part("Clasp", kit.cylinder(10, 0.04, 0.014), gold, M((cx, cy + 0.01, cz + 0.02), rot=(90, 0, 0), scale=(0.8, 1, 1.2)), group="Hips"))
    for bone in ("WatchA", "WatchB", "WatchC"):
        head, tail, _ = BONES[bone]
        tx, ty, tz = tail
        parts += [
            part("WatchChain", kit.tube([head, tail], [0.006, 0.006], seg=4), gold, group=bone),
            part("WatchCase", kit.cylinder(12, 0.034, 0.016), silver, M((tx, ty, tz - 0.034), rot=(90, 0, 0)), group=bone),
            part("WatchFace", kit.cylinder(12, 0.027, 0.006), face, M((tx, ty - 0.009, tz - 0.034), rot=(90, 0, 0)), group=bone),
        ]
    lx, ly, lz = BONES["Lantern"][1]
    parts += [
        part("LanternChain", kit.tube([CLASP, (lx, ly, lz)], [0.006, 0.006], seg=4), gold, group="Lantern"),
        part("LanternTop", kit.cone(4, 0.055, 0.01, 0.045), iron, M((lx, ly, lz - 0.02), rot=(0, 0, 45)), group="Lantern"),
        part("LanternGlass", kit.cube(), glass, M((lx, ly, lz - 0.085), scale=(0.065, 0.065, 0.085)), group="Lantern"),
        part("LanternBase", kit.cube(), iron, M((lx, ly, lz - 0.133), scale=(0.08, 0.08, 0.014)), group="Lantern"),
    ]

    mesh = kit.join(parts, "Widow")
    weigh(mesh, "GownBlend", by_height([(0.5, "Hem"), (0.88, "Skirt"), (1.24, "Skirt"), (1.32, "Hips")]))
    weigh(mesh, "BodiceBlend", by_height([(1.3, "Hips"), (1.4, "Spine"), (1.55, "Spine"), (1.72, "Chest"), (1.93, "Chest"), (2.03, "Head")]))
    weigh(mesh, "BackBlend", by_height([(1.95, "BackFall"), (2.15, "Head")]))
    weigh(mesh, "VeilCloth", veil_weights)
    weigh(mesh, "TrainCloth", train_weights)
    armature = kit.rig("WidowRig", BONES)
    kit.skin(mesh, armature)
    kit.animate(armature, ACTIONS)
    # (the skirt's pooling: the Hem's scale, keyed in every clip, 1 where it doesn't pool)
    hem = armature.pose.bones["Hem"]
    for name, scales in HEM_SCALE.items():
        armature.animation_data.action = bpy.data.actions[name]
        for f, (across, along) in enumerate(scales, start=1):
            hem.scale = (across, along, across)
            hem.keyframe_insert("scale", frame=f)
    hem.scale = (1, 1, 1)
    armature.animation_data.action = bpy.data.actions[next(iter(ACTIONS))]
    for name, worst in REACH.items():
        if worst > 0.02:
            print(f"[widow] {name}: the hands come off the veil by up to {worst * 100:.0f} cm")
    for name, paths in VEIL_PATH.items():
        far = max(V((p[5].x, p[5].y, 0)).length for p in paths)
        print(f"[widow] {name}: the veil's tip goes {far:.2f} m out at most")
    return [armature, mesh]
