"""The souls waiting to cross (the Hub): ordinary dead in a long queue along the island's shore path and out along the
Ferryman's jetty, waiting for a crossing that stopped. Not Soured, not Lingerers: just the dead, patient, holding their
few things. The same kind of being as the merchant soul (a pale hooded robe, a void face, two small eyes in it), but
cooler and paler: they haven't found the warmth yet. Pale blue-grey robes, a mantle over the shoulders, a deep hood with
a drooping point, faint pale-blue eyes; no lantern. Front is -Y; origin at the feet.

Variants (each its own FBX, soul_<variant>; the same bones and clips; about 25 of them make a queue, so they differ
most in what reads from above: what they carry):
  bundle   (1.66 m, thin): a bundle on a stick over the right shoulder, the bundle swinging behind.
  trunk    (1.55 m, stout): a small travel case held before it in both hands, a paper tag on its handle.
  elder    (1.6 m, stooped): an old soul in a shawl, bent, leaning on a walking stick (right hand), the left hand
           holding the shawl closed.
  child    (1.15 m): a small one with a big hood (its point ending in a bobble), a toy boat held in both hands.
  hat      (1.72 m to the crown): a wide-brimmed hat over the hood, a little posy of dried flowers in both hands.
  blanket  (1.7 m): a rolled blanket under the right arm.

Rig: Root (the soul's place: only Step moves it), Hips, Hem (the robe's skirt from the waist, swung after the body),
Spine, Chest, Head (child of Chest; at rest it looks straight ahead along -Y, upright: the game turns it about Z towards
the Apprentice; the hood is round about it, so it turns without clipping), Eyes (in the head: moved back into the void
to close them), ArmL/ForeL, ArmR/ForeR (the hand is the forearm's tip), FootL/FootR (under the hem), Prop (what it
carries) and Dangle (what hangs from that: the bundle).

Clips (30 fps):
  Idle (4 s loop): a slow breath, a slight sway, the weight settling; a blink. Frame 1 is the pose every clip starts and
    ends on.
  Shift (2.5 s): the weight onto the left foot, a glance down at what it holds, a tired hitch of its grip (a sigh), the
    weight over to the right, and back.
  Step (1.2 s): one small shuffling step forward as the queue moves up: the Root travels STEP (0.45 m) along -Y (eased,
    f5-f30); the game moves the soul on by that at the clip's end (and the Root's back at 0 in Idle).
  Bow (1.6 s): a small, respectful bow of the head and shoulders, the eyes closing at the bottom of it.

How it's animated (the Laden's way): each bone keyed on its own timing (kit.bake: tracks); the body leads (hips, spine,
chest), the head follows a few frames late. The hips and feet are keyed where they are in the clip's world (the Root's
travel taken off). What it carries isn't keyed through the body: it's carried at the chest, turning after it a few frames
late (and hitched by its own track, PropAdj), and the hands are bent onto it every frame (kit.reach); the elder's stick
stands on the floor (its tip keyed where it is) and leans to the hand. The hem and the bundle swing by their weight (a
little simulation). A free arm hangs from keyed rotations.
"""
import math

import bmesh
import bpy
from mathutils import Euler, Matrix, Quaternion, Vector

M, part = kit.M, kit.part  # `kit` is injected by build_model.py
V = Vector

VARIANTS = ["bundle", "trunk", "elder", "child", "hat", "blanket"]
STEP = 0.45      # how far Step moves it on (m): the Root's travel, the same for every soul
IDLE, SHIFT, STEP_LEN, BOW = 120, 75, 36, 48

# ---------------------------------------------------------------- the variants

# S: scale (1 = 1.6 m to the top of the hood); W: the robe's girth; HEAD: the hood's size (about the neck);
# TIP: the hood's point (0: none); BIAS: a pose added to every frame (the elder's stoop); LIVELY: how much it moves;
# GLANCE: Shift's look down (pitch, turn: + to its right); CARRY: what the prop's carried by and how; HANDS: each hand's
# grip (a point on the prop, or on the chest, in the rest frame) and its elbow's pole; None: hanging free.
SPECS = {
    "bundle": dict(S=1.0, W=0.92, HEAD=1.0, TIP=1.1, robe="#a9b3c3", shade="#7e8899",
                   CARRY="chest", PROP=(0.205, 0.1, 1.32), DANGLE=((0.21, 0.5, 1.5), (0.21, 0.5, 1.32)),
                   HANDS={"R": ("prop", (0.2, -0.2, 1.17), (0.6, 0.2, -1)), "L": None},
                   GLANCE=(12, 22), LIVELY=1.0),
    "trunk": dict(S=0.95, W=1.12, HEAD=1.0, TIP=0.8, robe="#a0acbd", shade="#78839a",
                  CARRY="chest", PROP=(0, -0.3, 0.9),
                  HANDS={"L": ("prop", (-0.24, -0.3, 0.92), (-1, 0.4, -0.5)),
                         "R": ("prop", (0.24, -0.3, 0.92), (1, 0.4, -0.5))},
                  GLANCE=(24, 0), LIVELY=0.8),
    "elder": dict(S=1.0, W=1.0, HEAD=1.0, TIP=0.6, robe="#b3b8c3", shade="#868b98",
                  CARRY="stick", PROP=(0.3, -0.36, 0.0), GRIP=(0.27, -0.27, 0.96),
                  HANDS={"R": ("stick", None, (0.8, 0.6, -0.4)), "L": ("chest", (-0.045, -0.21, 1.22), (-1, 0.2, -1))},
                  BIAS={"Spine": (12, 0, 0), "Chest": (18, 0, 0), "Head": (-24, 0, 0)},
                  GLANCE=(16, 10), LIVELY=0.7),
    "child": dict(S=0.68, W=1.0, HEAD=1.22, TIP=1.5, robe="#b5c1d0", shade="#8693a6",
                  CARRY="chest", PROP=(0, -0.28, 1.12),
                  HANDS={"L": ("prop", (-0.14, -0.27, 1.11), (-1, 0.3, -0.6)),
                         "R": ("prop", (0.14, -0.27, 1.11), (1, 0.3, -0.6))},
                  BIAS={"Head": (-5, 0, 0)}, GLANCE=(26, 0), LIVELY=1.6),
    "hat": dict(S=0.97, W=0.95, HEAD=1.0, TIP=0.0, robe="#a3aebd", shade="#79849a",
                CARRY="chest", PROP=(0, -0.25, 0.98),
                HANDS={"L": ("prop", (-0.04, -0.25, 0.99), (-1, 0.5, -0.4)),
                       "R": ("prop", (0.04, -0.25, 0.99), (1, 0.5, -0.4))},
                GLANCE=(22, 0), LIVELY=0.9),
    "blanket": dict(S=1.03, W=1.05, HEAD=1.0, TIP=1.0, robe="#acb6c6", shade="#7f899c",
                    CARRY="chest", PROP=(0.31, -0.05, 1.04),
                    HANDS={"R": ("prop", (0.35, -0.28, 0.99), (1, 0.7, -0.2)), "L": None},
                    GLANCE=(16, 20), LIVELY=1.0),
}

