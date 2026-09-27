"""Islets: big flattened jittered icospheres; the top gets moss (small) or autumn earth (big)."""
M, part = kit.M, kit.part  # `kit` is injected by build_model.py

VARIANTS = ["small", "big"]
SHAPES = {"small": (0.14, (2.2, 1.8, 1.0), ("Moss", "#8a9868")), "big": (0.12, (3.4, 2.9, 1.1), ("Earth", "#a67c45"))}


def build(variant):
    jitter, scale, top = SHAPES[variant]
    rng = kit.rng

    def shape(bm):
        kit.ico(2)(bm)
        for v in bm.verts:
            v.co += v.co.normalized() * rng.uniform(-jitter, jitter)

    ob = part(f"Islet_{variant}", shape, kit.mat("Rock", "#80838c"), M(scale=scale))
    kit.top_material(ob, kit.mat(*top))
    return [ob]
