"""Wooden jetty: planks, beams and posts. Runs from the origin along +Y (the Mooring post is its own model)."""
M, part = kit.M, kit.part  # `kit` is injected by build_model.py

COUNT, STEP = 14, 0.34


def build():
    rng = kit.rng
    planks = [kit.mat("Plank1", "#6e5746"), kit.mat("Plank2", "#5f4b3d"), kit.mat("Plank3", "#7a6250")]
    beam = kit.mat("Beam", "#46382e")
    length = (COUNT - 1) * STEP
    parts = []
    for i in range(COUNT):
        parts.append(part(f"Plank{i}", kit.cube(), planks[i % 3],
                          M((rng.uniform(-0.04, 0.04), i * STEP, rng.uniform(-0.012, 0.012) - 0.03),
                            rot=(0, rng.uniform(-1.5, 1.5), rng.uniform(-2.5, 2.5)),
                            scale=(1.7 + rng.uniform(-0.1, 0.1), 0.29, 0.06))))
    for x in (-0.62, 0.62):
        parts.append(part("Beam", kit.cube(), beam, M((x, length / 2, -0.11), scale=(0.12, length + 0.3, 0.1))))
        for y in (0.0, length / 3, 2 * length / 3, length):
            parts.append(part("Post", kit.cylinder(6, 0.09, 1.5), beam,
                              M((x * 1.16, y, -0.58), rot=(rng.uniform(-3, 3), rng.uniform(-3, 3), 0))))
    return [kit.join(parts, "Jetty")]
