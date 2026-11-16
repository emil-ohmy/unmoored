"""The Hold, the level: hold.py's living cave grown round the floors of a level drawn in the level editor
(game/Assets/_Project/Levels/the_hold_spiral.json, format "level/2": floors as paths, rounds and rectangles, in metres;
everything that isn't floor is the rock), not taken literally: the outline wanders, the walls lean and fold like cloth,
pale veins run through them, the kept press out of them in relief, the rock mass's tops lie dark behind.

It borrows hold.py's machinery (its fields' maths, its living walls, its reliefs, its materials, the camera's ceiling:
walls are low only where they'd hide a floor north of them, its Boss's chamber and the tunnel out behind the Boss's
face), with this level's plan instead of hold.py's own:
- the round floor the Hold (HOLD) stands in is its chamber, cut off on the north by the Boss's broad smooth wall (a metre
  behind its marker: where its face is),
  the tunnel's mouth in it behind the face, the tunnel short (the level's way runs north of it), then a cleft open to
  the sky (the way out: the grey day);
- where a door of eyes (EYED) stands, the wall is flat and upright (a built wall: the door's model stands against it);
- no crack, ramp, lake or stalactites (hold.py's own places).
Where the markers' pieces went (each eye's socket hole, each face's and hand's spot, the wall each of the Kept tears out
of, the Boss's wall, the way out, each door's wall, the way across each gate) it writes to
Levels/the_hold_spiral.spots.json for the scene builder."""
import json
import math
import os
import random
import runpy

import numpy as np

HERE = os.path.dirname(os.path.abspath(__file__))
LEVEL = os.path.normpath(os.path.join(HERE, "..", "..", "..", "game", "Assets", "_Project", "Levels", "the_hold_spiral.json"))
SPOTS = LEVEL.replace(".json", ".spots.json")

TUNNEL_ROOF = 3.5      # the tunnel behind the Boss's face: how far it runs in under the rock (hold.py's is 5.5)
CLEFT_LEN = 2.2        # then the cleft open above, this long, ending in a rock face
DOOR_HALF = 3.4        # a door of eyes' flat wall: half its width


def lib():
    """hold.py's functions, and the globals they read (to point at this level)."""
    h = runpy.run_path(os.path.join(HERE, "hold.py"), init_globals={"kit": kit}, run_name="hold_lib")
    return h, h["build_walls"].__globals__


def level():
    """The level's walkable floors (its +0.5 ground: the cave's floor), its markers, at its scale."""
    with open(LEVEL) as f:
        d = json.load(f)
    s = d.get("scale", 1.0) or 1.0
    floors = []
    for f in d["floors"]:
        if not 0.0 < f.get("top", 0.5) < 3.0:
            continue
        g = dict(f)
        if f["type"] == "path":
            g["points"] = [(p[0] * s, p[1] * s) for p in f["points"]]
            g["width"] = f["width"] * s
        elif f["type"] == "circle":
            g["e"], g["n"], g["r"] = f["e"] * s, f["n"] * s, f["r"] * s
        else:
            g["e"], g["n"], g["w"], g["h"] = f["e"] * s, f["n"] * s, f["w"] * s, f["h"] * s
        floors.append(g)
    marks = [dict(code=m["code"], e=m["e"] * s, n=m["n"] * s, label=m.get("label", "")) for m in d["markers"]]
    return d, floors, marks


def sd_floor(H, E, N, f):
    if f["type"] == "path":
        return H["sd_chain"](E, N, f["points"], f["width"] / 2)
    if f["type"] == "circle":
        return H["sd_circle"](E, N, (f["e"], f["n"]), f["r"])
    a = math.radians(f.get("rot", 0) or 0)
    x = (E - f["e"]) * math.cos(a) + (N - f["n"]) * math.sin(a)
    y = -(E - f["e"]) * math.sin(a) + (N - f["n"]) * math.cos(a)
    return H["sd_rrect"](x, y, -f["w"] / 2, f["w"] / 2, -f["h"] / 2, f["h"] / 2, min(0.8, f["w"] / 2, f["h"] / 2))


def extent(f):
    if f["type"] == "path":
        es, ns = [p[0] for p in f["points"]], [p[1] for p in f["points"]]
        w = f["width"] / 2
        return min(es) - w, max(es) + w, min(ns) - w, max(ns) + w
    if f["type"] == "circle":
        return f["e"] - f["r"], f["e"] + f["r"], f["n"] - f["r"], f["n"] + f["r"]
    r = math.hypot(f["w"], f["h"]) / 2
    return f["e"] - r, f["e"] + r, f["n"] - r, f["n"] + r


