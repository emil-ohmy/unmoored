"""The Hall of the Last Hour's keep: a square tower of pale limestone standing in the river where it falls, 16 m across,
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
  "roof"        an octagonal drum on its cornice, a great onion dome of green copper banded in gilt, a gilded spire
  "courtyard"   the paved court before the keep's door, walled in the river: high on the west and either side of
                the keep, a low parapet along the south (towards the camera: nothing tall stands south of where you
                walk); the walls crowned in cogs like the keep
  "gatehouse"   the gate in the court's west wall: a passage out to the stone quay in the river, a low arch at its
                outer end, and the gate tower on its north side, a stopped clock in its south face over the passage
  "hall"        the great hall along the court's east side (30 m), its door onto the court; tall windows east, round
                ones high on the west; tie-beams across it (the pendulums hang from them); a toothed rack inlaid down
                its aisle
  "hall_roof"   its steep copper roof, a row of gears standing along the ridge

Game directions (see masonry.py); origin at the tower's middle, at the water's surface. Where the game needs them
(MainSceneBuilder.LastHour): the floors (F), the stairs (STAIRS), the dial's centre (0, -OUT, F[3]), the hands'
tops at F[3], the channel (CHANNEL), the pendulum's pivot (PIVOT).
"""
import math

import masonry as m

VARIANTS = ["undercroft", "works", "gallery", "face", "belfry", "roof", "courtyard", "gatehouse", "hall", "hall_roof"]
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
    windows = WINDOWS.get(storey, {})
    south, north, west, east = (list(south) + [(a - 0.5, a + 0.5, PORT_U - 0.5, PORT_U + 0.5) for a in windows.get("south", [])],
                                list(north) + [(a - 0.5, a + 0.5, PORT_U - 0.5, PORT_U + 0.5) for a in windows.get("north", [])],
                                list(west) + [(a - 0.5, a + 0.5, PORT_U - 0.5, PORT_U + 0.5) for a in windows.get("west", [])],
                                list(east) + [(a - 0.5, a + 0.5, PORT_U - 0.5, PORT_U + 0.5) for a in windows.get("east", [])])

    def at(openings):
        return [(a0, a1, u0 + b0, u0 + b1) for a0, a1, b0, b1 in openings]
    wainscot(parts, storey, kit.mat("HallBrass_Metal", "#b8913f"), {"south": south, "north": north, "west": west, "east": east})
    m.wall(parts, -OUT, OUT, -OUT, -IN, lo, u1, openings=at(south), plinth=storey == 0)
    m.wall(parts, -OUT, OUT, IN, OUT, lo, u1, openings=at(north), plinth=storey == 0)
    m.wall(parts, -OUT, -IN, -IN, IN, lo, u1, openings=at(west), plinth=storey == 0)
    m.wall(parts, IN, OUT, -IN, IN, lo, u1, openings=at(east), plinth=storey == 0)


# The castle round the keep (game directions, from the keep's middle).
COURT = (-14.0, 16.0, -26.0, -8.0)          # the court's paving (e0, e1, n0, n1)
CURTAIN = 5.0                               # the curtain walls' tops
PARAPET = 2.4                               # the low south wall's top
PIT = (-6.7, -2.3, -23.7, -19.3)            # the gear pit in the court (e0, e1, n0, n1)
PASSAGE = (-19.0, -14.0, -19.0, -15.0)      # the gate's passage through the west wall (e0, e1, n0, n1)
GATE_TOWER = (-19.0, -14.0, -15.0, -10.6)   # the gate tower, north of it
GATE_TOP = 10.0
HALL = (16.0, 28.0, -26.8, 4.0)             # the great hall's walls' outside (e0, e1, n0, n1)
HALL_TOP = 9.5
HALL_DOOR = (-14.0, -11.0)                  # its door in the west wall (n), onto the court
BEAMS = (-20.0, -12.0, -4.0)                # its tie-beams (n): the pendulums hang from them

