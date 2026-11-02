"""The Hub: the Ferryman's island at the centre of the underworld, a quiet refuge. A small island in still black water:
a mossy top ringed by a shingle shore, a worn path from his hut's door south to the jetty (the builder lays the jetty,
from jetty.py), and his hut at the north, where you come up out of the Hold through the trapdoor in its floor.

Variants (game directions, see masonry.py; origins at the water's surface unless said):
  "island"    the ground: 28 x 22 m, its top at 0.5 (the quay's level), the hut's plot levelled at the north; the
              shingle sloping into the water, wading-deep a metre out, then a drop (the water's rule: below -0.35 you
              sink). Its path, its turf in patches, the wet shingle at the waterline.
  "hut"       the Ferryman's hut, origin at the centre of its floor (set on the island's plot): 5.6 x 4.6 m, a coursed
              stone footing and plank walls, the door south (open, swung in), a window east and west (warm), a stone
              hearth on the west wall (embers), the floor boarded, and in it, east of the middle, the open trapdoor: a
              ladder down a black shaft (the way up from the Hold).
  "hut_roof"  its roof, pitched to the north and south, grey shingles with moss, its eaves over the door, and the
              chimney (the builder fades it when you're inside: Storeys).
  "hatch"     the trapdoor's lid, 1 x 1 m, hinged along its north edge at the origin (the builder lays it open).
  "bench"     a weathered bench, 1.6 m (a place for a Lingerer to sit, later).
  "marker_city", "marker_orchard", "marker_hall"   a way marker: a tall pole standing in the water with a little
              lantern hung from its arm, one for each Area's waterway leading off into the mist (their colours: the
              Drowned City's cold teal, the Orchard's amber, the Hall's pale gold)."""
import math

import bpy
import numpy as np

import masonry as m

VARIANTS = ["island", "hut", "hut_roof", "hatch", "bench", "marker_city", "marker_orchard", "marker_hall"]

TOP = 0.5                          # the island's top (the quay's level)
RX, RY = 14.0, 11.0                # its half-extents (east-west, north-south)
CENTRE_N = 0.0
HUT_AT = (0.0, 5.2)                # the hut's floor centre (e, n) on the island
HUT_W, HUT_D = 5.6, 4.6
HUT_FLOOR = 0.28                   # the hut's floor above the island's top (its footing)
EAVES, RIDGE = 2.35, 3.85          # (above the hut's floor)
HATCH = (0.7, 1.7, 0.1, 1.1)       # the trapdoor's opening in the hut's floor (e0, e1, n0, n1, hut-local)
DOOR = (-0.6, 0.6)                 # the south door's span (e, hut-local)
WINDOW = (-0.5, 0.5, 1.0, 1.65)    # the east and west windows (n span, u span; hut-local)
JETTY_E = 0.0                      # where the jetty leaves the south shore (the builder's)
T = 0.22                           # wall thickness


def rr(e, n):
    """How far out (1 = the shore's top edge) a point is: an ellipse, its edge wobbling round."""
    q = np.hypot(e / RX, (n - CENTRE_N) / RY)
    th = np.arctan2((n - CENTRE_N) / RY, e / RX)
    edge = 1 + 0.07 * np.sin(3 * th + 1.1) + 0.045 * np.sin(5 * th + 2.3) + 0.03 * np.sin(8 * th + 0.4)
    return q / edge


def shore_d(e, n):
    """A signed distance (m, roughly) from the shore's top edge: negative inland."""
    return (rr(e, n) - 1) * min(RX, RY)


def path_e(n):
    """The path's middle (e) at n: from the hut's door down to the jetty, wandering a little."""
    return JETTY_E + 0.55 * np.sin(n * 0.33 + 0.6) * np.clip((HUT_AT[1] - 2.4 - n) / 3.0, 0, 1)


