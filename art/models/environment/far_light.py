"""The Far shore's light, where the dead go on: a tall, weathered post at the end of the jetty with a crossbar, the
Ferryman's mark (an oar through a ring) nailed to it, and a pale lantern hung from the bar that has never been lit
by anyone (the game gives it a soft, warm-white light). A few stones heaped at its foot. Origin at its foot."""
import math

M, part = kit.M, kit.part  # `kit` is injected by build_model.py


def build():
    wood, dark = kit.mat("PostWood", "#5a4a3d"), kit.mat("PostDark", "#3e332b")
    iron, stone = kit.mat("Iron", "#2c2b31"), kit.mat("Stone", "#5d5f66")
    glow = kit.mat("FarLight_Glow", "#fff1d6")
    rng = kit.rng
    parts = [
        part("Post", kit.cylinder(7, 0.09, 2.6), wood, M((0, 0, 1.3), rot=(1.5, -2, 0)), jitter=0.01),
        part("Bar", kit.cube(), dark, M((0.3, 0, 2.42), rot=(0, -3, 0), scale=(0.9, 0.09, 0.08))),
        part("Chain", kit.cube(), iron, M((0.62, 0, 2.24), scale=(0.015, 0.015, 0.3))),
        part("LampTop", kit.cone(6, 0.13, 0.02, 0.11), iron, M((0.62, 0, 2.04))),
        part("LampGlass", kit.cylinder(6, 0.1, 0.24), glow, M((0.62, 0, 1.86))),
        part("LampBase", kit.cylinder(6, 0.12, 0.03), iron, M((0.62, 0, 1.73))),
        # The Ferryman's mark: a ring with an oar through it.
        part("MarkRing", kit.tube([(0.16 * math.cos(a), -0.1, 1.6 + 0.16 * math.sin(a)) for a in [i * math.pi / 5 for i in range(11)]],
                                  [0.018] * 11, seg=5), iron),
        part("MarkOar", kit.cube(), dark, M((0, -0.11, 1.6), rot=(0, 35, 0), scale=(0.04, 0.02, 0.5))),
    ]
    for i in range(6):  # stones heaped at its foot
        a = i * math.pi / 3 + rng.uniform(-0.3, 0.3)
        r = rng.uniform(0.18, 0.3)
        parts.append(part("Stone", kit.ico(1, rng.uniform(0.08, 0.13)), stone, M((r * math.cos(a), r * math.sin(a), 0.05)), jitter=0.02))
    return [kit.join(parts, "FarLight")]
