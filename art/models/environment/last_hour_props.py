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
gilded spire, toothed rings round it (origin at its foot, at the water); "escapement" the clock's heart, a great
escape-wheel stood facing south and its anchor rocking over it, stopped (origin at the wheel's middle); "counterweight"
an iron weight on its chain (the chain 4 m up from the weight; origin at the chain's top); "turret" a shorter
round tower for the castle's corners (12 m, domed); "longcase" a tall case clock, its face to the south, stopped at a
quarter past nine (origin at its foot); "orrery" a great brass orrery, a gilt sun in its rings, the planets on their
arms (origin at its foot); "pendulum_hall" a pendulum 6.5 m long for the great hall (origin at its pivot);
the clutter of a working clock castle (origin at the foot, the front to the south, the back to a wall): "workbench"
(a vise, clock parts, a lamp), "shelf" (clock movements), "crate_gears", "gear_stack" (spare cogs leaning), "lathe"
(its flywheel and belt), "hoist" (a pulley under the ceiling, its chain and hook hanging 3 m: origin at the top),
"mainspring" (a great coiled spring on its side), "hourglass" (tall, its sand caught mid-fall), "chandelier" (a gear
ring of lamps on a chain 2 m: origin at the top), "cart" (laden with gears), "fountain" (an octagonal basin, a brass
column, a bowl of cogs; the water's the game's: held still in the air); "drive" a brass drive-rod 5.2 m long along east, a gear at each end
and its brackets (origin at its west end); (No greenery: nothing grows in it.)"""
import math

from mathutils import Vector

import masonry as m

VARIANTS = ["great_wheel", "gear_large", "gear_small", "shaft", "bell_large", "bell_small", "pendulum", "dial",
            "hand_hour", "hand_minute", "gear_wall_large", "gear_wall_small", "spire", "drive",
            "escapement", "counterweight", "turret", "longcase", "orrery", "pendulum_hall",
            "workbench", "shelf", "crate_gears", "gear_stack", "lathe", "hoist", "mainspring", "hourglass", "chandelier",
            "cart", "fountain"]
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
    elif variant == "gear_wall_large":
        parts += gear(2.6, 36, 0.22, brass_web, iron)
        parts.append(part("Boss", kit.cylinder(12, 0.55, 0.5), gilt))
    elif variant == "gear_wall_small":
        parts += gear(1.4, 20, 0.2, brass_web, iron)
        parts.append(part("Boss", kit.cylinder(12, 0.35, 0.45), gilt))
    elif variant in ("spire", "turret"):
        stones = [m.mat(x) for x in m.STONE]
        cap, dark = m.mat(m.CAP), kit.mat("SlitDark", "#1a1c22")
        r, height = (2.4, 20.0) if variant == "spire" else (2.0, 12.0)
        parts.append(part("Footing", kit.cylinder(16, r + 0.7, 1.6), m.mat(m.PLINTH), M((0, 0, -0.1))))
        # One body (no stacked solids inside it: the camera's cut through it stays clean), banded every two metres.
        parts.append(part("Body", kit.cylinder(16, r, height), stones[1], M((0, 0, height / 2))))
        for z in range(2, int(height), 2):
            parts.append(part("Band", kit.cylinder(16, r + 0.06, 0.12), cap, M((0, 0, z))))
        for z in [z_ for z_ in (5.0, 9.0, 13.0, 17.0) if z_ < height - 1]:
            for a in (0, 120, 240):
                ar = math.radians(a + z * 7)
                parts.append(part("Slit", kit.cube(), dark, M((math.cos(ar) * (r + 0.01), math.sin(ar) * (r + 0.01), z),
                                                              rot=(0, 0, math.degrees(ar)), scale=(0.1, 0.35, 1.1))))
        for z in [z_ for z_ in (4.0, 8.0, 12.0, 16.0) if z_ < height - 1]:  # toothed rings round it, a gear every few metres up
            parts.append(part("GearRing", kit.cylinder(20, r + 0.18, 0.3), iron, M((0, 0, z))))
            for k in range(26):
                ang = 2 * math.pi * k / 26
                parts.append(part("RingCog", kit.cube(), iron, M((math.cos(ang) * (r + 0.32), math.sin(ang) * (r + 0.32), z),
                                                                 rot=(0, 0, math.degrees(ang)), scale=(0.3, 0.16, 0.26))))
        parts.append(part("Cornice", kit.cylinder(16, r + 0.45, 0.4), cap, M((0, 0, height + 0.2))))
        k_ = r / 2.4
        profile = [(z * k_, x * k_) for z, x in [(0.0, 2.3), (0.5, 2.75), (1.2, 2.85), (1.9, 2.4), (2.6, 1.6), (3.2, 0.8), (3.6, 0.3), (3.9, 0.08)]]
        base = height + 0.4
        parts.append(part("Dome", kit.tube([(0, 0, base + z) for z, _ in profile], [x for _, x in profile], seg=14), copper, smooth=True))
        parts.append(part("DomeBand", kit.cylinder(14, 2.9 * k_, 0.16), gilt, M((0, 0, base + 1.1 * k_))))
        parts.append(part("Spire", kit.cone(8, 0.14, 0.02, 2.6), gilt, M((0, 0, base + 3.9 * k_ + 1.3))))
        parts.append(part("Orb", kit.sphere(8, 6, 0.24), gilt, M((0, 0, base + 4.0 * k_))))
    elif variant == "drive":
        # The rod along east; a gear stood on edge across it at each end (its axle the rod), a bracket under each.
        length = 5.2
        parts.append(part("Rod", kit.cylinder(12, 0.22, length), iron, M(G(length / 2, 0, 0), rot=(0, 90, 0))))
        for e in (0.4, length - 0.4):
            parts += gear(1.0, 14, 0.22, brass_web, iron, at=M(G(e, 0, 0), rot=(0, 90, 0)))
            parts.append(m.box("Bracket", m.mat(m.CAP), e - 0.25, e + 0.25, -0.3, 0.3, -1.6, -0.3))
        for e in (1.8, 3.4):
            parts.append(part("Collar", kit.cylinder(12, 0.32, 0.18), gilt, M(G(e, 0, 0), rot=(0, 90, 0))))
    elif variant == "escapement":
        # The escape-wheel (stood facing south: its axle north), its teeth raked like a saw's; the anchor above it, its
        # two pallets dipping into the teeth either side, its crutch running up to the pivot.
        at = M((0, 0, 0), rot=(90, 0, 0))
        parts.append(part("Wheel", kit.cylinder(30, 1.3, 0.14), iron, at))
        parts.append(part("WheelWeb", kit.cylinder(30, 1.15, 0.08), brass_web, at))
        parts.append(part("WheelHub", kit.cylinder(10, 0.25, 0.3), gilt, at))
        for k in range(30):
            ang = 2 * math.pi * k / 30
            parts.append(part("Tooth", kit.cube(), iron, M((math.cos(ang) * 1.42, 0, math.sin(ang) * 1.42), rot=(0, -math.degrees(ang) - 20, 0),
                                                           scale=(0.3, 0.14, 0.09))))
        for k in range(5):
            ang = 2 * math.pi * k / 5
            parts.append(part("Spoke", kit.cube(), iron, M((math.cos(ang) * 0.6, 0, math.sin(ang) * 0.6), rot=(0, -math.degrees(ang), 0),
                                                           scale=(1.1, 0.1, 0.12))))
        pivot = (0, -0.15, 2.0)
        parts.append(part("AnchorPivot", kit.cylinder(10, 0.18, 0.4), gilt, M(pivot, rot=(90, 0, 0))))
        for side in (-1, 1):
            arm_end = (side * 1.15, -0.15, 1.2)
            parts.append(part("AnchorArm", kit.cube(), iron, kit.along(pivot, arm_end) @ kit.M(scale=(0.16, 0.12, math.dist(pivot, arm_end)))))
            parts.append(part("Pallet", kit.cube(), gilt, M((side * 1.2, -0.15, 1.05), rot=(0, side * 30, 0), scale=(0.14, 0.14, 0.42))))
        parts.append(part("Crutch", kit.cube(), iron, M((0, -0.15, 2.9), scale=(0.1, 0.1, 1.8))))
    elif variant == "counterweight":
        parts.append(part("Chain", kit.cube(), iron, M((0, 0, -2.0), scale=(0.06, 0.06, 4.0))))
        for z in range(8):
            parts.append(part("Link", kit.cube(), iron, M((0, 0, -0.25 - z * 0.5), scale=(0.12, 0.04, 0.18))))
        dark_iron = kit.mat("WeightIron_Metal", "#3b3a40")
        parts.append(part("Hook", kit.cylinder(8, 0.12, 0.2), iron, M((0, 0, -4.05))))
        parts.append(part("Weight", kit.cylinder(12, 0.38, 1.1), dark_iron, M((0, 0, -4.7))))
        parts.append(part("WeightBand", kit.cylinder(12, 0.41, 0.1), iron, M((0, 0, -4.3))))
        parts.append(part("WeightBand", kit.cylinder(12, 0.41, 0.1), iron, M((0, 0, -5.1))))
    elif variant == "longcase":
        case = kit.mat("CaseWood", "#5e3f2a")
        enamel = kit.mat("CaseDial_Glow", "#3a3426")
        dark = kit.mat("HandDark", "#2a2620")
        glass = kit.mat("CaseGlass", "#2a3038")
        parts += [m.box("Plinth", case, -0.48, 0.48, -0.34, 0.34, 0.0, 0.35),
                  m.box("Trunk", case, -0.36, 0.36, -0.28, 0.28, 0.35, 2.3),
                  m.box("Window", glass, -0.2, 0.2, -0.3, -0.27, 0.8, 2.0),
                  m.box("Hood", case, -0.45, 0.45, -0.33, 0.33, 2.3, 3.05),
                  m.box("Crest", iron, -0.45, 0.45, -0.33, 0.33, 3.05, 3.12),
                  part("Face", kit.cylinder(20, 0.3, 0.03), enamel, M(G(0, -0.34, 2.68), rot=(90, 0, 0))),
                  part("Bezel", kit.cylinder(20, 0.34, 0.02), iron, M(G(0, -0.335, 2.68), rot=(90, 0, 0))),
                  m.box("Hour", dark, -0.17, 0.0, -0.37, -0.35, 2.665, 2.695),
                  m.box("Minute", dark, 0.0, 0.25, -0.37, -0.35, 2.67, 2.69),
                  part("Bob", kit.cylinder(14, 0.13, 0.03), iron, M(G(-0.05, -0.31, 1.15), rot=(90, 0, 0))),
                  m.box("Rod", iron, -0.075, -0.035, -0.31, -0.29, 1.15, 2.0)]
        for e in (-0.38, 0.38):
            parts.append(part("Finial", kit.sphere(6, 4, 0.07), gilt, M(G(e, 0, 3.2))))
        parts.append(part("Finial", kit.cone(6, 0.07, 0.01, 0.3), gilt, M(G(0, 0, 3.27))))
    elif variant == "orrery":
        # A pedestal of brass; on it the gilt sun in three tilted rings; arms out from the pedestal's head carrying
        # the planets, each at its own height (stopped where they stood).
        parts += [part("Base", kit.cylinder(16, 1.3, 0.3), iron, M((0, 0, 0.15))),
                  part("Step", kit.cylinder(16, 1.0, 0.25), brass_web, M((0, 0, 0.42))),
                  part("Column", kit.cylinder(12, 0.22, 2.3), iron, M((0, 0, 1.7))),
                  part("Sun", kit.sphere(16, 12, 0.6), gilt, M((0, 0, 3.4)), smooth=True)]
        for k, (rx, ry) in enumerate(((20, 0), (-30, 40), (70, -20))):
            for j in range(24):
                a0, a1 = 2 * math.pi * j / 24, 2 * math.pi * (j + 1) / 24
                rr = 1.1 + 0.18 * k
                pts = [kit.M(rot=(rx, ry, 0)) @ Vector((math.cos(a) * rr, math.sin(a) * rr, 0)) for a in (a0, a1)]
                p0 = (pts[0].x, pts[0].y, pts[0].z + 3.4)
                p1 = (pts[1].x, pts[1].y, pts[1].z + 3.4)
                parts.append(part("OrreryRing", kit.cube(), iron, kit.along(p0, p1) @ kit.M(scale=(0.06, 0.06, math.dist(p0, p1) + 0.02))))
        for k, (ang, reach, z, size) in enumerate(((20, 2.0, 2.6, 0.22), (140, 2.7, 2.3, 0.3), (250, 3.4, 2.0, 0.26), (320, 4.0, 2.8, 0.36))):
            a = math.radians(ang)
            tip = (math.cos(a) * reach, math.sin(a) * reach, z)
            parts.append(part("Arm", kit.cube(), iron, kit.along((0, 0, 2.6), tip) @ kit.M(scale=(0.07, 0.07, math.dist((0, 0, 2.6), tip)))))
            body = [copper, bronze, kit.mat("PlanetBlue", "#6f8fb4"), kit.mat("PlanetRed", "#a8664a")][k]
            parts.append(part("Planet", kit.sphere(10, 8, size), body, M(tip), smooth=True))
    elif variant == "pendulum_hall":
        length = 6.5
        parts += [part("Pivot", kit.cylinder(10, 0.12, 0.6), iron, M((0, 0, 0), rot=(90, 0, 0))),
                  part("Rod", kit.cube(), iron, M((0, 0, -length / 2), scale=(0.1, 0.06, length))),
                  part("Bob", kit.cylinder(24, 0.6, 0.18), bronze, M((0, 0, -length), rot=(90, 0, 0))),
                  part("BobRim", kit.cylinder(24, 0.64, 0.1), gilt, M((0, 0, -length), rot=(90, 0, 0)))]
    elif variant == "workbench":
        top = kit.mat("BenchTop", "#6a4c34")
        lamp = kit.mat("BenchLamp_Glow", "#ffcf8a")
        parts += [m.box("Top", top, -1.0, 1.0, -0.4, 0.4, 0.82, 0.9)]
        for e in (-0.9, 0.9):
            for n in (-0.32, 0.32):
                parts.append(m.box("Leg", timber, e - 0.05, e + 0.05, n - 0.05, n + 0.05, 0.0, 0.82))
        parts.append(m.box("Shelf", timber, -0.95, 0.95, -0.35, 0.35, 0.22, 0.27))
        parts.append(m.box("Vise", iron, -0.85, -0.6, -0.42, -0.2, 0.9, 1.05))
        parts.append(m.box("ViseJaw", iron, -0.85, -0.6, -0.5, -0.42, 0.9, 1.08))
        for k, (e, n, r) in enumerate(((-0.2, -0.05, 0.22), (0.25, 0.12, 0.14), (0.45, -0.15, 0.1), (0.05, 0.2, 0.09))):
            parts += gear(r, 8 + k * 2, 0.03, brass_web, iron, at=M(G(e, n, 0.92)))
        parts.append(part("LampPost", kit.cylinder(6, 0.02, 0.5), iron, M(G(0.75, 0.22, 1.15))))
        parts.append(part("Lamp", kit.sphere(8, 6, 0.08), lamp, M(G(0.75, 0.12, 1.38))))
        parts.append(part("Shade", kit.cone(8, 0.14, 0.04, 0.12), iron, M(G(0.75, 0.12, 1.47))))
        for k in range(3):
            parts.append(m.box("Box", kit.mat("PartsBox", "#5a4030"), -0.6 + k * 0.45, -0.25 + k * 0.45, -0.3, 0.3, 0.27, 0.5))
    elif variant == "shelf":
        case = kit.mat("CaseWood", "#5e3f2a")
        enamel = kit.mat("SmallDial", "#d8d0bc")
        parts += [m.box("Back", case, -1.0, 1.0, 0.18, 0.25, 0.0, 2.2)]
        for e in (-1.0, 0.95):
            parts.append(m.box("Side", case, e, e + 0.05, -0.25, 0.25, 0.0, 2.2))
        for k, u in enumerate((0.05, 0.75, 1.45, 2.15)):
            parts.append(m.box("Board", case, -1.0, 1.0, -0.25, 0.25, u, u + 0.05))
            if u > 2.0:
                continue
            for j, e in enumerate((-0.65, 0.0, 0.62)):
                if (k + j) % 3 == 0:  # a movement: a brass box of gears
                    parts.append(m.box("Movement", iron, e - 0.2, e + 0.2, -0.15, 0.15, u + 0.05, u + 0.45))
                    parts += gear(0.13, 10, 0.02, brass_web, iron, at=M(G(e, -0.16, u + 0.27), rot=(90, 0, 0)))
                elif (k + j) % 3 == 1:  # a small bracket clock
                    parts.append(m.box("Bracket", case, e - 0.17, e + 0.17, -0.13, 0.13, u + 0.05, u + 0.5))
                    parts.append(part("Dial", kit.cylinder(14, 0.12, 0.02), enamel, M(G(e, -0.14, u + 0.32), rot=(90, 0, 0))))
                else:  # loose wheels in a heap
                    for q in range(3):
                        parts += gear(0.09 + 0.03 * q, 8, 0.02, brass_web, iron, at=M(G(e + 0.08 * q - 0.08, 0.0, u + 0.06 + q * 0.025)))
    elif variant == "crate_gears":
        wood = kit.mat("CrateWood", "#7a5a3c")
        parts += [m.box("Floor", wood, -0.6, 0.6, -0.45, 0.45, 0.0, 0.06)]
        for n in (-0.45, 0.4):
            parts.append(m.box("SideN", wood, -0.6, 0.6, n, n + 0.05, 0.0, 0.62))
        for e in (-0.6, 0.55):
            parts.append(m.box("SideE", wood, e, e + 0.05, -0.45, 0.45, 0.0, 0.62))
        rng = kit.rng
        for k in range(7):
            r = rng.uniform(0.14, 0.3)
            parts += gear(r, int(8 + r * 30), 0.04, brass_web, iron,
                          at=M(G(rng.uniform(-0.35, 0.35), rng.uniform(-0.25, 0.25), 0.5 + k * 0.03), rot=(rng.uniform(-25, 25), rng.uniform(-25, 25), 0)))
    elif variant == "gear_stack":
        rng = kit.rng
        for k, r in enumerate((0.9, 0.7, 0.55)):  # leaning against the wall behind
            parts += gear(r, int(r * 22), 0.08, brass_web, iron, at=M(G(-0.6 + k * 0.6, 0.15 + k * 0.06, r + 0.02), rot=(78, 0, rng.uniform(-8, 8))))
        for k in range(4):  # a pile lying in front
            parts += gear(0.35 - k * 0.05, 10, 0.06, brass_web, iron, at=M(G(0.5, -0.45, 0.04 + k * 0.07), rot=(0, 0, k * 17)))
    elif variant == "lathe":
        dark = kit.mat("MachineIron_Metal", "#3e3e44")
        belt = kit.mat("Belt", "#3a2a1e")
        parts += [m.box("Bed", dark, -1.1, 1.1, -0.2, 0.2, 0.75, 0.95)]
        for e in (-0.9, 0.9):
            parts.append(m.box("Foot", dark, e - 0.12, e + 0.12, -0.3, 0.3, 0.0, 0.75))
        parts.append(m.box("Headstock", iron, -1.0, -0.6, -0.22, 0.22, 0.95, 1.35))
        parts.append(m.box("Tailstock", iron, 0.6, 0.85, -0.18, 0.18, 0.95, 1.25))
        parts.append(part("Spindle", kit.cylinder(8, 0.05, 1.3), iron, M(G(-0.0, 0, 1.15), rot=(0, 90, 0))))
        parts += gear(0.55, 14, 0.08, brass_web, iron, at=M(G(-1.25, 0.0, 1.0), rot=(0, 90, 0)))   # the flywheel
        p0, p1 = G(-1.25, 0.0, 1.5), G(-0.8, 0.0, 1.35)
        parts.append(part("Belt", kit.cube(), belt, kit.along(p0, p1) @ kit.M(scale=(0.04, 0.08, math.dist(p0, p1)))))
    elif variant == "hoist":
        parts += [m.box("Beam", timber, -0.6, 0.6, -0.12, 0.12, -0.25, 0.0),
                  part("Pulley", kit.cylinder(14, 0.25, 0.1), iron, M(G(0, 0, -0.45), rot=(90, 0, 0))),
                  m.box("Chain", iron, -0.03, 0.03, -0.03, 0.03, -3.0, -0.45),
                  m.box("Chain2", iron, 0.2, 0.26, -0.03, 0.03, -1.6, -0.45),
                  part("Hook", kit.tube([(0, 0, -3.0), (0, -0.15, -3.25), (0, 0, -3.4), (0, 0.12, -3.3)], [0.04] * 4, seg=5), iron),
                  part("Weight", kit.cylinder(10, 0.16, 0.3), kit.mat("WeightIron_Metal", "#3b3a40"), M(G(0.23, 0, -1.75)))]
    elif variant == "mainspring":
        # A great coiled spring of blued steel on its side, wound tight round its arbor, on a cradle.
        steel = kit.mat("BluedSteel_Metal", "#4a5a78")
        parts.append(m.box("Cradle", timber, -0.9, 0.9, -0.35, 0.35, 0.0, 0.3))
        pts = []
        for k in range(140):
            a = k * 0.18
            r = 0.15 + 0.0055 * k
            pts.append((math.cos(a) * r, 0, 1.15 + math.sin(a) * r))
        parts.append(part("Spring", kit.tube(pts, [0.035] * len(pts), seg=4), steel))
        parts.append(part("Arbor", kit.cylinder(10, 0.1, 0.5), iron, M((0, 0, 1.15), rot=(90, 0, 0))))
        parts.append(part("Barrel", kit.cylinder(24, 0.98, 0.06), iron, M((0, -0.22, 1.15), rot=(90, 0, 0))))
    elif variant == "hourglass":
        glass = kit.mat("HourGlass", "#a8bccb")
        sand = kit.mat("Sand", "#d9b878")
        parts += [part("Foot", kit.cylinder(10, 0.5, 0.12), iron, M((0, 0, 0.06))),
                  part("Head", kit.cylinder(10, 0.5, 0.12), iron, M((0, 0, 2.2))),
                  part("Lower", kit.cone(12, 0.42, 0.05, 1.0), glass, M((0, 0, 0.62))),
                  part("Upper", kit.cone(12, 0.05, 0.42, 1.0), glass, M((0, 0, 1.64))),
                  part("SandLow", kit.cone(12, 0.36, 0.08, 0.35), sand, M((0, 0, 0.3))),
                  part("SandHigh", kit.cone(12, 0.06, 0.24, 0.3), sand, M((0, 0, 1.3))),
                  part("Stream", kit.cylinder(5, 0.012, 0.75), sand, M((0, 0, 0.84)))]  # (caught falling)
        for k in range(3):
            a = 2 * math.pi * k / 3
            parts.append(part("Post", kit.cylinder(6, 0.04, 2.1), iron, M((math.cos(a) * 0.46, math.sin(a) * 0.46, 1.13))))
        parts.append(part("Finial", kit.sphere(8, 6, 0.1), gilt, M((0, 0, 2.35))))
    elif variant == "chandelier":
        lamp = kit.mat("Candle_Glow", "#ffcf8a")
        parts.append(m.box("Chain", iron, -0.03, 0.03, -0.03, 0.03, -2.0, 0.0))
        parts += gear(0.9, 18, 0.08, brass_web, iron, at=M((0, 0, -2.1)))
        for k in range(6):
            a = 2 * math.pi * k / 6
            parts.append(part("Cup", kit.cylinder(8, 0.07, 0.08), gilt, M((math.cos(a) * 0.75, math.sin(a) * 0.75, -2.0))))
            parts.append(part("Flame", kit.sphere(6, 4, 0.05), lamp, M((math.cos(a) * 0.75, math.sin(a) * 0.75, -1.9))))
    elif variant == "cart":
        wood = kit.mat("CartWood", "#6e5038")
        parts += [m.box("Bed", wood, -0.9, 0.9, -0.55, 0.55, 0.55, 0.65)]
        for n in (-0.55, 0.5):
            parts.append(m.box("Side", wood, -0.9, 0.9, n, n + 0.05, 0.65, 0.95))
        for e in (-0.9, 0.85):
            parts.append(m.box("End", wood, e, e + 0.05, -0.55, 0.55, 0.65, 0.95))
        for e in (-0.5, 0.5):  # its wheels are gears
            for n in (-0.62, 0.62):
                parts += gear(0.42, 12, 0.08, brass_web, iron, at=M(G(e, n, 0.42), rot=(90, 0, 0)))
        parts.append(m.box("Shaft", wood, 0.9, 1.9, -0.04, 0.04, 0.55, 0.62))
        rng = kit.rng
        for k in range(6):
            r = rng.uniform(0.18, 0.35)
            parts += gear(r, int(8 + r * 25), 0.05, brass_web, iron,
                          at=M(G(rng.uniform(-0.55, 0.55), rng.uniform(-0.3, 0.3), 0.75 + k * 0.05), rot=(rng.uniform(-20, 20), rng.uniform(-20, 20), 0)))
    elif variant == "fountain":
        stone = m.mat(m.CAP)
        parts.append(part("Basin", kit.cylinder(8, 2.0, 0.6), stone, M((0, 0, 0.3), rot=(0, 0, 22.5))))
        parts.append(part("BasinRim", kit.cylinder(8, 2.1, 0.12), m.mat(m.STONE[1]), M((0, 0, 0.62), rot=(0, 0, 22.5))))
        parts.append(part("Column", kit.cylinder(10, 0.22, 1.6), iron, M((0, 0, 1.4))))
        parts += gear(0.85, 14, 0.12, brass_web, iron, at=M((0, 0, 2.2)))   # the bowl, a gear
        parts.append(part("Bowl", kit.cone(12, 0.2, 0.75, 0.35), iron, M((0, 0, 2.05))))
        parts.append(part("Finial", kit.cone(8, 0.12, 0.02, 0.5), gilt, M((0, 0, 2.55))))
        for k in range(4):  # spouts
            a = 2 * math.pi * k / 4 + math.pi / 4
            parts.append(part("Spout", kit.cylinder(6, 0.04, 0.3), gilt, M((math.cos(a) * 0.85, math.sin(a) * 0.85, 2.25), rot=(0, 90, math.degrees(a)))))
    return [kit.join(parts, f"ClocktowerProps_{variant}")]