# ---------------------------------------------------------------- the rig (canonical: a 1.6 m soul)

NECK = (0, -0.02, 1.37)                 # the head's joint (the hood turns about it)
HOOD_C, HOOD_R = (0, -0.02, 1.47), (0.165, 0.175, 0.17)
FACE_C, FACE_R = (0, -0.15, 1.455), (0.1, 0.06, 0.11)
EYE = {-1: (-0.038, -0.205, 1.47), 1: (0.038, -0.205, 1.47)}
SH = {-1: (-0.19, -0.01, 1.31), 1: (0.19, -0.01, 1.31)}
EL = {-1: (-0.215, -0.02, 1.04), 1: (0.215, -0.02, 1.04)}
WR = {-1: (-0.22, -0.08, 0.78), 1: (0.22, -0.08, 0.78)}
FOOT = {-1: (-0.08, -0.04, 0.03), 1: (0.08, -0.04, 0.03)}
SIDES = ((-1, "L"), (1, "R"))


def canon_bones(spec):
    prop = V(spec["PROP"])
    dangle = spec.get("DANGLE", (tuple(prop), tuple(prop - V((0, 0, 0.2)))))
    bones = {
        "Root": ((0, 0, 0), (0, 0, 0.2), None),
        "Hips": ((0, 0, 0.85), (0, 0, 0.97), "Root"),
        "Hem": ((0, 0, 0.85), (0, 0, 0.1), "Hips"),
        "Spine": ((0, 0, 0.95), (0, 0, 1.15), "Hips"),
        "Chest": ((0, 0, 1.15), NECK, "Spine"),
        "Head": (NECK, (0, -0.02, 1.62), "Chest"),
        "Eyes": ((0, -0.2, 1.47), (0, -0.26, 1.47), "Head"),
    }
    for s, side in SIDES:
        bones["Arm" + side] = (SH[s], EL[s], "Chest")
        bones["Fore" + side] = (EL[s], WR[s], "Arm" + side)
    for s, side in SIDES:
        bones["Foot" + side] = (FOOT[s], (FOOT[s][0], FOOT[s][1] - 0.1, FOOT[s][2]), "Root")
    bones["Prop"] = (tuple(prop), tuple(prop + V((0, 0, 0.15))), "Root")
    bones["Dangle"] = (tuple(dangle[0]), tuple(dangle[1]), "Prop")
    return bones


def head_point(p, spec):
    """A point on the head (the hood, the face, the eyes) at the variant's head size (scaled about the neck)."""
    return tuple(V(NECK) + (V(p) - V(NECK)) * spec["HEAD"])


def scaled_bones(spec):
    S = spec["S"]
    out = {}
    for name, (h, t, parent) in canon_bones(spec).items():
        if name in ("Head", "Eyes"):
            h, t = head_point(h, spec), head_point(t, spec)
        out[name] = (tuple(V(h) * S), tuple(V(t) * S), parent)
    return out


# ---------------------------------------------------------------- tracks to poses

Z = ((0, 0, 0), (0, 0, 0))
BODY = ("Hips", "Spine", "Chest", "Head", "Eyes", "ArmL", "ForeL", "ArmR", "ForeR")
PSEUDO = ("Travel", "StepL", "StepR", "PropAdj", "Tip")
FREE_BIAS = {"L": {"ArmL": (0, 5, 0), "ForeL": (-7, 0, 0)}, "R": {"ArmR": (0, -5, 0), "ForeR": (-7, 0, 0)}}


def g(x, y, z):
    """A position (an offset) as a track value."""
    return ((0, 0, 0), (x, y, z))


def w(v):
    """A number as a track value."""
    return ((v, 0, 0), (0, 0, 0))


def q_of(rot):
    return Euler([math.radians(a) for a in rot]).to_quaternion()


def deg(q):
    return tuple(math.degrees(a) for a in q.to_euler("XYZ"))


def rot_of(value):
    return value[0] if isinstance(value[0], (tuple, list)) else value


def off_of(value):
    return value[1] if isinstance(value[0], (tuple, list)) else (0, 0, 0)


def body_pose(raw, spec):
    """One frame's tracks as the body's pose: the Root at its travel, the hips and feet where they're keyed in the
    clip's world (the travel taken off), the variant's bias on top, a free arm hanging out from the robe."""
    pose = {}
    travel = raw["Travel"][0][0] if "Travel" in raw else 0.0
    root = V((0, -travel, 0))
    pose["Root"] = ((0, 0, 0), tuple(root))
    bias = dict(spec.get("BIAS", {}))
    for side, grip in spec["HANDS"].items():
        if grip is None:
            bias.update(FREE_BIAS[side])
    for bone in BODY:
        value = raw.get(bone, Z)
        rot, off = rot_of(value), V(off_of(value))
        if bone == "Hips":
            off = off - root
        if bone in bias:
            rot = deg(q_of(bias[bone]) @ q_of(rot))
        pose[bone] = (tuple(rot), tuple(off))
    for s, side in SIDES:
        step = V(off_of(raw.get("Step" + side, Z)))
        pose["Foot" + side] = ((0, 0, 0), tuple(step - root))
    pose["Prop"] = Z
    pose["Dangle"] = Z
    pose["Hem"] = Z
    return pose


def prop_value(bones, mats, target):
    """The Prop bone's pose (it's a child of the Root) that puts it at the armature-space matrix `target`."""
    head = V(bones["Prop"][0])
    x = mats["Root"].inverted() @ target
    r = x.to_quaternion()
    off = x.translation - head + r @ head
    return (deg(r), tuple(off))


LAG = 3.0   # frames the carried thing's turn lags the chest's