def height(e, n):
    d = shore_d(e, n)
    rng = np.random.default_rng(3)
    lump = 0.035 * np.sin(e * 0.9 + 0.3) * np.sin(n * 1.1 + 1.7) + 0.02 * np.sin(e * 2.3 + n * 1.7)
    top = TOP + lump * np.clip(-d / 2, 0, 1)
    # The hut's plot: levelled (its footing sits on it).
    plot = np.clip(1.4 - np.maximum(np.abs(e - HUT_AT[0]) - HUT_W / 2, np.abs(n - HUT_AT[1]) - HUT_D / 2), 0, 1.4) / 1.4
    top = top * (1 - plot) + TOP * plot
    # The shore: shingle from the top's edge (d = -2.2) down to the waterline (d = 0), on under the water to wading
    # depth (d = 1.1), then a drop.
    s = np.clip((d + 2.2) / 2.2, 0, 1)
    shingle = TOP + (0.02 - TOP) * (1 - (1 - s) ** 1.6)
    under = 0.02 + (-0.3 - 0.02) * np.clip(d / 1.1, 0, 1)
    drop = -0.3 + (-1.4 + 0.3) * np.clip((d - 1.1) / 0.5, 0, 1)
    u = np.where(d < -2.2, top, np.where(d < 0, shingle, np.where(d < 1.1, under, drop)))
    return u


