"""A hook post: a weathered pile driven into a platform's edge, topped with a big iron ring for the Hook to catch,
lashed with rope. Origin at its base."""
M, part = kit.M, kit.part  # `kit` is injected by build_model.py


def build():
    wood, iron, rope = kit.mat("PileWood", "#4f3f33"), kit.mat("HookIron", "#3b3a42"), kit.mat("Rope", "#8f7d5e")
    parts = [
        part("Pile", kit.cylinder(7, 0.16, 1.7), wood, M((0, 0, 0.85), rot=(2, -3, 0)), jitter=0.01),
        part("Cap", kit.cone(7, 0.17, 0.12, 0.08), wood, M((0, 0, 1.74))),
        part("Lashing", kit.cylinder(7, 0.18, 0.12), rope, M((0, 0, 1.45))),
        part("Ring", kit.tube([(0.0, -0.2 * c, 1.95 + 0.2 * s) for c, s in
                               [(1, 0), (0.7, 0.7), (0, 1), (-0.7, 0.7), (-1, 0), (-0.7, -0.7), (0, -1), (0.7, -0.7), (1, 0)]],
                              [0.03] * 9, seg=5), iron),
        part("RingMount", kit.cube(), iron, M((0, 0, 1.8), scale=(0.08, 0.08, 0.14))),
    ]
    return [kit.join(parts, "HookPost")]
