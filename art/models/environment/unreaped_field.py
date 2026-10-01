"""The Unreaped Rows: the field above the orchard, behind its wall, where the Watchman keeps watch. Never harvested:
the wheat stands where it ripened, dead on its stalks, grey-gold, in rows; a few stooks stand where someone began
and stopped. "ground" (24 x 16 m, its top at 3.3 like the orchard's top terrace, which it adjoins along its south
edge: dry-stone retaining walls on its other three sides, ploughed earth in ridges along the rows, stubble, a few
leaves blown over the wall), "wheat" (a clump of standing dead wheat, about 1.4 m across and knee to hip high:
nothing solid in it), "stook" (sheaves stood together to dry, head-high). Game directions (see masonry.py), origin
at the centre of its south edge (the ground: at the water's surface; the others: on the ground)."""
import math
import masonry as m

VARIANTS = ["ground", "wheat", "stook"]
W, D, TOP = 24.0, 16.0, 3.3
EARTH = ("FieldEarth1", "#7a6d58"), ("FieldEarth2", "#857660"), ("FieldEarth3", "#665b4a")
WHEAT = ("Wheat1", "#a39a7c"), ("Wheat2", "#918869"), ("Wheat3", "#b3a986")  # (dead: gone grey on the stalk)


def rod(name, seg, r1, r2, a, b, material, smooth=False):
    """A cone from a to b (its length the distance between them)."""
    return m.part(name, m.kit.cone(seg, r1, r2, math.dist(a, b)), material, m.kit.along(a, b), smooth=smooth)


def ground(parts):
    rng = m.kit.rng
    e0, e1, n0, n1 = -W / 2, W / 2, 0.0, D
    bottom, t = -0.9, 0.45
    parts.append(m.box("Earth", m.mat(m.MORTAR), e0 + 0.3, e1 - 0.3, n0, n1 - 0.3, bottom, TOP - 0.1))
    m.wall(parts, e0, e0 + t, n0, n1, bottom, TOP, course=0.45)           # the ends
    m.wall(parts, e1 - t, e1, n0, n1, bottom, TOP, course=0.45)
    m.wall(parts, e0 + t, e1 - t, n1 - t, n1, bottom, TOP, course=0.45)   # the back (north)
    # Ploughed earth: ridges along the rows (north-south), each its own tone, a little uneven.
    tones = [m.mat(c) for c in EARTH]
    e = e0 + t
    k = 0
    while e < e1 - t - 0.05:
        w = min(0.6, e1 - t - e)
        n = n0
        while n < n1 - t - 0.05:
            d = min(rng.uniform(2.0, 4.0), n1 - t - n)
            lift = 0.05 if k % 2 == 0 else 0.0  # (ridge, furrow)
            parts.append(m.box("Furrow", tones[(k + int(n)) % 3] if lift else tones[2], e, e + w, n, n + d, TOP - 0.1, TOP + lift + rng.uniform(-0.008, 0.008)))
            n += d
        e += w
        k += 1
    # Stubble and fallen ears scattered over it, and a few leaves blown over the wall.
    straw = [m.mat(c) for c in WHEAT]
    for _ in range(420):
        se, sn = rng.uniform(e0 + t + 0.1, e1 - t - 0.1), rng.uniform(n0 + 0.1, n1 - t - 0.1)
        s = rng.uniform(0.08, 0.16)
        parts.append(m.part("Stubble", m.kit.cube(), rng.choice(straw), m.M((-se, -sn, TOP + 0.06), rot=(0, 0, rng.uniform(0, 360)), scale=(s, 0.015, 0.012))))
    leaves = [m.mat(l) for l in m.LEAVES]
    for _ in range(60):
        le, ln = rng.uniform(e0 + t + 0.1, e1 - t - 0.1), rng.gauss(1.2, 1.0)
        if not (n0 + 0.05 < ln < n1 - t):
            continue
        s = rng.uniform(0.11, 0.18)
        parts.append(m.part("Leaf", m.kit.cube(), rng.choice(leaves), m.M((-le, -ln, TOP + 0.07), rot=(0, 0, rng.uniform(0, 360)), scale=(s, s * 0.62, 0.008))))


def wheat(parts):
    rng = m.kit.rng
    tones = [m.mat(c) for c in WHEAT]
    head = m.kit.mat("WheatHead", "#bdb08a")
    for _ in range(95):
        r, a = 0.7 * math.sqrt(rng.random()), rng.uniform(0, math.tau)
        base = (math.cos(a) * r, math.sin(a) * r, 0.0)
        height = rng.uniform(0.55, 0.9) * (1.0 - 0.3 * r)  # (taller in the middle of the clump)
        lean, way = math.radians(rng.uniform(0, 16)), rng.uniform(0, math.tau)
        tip = (base[0] + math.cos(way) * math.sin(lean) * height, base[1] + math.sin(way) * math.sin(lean) * height, math.cos(lean) * height)
        parts.append(rod("Stalk", 3, 0.017, 0.009, base, tip, rng.choice(tones)))
        # The ear, nodding over: heavy with grain nobody came for.
        droop = rng.uniform(0.3, 0.9)
        end = (tip[0] + math.cos(way) * 0.17 * math.cos(droop), tip[1] + math.sin(way) * 0.17 * math.cos(droop), tip[2] - 0.17 * math.sin(droop) + 0.05)
        parts.append(rod("Ear", 4, 0.022, 0.016, tip, end, head))


def stook(parts):
    rng = m.kit.rng
    tones = [m.mat(c) for c in WHEAT]
    band = m.kit.mat("Rope", "#a38b62")
    for k in range(7):
        a = math.tau * k / 7 + rng.uniform(-0.1, 0.1)
        foot = (math.cos(a) * 0.42, math.sin(a) * 0.42, 0.0)
        top = (math.cos(a) * 0.1, math.sin(a) * 0.1, 1.25 + rng.uniform(-0.08, 0.06))
        tone = rng.choice(tones)
        parts.append(rod("Sheaf", 6, 0.13, 0.09, foot, top, tone, smooth=True))
        mid = tuple(foot[i] + (top[i] - foot[i]) * 0.55 for i in range(3))
        parts.append(rod("Band", 6, 0.115, 0.112, mid, tuple(mid[i] + (top[i] - foot[i]) * 0.06 for i in range(3)), band))
        # The ears splayed out at the top.
        over = (top[0] + math.cos(a) * 0.12, top[1] + math.sin(a) * 0.12, top[2] + 0.2)
        parts.append(rod("Ears", 6, 0.09, 0.17, top, over, tone))


def build(variant):
    m.orchard()
    parts = []
    {"ground": ground, "wheat": wheat, "stook": stook}[variant](parts)
    return [m.kit.join(parts, f"UnreapedField_{variant}")]
