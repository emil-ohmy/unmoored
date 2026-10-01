"""The Orchard's terraces, stepping up a hillside out of the water: dry-stone retaining walls (pale sandy stone, laid
without mortar, capped) round a body of earth, turf and fallen leaves on top. "ground" (the lowest, 32 x 10 m, its top
at 0.5 m like a quay: its south face stands in the water), "middle" (32 x 8, top 1.9: a 1.4 m step up) and "upper"
(32 x 8, top 3.3). Each drop is 1.4 m: low enough to jump down, one way. Game directions (see masonry.py), origin at
the centre of its south edge, at the water's surface."""
import masonry as m

VARIANTS = ["ground", "middle", "upper"]
SHAPES = {"ground": (32.0, 10.0, 0.5), "middle": (32.0, 8.0, 1.9), "upper": (32.0, 8.0, 3.3)}


def build(variant):
    m.orchard()
    w, d, top = SHAPES[variant]
    e0, e1, n0, n1 = -w / 2, w / 2, 0.0, d
    bottom = -0.9
    parts = [m.box("Earth", m.mat(m.MORTAR), e0 + 0.3, e1 - 0.3, n0 + 0.3, n1 - 0.3, bottom, top - 0.1)]
    t = 0.45  # the walls' thickness: dry-stone, chunky
    m.wall(parts, e0, e1, n0, n0 + t, bottom, top, course=0.45, plinth=variant == "ground")   # the face (south)
    m.wall(parts, e0, e0 + t, n0 + t, n1, bottom, top, course=0.45)                            # the ends
    m.wall(parts, e1 - t, e1, n0 + t, n1, bottom, top, course=0.45)
    m.wall(parts, e0 + t, e1 - t, n1 - t, n1, bottom, top, course=0.45, cap=False)               # the back (under the next)
    m.turf(parts, e0 + t, e1 - t, n0 + t, n1 - t, top + 0.02, density=6.5)
    return [m.kit.join(parts, f"Terrace_{variant}")]
