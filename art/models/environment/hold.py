"""The Hold: where the game begins, before the Hub. A flooded undercroft in the dark: black water, a forest of tall
wooden pilings rising out of sight, drowned masonry, broken boardwalks, slime, iron bands and chain. Nothing overhead is
modelled over the way (the dark is the ceiling), but for a scrap of plank ceiling at the top of the Climb.
(Never said in the game: it's the Ferryman's hold, under his great jetty, where he keeps what he's taken; the last
fight is here, when the water drains.)

The way, south to north (the camera looks north):
- the Waking: a lone round stone (the cut-off top of an old pier), where the Apprentice wakes: the origin;
- the Boardwalk: a broken timber walk on posts, planks gone here and there, a handrail half gone;
- the Foundations: a stone ledge turning east to a drowned wall (big blocks, arches half under water, iron rings),
  then north along it (open above: the game's shafts of grey light fall here);
- the Pens: a timber platform of kept things (heaps of dead lanterns, bundles in rope and chain, boats pulled up and
  overturned, cages hung from tall posts, their chains running up into the dark);
- the Mooring islet: a small round of stone (its middle clear: the game's Mooring post);
- the Basin's edge: broad steps down to a ledge round the west rim of a vast flooded basin, a great capstan on a pier
  in its middle, its chains rising straight up into the dark;
- the Climb: a timber stair round four posts up to a landing under a hatch in a scrap of plank ceiling.
Pilings fill the rest, hundreds, denser away from the way. Any that would stand between the game's camera (15 m back,
50 degrees down, from the south) and the way are cut down to stumps.

Variants: "hold" (all of it; the game places it at the origin); "lantern_dead" (one of the small unlit lanterns heaped in
the Pens, for the game to scatter: origin at its base).
Game directions (masonry: E east, N north, U up); the water's surface is u = 0 (the game draws it).
"""
import math
import random

import bmesh
import bpy
from mathutils import Matrix, Vector

import masonry as m

kit = m.kit
M = kit.M
VARIANTS = ["hold", "lantern_dead"]

DEEP = -2.5           # how far everything goes down into the water
TOP = 0.5             # walkable tops
LEDGE = 0.3           # the Basin's ledge
TAN = math.tan(math.radians(50))   # the camera's pitch: a ray to the way rises this much a metre south
BE, BN = 8.0, 52.0    # the Basin's middle (the capstan)
RIM_IN, RIM_OUT = 14.62, 15.36
ARC_IN, ARC_OUT, ARC_A0, ARC_A1 = 15.42, 17.9, 241.0, 172.0   # the ledge round the Basin (radii; angles, from east)
CORE = (-14.0, -10.0, 57.0, 61.0)  # the Climb's square of posts (e0, e1, n0, n1)
FW = 1.3              # a flight's width
STAIR_TOP = 10.0
RISE = (STAIR_TOP - LEDGE) / 39    # three flights of 13 risers
RUN = 4.0 / 12
LAND_E, LAND_N = -8.0, 63.8   # the top landing: the core's top, and a strip east of it running north to under the hatch
HATCH = (-9.0, 62.8)  # the hatch's middle (2 m square), in the ceiling at CEIL_U
CEIL_U = 12.5
CEILING = (-12.0, -6.0, 61.1, 66.9)   # the scrap of ceiling (e0, e1, n0, n1): north of the landing's open part


def G(e, n, u):
    return (-e, -n, u)


# ---------------------------------------------------------------- materials

def materials():
    c = kit.mat
    return dict(
        stone=[c("HoldStone1", "#3b3d3f"), c("HoldStone2", "#45474a"), c("HoldStone3", "#333537")],
        edge=c("HoldStoneEdge", "#58595a"),     # edge stones, a little paler: the water's edge reads
        wet=c("HoldStoneWet", "#26292a"),
        dark=c("HoldStoneDark", "#1b1c1d"),     # masonry high up, fading into the dark
        joint=c("HoldJoint", "#111213"),
        slime=c("HoldSlime", "#1b2419"),
        lichen=c("HoldLichen", "#7f8579"),
        wood=c("HoldWood", "#4a4038"),
        rot=c("HoldWoodRot", "#342e29"),
        pale=c("HoldWoodPale", "#5e564c"),
        pile=c("HoldPile", "#3d362f"),
        pile_up=c("HoldPileUpper", "#25211d"),
        pile_top=c("HoldPileTop", "#121110"),
        iron=c("HoldIron_Metal", "#2d2b2a"),
        rust=c("HoldRust", "#3d3029"),
        chain=c("HoldChain_Metal", "#302e2d"),
        rope=c("HoldRope", "#8d8676"),
        sack=c("HoldSacking", "#48433b"),
        sack_pale=c("HoldSackingPale", "#655e52"),
        boat=c("HoldBoat", "#332f2a"),
        boat_paint=c("HoldBoatPaint", "#565c60"),
        lantern=c("HoldLanternDead_Metal", "#2a2827"),
        glass=c("HoldGlassDead", "#70736c"),
        void=c("HoldVoid", "#050505"),
    )


# ---------------------------------------------------------------- one mesh, built up piece by piece

class Acc:
    """Everything goes into one mesh (thousands of pieces: faster than an object each)."""

    def __init__(self):
        self.verts, self.faces, self.fmat, self.mats, self.index = [], [], [], [], {}

    def mi(self, material):
        if material.name not in self.index:
            self.index[material.name] = len(self.mats)
            self.mats.append(material)
        return self.index[material.name]

    def add(self, build, material, matrix=None):
        bm = bmesh.new()
        build(bm)
        if matrix is not None:
            bmesh.ops.transform(bm, matrix=matrix, verts=bm.verts)
        bm.verts.index_update()
        base = len(self.verts)
        self.verts.extend(tuple(v.co) for v in bm.verts)
        k = self.mi(material)
        for f in bm.faces:
            self.faces.append(tuple(base + v.index for v in f.verts))
            self.fmat.append(k)
        bm.free()

    def stamp(self, tmpl, material, matrix):
        verts, faces = tmpl
        base = len(self.verts)
        self.verts.extend(tuple(matrix @ v) for v in verts)
        k = self.mi(material)
        for f in faces:
            self.faces.append(tuple(base + i for i in f))
            self.fmat.append(k)

    def raw(self, verts, faces, materials):
        base = len(self.verts)
        self.verts.extend(verts)
        for f, mat in zip(faces, materials):
            self.faces.append(tuple(base + i for i in f))
            self.fmat.append(self.mi(mat))

    def build(self, name):
        me = bpy.data.meshes.new(name)
        me.from_pydata(self.verts, [], self.faces)
        for mat in self.mats:
            me.materials.append(mat)
        me.polygons.foreach_set("material_index", self.fmat)
        me.polygons.foreach_set("use_smooth", [False] * len(self.faces))
        me.validate(clean_customdata=False)
        me.update()
        ob = bpy.data.objects.new(name, me)
        bpy.context.scene.collection.objects.link(ob)
        return ob


def template(build, matrix=None):
    bm = bmesh.new()
    build(bm)
    if matrix is not None:
        bmesh.ops.transform(bm, matrix=matrix, verts=bm.verts)
    bm.verts.index_update()
    t = ([v.co.copy() for v in bm.verts], [tuple(v.index for v in f.verts) for f in bm.faces])
    bm.free()
    return t


# ---------------------------------------------------------------- shapes (game coordinates)

def box(acc, mat, e0, e1, n0, n1, u0, u1):
    acc.add(kit.cube(), mat, M(G((e0 + e1) / 2, (n0 + n1) / 2, (u0 + u1) / 2), scale=(abs(e1 - e0), abs(n1 - n0), abs(u1 - u0))))


def obox(acc, mat, c, size, heading=0.0, pitch=0.0, roll=0.0):
    """A box centred at c, size (across, along, up), its length pointing at `heading` (degrees, clockwise from north);
    `pitch` raises its far end, `roll` tips it about its length."""
    acc.add(kit.cube(), mat, M(G(*c), rot=(pitch, roll, 180.0 - heading), scale=size))


def beam(acc, mat, p0, p1, w, h, roll=0.0):
    de, dn, du = p1[0] - p0[0], p1[1] - p0[1], p1[2] - p0[2]
    flat = math.hypot(de, dn)
    length = math.sqrt(flat * flat + du * du)
    c = ((p0[0] + p1[0]) / 2, (p0[1] + p1[1]) / 2, (p0[2] + p1[2]) / 2)
    obox(acc, mat, c, (w, length, h), math.degrees(math.atan2(de, dn)), math.degrees(math.atan2(du, flat)), roll)


def rail(acc, mat, p0, p1, w, h, trim=0.05):
    """A beam from p0 to p1, a hair short at both ends (rails meet at corners without overlapping)."""
    d = [b - a for a, b in zip(p0, p1)]
    length = math.sqrt(sum(x * x for x in d))
    t = min(trim / length, 0.2)
    beam(acc, mat, tuple(a + x * t for a, x in zip(p0, d)), tuple(b - x * t for b, x in zip(p1, d)), w, h)


def rod(acc, mat, p0, p1, r, seg=6, r1=None):
    length = math.dist(p0, p1)
    acc.add(kit.cone(seg, r, r if r1 is None else r1, length), mat, kit.along(G(*p0), G(*p1)))


def prism(acc, mat, outline, u0, u1):
    """A closed outline (e, n) extruded from u0 to u1 (caps triangulated: outlines may be concave)."""
    def build(bm):
        pts = [G(e, n, 0)[:2] for e, n in outline]
        bottom = [bm.verts.new((x, y, u0)) for x, y in pts]
        top = [bm.verts.new((x, y, u1)) for x, y in pts]
        k = len(pts)
        fb = bm.faces.new(list(reversed(bottom)))
        ft = bm.faces.new(top)
        for i in range(k):
            bm.faces.new((bottom[i], bottom[(i + 1) % k], top[(i + 1) % k], top[i]))
        bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
        bmesh.ops.triangulate(bm, faces=[fb, ft])
    acc.add(build, mat)


def sector(ce, cn, r0, r1, a0, a1, steps=1):
    """An annular sector's outline (angles in degrees, from east, anticlockwise)."""
    out = [(ce + r1 * math.cos(math.radians(a0 + (a1 - a0) * i / steps)), cn + r1 * math.sin(math.radians(a0 + (a1 - a0) * i / steps))) for i in range(steps + 1)]
    inn = [(ce + r0 * math.cos(math.radians(a0 + (a1 - a0) * i / steps)), cn + r0 * math.sin(math.radians(a0 + (a1 - a0) * i / steps))) for i in range(steps, -1, -1)]
    return out + inn


def circle(ce, cn, r, k=24, phase=0.0):
    return [(ce + r * math.cos(2 * math.pi * i / k + phase), cn + r * math.sin(2 * math.pi * i / k + phase)) for i in range(k)]


def loop_tube(points, r, sides, normal):
    """A closed tube round a loop of (Blender) points lying in a plane with this normal (rings, links, straps)."""
    pts = [Vector(p) for p in points]
    nrm = Vector(normal).normalized()

    def build(bm):
        rings = []
        k = len(pts)
        for i, p in enumerate(pts):
            t = (pts[(i + 1) % k] - pts[i - 1]).normalized()
            out = t.cross(nrm).normalized()
            rings.append([bm.verts.new(p + out * (r * math.cos(2 * math.pi * j / sides)) + nrm * (r * math.sin(2 * math.pi * j / sides)))
                          for j in range(sides)])
        for i in range(k):
            a, b = rings[i], rings[(i + 1) % k]
            for j in range(sides):
                bm.faces.new((a[j], a[(j + 1) % sides], b[(j + 1) % sides], b[j]))
        bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    return build


_links = {}


def link_template(length, thick, seg=8, sides=4):
    """A chain link: an oval ring, long along local Z, in the XZ plane."""
    key = (round(length, 4), round(thick, 4), seg, sides)
    if key not in _links:
        width = length * 0.62
        rx, rz = width / 2 - thick / 2, length / 2 - thick / 2
        pts = [(rx * math.cos(2 * math.pi * i / seg), 0.0, rz * math.sin(2 * math.pi * i / seg)) for i in range(seg)]
        _links[key] = template(loop_tube(pts, thick / 2, sides, (0, 1, 0)))
    return _links[key]


