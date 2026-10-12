"""Architecture helpers for model scripts (import masonry): coursed stone walls, flagstone floors, plank floors,
stone steps and balustrades, in the game's directions: E east, N north, U up. The export turns Blender's -Y into the
game's north and its -X into east, so a game point (e, n, u) is Blender's (-e, -n, u).

The village's palette (the quay, its walls, its timber), so new pieces sit with the old: stone blocks in three tones
with darker mortar showing in the joints, pale capstones on wall tops (they outline a room from the camera above),
warm paving, timber."""
import kit

M, part = kit.M, kit.part

STONE = ("WallStone1", "#6f6b66"), ("WallStone2", "#7b766f"), ("WallStone3", "#65615d")
MORTAR = ("Mortar", "#3f3b38")
CAP = ("CapStone", "#8e887d")
PLINTH = ("Plinth", "#57534f")
PAVING = ("Paving1", "#8e887d"), ("Paving2", "#847d72"), ("Paving3", "#98917f")
GROUT = ("QuayStone", "#625e5b")
PLANKS = ("Plank1", "#6e5746"), ("Plank2", "#5f4b3d"), ("Plank3", "#7a6250")
TIMBER = ("Timber", "#5a4232")


def mat(spec):
    return kit.mat(*spec)


def box(name, material, e0, e1, n0, n1, u0, u1):
    """An axis-aligned box in the game's directions."""
    return part(name, kit.cube(), material, M((-(e0 + e1) / 2, -(n0 + n1) / 2, (u0 + u1) / 2),
                                               scale=(abs(e1 - e0), abs(n1 - n0), abs(u1 - u0))))


def wall(parts, e0, e1, n0, n1, u0, u1, openings=(), course=0.4, cap=True, plinth=False):
    """A stone wall filling the box, laid in courses along its longer side: blocks of varied length and tone, a hair
    proud or shy of the face, over a mortar core that shows in the joints. `openings`: (a0, a1, b0, b1) spans along
    the wall's length (a) and height (b) left open (doors, windows). A pale capstone along the top; a plinth course."""
    rng = kit.rng
    along_e = (e1 - e0) >= (n1 - n0)
    a0, a1 = (e0, e1) if along_e else (n0, n1)
    t0, t1 = (n0, n1) if along_e else (e0, e1)
    stones = [mat(s) for s in STONE]
    mortar = mat(MORTAR)

    def piece(name, material, a_lo, a_hi, b_lo, b_hi, t_lo=None, t_hi=None):
        t_lo = t0 if t_lo is None else t_lo
        t_hi = t1 if t_hi is None else t_hi
        if along_e:
            parts.append(box(name, material, a_lo, a_hi, t_lo, t_hi, b_lo, b_hi))
        else:
            parts.append(box(name, material, t_lo, t_hi, a_lo, a_hi, b_lo, b_hi))

    def open_at(a, b):
        return any(o[0] - 1e-3 < a < o[1] + 1e-3 and o[2] - 1e-3 < b < o[3] + 1e-3 for o in openings)

    # The core, in pieces around the openings (so the openings stay open).
    cuts = sorted({a0, a1, *[max(a0, min(a1, x)) for o in openings for x in (o[0], o[1])]})
    for lo, hi in zip(cuts, cuts[1:]):
        if hi - lo < 1e-3:
            continue
        mid = (lo + hi) / 2
        spans = sorted({u0, u1, *[max(u0, min(u1, x)) for o in openings if o[0] <= mid <= o[1] for x in (o[2], o[3])]})
        for b_lo, b_hi in zip(spans, spans[1:]):
            if b_hi - b_lo > 1e-3 and not open_at(mid, (b_lo + b_hi) / 2):
                piece("Core", mortar, lo, hi, b_lo, b_hi, t0 + 0.03, t1 - 0.03)
    top = u1 - (0.1 if cap else 0.0)
    row = 0
    b = u0
    while b < top - 0.02:
        h = min(course, top - b)
        a = a0 - (rng.uniform(0.2, 0.5) if row % 2 else 0.0)
        while a < a1 - 0.02:
            length = rng.uniform(0.5, 1.1)
            lo, hi = max(a, a0), min(a + length, a1)
            if hi - lo > 0.08 and not open_at((lo + hi) / 2, b + h / 2):
                # Clip the block to the openings' edges (a block never bridges into a doorway).
                for o in openings:
                    if o[2] - 1e-3 < b + h / 2 < o[3] + 1e-3:
                        if lo < o[0] < hi and (lo + hi) / 2 < o[0]:
                            hi = o[0]
                        if lo < o[1] < hi and (lo + hi) / 2 > o[1]:
                            lo = o[1]
                sink = rng.uniform(-0.012, 0.012)
                material = mat(PLINTH) if plinth and row == 0 else rng.choice(stones)
                grow = 0.04 if plinth and row == 0 else 0.0
                piece("Stone", material, lo + 0.012, hi - 0.012, b + 0.012, b + h - 0.012, t0 - sink - grow, t1 + sink + grow)
            a += length
        b += h
        row += 1
    if cap:
        capstone = mat(CAP)
        a = a0
        while a < a1 - 0.02:
            hi = min(a + rng.uniform(0.7, 1.2), a1)
            lo, top_hi = a, hi
            if not open_at((lo + top_hi) / 2, u1 - 0.05):
                # Clipped to the openings' edges too (a capstone never juts out over a gap left open to the top).
                for o in openings:
                    if o[2] - 1e-3 < u1 - 0.05 < o[3] + 1e-3:
                        if lo < o[0] < top_hi and (lo + top_hi) / 2 < o[0]:
                            top_hi = o[0]
                        if lo < o[1] < top_hi and (lo + top_hi) / 2 > o[1]:
                            lo = o[1]
                piece("Cap", capstone, lo + 0.006, top_hi - 0.006, u1 - 0.1, u1 + rng.uniform(0.0, 0.015), t0 - 0.03, t1 + 0.03)
            a = hi