class DrawnPlan:
    """The fields hold.py's walls and ground read (Plan's), for the level: open where its floors are (their union,
    rounded, its edge wandering, but not along the Boss's wall nor a door's), the walls tall on the far sides and low
    on the near (south) ones, the camera's ceiling, the rock mass's tops."""

    def __init__(self, H, NS, floors, chamber, doors):
        RES = NS["RES"]
        ext = [extent(f) for f in floors]
        e0, e1 = min(x[0] for x in ext) - 7.0, max(x[1] for x in ext) + 7.0
        n0, n1 = min(x[2] for x in ext) - 7.0, max(x[3] for x in ext) + 7.0
        NS["GX0"], NS["GX1"], NS["GY0"], NS["GY1"] = e0, e1, n0, n1
        self.es = np.arange(e0, e1 + 1e-9, RES)
        self.ns = np.arange(n0, n1 + 1e-9, RES)
        E, N = np.meshgrid(self.es, self.ns)
        self.E, self.N = E, N
        fbm, smin, smoothstep = H["fbm"], H["smin"], H["smoothstep"]
        rest = None
        for f in floors:
            if f is chamber:
                continue
            x = sd_floor(H, E, N, f)
            rest = x if rest is None else smin(rest, x, 1.8)
        ch = H["sd_chamber"](E, N) if chamber is not None else np.full(E.shape, 99.0)
        self.chamber_sd = ch
        big = np.full(E.shape, 99.0)
        # (hold.py's spaces: the walls' columns take the nearest; the chamber is its sixth, as in hold.py)
        self.spaces = np.stack([rest, big, big, big, big, ch])
        o = smin(rest, ch, 1.2)
        # Its edge wandering (never into the way much: the noise mostly widens it), but calm along the Boss's wall and
        # each door's (smooth, upright: built, or the Boss's own).
        calm = np.zeros(E.shape)
        if chamber is not None:
            be, bn, bh = NS["BOSS_E"], NS["BOSS_N"], NS["BOSS_HALF"]
            calm = np.maximum(calm, smoothstep(bh + 1.5, bh, np.abs(E - be)) * smoothstep(bn - 5.5, bn - 2.5, N)
                              * smoothstep(bn + 5.0, bn + 3.0, N))
        for de, dn in doors:
            calm = np.maximum(calm, smoothstep(DOOR_HALF + 2.0, DOOR_HALF + 0.5, np.hypot(E - de, N - dn)))
        noise = 0.5 * fbm(E * 0.17, N * 0.17, 0.3, 2, seed=3) + 0.15 * fbm(E * 0.6, N * 0.6, 0.7, 2, seed=4)
        o = o + noise * (1 - calm)
        self.floor_sd = o
        self.O = o
        gy, gx = np.gradient(o, RES)
        gl = np.maximum(np.hypot(gx, gy), 1e-6)
        self.gx, self.gy = gx / gl, gy / gl
        TOP = NS["TOP"]
        self.level = TOP + 0.035 * fbm(E * 0.5, N * 0.5, 1.7, 2, seed=5)
        self.water = np.zeros(o.shape, bool)
        self.floor = o < 0.4
        self.high_floor = np.zeros(o.shape, bool)
        self.thr_only = np.zeros(o.shape, bool)
        self.ramp = np.zeros(o.shape, bool)
        self.ramp_full = self.ramp
        tall = 6.4 + 0.8 * fbm(E * 0.12, N * 0.12, 2.1, 2, seed=6)
        tall = tall + (12.0 - tall) * smoothstep(2.5, 0.5, ch)   # (the chamber's: as tall as the camera allows)
        facing = smoothstep(-0.6, -0.05, self.gy)
        self.hwall = 1.3 + (tall - 1.3) * facing
        self.C = H["Plan"].camera_ceiling(self)
        self.hc = np.minimum(self.hwall, self.C)
        d = np.maximum(o, 0)
        safe = H["blur"](H["minfilter"](self.C, 6), 5)
        rise = H["blur"](self.hwall, 6) + 0.3 * np.clip(d - 1.0, 0, 10)
        lump = 0.45 * fbm(E * 0.45, N * 0.45, 4.4, 3, seed=9) * smoothstep(1.2, 2.5, d)
        self.R = np.maximum(np.minimum(rise + lump, safe) - 0.15, 0.6)
        if chamber is not None:
            # Over the tunnel behind the Boss's face, the rock stands over its roof (the camera's ceiling allows it).
            be, bn = NS["BOSS_E"], NS["BOSS_N"]
            over = (np.abs(E - be) < 2.4) & (N > bn - 0.5) & (N < bn + TUNNEL_ROOF + 0.2)
            self.R = np.where(over, np.maximum(self.R, TOP + 1.0 + NS["TUNNEL_H"] + 0.9), self.R)

    def sample(self, grid, e, n):
        return H_PLAN_SAMPLE(self, grid, e, n)


def nearest(cols, e, n, pred):
    best, bd = None, 1e18
    for i, c in enumerate(cols):
        if not pred(i, c):
            continue
        d = (c.e - e) ** 2 + (c.n - n) ** 2
        if d < bd:
            best, bd = i, d
    return best