# The portholes: round brass-rimmed windows (each a square opening behind its ring), per storey and wall.
WINDOWS = {1: {"south": [-2.6, 2.6], "west": [-3.0, 3.0], "east": [4.5], "north": [-5.5]},
           2: {"west": [-5.0], "east": [-3.0, 0.0], "north": [-5.5, 5.5]},
           3: {"west": [-3.0, 3.0], "north": [-5.0, 5.0]}}
PORT_U, PORT_R = 2.0, 0.62  # a porthole's middle above its storey's floor; its ring's radius


def G(e, n, u):
    """A game point in Blender's frame."""
    return (-e, -n, u)


def on_wall(side, a, u, out=0.03):
    """A point on the outside face of a wall: `a` along it (e on south/north, n on west/east), at height u."""
    return {"south": (a, -OUT - out, u), "north": (a, OUT + out, u), "west": (-OUT - out, a, u), "east": (OUT + out, a, u)}[side]


def ring(parts, material, centre, side, radius, thick=0.1, segments=16):
    """A ring of brass in a wall's plane (a porthole's rim)."""
    e, n, u = centre
    for k in range(segments):
        a0, a1 = 2 * math.pi * k / segments, 2 * math.pi * (k + 1) / segments
        pts = []
        for a in (a0, a1):
            dx, du = math.cos(a) * radius, math.sin(a) * radius
            pts.append(G(e + dx, n, u + du) if side in ("south", "north") else G(e, n + dx, u + du))
        parts.append(part("Rim", kit.cube(), material, kit.along(pts[0], pts[1]) @ kit.M(scale=(thick, thick, math.dist(*pts) + 0.02))))


def porthole(parts, storey, side, a, brass, dark):
    """A round window: its brass rim, and its hands, stopped at a quarter past nine (the hour west, the minute east)."""
    u = F[storey] + PORT_U
    c = on_wall(side, a, u, 0.06)
    ring(parts, brass, c, side, PORT_R)
    ring(parts, brass, c, side, PORT_R - 0.14, thick=0.05)
    e, n, _ = c
    if side in ("south", "north"):
        parts.append(m.box("PortHand", dark, e - 0.32, e, n - 0.03, n + 0.03, u - 0.03, u + 0.03))
        parts.append(m.box("PortHand", dark, e, e + 0.46, n - 0.03, n + 0.03, u - 0.025, u + 0.025))
    else:
        parts.append(m.box("PortHand", dark, e - 0.03, e + 0.03, n - 0.32, n, u - 0.03, u + 0.03))
        parts.append(m.box("PortHand", dark, e - 0.03, e + 0.03, n, n + 0.46, u - 0.025, u + 0.025))


def gear(parts, rim, web, centre, radius, teeth, thick, rot=(0, 0, 0)):
    """A brass gear (lying flat at rot (0, 0, 0)), at a game point."""
    at = M(G(*centre), rot=rot)
    parts.append(part("GearRim", kit.cylinder(24, radius, thick), rim, at))
    parts.append(part("GearWeb", kit.cylinder(24, radius - 0.08, thick * 0.6), web, at))
    parts.append(part("GearHub", kit.cylinder(10, radius * 0.22, thick * 1.8), rim, at))
    for k in range(teeth):
        a = 2 * math.pi * k / teeth
        parts.append(part("Cog", kit.cube(), rim, at @ M((math.cos(a) * (radius + 0.07), math.sin(a) * (radius + 0.07), 0),
                                                        rot=(0, 0, math.degrees(a)), scale=(0.16, 0.12, thick))))


def cog_band(parts, u, brass):
    """Round the wall tops at height u: a brass band with cog teeth all round, the storey a gear in a stack of gears."""
    o, w = OUT + 0.16, OUT + 0.38
    for side in ("south", "north", "west", "east"):
        if side in ("south", "north"):
            s_ = -1 if side == "south" else 1
            parts.append(m.box("CogBand", brass, -o, o, min(s_ * OUT, s_ * o), max(s_ * OUT, s_ * o), u - 0.62, u - 0.2))
            a = -OUT + 0.4
            while a < OUT - 0.3:
                parts.append(m.box("Cog", brass, a - 0.16, a + 0.16, min(s_ * o, s_ * w), max(s_ * o, s_ * w), u - 0.58, u - 0.24))
                a += 0.72
        else:
            s_ = -1 if side == "west" else 1
            parts.append(m.box("CogBand", brass, min(s_ * OUT, s_ * o), max(s_ * OUT, s_ * o), -OUT, OUT, u - 0.62, u - 0.2))
            a = -OUT + 0.4
            while a < OUT - 0.3:
                parts.append(m.box("Cog", brass, min(s_ * o, s_ * w), max(s_ * o, s_ * w), a - 0.16, a + 0.16, u - 0.58, u - 0.24))
                a += 0.72


