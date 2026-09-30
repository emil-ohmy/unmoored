"""A stone half-wall railing along a tall edge, 1 m high, running east from its origin: "long" (2 m) and "short"
(0.8 m). It stops anyone walking off a drop too tall to take. Game directions (E, N, U)."""
M, part = kit.M, kit.part  # `kit` is injected by build_model.py

VARIANTS = ["long", "short"]


def box(name, mat, e0, e1, n0, n1, u0, u1):
    return part(name, kit.cube(), mat, M((-(e0 + e1) / 2, -(n0 + n1) / 2, (u0 + u1) / 2), scale=(e1 - e0, n1 - n0, u1 - u0)))


def build(variant):
    stone, dark = kit.mat("TowerStone", "#6f6a66"), kit.mat("TowerStoneDark", "#55504d")
    length = 2.0 if variant == "long" else 0.8
    parts = [box("Wall", stone, 0, length, -0.06, 0.06, 0, 0.9), box("Cap", dark, -0.02, length + 0.02, -0.09, 0.09, 0.9, 1.0)]
    return [kit.join(parts, f"Railing_{variant}")]