def carry(frames, raws, bones, spec, periodic, grip_len):
    """Place the prop every frame (carried at the chest, its turn lagging the chest's; or the stick, standing on the
    floor and leaning to the hand), then bend the arms onto their grips. Returns the worst hand-off (m)."""
    n = len(frames)
    cycle = n - 1 if periodic else n
    chest = [kit.pose_matrices(bones, p)["Chest"] for _, p in frames]
    S = spec["S"]
    centre = V(bones["Prop"][0])
    worst = 0.0
    for i, (_, pose) in enumerate(frames):
        raw = raws[i]
        # (a one-off's lag comes in over its first frames and goes out over its last: it starts and ends settled)
        lag = LAG if periodic else LAG * min(1.0, i / 8, (n - 1 - i) / 8)
        x = i - lag
        if periodic:
            x %= cycle
        x = max(0.0, x)
        a, b = int(math.floor(x)), min(int(math.floor(x)) + 1, n - 1)
        q_lag = chest[a].to_quaternion().slerp(chest[b].to_quaternion(), x - a)
        mats = kit.pose_matrices(bones, pose)
        adj = raw.get("PropAdj", Z)
        adj_m = (Matrix.Translation(centre + V(off_of(adj))) @ q_of(rot_of(adj)).to_matrix().to_4x4()
                 @ Matrix.Translation(-centre))
        if spec["CARRY"] == "chest":
            at = chest[i] @ centre
            target = Matrix.Translation(at) @ q_lag.to_matrix().to_4x4() @ Matrix.Translation(-centre) @ adj_m
            pose["Prop"] = prop_value(bones, mats, target)
            stick_hand = None
        else:
            # The stick: its tip on the floor where it's keyed, leaning to where the chest carries the hand (lagged).
            tip = centre + V(off_of(raw.get("Tip", Z)))
            grip = chest[i] @ (V(spec["GRIP"]) * S)
            grip = grip + V(off_of(adj))
            d = (grip - tip).normalized()
            turn = V((0, 0, 1)).rotation_difference(d)
            target = Matrix.Translation(tip) @ turn.to_matrix().to_4x4() @ Matrix.Translation(-centre)
            pose["Prop"] = prop_value(bones, mats, target)
            stick_hand = tip + d * grip_len
        for side, grip in spec["HANDS"].items():
            if grip is None:
                continue
            kind, point, pole = grip
            if kind == "prop":
                fn = (lambda m, p=V(point) * S: m["Prop"] @ p)
            elif kind == "chest":
                fn = (lambda m, p=V(point) * S: m["Chest"] @ p)
            else:
                fn = (lambda m, p=stick_hand: p)
            pose = kit.reach(bones, pose, "Arm" + side, "Fore" + side, fn, pole)
            m = kit.pose_matrices(bones, pose)
            worst = max(worst, (m["Fore" + side] @ V(bones["Fore" + side][1]) - fn(m)).length)
        frames[i] = (frames[i][0], pose)
    return worst


# bone: (stiffness pulling its tip to where its parent carries it, damping, gravity (in g))
LOOSE = {"Hem": (70.0, 7.0, 0.0), "Dangle": (0.0, 3.2, 1.0)}


