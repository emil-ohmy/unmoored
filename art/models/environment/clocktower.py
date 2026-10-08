"""The Stopped Clocktower: a square tower of cold blue-grey ashlar standing in the river where it falls, 16 m across,
five storeys of 4 m, each its own model so the game can hide the storeys above the Apprentice while they're inside
(Storeys). The stairs run straight up one wall a storey and turn the corner the next, so the climb winds round the
tower. Every drop taller than 1.5 m is railed (for now: the Vault comes later).

  "undercroft"  (floor 0.5) the water-gate door in the south wall onto the landing; the river running through the
                undercroft in a channel along the north side (arches in the west and east walls), the great wheel's
                pit in it; four square pillars; the stair up the east wall, rising north
  "works"       (4.5) boards, broken by the pendulum's well and the main shaft's slot; the stair up the north wall,
                rising west; windows on three sides
  "gallery"     (8.5) a ring of boards round an open middle (the bells hang in it, the pendulum passes through), railed
                along its inner edge; the stair up the west wall, rising south
  "face"        (12.5) the room behind the clock face: boards, the pendulum's slot; the dial's square frame in the south
                wall, a door either side of it out onto a railed ledge (west and east), the hands bridging between;
                the stair up the east wall, rising north
  "belfry"      (16.5) flagged, open on all four sides through tall arches (railed); nothing yet
  "roof"        a steep slated pyramid on its cornice
  "landing"     the stone landing before the south door, out in the river

Game directions (see masonry.py); origin at the tower's middle, at the water's surface. Where the game needs them
(MainSceneBuilder.Clocktower): the floors (F), the stairs (STAIRS), the dial's centre (0, -OUT, F[3]), the hands'
tops at F[3], the channel (CHANNEL), the pendulum's pivot (PIVOT).
"""
import masonry as m

VARIANTS = ["undercroft", "works", "gallery", "face", "belfry", "roof", "landing"]
M, part = m.M, m.part
kit = m.kit

OUT, IN = 8.0, 7.2
F = [0.5, 4.5, 8.5, 12.5, 16.5, 20.5]       # the floors (and the roof's foot)
RISE, RUN, STEPS, WIDE = 0.25, 0.32, 16, 1.4
LENGTH = RUN * STEPS                         # 5.12
CHANNEL = (2.5, 5.5)                         # the river's channel across the undercroft, n (from wall to wall)
WELL = (-3.4, 1.2, -1.6, 0.6)                # the pendulum's well through the works (e0, e1, n0, n1)
RING = 4.5                                   # the gallery's open middle: -RING .. RING
DIAL = 3.4                                   # the dial's half-width (its square frame in the south wall)
DOOR = 1.2                                   # half a doorway's width

# The stairs: (storey, e0, e1, n0, n1, rising) - each from its storey's floor up to the next.
STAIRS = {
    0: (IN - WIDE, IN, -3.5, -3.5 + LENGTH, "north"),    # the undercroft's, up the east wall
    1: (3.5 - LENGTH, 3.5, IN - WIDE, IN, "west"),       # the works', up the north wall
    2: (-IN, -IN + WIDE, 3.5 - LENGTH, 3.5, "south"),    # the gallery's, up the west wall
    3: (IN - WIDE, IN, -3.5, -3.5 + LENGTH, "north"),    # the face room's, up the east wall
}


def stair_hole(storey):
    """Where the stair from the storey below comes up through this storey's floor."""
    e0, e1, n0, n1, _ = STAIRS[storey - 1]
    return (e0 - 0.05, e1, n0 - 0.05, n1 + 0.05) if (e1 - e0) < (n1 - n0) else (e0 - 0.05, e1 + 0.05, n0 - 0.05, n1)