def lintel(parts, e0, e1, n0, n1, u0, u1):
    """A timber lintel and jambs framing an opening (the box is the opening, through the wall)."""
    timber = mat(TIMBER)
    along_e = (e1 - e0) >= (n1 - n0)
    if along_e:
        parts.append(box("Lintel", timber, e0 - 0.12, e1 + 0.12, n0 - 0.03, n1 + 0.03, u1, u1 + 0.18))
        parts.append(box("Jamb", timber, e0 - 0.1, e0, n0 - 0.03, n1 + 0.03, u0, u1))
        parts.append(box("Jamb", timber, e1, e1 + 0.1, n0 - 0.03, n1 + 0.03, u0, u1))
    else:
        parts.append(box("Lintel", timber, e0 - 0.03, e1 + 0.03, n0 - 0.12, n1 + 0.12, u1, u1 + 0.18))
        parts.append(box("Jamb", timber, e0 - 0.03, e1 + 0.03, n0 - 0.1, n0, u0, u1))
        parts.append(box("Jamb", timber, e0 - 0.03, e1 + 0.03, n1, n1 + 0.1, u0, u1))


def _inside(e, n, rect):
    return rect[0] - 1e-3 <= e <= rect[1] + 1e-3 and rect[2] - 1e-3 <= n <= rect[3] + 1e-3


def _clear(e0, e1, n0, n1, holes):
    return not any(e0 < h[1] and e1 > h[0] and n0 < h[3] and n1 > h[2] for h in holes)


def flagstones(parts, e0, e1, n0, n1, top, holes=(), base=True, thickness=0.2):
    """Flagstones over a grout bed, top at `top`: a jittered grid of slabs in three tones, clear of `holes`
    ((e0, e1, n0, n1) rects: a stair's opening)."""
    rng = kit.rng
    paving = [mat(p) for p in PAVING]
    if base:
        grout = mat(GROUT)
        for lo_e, hi_e, lo_n, hi_n in _subtract((e0, e1, n0, n1), holes):
            parts.append(box("Bed", grout, lo_e, hi_e, lo_n, hi_n, top - thickness, top - 0.03))
    n = n0
    row = 0
    while n < n1 - 0.05:
        d = min(rng.uniform(0.6, 0.85), n1 - n)
        e = e0 - (rng.uniform(0.2, 0.4) if row % 2 else 0.0)
        while e < e1 - 0.05:
            w = rng.uniform(0.6, 1.0)
            lo, hi = max(e, e0), min(e + w, e1)
            if hi - lo > 0.12 and _clear(lo, hi, n, n + d, holes):
                parts.append(box("Flag", rng.choice(paving), lo + 0.025, hi - 0.025, n + 0.025, n + d - 0.025,
                                 top - 0.06, top + rng.uniform(-0.008, 0.008)))
            e += w
        n += d
        row += 1


