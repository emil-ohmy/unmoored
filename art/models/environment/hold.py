"""The Hold: where the game begins, before the Hub. A cave whose walls are the souls kept in it: so many, kept so long,
that they became the place. From afar dark wet stone; close to, an undertone of something that was alive (a faint
grey-violet, pale streaks like fat in marble or the folds of cloth), and here and there the stone forms faces and hands,
half out of the wall, worn smooth, like bodies under a sheet. The walls breathe (material HoldWall_Breathe: the game
moves those vertices along their normals); floors, ledges, steps and islets are plain rock and never move. Black still
water in the pools and the lake (the game draws its surface at u = 0). Nothing overhead: the dark is the ceiling. No
light sources, nothing glows. (Never said in the game: it's the Ferryman's hold, where he keeps what he's taken.)

The way, south to north (the camera looks north from the south, 15 m back, 50 degrees down):
- the Pool: a round cavern, a black pool filling it, the rock islet where the Apprentice wakes (the origin), a rock bar
  north to the cavern's north side; walls tall round the north half, low on the south;
- the Breathing Passage: a winding passage north, its living walls close on either side;
- the Gallery of the Kept: a long chamber, its west, north and east walls dense with the kept (faces, hands, bodies under
  the sheet of the stone); a shallow black pool along its east side;
- the Black Water: a cavern north-west, a still lake; a ledge round its east and north sides; a rock islet near its middle
  (the game's Mooring post), a rock bar to it from the east ledge;
- the Way Out: from the north ledge's west end a rock ramp with broad steps climbs back east along the north wall to a
  ledge before a tall narrow crack in the wall; through it, a short threshold (a rock-walled gap through the wall, then
  a passage between living walls) into
- the Chamber of That Would Not Let Go (the Hold's Boss): a round cavern up at the crack's level, its floor flat rock,
  its walls tall on the north, east and west (faces and hands in the east and west walls), low on the south. Its north
  wall is a broad, smooth, gently concave face, clear of the kept: the Boss's face and arms (the game's) come out of it.
  At that wall's foot, behind where the face hangs, a tunnel mouth: a short tunnel north under the rock, rising, to an
  opening open above and to the north (the game lights grey daylight there): the way out, to the Hub.
Every wall is one sheet (a curtain round the open space: finely divided, smooth); the rock mass round the spaces is a
height field (its tops low south of any floor, so the camera always sees the way).

Variants: "hold" (all of it, placed at the origin); for the game to place and animate (each modelled facing -Y in
Blender, i.e. game north when placed with yaw 0; as for every static prop, transform.forward is then its back):
"face_1" (asleep), "face_2" (a silent cry), "face_3" (turned half aside), "hand_1" (palm flat, fingers spread),
"hand_2" (reaching, fingers curled): reliefs of the kept on a short stone back, origin at the centre of the back;
"eye_socket" (a ring of stone lips, hollow inside, origin at the eye's centre), "eye_ball" (origin at its centre, the
iris on its front), "eye_lid_top" / "eye_lid_bottom" (shells round the ball, modelled closed, origin at the eye's
centre: they turn about its left-right axis to open); "lantern_dead" (one small unlit lantern, origin at its base).
Game directions (E east, N north, U up); a game point (e, n, u) is Blender's (-e, -n, u).
"""
import math
import random

import bmesh
import bpy
import numpy as np
from mathutils import Matrix, Vector

import masonry as m

kit = m.kit
M = kit.M
VARIANTS = ["hold", "face_1", "face_2", "face_3", "hand_1", "hand_2", "eye_socket", "eye_ball", "eye_lid_top",
            "eye_lid_bottom", "lantern_dead"]

TOP = 0.5             # walkable tops
DEEP = -2.6           # the lake's bed (everything solid goes below u -1.5)

# The spaces (game e, n).
POOL_C, POOL_R = (0.0, 2.0), 8.0
ISLET_C, ISLET_R = (0.0, 0.0), 2.0                     # where the Apprentice wakes
BAR = [(0.0, 1.0), (0.0, 9.0)]                         # the rock bar north from the islet (2 m wide)
PASSAGE = [(0.0, 8.0), (0.4, 12.0), (2.0, 15.5), (2.6, 19.0), (1.9, 22.5), (3.0, 25.8), (4.0, 29.5)]
PASSAGE_R = [1.75, 1.8, 1.65, 1.85, 1.7, 1.8, 2.0]
GALLERY = (-2.0, 10.0, 28.0, 46.0)                     # e0, e1, n0, n1 (rounded)
GPOOL = (7.4, 11.0, 31.0, 43.0)                        # the gallery's shallow pool, against its east wall
THROAT = [(2.0, 44.5), (2.6, 52.6)]
THROAT_R = 1.35
LAKE_C = (-7.0, 57.5)                                  # the Black Water's middle
CAVERN = (13.5, 13.0, 13.5, 8.5)                       # its walls' radii east, west, north, south
WATER = (10.0, 13.6, 8.5, 9.5)                         # the lake's shore (beyond the walls west and south)
L_ISLET_C, L_ISLET_R = (-6.0, 54.0), 2.25              # the Mooring islet
L_BAR = [(-4.4, 54.0), (3.6, 54.0)]                    # the bar to it from the east ledge (1.5 m wide)
RAMP_T0, RAMP_TL, RAMP_T1 = 136.0, 77.0, 57.0          # the ramp round the north wall (degrees round LAKE_C, from east):
RAMP_RMID = 12.3                                       # its low end, where its top ledge begins, the ledge's end
RAMP_W, LAND_W, RAMP_BACK = 2.05, 2.7, 0.6             # its width (from the wall), the top ledge's; how far under the wall
RAMP_FOOT = 1.6                                        # a round of floor at its foot (the north ledge's west end)
STEPS, STEP_H, TOP_U = 9, 0.22, 6.0                    # its broad steps; the top ledge's height
CRACK_E, CRACK_W, CRACK_H = -2.0, 1.5, 4.0             # the crack in the north wall (its middle's e; width; height)
CRACK_FOOT = (-2.06, 70.31)                            # the crack's middle at the wall's foot (the wall there faces
_CD = math.hypot(0.33, 0.94)                           # (-0.33, -0.94)): the way the crack runs in, and across it
CRACK_DIR = (0.33 / _CD, 0.94 / _CD)
CRACK_SIDE = (CRACK_DIR[1], -CRACK_DIR[0])
CRACK_END = 1.8                                        # how far the crack runs in (through the wall, to the threshold)
THRESH_LEN, THRESH_HW = 4.4, 1.4                       # the threshold passage beyond (on from the crack, into the chamber)
CHAMBER_C, CHAMBER_R = (-1.0, 82.0), 8.5               # the Boss's chamber (its floor at TOP_U)
BOSS_E, BOSS_N, BOSS_BOW, BOSS_HALF = -1.0, 90.5, 22.0, 7.5   # its north wall's face: foot line n = BOSS_N - (e - BOSS_E)^2
#                                                        / (2 BOSS_BOW), from e BOSS_E - BOSS_HALF to BOSS_E + BOSS_HALF
TUNNEL_W, TUNNEL_H, TUNNEL_RISE = 2.4, 3.0, 1.0        # the way out behind it: its mouth; how far its floor rises
TUNNEL_ROOF, CLEFT_HW = 5.5, 1.4                       # where its roof ends (the opening, open above and north): its half-width
SPACES = ["pool", "passage", "gallery", "throat", "lake", "chamber", "threshold"]
H_TALL = [6.8, 6.2, 8.4, 5.6, 8.6, 15.3, 7.9]          # the walls' tops (u) on the far sides of each space
H_LOW = [1.3, 1.3, 1.3, 1.3, 1.3, 6.75, 6.75]            # the walls' tops on the near (south) sides
TAN_WALK, TAN_WATER = 1.0, 1.15                        # how steeply anything south of a floor may rise (the camera)

RES = 0.2                                              # the fields' grid
GX0, GX1, GY0, GY1 = -32.0, 22.0, -18.0, 104.0
STEP = 0.3                                             # the living walls' vertex spacing

S_TOP = math.radians(RAMP_T0 - RAMP_TL) * RAMP_RMID
STEP_S = [1.5 + k * (S_TOP - 2.7) / (STEPS - 1) for k in range(STEPS)]
SLOPE = (TOP_U - 0.55 - STEPS * STEP_H) / (S_TOP - 0.6)


def G(e, n, u):
    return (-e, -n, u)


# ---------------------------------------------------------------- materials

def materials():
    c = kit.mat
    return dict(
        wall=c("HoldWall_Breathe", "#3b3640"),       # the living walls: dark wet stone, a grey-violet undertone
        vein=c("HoldWallVein_Breathe", "#7a7268"),   # pale streaks in them (fat in marble, folds of cloth); living too
        kept=c("HoldKept_Breathe", "#5b5361"),       # the kept: faces, hands, a little paler; living
        rock=c("HoldRock", "#4a4b4e"),               # floors, ledges, islets, steps (slate grey: the way reads)
        wet=c("HoldRockWet", "#2d2f32"),             # wet edges, the ramp's face, the crack
        dark=c("HoldRockDark", "#19191c"),           # pool beds, the rock mass's tops, stalactites
        white=c("HoldEyeWhite", "#cdc8b8"),
        iris=c("HoldEyeIris", "#3c4a41"),
        pupil=c("HoldEyePupil", "#060707"),
        lantern=c("HoldLanternDead_Metal", "#2a2827"),
        glass=c("HoldGlassDead", "#70736c"),
    )


# ---------------------------------------------------------------- small maths

def smoothstep(e0, e1, x):
    t = np.clip((np.asarray(x, float) - e0) / (e1 - e0), 0.0, 1.0)
    return t * t * (3 - 2 * t)


def smin(a, b, k):
    h = np.clip(0.5 + 0.5 * (b - a) / k, 0.0, 1.0)
    return b * (1 - h) + a * h - k * h * (1 - h)


def smax(a, b, k):
    return -smin(-a, -b, k)


def _hash(ix, iy, iz, seed):
    with np.errstate(over="ignore"):
        return _hash2(ix, iy, iz, seed)


def _hash2(ix, iy, iz, seed):
    h = ix * np.int64(374761393) + iy * np.int64(668265263) + iz * np.int64(1440670441) + np.int64(seed * 1013904223 + 7)
    h = (h ^ (h >> 13)) * np.int64(1274126177)
    h = h ^ (h >> 16)
    return (h & 0xFFFF).astype(float) / 32767.5 - 1.0


def noise3(x, y, z, seed=0):
    x, y, z = np.broadcast_arrays(np.asarray(x, float), np.asarray(y, float), np.asarray(z, float))
    xf, yf, zf = np.floor(x), np.floor(y), np.floor(z)
    xi, yi, zi = xf.astype(np.int64), yf.astype(np.int64), zf.astype(np.int64)
    fx, fy, fz = x - xf, y - yf, z - zf
    fx, fy, fz = fx * fx * (3 - 2 * fx), fy * fy * (3 - 2 * fy), fz * fz * (3 - 2 * fz)

    def c(dx, dy, dz):
        return _hash(xi + dx, yi + dy, zi + dz, seed)

    x00 = c(0, 0, 0) + (c(1, 0, 0) - c(0, 0, 0)) * fx
    x10 = c(0, 1, 0) + (c(1, 1, 0) - c(0, 1, 0)) * fx
    x01 = c(0, 0, 1) + (c(1, 0, 1) - c(0, 0, 1)) * fx
    x11 = c(0, 1, 1) + (c(1, 1, 1) - c(0, 1, 1)) * fx
    y0 = x00 + (x10 - x00) * fy
    y1 = x01 + (x11 - x01) * fy
    return y0 + (y1 - y0) * fz


def fbm(x, y, z=0.0, octaves=3, seed=0):
    total, amp, norm = 0.0, 1.0, 0.0
    for k in range(octaves):
        total = total + noise3(np.asarray(x) * 2 ** k, np.asarray(y) * 2 ** k, np.asarray(z) * 2 ** k, seed + 31 * k) * amp
        norm += amp
        amp *= 0.5
    return total / norm


def g2(x, z, cx, cz, sx, sz):
    return np.exp(-(((x - cx) / sx) ** 2 + ((z - cz) / sz) ** 2))


# ---------------------------------------------------------------- one mesh, in game coordinates

class Mesh:
    """Everything goes into one mesh. Patches are added as arrays (game e, n, u) with the direction each face should
    face, so windings never need thinking about."""

    def __init__(self):
        self.v, self.f, self.fm, self.fs = [], [], [], []
        self.mats, self.index = [], {}

    def mi(self, material):
        if material.name not in self.index:
            self.index[material.name] = len(self.mats)
            self.mats.append(material)
        return self.index[material.name]

    def patch(self, V, T, mats, smooth, expect, per_face=False):
        """V (k, 3) points; T (m, 3) triangles (or (m, 4) quads); mats: one material or a list per face; expect: one
        direction, or (k, 3) per vertex, the faces' normals should agree with."""
        V = np.asarray(V, float)
        T = np.asarray(T, int)
        if len(T) == 0:
            return
        a, b, c = V[T[:, 0]], V[T[:, 1]], V[T[:, 2]]
        nrm = np.cross(b - a, c - a)
        ex = np.asarray(expect, float)
        if per_face:
            want = ex
        else:
            want = ex[T].mean(axis=1) if ex.ndim == 2 else np.broadcast_to(ex, nrm.shape)
        flip = (nrm * want).sum(axis=1) < 0
        T = T.copy()
        T[flip] = T[flip][:, ::-1]
        base = len(self.v)
        self.v.extend(map(tuple, V))
        ms = [self.mi(mats)] * len(T) if not isinstance(mats, (list, tuple)) else [self.mi(x) for x in mats]
        sm = [bool(smooth)] * len(T) if isinstance(smooth, bool) else [bool(x) for x in smooth]
        for t, k, f in zip(T, ms, sm):
            self.f.append(tuple(int(i) + base for i in t))
            self.fm.append(k)
            self.fs.append(f)

    def tris(self):
        return sum(len(f) - 2 for f in self.f)

    def build(self, name, recalc=False):
        me = bpy.data.meshes.new(name)
        me.from_pydata([(-e, -n, u) for e, n, u in self.v], [], self.f)
        for mat in self.mats:
            me.materials.append(mat)
        me.polygons.foreach_set("material_index", self.fm)
        me.polygons.foreach_set("use_smooth", self.fs)
        me.validate(clean_customdata=False)
        if recalc:   # a closed solid: normals out
            bm = bmesh.new()
            bm.from_mesh(me)
            bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
            bm.to_mesh(me)
            bm.free()
        me.update()
        ob = bpy.data.objects.new(name, me)
        bpy.context.scene.collection.objects.link(ob)
        return ob


