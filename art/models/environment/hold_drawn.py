"""The Hold as the user drew it: hold.py's living cave grown round the floor plan of their Figma drawing
(game/Assets/_Project/Levels/the_hold.json; its ground pieces at +0.5 are where it's open, everything else is the rock),
not taken literally: the outline wanders and rounds its corners, the walls lean and fold like cloth, pale veins run
through them, the kept press out of them in relief, the rock mass's tops lie dark behind. At the drawing's `scale`
(the drawing is a sketch: 0.6 of its metres), its `origin` (where "We begin here" is) at the model's origin.

It borrows hold.py's machinery (its fields' maths, its living walls, its reliefs, its materials, the camera's ceiling:
walls are low only where they'd hide a floor north of them), with this drawing's plan instead of hold.py's own, and
none of its crack, ramp or tunnel. Where the markers' pieces went (each eye's socket hole, each face's and hand's
spot, the wall each of the Kept tears out of) it writes to Levels/the_hold.spots.json for the scene builder."""
import json
import math
import os
import random
import runpy

import numpy as np

HERE = os.path.dirname(os.path.abspath(__file__))
LEVEL = os.path.normpath(os.path.join(HERE, "..", "..", "..", "game", "Assets", "_Project", "Levels", "the_hold.json"))
SPOTS = LEVEL.replace(".json", ".spots.json")


def lib():
    """hold.py's functions, and the globals they read (to point at this drawing)."""
    h = runpy.run_path(os.path.join(HERE, "hold.py"), init_globals={"kit": kit}, run_name="hold_lib")
    return h, h["build_walls"].__globals__


def drawing():
    with open(LEVEL) as f:
        d = json.load(f)
    s, oe, on = d.get("scale", 1.0), d["originE"], d["originN"]
    floors = [((g["e0"] - oe) * s, (g["e1"] - oe) * s, (g["n0"] - on) * s, (g["n1"] - on) * s)
              for g in d["ground"] if "+0.5" in g["kind"]]
    marks = [dict(code=m["code"], e=(m["e"] - oe) * s, n=(m["n"] - on) * s) for m in d["markers"]]
    return d, floors, marks


class DrawnPlan:
    """The fields hold.py's walls and ground read (Plan's), for the drawing: open where its floors are (their union,
    rounded, its edge wandering), the walls tall on the far sides and low on the near (south) ones, the camera's
    ceiling, the rock mass's tops."""

    def __init__(self, H, NS, floors):
        RES = NS["RES"]
        e0 = min(f[0] for f in floors) - 7.0
        e1 = max(f[1] for f in floors) + 7.0
        n0 = min(f[2] for f in floors) - 7.0
        n1 = max(f[3] for f in floors) + 7.0
        NS["GX0"], NS["GX1"], NS["GY0"], NS["GY1"] = e0, e1, n0, n1
        self.es = np.arange(e0, e1 + 1e-9, RES)
        self.ns = np.arange(n0, n1 + 1e-9, RES)
        E, N = np.meshgrid(self.es, self.ns)
        self.E, self.N = E, N
        fbm, smin, smoothstep = H["fbm"], H["smin"], H["smoothstep"]
        sd = [H["sd_rrect"](E, N, a, b, c, d, 1.6) for a, b, c, d in floors]
        o = sd[0]
        for x in sd[1:]:
            o = smin(o, x, 1.8)
        self.spaces = np.stack([o])
        # Its edge wandering (never into the way much: the noise mostly widens it).
        o = o + 0.5 * fbm(E * 0.17, N * 0.17, 0.3, 2, seed=3) + 0.15 * fbm(E * 0.6, N * 0.6, 0.7, 2, seed=4)
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
        facing = smoothstep(-0.6, -0.05, self.gy)
        self.hwall = 1.3 + (tall - 1.3) * facing
        self.C = H["Plan"].camera_ceiling(self)
        self.hc = np.minimum(self.hwall, self.C)
        d = np.maximum(o, 0)
        safe = H["blur"](H["minfilter"](self.C, 6), 5)
        rise = H["blur"](self.hwall, 6) + 0.3 * np.clip(d - 1.0, 0, 10)
        lump = 0.45 * fbm(E * 0.45, N * 0.45, 4.4, 3, seed=9) * smoothstep(1.2, 2.5, d)
        self.R = np.maximum(np.minimum(rise + lump, safe) - 0.15, 0.6)
        self.chamber_sd = np.full(o.shape, 99.0)

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
    """hold.py's choose_sites, for the drawing: the markers' eyes and presences on the wall nearest each, the kept in
    relief along every wall that's tall enough (not the low south ones), bodies swelling under the stone; no crack (a
    placeholder hold.py's walls expect, out of the way)."""

    def choose(cols, offsets, U, rng):
        sites = [dict(kind="crack", col=0, u=-100.0, flat=(0.001, 0.002), row=-1, space="drawn")]
        taken = []

        def free(e, n, u, r):
            return all((e - a) ** 2 + (n - b) ** 2 + (u - c) ** 2 > (r + rr) ** 2 for a, b, c, rr in taken)

        def tall(i, c, need=3.0):
            return c.H - c.ref >= need and 2 < i < len(cols) - 3 and cols[i - 2].loop == c.loop == cols[i + 2].loop

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


