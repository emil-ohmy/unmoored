"""The Mourner: one of the kept (the Hold's; what its living walls let go of), but not worn down to nothing like the
Kept: tall (2 m to the top of its bowed head), thin as a candle burnt down, black and soft as wet tar, a veil of tar
hanging over its face, two pale eyes under it. It carries something small wrapped in grey against its chest, in the
crook of its left arm, and never sets it down; every blow it strikes is with the long right arm. Come near what it
carries and its grief comes at you in threes. It waits on the Hold's hidden way (the side path to the grotto).

Built and animated as the Kept is (kept.py: its rig's ways, its drips, its melt), taller and upright, with the bundle.

Clips (30 fps; the game's code is written against these names and lengths):
  Idle (loop, 2 s): rocking the bundle, side to side, the head bowed over it; the right arm hanging, swaying after.
  Walk (loop, 1 s): slow, long steps, the body hardly rising; the bundle held still, the right arm swinging. Frame 1
    is the passing pose. Its feet are planted for about 1.4 m/s.
  Windup (0.8 s): the tell of its Grieve: it clutches the bundle in tight, turns its left shoulder to you, rears and
    raises the right arm high and back, the head lifting to you; trembling at the top (Grieve's frame 1).
  Grieve (1.6 s): three blows, a step into each: across from its right (the claws 1.1 m out at chest height, f8),
    back from its left (f22), then down from overhead, the heaviest (f38); the game lands them at those frames. Ends
    bent over, the arm low (GrieveRecover's frame 1).
  GrieveRecover (0.9 s): spent: it sags, drags the arm back, and bows over the bundle (the opening).
  Crouch (0.9 s): the tell of its Lunge: it sinks low, leaning forward, the right arm drawn back low; the head up.
  Lunge (0.4 s): it springs, the claws thrust out ahead (f5: the blow); the game carries it across the floor.
  LungeRecover (0.8 s): sprawled forward, it gathers itself back up to stand.
  Stagger (0.67 s): jolted back, the head snapping, the arm flung up; the bundle clutched; it steps in again.
  Death (1.5 s): it sinks to its knees, folds over the bundle, and melts into the floor (as the Kept), the eyes last.
"""
import math

import bmesh
import bpy
from mathutils import Euler, Quaternion, Vector

M, part = kit.M, kit.part  # `kit` is injected by build_model.py
V = Vector

# ---------------------------------------------------------------- the rig

SH = {-1: (-0.2, -0.04, 1.5), 1: (0.2, -0.04, 1.5)}        # shoulders
EL = {-1: (-0.25, -0.06, 1.13), 1: (0.25, -0.06, 1.13)}     # elbows
WR = {-1: (-0.28, -0.08, 0.74), 1: (0.28, -0.08, 0.74)}     # wrists
KN = {-1: (-0.285, -0.1, 0.62), 1: (0.285, -0.1, 0.62)}     # the hand bone's tail (the knuckles)
HIP = {-1: (-0.1, 0.02, 0.93), 1: (0.1, 0.02, 0.93)}
KNEE = {-1: (-0.12, -0.04, 0.51), 1: (0.12, -0.04, 0.51)}
ANK = {-1: (-0.11, 0.0, 0.07), 1: (0.11, 0.0, 0.07)}
TOE = {-1: (-0.11, -0.15, 0.03), 1: (0.11, -0.15, 0.03)}
FAN = (-1, 0, 1)


def finger(s, fan):
    """A finger's points (knuckle, joint, tip) and its claw's point, at rest: long, thin, curling forward."""
    x0, y0 = WR[s][0] + fan * 0.022, WR[s][1]
    pts = [(x0, y0 - 0.02, 0.65), (x0 + fan * 0.025, y0 - 0.05, 0.58), (x0 + fan * 0.04, y0 - 0.09, 0.53)]
    return pts, (x0 + fan * 0.05, y0 - 0.14, 0.51)


CLAW_TIPS = {s: [V(finger(s, f)[1]) for f in FAN] for s in (-1, 1)}
HEAD_C, HEAD_R = (0, -0.13, 1.71), (0.11, 0.115, 0.14)
EYE = {-1: (-0.04, -0.235, 1.7), 1: (0.04, -0.235, 1.7)}
DRIP = {"DripVeilL": ((-0.08, -0.22, 1.6), (-0.085, -0.23, 1.44), "Head"),
        "DripVeilR": ((0.08, -0.22, 1.6), (0.09, -0.23, 1.42), "Head"),
        "DripBelly": ((0.02, -0.1, 1.1), (0.025, -0.1, 0.94), "Spine"),
        "DripR": ((0.27, -0.03, 1.12), (0.28, -0.03, 0.96), "ArmR")}

BONES = {
    "Root": ((0, 0, 0), (0, 0, 0.3), None),
    "Hips": ((0, 0.02, 0.9), (0, 0.01, 1.02), "Root"),
    "Spine": ((0, 0.02, 0.98), (0, 0.0, 1.26), "Hips"),
    "Chest": ((0, 0.0, 1.26), (0, -0.07, 1.55), "Spine"),   # (stooped a little)
    "Head": ((0, -0.07, 1.55), (0, -0.16, 1.84), "Chest"),
    "Eyes": ((0, -0.225, 1.7), (0, -0.29, 1.7), "Head"),
    "Bundle": ((-0.03, -0.22, 1.26), (-0.03, -0.22, 1.42), "Chest"),  # what it carries, in the crook of the left arm
}
for s, side in ((-1, "L"), (1, "R")):
    BONES["Arm" + side] = (SH[s], EL[s], "Chest")
    BONES["Fore" + side] = (EL[s], WR[s], "Arm" + side)
    BONES["Hand" + side] = (WR[s], KN[s], "Fore" + side)
for s, side in ((-1, "L"), (1, "R")):
    BONES["Leg" + side] = (HIP[s], KNEE[s], "Hips")
    BONES["Shin" + side] = (KNEE[s], ANK[s], "Leg" + side)
    BONES["Foot" + side] = (ANK[s], TOE[s], "Shin" + side)
for name, (head, tail, parent) in DRIP.items():
    BONES[name] = (head, tail, parent)