def chain(acc, mat, path, length=0.2, thick=0.03, seg=8, sides=4, closed=False):
    """Links along a polyline of game points, each turned a quarter from the last."""
    tm = link_template(length, thick, seg, sides)
    pts = [Vector(G(*p)) for p in path]
    if closed:
        pts.append(pts[0])
    cum = [0.0]
    for a, b in zip(pts, pts[1:]):
        cum.append(cum[-1] + (b - a).length)
    total = cum[-1]

    def at(s):
        s = max(0.0, min(total, s))
        for i in range(len(pts) - 1):
            if s <= cum[i + 1] or i == len(pts) - 2:
                seg_len = max(1e-6, cum[i + 1] - cum[i])
                return pts[i].lerp(pts[i + 1], (s - cum[i]) / seg_len)
        return pts[-1]

    pitch = length - 1.7 * thick
    count = max(1, int(total / pitch))
    pitch = total / count if closed else pitch
    for i in range(count):
        s = pitch * (i + 0.5)
        p = at(s)
        d = at(s + pitch / 2) - at(s - pitch / 2)
        if d.length < 1e-6:
            continue
        d.normalize()
        q = d.to_track_quat("Z", "Y" if abs(d.y) < 0.95 else "X")
        acc.stamp(tm, mat, Matrix.Translation(p) @ q.to_matrix().to_4x4() @ Matrix.Rotation(math.radians(90 * (i % 2)), 4, "Z"))


def ring(acc, mat, c, radius, thick, facing, sides=4, seg=12, arc=360.0, start=0.0):
    """An iron ring (or a broken arc of one) centred at c, facing `facing` ('e' or 'n': upright, its face that way;
    'u': lying flat)."""
    k = seg if arc >= 360 else seg + 1
    pts = []
    for i in range(k):
        a = math.radians(start + arc * i / seg)
        x, y = radius * math.cos(a), radius * math.sin(a)
        if facing == "u":
            pts.append(G(c[0] + x, c[1] + y, c[2]))
        elif facing == "e":
            pts.append(G(c[0], c[1] + x, c[2] + y))
        else:
            pts.append(G(c[0] + x, c[1], c[2] + y))
    if arc >= 360:
        nrm = {"u": (0, 0, 1), "e": (1, 0, 0), "n": (0, 1, 0)}[facing]
        acc.add(loop_tube(pts, thick, sides, nrm), mat)
    else:
        acc.add(kit.tube(pts, [thick] * len(pts), seg=sides), mat)


# ---------------------------------------------------------------- the way (for keeping it clear, and the camera's view of it)

def inside(e, n, poly):
    hit = False
    for (x1, y1), (x2, y2) in zip(poly, poly[1:] + poly[:1]):
        if (y1 > n) != (y2 > n) and e < x1 + (n - y1) * (x2 - x1) / (y2 - y1):
            hit = not hit
    return hit


def poly_dist(e, n, poly):
    if inside(e, n, poly):
        return 0.0
    best = 1e9
    for (x1, y1), (x2, y2) in zip(poly, poly[1:] + poly[:1]):
        dx, dy = x2 - x1, y2 - y1
        t = max(0.0, min(1.0, ((e - x1) * dx + (n - y1) * dy) / max(1e-9, dx * dx + dy * dy)))
        best = min(best, math.hypot(e - x1 - t * dx, n - y1 - t * dy))
    return best


def strip(p0, p1, half):
    """A straight strip's outline, `half` either side of p0-p1."""
    de, dn = p1[0] - p0[0], p1[1] - p0[1]
    length = math.hypot(de, dn)
    pe, pn = -dn / length * half, de / length * half
    return [(p0[0] + pe, p0[1] + pn), (p1[0] + pe, p1[1] + pn), (p1[0] - pe, p1[1] - pn), (p0[0] - pe, p0[1] - pn)]


WAKING = circle(0.0, 0.0, 2.25, 24)
BOARD = [(-1.0, 2.0), (1.0, 2.0), (1.0, 13.9), (-1.0, 13.9)]
FOUND_A = [(-1.3, 13.8), (1.3, 13.8), (9.25, 17.77), (6.75, 19.33), (-1.3, 15.3)]
FOUND_B = [(6.75, 19.33), (9.25, 17.77), (9.25, 26.5), (6.75, 26.5)]
PENS = [(1.5, 26.5), (10.5, 26.5), (10.5, 33.5), (3.0, 33.5), (1.5, 31.2)]
CAUSEWAY = strip((2.0, 32.0), (0.25, 33.75), 0.6)
ISLET = circle(-1.0, 35.0, 2.0, 20)
STEPS = [(-2.7, 36.0), (0.1, 36.0), (0.1, 37.6), (-2.7, 37.6)]
ARC = sector(BE, BN, ARC_IN, ARC_OUT, ARC_A1, ARC_A0, 25)
PAD = [(-9.73, 54.49), (-7.27, 54.15), (-6.96, 55.73), (-6.9, 57.3), (-10.0, 57.3), (-10.0, 55.0)]
TOWER = [(-15.3, 55.7), (-8.0, 55.7), (-8.0, 63.8), (-15.3, 63.8)]
WALK = [(WAKING, TOP), (BOARD, TOP), (FOUND_A, TOP), (FOUND_B, TOP), (PENS, TOP), (CAUSEWAY, TOP), (ISLET, TOP),
        (STEPS, 0.4), (ARC, LEDGE), (PAD, LEDGE), (TOWER, LEDGE)]
PENS_PATH = [(8.0, 26.8), (5.2, 29.4), (2.3, 31.9)]  # kept clear of the hoard


def way_dist(e, n):
    return min(poly_dist(e, n, poly) for poly, _ in WALK)


def seg_dist(e, n, path):
    best = 1e9
    for (x1, y1), (x2, y2) in zip(path, path[1:]):
        dx, dy = x2 - x1, y2 - y1
        t = max(0.0, min(1.0, ((e - x1) * dx + (n - y1) * dy) / (dx * dx + dy * dy)))
        best = min(best, math.hypot(e - x1 - t * dx, n - y1 - t * dy))
    return best


def flight_points():
    """Points on the Climb's treads and landings (e, n, u)."""
    pts = []
    e0, e1, n0, n1 = CORE
    for k in range(13):
        t = k / 12
        pts.append((e1 - 4.0 * t, n0 - FW / 2, LEDGE + RISE * 12 * t))            # F1: south side, west
        pts.append((e0 - FW / 2, n0 + 4.0 * t, LEDGE + RISE * (13 + 12 * t)))      # F2: west side, north
        pts.append((e0 + 4.0 * t, n1 + FW / 2, LEDGE + RISE * (26 + 12 * t)))      # F3: north side, east
    for e in (-13.5, -12.0, -10.5, -9.0):
        for n in (57.5, 59.0, 60.5):
            pts.append((e, n, STAIR_TOP))
    for n in (61.5, 62.5, 63.4):
        pts.append((-9.0, n, STAIR_TOP))
    return pts


def view_samples():
    samples = []
    for poly, u in WALK:
        if poly is TOWER:
            continue
        es, ns = [p[0] for p in poly], [p[1] for p in poly]
        e = min(es)
        while e <= max(es):
            n = min(ns)
            while n <= max(ns):
                if inside(e, n, poly):
                    samples.append((e, n, u))
                n += 0.7
            e += 0.7
        for p in poly:  # (its corners too: narrow strips)
            samples.append((p[0], p[1], u))
    samples += flight_points()
    bins = {}
    for s in samples:
        bins.setdefault(math.floor(s[0]), []).append(s)
    return bins


SAMPLES = None


def view_ceiling(e, n, r=0.0):
    """How tall something at (e, n) may stand before it hides the way from the camera (south of it, looking down at
    50 degrees, 15 m back)."""
    best = 1e9
    reach = 0.5 + r
    for b in range(math.floor(e - reach), math.floor(e + reach) + 1):
        for se, sn, su in SAMPLES.get(b, ()):
            dn = sn - n
            if 0.0 < dn < 9.8 and abs(se - e) < reach:
                best = min(best, su + 0.2 + dn * TAN)
    return best


def clear_view(points, r=0.0):
    return all(u <= view_ceiling(e, n, r) for e, n, u in points)


# ---------------------------------------------------------------- pilings

class Pile:
    def __init__(self, e, n, r, top, lean, base=DEEP):
        self.e, self.n, self.r, self.top, self.lean, self.base = e, n, r, top, lean, base

    def axis(self, u):
        return (self.e + self.lean[0] * (u - self.base), self.n + self.lean[1] * (u - self.base), u)


def piling(acc, MT, rng, e, n, r, top, lean=(0.0, 0.0), seg=7, crust=0.35, snapped=False, crook=0.025, base=DEEP, step=True):
    """A timber pile from under the water up to `top`: a crust of slime at the waterline, then rotted timber getting
    darker up its length (so it fades up into the dark); crooked; snapped ones end in splinters."""
    p = Pile(e, n, r, top, lean, base)
    wood_end, upper_end = rng.uniform(2.6, 3.6), rng.uniform(5.0, 6.6)
    rings = [(base, r + 0.035)]
    if top > crust + 0.15:
        rings += [(crust, r + 0.035), (crust + 0.03, r)] if step else [(crust, r + 0.02)]
        for z, rr in ((wood_end, r * 0.97), (upper_end, r * 0.93)):
            if z < top - 0.25:
                rings.append((z, rr))
        rings.append((top, r * 0.9))
    else:
        rings.append((top, r + 0.03))
    verts, faces, mats = [], [], []
    phase = rng.uniform(0, 2 * math.pi)
    for i, (z, rr) in enumerate(rings):
        ce, cn, _ = p.axis(z)
        if 0 < i:
            ce += rng.uniform(-crook, crook)
            cn += rng.uniform(-crook, crook)
        for j in range(seg):
            a = phase + 2 * math.pi * j / seg
            zz = z
            if snapped and i == len(rings) - 1:
                zz = max(rings[i - 1][0] + 0.06, z + rng.uniform(-0.22, 0.12))
            verts.append(G(ce + rr * math.cos(a), cn + rr * math.sin(a), zz))
    for i in range(len(rings) - 1):
        mid = (rings[i][0] + rings[i + 1][0]) / 2
        mat = MT["slime"] if mid < crust + 0.01 else MT["pile"] if mid < wood_end else MT["pile_up"] if mid < upper_end else MT["pile_top"]
        for j in range(seg):
            a, b = i * seg + j, i * seg + (j + 1) % seg
            faces.append((a, b, b + seg, a + seg))   # (G is a half turn: the ring runs anticlockwise in Blender too)
            mats.append(mat)
    last = (len(rings) - 1) * seg
    top_mat = mats[-1] if mats else MT["pile"]
    if snapped:
        ce, cn, _ = p.axis(top)
        verts.append(G(ce, cn, top + 0.02))
        c = len(verts) - 1
        for j in range(seg):
            faces.append((last + j, last + (j + 1) % seg, c))
            mats.append(top_mat)
    else:
        faces.append(tuple(last + j for j in range(seg)))
        mats.append(top_mat)
    acc.raw(verts, faces, mats)
    return p


def pile_band(acc, MT, p, u, mat=None, seg=7):
    rr = p.r * 0.97 + 0.025
    rod(acc, mat or MT["iron"], p.axis(u - 0.045), p.axis(u + 0.045), rr, seg)


def strap(acc, MT, a, b, u):
    """An iron strap round two piles bound together."""
    ca, cb = a.axis(u), b.axis(u)
    de, dn = cb[0] - ca[0], cb[1] - ca[1]
    d = math.atan2(dn, de)
    pts = []
    for c, rr, a0 in ((ca, a.r + 0.03, d + math.pi / 2), (cb, b.r + 0.03, d - math.pi / 2)):
        for k in range(7):
            ang = a0 + math.pi * k / 6
            pts.append(G(c[0] + rr * math.cos(ang), c[1] + rr * math.sin(ang), u))
    acc.add(loop_tube(pts, 0.022, 4, (0, 0, 1)), MT["iron"])


# ---------------------------------------------------------------- paving

def local(poly, heading):
    h = math.radians(heading)
    ae, an = math.sin(h), math.cos(h)      # along
    xe, xn = math.cos(h), -math.sin(h)     # across
    return [(e * xe + n * xn, e * ae + n * an) for e, n in poly], (ae, an), (xe, xn)


