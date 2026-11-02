"""The Hold, That Would Not Let Go: the Hold's Boss (the first, the tutorial). At the way out, the cave itself won't let
you go: a great face forms in the round chamber's north wall, high up, and two enormous hands on long arms tear out of
the stone either side of it. It's rooted in the wall: the face is a relief of the living stone (worn smooth, like the
kept faces in the Hold's walls: a heavy brow, big lidded eyes, a broad mouth), the arms come out of the wall.

Placement: origin on the floor at the foot of the wall under the face. The wall's surface is the plane y = 0; the arena
is in front (-Y: the game's front, south-facing once placed). Everything "out" is at y < 0; retracting moves into y > 0
(through the cave wall's mesh, into the rock). Front -Y; its left is -X (the repo's convention: ShoulderL at x -4.5).

- The face: 4.5 m wide, 4 m tall, its centre 4.2 m up, out of the wall up to 1.5 m (the nose). Its mouth is a real hole
  (lined with HoldMouth, its back open, a dark Throat behind it while it lives): in Death the face sags and the jaw
  drops to the floor (its top under 0.15 m), the upper lip 3+ m up: the opening frames the tunnel in the wall behind it
  (2.4 m wide, 3 m tall, centred under the face). Round the face its skin of stone (HoldWall_Breathe) flows back into
  the wall (weighted to the Root at its rim), so the face can swell out of the stone, sink back, lunge and sag; below
  the chin it flows down into the floor (on the Jaw: it goes under the floor when the jaw drops).
- The eyes: two balls (0.8 m) in sockets under the brow, lidded (lid shells modelled closed, as the cave's eyes:
  LidTop opens with -X, LidBottom with +X), the balls turn to look (EyeL/EyeR).
- The arms: from the wall either side of the face, shoulders sunk into it at x +-4.5, 3.5 m up (their roots run 3.5 m
  back into the rock); upper arm 3 m, forearm 3 m, a hand 1.4 m long: a palm, four long fingers of three bones each
  (FingerL1a-c index .. FingerL4a-c little) and a thumb of two (ThumbLa, ThumbLb), long nails (TheHoldNail).
  ShoulderL/R slide along the wall's normal (+Y into the rock: withdrawn at +8 m, the arm straight out ahead of it, all
  of it at y > 0.3); their rotation is never keyed (always 0), so the game can turn an arm at its shoulder (yaw about
  its head: the shoulder joint) to aim a slam.
- Strands (StrandL1-3, R1-3): stone strands that stretch from the forearm and hand back to the wall when a hand tears
  out of it (Wake, Return), then snap back into the arm (hidden inside it otherwise).

Clips (30 fps; one-offs are stretched by the game). The rig is three subtrees under the Root: the Face (Face, Brow, Jaw,
Throat, EyeL/R, LidTop/BottomL/R) and each arm (ShoulderL and everything under it; the same for R). Face clips only
animate the face's bones; an arm's clips only that arm's (the FBX bakes every bone into every clip, the rest at rest:
play them with mixing transforms on Face / ShoulderL / ShoulderR). Dormant, Wake, Idle, Roar and Death animate all three.

How it's animated (the Laden's way, see laden.py): each bone keyed on its own timing (kit.bake: tracks). An arm isn't
keyed bone by bone: its wrist is keyed where it is in the world (_Wrist), the hand's turn in the world (_Hand: pitch,
roll, yaw), the elbow's bow (_Pole), and the arm is bent onto the wrist every frame (kit.reach), the hand turned to its
keyed turn whatever the forearm does; the fingers are driven by a grip (_Grip: curl, spread, thumb) that each finger
and each joint follows a frame or more late (the index first, the little finger last, the tips last of all: they trail
the hand and overshoot its stops), plus a twitch of one finger now and then (_Twitch1-4). Nothing goes through the
floor: if a hand would, its wrist is lifted (the build reports by how much). The face: its whole (Face) leads, the jaw
and brow follow late, the eyes look in saccades (hold, a quick turn, hold), the lids blink.
"""
import math

import bpy
import numpy as np
from mathutils import Euler, Matrix, Quaternion, Vector

M, part = kit.M, kit.part  # `kit` is injected by build_model.py

# ---------------------------------------------------------------- measures

FACE_C = 4.2                 # the face's centre (z)
MZ = 3.4                     # the mouth's line (z)
MOUTH_HW = 1.5               # the mouth's half-width (its corners)
LIP_UP, LIP_LO = 0.1, 0.08   # the mouth's half-height above and below its line, at rest (a slit: closed)
EYE_X, EYE_Z, EYE_R = 0.95, 4.68, 0.4
LID_IN, LID_OUT = 0.43, 0.47
SH = (4.5, 0.3, 3.5)         # the shoulder joint (|x|, y, z)
UPPER, FORE = 3.0, 3.0
PALM = 0.48
FACE_PIVOT = (0.0, 0.6, FACE_C)
JAW_HINGE = (0.0, 0.4, MZ + 0.05)
NS, NR, NB = 200, 58, 40     # the face's grid: round the mouth, out to its rim (ring NB on the face's edge)
SIDE_X = {"L": -1, "R": 1}


def smoothstep(e0, e1, x):
    t = np.clip((np.asarray(x, float) - e0) / (e1 - e0), 0.0, 1.0)
    return t * t * (3 - 2 * t)


def g2(x, z, cx, cz, sx, sz):
    return np.exp(-(((x - cx) / sx) ** 2 + ((z - cz) / sz) ** 2))


def smin(a, b, k):
    h = np.clip(0.5 + 0.5 * (b - a) / k, 0.0, 1.0)
    return b * (1 - h) + a * h - k * h * (1 - h)


def smax(a, b, k):
    return -smin(-a, -b, k)


# ---------------------------------------------------------------- the face, as a depth out of the wall

def relief(x, z):
    """The face's relief (metres out of the wall) before its sockets and lips: worn smooth, a sleeper's."""
    ax = np.abs(x)
    q = (x / 2.3) ** 2 + ((z - 4.25) / 2.05) ** 2
    head = 0.95 * np.power(smoothstep(1.25, 0.25, q), 0.6)
    f = np.zeros_like(x)
    f += 0.30 * g2(x, z, 0, 5.13, 1.8, 0.3)          # the brow, heavy
    f -= 0.06 * g2(x, z, 0, 5.2, 0.16, 0.32)         # its furrow
    f += 0.05 * g2(x, z, 0, 5.6, 1.3, 0.3)
    f -= 0.03 * g2(x, z, 0, 5.82, 1.2, 0.06)         # a crease across the forehead
    f += 0.16 * g2(ax, z, 1.3, 4.05, 0.45, 0.32)     # cheekbones
    f -= 0.09 * g2(ax, z, 0.78, 3.55, 0.2, 0.45)     # the hollows from the nose to the mouth's corners
    f += 0.08 * g2(ax, z, 0.3, 3.9, 0.17, 0.12)      # the nostrils' wings
    f -= 0.05 * g2(ax, z, 0.2, 3.84, 0.07, 0.05)
    f += 0.07 * g2(ax, z, 1.62, 3.38, 0.2, 0.28)     # pads at the mouth's corners
    t = np.clip((5.0 - z) / 1.05, 0, 1)
    f += (0.05 + 0.42 * t ** 1.5) * np.exp(-(x / (0.15 + 0.2 * t)) ** 2) * smoothstep(3.78, 3.98, z) * smoothstep(5.2, 4.85, z)
    f += 0.13 * g2(x, z, 0, 4.0, 0.24, 0.17)         # the nose's tip
    f += 0.1 * g2(x, z, 0, MZ + 0.18, 1.2, 0.13)     # the upper lip
    f -= 0.04 * g2(x, z, 0, MZ + 0.37, 0.1, 0.09)
    f += 0.1 * g2(x, z, 0, MZ - 0.22, 1.05, 0.14)    # the lower lip
    f -= 0.07 * g2(x, z, 0, MZ - 0.45, 1.0, 0.1)
    f += 0.24 * g2(x, z, 0, 2.55, 0.85, 0.36)        # the chin
    return head + f * smoothstep(1.15, 0.7, q) - 0.5 * smoothstep(1.0, 1.45, q)


def mound(x, z):
    """The stone round the face swelling out of the wall (into it at the rim); below, down into the floor."""
    H = np.where(z > FACE_C, 3.0, 4.8)
    r = np.sqrt((x / 3.1) ** 2 + ((z - FACE_C) / H) ** 2)
    return 0.26 * (1 - smoothstep(0.5, 0.82, r)) - 0.32 * smoothstep(0.68, 1.0, r)


def stone(x, z):
    return 0.022 * np.sin(2.7 * x + 1.1 * z) * np.sin(1.9 * z - 0.6 * x + 1.0) + 0.01 * np.sin(6.3 * x - 4.1 * z + 0.4)


def depth_raw(x, z):
    return smax(mound(x, z), relief(x, z), 0.25)


DEPTH_SCALE = 1.5 / float(max(depth_raw(np.zeros(1), np.array([zz]))[0] for zz in np.linspace(3.5, 4.4, 40)))
EYE_D = float(depth_raw(np.array([EYE_X]), np.array([EYE_Z]))[0]) * DEPTH_SCALE - 0.45   # the eyes' centres, out of the wall


def depth(x, z):
    d = depth_raw(x, z) * DEPTH_SCALE + stone(x, z) * smoothstep(1.3, 0.9, np.sqrt((x / 2.3) ** 2 + ((z - 4.25) / 2.05) ** 2))
    for sx in (-1, 1):
        r2 = (x - sx * EYE_X) ** 2 + (z - EYE_Z) ** 2
        d = smin(d, EYE_D - 0.15 + 2.0 * r2, 0.08)   # the socket, a bowl round the ball
    return d


def lens(t):
    """The mouth's edge at rest (a wide slit), round its middle: t from its right corner, anticlockwise from the front."""
    s, c = np.sin(t), np.cos(t)
    return MOUTH_HW * c, MZ + np.where(s > 0, LIP_UP, LIP_LO) * s - 0.1 * np.abs(c) ** 4   # (the corners turned down)


def rim(t):
    s = np.sin(t)
    return 3.1 * np.cos(t), FACE_C + np.where(s > 0, 3.0, 4.8) * s


def face_weights(x, z, ring):
    """Per vertex: (Face, Brow, Jaw, Root). The jaw: below the mouth's line (and down into the floor), with a soft seam
    beyond the corners; the stone round the face going over from the Face to the Root at its rim (in the wall), so it
    sinks and swells with the face; the brow over the eyes."""
    ax = np.abs(x)
    z_line = np.where(ax < MOUTH_HW, MZ - 0.1 * (np.minimum(ax, MOUTH_HW) / MOUTH_HW) ** 4, MZ - 0.1 - (ax - MOUTH_HW) * 1.6)
    delta = 0.025 + 0.275 * smoothstep(1.3, 1.75, ax)
    jaw = smoothstep(2.15, 1.65, ax) * smoothstep(z_line + delta, z_line - delta, z)
    root = smoothstep(NR - 6, NR, ring) * (1 - jaw)
    rest = 1 - jaw - root
    brow = rest * 0.95 * g2(x, z, 0, 5.25, 1.9, 0.45)
    return rest - brow, brow, jaw, root


