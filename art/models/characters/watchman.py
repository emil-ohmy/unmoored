"""The Watchman (the Orchard's miniboss: Abner Wick, Who Merits Flame), and the scarecrows he stands among (the same figure):
a coat stuffed with straw, a hat, a sack for a face with two stitched eyes, arms out along the crossbar, a reaping hook
in the right hand, a lantern (dark) hanging from the left. No legs: the coat hangs to the knees, straw poking out at
the cuffs and hem (the pole it stands on is a prop of its own, so he can tear free of it). Origin at the ground.

Clips (what each does is said above it): Idle (the scarecrows' too: on a stake in the wind), Lurch (a jerky hop
forward; the game plays 0.22 s of it, then Idle, again and again), Windup (0.6 s) / Swing (0.3 s) / Recover (0.9 s: the
hook; Recover also follows his fire), Reveal (1.2 s: he tears free), Thrash (alight: flailing, looping), Breathe (1 s:
the lantern to his face, drawing breath over its flame), Hold (1.9 s: the same, held: the feint), Spew (0.4 s, or 2 s
for three small ones), Stagger, Death (he slumps down into his own straw).

How it's animated (as the Laden): each bone keyed on its own timing (kit.bake: tracks), so nothing moves all at once:
the stake-stiff body leads (Root, at the ground, then Body, from the waist), the sack head lolls after it, the arms
flap after that. The loose things aren't keyed so much as shaken: the hat on his head, the coat's skirt (Hem), the
straw at his cuffs and the lantern on its cord are each sprung on what carries them (secondary(): they lag, overshoot
and settle; the lantern swings as a pendulum on its cord). What's keyed on them is only intent (the hat jumping off his
head as he tears free, the wind in the coat). Watched, he freezes (the game stops his clip where it is), so every frame
of every clip is a pose that reads as a statue: a scarecrow caught mid-lurch. Clips join as they're played: Windup's
end is Swing's start (and its loose things carry on), Swing and Spew end on the same spent pose Recover starts from
(their loose things settled), Breathe and Hold end on the pose Spew starts from; Recover, Reveal and Stagger end on the
rest pose Idle and Lurch start from.

The game hangs the lantern's light (and his fire) on ArmL where the lantern's glass is at rest: so when he breathes
fire he grips the lantern (it goes where his arm takes it, `_grip` 1) and only lets it swing once it's loosed. The hook
is fixed on ArmR (its swipe trail is hung on ArmR): the arms are single bones.
"""
import math

import bmesh
from mathutils import Quaternion, Vector

M, part = kit.M, kit.part  # `kit` is injected by build_model.py

SHOULDER = 1.52
HAND_L = (-0.78, 0, SHOULDER - 0.09)
BONES = {
    "Root": ((0, 0, 0), (0, 0, 0.3), None),
    "Body": ((0, 0, 0.75), (0, 0, SHOULDER), "Root"),
    "Head": ((0, 0, SHOULDER), (0, 0, 2.0), "Body"),
    "ArmL": ((-0.16, 0, SHOULDER - 0.04), (-0.82, 0, SHOULDER - 0.08), "Body"),
    "ArmR": ((0.16, 0, SHOULDER - 0.04), (0.82, 0, SHOULDER - 0.08), "Body"),
    # The loose things (sprung: see secondary()).
    "Hat": ((0, 0.01, 1.9), (0, 0.04, 2.16), "Head"),               # his hat, sat on the sack
    "Hem": ((0, 0, 1.22), (0, 0, 0.7), "Body"),                      # the coat's skirt, from the waist down
    "Lantern": (HAND_L, (-0.78, 0, 1.0), "ArmL"),                    # the lantern on its cord, from his left hand
    "CuffL": ((-0.74, 0, SHOULDER - 0.08), (-0.92, 0, SHOULDER - 0.08), "ArmL"),  # the straw out of his cuffs
    "CuffR": ((0.74, 0, SHOULDER - 0.08), (0.92, 0, SHOULDER - 0.08), "ArmR"),
}
GLASS = Vector((-0.78, 0, SHOULDER - 0.45))  # the lantern's glass at rest (where the game hangs its light, on ArmL)

# How each loose bone is sprung on what carries it: f (Hz) and zeta (its damping) of the spring to where it's carried,
# `inertia` how much the carrier's lurches throw it, `wind` how much the wind does, `limit` (degrees) how far it can go.
SPRINGS = {
    "Hat": dict(f=3.2, zeta=0.32, inertia=1.0, wind=0.1, limit=30, length=0.2),
    "Hem": dict(f=2.4, zeta=0.3, inertia=0.9, wind=1.0, limit=28, length=0.45),
    "CuffL": dict(f=5.5, zeta=0.25, inertia=0.8, wind=1.5, limit=30, length=0.15),
    "CuffR": dict(f=5.5, zeta=0.25, inertia=0.8, wind=1.5, limit=30, length=0.15),
}
LANTERN = dict(length=0.4, decay=0.6, limit=70)  # a pendulum: its cord, how long its swing takes to die (s), how far
FPS, SUB = 30.0, 8


