"""Where the Hook is taken, in the slice: a squat stone pedestal with the Ferryman's old grappling hook lying on it
in a coil of chain. Two objects: RelicPedestal (stays) and RelicHook (hidden once taken). Origin at the base."""
M, part = kit.M, kit.part  # `kit` is injected by build_model.py


def build():
    stone, stone_dark = kit.mat("RelicStone", "#6f6b66"), kit.mat("RelicStoneDark", "#55514d")
    iron, chain = kit.mat("HookIron", "#3b3a42"), kit.mat("Chain", "#4a4850")
    pedestal = [
        part("Base", kit.cylinder(8, 0.45, 0.2), stone_dark, M((0, 0, 0.1))),
        part("Column", kit.cylinder(8, 0.3, 0.6), stone, M((0, 0, 0.5)), jitter=0.01),
        part("Top", kit.cylinder(8, 0.42, 0.12), stone_dark, M((0, 0, 0.86))),
    ]
    hook = [
        part("Shank", kit.cylinder(5, 0.03, 0.42), iron, M((0, 0, 0.98), rot=(0, 80, 20))),
        part("Prong", kit.tube([(0.2, 0.07, 0.98), (0.28, 0.12, 0.99), (0.3, 0.2, 1.02), (0.25, 0.26, 1.04)], [0.028, 0.024, 0.02, 0.012], seg=5), iron),
        part("Prong", kit.tube([(0.2, 0.07, 0.98), (0.3, 0.02, 0.99), (0.36, -0.06, 1.02), (0.34, -0.14, 1.04)], [0.028, 0.024, 0.02, 0.012], seg=5), iron),
        part("Eye", kit.tube([(-0.2, -0.07, 0.98), (-0.26, -0.03, 1.0), (-0.24, 0.03, 1.02), (-0.18, 0.02, 1.0)], [0.02] * 4, seg=5), iron),
    ]
    import math
    for i in range(12):  # a coil of chain beside it
        a = i * 0.9
        hook.append(part("Link", kit.cylinder(5, 0.035, 0.02), chain, M((-0.05 + 0.14 * math.cos(a), 0.12 + 0.14 * math.sin(a), 0.94 + 0.008 * i), rot=(90 * (i % 2), 0, a * 57))))
    return [kit.join(pedestal, "RelicPedestal"), kit.join(hook, "RelicHook")]