def make_sites(marks):
    """hold.py's choose_sites, for the level: the markers' eyes and presences on the wall nearest each, the kept in
    relief along every wall that's tall enough (not the low south ones, nor the Boss's wall or a door's), bodies
    swelling under the stone; no crack (a placeholder hold.py's walls expect, out of the way)."""

    def choose(cols, offsets, U, rng):
        sites = [dict(kind="crack", col=0, u=-100.0, flat=(0.001, 0.002), row=-1, space="drawn")]
        taken = []

        def free(e, n, u, r):
            return all((e - a) ** 2 + (n - b) ** 2 + (u - c) ** 2 > (r + rr) ** 2 for a, b, c, rr in taken)

        def tall(i, c, need=3.0):
            return c.H - c.ref >= need and 2 < i < len(cols) - 3 and cols[i - 2].loop == c.loop == cols[i + 2].loop \
                and c.boss < 0.05

        for idx, m in enumerate(marks):
            if m["code"] not in ("EYE", "FACE", "HAND"):
                continue
            i = nearest(cols, m["e"], m["n"], lambda i, c: tall(i, c) and free(c.e, c.n, c.ref + 1.8, 0.9))
            if i is None:
                continue
            c = cols[i]
            if m["code"] == "EYE":
                rows = [r for r in range(len(c.u)) if c.zone[r] == 1 and 1.3 <= c.u[r] - c.ref <= 2.4]
                if not rows:
                    continue
                r = min(rows, key=lambda r: abs(c.u[r] - c.ref - 1.9))
                u = float(c.u[r])
                sites.append(dict(kind="eye", col=i, row=r, u=u, flat=(0.62, 0.95), space="drawn", mark=idx))
            else:
                u = c.ref + 1.2 + 0.6 * ((idx * 0.37) % 1.0)
                sites.append(dict(kind="presence", col=i, row=-1, u=u, flat=(0.55, 0.9), space="drawn", mark=idx,
                                  what=m["code"].lower()))
            taken.append((c.e, c.n, u, 0.95))
        # The kept in relief: about one every 3 m of tall wall.
        cand = [i for i, c in enumerate(cols) if tall(i, c, 1.6) and c.N[1] > -0.25]
        want = int(len(cand) * 0.3 / 3.0) if cand else 0
        got, tries = 0, 0
        while got < want and tries < want * 60:
            tries += 1
            i = rng.choice(cand)
            c = cols[i]
            face = rng.random() < 0.55
            size = rng.uniform(0.85, 1.35) if face else rng.uniform(0.7, 1.1)
            if rng.random() < 0.18:
                size *= 1.4
            lo, hi = c.ref + 0.35 + size * 0.5, min(c.H - 0.45 - size * 0.75, c.ref + 3.6)
            if hi <= lo:
                continue
            u = rng.uniform(lo, hi)
            if not free(c.e, c.n, u, size * 0.52):
                continue
            taken.append((c.e, c.n, u, size * 0.5))
            sites.append(dict(kind="face" if face else "hand", col=i, row=-1, u=u, size=size,
                              flat=(size * 0.8, size * 1.25), space="drawn"))
            got += 1
            if face and rng.random() < 0.6 and u - size * 1.3 > c.ref + 0.3:   # the body under it
                sites.append(dict(kind="body", col=i, s=c.s + rng.uniform(-0.1, 0.1), u=u - size * 1.3, amp=0.12 * size,
                                  ss=size * 0.7, su=size * 0.9))
        for _ in range(int(want * 0.6)):   # bodies alone, nothing showing but their shape
            if not cand:
                break
            i = rng.choice(cand)
            c = cols[i]
            if c.H - c.ref <= 3:
                continue
            sites.append(dict(kind="body", col=i, s=c.s, u=c.ref + rng.uniform(0.8, max(1.0, c.H - c.ref - 1.2)),
                              amp=rng.uniform(0.08, 0.16), ss=rng.uniform(0.3, 0.5), su=rng.uniform(0.5, 0.9)))
        print(f"[hold_drawn] sites: {sum(1 for s in sites if s['kind'] == 'eye')} eyes, "
              f"{sum(1 for s in sites if s['kind'] == 'presence')} presences, {got} reliefs")
        return sites

    return choose


def flat_walls(H, NS, original, doors):
    """hold.py's wall_columns, then the Boss's wall smooth and hardly leaning (as hold.py makes its own, here wherever
    the Boss stands), and each door's wall flat and upright."""
    smoothstep, profile = H["smoothstep"], H["profile"]

    def columns(plan):
        cols = original(plan)
        be, bn, bh = NS["BOSS_E"], NS["BOSS_N"], NS["BOSS_HALF"]
        for c in cols:
            boss = smoothstep(bh + 0.3, bh - 1.2, abs(c.e - be)) * smoothstep(0.45, 0.75, c.N[1]) * (c.space == 5) \
                * smoothstep(bn - 4.5, bn - 2.5, c.n)
            door = max([smoothstep(DOOR_HALF + 0.6, DOOR_HALF - 0.4, math.hypot(c.e - de, c.n - dn)) for de, dn in doors] + [0.0])
            c.boss = float(max(boss, door))
            if c.boss > 0:
                c.oh *= (1 - 0.6 * boss) * (1 - door)
                c.d, c.u, c.zone, c.t, c.nd, c.nu = profile(c.base, c.H, c.oh)
        return cols

    return columns


