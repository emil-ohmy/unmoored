"""The Hall of the Last Hour's works, each its own model (the game places them, still, at the moment the clock stopped):
"great_wheel" the water wheel in the undercroft's channel (radius 2.4, axle along north, origin at its axle's middle);
"gear_large" and "gear_small" iron-rimmed timber gears lying flat (axle along up, origin at the hub's middle: the game
stands them up as it needs); "shaft" the main shaft (11.8 m, rising from its origin); "bell_large" and "bell_small"
(origin at the headstock's top, the bell hanging below); "pendulum" (origin at the pivot, the rod 13 m down, the bob a
great disc in the plane of its swing, east-west); "dial" (the clock face in its square stone frame, its face to the
south, origin at its centre); "hand_hour" and "hand_minute" (iron beams pointing west and east from the dial's centre,
standing out from it to the south, their tops at the origin's height: walkable; origin at the dial's centre).
Outside: "gear_wall_large" and "gear_wall_small" thin brass gears hung on the walls (lying flat: stood up by the game);
"spire" a slender round tower of pale stone in the river (20 m), banded, slit windows, a green-copper onion dome and a
gilded spire (origin at its foot, at the water); "drive" a brass drive-rod 5.2 m long along east, a gear at each end
and its brackets (origin at its west end); "shrub" and "tree" greenery overgrowing the stone (origin at the ground)."""
import math

import masonry as m

VARIANTS = ["great_wheel", "gear_large", "gear_small", "shaft", "bell_large", "bell_small", "pendulum", "dial",
            "hand_hour", "hand_minute", "gear_wall_large", "gear_wall_small", "spire", "drive", "shrub", "tree"]
M, part = m.M, m.part
kit = m.kit


def G(e, n, u):
    return (-e, -n, u)


def gear(radius, teeth, thick, web, rim, at=None):
    """A gear lying flat: a disc in a rim, cogs round it, spokes, a hub (brass, all of it, in the Hall); `at` places it."""
    at = at if at is not None else M()
    parts = [part("Rim", kit.cylinder(32, radius, thick), rim, at),
             part("Web", kit.cylinder(32, radius - 0.12, thick * 0.6), web, at),
             part("Hub", kit.cylinder(12, radius * 0.18, thick * 1.8), rim, at)]
    for k in range(teeth):
        a = 2 * math.pi * k / teeth
        parts.append(part("Cog", kit.cube(), rim, at @ M((math.cos(a) * (radius + 0.1), math.sin(a) * (radius + 0.1), 0),
                                                         rot=(0, 0, math.degrees(a)), scale=(0.22, 0.16, thick))))
    for k in range(6):
        a = 2 * math.pi * k / 6
        parts.append(part("Spoke", kit.cube(), web, at @ M((math.cos(a) * radius * 0.5, math.sin(a) * radius * 0.5, 0),
                                                           rot=(0, 0, math.degrees(a)), scale=(radius * 0.85, 0.16, thick * 0.9))))
    return parts