def span_at(lpoly, y):
    xs = []
    for (x1, y1), (x2, y2) in zip(lpoly, lpoly[1:] + lpoly[:1]):
        if (y1 <= y < y2) or (y2 <= y < y1):
            xs.append(x1 + (y - y1) * (x2 - x1) / (y2 - y1))
    return (min(xs), max(xs)) if len(xs) >= 2 else None


def plinth(acc, MT, poly, top, side=None):
    """The body under a paved top: slime at the waterline, stone above it, down into the water."""
    prism(acc, MT["slime"], poly, DEEP, min(0.15, top - 0.08))
    if top - 0.07 > 0.15:
        prism(acc, side or MT["wet"], poly, 0.15, top - 0.07)


def pave(acc, MT, rng, poly, top, heading, depth=(0.6, 0.85), width=(0.6, 1.0), missing=0.03, edge_band=0.35):
    """Slabs over a (convex) outline in rows along `heading`, a hair tilted (they never quite lie flat any more)."""
    lpoly, (ae, an), (xe, xn) = local(poly, heading)
    ys = [p[1] for p in lpoly]
    y = min(ys) + 0.01
    row = 0
    while y < max(ys) - 0.12:
        d = min(rng.uniform(*depth), max(ys) - y)
        s0, s1 = span_at(lpoly, y + 0.02), span_at(lpoly, y + d - 0.02)
        if s0 and s1:
            lo, hi = max(s0[0], s1[0]) + 0.02, min(s0[1], s1[1]) - 0.02
            x = lo
            first = True
            while x < hi - 0.1:
                w = rng.uniform(*width) * (0.5 if first and row % 2 else 1.0)
                first = False
                x1 = min(x + w, hi)
                if hi - x1 < 0.25:
                    x1 = hi
                if rng.random() > missing:
                    cx, cy = (x + x1) / 2, y + d / 2
                    e = cx * xe + cy * ae
                    n = cx * xn + cy * an
                    near = min(abs(x - lo), abs(hi - x1)) < 0.05 and (x - lo < edge_band or hi - x1 < edge_band)
                    mat = MT["edge"] if near and rng.random() < 0.7 else rng.choice(MT["stone"])
                    obox(acc, mat, (e, n, top - 0.04 + rng.uniform(-0.006, 0.008)), (x1 - x - 0.035, d - 0.035, 0.08),
                         heading, rng.uniform(-0.5, 0.5), rng.uniform(-0.5, 0.5))
                x = x1
        y += d
        row += 1


def sector_slabs(acc, MT, rng, ce, cn, bands, a0, a1, step, top, missing=0.03, edge=()):
    """Slabs round a centre: `bands` of (r0, r1), each laid in pieces `step` degrees long (staggered band to band)."""
    lo, hi = min(a0, a1), max(a0, a1)
    for j, (r0, r1) in enumerate(bands):
        a = lo - (step / 2 if j % 2 else 0.0)
        while a < hi - 0.2:
            b = min(a + step * rng.uniform(0.8, 1.2), hi)
            s, t = max(a, lo), b
            if hi - t < step * 0.4:
                t = hi
            gap = math.degrees(0.018 / r1)
            if rng.random() > missing and t - s > 2 * gap:
                mat = MT["edge"] if j in edge and rng.random() < 0.75 else rng.choice(MT["stone"])
                prism(acc, mat, sector(ce, cn, r0 + 0.018, r1 - 0.018, s + gap, t - gap, 2), top - 0.075, top + rng.uniform(-0.006, 0.008))
            a = t


# ---------------------------------------------------------------- the pieces

def waking(acc, MT, rng):
    """The Waking: the cut-off top of an old great pier, round, alone in the water. A broken mooring ring on it, a
    chain trailing off its edge into the water."""
    prism(acc, MT["slime"], circle(0, 0, 2.3, 24), DEEP, 0.12)
    prism(acc, MT["wet"], circle(0, 0, 2.6, 24), DEEP, -0.45)   # (the pier widens under the water)
    # Its top course: a ring of big blocks, then the slabs.
    n = 14
    for k in range(n):
        a0, a1 = 360 * k / n + 0.4, 360 * (k + 1) / n - 0.4
        r1 = 2.25 + rng.uniform(-0.03, 0.04)
        prism(acc, MT["wet"] if rng.random() < 0.5 else MT["stone"][2], sector(0, 0, 1.3, r1, a0, a1, 2), 0.1, 0.43)
    prism(acc, MT["joint"], circle(0, 0, 1.32, 12), 0.1, 0.42)
    prism(acc, rng.choice(MT["stone"]), circle(0, 0, 0.68, 8, 0.2), 0.43, TOP + 0.004)
    sector_slabs(acc, MT, rng, 0.0, 0.0, [(0.7, 1.42)], 0, 360, 60, TOP, missing=0.0)
    for k in range(12):  # the outer ring of slabs (one gone at the south-west: the edge crumbled)
        a0, a1 = 30 * k + 7, 30 * (k + 1) + 7
        if k == 7:
            prism(acc, MT["wet"], sector(0, 0, 1.45, 2.05, a0 + 1, a1 - 1, 3), 0.3, 0.41)
            continue
        mat = MT["edge"] if rng.random() < 0.6 else rng.choice(MT["stone"])
        prism(acc, mat, sector(0, 0, 1.46, 2.2 + rng.uniform(-0.04, 0.02), a0 + 0.6, a1 - 0.6, 3), 0.425, TOP + rng.uniform(-0.006, 0.008))
    # Lichen, pale, on the edge (it catches the lantern).
    for a, r, s in ((40, 1.9, 0.32), (130, 1.75, 0.22), (300, 2.0, 0.26)):
        e, nn = r * math.cos(math.radians(a)), r * math.sin(math.radians(a))
        obox(acc, MT["lichen"], (e, nn, TOP + 0.012), (s, s * 0.7, 0.01), a + 20)
    # The mooring ring: its staple leaded into the stone, the ring snapped, lying on the slab.
    box(acc, MT["iron"], 1.38, 1.5, -0.98, -0.86, TOP - 0.02, TOP + 0.06)
    ring(acc, MT["iron"], (1.44, -1.18, TOP + 0.035), 0.24, 0.032, "u", sides=5, seg=14, arc=290, start=110)
    # A chain: a few links on the stone, trailing off the edge down into the water.
    chain(acc, MT["chain"], [(1.22, -1.0, TOP + 0.03), (0.6, -1.25, TOP + 0.03), (0.1, -1.7, TOP + 0.03), (-0.35, -2.05, TOP + 0.03),
                             (-0.6, -2.3, 0.4), (-0.75, -2.5, 0.0), (-0.82, -2.6, -0.6)], 0.17, 0.028)
    chain(acc, MT["chain"], [(-0.9, 0.6, TOP + 0.03), (-0.55, 0.95, TOP + 0.03), (-0.5, 1.35, TOP + 0.03)], 0.17, 0.028)


def boardwalk(acc, MT, rng):
    """From the Waking north: a broken timber walk 2 m wide on posts; planks gone here and there (never two side by
    side), a few sagging into the water; a handrail on its west side, half gone. It dips a little where a post gave."""
    n0, n1 = 2.0, 13.85

    def sag(n):
        return -0.1 * math.sin(math.pi * (n - 9.0) / 2.6) if 9.0 < n < 11.6 else 0.0

    for e in (-0.78, 0.78):
        pts = [n0, 9.0, 9.65, 10.3, 10.95, 11.6, n1]
        for a, b in zip(pts, pts[1:]):
            beam(acc, MT["rot"], (e, a, 0.38 + sag(a)), (e, b, 0.38 + sag(b)), 0.14, 0.12)
    count = 36
    missing, hanging, short = {5, 12, 19, 26, 31}, {12, 26}, {8, 22}
    for i in range(count):
        n = 2.45 + i * 0.32
        if i in hanging:  # one end still on the east stringer, the other down in the water
            beam(acc, rng.choice([MT["wood"], MT["rot"]]), (0.86, n, 0.47), (-1.05, n + rng.uniform(-0.08, 0.08), -0.35), 0.27, 0.055)
            continue
        if i in missing:
            continue
        mat = rng.choice([MT["wood"], MT["pale"], MT["rot"], MT["wood"]])
        if i in short:  # broken off: only its east half left
            obox(acc, mat, (0.52, n, 0.47 + sag(n)), (0.27, 0.96, 0.06), 90 + rng.uniform(-3, 3), rng.uniform(-1.5, -0.5))
            continue
        obox(acc, mat, (rng.uniform(-0.04, 0.04), n, 0.47 + sag(n) + rng.uniform(-0.008, 0.006)),
             (0.27 + rng.uniform(-0.02, 0.01), 2.0 + rng.uniform(-0.08, 0.06), 0.06), 90 + rng.uniform(-2.5, 2.5),
             rng.uniform(-1.2, 1.2), rng.uniform(-1.0, 1.0))
    # The posts under it, in pairs, braced (the braces mostly under the water).
    for k, n in enumerate((2.3, 4.7, 7.1, 9.5, 11.9, 13.6)):
        for e in (-0.8, 0.8):
            lean = (rng.uniform(-0.02, 0.02), rng.uniform(-0.02, 0.02))
            if k == 4 and e < 0:
                lean = (-0.06, 0.03)  # (the one that gave)
            top = 0.32 + sag(n)
            piling(acc, MT, rng, e - lean[0] * (top - DEEP), n - lean[1] * (top - DEEP), 0.11, top, lean, seg=6, crust=0.12)
        if k % 2 == 0:
            beam(acc, MT["rot"], (-0.8, n, -0.7), (0.8, n, 0.22), 0.1, 0.12)
    # The handrail, west side: posts at the deck's edge, rails between some of them; the rest snapped or gone.
    posts = [(2.5, 1.48), (4.9, 1.49), (7.3, 0.78), (9.7, None), (12.1, 1.47), (13.6, 0.58)]
    for n, top in posts:
        if top is None:
            continue
        box(acc, MT["wood"], -1.0, -0.88, n - 0.06, n + 0.06, 0.2, top)
        if top < 1.0:  # snapped: a splinter
            obox(acc, MT["pale"], (-0.92, n + 0.02, top + 0.08), (0.04, 0.05, 0.18), 0, 0, 12)
        rod(acc, MT["rope"], (-0.94, n, 1.0), (-0.94, n, 1.09), 0.085, 6)
    beam(acc, MT["wood"], (-0.94, 2.45, 1.4), (-0.94, 4.95, 1.4), 0.08, 0.1)
    beam(acc, MT["wood"], (-0.94, 2.45, 0.95), (-0.94, 4.95, 0.95), 0.07, 0.08)
    beam(acc, MT["wood"], (-0.94, 4.95, 1.38), (-1.3, 6.5, 0.05), 0.08, 0.1)   # broken off, down into the water
    beam(acc, MT["wood"], (-0.94, 12.05, 1.36), (-0.94, 13.0, 1.2), 0.08, 0.1)  # a stub of rail
    beam(acc, MT["wood"], (-0.94, 12.05, 0.95), (-0.94, 13.65, 0.93), 0.07, 0.08)


def foundations(acc, MT, rng):
    """The Foundations: a stone ledge (the top of an old footing) turning east from the Boardwalk to a drowned wall,
    then north along it. The wall: big blocks, slime at the waterline, its top broken, two arches half under the water
    (dark inside), iron rings. Nothing above it: the light comes down here."""
    for poly, heading in ((FOUND_A, 63.4), (FOUND_B, 0.0)):
        plinth(acc, MT, poly, TOP)
        pave(acc, MT, rng, poly, TOP, heading)
    wall(acc, MT, rng)


ARCHES = [(18.3, 20.5), (22.0, 24.2)]
SPRING = 1.2


def wall_top(n):
    prof = [(14.4, 1.0), (15.4, 2.1), (16.4, 3.4), (17.4, 4.9), (18.6, 5.6), (20.4, 5.3), (21.6, 6.0), (23.4, 5.4),
            (24.4, 4.4), (25.4, 3.8), (26.3, 2.4)]
    for (a, ha), (b, hb) in zip(prof, prof[1:]):
        if a <= n <= b:
            return ha + (hb - ha) * (n - a) / (b - a)
    return 1.0