def zipper(ta, tb):
    """Triangles joining two columns of vertices (parameters ta, tb rising from 0 to 1, any counts): pairs of indices
    ('a', i) / ('b', j), in a consistent winding."""
    out = []
    p = q = 0
    na, nb = len(ta), len(tb)
    while p < na - 1 or q < nb - 1:
        if p == na - 1 or (q < nb - 1 and tb[q + 1] < ta[p + 1]):
            out.append((("a", p), ("b", q), ("b", q + 1)))
            q += 1
        else:
            out.append((("a", p), ("b", q), ("a", p + 1)))
            p += 1
    return out


# ---------------------------------------------------------------- the plan: fields over the ground

def sd_circle(E, N, c, r):
    return np.hypot(E - c[0], N - c[1]) - r


def sd_chain(E, N, pts, radii):
    best = np.full(np.shape(E), 1e9)
    radii = radii if isinstance(radii, (list, tuple)) else [radii] * len(pts)
    for (a, ra), (b, rb) in zip(zip(pts, radii), zip(pts[1:], radii[1:])):
        dx, dy = b[0] - a[0], b[1] - a[1]
        t = np.clip(((E - a[0]) * dx + (N - a[1]) * dy) / (dx * dx + dy * dy), 0, 1)
        best = np.minimum(best, np.hypot(E - a[0] - t * dx, N - a[1] - t * dy) - (ra + (rb - ra) * t))
    return best


def sd_qellipse(E, N, c, radii):
    """An egg of four quarter-ellipses (radii east, west, north, south): an approximate distance."""
    x, y = E - c[0], N - c[1]
    a = np.where(x > 0, radii[0], radii[1])
    b = np.where(y > 0, radii[2], radii[3])
    k = np.sqrt((x / a) ** 2 + (y / b) ** 2)
    return np.hypot(x, y) * (1 - 1 / np.maximum(k, 1e-6))


def sd_rrect(E, N, e0, e1, n0, n1, r):
    cx, cy = (e0 + e1) / 2, (n0 + n1) / 2
    qx = np.abs(E - cx) - ((e1 - e0) / 2 - r)
    qy = np.abs(N - cy) - ((n1 - n0) / 2 - r)
    return np.hypot(np.maximum(qx, 0), np.maximum(qy, 0)) + np.minimum(np.maximum(qx, qy), 0) - r


def sd_obox(E, N, p0, d, length, hw, r):
    """A rounded box from p0 along the direction d (unit), length long, hw either side of its middle line."""
    x = (E - p0[0]) * d[1] - (N - p0[1]) * d[0]
    y = (E - p0[0]) * d[0] + (N - p0[1]) * d[1]
    return sd_rrect(x, y, -hw, hw, 0.0, length, r)


def boss_foot(e):
    """The Boss's wall: its foot line (n) at e (a shallow arc, concave towards the chamber)."""
    return BOSS_N - (np.asarray(e, float) - BOSS_E) ** 2 / (2 * BOSS_BOW)


def sd_chamber(E, N):
    """The Boss's chamber: a round cavern, its north side squared off into the Boss's broad wall."""
    circ = sd_circle(E, N, CHAMBER_C, CHAMBER_R)
    north = sd_rrect(E, N, BOSS_E - BOSS_HALF, BOSS_E + BOSS_HALF, CHAMBER_C[1], BOSS_N + 1.0, 3.0)
    return smax(smin(circ, north, 1.5), N - boss_foot(E), 0.8)


def sd_threshold(E, N):
    p0 = (CRACK_FOOT[0] + CRACK_DIR[0] * CRACK_END, CRACK_FOOT[1] + CRACK_DIR[1] * CRACK_END)
    return sd_obox(E, N, p0, CRACK_DIR, THRESH_LEN, THRESH_HW, 0.25)


def crack_frame(e, n):
    """(across, in): a point's offset across the crack (east +) and how far in from its foot."""
    de, dn = np.asarray(e, float) - CRACK_FOOT[0], np.asarray(n, float) - CRACK_FOOT[1]
    return de * CRACK_SIDE[0] + dn * CRACK_SIDE[1], de * CRACK_DIR[0] + dn * CRACK_DIR[1]


def tunnel_hw(rel):
    """The tunnel mouth's half-width at a fraction of its height (straight sides, a round top)."""
    t = np.clip((np.asarray(rel, float) - 0.62) / 0.38, 0, 1)
    return TUNNEL_W / 2 * np.sqrt(np.clip(1 - t * t, 0, 1))


def tunnel_floor(a):
    """The tunnel's floor (u) at a distance a in from the Boss's wall."""
    return TOP_U + TUNNEL_RISE * smoothstep(0.3, 7.0, a)


def r_cav(theta):
    """The Black Water cavern's wall (no noise), its distance from LAKE_C at an angle (degrees from east)."""
    t = np.radians(theta)
    c, s = np.cos(t), np.sin(t)
    a = np.where(c > 0, CAVERN[0], CAVERN[1])
    b = np.where(s > 0, CAVERN[2], CAVERN[3])
    return 1 / np.sqrt((c / a) ** 2 + (s / b) ** 2)


def polar(e, n):
    x, y = np.asarray(e, float) - LAKE_C[0], np.asarray(n, float) - LAKE_C[1]
    return np.degrees(np.arctan2(y, x)), np.hypot(x, y)


def at(theta, rho):
    t = np.radians(theta)
    return LAKE_C[0] + rho * np.cos(t), LAKE_C[1] + rho * np.sin(t)


THETA_CRACK = float(np.degrees(np.arccos((CRACK_E - LAKE_C[0]) / CAVERN[2])))


def ramp_s(theta):
    return np.radians(RAMP_T0 - np.asarray(theta, float)) * RAMP_RMID


def ramp_h(s):
    s = np.asarray(s, float)
    run = np.clip(s - 0.6, 0, S_TOP - 0.6)
    steps = sum((s >= x).astype(float) for x in STEP_S)
    return 0.55 + SLOPE * run + STEP_H * steps


def ramp_inner(theta):
    """How far in from the cavern wall the ramp's south edge lies (wider at its top ledge)."""
    return RAMP_W + (LAND_W - RAMP_W) * smoothstep(RAMP_TL + 3, RAMP_TL - 3, theta)


def ramp_back(theta):
    """How far the ramp's top runs on under the wall (not at the crack: the crack's floor goes on from its edge)."""
    return RAMP_BACK - 0.95 * smoothstep(5.5, 3.2, np.abs(np.asarray(theta) - THETA_CRACK))


def in_ramp(e, n, full=False):
    th, rho = polar(e, n)
    rc = r_cav(th)
    back = RAMP_BACK if full else ramp_back(th)
    return (th <= RAMP_T0) & (th >= RAMP_T1) & (rho >= rc - ramp_inner(th)) & (rho <= rc + back) & (th > 0)


class Plan:
    """The ground's fields on a fine grid: where it's open (O < 0), where floor is, every point's level, the wall
    heights, how tall anything may stand (C: the camera), the rock mass's tops."""

    def __init__(self):
        self.es = np.arange(GX0, GX1 + 1e-9, RES)
        self.ns = np.arange(GY0, GY1 + 1e-9, RES)
        E, N = np.meshgrid(self.es, self.ns)
        self.E, self.N = E, N
        pool = sd_circle(E, N, POOL_C, POOL_R)
        passage = sd_chain(E, N, PASSAGE, PASSAGE_R)
        gallery = sd_rrect(E, N, *GALLERY, 2.5)
        throat = sd_chain(E, N, THROAT, THROAT_R)
        cavern = sd_qellipse(E, N, LAKE_C, CAVERN)
        chamber = sd_chamber(E, N)
        thresh = sd_threshold(E, N)
        self.spaces = np.stack([pool, passage, gallery, throat, cavern, chamber, thresh])
        high = smin(chamber, thresh, 0.8)          # (up at the crack's level: the threshold and the chamber)
        self.chamber_sd = chamber
        gpool = sd_rrect(E, N, *GPOOL, 1.2)
        water_l = sd_qellipse(E, N, LAKE_C, WATER)
        ledge = np.maximum(cavern, -water_l)
        foot = sd_circle(E, N, at(RAMP_T0, r_cav(RAMP_T0) - 1.6), RAMP_FOOT)
        rough = 0.22 * fbm(E * 0.7, N * 0.7, 6.1, 2, seed=2)
        isl = np.minimum.reduce([sd_circle(E, N, ISLET_C, ISLET_R), sd_chain(E, N, BAR, 1.0),
                                 sd_circle(E, N, L_ISLET_C, L_ISLET_R), sd_chain(E, N, L_BAR, 0.75)])
        isl = np.minimum(isl + rough * smoothstep(-0.6, 0.0, isl), isl + 0.12)   # (rough shores, never much inside)
        floor = np.minimum.reduce([isl, passage, np.maximum(gallery, -gpool), throat, np.maximum(ledge, ledge + rough),
                                   foot, high + 0.3])
        self.floor_sd = floor
        th, rho = polar(E, N)
        quiet = smoothstep(20, 40, th) * smoothstep(175, 150, th) * smoothstep(6, 9, rho)
        o = smin(smin(smin(smin(pool, passage, 1.2), gallery, 1.5), throat, 1.0), cavern, 1.5)
        o = o + (0.5 * fbm(E * 0.17, N * 0.17, 0.3, 2, seed=3) + 0.15 * fbm(E * 0.6, N * 0.6, 0.7, 2, seed=4)) * (1 - quiet)
        # The chamber's walls a little irregular (never in: the noise only widens it), but not the Boss's wall, nor
        # round the threshold's end (just behind the crack's wall).
        _, along = crack_frame(E, N)
        calm = np.maximum(smoothstep(BOSS_HALF + 1.5, BOSS_HALF, np.abs(E - BOSS_E)) * smoothstep(84.5, 87.5, N),
                          smoothstep(CRACK_END + 4.5, CRACK_END + 2.5, along))
        rough_hi = (0.3 * fbm(E * 0.17, N * 0.17, 0.3, 2, seed=3) + 0.1 * fbm(E * 0.6, N * 0.6, 0.7, 2, seed=4)) * (1 - calm)
        o = np.minimum(o, np.minimum(high, high + rough_hi))
        o = np.minimum(o, floor - 0.3)
        self.O = o
        gy, gx = np.gradient(o, RES)
        gl = np.maximum(np.hypot(gx, gy), 1e-6)
        self.gx, self.gy = gx / gl, gy / gl
        # Water: open, not floor, in a body of water (pool, lake, the gallery's pool).
        body = np.minimum.reduce([sd_circle(E, N, POOL_C, POOL_R + 1.0), cavern - 1.0, gpool])
        water = (o < 0.4) & (floor > 0) & (body < 0)
        bed = np.where(gpool < 0, -0.35, np.where(cavern < 1.5, DEEP, -1.8))
        self.ramp = in_ramp(E, N)
        self.ramp_full = in_ramp(E, N, full=True)
        th_r = np.clip(th, RAMP_T1, RAMP_T0)
        level = TOP + 0.035 * fbm(E * 0.5, N * 0.5, 1.7, 2, seed=5)
        level = np.where((high < 3.0) & (high < self.spaces[:5].min(axis=0)), TOP_U, level)   # (flat)
        self.thr_only = (thresh < 0.3) & (chamber > 0.3)
        self.high_floor = (high < 1.0) & (high < self.spaces[:5].min(axis=0))
        level = np.where(self.ramp_full, ramp_h(ramp_s(th_r)), level)
        shore = 0.47 - 2.0 * np.maximum(floor, 0) - 0.25 * smoothstep(0.0, 0.5, floor) * fbm(E * 1.1, N * 1.1, 0.9, 2, seed=7)
        level = np.where(water, np.maximum(bed, shore), level)
        self.water = water
        self.level = level
        self.floor = (o < 0.4) & ~water
        # Wall heights wanted: tall on the far sides of a space, low on its near (south) side.
        idx = np.argmin(self.spaces, axis=0)
        tall = np.array(H_TALL)[idx] + 0.7 * fbm(E * 0.12, N * 0.12, 2.1, 2, seed=6)
        tall = tall + np.where(idx == 4, 4.2 * np.exp(-((E - CRACK_E) / 5.0) ** 2) * smoothstep(64, 68, N), 0.0)
        facing = smoothstep(-0.6, -0.05, self.gy)
        low = np.array(H_LOW)[idx]
        self.hwall = low + (tall - low) * facing
        self.C = self.camera_ceiling()
        self.hc = np.minimum(self.hwall, self.C)
        d = np.maximum(o, 0)
        safe = blur(minfilter(self.C, 6), 5)        # (eroded, then blurred: never above C)
        rise = blur(self.hwall, 6) + 0.3 * np.clip(d - 1.0, 0, 10)
        lump = 0.45 * fbm(E * 0.45, N * 0.45, 4.4, 3, seed=9) * smoothstep(1.2, 2.5, d)
        self.R = np.maximum(np.minimum(rise + lump, safe) - 0.15, 0.6)

    def camera_ceiling(self):
        """How tall anything at each point may stand before it hides a floor (or the middle of the water) north of it
        from the camera (from the south, looking down)."""
        rows, cols = self.O.shape
        walk = np.where(self.floor & (self.O < -0.7) & ~self.high_floor, self.level + 0.25, np.inf)
        wet = np.where(self.water & (self.O < -2.0), 0.25, np.inf)
        # The chamber's floor (not the threshold's: it's seen through the crack and over its wall), only where it
        # limits anything (the walls round the crack, the rock between): elsewhere the cave's as it was.
        high = np.where(self.floor & (self.O < -0.7) & self.high_floor & ~self.thr_only, self.level + 0.25, np.inf)
        C = np.full(self.O.shape, np.inf)
        for vals, tan in ((walk, TAN_WALK), (wet, TAN_WATER), (high, TAN_WALK)):
            run = np.full(cols, np.inf)
            out = np.empty(self.O.shape)
            for j in range(rows - 1, -1, -1):
                run = np.minimum(run + RES * tan, vals[j])
                out[j] = run
            if vals is high:
                out = np.where(out > 16.5, np.inf, out)
            C = np.minimum(C, out)
        C = np.minimum(C, np.minimum(np.roll(C, 1, axis=1), np.roll(C, -1, axis=1)))
        return np.minimum(C, 60.0)

    def sample(self, grid, e, n):
        e = np.asarray(e, float)
        n = np.asarray(n, float)
        rows, cols = grid.shape
        fi, fj = (e - GX0) / RES, (n - GY0) / RES
        i0 = np.clip(np.floor(fi).astype(int), 0, cols - 2)
        j0 = np.clip(np.floor(fj).astype(int), 0, rows - 2)
        ti, tj = np.clip(fi - i0, 0, 1), np.clip(fj - j0, 0, 1)
        return (grid[j0, i0] * (1 - ti) * (1 - tj) + grid[j0, i0 + 1] * ti * (1 - tj)
                + grid[j0 + 1, i0] * (1 - ti) * tj + grid[j0 + 1, i0 + 1] * ti * tj)