def build(variant):
    m.last_hour()
    timber = m.mat(m.TIMBER)
    wet = kit.mat("WetTimber", "#4a3a2a")
    iron = kit.mat("WorksBrass_Metal", "#c29a46")
    brass_web = kit.mat("BrassWeb_Metal", "#a8823e")
    bronze = kit.mat("BellBronze_Metal", "#9a7a46")
    copper = kit.mat("Verdigris", "#5f9e93")
    gilt = kit.mat("Gilt_Metal", "#e0bd62")
    parts = []
    if variant == "great_wheel":
        r = 2.4
        parts += [part("Axle", kit.cylinder(10, 0.16, 2.2), iron, M((0, 0, 0), rot=(90, 0, 0))),
                  part("Hub", kit.cylinder(12, 0.4, 1.2), timber, M((0, 0, 0), rot=(90, 0, 0)))]
        for side in (-0.65, 0.65):
            for k in range(20):
                a0, a1 = 2 * math.pi * k / 20, 2 * math.pi * (k + 1) / 20
                p0 = (math.cos(a0) * r, side, math.sin(a0) * r)
                p1 = (math.cos(a1) * r, side, math.sin(a1) * r)
                parts.append(part("Rim", kit.cube(), wet, kit.along(p0, p1) @ kit.M(scale=(0.16, 0.12, math.dist(p0, p1) + 0.05))))
            for k in range(10):
                a = 2 * math.pi * k / 10
                parts.append(part("Spoke", kit.cube(), timber, kit.along((0, side, 0), (math.cos(a) * r, side, math.sin(a) * r))
                                  @ kit.M(scale=(0.12, 0.1, r))))
        for k in range(20):
            a = 2 * math.pi * (k + 0.5) / 20
            c = (math.cos(a) * (r - 0.2), 0, math.sin(a) * (r - 0.2))
            parts.append(part("Paddle", kit.cube(), wet, M(c, rot=(0, -math.degrees(a), 0), scale=(0.5, 1.3, 0.06))))
    elif variant == "gear_large":
        parts += gear(2.0, 28, 0.32, brass_web, iron)
    elif variant == "gear_small":
        parts += gear(1.1, 16, 0.28, brass_web, iron)
    elif variant == "gear_wall_large":
        parts += gear(2.6, 36, 0.22, brass_web, iron)
        parts.append(part("Boss", kit.cylinder(12, 0.55, 0.5), gilt))
    elif variant == "gear_wall_small":
        parts += gear(1.4, 20, 0.2, brass_web, iron)
        parts.append(part("Boss", kit.cylinder(12, 0.35, 0.45), gilt))
    elif variant == "spire":
        stones = [m.mat(x) for x in m.STONE]
        cap, dark = m.mat(m.CAP), kit.mat("SlitDark", "#1a1c22")
        r, height = 2.4, 20.0
        parts.append(part("Footing", kit.cylinder(16, r + 0.7, 1.6), m.mat(m.PLINTH), M((0, 0, -0.1))))
        for k, z in enumerate(range(0, int(height), 2)):  # banded courses
            parts.append(part("Course", kit.cylinder(16, r - 0.02 * (k % 2), 2.0), stones[k % 3], M((0, 0, z + 1.0))))
            parts.append(part("Band", kit.cylinder(16, r + 0.06, 0.12), cap, M((0, 0, z + 2.0))))
        for z in (5.0, 9.0, 13.0, 17.0):
            for a in (0, 120, 240):
                ar = math.radians(a + z * 7)
                parts.append(part("Slit", kit.cube(), dark, M((math.cos(ar) * (r + 0.01), math.sin(ar) * (r + 0.01), z),
                                                              rot=(0, 0, math.degrees(ar)), scale=(0.1, 0.35, 1.1))))
        parts.append(part("Cornice", kit.cylinder(16, r + 0.45, 0.4), cap, M((0, 0, height + 0.2))))
        profile = [(0.0, 2.3), (0.5, 2.75), (1.2, 2.85), (1.9, 2.4), (2.6, 1.6), (3.2, 0.8), (3.6, 0.3), (3.9, 0.08)]
        base = height + 0.4
        parts.append(part("Dome", kit.tube([(0, 0, base + z) for z, _ in profile], [x for _, x in profile], seg=14), copper, smooth=True))
        parts.append(part("DomeBand", kit.cylinder(14, 2.9, 0.16), gilt, M((0, 0, base + 1.1))))
        parts.append(part("Spire", kit.cone(8, 0.14, 0.02, 2.6), gilt, M((0, 0, base + 3.9 + 1.3))))
        parts.append(part("Orb", kit.sphere(8, 6, 0.24), gilt, M((0, 0, base + 4.0))))
    elif variant == "drive":
        # The rod along east; a gear stood on edge across it at each end (its axle the rod), a bracket under each.
        length = 5.2
        parts.append(part("Rod", kit.cylinder(12, 0.22, length), iron, M(G(length / 2, 0, 0), rot=(0, 90, 0))))
        for e in (0.4, length - 0.4):
            parts += gear(1.0, 14, 0.22, brass_web, iron, at=M(G(e, 0, 0), rot=(0, 90, 0)))
            parts.append(m.box("Bracket", m.mat(m.CAP), e - 0.25, e + 0.25, -0.3, 0.3, -1.6, -0.3))
        for e in (1.8, 3.4):
            parts.append(part("Collar", kit.cylinder(12, 0.32, 0.18), gilt, M(G(e, 0, 0), rot=(0, 90, 0))))
    elif variant == "shrub":
        leaves = [kit.mat("HallLeaf1", "#4d6b3e"), kit.mat("HallLeaf2", "#5d7d47"), kit.mat("HallLeaf3", "#41593a")]
        rng = kit.rng
        for k in range(7):
            a = rng.uniform(0, 2 * math.pi)
            d = rng.uniform(0, 0.6)
            rr = rng.uniform(0.35, 0.6)
            parts.append(part("Leaves", kit.ico(1, rr), leaves[k % 3], M((math.cos(a) * d, math.sin(a) * d, rr * 0.7 + rng.uniform(0, 0.25))), jitter=0.06))
    elif variant == "tree":
        leaves = [kit.mat("HallLeaf1", "#4d6b3e"), kit.mat("HallLeaf2", "#5d7d47"), kit.mat("HallLeaf3", "#41593a")]
        bark = kit.mat("HallBark", "#4e3e30")
        rng = kit.rng
        parts.append(part("Trunk", kit.tube([(0, 0, 0), (0.1, 0.05, 1.2), (0.05, -0.1, 2.2)], [0.18, 0.13, 0.09], seg=6), bark))
        for k in range(8):
            a = rng.uniform(0, 2 * math.pi)
            d = rng.uniform(0.2, 0.9)
            rr = rng.uniform(0.5, 0.8)
            parts.append(part("Crown", kit.ico(1, rr), leaves[k % 3], M((math.cos(a) * d, math.sin(a) * d, 2.2 + rng.uniform(-0.3, 0.5))), jitter=0.08))
    elif variant == "shaft":
        parts += [part("Shaft", kit.cylinder(10, 0.2, 11.8), iron, M((0, 0, 5.9)))]
        for u in (0.4, 4.4, 8.4, 11.4):
            parts.append(part("Collar", kit.cylinder(10, 0.3, 0.2), iron, M((0, 0, u))))
    elif variant in ("bell_large", "bell_small"):
        r, h = (1.2, 1.8) if variant == "bell_large" else (0.8, 1.2)
        parts += [part("Headstock", kit.cube(), timber, M((0, 0, -0.2), scale=(r * 2.2, 0.35, 0.4))),
                  part("Crown", kit.cylinder(10, r * 0.35, 0.3), bronze, M((0, 0, -0.55))),
                  part("Waist", kit.cone(16, r * 0.62, r * 0.42, h * 0.55), bronze, M((0, 0, -0.7 - h * 0.275))),
                  part("Sound", kit.cone(16, r, r * 0.62, h * 0.45), bronze, M((0, 0, -0.7 - h * 0.55 - h * 0.225))),
                  part("Lip", kit.cylinder(16, r + 0.04, 0.08), bronze, M((0, 0, -0.7 - h))),
                  part("Clapper", kit.sphere(8, 6, r * 0.18), iron, M((0, 0, -0.7 - h + 0.05)))]
    elif variant == "pendulum":
        length = 13.0
        parts += [part("Pivot", kit.cylinder(10, 0.14, 0.9), iron, M((0, 0, 0), rot=(90, 0, 0))),
                  part("Rod", kit.cube(), iron, M((0, 0, -length / 2), scale=(0.12, 0.08, length))),
                  part("Bob", kit.cylinder(24, 0.85, 0.22), bronze, M((0, 0, -length), rot=(90, 0, 0))),
                  part("BobRim", kit.cylinder(24, 0.9, 0.12), iron, M((0, 0, -length), rot=(90, 0, 0)))]
    elif variant == "dial":
        stone = m.mat(m.CAP)
        glass = kit.mat("DialEnamel_Glow", "#3a3426")   # (cream enamel, lit faintly from within)
        numeral = kit.mat("Numeral", "#2a2620")
        parts += [m.box("Frame", stone, -3.4, 3.4, -0.2, 0.0, -3.4, 3.4),
                  part("Face", kit.cylinder(48, 3.15, 0.06), glass, M(G(0, -0.23, 0), rot=(90, 0, 0))),
                  part("Bezel", kit.cylinder(48, 3.3, 0.04), iron, M(G(0, -0.21, 0), rot=(90, 0, 0)))]
        for k in range(12):
            a = 2 * math.pi * k / 12
            long_ = 0.55 if k % 3 == 0 else 0.35
            c = (math.sin(a) * 2.75, math.cos(a) * 2.75)
            parts.append(part("Hour", kit.cube(), numeral, M(G(c[0], -0.27, c[1]), rot=(0, math.degrees(a), 0), scale=(0.12, 0.03, long_))))
    elif variant in ("hand_hour", "hand_minute"):
        # A beam standing out from the dial (n -0.05 .. -0.75), its top at the centre's height; the hub a round plate.
        length, west = (2.45, True) if variant == "hand_hour" else (3.3, False)
        e0, e1 = (-length, 0.2) if west else (-0.2, length)
        parts += [m.box("Hand", iron, e0, e1, -0.78, -0.05, -0.35, 0.0)]
        tip = e0 if west else e1
        parts.append(m.box("Tip", iron, tip - 0.2 if west else tip, tip if west else tip + 0.2, -0.78, -0.05, -0.5, 0.0))
        parts.append(part("Hub", kit.cylinder(16, 0.85, 0.35), iron, M(G(0, -0.45, -0.175))))
    return [kit.join(parts, f"ClocktowerProps_{variant}")]
