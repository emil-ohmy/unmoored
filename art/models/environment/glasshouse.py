"""The Glasshouse (the Orchard): the Gardener's palm house, beyond the far wall of the Unreaped Rows. Father Moth's
arena. A ruined glasshouse of iron ribs and broken panes on a brick plinth, moonlight coming through.

Variants:
  "ground": the terrace it stands on (32 x 26 m, its top at 3.3 like the field's, which it adjoins along its south
            edge): dry-stone retaining walls west, east and north, rough turf and leaves, a flagged path from the
            field's gate to the doors.
  "house":  the walls and floor (24 x 18 m): a brick plinth all round, iron mullions and a top rail above it, a few
            panes left in the walls, a doorway in the middle of the south wall; inside mossy flagstones, two shallow
            water channels running its length near the side walls, four raised brick beds of dead plants against them:
            the middle left open (his floor).
  "roof":   the barrel vault over it, iron ribs and what's left of the glass (it fades when you're inside).
  "leaf":   one of the doors' two leaves: an iron frame, glazing bars, two cracked panes; hung at its origin,
            reaching 1.25 m east.
  "lamp":   a lamp hung on a chain: origin at the chain's top (hang it from a rib), the lantern 2 m below.
  "tree":   the dead orange tree in its great tub, against the north wall (the orangery's last).
Game directions (see masonry.py); "ground"'s origin is the centre of its south edge at the water's surface, the
others' at the floor (the house's: the middle of its south wall)."""
import math
import masonry as m

VARIANTS = ["ground", "house", "roof", "leaf", "lamp", "tree"]
TOP = 3.3
W, D = 32.0, 26.0                 # the ground
HW, HD, WALL = 24.0, 18.0, 3.6    # the house: width, depth, wall height
RIDGE = 4.0                       # the vault's rise above the walls

IRON = ("GlasshouseIron_Metal", "#2e3a34")
GLASS = ("GlasshouseGlass", "#9db3aa")
BRICK = ("GlasshouseBrick1", "#7a4a38"), ("GlasshouseBrick2", "#6a4032"), ("GlasshouseBrick3", "#84543f")
MOSS = ("Moss", "#4e5a3a")
SOIL = ("DeadSoil", "#3a3026")
WATER = ("ChannelWater", "#14252c")
CHANNELS = (-8.6, 8.6)            # the channels' middles (e), 1.2 m wide, running the house's length
CHANNEL_N = (1.4, 16.6)           # (from near the south wall to near the north)
BEDS = [(-11.5, -10.0, 2.0, 7.4), (-11.5, -10.0, 10.6, 16.0), (10.0, 11.5, 2.0, 7.4), (10.0, 11.5, 10.6, 16.0)]
COLUMNS = []                      # (none: the vault spans it, the floor's his)


def iron():
    return m.mat(IRON)


def brick_box(parts, e0, e1, n0, n1, u0, u1, course=0.18):
    """Bricks: courses of stretchers in three tones over a dark mortar core."""
    rng = m.kit.rng
    bricks = [m.mat(b) for b in BRICK]
    parts.append(m.box("BrickCore", m.mat(m.MORTAR), e0 + 0.03, e1 - 0.03, n0 + 0.03, n1 - 0.03, u0, u1 - 0.04))  # (well inside the bricks: no face shared)
    along_e = (e1 - e0) >= (n1 - n0)
    a0, a1 = (e0, e1) if along_e else (n0, n1)
    u, row = u0, 0
    while u < u1 - 0.02:
        h = min(course, u1 - u)
        a = a0 - (0.13 if row % 2 else 0.0)
        while a < a1 - 0.02:
            lo, hi = max(a, a0), min(a + 0.26, a1)
            if hi - lo > 0.05:
                if along_e:
                    parts.append(m.box("Brick", rng.choice(bricks), lo + 0.01, hi - 0.01, n0, n1, u + 0.01, u + h - 0.01))
                else:
                    parts.append(m.box("Brick", rng.choice(bricks), e0, e1, lo + 0.01, hi - 0.01, u + 0.01, u + h - 0.01))
            a += 0.26
        u += h
        row += 1