def face_geometry():
    """The face's sheet (a grid from the mouth's edge out to the rim), the mouth's lining, its throat."""
    t = np.linspace(0, 2 * np.pi, NS, endpoint=False)
    lx, lz = lens(t)
    ox, oz = rim(t)
    # Along each ray from the mouth's edge to the rim, where it crosses the face's edge (ring NB lies on it: a smooth
    # line between the face and the stone round it).
    def rho(s):
        x, z = lx + (ox - lx) * s, lz + (oz - lz) * s
        return np.sqrt((x / 2.25) ** 2 + ((z - FACE_C) / 2.0) ** 2)
    lo, hi = np.zeros(NS), np.ones(NS)
    for _ in range(40):
        mid = (lo + hi) / 2
        inside = rho(mid) < 1.06
        lo, hi = np.where(inside, mid, lo), np.where(inside, hi, mid)
    sb = (lo + hi) / 2
    u = np.arange(NR + 1)
    a = np.clip(u / NB, 0, 1)
    s = np.where(u[:, None] <= NB, sb[None, :] * (0.3 * a + 0.7 * a ** 1.6)[:, None],
                 sb[None, :] + (1 - sb[None, :]) * (np.clip((u - NB) / (NR - NB), 0, 1))[:, None])
    X = lx[None, :] + (ox - lx)[None, :] * s
    Z = lz[None, :] + (oz - lz)[None, :] * s
    dist = s * np.hypot(ox - lx, oz - lz)[None, :]
    D = depth(X, Z) - 0.2 * np.exp(-(dist / 0.09) ** 2)        # the lips curl in at the edge
    V = np.stack([X, -D, Z], axis=-1).reshape(-1, 3)
    idx = np.arange((NR + 1) * NS).reshape(NR + 1, NS)
    F, mats = [], []
    for i in range(NR):
        for j in range(NS):
            k = (j + 1) % NS
            F.append((idx[i, j], idx[i + 1, j], idx[i + 1, k], idx[i, k]))   # (facing -Y: out)
            mats.append(1 if i < NB else 0)
    ring = np.repeat(np.arange(NR + 1), NS).astype(float)
    wf, wb, wj, wr = face_weights(X.reshape(-1), Z.reshape(-1), ring)
    # The mouth's lining: from the lips' edge back towards the wall, open at the back.
    ring0 = V[idx[0]]
    lining = [ring0]
    for frac in (0.15, 0.4, 0.7, 1.0):
        back = np.stack([ring0[:, 0] * (1 - 0.04 * frac), ring0[:, 1] + (-0.25 - ring0[:, 1]) * frac,
                         MZ + (ring0[:, 2] - MZ) * (1 - 0.15 * frac)], axis=-1)
        lining.append(back)
    base = len(V)
    Vl = np.vstack(lining[1:])
    lidx = [idx[0]] + [base + np.arange(NS) + NS * r for r in range(4)]
    for a, b in zip(lidx, lidx[1:]):
        for j in range(NS):
            k = (j + 1) % NS
            F.append((a[j], a[k], b[k], b[j]))   # (facing in, at the mouth's middle)
            mats.append(2)
    V = np.vstack([V, Vl])
    rep = lambda w: np.concatenate([w, np.tile(w[idx[0]], 4)])
    weights = {"Face": rep(wf), "Brow": rep(wb), "Jaw": rep(wj), "Root": rep(wr)}
    return V, F, mats, weights


# ---------------------------------------------------------------- eyes

def revolve(profile_pts, seg):
    """Revolve (rho, y) points about the y axis; returns V, T (poles closed where rho = 0)."""
    V, rings = [], []
    for rho, y in profile_pts:
        if rho < 1e-6:
            rings.append([len(V)])
            V.append((0.0, y, 0.0))
        else:
            ring = []
            for j in range(seg):
                a = 2 * math.pi * j / seg
                ring.append(len(V))
                V.append((rho * math.cos(a), y, rho * math.sin(a)))
            rings.append(ring)
    T = []
    for a, b in zip(rings, rings[1:]):
        if len(a) == 1:
            T += [(a[0], b[(j + 1) % seg], b[j]) for j in range(seg)]
        elif len(b) == 1:
            T += [(a[j], a[(j + 1) % seg], b[0]) for j in range(seg)]
        else:
            for j in range(seg):
                T += [(a[j], a[(j + 1) % seg], b[(j + 1) % seg], b[j])]
    return np.array(V, float), T


def eye_ball():
    """A ball, its front -Y: pupil to 10 degrees off its front, iris to 24 (the cave's eyes, bigger)."""
    angles = [0, 5, 9, 13, 18, 24, 30, 36, 45, 58, 72, 90, 110, 130, 150, 170, 180]
    pts = []
    for a in angles:
        r = EYE_R - (0.012 * (1 - a / 13) if a <= 13 else 0.0)
        pts.append((r * math.sin(math.radians(a)), -r * math.cos(math.radians(a))))
    V, T = revolve(pts, 32)
    ang = np.degrees(np.arctan2(np.hypot(V[:, 0], V[:, 2]), -V[:, 1]))
    mats = [2 if ang[list(f)].max() <= 13.01 else 1 if ang[list(f)].max() <= 30.01 else 0 for f in T]
    return V, T, mats


def eye_lid(top):
    """A lid: a thick shell round the ball from its margin (level with the eye's middle, in front) over the top (or
    under the bottom) towards the back; modelled closed."""
    sgn = 1 if top else -1
    ps = np.linspace(-78, 78, 17)
    fs = np.linspace(1.2, 122, 14)
    V, grid = [], {}
    for layer, r in (("o", LID_OUT), ("i", LID_IN)):
        for i, psi in enumerate(ps):
            for j, phi in enumerate(fs):
                a, b = math.radians(psi), math.radians(phi) * sgn
                rr = r + (0.012 if layer == "o" and j == 0 else 0.0)
                grid[(layer, i, j)] = len(V)
                V.append((rr * math.sin(a), -rr * math.cos(a) * math.cos(b), rr * math.cos(a) * math.sin(b)))
    quads = []
    ni, nj = len(ps), len(fs)
    for layer in ("o", "i"):
        for i in range(ni - 1):
            for j in range(nj - 1):
                quads.append((grid[(layer, i, j)], grid[(layer, i + 1, j)], grid[(layer, i + 1, j + 1)], grid[(layer, i, j + 1)]))
    for i in range(ni - 1):
        for j in (0, nj - 1):
            quads.append((grid[("o", i, j)], grid[("o", i + 1, j)], grid[("i", i + 1, j)], grid[("i", i, j)]))
    for j in range(nj - 1):
        for i in (0, ni - 1):
            quads.append((grid[("o", i, j)], grid[("o", i, j + 1)], grid[("i", i, j + 1)], grid[("i", i, j)]))
    return np.array(V), quads


# ---------------------------------------------------------------- the arms: rest geometry

FINGERS = {  # (across: + towards the face; out from the wrist; segment lengths; radius at the knuckle)
    1: (0.27, PALM, (0.34, 0.26, 0.20), 0.095),
    2: (0.09, PALM + 0.02, (0.38, 0.29, 0.23), 0.1),
    3: (-0.09, PALM, (0.36, 0.27, 0.21), 0.095),
    4: (-0.26, PALM - 0.07, (0.28, 0.21, 0.16), 0.085),
}
THUMB_BASE, THUMB_DIR, THUMB_LEN = (0.36, 0.14, -0.05), (0.75, 0.6, -0.2), (0.32, 0.26)
NAIL = 0.15
IDLE_WRIST = (-5.6, -3.4, 4.6)          # (the left's; the right's mirrored)
IDLE_POLE = (-0.75, 0.15, 0.65)
STRANDS = {  # base bone, where on it (fraction from head to tail), the wall point it stretches back to (the left's)
    1: ("Fore", 0.75, (-4.25, 0.05, 3.85)),
    2: ("Fore", 0.4, (-4.75, 0.05, 3.15)),
    3: ("Hand", 0.45, (-4.45, 0.05, 3.45)),
}


def mx(v, side):
    """A point (or direction) given for the left, made the given side's."""
    return tuple(v) if side == "L" else (-v[0], v[1], v[2])


def solve_two(shoulder, target, l1, l2, pole):
    s, g, pole = Vector(shoulder), Vector(target), Vector(pole)
    to = g - s
    dist = min(to.length, l1 + l2 - 1e-4)
    n = to.normalized()
    a = (l1 * l1 - l2 * l2 + dist * dist) / (2 * dist)
    h = math.sqrt(max(0.0, l1 * l1 - a * a))
    p = (pole - n * pole.dot(n)).normalized()
    return s + n * a + p * h, s + n * dist


def hand_point(side, u, v, w, wrist):
    """A point on the hand at rest: u across (+ towards the face), v out along it (-Y), w up."""
    sx = SIDE_X[side]
    return Vector((wrist[0] - sx * u, wrist[1] - v, wrist[2] + w))


def arm_rest(side):
    sx = SIDE_X[side]
    J = (sx * SH[0], SH[1], SH[2])
    W0 = mx(IDLE_WRIST, side)
    E0, W0 = solve_two(J, W0, UPPER, FORE, mx(IDLE_POLE, side))
    return Vector(J), E0, W0


def build_bones():
    bones = {
        "Root": ((0, 0, 0), (0, 0, 1.0), None),
        "Face": (FACE_PIVOT, (0, -0.6, FACE_C), "Root"),
        "Brow": ((0, -0.9, 5.1), (0, -1.3, 5.1), "Face"),
        "Jaw": (JAW_HINGE, (0, -0.9, 2.6), "Face"),
        "Throat": ((0, 0.0, 3.2), (0, 0.0, 2.6), "Face"),
    }
    for side, sx in SIDE_X.items():
        c = (sx * EYE_X, -EYE_D, EYE_Z)
        bones["Eye" + side] = (c, (c[0], c[1] - 0.5, c[2]), "Face")
        bones["LidTop" + side] = (c, (c[0], c[1], c[2] + 0.45), "Face")
        bones["LidBottom" + side] = (c, (c[0], c[1], c[2] - 0.45), "Face")
    for side, sx in SIDE_X.items():
        J, E0, W0 = arm_rest(side)
        bones["Shoulder" + side] = (tuple(J), tuple(J + Vector((0, -0.6, 0))), "Root")
        bones["Arm" + side] = (tuple(J), tuple(E0), "Shoulder" + side)
        bones["Fore" + side] = (tuple(E0), tuple(W0), "Arm" + side)
        bones["Hand" + side] = (tuple(W0), tuple(hand_point(side, 0.08, PALM, 0, W0)), "Fore" + side)
        for k, (u, v, lens_, _) in FINGERS.items():
            parent, at = "Hand" + side, v
            for seg, length in zip("abc", lens_):
                name = f"Finger{side}{k}{seg}"
                bones[name] = (tuple(hand_point(side, u, at, 0, W0)), tuple(hand_point(side, u, at + length, 0, W0)), parent)
                parent, at = name, at + length
        base = hand_point(side, *THUMB_BASE, W0)
        d = Vector((-sx * THUMB_DIR[0], -THUMB_DIR[1], THUMB_DIR[2])).normalized()
        bones["Thumb" + side + "a"] = (tuple(base), tuple(base + d * THUMB_LEN[0]), "Hand" + side)
        bones["Thumb" + side + "b"] = (tuple(base + d * THUMB_LEN[0]), tuple(base + d * sum(THUMB_LEN)), "Thumb" + side + "a")
        for k, (on, frac, _) in STRANDS.items():
            h, t, _ = bones[on + side]
            a = Vector(h).lerp(Vector(t), frac)
            back = (Vector(h) - Vector(t)).normalized()
            bones[f"Strand{side}{k}"] = (tuple(a + back * 0.25), tuple(a + back * 0.45), on + side)
    return bones


BONES = build_bones()
FACE_BONES = ["Face", "Brow", "Jaw", "Throat", "EyeL", "EyeR", "LidTopL", "LidBottomL", "LidTopR", "LidBottomR"]


def arm_bone_names(side):
    names = ["Shoulder" + side, "Arm" + side, "Fore" + side, "Hand" + side]
    names += [f"Finger{side}{k}{s}" for k in FINGERS for s in "abc"]
    names += ["Thumb" + side + "a", "Thumb" + side + "b"] + [f"Strand{side}{k}" for k in STRANDS]
    return names


# The thumbs' axes: flexing (towards the palm) and swinging (in the palm's plane), each side's own.
THUMB_AXES = {}
for _side, _sx in SIDE_X.items():
    _d = (Vector(BONES["Thumb" + _side + "a"][1]) - Vector(BONES["Thumb" + _side + "a"][0])).normalized()
    _n = Vector((0, 0, -1))
    THUMB_AXES[_side] = (_d.cross(_n).normalized(), _n * -_sx)

# Points on each hand that mustn't go below the floor: (bone, rest point, radius).
PROBES = {}
for _side in SIDE_X:
    _W0 = Vector(BONES["Hand" + _side][0])
    pr = [("Hand" + _side, _W0, 0.19)]
    for _u in (-0.25, 0.0, 0.25):
        for _v in (0.12, 0.3, 0.5):
            pr.append(("Hand" + _side, hand_point(_side, _u, _v, -0.18, _W0), 0.0))
    for _k, (_, _, _lens, _r) in FINGERS.items():
        for _s, _rr in zip("abc", (_r, _r * 0.85, _r * 0.72)):
            _h, _t, _ = BONES[f"Finger{_side}{_k}{_s}"]
            pr.append((f"Finger{_side}{_k}{_s}", Vector(_h), _rr))
            pr.append((f"Finger{_side}{_k}{_s}", Vector(_t), _rr * 0.85))
        _h, _t, _ = BONES[f"Finger{_side}{_k}c"]
        pr.append((f"Finger{_side}{_k}c", Vector(_t) + (Vector(_t) - Vector(_h)).normalized() * NAIL * 0.9 + Vector((0, 0, -0.04)), 0.0))
    _h, _t, _ = BONES["Thumb" + _side + "b"]
    pr.append(("Thumb" + _side + "b", Vector(_t), 0.06))
    PROBES[_side] = pr


# ---------------------------------------------------------------- keys and tracks

def W(x, y, z):          # a wrist (or a shoulder's offset, a pole): where
    return ((0, 0, 0), (x, y, z))


def H(pitch, roll, yaw):  # a hand's turn in the world: roll about its length, then pitch (+: fingers down), then yaw
    return ((pitch, roll, yaw), (0, 0, 0))


