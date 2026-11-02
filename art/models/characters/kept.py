"""The Kept: the only creature of the Hold (the game's opening, its tutorial): what the cave's living walls let go of,
a soul kept so long it's worn down to nothing. Like Dark Souls' Humanity: a small hunched figure, child-sized (1.12 m
to the top of its head, hunched), black and soft like wet tar, two pale eyes set close, long thin arms whose claws all
but drag on the floor, short bowed legs. It walks bent over, unsteady, like something that forgot how. It tears free of
the wall as you pass, comes after you in twos and threes; one blow, a clawing swipe of the long right arm; beaten, it
sinks back into the stone. A few strands of tar hang from its chin, its belly and its elbows.

Clips (30 fps; the game's code is written against these names and lengths):
  Idle (loop, 2 s): hunched, swaying unsteadily, the head lolling, a twitch, a blink, the arms swinging after the body;
    a shudder.
  Walk (loop, 1.2 s): a lurching shuffle, out of time: the right foot scuffs back along the floor and is flung forward,
    the left lifts and comes down heavy, stumbling; the left arm drags its knuckles. Frame 1 is Idle's frame 1 (its
    passing pose). Its feet are planted for about 0.42 m/s.
  Emerge (1.6 s): tearing free of the wall behind it (the game puts its back to the wall and moves it 0.9 m forward
    over the clip, evenly: anything planted here is planted for that): pressed back into the stone, arms spread into it,
    head tipped back, eyes shut; the eyes open, the head lifts, the right arm pulls out of the stone and reaches, the
    body strains with the tar stretched back to the wall; it tears (the head's strand first), lurches forward onto all
    fours, and rises into its hunch (Idle's frame 1).
  Windup (0.9 s): the tell: a dip, then it rears up and raises the right arm slowly, high and back, the claws spread
    back, the head cocked; trembling at the top (Swipe's frame 1).
  Swipe (0.35 s): down and across from its right to its left, in front, lurching after it, the arm stretching (tar):
    the claws 1.5 m out at chest height at the blow (frame 5.5); the follow-through low to its left (Recover's frame 1).
  Recover (0.77 s): carried past, it sways, drags the arm back along the floor and settles into its hunch.
  Stagger (0.67 s): jolted back, the head snapping, arms flung up, a stumble back and a step in again; the hunch.
  Death (1.5 s): it buckles to its knees and slumps; then it melts into the floor (the one clip below it: the Root
    sinks to -1.2 m and spreads flat as it goes), the eyes last, then nothing.

How it's animated (the Laden's way): each bone keyed on its own timing (kit.bake: tracks); the body leads (hips, spine,
chest), the head follows late. The legs aren't keyed: the feet are (StepL / StepR: offsets from where they stand) and
the legs bent onto them every frame (kit.reach), so they stay on the floor. The arms hang from keyed rotations, or are
bent onto a keyed wrist place (TgtL / TgtR, blended in by IkL / IkR) where they must be somewhere: in the wall, on the
floor, in the swipe; out of reach, the forearm stretches like tar (the hand moved down it). The drips aren't keyed:
they swing by their weight (a little simulation). The tar strands (TarHead, TarBack, TarL, TarR) are hidden inside it
except in Emerge, where they're stretched back to the wall (TarXW: how far, 1 = at the wall) and snap back.
"""
import math

import bmesh
import bpy
from mathutils import Euler, Matrix, Quaternion, Vector

M, part = kit.M, kit.part  # `kit` is injected by build_model.py
V = Vector

# ---------------------------------------------------------------- the rig

SH = {-1: (-0.17, -0.16, 0.83), 1: (0.17, -0.16, 0.83)}     # shoulders
EL = {-1: (-0.21, -0.2, 0.5), 1: (0.21, -0.2, 0.5)}          # elbows
WR = {-1: (-0.215, -0.23, 0.17), 1: (0.215, -0.23, 0.17)}      # wrists
KN = {-1: (-0.215, -0.25, 0.1), 1: (0.215, -0.25, 0.1)}        # the hand bone's tail (the knuckles)
HIP = {-1: (-0.085, 0.02, 0.42), 1: (0.085, 0.02, 0.42)}
KNEE = {-1: (-0.15, -0.04, 0.23), 1: (0.15, -0.04, 0.23)}
ANK = {-1: (-0.11, 0.0, 0.055), 1: (0.11, 0.0, 0.055)}
FAN = (-1, 0, 1)


def finger(s, fan):
    """A finger's points (knuckle, joint, tip) and its claw's point, at rest."""
    x0, y0 = WR[s][0] + fan * 0.018, WR[s][1]
    pts = [(x0, y0 - 0.015, 0.11), (x0 + fan * 0.02, y0 - 0.035, 0.065), (x0 + fan * 0.032, y0 - 0.065, 0.035)]
    return pts, (x0 + fan * 0.04, y0 - 0.095, 0.022)


CLAW_TIPS = {s: [V(finger(s, f)[1]) for f in FAN] + [V(finger(s, f)[0][-1]) - V((0, 0, 0.007)) for f in FAN]
             for s in (-1, 1)}
TOE = {-1: (-0.115, -0.1, 0.03), 1: (0.115, -0.1, 0.03)}
HEAD_C, HEAD_R = (0, -0.29, 0.96), (0.16, 0.155, 0.15)
EYE = {-1: (-0.045, -0.432, 0.975), 1: (0.045, -0.432, 0.975)}
# The tar strands' roots (inside it) and the drips' (head, tail).
TAR = {"TarHead": ((0, -0.18, 0.99), "Head"), "TarBack": ((0, 0.07, 0.76), "Chest"),
       "TarL": ((-0.215, -0.24, 0.135), "HandL"), "TarR": ((0.215, -0.24, 0.135), "HandR")}
DRIP = {"DripChin": ((0, -0.38, 0.85), (0, -0.385, 0.7), "Head"),
        "DripBelly": ((0.02, -0.09, 0.5), (0.025, -0.09, 0.36), "Spine"),
        "DripL": ((-0.245, -0.19, 0.53), (-0.26, -0.19, 0.39), "ArmL"),
        "DripR": ((0.245, -0.19, 0.53), (0.26, -0.19, 0.38), "ArmR")}