def wall(acc, MT, rng):
    e0, e1, n0, n1 = 9.25, 10.75, 14.4, 26.3
    # Its core (dark: shows in the joints), around the arches.
    cuts = [n0] + [x for a in ARCHES for x in a] + [n1]
    for i, (a, b) in enumerate(zip(cuts, cuts[1:])):
        if i % 2 == 1:
            continue
        s = a
        while s < b - 1e-3:
            t = min(b, s + 0.5)
            box(acc, MT["joint"], e0 + 0.05, e1 - 0.05, s, t, DEEP, min(wall_top(s), wall_top(t)) - 1.05)
            s = t
    for a, b in ARCHES:
        r = (b - a) / 2
        crown = SPRING + r
        box(acc, MT["joint"], e0 + 0.05, e1 - 0.05, a, b, crown + 0.05, max(crown + 0.1, min(wall_top(a), wall_top(b)) - 1.05))
        box(acc, MT["void"], e1 - 0.2, e1 - 0.05, a, b, -1.5, crown)   # (dark inside)
        # Above the springing: stepped fill under the arch's curve, and the ring of voussoirs on its face.
        k = 8
        for j in range(k):
            s, t = a + (b - a) * j / k, a + (b - a) * (j + 1) / k
            c = (a + b) / 2
            y = SPRING + math.sqrt(max(0.0, r * r - max((s - c) ** 2, (t - c) ** 2)))
            if y < crown + 0.04:
                box(acc, MT["wet"], e0 + 0.08, e1 - 0.2, s, t, y, crown + 0.06)
        vk = 9
        for j in range(vk):
            a0 = math.pi * j / vk
            a1 = math.pi * (j + 1) / vk
            pts = []
            for ang, rr in ((a0, r), (a1, r), (a1, r + 0.42), (a0, r + 0.42)):
                pts.append(((a + b) / 2 + rr * math.cos(ang), SPRING + rr * math.sin(ang)))
            mat = MT["edge"] if j in (vk // 2,) else rng.choice(MT["stone"])

            def vous(bm, pts=pts):
                # A wedge in the wall's face: the (n, u) outline extruded through e.
                lo = [bm.verts.new(G(e0 - 0.07, n, u)) for n, u in pts]
                hi = [bm.verts.new(G(e0 + 0.35, n, u)) for n, u in pts]
                bm.faces.new(lo)
                bm.faces.new(list(reversed(hi)))
                for i in range(4):
                    bm.faces.new((lo[i], lo[(i + 1) % 4], hi[(i + 1) % 4], hi[i]))
                bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
            acc.add(vous, mat)
    # The blocks, in courses: big, a hair proud or shy, tones by height (slime at the waterline, wet below it); two
    # rows through the wall's thickness, each broken off at its own height along the top.
    for t0, t1, face in ((e0, e0 + 0.74, True), (e0 + 0.78, e1, False)):
        u = -1.0
        row = 0
        while u < 6.2:
            h = rng.uniform(0.55, 0.75)
            s = n0 - (rng.uniform(0.3, 0.8) if row % 2 else rng.uniform(0.0, 0.3))
            while s < n1 - 0.05:
                length = rng.uniform(0.9, 1.7)
                lo, hi = max(s, n0), min(s + length, n1)
                s += length
                if hi - lo < 0.15:
                    continue
                top_here = min(wall_top(lo), wall_top(hi)) + rng.uniform(-0.3, 0.2) - (0.0 if face else 0.25)
                if u + h > top_here:
                    continue
                spans = [(lo, hi)]
                for a, b in ARCHES:
                    if u < SPRING + (b - a) / 2 + 0.02:
                        out = []
                        for x0, x1 in spans:
                            if x1 <= a or x0 >= b:
                                out.append((x0, x1))
                            else:
                                if x0 < a:
                                    out.append((x0, a))
                                if x1 > b:
                                    out.append((b, x1))
                        spans = out
                for x0, x1 in spans:
                    if x1 - x0 < 0.12:
                        continue
                    mid = u + h / 2
                    mat = MT["wet"] if mid < -0.3 else MT["slime"] if mid < 0.55 else MT["wet"] if mid < 1.1 and rng.random() < 0.6 else rng.choice(MT["stone"])
                    if mid > 4.6 and rng.random() < 0.5:
                        mat = MT["dark"]
                    proud = rng.uniform(-0.03, 0.04) if face else 0.0
                    back = 0.0 if face else rng.uniform(-0.03, 0.03)
                    box(acc, mat, t0 - proud, t1 - back - 0.02, x0 + 0.018, x1 - 0.018, u + 0.018, u + h - 0.018)
            u += h
            row += 1
    # Fallen blocks at its foot, in the water, south of the ledge.
    for n, u_top, yaw in ((15.0, 0.25, 12), (16.2, -0.05, -20), (13.8, 0.1, 35)):
        obox(acc, MT["wet"], (9.9 + rng.uniform(-0.3, 0.3), n, u_top - 0.33), (1.3, 0.8, 0.66), yaw, rng.uniform(-8, 8), rng.uniform(-10, 10))
    # Iron rings on its face, one with a few links of chain still on it.
    for n, u_ring in ((17.3, 1.55), (21.25, 1.7), (25.2, 1.5)):
        box(acc, MT["iron"], e0 - 0.1, e0 + 0.02, n - 0.05, n + 0.05, u_ring + 0.1, u_ring + 0.24)
        ring(acc, MT["iron"], (e0 - 0.09, n, u_ring), 0.15, 0.028, "e", sides=4, seg=12)
    chain(acc, MT["chain"], [(e0 - 0.11, 21.25, 1.55), (e0 - 0.16, 21.3, 1.2), (e0 - 0.14, 21.2, 0.85), (e0 - 0.2, 21.4, 0.6)], 0.15, 0.025)
    # Lichen on its face and its broken top (pale: they catch the light).
    for n, u_l, s in ((16.5, 2.6, 0.5), (19.6, 4.1, 0.35), (21.0, 3.2, 0.6), (23.3, 4.6, 0.4), (25.0, 2.7, 0.45), (18.8, 3.0, 0.3)):
        box(acc, MT["lichen"], e0 - 0.075, e0 - 0.06, n - s / 2, n + s / 2 * 0.8, u_l - s * 0.3, u_l + s * 0.3)


def deck(acc, MT, rng, poly, top, holes=0.015, mats=None):
    """A boarded floor over an outline: boards running north, butted at random lengths, clipped to the outline."""
    es = [p[0] for p in poly]
    mats = mats or [MT["wood"], MT["pale"], MT["rot"]]
    e = min(es)
    while e < max(es) - 0.05:
        w = min(0.24, max(es) - e)
        s0, s1 = span_at([(p[1], p[0]) for p in poly], e + 0.02), span_at([(p[1], p[0]) for p in poly], e + w - 0.02)
        if s0 and s1:
            lo, hi = max(s0[0], s1[0]), min(s0[1], s1[1])
            n = lo - rng.uniform(0.0, 1.5)
            while n < hi - 0.05:
                length = rng.uniform(1.4, 2.8)
                a, b = max(n, lo), min(n + length, hi)
                if b - a > 0.15:
                    if rng.random() < holes:  # (a board broken through: a stub of it left at one end)
                        b = a + (b - a) * rng.uniform(0.3, 0.6)
                    obox(acc, rng.choice(mats), (e + w / 2, (a + b) / 2, top - 0.028 + rng.uniform(-0.006, 0.004)),
                         (w - 0.025, b - a - 0.02, 0.055), 0.0, rng.uniform(-0.4, 0.4), rng.uniform(-0.8, 0.8))
                n += length
        e += w


def pens(acc, MT, rng):
    """The Pens: a wide old platform on piles, heaped with kept things: dead lanterns by the dozen, bundles tied in rope
    and chain, rowing boats pulled up and overturned, cages hung from tall posts at its edges (their chains run on up
    into the dark). Its north-west corner has broken away (the causeway to the islet leaves from there)."""
    deck(acc, MT, rng, PENS, TOP)
    # Joists under the boards, a fascia round the edge, piles under it all.
    n = 27.0
    while n < 33.3:
        s = span_at([(p[0], p[1]) for p in PENS], n)
        if s:
            box(acc, MT["rot"], s[0] + 0.05, s[1] - 0.05, n - 0.07, n + 0.07, 0.3, TOP - 0.056)
        n += 1.3
    for (a, b) in zip(PENS, PENS[1:] + PENS[:1]):
        rail(acc, MT["rot"], (a[0], a[1], 0.36), (b[0], b[1], 0.36), 0.12, 0.2, trim=0.07)
    for e in (1.75, 4.1, 6.4, 8.6, 10.25):
        for n in (26.75, 29.0, 31.3, 33.25):
            if poly_dist(e, n, PENS) < 0.3:
                piling(acc, MT, rng, e, n, 0.15, 0.3, (rng.uniform(-0.01, 0.01), rng.uniform(-0.01, 0.01)), seg=6, crust=0.14)
    # Boards broken off at the lost corner, hanging into the water.
    for t, drop in ((0.25, -0.5), (0.6, -0.3)):
        a = (1.5 + 1.5 * t, 31.2 + 2.3 * t)
        beam(acc, MT["rot"], (a[0] + 0.15, a[1] - 0.1, 0.46), (a[0] - 0.75, a[1] + 0.35, drop), 0.22, 0.05)
    # The causeway to the islet: planks across two stringers, on two posts.
    c0, c1 = (2.15, 31.85), (0.35, 33.65)
    d = (c1[0] - c0[0], c1[1] - c0[1])
    ln = math.hypot(*d)
    ux, uy = d[0] / ln, d[1] / ln
    px, py = -uy, ux
    for off in (-0.42, 0.42):
        beam(acc, MT["rot"], (c0[0] + px * off - ux * 0.4, c0[1] + py * off - uy * 0.4, 0.39), (c1[0] + px * off + ux * 0.3, c1[1] + py * off + uy * 0.3, 0.39), 0.12, 0.1)
    heading = math.degrees(math.atan2(ux, uy))
    k = 0
    s = 0.15
    while s < ln + 0.05:
        e, nn = c0[0] + ux * s, c0[1] + uy * s
        obox(acc, rng.choice([MT["wood"], MT["pale"]]), (e, nn, 0.472), (0.25, 1.2 + rng.uniform(-0.05, 0.05), 0.055),
             heading + 90 + rng.uniform(-4, 4), rng.uniform(-1.5, 1.5), rng.uniform(-1, 1))
        s += 0.31
        k += 1
    for s in (0.7, 1.9):
        for off in (-0.45, 0.45):
            piling(acc, MT, rng, c0[0] + ux * s + px * off, c0[1] + uy * s + py * off, 0.1, 0.34, seg=6, crust=0.15)
    hoard(acc, MT, rng)


# ---------------------------------------------------------------- the hoard

_lantern = None


def lantern_parts(MT):
    global _lantern
    if _lantern is None:
        L, g = MT["lantern"], MT["glass"]
        parts = [(template(kit.cube(), M((0, 0, 0.015), scale=(0.17, 0.17, 0.03))), L),
                 (template(kit.cube(), M((0, 0, 0.125), scale=(0.13, 0.13, 0.19))), g)]
        for sx in (-1, 1):
            for sy in (-1, 1):
                parts.append((template(kit.cube(), M((sx * 0.066, sy * 0.066, 0.1275), scale=(0.022, 0.022, 0.215))), L))
        parts.append((template(kit.cube(), M((0, 0, 0.235), scale=(0.16, 0.16, 0.02))), L))
        parts.append((template(kit.cone(4, 0.113, 0.025, 0.08), M((0, 0, 0.285), rot=(0, 0, 45))), L))
        parts.append((template(kit.cylinder(6, 0.025, 0.035), M((0, 0, 0.34))), L))
        hoop = [(0.045 * math.cos(2 * math.pi * i / 6), 0.0, 0.395 + 0.045 * math.sin(2 * math.pi * i / 6)) for i in range(6)]
        parts.append((template(loop_tube(hoop, 0.008, 3, (0, 1, 0))), L))
        _lantern = parts
    return _lantern


def lantern(acc, MT, matrix):
    for tmpl, mat in lantern_parts(MT):
        acc.stamp(tmpl, mat, matrix)


def lantern_at(acc, MT, rng, e, n, u, lying=False, tilt=0.0):
    yaw = rng.uniform(0, 360)
    if lying:
        w = Matrix.Translation(G(e, n, u + 0.085)) @ Matrix.Rotation(math.radians(yaw), 4, "Z") @ Matrix.Rotation(math.radians(90 + rng.uniform(-6, 6)), 4, "X") @ Matrix.Translation((0, 0, -0.18))
    else:
        w = Matrix.Translation(G(e, n, u)) @ Matrix.Rotation(math.radians(yaw), 4, "Z") @ Matrix.Rotation(math.radians(tilt), 4, "X")
    lantern(acc, MT, w)


def heap(acc, MT, rng, ce, cn, radius, count):
    """Lanterns heaped up: a layer on the boards (some on their sides), smaller layers on top, tipped every way."""
    placed = []
    layers = [(0.4, 1.0, 0.0, 10), (0.25, 0.75, 0.2, 35), (0.17, 0.52, 0.4, 35), (0.11, 0.32, 0.6, 30), (0.07, 0.14, 0.8, 25)]
    for frac, rf, lift, tilt in layers:
        for _ in range(max(1, round(count * frac))):
            for _try in range(20):
                a, r = rng.uniform(0, 2 * math.pi), radius * rf * math.sqrt(rng.random())
                e, n = ce + r * math.cos(a), cn + r * math.sin(a)
                if all(math.hypot(e - pe, n - pn) > 0.16 or abs(pu - lift) > 0.1 for pe, pn, pu in placed):
                    break
            placed.append((e, n, lift))
            if lift == 0.0 and rng.random() < 0.3:
                lantern_at(acc, MT, rng, e, n, TOP, lying=True)
            else:
                lantern_at(acc, MT, rng, e, n, TOP + lift * (1 - 0.35 * r / max(0.01, radius * rf)), tilt=rng.uniform(-tilt, tilt))


def sack(acc, MT, rng, e, n, u, size, heading, mat=None, bands=2, chained=False):
    sx, sy, sz = size

    def shape(bm):
        kit.ico(2)(bm)
        for v in bm.verts:
            v.co += v.co.normalized() * rng.uniform(-0.08, 0.08)
            if v.co.z < -0.6:
                v.co.z = -0.6 + (v.co.z + 0.6) * 0.2   # (it slumps: a flat bottom)
    base = M(G(e, n, u + sz * 0.3), rot=(rng.uniform(-6, 6), rng.uniform(-6, 6), 180 - heading))
    acc.add(shape, mat or rng.choice([MT["sack"], MT["sack_pale"]]), base @ Matrix.Diagonal((sx / 2, sy / 2, sz / 2, 1)))
    for k in range(bands):
        y = (k + 1) / (bands + 1) * 2 - 1
        f = math.sqrt(max(0.05, 1 - (y * 0.85) ** 2))
        acc.add(kit.cylinder(10, 0.5, 1.0), MT["rope"], base @ M((0, y * sy / 2 * 0.85, 0), rot=(90, 0, 0), scale=(sx * f * 1.04, sz * f * 1.04, 0.05)))
    if chained:
        loop = [(0.0, (sx / 2 * 1.08) * math.cos(2 * math.pi * i / 12), (sz / 2 * 1.08) * math.sin(2 * math.pi * i / 12)) for i in range(12)]
        world = []
        for x, y, z in loop:
            p = base @ Vector((y, 0.0, z))
            world.append((-p.x, -p.y, p.z))
        chain(acc, MT["chain"], world, 0.13, 0.022, seg=6, sides=3, closed=True)


def roll(acc, MT, rng, e, n, u, length, r, heading):
    base = M(G(e, n, u + r), rot=(90, 0, 180 - heading))
    acc.add(kit.cylinder(9, r, length), rng.choice([MT["sack"], MT["sack_pale"]]), base)
    for y in (-length * 0.3, length * 0.3):
        acc.add(kit.cylinder(9, r + 0.012, 0.05), MT["rope"], base @ M((0, 0, y)))


def trunk(acc, MT, rng, e, n, u, heading):
    obox(acc, MT["rot"], (e, n, u + 0.21), (0.5, 0.85, 0.42), heading)
    obox(acc, MT["wood"], (e, n, u + 0.46), (0.54, 0.89, 0.09), heading, 0, rng.uniform(-2, 2))
    h = math.radians(heading)
    for t in (-0.28, 0.28):
        obox(acc, MT["iron"], (e + math.sin(h) * t, n + math.cos(h) * t, u + 0.26), (0.53, 0.06, 0.45), heading)


TS = [0, 0.15, 0.35, 0.55, 0.75, 0.9, 1.0]
HALF_WIDTH = [0.5, 0.6, 0.63, 0.6, 0.48, 0.27, 0.03]
DEPTH = [0.34, 0.4, 0.42, 0.42, 0.39, 0.34, 0.3]
RIM = [0.42, 0.4, 0.39, 0.4, 0.43, 0.5, 0.6]


def hull(bm):
    """The village's rowing boat's hull (art/models/environment/boat.py), bow along +Y, keel near z=0."""
    sections = []
    for t, w, d, r in zip(TS, HALF_WIDTH, DEPTH, RIM):
        y = -1.3 + t * 2.7
        profile = [(-w, r), (-w * 0.82, r - d * 0.6), (0, r - d), (w * 0.82, r - d * 0.6), (w, r)]
        sections.append([bm.verts.new((x, y, z)) for x, z in profile])
    for a, b in zip(sections, sections[1:]):
        for k in range(4):
            bm.faces.new((a[k], a[k + 1], b[k + 1], b[k]))
    bm.faces.new(sections[0])
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    bm.normal_update()
    bmesh.ops.solidify(bm, geom=bm.faces[:], thickness=0.045)
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)


