"""The Drowned City quay: an irregular stone platform (top at z=0.5, sides down into the water),
paved with slightly raised slabs and edged with curb stones so the water's edge reads clearly."""
import math

M, part = kit.M, kit.part  # `kit` is injected by build_model.py

TOP = 0.5
OUTLINE = [(-8.0, -5.0), (-3.0, -5.6), (1.5, -5.2), (6.5, -5.8), (8.2, -2.5), (7.6, 1.5), (8.4, 4.6),
           (4.0, 5.6), (-1.0, 5.0), (-5.5, 5.8), (-8.4, 3.0), (-7.6, -1.0)]


def inside(x, y, pts):
    hit = False
    for (x1, y1), (x2, y2) in zip(pts, pts[1:] + pts[:1]):
        if (y1 > y) != (y2 > y) and x < x1 + (y - y1) * (x2 - x1) / (y2 - y1):
            hit = not hit
    return hit


def build():
    rng = kit.rng
    side, curb = kit.mat("QuayStone", "#625e5b"), kit.mat("CurbStone", "#77726c")
    slabs = [kit.mat("Paving1", "#8e887d"), kit.mat("Paving2", "#847d72"), kit.mat("Paving3", "#98917f")]
    outline = [(x + rng.uniform(-0.2, 0.2), y + rng.uniform(-0.2, 0.2)) for x, y in OUTLINE]

    parts = [part("Base", kit.prism(outline, TOP + 1.5), side, M((0, 0, -1.5)))]
    # Paving: a jittered grid of slabs inside the outline, kept clear of the curb.
    y = -5.4
    while y < 5.8:
        x = -8.4 + (0.45 if round(y / 0.95) % 2 else 0)
        while x < 8.6:
            w, d = rng.uniform(0.75, 1.0), rng.uniform(0.7, 0.85)
            corners = [(x + dx, y + dy) for dx in (-0.6, w + 0.6) for dy in (-0.6, d + 0.6)]
            if all(inside(cx, cy, outline) for cx, cy in corners):
                parts.append(part("Slab", kit.cube(), rng.choice(slabs),
                                  M((x + w / 2, y + d / 2, TOP + 0.015), rot=(0, 0, rng.uniform(-3, 3)), scale=(w - 0.06, d - 0.06, 0.05))))
            x += w
        y += 0.95
    # Curb stones along every edge.
    for (x1, y1), (x2, y2) in zip(outline, outline[1:] + outline[:1]):
        length = math.hypot(x2 - x1, y2 - y1)
        n = max(1, round(length / 0.9))
        angle = math.degrees(math.atan2(y2 - y1, x2 - x1))
        for i in range(n):
            t = (i + 0.5) / n
            parts.append(part("Curb", kit.cube(), curb,
                              M((x1 + (x2 - x1) * t, y1 + (y2 - y1) * t, TOP + 0.06), rot=(0, 0, angle + rng.uniform(-3, 3)),
                                scale=(length / n - 0.05, 0.42, 0.16 + rng.uniform(-0.02, 0.03)))))
    return [kit.join(parts, "Quay")]
