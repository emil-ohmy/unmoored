"""The Stopped Clocktower's works, each its own model (the game places them, still, at the moment the clock stopped):
"great_wheel" the water wheel in the undercroft's channel (radius 2.4, axle along north, origin at its axle's middle);
"gear_large" and "gear_small" iron-rimmed timber gears lying flat (axle along up, origin at the hub's middle: the game
stands them up as it needs); "shaft" the main shaft (11.8 m, rising from its origin); "bell_large" and "bell_small"
(origin at the headstock's top, the bell hanging below); "pendulum" (origin at the pivot, the rod 13 m down, the bob a
great disc in the plane of its swing, east-west); "dial" (the clock face in its square stone frame, its face to the
south, origin at its centre); "hand_hour" and "hand_minute" (iron beams pointing west and east from the dial's centre,
standing out from it to the south, their tops at the origin's height: walkable; origin at the dial's centre)."""
import math

import masonry as m

VARIANTS = ["great_wheel", "gear_large", "gear_small", "shaft", "bell_large", "bell_small", "pendulum", "dial",
            "hand_hour", "hand_minute"]
M, part = m.M, m.part
kit = m.kit


def G(e, n, u):
    return (-e, -n, u)


def gear(radius, teeth, thick, timber, iron):
    """A gear lying flat: a timber disc in an iron rim, cogs round it, spokes, a hub."""
    parts = [part("Rim", kit.cylinder(32, radius, thick), iron),
             part("Web", kit.cylinder(32, radius - 0.12, thick * 0.6), timber, M((0, 0, 0.0))),
             part("Hub", kit.cylinder(12, radius * 0.18, thick * 1.8), iron)]
    for k in range(teeth):
        a = 2 * math.pi * k / teeth
        parts.append(part("Cog", kit.cube(), iron, M((math.cos(a) * (radius + 0.1), math.sin(a) * (radius + 0.1), 0),
                                                     rot=(0, 0, math.degrees(a)), scale=(0.22, 0.16, thick))))
    for k in range(6):
        a = 2 * math.pi * k / 6
        parts.append(part("Spoke", kit.cube(), timber, M((math.cos(a) * radius * 0.5, math.sin(a) * radius * 0.5, 0),
                                                         rot=(0, 0, math.degrees(a)), scale=(radius * 0.85, 0.16, thick * 0.9))))
    return parts


def build(variant):
    m.clocktower()
    timber = m.mat(m.TIMBER)
    wet = kit.mat("WetTimber", "#2e2620")
    iron = kit.mat("WorksIron_Metal", "#34343b")
    bronze = kit.mat("BellBronze_Metal", "#7c6440")
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
        parts += gear(2.0, 28, 0.32, timber, iron)
    elif variant == "gear_small":
        parts += gear(1.1, 16, 0.28, timber, iron)
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
        glass = kit.mat("DialGlass_Glow", "#1b2330")
        numeral = kit.mat("Numeral", "#c8ccd4")
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