def boat_over(acc, MT, e, n, u, heading, scale=1.0, roll_deg=0.0, mat=None):
    """A rowing boat overturned, its gunwale resting at u."""
    mtx = (Matrix.Translation(G(e, n, u + 0.506 * scale)) @ Matrix.Rotation(math.radians(180 - heading), 4, "Z")
           @ Matrix.Rotation(math.radians(roll_deg), 4, "Y") @ Matrix.Rotation(math.pi, 4, "Y")
           @ Matrix.Rotation(math.radians(-3.8), 4, "X") @ Matrix.Diagonal((scale, scale, scale, 1)))
    acc.add(hull, mat or MT["boat"], mtx)
    # A strake of old paint along it.
    acc.add(kit.cube(), MT["boat_paint"], mtx @ M((0.63, -0.1, 0.35), rot=(0, 12, 0), scale=(0.02, 1.4, 0.07)))


def cage(acc, MT, rng, e, n, u, r=0.42, h=1.35, sunk=False):
    """A hanging cage: a ring top and bottom, bars between, a dome of bars to a hook. Returns where its chain hangs."""
    bars = 9
    for z in (u, u + h):
        ring(acc, MT["iron"], (e, n, z), r, 0.025, "u", sides=4, seg=bars)
    if not sunk:
        prism(acc, MT["rust"], circle(e, n, r - 0.02, bars), u - 0.03, u + 0.01)
    for k in range(bars):
        a = 2 * math.pi * k / bars
        rod(acc, MT["iron"], (e + r * math.cos(a), n + r * math.sin(a), u), (e + r * math.cos(a), n + r * math.sin(a), u + h), 0.014, 4)
    apex = (e, n, u + h + 0.38)
    for k in range(0, bars, 2):
        a = 2 * math.pi * k / bars
        mid = (e + r * 0.7 * math.cos(a), n + r * 0.7 * math.sin(a), u + h + 0.26)
        rod(acc, MT["iron"], (e + r * math.cos(a), n + r * math.sin(a), u + h), mid, 0.014, 4)
        rod(acc, MT["iron"], mid, apex, 0.014, 4)
    ring(acc, MT["iron"], (e, n, apex[2] + 0.07), 0.07, 0.016, "n", sides=4, seg=8)
    return (e, n, apex[2] + 0.14)


def gibbet(acc, MT, rng, e, n, out, cage_u, top=9.6):
    """A tall post at the platform's edge, an arm out over the water, a cage hung from it; a chain on from the post's
    top up into the dark."""
    p = piling(acc, MT, rng, e, n, 0.2, top, seg=8, crust=0.5)
    arm_u = top - 1.1
    tip = (e + out[0], n + out[1], arm_u)
    beam(acc, MT["pile_up"], (e - out[0] * 0.1, n - out[1] * 0.1, arm_u), (tip[0] + out[0] * 0.08, tip[1] + out[1] * 0.08, arm_u), 0.18, 0.22)
    beam(acc, MT["pile_up"], (e, n, arm_u - 1.4), (e + out[0] * 0.55, n + out[1] * 0.55, arm_u - 0.05), 0.12, 0.14)
    pile_band(acc, MT, p, arm_u, seg=8)
    pile_band(acc, MT, p, 1.6, seg=8)
    hook = cage(acc, MT, rng, tip[0], tip[1], cage_u)
    chain(acc, MT["chain"], [(tip[0], tip[1], arm_u - 0.1), hook], 0.2, 0.028, seg=6, sides=3)
    chain(acc, MT["chain"], [(e, n, top - 0.05), (e + 0.05, n + 0.1, 14.0)], 0.3, 0.04, seg=6, sides=3)


def hoard(acc, MT, rng):
    def ok(e, n, r):
        return seg_dist(e, n, PENS_PATH) > 1.15 + r and poly_dist(e, n, PENS) == 0.0
    # Lanterns: a big heap and a smaller one, and loose ones spilled between.
    heap(acc, MT, rng, 7.9, 31.4, 1.3, 64)
    heap(acc, MT, rng, 3.2, 27.55, 0.85, 22)
    for e, n in ((6.6, 30.4), (6.95, 29.9), (8.9, 29.6), (9.6, 30.2), (4.4, 28.3), (2.3, 28.6), (7.0, 32.9), (9.6, 32.8)):
        if ok(e, n, 0.1):
            lantern_at(acc, MT, rng, e + rng.uniform(-0.1, 0.1), n + rng.uniform(-0.1, 0.1), TOP, lying=rng.random() < 0.7, tilt=rng.uniform(-8, 8))
    # Boats, pulled up along the east edge and overturned, one on another; a third on its side against them.
    boat_over(acc, MT, 9.75, 28.9, TOP, 2.0)
    boat_over(acc, MT, 9.7, 29.0, TOP + 0.33, -5.0, 0.9)
    boat_over(acc, MT, 6.4, 32.95, TOP, 96.0, 0.95, roll_deg=8.0)
    # Bundles: sacks and rolls tied in rope, some bound in chain; a trunk.
    for e, n, s, h, chained in ((3.9, 32.9, (0.62, 0.8, 0.5), 80, False), (4.4, 32.3, (0.5, 0.65, 0.42), 20, True),
                                (3.5, 32.2, (0.45, 0.55, 0.38), -30, False), (10.0, 31.0, (0.6, 0.75, 0.5), 10, True),
                                (10.05, 32.0, (0.55, 0.6, 0.45), 95, False), (5.0, 27.0, (0.55, 0.7, 0.45), 75, False),
                                (5.75, 26.95, (0.48, 0.6, 0.4), 105, True)):
        if ok(e, n, 0.2):
            sack(acc, MT, rng, e, n, TOP, s, h, chained=chained)
    roll(acc, MT, rng, 9.9, 32.75, TOP, 1.0, 0.2, 10)
    roll(acc, MT, rng, 9.95, 32.55, TOP + 0.38, 0.9, 0.18, 25)
    roll(acc, MT, rng, 2.4, 27.05, TOP, 0.9, 0.17, 95)
    trunk(acc, MT, rng, 10.0, 27.0, TOP, 2)
    sack(acc, MT, rng, 10.0, 27.05, TOP + 0.5, (0.42, 0.5, 0.32), 40)
    # The cages: on tall posts at the north and east edges, hung out over the water; one sunk to its middle, its
    # chain straight up into the dark.
    gibbet(acc, MT, rng, 4.6, 34.0, (0.0, 1.5), 1.3)
    gibbet(acc, MT, rng, 8.7, 34.0, (0.3, 1.45), 0.25, top=10.2)
    gibbet(acc, MT, rng, 11.15, 29.6, (1.5, 0.0), 2.1, top=9.0)
    hook = cage(acc, MT, rng, 12.1, 33.9, -0.75, sunk=True)
    chain(acc, MT["chain"], [hook, (12.1, 33.9, 14.0)], 0.28, 0.038, seg=6, sides=3)