def minfilter(G, r):
    out = G.copy()
    for _ in range(r):
        out = np.minimum.reduce([out, np.roll(out, 1, 0), np.roll(out, -1, 0), np.roll(out, 1, 1), np.roll(out, -1, 1)])
    return out


def blur(G, r):
    out = G.copy()
    for _ in range(r):
        out = (out + np.roll(out, 1, 0) + np.roll(out, -1, 0) + np.roll(out, 1, 1) + np.roll(out, -1, 1)) / 5
    return out


def contours(plan):
    """The open space's outline(s): marching squares on O, chained into loops of (e, n)."""
    V = plan.O
    ins = V < 0
    c0, c1, c2, c3 = ins[:-1, :-1], ins[:-1, 1:], ins[1:, 1:], ins[1:, :-1]
    case = c0 * 1 + c1 * 2 + c2 * 4 + c3 * 8
    js, is_ = np.nonzero((case != 0) & (case != 15))
    links = {}
    pos = {}

    def edge_point(key):
        if key not in pos:
            kind, i, j = key
            if kind == "h":
                a, b = V[j, i], V[j, i + 1]
                t = a / (a - b)
                pos[key] = (plan.es[i] + t * RES, plan.ns[j])
            else:
                a, b = V[j, i], V[j + 1, i]
                t = a / (a - b)
                pos[key] = (plan.es[i], plan.ns[j] + t * RES)
        return key

    def link(a, b):
        links.setdefault(a, []).append(b)
        links.setdefault(b, []).append(a)

    for j, i in zip(js, is_):
        s = [ins[j, i], ins[j, i + 1], ins[j + 1, i + 1], ins[j + 1, i]]
        e = [("h", i, j), ("v", i + 1, j), ("h", i, j + 1), ("v", i, j)]
        crossing = [k for k in range(4) if s[k] != s[(k + 1) % 4]]
        keys = [edge_point(e[k]) if k in crossing else None for k in range(4)]
        if len(crossing) == 2:
            link(keys[crossing[0]], keys[crossing[1]])
        elif len(crossing) == 4:
            centre = (V[j, i] + V[j, i + 1] + V[j + 1, i + 1] + V[j + 1, i]) / 4 < 0
            if centre == s[0]:
                link(keys[0], keys[1])
                link(keys[2], keys[3])
            else:
                link(keys[0], keys[3])
                link(keys[1], keys[2])
    loops, seen = [], set()
    for start in links:
        if start in seen:
            continue
        loop, prev, cur = [], None, start
        while cur not in seen:
            seen.add(cur)
            loop.append(pos[cur])
            nxt = [x for x in links[cur] if x != prev]
            if not nxt:
                break
            prev, cur = cur, nxt[0]
        loops.append(np.array(loop))
    return loops


def resample(loop, step):
    pts = np.array(loop, float)
    for _ in range(4):
        pts = 0.5 * pts + 0.25 * (np.roll(pts, 1, 0) + np.roll(pts, -1, 0))
    seg = np.linalg.norm(np.roll(pts, -1, 0) - pts, axis=1)
    L = seg.sum()
    k = max(8, int(round(L / step)))
    cum = np.concatenate([[0.0], np.cumsum(seg)])
    ext = np.vstack([pts, pts[:1]])
    t = np.arange(k) * L / k
    return np.stack([np.interp(t, cum, ext[:, 0]), np.interp(t, cum, ext[:, 1])], axis=1), L / k


# ---------------------------------------------------------------- the living walls

class Column:
    pass


def catmull(points, sub):
    pts = [points[0]] + list(points) + [points[-1]]
    out = []
    for i in range(1, len(pts) - 2):
        p0, p1, p2, p3 = (np.array(pts[i + k - 1]) for k in range(4))
        for k in range(sub):
            t = k / sub
            out.append(0.5 * ((2 * p1) + (-p0 + p2) * t + (2 * p0 - 5 * p1 + 4 * p2 - p3) * t * t
                              + (-p0 + 3 * p1 - 3 * p2 + p3) * t ** 3))
    out.append(np.array(points[-1]))
    return out


def profile(base, H, oh):
    """A wall's section (d out into the rock from its foot line, u up): its foot below the floor, its face leaning in
    towards the top, rolling over at the top and back down into the rock mass behind. Returns rows ~STEP apart:
    (d, u, zone) with zone 0 below the floor, 1 the face, 2 the top."""
    pts = [(0.3, base - 0.75), (0.03, base - 0.12)]
    k = max(3, int((H - base) / 0.08))
    for i in range(k + 1):
        t = i / k
        u = base + t * (H - base)
        pts.append((-0.12 * math.exp(-(u - base) / 0.35) - oh * (t * t * (3 - 2 * t)) ** 1.6, u))
    face_end = len(pts)
    lip = [(-oh, H), (-oh + 0.12, H + 0.16), (0.25 - oh * 0.5, H + 0.24), (0.7, H + 0.14), (1.1, H - 0.2),
           (1.35, H - 0.8), (1.45, H - 1.5)]
    pts += [tuple(p) for p in catmull(lip, 6)[1:]]
    P = np.array(pts)
    seg = np.linalg.norm(np.diff(P, axis=0), axis=1)
    cum = np.concatenate([[0], np.cumsum(seg)])
    L = cum[-1]
    n = max(4, int(round(L / STEP)))
    t = np.linspace(0, L, n + 1)
    d = np.interp(t, cum, P[:, 0])
    u = np.interp(t, cum, P[:, 1])
    zone = np.where(u < base - 0.05, 0, np.where(t <= cum[face_end - 1] + 1e-6, 1, 2))
    # The section's normal (towards the open side / up), from the tangent.
    td = np.gradient(d)
    tu = np.gradient(u)
    tl = np.maximum(np.hypot(td, tu), 1e-9)
    return d, u, zone, t / L, -tu / tl, td / tl


def wall_columns(plan):
    cols = []
    for li, loop in enumerate(contours(plan)):
        if len(loop) < 20:
            continue
        P, spacing = resample(loop, STEP)
        if spacing * len(P) < 4.0:
            continue
        Nv = np.stack([plan.sample(plan.gx, P[:, 0], P[:, 1]), plan.sample(plan.gy, P[:, 0], P[:, 1])], axis=1)
        for _ in range(6):
            Nv = 0.5 * Nv + 0.25 * (np.roll(Nv, 1, 0) + np.roll(Nv, -1, 0))
        Nv /= np.maximum(np.linalg.norm(Nv, axis=1, keepdims=True), 1e-9)
        inner = P - 0.45 * Nv
        base = plan.sample(plan.level, inner[:, 0], inner[:, 1])
        space = np.argmin(np.stack([plan.sample(s, P[:, 0], P[:, 1]) for s in plan.spaces]), axis=0)
        H = np.full(len(P), np.inf)
        for dd in (0.0, 0.4, 0.9, 1.3):
            q = P + Nv * dd
            H = np.minimum(H, plan.sample(plan.hc, q[:, 0], q[:, 1]))
        raw = H.copy()
        for _ in range(3):
            H = 0.5 * H + 0.25 * (np.roll(H, 1) + np.roll(H, -1))
        H = np.minimum(H, raw + 0.25)
        H = np.maximum(H, base + 0.75)
        # The crack's wall stands a little over the crack, whatever the chamber behind it would like (the camera).
        off = np.abs(P[:, 0] - CRACK_FOOT[0])
        crack_min = TOP_U + CRACK_H + 0.45 - 1.6 * smoothstep(0.7, 2.4, off)
        H = np.where((space == 4) & (P[:, 1] > 64) & (off < 2.4), np.maximum(H, crack_min), H)
        # The Boss's wall: smooth, hardly leaning.
        boss = smoothstep(BOSS_HALF + 0.3, BOSS_HALF - 1.2, np.abs(P[:, 0] - BOSS_E)) * smoothstep(0.45, 0.75, Nv[:, 1]) \
            * (space == 5) * smoothstep(86.0, 88.0, P[:, 1])
        ref = np.maximum(base, 0.0)
        wob = fbm(P[:, 0] * 0.4, P[:, 1] * 0.4, 5.5, 2, seed=8)
        oh = np.clip(0.065 * (H - ref), 0, 0.42) * (0.55 + 0.45 * np.clip(wob + 0.5, 0, 1))
        q = P - Nv * 0.45
        oh *= smoothstep(H - 1.2, H + 0.4, plan.sample(plan.C, q[:, 0], q[:, 1]))   # (no lean where it would hide a floor)
        oh *= 1 - 0.6 * boss
        for k in range(len(P)):
            c = Column()
            c.loop, c.k, c.s = li, k, k * spacing
            c.e, c.n = float(P[k, 0]), float(P[k, 1])
            c.N = (float(Nv[k, 0]), float(Nv[k, 1]))
            c.base, c.H, c.oh, c.space, c.ref = float(base[k]), float(H[k]), float(oh[k]), int(space[k]), float(ref[k])
            c.boss = float(boss[k])
            c.d, c.u, c.zone, c.t, c.nd, c.nu = profile(c.base, c.H, c.oh)
            cols.append(c)
    return cols


def raw_disp(e, n, u, s, rel, space):
    """How far the living wall stands out from its section (towards the open side): slow bulges, folds hanging like
    cloth, a fine grain; the gallery's more crowded."""
    big = 0.21 * fbm(e * 0.3, n * 0.3, u * 0.22, 3, seed=11)
    sway = 0.8 * fbm(e * 0.13, n * 0.13, u * 0.11, 2, seed=12)
    fold = (1 - np.abs(np.sin(np.pi * (s / 1.6 + sway)))) ** 3
    famp = 0.05 + 0.07 * np.clip(fbm(e * 0.09, n * 0.09, 0.5, 1, seed=13) + 0.4, 0, 1)
    grain = 0.025 * fbm(e * 1.3, n * 1.3, u * 1.3, 2, seed=14)
    gal = (space == 2)
    out = big + fold * famp + grain + gal * 0.06 * fbm(e * 0.8, n * 0.8, u * 0.6, 2, seed=15)
    return out * (0.45 + 0.55 * smoothstep(-0.3, 0.6, rel))


