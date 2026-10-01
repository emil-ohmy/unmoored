"""The orchard's walls: dry-stone of pale sandy stone, 1.9 m high (a little over head height: you see over it only
from a terrace above), capped with rounded cope stones, a few leaves caught along the top. "long" (4 m, running east
from its origin), "gate" (4 m, with an arched gateway 2.2 m wide in the middle and its old plank gate standing open)
and "end" (a squared pillar). Game directions (see masonry.py), origin at the west end of its centre line."""
import math
import masonry as m

VARIANTS = ["long", "gate", "end"]
H, T = 1.9, 0.5


def cope(parts, e0, e1):
    rng = m.kit.rng
    capstone = m.mat(m.CAP)
    e = e0
    while e < e1 - 0.02:
        hi = min(e + rng.uniform(0.35, 0.6), e1)
        parts.append(m.part("Cope", m.kit.cylinder(6, T * 0.55, hi - e - 0.02), capstone,
                            m.M((-(e + hi) / 2, 0, H + 0.02), rot=(0, 90, 0), scale=(0.55, 1, 1))))
        e = hi


def build(variant):
    m.orchard()
    rng = m.kit.rng
    parts = []
    if variant == "end":
        m.wall(parts, -0.35, 0.35, -0.35, 0.35, -0.3, H + 0.35, course=0.4)
        return [m.kit.join(parts, "OrchardWall_end")]
    length = 4.0
    if variant == "long":
        m.wall(parts, 0, length, -T / 2, T / 2, -0.3, H, course=0.42, cap=False)
        cope(parts, 0, length)
    else:
        g0, g1, spring = 0.9, 3.1, 1.75  # the gateway, and where its arch springs
        m.wall(parts, 0, length, -T / 2, T / 2, -0.3, H + 0.55, openings=[(g0, g1, -0.3, spring + 0.75)], course=0.42, cap=False)
        cope(parts, 0, g0)
        cope(parts, g1, length)
        # The arch: voussoirs round from spring to spring.
        stones = [m.mat(s) for s in m.STONE]
        cx, r = (g0 + g1) / 2, (g1 - g0) / 2
        for k in range(9):
            a = math.pi * (k + 0.5) / 9
            e, u = cx - math.cos(a) * (r + 0.12), spring + math.sin(a) * (r * 0.62 + 0.12)
            parts.append(m.part("Voussoir", m.kit.cube(), stones[k % 3], m.M((-e, 0, u), rot=(0, 90 - math.degrees(a), 0), scale=(0.22, T + 0.06, 0.3))))
        parts.append(m.box("Keystone", m.mat(m.CAP), cx - 0.13, cx + 0.13, -T / 2 - 0.05, T / 2 + 0.05, spring + r * 0.62 + 0.05, spring + r * 0.62 + 0.42))
        # The gate, one leaf, hung on the west jamb and standing open into the far side (north).
        wood = m.kit.mat("GateWood", "#5a4434")
        hinge = (g0 + 0.05, T / 2 + 0.05)
        for k in range(5):
            along = 0.08 + k * 0.21
            e, n = hinge[0] + math.cos(math.radians(80)) * along, hinge[1] + math.sin(math.radians(80)) * along
            parts.append(m.part("GatePlank", m.kit.cube(), wood, m.M((-e, -n, 0.95), rot=(0, 0, -80), scale=(0.19, 0.05, 1.7))))
        parts.append(m.part("GateBrace", m.kit.cube(), wood, m.M((-(hinge[0] + 0.09), -(hinge[1] + 0.5), 0.95), rot=(0, 52, -80), scale=(1.3, 0.06, 0.08))))
    # A few leaves caught along the top.
    leaves = [m.mat(l) for l in m.LEAVES]
    for _ in range(10):
        e = rng.uniform(0.1, length - 0.1)
        s = rng.uniform(0.1, 0.16)
        parts.append(m.part("Leaf", m.kit.cube(), rng.choice(leaves), m.M((-e, rng.uniform(-0.15, 0.15), H + 0.27), rot=(rng.uniform(-20, 20), 0, rng.uniform(0, 360)), scale=(s, s * 0.6, 0.008))))
    return [m.kit.join(parts, f"OrchardWall_{variant}")]