# ---------------------------------------------------------------- the islet, the Basin

def islet(acc, MT, rng):
    """The Mooring islet: a small round of stone, its middle clear (the game's Mooring post); broad steps down off its
    north side to the Basin's ledge."""
    outline = [(-1.0 + (2.05 + rng.uniform(0, 0.12)) * math.cos(2 * math.pi * i / 16), 35.0 + (2.05 + rng.uniform(0, 0.12)) * math.sin(2 * math.pi * i / 16)) for i in range(16)]
    plinth(acc, MT, outline, TOP)
    # Edge stones round it (big, a little uneven), slabs in the middle.
    for k in range(11):
        a0, a1 = 360 * k / 11 + 1.0, 360 * (k + 1) / 11 - 1.0
        mat = MT["edge"] if rng.random() < 0.6 else rng.choice(MT["stone"])
        prism(acc, mat, sector(-1.0, 35.0, 1.5, 2.08, a0, a1, 3), TOP - 0.075, TOP + rng.uniform(-0.004, 0.012))
    pave(acc, MT, rng, circle(-1.0, 35.0, 1.47, 12), TOP, 0.0, depth=(0.6, 0.8), width=(0.6, 0.9), missing=0.0)
    for a, r in ((200, 1.8), (330, 1.85), (75, 1.75)):
        e, n = -1.0 + r * math.cos(math.radians(a)), 35.0 + r * math.sin(math.radians(a))
        obox(acc, MT["lichen"], (e, n, TOP + 0.016), (0.3, 0.2, 0.01), a)
    # Rubble round its foot.
    for a in (150, 175, 205, 250, 20, 50):
        r = rng.uniform(2.3, 2.8)
        obox(acc, MT["wet"], (-1.0 + r * math.cos(math.radians(a)), 35.0 + r * math.sin(math.radians(a)), rng.uniform(-0.35, -0.1)),
             (rng.uniform(0.5, 0.9), rng.uniform(0.6, 1.0), 0.5), rng.uniform(0, 180), rng.uniform(-12, 12), rng.uniform(-12, 12))
    # The steps: from the islet (u 0.5) down one broad step (0.4) to the ledge (0.3).
    plinth(acc, MT, STEPS, 0.4)
    pave(acc, MT, rng, STEPS, 0.4, 0.0, depth=(0.55, 0.8), missing=0.0)


def basin(acc, MT, rng):
    """The Basin: a vast round of flooded water, its rim a low wall mostly awash; a ledge round its west side (the
    way); in its middle a great capstan on a stone pier, its chains rising straight up into the dark."""
    # The ledge: a curved stone shelf outside the rim.
    plinth(acc, MT, ARC, LEDGE)
    sector_slabs(acc, MT, rng, BE, BN, [(ARC_IN, 16.25), (16.25, 17.08), (17.08, ARC_OUT)], ARC_A1, ARC_A0, 3.2, LEDGE, edge=(0, 2))
    # The foot of the stair: a pad of stone where the ledge ends.
    plinth(acc, MT, PAD, LEDGE)
    pave(acc, MT, rng, PAD, LEDGE, 90.0, missing=0.0)
    # Old bollards along the ledge's outer edge.
    for a in (226.0, 203.0, 181.0):
        r = ARC_OUT - 0.25
        e, n = BE + r * math.cos(math.radians(a)), BN + r * math.sin(math.radians(a))
        rod(acc, MT["wet"], (e, n, LEDGE - 0.05), (e, n, LEDGE + 0.5), 0.2, 8, 0.17)
        rod(acc, MT["edge"], (e, n, LEDGE + 0.5), (e, n, LEDGE + 0.58), 0.17, 8, 0.12)
    # The rim: a ring of curved blocks, mostly awash; where the ledge runs, whole and just above the water.
    a = 0.0
    while a < 360.0:
        step = rng.uniform(3.6, 5.0)
        b = min(a + step, 360.0)
        mid = (a + b) / 2
        by_ledge = ARC_A1 - 4 <= mid <= ARC_A0 + 4
        if by_ledge:
            top = 0.2 + rng.uniform(-0.01, 0.015)
        else:
            top = rng.uniform(-0.12, 0.24) if rng.random() < 0.8 else rng.uniform(-0.4, -0.15)
            if rng.random() < 0.06:
                a = b
                continue
        gap = math.degrees(0.025 / RIM_OUT)
        outline = sector(BE, BN, RIM_IN + rng.uniform(-0.04, 0.04), RIM_OUT, a + gap, b - gap, 2)
        if not by_ledge and rng.random() < 0.12:  # (a block knocked off: lying askew beside its place)
            r = (RIM_IN + RIM_OUT) / 2 + rng.uniform(-0.6, 0.6)
            obox(acc, MT["wet"], (BE + r * math.cos(math.radians(mid)), BN + r * math.sin(math.radians(mid)), -0.25), (0.8, 1.1, 0.6), 90 - mid + rng.uniform(-30, 30), rng.uniform(-15, 15), rng.uniform(-15, 15))
        else:
            prism(acc, MT["slime"], outline, DEEP, min(top, 0.08) - 0.02)
            prism(acc, MT["wet"] if by_ledge or rng.random() < 0.7 else MT["slime"], outline, min(top, 0.08) - 0.02, top)
        a = b
    # A few rings on the rim's inner face, by the ledge.
    for ang in (232.0, 210.0, 190.0, 172.0):
        e, n = BE + (RIM_IN - 0.08) * math.cos(math.radians(ang)), BN + (RIM_IN - 0.08) * math.sin(math.radians(ang))
        ring(acc, MT["iron"], (e, n, 0.05), 0.13, 0.024, "u", sides=4, seg=10)
    capstan(acc, MT, rng)
    # Stumps of old piles inside the rim.
    for ang, r, top in ((115, 12.5, 0.9), (60, 13.4, 0.4), (20, 11.8, 1.6), (300, 13.0, 0.7), (150, 13.2, 1.2), (250, 12.6, 0.35)):
        piling(acc, MT, rng, BE + r * math.cos(math.radians(ang)), BN + r * math.sin(math.radians(ang)), rng.uniform(0.16, 0.24), top,
               (rng.uniform(-0.03, 0.03), rng.uniform(-0.03, 0.03)), seg=7, crust=0.25, snapped=True)


def capstan(acc, MT, rng):
    """A colossal capstan: a drum 4 m across and 3 m tall, iron-banded, on a stone pier; bars out from its head (some
    broken); great chains wound on it, rising straight up into the dark, and two running slack down into the water."""
    oct8 = circle(BE, BN, 3.25, 8, math.pi / 8)
    prism(acc, MT["slime"], oct8, DEEP, 0.25)
    prism(acc, MT["wet"], oct8, 0.25, 0.62)
    sector_slabs(acc, MT, rng, BE, BN, [(2.35, 2.98)], 0, 360, 45, 0.7, missing=0.0, edge=(0,))
    drum0, drum1 = 0.95, 3.65
    acc.add(kit.cylinder(24, 2.32, 0.33), MT["iron"], M(G(BE, BN, 0.62 + 0.165)))
    acc.add(kit.cylinder(24, 2.0, drum1 - drum0), MT["rot"], M(G(BE, BN, (drum0 + drum1) / 2)))
    for k in range(12):  # staves
        a = 2 * math.pi * (k + 0.5) / 12
        obox(acc, MT["wood"], (BE + 2.0 * math.cos(a), BN + 2.0 * math.sin(a), (drum0 + drum1) / 2), (0.06, 0.16, drum1 - drum0 - 0.02), 90 - math.degrees(a) + 90)
    for u in (1.12, 1.75, 2.95, 3.48):
        acc.add(kit.cylinder(24, 2.07, 0.13), MT["iron"], M(G(BE, BN, u)))
    acc.add(kit.cylinder(24, 2.22, 0.2), MT["wood"], M(G(BE, BN, drum1 + 0.1)))
    acc.add(kit.cylinder(16, 0.85, 0.45), MT["iron"], M(G(BE, BN, drum1 + 0.2 + 0.225)))
    acc.add(kit.cone(16, 0.6, 0.35, 0.2), MT["iron"], M(G(BE, BN, drum1 + 0.65 + 0.1)))
    # The bars: eight out from the head, three snapped short; one fallen on the pier.
    for k in range(8):
        a = math.radians(22.5 + 45 * k)
        length = 3.4 if k not in (1, 4, 6) else rng.uniform(1.4, 2.1)
        p0 = (BE + 0.7 * math.cos(a), BN + 0.7 * math.sin(a), drum1 + 0.42)
        p1 = (BE + length * math.cos(a), BN + length * math.sin(a), drum1 + 0.42 + rng.uniform(-0.05, 0.05))
        beam(acc, MT["wood"], p0, p1, 0.15, 0.15)
        if length > 3:
            beam(acc, MT["iron"], (p1[0] - 0.12 * math.cos(a), p1[1] - 0.12 * math.sin(a), p1[2]), (p1[0] + 0.02 * math.cos(a), p1[1] + 0.02 * math.sin(a), p1[2]), 0.17, 0.17)
    beam(acc, MT["wood"], (BE + 2.5, BN - 1.4, 0.78), (BE + 2.9, BN + 1.3, 0.8), 0.15, 0.15)
    # The chains: one turn wound on the drum, five rising from it into the dark, two slack down into the water.
    great = (0.56, 0.09)
    coil = [(BE + 2.1 * math.cos(2 * math.pi * i / 24), BN + 2.1 * math.sin(2 * math.pi * i / 24), 2.35 + 0.18 * i / 24) for i in range(25)]
    chain(acc, MT["chain"], coil, *great, seg=8, sides=4)
    for ang in (20, 95, 160, 235, 300):
        a = math.radians(ang)
        x, y = BE + 2.12 * math.cos(a), BN + 2.12 * math.sin(a)
        chain(acc, MT["chain"], [(x, y, 2.5), (x, y, 14.0)], *great, seg=8, sides=3)
    for ang, reach in ((200, 7.0), (330, 7.5)):
        a = math.radians(ang)
        x0, y0 = BE + 2.15 * math.cos(a), BN + 2.15 * math.sin(a)
        x1, y1 = BE + reach * math.cos(a), BN + reach * math.sin(a)
        path = []
        for i in range(9):
            t = i / 8
            path.append((x0 + (x1 - x0) * t, y0 + (y1 - y0) * t, 1.3 - 2.9 * t - 0.6 * math.sin(math.pi * t)))
        chain(acc, MT["chain"], path, *great, seg=8, sides=4)


# ---------------------------------------------------------------- the Climb