def build_walls(plan, mesh, MT, rng):
    cols = wall_columns(plan)
    # Every row of every column, flat.
    ci = np.concatenate([np.full(len(c.u), i) for i, c in enumerate(cols)])
    D = np.concatenate([c.d for c in cols])
    U = np.concatenate([c.u for c in cols])
    Z = np.concatenate([c.zone for c in cols])
    T = np.concatenate([c.t for c in cols])
    ND = np.concatenate([c.nd for c in cols])
    NU = np.concatenate([c.nu for c in cols])
    Ce = np.array([c.e for c in cols])[ci]
    Cn = np.array([c.n for c in cols])[ci]
    Ne = np.array([c.N[0] for c in cols])[ci]
    Nn = np.array([c.N[1] for c in cols])[ci]
    S = np.array([c.s for c in cols])[ci]
    REL = U - np.array([c.ref for c in cols])[ci]
    SP = np.array([c.space for c in cols])[ci]
    BE, BN = Ce + Ne * D, Cn + Nn * D      # the undisplaced points
    offsets = np.concatenate([[0], np.cumsum([len(c.u) for c in cols])])

    sites = choose_sites(cols, offsets, U, rng)
    bodies = [s for s in sites if s["kind"] == "body"]
    disp = raw_disp(BE, BN, U, S, REL, SP)
    for b in bodies:   # bodies under the sheet: shoulders and backs pressing out
        same = (ci >= b["col"] - 12) & (ci <= b["col"] + 12)
        ds = (S[same] - b["s"]) / b["ss"]
        du = (U[same] - b["u"]) / b["su"]
        disp[same] += b["amp"] * np.exp(-(ds * ds + du * du) ** 1.3)
    disp *= np.where(Z == 2, 0.35, 1.0)
    disp *= 1 - 0.8 * np.array([c.boss for c in cols])[ci]      # (the Boss's wall: smooth)
    # Flatten the wall round every place something sits in it (an eye, a face, the crack).
    for st in sites:
        if st["kind"] not in ("eye", "presence", "crack"):
            continue
        r0, r1 = st["flat"]
        near = (np.abs(ci - st["col"]) <= int(r1 / STEP) + 2) & (np.abs(U - st["u"]) < r1 + 0.3)
        c = cols[st["col"]]
        cd = float(np.interp(st["u"], c.u, c.d))
        ce, cn = c.e + c.N[0] * cd, c.n + c.N[1] * cd
        centre = raw_disp(np.array([ce]), np.array([cn]), np.array([st["u"]]), np.array([c.s]),
                          np.array([st["u"] - c.ref]), np.array([c.space]))[0] * (0.45 + 0.55 * smoothstep(-0.3, 0.6, st["u"] - c.ref))
        if st["kind"] == "crack":
            centre = 0.0
        dist = np.sqrt((BE[near] - ce) ** 2 + (BN[near] - cn) ** 2 + (U[near] - st["u"]) ** 2)
        w = smoothstep(r0, r1, dist)
        disp[near] = centre + (disp[near] - centre) * w
    # Displace along each section's normal.
    D2 = D + ND * disp
    U2 = U + NU * disp
    X = np.stack([Ce + Ne * D2, Cn + Nn * D2, U2], axis=1)
    for i, c in enumerate(cols):
        c.X = X[offsets[i]:offsets[i + 1]]
    expect = np.stack([Ne * ND, Nn * ND, NU], axis=1)   # the section's normal in the world (into the open / up)
    # Faces: zip each column to the next round its loop.
    eye_holes = {(st["col"], st["row"]) for st in sites if st["kind"] == "eye"}
    crack = next(st for st in sites if st["kind"] == "crack")
    tris = []
    for i, c in enumerate(cols):
        j = i + 1
        if j == len(cols) or cols[j].loop != c.loop:
            j = next(k for k in range(len(cols)) if cols[k].loop == c.loop)
        o = cols[j]
        for tri in zipper(c.t, o.t):
            idx = []
            hole = False
            for side, r in tri:
                col = i if side == "a" else j
                if (col, r) in eye_holes:
                    hole = True
                idx.append(offsets[col] + r)
            if hole:
                continue
            tris.append(idx)
    tris = np.array(tris)
    cen = X[tris].mean(axis=1)
    # The crack through the lake's wall (its face: its rolled top stays, over the crack's passage) and on through the
    # threshold's end wall (as wide as the passage behind the crack); the tunnel's mouth in the Boss's wall.
    tsp = SP[tris[:, 0]]
    lake_face = (tsp == 4) & (Z[tris].max(axis=1) <= 1)
    keep = ~(in_crack(cen[:, 0], cen[:, 1], cen[:, 2]) & (lake_face | (tsp != 4)))
    keep &= ~(in_crack(cen[:, 0], cen[:, 1], cen[:, 2], wide=True) & (tsp == 6))
    keep &= ~(in_tunnel(cen[:, 0], cen[:, 1], cen[:, 2]) & (tsp == 5))
    tris = tris[keep]
    cen = cen[keep]
    # Pale streaks, a face wide: down the crests of some folds (like cloth), and along veins wandering through the
    # stone (like fat in marble).
    phase = np.floor(S / 1.6 + 0.8 * fbm(BE * 0.13, BN * 0.13, U * 0.11, 2, seed=12))
    marble = fbm(BE * 0.2 + U * 0.05, BN * 0.2, U * 0.14, 3, seed=21)
    crest = phase[tris].min(axis=1) != phase[tris].max(axis=1)
    crest &= np.array([c.boss for c in cols])[ci][tris].max(axis=1) < 0.2     # (no folds on the Boss's wall)
    seam = (marble[tris].min(axis=1) < 0) & (marble[tris].max(axis=1) > 0)
    m1 = (fbm(cen[:, 0] * 0.3, cen[:, 1] * 0.3, cen[:, 2] * 0.9, 2, seed=23) > 0.22) \
        & (fbm(cen[:, 0] * 0.08, cen[:, 1] * 0.08, 0.3, 1, seed=25) > -0.1)
    m2 = fbm(cen[:, 0] * 0.2, cen[:, 1] * 0.2, cen[:, 2] * 0.3, 2, seed=24) > 0.3
    vein = (crest & m1) | (seam & m2)
    vein &= (Z[tris] == 1).all(axis=1)
    mats = [MT["vein"] if x else MT["wall"] for x in vein]
    mesh.patch(X, tris, mats, True, expect)
    print(f"[hold] living walls: {len(cols)} columns, {len(tris)} tris, veins {int(vein.sum())}")
    return cols, sites


def in_crack(e, n, u, wide=False):
    """In the crack's hole (wide: as wide as its passage's sides behind the hole)."""
    rel = (np.asarray(u) - TOP_U) / CRACK_H
    w = CRACK_W / 2 * np.power(np.clip(1 - np.clip(rel, 0, 1) ** 1.7, 0, 1), 0.7)
    if wide:
        w = w + 0.28 * (1 - 0.5 * np.clip(rel, 0, 1)) + 0.04
    x, a = crack_frame(e, n)
    return (np.abs(x) < w) & (rel > -0.02) & (rel < 1.0) & (a > -1.5) & (a < CRACK_END + 0.6)


def in_tunnel(e, n, u):
    """In the tunnel's mouth (in the Boss's wall)."""
    rel = (np.asarray(u) - TOP_U) / TUNNEL_H
    return (np.abs(np.asarray(e) - BOSS_E) < tunnel_hw(rel)) & (rel > -0.02) & (rel < 1.0) \
        & (np.abs(np.asarray(n) - BOSS_N) < 1.5)


def surface(cols, k, u, frac=0.0):
    """A point on the living wall: column k (+ frac towards the next), at height u (on its face)."""
    def one(c):
        face = c.zone == 1
        return np.array([np.interp(u, c.u[face], c.X[face][:, a]) for a in range(3)])
    p = one(cols[k])
    if frac:
        p = p + (one(cols[k + 1]) - p) * frac
    return p


def frame_at(cols, k, u):
    p = surface(cols, k, u)
    a, b = surface(cols, k - 1, u), surface(cols, k + 1, u)
    lo, hi = surface(cols, k, u - 0.25), surface(cols, k, u + 0.25)
    nrm = np.cross(b - a, hi - lo)
    inward = -np.array([cols[k].N[0], cols[k].N[1], 0.0])
    if np.dot(nrm, inward) < 0:
        nrm = -nrm
    nrm /= np.linalg.norm(nrm)
    return p, nrm


def choose_sites(cols, offsets, U, rng):
    """Where the kept press out (reliefs), where bodies swell under the stone, where eyes open (left as holes for the
    game's sockets), where the game's faces and hands may push out; and the crack."""
    sites = []
    taken = []

    def free(e, n, u, r):
        return all((e - a) ** 2 + (n - b) ** 2 + (u - c) ** 2 > (r + rr) ** 2 for a, b, c, rr in taken)

    def ok(c, need):
        if c.N[1] < -0.25 or c.H - c.ref < need:
            return False
        if abs(c.e - CRACK_E) < 3.2 and c.n > 64:
            return False
        return True

    # The crack: on the north wall, above the top ledge.
    k = min((i for i, c in enumerate(cols) if c.space == 4 and c.n > 64 and c.N[1] > 0.5),
            key=lambda i: abs(cols[i].e - CRACK_E))
    sites.append(dict(kind="crack", col=k, u=TOP_U + CRACK_H / 2, flat=(2.6, 3.6), row=-1))
    taken.append((cols[k].e, cols[k].n, TOP_U + 2, 3.0))
    # Eyes (row-aligned: the game's socket fills the hole round one vertex), and the presence spots.
    want_eyes = {"passage": 7, "gallery": 8, "lake": 7}
    want_pres = {"gallery": 7, "passage": 5}
    for kind, want, gap, flat in (("presence", want_pres, 0.85, (0.55, 0.9)), ("eye", want_eyes, 0.95, (0.62, 0.95))):
        for space, count in want.items():
            sidx = SPACES.index(space)
            cand = [i for i, c in enumerate(cols) if c.space == sidx and ok(c, 3.0) and 2 < i < len(cols) - 3]
            rng.shuffle(cand)
            got = 0
            for i in cand:
                if got >= count:
                    break
                c = cols[i]
                rows = [r for r in range(len(c.u)) if c.zone[r] == 1 and 0.9 <= c.u[r] - c.ref <= 2.5]
                if not rows:
                    continue
                r = rng.choice(rows)
                u = float(c.u[r])
                if not free(c.e, c.n, u, gap):
                    continue
                if kind == "presence":
                    u = c.ref + rng.uniform(1.1, 2.1)
                taken.append((c.e, c.n, u, gap))
                sites.append(dict(kind=kind, col=i, row=r if kind == "eye" else -1, u=u, flat=flat, space=space))
                got += 1
    # The kept, in relief: dense in the gallery, here and there elsewhere.
    want = {"gallery": 58, "passage": 11, "pool": 7, "lake": 11, "throat": 2}
    for space, count in want.items():
        sidx = SPACES.index(space)
        cand = [i for i, c in enumerate(cols) if c.space == sidx and ok(c, 1.6) and 2 < i < len(cols) - 3]
        got, tries = 0, 0
        while got < count and tries < count * 60 and cand:
            tries += 1
            i = rng.choice(cand)
            c = cols[i]
            face = rng.random() < 0.55
            size = rng.uniform(0.85, 1.35) if face else rng.uniform(0.7, 1.1)
            if space == "gallery" and rng.random() < 0.22:
                size *= 1.45
            lo, hi = c.ref + 0.35 + size * 0.5, c.H - 0.45 - size * 0.75   # (clear of the wall's rolled top)
            if hi <= lo:
                continue
            u = rng.uniform(lo, hi) if space == "gallery" else rng.uniform(lo, min(hi, c.ref + 3.4))
            if not free(c.e, c.n, u, size * 0.52):
                continue
            taken.append((c.e, c.n, u, size * 0.5))
            sites.append(dict(kind="face" if face else "hand", col=i, row=-1, u=u, size=size,
                              flat=(size * 0.8, size * 1.25), space=space))
            got += 1
            if face and space == "gallery" and rng.random() < 0.7 and u - size * 1.3 > c.ref + 0.3:   # the body under it
                sites.append(dict(kind="body", col=i, s=c.s + rng.uniform(-0.1, 0.1), u=u - size * 1.3, amp=0.12 * size,
                                  ss=size * 0.7, su=size * 0.9))
    for _ in range(30):   # bodies alone, nothing showing but their shape
        cand = [i for i, c in enumerate(cols) if c.space == 2 and c.H - c.ref > 3]
        i = rng.choice(cand)
        c = cols[i]
        sites.append(dict(kind="body", col=i, s=c.s, u=c.ref + rng.uniform(0.8, max(1.0, c.H - c.ref - 1.2)),
                          amp=rng.uniform(0.08, 0.16), ss=rng.uniform(0.3, 0.5), su=rng.uniform(0.5, 0.9)))
    chamber_sites(cols, sites, taken, free)
    return sites


def chamber_sites(cols, sites, taken, free):
    """The chamber's east and west walls: a few eyes, presence spots, reliefs of the kept (fewer than the gallery's).
    Never on the Boss's wall. (Their own dice, after everything else's: nothing else moves.)"""
    rng = random.Random(8231)

    def ok(i, need):
        c = cols[i]
        if c.space != 5 or abs(c.N[0]) < 0.55 or c.boss > 0.0 or c.n < 75.5 or c.H - c.ref < need:
            return False
        return 3 <= i < len(cols) - 3 and cols[i - 3].loop == c.loop == cols[i + 3].loop

    for kind, count, gap, flat in (("presence", 4, 0.85, (0.55, 0.9)), ("eye", 6, 0.95, (0.62, 0.95))):
        cand = [i for i in range(len(cols)) if ok(i, 3.0)]
        rng.shuffle(cand)
        got = 0
        for i in cand:
            if got >= count:
                break
            c = cols[i]
            rows = [r for r in range(len(c.u)) if c.zone[r] == 1 and 0.9 <= c.u[r] - c.ref <= 2.5]
            r = rng.choice(rows)
            u = float(c.u[r])
            if kind == "presence":
                u = c.ref + rng.uniform(1.1, 2.1)
            if not free(c.e, c.n, u, gap):
                continue
            taken.append((c.e, c.n, u, gap))
            sites.append(dict(kind=kind, col=i, row=r if kind == "eye" else -1, u=u, flat=flat, space="chamber"))
            got += 1
    cand = [i for i in range(len(cols)) if ok(i, 1.6)]
    got, tries = 0, 0
    while got < 14 and tries < 2000:
        tries += 1
        i = rng.choice(cand)
        c = cols[i]
        face = rng.random() < 0.55
        size = rng.uniform(0.85, 1.35) if face else rng.uniform(0.7, 1.1)
        if rng.random() < 0.25:
            size *= 1.4
        lo, hi = c.ref + 0.35 + size * 0.5, min(c.H - 0.45 - size * 0.75, c.ref + 6.0)
        if hi <= lo:
            continue
        u = rng.uniform(lo, hi)
        if not free(c.e, c.n, u, size * 0.52):
            continue
        taken.append((c.e, c.n, u, size * 0.5))
        sites.append(dict(kind="face" if face else "hand", col=i, row=-1, u=u, size=size, flat=(size * 0.8, size * 1.25),
                          space="chamber"))
        got += 1
        if face and rng.random() < 0.6 and u - size * 1.3 > c.ref + 0.3:   # the body under it
            sites.append(dict(kind="body", col=i, s=c.s + rng.uniform(-0.1, 0.1), u=u - size * 1.3, amp=0.12 * size,
                              ss=size * 0.7, su=size * 0.9))


# ---------------------------------------------------------------- the kept: reliefs (faces, hands) as height maps

