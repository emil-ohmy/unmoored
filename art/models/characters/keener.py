"""A Keener (the Hall of the Last Hour): one of the mourners who stopped the Hall's clocks at the minute of a death, and
never let the grief go. A figure in black crape to the floor (a wide bell of a skirt, a crape overskirt to the knee), a
weeping veil over the whole face so that only the Soured's green eyes show through it, its long fall down the back;
white weepers (mourning cuffs) at the wrists. In its left hand a silver watch, stopped at the minute (its hands at
twenty past four), a loop of chain hanging from it; in its right a white handkerchief. A small green lantern at its
belt, like every Soured's.

Clips (30 fps): Idle (3 s loop: rocking over the watch, a sob, the handkerchief to the veil), Walk (2 s loop: a glide,
a float under its skirts), Breath (1.2 s: the knell's tell: it gathers itself, then rises out of its skirts, head thrown
back, the watch lifted high, the other arm flung out), Toll (1.2 s: the knell: thrown forward, the watch held out at you,
the handkerchief to the veil, a shudder at each of its three rings, at 0.18 s, 0.38 s and 0.58 s (the frames 6.4, 12.4
and 18.4: Keener.cs's volley, impactAt 0.15 of 1.2 s then every 0.2 s), then held, keening), Recover (1.4 s: spent,
bowed low, arms hanging, heaving, then back to the watch), Stagger, Death (a last look up, then it folds down into its
crape and falls forward over it).

How it's animated (as the Laden): each bone keyed on its own timing (kit.bake: tracks); the body leads (the waist, then
the chest), the head follows a few frames late. The loose things aren't keyed: they're swung every frame by a little
simulation of their weight (loose()): the veil's long fall and the skirt's hem lag behind the body on a spring and
overshoot; the watch chain and the handkerchief hang and swing from what holds them. The arms aren't keyed either: the
watch is keyed where it is (on the chest) and the left hand is bent onto it every frame (kit.reach); the right hand goes
where its track ("HandR", not a bone) puts it, on the chest or on the veil. The skirt's hem never goes through the floor.
"""
import math

from mathutils import Vector

M, part = kit.M, kit.part  # `kit` is injected by build_model.py

SHOULDER = 1.42
HIP = 0.95
CHEST = 1.18
HAND_L, HAND_R = (-0.25, -0.02, 0.86), (0.25, -0.02, 0.86)
PALM_L, PALM_R = (-0.25, -0.03, 0.82), (0.25, -0.03, 0.82)          # the gloves' centres: the forearms' tips
SHOULDER_L, SHOULDER_R = (-0.21, 0, SHOULDER - 0.04), (0.21, 0, SHOULDER - 0.04)
ELBOW_L, ELBOW_R = (-0.235, 0.01, 1.12), (0.235, 0.01, 1.12)
WATCH = (HAND_L[0], HAND_L[1] - 0.08, HAND_L[2] - 0.06)            # the watch's centre, in front of the left glove
BOW = (WATCH[0], WATCH[1] + 0.01, WATCH[2] + 0.09)                  # its bow, where the chain hangs from
BONES = {
    "Root": ((0, 0, 0), (0, 0, 0.3), None),
    "Skirt": ((0, 0, HIP), (0, 0, 0.45), "Root"),                   # the skirts, from the waist
    "Hem": ((0, 0, 0.45), (0, 0, 0.02), "Skirt"),                   # the bell's lower half and its hem (swung)
    "Spine": ((0, 0, HIP), (0, 0, CHEST), "Root"),                  # the waist (and its lantern)
    "Chest": ((0, 0, CHEST), (0, 0, SHOULDER), "Spine"),
    "Head": ((0, 0, SHOULDER), (0, -0.02, 1.8), "Chest"),
    "Veil": ((0, 0.08, 1.45), (0, 0.13, 1.0), "Chest"),             # the veil's fall down the back (swung)
    "ArmL": (SHOULDER_L, ELBOW_L, "Chest"),
    "ForeL": (ELBOW_L, PALM_L, "ArmL"),
    "ArmR": (SHOULDER_R, ELBOW_R, "Chest"),
    "ForeR": (ELBOW_R, PALM_R, "ArmR"),
    "Watch": (WATCH, (WATCH[0], WATCH[1] - 0.08, WATCH[2]), "Chest"),  # keyed where it is: the left hand follows it
    "Chain": (BOW, (BOW[0], BOW[1], BOW[2] - 0.22), "Watch"),       # (swung)
    "Kerchief": (PALM_R, (0.26, -0.05, 0.58), "ForeR"),             # the handkerchief, pinched in the glove (swung)
}
POLE_L, POLE_R = (-1, 0.5, -0.6), (1, 0.5, -0.6)                    # the elbows bow out and back


def watch(pos, rot):
    """The watch with its centre at `pos` (in the chest's rest frame), turned `rot` (at rest its face looks ahead, -Y;
    -90 about X turns it up)."""
    return (tuple(rot), tuple(p - w for p, w in zip(pos, WATCH)))


def hand(pos, face=0.0):
    """The right hand's place (the glove's centre), in the rest frame: carried by the chest, or (face 1) by the head."""
    return ((face, 0, 0), tuple(pos))


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