def _deg(q):
    return tuple(math.degrees(a) for a in q.to_euler("XYZ"))


def secondary(frames, periodic, settle=0, start=None):
    """The loose bones sprung on what carries them, every frame (frames as kit.bake gives them; each loose bone's own
    key is its intent: where it would sit if it weren't loose). `_grip` (0..1): how firmly his hand holds the lantern
    (1: it goes where his arm goes; 0: it swings on its cord). `_wind` (m/s^2, armature axes): the wind on it all.
    `settle`: over a one-off's last frames, everything loose comes to rest where it's carried (the loop that follows
    picks it up from there). `start`: how the loose things were left by the clip before (what end() gave), to carry on
    from; else they start at rest. Returns the frames and how it leaves them."""
    n = len(frames) - 1 if periodic else len(frames)  # (a loop's last frame is its first again)
    mats, target, head, grip, wind = [], {}, {}, [], []
    for _, pose in frames[:n]:
        m = kit.pose_matrices(BONES, pose)
        mats.append(m)
        grip.append(min(1.0, max(0.0, pose.get("_grip", ((0, 0, 0), (0, 0, 0)))[0][0])))
        wind.append(Vector(pose.get("_wind", ((0, 0, 0), (0, 0, 0)))[0]))
        for b in list(SPRINGS) + ["Lantern"]:
            target.setdefault(b, []).append(m[b].to_quaternion())
            head.setdefault(b, []).append(m[b] @ Vector(BONES[b][0]))

    def at(seq, i):
        return seq[i % n] if periodic else seq[max(0, min(n - 1, i))]

    dt = 1.0 / FPS / SUB
    result = {b: [None] * n for b in list(SPRINGS) + ["Lantern"]}
    state = {}
    passes = 3 if periodic else 1
    for b, s in SPRINGS.items():
        w0 = 2 * math.pi * s["f"]
        k, c = w0 * w0, 2 * s["zeta"] * w0
        rest_dir = (Vector(BONES[b][1]) - Vector(BONES[b][0])).normalized()
        q, w = target[b][0].copy(), Vector((0, 0, 0))
        if start:
            q, w = start[b][0].copy(), start[b][1].copy()
        for p in range(passes):
            for i in range(n):
                if p == passes - 1:
                    result[b][i] = q.copy()
                    if i == n - 1:
                        state[b] = (q.copy(), w.copy())
                acc = (at(head[b], i + 1) - 2 * at(head[b], i) + at(head[b], i - 1)) * FPS * FPS
                if not periodic and (i == 0 or i == n - 1):
                    acc = Vector((0, 0, 0))
                force = -acc * s["inertia"] + at(wind, i) * s["wind"]
                for j in range(1, SUB + 1):
                    t = at(target[b], i).slerp(at(target[b], i + 1) if (periodic or i + 1 < n) else at(target[b], i), j / SUB)
                    e = t @ q.inverted()
                    if e.w < 0:
                        e = -e
                    axis, angle = e.to_axis_angle()
                    r = q @ rest_dir
                    alpha = axis * (angle * k) - w * c + r.cross(force) / s["length"]
                    w = w + alpha * dt
                    if w.length > 1e-9:
                        q = Quaternion(w.normalized(), w.length * dt) @ q
                    q.normalize()
                    off = (t.inverted() @ q)
                    if off.w < 0:
                        off = -off
                    lim = math.radians(s["limit"])
                    if 2 * math.acos(min(1.0, off.w)) > lim:
                        q = t.slerp(q, lim / (2 * math.acos(min(1.0, off.w))))
                        w = w * 0.5
    # The lantern: a weight on a cord from his hand (Verlet), swinging after the hand, wind-blown, gripped when held.
    L = LANTERN["length"]
    down = (Vector(BONES["Lantern"][1]) - Vector(BONES["Lantern"][0])).normalized()
    damp = 1 - math.exp(-dt / LANTERN["decay"])
    x = head["Lantern"][0] + (target["Lantern"][0] @ down) * L
    xp = x.copy()
    if start:
        x, xp = head["Lantern"][0] + start["Lantern"][0], head["Lantern"][0] + start["Lantern"][1]
    DOWN, limit = Vector((0, 0, -1)), math.radians(LANTERN["limit"])
    for p in range(passes):
        for i in range(n):
            pivot = head["Lantern"][i]
            rigid = target["Lantern"][i] @ down
            d = (x - pivot).normalized()
            if p == passes - 1:
                free = rigid.rotation_difference(d) @ target["Lantern"][i]
                result["Lantern"][i] = free.slerp(target["Lantern"][i], grip[i])
                if i == n - 1:
                    state["Lantern"] = (x - pivot, xp - pivot)
            # (a held lantern goes with the hand: the weight is carried where the hand puts it)
            if grip[i] > 0.5:
                x = pivot + rigid * L
                xp = x.copy()
            nxt = at(head["Lantern"], i + 1) if (periodic or i + 1 < n) else pivot
            for j in range(1, SUB + 1):
                pv = pivot.lerp(nxt, j / SUB)
                v = (x - xp) * (1 - damp)
                xp = x
                x = x + v + (Vector((0, 0, -9.81)) + at(wind, i)) * dt * dt
                x = pv + (x - pv).normalized() * L
                # (it can't swing up past this, the cord being on his hand: there it stops dead)
                d = (x - pv).normalized()
                if d.angle(DOWN, 0) > limit:
                    x = pv + DOWN.slerp(d, limit / d.angle(DOWN)) * L
                    xp = x - (x - xp) * 0.3
    for i in range(max(0, n - settle), n if settle else 0):
        t = (i - (n - settle) + 1) / settle
        for b in result:
            result[b][i] = result[b][i].slerp(target[b][i], t * t * (3 - 2 * t))
    out = []
    for i, (f, pose) in enumerate(frames):
        pose = {b: v for b, v in pose.items() if not b.startswith("_")}
        k = i % n
        for b in result:
            parent = mats[k][BONES[b][2]].to_quaternion()
            pose[b] = (_deg(parent.inverted() @ result[b][k]), tuple(pose.get(b, Z)[1]))
        out.append((f, pose))
    return out, state