def spines(parts, storey, brass, web):
    """A clockwork spine up each corner: a brass column, and gears along it facing out across the corner."""
    u0, u1 = F[storey], F[storey + 1]
    lo = -0.9 if storey == 0 else u0
    for sx in (-1, 1):
        for sy in (-1, 1):
            parts.append(part("Spine", kit.cylinder(10, 0.2, u1 - lo), brass, M(G(sx * (OUT + 0.12), sy * (OUT + 0.12), (lo + u1) / 2))))
            yaw = math.degrees(math.atan2(-sy, -sx)) + 90
            for k, du in enumerate((1.2, 3.0)):
                gear(parts, brass, web, (sx * (OUT + 0.34), sy * (OUT + 0.34), u0 + du), 0.6 if k == 0 else 0.42, 10 if k == 0 else 8, 0.12,
                     rot=(90, 0, yaw))


def pipes(parts, storey, brass):
    """Brass pipes up the outside of the walls, with a collar every storey."""
    u0, u1 = F[storey], F[storey + 1]
    lo = -0.6 if storey == 0 else u0
    for side, a in (("south", -7.35), ("south", 7.35), ("east", 6.6), ("west", -6.6), ("north", 0.0)):
        e, n, _ = on_wall(side, a, 0, 0.22)
        parts.append(part("Pipe", kit.cylinder(8, 0.13, u1 - lo), brass, M(G(e, n, (lo + u1) / 2))))
        parts.append(part("PipeCollar", kit.cylinder(8, 0.19, 0.12), brass, M(G(e, n, u0 + 0.3))))


def cog_strip(parts, brass, e0, e1, n0, n1, u, face):
    """A cogged brass band along a straight wall's top (just under it), on one face ("south", "north", "west", "east":
    the face it stands out from): the band, and its teeth."""
    along_e = face in ("south", "north")
    out = {"south": -1, "north": 1, "west": -1, "east": 1}[face]
    edge = (n0 if face == "south" else n1) if along_e else (e0 if face == "west" else e1)
    band = sorted((edge, edge + out * 0.16))
    teeth = sorted((edge + out * 0.16, edge + out * 0.38))
    lo, hi = (e0, e1) if along_e else (n0, n1)
    if along_e:
        parts.append(m.box("CogBand", brass, lo, hi, *band, u - 0.62, u - 0.2))
    else:
        parts.append(m.box("CogBand", brass, *band, lo, hi, u - 0.62, u - 0.2))
    a = lo + 0.4
    while a < hi - 0.3:
        if along_e:
            parts.append(m.box("Cog", brass, a - 0.16, a + 0.16, *teeth, u - 0.58, u - 0.24))
        else:
            parts.append(m.box("Cog", brass, *teeth, a - 0.16, a + 0.16, u - 0.58, u - 0.24))
        a += 0.72


def wainscot(parts, storey, brass, openings):
    """A brass band round the room inside, at hand height, broken at every opening it would cross (`openings`: per
    side, (a0, a1, b0, b1) as walls() has them, b relative to the floor)."""
    u0, b = F[storey], 1.0
    for side, ops in openings.items():
        cuts = sorted((a0, a1) for a0, a1, b0, b1 in ops if b0 <= b + 0.16 and b1 >= b)
        a = -IN
        spans = []
        for a0, a1 in cuts:
            if a0 > a:
                spans.append((a, a0))
            a = max(a, a1)
        if a < IN:
            spans.append((a, IN))
        for lo, hi in spans:
            if hi - lo < 0.05:
                continue
            box = {"south": (lo, hi, -IN, -IN + 0.04), "north": (lo, hi, IN - 0.04, IN),
                   "west": (-IN, -IN + 0.04, lo, hi), "east": (IN - 0.04, IN, lo, hi)}[side]
            parts.append(m.box("Band", brass, *box, u0 + b, u0 + b + 0.16))