def G(curl, spread, thumb):  # a grip: the fingers' curl (+: in, -: bent back), their spread, the thumb's flex
    return ((curl, spread, thumb), (0, 0, 0))


def S(w):                 # a weight (a twitch's curl, a strand's pin)
    return ((w, 0, 0), (0, 0, 0))


def F_(rx=0, ry=0, rz=0, x=0, y=0, z=0):  # a face bone: rotation, offset
    return ((rx, ry, rz), (x, y, z))


def held(pose, frame):
    return {bone: [(frame, value)] for bone, value in pose.items()}


def keys(*parts):
    merged = {}
    for p in parts:
        for bone, ks in p.items():
            merged.setdefault(bone, []).extend(ks)
    return merged


def shift(tracks, df, wrap=None):
    out = {}
    for b, ks in tracks.items():
        out[b] = [((1 + (f - 1 + df) % wrap) if wrap else f + df, v) for f, v in ks]
    return out


def add3(a, b):
    return tuple(x + y for x, y in zip(a, b))


def mirror_value(name, value):
    rot, off = value
    if name in ("Shoulder", "_Wrist", "_Pole"):
        return (tuple(rot), (-off[0], off[1], off[2]))
    if name == "_Hand":
        return ((rot[0], -rot[1], -rot[2]), tuple(off))
    return value


def sided(tracks, side):
    """Arm tracks authored for the left (x < 0, its outside is -x) made the given side's: renamed, mirrored for R."""
    out = {}
    for name, ks in tracks.items():
        out[name + side] = ks if side == "L" else [(f, mirror_value(name, v)) for f, v in ks]
    return out


ARM_TRACKS = ["Shoulder", "_Wrist", "_Hand", "_Pole", "_Grip", "_Twitch1", "_Twitch2", "_Twitch3", "_Twitch4",
              "_Tear1", "_Tear2", "_Tear3"]
FACE_TRACKS = ["Face", "Brow", "Jaw", "Throat", "_Look", "_Lids"]


def pose_at(tracks, length, periodic, frame=1):
    """The tracks' values at a frame (what a clip starting or ending there must match)."""
    poses = kit.bake({b: sorted(dict(ks).items()) for b, ks in tracks.items()}, length, periodic)
    return poses[frame - 1][1]


# ---------------------------------------------------------------- solving a frame

def deg(e):
    return tuple(math.degrees(a) for a in e)


def hand_matrix(value):
    pitch, roll, yaw = (math.radians(a) for a in value[0])
    return Matrix.Rotation(yaw, 3, "Z") @ Matrix.Rotation(pitch, 3, "X") @ Matrix.Rotation(roll, 3, "Y")


def about(axis, angle):
    return Quaternion(axis, math.radians(angle))


def sample(series, f, length, periodic):
    """A series (one value a frame, frame 1 first) at a fractional frame f: wrapped in a loop, held at its ends."""
    if periodic:
        x = (f - 1) % length
    else:
        x = min(max(f - 1, 0.0), len(series) - 1)
    i = int(math.floor(x))
    t = x - i
    a, b = series[i % len(series)], series[min(i + 1, len(series) - 1)]
    return tuple(p + (q - p) * t for p, q in zip(a, b))


FINGER_WEIGHT = {1: 0.9, 2: 1.0, 3: 1.05, 4: 1.12}
SEG_WEIGHT = {"a": 0.8, "b": 1.0, "c": 0.75}
FAN = {1: 1.0, 2: 0.35, 3: -0.35, 4: -1.0}


def fingers(p, side, f, grips, twitches, length, periodic):
    """Each finger's joints from the grip, each a little late (index first, the little finger last, the tips last of
    all); in a one-off the lag eases out over its last frames, so it ends on its keyed grip."""
    fade = 1.0 if periodic else max(0.0, min(1.0, (length + 1 - f) / 8.0))
    mirror = side == "R"
    for k in FINGERS:
        tw = twitches[k][f - 1][0] if twitches[k] else 0.0
        for j, seg in enumerate("abc"):
            lag = (0.8 + 0.7 * j + 0.45 * (k - 1)) * fade
            curl, spread, _ = sample(grips, f - lag, length, periodic)
            c = curl * SEG_WEIGHT[seg] * FINGER_WEIGHT[k] + tw * (0.45, 1.0, 0.8)[j]
            rz = spread * FAN[k] if seg == "a" else 0.0
            p[f"Finger{side}{k}{seg}"] = ((c, 0.0, -rz if mirror else rz), (0, 0, 0))
    flex_axis, swing_axis = THUMB_AXES[side]
    for j, seg in enumerate("ab"):
        lag = (0.5 + 0.8 * j) * fade
        curl, spread, thumb = sample(grips, f - lag, length, periodic)
        q = about(flex_axis, thumb * (0.7, 1.0)[j])
        if seg == "a":
            q = about(swing_axis, -spread * 0.6) @ q
        p[f"Thumb{side}{seg}"] = (deg(q.to_euler("XYZ")), (0, 0, 0))


def lowest(m, side):
    return min((m[b] @ pt).z - r for b, pt, r in PROBES[side])


def solve_arm(p, side, wrist, hand, pole, prev):
    """The arm bent onto its wrist (kit.reach), the hand turned to its keyed turn in the world."""
    upper, fore, hand_b = "Arm" + side, "Fore" + side, "Hand" + side
    p = kit.reach(BONES, p, upper, fore, lambda m, t=wrist.copy(): t, pole)
    m = kit.pose_matrices(BONES, p)
    R = m[fore].to_3x3().inverted() @ hand
    e = R.to_euler("XYZ", prev) if prev is not None else R.to_euler("XYZ")
    p[hand_b] = (deg(e), (0, 0, 0))
    return p, e


REPORT = {}


def clip(n, tracks, periodic=False):
    """A clip of `n` frames (frame n is a loop's frame 1 again), from per-bone tracks, baked to every frame: the face's
    look and lids spread to both eyes, the fingers following the grip, the arms bent onto their wrists, the strands
    pinned to the wall while they're torn, and nothing through the floor."""
    length = n - 1
    tracks = {b: sorted(dict(ks).items()) for b, ks in tracks.items()}   # (a frame keyed twice: the later key)
    frames = kit.bake(tracks, length, periodic)
    sides = [s for s in "LR" if "_Wrist" + s in tracks]
    series = {}
    for s in sides:
        series[s] = ([p["_Grip" + s][0] for _, p in frames],
                     {k: ([p[f"_Twitch{k}{s}"][0] for _, p in frames] if f"_Twitch{k}{s}" in tracks else None)
                      for k in FINGERS})
    out, lift, prev = [], {s: 0.0 for s in sides}, {s: None for s in sides}
    reach_off = {s: 0.0 for s in sides}
    for f, p in frames:
        p = dict(p)
        if "_Look" in p:
            look = p.pop("_Look")[0]
            p["EyeL"] = ((look[0], 0, look[2]), (0, 0, 0))
            p["EyeR"] = ((look[0], 0, look[2]), (0, 0, 0))
        if "_Lids" in p:
            top, bottom, _ = p.pop("_Lids")[0]
            for s in "LR":
                p["LidTop" + s] = ((top, 0, 0), (0, 0, 0))
                p["LidBottom" + s] = ((bottom, 0, 0), (0, 0, 0))
        for s in sides:
            wrist = Vector(p.pop("_Wrist" + s)[1])
            hand = hand_matrix(p.pop("_Hand" + s))
            pole = Vector(p.pop("_Pole" + s)[1])
            p.pop("_Grip" + s)
            tears = {k: p.pop(f"_Tear{k}{s}", ((0, 0, 0), (0, 0, 0)))[0][0] for k in STRANDS}
            for k in FINGERS:
                p.pop(f"_Twitch{k}{s}", None)
            grips, twitches = series[s]
            fingers(p, s, f, grips, twitches, length, periodic)
            target = wrist.copy()
            for _ in range(4):
                q, e = solve_arm(p, s, target, hand, pole, prev[s])
                low = lowest(kit.pose_matrices(BONES, q), s)
                if low >= -0.003:
                    break
                target.z += -low + 0.004
            p, prev[s] = q, e
            lift[s] = max(lift[s], target.z - wrist.z)
            m = kit.pose_matrices(BONES, p)
            reach_off[s] = max(reach_off[s], (m["Fore" + s] @ Vector(BONES["Fore" + s][1]) - target).length)
            for k, (on, _, at) in STRANDS.items():
                w = tears[k]
                name = f"Strand{s}{k}"
                if abs(w) < 1e-4:
                    p[name] = ((0, 0, 0), (0, 0, 0))
                    continue
                parent = m[on + s]
                head = Vector(BONES[name][0])
                goal = parent.inverted() @ Vector(mx(at, s))
                p[name] = ((0, 0, 0), tuple(w * (goal - head)))
        out.append((f, p))
    REPORT[len(REPORT)] = {"lift": lift, "reach": reach_off, "frames": out}
    return ("LINEAR", out)


# ---------------------------------------------------------------- the face's poses

DORMANT_FACE = {"Face": F_(y=1.15), "Brow": F_(), "Jaw": F_(), "Throat": F_(), "_Look": F_(), "_Lids": F_()}

# Idle (3 s loop): it breathes (one slow breath: the face swells out and lifts, the jaw and brow following late), its
# eyes look about the arena in saccades (a hold, a quick turn, a hold), it blinks once.
IDLE_FACE_TRACKS = {
    "Face": [(1, F_(2, 0, 0, 0, 0, 0)), (24, F_(1.2, 0, 0.4, 0, -0.07, 0.05)), (50, F_(2.6, 0, -0.3, 0, 0.02, -0.03)),
             (72, F_(2.2, 0, 0.2, 0, -0.02, 0.0))],
    "Brow": [(6, F_()), (31, F_(2, 0, 0, 0, -0.02, -0.03)), (58, F_(-1, 0, 0, 0, 0, 0.02))],
    "Jaw": [(1, F_(3)), (31, F_(5.5)), (57, F_(2)), (79, F_(4))],
    "Throat": [(1, F_())],
    "_Look": [(1, F_(14, 0, 0)), (10, F_(14, 0, 0)), (12, F_(10, 0, -12)), (30, F_(10, 0, -12)), (32, F_(17, 0, 9)),
              (52, F_(17, 0, 9)), (54, F_(12, 0, 21)), (66, F_(12, 0, 21)), (68, F_(15, 0, 1)), (88, F_(14, 0, 0))],
    "_Lids": [(1, F_(-32, 16)), (38, F_(-32, 16)), (40, F_(-3, 2)), (41, F_(0, 0)), (44, F_(-24, 12)), (47, F_(-34, 17)),
              (70, F_(-27, 14)), (86, F_(-32, 16))],
}
IDLE_FACE = pose_at(IDLE_FACE_TRACKS, 90, True)
DORMANT_FACE_TRACKS = {
    "Face": [(1, F_(y=1.15)), (31, F_(0.6, 0, 0, 0, 1.08, 0.03))],
    "Brow": [(1, F_()), (34, F_(1, 0, 0, 0, 0, -0.01))],
    "Jaw": [(1, F_()), (36, F_(1.5))],
    "Throat": [(1, F_())], "_Look": [(1, F_())], "_Lids": [(1, F_())],
}

# ---------------------------------------------------------------- the arms' poses (the left's: the right's mirrored)

WITHDRAWN = {"Shoulder": W(0, 8.0, 0), "_Wrist": W(-4.5, 2.35, 3.5), "_Hand": H(0, 0, 0), "_Pole": W(-0.7, 0, 0.7),
             "_Grip": G(5, 2, 5), "_Tear1": S(0), "_Tear2": S(0), "_Tear3": S(0),
             "_Twitch1": S(0), "_Twitch2": S(0), "_Twitch3": S(0), "_Twitch4": S(0)}


def idle_arm(phase, v):
    """Idle's arm (3 s loop): hovering, raised beside the face, swaying slowly (the wrist leads, the hand turns after
    it), the fingers flexing in a slow wave, one or two twitching; `phase` shifts it round the loop, `v` varies it."""
    iw = IDLE_WRIST
    t = {
        "Shoulder": [(1, W(0, 0, 0)), (30, W(0, -0.06, 0.03)), (61, W(0, 0.04, -0.02))],
        "_Wrist": [(1, W(*iw)), (21, W(*add3(iw, (-0.12 * v, -0.1, 0.2)))), (45, W(*add3(iw, (0.05, 0.12 * v, 0.04)))),
                   (68, W(*add3(iw, (0.1, -0.05, -0.13 * v))))],
        "_Hand": [(1, H(35, -10, 12)), (25, H(29, -14, 8)), (50, H(41, -6, 14)), (73, H(34, -12, 17))],
        "_Pole": [(1, W(*IDLE_POLE)), (46, W(-0.7, 0.2, 0.7))],
        "_Grip": [(1, G(18, 4, 10)), (18, G(25, 2, 15)), (41, G(11, 8, 6)), (66, G(27, 3, 16))],
        "_Twitch1": [(1, S(0)), (24, S(0)), (27, S(13 * v)), (33, S(-2)), (37, S(0))],
        "_Twitch2": [(1, S(0)), (60, S(0)), (62, S(-9)), (67, S(3)), (71, S(0))],
        "_Twitch3": [(1, S(0)), (40, S(0)), (43, S(16)), (49, S(-3)), (53, S(0))],
        "_Twitch4": [(1, S(0)), (80, S(0)), (82, S(11 * v)), (86, S(0))],
        "_Tear1": [(1, S(0))], "_Tear2": [(1, S(0))], "_Tear3": [(1, S(0))],
    }
    return shift(t, phase, 90) if phase else t