HOUSE_AT = 4.2                    # the house's doors, in from the ground's south edge (the scene stands it there)


def ground(parts):
    e0, e1, n0, n1 = -W / 2, W / 2, 0.0, D
    bottom, t = -0.9, 0.45
    # The earth: up under the turf, and sunk under the house (it brings its own floor, its channels dug below it).
    h = (-HW / 2, HW / 2, HOUSE_AT, HOUSE_AT + HD)
    earth = m.mat(m.MORTAR)
    for (a0, a1, b0, b1) in [(e0 + 0.3, e1 - 0.3, n0, h[2]), (e0 + 0.3, e1 - 0.3, h[3], n1 - 0.3), (e0 + 0.3, h[0], h[2], h[3]), (h[1], e1 - 0.3, h[2], h[3])]:
        parts.append(m.box("Earth", earth, a0, a1, b0, b1, bottom, TOP - 0.1))
    parts.append(m.box("Earth", earth, h[0], h[1], h[2], h[3], bottom, TOP - 0.8))
    m.wall(parts, e0, e0 + t, n0, n1, bottom, TOP, course=0.45)
    m.wall(parts, e1 - t, e1, n0, n1, bottom, TOP, course=0.45)
    m.wall(parts, e0 + t, e1 - t, n1 - t, n1, bottom, TOP, course=0.45)
    path = (-1.0, 1.0, n0, 4.2)  # (the field's gate to the doors)
    m.turf(parts, e0 + t, e1 - t, n0, n1 - t, TOP + 0.02, density=3.0, holes=[path, h])
    m.flagstones(parts, path[0], path[1], path[2], path[3], TOP + 0.04)