def arms(pose):
    """The left hand on the watch, the right where HandR puts it, the elbows bowed out and back."""
    (face, _, _), at = pose["HandR"]
    at = Vector(at)
    pose = kit.reach(BONES, pose, "ArmL", "ForeL", lambda m: m["Watch"] @ Vector(PALM_L), POLE_L)
    return kit.reach(BONES, pose, "ArmR", "ForeR", lambda m: (m["Chest"] @ at).lerp(m["Head"] @ at, face), POLE_R)


# ---------------------------------------------------------------- the loose things: swung by their weight

def skirt_radius(z):
    """How far out the skirts are at height z (in the skirt's rest frame); None above them."""
    if z > 0.96 or z < 0.0:
        return None
    if z >= 0.54:
        return 0.36 + (0.21 - 0.36) * (z - 0.54) / 0.42  # (the overskirt)
    return 0.42 + (0.19 - 0.42) * (z - 0.02) / 0.93


def outside(pos, m):
    """`pos` pushed out of the skirts and the bodice (a handkerchief or a chain lies on them, never in them), and above
    the floor."""
    for bone, radius, squash, y0 in (("Skirt", skirt_radius, 0.92, 0.02),
                                     ("Spine", lambda z: 0.2 + 0.02 * (z - 0.93) / 0.51 if 0.93 <= z <= 1.44 else None, 0.74, 0.0)):
        local = m[bone].inverted() @ pos
        r = radius(local.z)
        if r is None:
            continue
        r += 0.02
        x, y = local.x, (local.y - y0) / squash
        d = math.hypot(x, y)
        if d < r:
            if d < 1e-6:
                x, y, d = 0.0, -1.0, 1.0
            local = Vector((x * r / d, y * r / d * squash + y0, local.z))
            pos = m[bone] @ local
    if pos.z < 0.05:
        pos = Vector((pos.x, pos.y, 0.05))
    return pos


# bone: (stiffness pulling it back to where the body would carry it, damping, gravity (in g), pushed out of the body)
LOOSE = {
    "Veil": (55.0, 5.0, 0.15, False),
    "Hem": (70.0, 6.5, 0.0, False),
    "Chain": (0.0, 6.0, 1.0, True),
    "Kerchief": (0.0, 7.0, 1.0, True),
}