IDLE_ARM = {"L": sided(idle_arm(0, 1.0), "L"), "R": sided(idle_arm(43, 0.8), "R")}
IDLE1 = {s: {b[:-1]: v for b, v in pose_at(IDLE_ARM[s], 90, True).items()} for s in "LR"}   # (unsided names)
for _s in "LR":   # (back to the left's terms, for authoring: the right's is mirrored again by `sided`)
    if _s == "R":
        IDLE1[_s] = {b: mirror_value(b, v) for b, v in IDLE1[_s].items()}


def arm_clip(side, authored):
    """An arm clip's tracks for `side` from tracks authored for the left, given as a function of that side's Idle frame
    1 pose (in the left's terms)."""
    return sided(authored(IDLE1[side]), side)


def both(authored):
    return keys(arm_clip("L", authored), arm_clip("R", authored))


# The slam: the hand rears up high over its shoulder, fingers spread, trembling; brought down flat on the floor in
# front of its shoulder (4.4 m out to the wrist, the fingers on to 5.8 m), straight out from the wall (x stays on the
# shoulder's: its own plane).
SLAM_TOP = {"Shoulder": W(0, 0.12, 0.12), "_Wrist": W(-4.6, -2.0, 8.25), "_Hand": H(-42, 0, 0), "_Pole": W(-0.35, -0.1, 1.0),
            "_Grip": G(-10, 15, -8)}
SLAM_AT = (-4.5, -4.4, 0.2)
SLAM_LAID = {"Shoulder": W(0, -0.22, -0.2), "_Wrist": W(*SLAM_AT), "_Hand": H(0, 0, 3), "_Pole": W(-0.15, 0.0, 1.0),
             "_Grip": G(4, 13, 2)}


def slam_draw(I):
    # SlamDraw (1.2 s, the tell): a dip first (the hand sinks and curls, gathering), then it rears up high over the
    # shoulder, the arm unfolding, the hand turned up and back and the fingers flung wide, overshooting the top and
    # settling; held there, trembling (the fingers quivering).
    return keys(held(I, 1), {
        "Shoulder": [(8, W(0, -0.1, -0.1)), (24, W(0, 0.15, 0.15)), (37, SLAM_TOP["Shoulder"])],
        "_Wrist": [(7, W(-5.4, -3.7, 3.9)), (21, W(-4.7, -2.3, 8.0)), (25, W(-4.65, -2.0, 8.5)), (29, W(-4.6, -2.05, 8.2)),
                   (31, W(-4.63, -2.0, 8.28)), (33, W(-4.58, -2.02, 8.22)), (35, W(-4.62, -2.0, 8.27)), (37, SLAM_TOP["_Wrist"])],
        "_Hand": [(9, H(52, -10, 10)), (20, H(-18, 0, 3)), (26, H(-49, 3, 0)), (30, H(-40, 0, 0)), (33, H(-44, 1, 0)),
                  (37, SLAM_TOP["_Hand"])],
        "_Pole": [(14, W(-0.6, -0.1, 0.8)), (37, SLAM_TOP["_Pole"])],
        "_Grip": [(8, G(36, 0, 22)), (19, G(4, 10, 0)), (25, G(-13, 17, -10)), (29, G(-8, 14, -6)), (37, SLAM_TOP["_Grip"])],
        "_Twitch1": [(30, S(0)), (32, S(-6)), (34, S(4)), (36, S(-3)), (37, S(0))],
        "_Twitch2": [(30, S(0)), (31, S(5)), (33, S(-5)), (35, S(4)), (37, S(0))],
        "_Twitch3": [(30, S(0)), (32, S(6)), (34, S(-4)), (36, S(3)), (37, S(0))],
        "_Twitch4": [(30, S(0)), (31, S(-5)), (33, S(6)), (35, S(-4)), (37, S(0))],
    })


def slam(I):
    # Slam (0.4 s; lands at 55%, frame 7.6): it comes down in its own plane, faster and faster, the hand lagging back at
    # first (fingers bent back by the air) then whipping flat; the palm lands at 7.6 on the floor, the shoulder thrust
    # out after it; the fingers slap down a frame or two later, bounce, and settle.
    w = SLAM_AT
    return {
        "Shoulder": [(1, SLAM_TOP["Shoulder"]), (5, W(0, -0.08, -0.05)), (7.6, W(0, -0.2, -0.16)), (10, W(0, -0.28, -0.24)),
                     (13, SLAM_LAID["Shoulder"])],
        "_Wrist": [(1, SLAM_TOP["_Wrist"]), (3, W(-4.6, -2.5, 7.75)), (5, W(-4.55, -3.75, 5.5)), (6.6, W(-4.5, -4.3, 2.3)),
                   (7.6, W(*w)), (9, W(w[0], w[1] - 0.04, w[2] - 0.05)), (11, W(w[0], w[1] - 0.02, w[2] + 0.02)), (13, W(*w))],
        "_Hand": [(1, SLAM_TOP["_Hand"]), (3, H(-52, 0, 0)), (5, H(-24, 0, 2)), (6.6, H(9, 0, 3)), (7.6, H(0, 0, 3)),
                  (9, H(-3, 0, 3)), (13, SLAM_LAID["_Hand"])],
        "_Pole": [(1, SLAM_TOP["_Pole"]), (7, SLAM_LAID["_Pole"])],
        "_Grip": [(1, SLAM_TOP["_Grip"]), (4, G(-20, 16, -10)), (7.6, G(-15, 14, -6)), (9, G(14, 13, 5)), (11, G(3, 13, 1)),
                  (13, SLAM_LAID["_Grip"])],
        "_Twitch1": [(1, S(0))], "_Twitch2": [(1, S(0))], "_Twitch3": [(1, S(0))], "_Twitch4": [(1, S(0))],
        "_Tear1": [(1, S(0))], "_Tear2": [(1, S(0))], "_Tear3": [(1, S(0))],
    }


def slam_stunned(I):
    # SlamStunned (1 s loop, the opening): the hand lies flat where it fell, dazed: the arm heaving over it, the hand
    # rocking a little, a finger twitching now and then (the middle, the little, the index), the thumb flexing.
    w = SLAM_AT
    return {
        "Shoulder": [(1, SLAM_LAID["Shoulder"]), (16, W(0, -0.16, -0.26))],
        "_Wrist": [(1, W(*w)), (12, W(w[0] + 0.02, w[1], w[2])), (24, W(w[0], w[1] + 0.015, w[2]))],
        "_Hand": [(1, SLAM_LAID["_Hand"]), (9, H(1, 2, 4)), (20, H(-1, -1.5, 2))],
        "_Pole": [(1, SLAM_LAID["_Pole"]), (15, W(-0.3, 0.05, 1.0))],
        "_Grip": [(1, SLAM_LAID["_Grip"]), (14, G(10, 12, 8)), (24, G(6, 14, 0))],
        "_Twitch2": [(1, S(0)), (6, S(0)), (7, S(-16)), (10, S(4)), (13, S(0))],
        "_Twitch4": [(1, S(0)), (15, S(0)), (16, S(-13)), (18, S(6)), (21, S(0))],
        "_Twitch1": [(1, S(0)), (22, S(0)), (23, S(-10)), (25, S(3)), (27, S(0))],
        "_Twitch3": [(1, S(0)), (27, S(0)), (28, S(7)), (30, S(0))],
        "_Tear1": [(1, S(0))], "_Tear2": [(1, S(0))], "_Tear3": [(1, S(0))],
    }


def slam_recover(I):
    # SlamRecover (1 s): the fingers claw into the floor and drag the hand back towards the wall, then it hauls itself
    # up, the hand hanging from the wrist, the fingers trailing; up past its hover and down into it (Idle's frame 1).
    w = SLAM_AT
    return keys(held(SLAM_LAID, 1), {
        "Shoulder": [(12, W(0, 0.1, 0.0)), (22, W(0, -0.05, 0.05)), (31, I["Shoulder"])],
        "_Wrist": [(5, W(w[0], w[1] + 0.05, w[2] + 0.04)), (13, W(-4.6, -3.7, 0.3)), (20, W(-5.2, -3.2, 3.2)),
                   (26, W(-5.7, -3.5, 4.95)), (31, I["_Wrist"])],
        "_Hand": [(6, H(-6, 0, 3)), (13, H(-4, -5, 6)), (20, H(56, -10, 10)), (26, H(30, -12, 12)), (31, I["_Hand"])],
        "_Pole": [(8, W(-0.2, 0.0, 1.0)), (20, W(*IDLE_POLE)), (31, I["_Pole"])],
        "_Grip": [(6, G(26, 6, 20)), (14, G(34, 4, 24)), (20, G(30, 6, 15)), (26, G(21, 4, 12)), (31, I["_Grip"])],
        "_Twitch1": [(31, I["_Twitch1"])], "_Twitch2": [(31, I["_Twitch2"])], "_Twitch3": [(31, I["_Twitch3"])],
        "_Twitch4": [(31, I["_Twitch4"])],
        "_Tear1": [(1, S(0))], "_Tear2": [(1, S(0))], "_Tear3": [(1, S(0))],
    })


# The sweep: drawn out far to its side, low, the palm turned to face across the arena (the hand on its edge, the little
# finger down); swept low along the floor in an arc across in front of the face, the shoulder tearing out of the wall
# after it to reach; carried on past and gathered back.
SWEEP_BACK = {"Shoulder": W(-0.3, -0.42, -0.52), "_Wrist": W(-7.6, -2.2, 0.75), "_Hand": H(22, -90, -35),
              "_Pole": W(-0.7, 0.1, 0.7), "_Grip": G(6, 11, -2)}
SWEEP_END = {"Shoulder": W(2.6, -1.5, -0.85), "_Wrist": W(3.6, -2.5, 1.2), "_Hand": H(30, -85, 60),
             "_Pole": W(0.6, 0.3, 0.8), "_Grip": G(20, 10, 8)}


def sweep_draw(I):
    # SweepDraw (0.9 s, the tell): a wind the other way first (the hand eased in and up), then hauled out far to its
    # side and down, low, turning on its edge, the palm facing across; overshooting out and settling, trembling.
    return keys(held(I, 1), {
        "Shoulder": [(6, W(0.05, 0.05, 0.1)), (20, W(-0.3, -0.4, -0.5)), (24, W(-0.33, -0.45, -0.56)), (28, SWEEP_BACK["Shoulder"])],
        "_Wrist": [(6, W(-4.9, -3.6, 4.9)), (18, W(-7.3, -2.4, 1.1)), (22, W(-7.75, -2.1, 0.7)), (25, W(-7.55, -2.2, 0.8)),
                   (28, SWEEP_BACK["_Wrist"])],
        "_Hand": [(7, H(40, -15, 22)), (16, H(28, -70, -25)), (22, H(24, -94, -39)), (28, SWEEP_BACK["_Hand"])],
        "_Pole": [(16, W(-0.8, 0.0, 0.6)), (28, SWEEP_BACK["_Pole"])],
        "_Grip": [(8, G(30, 0, 15)), (18, G(10, 8, 0)), (24, G(3, 12, -4)), (28, SWEEP_BACK["_Grip"])],
        "_Twitch1": [(22, S(0)), (24, S(-4)), (26, S(3)), (28, S(0))],
        "_Twitch3": [(22, S(0)), (23, S(4)), (25, S(-4)), (27, S(2)), (28, S(0))],
        "_Twitch2": [(28, S(0))], "_Twitch4": [(28, S(0))],
    })


