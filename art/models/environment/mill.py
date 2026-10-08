"""The Orchard's mill (the Stroke's first use for getting about): a stone yard off the bottom terrace's east end, at the
terrace's height, its dry-stone walls standing in the water. Along its south side the mill-race: a raised stone trough
of shallow water (step in through the gap in its north wall), running east to a spout over the yard's edge, where the
water wheel stands half in the river on two timber posts. North of the race, the mill store: roofless dry-stone walls
round a small room, its doorway shut by the sluice gate, a heavy timber board in stone grooves under a timber gantry;
its chain runs from the wheel's axle over a pulley on the gantry. A wave down the race turns the wheel; the wheel
winds the gate up.

Variants: "yard" (everything fixed), "wheel" (the wheel alone, origin at its axle's middle, the axle along north;
the game turns it), "gate" (the board alone, origin at its bottom middle; the game raises it). Game directions (see
masonry.py): the yard's origin at the middle of its west edge, at the water's surface; it spans 10 m east and 10 m
north-south, its top at 0.5 m like the terrace. Where the game needs them (MainSceneBuilder.Orchard: Mill):
  the race's inside: e 1.25 .. 9.95, n -3.8 .. -2.8, its bed at 0.6, its water at 0.85; the step in at e 1.6 .. 2.6
  the wheel's axle: e 10.9, n -3.3, u 0.2 (radius 1.3); the spout's lip at e 10.25
  the store's doorway: e 7.5 .. 8.7 in its south wall (n -1.0 .. -0.6), 0.5 .. 2.35 high; the gate's travel 1.85 up
  the gantry's pulley: e 8.1, n -0.8, u 4.1; the wheel post's pulley: e 10.9, n -2.55, u 1.9
"""
import math

import masonry as m

VARIANTS = ["yard", "wheel", "gate"]
M, part = m.M, m.part
kit = m.kit

TOP = 0.5
RACE_N0, RACE_N1 = -4.05, -2.55   # the trough's outside
RACE_E0, RACE_E1 = 1.0, 10.0
WALL_T = 0.25
BED, RIM = 0.6, 1.0
AXLE = (10.9, -3.3, 0.2)
WHEEL_R = 1.3
STORE = (6.6, 9.7, -1.0, 2.6)     # e0, e1, n0, n1 (outside)
STORE_H = 2.7
DOOR = (7.5, 8.7)
DOOR_TOP = 2.35


def G(e, n, u):
    """A game point in Blender's frame."""
    return (-e, -n, u)


def build(variant):
    m.orchard()
    if variant == "wheel":
        return [kit.join(wheel(), "MillWheel")]
    if variant == "gate":
        return [kit.join(gate(), "SluiceGate")]
    return [kit.join(yard(), "MillYard")]


