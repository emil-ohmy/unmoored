"""The merchant soul: a hunched, hooded soul in a pale robe, bent under a great pack of wares (a crate, a bedroll,
pots and trinkets), leaning on a hooked staff from which a small warm lantern hangs. The game adds its light and a
slow sway. Front is -Y; origin at its feet."""
M, part, along = kit.M, kit.part, kit.along  # `kit` is injected by build_model.py


def build():
    robe = kit.mat("Robe", "#8e97aa")
    robe_dark = kit.mat("RobeShade", "#646b7d")
    void = kit.mat("Void", "#0b0a10")
    eyes = kit.mat("MerchantEyes_Glow", "#ffcf7a")
    wood = kit.mat("Wood", "#5a4636")
    cloth = kit.mat("Cloth", "#7b6a55")
    metal = kit.mat("Metal", "#2c2b31")
    glass = kit.mat("MerchantLantern_Glow", "#ffb866")
    parts = [
        # Body: a robe tipping forward under the load, a hunched back, a deep hood with only two warm eyes inside.
        part("Robe", kit.cone(9, 0.42, 0.2, 1.0), robe, M((0, -0.04, 0.5), rot=(12, 0, 0)), jitter=0.01),
        part("Hem", kit.cone(9, 0.45, 0.42, 0.08), robe_dark, M((0, 0, 0.04))),
        part("Hunch", kit.sphere(9, 7), robe, M((0, -0.1, 1.0), scale=(0.3, 0.28, 0.22)), smooth=True),
        part("Hood", kit.sphere(9, 7), robe_dark, M((0, -0.27, 1.1), rot=(20, 0, 0), scale=(0.2, 0.22, 0.22)), smooth=True),
        part("Face", kit.sphere(8, 6), void, M((0, -0.42, 1.08), scale=(0.12, 0.06, 0.13)), smooth=True),
        part("EyeL", kit.ico(1, 0.018), eyes, M((-0.045, -0.47, 1.11))),
        part("EyeR", kit.ico(1, 0.018), eyes, M((0.045, -0.47, 1.11))),
        part("Arm", kit.tube([(0.2, -0.16, 1.02), (0.3, -0.3, 0.92), (0.35, -0.42, 0.98)], [0.07, 0.06, 0.05]), robe),
        # The pack.
        part("Crate", kit.cube(), wood, M((0, 0.27, 1.0), rot=(-8, 0, 0), scale=(0.5, 0.34, 0.56))),
        part("CrateBand", kit.cube(), metal, M((0, 0.27, 1.0), rot=(-8, 0, 0), scale=(0.52, 0.36, 0.04))),
        part("Bedroll", kit.cylinder(8, 0.1, 0.6), cloth, M((0, 0.32, 1.36), rot=(0, 90, 0))),
        part("PotL", kit.cylinder(7, 0.07, 0.12), metal, M((-0.3, 0.3, 0.78))),
        part("PotR", kit.cylinder(7, 0.06, 0.1), metal, M((0.3, 0.26, 0.74))),
        part("Bundle", kit.sphere(7, 5), cloth, M((0.18, 0.44, 0.82), scale=(0.13, 0.11, 0.12)), smooth=True),
        part("StrapL", kit.cube(), cloth, M((-0.15, 0.02, 1.02), rot=(-30, 0, 0), scale=(0.04, 0.02, 0.4))),
        part("StrapR", kit.cube(), cloth, M((0.15, 0.02, 1.02), rot=(-30, 0, 0), scale=(0.04, 0.02, 0.4))),
        # The staff, its hook, and the lantern hanging from it.
        part("Staff", kit.cylinder(5, 0.022, 1.66), wood, along((0.34, -0.36, 0.0), (0.37, -0.44, 1.64))),
        part("Hook", kit.tube([(0.37, -0.44, 1.62), (0.37, -0.56, 1.7), (0.37, -0.66, 1.64), (0.37, -0.66, 1.54)],
                              [0.02, 0.018, 0.016, 0.014]), metal),
        part("LanternTop", kit.cone(4, 0.07, 0.01, 0.06), metal, M((0.37, -0.66, 1.47), rot=(0, 0, 45))),
        part("LanternGlass", kit.cube(), glass, M((0.37, -0.66, 1.38), scale=(0.09, 0.09, 0.12))),
        part("LanternBase", kit.cube(), metal, M((0.37, -0.66, 1.31), scale=(0.11, 0.11, 0.02))),
    ]
    return [kit.join(parts, "MerchantSoul")]