def stair(parts, storey):
    """The stair from this storey's floor up to the next: stone steps along the wall, a half-wall capped along its
    open side (stepped, never above the floor it arrives at)."""
    e0, e1, n0, n1, rising = STAIRS[storey]
    u0, top_floor = F[storey], F[storey + 1]
    stone, cap = m.mat(m.STONE[0]), m.mat(m.CAP)
    for i in range(STEPS):
        top = u0 + (i + 1) * RISE
        if rising == "north":
            a, b = n0 + i * RUN, n0 + (i + 1) * RUN
            m.step(parts, e0, e1, a, b, u0, top, tone=i)
            wall = (e0 - 0.16, e0, a, b)
        elif rising == "south":
            a, b = n1 - (i + 1) * RUN, n1 - i * RUN
            m.step(parts, e0, e1, a, b, u0, top, tone=i)
            wall = (e1, e1 + 0.16, a, b)
        else:  # west
            a, b = e1 - (i + 1) * RUN, e1 - i * RUN
            m.step(parts, a, b, n0, n1, u0, top, tone=i)
            wall = (a, b, n0 - 0.16, n0)
        wall_top = min(top + 0.9, top_floor - 0.1)
        parts.append(m.box("StairWall", stone, *wall, u0, wall_top))
        parts.append(m.box("StairCap", cap, wall[0] - 0.02, wall[1] + 0.02, wall[2], wall[3], wall_top, wall_top + 0.1))


def walls(parts, storey, south=(), north=(), west=(), east=()):
    """The storey's four walls, from its floor to the next, with `openings` (a0, a1, b0, b1) in each: a along the
    wall (e for south/north, n for west/east), b heights relative to the storey's floor."""
    u0, u1 = F[storey], F[storey + 1]
    lo = -0.9 if storey == 0 else u0
    def at(openings):
        return [(a0, a1, u0 + b0, u0 + b1) for a0, a1, b0, b1 in openings]
    m.wall(parts, -OUT, OUT, -OUT, -IN, lo, u1, openings=at(south), plinth=storey == 0)
    m.wall(parts, -OUT, OUT, IN, OUT, lo, u1, openings=at(north), plinth=storey == 0)
    m.wall(parts, -OUT, -IN, -IN, IN, lo, u1, openings=at(west), plinth=storey == 0)
    m.wall(parts, IN, OUT, -IN, IN, lo, u1, openings=at(east), plinth=storey == 0)


def window(e_or_n, width=0.5, sill=1.4, head=2.8):
    return (e_or_n - width / 2, e_or_n + width / 2, sill, head)


