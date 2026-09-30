"""A stone balustrade along a tall edge, 1 m high, running east from its origin: "long" (2 m), "short" (0.8 m) and "stub" (0.45 m):
square posts, a low plinth, a capped rail. It stops anyone walking off a drop too tall to take."""
import masonry as m

VARIANTS = ["long", "short", "stub"]


def build(variant):
    length = {"long": 2.0, "short": 0.8, "stub": 0.45}[variant]
    parts = []
    m.balustrade(parts, 0, 0, length, 0, 0)
    return [m.kit.join(parts, f"Railing_{variant}")]