def face_shape(X, Z, p):
    """A face's depth (out of its plane) in face units (crown to chin about 1), worn smooth; -1 off the head."""
    mo = p.get("mouth", 0.0)
    Zj = Z + 0.07 * mo * smoothstep(-0.15, -0.45, Z)
    q = (X / 0.36) ** 2 + ((Zj - 0.02) / 0.52) ** 2
    head = 0.3 * np.power(smoothstep(1.18, 0.45, q), 0.55)   # (its sides taper into the stone: no hard edge)
    mask = smoothstep(1.0, 0.7, q)
    f = np.zeros_like(X)
    br = p.get("brow", 0.0)
    gaunt = p.get("gaunt", 0.0)
    f += 0.03 * g2(X, Z, 0, 0.14 + br * 0.03, 0.26, 0.045)
    for sx in (-1, 1):
        ex = sx * 0.125
        f -= 0.075 * g2(X, Z, ex, 0.045, 0.09, 0.06)
        if p["eyes"] == "closed":
            f += 0.05 * g2(X, Z, ex, 0.035, 0.075, 0.042)
            f -= 0.012 * g2(X, Z, ex, 0.012, 0.075, 0.009)
        else:
            f -= 0.06 * g2(X, Z, ex, 0.04, 0.055, 0.045)
        f += 0.03 * (1 - gaunt) * g2(X, Z, sx * 0.17, -0.08, 0.08, 0.07)
        f -= 0.035 * gaunt * g2(X, Z, sx * 0.16, -0.18, 0.06, 0.08)
        f -= 0.018 * g2(X, Z, sx * 0.1, -0.21, 0.025, 0.07)
        f += 0.016 * g2(X, Z, sx * 0.042, -0.155, 0.025, 0.02)
    t = np.clip((0.07 - Z) / 0.22, 0, 1)
    f += (0.025 + 0.075 * t ** 1.4) * np.exp(-(X / (0.022 + 0.022 * t)) ** 2) * smoothstep(-0.18, -0.13, Z) \
        * smoothstep(0.13, 0.03, Z)
    f += 0.025 * g2(X, Z, 0, -0.14, 0.035, 0.03)
    mz = -0.262 - 0.03 * mo
    f += 0.025 * g2(X, Z, 0, -0.228, 0.085, 0.022)
    f += 0.03 * g2(X, Z, 0, mz - 0.03 - 0.07 * mo, 0.075, 0.026)
    if mo > 0:
        f -= 0.26 * mo * g2(X, Z, 0, mz - 0.035 * mo, 0.048 + 0.022 * mo, 0.03 + 0.06 * mo)
    else:
        f -= 0.018 * g2(X, Z, 0, -0.252, 0.09, 0.008)
    f += 0.035 * g2(X, Z, 0, -0.42 - 0.07 * mo, 0.09, 0.06)
    return np.where(q < 1.18, head + f * mask, -1.0)


def rot(yaw=0.0, pitch=0.0):
    """Turns a head (degrees): yaw about up (z), pitch about across (x, + looks up)."""
    a, b = math.radians(yaw), math.radians(pitch)
    Rz = np.array([[math.cos(a), -math.sin(a), 0], [math.sin(a), math.cos(a), 0], [0, 0, 1]])
    Rx = np.array([[1, 0, 0], [0, math.cos(b), -math.sin(b)], [0, math.sin(b), math.cos(b)]])
    return Rz @ Rx


def march(x, z, fn, R):
    """The front of a turned shape seen along -y: the largest y where (R^T (x, y, z)) is inside fn's head."""
    Rt = R.T
    ys = np.linspace(0.7, -0.25, 48)
    dy = ys[0] - ys[1]
    out = np.full(x.shape, -1.0)
    found = np.zeros(x.shape, bool)

    def inside(y):
        hx = Rt[0, 0] * x + Rt[0, 1] * y + Rt[0, 2] * z
        hy = Rt[1, 0] * x + Rt[1, 1] * y + Rt[1, 2] * z
        hz = Rt[2, 0] * x + Rt[2, 1] * y + Rt[2, 2] * z
        return hy <= fn(hx, hz)

    for y in ys:
        new = inside(np.full(x.shape, y)) & ~found
        out[new] = y
        found |= new
    lo, hi = out.copy(), out + dy
    for _ in range(6):
        mid = (lo + hi) / 2
        ins = inside(mid)
        lo = np.where(ins, mid, lo)
        hi = np.where(ins, hi, mid)
    return np.where(found, lo, -1.0)


def spheres_depth(x, z, S):
    best = np.full(x.shape, -1.0)
    for cx, cy, cz, r in S:
        d2 = r * r - (x - cx) ** 2 - (z - cz) ** 2
        best = np.maximum(best, np.where(d2 > 0, cy + np.sqrt(np.maximum(d2, 0)), -1.0))
    return best


def hand_shape(x, z, p):
    """A hand's depth (metres, about 0.45 long wrist to fingertip), pressing out of the stone: palm, fingers in chains of
    spheres (curled towards the viewer), thumb, the wrist going back in."""
    q = (x / 0.095) ** 2 + (z / 0.105) ** 2
    palm = np.where(q < 1, -0.005 + 0.042 * np.sqrt(np.clip(1 - q, 0, 1)), -1.0)
    S = []

    def capsule(a, b, ra, rb):
        a, b = np.array(a, float), np.array(b, float)
        n = max(3, int(np.linalg.norm(b - a) / 0.01) + 1)
        for t in np.linspace(0, 1, n):
            c = a + (b - a) * t
            S.append((c[0], c[1], c[2], ra + (rb - ra) * t))

    capsule((0, 0.0, -0.08), (0, -0.075, -0.3), 0.05, 0.056)
    spread = p.get("spread", 15.0)
    curls = p.get("curl", (0, 0, 0))
    for kx, length, fan, r0 in ((-0.068, 0.145, -1.0, 0.02), (-0.023, 0.168, -0.33, 0.021), (0.023, 0.158, 0.33, 0.02),
                                (0.066, 0.122, 1.0, 0.018)):
        a = math.radians(spread * fan)
        flat = np.array([math.sin(a), 0.0, math.cos(a)])
        pos = np.array([kx, 0.004, 0.09])
        c = 0.0
        rs = [r0, r0 * 0.9, r0 * 0.8, r0 * 0.7]
        for seg, (frac, cu) in enumerate(zip((0.45, 0.31, 0.24), curls)):
            c += math.radians(cu)
            d = math.cos(c) * flat + math.sin(c) * np.array([0, 1.0, 0])
            end = pos + d * length * frac
            capsule(pos, end, rs[seg], rs[seg + 1])
            pos = end
    ta = math.radians(p.get("thumb", 55.0))
    tc = math.radians(p.get("thumb_curl", 15.0))
    pos = np.array([0.075, 0.0, -0.045])
    flat = np.array([math.sin(ta), 0.0, math.cos(ta)])
    for frac, r0, r1 in ((0.075, 0.025, 0.022), (0.06, 0.022, 0.017)):
        d = math.cos(tc) * flat + math.sin(tc) * np.array([0, 1.0, 0])
        end = pos + d * frac
        capsule(pos, end, r0, r1)
        pos = end
        tc *= 1.6
    return np.maximum(palm, spheres_depth(x, z, S))


def polar_grid(ax, az, nr, ns):
    xs, zs, rn = [0.0], [0.0], [0.0]
    for i in range(1, nr + 1):
        t = i / nr
        for j in range(ns):
            a = 2 * math.pi * j / ns
            xs.append(ax * t * math.cos(a))
            zs.append(az * t * math.sin(a))
            rn.append(t)
    T = [(0, 1 + j, 1 + (j + 1) % ns) for j in range(ns)]
    for i in range(1, nr):
        for j in range(ns):
            a, b = 1 + (i - 1) * ns + j, 1 + (i - 1) * ns + (j + 1) % ns
            c, d = 1 + i * ns + (j + 1) % ns, 1 + i * ns + j
            T += [(a, b, c), (a, c, d)]
    return np.array(xs), np.array(zs), np.array(rn), np.array(T)


def relief(kind, p, size, nr=13, ns=36, rim=-0.1):
    """A relief of the kept on its sheet of stone: local (x across, y out, z up), metres. Returns points, triangles,
    and per-vertex how much is body (vs sheet)."""
    if kind == "face":
        ax, az = 0.56 * size, 0.72 * size
    else:
        ax, az = 0.5 * size, 0.68 * size
    x, z, r, T = polar_grid(ax, az, nr, ns)
    if kind == "face":
        R = rot(p.get("yaw", 0.0), p.get("pitch", 0.0))
        body = march(x / size, (z - 0.02 * size) / size, lambda X, Z: face_shape(X, Z, p), R) * size
        body = np.where(body > -0.5 * size, body, -1.0)
    else:
        k = size / 0.47
        ca, sa = math.cos(math.radians(p.get("roll", 0))), math.sin(math.radians(p.get("roll", 0)))
        hx, hz = (x * ca + z * sa) / k, (-x * sa + z * ca) / k
        if p.get("mirror"):
            hx = -hx
        body = hand_shape(hx, hz + 0.02, p) * k
    sheet = 0.04 * size * (1 - r * r) + rim * r ** 5
    fade = smoothstep(1.0, 0.84, r)
    body = np.where(body > -0.9, body * fade + (rim - 0.02) * (1 - fade), -1.0)
    y = smax(sheet, body, (0.045 + 0.03 * min(abs(p.get("yaw", 0.0)) / 40, 1)) * size)
    y = np.where(r > 0.97, sheet, y)
    kept = smoothstep(0.0, 0.02 * size, body - sheet)
    return np.stack([x, y, z], axis=1), T, kept, r


def face_params(rng, kind=None):
    kind = kind or rng.choice(["asleep", "asleep", "asleep", "cry", "aside", "aside", "blind"])
    if kind == "asleep":
        return dict(eyes="closed", mouth=0.0, yaw=rng.uniform(-15, 15), pitch=rng.uniform(-12, 6), gaunt=rng.random() * 0.5)
    if kind == "cry":
        return dict(eyes="hollow", mouth=rng.uniform(0.6, 1.0), brow=1.0, yaw=rng.uniform(-12, 12), pitch=rng.uniform(0, 14),
                    gaunt=rng.uniform(0.4, 1.0))
    if kind == "aside":
        return dict(eyes="closed", mouth=rng.choice([0.0, 0.0, 0.3]), yaw=rng.choice([-1, 1]) * rng.uniform(32, 50),
                    pitch=rng.uniform(-10, 8), gaunt=rng.random() * 0.6)
    return dict(eyes="hollow", mouth=rng.choice([0.0, 0.35]), yaw=rng.uniform(-20, 20), pitch=rng.uniform(-6, 10),
                gaunt=rng.uniform(0.2, 0.8))


def hand_params(rng, kind=None):
    kind = kind or rng.choice(["flat", "flat", "reach", "reach", "clutch"])
    roll = rng.uniform(-50, 50) if rng.random() < 0.8 else rng.uniform(120, 240)
    if kind == "flat":
        return dict(spread=rng.uniform(14, 28), curl=(0, 0, 0), thumb=rng.uniform(50, 70), thumb_curl=0.0, roll=roll,
                    mirror=rng.random() < 0.5)
    if kind == "reach":
        return dict(spread=rng.uniform(8, 18), curl=(rng.uniform(15, 30), rng.uniform(35, 55), rng.uniform(25, 45)),
                    thumb=40, thumb_curl=25, roll=roll, mirror=rng.random() < 0.5)
    return dict(spread=4, curl=(75, 95, 70), thumb=25, thumb_curl=50, roll=roll, mirror=rng.random() < 0.5)


def wall_at(cols, k, cf, u):
    """Points on the living wall at fractional columns cf (round the loop from column k) and heights u (arrays)."""
    out = np.zeros((len(cf), 3))
    lo = np.floor(cf).astype(int)
    t = cf - lo
    loop = cols[k].loop
    for c0 in np.unique(lo):
        sel = lo == c0
        pts = []
        for c in (c0, c0 + 1):
            c = int(np.clip(c, 0, len(cols) - 1))
            if cols[c].loop != loop:
                c = k
            col = cols[c]
            face = col.zone >= 1
            uu = col.u[face]
            X = col.X[face]
            # (on the face only: heights above its top fall back on the top's last face row)
            top = np.searchsorted(np.diff(uu) < 0, True) if np.any(np.diff(uu) < 0) else len(uu)
            uu, X = uu[:top], X[:top]
            pts.append(np.stack([np.interp(u[sel], uu, X[:, a]) for a in range(3)], axis=1))
        out[sel] = pts[0] + (pts[1] - pts[0]) * t[sel][:, None]
    return out


def place_reliefs(cols, sites, mesh, MT, rng):
    """The kept: each relief wrapped onto the wall where it sits (its rim always just behind the stone)."""
    count, tris = 0, 0
    for st in sites:
        if st["kind"] not in ("face", "hand"):
            continue
        k = st["col"]
        p, out = frame_at(cols, k, st["u"])
        step = np.linalg.norm(np.array([cols[k + 1].e - cols[k].e, cols[k + 1].n - cols[k].n]))
        up = np.array([0, 0, 1.0]) - out * out[2]
        up /= np.linalg.norm(up)
        right = np.cross(out, up)
        along = np.array([cols[k + 1].e - cols[k].e, cols[k + 1].n - cols[k].n, 0.0])
        sgn = 1.0 if np.dot(along, right) > 0 else -1.0
        params = face_params(rng) if st["kind"] == "face" else hand_params(rng)
        V, T, kept, _ = relief(st["kind"], params, st["size"])
        roll = math.radians(rng.uniform(-18, 18)) if st["kind"] == "face" else 0.0
        x = V[:, 0] * math.cos(roll) - V[:, 2] * math.sin(roll)
        z = V[:, 0] * math.sin(roll) + V[:, 2] * math.cos(roll)
        base = wall_at(cols, k, k + sgn * x / step, st["u"] + z)
        W = base + V[:, 1:2] * out
        tn = np.cross(W[T[:, 1]] - W[T[:, 0]], W[T[:, 2]] - W[T[:, 0]])
        tn /= np.maximum(np.linalg.norm(tn, axis=1, keepdims=True), 1e-12)
        ref = np.cross(V[T[:, 1]] - V[T[:, 0]], V[T[:, 2]] - V[T[:, 0]])[:, 1]
        if np.mean((tn @ out) * np.sign(ref) < -0.05) > 0.003:    # (folded round a sharp turn of the wall: leave it out)
            continue
        km = kept[T].mean(axis=1)
        ns = 36
        quad = km[ns:].reshape(-1, 2).mean(axis=1)          # (the rings' quads: both halves alike, no saw-tooth)
        km = np.concatenate([km[:ns], np.repeat(quad, 2)])
        if st["kind"] == "face":      # (a face: all of it the kept's paler stone, to where it sinks into the wall)
            km[:] = 1.0
        mats = [MT["kept"] if v > 0.5 else MT["wall"] for v in km]
        mesh.patch(W, T, mats, True, out)
        count += 1
        tris += len(T)
    print(f"[hold] the kept: {count} reliefs, {tris} tris")