def planks(parts, e0, e1, n0, n1, top, holes=(), thickness=0.2):
    """A boarded floor, top at `top`: boards running north, in three tones, butted at random lengths, over joists."""
    rng = kit.rng
    boards = [mat(p) for p in PLANKS]
    timber = mat(TIMBER)
    for lo_e, hi_e, lo_n, hi_n in _subtract((e0, e1, n0, n1), holes):
        parts.append(box("Joists", timber, lo_e, hi_e, lo_n, hi_n, top - thickness, top - 0.04))
    e = e0
    while e < e1 - 0.02:
        w = min(0.24, e1 - e)
        n = n0 - rng.uniform(0.0, 1.2)
        while n < n1 - 0.02:
            length = rng.uniform(1.4, 2.6)
            lo, hi = max(n, n0), min(n + length, n1)
            if hi - lo > 0.1:
                for lo2, hi2 in _clip_span(e, e + w, lo, hi, holes):
                    parts.append(box("Board", rng.choice(boards), e + 0.01, e + w - 0.01, lo2 + 0.008, hi2 - 0.008,
                                     top - 0.045, top + rng.uniform(-0.004, 0.004)))
            n += length
        e += w


def _clip_span(e0, e1, lo, hi, holes):
    """The parts of a board's run (lo..hi, north) that stay clear of the holes."""
    spans = [(lo, hi)]
    for h in holes:
        if e0 < h[1] and e1 > h[0]:
            out = []
            for a, b in spans:
                if b <= h[2] or a >= h[3]:
                    out.append((a, b))
                else:
                    if a < h[2]:
                        out.append((a, h[2]))
                    if b > h[3]:
                        out.append((h[3], b))
            spans = out
    return [(a, b) for a, b in spans if b - a > 0.1]


def _subtract(rect, holes):
    """A rectangle less some holes, as a list of rectangles (a coarse split: good enough for floor beds)."""
    pieces = [rect]
    for h in holes:
        out = []
        for e0, e1, n0, n1 in pieces:
            if h[0] >= e1 or h[1] <= e0 or h[2] >= n1 or h[3] <= n0:
                out.append((e0, e1, n0, n1))
                continue
            if h[0] > e0:
                out.append((e0, h[0], n0, n1))
            if h[1] < e1:
                out.append((h[1], e1, n0, n1))
            if h[2] > n0:
                out.append((max(e0, h[0]), min(e1, h[1]), n0, h[2]))
            if h[3] < n1:
                out.append((max(e0, h[0]), min(e1, h[1]), h[3], n1))
        pieces = out
    return [p for p in pieces if p[1] - p[0] > 1e-3 and p[3] - p[2] > 1e-3]


def step(parts, e0, e1, n0, n1, u0, u1, tone=0):
    """One stone step: its block, and a pale worn edge along the tread's front so each step reads from above."""
    stones = [mat(s) for s in STONE]
    parts.append(box("Step", stones[tone % 3], e0, e1, n0, n1, u0, u1 - 0.04))
    parts.append(box("Tread", mat(CAP), e0 + 0.01, e1 - 0.01, n0 + 0.01, n1 - 0.01, u1 - 0.04, u1))


def balustrade(parts, e0, n0, e1, n1, u0, height=1.0):
    """A stone balustrade from (e0, n0) to (e1, n1), standing on `u0`: square posts about every metre, a low plinth
    and a capped rail. Solid enough to stop anyone walking through it."""
    import math
    stones = [mat(s) for s in STONE]
    capstone, plinth = mat(CAP), mat(PLINTH)
    length = math.hypot(e1 - e0, n1 - n0)
    count = max(1, round(length / 1.0))
    half = 0.07
    lo_e, hi_e, lo_n, hi_n = min(e0, e1), max(e0, e1), min(n0, n1), max(n0, n1)
    if abs(e1 - e0) >= abs(n1 - n0):
        parts.append(box("RailBase", plinth, lo_e, hi_e, lo_n - half, hi_n + half, u0, u0 + 0.15))
        parts.append(box("RailWall", stones[0], lo_e, hi_e, lo_n - 0.05, hi_n + 0.05, u0 + 0.15, u0 + height - 0.1))
        parts.append(box("RailCap", capstone, lo_e - 0.02, hi_e + 0.02, lo_n - 0.09, hi_n + 0.09, u0 + height - 0.1, u0 + height))
    else:
        parts.append(box("RailBase", plinth, lo_e - half, hi_e + half, lo_n, hi_n, u0, u0 + 0.15))
        parts.append(box("RailWall", stones[0], lo_e - 0.05, hi_e + 0.05, lo_n, hi_n, u0 + 0.15, u0 + height - 0.1))
        parts.append(box("RailCap", capstone, lo_e - 0.09, hi_e + 0.09, lo_n - 0.02, hi_n + 0.02, u0 + height - 0.1, u0 + height))
    for i in range(count + 1):
        t = i / count
        e, n = e0 + (e1 - e0) * t, n0 + (n1 - n0) * t
        parts.append(box("RailPost", stones[1 + i % 2], e - 0.1, e + 0.1, n - 0.1, n + 0.1, u0, u0 + height + 0.06))


