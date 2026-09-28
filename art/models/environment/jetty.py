"""Wooden jetty pieces: planks, beams and posts, running from the origin along +Y (the Mooring post is its own model).
Variants: "walk", a 1.7 m wide boardwalk segment (4.4 m long); "landing", a 3.2 m square platform."""
M, part = kit.M, kit.part  # `kit` is injected by build_model.py

VARIANTS = ["walk", "landing"]
SHAPES = {"walk": (1.7, 14), "landing": (3.2, 10)}  # width, plank count
STEP = 0.34


def build(variant):
    rng = kit.rng
    width, count = SHAPES[variant]
    planks = [kit.mat("Plank1", "#6e5746"), kit.mat("Plank2", "#5f4b3d"), kit.mat("Plank3", "#7a6250")]
    beam = kit.mat("Beam", "#46382e")
    length = (count - 1) * STEP
    beam_x = width / 2 - 0.23
    parts = []
    for i in range(count):
        parts.append(part(f"Plank{i}", kit.cube(), planks[i % 3],
                          M((rng.uniform(-0.04, 0.04), i * STEP, rng.uniform(-0.012, 0.012) - 0.03),
                            rot=(0, rng.uniform(-1.5, 1.5), rng.uniform(-2.5, 2.5)),
                            scale=(width + rng.uniform(-0.1, 0.1), 0.29, 0.06))))
    for x in (-beam_x, 0.0, beam_x) if variant == "landing" else (-beam_x, beam_x):
        parts.append(part("Beam", kit.cube(), beam, M((x, length / 2, -0.11), scale=(0.12, length + 0.3, 0.1))))
    for x in (-beam_x * 1.16, beam_x * 1.16):
        for y in (0.0, length / 3, 2 * length / 3, length) if variant == "walk" else (0.0, length):
            parts.append(part("Post", kit.cylinder(6, 0.09, 1.5), beam,
                              M((x, y, -0.58), rot=(rng.uniform(-3, 3), rng.uniform(-3, 3), 0))))
    return [kit.join(parts, f"Jetty_{variant}")]
