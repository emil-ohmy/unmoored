"""Ruined stone wall segment with a broken top: cover on the quay. Runs along X, centred, base at z=0."""
M, part = kit.M, kit.part  # `kit` is injected by build_model.py

VARIANTS = ["short", "long"]
LENGTHS = {"short": 2.2, "long": 3.8}


def build(variant):
    rng = kit.rng
    stones = [kit.mat("WallStone1", "#6f6b66"), kit.mat("WallStone2", "#7b766f"), kit.mat("WallStone3", "#65615d")]
    length = LENGTHS[variant]
    parts = []
    x = -length / 2
    column = 0
    while x < length / 2 - 0.1:
        w = min(rng.uniform(0.45, 0.7), length / 2 - x)
        top = 1.3 - abs((x + w / 2) / length) * 0.9 + rng.uniform(-0.25, 0.1)  # tallest in the middle, broken ends
        z, row = 0.0, 0
        while z < top:
            h = min(rng.uniform(0.28, 0.36), top - z)
            if h < 0.1:
                break
            offset = 0.12 if (row + column) % 2 else 0.0
            parts.append(part("Stone", kit.cube(), rng.choice(stones),
                              M((x + w / 2 + offset * 0.2, rng.uniform(-0.03, 0.03), z + h / 2), rot=(0, 0, rng.uniform(-3, 3)),
                                scale=(w - 0.04, 0.42 + rng.uniform(-0.04, 0.04), h - 0.03))))
            z += h
            row += 1
        x += w
        column += 1
    return [kit.join(parts, f"RuinedWall_{variant}")]