def loose(frames, periodic):
    """Swing the loose bones by their weight, frame by frame (a point on each one's tip, on a spring towards where its
    parent carries it and/or hanging; a loop's swing runs three times round so its seam joins)."""
    mats = [kit.pose_matrices(BONES, p) for _, p in frames]
    n = len(frames)
    cycle = n - 1 if periodic else n
    order = list(range(cycle)) * (3 if periodic else 1)
    sub = 6
    dt = 1.0 / 30 / sub
    for bone, (k, c, g, collide) in LOOSE.items():
        head, tail, parent = BONES[bone]
        length = (Vector(tail) - Vector(head)).length
        rest_dir = (Vector(tail) - Vector(head)).normalized()

        def at(i):
            return mats[i][bone] @ Vector(head), mats[i][bone] @ Vector(tail)

        def step(a, b, pos, vel):
            ha, ta = at(a)
            hb, tb = at(b)
            for s in range(sub):
                u = (s + 1) / sub
                h, t = ha.lerp(hb, u), ta.lerp(tb, u)
                acc = (t - pos) * k - vel * c + Vector((0, 0, -9.81 * g))
                new = pos + (vel + acc * dt) * dt
                d = new - h
                new = h + d.normalized() * length if d.length > 1e-6 else t
                if collide:
                    new = outside(new, mats[b])
                vel = (new - pos) / dt
                pos = new
            return pos, vel

        pos, vel = at(order[0])[1], Vector()
        for _ in range(120):  # (settled at the first frame before the clip begins)
            pos, vel = step(order[0], order[0], pos, vel)
        out = {order[0]: pos}
        for a, b in zip(order, order[1:] + order[:1] if periodic else order[1:]):
            pos, vel = step(a, b, pos, vel)
            out[b] = pos
        if periodic:
            out[n - 1] = out[0]
        else:
            # (a one-off eases them to rest over its last frames: the next clip begins with them settled)
            rest = out[n - 1]
            vel = Vector()
            for _ in range(150):
                rest, vel = step(n - 1, n - 1, rest, vel)
            ease = min(8, n // 3)
            for i in range(n - ease, n):
                u = (i - (n - 1 - ease)) / ease
                out[i] = out[i].lerp(rest, u * u * (3 - 2 * u))
        for i, (_, p) in enumerate(frames):
            h = mats[i][bone] @ Vector(head)
            inward = mats[i][parent].to_quaternion().inverted() @ (out[i] - h)
            q = rest_dir.rotation_difference(inward.normalized())
            offset = p.get(bone, ((0, 0, 0), (0, 0, 0)))
            offset = offset[1] if isinstance(offset[0], (tuple, list)) else (0, 0, 0)
            p[bone] = (tuple(math.degrees(a) for a in q.to_euler("XYZ")), tuple(offset))


HEM_RING = [Vector((0.43 * math.cos(a), 0.02 + 0.43 * 0.92 * math.sin(a), 0.01)) for a in (2 * math.pi * i / 16 for i in range(16))]


def floor(pose):
    """The hem lifted where it would go through the floor (it crumples on it, it doesn't sink)."""
    m = kit.pose_matrices(BONES, pose)
    low = min((m["Hem"] @ v).z for v in HEM_RING)
    if low < 0.01:
        rot, offset = pose["Hem"]
        lift = m["Skirt"].to_quaternion().inverted() @ Vector((0, 0, 0.01 - low))
        pose["Hem"] = (rot, tuple(Vector(offset) + lift))


REACH = {}  # (the build's report: how far each clip's hands come off what they hold, at worst)


def clip(length, tracks, periodic=False):
    """A clip `length` frames long (30 a second) from per-bone tracks, baked to every frame: the hands on the watch and
    where HandR says, the loose things swung, the hem kept off the floor."""
    frames = [(f, arms(p)) for f, p in kit.bake(tracks, length, periodic)]
    loose(frames, periodic)
    worst = 0.0
    for _, p in frames:
        floor(p)
        m = kit.pose_matrices(BONES, p)
        (face, _, _), at = p.pop("HandR")
        at = Vector(at)
        worst = max(worst, (m["ForeL"] @ Vector(PALM_L) - m["Watch"] @ Vector(PALM_L)).length,
                    (m["ForeR"] @ Vector(PALM_R) - (m["Chest"] @ at).lerp(m["Head"] @ at, face)).length)
    REACH[len(REACH)] = worst
    return ("LINEAR", frames)


# ---------------------------------------------------------------- poses

# Mourning (what Idle and Walk begin on, and Recover and Stagger end on): bowed at the waist and the shoulders, the head
# hung over the watch held face-up at its chest, the right hand clasped under it, the handkerchief hanging.
MOURN_WATCH = watch((-0.06, -0.25, 1.12), (-65, 0, 0))
MOURN_HAND = hand((0.05, -0.27, 1.04))
MOURN = {"Root": ((0, 0, 0), (0, 0, 0)), "Skirt": (0, 0, 0), "Spine": (4, 0, 0), "Chest": (8, 0, 0), "Head": (18, 0, 0),
         "Watch": MOURN_WATCH, "HandR": MOURN_HAND}
FACE = (0.06, -0.225, 1.66)  # (the veil before its eyes, in the head's rest frame: the handkerchief pressed there)


def near(pos, dx=0.0, dy=0.0, dz=0.0):
    return (pos[0] + dx, pos[1] + dy, pos[2] + dz)


# The breath drawn (Breath ends on it, Toll begins on it): risen out of its skirts, arched back, the head thrown back,
# the watch held high (its face to you), the right arm flung wide.
HIGH = (-0.3, -0.12, 1.8)
BREATH_FULL = {"Root": ((0, 0, 0), (0, 0, 0.055)), "Skirt": (2, 0, 0), "Spine": (-11, 0, 0), "Chest": (-14, 0, 0),
               "Head": (-30, 0, 0), "Watch": watch(HIGH, (0, 0, -6)), "HandR": hand((0.64, -0.06, 1.44))}
# Keening (Toll ends on it, Recover begins on it): thrown forward, the watch held out at you, the handkerchief to the veil.
OUT = (-0.15, -0.37, 1.76)  # (up before the chest: the chest is thrown forward, so it's held out level at you)
KEEN = {"Root": ((0, 0, 0), (0, -0.07, -0.03)), "Skirt": (-3, 0, 0), "Spine": (24, 0, 0), "Chest": (17, 0, 0),
        "Head": (2, 0, 0), "Watch": watch(OUT, (-60, 0, 0)), "HandR": hand(FACE, 1.0)}
# Spent (Recover): bowed low, the arms hanging, the watch dangling from its fingers.
HANG = (-0.22, -0.44, 1.05)
HANG_WATCH = watch(HANG, (10, 0, 0))
HANG_HAND = hand((0.22, -0.36, 1.08))

RING = (6.4, 12.4, 18.4)  # (the frames the Toll's three rings leave on)

ACTIONS = {
    # Idle (3 s loop): it rocks, slowly and heavily, over the watch (forward twice), breathing; the head lifts a little,
    # turning, as if it heard something (frame 40), then a sob takes it (the chest hitches three times, 52 to 66) and
    # it presses the handkerchief to its veil, twice, the head sinking into it; the hand comes down again and the head
    # settles over the watch. The veil's fall and the hem sway after the rocking; the chain swings off the watch.
    "Idle": clip(90, {
        "Root": [(1, ((0, 0, 0), (0, 0, 0))), (24, ((0, 1, 0), (0, 0, 0.008))), (46, ((0, 0, 0), (0, 0, -0.004))),
                 (70, ((0, -1, 0), (0, 0, 0.01)))],
        "Skirt": [(1, (0, 0, 0)), (32, (1, -1, 0)), (78, (-1, 1, 0))],
        "Spine": [(1, (4, 0, 0)), (23, (8, 0.8, 0)), (46, (3, 0, 0)), (68, (8, -0.8, 0))],
        "Chest": [(1, (8, 0, 0)), (14, (5, 0, 0)), (27, (10, 1, 0)), (40, (5, 0, -2)), (52, (10, 0, 0)), (55, (6, 0, 0)),
                  (58, (11, 0, 0)), (61, (6, 0, 0)), (65, (13, -1, 0)), (77, (9, 0, 0))],
        "Head": [(1, (18, 0, 0)), (17, (16, 0, 0)), (30, (20, 0, -3)), (42, (8, 2, -10)), (48, (6, 2, -9)), (56, (24, 0, -4)),
                 (60, (19, 0, -2)), (64, (27, 0, 2)), (70, (24, 0, 3)), (80, (21, 0, 1))],
        "Watch": [(1, MOURN_WATCH), (30, watch((-0.06, -0.25, 1.13), (-60, 5, 0))),
                  (58, watch((-0.04, -0.24, 1.17), (-70, -3, 0))), (80, watch((-0.06, -0.255, 1.11), (-66, 0, 0)))],
        "HandR": [(1, MOURN_HAND), (36, hand((0.05, -0.27, 1.05))), (46, hand(near(FACE, 0.02, -0.06, -0.04), 1.0)),
                  (52, hand(FACE, 1.0)), (56, hand(near(FACE, 0.01, -0.04, -0.02), 1.0)), (60, hand(near(FACE, 0, 0.005), 1.0)),
                  (66, hand(near(FACE, 0, -0.01, -0.01), 1.0)), (76, hand((0.07, -0.3, 1.1), 0.3)), (86, MOURN_HAND)],
    }, periodic=True),
    # Walk (2 s loop: it has no feet, it floats): it rises (frame 16, 46) and sinks back into its skirts (1, 31), the
    # hem lifting off the floor at the top of each float and settling; it sways side to side with each (the chest
    # after the waist, the head after the chest, keeping level), the skirts swirl round after the sway, the veil
    # streams. Hunched over the watch the whole way (it's the same coming at you or drifting back from you). Frame 1 is
    # the mourning pose, so it joins what's before and after.
    "Walk": clip(60, {
        "Root": [(1, ((0, 0, 0), (0, 0, 0))), (16, ((1.5, 4, 0), (0.02, 0, 0.055))), (31, ((0, 0, 0), (0, 0, 0))),
                 (46, ((1.5, -4, 0), (-0.02, 0, 0.055)))],
        "Skirt": [(5, (0, -1.5, 3)), (22, (1, 2, -5)), (35, (0, 1.5, -3)), (52, (1, -2, 5))],
        "Spine": [(1, (4, 0, 0)), (12, (6, -0.5, 0)), (20, (7, -2, 0)), (31, (4, 0, 0)), (42, (6, 0.5, 0)), (50, (7, 2, 0))],
        "Chest": [(3, (9, 0, 0)), (12, (6, -0.5, 1)), (22, (8, -2, 2)), (33, (9, 0, 0)), (42, (6, 0.5, -1)), (52, (8, 2, -2))],
        "Head": [(5, (21, 0, 0)), (19, (14, -0.5, 3)), (25, (16, -1.5, 4)), (35, (21, 0, 0)), (49, (14, 0.5, -3)), (55, (16, 1.5, -4))],
        "Watch": [(3, watch((-0.06, -0.25, 1.125), (-66, 0, 0))), (20, watch((-0.06, -0.255, 1.1), (-60, 3, 0))),
                  (33, watch((-0.06, -0.25, 1.125), (-66, 0, 0))), (50, watch((-0.06, -0.255, 1.1), (-60, -3, 0)))],
        "HandR": [(4, hand((0.05, -0.27, 1.045))), (21, hand((0.05, -0.28, 1.02))), (34, hand((0.05, -0.27, 1.045))),
                  (51, hand((0.05, -0.28, 1.02)))],
    }, periodic=True),
    # Breath (1.2 s: the tell): it gathers itself first, sinking into its skirts and curling over the watch (frame 7),
    # then rises right out of them (the hem leaves the floor), arching back, the watch swept up past its face to high
    # overhead (overshooting at 25), the right arm flung out wide, the handkerchief streaming after it; the head thrown
    # back last (28). Held there, trembling, drawn up full.
    "Breath": clip(36, keys(held(MOURN, 1), {
        "Root": [(7, ((0, 0, 0), (0, 0.01, -0.035))), (20, ((0, 0, 0), (0, 0, 0.04))), (26, ((0, 0, 0), (0, 0, 0.065))),
                 (30, ((0, 0, 0), (0, 0, 0.05))), (37, BREATH_FULL["Root"])],
        "Skirt": [(9, (-3, 0, 0)), (24, (4, 0, 0)), (37, BREATH_FULL["Skirt"])],
        "Spine": [(6, (10, 0, 0)), (18, (-4, 0, 0)), (25, (-13, 0, 0)), (30, (-10, 0, 0)), (37, BREATH_FULL["Spine"])],
        "Chest": [(7, (16, 0, 0)), (19, (-6, 0, 0)), (26, (-17, 0, 0)), (31, (-13, 0, 0)), (34, (-15, 0, 0)), (37, BREATH_FULL["Chest"])],
        "Head": [(8, (30, 0, 0)), (17, (10, 0, 0)), (24, (-18, 0, 0)), (28, (-35, 0, 0)), (32, (-28, 0, 0)), (37, BREATH_FULL["Head"])],
        "Watch": [(7, watch((-0.04, -0.21, 1.08), (-78, 0, 0))), (18, watch((-0.14, -0.3, 1.52), (-30, 0, 0))),
                  (25, watch(near(HIGH, 0, 0, 0.035), (4, 0, -8))), (30, watch(near(HIGH, 0, 0, -0.01), (-2, 0, -5))),
                  (37, BREATH_FULL["Watch"])],
        "HandR": [(7, hand((0.06, -0.25, 1.0))), (17, hand((0.4, -0.22, 1.2))), (24, hand((0.67, -0.08, 1.48))),
                  (29, hand((0.62, -0.06, 1.42))), (37, BREATH_FULL["HandR"])],
    })),
    # Toll (1.2 s: the knell): thrown forward, the watch swung over and held out at you, the handkerchief clapped to
    # the veil; at each ring (frames 6.4, 12.4, 18.4: Keener.cs's three, as they leave) a shudder: the body jolts
    # forward and down to its extreme, the watch thrust out further, the head whipping after a frame late; it rebounds
    # and gathers for the next. After the third it sags and holds, keening, shaking a little.
    "Toll": clip(36, keys(held(BREATH_FULL, 1), {
        "Root": [(4, ((0, 0, 0), (0, -0.03, 0.03))), (RING[0], ((0, 0, 0), (0, -0.07, -0.035))), (9, ((0, 0, 0), (0, -0.05, -0.005))),
                 (10.8, ((0, 0, 0), (0, -0.045, 0.0))), (RING[1], ((0, 0, 0), (0, -0.075, -0.04))), (15, ((0, 0, 0), (0, -0.06, -0.01))),
                 (16.8, ((0, 0, 0), (0, -0.055, -0.005))), (RING[2], ((0, 0, 0), (0, -0.08, -0.045))), (23, ((0, 0, 0), (0, -0.07, -0.025))),
                 (30, ((0, 0, 0), (0, -0.07, -0.035))), (37, KEEN["Root"])],
        "Skirt": [(RING[0], (-5, 0, 0)), (9, (-1, 0, 0)), (RING[1], (-5, 0, 0)), (15, (-2, 0, 0)), (RING[2], (-6, 0, 0)), (26, (-3, 0, 0)),
                  (37, KEEN["Skirt"])],
        "Spine": [(3, (-5, 0, 0)), (RING[0], (24, 0, 0)), (9, (16, 0, 0)), (10.8, (15, 0, 0)), (RING[1], (26, 0, 0)), (15, (18, 0, 0)),
                  (16.8, (17, 0, 0)), (RING[2], (29, 0, 0)), (23, (22, 0, 0)), (30, (25, 0, 0)), (37, KEEN["Spine"])],
        "Chest": [(4, (-6, 0, 0)), (RING[0], (20, 0, 0)), (9, (12, 0, 1)), (10.8, (11, 0, 0)), (RING[1], (22, 0, -1)), (15, (14, 0, 0)),
                  (16.8, (13, 0, 1)), (RING[2], (24, 0, 0)), (23, (15, 0, 0)), (28, (18, 1, 0)), (32, (16, -1, 0)), (37, KEEN["Chest"])],
        "Head": [(4.5, (-18, 0, 0)), (7.5, (10, 0, 0)), (10, (-3, 0, 0)), (13.5, (11, 0, 2)), (16, (-1, 0, 0)), (19.5, (13, 0, -2)),
                 (24, (0, 0, 0)), (30, (5, 0, 0)), (37, KEEN["Head"])],
        "Watch": [(4, watch((-0.21, -0.26, 1.84), (-30, 0, -4))), (RING[0], watch(near(OUT, 0, -0.03, 0.04), (-56, 0, 5))),
                  (9, watch(near(OUT, 0, 0.02, -0.025), (-64, 0, -2))), (10.8, watch(near(OUT, 0, 0.025, -0.03), (-64, 0, -3))),
                  (RING[1], watch(near(OUT, 0, -0.035, 0.045), (-55, 0, 6))), (15, watch(near(OUT, 0, 0.015, -0.02), (-64, 0, -2))),
                  (16.8, watch(near(OUT, 0, 0.02, -0.025), (-64, 0, -3))), (RING[2], watch(near(OUT, 0, -0.04, 0.05), (-54, 0, 7))),
                  (23, watch(near(OUT, 0, 0.0, -0.01), (-62, 0, 0))), (29, watch(near(OUT, 0.01, 0, 0.01), (-59, 0, 3))),
                  (37, KEEN["Watch"])],
        "HandR": [(4, hand((0.4, -0.3, 1.5), 0.4)), (8, hand(near(FACE, 0, -0.02), 1.0)), (RING[1], hand(FACE, 1.0)),
                  (RING[2], hand(near(FACE, 0, 0.005), 1.0)), (37, KEEN["HandR"])],
    })),
    # Recover (1.4 s: the punish window): spent, it slumps (the arms drop, the watch dangling, the handkerchief hand
    # falling from the veil; the body sags past its rest and settles, the head last), heaves twice for breath, bowed
    # low; then it gathers the watch back to its chest (the left hand first, the right after), straightens, and the
    # head comes down over the watch: mourning again, as Idle and Walk begin.
    "Recover": clip(42, keys(held(KEEN, 1), held(MOURN, 43), {
        "Root": [(6, ((0, 0, 0), (0, -0.06, -0.07))), (11, ((0, 0, 0), (0, -0.05, -0.05))), (17, ((0, 0, 0), (0, -0.05, -0.035))),
                 (22, ((0, 0, 0), (0, -0.05, -0.055))), (27, ((0, 0, 0), (0, -0.05, -0.035))), (31, ((0, 0, 0), (0, -0.045, -0.05))),
                 (38, ((0, 0, 0), (0, -0.01, -0.01)))],
        "Skirt": [(7, (-4, 0, 0)), (20, (-2, 0, 0)), (33, (-3, 0, 0))],
        "Spine": [(6, (34, 0, 0)), (11, (30, 0, 0)), (17, (27, 0, 0)), (22, (31, 0, 0)), (27, (27, 0, 0)), (31, (30, 0, 0)),
                  (38, (8, 0, 0))],
        "Chest": [(7, (24, 0, 0)), (12, (20, 0, 0)), (18, (14, 0, 0)), (23, (22, 0, 0)), (28, (15, 0, 0)), (32, (20, 0, 0)),
                  (39, (10, 0, 0))],
        "Head": [(9, (34, 0, 0)), (14, (26, 0, 2)), (20, (22, 0, 0)), (25, (30, 0, -2)), (30, (24, 0, 0)), (34, (30, 0, 0)),
                 (40, (14, 0, 0))],
        "Watch": [(4, watch((-0.17, -0.44, 1.56), (-40, 0, 0))), (9, watch(near(HANG, 0, 0, -0.02), (14, 0, 0))),
                  (13, watch(near(HANG, 0, 0.01, 0.015), (6, 0, 0))), (17, HANG_WATCH),
                  (29, watch(near(HANG, 0, 0, 0.01), (8, 0, 0))), (37, watch((-0.08, -0.27, 1.12), (-55, 0, 0)))],
        "HandR": [(4, hand(near(FACE, 0.04, -0.06, -0.08), 0.6)), (10, hand((0.22, -0.36, 1.06))), (15, hand((0.21, -0.35, 1.1))), (31, hand((0.22, -0.36, 1.08))), (39, hand((0.07, -0.28, 1.05)))],
    })),
    # Stagger (0.57 s as made; played over 1.1 s, or a hook's yank): struck, it rocks back on its skirts (the waist
    # first, the head whipping back after), the watch and the handkerchief flung up and out, the hem swinging; it
    # sways forward past its rest, catching itself, and settles back to mourning over the watch.
    "Stagger": clip(17, keys(held(MOURN, 1), held(MOURN, 18), {
        "Root": [(3, ((-5, 0, 3), (0, 0.07, 0.01))), (7, ((-2, 0, -2), (0, 0.12, 0.0))), (11, ((3, 0, 0), (0, 0.05, -0.02))),
                 (14, ((1, 0, 0), (0, 0.01, -0.005)))],
        "Skirt": [(4, (4, 0, -3)), (9, (-3, 0, 2)), (13, (1, 0, 0))],
        "Spine": [(3, (-14, 0, 4)), (7, (-8, 0, 0)), (11, (10, 0, -2)), (14, (5, 0, 0))],
        "Chest": [(4, (-16, 0, 6)), (8, (-6, 0, 0)), (12, (13, 0, -2)), (15, (7, 0, 0))],
        "Head": [(2, (24, 0, 0)), (5, (-26, 0, 6)), (9, (-6, 0, -4)), (13, (28, 0, 2)), (16, (17, 0, 0))],
        "Watch": [(4, watch((-0.32, -0.3, 1.42), (-20, 0, -20))), (9, watch((-0.2, -0.32, 1.3), (-35, 0, -8))),
                  (14, watch((-0.07, -0.26, 1.15), (-62, 0, 2)))],
        "HandR": [(4, hand((0.46, -0.24, 1.32))), (9, hand((0.34, -0.3, 1.15))), (14, hand((0.07, -0.28, 1.06)))],
    })),
    # Death (0.97 s as made; played over 1.1 s, dissolving from about halfway): a jolt, and a last look up (the head
    # thrown back, the watch lifted to its face, the handkerchief hand falling open); a beat; then it folds down into
    # its crape (sinking, the skirts crumpling on the floor, the bodice curling over), and topples forward over them,
    # the arms falling ahead of it, the head last; a small bounce, and still.
    "Death": clip(29, keys(held(MOURN, 1), {
        "Root": [(4, ((0, 0, 0), (0, 0.03, 0.02))), (8, ((0, 0, 0), (0, 0.02, 0.04))), (11, ((0, 0, 0), (0, 0.02, 0.035))),
                 (19, ((0, 0, 0), (0, -0.05, -0.36))), (24, ((0, 0, 0), (0, -0.14, -0.46))), (27, ((0, 0, 0), (0, -0.15, -0.44))),
                 (30, ((0, 0, 0), (0, -0.15, -0.45)))],
        "Skirt": [(6, (2, 0, 0)), (20, (-6, 0, 0)), (26, (-10, 0, 0)), (30, (-9, 0, 0))],
        "Spine": [(4, (-4, 0, 0)), (8, (-8, 0, 0)), (11, (-6, 0, 0)), (17, (18, 0, 2)), (22, (52, 0, 4)), (25, (68, 0, 4)),
                  (27, (62, 0, 4)), (30, (64, 0, 4))],
        "Chest": [(5, (-8, 0, 0)), (9, (-14, 0, 0)), (12, (-10, 0, 0)), (18, (16, 0, 0)), (23, (34, 0, -4)), (26, (36, 0, -4)),
                  (28, (32, 0, -4)), (30, (33, 0, -4))],
        "Head": [(3, (22, 0, 0)), (7, (-20, 0, 0)), (10, (-32, 0, 4)), (13, (-28, 0, 4)), (19, (6, 0, 0)), (24, (30, 0, 8)),
                 (27, (42, 0, 10)), (30, (38, 0, 10))],
        "Watch": [(7, watch((-0.12, -0.33, 1.5), (-15, 0, 10))), (12, watch((-0.12, -0.34, 1.52), (-12, 0, 12))),
                  (19, watch((-0.18, -0.38, 1.2), (0, 0, 0))), (24, watch((-0.22, -0.49, 1.12), (30, 0, -10))),
                  (30, watch((-0.23, -0.47, 1.1), (40, 0, -14)))],
        "HandR": [(6, hand((0.3, -0.2, 1.1))), (12, hand((0.34, -0.2, 1.04))), (20, hand((0.26, -0.36, 1.06))),
                  (25, hand((0.24, -0.44, 1.11))), (30, hand((0.24, -0.43, 1.1)))],
    })),
}


def blend(mesh, temp, low, high, z0, z1):
    """The vertices of the parts grouped `temp` shared between two bones by height: `low` below z0, `high` above z1,
    eased between (so the body bends there instead of breaking)."""
    groups = mesh.vertex_groups
    t = groups[temp]
    lo = groups.get(low) or groups.new(name=low)
    hi = groups.get(high) or groups.new(name=high)
    for v in [v for v in mesh.data.vertices if any(g.group == t.index for g in v.groups)]:
        s = min(1.0, max(0.0, (v.co.z - z0) / (z1 - z0)))
        s = s * s * (3 - 2 * s)
        if s < 1.0:
            lo.add([v.index], 1.0 - s, "REPLACE")
        if s > 0.0:
            hi.add([v.index], s, "REPLACE")
    groups.remove(t)


def build():
    crape, crape_sheen = kit.mat("KeenerCrape", "#1d1b21"), kit.mat("KeenerCrapeSheen", "#2e2b35")
    weeper, linen = kit.mat("KeenerWeeper", "#e8e4dc"), kit.mat("KeenerHandkerchief", "#ece8df")
    glove = kit.mat("KeenerGlove", "#141317")
    silver, face = kit.mat("KeenerSilver_Metal", "#c4c8cc"), kit.mat("KeenerWatchFace", "#efe9d8")
    black, eye = kit.mat("KeenerWatchHand", "#141316"), kit.mat("SouredEye_Glow", "#c8f07a")
    iron = kit.mat("KeenerIron", "#2f2d33")

    parts = []
    # The skirts: a wide bell of crape to the floor (its lower half swings: shared with the Hem by height), a crape
    # overskirt to the knee, a dark hem.
    parts += [
        part("Skirt", kit.cone(12, 0.42, 0.19, HIP - 0.02), crape, M((0, 0.02, HIP / 2 + 0.01), scale=(1, 0.92, 1)), smooth=True, group="SkirtBlend"),
        part("Overskirt", kit.cone(12, 0.36, 0.21, 0.42), crape_sheen, M((0, 0.01, HIP - 0.2), scale=(1, 0.92, 1)), smooth=True, group="Skirt"),
        part("Hem", kit.cylinder(12, 0.43, 0.05), crape_sheen, M((0, 0.02, 0.035), scale=(1, 0.92, 1)), smooth=True, group="Hem"),
    ]
    # The bodice, buttoned high (it bends between the waist and the chest); a white band at the throat.
    parts += [
        part("Bodice", kit.cone(10, 0.2, 0.22, SHOULDER - HIP + 0.04), crape, M((0, 0.0, (HIP + SHOULDER) / 2), scale=(1, 0.74, 1)), smooth=True, group="BodiceBlend"),
        part("Waist", kit.cylinder(10, 0.2, 0.06), crape_sheen, M((0, 0.0, HIP + 0.03), scale=(1, 0.78, 1)), smooth=True, group="Spine"),
        part("Shoulders", kit.sphere(10, 6), crape, M((0, 0, SHOULDER - 0.05), scale=(0.27, 0.17, 0.09)), smooth=True, group="Chest"),
        part("Band", kit.cylinder(9, 0.075, 0.04), weeper, M((0, -0.005, SHOULDER + 0.03)), smooth=True, group="Chest"),
        # The veil's fall: down the back from the head to the waist (its top on the head, the rest swinging).
        part("VeilFall", kit.cone(9, 0.25, 0.14, SHOULDER - HIP + 0.1), crape_sheen, M((0, 0.07, (HIP + SHOULDER) / 2 + 0.1), scale=(1, 0.6, 1)), smooth=True, group="VeilBlend"),
    ]
    # The head under its weeping veil: no face, the eyes' green through the crape; a small round cap at the crown.
    parts += [
        part("Veil", kit.cone(10, 0.2, 0.09, 0.32), crape_sheen, M((0, -0.01, 1.6)), smooth=True, group="Head"),
        part("VeilCrown", kit.sphere(10, 7, 0.135), crape, M((0, -0.02, 1.74), scale=(1.0, 1.0, 1.05)), smooth=True, group="Head"),
        part("Cap", kit.cylinder(10, 0.1, 0.05), crape_sheen, M((0, 0.0, 1.86)), smooth=True, group="Head"),
    ]
    for s in (-1, 1):
        parts.append(part("Eye", kit.sphere(8, 6, 0.024), eye, M((s * 0.05, -0.15, 1.72), scale=(1.2, 0.6, 0.8)), smooth=True, group="Head"))
    # Arms in crape sleeves (in two: to the elbow, a knob of crape there, and on to the wrist, flaring), the white
    # weepers at the wrists, black gloves.
    for s, arm, fore, hand_at, elbow in ((-1, "ArmL", "ForeL", HAND_L, ELBOW_L), (1, "ArmR", "ForeR", HAND_R, ELBOW_R)):
        shoulder = (s * 0.21, 0, SHOULDER - 0.04)
        wrist = (hand_at[0], hand_at[1], hand_at[2] + 0.08)
        parts.append(part("Sleeve", kit.cone(7, 0.062, 0.055, math.dist(elbow, shoulder) + 0.02), crape, kit.along(elbow, shoulder), smooth=True, group=arm))
        parts.append(part("Elbow", kit.sphere(8, 6, 0.062), crape, M(elbow), smooth=True, group=arm))
        parts.append(part("Sleeve", kit.cone(7, 0.07, 0.062, math.dist(wrist, elbow)), crape, kit.along(wrist, elbow), smooth=True, group=fore))
        parts.append(part("Weeper", kit.cone(8, 0.075, 0.06, 0.12), weeper, kit.along(hand_at, (hand_at[0], hand_at[1], hand_at[2] + 0.13)), smooth=True, group=fore))
        parts.append(part("Glove", kit.sphere(7, 5, 0.055), glove, M((hand_at[0], hand_at[1] - 0.01, hand_at[2] - 0.04)), smooth=True, group=fore))
    # The stopped watch, in the left hand: a silver case, a pale face, its hands at twenty past four; the chain looped
    # down from it.
    wx, wy, wz = WATCH
    parts += [
        part("WatchCase", kit.cylinder(16, 0.075, 0.035), silver, M((wx, wy + 0.01, wz), rot=(90, 0, 0)), smooth=True, group="Watch"),
        part("WatchFace", kit.cylinder(16, 0.062, 0.01), face, M((wx, wy - 0.005, wz), rot=(90, 0, 0)), group="Watch"),
        part("WatchBow", kit.cylinder(8, 0.018, 0.025), silver, M((wx, wy + 0.01, wz + 0.088)), group="Watch"),
    ]
    for angle, length in ((-120, 0.035), (110, 0.05)):  # the hour hand towards four, the minute hand at twenty past
        a = math.radians(angle)
        parts.append(part("WatchHand", kit.cube(), black, M((wx + math.sin(a) * length / 2, wy - 0.012, wz + math.cos(a) * length / 2),
                                                           rot=(0, -angle, 0), scale=(0.008, 0.004, length)), group="Watch"))
    chain = [(wx + 0.01 * math.sin(t * math.pi), wy + 0.01, wz + 0.09 - 0.22 * math.sin(t * math.pi / 2) ** 0.8) for t in (k / 8 for k in range(9))]
    parts.append(part("WatchChain", kit.tube(chain, [0.008] * len(chain), seg=4), silver, group="Chain"))
    # The handkerchief, hanging from the right hand.
    parts.append(part("Handkerchief", kit.cube(), linen, M((HAND_R[0] + 0.01, HAND_R[1] - 0.03, HAND_R[2] - 0.17), rot=(4, 0, 8), scale=(0.11, 0.015, 0.22)), group="Kerchief"))
    # A lantern hung from its belt, like every Soured's (on the Spine: the game hangs its light there).
    lantern_glass = kit.mat("SouredLantern_Glow", "#b6f06a")
    parts += [
        part("BeltLanternTop", kit.cone(4, 0.06, 0.01, 0.05), iron, M((-0.24, -0.04, 0.92), rot=(0, 0, 45)), group="Spine"),
        part("BeltLanternGlass", kit.cube(), lantern_glass, M((-0.24, -0.04, 0.85), scale=(0.08, 0.08, 0.1)), group="Spine"),
        part("BeltLanternBase", kit.cube(), iron, M((-0.24, -0.04, 0.795), scale=(0.1, 0.1, 0.015)), group="Spine"),
    ]
    mesh = kit.join(parts, "Keener")
    blend(mesh, "SkirtBlend", "Hem", "Skirt", 0.28, 0.58)
    blend(mesh, "BodiceBlend", "Spine", "Chest", 1.06, 1.3)
    blend(mesh, "VeilBlend", "Veil", "Head", 1.36, 1.5)
    armature = kit.rig("KeenerRig", BONES)
    kit.skin(mesh, armature)
    kit.animate(armature, ACTIONS)
    for (name, _), worst in zip(ACTIONS.items(), REACH.values()):
        if worst > 0.02:
            print(f"[keener] {name}: the hands come off what they hold by up to {worst * 100:.0f} cm")
    return [armature, mesh]