def build_ground(H, NS, plan, mesh, MT, chamber):
    """hold.py's ground (the floors, the rock mass behind the walls), without its crack or ramp; none in the Boss's
    tunnel's mouth (the tunnel has its own floor) nor over the cleft at its end (open above)."""
    fbm = H["fbm"]
    step = 2
    E, N = plan.E[::step, ::step], plan.N[::step, ::step]
    O = plan.O[::step, ::step]
    lev = plan.level[::step, ::step]
    flo = plan.floor[::step, ::step]
    R = plan.R[::step, ::step]
    gx, gy = plan.gx[::step, ::step], plan.gy[::step, ::step]
    d = np.maximum(O, 0)
    near = plan.sample(plan.level, E - (d + 0.35) * gx, N - (d + 0.35) * gy)
    h = np.where(O < 0, lev, np.where(O < 0.95, near, R))
    strip = np.zeros(E.shape, bool)
    if chamber is not None:
        be, bn, cl = NS["BOSS_E"], NS["BOSS_N"], NS["CLEFT_HW"]
        strip |= (np.abs(E - be) < 1.45) & (N - bn > -0.05) & (N - bn < 1.45) & (O < 0.95)
        strip |= (np.abs(E - be) < cl - 0.05) & (N - bn > TUNNEL_ROOF + 0.05) & (N - bn < TUNNEL_ROOF + CLEFT_LEN + 0.05)
    rows, cols = E.shape
    idx = np.arange(rows * cols).reshape(rows, cols)
    V = np.stack([E.ravel(), N.ravel(), h.ravel()], axis=1)
    a, b = idx[:-1, :-1].ravel(), idx[:-1, 1:].ravel()
    c, dd = idx[1:, 1:].ravel(), idx[1:, :-1].ravel()
    vv = strip.ravel()
    keep = ~(vv[a] | vv[b] | vv[c] | vv[dd])
    a, b, c, dd = a[keep], b[keep], c[keep], dd[keep]
    hv = h.ravel()
    diag = np.abs(hv[a] - hv[c]) <= np.abs(hv[b] - hv[dd])
    T1 = np.where(diag[:, None], np.stack([a, b, c], 1), np.stack([a, b, dd], 1))
    T2 = np.where(diag[:, None], np.stack([a, c, dd], 1), np.stack([b, c, dd], 1))
    T = np.vstack([T1, T2])
    fl = flo.ravel()
    allfloor = fl[T].all(axis=1)
    anyfloor = fl[T].any(axis=1)
    # (wet rock only at the walls' feet: patches of it out on the floor come out stepped on this grid)
    mats = [MT["rock"] if af else MT["wet"] if an else MT["dark"] for af, an in zip(allfloor, anyfloor)]
    smooth = [m is MT["dark"] for m in mats]
    used = np.unique(T)
    remap = np.full(len(V), -1)
    remap[used] = np.arange(len(used))
    mesh.patch(V[used], remap[T], mats, smooth, (0, 0, 1.0))
    print(f"[hold_drawn] ground: {len(T)} tris")


def build_tunnel(H, NS, cols, plan, mesh, MT):
    """hold.py's tunnel behind the Boss's face, shorter (the level's way runs north of the chamber), its cleft ending
    in a rock face (hold.py's runs on to where its rock ends)."""
    bn = NS["BOSS_N"]
    gy1 = NS["GY1"]
    NS["GY1"] = bn + 1.0 + TUNNEL_ROOF + CLEFT_LEN   # (hold.py's cleft runs on till this)
    H["build_tunnel"](cols, plan, mesh, MT)
    NS["GY1"] = gy1
    a_end = TUNNEL_ROOF + CLEFT_LEN
    be, cl, floor = NS["BOSS_E"], NS["CLEFT_HW"], H["tunnel_floor"]
    xs = np.linspace(-cl - 0.25, cl + 0.25, 9)
    V, T, nu = [], [], 10
    for x in xs:
        e, n = be + x, bn + a_end + 0.02
        u0 = float(floor(a_end)) - 1.7
        top = float(plan.sample(plan.R, e, n)) + 0.1
        for j in range(nu + 1):
            V.append((e, n, u0 + (top - u0) * j / nu))
    for r in range(len(xs) - 1):
        for j in range(nu):
            p0, p1 = r * (nu + 1) + j, (r + 1) * (nu + 1) + j
            T += [(p0, p1, p1 + 1), (p0, p1 + 1, p0 + 1)]
    mesh.patch(np.array(V), np.array(T), MT["wet"], False, (0.0, -1.0, 0.0))
    return a_end