def build(variant):
    m.last_hour()
    parts = []
    iron = kit.mat("HallBrass_Metal", "#b8913f")
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
        walls(parts, 1)
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
        walls(parts, 2, south=[(-DIAL, DIAL, 0.6, 4.0)])                               # the dial's lower half
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
                               (6.0 - DOOR / 1.5, 6.0 + DOOR / 1.5, 0, 2.4)])
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
        copper = kit.mat("Verdigris", "#5f9e93")
        gilt = kit.mat("Gilt_Metal", "#e0bd62")
        parts.append(m.box("Cornice", m.mat(m.CAP), -OUT - 0.4, OUT + 0.4, -OUT - 0.4, OUT + 0.4, f, f + 0.35))
        parts.append(m.box("Roof", copper, -OUT - 0.2, OUT + 0.2, -OUT - 0.2, OUT + 0.2, f + 0.35, f + 0.6))
        base = f + 0.6
        parts.append(part("Drum", kit.cylinder(8, 6.0, 2.2), m.mat(m.STONE[1]), M((0, 0, base + 1.1), rot=(0, 0, 22.5))))
        parts.append(part("DrumBand", kit.cylinder(8, 6.15, 0.25), gilt, M((0, 0, base + 2.2), rot=(0, 0, 22.5))))
        # The dome sits on a toothed crown: a gear ring round the drum's top (the dome a cap that could turn).
        for k in range(40):
            ang = 2 * math.pi * k / 40
            parts.append(part("CrownCog", kit.cube(), gilt, M((math.cos(ang) * 6.45, math.sin(ang) * 6.45, base + 2.05),
                                                             rot=(0, 0, math.degrees(ang)), scale=(0.5, 0.26, 0.42))))
        # The onion: a lathe of rings, swelling out past the drum and drawing up to a point.
        profile = [(0.0, 5.6), (0.8, 6.3), (1.8, 6.5), (2.8, 6.0), (3.8, 4.6), (4.7, 2.8), (5.4, 1.3), (6.0, 0.45), (6.4, 0.12)]
        dome_base = base + 2.3
        parts.append(part("Dome", kit.tube([(0, 0, dome_base + z) for z, _ in profile], [r for _, r in profile], seg=16),
                          copper, smooth=True))
        parts.append(part("DomeBand", kit.cylinder(16, 6.55, 0.22), gilt, M((0, 0, dome_base + 1.8))))
        for k in range(8):  # gilt ribs up the dome
            a = 2 * math.pi * k / 8 + 0.2
            pts = [(r * 1.01 * math.cos(a), r * 1.01 * math.sin(a), dome_base + z) for z, r in profile[1:-1]]
            parts.append(part("Rib", kit.tube(pts, [0.09] * len(pts), seg=4), gilt))
        top = dome_base + 6.4
        parts.append(part("Spire", kit.cone(8, 0.22, 0.02, 4.0), gilt, M((0, 0, top + 2.0))))
        parts.append(part("Orb", kit.sphere(10, 8, 0.38), gilt, M((0, 0, top + 0.6))))
    elif variant == "courtyard":
        f = F[0]
        brass = kit.mat("HallBrass_Metal", "#b8913f")
        e0, e1, n0, n1 = COURT
        q0, q1, qn0, qn1 = PIT
        for box in ((e0, q0, n0, n1), (q1, e1, n0, n1), (q0, q1, n0, qn0), (q0, q1, qn1, n1)):   # (round the pit)
            parts.append(m.box("Earth", m.mat(m.MORTAR), *box, -0.9, f - 0.2))
        m.flagstones(parts, e0, e1, n0, n1, f, holes=[PIT])
        # The gear well: a square well sunk in the paving (floored just above the river's level: nothing deeper shows
        # under the water), a great gear lying in it and its pinion, railed round.
        web = kit.mat("HallBrassWeb_Metal", "#9c7a38")
        low = 0.06
        parts.append(m.box("PitFloor", m.mat(m.PLINTH), q0, q1, qn0, qn1, -0.9, low))
        for box in ((q0, q1, qn0, qn0 + 0.25), (q0, q1, qn1 - 0.25, qn1), (q0, q0 + 0.25, qn0, qn1), (q1 - 0.25, q1, qn0, qn1)):
            parts.append(m.box("PitSide", m.mat(m.STONE[0]), *box, low, f))
        cx, cn = (q0 + q1) / 2, (qn0 + qn1) / 2
        gear(parts, brass, web, (cx - 0.3, cn - 0.2, low + 0.16), 1.6, 26, 0.2)
        gear(parts, brass, web, (cx + 1.35, cn + 1.1, low + 0.36), 0.55, 10, 0.18)
        parts.append(part("PitHub", kit.cylinder(10, 0.3, 0.5), brass, M(G(cx - 0.3, cn - 0.2, low + 0.25))))
        m.balustrade(parts, q0, qn0, q1, qn0, f)
        m.balustrade(parts, q0, qn1, q1, qn1, f)
        m.balustrade(parts, q0, qn0, q0, qn1, f)
        m.balustrade(parts, q1, qn0, q1, qn1, f)
        # A brass ring set in the paving before the keep's door, and a rack running to it from the gate.
        parts.append(m.box("Rack", brass, -0.4, 0.4, n0 + 1.0, n1 - 3.6, f, f + 0.03))
        n = n0 + 1.2
        while n < n1 - 3.8:
            parts.append(m.box("RackCog", brass, -0.62, -0.4, n - 0.12, n + 0.12, f, f + 0.03))
            parts.append(m.box("RackCog", brass, 0.4, 0.62, n - 0.12, n + 0.12, f, f + 0.03))
            n += 0.6
        # The curtain walls, standing in the river: high on the west (broken by the gate) and either side of the keep;
        # a low parapet along the south. Crowned in cogs on both faces.
        p0, p1, pn0, pn1 = PASSAGE
        _, _, tn0, tn1 = GATE_TOWER
        walls_ = [(e0 - 0.8, e0, n0 - 0.8, pn0 - 0.8, CURTAIN, ("west", "east")),            # west, south of the gate
                  (e0 - 0.8, e0, tn1, n1 + 0.8, CURTAIN, ("west", "east")),                  # west, north of its tower
                  (e0 - 0.8, -OUT, n1, n1 + 0.8, CURTAIN, ("south", "north")),               # to the keep's south-west corner
                  (OUT, e1, n1, n1 + 0.8, CURTAIN, ("south", "north")),                      # keep to hall
                  (e0 - 0.8, e1, n0 - 0.8, n0, PARAPET, ("south", "north"))]                 # the south parapet
        for we0, we1, wn0, wn1, top, faces in walls_:
            m.wall(parts, we0, we1, wn0, wn1, -0.9, top, course=0.45, plinth=True)
            for face in faces:
                cog_strip(parts, brass, we0, we1, wn0, wn1, top, face)
    elif variant == "gatehouse":
        f = F[0]
        brass = kit.mat("HallBrass_Metal", "#b8913f")
        p0, p1, pn0, pn1 = PASSAGE
        g0, g1, gn0, gn1 = GATE_TOWER
        t = 0.7
        # The passage's floor, its low south wall, the arch at its outer end.
        parts.append(m.box("Earth", m.mat(m.MORTAR), p0, p1, pn0, pn1, -0.9, f - 0.2))
        m.flagstones(parts, p0, p1, pn0, pn1, f)
        m.wall(parts, p0, p1, pn0 - 0.8, pn0, -0.9, PARAPET, course=0.45, plinth=True)
        cog_strip(parts, brass, p0, p1, pn0 - 0.8, pn0, PARAPET, "south")
        cog_strip(parts, brass, p0, p1, pn0 - 0.8, pn0, PARAPET, "north")
        for n_ in (pn0 - 0.8, pn1 - 0.1):
            m.wall(parts, p0, p0 + 0.9, n_, n_ + 0.9, -0.9, 4.6, course=0.45, plinth=True)  # the arch's piers
        parts.append(m.box("Arch", m.mat(m.CAP), p0 - 0.05, p0 + 0.95, pn0 - 0.85, pn1 + 0.85, 4.2, 4.6))
        cog_strip(parts, brass, p0, p0 + 0.9, pn0 - 0.8, pn1 + 0.8, 4.6, "west")
        # The gate tower (hollow: walls round an inside), its cogged crown; the clock's frame in its south face.
        m.wall(parts, g0, g1, gn0, gn0 + t, -0.9, GATE_TOP, course=0.45, plinth=True)
        m.wall(parts, g0, g1, gn1 - t, gn1, -0.9, GATE_TOP, course=0.45, plinth=True)
        m.wall(parts, g0, g0 + t, gn0 + t, gn1 - t, -0.9, GATE_TOP, course=0.45, plinth=True)
        m.wall(parts, g1 - t, g1, gn0 + t, gn1 - t, -0.9, GATE_TOP, course=0.45, plinth=True)
        parts.append(m.box("TowerTop", m.mat(m.CAP), g0 + t, g1 - t, gn0 + t, gn1 - t, GATE_TOP - 0.3, GATE_TOP - 0.1))
        for face in ("south", "north", "west", "east"):
            cog_strip(parts, brass, g0, g1, gn0, gn1, GATE_TOP, face)
        # The quay outside, in the river.
        q0 = p0 - 4.5
        parts.append(m.box("Earth", m.mat(m.MORTAR), q0, p0, pn0 - 1.0, pn1 + 1.0, -0.9, f - 0.2))
        m.wall(parts, q0 - 0.3, q0, pn0 - 1.3, pn1 + 1.3, -0.9, f, course=0.45, plinth=True)
        m.wall(parts, q0, p0, pn0 - 1.3, pn0 - 1.0, -0.9, f, course=0.45, plinth=True)
        m.wall(parts, q0, p0, pn1 + 1.0, pn1 + 1.3, -0.9, f, course=0.45, plinth=True)
        m.flagstones(parts, q0, p0, pn0 - 1.0, pn1 + 1.0, f)
    elif variant == "hall":
        f = F[0]
        brass = kit.mat("HallBrass_Metal", "#b8913f")
        h0, h1, hn0, hn1 = HALL
        i0, i1, in0, in1 = h0 + 0.8, h1 - 0.8, hn0 + 0.8, hn1 - 0.8
        parts.append(m.box("Earth", m.mat(m.MORTAR), i0, i1, in0, in1, -0.9, f - 0.2))
        m.flagstones(parts, i0, i1, in0, in1, f)
        # The rack down its aisle (to the orrery at its north end).
        mid = (i0 + i1) / 2
        parts.append(m.box("Rack", brass, mid - 0.35, mid + 0.35, in0 + 1.0, in1 - 4.0, f, f + 0.03))
        n = in0 + 1.2
        while n < in1 - 4.2:
            parts.append(m.box("RackCog", brass, mid - 0.58, mid - 0.35, n - 0.12, n + 0.12, f, f + 0.03))
            parts.append(m.box("RackCog", brass, mid + 0.35, mid + 0.58, n - 0.12, n + 0.12, f, f + 0.03))
            n += 0.6
        tall = [(n_ - 0.6, n_ + 0.6, f + 1.6, f + 5.6) for n_ in (-20.0, -12.0, -4.0, 1.5)]
        high = [(n_ - 0.5, n_ + 0.5, f + 6.0, f + 7.0) for n_ in (-22.0, -6.0)]
        door = [(HALL_DOOR[0], HALL_DOOR[1], f, f + 4.2)]
        m.wall(parts, h0, i0, in0, in1, -0.9, HALL_TOP, openings=door + high, plinth=True)      # west (onto the court)
        m.lintel(parts, h0, i0, HALL_DOOR[0], HALL_DOOR[1], f, f + 4.2)
        m.wall(parts, i1, h1, in0, in1, -0.9, HALL_TOP, openings=tall, plinth=True)              # east
        m.wall(parts, h0, h1, hn0, in0, -0.9, HALL_TOP, plinth=True)                              # south
        m.wall(parts, h0, h1, in1, hn1, -0.9, HALL_TOP, plinth=True)                              # north
        for face, box in (("west", (h0, i0, in0, in1)), ("east", (i1, h1, in0, in1)), ("south", (h0, h1, hn0, in0)), ("north", (h0, h1, in1, hn1))):
            cog_strip(parts, brass, *box, HALL_TOP, face)
        for n_ in BEAMS:                                                                         # the tie-beams
            parts.append(m.box("TieBeam", m.mat(m.TIMBER), i0, i1, n_ - 0.2, n_ + 0.2, HALL_TOP - 1.0, HALL_TOP - 0.6))
            parts.append(m.box("BeamBrass", brass, i0, i1, n_ - 0.22, n_ + 0.22, HALL_TOP - 1.04, HALL_TOP - 0.98))
    elif variant == "hall_roof":
        h0, h1, hn0, hn1 = HALL
        brass = kit.mat("HallBrass_Metal", "#b8913f")
        web = kit.mat("HallBrassWeb_Metal", "#9c7a38")
        copper = kit.mat("Verdigris", "#5f9e93")
        gilt = kit.mat("Gilt_Metal", "#e0bd62")
        mid, half = (h0 + h1) / 2, (h1 - h0) / 2 + 0.5
        rise = 5.0
        slope = math.degrees(math.atan2(rise, half))
        length = (hn1 - hn0) + 1.0
        run = math.hypot(half, rise)
        base = HALL_TOP
        for side in (-1, 1):  # the two slopes
            c = (mid + side * half / 2, (hn0 + hn1) / 2, base + rise / 2)
            parts.append(part("RoofSlope", kit.cube(), copper, M(G(*c), rot=(0, side * slope, 0), scale=(run, length, 0.25))))
        for n_ in (hn0 - 0.45, hn1 + 0.45):  # the gables, stepped
            for k in range(6):
                w = half * (1 - k / 6)
                parts.append(m.box("Gable", m.mat(m.STONE[k % 3]), mid - w, mid + w, n_ - 0.3, n_ + 0.3, base + k * rise / 6, base + (k + 1) * rise / 6))
        parts.append(m.box("Ridge", gilt, mid - 0.2, mid + 0.2, hn0 - 0.6, hn1 + 0.6, base + rise - 0.1, base + rise + 0.15))
        n_ = hn0 + 2.0
        while n_ < hn1 - 1.0:  # gears standing along the ridge, their axles along it
            gear(parts, brass, web, (mid, n_, base + rise + 0.9), 0.9, 12, 0.14, rot=(90, 0, 0))
            n_ += 3.8
    # The machine of it: each storey's cogged crown, its corner spines and pipes, its portholes, its brass band inside;
    # a great gear inlaid in the floors (the belfry's floor is one).
    brass = kit.mat("HallBrass_Metal", "#b8913f")
    web = kit.mat("HallBrassWeb_Metal", "#9c7a38")
    dark = kit.mat("HandDark", "#2a2620")
    storey = VARIANTS.index(variant)
    if storey <= 4:
        cog_band(parts, F[storey + 1], brass)
        spines(parts, storey, brass, web)
        pipes(parts, storey, brass)
        for side, positions in WINDOWS.get(storey, {}).items():
            for a in positions:
                porthole(parts, storey, side, a, brass, dark)
    inlay = {0: ((0.0, -3.4), 2.2), 3: ((2.8, -3.6), 1.6), 4: ((-1.0, -1.0), 4.2)}.get(storey)
    if inlay is not None:
        (e, n), r = inlay
        gear(parts, brass, web, (e, n, F[storey] + 0.01), r, int(r * 11), 0.03)
    return [kit.join(parts, f"LastHour_{variant}")]
