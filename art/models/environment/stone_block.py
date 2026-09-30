"""Raised stone blocks to stand on: "walkway" (6 x 3 m, 2.4 m high: a storey, level with the tower's first floor),
"low" (4 x 4 m, 1.4 m: low enough to drop off) and "high" (4 x 4 m, 2.8 m: too high to drop off, railed by the
builder). Paved on top, with coursed stone sides. Game directions (E, N, U), origin at the centre of the base."""
M, part = kit.M, kit.part  # `kit` is injected by build_model.py

VARIANTS = ["walkway", "low", "high"]
SHAPES = {"walkway": (6.0, 3.0, 2.4), "low": (4.0, 4.0, 1.4), "high": (4.0, 4.0, 2.8)}


def box(name, mat, e0, e1, n0, n1, u0, u1):
    return part(name, kit.cube(), mat, M((-(e0 + e1) / 2, -(n0 + n1) / 2, (u0 + u1) / 2), scale=(e1 - e0, n1 - n0, u1 - u0)))


def build(variant):
    stone, dark = kit.mat("TowerStone", "#6f6a66"), kit.mat("TowerStoneDark", "#55504d")
    paving = kit.mat("TowerFloor", "#5b5048")
    w, d, h = SHAPES[variant]
    parts = [box("Core", stone, -w / 2, w / 2, -d / 2, d / 2, -0.6, h - 0.08)]
    # Courses: a darker band every 0.6 m, standing a hair proud of the face.
    z = 0.3
    while z < h - 0.2:
        parts.append(box("Course", dark, -w / 2 - 0.02, w / 2 + 0.02, -d / 2 - 0.02, d / 2 + 0.02, z, z + 0.06))
        z += 0.6
    # Paving slabs on top, a little uneven.
    rng = kit.rng
    e = -w / 2
    while e < w / 2 - 0.01:
        n = -d / 2
        while n < d / 2 - 0.01:
            parts.append(box("Slab", paving if rng.random() > 0.3 else dark, e + 0.02, min(e + 1.0, w / 2) - 0.02,
                             n + 0.02, min(n + 1.0, d / 2) - 0.02, h - 0.08, h + rng.uniform(-0.01, 0.01)))
            n += 1.0
        e += 1.0
    return [kit.join(parts, f"StoneBlock_{variant}")]