def yard():
    parts = []
    stone_cap = m.mat(m.CAP)
    timber = m.mat(m.TIMBER)
    iron = kit.mat("ChainIron_Metal", "#3a3a40")
    # The platform: earth inside dry-stone walls round its three water sides (its west side meets the terrace).
    parts.append(m.box("Earth", m.mat(m.MORTAR), 0.0, 9.7, -4.7, 4.7, -0.9, TOP - 0.1))
    m.wall(parts, 0.0, 10.0, -5.0, -4.55, -0.9, TOP, course=0.45, plinth=True)   # south
    m.wall(parts, 0.0, 10.0, 4.55, 5.0, -0.9, TOP, course=0.45, plinth=True)     # north
    m.wall(parts, 9.55, 10.0, -4.55, 4.55, -0.9, TOP, course=0.45, plinth=True)  # east
    m.flagstones(parts, 0.0, 9.55, -4.55, 4.55, TOP + 0.02, holes=[(RACE_E0, RACE_E1, RACE_N0, RACE_N1)])

    # The race: a stone trough, its bed a little above the yard, its walls knee-high; open at its east end (the spout).
    m.wall(parts, RACE_E0, RACE_E1, RACE_N0, RACE_N0 + WALL_T, TOP, RIM, course=0.25)                 # south side
    m.wall(parts, RACE_E0, RACE_E1, RACE_N1 - WALL_T, RACE_N1, TOP, RIM, course=0.25,
           openings=[(1.6, 2.6, TOP, RIM + 0.1)])                                                     # north side, the step in
    m.wall(parts, RACE_E0, RACE_E0 + WALL_T, RACE_N0 + WALL_T, RACE_N1 - WALL_T, TOP, RIM + 0.35, course=0.25)  # its head
    parts.append(m.box("RaceBed", m.mat(m.PLINTH), RACE_E0 + WALL_T, RACE_E1 + 0.25, RACE_N0 + WALL_T, RACE_N1 - WALL_T, TOP, BED))
    parts.append(m.box("Step", stone_cap, 1.6, 2.6, RACE_N1, RACE_N1 + 0.45, TOP, TOP + 0.1))
    # The spout: the bed carried out over the yard's edge on a timber chute, a lip at its end.
    parts.append(m.box("Chute", timber, 10.0, 10.3, RACE_N0 + 0.15, RACE_N1 - 0.15, BED - 0.12, BED))
    for n in (RACE_N0 + 0.15, RACE_N1 - 0.25):
        parts.append(m.box("ChuteSide", timber, 10.0, 10.3, n, n + 0.1, BED, BED + 0.25))
    # The spring at its head: a worn stone face with a spout, the water welling up out of the hill.
    parts.append(m.box("SpringFace", stone_cap, RACE_E0 - 0.1, RACE_E0 + 0.05, RACE_N0 + 0.2, RACE_N1 - 0.2, RIM, RIM + 0.55))
    parts.append(part("SpringSpout", kit.cylinder(8, 0.06, 0.3), iron, M(G(RACE_E0 + 0.3, -3.3, RIM + 0.15), rot=(0, 90, 0))))

    # The wheel's posts: two timbers rising out of the river either side of it, the axle's bearings on them; on the
    # north post's top, a pulley for the chain.
    for n in (AXLE[1] - 0.62, AXLE[1] + 0.62):
        parts.append(m.box("WheelPost", timber, AXLE[0] - 0.12, AXLE[0] + 0.12, n - 0.1, n + 0.1, -0.9, 1.8))
        parts.append(m.box("Bearing", iron, AXLE[0] - 0.16, AXLE[0] + 0.16, n - 0.12, n + 0.12, AXLE[2] - 0.1, AXLE[2] + 0.1))
    parts.append(m.box("PostBrace", timber, AXLE[0] - 0.08, AXLE[0] + 0.08, AXLE[1] - 0.62, AXLE[1] + 0.62, 1.7, 1.86))
    parts.append(m.box("PostTie", timber, 9.95, AXLE[0], AXLE[1] - 0.06, AXLE[1] + 0.06, 1.55, 1.7))
    parts.append(part("Pulley", kit.cylinder(10, 0.14, 0.06), iron, M(G(AXLE[0], -2.55, 1.9), rot=(90, 0, 0))))

    # The store: roofless dry-stone walls, the doorway in its south wall, the sluice gate's grooves and gantry over it.
    e0, e1, n0, n1 = STORE
    t = 0.4
    m.wall(parts, e0, e1, n0, n0 + t, TOP, STORE_H, course=0.4, openings=[(DOOR[0], DOOR[1], TOP, DOOR_TOP)])  # south
    m.wall(parts, e0, e1, n1 - t, n1, TOP, STORE_H, course=0.4)                                                # north
    m.wall(parts, e0, e0 + t, n0 + t, n1 - t, TOP, STORE_H, course=0.4)                                        # west
    m.wall(parts, e1 - t, e1, n0 + t, n1 - t, TOP, STORE_H, course=0.4)                                        # east
    parts.append(m.box("Lintel", stone_cap, DOOR[0] - 0.15, DOOR[1] + 0.15, n0 - 0.02, n0 + t + 0.02, DOOR_TOP, DOOR_TOP + 0.3))
    for e in (DOOR[0] - 0.22, DOOR[1] + 0.02):
        parts.append(m.box("Groove", stone_cap, e, e + 0.2, n0 - 0.28, n0 - 0.02, TOP, DOOR_TOP + 0.3))      # the gate runs in these
        parts.append(m.box("GantryPost", timber, e + 0.04, e + 0.16, n0 - 0.24, n0 - 0.06, DOOR_TOP + 0.3, 4.3))
    parts.append(m.box("GantryBeam", timber, DOOR[0] - 0.3, DOOR[1] + 0.3, n0 - 0.26, n0 - 0.04, 4.2, 4.38))
    parts.append(part("GantryPulley", kit.cylinder(10, 0.16, 0.06), iron, M(G(8.1, -0.8, 4.1), rot=(0, 90, 0))))
    return parts