def build_ground(H, plan, mesh, MT):
    """hold.py's ground (the floors, the rock mass behind the walls), without its crack, tunnel or ramp."""
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
    rows, cols = E.shape
    idx = np.arange(rows * cols).reshape(rows, cols)
    V = np.stack([E.ravel(), N.ravel(), h.ravel()], axis=1)
    a, b = idx[:-1, :-1].ravel(), idx[:-1, 1:].ravel()
    c, dd = idx[1:, 1:].ravel(), idx[1:, :-1].ravel()
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
    mesh.patch(V, T, mats, smooth, (0, 0, 1.0))
    print(f"[hold_drawn] ground: {len(T)} tris")


def spots(H, cols, sites, marks):
    """Where each marker's piece went: the eyes' holes and presences' spots (as hold.py reports them), the wall each of
    the Kept tears out of (the wall foot nearest its spot, and the way out of it)."""
    frame_at = H["frame_at"]
    out = dict(eyes=[], presences=[], kept=[])
    for st in sites:
        if st["kind"] == "eye":
            k = st["col"]
            p = cols[k].X[st["row"]]
            _, nrm = frame_at(cols, k, float(cols[k].u[st["row"]]))
            p = p - 0.05 * nrm
            out["eyes"].append(dict(at=[round(float(x), 3) for x in p], normal=[round(float(x), 3) for x in nrm]))
        elif st["kind"] == "presence":
            p, nrm = frame_at(cols, st["col"], st["u"])
            out["presences"].append(dict(what=st["what"], at=[round(float(x), 3) for x in p],
                                         normal=[round(float(x), 3) for x in nrm]))
    for m in marks:
        if m["code"] != "KP":
            continue
        i = nearest(cols, m["e"], m["n"], lambda i, c: True)
        c = cols[i]
        out["kept"].append(dict(spot=[round(m["e"], 3), round(m["n"], 3)], foot=[round(c.e, 3), round(c.n, 3), round(c.ref, 3)],
                                out=[round(-c.N[0], 3), round(-c.N[1], 3)]))
    with open(SPOTS, "w") as f:
        json.dump(out, f, indent=1)
    print(f"[hold_drawn] spots: {len(out['eyes'])} eyes, {len(out['presences'])} presences, {len(out['kept'])} Kept -> {SPOTS}")


H_PLAN_SAMPLE = None


def build():
    global H_PLAN_SAMPLE
    H, NS = lib()
    H_PLAN_SAMPLE = H["Plan"].sample
    d, floors, marks = drawing()
    plan = DrawnPlan(H, NS, floors)
    # (hold.py's special places: none here)
    NS["in_crack"] = lambda e, n, u, wide=False: np.zeros(np.shape(e), bool)
    NS["in_tunnel"] = lambda e, n, u: np.zeros(np.shape(e), bool)
    NS["CRACK_E"], NS["CRACK_FOOT"], NS["BOSS_E"] = 1e5, (1e5, 1e5), 1e5
    NS["choose_sites"] = make_sites(marks)
    MT = H["materials"]()
    rng = random.Random(2611)
    mesh = H["Mesh"]()
    cols, sites = H["build_walls"](plan, mesh, MT, rng)
    H["place_reliefs"](cols, sites, mesh, MT, rng)
    build_ground(H, plan, mesh, MT)
    # (no stalactites: hold.py places them for its own cave; here they'd hang over the way)
    spots(H, cols, sites, marks)
    ob = mesh.build("HoldDrawn")
    print(f"[hold_drawn] faces {len(ob.data.polygons)}, verts {len(ob.data.vertices)}")
    return split_tiles(ob, 10.0)


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