# ---------------------------------------------------------------- the ground: floors, beds, the rock mass

def build_ground(plan, mesh, MT):
    step = 2   # every other node of the fine grid (0.4 m)
    E, N = plan.E[::step, ::step], plan.N[::step, ::step]
    O = plan.O[::step, ::step]
    lev = plan.level[::step, ::step]
    flo = plan.floor[::step, ::step]
    wat = plan.water[::step, ::step]
    ramp = plan.ramp[::step, ::step]
    R = plan.R[::step, ::step]
    gx, gy = plan.gx[::step, ::step], plan.gy[::step, ::step]
    d = np.maximum(O, 0)
    near = plan.sample(plan.level, E - (d + 0.35) * gx, N - (d + 0.35) * gy)
    h = np.where(O < 0, lev, np.where(O < 0.95, near, R))
    h = np.where(ramp, 0.3, h)
    # No ground in the crack's way (its passage has its own floor, to the threshold's), in the tunnel's mouth (its own
    # floor), nor over the opening at its end (open above, the cleft's walls and lips round it).
    x, a = crack_frame(E, N)
    strip = (np.abs(x) < 1.15) & (a > -1.5) & (a < CRACK_END - 0.05) & (O < 0.95)
    strip |= (np.abs(E - BOSS_E) < 1.45) & (N - BOSS_N > -0.05) & (N - BOSS_N < 1.45) & (O < 0.95)
    strip |= (np.abs(E - BOSS_E) < CLEFT_HW - 0.05) & (N - BOSS_N > TUNNEL_ROOF + 0.05)   # (its edges on grid lines)
    rows, cols = E.shape
    idx = np.arange(rows * cols).reshape(rows, cols)
    V = np.stack([E.ravel(), N.ravel(), h.ravel()], axis=1)
    a, b = idx[:-1, :-1].ravel(), idx[:-1, 1:].ravel()
    c, dd = idx[1:, 1:].ravel(), idx[1:, :-1].ravel()
    hv = h.ravel()
    far = (O.ravel() > 11.0)
    drop = (far[a] & far[b] & far[c] & far[dd])
    rr = ramp.ravel()
    drop |= rr[a] & rr[b] & rr[c] & rr[dd]
    vv = strip.ravel()
    drop |= vv[a] | vv[b] | vv[c] | vv[dd]
    a, b, c, dd = a[~drop], b[~drop], c[~drop], dd[~drop]
    diag = np.abs(hv[a] - hv[c]) <= np.abs(hv[b] - hv[dd])
    T1 = np.where(diag[:, None], np.stack([a, b, c], 1), np.stack([a, b, dd], 1))
    T2 = np.where(diag[:, None], np.stack([a, c, dd], 1), np.stack([b, c, dd], 1))
    T = np.vstack([T1, T2])
    fl = flo.ravel() & ~rr
    allfloor = fl[T].all(axis=1)
    anyfloor = fl[T].any(axis=1)
    hmean = hv[T].mean(axis=1)
    cen = V[T].mean(axis=1)
    damp = fbm(cen[:, 0] * 0.35, cen[:, 1] * 0.35, 3.3, 2, seed=31) > 0.25
    # The chamber: dark wet rock in patches along its east and west sides (no water up here).
    csd = plan.sample(plan.chamber_sd, cen[:, 0], cen[:, 1])
    wet_side = (csd < 0) & (csd > -0.75 - 0.6 * fbm(cen[:, 0] * 0.5, cen[:, 1] * 0.5, 7.7, 2, seed=33)) \
        & (np.abs(cen[:, 0] - BOSS_E) > 4.5) & (cen[:, 1] > 75.5) & (cen[:, 1] < 88.0) \
        & (fbm(cen[:, 0] * 0.3, cen[:, 1] * 0.3, 2.9, 2, seed=32) > -0.12)
    mats = []
    for af, an, hm, dp, ws in zip(allfloor, anyfloor, hmean, damp, wet_side):
        if af and ws:
            mats.append(MT["wet"])
        elif af:
            mats.append(MT["rock"])
        elif an and hm > -1.0:
            mats.append(MT["wet"])
        else:
            mats.append(MT["dark"])
    used = np.unique(T)
    remap = np.full(len(V), -1)
    remap[used] = np.arange(len(used))
    smooth = [m is MT["dark"] for m in mats]
    mesh.patch(V[used], remap[T], mats, smooth, (0, 0, 1.0))
    print(f"[hold] ground: {len(T)} tris")


# ---------------------------------------------------------------- the ramp, its top ledge, the crack

def build_ramp(cols, plan, mesh, MT):
    """A rock path round the north wall from the north ledge's west end up to the ledge before the crack: a swept
    section (its top across, its south face down to the ledge), broad steps as risers."""
    dth = math.degrees(0.3 / RAMP_RMID)
    thetas = list(np.arange(RAMP_T0, RAMP_T1, -dth)) + [RAMP_T1]
    step_th = [RAMP_T0 - math.degrees(x / RAMP_RMID) for x in STEP_S]
    thetas = [t for t in thetas if all(abs(t - x) > dth * 0.3 for x in step_th)]
    secs = []   # (theta, h, riser-before)
    for t in sorted(thetas + step_th, reverse=True):
        s = float(ramp_s(t))
        if any(abs(t - x) < 1e-6 for x in step_th):
            secs.append((t, float(ramp_h(s - 1e-4)), False))
            secs.append((t, float(ramp_h(s + 1e-4)), True))
        else:
            secs.append((t, float(ramp_h(s)), False))
    across = 6
    V, ex = [], []

    def add(p, e):
        V.append(p)
        ex.append(e)
        return len(V) - 1

    tops, faces = [], []
    for t, h, _ in secs:
        rc = float(r_cav(t))
        rin = rc - float(ramp_inner(t)) + 0.08 * float(noise3(t * 0.25, 1.5, 0.5, seed=41))
        rout = rc + float(ramp_back(t))
        tr = math.radians(t)
        radial = (math.cos(tr), math.sin(tr), 0.0)
        row = []
        for k in range(across + 1):
            e, n = at(t, rin + (rout - rin) * k / across)
            row.append(add((float(e), float(n), h), (0.0, 0.0, 1.0)))
        tops.append(row)
        col = []
        m = max(1, int(math.ceil((h - 0.25) / 0.35)))
        for k in range(m + 1):
            u = h - (h - 0.25) * k / m
            bulge = 0.12 * float(fbm(t * 0.3, u * 0.7, 2.2, 2, seed=42)) if 0 < k < m else 0.0
            e, n = at(t, rin - bulge)
            col.append(add((float(e), float(n), u), (-radial[0], -radial[1], 0.0)))
        faces.append(col)
    T, mats, fex = [], [], []
    for i in range(len(secs) - 1):
        riser = secs[i + 1][2]
        tr = math.radians(secs[i][0])
        back = (-math.sin(tr), math.cos(tr), 0.0)    # towards the ramp's foot (down the slope)
        for k in range(across):
            a, b, c, d = tops[i][k], tops[i][k + 1], tops[i + 1][k + 1], tops[i + 1][k]
            T += [(a, b, c), (a, c, d)]
            mats += [MT["wet"] if riser else MT["rock"]] * 2
            fex += [back if riser else (0.0, 0.0, 1.0)] * 2
        if riser:
            continue
        ta = np.linspace(0, 1, len(faces[i]))
        tb = np.linspace(0, 1, len(faces[i + 1]))
        for tri in zipper(ta, tb):
            T.append(tuple(faces[i][r] if side == "a" else faces[i + 1][r] for side, r in tri))
            mats.append(MT["wet"])
            fex.append((-math.cos(tr), -math.sin(tr), 0.0))
    # The ends: the foot, and the top ledge's east end (a cliff down to the ledge).
    for row, sign in ((0, 1), (len(secs) - 1, -1)):
        tr = math.radians(secs[row][0])
        tang = (-sign * math.sin(tr), sign * math.cos(tr), 0.0)
        bottom = [add((V[q][0], V[q][1], 0.25), tang) for q in tops[row]]
        for k in range(across):
            a, b, c, d = tops[row][k], tops[row][k + 1], bottom[k + 1], bottom[k]
            T += [(a, b, c), (a, c, d)]
            mats += [MT["wet"]] * 2
            fex += [tang] * 2
    mesh.patch(np.array(V, float), np.array(T), mats, False, np.array(fex), per_face=True)
    print(f"[hold] ramp: {len(T)} tris; foot at theta {RAMP_T0}, top ledge (u {TOP_U}) from theta {RAMP_TL} to {RAMP_T1}")


