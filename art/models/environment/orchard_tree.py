"""An orchard tree on the last evening of autumn: a gnarled near-black trunk, twisting up and forking into a few
heavy limbs, under a crown of faceted clumps of leaves in rust, ochre and deep red (darker inside, where the light
doesn't reach), the fruit hanging overripe and refusing to fall. "apple" (round and spreading), "apple_old" (leaning,
hollow-backed, half its crown gone thin), "pear" (taller and narrower) and "flooded" (standing in the water: the same
apple, its foot sunk). Origin at the foot of the trunk (at the ground; "flooded": at the water's surface).
Game directions: up is up; the builder turns them."""
import math

M, part = kit.M, kit.part  # `kit` is injected by build_model.py

VARIANTS = ["apple", "apple_old", "pear", "flooded"]


def build(variant):
    rng = kit.rng
    bark, bark_dark = kit.mat("Bark", "#2a2226"), kit.mat("BarkDark", "#1c171b")
    leaves = [kit.mat("CrownRust", "#9c4628"), kit.mat("CrownOchre", "#b9822f"), kit.mat("CrownRed", "#7a2824"),
              kit.mat("CrownDeep", "#4e2a22")]
    fruit = kit.mat("Fruit", "#8a2a20") if variant != "pear" else kit.mat("Pear", "#a8862f")
    tall = {"apple": 1.0, "apple_old": 0.9, "pear": 1.3, "flooded": 0.95}[variant]
    lean = {"apple": 4, "apple_old": 14, "pear": 2, "flooded": 6}[variant]
    foot = -0.6 if variant == "flooded" else -0.05

    # The trunk: a twisting tube, forking at about 1.4 m.
    pts, r = [], []
    h_fork = 1.4 * tall
    for k in range(7):
        t = k / 6
        x = math.sin(t * 2.6 + 0.5) * 0.12 + math.sin(math.radians(lean)) * t * h_fork
        y = math.cos(t * 2.1) * 0.08
        pts.append((x, y, foot + (h_fork - foot) * t))
        r.append(0.2 - 0.07 * t)
    parts = [part("Trunk", kit.tube(pts, r, seg=7), bark, jitter=0.012)]
    # Roots flaring at the foot.
    for k in range(4):
        a = math.radians(k * 90 + rng.uniform(-25, 25))
        parts.append(part("Root", kit.tube([(0, 0, foot + 0.15), (math.cos(a) * 0.32, math.sin(a) * 0.32, foot + 0.02)], [0.12, 0.05], seg=5), bark_dark))
    top = pts[-1]
    # Limbs: three or four, out and up, each ending in a clump.
    limbs = 3 if variant == "pear" else 4
    tips = []
    for k in range(limbs):
        a = math.radians(k * 360 / limbs + rng.uniform(-30, 30))
        spread = (0.55 if variant == "pear" else 1.0) * rng.uniform(0.8, 1.1)
        rise = rng.uniform(0.6, 1.0) * tall
        mid = (top[0] + math.cos(a) * spread * 0.5, top[1] + math.sin(a) * spread * 0.5, top[2] + rise * 0.55)
        tip = (top[0] + math.cos(a) * spread, top[1] + math.sin(a) * spread, top[2] + rise)
        parts.append(part("Limb", kit.tube([top, mid, tip], [0.11, 0.07, 0.035], seg=5), bark, jitter=0.01))
        tips.append(tip)
    # The crown: many small clumps of leaves in a loose dome over the limbs (gaps between them, so it reads as foliage
    # and the limbs show through), the deep tone underneath and inside, the light ones on top.
    centre = (top[0], top[1], top[2] + (1.2 if variant == "pear" else 0.85) * tall)
    radius = (0.8 if variant == "pear" else 1.35) * (0.85 if variant == "apple_old" else 1.0)
    height = (1.5 if variant == "pear" else 0.95) * tall
    count = {"apple": 34, "apple_old": 18, "pear": 26, "flooded": 30}[variant]
    clumps = []
    for k in range(count):
        # Fibonacci-ish over the upper part of an ellipsoid shell, a little in from it.
        t = (k + 0.5) / count
        polar = math.acos(1 - t * 1.25)            # (the top and the sides; the underside left open)
        az = k * 2.39996 + rng.uniform(-0.2, 0.2)
        shell = rng.uniform(0.75, 1.0)
        c = (centre[0] + math.sin(polar) * math.cos(az) * radius * shell,
             centre[1] + math.sin(polar) * math.sin(az) * radius * shell,
             centre[2] + math.cos(polar) * height * 0.6 * shell)
        if variant == "apple_old" and math.cos(az) > 0.4:
            continue  # (the side the crown's gone thin)
        upper = math.cos(polar) > 0.35
        material = rng.choice(leaves[:3]) if upper or rng.random() < 0.5 else leaves[3]
        size = rng.uniform(0.26, 0.4) * (0.85 if variant == "pear" else 1.0)
        parts.append(part("Leaves", kit.ico(1, size), material, M(c, rot=(rng.uniform(0, 40), 0, rng.uniform(0, 360)), scale=(1.15, 1.0, 0.7)), jitter=0.03))
        clumps.append(c)
    # A few loose leaves about the crown, as if just let go.
    for k in range(10):
        c = rng.choice(clumps)
        at = (c[0] + rng.uniform(-0.5, 0.5), c[1] + rng.uniform(-0.5, 0.5), c[2] - rng.uniform(0.2, 0.9))
        s2 = rng.uniform(0.08, 0.13)
        parts.append(part("Leaf", kit.cube(), rng.choice(leaves[:3]), M(at, rot=(rng.uniform(0, 60), rng.uniform(0, 60), rng.uniform(0, 360)), scale=(s2, s2 * 0.6, 0.008))))
    # The fruit: hanging under the crown's edge, overripe, refusing to fall.
    for k in range(9 if variant != "apple_old" else 4):
        c = rng.choice([q for q in clumps if q[2] < centre[2] + 0.2] or clumps)
        at = (c[0] * 1.0, c[1] * 1.0, c[2] - 0.32)
        parts.append(part("Fruit", kit.ico(1, 0.075 if variant != "pear" else 0.065), fruit, M(at, scale=(1, 1, 1.0 if variant != "pear" else 1.3))))
    return [kit.join(parts, f"OrchardTree_{variant}")]
