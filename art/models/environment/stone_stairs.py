"""A flight of stone steps, rising towards the game's north from its origin (the foot of the first step), 1.6 m
wide: "storey" (2.4 m: 12 steps) and "low" (1.4 m: 7 steps). Solid underneath. Game directions (E, N, U)."""
M, part = kit.M, kit.part  # `kit` is injected by build_model.py

VARIANTS = ["storey", "low"]
RISE, RUN, WIDTH = 0.2, 0.333, 1.6


def box(name, mat, e0, e1, n0, n1, u0, u1):
    return part(name, kit.cube(), mat, M((-(e0 + e1) / 2, -(n0 + n1) / 2, (u0 + u1) / 2), scale=(e1 - e0, n1 - n0, u1 - u0)))


def build(variant):
    stone, dark = kit.mat("TowerStone", "#6f6a66"), kit.mat("TowerStoneDark", "#55504d")
    steps = 12 if variant == "storey" else 7
    parts = []
    for i in range(steps):
        parts.append(box("Step", dark if i % 2 else stone, -WIDTH / 2, WIDTH / 2, i * RUN, (i + 1) * RUN, -0.4, (i + 1) * RISE))
    return [kit.join(parts, f"StoneStairs_{variant}")]