def sweep(I):
    # Sweep (0.6 s; crosses the middle at 50%, frame 10): swung in along the floor, the palm leading, the fingertips
    # brushing it, round in front of the face and across to the other side; the shoulder tears out of the wall after
    # it (it has to, to reach); the fingers trail, bent back, and curl in at the end.
    return keys(held(SWEEP_BACK, 1), {
        "Shoulder": [(6, W(0.6, -1.2, -0.9)), (10, W(2.0, -2.0, -1.2)), (15, W(2.5, -1.8, -1.0)), (19, SWEEP_END["Shoulder"])],
        "_Wrist": [(4, W(-7.0, -3.8, 0.55)), (7, W(-4.4, -5.0, 0.5)), (10, W(0.0, -5.3, 0.5)), (13, W(2.6, -4.4, 0.55)),
                   (16, W(3.3, -3.0, 0.85)), (19, SWEEP_END["_Wrist"])],
        "_Hand": [(5, H(20, -90, -28)), (10, H(18, -90, 2)), (14, H(20, -90, 30)), (19, SWEEP_END["_Hand"])],
        "_Pole": [(10, W(0.0, 0.3, 1.0)), (19, SWEEP_END["_Pole"])],
        "_Grip": [(6, G(-6, 6, -6)), (12, G(-10, 4, -8)), (16, G(6, 8, 0)), (19, SWEEP_END["_Grip"])],
        "_Twitch1": [(1, S(0))], "_Twitch2": [(1, S(0))], "_Twitch3": [(1, S(0))], "_Twitch4": [(1, S(0))],
        "_Tear1": [(1, S(0))], "_Tear2": [(1, S(0))], "_Tear3": [(1, S(0))],
    })


def sweep_recover(I):
    # SweepRecover (0.8 s): carried on past by its weight (the hand flung up and out), then the shoulder sinks back into
    # the wall and the hand is gathered back round in front, up to its hover (overshooting, settling: Idle's frame 1).
    return keys(held(SWEEP_END, 1), {
        "Shoulder": [(5, W(2.75, -1.35, -0.75)), (14, W(1.0, -0.6, -0.3)), (20, W(-0.1, 0.05, 0.05)), (25, I["Shoulder"])],
        "_Wrist": [(5, W(3.9, -2.1, 1.8)), (12, W(0.5, -4.2, 3.0)), (19, W(-5.0, -3.8, 4.9)), (22, W(-5.75, -3.35, 4.75)),
                   (25, I["_Wrist"])],
        "_Hand": [(6, H(45, -70, 70)), (13, H(50, -30, 30)), (20, H(36, -8, 10)), (25, I["_Hand"])],
        "_Pole": [(12, W(0.0, 0.2, 1.0)), (25, I["_Pole"])],
        "_Grip": [(5, G(30, 6, 14)), (12, G(14, 4, 8)), (19, G(22, 3, 12)), (25, I["_Grip"])],
        "_Twitch1": [(25, I["_Twitch1"])], "_Twitch2": [(25, I["_Twitch2"])], "_Twitch3": [(25, I["_Twitch3"])],
        "_Twitch4": [(25, I["_Twitch4"])],
        "_Tear1": [(1, S(0))], "_Tear2": [(1, S(0))], "_Tear3": [(1, S(0))],
    })


def withdraw(I, start=1, scale=1.0):
    # Withdraw (1 s): it flinches (the hand jerks up, the fingers clench), then the arm is sucked back into the wall,
    # the shoulder first, the arm straightening out ahead of it, the fingers trailing open, last to go.
    t = lambda f: start + (f - 1) * scale
    return keys(held(I, start), {
        "Shoulder": [(t(6), W(0, -0.12, 0.05)), (t(14), W(0, 1.2, 0)), (t(22), W(0, 4.5, 0)), (t(28), W(0, 7.6, 0)),
                     (t(31), WITHDRAWN["Shoulder"])],
        "_Wrist": [(t(6), W(-5.8, -3.6, 5.2)), (t(14), W(-5.0, -3.6, 4.0)), (t(22), W(-4.6, -1.1, 3.6)), (t(28), W(-4.5, 1.95, 3.5)),
                   (t(31), WITHDRAWN["_Wrist"])],
        "_Hand": [(t(6), H(20, -20, 15)), (t(14), H(25, -5, 8)), (t(22), H(10, 0, 2)), (t(31), WITHDRAWN["_Hand"])],
        "_Pole": [(t(20), W(-0.7, 0, 0.7)), (t(31), WITHDRAWN["_Pole"])],
        "_Grip": [(t(6), G(60, 0, 30)), (t(16), G(50, 2, 25)), (t(24), G(14, 6, 5)), (t(31), WITHDRAWN["_Grip"])],
        "_Twitch1": [(t(31), S(0))], "_Twitch2": [(t(31), S(0))], "_Twitch3": [(t(31), S(0))], "_Twitch4": [(t(31), S(0))],
        "_Tear1": [(start, S(0))], "_Tear2": [(start, S(0))], "_Tear3": [(start, S(0))],
    })


def tear_out(I, start=1, scale=1.0, high=0.0, end=37):
    """The hand tearing out of the wall (Return; Wake's hands): pushed out, fingers first, splayed, stone strands
    stretching back from the forearm and hand to the wall where it came out, snapping one after another; it rises high,
    spread, and comes down into its hover (Idle's frame 1 at `end`)."""
    t = lambda f: start + (f - 1) * scale
    return keys(held(WITHDRAWN, start), {
        "Shoulder": [(t(4), W(0, 7.4, 0)), (t(12), W(0, 1.5, 0)), (t(16), W(0, -0.25, -0.1)), (t(22), W(0, 0.05, 0.05)),
                     (t(30), W(0, -0.03, 0)), (end, I["Shoulder"])],
        "_Wrist": [(t(4), W(-4.5, 1.75, 3.5)), (t(8), W(-4.55, -1.1, 3.6)), (t(12), W(-4.7, -3.9, 3.9)),
                   (t(17), W(-5.6, -3.6, 6.0 + high)), (t(22), W(-6.0, -3.3, 6.4 + high)), (t(29), W(-5.5, -3.5, 4.3)),
                   (t(33), W(-5.65, -3.38, 4.7)), (end, I["_Wrist"])],
        "_Hand": [(t(8), H(-5, 0, 0)), (t(13), H(10, -5, 8)), (t(18), H(-30, 0, 10)), (t(23), H(-25, 0, 8)), (t(30), H(40, -10, 12)),
                  (end, I["_Hand"])],
        "_Pole": [(t(20), W(-0.8, 0.0, 0.6)), (end, I["_Pole"])],
        "_Grip": [(t(6), G(-10, 12, -5)), (t(14), G(-5, 14, -6)), (t(20), G(-14, 18, -10)), (t(26), G(25, 5, 15)),
                  (t(31), G(16, 4, 8)), (end, I["_Grip"])],
        "_Tear1": [(t(9), S(1)), (t(15), S(1)), (t(17), S(-0.15)), (t(20), S(0.04)), (t(23), S(0))],
        "_Tear2": [(t(9), S(1)), (t(17), S(1)), (t(19), S(-0.15)), (t(22), S(0.04)), (t(25), S(0))],
        "_Tear3": [(t(9), S(1)), (t(19), S(1)), (t(21), S(-0.15)), (t(24), S(0.04)), (t(27), S(0))],
        "_Twitch1": [(end, I["_Twitch1"])], "_Twitch2": [(end, I["_Twitch2"])], "_Twitch3": [(end, I["_Twitch3"])],
        "_Twitch4": [(end, I["_Twitch4"])],
    })


def ret(I):
    return tear_out(I)


# ---------------------------------------------------------------- the face's clips

def face_from(pose, frame):
    return held({b: pose[b] for b in FACE_TRACKS}, frame)


BREATH_IN = {"Face": F_(-8, 0, 0, 0, -0.35, 0.3), "Brow": F_(4, 0, 0, 0, -0.03, -0.1), "Jaw": F_(15, 0, 0, 0, 0, -0.2),
             "Throat": F_(), "_Look": F_(4, 0, 0), "_Lids": F_(-20, 22)}
BREATH_OUT = {"Face": F_(6, 0, 0, 0, -1.0, -0.12), "Brow": F_(6, 0, 0, 0, -0.04, -0.12), "Jaw": F_(30, 0, 0, 0, -0.1, -0.7),
              "Throat": F_(), "_Look": F_(18, 0, 0), "_Lids": F_(-16, 15)}
BITE_BACK = {"Face": F_(-12, 0, 0, 0, 0.45, 0.35), "Brow": F_(3, 0, 0, 0, -0.02, -0.08), "Jaw": F_(32, 0, 0, 0, 0, -0.5),
             "Throat": F_(), "_Look": F_(26, 0, 0), "_Lids": F_(-70, 55)}
BITE_AT = {"Face": F_(20, 0, 0, 0, -1.6, -2.2), "Brow": F_(8, 0, 0, 0, -0.03, -0.1), "Jaw": F_(-2, 0, 0, 0, 0, 0),
           "Throat": F_(), "_Look": F_(20, 0, 0), "_Lids": F_(-62, 48)}
SAGGED = {"Face": F_(12, 3, 2, 0, -0.85, -1.9), "Brow": F_(5, 0, 0, 0, -0.03, -0.09), "Jaw": F_(10, 0, 0, 0, 0, -0.1),
          "Throat": F_(), "_Look": F_(22, 0, 4), "_Lids": F_(-22, 10)}


def breath_draw():
    # BreathDraw (1.2 s): it draws a great breath: the face swells out and lifts, tipping back a little, the mouth
    # opening after it, the eyes narrowing, the brow coming down; held at the top, straining (a tremor).
    I = IDLE_FACE
    return keys(face_from(I, 1), {
        "Face": [(6, F_(3, 0, 0, 0, 0.04, -0.04)), (24, F_(-6, 0, 0, 0, -0.3, 0.26)), (30, F_(-8.5, 0, 0, 0, -0.37, 0.32)),
                 (33, F_(-7.5, 0, 0, 0, -0.34, 0.29)), (35, F_(-8.2, 0, 0, 0, -0.36, 0.31)), (37, BREATH_IN["Face"])],
        "Brow": [(10, F_(1)), (28, F_(4, 0, 0, 0, -0.03, -0.11)), (37, BREATH_IN["Brow"])],
        "Jaw": [(8, F_(2)), (28, F_(13, 0, 0, 0, 0, -0.18)), (33, F_(16, 0, 0, 0, 0, -0.22)), (37, BREATH_IN["Jaw"])],
        "Throat": [(1, F_())],
        "_Look": [(14, F_(10, 0, 0)), (16, F_(4, 0, 0)), (37, BREATH_IN["_Look"])],
        "_Lids": [(12, F_(-50, 38)), (26, F_(-22, 24)), (37, BREATH_IN["_Lids"])],
    })


def breath():
    # Breath (1.5 s): it breathes out a gust: the face pushes out and down at the arena, the mouth thrown wide (the jaw
    # lagging the push), the face shaking with it; the gust dies away (the jaw sinking, the face held out).
    return keys(face_from(BREATH_IN, 1), {
        "Face": [(5, F_(4, 0, 0, 0, -0.85, -0.05)), (8, F_(6.5, 0, 0, 0, -1.05, -0.14)), (12, F_(5.5, 0.6, 0, 0, -0.97, -0.1)),
                 (16, F_(6.5, -0.6, 0, 0, -1.02, -0.13)), (20, F_(5.8, 0.5, 0, 0, -0.98, -0.11)), (24, F_(6.4, -0.5, 0, 0, -1.01, -0.13)),
                 (29, F_(5.9, 0.3, 0, 0, -0.99, -0.12)), (34, F_(6.2, -0.2, 0, 0, -1.0, -0.12)), (46, BREATH_OUT["Face"])],
        "Brow": [(6, F_(7, 0, 0, 0, -0.04, -0.13)), (40, F_(6, 0, 0, 0, -0.04, -0.12)), (46, BREATH_OUT["Brow"])],
        "Jaw": [(4, F_(22, 0, 0, 0, -0.05, -0.45)), (9, F_(32, 0, 0, 0, -0.1, -0.76)), (13, F_(29, 0, 0, 0, -0.1, -0.68)),
                (33, F_(31, 0, 0, 0, -0.1, -0.72)), (46, BREATH_OUT["Jaw"])],
        "Throat": [(1, F_())],
        "_Look": [(6, F_(18, 0, 0)), (46, BREATH_OUT["_Look"])],
        "_Lids": [(5, F_(-12, 12)), (30, F_(-14, 13)), (46, BREATH_OUT["_Lids"])],
    })


def breath_recover():
    # BreathRecover (0.8 s): spent, it sinks back into its hover, the jaw closing after, the eyes opening.
    I = IDLE_FACE
    return keys(face_from(BREATH_OUT, 1), {
        "Face": [(10, F_(1, 0, 0, 0, -0.25, -0.05)), (17, F_(3, 0, 0, 0, 0.06, 0.03)), (25, I["Face"])],
        "Brow": [(12, F_(2, 0, 0, 0, -0.02, -0.05)), (25, I["Brow"])],
        "Jaw": [(6, F_(24, 0, 0, 0, -0.08, -0.55)), (16, F_(4, 0, 0, 0, 0, -0.02)), (20, F_(2)), (25, I["Jaw"])],
        "Throat": [(1, F_())],
        "_Look": [(14, F_(12, 0, 0)), (25, I["_Look"])],
        "_Lids": [(12, F_(-45, 33)), (25, I["_Lids"])],
    })