L_ARM = (V(EL[1]) - V(SH[1])).length
L_FORE = (V(WR[1]) - V(EL[1])).length
FORE_DIR = {s: (V(WR[s]) - V(EL[s])).normalized() for s in (-1, 1)}
STRETCH_MAX = 0.5     # how far the forearm stretches (tar) to reach what's out of reach
# The left wrist's place, cradling (in the chest's frame, as at rest): under the bundle, across the body.
CRADLE = V((0.04, -0.24, 1.2))
CRADLE_POLE = V((-1, 0.2, -0.6))

# ---------------------------------------------------------------- tracks to poses

Z = ((0, 0, 0), (0, 0, 0))
PSEUDO = ("TgtR", "IkR", "PoleR", "StepL", "StepR", "Melt", "EyesUp", "Clutch")


def g(x, y, z):
    """A position (a target, an offset, a pole) as a track value."""
    return ((0, 0, 0), (x, y, z))


def w(v):
    """A number as a track value."""
    return ((v, 0, 0), (0, 0, 0))


def _quat(rot):
    return Euler([math.radians(a) for a in rot]).to_quaternion()


def _deg(q):
    return tuple(math.degrees(a) for a in q.to_euler("XYZ"))


def _rot_of(value):
    return value[0] if isinstance(value[0], (tuple, list)) else value


def _off_of(value):
    return value[1] if isinstance(value[0], (tuple, list)) else (0, 0, 0)


def num(raw, track, default=0.0):
    return raw[track][0][0] if track in raw else default


def at(raw, track, default=(0, 0, 0)):
    return V(raw[track][1]) if track in raw else V(default)


def off_floor(pose, s, side, floor_z):
    """A hand that would go through the floor is lifted: the wrist bends back, then the arm swings up (kept.py's)."""
    hand, arm = "Hand" + side, "Arm" + side
    for i in range(50):
        m = kit.pose_matrices(BONES, pose)
        low = min((m[hand] @ t).z for t in CLAW_TIPS[s])
        if low >= floor_z + 0.012:
            break
        if i < 14:
            rot, off = pose.get(hand, Z)
            pose[hand] = (_deg(_quat((-3, 0, 0)) @ _quat(_rot_of(rot))), tuple(off))
            continue
        shoulder, wrist = m["Chest"] @ V(SH[s]), m["Fore" + side] @ V(WR[s])
        d = (wrist - shoulder).normalized()
        toward = (V((d.x + s * 0.4, d.y - 0.05, 0.0)).normalized() + V((0, 0, 0.6))).normalized()
        axis = d.cross(toward)
        if axis.length < 1e-5:
            break
        turn = Quaternion(axis.normalized(), math.radians(1.5))
        chest = m["Chest"].to_quaternion()
        rot, off = pose.get(arm, Z)
        pose[arm] = (_deg(chest.inverted() @ turn @ chest @ _quat(_rot_of(rot))), tuple(off))
    return pose


MELT = {}        # (Death) the Root's spread (sxy, sz) each frame
CURRENT = [None]


def finish(raw, frame, floor=True):
    """One frame's tracks made a pose: the legs bent onto their feet; the left arm onto the bundle (always: it never
    lets go; Clutch draws it in tighter); the right onto its wrist's place where keyed (blended with its keyed hang),
    the forearm stretched where that's out of reach; the feet flat; the eyes held above the floor as it melts."""
    pose = {k: (tuple(_rot_of(v)), tuple(_off_of(v))) for k, v in raw.items() if k in BONES}
    root = kit.pose_matrices(BONES, pose)["Root"]
    for s, side in ((-1, "L"), (1, "R")):
        foot = root @ (V(ANK[s]) + at(raw, "Step" + side))
        pose = kit.reach(BONES, pose, "Leg" + side, "Shin" + side, lambda _m, t=foot: t, (s * 0.5, -1, 0.1))
    # The left arm cradles the bundle, wherever the chest goes.
    m = kit.pose_matrices(BONES, pose)
    clutch = num(raw, "Clutch")
    cradle = m["Chest"] @ (CRADLE + V((0.03, 0.05, 0.04)) * clutch)
    pole = m["Chest"].to_3x3() @ CRADLE_POLE
    pose = kit.reach(BONES, pose, "ArmL", "ForeL", lambda _m, t=cradle: t, pole)
    weight = max(0.0, min(1.0, num(raw, "IkR")))
    if "TgtR" in raw and weight > 0:
        target = root @ at(raw, "TgtR")
        pole = at(raw, "PoleR", (1, 0.4, -0.1))
        fk = {b: pose.get(b, Z) for b in ("ArmR", "ForeR")}
        ik = kit.reach(BONES, pose, "ArmR", "ForeR", lambda _m, t=target: t, pole)
        for b in fk:
            q = _quat(fk[b][0]).slerp(_quat(_rot_of(ik[b])), weight)
            pose[b] = (_deg(q), tuple(fk[b][1]))
        m = kit.pose_matrices(BONES, pose)
        short = (target - m["Chest"] @ V(SH[1])).length - (L_ARM + L_FORE)
        if short > 0:
            rot, off = pose.get("HandR", Z)
            pose["HandR"] = (rot, tuple(V(off) + FORE_DIR[1] * min(STRETCH_MAX, short) * weight))
    if floor:
        pose = off_floor(pose, 1, "R", min(0.0, root.translation.z))
    m = kit.pose_matrices(BONES, pose)
    for s, side in ((-1, "L"), (1, "R")):
        keyed = _quat(_rot_of(raw.get("Foot" + side, (0, 0, 0))))
        pose["Foot" + side] = (_deg(m["Shin" + side].to_quaternion().inverted() @ keyed), (0, 0, 0))
    if "Melt" in raw:
        sxy, sz = raw["Melt"][0][0], raw["Melt"][0][1]
        MELT.setdefault(CURRENT[0], {})[frame] = (sxy, sz)
        up = num(raw, "EyesUp")
        if up > 0:
            d = V(pose["Root"][1])
            eye = m["Eyes"] @ V(BONES["Eyes"][0])
            true_z = sz * (eye.z - d.z) + d.z
            need = max(0.0, 0.012 - true_z) / max(sz, 1e-3) * up
            rot, off = pose.get("Eyes", Z)
            pose["Eyes"] = (rot, tuple(V(off) + m["Head"].to_quaternion().inverted() @ V((0, 0, need))))
    return pose