def climb(acc, MT, rng):
    """A tall timber stair round four thick posts: three flights (south side going west, west side going north, north
    side going east) with landings at the corners, up to a landing on the posts at u 10, under a hatch in a scrap of
    plank ceiling (the jetty's underside: the only bit of it there is)."""
    e0, e1, n0, n1 = CORE
    posts = [(e0 + 0.32, n0 + 0.32), (e1 - 0.32, n0 + 0.32), (e0 + 0.32, n1 - 0.32), (e1 - 0.32, n1 - 0.32)]
    core = [piling(acc, MT, rng, e, n, 0.32, STAIR_TOP + 1.1, seg=8, crust=0.7, crook=0.0) for e, n in posts]
    for e, n in posts:
        rod(acc, MT["iron"], (e, n, STAIR_TOP + 1.1), (e, n, STAIR_TOP + 1.18), 0.27, 8, 0.12)
    for p in core:
        for u in (1.4, 4.8, 8.3):
            pile_band(acc, MT, p, u, seg=8)
    # Ties between the posts at each landing's height, and X braces on the core's faces.
    for u in (LEDGE + 13 * RISE - 0.36, LEDGE + 26 * RISE - 0.36, STAIR_TOP - 0.42):
        for a, b in ((0, 1), (2, 3), (0, 2), (1, 3)):
            beam(acc, MT["wood"], (posts[a][0], posts[a][1], u), (posts[b][0], posts[b][1], u), 0.16, 0.2)
    for a, b in ((0, 1), (2, 3), (0, 2), (1, 3)):
        (ea, na), (eb, nb) = posts[a], posts[b]
        for lo, hi in ((-0.6, 3.2), (3.4, 6.4), (6.6, 9.6)):
            beam(acc, MT["rot"], (ea, na, lo), (eb, nb, hi), 0.1, 0.14)
    # The flights: open treads on stringers; the outer stringer on posts down into the water; a rail outside.
    flights = [  # (start (e, n), direction (de, dn), outward (oe, on), base u)
        ((e1, n0 - FW / 2), (-1, 0), (0, -1), LEDGE),
        ((e0 - FW / 2, n0), (0, 1), (-1, 0), LEDGE + 13 * RISE),
        ((e0, n1 + FW / 2), (1, 0), (0, 1), LEDGE + 26 * RISE),
    ]
    for (se, sn), (de, dn), (oe, on), u0 in flights:
        heading = math.degrees(math.atan2(de, dn))
        for k in range(1, 13):
            t = (k - 0.5) * RUN
            obox(acc, rng.choice([MT["pale"], MT["wood"], MT["pale"]]), (se + de * t, sn + dn * t, u0 + k * RISE - 0.03),
                 (RUN - 0.03, FW - 0.04, 0.06), heading + 90, rng.uniform(-0.6, 0.6), rng.uniform(-0.6, 0.6))
        for side in (-1, 1):
            off = side * (FW / 2 - 0.05)
            a = (se + oe * off, sn + on * off, u0 - 0.12)
            b = (se + oe * off + de * 4.0, sn + on * off + dn * 4.0, u0 + 12 * RISE - 0.12)
            beam(acc, MT["rot"], (a[0] - de * 0.15, a[1] - dn * 0.15, a[2] - 0.15 * RISE / RUN), (b[0] + de * 0.1, b[1] + dn * 0.1, b[2] + 0.1 * RISE / RUN), 0.08, 0.26)
        # Outer posts under the stringer (mid-flight), and the rail.
        oute, outn = se + oe * (FW / 2 + 0.02), sn + on * (FW / 2 + 0.02)
        for t in (1.3, 2.8):
            piling(acc, MT, rng, oute + de * t, outn + dn * t, 0.12, u0 + (t / RUN) * RISE - 0.28, seg=6, crust=0.4, crook=0.0)
        tops = []
        for t in (0.05, 2.0, 3.95):
            p = (oute + de * t, outn + dn * t)
            u = u0 + min(12, max(1, round(t / RUN))) * RISE
            box(acc, MT["wood"], p[0] - 0.05, p[0] + 0.05, p[1] - 0.05, p[1] + 0.05, u - 0.25, u + 1.0)
            tops.append((p[0], p[1], u + 0.97))
        beam(acc, MT["wood"], tops[0], tops[-1], 0.07, 0.09)
    # The corner landings (south-west at the top of F1, north-west at the top of F2), on posts.
    for (le0, le1, ln0, ln1), u in (((e0 - FW, e0, n0 - FW, n0), LEDGE + 13 * RISE), ((e0 - FW, e0, n1, n1 + FW), LEDGE + 26 * RISE)):
        for k in range(5):
            s = ln0 + k * FW / 5
            obox(acc, rng.choice([MT["pale"], MT["wood"]]), ((le0 + le1) / 2, s + FW / 10, u - 0.03), (FW / 5 - 0.025, FW - 0.04, 0.06), 90, 0, rng.uniform(-0.5, 0.5))
        box(acc, MT["rot"], le0 + 0.05, le1 - 0.05, ln0 + 0.05, ln1 - 0.05, u - 0.26, u - 0.06)
        corner = (le0 + 0.12, ln0 + 0.12 if ln0 < n0 else ln1 - 0.12)
        piling(acc, MT, rng, corner[0], corner[1], 0.14, u - 0.06, seg=6, crust=0.4, crook=0.0)
        for (pa, pb) in (((le0 + 0.05, ln0 + 0.05), (le0 + 0.05, ln1 - 0.05)),
                         (((le0 + 0.05, ln0 + 0.05), (le1, ln0 + 0.05)) if ln0 < n0 else ((le0 + 0.05, ln1 - 0.05), (le1, ln1 - 0.05)))):
            box(acc, MT["wood"], pa[0] - 0.05, pa[0] + 0.05, pa[1] - 0.05, pa[1] + 0.05, u, u + 1.03)
            rail(acc, MT["wood"], (pa[0], pa[1], u + 0.97), (pb[0], pb[1], u + 0.97), 0.07, 0.09)
    # The top landing: boards over the core, and a strip east of it running north (F3 arrives into it from the west)
    # to under the hatch; on joists and posts; rails round it.
    le1, ln1 = LAND_E, LAND_N
    for n in (n0 + 0.2, n0 + 1.4, n0 + 2.6, n0 + 3.8):
        box(acc, MT["rot"], e0 + 0.05, le1 - 0.05, n - 0.08, n + 0.08, STAIR_TOP - 0.3, STAIR_TOP - 0.06)
    for n in (n1 + 0.25, n1 + 1.45, ln1 - 0.2):
        box(acc, MT["rot"], e1 + 0.05, le1 - 0.05, n - 0.08, n + 0.08, STAIR_TOP - 0.3, STAIR_TOP - 0.06)
    e = e0
    while e < le1 - 0.02:
        w = min(0.24, le1 - e)
        hi = ln1 if e >= e1 - 0.01 else n1
        obox(acc, rng.choice([MT["pale"], MT["wood"], MT["rot"]]), (e + w / 2, (n0 + hi) / 2, STAIR_TOP - 0.028),
             (w - 0.025, hi - n0 - 0.02, 0.055), 0, rng.uniform(-0.3, 0.3), rng.uniform(-0.5, 0.5))
        e += w
    for pe, pn in ((le1 - 0.15, 57.5), (le1 - 0.15, 60.6), (le1 - 0.15, ln1 - 0.15), (e1 + 0.15, ln1 - 0.15)):
        piling(acc, MT, rng, pe, pn, 0.15, STAIR_TOP - 0.3, seg=6, crust=0.5, crook=0.0)
    rails = [((e0, n0), (le1, n0)), ((le1, n0), (le1, ln1)), ((le1, ln1), (e1, ln1)), ((e1, ln1), (e1, n1 + FW + 0.05)),
             ((e1 - 0.12, n1), (e0, n1)), ((e0, n1), (e0, n0))]
    for (ea, na), (eb, nb) in rails:
        for t in (0.0, 0.5):
            pe, pn = ea + (eb - ea) * t, na + (nb - na) * t
            box(acc, MT["wood"], pe - 0.05, pe + 0.05, pn - 0.05, pn + 0.05, STAIR_TOP, STAIR_TOP + 1.04)
        rail(acc, MT["wood"], (ea, na, STAIR_TOP + 0.97), (eb, nb, STAIR_TOP + 0.97), 0.07, 0.09)
        rail(acc, MT["wood"], (ea, na, STAIR_TOP + 0.5), (eb, nb, STAIR_TOP + 0.5), 0.05, 0.07)
    # The scrap of ceiling (north of the landing's open part: the camera looks in under its edge): joists east-west,
    # boards running north on them, ragged at the ends; the hatch in it over the landing's strip, framed, its lid
    # thrown back; piles under its corners.
    he, hn = HATCH
    ce0, ce1, cn0, cn1 = CEILING
    for n in (cn0 + 0.25, hn - 1.1, hn + 1.1, 65.4, cn1 - 0.3):
        box(acc, MT["pile_up"], ce0 + rng.uniform(-0.2, 0.3), ce1 + rng.uniform(-0.3, 0.2), n - 0.1, n + 0.1, CEIL_U - 0.36, CEIL_U - 0.07)
    for e in (he - 1.1, he + 1.1):
        box(acc, MT["pile_up"], e - 0.1, e + 0.1, hn - 1.0, hn + 1.0, CEIL_U - 0.33, CEIL_U - 0.07)
    e = ce0
    while e < ce1 - 0.1:
        w = 0.28
        lo, hi = cn0 + rng.uniform(0.0, 0.45), cn1 - rng.uniform(0.0, 1.0)
        spans = [(lo, hi)]
        if he - 1.0 < e + w / 2 < he + 1.0:
            spans = [(lo, hn - 1.0), (hn + 1.0, hi)]
        if rng.random() < 0.08:
            spans = spans[-1:]
        for a, b in spans:
            if b - a > 0.2:
                obox(acc, rng.choice([MT["pile_up"], MT["rot"]]), (e + w / 2, (a + b) / 2, CEIL_U - 0.035), (w - 0.025, b - a, 0.07), 0,
                     rng.uniform(-0.4, 0.4), rng.uniform(-0.8, 0.8))
        e += w
    # The lid, thrown back: boards on two battens, leaning up from the hatch's north edge.
    lid = Matrix.Translation(G(he, hn + 1.0, CEIL_U)) @ Matrix.Rotation(math.radians(-70), 4, "X")
    for k in range(7):
        x = -0.9 + k * 0.3
        acc.add(kit.cube(), MT["rot"], lid @ M((-x, -1.0, 0.03), scale=(0.27, 2.0, 0.05)))
    for y in (-0.4, -1.6):
        acc.add(kit.cube(), MT["pile_up"], lid @ M((0, y, 0.09), scale=(1.9, 0.12, 0.07)))
    for e, n in ((ce0 + 0.3, 63.2), (ce1 - 0.3, cn0 + 0.4), (ce0 + 0.3, cn1 - 0.3), (ce1 - 0.3, cn1 - 0.3)):
        piling(acc, MT, rng, e, n, 0.26, CEIL_U - 0.36, seg=7, crust=0.5)


# ---------------------------------------------------------------- the drowned masonry out in the dark

def stone_pier(acc, MT, rng, e, n, w, top):
    """A square foundation pier (the Hub's): courses of big stone, each a little askew, darker as it rises."""
    u = -1.2
    while u < top - 0.05:
        h = min(rng.uniform(0.55, 0.8), top - u)
        mat = MT["slime"] if u + h < 0.3 else MT["wet"] if u < 0.6 else rng.choice(MT["stone"]) if u < 3.5 else MT["wet"] if u < 5.5 else MT["dark"]
        s = w + rng.uniform(-0.08, 0.08)
        obox(acc, mat, (e + rng.uniform(-0.04, 0.04), n + rng.uniform(-0.04, 0.04), u + h / 2), (s, s + rng.uniform(-0.06, 0.06), h - 0.03), rng.uniform(-2, 2))
        u += h
    box(acc, MT["joint"], e - w / 2 + 0.06, e + w / 2 - 0.06, n - w / 2 + 0.06, n + w / 2 - 0.06, DEEP, top - 0.2)


def arcade(acc, MT, rng, ce, cn, span, spring, pier_h, broken):
    """Two piers and an arch between them (in a plane facing south), half of it fallen."""
    for side in (-1, 1):
        stone_pier(acc, MT, rng, ce + side * (span / 2 + 0.6), cn, 1.2, pier_h[0 if side < 0 else 1])
    r = span / 2
    k = 9
    for j in range(k):
        if j >= broken:
            break
        a0, a1 = math.pi - math.pi * j / k, math.pi - math.pi * (j + 1) / k
        pts = [(ce + rr * math.cos(a), spring + rr * math.sin(a)) for a, rr in ((a0, r), (a1, r), (a1, r + 0.5), (a0, r + 0.5))]
        mat = rng.choice(MT["stone"]) if j % 3 else MT["wet"]

        def vous(bm, pts=pts):
            lo = [bm.verts.new(G(e, cn - 0.5, u)) for e, u in pts]
            hi = [bm.verts.new(G(e, cn + 0.5, u)) for e, u in pts]
            bm.faces.new(lo)
            bm.faces.new(list(reversed(hi)))
            for i in range(4):
                bm.faces.new((lo[i], lo[(i + 1) % 4], hi[(i + 1) % 4], hi[i]))
            bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
        acc.add(vous, mat)
    # A fallen voussoir or two in the water below.
    for _ in range(2):
        obox(acc, MT["wet"], (ce + rng.uniform(-r, r) * 0.6, cn + rng.uniform(-0.8, 0.8), -0.15), (0.5, 0.9, 0.45), rng.uniform(0, 180), rng.uniform(-20, 20), rng.uniform(-20, 20))