REPORT = {}  # (the build's report: how far the lantern's glass is from where the game hangs its light, at worst)


ENDS = {}  # (how each clip leaves its loose things: the clip that follows it in play carries on from there)


def clip(length, tracks, periodic=False, name="", lit=(), settle=0, after=None, loose_loop=None):
    """A clip `length` frames long (30 a second) from per-bone tracks, baked to every frame, the loose things sprung.
    `lit`: the frames where the game's light (and his fire) should be on the lantern's glass (it's hung on ArmL).
    `after`: the clip it follows in play (its loose things carry on from how that one left them). `loose_loop`: False
    for a loop the game only ever plays a stretch of, from the start (its loose things start at rest, and come back)."""
    loop = periodic if loose_loop is None else loose_loop
    frames, ENDS[name] = secondary(kit.bake(tracks, length, periodic), loop, settle, ENDS.get(after))
    worst = 0.0
    for f, p in frames:
        if f in lit:
            m = kit.pose_matrices(BONES, p)
            worst = max(worst, (m["ArmL"] @ GLASS - m["Lantern"] @ GLASS).length)
    REPORT[name] = worst
    return ("LINEAR", frames)


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


# Arms out to the sides (a scarecrow's T). On ArmR: +Y drops it, -Y raises it, -Z swings it forward, +Z back, and X
# rolls it (the hook hanging from the hand: -X swings it forward, +X back). On ArmL the other way about: +Y raises it,
# +Z swings it forward.
Z = ((0, 0, 0), (0, 0, 0))
REST = {"Root": Z, "Body": (0, 0, 0), "Head": (0, 0, 0), "ArmL": (0, 0, 0), "ArmR": (0, 0, 0)}
# The hook drawn back high (the end of Windup, the start of Swing): up over his right shoulder, the blade cocked up, the
# body twisted away after it; the lantern arm thrown forward to balance; the head still on you.
WOUND = {"Root": ((0, 0, 0), (0, 0.04, 0.06)), "Body": (-8, -6, 20), "Head": (-6, 4, -20),
         "ArmR": (-35, -50, 4), "ArmL": (0, 14, 34)}
# Spent (the end of Swing and of Spew, the start of Recover): thrown forward, the hook across his front, low, the lantern
# arm dropped before him (rolled so the lantern hangs straight: held or let go, it's in the same place).
SPENT = {"Root": ((0, 0, 0), (0, -0.1, -0.04)), "Body": (14, 2, -16), "Head": (10, -2, 14),
         "ArmR": (-30, 14, -92), "ArmL": (-14, 0, 30)}
# Drawing breath (the end of Breathe and Hold, the start of Spew): leaning back, the lantern held up before his face,
# tipped towards it, the head back, looking up at it.
DRAW = {"Root": ((0, 0, 0), (0, 0.04, 0.07)), "Body": (-12, 0, -4), "Head": (-14, 0, 6),
        "ArmL": (0, 26, 98), "ArmR": (0, 20, 22)}


def mirror(rot):
    """ArmR's rotation for the same gesture on ArmL (and back)."""
    return (rot[0], -rot[1], -rot[2])


