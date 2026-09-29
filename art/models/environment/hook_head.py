"""The Hook's head in flight: the same grappling hook the Apprentice carries (sized the same), flung along the tether to
a hook post's ring. The game spawns it for each throw; origin at the shank's eye (where the chain ties on)."""
M, part = kit.M, kit.part  # `kit` is injected by build_model.py


def build():
    iron = kit.mat("HookIron", "#3b3a42")
    k = 1.5

    def at(dx, dy, dz):
        return (dx * k, dy * k, dz * k)

    parts = [
        part("HookShank", kit.cylinder(5, 0.024 * k, 0.24 * k), iron, M(at(0, -0.12, 0), rot=(90, 0, 0))),
        part("HookProngL", kit.tube([at(0, -0.24, 0), at(-0.07, -0.26, 0), at(-0.1, -0.2, 0)], [0.022 * k, 0.017 * k, 0.01 * k], seg=5), iron),
        part("HookProngR", kit.tube([at(0, -0.24, 0), at(0.07, -0.26, 0), at(0.1, -0.2, 0)], [0.022 * k, 0.017 * k, 0.01 * k], seg=5), iron),
        part("HookProngB", kit.tube([at(0, -0.24, 0), at(0, -0.26, 0.07), at(0, -0.2, 0.1)], [0.022 * k, 0.017 * k, 0.01 * k], seg=5), iron),
        part("HookEye", kit.cylinder(6, 0.032 * k, 0.02 * k), iron, M(at(0, 0.0, 0))),
    ]
    return [kit.join(parts, "HookHead")]