BONES = {
    "Root": ((0, 0, 0), (0, 0, 0.2), None),
    "Hips": ((0, 0.02, 0.4), (0, 0.01, 0.52), "Root"),
    "Spine": ((0, 0.04, 0.46), (0, -0.01, 0.7), "Hips"),
    "Chest": ((0, -0.01, 0.7), (0, -0.17, 0.86), "Spine"),     # the hunch: shoulders, the hump
    "Head": ((0, -0.17, 0.86), (0, -0.32, 1.05), "Chest"),
    "Eyes": ((0, -0.41, 0.975), (0, -0.48, 0.975), "Head"),      # both eyes (moved back into the head: shut)
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
for name, (base, parent) in TAR.items():
    BONES[name] = (base, tuple(V(base) + V((0, 0.06, 0))), parent)

L_ARM = (V(EL[1]) - V(SH[1])).length
L_FORE = (V(WR[1]) - V(EL[1])).length
FORE_DIR = {s: (V(WR[s]) - V(EL[s])).normalized() for s in (-1, 1)}
WALL_Y = 0.2          # (Emerge) the wall's face behind it at the start
EMERGE_LEN = 48
STRETCH_MAX = 0.42    # how far the forearm stretches (tar) to reach what's out of reach

# ---------------------------------------------------------------- tracks to poses

Z = ((0, 0, 0), (0, 0, 0))
PSEUDO = ("TgtL", "TgtR", "IkL", "IkR", "PoleL", "PoleR", "StepL", "StepR", "Melt", "EyesUp",
          "TarHeadW", "TarBackW", "TarLW", "TarRW")


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


def progress(frame, wall):
    """(Emerge) how far the game has moved it forward out of the wall by `frame` (evenly, 0.9 m over the clip)."""
    return 0.9 * (frame - 1) / EMERGE_LEN if wall else 0.0


def off_floor(pose, s, side, floor_z):
    """A hand that would go through the floor is lifted: the wrist bends (the claws curl back off it), then the arm
    swings up (out and on the way it's pointing) until the claws just clear it: it drags its knuckles."""
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
        h = V((d.x + s * 0.4, d.y - 0.05, 0.0))
        toward = (h.normalized() + V((0, 0, 0.6))).normalized()
        axis = d.cross(toward)
        if axis.length < 1e-5:
            break
        turn = Quaternion(axis.normalized(), math.radians(1.5))
        chest = m["Chest"].to_quaternion()
        rot, off = pose.get(arm, Z)
        pose[arm] = (_deg(chest.inverted() @ turn @ chest @ _quat(_rot_of(rot))), tuple(off))
    return pose


ANCHORS = {}     # (Emerge) where each strand is stuck in the wall (wall space)
MELT = {}        # (Death) the Root's spread (sxy, sz) each frame
CURRENT = [None]


def finish(raw, frame, wall=False, floor=True):
    """One frame's tracks made a pose: the legs bent onto their feet, the arms onto their wrists' places (blended with
    their keyed hang), the forearms stretched where that's out of reach, the feet flat, the strands stretched, the
    eyes held above the floor while it melts."""
    pose = {k: (tuple(_rot_of(v)), tuple(_off_of(v))) for k, v in raw.items() if k in BONES}
    shift = V((0, progress(frame, wall), 0))
    if wall:
        rot, off = pose["Hips"]
        pose["Hips"] = (rot, tuple(V(off) + shift))
    root = kit.pose_matrices(BONES, pose)["Root"]
    for s, side in ((-1, "L"), (1, "R")):
        foot = root @ (V(ANK[s]) + at(raw, "Step" + side) + shift)
        pose = kit.reach(BONES, pose, "Leg" + side, "Shin" + side, lambda _m, t=foot: t, (s * 0.55, -1, 0.1))
    for s, side in ((-1, "L"), (1, "R")):
        weight = max(0.0, min(1.0, num(raw, "Ik" + side)))
        if "Tgt" + side not in raw or weight <= 0:
            continue
        target = root @ (at(raw, "Tgt" + side) + shift)
        pole = at(raw, "Pole" + side, (s, 0.4, -0.1))
        fk = {b: pose.get(b, Z) for b in ("Arm" + side, "Fore" + side)}
        ik = kit.reach(BONES, pose, "Arm" + side, "Fore" + side, lambda _m, t=target: t, pole)
        for b in fk:
            q = _quat(fk[b][0]).slerp(_quat(_rot_of(ik[b])), weight)
            pose[b] = (_deg(q), tuple(fk[b][1]))
        m = kit.pose_matrices(BONES, pose)
        shoulder = m["Chest"] @ V(SH[s])
        short = (target - shoulder).length - (L_ARM + L_FORE)
        if short > 0:
            stretch = min(STRETCH_MAX, short) * weight
            rot, off = pose.get("Hand" + side, Z)
            pose["Hand" + side] = (rot, tuple(V(off) + FORE_DIR[s] * stretch))
    if floor:   # (the floor sinks with the Root as it melts)
        for s, side in ((-1, "L"), (1, "R")):
            pose = off_floor(pose, s, side, min(0.0, root.translation.z))
    m = kit.pose_matrices(BONES, pose)
    for s, side in ((-1, "L"), (1, "R")):
        # The feet keep their keyed turn in the world (flat on the floor, unless keyed), whatever the shins do.
        keyed = _quat(_rot_of(raw.get("Foot" + side, (0, 0, 0))))
        parent = m["Shin" + side].to_quaternion()
        pose["Foot" + side] = (_deg(parent.inverted() @ keyed), (0, 0, 0))
    for name, (base, parent) in TAR.items():
        weight = num(raw, name + "W")
        rot = (0, 0, 0)
        if abs(weight) < 1e-4 or name not in ANCHORS:
            pose[name] = (rot, (0, 0, 0))
            continue
        carried = m[parent] @ V(base)
        anchor = root @ (ANCHORS[name] + shift)
        target = carried.lerp(anchor, weight)
        pose[name] = (rot, tuple(m[parent].inverted() @ target - V(base)))
    if "Melt" in raw:
        sxy, sz = raw["Melt"][0][0], raw["Melt"][0][1]
        MELT.setdefault(CURRENT[0], {})[frame] = (sxy, sz)
        up = num(raw, "EyesUp")
        if up > 0:
            # The Root spreads (sxy) and flattens (sz) about its head; it's sunk by its offset. Raise the eyes in the
            # head so they're at the floor (just) when the head's gone under it.
            d = V(pose["Root"][1])
            eye = m["Eyes"] @ V(BONES["Eyes"][0])
            true_z = sz * (eye.z - d.z) + d.z
            need = max(0.0, 0.012 - true_z) / max(sz, 1e-3) * up
            rot, off = pose.get("Eyes", Z)
            lift = m["Head"].to_quaternion().inverted() @ V((0, 0, need))
            pose["Eyes"] = (rot, tuple(V(off) + lift))
    return pose


# ---------------------------------------------------------------- the drips (swung by their weight)

DRIPS = {"DripChin": (9.0, 4.0), "DripBelly": (7.0, 3.6), "DripL": (3.0, 2.8), "DripR": (3.0, 2.8)}
SUB = 6
DT = 1.0 / 30 / SUB


def run(n, periodic, step, start, settle):
    """Drive a simulation over the clip's frames (the Widow's): a loop runs three times round (its seam joins); a
    one-off starts settled and eases to rest over its last frames (the next clip begins settled)."""
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
            q = rest_dir.rotation_difference(inward.normalized())
            p[bone] = (_deg(q), (0.0, 0.0, 0.0))


def clip(name, length, tracks, periodic=False, wall=False, floor=True):
    """A clip `length` frames long (30 a second; frames 1..length+1) from per-bone tracks, baked to every frame."""
    CURRENT[0] = name
    frames = [(f, finish(raw, f, wall)) for f, raw in kit.bake(tracks, length, periodic)]
    drips(frames, periodic, floor)
    for _, p in frames:
        for b in PSEUDO:
            p.pop(b, None)
    return ("LINEAR", frames)


def held(pose, frame):
    """A whole pose as a key at `frame` for every track (to start or end a clip on it)."""
    return {track: [(frame, value)] for track, value in pose.items()}


def keys(*parts):
    """Tracks merged: each a {track: [(frame, value)]}."""
    merged = {}
    for p in parts:
        for track, ks in p.items():
            merged.setdefault(track, {}).update(dict(ks))   # (a later key on the same frame wins)
    return {track: sorted(ks.items()) for track, ks in merged.items()}


# ---------------------------------------------------------------- poses

# HUNCH (Idle's frame 1: Walk starts on it, Emerge, Recover and Stagger end on it): the rest as built: bent over, the
# head forward and up, the arms hanging, claws just off the floor, feet planted.
HUNCH = {"Root": Z, "Hips": Z, "Spine": (0, 0, 0), "Chest": (0, 0, 0), "Head": (0, 0, 0), "Eyes": Z,
         "ArmL": (0, 0, 0), "ForeL": (0, 0, 0), "HandL": (0, 0, 0), "ArmR": (0, 0, 0), "ForeR": (0, 0, 0),
         "HandR": (0, 0, 0), "StepL": g(0, 0, 0), "StepR": g(0, 0, 0)}
WRIST_R = V(WR[1])
WRIST_L = V(WR[-1])

# RAISED (Windup's end, Swipe's start): reared up, turned to its right, the right arm high and back, claws spread back.
RAISED_HIPS = ((-6, 0, 6), (0, 0.035, 0.022))
RAISED_TGT = (0.4, 0.2, 1.34)
# THROUGH (Swipe's end, Recover's start): lurched forward and round to its left, the arm low to its left.
THROUGH_HIPS = ((6, 0, -16), (-0.04, -0.2, -0.03))
THROUGH_TGT = (-0.55, -0.5, 0.22)
LUNGE_STEP = g(-0.03, -0.2, 0)

ACTIONS = {}

# Idle (2 s loop): it can't stand still: an irregular sway (the hips lead, the spine, the chest after), the head lolling
# heavily after it and twitching once, a blink, the arms swinging after the body; a shudder runs through it (f46-51).
ACTIONS["Idle"] = clip("Idle", 60, keys(held(HUNCH, 1), {
    "Hips": [(14, ((0, 3, 0), (0.016, 0, -0.008))), (27, ((0, -1, 0), (0.004, 0, 0.002))),
             (40, ((0, -4, -2), (-0.018, 0.004, -0.01))), (52, ((0, -1, 1), (-0.006, 0, 0.0)))],
    "Spine": [(16, (2, 3, 1)), (29, (0, -1, 0)), (42, (3, -4, -1)), (54, (1, -1, 0))],
    "Chest": [(10, (-3, 1, 0)), (18, (2, 4, 0)), (31, (-2, -1, 0)), (44, (4, -4, -2)), (46, (5, -1, -2)),
              (47, (3, -5, -1)), (48, (5, -1, -2)), (49, (3, -4, -1)), (50, (4, -2, -1)), (55, (1, -1, 0))],
    "Head": [(8, (2, 2, -3)), (20, (4, 10, 6)), (25, (6, 12, 7)), (30, (5, 10, 5)), (32, (-6, -5, -12)),
             (40, (0, -8, -6)), (47, (3, -4, 3)), (48, (1, -7, 1)), (49, (4, -3, 4)), (56, (1, -1, 1))],
    "Eyes": [(17, Z), (19, g(0, 0.03, 0)), (21, g(0, 0.03, 0)), (23, Z)],
    "ArmL": [(18, (-2, 3, 0)), (31, (2, -1, 0)), (45, (-3, -4, 0)), (57, (1, 0, 0))],
    "ForeL": [(21, (-3, 2, 0)), (34, (3, -1, 0)), (48, (-4, -3, 0)), (59, (1, 0, 0))],
    "HandL": [(24, (4, 0, 0)), (38, (-2, 0, 0)), (51, (5, 0, 0))],
    "ArmR": [(20, (2, 4, 0)), (33, (-1, -1, 0)), (47, (2, -5, 0)), (58, (0, -1, 0))],
    "ForeR": [(23, (3, 3, 0)), (36, (-2, -1, 0)), (50, (3, -4, 0))],
    "HandR": [(26, (6, 0, 0)), (40, (-3, 0, 0)), (52, (5, 0, 0))],
}), periodic=True)

# Walk (1.2 s loop, upright passing pose at frame 1): a lurching shuffle, out of time: the right foot scuffs back along
# the floor and is flung forward (lands f6), the left lifts high and comes down heavy (f26), the body stumbling over it;
# the hips lurch onto each foot, the chest rolls after them, the head bobs after the chest; the left arm drags its
# knuckles, the right swings loose.
ACTIONS["Walk"] = clip("Walk", 36, keys(held(HUNCH, 1), {
    "StepR": [(4, g(0.01, -0.1, 0.035)), (6, g(0, -0.135, 0)), (16, g(0, 0.0, 0)), (26, g(0, 0.13, 0)),
              (31, g(0, 0.075, 0.012))],
    "StepL": [(10, g(0, 0.13, 0)), (14, g(-0.01, 0.07, 0.055)), (20, g(-0.02, -0.06, 0.06)), (24, g(-0.01, -0.13, 0.025)),
              (26, g(0, -0.145, 0))],
    "Hips": [(6, ((1, 6, -3), (0.03, 0, -0.022))), (12, ((0, 7, -4), (0.04, 0, 0.0))), (19, ((0, 1, 0), (0.008, 0, 0.006))),
             (26, ((3, -8, 5), (-0.035, -0.02, -0.035))), (30, ((2, -10, 4), (-0.045, -0.01, -0.025))), (34, ((1, -3, 1), (-0.012, 0, -0.004)))],
    "Spine": [(8, (1, 5, -2)), (14, (0, 6, -3)), (21, (0, 0, 0)), (28, (4, -7, 4)), (32, (3, -8, 3))],
    "Chest": [(9, (0, 5, -4)), (15, (-1, 6, -5)), (22, (1, 0, 0)), (29, (5, -8, 5)), (33, (4, -9, 5))],
    "Head": [(4, (-3, -3, 3)), (11, (3, -9, 5)), (17, (0, -7, 4)), (24, (-5, 0, 0)), (29, (8, 11, -7)), (33, (2, 13, -5))],
    "ArmL": [(10, (5, 2, 0)), (20, (7, 1, 0)), (29, (3, 3, 0)), (34, (1, 1, 0))],
    # (the left hand drags along the floor, behind, the elbow bowed out; the arm hangs again as it passes)
    "TgtL": [(1, g(*WRIST_L)), (8, g(-0.25, -0.17, 0.15)), (16, g(-0.26, -0.07, 0.13)), (24, g(-0.28, -0.02, 0.125)), (30, g(-0.27, -0.09, 0.14)),
             (34, g(-0.23, -0.19, 0.16))],
    "IkL": [(1, w(0)), (7, w(1)), (32, w(1))],
    "PoleL": [(1, g(-1, 0.6, 0.2))],
    "ForeL": [(12, (4, 0, 0)), (22, (8, 0, 0)), (31, (2, 0, 0))],
    "HandL": [(12, (10, 0, 0)), (24, (12, 0, 0)), (32, (4, 0, 0))],
    "ArmR": [(6, (-10, -3, 0)), (15, (-6, -5, 0)), (24, (12, -2, 0)), (32, (6, 0, 0))],
    "ForeR": [(9, (-8, 0, 0)), (18, (-4, 0, 0)), (27, (10, 0, 0)), (34, (4, 0, 0))],
    "HandR": [(11, (-6, 0, 0)), (29, (10, 0, 0))],
}), periodic=True)

# Emerge (1.6 s; in the wall's space: the game moves it 0.9 m forward over the clip, so the keyed hips, wrists and feet
# are where they are in the world, and what's planted stays planted): pressed back into the stone, arms spread into it,
# head tipped back, eyes shut. A twitch; the eyes open; the head lifts, the right arm pulls out of the stone and
# reaches, the body straining forward, the strands stretched back to the wall; the head's strand tears, then the rest:
# it lurches forward, the left arm ripping out, onto all fours (the right hand, then the left); it gathers itself and
# rises into its hunch.
END = -0.9   # (where it ends, in the wall's space)
EMERGE = {
    "Root": [(1, Z)],
    "Hips": [(1, ((-14, 0, 0), (0, 0.09, 0.0))), (8, ((-14, 0, 0), (0, 0.09, 0.0))), (12, ((-10, 0, 3), (0, 0.07, 0.0))),
             (18, ((-3, 0, -4), (0, 0.0, -0.02))), (24, ((3, 0, 4), (0, -0.07, -0.04))), (26, ((0, 0, 2), (0, -0.04, -0.03))),
             (29, ((24, 0, 0), (0, -0.4, -0.1))), (33, ((30, 0, -4), (0, -0.6, -0.14))), (38, ((24, 0, 2), (0, -0.7, -0.13))),
             (43, ((5, 0, 0), (0, -0.85, -0.02))), (46, ((-2, 0, 0), (0, END - 0.01, 0.006))), (49, ((0, 0, 0), (0, END, 0)))],
    "Spine": [(1, (-16, 0, 0)), (10, (-14, 0, 2)), (18, (-4, 0, -3)), (24, (6, 0, 4)), (26, (4, 0, 2)), (29, (20, 0, 0)),
              (34, (24, 0, -3)), (40, (14, 0, 2)), (45, (-2, 0, 0)), (49, (0, 0, 0))],
    "Chest": [(1, (-18, 0, 0)), (11, (-16, -4, 6)), (19, (0, -6, 8)), (24, (10, 4, -4)), (26, (8, 0, 0)), (29, (16, 0, 0)),
              (35, (20, 0, 4)), (41, (8, 0, 0)), (46, (-2, 0, 0)), (49, (0, 0, 0))],
    "Head": [(1, (-40, 0, 0)), (6, (-40, 0, 0)), (8, (-30, 0, 6)), (10, (-34, 4, -4)), (14, (-6, -6, 10)), (20, (10, 8, -6)),
             (23, (16, -6, 4)), (27, (18, 0, 0)), (30, (-10, 0, 0)), (34, (-26, 0, 4)), (40, (-14, -4, -6)), (45, (4, 2, 2)),
             (49, (0, 0, 0))],
    "Eyes": [(1, g(0, 0.03, 0)), (5, g(0, 0.03, 0)), (8, Z), (49, Z)],
    "TgtR": [(1, g(0.5, 0.42, 0.8)), (8, g(0.5, 0.42, 0.8)), (11, g(0.46, 0.18, 0.84)), (16, g(0.38, -0.3, 0.7)),
             (21, g(0.36, -0.36, 0.6)), (26, g(0.33, -0.42, 0.55)), (29, g(0.31, -0.75, 0.3)), (32, g(0.29, -0.92, 0.17)),
             (38, g(0.29, -0.92, 0.17)), (43, g(0.24, -0.98, 0.24)), (49, g(WRIST_R.x, WRIST_R.y + END, WRIST_R.z))],
    "IkR": [(1, w(1)), (38, w(1)), (46, w(0)), (49, w(0))],
    "PoleR": [(1, g(1, 0.3, 0.2)), (49, g(1, 0.4, -0.1))],
    "TgtL": [(1, g(-0.5, 0.42, 0.74)), (24, g(-0.5, 0.42, 0.74)), (26, g(-0.48, 0.32, 0.72)), (28, g(-0.4, -0.2, 0.5)),
             (31, g(-0.32, -0.7, 0.25)), (34, g(-0.3, -0.86, 0.17)), (39, g(-0.3, -0.86, 0.17)), (44, g(-0.25, -0.95, 0.24)),
             (49, g(WRIST_L.x, WRIST_L.y + END, WRIST_L.z))],
    "IkL": [(1, w(1)), (39, w(1)), (47, w(0)), (49, w(0))],
    "PoleL": [(1, g(-1, 0.3, 0.2)), (49, g(-1, 0.4, -0.1))],
    "HandR": [(1, (-20, 0, 0)), (12, (-30, 0, 0)), (20, (-10, 0, 0)), (29, (-30, 0, 0)), (32, (-55, 0, 0)), (38, (-55, 0, 0)),
              (46, (0, 0, 0)), (49, (0, 0, 0))],
    "HandL": [(1, (-20, 0, 0)), (26, (-20, 0, 0)), (30, (-30, 0, 0)), (34, (-55, 0, 0)), (39, (-55, 0, 0)), (47, (0, 0, 0)),
              (49, (0, 0, 0))],
    "ArmL": [(1, (0, 0, 0))], "ForeL": [(1, (0, 0, 0))], "ArmR": [(1, (0, 0, 0))], "ForeR": [(1, (0, 0, 0))],
    "StepL": [(1, g(0, 0, 0)), (26, g(0, 0, 0)), (28, g(0, -0.2, 0.06)), (30, g(0, -0.45, 0)), (39, g(0, -0.45, 0)),
              (42, g(0, -0.7, 0.05)), (44, g(0, END, 0)), (49, g(0, END, 0))],
    "StepR": [(1, g(0, 0, 0)), (29, g(0, 0, 0)), (31, g(0, -0.25, 0.06)), (33, g(0, -0.55, 0)), (43, g(0, -0.55, 0)),
              (46, g(0, -0.8, 0.04)), (48, g(0, END, 0)), (49, g(0, END, 0))],
    "TarHeadW": [(1, w(1)), (22, w(1)), (24, w(-0.12)), (27, w(0))],
    "TarBackW": [(1, w(1)), (26, w(1)), (28, w(-0.12)), (31, w(0))],
    "TarRW": [(1, w(1)), (25, w(1)), (27, w(-0.12)), (30, w(0))],
    "TarLW": [(1, w(1)), (27, w(1)), (29, w(-0.12)), (32, w(0))],
}
# (where each strand sticks in the wall: its root at the start, pushed back to the wall's face)
_first = kit.pose_matrices(BONES, finish(kit.bake(EMERGE, EMERGE_LEN)[0][1], 1, wall=True))
for _name, (_base, _parent) in TAR.items():
    _p = _first[_parent] @ V(_base)
    ANCHORS[_name] = V((_p.x, max(_p.y, WALL_Y) + 0.03, _p.z))
ACTIONS["Emerge"] = clip("Emerge", EMERGE_LEN, EMERGE, wall=True)

# Windup (0.9 s): a dip (getting under it), then it rears up, turning to its right, and lifts the right arm slowly, high
# and back, the hand cocked back and the claws spread; the head follows, cocked, still on you; the left arm swings out
# for balance; at the top it trembles.
ACTIONS["Windup"] = clip("Windup", 27, keys(held(HUNCH, 1), {
    "Hips": [(5, ((3, 0, 0), (0, -0.01, -0.02))), (16, ((-5, 0, 4), (0, 0.03, 0.016))), (24, ((-7, 0, 6), (0, 0.04, 0.024))),
             (28, RAISED_HIPS)],
    "Spine": [(6, (6, 0, 0)), (17, (-8, 0, 6)), (25, (-12, 0, 9)), (28, (-11, 0, 8))],
    "Chest": [(7, (5, 0, 0)), (18, (-10, -4, 10)), (26, (-14, -6, 14)), (28, (-13, -5, 13))],
    "Head": [(9, (6, 0, 0)), (20, (-4, 10, -14)), (26, (-7, 16, -17)), (28, (-6, 15, -16))],
    "TgtR": [(1, g(*WRIST_R)), (8, g(0.3, -0.26, 0.3)), (17, g(0.44, -0.02, 1.0)), (24, g(0.41, 0.21, 1.36)),
             (26, g(0.405, 0.205, 1.35)), (28, g(*RAISED_TGT))],
    "IkR": [(1, w(0)), (10, w(1)), (28, w(1))],
    "PoleR": [(1, g(1, 0.4, -0.1)), (17, g(1, 0.3, 0.4)), (28, g(0.8, 0.4, 0.6))],
    "HandR": [(10, (-6, 0, 0)), (20, (-30, 0, 0)), (22, (-36, 0, 0)), (23, (-31, 3, 0)), (24, (-37, -2, 0)),
              (25, (-32, 3, 0)), (26, (-37, -2, 0)), (27, (-33, 2, 0)), (28, (-36, 0, 0))],
    "ArmL": [(10, (4, 0, 0)), (22, (8, 12, 0)), (28, (8, 11, 0))],
    "ForeL": [(13, (4, 0, 0)), (25, (-6, 6, 0)), (28, (-5, 5, 0))],
    "HandL": [(16, (6, 0, 0)), (28, (-8, 0, 0))],
    "Eyes": [(1, Z), (28, Z)],
}))

# Swipe (0.35 s; the blow at frame 5.5): down and across from its right to its left in front; the hips lunge first (the
# left foot steps in), the chest turns after them, the arm whips round last and stretches to reach (the claws 1.5 m out
# at chest height at the blow); the head comes round after; follow-through low to its left, the left arm flung back.
ACTIONS["Swipe"] = clip("Swipe", 10, {
    "Root": [(1, Z)],
    "Hips": [(1, RAISED_HIPS), (3, ((2, 0, 2), (0, -0.06, 0.01))), (5.5, ((6, 0, -8), (-0.01, -0.19, -0.02))),
             (8, ((7, 0, -14), (-0.03, -0.22, -0.03))), (11, THROUGH_HIPS)],
    "Spine": [(1, (-11, 0, 8)), (3, (-2, 0, 4)), (5.5, (8, 0, -10)), (8, (11, 0, -18)), (11, (10, 0, -20))],
    "Chest": [(1, (-13, -5, 13)), (2, (-12, -5, 12)), (4, (-2, 0, 4)), (6, (9, 4, -14)), (8, (12, 6, -20)), (11, (11, 5, -22))],
    "Head": [(1, (-6, 15, -16)), (4, (-8, 10, -12)), (7, (0, 0, 4)), (9, (10, -6, 14)), (11, (8, -6, 12))],
    "Eyes": [(1, Z)],
    "TgtR": [(1, g(*RAISED_TGT)), (3, g(0.58, -0.4, 1.12)), (5.5, g(0.07, -1.46, 0.86)), (8, g(-0.62, -0.85, 0.42)),
             (11, g(*THROUGH_TGT))],
    "IkR": [(1, w(1))],
    "PoleR": [(1, g(0.8, 0.4, 0.6)), (5.5, g(0.3, 0.2, 1)), (11, g(0.2, 0.5, 1))],
    "HandR": [(1, (-36, 0, 0)), (4, (-14, 0, 0)), (6, (8, 0, -8)), (11, (18, 0, 0))],
    "ForeR": [(1, (0, 0, 0))], "ArmR": [(1, (0, 0, 0))],
    "ArmL": [(1, (8, 11, 0)), (6, (20, 14, 0)), (11, (14, 6, 0))],
    "ForeL": [(1, (-5, 5, 0)), (7, (10, 8, 0)), (11, (6, 4, 0))],
    "HandL": [(1, (-8, 0, 0)), (11, (4, 0, 0))],
    "StepL": [(1, g(0, 0, 0)), (3, g(-0.01, -0.08, 0.045)), (5.5, LUNGE_STEP), (11, LUNGE_STEP)],
    "StepR": [(1, g(0, 0, 0)), (11, g(0, 0, 0))],
})

# Recover (0.77 s): carried on past by the swing, it sways (over to its left, then back), drags the arm back along the
# floor to its side, and settles into its hunch; the right foot shuffles under it, then the left steps back in.
ACTIONS["Recover"] = clip("Recover", 23, keys(held(HUNCH, 24), {
    "Hips": [(1, THROUGH_HIPS), (5, ((5, -4, -18), (-0.07, -0.2, -0.04))), (11, ((2, 4, -8), (-0.02, -0.1, -0.01))),
             (17, ((-1, -2, 2), (0.01, -0.02, 0.004)))],
    "Spine": [(1, (10, 0, -20)), (6, (12, -4, -22)), (12, (5, 4, -8)), (18, (-1, -1, 2))],
    "Chest": [(1, (11, 5, -22)), (7, (13, 2, -24)), (13, (5, 5, -6)), (19, (-2, -1, 2))],
    "Head": [(1, (8, -6, 12)), (8, (16, -8, 16)), (14, (2, 8, -2)), (20, (-2, -2, 3))],
    "Eyes": [(1, Z)],
    "TgtR": [(1, g(*THROUGH_TGT)), (6, g(-0.45, -0.55, 0.15)), (13, g(-0.05, -0.45, 0.15)), (18, g(0.18, -0.26, 0.17)),
             (24, g(*WRIST_R))],
    "IkR": [(1, w(1)), (13, w(1)), (22, w(0))],
    "PoleR": [(1, g(0.2, 0.5, 1)), (13, g(1, 0.4, 0.3)), (24, g(1, 0.4, -0.1))],
    "HandR": [(1, (18, 0, 0)), (8, (24, 0, 0)), (16, (10, 0, 0))],
    "ArmL": [(1, (14, 6, 0)), (8, (-8, -6, 0)), (15, (4, 2, 0))],
    "ForeL": [(1, (6, 4, 0)), (10, (-10, -4, 0)), (17, (4, 1, 0))],
    "HandL": [(1, (4, 0, 0)), (12, (-6, 0, 0))],
    "StepL": [(1, LUNGE_STEP), (10, LUNGE_STEP), (13, g(-0.02, -0.1, 0.035)), (16, g(0, 0, 0))],
    "StepR": [(1, g(0, 0, 0)), (4, g(0, 0, 0)), (6, g(-0.05, -0.05, 0.03)), (8, g(-0.07, -0.07, 0)), (17, g(-0.07, -0.07, 0)),
              (20, g(-0.03, -0.03, 0.025)), (22, g(0, 0, 0))],
}))

# Stagger (0.67 s): struck: jolted back (the hips first), the head snapping back a frame late, the arms flung up and
# out, the forearms after; a stumble back (right foot, then left) and it gathers itself, stepping in again, the hunch.
ACTIONS["Stagger"] = clip("Stagger", 20, keys(held(HUNCH, 1), held(HUNCH, 21), {
    "Hips": [(3, ((-8, 0, 0), (0, 0.09, 0.008))), (6, ((-10, 3, 0), (0.01, 0.13, -0.01))), (10, ((-2, -3, 0), (-0.01, 0.1, -0.03))),
             (14, ((3, 1, 0), (0, 0.04, -0.015)))],
    "Spine": [(3, (-12, 0, 0)), (7, (-14, 4, 0)), (11, (4, -3, 0)), (15, (3, 0, 0))],
    "Chest": [(3, (-14, 0, 0)), (5, (-18, 4, 4)), (10, (6, -4, 0)), (15, (2, 1, 0))],
    "Head": [(2, (2, 0, 0)), (4, (-32, 0, 0)), (5, (-36, 6, 6)), (8, (-10, -6, -8)), (12, (14, 4, 4)), (16, (-3, 0, 0))],
    "Eyes": [(4, Z), (5, g(0, 0.03, 0)), (8, g(0, 0.03, 0)), (10, Z)],
    "ArmL": [(4, (-55, 35, 0)), (6, (-72, 48, 0)), (10, (-28, 20, 0)), (14, (6, 0, 0))],
    "ArmR": [(4, (-50, -35, 0)), (6, (-68, -46, 0)), (10, (-24, -18, 0)), (14, (6, 0, 0))],
    "ForeL": [(5, (-20, 0, 0)), (8, (-40, 4, 0)), (12, (-10, 0, 0)), (16, (8, 0, 0))],
    "ForeR": [(5, (-18, 0, 0)), (8, (-38, -4, 0)), (12, (-8, 0, 0)), (16, (8, 0, 0))],
    "HandL": [(6, (-30, 0, 0)), (10, (-40, 0, 0)), (15, (10, 0, 0))],
    "HandR": [(6, (-30, 0, 0)), (10, (-40, 0, 0)), (15, (10, 0, 0))],
    "StepR": [(3, g(0, 0, 0)), (5, g(0.01, 0.06, 0.05)), (8, g(0.02, 0.13, 0)), (15, g(0.02, 0.13, 0)), (18, g(0.01, 0.05, 0.035))],
    "StepL": [(9, g(0, 0, 0)), (11, g(-0.01, 0.06, 0.04)), (13, g(-0.01, 0.1, 0)), (16, g(-0.01, 0.1, 0)), (19, g(0, 0.03, 0.03))],
}))

# Death (1.5 s): a jolt back; it buckles, slumps to its knees, folds forward, the arms flopping to the floor; then it
# melts down into the stone, spreading flat as it goes (Melt: the Root spread and flattened), the eyes last (held at
# the floor a moment after the head's gone under), then nothing.
ACTIONS["Death"] = clip("Death", 45, keys(held(HUNCH, 1), {
    "Root": [(22, Z), (30, ((0, 0, 0), (0, 0, -0.1))), (38, ((0, 0, 0), (0, 0, -0.45))), (46, ((0, 0, 0), (0, 0, -1.2)))],
    "Melt": [(1, ((1, 1, 0), (0, 0, 0))), (22, ((1, 1, 0), (0, 0, 0))), (31, ((1.5, 0.7, 0), (0, 0, 0))), (39, ((2.1, 0.4, 0), (0, 0, 0))),
             (46, ((2.5, 0.22, 0), (0, 0, 0)))],
    "EyesUp": [(1, w(0)), (27, w(0)), (31, w(1)), (41, w(1)), (45, w(0))],
    "Hips": [(4, ((-3, 0, 0), (0, 0.04, 0.0))), (10, ((4, 4, 0), (0, 0.04, -0.12))), (15, ((10, 2, 0), (0, 0.07, -0.2))),
             (18, ((8, 2, 0), (0, 0.07, -0.185))), (22, ((10, 2, 0), (0, 0.07, -0.2)))],
    "Spine": [(4, (-5, 0, 0)), (10, (4, 2, 0)), (16, (20, 0, 0)), (22, (34, 0, 4)), (30, (40, 0, 4))],
    "Chest": [(5, (-8, 0, 0)), (11, (2, 0, 0)), (17, (20, 0, 0)), (23, (28, 0, 4)), (31, (34, 0, 4))],
    "Head": [(4, (-14, 0, 0)), (6, (-18, 5, 0)), (12, (-6, 0, 0)), (18, (24, -6, 0)), (24, (36, -12, 8)), (32, (40, -14, 8))],
    "Eyes": [(1, Z)],
    "ArmL": [(6, (-5, 5, 0)), (14, (4, 4, 0)), (20, (-24, 20, 0)), (28, (-30, 34, 0)), (36, (-26, 50, 0))],
    "ArmR": [(6, (-5, -5, 0)), (14, (4, -4, 0)), (21, (-26, -20, 0)), (29, (-30, -36, 0)), (37, (-26, -52, 0))],
    "ForeL": [(7, (-10, 0, 0)), (16, (8, 0, 0)), (24, (-20, 6, 0)), (34, (-10, 10, 0))],
    "ForeR": [(7, (-10, 0, 0)), (16, (8, 0, 0)), (25, (-20, -6, 0)), (35, (-10, -10, 0))],
    "HandL": [(10, (10, 0, 0)), (24, (-30, 0, 0)), (34, (-40, 0, 0))],
    "HandR": [(10, (10, 0, 0)), (25, (-30, 0, 0)), (35, (-40, 0, 0))],
    "StepL": [(8, g(0, 0, 0)), (13, g(0, 0.12, 0.02)), (16, g(0, 0.17, -0.015)), (46, g(0, 0.17, -0.015))],
    "StepR": [(9, g(0, 0, 0)), (14, g(0, 0.12, 0.02)), (17, g(0, 0.16, -0.015)), (46, g(0, 0.16, -0.015))],
}), floor=False)


def melt_keys(armature):
    """Key the Root's scale on every clip: 1 everywhere (an unkeyed bone keeps the last clip's scale), Death's spread
    and flattened as it melts."""
    root = armature.pose.bones["Root"]
    axes = armature.data.bones["Root"].matrix_local.to_3x3()
    for action in bpy.data.actions:
        armature.animation_data.action = action
        start, end = (int(f) for f in action.frame_range)
        table = MELT.get(action.name, {})
        for f in range(start, end + 1):
            sxy, sz = table.get(f, (1.0, 1.0))
            world = Vector((sxy, sxy, sz))
            local = Vector([sum(world[j] * axes[j][i] ** 2 for j in range(3)) for i in range(3)])
            root.scale = local
            root.keyframe_insert("scale", frame=f)
    armature.animation_data.action = bpy.data.actions["Idle"]


# ---------------------------------------------------------------- the model

def weigh(ob, fn):
    """Skin a part smoothly: fn(rest point) -> {bone: weight}."""
    for v in ob.data.vertices:
        for bone, wt in fn(v.co).items():
            if wt <= 0:
                continue
            group = ob.vertex_groups.get(bone) or ob.vertex_groups.new(name=bone)
            group.add([v.index], wt, "REPLACE")
    return ob


# The body in one soft piece: rings up a bent spine (pelvis, belly, the hump rising behind the head, the neck sunk into
# it), each (centre, half-width, half-depth), weighted to the hips, spine and chest by height.
TORSO = [((0, 0.04, 0.33), 0.04, 0.04), ((0, 0.04, 0.36), 0.095, 0.085), ((0, 0.045, 0.42), 0.125, 0.115),
         ((0, 0.045, 0.5), 0.13, 0.12), ((0, 0.035, 0.58), 0.14, 0.13), ((0, 0.005, 0.67), 0.16, 0.15),
         ((0, -0.04, 0.75), 0.185, 0.165), ((0, -0.1, 0.82), 0.19, 0.155), ((0, -0.17, 0.865), 0.15, 0.12),
         ((0, -0.22, 0.88), 0.09, 0.07), ((0, -0.24, 0.885), 0.03, 0.03)]


def ring_tube(rings, seg=16):
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
        bm.faces.new(list(reversed(loops[0])))
        bm.faces.new(loops[-1])
        bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    return build_fn


def torso():
    ob = part("Torso", ring_tube(TORSO), bpy.data.materials["KeptBody"], None, smooth=True, jitter=0.005)
    kit.top_material(ob, bpy.data.materials["KeptBodyEdge"], 0.62)

    def by_height(co):
        def ramp(z, a, b):
            return max(0.0, min(1.0, (z - a) / (b - a)))
        chest = ramp(co.z, 0.64, 0.74)
        spine = ramp(co.z, 0.44, 0.52) * (1 - chest)
        return {"Hips": 1 - spine - chest, "Spine": spine, "Chest": chest}
    return weigh(ob, by_height)


def build():
    body, edge = kit.mat("KeptBody", "#0e0d12"), kit.mat("KeptBodyEdge", "#26222f")
    eye, claw = kit.mat("KeptEye_Glow", "#d9dccf"), kit.mat("KeptClaw", "#4a4850")
    palm = kit.mat("KeptPalm", "#0e0d12")   # (the hand: a swipe trail's inner edge)

    def lumpy(name, build_fn, matrix, group, jitter=0.006, sheen=None):
        ob = part(name, build_fn, body, matrix, smooth=True, jitter=jitter, group=group)
        if sheen is not None:
            kit.top_material(ob, edge, sheen)
        return ob

    parts = [
        torso(),
        lumpy("Head", kit.sphere(16, 12), M(HEAD_C, rot=(-8, 0, 0), scale=HEAD_R), "Head", jitter=0.004, sheen=0.6),
        # (the jaw sagging under the head, where the chin's drip hangs from)
        lumpy("Jaw", kit.sphere(10, 7), M((0, -0.35, 0.87), scale=(0.1, 0.08, 0.06)), "Head"),
    ]
    for s, side in ((-1, "L"), (1, "R")):
        parts.append(part("Eye", kit.sphere(10, 7), eye, M(EYE[s], rot=(0, 0, s * -12), scale=(0.021, 0.014, 0.019)),
                          smooth=True, group="Eyes"))
        parts.append(lumpy("Shoulder", kit.sphere(10, 7), M(SH[s], scale=(0.075, 0.075, 0.07)), "Chest", sheen=0.62))
        parts.append(lumpy("UpperArm", kit.tube([SH[s], EL[s]], [0.046, 0.031], seg=8), None, "Arm" + side, jitter=0.002))
        parts.append(lumpy("Elbow", kit.sphere(8, 6, 0.031), M(EL[s]), "Arm" + side, jitter=0.002))
        el, wr = V(EL[s]), V(WR[s])
        fore = part("Forearm", kit.tube([EL[s], WR[s]], [0.03, 0.022], seg=8), body, None, smooth=True)

        def fore_w(co, el=el, wr=wr, side=side):
            t = max(0.0, min(1.0, (co - el).dot(wr - el) / (wr - el).length_squared))
            t = max(0.0, (t - 0.25) / 0.75)
            return {"Fore" + side: 1 - t, "Hand" + side: t}
        parts.append(weigh(fore, fore_w))
        # The hand: a narrow palm, three long thin fingers curling forward, fanned, a claw on each.
        parts.append(part("Palm", kit.sphere(8, 6), palm, M((WR[s][0], WR[s][1] - 0.01, 0.135), scale=(0.034, 0.024, 0.048)),
                          smooth=True, group="Hand" + side))
        for fan in FAN:
            pts, tip = finger(s, fan)
            parts.append(part("Finger", kit.tube(pts, [0.012, 0.009, 0.007], seg=6), body, None, smooth=True,
                              group="Hand" + side))
            parts.append(part("Claw", kit.cone(5, 0.008, 0.0, 0.04), claw, kit.along(pts[-1], tip), group="Hand" + side))
        # Legs: short and bowed, the knees out; small soft feet.
        parts.append(lumpy("Thigh", kit.tube([HIP[s], KNEE[s]], [0.062, 0.042], seg=8), None, "Leg" + side, jitter=0.003))
        parts.append(lumpy("Knee", kit.sphere(8, 6, 0.041), M(KNEE[s]), "Leg" + side, jitter=0.002))
        parts.append(lumpy("Shin", kit.tube([KNEE[s], ANK[s]], [0.04, 0.03], seg=8), None, "Shin" + side, jitter=0.002))
        parts.append(lumpy("Foot", kit.sphere(10, 6), M((s * 0.115, -0.045, 0.034), scale=(0.045, 0.075, 0.03)), "Foot" + side,
                           jitter=0.002))
    # The drips: a strand of tar each, a drop at its end.
    for name, (head, tail, _) in DRIP.items():
        h, t = V(head), V(tail)
        mid = h.lerp(t, 0.55)
        parts.append(part(name, kit.tube([h, mid, t], [0.018, 0.008, 0.008], seg=6), body, None, smooth=True, group=name))
        parts.append(part(name + "Drop", kit.sphere(8, 6), body, M(t - V((0, 0, 0.008)), scale=(0.017, 0.017, 0.024)),
                          smooth=True, group=name))
    # The strands (Emerge): hidden in it, each a tube from its root (on the body) to its tip (on its bone): stretched,
    # it thins in the middle.
    for name, (base, parent) in TAR.items():
        b = V(base)
        size = 0.6 if name in ("TarL", "TarR") else 1.0
        n = 6
        pts = [b + V((0, 0.02 * i / (n - 1), 0)) for i in range(n)]
        radii = [r * size for r in (0.04, 0.026, 0.016, 0.016, 0.028, 0.05)]
        strand = part(name, kit.tube(pts, radii, seg=7), body, None, smooth=True)

        def tar_w(co, b=b, parent=parent, name=name):
            t = max(0.0, min(1.0, (co.y - b.y) / 0.02))
            return {parent: 1 - t, name: t}
        parts.append(weigh(strand, tar_w))

    mesh = kit.join(parts, "Kept")
    armature = kit.rig("KeptRig", BONES)
    kit.skin(mesh, armature)
    kit.animate(armature, ACTIONS)
    melt_keys(armature)
    return [armature, mesh]
