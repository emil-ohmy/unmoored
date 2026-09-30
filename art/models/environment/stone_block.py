"""Raised stone blocks to stand on: "walkway" (6 x 3 m, 2.4 m high: a storey, level with the tower's first floor),
"low" (4 x 4 m, 1.4 m: low enough to drop off) and "high" (4 x 4 m, 2.8 m: too high, railed by the builder).
Coursed stone sides in the village's palette with a plinth, pale capstones round the top edge, flagstones on top.
Game directions (see masonry.py), origin at the centre of the base."""
import masonry as m

VARIANTS = ["walkway", "low", "high"]
SHAPES = {"walkway": (6.0, 3.0, 2.4), "low": (4.0, 4.0, 1.4), "high": (4.0, 4.0, 2.8)}


def build(variant):
    w, d, h = SHAPES[variant]
    e0, e1, n0, n1 = -w / 2, w / 2, -d / 2, d / 2
    parts = [m.box("Fill", m.mat(m.MORTAR), e0 + 0.2, e1 - 0.2, n0 + 0.2, n1 - 0.2, -0.6, h - 0.1)]
    # The sides (from below the quay's paving, so there's no gap), capped.
    m.wall(parts, e0, e1, n0, n0 + 0.3, -0.3, h, course=0.55, plinth=True)
    m.wall(parts, e0, e1, n1 - 0.3, n1, -0.3, h, course=0.55, plinth=True)
    m.wall(parts, e0, e0 + 0.3, n0 + 0.3, n1 - 0.3, -0.3, h, course=0.55, plinth=True)
    m.wall(parts, e1 - 0.3, e1, n0 + 0.3, n1 - 0.3, -0.3, h, course=0.55, plinth=True)
    m.flagstones(parts, e0 + 0.3, e1 - 0.3, n0 + 0.3, n1 - 0.3, h + 0.005)
    return [m.kit.join(parts, f"StoneBlock_{variant}")]
