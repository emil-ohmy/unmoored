"""Old Hesper, a Lingerer: a small, bent old soul in a pale shawl, sitting on an upturned crate at the landing's edge,
looking out over the water for her son, her hands empty in her lap. Beside her on the planks, the little lamp she keeps
burning for him (the game adds its light). Like every soul: no face in the hood, only two small warm eyes.
Front is -Y (where she looks); origin at her feet."""
M, part, along = kit.M, kit.part, kit.along  # `kit` is injected by build_model.py


def build():
    shawl = kit.mat("Shawl", "#9aa1b3")
    shawl_dark = kit.mat("ShawlShade", "#6e7486")
    skirt = kit.mat("Skirt", "#5f6576")
    void = kit.mat("Void", "#0b0a10")
    eyes = kit.mat("HesperEyes_Glow", "#ffcf7a")
    wood, wood_dark = kit.mat("CrateWood", "#6a513c"), kit.mat("CrateWoodDark", "#4a382b")
    metal = kit.mat("Metal", "#2c2b31")
    glass = kit.mat("HesperLamp_Glow", "#ffb866")
    parts = [
        # The crate she sits on, and her skirt falling over its edge.
        part("Crate", kit.cube(), wood, M((0, 0.05, 0.22), rot=(0, 0, 6), scale=(0.56, 0.46, 0.44))),
        part("CrateSlat", kit.cube(), wood_dark, M((0, -0.19, 0.22), rot=(0, 0, 6), scale=(0.58, 0.02, 0.06))),
        part("CrateSlat", kit.cube(), wood_dark, M((0, -0.19, 0.36), rot=(0, 0, 6), scale=(0.58, 0.02, 0.06))),
        part("Skirt", kit.cone(9, 0.34, 0.24, 0.42), skirt, M((0, -0.12, 0.42), rot=(24, 0, 0)), jitter=0.008),
        part("Knees", kit.sphere(8, 6), skirt, M((0, -0.3, 0.5), scale=(0.24, 0.18, 0.1)), smooth=True),
        # Bent forward, the shawl over her head and shoulders, a small deep hood with two warm eyes.
        part("Body", kit.sphere(9, 7), shawl, M((0, -0.02, 0.76), rot=(18, 0, 0), scale=(0.25, 0.22, 0.3)), smooth=True),
        part("Shawl", kit.cone(9, 0.3, 0.12, 0.34), shawl_dark, M((0, -0.04, 0.84), rot=(18, 0, 0)), jitter=0.01),
        part("Hood", kit.sphere(9, 7), shawl, M((0, -0.18, 0.98), rot=(28, 0, 0), scale=(0.15, 0.16, 0.16)), smooth=True),
        part("Face", kit.sphere(8, 6), void, M((0, -0.29, 0.95), scale=(0.09, 0.05, 0.1)), smooth=True),
        part("EyeL", kit.ico(1, 0.014), eyes, M((-0.034, -0.33, 0.97))),
        part("EyeR", kit.ico(1, 0.014), eyes, M((0.034, -0.33, 0.97))),
        # Her arms down to her hands, folded empty in her lap.
        part("ArmL", kit.tube([(-0.2, -0.04, 0.84), (-0.2, -0.18, 0.66), (-0.07, -0.3, 0.58)], [0.055, 0.05, 0.045]), shawl),
        part("ArmR", kit.tube([(0.2, -0.04, 0.84), (0.2, -0.18, 0.66), (0.07, -0.3, 0.58)], [0.055, 0.05, 0.045]), shawl),
        part("Hands", kit.sphere(7, 5), shawl_dark, M((0, -0.32, 0.58), scale=(0.1, 0.06, 0.05)), smooth=True),
        # Her lamp, on the planks beside her.
        part("LampBase", kit.cylinder(8, 0.07, 0.03), metal, M((0.42, -0.2, 0.015))),
        part("LampGlass", kit.cylinder(8, 0.05, 0.12), glass, M((0.42, -0.2, 0.09))),
        part("LampCap", kit.cone(8, 0.07, 0.02, 0.05), metal, M((0.42, -0.2, 0.175))),
        part("LampRing", kit.tube([(0.37, -0.2, 0.2), (0.42, -0.2, 0.25), (0.47, -0.2, 0.2)], [0.008, 0.008, 0.008], seg=4), metal),
    ]
    return [kit.join(parts, "Hesper")]
