"""Standing torch: a wooden pole in a stone foot, an iron basket and a layered low-poly flame.
The flame materials end in _Glow; the game adds a flickering light and embers."""
M, part = kit.M, kit.part  # `kit` is injected by build_model.py


def build():
    wood, metal, stone = kit.mat("TorchWood", "#4a3526"), kit.mat("Metal", "#2c2b31"), kit.mat("QuayStone", "#625e5b")
    flame, core = kit.mat("Torch_Glow", "#ff4a10"), kit.mat("TorchCore_Glow", "#ff9a30")
    parts = [
        part("Foot", kit.cylinder(6, 0.2, 0.14), stone, M((0, 0, 0.07))),
        part("Pole", kit.cone(6, 0.055, 0.045, 1.55), wood, M((0, 0, 0.9))),
        part("Basket", kit.cone(6, 0.09, 0.19, 0.22), metal, M((0, 0, 1.72))),
        part("Flame", kit.cone(6, 0.16, 0.0, 0.42), flame, M((0, 0, 1.98), rot=(0, 0, 15)), jitter=0.015),
        part("FlameTongue", kit.cone(5, 0.08, 0.0, 0.3), flame, M((0.06, 0.03, 2.08), rot=(10, 12, 0))),
        part("FlameCore", kit.cone(6, 0.09, 0.0, 0.3), core, M((0, 0, 1.94))),
    ]
    return [kit.join(parts, "Torch")]