def bite_draw():
    # BiteDraw (0.8 s): the face pulls back into the wall and up, the mouth opening wide after it, the eyes wide and on
    # you; held, quivering.
    I = IDLE_FACE
    return keys(face_from(I, 1), {
        "Face": [(5, F_(4, 0, 0, 0, -0.08, -0.05)), (16, F_(-10, 0, 0, 0, 0.38, 0.3)), (20, F_(-13, 0, 0, 0, 0.48, 0.37)),
                 (22, F_(-11.5, 0, 0, 0, 0.44, 0.34)), (25, BITE_BACK["Face"])],
        "Brow": [(8, F_(-2, 0, 0, 0, 0, 0.04)), (18, F_(3, 0, 0, 0, -0.02, -0.08)), (25, BITE_BACK["Brow"])],
        "Jaw": [(7, F_(2)), (17, F_(28, 0, 0, 0, 0, -0.42)), (21, F_(34, 0, 0, 0, 0, -0.54)), (25, BITE_BACK["Jaw"])],
        "Throat": [(1, F_())],
        "_Look": [(6, F_(18, 0, 0)), (8, F_(26, 0, 0)), (25, BITE_BACK["_Look"])],
        "_Lids": [(6, F_(-58, 42)), (12, F_(-72, 56)), (25, BITE_BACK["_Lids"])],
    })


def bite():
    # Bite (0.5 s; the jaws snap shut at 50%, frame 8.5): it lunges down and out, the jaw hanging open behind the lunge,
    # and snaps shut at the floor in front of the wall; the face carried on a little by its weight and settling.
    return keys(face_from(BITE_BACK, 1), {
        "Face": [(3, F_(-6, 0, 0, 0, 0.2, 0.15)), (6, F_(12, 0, 0, 0, -1.0, -1.3)), (8.5, BITE_AT["Face"]),
                 (11, F_(21.5, 0, 0, 0, -1.7, -2.3)), (16, BITE_AT["Face"])],
        "Brow": [(4, F_(0, 0, 0, 0, 0, -0.02)), (9, F_(9, 0, 0, 0, -0.04, -0.13)), (16, BITE_AT["Brow"])],
        "Jaw": [(3, F_(34, 0, 0, 0, 0, -0.54)), (6, F_(36, 0, 0, 0, 0, -0.6)), (8.5, F_(-2, 0, 0, 0, 0, 0.0)),
                (10, F_(5, 0, 0, 0, 0, -0.04)), (12, F_(-1)), (16, BITE_AT["Jaw"])],
        "Throat": [(1, F_())],
        "_Look": [(5, F_(30, 0, 0)), (16, BITE_AT["_Look"])],
        "_Lids": [(7, F_(-66, 50)), (9, F_(-40, 30)), (12, F_(-64, 49)), (16, BITE_AT["_Lids"])],
    })


def bite_recover():
    # BiteRecover (0.8 s): it hauls its face back up out of the floor, up past its hover and down into it.
    I = IDLE_FACE
    return keys(face_from(BITE_AT, 1), {
        "Face": [(5, F_(18, 0, 0, 0, -1.55, -2.1)), (14, F_(4, 0, 0, 0, -0.3, -0.2)), (19, F_(0.5, 0, 0, 0, 0.06, 0.08)),
                 (25, I["Face"])],
        "Brow": [(8, F_(5, 0, 0, 0, -0.03, -0.08)), (25, I["Brow"])],
        "Jaw": [(6, F_(8, 0, 0, 0, 0, -0.08)), (15, F_(2)), (25, I["Jaw"])],
        "Throat": [(1, F_())],
        "_Look": [(10, F_(18, 0, -4)), (25, I["_Look"])],
        "_Lids": [(10, F_(-48, 34)), (25, I["_Lids"])],
    })


def sag():
    # Sag (1.2 s): spent, it lets out a breath (a small lift first), then sinks down and forward, faster as it goes,
    # until its chin is near the floor; it lands heavily (overshooting down, a rebound) and settles, tipped and
    # slack, the jaw falling open after the landing, the eyes half shut.
    I = IDLE_FACE
    s = SAGGED["Face"]
    return keys(face_from(I, 1), {
        "Face": [(7, F_(0, 0, 0, 0, 0.05, 0.14)), (18, F_(6, 1, 1, 0, -0.35, -0.7)), (26, F_(13, 3.5, 2, 0, -0.9, -2.02)),
                 (30, F_(11, 2.6, 2, 0, -0.82, -1.84)), (34, F_(12.4, 3.1, 2, 0, -0.86, -1.93)), (37, s)],
        "Brow": [(9, F_(-2, 0, 0, 0, 0, 0.03)), (27, F_(7, 0, 0, 0, -0.04, -0.12)), (32, F_(4, 0, 0, 0, -0.02, -0.07)),
                 (37, SAGGED["Brow"])],
        "Jaw": [(8, F_(1)), (24, F_(3)), (29, F_(14, 0, 0, 0, 0, -0.14)), (33, F_(8, 0, 0, 0, 0, -0.08)), (37, SAGGED["Jaw"])],
        "Throat": [(1, F_())],
        "_Look": [(10, F_(8, 0, 0)), (12, F_(22, 0, 4)), (37, SAGGED["_Look"])],
        "_Lids": [(8, F_(-45, 34)), (22, F_(-30, 18)), (28, F_(-14, 6)), (37, SAGGED["_Lids"])],
    })


def sagged():
    # Sagged (2 s loop): resting there, heaving: two great breaths, the face lifting and sinking on its chin, the jaw
    # working after each, the lids fluttering, an eye roll.
    s = SAGGED
    fx = lambda dz, dy, rx: F_(12 + rx, 3, 2, 0, -0.85 + dy, -1.9 + dz)
    return {
        "Face": [(1, s["Face"]), (14, fx(0.12, -0.04, -1.2)), (30, fx(-0.03, 0.01, 0.4)), (44, fx(0.09, -0.03, -0.9))],
        "Brow": [(1, s["Brow"]), (18, F_(4, 0, 0, 0, -0.02, -0.06)), (47, F_(4.5, 0, 0, 0, -0.025, -0.07))],
        "Jaw": [(1, s["Jaw"]), (19, F_(17, 0, 0, 0, 0, -0.16)), (34, F_(7, 0, 0, 0, 0, -0.06)), (49, F_(15, 0, 0, 0, 0, -0.14))],
        "Throat": [(1, F_())],
        "_Look": [(1, s["_Look"]), (25, s["_Look"]), (28, F_(12, 0, -10)), (40, F_(12, 0, -10)), (43, F_(22, 0, 4))],
        "_Lids": [(1, s["_Lids"]), (12, F_(-27, 12)), (22, F_(-10, 5)), (24, F_(-25, 11)), (40, F_(-18, 8)), (52, F_(-24, 11))],
    }


def rise():
    # Rise (1.2 s): it gathers itself (a sink first, pressing down), then heaves up and back into the wall, past its
    # hover and down into it, the jaw closing, the eyes opening last.
    I = IDLE_FACE
    return keys(face_from(SAGGED, 1), {
        "Face": [(6, F_(14, 3, 2, 0, -0.88, -2.02)), (20, F_(4, 1, 0.5, 0, -0.2, -0.35)), (27, F_(0, 0, 0, 0, 0.08, 0.14)),
                 (32, F_(2.6, 0, 0, 0, -0.02, -0.03)), (37, I["Face"])],
        "Brow": [(8, F_(7, 0, 0, 0, -0.04, -0.12)), (24, F_(-2, 0, 0, 0, 0, 0.04)), (37, I["Brow"])],
        "Jaw": [(5, F_(4)), (17, F_(10, 0, 0, 0, 0, -0.08)), (26, F_(1)), (37, I["Jaw"])],
        "Throat": [(1, F_())],
        "_Look": [(14, F_(22, 0, 4)), (17, F_(12, 0, 0)), (37, I["_Look"])],
        "_Lids": [(6, F_(-12, 6)), (24, F_(-40, 28)), (30, F_(-60, 44)), (37, I["_Lids"])],
    })


# ---------------------------------------------------------------- the whole body's clips

def dormant():
    # Dormant (2 s loop): all but sunk into the wall: the face a faint relief (its nose and brow), eyes shut, breathing
    # faintly, slowly; the arms withdrawn into the rock.
    return keys(DORMANT_FACE_TRACKS, sided(held(WITHDRAWN, 1), "L"), sided(held(WITHDRAWN, 1), "R"))


def wake():
    # Wake (4 s): a shudder (it sinks a little further in), then the face swells out of the stone, overshooting and
    # settling; the eyes crack and open (a long look round: left, right, down at you), the mouth gapes; then the hands
    # tear out either side, fingers first, strands of stone stretching back to the wall and snapping, the left a beat
    # before the right; they rise high and spread, and come down into their hover (Idle's frame 1).
    I = IDLE_FACE
    face = keys(face_from(DORMANT_FACE, 1), {
        "Face": [(10, F_(0.5, 0, 0, 0, 1.2, -0.03)), (30, F_(-2, 0, 0, 0, 0.3, 0.08)), (40, F_(-4, 0, 0, 0, -0.15, 0.12)),
                 (48, F_(-2, 0, 0, 0, 0.03, 0.05)), (60, F_()), (76, F_(3, 0, -2, 0, -0.05, 0)), (92, F_(2, 0, 2)),
                 (106, F_(-3, 0, 0, 0, -0.12, 0.08)), (121, I["Face"])],
        "Brow": [(30, F_()), (44, F_(-4, 0, 0, 0, 0, 0.08)), (70, F_(0, 0, 0, 0, 0, 0.02)), (108, F_(4, 0, 0, 0, -0.02, -0.06)),
                 (121, I["Brow"])],
        "Jaw": [(40, F_(2)), (52, F_(26, 0, 0, 0, 0, -0.3)), (66, F_(22, 0, 0, 0, 0, -0.25)), (80, F_(6)),
                (106, F_(18, 0, 0, 0, 0, -0.15)), (121, I["Jaw"])],
        "Throat": [(1, F_())],
        "_Look": [(50, F_()), (58, F_(5, 0, 0)), (60, F_(8, 0, -18)), (74, F_(8, 0, -18)), (77, F_(10, 0, 18)),
                  (90, F_(10, 0, 18)), (93, F_(18, 0, 0)), (121, I["_Look"])],
        "_Lids": [(38, F_()), (44, F_(-10, 6)), (48, F_(-6, 4)), (58, F_(-64, 46)), (100, F_(-60, 44)), (121, I["_Lids"])],
    })
    arms = keys(sided(tear_out(IDLE1["L"], start=56, scale=1.15, high=0.6, end=121), "L"),
                sided(held(WITHDRAWN, 1), "L"),
                sided(tear_out(IDLE1["R"], start=62, scale=1.1, high=0.4, end=121), "R"),
                sided(held(WITHDRAWN, 1), "R"))
    return keys(face, arms)