def build_crack(cols, sites, plan, mesh, MT):
    """The crack's passage through the lake's wall: its sides follow the hole in the living wall (a little wider, just
    behind it), its floor goes on from the top ledge's edge into the threshold's; it comes out through the threshold's
    end wall (lower there: the lake wall's rolled top and a lid of rock go over it)."""
    st = next(s for s in sites if s["kind"] == "crack")
    k = st["col"]
    c = cols[k]
    nrm = np.array([CRACK_DIR[0], CRACK_DIR[1], 0.0])     # into the rock
    side = np.array([CRACK_SIDE[0], CRACK_SIDE[1], 0.0])  # across (east)
    foot = np.array([CRACK_FOOT[0], CRACK_FOOT[1], 0.0])
    margin = 0.28
    us = list(np.linspace(TOP_U - 0.05, TOP_U + CRACK_H + 0.12, 16))
    half = []
    for u in us:
        rel = (u - TOP_U) / CRACK_H
        w = CRACK_W / 2 * max(0.0, 1 - min(max(rel, 0), 1) ** 1.7) ** 0.7 + margin * (1 - 0.5 * max(rel, 0))
        half.append(w)
    outline = [(-w, u) for w, u in zip(half, us)] + [(w, u) for w, u in reversed(list(zip(half, us))[:-1])]
    outline[len(us) - 1] = (0.0, us[-1])
    lean = {round(u, 5): float(np.dot(surface(cols, k, u) - foot, nrm)) for u in us}
    end = CRACK_END + 0.15
    depths = [0.0, 0.5, 1.0, 1.45, end]
    knee, apex_end = TOP_U + 1.3, TOP_U + 2.4      # (behind the wall its top comes down: under the lid at its end)

    def point(x, u, dpt):
        jag = 0.06 * float(noise3(x * 3 + dpt, u * 2.5, 0.0, seed=51)) if 0 < dpt < end else 0.0
        p = foot + side * (x * (1 + 0.04 * dpt) + jag) + nrm * (lean[round(u, 5)] + dpt + 0.06)
        apex = us[-1] + (apex_end - us[-1]) * float(smoothstep(1.0, end, dpt))
        p[2] = u if u <= knee else knee + (u - knee) * (apex - knee) / (us[-1] - knee)
        return p

    V, rings = [], []
    for dpt in depths:
        rings.append([len(V) + j for j in range(len(outline))])
        V += [point(x, u, dpt) for x, u in outline]
    T = []
    for a, b in zip(rings, rings[1:]):
        for j in range(len(a) - 1):
            T += [(a[j], a[j + 1], b[j + 1]), (a[j], b[j + 1], b[j])]
    V = np.array(V)
    T = np.array(T)
    # Facing in: towards the crack's middle line (and down from its roof).
    cen = V[T].mean(axis=1)
    lat = ((cen - foot) * side).sum(axis=1)
    want = -np.outer(np.sign(lat), side) + np.outer(cen[:, 2] > knee + 0.8, [0, 0, -1.0])
    mesh.patch(V, T, MT["wet"], False, want, per_face=True)
    # The floor: from the top ledge's back edge on through, then a little under the threshold's floor (no seam to see).
    w0 = CRACK_W / 2 + margin + 0.05
    xs = np.linspace(-1.0, 1.0, 5)
    FV = []
    for x in xs * w0:
        pe = (foot + side * x)[0]
        th = float(np.degrees(np.arccos(np.clip((pe - LAKE_C[0]) / CAVERN[2], -1, 1))))
        e, n = at(th, float(r_cav(th)) + float(ramp_back(th)))
        FV.append((float(e), float(n), TOP_U))
    rows = [(0.5, w0 * 1.02, TOP_U), (1.0, w0 * 1.04, TOP_U), (1.45, w0 * 1.06, TOP_U), (CRACK_END - 0.3, w0 * 1.07, TOP_U),
            (end, 1.75, TOP_U - 0.03), (CRACK_END + 0.55, 1.75, TOP_U - 0.12)]
    for dpt, hw, u in rows:
        for x in xs * hw:
            p = foot + side * x + nrm * (dpt + 0.06)
            FV.append((p[0], p[1], u))
    nx = len(xs)
    FT = []
    for r in range(len(rows)):
        for j in range(nx - 1):
            a, b = r * nx + j, r * nx + j + 1
            FT += [(a, b, b + nx), (a, b + nx, a + nx)]
    mesh.patch(np.array(FV), np.array(FT), MT["rock"], False, (0, 0, 1.0))
    # The lid: rock over the passage's last stretch, between the lake wall's rolled top and the threshold.
    LV, LT = [], []
    ls = np.linspace(-1.6, 1.6, 9)
    ds = np.linspace(1.25, CRACK_END + 0.2, 4)
    for dd in ds:
        for x in ls:
            p = foot + side * x + nrm * dd
            LV.append((p[0], p[1], apex_end + 0.1 + 0.04 * float(noise3(x * 2, dd * 2, 0.3, seed=52))))
    nl = len(ls)
    for r in range(len(ds) - 1):
        for j in range(nl - 1):
            a, b = r * nl + j, r * nl + j + 1
            LT += [(a, b, b + nl), (a, b + nl, a + nl)]
    mesh.patch(np.array(LV), np.array(LT), MT["dark"], False, (0, 0, 1.0))
    mid = foot + nrm * lean[round(us[len(us) // 2], 5)]
    print(f"[hold] crack: middle ({mid[0]:.2f}, {mid[1]:.2f}, {TOP_U + CRACK_H / 2:.2f}), bottom u {TOP_U}, "
          f"{CRACK_W} m wide, {CRACK_H} m tall, facing ({-nrm[0]:.2f}, {-nrm[1]:.2f}); runs {CRACK_END} m back "
          f"(col {c.e:.2f}, {c.n:.2f})")
    p0 = foot + nrm * CRACK_END
    p1 = p0 + nrm * THRESH_LEN
    print(f"[hold] threshold: from ({p0[0]:.2f}, {p0[1]:.2f}) to ({p1[0]:.2f}, {p1[1]:.2f}) (into the chamber), "
          f"{2 * THRESH_HW} m wide, floor u {TOP_U}")


def build_tunnel(cols, plan, mesh, MT):
    """The way out: the tunnel from the Boss's wall's foot north under the rock (sides, a round roof), its floor
    rising; where its roof ends, the opening: a cleft open above and on to the north (rock walls up to the rock mass's
    top, a lip round it)."""
    k = min((i for i, c in enumerate(cols) if c.space == 5 and c.N[1] > 0.6), key=lambda i: abs(cols[i].e - BOSS_E))
    rels = np.linspace(0, 1, 21)
    W = tunnel_hw(rels) + 0.12 * (1 - 0.5 * rels)
    us = TOP_U - 0.05 + rels * (TUNNEL_H + 0.17)
    W[-1] = 0.0
    lean = {i: float(surface(cols, k, us[i])[1] - boss_foot(cols[k].e)) for i in range(len(us))}
    outline = [(-W[i], i) for i in range(len(us))] + [(W[i], i) for i in reversed(range(len(us) - 1))]
    depths = [0.0, 0.6, 1.4, 2.4, 3.4, 4.4, TUNNEL_ROOF]

    def point(x, i, a):
        jag = 0.07 * float(noise3(x * 2.5 + a, us[i] * 2.0, 1.3, seed=71)) if 0 < a else 0.0
        e = BOSS_E + x + jag
        return np.array([e, float(boss_foot(e)) + lean[i] + a + 0.06, us[i] - TOP_U + float(tunnel_floor(a))])

    V, rings = [], []
    for a in depths:
        rings.append([len(V) + j for j in range(len(outline))])
        V += [point(x, i, a) for x, i in outline]
    T = []
    for ra, rb in zip(rings, rings[1:]):
        for j in range(len(ra) - 1):
            T += [(ra[j], ra[j + 1], rb[j + 1]), (ra[j], rb[j + 1], rb[j])]
    V = np.array(V)
    T = np.array(T)
    cen = V[T].mean(axis=1)
    lat = cen[:, 0] - BOSS_E
    want = np.zeros((len(T), 3))
    want[:, 0] = -np.sign(lat)
    want[np.abs(lat) < 0.5] = (0, 0, -1.0)
    mesh.patch(V, T, MT["wet"], False, want, per_face=True)
    # Where it ends: the opening, a cleft up through the rock mass, on north until the rock mass ends. (Its edges at
    # the top are the ground's, on its grid lines: the walls meet them.)
    a_end = TUNNEL_ROOF + 1.0
    while a_end < GY1 - BOSS_N - 1.0 and max(plan.sample(plan.O, BOSS_E + sx * CLEFT_HW, BOSS_N + a_end)
                                             for sx in (-1, 1)) < 11.4:
        a_end += 0.4

    def top(e, n):
        return float(plan.sample(plan.R, e, n)) + 0.05

    al = np.arange(TUNNEL_ROOF, a_end + 1e-6, 0.4)
    for sx in (-1, 1):
        CV, CT = [], []
        nu = 14
        for a in al:
            e0 = BOSS_E + sx * CLEFT_HW
            t = top(e0, BOSS_N + a)
            for j in range(nu + 1):
                f = j / nu
                u = float(tunnel_floor(a)) - 0.4 + f * (t - float(tunnel_floor(a)) + 0.4)
                jag = (0.1 + 0.08 * float(noise3(a * 0.9, u * 0.8, sx * 3.1, seed=72))) * math.sin(math.pi * f) ** 0.5
                CV.append((e0 - sx * jag, BOSS_N + a, u))
        for r in range(len(al) - 1):
            for j in range(nu):
                p0, p1 = r * (nu + 1) + j, (r + 1) * (nu + 1) + j
                CT += [(p0, p1, p1 + 1), (p0, p1 + 1, p0 + 1)]
        mesh.patch(np.array(CV), np.array(CT), MT["wet"], False, (-sx, 0.0, 0.0))
    # Over the tunnel's end: the rock face from its roof up to the top (facing the cleft).
    xs = np.linspace(-CLEFT_HW, CLEFT_HW, 8)
    tops_w = W[::-1]
    EV, ET = [], []
    nu = 10
    for x in xs:
        ub = TOP_U - 0.4 if abs(x) >= W[0] else float(np.interp(abs(x), tops_w, us[::-1]))
        ub += float(tunnel_floor(TUNNEL_ROOF)) - TOP_U - 0.03
        e = BOSS_E + x
        n = BOSS_N + TUNNEL_ROOF
        t = top(e, n)
        for j in range(nu + 1):
            EV.append((e, n, ub + (t - ub) * j / nu))
    for r in range(len(xs) - 1):
        for j in range(nu):
            p0, p1 = r * (nu + 1) + j, (r + 1) * (nu + 1) + j
            ET += [(p0, p1, p1 + 1), (p0, p1 + 1, p0 + 1)]
    mesh.patch(np.array(EV), np.array(ET), MT["wet"], False, (0.0, 1.0, 0.0))
    # The floor: from just in front of the wall (a little under the chamber's floor), up the tunnel, through the cleft
    # to its end, then a broken edge down.
    rows = [(-0.45, 1.9, TOP_U - 0.15), (0.05, 1.9, TOP_U)]
    for a in depths[1:] + list(al[1:]):
        rows.append((a, (W[0] + 0.05) if a <= TUNNEL_ROOF else CLEFT_HW + 0.05, float(tunnel_floor(a))))
    rows.append((a_end + 0.05, CLEFT_HW + 0.05, float(tunnel_floor(a_end)) - 1.6))
    fx = np.linspace(-1, 1, 9)
    FV = []
    for a, hw, u in rows:
        for x in fx * hw:
            e = BOSS_E + x
            FV.append((e, float(boss_foot(e)) + lean[0] + a if a < 0.6 else BOSS_N + a, u))
    FT = []
    nf = len(fx)
    for r in range(len(rows) - 1):
        for j in range(nf - 1):
            p0 = r * nf + j
            FT += [(p0, p0 + 1, p0 + 1 + nf), (p0, p0 + 1 + nf, p0 + nf)]
    fm = [MT["wet"] if r == len(rows) - 2 else MT["rock"] for r in range(len(rows) - 1) for _ in range(2 * (nf - 1))]
    ex = [(0, 1.0, 0.2) if r == len(rows) - 2 else (0, 0, 1.0) for r in range(len(rows) - 1) for _ in range(2 * (nf - 1))]
    mesh.patch(np.array(FV), np.array(FT), fm, False, np.array(ex, float), per_face=True)
    mouth_n = float(surface(cols, k, TOP_U + 1.5)[1])
    print(f"[hold] Boss's wall: foot line n = {BOSS_N} - (e - {BOSS_E})^2 / {2 * BOSS_BOW:.0f}, e {BOSS_E - BOSS_HALF:.1f} to "
          f"{BOSS_E + BOSS_HALF:.1f}, facing south; its face at e {BOSS_E}: n {float(surface(cols, k, TOP_U + 0.5)[1]):.2f} "
          f"(u {TOP_U + 0.5}), {float(surface(cols, k, TOP_U + 4.0)[1]):.2f} (u {TOP_U + 4.0}), "
          f"{float(surface(cols, k, TOP_U + 7.0)[1]):.2f} (u {TOP_U + 7.0}); top u {cols[k].H:.2f}")
    print(f"[hold] tunnel: mouth centre ({BOSS_E}, {mouth_n:.2f}), floor u {TOP_U}, {TUNNEL_W} m wide, {TUNNEL_H} m tall; north "
          f"to n {BOSS_N + TUNNEL_ROOF:.1f} (floor u {float(tunnel_floor(TUNNEL_ROOF)):.2f}), then the opening (open above "
          f"and north): {2 * CLEFT_HW} m wide, n {BOSS_N + TUNNEL_ROOF:.1f} to {BOSS_N + a_end:.1f}, floor u "
          f"{float(tunnel_floor(a_end)):.2f}, walls to u {top(BOSS_E, BOSS_N + TUNNEL_ROOF):.1f}")


def build_rocks(plan, mesh, MT, rng):
    """Rocks in the black water: low boulders by the shores and walls, spires (stalagmites) rising near the walls."""
    ws = np.argwhere(plan.water)
    placed = []
    V, T, ex = [], [], []
    counts = {"boulder": 0, "spire": 0}
    for kind, want in (("boulder", 14), ("spire", 8)):
        tries = 0
        while counts[kind] < want and tries < 4000:
            tries += 1
            j, i = ws[rng.randrange(len(ws))]
            e, n = float(plan.es[i]) + rng.uniform(-0.1, 0.1), float(plan.ns[j]) + rng.uniform(-0.1, 0.1)
            fs = float(plan.sample(plan.floor_sd, e, n))
            o = float(plan.sample(plan.O, e, n))
            if fs < 0.9:
                continue
            if plan.sample(plan.level, e, n) > -1.0:      # (not in the gallery's shallow pool)
                continue
            if kind == "spire" and not (-2.6 < o < -0.6):
                continue
            if kind == "boulder" and not (-3.0 < o < -0.5):
                continue
            r = rng.uniform(0.35, 0.95) if kind == "boulder" else rng.uniform(0.55, 0.95)
            if kind == "boulder":
                top = rng.uniform(0.05, 0.45)
            else:
                top = rng.uniform(1.2, 3.0)
            if top > float(plan.sample(plan.C, e, n)) - 0.2:
                continue
            if any((e - a) ** 2 + (n - b) ** 2 < (r + rr + 0.4) ** 2 for a, b, rr in placed):
                continue
            placed.append((e, n, r))
            counts[kind] += 1
            base = len(V)
            if kind == "boulder":
                rings, seg = 5, 9
                for k in range(rings + 1):
                    a = math.radians(-60 + 150 * k / rings)          # from under the water up over the top
                    for q in range(seg):
                        b = 2 * math.pi * q / seg + k * 0.3
                        rad = r * math.cos(a) * (1 + 0.3 * float(noise3(e + 1.5 * math.cos(b), n + 1.5 * math.sin(b), k * 0.9, seed=61)))
                        V.append((e + rad * math.cos(b), n + rad * math.sin(b) * 0.85, top - 0.55 * r + 0.55 * r * math.sin(a) * 1.1
                                  if k < rings else top))
                        ex.append((math.cos(b), math.sin(b), 0.6 * math.sin(a)))
            else:
                rings, seg = 6, 8
                for k in range(rings + 1):
                    f = k / rings
                    u = -1.8 + (top + 1.8) * f
                    rad = r * (1 - 0.85 * f ** 1.4) + 0.03
                    for q in range(seg):
                        b = 2 * math.pi * q / seg + k * 0.35
                        w = 1 + 0.2 * float(noise3(e * 3 + q, n * 3, u, seed=62))
                        V.append((e + rad * w * math.cos(b) + 0.15 * f * f, n + rad * w * math.sin(b), u))
                        ex.append((math.cos(b), math.sin(b), 0.2))
            for k in range(rings):
                for q in range(seg):
                    a0, b0 = base + k * seg + q, base + k * seg + (q + 1) % seg
                    c0, d0 = base + (k + 1) * seg + (q + 1) % seg, base + (k + 1) * seg + q
                    T += [(a0, b0, c0), (a0, c0, d0)]
            cap = len(V)
            V.append((e, n, top + (0.05 if kind == "boulder" else 0.15)))
            ex.append((0, 0, 1.0))
            for q in range(seg):
                T.append((base + rings * seg + q, base + rings * seg + (q + 1) % seg, cap))
    if V:
        mesh.patch(np.array(V), np.array(T), MT["wet"], False, np.array(ex))
    print(f"[hold] rocks in the water: {counts['boulder']} boulders, {counts['spire']} spires")


# ---------------------------------------------------------------- stalactites

def build_stalactites(cols, plan, mesh, MT, rng):
    """Stalactites hanging from the walls' tops (never over the middle of a floor, never in the camera's way)."""
    count = 0
    V, T = [], []
    last = -99.0
    seg = 6
    for i, c in enumerate(cols):
        if c.space >= 5:      # (the chamber's: below, on their own dice)
            continue
        if c.H - c.ref < 4.2 or c.N[1] < -0.3 or c.s - last < rng.uniform(0.9, 2.6):
            continue
        if abs(c.e - CRACK_E) < 2.5 and c.n > 64:
            continue
        if rng.random() < 0.45:
            continue
        top = c.H - 0.05
        length = rng.uniform(0.6, min(2.6, c.H - c.ref - 2.7))
        if length < 0.5:
            continue
        r = rng.uniform(0.09, 0.22) * (0.6 + 0.25 * length)
        dd = -c.oh * rng.uniform(0.3, 0.75) + 0.12
        e, n = c.e + c.N[0] * dd, c.n + c.N[1] * dd
        if plan.sample(plan.C, e, n) < top:
            continue
        last = c.s
        base = len(V)
        sway = (rng.uniform(-0.06, 0.06), rng.uniform(-0.06, 0.06))
        for k in range(3):
            frac = k / 3
            rad = r * (1 - frac) ** 1.3
            for j in range(seg):
                a = 2 * math.pi * j / seg + k * 0.4
                V.append((e + rad * math.cos(a) + sway[0] * frac, n + rad * math.sin(a) + sway[1] * frac,
                          top - length * frac * 0.85))
        tip = len(V)
        V.append((e + sway[0], n + sway[1], top - length))
        for k in range(2):
            for j in range(seg):
                a, b = base + k * seg + j, base + k * seg + (j + 1) % seg
                c2, d2 = base + (k + 1) * seg + (j + 1) % seg, base + (k + 1) * seg + j
                T += [(a, b, c2), (a, c2, d2)]
        for j in range(seg):
            T.append((base + 2 * seg + j, base + 2 * seg + (j + 1) % seg, tip))
        count += 1
    # The chamber's east and west walls (never the Boss's wall).
    rng = random.Random(9173)
    last = -99.0
    for i, c in enumerate(cols):
        if c.space != 5 or abs(c.N[0]) < 0.55 or c.boss > 0 or c.H - c.ref < 4.2 or c.s - last < rng.uniform(1.2, 3.0):
            continue
        if rng.random() < 0.4:
            continue
        top = c.H - 0.05
        length = rng.uniform(0.6, min(2.4, c.H - c.ref - 2.7))
        r = rng.uniform(0.09, 0.22) * (0.6 + 0.25 * length)
        dd = -c.oh * rng.uniform(0.3, 0.75) + 0.12
        e, n = c.e + c.N[0] * dd, c.n + c.N[1] * dd
        if plan.sample(plan.C, e, n) < top:
            continue
        last = c.s
        base = len(V)
        sway = (rng.uniform(-0.06, 0.06), rng.uniform(-0.06, 0.06))
        for k in range(3):
            frac = k / 3
            rad = r * (1 - frac) ** 1.3
            for j in range(seg):
                a = 2 * math.pi * j / seg + k * 0.4
                V.append((e + rad * math.cos(a) + sway[0] * frac, n + rad * math.sin(a) + sway[1] * frac,
                          top - length * frac * 0.85))
        tip = len(V)
        V.append((e + sway[0], n + sway[1], top - length))
        for k in range(2):
            for j in range(seg):
                a, b = base + k * seg + j, base + k * seg + (j + 1) % seg
                c2, d2 = base + (k + 1) * seg + (j + 1) % seg, base + (k + 1) * seg + j
                T += [(a, b, c2), (a, c2, d2)]
        for j in range(seg):
            T.append((base + 2 * seg + j, base + 2 * seg + (j + 1) % seg, tip))
        count += 1
    if V:
        Vv = np.array(V)
        ex = np.zeros_like(Vv)
        per = 3 * seg + 1
        for g in range(len(Vv) // per):
            b0 = g * per
            ax = Vv[b0:b0 + seg].mean(axis=0)
            ex[b0:b0 + per, :2] = Vv[b0:b0 + per, :2] - ax[:2]
            ex[b0 + per - 1] = (0, 0, -1.0)
        mesh.patch(Vv, np.array(T), MT["dark"], False, ex)
    print(f"[hold] stalactites: {count}")


# ---------------------------------------------------------------- the variants: the kept, the eyes

def solid_relief(kind, p, size, MT):
    """A relief on a short stone back (for the game to push out of a wall): out is game north (Blender -Y); origin at
    the centre of the back."""
    ns = 60
    V, T, kept, r = relief(kind, p, size, nr=22, ns=ns, rim=0.0)
    back = 0.14
    rim0 = len(V) - ns
    Vb = V[rim0:].copy()
    Vb[:, 1] = -back
    allV = np.vstack([V, Vb, [[0.0, -back, 0.0]]])
    b0, cc = len(V), len(V) + ns
    T2 = []
    for j in range(ns):
        a, b = rim0 + j, rim0 + (j + 1) % ns
        T2 += [(a, b, b0 + (j + 1) % ns), (a, b0 + (j + 1) % ns, b0 + j), (b0 + j, b0 + (j + 1) % ns, cc)]
    allT = np.vstack([T, np.array(T2)])
    allV[:, 1] += back   # the back's centre at the origin
    mesh = Mesh()
    mesh.patch(allV, allT, MT["kept"], True, allV - np.array([0.0, back * 0.4, 0.0]))
    ob = mesh.build("Hold_" + kind, recalc=True)
    span = allV.max(axis=0) - allV.min(axis=0)
    print(f"[hold] {kind} {p}: {span[0]:.2f} wide, {span[2]:.2f} tall, {allV[:, 1].max():.2f} out from its back; "
          f"{mesh.tris()} tris")
    return ob


def revolve(profile_pts, seg, scale=(1, 1, 1)):
    """Revolve (rho, y) points about the y axis (out); returns V, T (rings joined, poles closed where rho = 0)."""
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
                V.append((rho * math.cos(a) * scale[0], y * scale[1], rho * math.sin(a) * scale[2]))
            rings.append(ring)
    T = []
    for a, b in zip(rings, rings[1:]):
        if len(a) == 1 and len(b) == 1:
            continue
        if len(a) == 1:
            T += [(a[0], b[j], b[(j + 1) % seg]) for j in range(seg)]
        elif len(b) == 1:
            T += [(a[j], a[(j + 1) % seg], b[0]) for j in range(seg)]
        else:
            for j in range(seg):
                T += [(a[j], a[(j + 1) % seg], b[(j + 1) % seg]), (a[j], b[(j + 1) % seg], b[j])]
    return np.array(V, float), np.array(T)


FACE = 0.46            # the face variants' size (crown to chin about this; their stone about 0.65 tall)
EYE_R = 0.17
LID_IN, LID_OUT = 0.183, 0.2
LID_OPEN_TOP, LID_OPEN_BOTTOM = 68.0, 52.0


def eye_socket(MT):
    """Stone lips round a hollow: the eye's cup behind, the lips curling in over it. Local y is out (game north)."""
    pts = [(0.0, -0.255), (0.1, -0.235), (0.18, -0.18), (0.235, -0.09), (0.25, 0.0), (0.24, 0.07), (0.215, 0.12),
           (0.185, 0.165), (0.16, 0.2), (0.152, 0.225), (0.165, 0.25), (0.2, 0.265), (0.255, 0.255), (0.3, 0.215),
           (0.325, 0.14), (0.33, 0.05), (0.31, -0.05), (0.26, -0.17), (0.17, -0.27), (0.0, -0.31)]
    seg = 40
    V, T = revolve(pts, seg, scale=(1.12, 1.0, 0.95))
    ring_of = []
    for i, (r, y) in enumerate(pts):
        ring_of += [i] * (1 if r < 1e-6 else seg)
    ring_of = np.array(ring_of)
    inner = ring_of[T].mean(axis=1) < 10
    mats = [MT["kept"] if x else MT["wall"] for x in inner]
    mesh = Mesh()
    mesh.patch(V, T, mats, True, V)
    ob = mesh.build("Hold_eye_socket", recalc=True)
    print(f"[hold] eye_socket: {np.ptp(V[:, 0]):.2f} wide, {np.ptp(V[:, 2]):.2f} tall, lips {V[:, 1].max():.2f} out from "
          f"the eye's centre, back {-V[:, 1].min():.2f} behind; opening {2 * 0.152 * 1.12:.2f} x {2 * 0.152 * 0.95:.2f}; "
          f"{mesh.tris()} tris")
    return ob


def eye_ball(MT):
    seg = 40
    angles = [0, 4, 8, 10, 14, 19, 24, 30, 40, 55, 70, 90, 110, 130, 150, 170, 180]
    pts = []
    for a in angles:
        r = EYE_R - (0.004 * (1 - a / 10) if a <= 10 else 0.0)   # the pupil a little sunk
        ar = math.radians(a)
        pts.append((r * math.sin(ar), r * math.cos(ar)))
    V, T = revolve(pts, seg)
    ang = np.degrees(np.arctan2(np.hypot(V[:, 0], V[:, 2]), V[:, 1]))
    tri_ang = ang[T].max(axis=1)
    mats = [MT["pupil"] if a <= 10.01 else MT["iris"] if a <= 24.01 else MT["white"] for a in tri_ang]
    mesh = Mesh()
    mesh.patch(V, T, mats, True, V)
    print(f"[hold] eye_ball: {2 * EYE_R:.2f} across, pupil to 10 deg, iris to 24 deg off its front; {mesh.tris()} tris")
    return mesh.build("Hold_eye_ball", recalc=True)


def eye_lid(MT, top):
    """A lid: a thick shell round the ball, from its margin (level with the eye's middle, in front) up over the top (or
    down under the bottom) towards the back; modelled closed."""
    sgn = 1 if top else -1
    ps = np.linspace(-78, 78, 25)          # across (degrees from the middle)
    fs = np.linspace(1.2, 122, 22)         # from the margin round (degrees)
    V = []

    def pt(psi, phi, r):
        a, b = math.radians(psi), math.radians(phi) * sgn
        return (r * math.sin(a), r * math.cos(a) * math.cos(b), r * math.cos(a) * math.sin(b))

    grid = {}
    for layer, r in (("o", LID_OUT), ("i", LID_IN)):
        for i, psi in enumerate(ps):
            for j, phi in enumerate(fs):
                grid[(layer, i, j)] = len(V)
                V.append(pt(psi, phi, r + (0.005 if layer == "o" and j == 0 else 0.0)))
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
    T = []
    for a, b, c, d in quads:
        T += [(a, b, c), (a, c, d)]
    Vv = np.array(V)
    mesh = Mesh()
    mesh.patch(Vv, np.array(T), MT["kept"], True, Vv)
    name = "Hold_eye_lid_top" if top else "Hold_eye_lid_bottom"
    print(f"[hold] {name}: shell r {LID_IN}-{LID_OUT}, closed (margin at the eye's middle, in front); opens by "
          f"{LID_OPEN_TOP if top else LID_OPEN_BOTTOM} deg about the eye's left-right axis, margin {'up' if top else 'down'}")
    return mesh.build(name, recalc=True)


# ---------------------------------------------------------------- the dead lantern (unchanged)

class Acc:
    """Everything goes into one mesh (thousands of pieces: faster than an object each)."""

    def __init__(self):
        self.verts, self.faces, self.fmat, self.mats, self.index = [], [], [], [], {}

    def mi(self, material):
        if material.name not in self.index:
            self.index[material.name] = len(self.mats)
            self.mats.append(material)
        return self.index[material.name]

    def stamp(self, tmpl, material, matrix):
        verts, faces = tmpl
        base = len(self.verts)
        self.verts.extend(tuple(matrix @ v) for v in verts)
        k = self.mi(material)
        for f in faces:
            self.faces.append(tuple(base + i for i in f))
            self.fmat.append(k)

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


# ---------------------------------------------------------------- build

def report_spots(cols, sites):
    for kind in ("eye", "presence"):
        for st in sites:
            if st["kind"] != kind:
                continue
            k = st["col"]
            if kind == "eye":
                p = cols[k].X[st["row"]]
                _, nrm = frame_at(cols, k, float(cols[k].u[st["row"]]))
                p = p - 0.05 * nrm
            else:
                p, nrm = frame_at(cols, k, st["u"])
            print(f"[hold] {kind} {st['space']}: ({p[0]:.2f}, {p[1]:.2f}, {p[2]:.2f}) normal ({nrm[0]:.2f}, {nrm[1]:.2f}, "
                  f"{nrm[2]:.2f}) {p[2] - cols[k].ref:.2f} above the floor")


def build(variant):
    MT = materials()
    if variant == "lantern_dead":
        acc = Acc()
        lantern(acc, MT, Matrix.Identity(4))
        return [acc.build("Hold_lantern_dead")]
    if variant == "face_1":
        return [solid_relief("face", dict(eyes="closed", mouth=0.0, yaw=0, pitch=-6, gaunt=0.2), FACE, MT)]
    if variant == "face_2":
        return [solid_relief("face", dict(eyes="hollow", mouth=1.0, brow=1.0, yaw=0, pitch=8, gaunt=0.8), FACE, MT)]
    if variant == "face_3":
        return [solid_relief("face", dict(eyes="closed", mouth=0.0, yaw=42, pitch=-4, gaunt=0.3), FACE, MT)]
    if variant == "hand_1":
        return [solid_relief("hand", dict(spread=26, curl=(0, 0, 0), thumb=62, thumb_curl=0.0, roll=0), 0.45, MT)]
    if variant == "hand_2":
        return [solid_relief("hand", dict(spread=14, curl=(25, 50, 40), thumb=40, thumb_curl=25, roll=0), 0.45, MT)]
    if variant == "eye_socket":
        return [eye_socket(MT)]
    if variant == "eye_ball":
        return [eye_ball(MT)]
    if variant in ("eye_lid_top", "eye_lid_bottom"):
        return [eye_lid(MT, variant == "eye_lid_top")]

    rng = random.Random(1717)
    plan = Plan()
    mesh = Mesh()
    cols, sites = build_walls(plan, mesh, MT, rng)
    place_reliefs(cols, sites, mesh, MT, rng)
    build_ground(plan, mesh, MT)
    build_ramp(cols, plan, mesh, MT)
    build_crack(cols, sites, plan, mesh, MT)
    build_tunnel(cols, plan, mesh, MT)
    build_stalactites(cols, plan, mesh, MT, rng)
    build_rocks(plan, mesh, MT, rng)
    report_spots(cols, sites)
    ob = mesh.build("Hold")
    tris = sum(len(p.vertices) - 2 for p in ob.data.polygons)
    print(f"[hold] faces {len(ob.data.polygons)}, tris {tris}, verts {len(ob.data.vertices)}")
    return [ob]
