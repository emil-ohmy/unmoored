"""A shortcut gate on a boardwalk: two stout posts with a lintel and a short plank deck, and a door of rough
vertical bars with cross braces, barred by a heavy plank on its far side (the side it opens from). Exports two
objects: GateFrame (static) and GateDoor (the game swings it about its hinge). Walkway along Y; origin at its base."""
M, part = kit.M, kit.part  # `kit` is injected by build_model.py


def build():
    rng = kit.rng
    wood, wood_dark = kit.mat("GateWood", "#6b523f"), kit.mat("GateWoodDark", "#4a382b")
    metal, rope = kit.mat("GateIron", "#2f2d33"), kit.mat("Rope", "#8f7d5e")
    frame = [
        part("PostL", kit.cube(), wood_dark, M((-0.98, 0, 0.9), scale=(0.18, 0.18, 1.8))),
        part("PostR", kit.cube(), wood_dark, M((0.98, 0, 0.9), scale=(0.18, 0.18, 1.8))),
        part("Lintel", kit.cube(), wood_dark, M((0, 0, 1.86), rot=(0, 2, 0), scale=(2.3, 0.2, 0.16))),
        part("Lashing", kit.cylinder(6, 0.1, 0.14), rope, M((-0.98, 0, 1.7), rot=(0, 0, 0))),
        part("Lashing", kit.cylinder(6, 0.1, 0.14), rope, M((0.98, 0, 1.7), rot=(0, 0, 0))),
    ]
    for i, y in enumerate((-0.6, -0.26, 0.08, 0.42, 0.76)):  # the deck under it, bridging to the next platform
        frame.append(part("DeckPlank", kit.cube(), wood, M((0, y, -0.02), rot=(0, 0, rng.uniform(-2, 2)), scale=(1.7, 0.3, 0.07))))
    door = []
    for i in range(7):
        x = -0.78 + i * 0.26
        door.append(part("Bar", kit.cube(), wood, M((x, 0, 0.82), rot=(0, rng.uniform(-2, 2), 0), scale=(0.09, 0.08, 1.5 + rng.uniform(-0.08, 0.06)))))
    door += [
        part("Brace", kit.cube(), wood_dark, M((0, 0.05, 1.3), scale=(1.66, 0.06, 0.12))),
        part("Brace", kit.cube(), wood_dark, M((0, 0.05, 0.35), scale=(1.66, 0.06, 0.12))),
        part("Diagonal", kit.cube(), wood_dark, M((0, 0.05, 0.82), rot=(0, 32, 0), scale=(1.9, 0.05, 0.1))),
        part("BarringPlank", kit.cube(), wood, M((0, -0.14, 0.9), scale=(1.95, 0.1, 0.16))),  # on its opening side
        part("Hasp", kit.cube(), metal, M((0.7, -0.08, 0.9), scale=(0.1, 0.04, 0.22))),
    ]
    return [kit.join(frame, "GateFrame"), kit.join(door, "GateDoor")]