def roar():
    # Roar (1.6 s; the phase change): it screams silently: a gathering (the face drawn in, the hands curling), then the
    # face thrown out and back, the jaw wide, the eyes staring, the brow up, shaking; the hands rise and clench into
    # fists, trembling; it lets go and sinks back into its hover.
    I = IDLE_FACE
    face = keys(face_from(I, 1), {
        "Face": [(8, F_(6, 0, 0, 0, 0.12, -0.08)), (15, F_(-12, 0, 0, 0, -0.65, 0.25)), (19, F_(-10.5, 0.8, 0, 0, -0.58, 0.22)),
                 (23, F_(-12, -0.8, 0, 0, -0.63, 0.25)), (27, F_(-10.8, 0.7, 0, 0, -0.6, 0.23)), (31, F_(-11.8, -0.6, 0, 0, -0.62, 0.24)),
                 (36, F_(-10.5, 0, 0, 0, -0.57, 0.21)), (43, F_(3, 0, 0, 0, 0.05, -0.02)), (49, I["Face"])],
        "Brow": [(9, F_(5, 0, 0, 0, -0.02, -0.08)), (16, F_(-6, 0, 0, 0, 0, 0.12)), (38, F_(-5, 0, 0, 0, 0, 0.1)), (49, I["Brow"])],
        "Jaw": [(9, F_(1)), (17, F_(33, 0, 0, 0, -0.1, -0.82)), (21, F_(30, 0, 0, 0, -0.1, -0.74)), (27, F_(33, 0, 0, 0, -0.1, -0.8)),
                (36, F_(31, 0, 0, 0, -0.1, -0.76)), (45, F_(3)), (49, I["Jaw"])],
        "Throat": [(1, F_())],
        "_Look": [(10, F_(14, 0, 0)), (16, F_(4, 0, 0)), (38, F_(6, 0, 0)), (49, I["_Look"])],
        "_Lids": [(9, F_(-30, 24)), (16, F_(-76, 58)), (38, F_(-74, 57)), (46, F_(-50, 37)), (49, I["_Lids"])],
    })

    def arm(I):
        iw = I["_Wrist"][1]
        return keys(held(I, 1), {
            "Shoulder": [(10, W(0, 0.05, 0)), (16, W(0, -0.2, 0.15)), (38, W(0, -0.18, 0.14)), (49, I["Shoulder"])],
            "_Wrist": [(8, W(*add3(iw, (0.2, 0.05, -0.25)))), (15, W(*add3(iw, (-0.45, 0.2, 1.3)))), (19, W(*add3(iw, (-0.4, 0.18, 1.2)))),
                       (23, W(*add3(iw, (-0.46, 0.2, 1.3)))), (28, W(*add3(iw, (-0.41, 0.18, 1.22)))), (33, W(*add3(iw, (-0.45, 0.2, 1.28)))),
                       (39, W(*add3(iw, (-0.38, 0.15, 1.1)))), (45, W(*add3(iw, (0.0, 0.0, -0.15)))), (49, I["_Wrist"])],
            "_Hand": [(10, H(45, -10, 12)), (16, H(-5, -20, 10)), (38, H(-2, -18, 10)), (49, I["_Hand"])],
            "_Pole": [(16, W(-0.8, 0.0, 0.6)), (49, I["_Pole"])],
            "_Grip": [(7, G(-10, 14, -6)), (14, G(88, 0, 45)), (38, G(84, 0, 42)), (43, G(10, 8, 5)), (49, I["_Grip"])],
            "_Twitch1": [(16, S(0)), (19, S(-6)), (23, S(5)), (27, S(-5)), (31, S(4)), (35, S(0)), (49, I["_Twitch1"])],
            "_Twitch3": [(16, S(0)), (21, S(5)), (25, S(-6)), (29, S(5)), (33, S(-3)), (37, S(0)), (49, I["_Twitch3"])],
            "_Twitch2": [(49, I["_Twitch2"])], "_Twitch4": [(49, I["_Twitch4"])],
            "_Tear1": [(1, S(0))], "_Tear2": [(1, S(0))], "_Tear3": [(1, S(0))],
        })
    return keys(face, both(arm))


DEATH_JAW = F_(8, 0, 0, 0, -0.2, -2.85)    # (the jaw down on the floor: see the build's report)
DEAD_FACE = {"Face": F_(5, 0, -1, 0, 0.1, -0.27), "Brow": F_(6, 0, 0, 0, -0.03, -0.1), "Jaw": DEATH_JAW,
             "Throat": F_(0, 0, 0, 0, 0, -5.0), "_Look": F_(26, 0, 0), "_Lids": F_(0, 0)}


def death():
    # Death (5 s): it lets go. A jolt (the face thrown back, the jaw open in a last silent cry, the hands clenching, then
    # flung open); the hands go limp, sag and hang, and are drawn slowly back into the wall, gone; the face sags down,
    # slack, the jaw falling open wide (heavier and heavier) until it lies on the floor, the mouth open on the way out
    # behind it; the eyes roll and close. Still.
    I = IDLE_FACE
    d = DEAD_FACE
    face = keys(face_from(I, 1), {
        "Face": [(6, F_(-7, 0, 0, 0, 0.15, 0.12)), (16, F_(-5, 0, 0, 0, 0.12, 0.08)), (40, F_(-2, 0, -1, 0, 0.1, 0.0)),
                 (70, F_(3, 0, -1.5, 0, 0.1, -0.18)), (100, F_(5.5, 0, -1, 0, 0.1, -0.3)), (110, F_(4.8, 0, -1, 0, 0.1, -0.25)),
                 (122, d["Face"])],
        "Brow": [(7, F_(-5, 0, 0, 0, 0, 0.1)), (40, F_(0)), (100, F_(6, 0, 0, 0, -0.03, -0.11)), (122, d["Brow"])],
        "Jaw": [(5, F_(4)), (12, F_(24, 0, 0, 0, 0, -0.35)), (24, F_(18, 0, 0, 0, 0, -0.3)), (45, F_(22, 0, 0, 0, -0.05, -0.6)),
                (70, F_(16, 0, 0, 0, -0.1, -1.4)), (92, F_(9, 0, 0, 0, -0.2, -2.92)), (98, F_(8.5, 0, 0, 0, -0.2, -2.77)),
                (106, F_(8, 0, 0, 0, -0.2, -2.88)), (118, d["Jaw"])],
        "Throat": [(60, F_()), (80, F_(0, 0, 0, 0, 0, -2.0)), (100, d["Throat"])],
        "_Look": [(6, F_(-6, 0, 0)), (24, F_(-4, 0, 0)), (27, F_(10, 0, 12)), (50, F_(10, 0, 12)), (54, F_(16, 0, -6)),
                  (90, F_(20, 0, 0)), (130, d["_Look"])],
        "_Lids": [(6, F_(-74, 56)), (30, F_(-66, 50)), (70, F_(-40, 28)), (105, F_(-14, 8)), (112, F_(-18, 10)),
                  (132, d["_Lids"])],
    })

    def arm(I):
        return keys(held(I, 1), {
            "Shoulder": [(8, W(0, -0.15, 0.1)), (20, W(0, -0.1, 0.0)), (40, W(0, 0.0, -0.5)), (60, W(0, 0.3, -0.6)),
                         (85, W(0, 3.0, -0.4)), (110, W(0, 7.6, 0)), (118, WITHDRAWN["Shoulder"])],
            "_Wrist": [(8, W(-5.8, -3.6, 5.6)), (16, W(-5.5, -3.5, 5.0)), (30, W(-5.3, -3.6, 2.6)), (42, W(-5.1, -3.8, 1.7)),
                       (56, W(-5.0, -3.4, 1.85)), (85, W(-4.6, -1.0, 2.9)), (110, W(-4.5, 1.95, 3.5)), (118, WITHDRAWN["_Wrist"])],
            "_Hand": [(8, H(10, -10, 10)), (20, H(30, -10, 10)), (35, H(80, -5, 5)), (50, H(85, -3, 4)), (80, H(60, 0, 2)),
                      (105, H(10, 0, 0)), (118, WITHDRAWN["_Hand"])],
            "_Pole": [(40, W(-0.8, 0.2, 0.5)), (118, WITHDRAWN["_Pole"])],
            "_Grip": [(6, G(75, 0, 40)), (12, G(70, 0, 35)), (24, G(-10, 14, -8)), (34, G(30, 4, 15)), (60, G(35, 2, 18)),
                      (110, G(10, 0, 5)), (118, WITHDRAWN["_Grip"])],
            "_Twitch2": [(36, S(0)), (40, S(-14)), (46, S(0))], "_Twitch4": [(52, S(0)), (55, S(-10)), (60, S(0))],
            "_Twitch1": [(118, S(0))], "_Twitch3": [(118, S(0))],
            "_Tear1": [(1, S(0))], "_Tear2": [(1, S(0))], "_Tear3": [(1, S(0))],
        })
    return keys(face, both(arm))


def idle():
    return keys(IDLE_FACE_TRACKS, IDLE_ARM["L"], IDLE_ARM["R"])


ACTIONS = {
    "Dormant": clip(61, dormant(), periodic=True),
    "Wake": clip(121, wake()),
    "Idle": clip(91, idle(), periodic=True),
}
for _side in "LR":
    ACTIONS.update({
        "SlamDraw" + _side: clip(37, arm_clip(_side, slam_draw)),
        "Slam" + _side: clip(13, arm_clip(_side, slam)),
        "SlamStunned" + _side: clip(31, arm_clip(_side, slam_stunned), periodic=True),
        "SlamRecover" + _side: clip(31, arm_clip(_side, slam_recover)),
    })
for _side in "LR":
    ACTIONS.update({
        "SweepDraw" + _side: clip(28, arm_clip(_side, sweep_draw)),
        "Sweep" + _side: clip(19, arm_clip(_side, sweep)),
        "SweepRecover" + _side: clip(25, arm_clip(_side, sweep_recover)),
    })
for _side in "LR":
    ACTIONS.update({
        "Withdraw" + _side: clip(31, arm_clip(_side, withdraw)),
        "Return" + _side: clip(37, arm_clip(_side, ret)),
    })
ACTIONS.update({
    "BreathDraw": clip(37, breath_draw()),
    "Breath": clip(46, breath()),
    "BreathRecover": clip(25, breath_recover()),
    "BiteDraw": clip(25, bite_draw()),
    "Bite": clip(16, bite()),
    "BiteRecover": clip(25, bite_recover()),
    "Sag": clip(37, sag()),
    "Sagged": clip(61, sagged(), periodic=True),
    "Rise": clip(37, rise()),
    "Roar": clip(49, roar()),
    "Death": clip(151, death()),
})


# ---------------------------------------------------------------- building

def mesh_object(name, V, F, face_mats, materials, groups):
    me = bpy.data.meshes.new(name)
    me.from_pydata([tuple(map(float, v)) for v in V], [], [tuple(int(i) for i in f) for f in F])
    me.update()
    for m in materials:
        me.materials.append(m)
    me.polygons.foreach_set("material_index", [int(i) for i in face_mats])
    me.polygons.foreach_set("use_smooth", [True] * len(F))
    ob = bpy.data.objects.new(name, me)
    bpy.context.scene.collection.objects.link(ob)
    for g, w in groups.items():
        vg = ob.vertex_groups.new(name=g)
        for i in np.nonzero(np.asarray(w) > 1e-4)[0]:
            vg.add([int(i)], float(w[i]), "REPLACE")
    return ob


def closed(name, V, F, face_mats, materials, group, matrix):
    import bmesh
    Vt = [tuple(matrix @ Vector(v)) for v in V]
    ob = mesh_object(name, Vt, F, face_mats, materials, {group: np.ones(len(V))})
    bm = bmesh.new()
    bm.from_mesh(ob.data)
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    bm.to_mesh(ob.data)
    bm.free()
    return ob


def strand(name, a, b, base, bone, material):
    """A strand of stone from a (on its base bone) to b (its own bone): stretched when its bone is pinned to the wall."""
    n = 9
    pts = [Vector(a).lerp(Vector(b), i / (n - 1)) for i in range(n)]
    radii = [0.12 * (1 - 0.55 * math.sin(math.pi * i / (n - 1))) for i in range(n)]
    ob = part(name, kit.tube(pts, radii, seg=7), material, smooth=True)
    gb, gs = ob.vertex_groups.new(name=base), ob.vertex_groups.new(name=bone)
    d = Vector(b) - Vector(a)
    for v in ob.data.vertices:
        t = max(0.0, min(1.0, (v.co - Vector(a)).dot(d) / d.length_squared))
        t = t * t * (3 - 2 * t)
        gb.add([v.index], 1 - t, "REPLACE")
        gs.add([v.index], t, "REPLACE")
    return ob