ACTIONS = {
    # Idle (2 s loop; the scarecrows play it too, so it's a scarecrow's: on its stake, in the wind). Barely a sway of the
    # body, the sack head lolling after it, and once a creak of the head round (a twitch: is it watching?); a gust
    # comes through (frame 34), pushing the arms back, flapping the coat and the straw at the cuffs, swinging the
    # lantern. Frame 1 is the rest pose (held still when he hides again).
    "Idle": clip(59, keys(held(REST, 1), {
        "Body": [(13, (1.2, 1.6, 0.5)), (25, (0.3, -0.5, -0.4)), (37, (2.6, 2.6, 1.0)), (43, (1.9, 2.1, 0.8)), (52, (-0.6, -1.2, -0.3))],
        "Head": [(6, (0, 0, 0)), (18, (-2, 2.5, 1.5)), (28, (1, -1.5, -1)), (30, (0.5, -1, 6)), (41, (-3, 3.5, 5)), (56, (1, -2, 1))],
        "ArmL": [(15, (0, -2, 1.5)), (27, (0, 1, -0.5)), (39, (0, -3, -3.5)), (45, (0, -2, -2.5)), (54, (0, 1.5, 0.5))],
        "ArmR": [(16, (0, 2, 1)), (28, (0, -1, 0.5)), (40, (0, 3, 4)), (46, (0, 2.2, 3)), (55, (0, -1.5, -0.5))],
        "Hem": [(1, (0, 0, 0)), (9, (-2, 1, 0)), (19, (1.5, -1.5, 0)), (29, (-1, 1, 0)), (37, (5, 2.5, 2)), (45, (2, -1, -1)), (52, (-1, 1, 0))],
        "CuffL": [(1, (0, 0, 0)), (6, (0, 4, 6)), (12, (0, -3, -2)), (20, (0, 5, 4)), (27, (0, -2, -5)), (36, (0, 7, -10)), (41, (0, -4, -4)), (48, (0, 5, -4)), (54, (0, -3, 3))],
        "CuffR": [(1, (0, 0, 0)), (4, (0, -3, 4)), (11, (0, 4, -3)), (18, (0, -5, 3)), (26, (0, 3, -4)), (37, (0, -6, 10)), (42, (0, 4, 5)), (49, (0, -5, 4)), (56, (0, 2, -2))],
        "_wind": [(1, (0, 0, 0)), (24, (0, 0.2, 0)), (34, (0.3, 2.2, 0)), (40, (0.2, 1.2, 0)), (48, (0, 0.2, 0))],
    }), periodic=True, name="Idle"),
    # Lurch (8 frames, looping; the game plays it 0.22 s at a time while he darts at you, then Idle). A jerky hop: he
    # sinks and rocks back (coiling, the arms coming up), throws himself forward and up (the arms flung down after, the
    # head jerked back), lands pitched over (a squash, the head snapping forward) and rocks back up; the coat and hat
    # bounce after him.
    "Lurch": clip(8, {
        "Root": [(1, Z), (3, ((-3, 0, 0), (0, 0.02, -0.07))), (5, ((7, 0, 0), (0, -0.06, 0.13))), (7, ((5, 0, 0), (0, -0.04, -0.05)))],
        "Body": [(1, (0, 0, 0)), (3, (-8, 0, 0)), (5, (16, 0, 0)), (6, (22, 0, 0)), (7, (14, 0, 0))],
        "Head": [(2, (0, 0, 0)), (4, (10, 0, 0)), (6, (-12, 0, 0)), (8, (10, 0, 0))],
        "ArmL": [(2, (0, 0, 0)), (4, (0, 18, -8)), (6, (0, -20, 6)), (8, (0, 6, 0))],
        "ArmR": [(2, (0, 0, 0)), (4, mirror((0, 18, -8))), (6, mirror((0, -20, 6))), (8, mirror((0, 6, 0)))],
    }, periodic=True, name="Lurch", loose_loop=False, settle=3),
    # Windup (0.6 s): the hook dips forward and down (a wind the other way), then is hauled up over his right shoulder,
    # cocked, the body twisting away after it and rising onto its toes (as it were), the lantern arm thrown forward to
    # balance; the head stays on you, turning against the body. Held there at the end, straining.
    "Windup": clip(14, keys(held(REST, 1), {
        "Root": [(4, ((0, 0, 0), (0, 0, -0.04))), (11, ((0, 0, 0), (0, 0.03, 0.05))), (15, WOUND["Root"])],
        "Body": [(4, (5, 0, -10)), (10, (-7, -5, 16)), (13, (-8, -6, 19)), (15, WOUND["Body"])],
        "Head": [(5, (5, 0, 8)), (11, (-4, 4, -16)), (15, WOUND["Head"])],
        "ArmR": [(4, (-12, 10, -20)), (8, (-14, -26, 12)), (12, (-32, -47, 6)), (15, WOUND["ArmR"])],
        "ArmL": [(5, (0, -4, 6)), (11, (0, 12, 30)), (15, WOUND["ArmL"])],
    }), name="Windup"),
    # Swing (0.3 s; the blow lands at 0.4 of it, frame 4): the body unwinds first, the arm whipping down and round after
    # it, the hook cocked out ahead; at frame 4 it's straight out before him, the body lunging after it; it carries on
    # across to his left, the body overshooting the turn and rebounding; the head follows the hook round.
    "Swing": clip(7, keys(held(WOUND, 1), held(SPENT, 8), {
        "Root": [(3, ((0, 0, 0), (0, -0.04, 0.0))), (4, ((4, 0, 0), (0, -0.14, -0.04)))],
        "Body": [(2, (-5, -4, 12)), (4, (12, 4, -22)), (6, (15, 4, -38))],
        "Head": [(3, (-2, 3, -12)), (5, (6, -2, 10)), (7, (10, -2, 20))],
        "ArmR": [(2, (-34, -42, 2)), (3, (-24, -12, -24)), (4, (-30, 6, -70)), (6, (-38, 14, -102))],
        "ArmL": [(3, (-4, 8, 14)), (5, (-10, 0, 18))],
    }), name="Swing", after="Windup", settle=3),
    # Recover (0.9 s; after the hook and after his fire): spent, he sags over (the body folding, the head hanging, the
    # arms dropping limp, the lantern let go: the opening), hangs there, then jerks himself back up onto his
    # stake-straight T, the arms flung up past it and dropping back, the head last.
    "Recover": clip(21, keys(held(SPENT, 1), held(REST, 22), {
        "Root": [(6, ((0, 0, 0), (0, -0.1, -0.07))), (12, ((0, 0, 0), (0, -0.08, -0.06))), (15, ((0, 0, 0), (0, -0.04, -0.09))), (18, ((0, 0, 0), (0, 0.02, 0.04)))],
        "Body": [(7, (22, 4, -12)), (12, (20, 3, -10)), (15, (18, 0, -5)), (18, (-6, 0, 2)), (20, (-2, 0, 0))],
        "Head": [(8, (34, -4, 10)), (13, (30, -2, 8)), (16, (26, 0, 4)), (19, (-10, 0, -2))],
        "ArmR": [(7, (-20, 32, -70)), (13, (-16, 34, -60)), (16, (-8, 20, -30)), (18, (4, -12, 4)), (20, (0, 4, 0))],
        "ArmL": [(7, (-10, -30, 20)), (13, (-6, -30, 16)), (16, (-2, -20, 8)), (18, (0, 12, -4)), (20, (0, -4, 0))],
        "_grip": [(1, (1, 0, 0)), (2, (1, 0, 0)), (4, (0, 0, 0))],
    }), name="Recover", settle=6),
    # Reveal (1.2 s: he tears free of his stake; again, alight, for round 3). A shiver, a sinking (gathering himself),
    # then a jolt up off the stake: the arms drop limp and the head lolls over, the hat jumping off his head; he drops
    # back down (a squash), the arms are flung up, and the head snaps round to you (overshooting); then he settles.
    "Reveal": clip(28, keys(held(REST, 1), held(REST, 29), {
        "Root": [(3, ((0, 0, 0), (0, 0, -0.03))), (5, ((0, 0, 0), (0, 0, -0.06))), (7, ((0, 0, 0), (0, 0.02, 0.3))), (9, ((0, 0, 0), (0, 0.0, 0.26))),
                 (12, ((0, 0, 0), (0, -0.02, -0.07))), (15, ((0, 0, 0), (0, 0, 0.02))), (19, ((0, 0, 0), (0, 0, -0.02)))],
        "Body": [(2, (1, -1.5, 0)), (3, (-1, 1.5, 0)), (4, (1, -1.5, 0)), (5, (4, 0, 0)), (7, (-14, 0, 0)), (10, (-6, 2, 0)), (12, (12, -2, 0)),
                 (16, (-4, 0, 4)), (20, (4, 0, 10)), (24, (-2, 0, -2))],
        "Head": [(3, (0, 0, 4)), (4, (0, 0, -3)), (6, (4, 0, 0)), (9, (30, -12, -30)), (13, (36, -16, -48)), (17, (32, -14, -52)),
                 (19, (6, 0, -6)), (20, (-6, 4, 12)), (22, (-4, 0, 6)), (25, (2, 0, -2))],
        "ArmL": [(5, (0, 4, 0)), (8, (0, -50, -6)), (11, (0, -62, 4)), (13, (0, -58, 0)), (16, (0, 30, 10)), (18, (0, 22, 6)), (21, (0, -6, 0)), (24, (0, 4, 0))],
        "ArmR": [(5, mirror((0, 4, 0))), (8, mirror((0, -50, -6))), (11, mirror((0, -62, 4))), (13, mirror((0, -58, 0))), (16, mirror((0, 30, 10))),
                 (18, mirror((0, 22, 6))), (21, mirror((0, -6, 0))), (24, mirror((0, 4, 0)))],
        "Hat": [(6, Z), (9, ((-8, 0, 0), (0, 0, 0.08))), (12, ((4, 0, 0), (0, 0, 0))), (15, Z)],
    }), name="Reveal", settle=6),
    # Thrash (8 frames, looping: alight, coming at you). Flailing: the arms windmill against each other, the body
    # rocking and bobbing under them (a hop each half), the head thrown about after it, everything loose flapping.
    "Thrash": clip(8, {
        "Root": [(1, ((0, 0, 0), (0, 0, 0.05))), (3, ((0, 0, 0), (0, 0, -0.04))), (5, ((0, 0, 0), (0, 0, 0.05))), (7, ((0, 0, 0), (0, 0, -0.04)))],
        "Body": [(1, (10, 11, 14)), (5, (10, -11, -14))],
        "Head": [(2, (-18, -14, -24)), (6, (-12, 14, 24))],
        "ArmL": [(2, (-30, 55, 40)), (6, (30, -40, -30))],
        "ArmR": [(2, (-30, 40, 30)), (6, (30, -55, -40))],
    }, periodic=True, name="Thrash"),
    # Breathe (1 s; 0.8 s for the small ones): a hunch over the lantern (gathering), then he rises and leans back, the
    # lantern brought up before his face (gripped: it stops swinging), the head tipping back after the body, drawing a
    # long breath over its flame (still rising, to the end).
    "Breathe": clip(23, keys(held(REST, 1), {
        "Root": [(5, ((0, 0, 0), (0, -0.02, -0.04))), (13, ((0, 0, 0), (0, 0.03, 0.05))), (24, DRAW["Root"])],
        "Body": [(5, (7, 0, 2)), (13, (-8, 0, -2)), (19, (-10.5, 0, -3)), (24, DRAW["Body"])],
        "Head": [(7, (12, 0, 0)), (15, (-7, 0, 4)), (21, (-12, 0, 6)), (24, DRAW["Head"])],
        "ArmL": [(5, (0, -6, 16)), (12, (0, 18, 80)), (16, (0, 23, 92)), (24, DRAW["ArmL"])],
        "ArmR": [(6, (0, 4, 4)), (14, (0, 18, 18)), (24, DRAW["ArmR"])],
        "_grip": [(1, (0, 0, 0)), (6, (0, 0, 0)), (12, (1, 0, 0)), (24, (1, 0, 0))],
    }), name="Breathe", lit=range(12, 25)),
    # Hold (1.9 s; his last great breath: 1.7 s): as Breathe, but held, trembling. Where a breath would be loosed (frame
    # 24, a second in), he jerks forward as if to: and doesn't; he draws back deeper and holds it, shaking.
    "Hold": clip(45, keys(held(REST, 1), {
        "Root": [(5, ((0, 0, 0), (0, -0.02, -0.04))), (13, ((0, 0, 0), (0, 0.03, 0.05))), (23, ((0, 0, 0), (0, 0.04, 0.07))),
                 (25, ((0, 0, 0), (0, -0.01, 0.05))), (28, ((0, 0, 0), (0, 0.05, 0.08))), (46, DRAW["Root"])],
        "Body": [(5, (7, 0, 2)), (13, (-8, 0, -2)), (18, (-10.5, 0, -3)), (23, (-11.5, 0, -3)), (25, (-4, 0, -2)), (28, (-13, 0, -4)),
                 (34, (-13, 1, -3)), (40, (-12.5, -1, -5)), (46, DRAW["Body"])],
        "Head": [(7, (12, 0, 0)), (15, (-7, 0, 4)), (21, (-12, 0, 6)), (24, (-13, 0, 6)), (26, (-3, 0, 4)), (29, (-15, 0, 6)),
                 (31, (-15, 1.5, 5)), (33, (-13, -1.5, 7)), (35, (-15.5, 1.5, 5)), (37, (-13, -1.5, 7)), (39, (-15.5, 1.5, 6)),
                 (41, (-13, -1.5, 6)), (43, (-15, 1, 5)), (46, DRAW["Head"])],
        "ArmL": [(5, (0, -6, 16)), (12, (0, 18, 80)), (16, (0, 23, 92)), (24, (0, 25, 96)), (26, (0, 16, 100)), (29, (0, 27, 97)),
                 (32, (0, 25, 99)), (35, (0, 28, 97)), (38, (0, 25, 99)), (41, (0, 28, 97)), (44, (0, 25.5, 98)), (46, DRAW["ArmL"])],
        "ArmR": [(6, (0, 4, 4)), (14, (0, 18, 18)), (24, (0, 20, 22)), (26, (0, 13, 15)), (30, (0, 21, 23)), (46, DRAW["ArmR"])],
        "_grip": [(1, (0, 0, 0)), (6, (0, 0, 0)), (12, (1, 0, 0)), (46, (1, 0, 0))],
    }), name="Hold", lit=range(12, 47)),
    # Spew (0.4 s, the breath loosed at frame 4; drawn out to 2 s for three small ones, loosed at frames 1.5, 5 and 8.6):
    # thrown forward, the lantern thrust out at you at arm's length (the hook arm flung across after it), two more
    # heaves after the first (the last of his breath), and he sags, letting the lantern go.
    "Spew": clip(9, keys(held(DRAW, 1), held(SPENT, 10), {
        "Root": [(3, ((0, 0, 0), (0, -0.06, 0.02))), (4, ((0, 0, 0), (0, -0.14, -0.02))), (6, ((0, 0, 0), (0, -0.1, -0.03))), (8, ((0, 0, 0), (0, -0.12, -0.04)))],
        "Body": [(3, (10, 0, 10)), (4, (24, 0, 12)), (5, (17, 0, 6)), (6, (21, 0, 2)), (7, (15, 1, -6)), (9, (18, 2, -13))],
        "Head": [(3, (-6, 0, 0)), (4, (14, 0, -4)), (5, (22, 0, -2)), (6, (12, 0, 4)), (8, (16, -2, 10))],
        "ArmL": [(3, (0, 26, 96)), (4, (0, 22, 88)), (5, (0, 22, 82)), (6, (0, 20, 84)), (8, (-6, 8, 56)), (9, (-10, 4, 42))],
        "ArmR": [(3, (0, 6, -10)), (5, (-20, 12, -60)), (7, (-28, 14, -84))],
        "_grip": [(1, (1, 0, 0)), (10, (1, 0, 0))],
    }), name="Spew", lit=range(1, 10), after="Breathe", settle=3),
    # Stagger (0.5 s as made; reeling into round 2 it's drawn out to 2 s): knocked back, the arms flung up, the head
    # whipped back after, the hat jumping; a stumble forward, a wobble back, and he's upright again.
    "Stagger": clip(15, keys(held(REST, 1), held(REST, 16), {
        "Root": [(3, ((-6, 0, 0), (0, 0.1, 0.03))), (6, ((-3, 0, 0), (0, 0.12, -0.03))), (9, ((3, 0, 0), (0, 0.06, -0.05))), (12, ((0, 0, 0), (0, 0.02, 0.01)))],
        "Body": [(3, (-16, 6, 4)), (5, (-20, 8, 6)), (9, (10, -3, -2)), (12, (-3, 1, 0))],
        "Head": [(3, (6, 0, 0)), (5, (-30, 6, 10)), (8, (-14, -4, 0)), (11, (16, -2, -4)), (14, (-3, 0, 0))],
        "ArmL": [(4, (0, 34, 22)), (7, (0, 22, 10)), (10, (0, -22, -6)), (13, (0, 6, 0))],
        "ArmR": [(4, (0, -30, -26)), (7, (0, -20, -14)), (10, (0, 24, 8)), (13, (0, -5, 0))],
        "Hat": [(1, Z), (4, ((-10, 0, 0), (0, 0, 0.04))), (8, ((-4, 0, 0), (0, 0, 0))), (16, Z)],
    }), name="Stagger", settle=5),
    # Death (1 s; then he burns): a jolt back, the arms flung up; then he slumps down into his own coat, folding over,
    # the arms dropping limp (a bounce), the head lolling, the hat slipping down over his face; the coat crumples on the
    # ground.
    "Death": clip(19, keys(held(REST, 1), {
        "Root": [(3, ((0, 0, 0), (0, 0.04, 0.03))), (8, ((0, 0, 0), (0, 0.0, -0.18))), (12, ((0, 0, 0), (0, -0.04, -0.36))), (14, ((0, 0, 0), (0, -0.04, -0.33))),
                 (17, ((0, 0, 0), (0, -0.05, -0.37))), (20, ((0, 0, 0), (0, -0.05, -0.36)))],
        "Body": [(3, (-12, 0, 0)), (8, (8, 3, 4)), (12, (24, 6, 8)), (15, (30, 8, 10)), (17, (27, 7, 9)), (20, (29, 8, 10))],
        "Head": [(4, (-20, 0, 0)), (9, (-2, 4, 10)), (13, (30, 10, 24)), (16, (42, 12, 28)), (18, (37, 10, 26)), (20, (40, 12, 28))],
        "ArmL": [(4, (0, 20, 8)), (9, (0, -30, 10)), (13, (0, -62, 6)), (16, (0, -52, 4)), (20, (0, -58, 4))],
        "ArmR": [(4, (0, -20, -8)), (9, (0, 30, -10)), (13, (0, 64, -6)), (16, (0, 54, -4)), (20, (0, 60, -4))],
        "Hem": [(1, (0, 0, 0)), (9, (0, 0, 0)), (13, (-10, 0, 0)), (20, (-14, 0, 0))],
        "Hat": [(1, Z), (8, Z), (12, ((-10, 0, -6), (0, 0, 0.03))), (15, ((24, 0, 14), (0, -0.07, -0.06))), (20, ((22, 0, 14), (0, -0.07, -0.07)))],
    }), name="Death"),
}


