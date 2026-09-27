"""Barrel with iron hoops."""
M, part = kit.M, kit.part  # `kit` is injected by build_model.py


def build():
    wood, hoop = kit.mat("BarrelWood", "#7d5a3c"), kit.mat("HeapMetal", "#34313a")
    parts = [part("Body", kit.cone(10, 0.3, 0.3, 0.8), wood, M((0, 0, 0.4))),
             part("Bulge", kit.cone(10, 0.34, 0.34, 0.3), wood, M((0, 0, 0.4))),
             part("Top", kit.cylinder(10, 0.27, 0.02), kit.mat("BarrelTop", "#5e4430"), M((0, 0, 0.8)))]
    for z in (0.12, 0.68):
        parts.append(part("Hoop", kit.cylinder(10, 0.315, 0.05), hoop, M((0, 0, z))))
    return [kit.join(parts, "Barrel")]
