"""Half-sunken village house: plaster walls with timber framing, dark windows, slate roof, tilted as it sinks.
Origin at the waterline; about 1 m of the house is under water."""
M, part = kit.M, kit.part  # `kit` is injected by build_model.py

VARIANTS = ["a", "b"]
SIZES = {"a": (3.0, 2.6, 2.4, (0, 4, 2)), "b": (2.4, 2.2, 3.0, (-5, -3, -4))}


def build(variant):
    width, depth, height, tilt = SIZES[variant]
    plaster, timber = kit.mat("Plaster", "#b3a58e"), kit.mat("Timber", "#5a4232")
    slate, window = kit.mat("Slate", "#4a5160"), kit.mat("WindowDark", "#1c1d24")
    base = -1.0
    tilt_m = M(rot=tilt)
    parts = [part("Walls", kit.cube(), plaster, tilt_m @ M((0, 0, base + height / 2), scale=(width, depth, height)))]
    # Timber framing: corner posts, a mid beam, a sill.
    for sx in (-1, 1):
        for sy in (-1, 1):
            parts.append(part("Corner", kit.cube(), timber, tilt_m @ M((sx * width / 2, sy * depth / 2, base + height / 2), scale=(0.14, 0.14, height + 0.02))))
    for z in (base + height * 0.55, base + height - 0.05):
        parts.append(part("BeamFront", kit.cube(), timber, tilt_m @ M((0, -depth / 2 - 0.02, z), scale=(width, 0.08, 0.12))))
        parts.append(part("BeamSide", kit.cube(), timber, tilt_m @ M((width / 2 + 0.02, 0, z), scale=(0.08, depth, 0.12))))
    # Windows (front and side) and a door half under water.
    for x in (-width * 0.25, width * 0.25):
        parts.append(part("Window", kit.cube(), window, tilt_m @ M((x, -depth / 2 - 0.03, base + height * 0.75), scale=(0.45, 0.06, 0.55))))
    parts.append(part("SideWindow", kit.cube(), window, tilt_m @ M((width / 2 + 0.03, 0, base + height * 0.75), scale=(0.06, 0.45, 0.5))))
    parts.append(part("Door", kit.cube(), timber, tilt_m @ M((width * 0.05, -depth / 2 - 0.04, base + 0.95), scale=(0.7, 0.06, 1.3))))
    # Roof: a triangular prism over the depth, with a chimney.
    ridge = height + 1.1
    roof = [(-width / 2 - 0.25, base + height), (width / 2 + 0.25, base + height), (0, base + ridge)]

    def roof_shape(bm):
        kit.prism(roof, depth + 0.4)(bm)

    parts.append(part("Roof", roof_shape, slate, tilt_m @ M((0, depth / 2 + 0.2, 0), rot=(90, 0, 0))))
    parts.append(part("Chimney", kit.cube(), plaster, tilt_m @ M((width * 0.28, depth * 0.1, base + ridge - 0.3), scale=(0.35, 0.35, 0.9))))
    return [kit.join(parts, f"House_{variant}")]