def masonry(acc, MT, rng):
    placed = []
    for e, n, w, top in ((-15.0, 28.0, 2.2, 8.5), (19.0, 21.0, 2.0, 9.5), (23.0, 44.0, 2.4, 10.0), (-21.0, 45.0, 2.0, 8.0),
                         (20.5, 65.0, 2.2, 9.0), (-4.0, 71.0, 2.0, 9.5), (-21.0, 14.0, 1.8, 7.0), (15.0, -7.0, 2.0, 6.0),
                         (-9.0, -6.0, 1.8, 6.5), (27.0, 8.0, 2.0, 8.0)):
        top = min(top, view_ceiling(e, n, w * 0.7) - 0.3)
        if top > 0.6 and way_dist(e, n) > w + 1.5:
            stone_pier(acc, MT, rng, e, n, w, top)
            placed.append((e, n, w * 0.75 + 0.6))
    for e, n, w, top in ((-6.0, 4.5, 1.6, 0.3), (6.0, 7.5, 1.8, -0.1), (-9.0, 10.5, 2.0, 0.6), (13.0, 13.0, 1.5, 0.25),
                         (-4.5, 26.0, 1.6, 0.5), (14.5, 37.0, 1.8, 0.4), (-11.5, 37.0, 1.6, -0.05), (-19.0, 54.0, 1.6, 0.3),
                         (3.0, 65.0, 1.8, 0.2), (24.0, 30.0, 2.0, 0.5), (-14.0, 2.0, 1.6, 0.8), (18.0, 50.0, 1.4, 0.1)):
        if way_dist(e, n) > w + 1.2 and (BE - e) ** 2 + (BN - n) ** 2 > 16.5 ** 2:
            stone_pier(acc, MT, rng, e, n, w, top)
            placed.append((e, n, w * 0.75 + 0.6))
    arcade(acc, MT, rng, -8.8, 21.0, 3.2, 1.3, (2.4, 3.3), 6)
    placed += [(-11.2, 21.0, 1.6), (-6.4, 21.0, 1.6), (-8.8, 21.0, 2.2)]
    arcade(acc, MT, rng, 17.0, 11.0, 2.4, 0.9, (1.7, 2.2), 4)
    placed += [(15.2, 11.0, 1.5), (18.8, 11.0, 1.5), (17.0, 11.0, 1.8)]
    # A fallen column drum, half under.
    acc.add(kit.cylinder(12, 0.7, 1.6), MT["wet"], M(G(12.5, 4.0, -0.15), rot=(90, 8, 70)))
    placed.append((12.5, 4.0, 1.3))
    return placed


# ---------------------------------------------------------------- the forest

def forest(acc, MT, rng, keep_out):
    """Hundreds of piles, thicker away from the way, none on it (or within a metre of it), none leaning over it; any
    that would hide the way from the camera cut down to stumps."""
    piles = []
    grid = {}

    def near(e, n, s):
        for gx in range(math.floor(e / 2) - 2, math.floor(e / 2) + 3):
            for gy in range(math.floor(n / 2) - 2, math.floor(n / 2) + 3):
                for p in grid.get((gx, gy), ()):
                    if math.hypot(p.e - e, p.n - n) < s:
                        return True
        return False

    def put(p):
        piles.append(p)
        grid.setdefault((math.floor(p.e / 2), math.floor(p.n / 2)), []).append(p)

    def blocked(e, n):
        if (e - BE) ** 2 + (n - BN) ** 2 < 16.0 ** 2:
            return True
        if 8.6 < e < 11.4 and 13.4 < n < 26.9:
            return True
        if -16.6 < e < -5.2 and 54.4 < n < 67.8:
            return True
        return any(math.hypot(e - ke, n - kn) < kr for ke, kn, kr in keep_out)

    stats = dict(stumps=0, pairs=0, beams=0)
    for _ in range(26000):
        e, n = rng.uniform(-25.0, 30.0), rng.uniform(-12.0, 75.0)
        d = way_dist(e, n)
        if d < 1.4 or blocked(e, n):
            continue
        spacing = 2.7 if d < 4 else 2.1 if d < 8 else 1.5
        if near(e, n, spacing) or rng.random() > (0.55 if d < 4 else 0.85 if d < 8 else 1.0):
            continue
        r = rng.uniform(0.13, 0.25) if rng.random() < 0.85 else rng.uniform(0.2, 0.25)
        top = rng.uniform(5.0, 12.0) if d < 10 else rng.uniform(7.0, 12.0)
        lean = (rng.uniform(-0.012, 0.012), rng.uniform(-0.012, 0.012))
        if rng.random() < 0.22:
            a, t = rng.uniform(0, 2 * math.pi), math.tan(math.radians(rng.uniform(4, 12)))
            lean = (t * math.cos(a), t * math.sin(a))
        snapped = rng.random() < 0.14
        if snapped:
            top = rng.uniform(0.6, 4.0)
        # Never leaning over the way.
        te, tn = e + lean[0] * (top - DEEP), n + lean[1] * (top - DEEP)
        if way_dist(te, tn) < 1.0 + r or way_dist((e + te) / 2, (n + tn) / 2) < 1.0 + r:
            lean = (0.0, 0.0)
            te, tn = e, n
        ceiling = min(view_ceiling(e, n, r), view_ceiling(te, tn, r), view_ceiling((e + te) / 2, (n + tn) / 2, r))
        if top > ceiling - 0.2:
            if ceiling < 0.75:
                continue
            top = rng.uniform(max(0.45, ceiling * 0.45), ceiling - 0.2)
            snapped = True
            stats["stumps"] += 1
        crust = rng.uniform(0.75, 1.3) if rng.random() < 0.2 else rng.uniform(0.2, 0.5)
        seg = 7 if d < 9 else 6 if d < 15 else 5
        p = piling(acc, MT, rng, e, n, r, top, lean, seg=seg, crust=min(crust, top - 0.2) if top > 0.6 else crust, snapped=snapped, step=d < 15)
        p.snapped = snapped
        put(p)
        if d < 12 and rng.random() < 0.13 and top > 1.8:
            for _k in range(rng.choice((1, 1, 2))):
                pile_band(acc, MT, p, rng.uniform(0.9, min(3.5, top - 0.4)), MT["iron"] if rng.random() < 0.6 else MT["rust"], seg)
        # Some bound in pairs.
        if rng.random() < 0.09 and not snapped:
            a = rng.uniform(0, 2 * math.pi)
            r2 = rng.uniform(0.12, r)
            e2, n2 = e + (r + r2 + 0.015) * math.cos(a), n + (r + r2 + 0.015) * math.sin(a)
            top2 = min(top + rng.uniform(-1.5, 0.8), view_ceiling(e2, n2, r2) - 0.2)
            if way_dist(e2, n2) > 1.0 + r2 and top2 > 1.5 and not blocked(e2, n2):
                q = piling(acc, MT, rng, e2, n2, r2, top2, lean, seg=seg, crust=crust)
                q.snapped = False
                put(q)
                for u in (rng.uniform(0.8, 1.6), rng.uniform(2.2, min(4.0, top2 - 0.3))):
                    if u < min(top, top2) - 0.2:
                        strap(acc, MT, p, q, u)
                stats["pairs"] += 1
    # Cross-beams between neighbours, here and there; a few braces.
    for p in piles:
        if p.top < 4.5 or getattr(p, "snapped", False) or rng.random() > 0.2:
            continue
        for q in piles:
            if q is p or q.top < 4.5 or getattr(q, "snapped", False):
                continue
            dist = math.hypot(q.e - p.e, q.n - p.n)
            if not 1.4 < dist < 3.4:
                continue
            u = rng.uniform(1.8, min(p.top, q.top) - 1.2)
            a, b = p.axis(u), q.axis(u + rng.uniform(-0.25, 0.25))
            mid = ((a[0] + b[0]) / 2, (a[1] + b[1]) / 2, (a[2] + b[2]) / 2)
            if way_dist(mid[0], mid[1]) < 1.6 or not clear_view([a, b, mid], 0.12):
                continue
            beam(acc, MT["pile"] if u < 3.2 else MT["pile_up"] if u < 5.5 else MT["pile_top"], a, b, 0.16, 0.22, rng.uniform(-3, 3))
            stats["beams"] += 1
            if rng.random() < 0.3:
                lo = p.axis(max(0.4, u - 2.0))
                beam(acc, MT["pile"], lo, b, 0.12, 0.15)
            break
    return piles, stats


def hanging_chains(acc, MT, rng):
    """Chains hanging down out of the dark among the piles, ending in the water or just short of it."""
    out = []
    for e, n, end in ((-3.4, 8.6, 0.9), (5.2, 4.2, -0.5), (12.6, 23.0, 1.4), (-4.8, 31.5, -0.5), (-11.5, 47.5, 1.1), (2.5, 41.5, -0.5)):
        if way_dist(e, n) > 1.6 and clear_view([(e, n, 14.0), (e, n, end)], 0.1):
            chain(acc, MT["chain"], [(e, n, 14.0), (e + rng.uniform(-0.05, 0.05), n + rng.uniform(-0.05, 0.05), end)], 0.3, 0.04, seg=6, sides=3)
            if end > 0:
                ring(acc, MT["iron"], (e, n, end - 0.1), 0.11, 0.022, "n", sides=4, seg=10, arc=250, start=110)
            out.append((e, n))
    return out


def eyes(piles):
    """Print spots for eyes in the dark: out among the piles, 4-10 m off the way, with nothing tall straight between
    them and the camera."""
    spots = []
    for gx in range(-24, 30):
        for gy in range(-10, 74):
            e, n = gx + 0.5, gy + 0.5
            d = way_dist(e, n)
            if not 4.5 < d < 9.5 or (e - BE) ** 2 + (n - BN) ** 2 < 16.0 ** 2:
                continue
            if any(math.hypot(p.e - e, p.n - n) < 0.7 for p in piles):
                continue
            if not any(abs(se - e) < 7.0 and -1.0 < n - sn < 6.5 for col in SAMPLES.values() for se, sn, su in col):
                continue
            if any(abs(p.e - e) < 0.45 and 0 < n - p.n < 4.0 and p.top > 1.2 + (n - p.n) * TAN for p in piles):
                continue
            dense = sum(1 for p in piles if math.hypot(p.e - e, p.n - n) < 4.0)
            spots.append((dense, e, n, d))
    spots.sort(reverse=True)
    chosen = []
    for dense, e, n, d in spots:
        if all(math.hypot(e - ce, n - cn) > 8.0 for _, ce, cn, _ in chosen):
            chosen.append((dense, e, n, d))
        if len(chosen) == 12:
            break
    for dense, e, n, d in sorted(chosen, key=lambda c: c[2]):
        print(f"[hold] eyes ({e:.1f}, {n:.1f}) {d:.1f} m off the way, {dense} piles within 4 m")


# ---------------------------------------------------------------- build

def build(variant):
    global SAMPLES
    MT = materials()
    acc = Acc()
    if variant == "lantern_dead":
        lantern(acc, MT, Matrix.Identity(4))
        return [acc.build("Hold_lantern_dead")]

    rng = random.Random(1717)
    SAMPLES = view_samples()
    waking(acc, MT, rng)
    boardwalk(acc, MT, rng)
    foundations(acc, MT, rng)
    pens(acc, MT, rng)
    islet(acc, MT, rng)
    basin(acc, MT, rng)
    climb(acc, MT, rng)
    keep = masonry(acc, MT, rng)
    keep += [(4.6, 34.0, 1.0), (4.6, 35.5, 1.1), (8.7, 34.0, 1.0), (9.0, 35.5, 1.1), (11.15, 29.6, 1.0), (12.65, 29.6, 1.1),
             (12.1, 33.9, 1.0)]
    chains = hanging_chains(acc, MT, rng)
    keep += [(e, n, 0.8) for e, n in chains]
    piles, stats = forest(acc, MT, rng, keep)
    eyes(piles)
    ob = acc.build("Hold")
    tris = sum(len(p.vertices) - 2 for p in ob.data.polygons)
    print(f"[hold] piles {len(piles)} (stumps cut for the camera {stats['stumps']}, pairs {stats['pairs']}, beams {stats['beams']}); "
          f"faces {len(ob.data.polygons)}, tris {tris}, verts {len(ob.data.vertices)}")
    return [ob]