# ---------------------------------------------------------------- the drips (swung by their weight: kept.py's)

DRIPS = {"DripVeilL": (8.0, 3.6), "DripVeilR": (8.0, 3.6), "DripBelly": (7.0, 3.6), "DripR": (3.0, 2.8)}
SUB = 6
DT = 1.0 / 30 / SUB


def run(n, periodic, step, start, settle):
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
        ease = min(6, n // 3)
        for i in range(n - ease, n):
            u = (i - (n - 1 - ease)) / ease
            out[i] = settle(out[i], rest, u * u * (3 - 2 * u))
    return [out[i] for i in range(n)]


def drips(frames, periodic, floor=True):
    mats = [kit.pose_matrices(BONES, p) for _, p in frames]
    n = len(frames)
    for bone, (k, c) in DRIPS.items():
        head, tail, parent = BONES[bone]
        length = (V(tail) - V(head)).length
        rest_dir = (V(tail) - V(head)).normalized()

        def carry(i, head=head, tail=tail, parent=parent):
            return mats[i][parent] @ V(head), mats[i][parent] @ V(tail)

        def step(a, b, state, carry=carry, length=length, k=k, c=c):
            pos, vel = state
            ha, ta = carry(a)
            hb, tb = carry(b)
            for s in range(SUB):
                u = (s + 1) / SUB
                h, t = ha.lerp(hb, u), ta.lerp(tb, u)
                acc = (t - pos) * k - vel * c + V((0, 0, -9.81))
                new = pos + (vel + acc * DT) * DT
                d = new - h
                new = h + d.normalized() * length if d.length > 1e-6 else t
                if floor and new.z < 0.015:
                    new.z = 0.015
                vel = (new - pos) / DT
                pos = new
            return pos, vel

        states = run(n, periodic, step, lambda i, carry=carry: (carry(i)[1], V()),
                     lambda s, r, u: (s[0].lerp(r[0], u), s[1]))
        for i, (_, p) in enumerate(frames):
            h = mats[i][parent] @ V(head)
            inward = mats[i][parent].to_quaternion().inverted() @ (states[i][0] - h)
            p[bone] = (_deg(rest_dir.rotation_difference(inward.normalized())), (0.0, 0.0, 0.0))


def clip(name, length, tracks, periodic=False, floor=True):
    """A clip `length` frames long (30 a second; frames 1..length+1) from per-bone tracks, baked to every frame."""
    CURRENT[0] = name
    frames = [(f, finish(raw, f, floor)) for f, raw in kit.bake(tracks, length, periodic)]
    drips(frames, periodic, floor)
    for _, p in frames:
        for b in PSEUDO:
            p.pop(b, None)
    return ("LINEAR", frames)


def held(pose, frame):
    return {track: [(frame, value)] for track, value in pose.items()}


def keys(*parts):
    merged = {}
    for p in parts:
        for track, ks in p.items():
            merged.setdefault(track, {}).update(dict(ks))
    return {track: sorted(ks.items()) for track, ks in merged.items()}


# ---------------------------------------------------------------- poses

# STAND (Idle's frame 1; Walk starts on it, the recoveries and Stagger end on it): as built, the head bowed over the
# bundle, the right arm hanging.
STAND = {"Root": Z, "Hips": Z, "Spine": (0, 0, 0), "Chest": (0, 0, 0), "Head": (14, 0, 0), "Eyes": Z, "Bundle": Z,
         "ArmR": (0, 0, 0), "ForeR": (0, 0, 0), "HandR": (0, 0, 0), "StepL": g(0, 0, 0), "StepR": g(0, 0, 0),
         "Clutch": w(0), "IkR": w(0)}
WRIST_R = V(WR[1])

# RAISED (Windup's end, Grieve's start): reared, turned so the left shoulder (and the bundle) is away from you, the
# right arm high and back, the claws spread back, the bundle clutched in.
RAISED_HIPS = ((-5, 0, 10), (0, 0.04, 0.02))
RAISED_TGT = (0.42, 0.26, 2.0)
# THROUGH (Grieve's end, GrieveRecover's start): bent over after the overhead blow, the arm low ahead.
THROUGH_HIPS = ((8, 0, 0), (0, -0.12, -0.08))
THROUGH_TGT = (0.05, -0.85, 0.3)
# LOW (Crouch's end, Lunge's start): sunk low, leaning forward, the arm drawn back low.
LOW_HIPS = ((14, 0, 6), (0, 0.08, -0.28))
LOW_TGT = (0.42, 0.45, 0.85)
# SPRAWLED (Lunge's end, LungeRecover's start): thrown forward, the arm out ahead low.
SPRAWL_HIPS = ((18, 0, -4), (0, -0.22, -0.22))
SPRAWL_TGT = (0.05, -1.05, 0.45)

ACTIONS = {}

# Idle (2 s loop): rocking the bundle, slowly, side to side (the hips lead, the chest after, the head bowed over it,
# after the chest); a hitch in it once, as if a sob; the right arm hanging, swaying after the body.
ACTIONS["Idle"] = clip("Idle", 60, keys(held(STAND, 1), {
    "Hips": [(15, ((0, 0, 4), (0.025, 0, -0.005))), (30, ((0, 0, 0), (0, 0, 0))), (45, ((0, 0, -4), (-0.025, 0, -0.005)))],
    "Spine": [(17, (1, -2, 3)), (32, (0, 0, 0)), (47, (1, 2, -3))],
    "Chest": [(19, (2, -3, 4)), (34, (0, 0, 0)), (40, (4, 0, 0)), (42, (1, 0, 0)), (49, (2, 3, -4))],
    "Head": [(1, (14, 0, 0)), (22, (16, -4, 3)), (37, (14, 0, 0)), (41, (20, 0, 0)), (43, (15, 0, 0)), (52, (16, 4, -3))],
    "Clutch": [(1, w(0)), (38, w(0)), (41, w(0.6)), (46, w(0))],
    "ArmR": [(18, (2, 3, 0)), (33, (-1, 0, 0)), (48, (2, -3, 0))],
    "ForeR": [(21, (3, 2, 0)), (36, (-2, 0, 0)), (51, (3, -2, 0))],
    "HandR": [(24, (5, 0, 0)), (39, (-2, 0, 0)), (54, (5, 0, 0))],
    "Bundle": [(1, Z)],
}), periodic=True)


# Walk (1 s loop, the passing pose at frame 1): slow and long, the body hardly rising. Each foot is planted half the
# loop (15 frames), going back under it 0.7 m at an even pace (1.4 m/s: keyed every few frames, so it doesn't slide),
# then swung forward, lifted; the right planted under it at frame 1 (its middle), the left half a loop after. The hips
# sway onto each; the right arm swings after, the bundle held still.
ACTIONS["Walk"] = clip("Walk", 30, keys(held(STAND, 1), {
    "StepR": [(1, g(0, 0.0, 0)), (4.5, g(0, 0.163, 0)), (8, g(0, 0.35, 0)), (11, g(0, 0.25, 0.09)), (16, g(0, -0.05, 0.11)),
              (21, g(0, -0.3, 0.03)), (23, g(0, -0.35, 0)), (27, g(0, -0.175, 0))],
    "StepL": [(1, g(0, -0.05, 0.11)), (6, g(0, -0.3, 0.03)), (8, g(0, -0.35, 0)), (12, g(0, -0.175, 0)), (16, g(0, 0.0, 0)),
              (19.5, g(0, 0.163, 0)), (23, g(0, 0.35, 0)), (26, g(0, 0.25, 0.09))],
    "Hips": [(1, ((0, 2, 0), (0, 0, 0.01))), (8, ((0, -3, -2), (-0.02, 0, -0.02))), (16, ((0, -2, 0), (0, 0, 0.01))),
             (23, ((0, 3, 2), (0.02, 0, -0.02)))],
    "Chest": [(3, (2, 2, -2)), (11, (3, -3, 3)), (18, (2, -2, 2)), (26, (3, 3, -3))],
    "Head": [(5, (14, 0, 1)), (13, (15, -2, -2)), (20, (14, 0, -1)), (28, (15, 2, 2))],
    "ArmR": [(1, (-6, 0, 0)), (16, (10, 0, 0))],
    "ForeR": [(4, (-4, 0, 0)), (19, (6, 0, 0))],
    "HandR": [(6, (-5, 0, 0)), (21, (8, 0, 0))],
}), periodic=True)

# Windup (0.8 s): it clutches the bundle in, turns its left shoulder (and the bundle) from you, a dip, then rears and
# lifts the right arm high and back, slowly, the claws spread back; the head lifts to you; trembling at the top.
ACTIONS["Windup"] = clip("Windup", 24, keys(held(STAND, 1), {
    "Clutch": [(1, w(0)), (8, w(1)), (25, w(1))],
    "Hips": [(5, ((3, 0, 2), (0, -0.01, -0.03))), (15, ((-3, 0, 7), (0, 0.03, 0.01))), (22, ((-5, 0, 10), (0, 0.04, 0.02))),
             (25, RAISED_HIPS)],
    "Spine": [(6, (5, 0, 3)), (16, (-6, 0, 8)), (25, (-8, 0, 10))],
    "Chest": [(7, (4, 0, 4)), (17, (-8, -4, 12)), (25, (-10, -5, 14))],
    "Head": [(9, (16, 0, 0)), (18, (-2, 6, -10)), (23, (-6, 8, -14)), (25, (-5, 8, -13))],
    "TgtR": [(1, g(*WRIST_R)), (8, g(0.34, -0.2, 0.9)), (15, g(0.44, -0.05, 1.6)), (21, g(0.43, 0.24, 1.98)),
             (23, g(0.425, 0.25, 1.97)), (25, g(*RAISED_TGT))],
    "IkR": [(1, w(0)), (9, w(1)), (25, w(1))],
    "PoleR": [(1, g(1, 0.4, -0.1)), (15, g(1, 0.3, 0.4)), (25, g(0.8, 0.4, 0.6))],
    "HandR": [(10, (-8, 0, 0)), (19, (-32, 0, 0)), (21, (-37, 2, 0)), (22, (-32, -2, 0)), (23, (-37, 2, 0)), (25, (-35, 0, 0))],
}))

# Grieve (1.6 s): three blows, the claws stretched out (tar) to reach, a step into each (the game shoves it on too):
# across from its right to its left at chest height (f8); wound round to its left, then back across (f22); then up
# high and down from overhead in front, the body folding after it, the heaviest (f38). Ends bent over, the arm low.
ACTIONS["Grieve"] = clip("Grieve", 47, keys(held(STAND, 1), {
    "Clutch": [(1, w(1)), (48, w(1))],
    "Hips": [(1, RAISED_HIPS), (5, ((2, 0, 2), (0, -0.05, 0.0))), (8, ((5, 0, -8), (0, -0.12, -0.02))),
             (13, ((3, 0, -14), (0, -0.1, -0.02))), (18, ((2, 0, -6), (0, -0.08, 0.0))), (22, ((5, 0, 10), (0, -0.14, -0.02))),
             (27, ((-4, 0, 6), (0, -0.06, 0.03))), (33, ((-8, 0, 2), (0, -0.04, 0.05))), (38, ((10, 0, 0), (0, -0.18, -0.08))),
             (44, THROUGH_HIPS), (48, THROUGH_HIPS)],
    "Spine": [(1, (-8, 0, 10)), (5, (0, 0, 4)), (8, (6, 0, -10)), (13, (4, 0, -18)), (18, (2, 0, -8)), (22, (6, 0, 12)),
              (27, (-4, 0, 8)), (33, (-10, 0, 2)), (38, (16, 0, 0)), (44, (14, 0, 0)), (48, (14, 0, 0))],
    "Chest": [(1, (-10, -5, 14)), (4, (-4, -2, 8)), (8, (8, 3, -14)), (13, (6, 4, -22)), (18, (2, 2, -10)), (22, (8, -3, 16)),
              (27, (-6, -2, 10)), (33, (-14, 0, 2)), (38, (22, 0, 0)), (44, (20, 0, 0)), (48, (20, 0, 0))],
    "Head": [(1, (-5, 8, -13)), (5, (-4, 6, -10)), (10, (2, -2, 6)), (15, (4, -6, 12)), (20, (0, 0, 0)), (24, (4, 6, -12)),
             (30, (-8, 2, -6)), (35, (-12, 0, 0)), (40, (10, 0, 0)), (46, (18, 0, 0))],
    "TgtR": [(1, g(*RAISED_TGT)), (4, g(0.6, -0.35, 1.7)), (8, g(0.0, -1.1, 1.25)), (11, g(-0.55, -0.65, 0.95)),
             (14, g(-0.7, -0.35, 1.3)), (18, g(-0.55, -0.75, 1.4)), (22, g(0.1, -1.15, 1.2)), (25, g(0.6, -0.55, 1.05)),
             (29, g(0.3, 0.05, 2.0)), (33, g(0.22, -0.25, 2.15)), (38, g(0.05, -1.1, 0.55)), (42, g(0.02, -0.95, 0.3)),
             (48, g(*THROUGH_TGT))],
    "IkR": [(1, w(1)), (48, w(1))],
    "PoleR": [(1, g(0.8, 0.4, 0.6)), (8, g(0.3, 0.2, 1)), (14, g(0.2, 0.5, 1)), (22, g(1, 0.2, 0.4)), (30, g(1, 0.5, 0.3)),
              (38, g(1, 0.3, 0.6)), (48, g(1, 0.4, 0.2))],
    "HandR": [(1, (-35, 0, 0)), (5, (-12, 0, 0)), (8, (6, 0, -8)), (12, (12, 0, 0)), (17, (-10, 0, 0)), (22, (6, 0, 8)),
              (26, (10, 0, 0)), (32, (-30, 0, 0)), (38, (16, 0, 0)), (48, (20, 0, 0))],
    "StepL": [(1, g(0, 0, 0)), (4, g(0, -0.08, 0.05)), (8, g(-0.02, -0.22, 0)), (48, g(-0.02, -0.22, 0))],
    "StepR": [(1, g(0, 0, 0)), (17, g(0, 0, 0)), (19, g(0.02, -0.1, 0.05)), (22, g(0.03, -0.24, 0)), (35, g(0.03, -0.24, 0)),
              (37, g(0.02, -0.3, 0.04)), (38, g(0.02, -0.36, 0)), (48, g(0.02, -0.36, 0))],
}))

# GrieveRecover (0.9 s): spent: it sags lower, drags the arm back along the floor to its side, shuffles its feet in,
# and straightens to bow over the bundle again (the opening to strike it).
ACTIONS["GrieveRecover"] = clip("GrieveRecover", 27, keys(held(STAND, 28), {
    "Clutch": [(1, w(1)), (20, w(0.3)), (28, w(0))],
    "Hips": [(1, THROUGH_HIPS), (6, ((12, 0, 0), (0, -0.14, -0.12))), (14, ((6, 0, 0), (0, -0.06, -0.05))), (22, ((1, 0, 0), (0, -0.01, 0.0)))],
    "Spine": [(1, (14, 0, 0)), (7, (18, 0, 2)), (15, (8, 0, 0)), (23, (1, 0, 0))],
    "Chest": [(1, (20, 0, 0)), (8, (24, 0, 2)), (16, (10, 0, 0)), (24, (1, 0, 0))],
    "Head": [(1, (18, 0, 0)), (9, (26, 0, 0)), (18, (20, 0, 0)), (26, (14, 0, 0))],
    "TgtR": [(1, g(*THROUGH_TGT)), (8, g(0.15, -0.6, 0.15)), (16, g(0.28, -0.3, 0.3)), (24, g(*WRIST_R))],
    "IkR": [(1, w(1)), (16, w(1)), (26, w(0))],
    "PoleR": [(1, g(1, 0.4, 0.2)), (26, g(1, 0.4, -0.1))],
    "StepL": [(1, g(-0.02, -0.22, 0)), (10, g(-0.02, -0.22, 0)), (13, g(-0.01, -0.12, 0.04)), (16, g(0, 0, 0))],
    "StepR": [(1, g(0.02, -0.36, 0)), (16, g(0.02, -0.36, 0)), (19, g(0.01, -0.18, 0.04)), (23, g(0, 0, 0))],
}))

# Crouch (0.9 s): it sinks, lower and lower, leaning forward, the right arm drawn back low behind it; the head comes up
# to you; it quivers, coiled (Lunge's frame 1).
ACTIONS["Crouch"] = clip("Crouch", 27, keys(held(STAND, 1), {
    "Clutch": [(1, w(0)), (8, w(1)), (28, w(1))],
    "Hips": [(6, ((4, 0, 2), (0, 0.02, -0.08))), (16, ((10, 0, 5), (0, 0.06, -0.2))), (24, ((13, 0, 6), (0, 0.075, -0.27))),
             (28, LOW_HIPS)],
    "Spine": [(7, (6, 0, 2)), (18, (14, 0, 4)), (28, (18, 0, 4))],
    "Chest": [(8, (8, 0, 3)), (19, (16, 0, 5)), (28, (20, 0, 6))],
    "Head": [(10, (8, 0, 0)), (20, (-16, 0, -4)), (28, (-22, 0, -5))],
    "TgtR": [(1, g(*WRIST_R)), (10, g(0.36, 0.15, 0.7)), (20, g(0.42, 0.42, 0.82)), (24, g(0.425, 0.45, 0.85)),
             (25, g(0.415, 0.44, 0.86)), (26, g(0.425, 0.46, 0.84)), (28, g(*LOW_TGT))],
    "IkR": [(1, w(0)), (10, w(1)), (28, w(1))],
    "PoleR": [(1, g(1, 0.4, -0.1)), (28, g(1, -0.2, 0.3))],
    "StepL": [(1, g(0, 0, 0)), (8, g(0, -0.05, 0.04)), (11, g(-0.02, -0.12, 0)), (28, g(-0.02, -0.12, 0))],
    "StepR": [(1, g(0, 0, 0)), (28, g(0, 0.08, 0))],
    "HandR": [(14, (-20, 0, 0)), (28, (-30, 0, 0))],
}))

# Lunge (0.4 s; the blow at f5): it springs, low and long, the claws thrust out ahead and up at you, the body
# stretched after them, the head down behind the arm; it comes down sprawled forward.
ACTIONS["Lunge"] = clip("Lunge", 11, {
    "Root": [(1, Z)],
    "Clutch": [(1, w(1)), (12, w(1))],
    "Hips": [(1, LOW_HIPS), (3, ((20, 0, 2), (0, -0.1, -0.16))), (5, ((22, 0, -2), (0, -0.25, -0.14))), (8, ((20, 0, -4), (0, -0.25, -0.2))),
             (12, SPRAWL_HIPS)],
    "Spine": [(1, (18, 0, 4)), (5, (22, 0, -2)), (12, (20, 0, -2))],
    "Chest": [(1, (20, 0, 6)), (4, (18, 0, 0)), (6, (14, 0, -6)), (12, (18, 0, -4))],
    "Head": [(1, (-22, 0, -5)), (5, (-10, 0, 0)), (12, (6, 0, 0))],
    "Eyes": [(1, Z)], "Bundle": [(1, Z)], "ArmR": [(1, (0, 0, 0))], "ForeR": [(1, (0, 0, 0))],
    "TgtR": [(1, g(*LOW_TGT)), (3, g(0.3, -0.4, 1.0)), (5, g(0.12, -1.3, 1.05)), (8, g(0.08, -1.2, 0.75)), (12, g(*SPRAWL_TGT))],
    "IkR": [(1, w(1))],
    "PoleR": [(1, g(1, -0.2, 0.3)), (5, g(1, 0.3, 0.2)), (12, g(1, 0.4, 0.3))],
    "HandR": [(1, (-30, 0, 0)), (5, (10, 0, 0)), (12, (14, 0, 0))],
    "StepL": [(1, g(-0.02, -0.12, 0)), (3, g(-0.02, -0.3, 0.08)), (6, g(-0.03, -0.5, 0)), (12, g(-0.03, -0.5, 0))],
    "StepR": [(1, g(0, 0.08, 0)), (12, g(0, 0.08, 0))],
})

# LungeRecover (0.8 s): sprawled forward, it draws the arm back and gathers itself up, the back foot stepping in, the
# body straightening last; bowed over the bundle.
ACTIONS["LungeRecover"] = clip("LungeRecover", 24, keys(held(STAND, 25), {
    "Clutch": [(1, w(1)), (18, w(0.4)), (25, w(0))],
    "Hips": [(1, SPRAWL_HIPS), (8, ((14, 0, -2), (0, -0.18, -0.16))), (16, ((5, 0, 0), (0, -0.06, -0.05))), (21, ((1, 0, 0), (0, 0, 0.0)))],
    "Spine": [(1, (20, 0, -2)), (10, (14, 0, 0)), (18, (4, 0, 0)), (23, (0, 0, 0))],
    "Chest": [(1, (18, 0, -4)), (11, (14, 0, 0)), (19, (4, 0, 0)), (24, (0, 0, 0))],
    "Head": [(1, (6, 0, 0)), (12, (10, 0, 0)), (22, (14, 0, 0))],
    "TgtR": [(1, g(*SPRAWL_TGT)), (9, g(0.2, -0.6, 0.4)), (17, g(0.28, -0.25, 0.55)), (23, g(*WRIST_R))],
    "IkR": [(1, w(1)), (16, w(1)), (23, w(0))],
    "PoleR": [(1, g(1, 0.4, 0.3)), (23, g(1, 0.4, -0.1))],
    "StepL": [(1, g(-0.03, -0.5, 0)), (12, g(-0.03, -0.5, 0)), (15, g(-0.02, -0.25, 0.05)), (19, g(0, 0, 0))],
    "StepR": [(1, g(0, 0.08, 0)), (6, g(0, 0.08, 0)), (9, g(0, 0.02, 0.04)), (13, g(0, 0, 0))],
}))

# Stagger (0.67 s): struck: jolted back (the hips first), the head snapping back a frame late, the arm flung up, the
# bundle clutched in; a stumble back and it steps in again, bowed.
ACTIONS["Stagger"] = clip("Stagger", 20, keys(held(STAND, 1), held(STAND, 21), {
    "Clutch": [(1, w(0)), (3, w(1)), (16, w(1)), (21, w(0))],
    "Hips": [(3, ((-7, 0, 0), (0, 0.1, 0.0))), (6, ((-9, 3, 0), (0.01, 0.15, -0.02))), (10, ((-2, -3, 0), (0, 0.1, -0.03))),
             (14, ((2, 1, 0), (0, 0.04, -0.01)))],
    "Spine": [(3, (-10, 0, 0)), (7, (-12, 3, 0)), (11, (3, -2, 0)), (15, (2, 0, 0))],
    "Chest": [(3, (-12, 0, 0)), (5, (-16, 3, 3)), (10, (5, -3, 0)), (15, (2, 1, 0))],
    "Head": [(2, (14, 0, 0)), (4, (-24, 0, 0)), (5, (-28, 5, 5)), (8, (-4, -5, -6)), (12, (20, 3, 3)), (16, (13, 0, 0))],
    "ArmR": [(4, (-50, -30, 0)), (6, (-66, -40, 0)), (10, (-24, -16, 0)), (14, (4, 0, 0))],
    "ForeR": [(5, (-18, 0, 0)), (8, (-36, -4, 0)), (12, (-8, 0, 0)), (16, (6, 0, 0))],
    "HandR": [(6, (-30, 0, 0)), (10, (-38, 0, 0)), (15, (8, 0, 0))],
    "StepR": [(3, g(0, 0, 0)), (5, g(0.01, 0.08, 0.06)), (8, g(0.02, 0.16, 0)), (15, g(0.02, 0.16, 0)), (18, g(0.01, 0.06, 0.04))],
    "StepL": [(9, g(0, 0, 0)), (11, g(-0.01, 0.07, 0.05)), (13, g(-0.01, 0.12, 0)), (16, g(-0.01, 0.12, 0)), (19, g(0, 0.04, 0.03))],
}))

# Death (1.5 s): a jolt back; it sinks to its knees, folds forward over the bundle (it holds it to the last), the right
# arm falling to the floor; then it melts into the stone (as the Kept), spreading flat, the eyes last, then nothing.
ACTIONS["Death"] = clip("Death", 45, keys(held(STAND, 1), {
    "Clutch": [(1, w(0)), (6, w(1)), (46, w(1))],
    "Root": [(22, Z), (30, ((0, 0, 0), (0, 0, -0.15))), (38, ((0, 0, 0), (0, 0, -0.8))), (46, ((0, 0, 0), (0, 0, -2.0)))],
    "Melt": [(1, ((1, 1, 0), (0, 0, 0))), (22, ((1, 1, 0), (0, 0, 0))), (31, ((1.5, 0.7, 0), (0, 0, 0))), (39, ((2.1, 0.4, 0), (0, 0, 0))),
             (46, ((2.5, 0.2, 0), (0, 0, 0)))],
    "EyesUp": [(1, w(0)), (27, w(0)), (31, w(1)), (41, w(1)), (45, w(0))],
    "Hips": [(4, ((-4, 0, 0), (0, 0.05, 0.0))), (10, ((4, 0, 0), (0, 0.05, -0.2))), (15, ((10, 0, 0), (0, 0.08, -0.4))),
             (18, ((8, 0, 0), (0, 0.08, -0.38))), (22, ((12, 0, 0), (0, 0.08, -0.42)))],
    "Spine": [(4, (-6, 0, 0)), (10, (4, 0, 0)), (16, (18, 0, 0)), (22, (30, 0, 0)), (30, (36, 0, 0))],
    "Chest": [(5, (-8, 0, 0)), (11, (2, 0, 0)), (17, (20, 0, 0)), (23, (28, 0, 0)), (31, (34, 0, 0))],
    "Head": [(4, (-12, 0, 0)), (8, (-16, 4, 0)), (14, (10, 0, 0)), (20, (30, -4, 0)), (28, (40, -8, 4))],
    "ArmR": [(6, (-5, -5, 0)), (14, (6, -4, 0)), (21, (-24, -18, 0)), (29, (-28, -34, 0)), (37, (-24, -48, 0))],
    "ForeR": [(7, (-10, 0, 0)), (16, (8, 0, 0)), (25, (-18, -6, 0)), (35, (-10, -10, 0))],
    "HandR": [(10, (10, 0, 0)), (25, (-30, 0, 0)), (35, (-40, 0, 0))],
    "StepL": [(8, g(0, 0, 0)), (13, g(0, 0.16, 0.02)), (16, g(0, 0.24, -0.015)), (46, g(0, 0.24, -0.015))],
    "StepR": [(9, g(0, 0, 0)), (14, g(0, 0.16, 0.02)), (17, g(0, 0.22, -0.015)), (46, g(0, 0.22, -0.015))],
}), floor=False)


def melt_keys(armature):
    """kept.py's: the Root's scale keyed on every clip, 1 but in Death (spread and flattened as it melts)."""
    root = armature.pose.bones["Root"]
    axes = armature.data.bones["Root"].matrix_local.to_3x3()
    for action in bpy.data.actions:
        armature.animation_data.action = action
        start, end = (int(f) for f in action.frame_range)
        table = MELT.get(action.name, {})
        for f in range(start, end + 1):
            sxy, sz = table.get(f, (1.0, 1.0))
            world = Vector((sxy, sxy, sz))
            root.scale = Vector([sum(world[j] * axes[j][i] ** 2 for j in range(3)) for i in range(3)])
            root.keyframe_insert("scale", frame=f)
    armature.animation_data.action = bpy.data.actions["Idle"]


# ---------------------------------------------------------------- the model

def weigh(ob, fn):
    for v in ob.data.vertices:
        for bone, wt in fn(v.co).items():
            if wt <= 0:
                continue
            group = ob.vertex_groups.get(bone) or ob.vertex_groups.new(name=bone)
            group.add([v.index], wt, "REPLACE")
    return ob


# The body in one soft piece, thin: rings up its spine from the pelvis to the neck (centre, half-width, half-depth).
TORSO = [((0, 0.03, 0.86), 0.04, 0.04), ((0, 0.03, 0.9), 0.12, 0.09), ((0, 0.03, 0.98), 0.14, 0.1),
         ((0, 0.025, 1.08), 0.125, 0.095), ((0, 0.02, 1.18), 0.12, 0.09), ((0, 0.0, 1.28), 0.14, 0.1),
         ((0, -0.02, 1.38), 0.17, 0.11), ((0, -0.04, 1.47), 0.19, 0.11), ((0, -0.06, 1.53), 0.14, 0.09),
         ((0, -0.08, 1.57), 0.07, 0.06), ((0, -0.09, 1.59), 0.03, 0.03)]
# Its long tattered skirt of tar from the hips to the shins (weighted to the hips: it sways with them).
SKIRT = [((0, 0.03, 0.97), 0.15, 0.11), ((0, 0.03, 0.82), 0.19, 0.14), ((0, 0.04, 0.62), 0.23, 0.17),
         ((0, 0.05, 0.42), 0.26, 0.2), ((0, 0.06, 0.28), 0.28, 0.22)]


def ring_tube(rings, seg=16, cap=True):
    def build_fn(bm):
        pts = [V(c) for c, _, _ in rings]
        loops = []
        for i, (c, rx, ry) in enumerate(rings):
            d = (pts[min(i + 1, len(pts) - 1)] - pts[max(i - 1, 0)]).normalized()
            q = d.to_track_quat("Z", "Y")
            loops.append([bm.verts.new(V(c) + q @ V((math.cos(a) * rx, math.sin(a) * ry, 0.0)))
                          for a in (2 * math.pi * k / seg for k in range(seg))])
        for a, b in zip(loops, loops[1:]):
            for k in range(seg):
                bm.faces.new((a[k], a[(k + 1) % seg], b[(k + 1) % seg], b[k]))
        if cap:
            bm.faces.new(list(reversed(loops[0])))
            bm.faces.new(loops[-1])
        bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    return build_fn


def build():
    body, edge = kit.mat("MournerBody", "#0e0d12"), kit.mat("MournerBodyEdge", "#352f42")
    eye, claw = kit.mat("MournerEye_Glow", "#d9dccf"), kit.mat("MournerClaw", "#4a4850")
    palm = kit.mat("MournerPalm", "#0e0d12")   # (the hand: a swipe trail's inner edge)
    # (what it carries glows, faintly, pale: in the dark it's what you see of the Mourner, and its tells read by it)
    wrap, wrap_dark = kit.mat("MournerWrap_Glow", "#7d8590"), kit.mat("MournerWrapDark", "#4d4946")

    def lumpy(name, build_fn, matrix, group, jitter=0.006, sheen=None):
        ob = part(name, build_fn, body, matrix, smooth=True, jitter=jitter, group=group)
        if sheen is not None:
            kit.top_material(ob, edge, sheen)
        return ob

    torso = part("Torso", ring_tube(TORSO), body, None, smooth=True, jitter=0.005)
    kit.top_material(torso, edge, 0.62)

    def by_height(co):
        def ramp(z, a, b):
            return max(0.0, min(1.0, (z - a) / (b - a)))
        chest = ramp(co.z, 1.22, 1.32)
        spine = ramp(co.z, 0.96, 1.06) * (1 - chest)
        return {"Hips": 1 - spine - chest, "Spine": spine, "Chest": chest}
    parts = [weigh(torso, by_height)]
    skirt = part("Skirt", ring_tube(SKIRT, seg=18, cap=False), body, None, smooth=True, jitter=0.012)
    kit.top_material(skirt, edge, 0.7)
    parts.append(weigh(skirt, lambda co: {"Hips": 1.0}))
    parts += [
        lumpy("Head", kit.sphere(16, 12), M(HEAD_C, rot=(-10, 0, 0), scale=HEAD_R), "Head", jitter=0.004, sheen=0.6),
        # The veil of tar over its face: a long hood hanging from the crown down past the chin, open at the face.
        lumpy("Veil", kit.cone(14, 0.15, 0.07, 0.38), M((0, -0.11, 1.66), rot=(-14, 0, 0)), "Head", jitter=0.01, sheen=0.7),
    ]
    for s in (-1, 1):
        parts.append(part("Eye", kit.sphere(10, 7), eye, M(EYE[s], rot=(0, 0, s * -12), scale=(0.019, 0.012, 0.017)),
                          smooth=True, group="Eyes"))
    # What it carries: something small, wrapped in grey, a fold of the wrap over its top; weighted to its own bone.
    parts.append(part("Wrap", kit.sphere(12, 9), wrap, M((-0.03, -0.22, 1.3), rot=(0, 18, 0), scale=(0.09, 0.08, 0.15)),
                      smooth=True, jitter=0.006, group="Bundle"))
    parts.append(part("WrapFold", kit.sphere(10, 7), wrap_dark, M((-0.04, -0.25, 1.4), rot=(20, 10, 0), scale=(0.07, 0.05, 0.05)),
                      smooth=True, group="Bundle"))
    for s, side in ((-1, "L"), (1, "R")):
        parts.append(lumpy("Shoulder", kit.sphere(10, 7), M(SH[s], scale=(0.07, 0.07, 0.065)), "Chest", sheen=0.62))
        parts.append(lumpy("UpperArm", kit.tube([SH[s], EL[s]], [0.045, 0.032], seg=8), None, "Arm" + side, jitter=0.002))
        parts.append(lumpy("Elbow", kit.sphere(8, 6, 0.032), M(EL[s]), "Arm" + side, jitter=0.002))
        el, wr = V(EL[s]), V(WR[s])
        fore = part("Forearm", kit.tube([EL[s], WR[s]], [0.031, 0.022], seg=8), body, None, smooth=True)

        def fore_w(co, el=el, wr=wr, side=side):
            t = max(0.0, min(1.0, (co - el).dot(wr - el) / (wr - el).length_squared))
            t = max(0.0, (t - 0.25) / 0.75)
            return {"Fore" + side: 1 - t, "Hand" + side: t}
        parts.append(weigh(fore, fore_w))
        parts.append(part("Palm", kit.sphere(8, 6), palm, M((WR[s][0], WR[s][1] - 0.012, 0.69), scale=(0.035, 0.026, 0.055)),
                          smooth=True, group="Hand" + side))
        for fan in FAN:
            pts, tip = finger(s, fan)
            parts.append(part("Finger", kit.tube(pts, [0.012, 0.009, 0.007], seg=6), body, None, smooth=True, group="Hand" + side))
            parts.append(part("Claw", kit.cone(5, 0.009, 0.0, 0.06), claw, kit.along(pts[-1], tip), group="Hand" + side))
        # Legs: long and thin under the skirt; small soft feet.
        parts.append(lumpy("Thigh", kit.tube([HIP[s], KNEE[s]], [0.055, 0.04], seg=8), None, "Leg" + side, jitter=0.003))
        parts.append(lumpy("Knee", kit.sphere(8, 6, 0.04), M(KNEE[s]), "Leg" + side, jitter=0.002))
        parts.append(lumpy("Shin", kit.tube([KNEE[s], ANK[s]], [0.04, 0.03], seg=8), None, "Shin" + side, jitter=0.002))
        parts.append(lumpy("Foot", kit.sphere(10, 6), M((s * 0.11, -0.05, 0.035), scale=(0.045, 0.085, 0.032)), "Foot" + side,
                           jitter=0.002))
    for name, (head, tail, _) in DRIP.items():
        h, t = V(head), V(tail)
        mid = h.lerp(t, 0.55)
        parts.append(part(name, kit.tube([h, mid, t], [0.018, 0.008, 0.008], seg=6), body, None, smooth=True, group=name))
        parts.append(part(name + "Drop", kit.sphere(8, 6), body, M(t - V((0, 0, 0.008)), scale=(0.017, 0.017, 0.024)),
                          smooth=True, group=name))

    mesh = kit.join(parts, "Mourner")
    armature = kit.rig("MournerRig", BONES)
    kit.skin(mesh, armature)
    kit.animate(armature, ACTIONS)
    melt_keys(armature)
    return [armature, mesh]
