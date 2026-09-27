"""Rocks: faceted, jittered icospheres; upward faces get moss. Sits half under the waterline (origin at z=0)."""
M, part = kit.M, kit.part  # `kit` is injected by build_model.py

VARIANTS = ["a", "b", "c"]
SHAPES = {"a": (0.22, (0.9, 0.75, 0.55)), "b": (0.25, (0.6, 0.55, 0.5)), "c": (0.2, (1.4, 1.0, 0.6))}


def build(variant):
    jitter, scale = SHAPES[variant]
    rng = kit.rng

    def shape(bm):
        kit.ico(1)(bm)
        for v in bm.verts:
            v.co += v.co.normalized() * rng.uniform(-jitter, jitter)

    ob = part(f"Rock_{variant}", shape, kit.mat("Rock", "#80838c"), M(scale=scale))
    kit.top_material(ob, kit.mat("Moss", "#8a9868"))
    return [ob]