def chord(plan, e, n, reach=9.0):
    """The narrowest way across the floor through (e, n): its two ends (where the floor ends each side)."""
    best = None
    for k in range(36):
        a = math.pi * k / 36
        de, dn = math.cos(a), math.sin(a)
        ends = []
        for sgn in (1, -1):
            t = 0.0
            while t < reach and float(plan.sample(plan.O, e + sgn * t * de, n + sgn * t * dn)) < 0.0:
                t += 0.1
            ends.append((e + sgn * t * de, n + sgn * t * dn))
        w = math.hypot(ends[0][0] - ends[1][0], ends[0][1] - ends[1][1])
        if best is None or w < best[0]:
            best = (w, ends)
    return best[1]


# The floor's small things (hold_floor.py's pieces), by where they grow: (piece, chance per square metre at the floor's
# edge, out in the way, at most this near another of its kind).
GROWTH = [("lichen_1", 0.16, 0.035, 0.9), ("lichen_2", 0.16, 0.035, 0.9), ("moss_1", 0.14, 0.01, 0.8),
          ("moss_2", 0.14, 0.01, 0.8), ("fungus_1", 0.05, 0.0, 1.2), ("fungus_2", 0.05, 0.0, 1.2),
          ("fungus_glow", 0.035, 0.0, 3.0), ("puddle_1", 0.012, 0.018, 4.0), ("puddle_2", 0.012, 0.018, 4.0)]
DROPPED = ["shoe", "boot", "rag", "bottle", "cup", "comb", "spoon", "coins"]
DROPPED_PER_M2 = 0.012
EDGE = 1.4          # (the band along the walls' feet where things gather, metres in from them)
KEEP_CLEAR = {"START": 2.5, "MP": 2.2, "MSG": 1.4, "WS": 1.4, "OB": 1.4, "SG": 2.0, "AG": 3.0, "EYED": 3.5, "HOLD": 6.0}


def scatter(plan, marks, chamber, rng):
    """Where the floor's small things lie: lichen, moss and fungus thick along the walls' feet and thin out in the way,
    a few black puddles, and here and there something one of the kept dropped. Clear of the set pieces, the messages
    and the start; in the Boss's chamber only along its edge. Each: its piece, where (e, n, the floor's height), its
    turn, its size."""
    out, placed = [], {}
    clear = [(m["e"], m["n"], KEEP_CLEAR[m["code"]]) for m in marks if m["code"] in KEEP_CLEAR]
    step = 0.5
    e0, e1, n0, n1 = plan.es[0], plan.es[-1], plan.ns[0], plan.ns[-1]
    cell = step * step

    def free(e, n, kind, near):
        for (pe, pn) in placed.get(kind, []):
            if (pe - e) ** 2 + (pn - n) ** 2 < near * near:
                return False
        return True

    for n in np.arange(n0, n1, step):
        for e in np.arange(e0, e1, step):
            e_, n_ = e + rng.uniform(-0.2, 0.2), n + rng.uniform(-0.2, 0.2)
            o = float(plan.sample(plan.O, e_, n_))
            if o > -0.35:
                continue                                 # (in the walls, or at their very feet: they lean over it)
            if any((e_ - ce) ** 2 + (n_ - cn) ** 2 < r * r for ce, cn, r in clear):
                continue
            in_chamber = chamber is not None and float(plan.sample(plan.chamber_sd, e_, n_)) < 0.0
            edge = o > -EDGE
            if in_chamber and not edge:
                continue                                 # (the fight's floor: clear)
            u = float(plan.sample(plan.level, e_, n_))
            for kind, at_edge, out_in_way, near in GROWTH:
                if rng.random() < (at_edge if edge else out_in_way) * cell and free(e_, n_, kind, near):
                    placed.setdefault(kind, []).append((e_, n_))
                    big = 1.0 if kind.startswith("puddle") else rng.uniform(0.75, 1.3)
                    out.append(dict(what=kind, at=[round(e_, 3), round(n_, 3), round(u, 3)],
                                    yaw=round(rng.uniform(0, 360), 1), scale=round(big, 2)))
                    break
            else:
                if not in_chamber and rng.random() < DROPPED_PER_M2 * cell and free(e_, n_, "dropped", 2.5):
                    placed.setdefault("dropped", []).append((e_, n_))
                    out.append(dict(what=rng.choice(DROPPED), at=[round(e_, 3), round(n_, 3), round(u, 3)],
                                    yaw=round(rng.uniform(0, 360), 1), scale=1.0))
    counts = {}
    for d in out:
        k = d["what"] if d["what"] not in DROPPED else "dropped"
        counts[k] = counts.get(k, 0) + 1
    print(f"[hold_drawn] the floor's small things: {len(out)} ({', '.join(f'{v} {k}' for k, v in sorted(counts.items()))})")
    return out