# ---------------------------------------------------------------- the Orchard of Last Autumn

def orchard():
    """Switch to the Orchard's palette (call before building): dry-stone of pale, sandy stone laid without mortar (dark
    gaps show between the stones), sun-bleached capstones, weathered timber."""
    global STONE, MORTAR, CAP, PLINTH, TIMBER
    STONE = ("Sandstone1", "#9a8a6e"), ("Sandstone2", "#a8977a"), ("Sandstone3", "#8a7a60")
    MORTAR = ("DryJoint", "#3e352b")
    CAP = ("SandCap", "#bcae8c")
    PLINTH = ("SandPlinth", "#7a6c55")
    TIMBER = ("OrchardTimber", "#5e4634")


# ---------------------------------------------------------------- the Hall of the Last Hour

def last_hour():
    """Switch to the Hall of the Last Hour's palette (call before building): pale limestone, almost cream under the moon,
    fine pale joints, cream capstones; warm honey floorboards; dark old timber."""
    global STONE, MORTAR, CAP, PLINTH, TIMBER, PLANKS, PAVING
    STONE = ("Limestone1", "#c3c1b8"), ("Limestone2", "#cdcbc2"), ("Limestone3", "#b6b4ab")
    MORTAR = ("LimeJoint", "#8a877e")
    CAP = ("LimeCap", "#e2ddcc")
    PLINTH = ("LimePlinth", "#9c998f")
    TIMBER = ("HallTimber", "#5b4532")
    PLANKS = ("HallBoard1", "#9a7a55"), ("HallBoard2", "#8c6d4b"), ("HallBoard3", "#a6855c")
    PAVING = ("HallFlag1", "#c9c5b8"), ("HallFlag2", "#bdb9ac"), ("HallFlag3", "#d3cfc2")


LEAVES = ("LeafRust", "#9a4a2c"), ("LeafOchre", "#b8873a"), ("LeafRed", "#7c2a25"), ("LeafBrown", "#7a5432"), ("LeafTan", "#9a7a4a")
TURF = ("Turf1", "#68684d"), ("Turf2", "#727050"), ("Turf3", "#5c5c45")


def turf(parts, e0, e1, n0, n1, top, density=4.0, holes=()):
    """Turf, dull olive-grey in patches, under a litter of fallen leaves (flat slips in rust, ochre, deep red and
    brown, lying every which way, thicker in drifts). `holes`: rects left bare (a stair's foot)."""
    import math
    rng = kit.rng
    tones = [mat(t) for t in TURF]
    n = n0
    while n < n1 - 0.05:
        d = min(rng.uniform(1.2, 2.2), n1 - n)
        e = e0
        while e < e1 - 0.05:
            w = min(rng.uniform(1.5, 3.0), e1 - e)
            if _clear(e, e + w, n, n + d, holes):
                parts.append(box("Turf", rng.choice(tones), e, e + w, n, n + d, top - 0.1, top + rng.uniform(-0.01, 0.01)))
            e += w
        n += d
    leaves = [mat(l) for l in LEAVES]
    count = int((e1 - e0) * (n1 - n0) * density)
    # Drifts: a few centres the leaves gather round.
    drifts = [(rng.uniform(e0, e1), rng.uniform(n0, n1)) for _ in range(max(1, count // 140))]
    for _ in range(count):
        if rng.random() < 0.75:  # (most of them in drifts: against walls, round trunks, where the air dropped them)
            ce, cn = rng.choice(drifts)
            e, n = ce + rng.gauss(0, 1.0), cn + rng.gauss(0, 1.0)
        else:
            e, n = rng.uniform(e0, e1), rng.uniform(n0, n1)
        if not (e0 + 0.05 < e < e1 - 0.05 and n0 + 0.05 < n < n1 - 0.05) or not _clear(e - 0.1, e + 0.1, n - 0.1, n + 0.1, holes):
            continue
        size = rng.uniform(0.11, 0.2)
        parts.append(part("Leaf", kit.cube(), rng.choice(leaves),
                          M((-e, -n, top + 0.012 + rng.uniform(0, 0.012)), rot=(rng.uniform(-8, 8), rng.uniform(-8, 8), rng.uniform(0, 360)),
                            scale=(size, size * 0.62, 0.008))))
