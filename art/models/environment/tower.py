"""A stone watchtower in three storeys, each its own model so the game can hide the storeys above the Apprentice
while they're inside (Storeys): "ground" (walls, a door to the south, the stair up along the east wall), "middle"
(the first floor, a door west onto a walkway, a balcony to the south, the stair to the roof along the north wall)
and "top" (the roof, a parapet round it with a chained gap on the east for the Hook). 7 x 7 m, storeys 2.4 m.
Every drop taller than 1.5 m is walled or railed. Modelled in the game's directions: E east, N north, U up (see
`box`); origin at the ground floor's centre."""
M, part = kit.M, kit.part  # `kit` is injected by build_model.py

VARIANTS = ["ground", "middle", "top"]
H = 2.4       # storey height
OUT = 3.5     # outer half-width
IN = 3.15     # inner half-width
RISE, RUN, STEPS = 0.2, 0.333, 12
RAIL = 1.0    # railing / half-wall height


def box(name, mat, e0, e1, n0, n1, u0, u1):
    """An axis-aligned box in the game's directions (east, north, up). The export turns Blender's -Y into the game's
    north and its -X into east, so a game point (e, n, u) is Blender's (-e, -n, u)."""
    return part(name, kit.cube(), mat, M((-(e0 + e1) / 2, -(n0 + n1) / 2, (u0 + u1) / 2), scale=(e1 - e0, n1 - n0, u1 - u0)))


def build(variant):
    stone, dark = kit.mat("TowerStone", "#6f6a66"), kit.mat("TowerStoneDark", "#55504d")
    floor, wood = kit.mat("TowerFloor", "#5b5048"), kit.mat("TowerWood", "#5a4332")
    iron = kit.mat("TowerIron", "#2c2b31")
    parts = []
    if variant == "ground":
        parts.append(box("Floor", floor, -OUT, OUT, -OUT, OUT, -0.05, 0.02))
        # Walls, a door in the south one (the camera's side).
        parts += [
            box("WallS", stone, -OUT, -0.7, -OUT, -IN, 0, H), box("WallS", stone, 0.7, OUT, -OUT, -IN, 0, H),
            box("Lintel", dark, -0.7, 0.7, -OUT, -IN, 2.0, H),
            box("WallN", stone, -OUT, OUT, IN, OUT, 0, H),
            box("WallW", stone, -OUT, -IN, -IN, IN, 0, H),
            box("WallE", stone, IN, OUT, -IN, IN, 0, H),
        ]
        # The stair up, along the east wall, rising north; a half-wall on its open side.
        for i in range(STEPS):
            n0 = -2.4 + i * RUN
            parts.append(box("Step", dark if i % 2 else stone, 2.0, IN, n0, n0 + RUN, 0, (i + 1) * RISE))
            parts.append(box("StairWall", stone, 1.9, 2.0, n0, n0 + RUN, 0, (i + 1) * RISE + RAIL))
    elif variant == "middle":
        # The first floor, with the stair's opening at the east; railed round it (open where the stair arrives, north).
        parts += [
            box("Floor", floor, -IN, 1.95, -IN, IN, H - 0.2, H),
            box("Floor", floor, 1.95, IN, 1.6, IN, H - 0.2, H),
            box("Floor", floor, 1.95, IN, -IN, -2.5, H - 0.2, H),
            box("RailW", stone, 1.85, 1.95, -2.5, 1.6, H, H + RAIL),
            box("RailS", stone, 1.85, IN, -2.6, -2.5, H, H + RAIL),
        ]
        # Walls: a door west (the walkway) and one south (the balcony).
        parts += [
            box("WallS", stone, -OUT, -0.7, -OUT, -IN, H, 2 * H), box("WallS", stone, 0.7, OUT, -OUT, -IN, H, 2 * H),
            box("Lintel", dark, -0.7, 0.7, -OUT, -IN, H + 2.0, 2 * H),
            box("WallN", stone, -OUT, OUT, IN, OUT, H, 2 * H),
            box("WallW", stone, -OUT, -IN, -IN, -0.7, H, 2 * H), box("WallW", stone, -OUT, -IN, 0.7, IN, H, 2 * H),
            box("Lintel", dark, -OUT, -IN, -0.7, 0.7, H + 2.0, 2 * H),
            box("WallE", stone, IN, OUT, -IN, IN, H, 2 * H),
        ]
        # The balcony over the ground floor's door, railed.
        parts += [
            box("Balcony", floor, -2.0, 2.0, -5.3, -OUT, H - 0.2, H),
            box("BalconyBeam", wood, -2.0, 2.0, -5.3, -5.1, H - 0.45, H - 0.2),
            box("RailS", stone, -2.0, 2.0, -5.3, -5.2, H, H + RAIL),
            box("RailW", stone, -2.0, -1.9, -5.2, -OUT, H, H + RAIL),
            box("RailE", stone, 1.9, 2.0, -5.2, -OUT, H, H + RAIL),
        ]
        # The stair to the roof, along the north wall, rising west from the landing in the north-east corner.
        for i in range(STEPS):
            e1 = 1.9 - i * RUN
            parts.append(box("Step", dark if i % 2 else stone, e1 - RUN, e1, 2.0, IN, H, H + (i + 1) * RISE))
            parts.append(box("StairWall", stone, e1 - RUN, e1, 1.9, 2.0, H, H + (i + 1) * RISE + RAIL))
    else:
        top = 2 * H
        # The roof, its opening over the stair (railed south and east; the stair arrives at its west end).
        parts += [
            box("Roof", floor, -OUT, OUT, -OUT, 1.95, top - 0.2, top),
            box("Roof", floor, -OUT, -2.2, 1.95, OUT, top - 0.2, top),
            box("Roof", floor, 1.95, OUT, 1.95, OUT, top - 0.2, top),
            box("Roof", floor, -2.2, 1.95, IN, OUT, top - 0.2, top),
            box("RailS", stone, -2.2, 1.95, 1.85, 1.95, top, top + RAIL),
            box("RailE", stone, 1.95, 2.05, 1.95, IN, top, top + RAIL),
        ]
        # The parapet, with a gap on the east side for the Hook, chained across (walk into it: it holds).
        parts += [
            box("ParapetS", stone, -OUT, OUT, -OUT, -3.2, top, top + RAIL),
            box("ParapetN", stone, -OUT, OUT, 3.2, OUT, top, top + RAIL),
            box("ParapetW", stone, -OUT, -3.2, -3.2, 3.2, top, top + RAIL),
            box("ParapetE", stone, 3.2, OUT, -3.2, -0.8, top, top + RAIL),
            box("ParapetE", stone, 3.2, OUT, 0.8, 3.2, top, top + RAIL),
            box("Chain", iron, 3.3, 3.4, -0.8, 0.8, top + 0.5, top + 0.62),
            box("ChainPost", iron, 3.25, 3.45, -0.85, -0.75, top, top + 0.7),
            box("ChainPost", iron, 3.25, 3.45, 0.75, 0.85, top, top + 0.7),
        ]
    return [kit.join(parts, f"Tower_{variant}")]