def spots(H, NS, cols, sites, marks, plan, chamber, a_end, floors):
    """Where each marker's piece went: the eyes' holes and presences' spots (as hold.py reports them), the wall each of
    the Kept tears out of (the wall foot nearest its spot, and the way out of it), the Boss's wall and the way out
    behind it, each door's wall, the way across each gate (the arena's, a shortcut's); where a cold gleam hangs over
    the floors (every 12 m along each way, one over each round or rectangle), and the level's extent."""
    frame_at, surface = H["frame_at"], H["surface"]
    r3 = lambda v: [round(float(x), 3) for x in v]
    out = dict(eyes=[], presences=[], kept=[], doors=[], gates=[])
    for st in sites:
        if st["kind"] == "eye":
            k = st["col"]
            p = cols[k].X[st["row"]]
            _, nrm = frame_at(cols, k, float(cols[k].u[st["row"]]))
            p = p - 0.05 * nrm
            out["eyes"].append(dict(at=r3(p), normal=r3(nrm)))
        elif st["kind"] == "presence":
            p, nrm = frame_at(cols, st["col"], st["u"])
            out["presences"].append(dict(what=st["what"], at=r3(p), normal=r3(nrm)))
    for m in marks:
        if m["code"] == "KP":
            i = nearest(cols, m["e"], m["n"], lambda i, c: c.boss < 0.05)
            c = cols[i]
            out["kept"].append(dict(spot=r3((m["e"], m["n"])), foot=r3((c.e, c.n, c.ref)), out=r3((-c.N[0], -c.N[1]))))
        elif m["code"] == "EYED":
            i = nearest(cols, m["e"], m["n"], lambda i, c: True)
            c = cols[i]
            out["doors"].append(dict(foot=r3((c.e, c.n, c.ref)), out=r3((-c.N[0], -c.N[1]))))
        elif m["code"] in ("AG", "SG"):
            a, b = chord(plan, m["e"], m["n"])
            out["gates"].append(dict(code=m["code"], spot=r3((m["e"], m["n"])), a=r3(a), b=r3(b)))
    gleams = []
    for f in floors:
        if f is chamber:
            continue
        if f["type"] == "path":
            pts = np.array(f["points"], float)
            seg = np.linalg.norm(np.diff(pts, axis=0), axis=1)
            cum = np.concatenate([[0.0], np.cumsum(seg)])
            for t in np.arange(min(5.0, cum[-1] / 2), cum[-1], 12.0):
                gleams.append(r3((np.interp(t, cum, pts[:, 0]), np.interp(t, cum, pts[:, 1]))))
        else:
            gleams.append(r3((f["e"], f["n"])))
    out["gleams"] = [v for g in gleams for v in g]   # (flat: e, n, e, n... for Unity's JSON)
    out["details"] = scatter(plan, marks, chamber, random.Random(2612))
    x = [extent(f) for f in floors]
    out["extent"] = r3((min(v[0] for v in x), max(v[1] for v in x), min(v[2] for v in x), max(v[3] for v in x)))
    if chamber is not None:
        be, bn, top = NS["BOSS_E"], NS["BOSS_N"], NS["TOP"]
        k = min((i for i, c in enumerate(cols) if c.space == 5 and c.N[1] > 0.6), key=lambda i: abs(cols[i].e - be))
        wall_n = float(surface(cols, k, top + 0.5)[1])
        mid = (TUNNEL_ROOF + a_end) / 2
        out["boss"] = dict(wall=r3((be, wall_n, top)), chamber=r3((chamber["e"], chamber["n"], chamber["r"])),
                           way_out=r3((be, bn + mid, float(H["tunnel_floor"](mid)))))
        print(f"[hold_drawn] the Boss's wall: its face at ({be:.2f}, {wall_n:.2f}); the way out at n {bn + mid:.2f}")
    with open(SPOTS, "w") as f:
        json.dump(out, f, indent=1)
    print(f"[hold_drawn] spots: {len(out['eyes'])} eyes, {len(out['presences'])} presences, {len(out['kept'])} Kept, "
          f"{len(out['doors'])} doors, {len(out['gates'])} gates -> {SPOTS}")