def house(parts):
    rng = m.kit.rng
    e0, e1, n0, n1 = -HW / 2, HW / 2, 0.0, HD
    door = (-1.3, 1.3)
    t = 0.3
    # The floor: flagstones, with the channels left open (stone-lined, shallow water in them), moss in the cracks.
    holes = [(c - 0.6, c + 0.6, CHANNEL_N[0], CHANNEL_N[1]) for c in CHANNELS]
    m.flagstones(parts, e0 + t, e1 - t, n0 + t, n1 - t, 0.06, holes=holes)
    moss = m.mat(MOSS)
    for _ in range(40):
        e, n = rng.uniform(e0 + 0.5, e1 - 0.5), rng.uniform(n0 + 0.5, n1 - 0.5)
        if any(abs(e - c) < 0.8 for c in CHANNELS) or any(a0 - 0.4 < e < a1 + 0.4 and b0 - 0.4 < n < b1 + 0.4 for (a0, a1, b0, b1) in BEDS):
            continue
        s = rng.uniform(0.3, 0.8)
        parts.append(m.part("Moss", m.kit.cube(), moss, m.M((-e, -n, 0.065), rot=(0, 0, rng.uniform(0, 90)), scale=(s, s * rng.uniform(0.4, 0.9), 0.01))))
    water = m.mat(WATER)
    for c in CHANNELS:
        parts.append(m.box("ChannelBed", water, c - 0.6, c + 0.6, CHANNEL_N[0] - 0.04, CHANNEL_N[1] + 0.04, -0.6, -0.35))  # (the water itself: the scene's, laid over it)
        for side in (-1, 1):
            parts.append(m.box("Kerb", m.mat(m.CAP), c + side * 0.6 - 0.08, c + side * 0.6 + 0.08, CHANNEL_N[0] - 0.08, CHANNEL_N[1] + 0.08, -0.35, 0.1))
        for nn in CHANNEL_N:
            parts.append(m.box("KerbEnd", m.mat(m.CAP), c - 0.52, c + 0.52, nn - 0.08, nn + 0.08, -0.35, 0.1))  # (astride the floor's edge, like the sides)
    # The plinth: brick, knee high, all round (the doorway left open), a stone sill on top.
    sill = m.mat(m.CAP)
    for (a0, a1, b0, b1) in [(e0, door[0], n0, n0 + t), (door[1], e1, n0, n0 + t), (e0, e1, n1 - t, n1), (e0, e0 + t, n0 + t, n1 - t), (e1 - t, e1, n0 + t, n1 - t)]:
        brick_box(parts, a0, a1, b0, b1, 0.0, 0.6)
        ends = 0.03 if b1 - b0 < 1.0 else -0.03  # (the side walls' sills butt against the front's and back's: no overlap)
        parts.append(m.box("Sill", sill, a0 - 0.03, a1 + 0.03, b0 - ends, b1 + ends, 0.6, 0.66))
    # Iron above it: mullions every 1.6 m, a rail at the top, a few panes left (the rest broken out).
    glass = m.mat(GLASS)
    ir = iron()

    def side(along_e, fixed, lo, hi, skip=None):
        a = lo
        while a <= hi + 1e-3:
            if not (skip and skip[0] - 0.1 < a < skip[1] + 0.1):
                if along_e:
                    parts.append(m.box("Mullion", ir, a - 0.05, a + 0.05, fixed - 0.05, fixed + 0.05, 0.66, WALL - 0.04))
                else:
                    parts.append(m.box("Mullion", ir, fixed - 0.05, fixed + 0.05, a - 0.05, a + 0.05, 0.66, WALL - 0.04))
            nxt = a + 1.6
            if nxt <= hi + 1e-3 and rng.random() < 0.45 and not (skip and a < skip[1] and nxt > skip[0]):
                u0 = rng.uniform(0.7, 1.6)
                u1 = min(WALL - 0.05, u0 + rng.uniform(0.8, 1.8))
                if along_e:
                    parts.append(m.box("Pane", glass, a + 0.06, nxt - 0.06, fixed - 0.01, fixed + 0.01, u0, u1))
                else:
                    parts.append(m.box("Pane", glass, fixed - 0.01, fixed + 0.01, a + 0.06, nxt - 0.06, u0, u1))
            a = nxt
        if along_e:
            parts.append(m.box("Rail", ir, lo - 0.06, hi + 0.06, fixed - 0.07, fixed + 0.07, WALL - 0.12, WALL))
        else:
            parts.append(m.box("Rail", ir, fixed - 0.07, fixed + 0.07, lo - 0.06, hi + 0.06, WALL - 0.12, WALL))

    side(True, n0 + t / 2, e0, e1, skip=door)
    side(True, n1 - t / 2, e0, e1)
    side(False, e0 + t / 2, n0, n1)
    side(False, e1 - t / 2, n0, n1)
    # The doorway: a lintel of iron over it, posts each side.
    parts.append(m.box("Lintel", ir, door[0] - 0.1, door[1] + 0.1, n0, n0 + t, 2.7, 2.9))
    for d in door:
        parts.append(m.box("DoorPost", ir, d - 0.08, d + 0.08, n0 - 0.05, n0 + t + 0.05, -0.02, 2.9))  # (proud of the bricks)
    # The beds: brick-edged, dead soil, dead stalks and a few brown husks of plants.
    stalk = m.mat(("DeadStalk", "#6a5a42"))
    for (a0, a1, b0, b1) in BEDS:
        # (the long sides full length, the ends between them: no corner shared; the soil kept off the bricks)
        long_e = (a1 - a0) >= (b1 - b0)
        edges = ([(a0, a1, b0, b0 + 0.18), (a0, a1, b1 - 0.18, b1), (a0, a0 + 0.18, b0 + 0.2, b1 - 0.2), (a1 - 0.18, a1, b0 + 0.2, b1 - 0.2)] if long_e
                 else [(a0, a0 + 0.18, b0, b1), (a1 - 0.18, a1, b0, b1), (a0 + 0.2, a1 - 0.2, b0, b0 + 0.18), (a0 + 0.2, a1 - 0.2, b1 - 0.18, b1)])
        for (x0, x1, y0, y1) in edges:
            brick_box(parts, x0, x1, y0, y1, 0.06, 0.5)
        parts.append(m.box("Soil", m.mat(SOIL), a0 + 0.2, a1 - 0.2, b0 + 0.2, b1 - 0.2, 0.06, 0.43))
        for _ in range(16):
            e, n = rng.uniform(a0 + 0.3, a1 - 0.3), rng.uniform(b0 + 0.3, b1 - 0.3)
            h = rng.uniform(0.3, 0.9)
            lean = (rng.uniform(-0.25, 0.25), rng.uniform(-0.25, 0.25))
            tip = (-(e + lean[0]), -(n + lean[1]), 0.4 + h)
            parts.append(m.part("Stalk", m.kit.cone(3, 0.02, 0.006, math.dist((-e, -n, 0.4), tip)), stalk, m.kit.along((-e, -n, 0.4), tip)))
    # The columns: iron, up to the vault, a flare at the head.
    for (e, n) in COLUMNS:
        parts.append(m.part("Column", m.kit.cylinder(8, 0.14, WALL + RIDGE - 0.4), ir, m.M((-e, -n, (WALL + RIDGE - 0.4) / 2))))
        parts.append(m.part("ColumnFoot", m.kit.cylinder(8, 0.24, 0.3), ir, m.M((-e, -n, 0.15))))
        parts.append(m.part("ColumnHead", m.kit.cone(8, 0.14, 0.32, 0.4), ir, m.M((-e, -n, WALL + RIDGE - 0.6))))


