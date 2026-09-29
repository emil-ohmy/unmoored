"""A Healing charge piece: an ember glowing in a small iron dish on a short wooden post, found behind a secret.
Origin at the base."""
M, part = kit.M, kit.part  # `kit` is injected by build_model.py


def build():
    wood, iron, ember = kit.mat("PileWood", "#4f3f33"), kit.mat("HookIron", "#3b3a42"), kit.mat("ChargeEmber_Glow", "#ffab52")
    parts = [
        part("Post", kit.cylinder(6, 0.07, 0.8), wood, M((0, 0, 0.4))),
        part("Dish", kit.cone(8, 0.08, 0.2, 0.1), iron, M((0, 0, 0.85))),
        part("Ember", kit.ico(1, 0.1), ember, M((0, 0, 0.94), scale=(1, 1, 0.8))),
    ]
    return [kit.join(parts, "ChargeEmber")]
