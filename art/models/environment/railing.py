"""A stone balustrade along a tall edge, 1 m high, running east from its origin: "long" (2 m) and "short" (0.8 m):
square posts, a low plinth, a capped rail. It stops anyone walking off a drop too tall to take."""
import masonry as m

VARIANTS = ["long", "short"]


def build(variant):
    length = 2.0 if variant == "long" else 0.8
    parts = []
    m.balustrade(parts, 0, 0, length, 0, 0)
    return [m.kit.join(parts, f"Railing_{variant}")]