def arm_parts(side, wall, kept, nail):
    sx = SIDE_X[side]
    J, E0, W0 = Vector(BONES["Arm" + side][0]), Vector(BONES["Fore" + side][0]), Vector(BONES["Hand" + side][0])
    parts = []
    # The root, back into the rock; the shoulder where it comes out of the wall.
    parts.append(part("ArmRoot", kit.tube([J + Vector((0, 3.6, 0)), J + Vector((0, 1.6, 0)), J], [0.72, 0.66, 0.56], seg=14),
                      wall, smooth=True, jitter=0.03, group="Shoulder" + side))
    parts.append(part("ShoulderKnob", kit.sphere(14, 10, 0.6), wall, M(tuple(J)), smooth=True, jitter=0.03, group="Shoulder" + side))
    # The upper arm and forearm: long, knotted, like something pulled long; the elbow a knob of stone.
    mid = J.lerp(E0, 0.45)
    parts.append(part("UpperArm", kit.tube([J, J.lerp(E0, 0.2), mid, J.lerp(E0, 0.8), E0], [0.5, 0.44, 0.47, 0.38, 0.36], seg=14),
                      kept, smooth=True, jitter=0.025, group="Arm" + side))
    parts.append(part("Elbow", kit.sphere(12, 9, 0.43), kept, M(tuple(E0)), smooth=True, jitter=0.02, group="Arm" + side))
    parts.append(part("Forearm", kit.tube([E0, E0.lerp(W0, 0.3), E0.lerp(W0, 0.6), W0], [0.38, 0.36, 0.31, 0.25], seg=14),
                      kept, smooth=True, jitter=0.02, group="Fore" + side))
    parts.append(part("Wrist", kit.sphere(12, 8, 0.27), kept, M(tuple(W0), scale=(1.15, 1, 0.7)), smooth=True, group="Hand" + side))
    # The hand: a broad flat palm, knuckles, four long fingers in three joints each, a thumb, long nails.
    palm_c = hand_point(side, 0.0, 0.27, -0.05, W0)
    parts.append(part("Palm", kit.sphere(16, 10), kept, M(tuple(palm_c), scale=(0.46, 0.32, 0.15)), smooth=True, group="Hand" + side))
    for k, (u, v, lens_, r) in FINGERS.items():
        parts.append(part("Knuckle", kit.sphere(10, 7, r * 1.25), kept, M(tuple(hand_point(side, u, v - 0.03, 0.02, W0))),
                          smooth=True, group="Hand" + side))
        rr = (r, r * 0.85, r * 0.72, r * 0.6)
        for j, seg in enumerate("abc"):
            name = f"Finger{side}{k}{seg}"
            h, t, _ = BONES[name]
            parts.append(part("Finger", kit.tube([h, t], [rr[j], rr[j + 1]], seg=9), kept, smooth=True, group=name))
            if seg != "a":
                parts.append(part("Joint", kit.sphere(9, 6, rr[j] * 1.08), kept, M(h), smooth=True, group=name))
        h, t, _ = BONES[f"Finger{side}{k}c"]
        d = (Vector(t) - Vector(h)).normalized()
        tip = Vector(t) + d * NAIL + Vector((0, 0, -0.04))
        parts.append(part("Nail", kit.cone(6, rr[3] * 1.05, 0.004, NAIL + 0.06), nail,
                          kit.along(tuple(Vector(t) - d * 0.06), tuple(tip)), smooth=False, group=f"Finger{side}{k}c"))
    for j, seg in enumerate("ab"):
        name = f"Thumb{side}{seg}"
        h, t, _ = BONES[name]
        parts.append(part("Thumb", kit.tube([h, t], [(0.13, 0.11)[j], (0.11, 0.085)[j]], seg=9), kept, smooth=True, group=name))
        parts.append(part("ThumbJoint", kit.sphere(9, 6, (0.14, 0.12)[j]), kept, M(h), smooth=True, group=name))
    h, t, _ = BONES[f"Thumb{side}b"]
    d = (Vector(t) - Vector(h)).normalized()
    parts.append(part("Nail", kit.cone(6, 0.07, 0.004, NAIL + 0.04), nail, kit.along(tuple(Vector(t) - d * 0.05), tuple(Vector(t) + d * NAIL)),
                      group=f"Thumb{side}b"))
    # The strands (inside the arm until they're torn).
    for k, (on, _, _) in STRANDS.items():
        name = f"Strand{side}{k}"
        h, _, _ = BONES[name]
        base_h, base_t, _ = BONES[on + side]
        a = Vector(h) - (Vector(base_h) - Vector(base_t)).normalized() * 0.25
        parts.append(strand("Strand", a, h, on + side, name, kept))
    return parts


def posed_vertices(V, groups, pose):
    m = kit.pose_matrices(BONES, pose)
    out = np.zeros_like(V)
    total = np.zeros(len(V))
    Vh = np.hstack([V, np.ones((len(V), 1))])
    for g, w in groups.items():
        if g not in m:
            continue
        sel = w > 1e-5
        if not sel.any():
            continue
        A = np.array(m[g])
        out[sel] += (Vh[sel] @ A.T)[:, :3] * w[sel, None]
        total[sel] += w[sel]
    return out / np.maximum(total, 1e-9)[:, None]


def mesh_arrays(ob):
    V = np.array([v.co[:] for v in ob.data.vertices])
    groups = {}
    names = {g.index: g.name for g in ob.vertex_groups}
    for g in names.values():
        groups[g] = np.zeros(len(V))
    for v in ob.data.vertices:
        for ge in v.groups:
            groups[names[ge.group]][v.index] = ge.weight
    return V, groups


def report(ob):
    V, groups = mesh_arrays(ob)
    frames_of = {name: frames for name, (_, frames) in ACTIONS.items()}
    arm_w = sum(groups.get(b, 0) for s in "LR" for b in arm_bone_names(s))
    arm_sel = arm_w > 0.5
    face_sel = ~arm_sel
    jaw_sel = groups["Jaw"] > 0.5

    def at(name, frame):
        return posed_vertices(V, groups, dict(frames_of[name][frame - 1][1]))

    P = at("Death", 151)
    rest0 = V
    box = (np.abs(P[:, 0]) < 1.2) & (P[:, 2] > 0.15) & (P[:, 2] < 3.0) & (P[:, 1] > -3.0) & (P[:, 1] < 3.0)
    walk = (np.abs(P[:, 0]) < 1.2) & (P[:, 1] < 1.0) & (P[:, 1] > -3.0)
    for i in np.nonzero(box)[0][:8]:
        print(f"[the_hold]   in the way: rest ({rest0[i, 0]:+.2f}, {rest0[i, 1]:+.2f}, {rest0[i, 2]:.2f}) -> ({P[i, 0]:+.2f}, {P[i, 1]:+.2f}, {P[i, 2]:.2f})"
              f" face {groups['Face'][i]:.2f} jaw {groups['Jaw'][i]:.2f} root {groups['Root'][i]:.2f} brow {groups['Brow'][i]:.2f}")
    lip = P[jaw_sel & walk][:, 2].max()
    above = P[walk & (P[:, 2] > 1.0)][:, 2].min()
    print(f"[the_hold] Death, last frame: {int(box.sum())} vertices in the tunnel's way (|x| < 1.2, 0.15 < z < 3, -3 < y < 3);"
          f" the jaw's top {lip:.2f} m, the lowest thing over the way {above:.2f} m; arms' nearest {P[arm_sel][:, 1].min():+.2f} m (y)")
    rest = posed_vertices(V, groups, {})
    chin = int(np.argmin(np.where(face_sel & (np.abs(rest[:, 0]) < 0.15) & (rest[:, 2] > 2.2) & (rest[:, 2] < 2.7),
                                  rest[:, 1], 9)))
    print(f"[the_hold] the face at rest: its centre {FACE_C} m up, out up to {-rest[face_sel][:, 1].min():.2f} m, "
          f"{np.ptp(rest[face_sel & (rest[:, 1] < -0.3)][:, 0]):.2f} m wide; eyes {2 * EYE_R:.1f} m at z {EYE_Z}, {EYE_D:.2f} m out")
    for name, frame in (("Sag", 37), ("Sagged", 1), ("Idle", 1)):
        Q = at(name, frame)
        print(f"[the_hold] {name} f{frame}: the chin {Q[chin, 2]:.2f} m up, {-Q[chin, 1]:.2f} m out")
    Q = at("Bite", 9)
    lips = jaw_sel & (np.abs(rest[:, 0]) < 0.8) & (rest[:, 2] > MZ - 0.3)
    print(f"[the_hold] Bite f9 (the snap): the lower lip {Q[lips][:, 2].min():.2f}-{Q[lips][:, 2].max():.2f} m up, "
          f"{-Q[lips][:, 1].max():.2f}-{-Q[lips][:, 1].min():.2f} m out; the face's lowest {Q[face_sel & (Q[:, 1] < -0.2)][:, 2].min():+.2f} m")
    side_sel = {s: sum(groups.get(b, 0) for b in arm_bone_names(s)) > 0.5 for s in "LR"}
    for name, sides in (("Dormant", "LR"), ("WithdrawL", "L"), ("WithdrawR", "R"), ("Death", "LR")):
        Q = at(name, len(frames_of[name]))
        near = min(Q[side_sel[s]][:, 1].min() for s in sides)
        print(f"[the_hold] {name} last frame: the arm{'s' if len(sides) > 1 else ' ' + sides}'s nearest {near:+.2f} m "
              f"(y; > 0: in the wall); the face out {-Q[face_sel][:, 1].min():.2f} m")
    for side in "LR":
        hand_sel = sum(groups.get(b, 0) for b in arm_bone_names(side)[3:-3]) > 0.5
        Q = at("Slam" + side, 8)
        print(f"[the_hold] Slam{side} f8: the hand {Q[hand_sel][:, 2].min():+.2f}..{Q[hand_sel][:, 2].max():.2f} m up, "
              f"{-Q[hand_sel][:, 1].max():.2f}-{-Q[hand_sel][:, 1].min():.2f} m out, x {Q[hand_sel][:, 0].mean():+.2f}")
        Q = at("SlamStunned" + side, 1)
        print(f"[the_hold] SlamStunned{side}: the hand {Q[hand_sel][:, 2].min():+.2f}..{Q[hand_sel][:, 2].max():.2f} m up")
        nails = (groups.get(f"Finger{side}2c", 0) > 0.5)
        wr, tr, tz = [], [], []
        for f in range(1, 20):
            Q = at("Sweep" + side, f)
            m = kit.pose_matrices(BONES, dict(frames_of["Sweep" + side][f - 1][1]))
            w = m["Fore" + side] @ Vector(BONES["Fore" + side][1])
            wr.append(math.hypot(w.x, w.y))
            tip = Q[nails]
            tr.append(np.hypot(tip[:, 0], tip[:, 1]).max())
            tz.append(Q[hand_sel][:, 2].min())
        Q = at("Sweep" + side, 10)
        w = kit.pose_matrices(BONES, dict(frames_of["Sweep" + side][9][1]))["Fore" + side] @ Vector(BONES["Fore" + side][1])
        print(f"[the_hold] Sweep{side}: the wrist {min(wr):.1f}-{max(wr):.1f} m from the origin, the middle fingertip "
              f"{min(tr):.1f}-{max(tr):.1f} m; the hand's lowest per frame {' '.join(f'{z:.2f}' for z in tz)}; "
              f"at f10 the wrist at ({w.x:+.1f}, {w.y:+.1f}, {w.z:.1f})")
    for i, name in enumerate(ACTIONS):
        r = REPORT[i]
        notes = [f"{s}: wrist lifted off the floor by up to {r['lift'][s]:.2f} m" for s in r["lift"] if r["lift"][s] > 0.02]
        notes += [f"{s}: wrist off its target by up to {r['reach'][s]:.2f} m" for s in r["reach"] if r["reach"][s] > 0.05]
        for note in notes:
            print(f"[the_hold] {name}: {note}")


def build():
    wall, kept = kit.mat("HoldWall_Breathe", "#3b3640"), kit.mat("HoldKept_Breathe", "#5b5361")
    white, iris, pupil = kit.mat("HoldEyeWhite", "#cdc8b8"), kit.mat("HoldEyeIris", "#3c4a41"), kit.mat("HoldEyePupil", "#060707")
    mouth, nail = kit.mat("HoldMouth", "#0b0a0d"), kit.mat("TheHoldNail", "#a49a84")

    V, F, mats, weights = face_geometry()
    parts = [mesh_object("Face", V, F, mats, [wall, kept, mouth], weights)]
    # The throat: a dark hollow behind the mouth while it lives (it drops away in Death, so the way out is open).
    tV, tF = [(0.0, -0.2, 3.2)], []
    for j in range(32):
        a = 2 * math.pi * j / 32
        tV.append((1.8 * math.cos(a), -0.2, 3.2 + 1.3 * math.sin(a)))
    tF = [(0, j + 1, (j + 1) % 32 + 1) for j in range(32)]
    parts.append(mesh_object("Throat", tV, tF, [0] * len(tF), [mouth], {"Throat": np.ones(len(tV))}))
    bV, bT, bM = eye_ball()
    lt, ltq = eye_lid(True)
    lb, lbq = eye_lid(False)
    for side, sx in SIDE_X.items():
        c = (sx * EYE_X, -EYE_D, EYE_Z)
        parts.append(closed("EyeBall", bV, bT, bM, [white, iris, pupil], "Eye" + side, M(c)))
        parts.append(closed("LidTop", lt, ltq, [0] * len(ltq), [kept], "LidTop" + side, M(c)))
        parts.append(closed("LidBottom", lb, lbq, [0] * len(lbq), [kept], "LidBottom" + side, M(c)))
        parts += arm_parts(side, wall, kept, nail)
    mesh = kit.join(parts, "TheHold")
    armature = kit.rig("TheHoldRig", BONES)
    kit.skin(mesh, armature)
    kit.animate(armature, ACTIONS)
    for name, (_, frames) in ACTIONS.items():
        bones = sorted({b for _, p in frames for b in p})
        face = [b for b in bones if b in FACE_BONES]
        arms = {s: [b for b in bones if b in arm_bone_names(s)] for s in "LR"}
        parts_ = (["face"] if face else []) + [f"arm {s}" for s in "LR" if arms[s]]
        print(f"[the_hold] {name}: {len(frames)} frames; keys {', '.join(parts_)} ({len(bones)} bones)")
    report(mesh)
    print(f"[the_hold] {len(mesh.data.vertices)} vertices, {sum(len(p.vertices) - 2 for p in mesh.data.polygons)} tris, "
          f"{len(BONES)} bones")
    return [armature, mesh]