def build(variant):
    m.clocktower()
    parts = []
    iron = kit.mat("TowerIron_Metal", "#2c2c32")
    timber = m.mat(m.TIMBER)
    if variant == "undercroft":
        f = F[0]
        c0, c1 = CHANNEL
        # The floor (flagged), the channel left open down to the river; earth under it all but the channel.
        parts.append(m.box("Earth", m.mat(m.MORTAR), -IN, IN, -IN, c0, -0.9, f - 0.2))
        parts.append(m.box("Earth", m.mat(m.MORTAR), -IN, IN, c1, IN, -0.9, f - 0.2))
        m.flagstones(parts, -IN, IN, -IN, IN, f, holes=[(-IN, IN, c0, c1)])
        m.wall(parts, -IN, IN, c0 - 0.3, c0, -0.9, f, course=0.3)                      # the channel's sides
        m.wall(parts, -IN, IN, c1, c1 + 0.3, -0.9, f, course=0.3)
        m.balustrade(parts, -IN, c0 - 0.15, IN, c0 - 0.15, f)                          # railed along it
        m.balustrade(parts, -IN, c1 + 0.15, IN, c1 + 0.15, f)
        walls(parts, 0,
              south=[(-DOOR, DOOR, 0, 2.8)],                                           # the water-gate door
              west=[(c0, c1, -1.4, 2.3)], east=[(c0, c1, -1.4, 2.3)])                   # the river's arches
        m.lintel(parts, -DOOR, DOOR, -OUT, -IN, f, f + 2.8)
        for e, n in ((-4.0, -3.6), (4.0, -3.6), (-4.0, 0.4), (4.0, 0.4)):            # the pillars under the works
            m.wall(parts, e - 0.45, e + 0.45, n - 0.45, n + 0.45, f, F[1], course=0.4, cap=False)
            parts.append(m.box("Capital", m.mat(m.CAP), e - 0.6, e + 0.6, n - 0.6, n + 0.6, F[1] - 0.3, F[1] - 0.12))
        # A pipe out of the channel's south side, dripping (the drops hang still in the air: the game's).
        parts.append(part("Pipe", kit.cylinder(8, 0.09, 1.2), iron, M((3.0, -(c0 - 0.6), f + 2.0), rot=(90, 0, 0))))
        stair(parts, 0)
    elif variant == "works":
        f = F[1]
        holes = [stair_hole(1), WELL, (-0.5, 0.5, 3.5, 4.5)]                            # the main shaft's slot
        m.planks(parts, -IN, IN, -IN, IN, f, holes=holes)
        we0, we1, wn0, wn1 = WELL
        m.balustrade(parts, we0, wn0, we1, wn0, f)
        m.balustrade(parts, we0, wn1, we1, wn1, f)
        m.balustrade(parts, we0, wn0, we0, wn1, f)
        m.balustrade(parts, we1, wn0, we1, wn1, f)
        e0, e1, n0, n1, _ = STAIRS[0]
        m.balustrade(parts, e0 - 0.1, n0 - 0.05, e0 - 0.1, n1 - 1.6, f)               # beside the stair from below
        m.balustrade(parts, e0 - 0.1, n0 - 0.1, IN, n0 - 0.1, f)
        walls(parts, 1, south=[window(-4.5), window(4.5)], west=[window(-3.0), window(3.0)], east=[window(4.5)],
              north=[window(-5.5)])
        stair(parts, 1)
        # Beams under the gallery above, across the tower.
        for e in (-4.6, 4.6):
            parts.append(m.box("Beam", timber, e - 0.15, e + 0.15, -IN, IN, F[2] - 0.45, F[2] - 0.2))
    elif variant == "gallery":
        f = F[2]
        m.planks(parts, -IN, IN, -IN, IN, f, holes=[(-RING, RING, -RING, RING), stair_hole(2)])
        # Railed round the open middle (where the stair from below arrives, along the north, the ring is open to it).
        m.balustrade(parts, -RING, -RING, RING, -RING, f)
        m.balustrade(parts, -RING, RING, RING, RING, f)
        m.balustrade(parts, -RING, -RING, -RING, RING, f)
        m.balustrade(parts, RING, -RING, RING, RING, f)
        e0, e1, n0, n1, _ = STAIRS[1]  # (it rises west: its top at e0, open; railed along its side and across its foot)
        m.balustrade(parts, e0 + 1.6, n0 - 0.1, e1 + 0.1, n0 - 0.1, f)
        m.balustrade(parts, e1 + 0.1, n0 - 0.1, e1 + 0.1, IN, f)
        walls(parts, 2, south=[(-DIAL, DIAL, 0.6, 4.0)],                               # the dial's lower half
              west=[window(-5.0)], east=[window(-3.0), window(3.0)], north=[window(-5.5), window(5.5)])
        # The bells' frame: two great beams across the open middle, under the face room's floor.
        for n in (-2.6, 2.6):
            parts.append(m.box("BellBeam", timber, -IN, IN, n - 0.22, n + 0.22, F[3] - 0.7, F[3] - 0.3))
        stair(parts, 2)
    elif variant == "face":
        f = F[3]
        m.planks(parts, -IN, IN, -IN, IN, f, holes=[stair_hole(3), (-1.4, 0.7, -1.2, 0.2), (-0.5, 0.5, 3.5, 4.5)])  # the pendulum's slot, the shaft's
        e0, e1, n0, n1, _ = STAIRS[2]  # (it rises south: its top at n0, open; railed beside its lower part and across its foot)
        m.balustrade(parts, e1 + 0.1, n0 + 1.6, e1 + 0.1, n1 + 0.05, f)
        m.balustrade(parts, -IN, n1 + 0.1, e1 + 0.1, n1 + 0.1, f)
        m.balustrade(parts, -1.4, -1.2, 0.7, -1.2, f)
        m.balustrade(parts, -1.4, 0.2, 0.7, 0.2, f)
        walls(parts, 3, south=[(-DIAL, DIAL, -0.4 - 0.0, DIAL), (-6.0 - DOOR / 1.5, -6.0 + DOOR / 1.5, 0, 2.4),
                               (6.0 - DOOR / 1.5, 6.0 + DOOR / 1.5, 0, 2.4)],
              west=[window(-3.0), window(3.0)], north=[window(-5.0), window(5.0)])
        for e in (-6.0, 6.0):
            m.lintel(parts, e - DOOR / 1.5, e + DOOR / 1.5, -OUT, -IN, f, f + 2.4)
        # The ledges outside, west and east of the dial (the hands bridge between them), railed along their edge.
        for lo, hi in ((-OUT - 0.4, -DIAL + 0.8), (DIAL - 0.8, OUT + 0.4)):
            m.flagstones(parts, lo, hi, -OUT - 1.4, -OUT, f, thickness=0.3)
            parts.append(m.box("Corbel", m.mat(m.CAP), lo, hi, -OUT - 1.4, -OUT, f - 0.6, f - 0.3))
            m.balustrade(parts, lo, -OUT - 1.35, hi, -OUT - 1.35, f)
        m.balustrade(parts, -OUT - 0.35, -OUT - 1.35, -OUT - 0.35, -OUT, f)
        m.balustrade(parts, OUT + 0.35, -OUT - 1.35, OUT + 0.35, -OUT, f)
        stair(parts, 3)
    elif variant == "belfry":
        f = F[4]
        m.flagstones(parts, -IN, IN, -IN, IN, f, holes=[stair_hole(4)])
        e0, e1, n0, n1, _ = STAIRS[3]
        m.balustrade(parts, e0 - 0.1, n0, e0 - 0.1, n1 - 1.6, f)
        m.balustrade(parts, e0 - 0.1, n0 - 0.1, IN, n0 - 0.1, f)
        arch = [(-3.0, 3.0, 0, 3.4)]
        walls(parts, 4, south=arch, north=arch, west=arch, east=arch)
        for line in ((-3.0, -IN - 0.4, 3.0, -IN - 0.4), (-3.0, IN + 0.4, 3.0, IN + 0.4)):
            m.balustrade(parts, *line, f)
        for line in ((-IN - 0.4, -3.0, -IN - 0.4, 3.0), (IN + 0.4, -3.0, IN + 0.4, 3.0)):
            m.balustrade(parts, *line, f)
        for e0_, e1_, n0_, n1_ in ((-3.15, 3.15, -OUT, -IN), (-3.15, 3.15, IN, OUT)):
            parts.append(m.box("ArchHead", m.mat(m.CAP), e0_, e1_, n0_ - 0.05, n1_ + 0.05, f + 3.4, f + 3.65))
        for e0_, e1_, n0_, n1_ in ((-OUT, -IN, -3.15, 3.15), (IN, OUT, -3.15, 3.15)):
            parts.append(m.box("ArchHead", m.mat(m.CAP), e0_ - 0.05, e1_ + 0.05, n0_, n1_, f + 3.4, f + 3.65))
    elif variant == "roof":
        f = F[5]
        slate = kit.mat("Slate", "#2f3238")
        parts.append(m.box("Cornice", m.mat(m.CAP), -OUT - 0.4, OUT + 0.4, -OUT - 0.4, OUT + 0.4, f, f + 0.35))
        parts.append(part("Roof", kit.cone(4, (OUT + 0.6) * 1.414, 0.05, 7.0), slate, M((0, 0, f + 0.35 + 3.5), rot=(0, 0, 45))))
        parts.append(part("Finial", kit.cylinder(6, 0.08, 1.6), iron, M((0, 0, f + 0.35 + 7.6))))
    else:  # landing
        f = F[0]
        parts.append(m.box("Earth", m.mat(m.MORTAR), -4.5, 4.5, -14.0, -OUT, -0.9, f - 0.2))
        m.wall(parts, -4.8, 4.8, -14.3, -14.0, -0.9, f, course=0.45, plinth=True)
        m.wall(parts, -4.8, -4.5, -14.0, -OUT, -0.9, f, course=0.45, plinth=True)
        m.wall(parts, 4.5, 4.8, -14.0, -OUT, -0.9, f, course=0.45, plinth=True)
        m.flagstones(parts, -4.5, 4.5, -14.0, -OUT, f)
    return [kit.join(parts, f"Clocktower_{variant}")]