def build_seal(H, MT, a, b, rng):
    """The arena's seal (the scene builder sinks it in the floor till the Hold wakes with you inside, HoldSeal): one low
    ridge of the living stone right across the way in (from a to b, its ends run on into the walls), lumpy, folded,
    the kept pressing up under it (a back, a shoulder, a head), as the walls' bodies do. Its own object, HoldSeal."""
    fbm, smoothstep = H["fbm"], H["smoothstep"]
    a, b = np.array(a, float), np.array(b, float)
    along = (b - a) / np.linalg.norm(b - a)
    side = np.array([-along[1], along[0]])
    L = float(np.linalg.norm(b - a)) + 2.4
    start = a - along * 1.2
    bodies = [(rng.uniform(0.15, 0.85) * L, rng.uniform(0.1, 0.95), rng.uniform(0.35, 0.6), rng.uniform(0.1, 0.18))
              for _ in range(int(L / 1.3))]   # (along, round its section 0..1, size, how far out)
    ss = np.linspace(0, L, int(L / 0.12) + 1)
    ts = np.linspace(0, 1, 31)
    top = 0.5   # (the floor)
    V = []
    for s_ in ss:
        for t in ts:
            ang = math.pi * t
            r = 1.0 + 0.12 * float(fbm(s_ * 0.9, t * 2.0, 1.7, 2, seed=61)) + 0.035 * float(fbm(s_ * 4.0, t * 7.0, 2.9, 1, seed=62))
            for bs, bt, bz, bo in bodies:
                d2 = ((s_ - bs) / bz) ** 2 + ((t - bt) / (bz * 0.5)) ** 2
                r += bo * math.exp(-d2 ** 1.3)
            fold = 0.04 * (1 - abs(math.sin(math.pi * (s_ / 0.9 + 0.6 * float(fbm(s_ * 0.3, 0.0, 3.1, 1, seed=63)))))) ** 3
            r += fold
            x = -0.85 * math.cos(ang) * r
            u = top - 0.45 + 1.75 * math.sin(ang) ** 0.75 * r
            p = start + along * s_ + side * x
            V.append((p[0], p[1], u))
    V = np.array(V)
    nt = len(ts)
    T = []
    for i in range(len(ss) - 1):
        for j in range(nt - 1):
            p, q = i * nt + j, (i + 1) * nt + j
            T += [(p, p + 1, q + 1), (p, q + 1, q)]
    centre = np.array([*((a + b) / 2), top - 0.6])
    mesh = H["Mesh"]()
    mesh.patch(V, np.array(T), MT["wall"], True, V - centre)
    ob = mesh.build("HoldSeal")
    print(f"[hold_drawn] the arena's seal: a ridge {L:.1f} m long across the way in, {len(bodies)} of the kept under it")
    return ob


GATE_HALF = 1.07   # (shortcut_gate.py: its posts' outer faces, either side of its middle)
GATE_JAMB_H = 2.3  # (the jambs' crest over the floor: a little over the gate's lintel, whose ends sit in them; low, as
                   #  the camera looks north over them)


def build_gate_wall(H, MT, a, b, rng):
    """A shortcut gate's wall (the scene builder stands shortcut_gate in its gap): two jambs of the living stone, each
    from a wall of the way (from a, and from b: run on into the walls) in to the gate's post, where it ends in a flat
    face the post stands against; lumpy and folded, the kept pressing up in them, as in the seal. Without them the gate
    stood in the open between the walls. Its own object, GateWall (solid: the game gives it a collider)."""
    fbm = H["fbm"]
    a, b = np.array(a, float), np.array(b, float)
    width = float(np.linalg.norm(b - a))
    along = (b - a) / width
    side = np.array([-along[1], along[0]])
    mid = (a + b) / 2
    top = 0.5   # (the floor)
    mesh = H["Mesh"]()
    made = 0
    for sgn, seed in ((-1, 71), (1, 75)):
        L = width / 2 + 1.2 - GATE_HALF   # (from the post out, and on 1.2 m into the wall)
        if L < 1.4:
            continue                       # (the way hardly wider than the gate: its posts touch the walls)
        start = mid + along * sgn * GATE_HALF   # (the face against the post)
        out = along * sgn
        bodies = [(rng.uniform(0.25, 0.9) * L, rng.uniform(0.15, 0.9), rng.uniform(0.3, 0.5), rng.uniform(0.08, 0.15))
                  for _ in range(max(1, int(L / 1.1)))]
        ss = np.linspace(0, L, int(L / 0.12) + 1)
        ts = np.linspace(0, 1, 31)
        V = []
        for s_ in ss:
            for t in ts:
                ang = math.pi * t
                r = 1.0 + 0.1 * float(fbm(s_ * 0.9, t * 2.0, 1.7, 2, seed=seed)) + 0.03 * float(fbm(s_ * 4.0, t * 7.0, 2.9, 1, seed=seed + 1))
                for bs, bt, bz, bo in bodies:
                    d2 = ((s_ - bs) / bz) ** 2 + ((t - bt) / (bz * 0.5)) ** 2
                    r += bo * math.exp(-d2 ** 1.3)
                r += 0.04 * (1 - abs(math.sin(math.pi * (s_ / 0.9 + 0.6 * float(fbm(s_ * 0.3, 0.0, 3.1, 1, seed=seed + 2)))))) ** 3
                # (squared off near the post: its face stands upright, not a rounded end)
                x = -0.7 * math.cos(ang) * r
                u = top - 0.45 + (GATE_JAMB_H + 0.45) * math.sin(ang) ** 0.6 * r
                p = start + out * s_ + side * x
                V.append((p[0], p[1], u))
        V = np.array(V)
        nt = len(ts)
        T = []
        for i in range(len(ss) - 1):
            for j in range(nt - 1):
                p, q = i * nt + j, (i + 1) * nt + j
                T += [(p, p + 1, q + 1), (p, q + 1, q)]
        axis = np.array([*(start + out * L / 2), top - 0.6])
        mesh.patch(V, np.array(T), MT["wall"], True, V - axis)
        # The face against the post: the first section closed (a fan from its middle, down to the floor).
        ring = V[:nt]
        centre = np.array([ring[:, 0].mean(), ring[:, 1].mean(), top - 0.45])
        CV = np.vstack([ring, centre])
        CT = [(j, j + 1, nt) for j in range(nt - 1)]
        mesh.patch(CV, np.array(CT), MT["wall"], False, (-out[0], -out[1], 0.0))
        made += 1
    if made == 0:
        return None
    ob = mesh.build("GateWall")
    print(f"[hold_drawn] a shortcut gate's wall: {made} jambs across a way {width:.1f} m wide, up to {GATE_JAMB_H} m")
    return ob