def wheel():
    """The water wheel: two rims on spokes round a hub, paddles between the rims; the axle along north."""
    timber = m.mat(m.TIMBER)
    wet = kit.mat("WetTimber", "#3e2e22")
    iron = kit.mat("WheelIron_Metal", "#3a3a40")
    parts = [part("Axle", kit.cylinder(10, 0.1, 1.5), iron, M((0, 0, 0), rot=(90, 0, 0))),
             part("Hub", kit.cylinder(10, 0.22, 0.7), timber, M((0, 0, 0), rot=(90, 0, 0)))]
    for side in (-0.36, 0.36):
        for k in range(16):
            a0, a1 = 2 * math.pi * k / 16, 2 * math.pi * (k + 1) / 16
            p0 = (math.cos(a0) * WHEEL_R, side, math.sin(a0) * WHEEL_R)
            p1 = (math.cos(a1) * WHEEL_R, side, math.sin(a1) * WHEEL_R)
            parts.append(part("Rim", kit.cube(), wet, kit.along(p0, p1) @ kit.M(scale=(0.1, 0.09, math.dist(p0, p1) + 0.04))))
        for k in range(8):
            a = 2 * math.pi * k / 8
            parts.append(part("Spoke", kit.cube(), timber, kit.along((0, side, 0), (math.cos(a) * WHEEL_R, side, math.sin(a) * WHEEL_R))
                              @ kit.M(scale=(0.08, 0.07, WHEEL_R))))
    for k in range(16):
        a = 2 * math.pi * (k + 0.5) / 16
        c = (math.cos(a) * (WHEEL_R - 0.12), 0, math.sin(a) * (WHEEL_R - 0.12))
        parts.append(part("Paddle", kit.cube(), wet, M(c, rot=(0, -math.degrees(a), 0), scale=(0.34, 0.8, 0.05))))
    return parts


def gate():
    """The sluice gate: a heavy board of planks on two cross-battens, an iron ring at its top for the chain."""
    timber = m.mat(m.TIMBER)
    planks = [m.mat(p) for p in m.PLANKS]
    iron = kit.mat("GateIron_Metal", "#3a3a40")
    w, h = DOOR[1] - DOOR[0] + 0.3, DOOR_TOP - TOP + 0.15
    parts = []
    k, e = 0, -w / 2
    while e < w / 2 - 0.01:
        pw = min(0.25, w / 2 - e)
        parts.append(m.box("Plank", planks[k % 3], e + 0.005, e + pw - 0.005, -0.06, 0.06, 0.0, h))
        e += pw
        k += 1
    for u in (0.3, h - 0.35):
        parts.append(m.box("Batten", timber, -w / 2 + 0.05, w / 2 - 0.05, -0.12, -0.06, u, u + 0.16))
    parts.append(part("Ring", kit.cylinder(10, 0.08, 0.03), iron, M(G(0, -0.09, h + 0.05), rot=(0, 90, 0))))
    return parts
