"""A flight of dry-stone steps up a terrace's face (1.4 m: 7 steps), rising towards the game's north from its origin
(the foot of the first step), 2 m wide (room for a Laden); worn treads strewn with a few leaves, rough cheeks either
side. Game directions (see masonry.py)."""
import masonry as m

RISE, RUN, WIDTH, STEPS = 0.2, 0.333, 2.0, 7


def build():
    m.orchard()
    rng = m.kit.rng
    parts = []
    stones = [m.mat(s) for s in m.STONE]
    cap = m.mat(m.CAP)
    leaves = [m.mat(l) for l in m.LEAVES]
    for i in range(STEPS):
        top = (i + 1) * RISE
        m.step(parts, -WIDTH / 2, WIDTH / 2, i * RUN, (i + 1) * RUN, -0.9, top, tone=i)
        for e0, e1 in ((-WIDTH / 2 - 0.3, -WIDTH / 2), (WIDTH / 2, WIDTH / 2 + 0.3)):
            parts.append(m.box("Cheek", stones[(i + 1) % 3], e0, e1, i * RUN, (i + 1) * RUN, -0.9, top + 0.15))
            parts.append(m.box("CheekCap", cap, e0 - 0.01, e1 + 0.01, i * RUN, (i + 1) * RUN, top + 0.15, top + 0.22))
        for _ in range(3):
            e, n = rng.uniform(-0.9, 0.9), rng.uniform(i * RUN + 0.03, (i + 1) * RUN - 0.03)
            s = rng.uniform(0.09, 0.14)
            parts.append(m.part("Leaf", m.kit.cube(), rng.choice(leaves), m.M((-e, -n, top + 0.006 + rng.uniform(0, 0.006)), rot=(0, 0, rng.uniform(0, 360)), scale=(s, s * 0.6, 0.008))))
    return [m.kit.join(parts, "OrchardSteps")]