def roof(parts):
    """Ribs every 2 m across the house (arcs over the depth), purlins along it, a third of the glass left."""
    rng = m.kit.rng
    ir, glass = iron(), m.mat(GLASS)
    e0, e1 = -HW / 2, HW / 2
    steps = 10

    def arc(k):
        th = math.pi * k / steps
        return HD / 2 - math.cos(th) * HD / 2, WALL + math.sin(th) * RIDGE

    ribs = []
    e = e0
    while e <= e1 + 1e-3:
        pts = [(-e, -arc(k)[0], arc(k)[1]) for k in range(steps + 1)]
        parts.append(m.part("Rib", m.kit.tube(pts, [0.06] * len(pts), seg=4), ir))
        ribs.append(e)
        e += 2.0
    for k in range(1, steps):
        n, u = arc(k)
        parts.append(m.box("Purlin", ir, e0, e1, n - 0.04, n + 0.04, u - 0.04, u + 0.04))
    for i in range(len(ribs) - 1):
        for k in range(steps):
            if rng.random() > 0.33:
                continue
            (na, ua), (nb, ub) = arc(k), arc(k + 1)
            mid_n, mid_u = (na + nb) / 2, (ua + ub) / 2
            tilt = math.degrees(math.atan2(ub - ua, nb - na))
            length = math.hypot(nb - na, ub - ua)
            parts.append(m.part("Pane", m.kit.cube(), glass, m.M((-(ribs[i] + ribs[i + 1]) / 2, -mid_n, mid_u), rot=(tilt, 0, 0), scale=(1.9, length - 0.04, 0.02))))


def leaf(parts):
    ir, glass = iron(), m.mat(GLASS)
    w, h = 1.25, 2.6
    parts += [
        m.box("Stile", ir, 0.02, 0.1, -0.04, 0.04, 0.0, h),
        m.box("Stile", ir, w - 0.08, w, -0.04, 0.04, 0.0, h),
        m.box("TopRail", ir, 0.02, w, -0.04, 0.04, h - 0.08, h),
        m.box("MidRail", ir, 0.02, w, -0.04, 0.04, 1.0, 1.08),
        m.box("BottomRail", ir, 0.02, w, -0.04, 0.04, 0.05, 0.13),
        m.box("Bar", ir, w / 2 - 0.025, w / 2 + 0.025, -0.03, 0.03, 0.13, h - 0.08),
        m.box("Pane", glass, 0.1, w / 2 - 0.03, -0.01, 0.01, 1.08, h - 0.1),
        m.box("Pane", glass, w / 2 + 0.03, w - 0.08, -0.01, 0.01, 1.4, h - 0.1),  # (one cracked, its top half gone)
        m.box("Kick", m.mat(BRICK[0]), 0.1, w - 0.08, -0.02, 0.02, 0.13, 1.0),
    ]


