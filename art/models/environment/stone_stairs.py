"""A flight of stone steps, rising towards the game's north from its origin (the foot of the first step), 1.6 m
wide: "storey" (2.4 m: 12 steps) and "low" (1.4 m: 7 steps). Each step a block with a pale worn tread edge; coursed
cheeks either side. Game directions (see masonry.py)."""
import masonry as m

VARIANTS = ["storey", "low"]
RISE, RUN, WIDTH = 0.2, 0.333, 1.6


def build(variant):
    steps = 12 if variant == "storey" else 7
    parts = []
    stones = [m.mat(s) for s in m.STONE]
    cap = m.mat(m.CAP)
    for i in range(steps):
        top = (i + 1) * RISE
        m.step(parts, -WIDTH / 2, WIDTH / 2, i * RUN, (i + 1) * RUN, -0.4, top, tone=i)
        # A low cheek either side, following the steps.
        for e0, e1 in ((-WIDTH / 2 - 0.18, -WIDTH / 2), (WIDTH / 2, WIDTH / 2 + 0.18)):
            parts.append(m.box("Cheek", stones[(i + 1) % 3], e0, e1, i * RUN, (i + 1) * RUN, -0.4, top + 0.12))
            parts.append(m.box("CheekCap", cap, e0 - 0.01, e1 + 0.01, i * RUN, (i + 1) * RUN, top + 0.12, top + 0.18))
    return [m.kit.join(parts, f"StoneStairs_{variant}")]