H_PLAN_SAMPLE = None


def build():
    global H_PLAN_SAMPLE
    H, NS = lib()
    H_PLAN_SAMPLE = H["Plan"].sample
    d, floors, marks = level()
    # The chamber: the round floor the Hold stands in (its Boss's wall the north side of it).
    hold = next((m for m in marks if m["code"] == "HOLD"), None)
    chamber = None
    if hold is not None:
        rounds = [f for f in floors if f["type"] == "circle"]
        chamber = min(rounds, key=lambda f: math.hypot(f["e"] - hold["e"], f["n"] - hold["n"]) - f["r"])
        NS["CHAMBER_C"], NS["CHAMBER_R"] = (chamber["e"], chamber["n"]), chamber["r"]
        # (its wall a metre behind its marker, where its face is; never beyond the round's own north edge)
        NS["BOSS_E"], NS["BOSS_N"] = hold["e"], min(hold["n"] + 1.0, chamber["n"] + chamber["r"])
        NS["BOSS_HALF"] = min(7.5, 0.8 * chamber["r"])
    else:
        NS["BOSS_E"], NS["BOSS_N"], NS["BOSS_HALF"] = 1e5, 1e5, 1.0
    NS["TOP_U"] = NS["TOP"]             # (the chamber's floor: the cave's, not hold.py's ledge)
    NS["TUNNEL_ROOF"] = TUNNEL_ROOF
    doors = [(m["e"], m["n"]) for m in marks if m["code"] == "EYED"]
    plan = DrawnPlan(H, NS, floors, chamber, doors)
    # (hold.py's other places: none here)
    NS["in_crack"] = lambda e, n, u, wide=False: np.zeros(np.shape(e), bool)
    NS["CRACK_E"], NS["CRACK_FOOT"] = 1e5, (1e5, 1e5)
    NS["choose_sites"] = make_sites(marks)
    NS["wall_columns"] = flat_walls(H, NS, H["wall_columns"], doors)
    MT = H["materials"]()
    rng = random.Random(2611)
    mesh = H["Mesh"]()
    cols, sites = H["build_walls"](plan, mesh, MT, rng)
    H["place_reliefs"](cols, sites, mesh, MT, rng)
    build_ground(H, NS, plan, mesh, MT, chamber)
    a_end = build_tunnel(H, NS, cols, plan, mesh, MT) if chamber is not None else 0.0
    # (no stalactites: hold.py places them for its own cave; here they'd hang over the way)
    spots(H, NS, cols, sites, marks, plan, chamber, a_end, floors)
    ob = mesh.build("HoldDrawn")
    print(f"[hold_drawn] faces {len(ob.data.polygons)}, verts {len(ob.data.vertices)}")
    seals = []
    for m in marks:
        if m["code"] == "AG" and chamber is not None:
            ea, eb = chord(plan, m["e"], m["n"])
            seals.append(build_seal(H, MT, ea, eb, random.Random(4401)))
        elif m["code"] == "SG":
            ea, eb = chord(plan, m["e"], m["n"])
            wall = build_gate_wall(H, MT, ea, eb, random.Random(4402 + len(seals)))
            if wall is not None:
                seals.append(wall)
    return split_tiles(ob, 10.0) + seals


def split_tiles(ob, size):
    """The cave in tiles `size` metres square: each its own object, so each is lit by the lights near it (a renderer
    that lights an object by its few most important lights would light one great cave by the lights nearest its middle)."""
    import bmesh
    import bpy
    me = ob.data
    keys = [(math.floor(p.center.x / size), math.floor(p.center.y / size)) for p in me.polygons]
    out = []
    for key in sorted(set(keys)):
        bm = bmesh.new()
        bm.from_mesh(me)
        bm.faces.ensure_lookup_table()
        drop = [f for f, k in zip(bm.faces, keys) if k != key]
        bmesh.ops.delete(bm, geom=drop, context="FACES")
        part = bpy.data.meshes.new(f"HoldDrawn_{key[0]}_{key[1]}")
        bm.to_mesh(part)
        bm.free()
        for mat in me.materials:
            part.materials.append(mat)
        tile = bpy.data.objects.new(part.name, part)
        bpy.context.scene.collection.objects.link(tile)
        out.append(tile)
    bpy.data.objects.remove(ob)
    print(f"[hold_drawn] in {len(out)} tiles of {size:.0f} m")
    return out