def island():
    step = 0.5
    es = np.arange(-RX - 3, RX + 3 + 1e-6, step)
    ns = np.arange(-RY - 3, RY + 3 + 1e-6, step)
    E, N = np.meshgrid(es, ns, indexing="xy")
    rng = np.random.default_rng(7)
    E2 = E + rng.uniform(-0.12, 0.12, E.shape)  # (a little irregular: no grid showing)
    N2 = N + rng.uniform(-0.12, 0.12, N.shape)
    U = height(E2, N2)
    U += np.where(shore_d(E2, N2) > -2.2, rng.uniform(-0.03, 0.03, U.shape), 0)  # (the shingle: knobbly)
    rows, cols = E.shape
    keep = shore_d(E2, N2) < 1.9
    index = -np.ones(E.shape, int)
    verts = []
    for i in range(rows):
        for j in range(cols):
            if keep[i, j]:
                index[i, j] = len(verts)
                verts.append((-E2[i, j], -N2[i, j], U[i, j]))  # (Blender: -e, -n, u)
    mats = {
        "turf": [m.kit.mat("HubTurf1", "#5d6650"), m.kit.mat("HubTurf2", "#677056"), m.kit.mat("HubTurf3", "#545c49")],
        "path": [m.kit.mat("HubPath1", "#6f6455"), m.kit.mat("HubPath2", "#776b5b")],
        "shingle": [m.kit.mat("HubShingle1", "#7b7a76"), m.kit.mat("HubShingle2", "#8a8883"), m.kit.mat("HubShingle3", "#6c6b68")],
        "wet": [m.kit.mat("HubWetShingle", "#4a4b4d")],
        "rock": [m.kit.mat("HubRock", "#3e3d3f")],
    }
    order = [x for k in ("turf", "path", "shingle", "wet", "rock") for x in mats[k]]
    faces, fmat = [], []
    patch = rng.integers(0, 3, (rows // 4 + 2, cols // 4 + 2))  # (the turf's tones in patches)
    for i in range(rows - 1):
        for j in range(cols - 1):
            q = [index[i, j], index[i, j + 1], index[i + 1, j + 1], index[i + 1, j]]
            if min(q) < 0:
                continue
            ce, cn = E2[i:i + 2, j:j + 2].mean(), N2[i:i + 2, j:j + 2].mean()
            cu = U[i:i + 2, j:j + 2].mean()
            d = float(shore_d(ce, cn))
            slope = U[i:i + 2, j:j + 2].max() - U[i:i + 2, j:j + 2].min()
            on_path = abs(ce - path_e(cn)) < 0.75 + 0.15 * math.sin(cn * 2.1) and -RY + 1.5 < cn < HUT_AT[1] - 2.2
            if cu < -0.4 or slope > 0.45:
                mt = mats["rock"][0]
            elif cu < 0.09:
                mt = mats["wet"][0]
            elif d > -2.0:
                mt = mats["shingle"][(i * 7 + j * 3) % 3]
            elif on_path:
                mt = mats["path"][(i + j) % 2]
            else:
                mt = mats["turf"][patch[i // 4, j // 4]]
            # (Blender: -e, -n flips handedness: wind so the faces look up)
            faces.append((q[0], q[3], q[2], q[1]))
            fmat.append(order.index(mt))
    me = bpy.data.meshes.new("HubIsland")
    me.from_pydata(verts, [], faces)
    for mt in order:
        me.materials.append(mt)
    me.polygons.foreach_set("material_index", fmat)
    me.validate()
    me.update()
    # (faces up: make sure)
    import bmesh
    bm = bmesh.new()
    bm.from_mesh(me)
    for f in bm.faces:
        f.normal_update()
        if f.normal.z < 0:
            f.normal_flip()
    bm.to_mesh(me)
    bm.free()
    ob = bpy.data.objects.new("HubIsland", me)
    bpy.context.scene.collection.objects.link(ob)
    return ob


# ---------------------------------------------------------------- the hut

E0, E1, N0, N1 = -HUT_W / 2, HUT_W / 2, -HUT_D / 2, HUT_D / 2
FOOT = 0.6  # the stone footing's top (above the floor's level 0... see below)


def plank_wall(parts, e0, e1, n0, n1, u0, u1, openings=()):
    """Upright boards (three tones, each a hair proud or shy) between a sill rail and a wall plate, clear of
    `openings` ((a0, a1, b0, b1) along the wall and up it)."""
    rng = m.kit.rng
    boards = [m.mat(p) for p in m.PLANKS] + [m.kit.mat("HutBoardGrey", "#6a6158")]
    timber = m.mat(m.TIMBER)
    along_e = (e1 - e0) >= (n1 - n0)
    a0, a1 = (e0, e1) if along_e else (n0, n1)
    t0, t1 = (n0, n1) if along_e else (e0, e1)

    def box(name, material, lo, hi, b0, b1, tlo=None, thi=None):
        tlo = t0 if tlo is None else tlo
        thi = t1 if thi is None else thi
        if along_e:
            parts.append(m.box(name, material, lo, hi, tlo, thi, b0, b1))
        else:
            parts.append(m.box(name, material, tlo, thi, lo, hi, b0, b1))

    a = a0
    while a < a1 - 1e-3:
        w = min(rng.uniform(0.2, 0.28), a1 - a)
        mid = a + w / 2
        spans = [(u0, u1)]
        for o in openings:
            if o[0] < mid < o[1]:
                spans = [(lo, hi) for lo, hi in ((s0, min(s1, o[2])) for s0, s1 in spans) if hi - lo > 0.02] + \
                        [(lo, hi) for lo, hi in ((max(s0, o[3]), s1) for s0, s1 in spans) if hi - lo > 0.02]
        inset = rng.uniform(-0.012, 0.012)
        for lo, hi in spans:
            box("Board", rng.choice(boards), a + 0.006, a + w - 0.006, lo, hi, t0 + 0.03 + inset, t1 - 0.03 + inset)
        a += w
    # The wall plate along the top and a sill rail at the foot, outside the boards.
    lift = 0.0 if along_e else -0.012  # (the end walls' plates a hair lower: none in one plane at the corners)
    box("Plate", timber, a0, a1, u1 - 0.14 + lift, u1 + 0.02 + lift, t0 - 0.02, t1 + 0.02)


def hut():
    parts = []
    timber = m.mat(m.TIMBER)
    void = m.kit.mat("HutShaft", "#050506")
    # The footing: coursed stone, from below the ground up to the floor (0 is the island's top; the floor at FLOOR).
    F = HUT_FLOOR
    for e0, e1, n0, n1, holes in (
            (E0, E1, N0, N0 + T, [(DOOR[0] - 0.03, DOOR[1] + 0.03, F - 0.02, F + 2.2)]),  # (a hair wider: the jambs cover its edge)
            (E0, E1, N1 - T, N1, []),
            (E0, E0 + T, N0 + T, N1 - T, []),
            (E1 - T, E1, N0 + T, N1 - T, [])):
        m.wall(parts, e0, e1, n0, n1, -0.4, F + 0.35, openings=[(h[0], h[1], h[2], h[3]) for h in holes], course=0.22, cap=False)
    # The step up to the door (outside, south), a flat stone.
    parts.append(m.box("DoorStep", m.mat(m.CAP), DOOR[0] - 0.2, DOOR[1] + 0.2, N0 - 0.5, N0, -0.1, F * 0.5))
    # The floor: boards over joists, the trapdoor's opening left open.
    m.planks(parts, E0 + T, E1 - T, N0 + T, N1 - T, F, holes=[HATCH])
    # The shaft under the trapdoor: black, its sides boarded at the top, a ladder down it.
    h0, h1, k0, k1 = HATCH
    parts.append(m.box("ShaftFloor", void, h0, h1, k0, k1, -3.0, -2.9))
    for e0, e1, n0, n1 in ((h0 + 0.005, h0 + 0.06, k0 + 0.06, k1 - 0.06), (h1 - 0.06, h1 - 0.005, k0 + 0.06, k1 - 0.06),
                           (h0 + 0.005, h1 - 0.005, k0 + 0.005, k0 + 0.06), (h0 + 0.005, h1 - 0.005, k1 - 0.06, k1 - 0.005)):
        # (lining the opening, inside it: clear of the joists round it)
        parts.append(m.box("Shaft", void, e0, e1, n0, n1, -2.89, F - 0.21))
        parts.append(m.box("ShaftBoards", timber, e0, e1, n0, n1, F - 0.21, F - 0.05))
    for s in (h0 + 0.22, h1 - 0.22):  # the ladder's stiles against the north side, its rungs
        parts.append(m.box("LadderStile", timber, s - 0.03, s + 0.03, k1 - 0.1, k1 - 0.07, -2.885, F + 0.5))
    for k in range(9):
        u = -2.6 + k * 0.33
        parts.append(m.box("LadderRung", timber, h0 + 0.2, h1 - 0.2, k1 - 0.09, k1 - 0.05, u, u + 0.04))
    # The walls above the footing: boards, the door south, a window east and west.
    U0, U1 = F + 0.35, F + EAVES
    door = (DOOR[0] - 0.1, DOOR[1] + 0.1, F, F + 2.18)  # (the boards clear of its jambs and lintel)
    plank_wall(parts, E0, E1, N0, N0 + T, U0, U1, openings=[door])
    plank_wall(parts, E0, E1, N1 - T, N1, U0, U1)
    for e0, e1 in ((E0, E0 + T), (E1 - T, E1)):
        plank_wall(parts, e0, e1, N0 + T, N1 - T, U0, U1, openings=[(WINDOW[0], WINDOW[1], F + WINDOW[2], F + WINDOW[3])])
    # Corner posts (on the long walls' ends only: no two faces in one plane).
    for e in (E0, E1 - 0.16):
        for n in (N0, N1 - 0.16):
            parts.append(m.box("Post", timber, e - 0.03, e + 0.19, n - 0.03, n + 0.19, U0 + 0.005, U1 + 0.04))
    m.lintel(parts, DOOR[0], DOOR[1], N0, N0 + T, F, F + 2.0)
    # The door, open: swung right in, flat against the wall east of the opening (out of the way).
    door_mat = m.kit.mat("HutDoor", "#5c4a3b")
    parts.append(m.box("Door", door_mat, DOOR[1] + 0.12, DOOR[1] + 1.24, N0 + T + 0.02, N0 + T + 0.08, F + 0.03, F + 1.99))
    # The windows: warm panes, a frame, and the shutters open against the wall outside.
    glass = m.kit.mat("HutWindow_Glow", "#ffbe73")
    for side in (-1, 1):
        e_out = E1 if side > 0 else E0
        e_in = E1 - T if side > 0 else E0 + T
        mid = (e_out + e_in) / 2
        parts.append(m.box("WindowPane", glass, mid - 0.02, mid + 0.02, WINDOW[0], WINDOW[1], F + WINDOW[2], F + WINDOW[3]))
        parts.append(m.box("WindowBar", timber, mid - 0.04, mid + 0.04, -0.03, 0.03, F + WINDOW[2] + 0.01, F + WINDOW[3] - 0.01))
        parts.append(m.box("WindowSill", timber, min(e_out, e_in) - 0.06, max(e_out, e_in) + 0.06, WINDOW[0] - 0.08, WINDOW[1] + 0.08,
                           F + WINDOW[2] - 0.06, F + WINDOW[2] + 0.006))
        for n in (WINDOW[0] - 0.55, WINDOW[1] + 0.05):
            parts.append(m.box("Shutter", door_mat, e_out + side * 0.03, e_out + side * 0.07, n, n + 0.5, F + WINDOW[2] - 0.02, F + WINDOW[3] + 0.02))
    # The hearth on the west wall: a stone fireplace, its embers.
    stone = m.mat(m.STONE[1])
    parts.append(m.box("Hearth", stone, E0 + T + 0.01, E0 + T + 0.55, -0.9, 0.9, F, F + 0.25))
    parts.append(m.box("HearthBack", stone, E0 + T + 0.02, E0 + T + 0.2, -0.78, 0.78, F + 0.255, F + 1.29))
    parts.append(m.box("HearthCheekS", stone, E0 + T + 0.01, E0 + T + 0.5, -0.8, -0.6, F + 0.25, F + 1.3))
    parts.append(m.box("HearthCheekN", stone, E0 + T + 0.01, E0 + T + 0.5, 0.6, 0.8, F + 0.25, F + 1.3))
    parts.append(m.box("Mantel", timber, E0 + T + 0.01, E0 + T + 0.62, -0.95, 0.95, F + 1.3, F + 1.42))
    embers = m.kit.mat("HutEmbers_Glow", "#ff7a32")
    for k in range(5):
        a = k * 1.3
        parts.append(m.part("Ember", m.kit.ico(1, 0.07), embers, m.M((-(E0 + T + 0.32 + 0.06 * math.cos(a)), -(0.15 * math.sin(a * 1.7)), F + 0.3))))
    parts.append(m.part("Log", m.kit.cylinder(6, 0.06, 0.6), timber, m.M((-(E0 + T + 0.34), 0, F + 0.33), rot=(90, 0, 8))))
    return [m.kit.join(parts, "Hut")]


def hut_roof():
    parts = []
    rng = m.kit.rng
    F = HUT_FLOOR
    shingles = [m.kit.mat("HutShingle1", "#8c8277"), m.kit.mat("HutShingle2", "#9a8f82"), m.kit.mat("HutShingle3", "#7e756b")]
    moss = m.kit.mat("HutMoss", "#6f7a55")
    over = 0.45  # the eaves' overhang
    eaves, ridge = F + EAVES, F + RIDGE
    half = HUT_D / 2 + over
    slope = math.degrees(math.atan2(ridge - eaves, half))
    rows = 14
    for side in (-1, 1):
        for k in range(rows):
            t = (k + 0.5) / rows
            n = side * half * (1 - t)
            u = eaves + (ridge - eaves) * t + 0.02 * (k % 2)
            depth = half / rows * 1.25 / math.cos(math.radians(slope))
            lo = E0 - over - 0.011 * k - (0.005 if side > 0 else 0.0)  # (each course's ends a little out from the last's:
            hi = E1 + over + 0.011 * k + (0.005 if side > 0 else 0.0)  #  none in one plane)
            e = lo - (0.17 if k % 2 else 0.0)   # (each course's joints offset from the last)
            while e < hi - 0.05:
                w = min(rng.uniform(0.28, 0.42), hi - e)
                mat = moss if rng.random() < 0.14 else rng.choice(shingles)
                c = max(e, lo) + (min(e + w, hi) - max(e, lo)) / 2
                width = min(e + w, hi) - max(e, lo) - 0.02
                if width > 0.05:
                    parts.append(m.part("Shingle", m.kit.cube(), mat,
                                        m.M((-c, -n, u + rng.uniform(-0.008, 0.008)), rot=(side * slope + rng.uniform(-1.5, 1.5), 0, 0),
                                            scale=(width, depth, 0.07 + 0.004 * (k % 3)))))
                e += w
    parts.append(m.box("Ridge", m.mat(m.TIMBER), E0 - over - 0.07, E1 + over + 0.07, -0.12, 0.12, ridge - 0.04, ridge + 0.1))
    # The gables: boards up to the ridge, over the walls' plates (east and west).
    boards = m.mat(m.PLANKS[1])
    for e0, e1 in ((E0, E0 + T), (E1 - T, E1)):
        for k in range(5):
            h = (HUT_D / 2) * (1 - k / 5)
            parts.append(m.box("Gable", boards, e0 + 0.03, e1 - 0.03, -h, h, eaves + k * 0.3, eaves + (k + 1) * 0.3))
    # The chimney, up from the hearth through the roof's west end.
    stone = m.mat(m.STONE[0])
    parts.append(m.box("Chimney", stone, E0 + 0.1, E0 + 0.75, -0.35, 0.35, eaves - 0.2, ridge + 0.9))
    parts.append(m.box("ChimneyCap", m.mat(m.CAP), E0 + 0.04, E0 + 0.81, -0.41, 0.41, ridge + 0.9, ridge + 1.0))
    return [m.kit.join(parts, "HutRoof")]


def hatch():
    parts = []
    w, d = HATCH[1] - HATCH[0], HATCH[3] - HATCH[2]
    boards = [m.mat(p) for p in m.PLANKS]
    for k in range(4):
        e = -w / 2 + k * w / 4
        parts.append(m.box("HatchBoard", boards[k % 3], e + 0.01, e + w / 4 - 0.01, -d, 0, 0.0, 0.05))
    timber = m.mat(m.TIMBER)
    for n in (-d + 0.15, -0.15):
        parts.append(m.box("HatchBatten", timber, -w / 2 + 0.05, w / 2 - 0.05, n - 0.05, n + 0.05, 0.05, 0.09))
    iron = m.kit.mat("HatchIron", "#2e2c2c")
    parts.append(m.part("HatchRing", m.kit.cylinder(10, 0.06, 0.02), iron, m.M((0, d * 0.75, 0.062))))
    for e in (-w / 2 + 0.2, w / 2 - 0.2):
        parts.append(m.box("Hinge", iron, e - 0.04, e + 0.04, -0.09, 0.02, 0.052, 0.068))
    return [m.kit.join(parts, "Hatch")]


def bench():
    parts = []
    wood = m.kit.mat("BenchWood", "#6b6156")
    dark = m.mat(m.TIMBER)
    parts.append(m.box("Seat", wood, -0.8, 0.8, -0.18, 0.18, 0.42, 0.48))
    for e in (-0.62, 0.62):
        parts.append(m.box("Leg", dark, e - 0.06, e + 0.06, -0.14, 0.14, 0.0, 0.42))
    parts.append(m.box("Stretcher", dark, -0.62, 0.62, -0.03, 0.03, 0.12, 0.18))
    return [m.kit.join(parts, "Bench")]


MARKER_COLOURS = {"marker_city": "#8fd6cc", "marker_orchard": "#ffb35c", "marker_hall": "#f2dfa6"}
MARKER_GLOWS = {"marker_city": "MarkerCity_Glow", "marker_orchard": "MarkerOrchard_Glow", "marker_hall": "MarkerHall_Glow"}


def marker(variant):
    parts = []
    wood = m.kit.mat("MarkerPole", "#4c443d")
    iron = m.kit.mat("MarkerIron", "#2a2828")
    glow = m.kit.mat(MARKER_GLOWS[variant], MARKER_COLOURS[variant])
    stone = m.kit.mat("MarkerStones", "#4a4a4c")
    parts.append(m.part("Pole", m.kit.cylinder(7, 0.07, 3.2), wood, m.M((0, 0, 1.0), rot=(2, 1, 0))))
    parts.append(m.part("Arm", m.kit.cylinder(5, 0.035, 0.6), wood, m.M((-0.25, 0, 2.45), rot=(0, 90, 0))))
    parts.append(m.part("Chain", m.kit.cylinder(4, 0.01, 0.2), iron, m.M((-0.5, 0, 2.33))))
    parts.append(m.part("LanternTop", m.kit.cone(4, 0.07, 0.01, 0.06), iron, m.M((-0.5, 0, 2.22), rot=(0, 0, 45))))
    parts.append(m.box("LanternGlass", glow, 0.44, 0.56, -0.06, 0.06, 1.98, 2.18))
    parts.append(m.box("LanternBase", iron, 0.43, 0.57, -0.07, 0.07, 1.96, 1.99))
    for k in range(5):  # the stones heaped round its foot (below and just above the water)
        a = k * 1.256
        parts.append(m.part("Stone", m.kit.ico(1, 0.22 + 0.05 * (k % 2)), stone,
                            m.M((-0.3 * math.cos(a), -0.3 * math.sin(a), -0.12 + 0.06 * (k % 3)), scale=(1, 0.8, 0.6)), jitter=0.04))
    return [m.kit.join(parts, "Marker")]


def build(variant):
    if variant == "island":
        return [island()]
    if variant == "hut":
        return hut()
    if variant == "hut_roof":
        return hut_roof()
    if variant == "hatch":
        return hatch()
    if variant == "bench":
        return bench()
    return marker(variant)
