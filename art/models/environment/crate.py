"""Wooden crate with plank edges."""
M, part = kit.M, kit.part  # `kit` is injected by build_model.py


def build():
    wood, edge = kit.mat("CrateWood", "#8a6a48"), kit.mat("CrateEdge", "#5f4731")
    s = 0.7
    parts = [part("Box", kit.cube(), wood, M((0, 0, s / 2), scale=(s, s, s)))]
    for dx in (-1, 1):
        for dy in (-1, 1):
            parts.append(part("Edge", kit.cube(), edge, M((dx * s / 2, dy * s / 2, s / 2), scale=(0.08, 0.08, s + 0.02))))
    for z in (0.02, s - 0.02):
        for dy in (-1, 1):
            parts.append(part("Rim", kit.cube(), edge, M((0, dy * s / 2, z), scale=(s + 0.02, 0.08, 0.08))))
    return [kit.join(parts, "Crate")]