def lamp(parts):
    ir = iron()
    glow = m.kit.mat("GlasshouseLamp_Glow", "#ffcf88")
    for k in range(8):  # the chain: links alternating
        parts.append(m.part("Link", m.kit.cube(), ir, m.M((0, 0, -0.12 - k * 0.22), rot=(0, 0, 90 * (k % 2)), scale=(0.08, 0.02, 0.18))))
    parts += [
        m.part("Hood", m.kit.cone(6, 0.3, 0.06, 0.25), ir, m.M((0, 0, -1.95))),
        m.part("Glass", m.kit.cylinder(6, 0.2, 0.4), glow, m.M((0, 0, -2.25))),
        m.part("Base", m.kit.cylinder(6, 0.24, 0.05), ir, m.M((0, 0, -2.47))),
    ]
    for k in range(6):
        a = math.tau * k / 6
        parts.append(m.part("Cage", m.kit.cube(), ir, m.M((math.cos(a) * 0.22, math.sin(a) * 0.22, -2.25), scale=(0.025, 0.025, 0.46))))


def tree(parts):
    rng = m.kit.rng
    wood, band = m.mat(("TubOak", "#5a3e2a")), iron()
    bark = m.mat(("DeadBark", "#3e3229"))
    parts += [
        m.part("Tub", m.kit.cone(12, 0.85, 1.0, 0.9), wood, m.M((0, 0, 0.45))),
        m.part("TubBand", m.kit.cylinder(12, 1.0, 0.06), band, m.M((0, 0, 0.2))),
        m.part("TubBand", m.kit.cylinder(12, 0.9, 0.06), band, m.M((0, 0, 0.75))),
        m.part("Soil", m.kit.cylinder(12, 0.82, 0.04), m.mat(SOIL), m.M((0, 0, 0.88))),
    ]
    trunk = [(0, 0, 0.88), (0.05, 0.02, 1.6), (-0.04, 0.06, 2.3), (0.02, 0.0, 2.8)]
    parts.append(m.part("Trunk", m.kit.tube(trunk, [0.14, 0.11, 0.09, 0.06], seg=6), bark, jitter=0.01))
    for k in range(7):
        a = math.tau * k / 7 + rng.uniform(-0.2, 0.2)
        base = trunk[2] if k % 2 else trunk[1]
        r = rng.uniform(0.9, 1.5)
        mid = (base[0] + math.cos(a) * r * 0.5, base[1] + math.sin(a) * r * 0.5, base[2] + rng.uniform(0.3, 0.6))
        tip = (base[0] + math.cos(a) * r, base[1] + math.sin(a) * r, mid[2] + rng.uniform(-0.1, 0.5))
        parts.append(m.part("Branch", m.kit.tube([base, mid, tip], [0.06, 0.035, 0.012], seg=5), bark))
    # A few withered oranges still hanging on.
    rind = m.mat(("WitheredOrange", "#8a5a28"))
    for _ in range(5):
        a = rng.uniform(0, math.tau)
        parts.append(m.part("Orange", m.kit.sphere(6, 4, 0.06), rind, m.M((math.cos(a) * rng.uniform(0.5, 1.1), math.sin(a) * rng.uniform(0.5, 1.1), rng.uniform(2.0, 2.8)))))


def build(variant):
    m.orchard()
    parts = []
    {"ground": ground, "house": house, "roof": roof, "leaf": leaf, "lamp": lamp, "tree": tree}[variant](parts)
    return [m.kit.join(parts, f"Glasshouse_{variant}")]