def loose(frames, bones, periodic):
    """Swing the loose bones by their weight (the Keener's way): a point on each one's tip on a spring towards where its
    parent carries it, and/or hanging; a loop runs three times round so its seam joins; a one-off starts settled and
    eases to rest over its last frames (the next clip starts settled)."""
    n = len(frames)
    cycle = n - 1 if periodic else n
    order = list(range(cycle)) * (3 if periodic else 1)
    sub = 6
    dt = 1.0 / 30 / sub
    for bone, (k, c, grav) in LOOSE.items():
        mats = [kit.pose_matrices(bones, p) for _, p in frames]
        head, tail, parent = bones[bone]
        length = (V(tail) - V(head)).length
        rest_dir = (V(tail) - V(head)).normalized()

        def at(i):
            return mats[i][bone] @ V(head), mats[i][bone] @ V(tail)

        def step(a, b, pos, vel):
            ha, ta = at(a)
            hb, tb = at(b)
            for s in range(sub):
                u = (s + 1) / sub
                h, t = ha.lerp(hb, u), ta.lerp(tb, u)
                acc = (t - pos) * k - vel * c + V((0, 0, -9.81 * grav))
                new = pos + (vel + acc * dt) * dt
                d = new - h
                new = h + d.normalized() * length if d.length > 1e-6 else t
                vel = (new - pos) / dt
                pos = new
            return pos, vel

        pos, vel = at(order[0])[1], V()
        for _ in range(240):
            pos, vel = step(order[0], order[0], pos, vel)
        out = {order[0]: pos}
        for a, b in zip(order, order[1:] + order[:1] if periodic else order[1:]):
            pos, vel = step(a, b, pos, vel)
            out[b] = pos
        if periodic:
            out[n - 1] = out[0]
        else:
            rest, vel = out[n - 1], V()
            for _ in range(240):
                rest, vel = step(n - 1, n - 1, rest, vel)
            ease = min(10, n // 3)
            for i in range(n - ease, n):
                u = (i - (n - 1 - ease)) / ease
                out[i] = out[i].lerp(rest, u * u * (3 - 2 * u))
        for i, (_, p) in enumerate(frames):
            h = mats[i][bone] @ V(head)
            inward = mats[i][parent].to_quaternion().inverted() @ (out[i] - h)
            p[bone] = (deg(rest_dir.rotation_difference(inward.normalized())), (0.0, 0.0, 0.0))


def clip(name, length, tracks, spec, bones, periodic=False, grip_len=0.0, report=None):
    raws = [raw for _, raw in kit.bake(tracks, length, periodic)]
    frames = [(f + 1, body_pose(raw, spec)) for f, raw in enumerate(raws)]
    worst = carry(frames, raws, bones, spec, periodic, grip_len)
    loose(frames, bones, periodic)
    if report is not None:
        report[name] = worst
    return ("LINEAR", frames)


def held(pose, frame):
    return {track: [(frame, value)] for track, value in pose.items()}


def keys(*parts):
    """Tracks merged: each a {track: [(frame, value)]} (a later key on the same frame wins)."""
    merged = {}
    for p in parts:
        for track, ks in p.items():
            merged.setdefault(track, {}).update(dict(ks))
    return {track: sorted(ks.items()) for track, ks in merged.items()}


NEUTRAL = {b: Z for b in BODY}
NEUTRAL.update({"Travel": w(0), "StepL": g(0, 0, 0), "StepR": g(0, 0, 0), "PropAdj": Z, "Tip": g(0, 0, 0)})


def actions(spec, bones, report, grip_len):
    a = spec["LIVELY"]
    pitch, turn = spec["GLANCE"]
    closed = g(0, 0.025, 0)
    out = {}

    # Idle (4 s loop): one slow breath (the chest lifts and opens, then sinks a little lower), the weight settling over to
    # the right and back to the left (the hips lead, the spine rolls against them later); the head drifts after the
    # breath, a little look about; the free arm swings after the body; a blink at f70.
    out["Idle"] = clip("Idle", IDLE, keys(held(NEUTRAL, 1), {
        "Hips": [(34, ((0, -1.2 * a, 0), (0.012 * a, 0, -0.002))), (62, ((0, 0, 0), (0.002 * a, 0, 0.002))),
                 (92, ((0, 0.9 * a, 0), (-0.009 * a, 0, -0.002)))],
        "Spine": [(40, (-1.0, 0.9 * a, 0)), (70, (0.5, 0, 0)), (100, (0.8, -0.7 * a, 0))],
        "Chest": [(44, ((-2.4, 0.4 * a, 0), (0, 0, 0.005))), (88, ((1.8, -0.3 * a, 0), (0, 0, -0.003)))],
        "Head": [(28, (0.5, 0, 2.5 * a)), (52, (-1.6, 0, 1.5 * a)), (80, (1.2, 0, -2.5 * a)), (100, (2, 0, -1.5 * a))],
        "Eyes": [(70, Z), (72, closed), (74, closed), (77, Z)],
        "ArmL": [(48, (-1.5, 0.6, 0)), (94, (1.2, -0.4, 0))],
        "ForeL": [(54, (-2, 0, 0)), (100, (1.6, 0, 0))],
        "ArmR": [(44, (-1.5, -0.6, 0)), (90, (1.2, 0.4, 0))],
        "ForeR": [(50, (-2, 0, 0)), (96, (1.6, 0, 0))],
        "PropAdj": [(50, ((0, 0, 0), (0, 0, 0.004))), (96, ((0, 0, 0), (0, 0, -0.003)))],
    }), spec, bones, periodic=True, grip_len=grip_len, report=report)

    # Shift (2.5 s): a little dip, the weight over onto the left foot (the right hip drops), the head turning down to what
    # it holds (late, held on it); a tired hitch of the grip (it hoists what it carries and lets it settle, with a sigh: the
    # chest lifts, then slumps); the weight over to the right, the head coming back up last; back to the middle, settled.
    out["Shift"] = clip("Shift", SHIFT, keys(held(NEUTRAL, 1), held(NEUTRAL, SHIFT + 1), {
        "Hips": [(9, ((0, 0, 0), (-0.005, 0, -0.005))), (22, ((0, 2.5, -2), (-0.032, 0, -0.008))),
                 (31, ((0, 2.2, -2), (-0.029, 0, -0.007))), (45, ((0, -0.4, 1), (-0.004, 0, 0.004))),
                 (55, ((0, -2.2, 1.5), (0.026, 0, -0.006))), (64, ((0, -1.5, 1), (0.018, 0, -0.004)))],
        "Spine": [(24, (1.5, -1.6, -1.5)), (34, (3, -1.2, -1)), (47, (-2, 0.5, 0.5)), (58, (1, 1.5, 1)), (68, (0.4, 0.5, 0.3))],
        "Chest": [(14, (0.5, 0, 0)), (26, (3, -1.3, -1)), (36, (6, -0.5, 0)), (44, ((-2.5, 0, 0), (0, 0, 0.006))),
                  (52, (4, 0.5, 0.5)), (62, (1, 1.2, 1)), (69, (0.3, 0.3, 0.2))],
        "Head": [(16, (1, 0, -2)), (30, (pitch * 0.85, 0, turn * 0.9)), (40, (pitch, 0, turn)), (49, (pitch * 0.75, 0, turn * 0.8)),
                 (59, (3, 0, turn * 0.2)), (67, (-1.5, 0, -1)), (72, (0.4, 0, 0))],
        "Eyes": [(57, Z), (59, closed), (61, closed), (64, Z)],
        "PropAdj": [(35, Z), (41, ((-3, 0, 0), (0, 0, 0.035))), (47, ((1, 0, 0), (0, 0, -0.008))), (53, ((0, 0, 0), (0, 0, 0.003))),
                    (59, Z)],
        "Tip": [(37, g(0, 0, 0)), (41, g(0, -0.01, 0.045)), (46, g(0, 0, 0))],
        "ArmL": [(26, (-2, 1, 0)), (40, (1, 0, 0)), (57, (2, -1, 0)), (68, (0.4, 0, 0))],
        "ForeL": [(30, (-3, 0, 0)), (44, (2, 0, 0)), (61, (2, 0, 0)), (70, (0.3, 0, 0))],
        "ArmR": [(26, (2, 1, 0)), (40, (-1, 0, 0)), (57, (-2, -1, 0)), (68, (-0.4, 0, 0))],
        "ForeR": [(30, (-3, 0, 0)), (44, (2, 0, 0)), (61, (2, 0, 0)), (70, (0.3, 0, 0))],
        "StepR": [(19, g(0, 0, 0)), (25, g(0.006, 0.0, 0.012)), (30, g(0.012, 0.01, 0)), (49, g(0.012, 0.01, 0)),
                  (53, g(0.006, 0.005, 0.01)), (57, g(0, 0, 0))],
        "StepL": [(52, g(0, 0, 0)), (57, g(0, 0, 0.01)), (61, g(0, 0, 0))],
    }), spec, bones, grip_len=grip_len, report=report)

    # Step (1.2 s): the queue moves up. The weight onto the right foot; the left shuffles forward, low, the body gliding
    # after it (the Root travelling STEP along -Y, f5-f30), a dip as it lands; the right foot drags up to join it, the
    # body overshooting a little and settling back; the hem swings after it all. The elder plants its stick first.
    D = STEP
    out["Step"] = clip("Step", STEP_LEN, {
        "Travel": [(1, w(0)), (5, w(0)), (30, w(D)), (37, w(D))],
        "Hips": [(1, Z), (5, ((0, 1.5, 0), (0.018, 0.008, -0.006))), (12, ((0, 0.5, 0), (0.01, -0.15, 0.006))),
                 (16, ((0, -0.4, 0), (-0.002, -0.27, -0.004))), (19, ((0, -1, 0), (-0.006, -0.35, -0.014))),
                 (24, ((0, -1.6, 0), (-0.012, -0.43, -0.004))),
                 (30, ((0, -0.5, 0), (-0.004, -D - 0.006, 0.002))), (37, ((0, 0, 0), (0, -D, 0)))],
        "StepL": [(1, g(0, 0, 0)), (6, g(0, 0, 0)), (11, g(0, -0.1, 0.022)), (16, g(0, -0.31, 0.02)), (19, g(0, -D, 0)),
                  (37, g(0, -D, 0))],
        "StepR": [(1, g(0, 0, 0)), (18, g(0, 0, 0)), (23, g(0, -0.13, 0.015)), (28, g(0, -0.38, 0.012)), (31, g(0, -D, 0)),
                  (37, g(0, -D, 0))],
        "Spine": [(1, (0, 0, 0)), (8, (2.5, 0.5, 0)), (16, (4.5, -0.8, 0)), (24, (2, -0.5, 0)), (31, (-1, 0, 0)), (37, (0, 0, 0))],
        "Chest": [(1, (0, 0, 0)), (9, (2, 0.6, 0)), (18, (4, -1, 0)), (26, (1, -0.3, 0)), (33, (-1, 0, 0)), (37, (0, 0, 0))],
        "Head": [(1, (0, 0, 0)), (11, (-3, 0, 0)), (20, (1, 0, 0)), (27, (3, 0, 0)), (34, (-0.8, 0, 0)), (37, (0, 0, 0))],
        "Eyes": [(1, Z)],
        "ArmL": [(1, (0, 0, 0)), (10, (-5, 0, 0)), (20, (4, 0, 0)), (29, (-2, 0, 0)), (37, (0, 0, 0))],
        "ForeL": [(1, (0, 0, 0)), (13, (-5, 0, 0)), (23, (4, 0, 0)), (32, (-1, 0, 0)), (37, (0, 0, 0))],
        "ArmR": [(1, (0, 0, 0)), (10, (4, 0, 0)), (20, (-5, 0, 0)), (29, (2, 0, 0)), (37, (0, 0, 0))],
        "ForeR": [(1, (0, 0, 0)), (13, (4, 0, 0)), (23, (-5, 0, 0)), (32, (1, 0, 0)), (37, (0, 0, 0))],
        "PropAdj": [(1, Z), (14, ((0, 0, 0), (0, 0, 0.01))), (21, ((2, 0, 0), (0, 0, -0.012))), (27, ((-1, 0, 0), (0, 0, 0.004))),
                    (33, Z), (37, Z)],
        "Tip": [(1, g(0, 0, 0)), (3, g(0, 0, 0)), (7, g(0, -0.1, 0.06)), (12, g(0, -D, 0.025)), (14, g(0, -D, 0)), (37, g(0, -D, 0))],
    }, spec, bones, grip_len=grip_len, report=report)

    # Bow (1.6 s): a small lift first (drawing itself up), then the shoulders bow and the head bows lower after them (the
    # hips easing back against it), the eyes closing; held a beat; it straightens, the head last, a touch past upright,
    # and settles.
    out["Bow"] = clip("Bow", BOW, keys(held(NEUTRAL, 1), held(NEUTRAL, BOW + 1), {
        "Hips": [(10, ((0, 0, 0), (0, 0.01, -0.004))), (22, ((0, 0, 0), (0, 0.022, -0.008))), (30, ((0, 0, 0), (0, 0.02, -0.007))),
                 (42, ((0, 0, 0), (0, 0.004, 0)))],
        "Spine": [(8, (-1, 0, 0)), (18, (7, 0, 0)), (28, (8, 0, 0)), (38, (1, 0, 0)), (44, (-0.6, 0, 0))],
        "Chest": [(9, ((-1.5, 0, 0), (0, 0, 0.004))), (19, (10, 0, 0)), (29, (11, 0, 0)), (39, (2, 0, 0)), (45, (-0.8, 0, 0))],
        "Head": [(11, (-2, 0, 0)), (22, (20, 0, 0)), (31, (22, 0, 0)), (41, (4, 0, 0)), (46, (-1.5, 0, 0))],
        "Eyes": [(20, Z), (24, closed), (31, closed), (35, Z)],
        "ArmL": [(20, (-4, 0, 0)), (32, (-4, 0, 0)), (44, (0.8, 0, 0))],
        "ForeL": [(23, (-5, 0, 0)), (34, (-4, 0, 0)), (46, (1, 0, 0))],
        "ArmR": [(20, (-4, 0, 0)), (32, (-4, 0, 0)), (44, (0.8, 0, 0))],
        "ForeR": [(23, (-5, 0, 0)), (34, (-4, 0, 0)), (46, (1, 0, 0))],
    }), spec, bones, grip_len=grip_len, report=report)
    return out


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


def ramp(z, lo, hi):
    return max(0.0, min(1.0, (z - lo) / (hi - lo)))


def by_height(co):
    """The robe: the skirt swings from the waist (Hem), the body above bends with the spine and chest."""
    hem = 1 - ramp(co.z, 0.35, 0.8)
    chest = ramp(co.z, 1.1, 1.24)
    spine = ramp(co.z, 0.9, 1.02) * (1 - chest)
    return {"Hem": hem, "Hips": max(0.0, 1 - hem - spine - chest), "Spine": spine, "Chest": chest}


def ring_tube(rings, seg=14):
    """A tube through rings (centre, half-width, half-depth), capped."""
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


# (z, half-width, half-depth): a bell from the hem, drawn in at the waist, the chest, sloping shoulders, the neck.
ROBE = [(0.05, 0.315, 0.29), (0.18, 0.29, 0.268), (0.4, 0.25, 0.228), (0.65, 0.218, 0.195), (0.9, 0.196, 0.168),
        (1.08, 0.203, 0.166), (1.22, 0.205, 0.158), (1.31, 0.172, 0.13), (1.38, 0.1, 0.085), (1.42, 0.03, 0.03)]


def hood_shape(bm):
    """The hood: a ball drawn back and up behind into its point, its brow a little forward over the face."""
    kit.sphere(14, 10)(bm)
    for v in bm.verts:
        x, y, z = v.co
        if z > 0.1 and y > -0.4:
            t = (z - 0.1) / 0.9
            v.co.y += 0.2 * t * (y + 0.4) / 1.4
            v.co.z += 0.05 * t
        if y < -0.45 and z > 0.3:
            v.co.y -= 0.12 * (z - 0.3)


def body(variant, spec, robe, shade, void, eye, hand):
    W = spec["W"]
    parts = []
    ob = part("Robe", ring_tube([((0, 0, z), rx * W, ry * W) for z, rx, ry in ROBE]), robe, None, smooth=True, jitter=0.006)
    parts.append(weigh(ob, by_height))
    ob = part("RobeHem", kit.cone(14, 0.322, 0.31, 0.05), shade, M((0, 0, 0.07), scale=(W, W * 0.92, 1)), jitter=0.004)
    parts.append(weigh(ob, by_height))
    # (a cord at the waist, its two ends hanging at the front)
    belt = [(0.198 * W * math.cos(a), 0.17 * W * math.sin(a), 0.93) for a in (2 * math.pi * i / 16 for i in range(17))]
    parts.append(weigh(part("Cord", kit.tube(belt, [0.014] * len(belt), seg=5), shade, None), by_height))
    for x, dz in ((-0.03, 0.2), (0.02, 0.15)):
        ob = part("CordEnd", kit.tube([(x, -0.17 * W, 0.93), (x - 0.01, -0.18 * W, 0.93 - dz * 0.5), (x - 0.005, -0.185 * W, 0.93 - dz)],
                                      [0.012, 0.011, 0.016], seg=5), shade, None)
        parts.append(weigh(ob, by_height))
    if variant != "elder":   # (the elder's shawl is its mantle)
        parts.append(part("Mantle", kit.cone(14, 0.255, 0.12, 0.17), robe, M((0, 0.005, 1.3), scale=(W, W * 0.86, 1)),
                          jitter=0.005, group="Chest"))
        parts.append(part("MantleEdge", kit.cone(14, 0.262, 0.25, 0.03), shade, M((0, 0.005, 1.225), scale=(W, W * 0.86, 1)),
                          group="Chest"))

    # The hood: round about the neck (it turns with the head), a rim round the void of the face, two faint eyes in it,
    # a soft point drooping behind.
    def hp(p):
        return head_point(p, spec)
    k = spec["HEAD"]
    parts.append(part("Hood", hood_shape, shade, M(hp(HOOD_C), scale=tuple(r * k for r in HOOD_R)), smooth=True,
                      jitter=0.003, group="Head"))
    parts.append(part("Face", kit.sphere(12, 9), void, M(hp(FACE_C), scale=tuple(r * k for r in FACE_R)), smooth=True,
                      group="Head"))
    rim, rims = [], []
    for i in range(15):
        a = 2 * math.pi * i / 14
        x, z = 0.106 * math.cos(a), FACE_C[2] + 0.116 * math.sin(a)
        u = 1 - (x / HOOD_R[0]) ** 2 - ((z - HOOD_C[2]) / HOOD_R[2]) ** 2
        y = HOOD_C[1] - HOOD_R[1] * math.sqrt(max(0.0, u))
        brow = max(0.0, math.sin(a))
        rim.append(hp((x, y - 0.004 - 0.02 * brow ** 2, z + 0.006 * brow)))
        rims.append((0.012 + 0.016 * brow ** 1.5) * k)
    parts.append(part("HoodRim", kit.tube(rim, rims, seg=6), shade, None, smooth=True, group="Head"))
    for s in (-1, 1):
        parts.append(part("Eye", kit.sphere(8, 6), eye, M(hp(EYE[s]), scale=(0.016 * k, 0.01 * k, 0.019 * k)), smooth=True,
                          group="Eyes"))
    tip = spec["TIP"]
    if tip:
        pts = [(0, 0.09, 1.585), (0, 0.09 + 0.1 * tip, 1.575 - 0.01 * tip), (0, 0.09 + 0.16 * tip, 1.55 - 0.07 * tip)]
        parts.append(part("HoodTip", kit.tube([hp(p) for p in pts], [0.075 * k, 0.04 * k, 0.014 * k], seg=7), shade, None,
                          smooth=True, group="Head"))
        if variant == "child":
            parts.append(part("Bobble", kit.ico(1, 0.035 * k), robe, M(hp(pts[-1])), group="Head"))

    # The arms: a sleeve to the elbow, a bell of a sleeve to the wrist, a small pale hand.
    for s, side in SIDES:
        parts.append(part("Sleeve", kit.cone(8, 0.066, 0.056, (V(EL[s]) - V(SH[s])).length), robe, kit.along(SH[s], EL[s]),
                          smooth=True, group="Arm" + side))
        parts.append(part("Elbow", kit.sphere(8, 6, 0.056), robe, M(EL[s]), smooth=True, group="Arm" + side))
        cuff = V(EL[s]).lerp(V(WR[s]), 0.9)
        parts.append(part("Cuff", kit.cone(8, 0.055, 0.074, (cuff - V(EL[s])).length), robe, kit.along(EL[s], cuff),
                          smooth=True, group="Fore" + side))
        parts.append(part("CuffEdge", kit.cone(8, 0.077, 0.077, 0.02), shade, kit.along(cuff - (cuff - V(EL[s])).normalized() * 0.01, cuff),
                          group="Fore" + side))
        parts.append(part("Hand", kit.sphere(8, 6), hand, M(WR[s], scale=(0.034, 0.032, 0.042)), smooth=True, group="Fore" + side))
        # (the feet: two dark toes under the hem)
        f = FOOT[s]
        parts.append(part("Foot", kit.sphere(8, 5), void, M((f[0], f[1] - 0.06, 0.028), scale=(0.045, 0.075, 0.03)), smooth=True,
                          group="Foot" + side))
    return parts


def props(variant, spec, bones):
    """What it carries (built in the canonical rest frame, like the body)."""
    parts = []
    P = "Prop"
    if variant == "bundle":
        wood, cloth, knot = kit.mat("SoulWood", "#6b6158"), kit.mat("SoulBundle", "#93787a"), kit.mat("SoulBundleKnot", "#7b6266")
        patch = kit.mat("SoulPatch", "#8a8f9e")
        a, b = V((0.2, -0.32, 1.115)), V((0.21, 0.54, 1.545))
        parts.append(part("Stick", kit.cylinder(6, 0.016, (b - a).length), wood, kit.along(a, b), group=P))
        dh, dt, _ = canon_bones(spec)["Dangle"]
        c = V(dt)
        parts.append(part("Bundle", kit.sphere(10, 8), cloth, M(c, rot=(0, 0, 20), scale=(0.15, 0.135, 0.125)), smooth=True,
                          jitter=0.01, group="Dangle"))
        parts.append(part("BundlePatch", kit.cube(), patch, M(c + V((0.0, -0.12, 0.01)), rot=(-20, 0, 14), scale=(0.07, 0.03, 0.07)),
                          group="Dangle"))
        parts.append(part("BundleNeck", kit.cone(7, 0.06, 0.03, 0.08), cloth, M(c + V((0, 0, 0.14))), group="Dangle"))
        parts.append(part("BundleKnot", kit.sphere(7, 5, 0.035), knot, M(V(dh) - V((0, 0, 0.02))), smooth=True, group="Dangle"))
        for s in (-1, 1):
            parts.append(part("BundleEar", kit.cone(5, 0.025, 0.0, 0.08), knot,
                              kit.along(V(dh) - V((0, 0, 0.02)), V(dh) + V((s * 0.07, 0.02, 0.03))), group="Dangle"))
    elif variant == "trunk":
        leather, strap = kit.mat("SoulCase", "#665650"), kit.mat("SoulCaseStrap", "#4a3f3d")
        brass, paper = kit.mat("SoulCaseCorner_Metal", "#8f8467"), kit.mat("SoulTag", "#d4d0c4")
        c = V(spec["PROP"])
        parts.append(part("Case", kit.cube(), leather, M(c, scale=(0.42, 0.18, 0.3)), group=P))
        parts.append(part("CaseLid", kit.cube(), strap, M(c + V((0, 0, 0.06)), scale=(0.43, 0.19, 0.02)), group=P))
        for x in (-0.11, 0.11):
            parts.append(part("CaseStrap", kit.cube(), strap, M(c + V((x, 0, 0)), scale=(0.04, 0.19, 0.31)), group=P))
        for x in (-0.205, 0.205):
            for z in (-0.14, 0.14):
                parts.append(part("CaseCorner", kit.cube(), brass, M(c + V((x, 0, z)), scale=(0.025, 0.19, 0.03)), group=P))
        handle = [c + V((-0.07, 0, 0.15)), c + V((-0.06, 0, 0.2)), c + V((0.06, 0, 0.2)), c + V((0.07, 0, 0.15))]
        parts.append(part("CaseHandle", kit.tube(handle, [0.013] * 4, seg=5), strap, None, group=P))
        parts.append(part("CaseTagString", kit.cube(), paper, M(c + V((0.06, -0.06, 0.15)), rot=(0, 30, 0), scale=(0.004, 0.004, 0.06)),
                          group=P))
        parts.append(part("CaseTag", kit.cube(), paper, M(c + V((0.08, -0.1, 0.11)), rot=(-12, 25, -10), scale=(0.05, 0.004, 0.08)),
                          group=P))
    elif variant == "elder":
        wood, shawl = kit.mat("SoulWood", "#6b6158"), kit.mat("SoulShawl", "#8c8899")
        fringe = kit.mat("SoulShawlEdge", "#6d6a7b")
        tip = V(spec["PROP"])
        L = spec["_GRIP_LEN"] / spec["S"]
        top = tip + V((0, 0, L + 0.05))
        parts.append(part("Stick", kit.cone(6, 0.014, 0.019, L + 0.05), wood, kit.along(tip, top), group=P))
        crook = [top - V((0, 0, 0.01)), top + V((0, -0.03, 0.04)), top + V((0, -0.09, 0.03)), top + V((0, -0.1, -0.01))]
        parts.append(part("StickCrook", kit.tube(crook, [0.02, 0.02, 0.018, 0.016], seg=6), wood, None, group=P))
        parts.append(part("StickFerrule", kit.cylinder(6, 0.017, 0.04), fringe, M(tip + V((0, 0, 0.02))), group=P))

        # The shawl over the shoulders, its point hanging down the back; a knot at the front where the left hand holds it.
        def drape(bm):
            kit.cone(16, 0.3, 0.155, 0.24)(bm)
            for v in bm.verts:
                if v.co.z < 0 and v.co.y > 0.02:
                    drop = 0.3 * (v.co.y / 0.3) ** 1.6
                    v.co.z -= drop
                    v.co.y -= drop * 0.55     # (drawn in against the back as it hangs)
                if v.co.z < 0 and v.co.y < -0.15:
                    v.co.z += 0.04
        ob = part("Shawl", drape, shawl, M((0, 0.01, 1.27), scale=(1.0, 0.88, 1)), jitter=0.006)
        parts.append(weigh(ob, lambda co: {"Chest": ramp(co.z, 0.98, 1.16), "Spine": 1 - ramp(co.z, 0.98, 1.16)}))
        ob = part("ShawlEdge", kit.cone(16, 0.305, 0.29, 0.025), fringe, M((0, 0.01, 1.15), scale=(1.0, 0.88, 1)))
        parts.append(weigh(ob, lambda co: {"Chest": 1.0}))
        parts.append(part("ShawlKnot", kit.sphere(7, 5), shawl, M((-0.02, -0.2, 1.24), scale=(0.045, 0.035, 0.04)), smooth=True,
                          group="Chest"))
        for x in (-0.04, 0.0):
            parts.append(part("ShawlTail", kit.cone(5, 0.025, 0.006, 0.12), shawl,
                              kit.along((x - 0.01, -0.21, 1.22), (x - 0.03, -0.22, 1.1)), group="Chest"))
    elif variant == "child":
        hull, sail, mast = kit.mat("SoulToyBoat", "#7d6c62"), kit.mat("SoulToySail", "#d6d3ca"), kit.mat("SoulWood", "#6b6158")
        c = V(spec["PROP"])

        def boat(bm):
            pts = [(-0.17, 0.0), (-0.12, 0.06), (0.1, 0.065), (0.17, 0.0), (0.1, -0.065), (-0.12, -0.06)]
            kit.prism(pts, 0.07)(bm)
            for v in bm.verts:
                if v.co.z < 0.01:
                    v.co.x *= 0.8
                    v.co.y *= 0.55
        parts.append(part("ToyHull", boat, hull, M(c - V((0, 0, 0.04))), group=P))
        parts.append(part("ToyDeck", kit.cube(), mast, M(c + V((0, 0, 0.032)), scale=(0.24, 0.09, 0.008)), group=P))
        parts.append(part("ToyMast", kit.cylinder(5, 0.008, 0.24), mast, M(c + V((0.01, 0, 0.15))), group=P))
        parts.append(part("ToySail", kit.prism([(0, 0), (0.12, 0), (0, 0.18)], 0.006), sail,
                          M(c + V((0.02, 0.003, 0.06)), rot=(90, 0, 0)), group=P))
        parts.append(part("ToyFlag", kit.prism([(0, 0), (0.04, 0.012), (0, 0.024)], 0.004), hull,
                          M(c + V((0.01, 0.002, 0.25)), rot=(90, 0, 0)), group=P))
    elif variant == "hat":
        felt, band = kit.mat("SoulHat", "#4f5361"), kit.mat("SoulHatBand", "#6f6680")
        stems, ribbon = kit.mat("SoulStems", "#6c7468"), kit.mat("SoulRibbon", "#a59bb3")
        blooms = [kit.mat("SoulBloom", "#c9bfd2"), kit.mat("SoulBloomPale", "#dcd8cf"), kit.mat("SoulBloomDusk", "#a99ab0")]

        def hp(p):
            return head_point(p, spec)
        parts.append(part("HatBrim", kit.cone(18, 0.3, 0.16, 0.035), felt, M(hp((0, -0.02, 1.598)), rot=(5, 0, 0)), jitter=0.003,
                          group="Head"))
        parts.append(part("HatCrown", kit.cone(12, 0.135, 0.112, 0.13), felt, M(hp((0, -0.017, 1.68)), rot=(5, 0, 0)), group="Head"))
        parts.append(part("HatTop", kit.cone(12, 0.112, 0.07, 0.02), felt, M(hp((0, -0.01, 1.752)), rot=(5, 0, 0)), group="Head"))
        parts.append(part("HatBand", kit.cylinder(12, 0.138, 0.032), band, M(hp((0, -0.019, 1.635)), rot=(5, 0, 0)), group="Head"))
        c = V(spec["PROP"])
        parts.append(part("PosyStems", kit.cone(6, 0.012, 0.03, 0.2), stems, kit.along(c + V((0, 0.0, -0.11)), c + V((0, -0.05, 0.06))),
                          group=P))
        parts.append(part("PosyRibbon", kit.cylinder(7, 0.03, 0.025), ribbon, M(c + V((0, -0.005, -0.01)), rot=(-14, 0, 0)), group=P))
        for i, (dx, dy, dz) in enumerate(((0, -0.06, 0.09), (-0.04, -0.04, 0.075), (0.04, -0.05, 0.08), (-0.015, -0.09, 0.065),
                                          (0.025, -0.02, 0.1), (0.0, -0.03, 0.115))):
            parts.append(part("PosyBloom", kit.ico(1, 0.026), blooms[i % 3], M(c + V((dx, dy, dz))), group=P))
    elif variant == "blanket":
        wool, stripe = kit.mat("SoulBlanket", "#857b92"), kit.mat("SoulBlanketStripe", "#5c6679")
        cord = kit.mat("SoulCord", "#575160")
        c = V(spec["PROP"])
        a, b = c + V((0, -0.27, -0.045)), c + V((0, 0.25, 0.045))
        ax = (b - a).normalized()
        parts.append(part("Roll", kit.cylinder(10, 0.088, (b - a).length), wool, kit.along(a, b), jitter=0.004, group=P))
        for e, sgn in ((a, -1), (b, 1)):
            parts.append(part("RollEnd", kit.cylinder(10, 0.06, 0.01), stripe, kit.along(e, e + ax * sgn * 0.006), group=P))
            parts.append(part("RollCore", kit.cylinder(8, 0.028, 0.01), wool, kit.along(e, e + ax * sgn * 0.009), group=P))
        for t in (-0.15, 0.13):
            parts.append(part("RollCord", kit.cylinder(10, 0.093, 0.018), cord, kit.along(c + ax * t, c + ax * (t + 0.018)), group=P))
        parts.append(part("RollStripe", kit.cylinder(10, 0.091, 0.05), stripe, kit.along(c + ax * -0.03, c + ax * 0.02), group=P))
    return parts


def build(variant):
    spec = SPECS[variant]
    S = spec["S"]
    robe, shade = kit.mat("SoulRobe", spec["robe"]), kit.mat("SoulRobeShade", spec["shade"])
    void, eye = kit.mat("SoulVoid", "#0b0c12"), kit.mat("SoulEye_Glow", "#9ccbe8")
    hand = kit.mat("SoulHand", "#c2cad6")
    bones = scaled_bones(spec)

    grip_len = 0.0
    if spec["CARRY"] == "stick":
        # (the stick's length to the hand: from its tip to where the chest carries the hand in the first pose)
        m = kit.pose_matrices(bones, body_pose({}, spec))
        grip_len = (m["Chest"] @ (V(spec["GRIP"]) * S) - V(bones["Prop"][0])).length
        spec["_GRIP_LEN"] = grip_len

    report = {}
    acts = actions(spec, bones, report, grip_len)

    parts = body(variant, spec, robe, shade, void, eye, hand) + props(variant, spec, bones)
    mesh = kit.join(parts, "Soul")
    mesh.data.transform(Matrix.Scale(S, 4))
    armature = kit.rig("SoulRig", bones)
    kit.skin(mesh, armature)
    kit.animate(armature, acts)
    for name, worst in report.items():
        if worst > 0.02:
            print(f"[soul] {variant} {name}: a hand comes off its grip by up to {worst * 100:.0f} cm")
    bpy.context.scene.frame_set(1)
    bpy.context.view_layer.update()
    dg = bpy.context.evaluated_depsgraph_get()
    ev = mesh.evaluated_get(dg)
    me = ev.to_mesh()
    top = max((ev.matrix_world @ v.co).z for v in me.vertices)
    ev.to_mesh_clear()
    print(f"[soul] {variant}: {top:.2f} m tall as it stands; hands on their grips to {max(report.values()) * 100:.1f} cm")
    return [armature, mesh]