def build():
    rng = kit.rng
    coat, coat_dark = kit.mat("Coat", "#5e4a52"), kit.mat("CoatDark", "#45363c")
    sack, stitch, hat = kit.mat("Sack", "#a89068"), kit.mat("Ink", "#1a1622"), kit.mat("Hat", "#2e2622")
    straw, iron, wood = kit.mat("Straw", "#b08f52"), kit.mat("HookIron", "#3b3a42"), kit.mat("OrchardWood", "#6a5038")
    glass = kit.mat("WatchmanLanternGlass", "#4a4232")  # (dark: unlit until he's found)
    lantern_iron = kit.mat("LanternIron", "#2f2d33")

    def coat_shape(bm):
        kit.cone(9, 0.34, 0.2, 0.85)(bm)
        for v in bm.verts:
            if v.co.z < -0.3:
                v.co.z += rng.uniform(-0.06, 0.04)  # a ragged hem
        # (rings down its length, so the skirt can bend from the waist: see the weights below)
        sides = [e for e in bm.edges if abs(e.verts[0].co.z - e.verts[1].co.z) > 0.3]
        bmesh.ops.subdivide_edges(bm, edges=sides, cuts=3, use_grid_fill=True)
    coat_part = part("Coat", coat_shape, coat, M((0, 0, 1.12)), smooth=True)
    # The coat's top goes with the body, its skirt with the Hem (blended between, so it bends rather than tears).
    body_group, hem_group = coat_part.vertex_groups.new(name="Body"), coat_part.vertex_groups.new(name="Hem")
    for v in coat_part.data.vertices:
        t = min(1.0, max(0.0, (1.32 - v.co.z) / (1.32 - 0.88)))
        w = t * t * (3 - 2 * t)
        body_group.add([v.index], 1 - w, "REPLACE")
        hem_group.add([v.index], w, "REPLACE")
    parts = [
        coat_part,
        part("Collar", kit.cylinder(9, 0.22, 0.08), coat_dark, M((0, 0, SHOULDER + 0.02)), group="Body"),
        part("Head", kit.sphere(9, 7, 0.17), sack, M((0, 0, 1.76), scale=(1, 0.95, 1.05)), smooth=True, group="Head"),
        part("Tie", kit.cylinder(8, 0.1, 0.05), kit.mat("Rope", "#a38b62"), M((0, 0, 1.6)), group="Head"),
        part("HatBrim", kit.cylinder(12, 0.32, 0.03), hat, M((0, 0, 1.9), rot=(8, 0, 0)), group="Hat"),
        part("HatCrown", kit.cone(9, 0.16, 0.12, 0.24), hat, M((0, 0.01, 2.03), rot=(8, 0, 0)), group="Hat"),
    ]
    # The face: two crossed stitches for eyes, a stitched mouth.
    for s in (-1, 1):
        for a in (45, -45):
            parts.append(part("Stitch", kit.cube(), stitch, M((s * 0.06, -0.162, 1.8), rot=(0, a, 0), scale=(0.07, 0.01, 0.012)), group="Head"))
    for k in range(5):
        parts.append(part("Mouth", kit.cube(), stitch, M((-0.06 + k * 0.03, -0.165, 1.7), rot=(0, 90, 0), scale=(0.035, 0.01, 0.008)), group="Head"))
    # The arms: sleeves along the crossbar, straw out of the cuffs.
    for s, bone, cuff in ((-1, "ArmL", "CuffL"), (1, "ArmR", "CuffR")):
        parts.append(part("Sleeve", kit.tube([(s * 0.16, 0, SHOULDER - 0.04), (s * 0.5, 0, SHOULDER - 0.06), (s * 0.74, 0, SHOULDER - 0.08)],
                                             [0.11, 0.09, 0.08], seg=7), coat, group=bone))
        for k in range(4):
            a = math.radians(k * 90 + 20)
            tip = (s * 0.92, math.cos(a) * 0.06, SHOULDER - 0.08 + math.sin(a) * 0.06)
            parts.append(part("Straw", kit.cone(4, 0.025, 0.0, 0.2), straw, kit.along((s * 0.76, 0, SHOULDER - 0.08), tip), group=cuff))
    for k in range(10):
        a = math.tau * k / 10
        base = (math.cos(a) * 0.3, math.sin(a) * 0.3, 0.7)
        tip = (math.cos(a) * 0.36, math.sin(a) * 0.36, 0.5 + rng.uniform(-0.05, 0.05))
        parts.append(part("Straw", kit.cone(4, 0.03, 0.0, 0.22), straw, kit.along(base, tip), group="Hem"))
    # The reaping hook, in the right hand: a handle, and the curved blade forward and down.
    parts.append(part("HookHandle", kit.cylinder(6, 0.025, 0.55), wood, M((0.8, -0.05, SHOULDER - 0.3), rot=(10, 0, 0)), group="ArmR"))
    blade = [(0.8, -0.08, SHOULDER - 0.55), (0.82, -0.3, SHOULDER - 0.6), (0.8, -0.48, SHOULDER - 0.5), (0.78, -0.55, SHOULDER - 0.32)]
    parts.append(part("HookBlade", kit.tube(blade, [0.035, 0.03, 0.022, 0.008], seg=4), iron, group="ArmR"))
    # The lantern, hanging from the left hand on its cord: dark.
    lx = -0.78
    parts += [
        part("LanternCord", kit.cylinder(4, 0.006, 0.2), kit.mat("Rope", "#a38b62"), M((lx, 0, SHOULDER - 0.2)), group="Lantern"),
        part("LanternTop", kit.cone(4, 0.08, 0.02, 0.06), lantern_iron, M((lx, 0, SHOULDER - 0.33), rot=(0, 0, 45)), group="Lantern"),
        part("LanternGlass", kit.cube(), glass, M((lx, 0, SHOULDER - 0.45), scale=(0.1, 0.1, 0.15)), group="Lantern"),
        part("LanternBase", kit.cube(), lantern_iron, M((lx, 0, SHOULDER - 0.54), scale=(0.12, 0.12, 0.02)), group="Lantern"),
    ]
    mesh = kit.join(parts, "Watchman")
    armature = kit.rig("WatchmanRig", BONES)
    kit.skin(mesh, armature)
    kit.animate(armature, ACTIONS)
    for name, gap in REPORT.items():
        if gap > 0.03:
            print(f"[kit] watchman {name}: the lantern's glass is up to {gap * 100:.0f} cm from its light while it's lit")
    return [armature, mesh]
