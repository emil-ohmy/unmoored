"""The Hour's chamber (the One Who Waits: the Hall of the Last Hour's Boss): the room behind the great dial, inside the
clock. Its floor is a dial too, a disc of marble 19 m across with a brass chapter ring and Roman numerals round its edge,
the Hour hanging over its middle (the game hangs it, and turns the floor dial's hands). The north wall is the great dial
itself, seen from behind: a round of pale glass 11 m across, lit through from outside, its numerals reversed, its
hands stopped at three minutes to twelve. Tall walls on the west and east; to the south (towards the camera) only a low
parapet round the floor's edge, the dark of the works below, and a narrow bridge in from the south, off a landing. Over the middle a
beam out from the dial wall carries the Hour's contraption (a crown of brass gears its cables run up into); two lower
beams west and east of it carry the pendulums (the game's), swinging north and south.

Variants: "chamber"; "hand_long" and "hand_short" (the floor dial's hands: flat iron bars lying on the floor, 0.3 m
high, pointing north from their pivot: origin at the pivot, on the floor; 9 m and 6 m long).
Game directions (masonry: E east, N north, U up); origin at the floor's middle, on the floor.
"""
import math

import masonry as m

VARIANTS = ["chamber", "hand_long", "hand_short"]
M, part = m.M, m.part
kit = m.kit

R = 9.6            # the floor disc's radius
WALL_N = 8.0       # the dial wall's face (n)
DIAL_R, DIAL_U = 5.5, 6.8   # the great dial's radius, and its middle's height
BRIDGE = 1.2       # half the bridge's width
BEAM_U = 11.2      # the contraption's beam
BLADE_N, BLADE_U = 4.6, 8.2   # the pendulums' beams (e either side), their pivots' height


def G(e, n, u):
    return (-e, -n, u)


def glyph(parts, material, numeral, centre, angle, size, thick, u, upright=False):
    """A Roman numeral in bars, centred at `centre` (e, n), its top towards `angle` (radians, from north, clockwise
    in game: east of north); lying in the floor (u its top), or `upright` in a north-facing plane (reversed: seen
    from behind)."""
    letters = list(numeral)
    width = {"I": 0.28, "V": 0.6, "X": 0.6}
    total = sum(width[c] for c in letters) * size + 0.12 * size * (len(letters) - 1)
    x = -total / 2
    for c in letters:
        w = width[c] * size
        bars = {"I": [((0, -0.5), (0, 0.5))], "V": [((-0.5, 0.5), (0, -0.5)), ((0.5, 0.5), (0, -0.5))],
                "X": [((-0.5, 0.5), (0.5, -0.5)), ((0.5, 0.5), (-0.5, -0.5))]}[c]
        for (a0, b0), (a1, b1) in bars:
            p0 = (x + w / 2 + a0 * w, b0 * size)
            p1 = (x + w / 2 + a1 * w, b1 * size)
            mid = ((p0[0] + p1[0]) / 2, (p0[1] + p1[1]) / 2)
            length = math.dist(p0, p1)
            tilt = math.degrees(math.atan2(p1[0] - p0[0], p1[1] - p0[1]))
            if upright:
                # In the dial's plane (e, u), reversed (from behind): x runs west.
                e, uu = centre[0] - mid[0], centre[1] + mid[1]
                parts.append(part("Numeral", kit.cube(), material, M(G(e, WALL_N - 0.02, uu), rot=(0, tilt, 0), scale=(0.09 * size, 0.04, length))))
            else:
                # In the floor: rotate the glyph's frame (x across, y towards its top) to face `angle`.
                ca, sa = math.cos(angle), math.sin(angle)
                e = centre[0] + mid[0] * ca + mid[1] * sa
                n = centre[1] - mid[0] * sa + mid[1] * ca
                yaw = math.degrees(angle) + tilt
                parts.append(part("Numeral", kit.cube(), material, M(G(e, n, u - thick / 2), rot=(0, 0, -yaw), scale=(0.1 * size, length, thick))))
        x += w + 0.12 * size


def build(variant):
    m.last_hour()
    parts = []
    brass = kit.mat("HallBrass_Metal", "#b8913f")
    iron = kit.mat("FloorHandIron_Metal", "#2f3038")
    gilt = kit.mat("Gilt_Metal", "#e0bd62")
    if variant in ("hand_long", "hand_short"):
        length, wide = (9.0, 0.36) if variant == "hand_long" else (6.0, 0.5)
        # A flat bar along +n from the pivot, a spade near its tip, a tail and counterweight behind; low.
        parts.append(m.box("Bar", iron, -wide / 2, wide / 2, -1.2, length - 0.6, 0.0, 0.3))
        parts.append(part("Spade", kit.cylinder(4, wide * 1.5, 0.3), iron, M(G(0, length - 1.0, 0.15), rot=(0, 0, 45), scale=(1, 1.4, 1))))
        parts.append(part("Tip", kit.cone(4, wide * 0.7, 0.02, 0.3), iron, M(G(0, length - 0.35, 0.15), rot=(90, 0, 45))))
        parts.append(part("Counterweight", kit.cylinder(12, wide * 1.3, 0.3), iron, M(G(0, -1.2, 0.15))))
        parts.append(m.box("Edge", brass, -wide / 2 - 0.02, wide / 2 + 0.02, -1.0, length - 0.8, 0.28, 0.33))
        parts.append(part("Pivot", kit.cylinder(16, 0.45, 0.4), brass, M((0, 0, 0.2))))
        return [kit.join(parts, f"TheHour_{variant}")]

    marble = kit.mat("DialMarble", "#d8cfb8")
    marble_dark = kit.mat("DialMarbleVein", "#b9ad92")
    numeral = kit.mat("DialNumeral", "#2a2620")
    glass = kit.mat("GreatDial_Glow", "#7a7262")  # (lit from outside: a dim pale glow; the light's the game's)
    stone = m.mat(m.STONE[0])
    cap = m.mat(m.CAP)
    timber = m.mat(m.TIMBER)
    # The floor: a marble disc on a stone drum going down into the dark, the dial in it.
    parts.append(part("Floor", kit.cylinder(64, R, 0.4), marble, M((0, 0, -0.2))))
    parts.append(part("FloorDrum", kit.cylinder(64, R - 0.3, 3.0), stone, M((0, 0, -1.9))))
    for r, w, mat in ((R - 0.15, 0.18, brass), (R - 2.6, 0.08, brass), (R - 2.9, 0.05, brass), (2.2, 0.06, marble_dark), (4.6, 0.04, marble_dark)):
        n = 72
        for k in range(n):  # (a ring of short boxes: flush, a hair proud)
            a0, a1 = 2 * math.pi * k / n, 2 * math.pi * (k + 1) / n
            p0 = (math.sin(a0) * r, math.cos(a0) * r)
            p1 = (math.sin(a1) * r, math.cos(a1) * r)
            mid = ((p0[0] + p1[0]) / 2, (p0[1] + p1[1]) / 2)
            yaw = math.degrees(math.atan2(p1[0] - p0[0], p1[1] - p0[1]))
            parts.append(part("Ring", kit.cube(), mat, M(G(mid[0], mid[1], 0.005), rot=(0, 0, -yaw), scale=(w, math.dist(p0, p1) + 0.02, 0.012))))
    names = ["XII", "I", "II", "III", "IIII", "V", "VI", "VII", "VIII", "IX", "X", "XI"]
    for k, name in enumerate(names):
        a = 2 * math.pi * k / 12
        c = (math.sin(a) * (R - 1.35), math.cos(a) * (R - 1.35))
        glyph(parts, numeral, name, c, a, 1.0, 0.012, 0.012)
    for k in range(60):  # the minute marks between the rings
        if k % 5 == 0:
            continue
        a = 2 * math.pi * k / 60
        c = (math.sin(a) * (R - 0.45), math.cos(a) * (R - 0.45))
        parts.append(part("Minute", kit.cube(), numeral, M(G(c[0], c[1], 0.006), rot=(0, 0, -math.degrees(a)), scale=(0.06, 0.4, 0.012))))
    parts.append(part("Boss", kit.cylinder(24, 1.0, 0.08), brass, M((0, 0, 0.04))))
    # The dial wall: the great dial seen from behind (glass lit through, numerals reversed, hands at three to twelve),
    # in a wall of stone, tall; a gallery ledge under it.
    parts.append(m.box("Footing", stone, -R - 0.6, R + 0.6, WALL_N, WALL_N + 1.0, -4.0, 0.0))
    m.wall(parts, -R - 0.6, R + 0.6, WALL_N, WALL_N + 1.0, 0.0, 14.0, openings=[(-DIAL_R, DIAL_R, DIAL_U - DIAL_R, DIAL_U + DIAL_R)], course=0.5)
    parts.append(part("DialGlass", kit.cylinder(48, DIAL_R + 0.1, 0.06), glass, M(G(0, WALL_N + 0.5, DIAL_U), rot=(90, 0, 0))))
    for r in (DIAL_R, DIAL_R - 0.9):
        n = 64
        for k in range(n):
            a0, a1 = 2 * math.pi * k / n, 2 * math.pi * (k + 1) / n
            p0 = G(math.sin(a0) * r, WALL_N + 0.1, DIAL_U + math.cos(a0) * r)
            p1 = G(math.sin(a1) * r, WALL_N + 0.1, DIAL_U + math.cos(a1) * r)
            parts.append(part("DialRing", kit.cube(), brass, kit.along(p0, p1) @ kit.M(scale=(0.16, 0.16, math.dist(p0, p1) + 0.02))))
    for k, name in enumerate(names):
        a = 2 * math.pi * k / 12
        glyph(parts, numeral, name, (-math.sin(a) * (DIAL_R - 0.5), DIAL_U + math.cos(a) * (DIAL_R - 0.5)), a, 0.7, 0.04, 0, upright=True)
    for angle, length, wide in ((-1.5, DIAL_R - 1.6, 0.32), (-18, DIAL_R - 0.6, 0.2)):  # (from behind: the hour at twelve, the minute three minutes short)
        a = math.radians(angle)
        tip = (-math.sin(a) * length, DIAL_U + math.cos(a) * length)
        parts.append(part("DialHand", kit.cube(), numeral, M(G(tip[0] / 2, WALL_N + 0.2, (DIAL_U + tip[1]) / 2), rot=(0, math.degrees(a), 0), scale=(wide, 0.06, length))))
    parts.append(part("DialArbor", kit.cylinder(12, 0.35, 0.4), brass, M(G(0, WALL_N + 0.2, DIAL_U), rot=(90, 0, 0))))
    # The side walls, tall, west and east, from the dial wall round to the floor's middle line.
    for side in (-1, 1):
        e0, e1 = (-R - 0.6, -R + 0.2) if side < 0 else (R - 0.2, R + 0.6)
        m.wall(parts, e0, e1, -1.0, WALL_N, -4.0, 12.0, course=0.5)
        parts.append(m.box("Pilaster", cap, e0 - 0.1 * side, e1 - 0.1 * side, -1.4, -0.6, -4.0, 12.2))
    # South: the floor's edge, a low parapet round it (open for the bridge), the bridge going south into the dark.
    seg = 28
    for k in range(seg + 1):
        a0 = math.radians(90 + 180 * k / seg)
        a1 = math.radians(90 + 180 * (k + 1) / seg)
        if k == seg:
            break
        mid = (a0 + a1) / 2
        e_mid = math.sin(mid) * (R - 0.15)
        if abs(e_mid) < BRIDGE + 0.2 and math.cos(mid) < 0:
            continue  # (the bridge's gap)
        p0 = (math.sin(a0) * (R - 0.15), math.cos(a0) * (R - 0.15))
        p1 = (math.sin(a1) * (R - 0.15), math.cos(a1) * (R - 0.15))
        m.balustrade(parts, p0[0], p0[1], p1[0], p1[1], 0.0)
    m.flagstones(parts, -BRIDGE, BRIDGE, -R - 5.0, -R + 0.4, 0.0)
    parts.append(m.box("BridgeArch", stone, -BRIDGE, BRIDGE, -R - 5.0, -R + 0.4, -1.2, -0.2))
    for side in (-1, 1):
        m.balustrade(parts, side * (BRIDGE + 0.1), -R - 5.0, side * (BRIDGE + 0.1), -R + 0.2, 0.0)
    # The landing at the bridge's south end (where you come in: the lab's posts stand on it), railed.
    l0, l1, ln0, ln1 = -3.6, 3.6, -R - 9.0, -R - 5.0
    m.flagstones(parts, l0, l1, ln0, ln1, 0.0)
    parts.append(m.box("LandingPier", stone, l0, l1, ln0, ln1, -3.0, -0.2))
    m.balustrade(parts, l0 + 0.1, ln0 + 0.1, l1 - 0.1, ln0 + 0.1, 0.0)
    m.balustrade(parts, l0 + 0.1, ln0 + 0.1, l0 + 0.1, ln1, 0.0)
    m.balustrade(parts, l1 - 0.1, ln0 + 0.1, l1 - 0.1, ln1, 0.0)
    m.balustrade(parts, l0 + 0.1, ln1 - 0.1, -BRIDGE - 0.1, ln1 - 0.1, 0.0)
    m.balustrade(parts, BRIDGE + 0.1, ln1 - 0.1, l1 - 0.1, ln1 - 0.1, 0.0)
    # Below: the works in the dark (gears, seen past the parapet).
    for e, n, u, r in ((-6.5, -9.5, -5.0, 3.2), (5.5, -10.5, -6.5, 4.0), (0.0, -13.0, -8.0, 3.0)):
        parts.append(part("DeepGear", kit.cylinder(20, r, 0.4), brass, M(G(e, n, u), rot=(10, 0, 0))))
        for k in range(int(r * 6)):
            a = 2 * math.pi * k / int(r * 6)
            parts.append(part("DeepCog", kit.cube(), brass, M(G(e + math.cos(a) * (r + 0.2), n + math.sin(a) * (r + 0.2) * 0.98, u), rot=(10, 0, -math.degrees(a)), scale=(0.4, 0.3, 0.4))))
    # The beams, out from the dial wall (north to south: seen end-on from the camera, never across the Hour): the
    # contraption's to over the middle; the pendulums' west and east of it, braced from the wall.
    parts.append(m.box("Beam", timber, -0.35, 0.35, -1.0, WALL_N, BEAM_U, BEAM_U + 0.7))
    parts.append(m.box("BeamStrap", brass, -0.37, 0.37, -1.0, WALL_N, BEAM_U - 0.04, BEAM_U + 0.02))
    parts.append(part("Brace", kit.cube(), timber, kit.along(G(0, WALL_N, BEAM_U - 4.0), G(0, 2.0, BEAM_U)) @ kit.M(scale=(0.4, 0.4, math.dist((WALL_N, BEAM_U - 4.0), (2.0, BEAM_U))))))
    for e in (-BLADE_N, BLADE_N):
        parts.append(m.box("BladeBeam", timber, e - 0.25, e + 0.25, -5.5, WALL_N, BLADE_U + 0.2, BLADE_U + 0.6))
        parts.append(part("Brace", kit.cube(), timber, kit.along(G(e, WALL_N, BLADE_U - 3.0), G(e, 3.0, BLADE_U + 0.2)) @ kit.M(scale=(0.3, 0.3, math.dist((WALL_N, BLADE_U - 3.0), (3.0, BLADE_U + 0.2))))))
    # The contraption: a crown of brass gears hung from the beam, its drum (the cables go up into it).
    parts.append(part("Drum", kit.cylinder(16, 0.6, 1.0), brass, M((0, 0, BEAM_U - 0.6))))
    for k, (r, u) in enumerate(((1.6, BEAM_U - 1.2), (1.1, BEAM_U - 1.5))):
        parts.append(part("Crown", kit.cylinder(32, r, 0.14), brass, M((0, 0, u))))
        for j in range(int(r * 10)):
            a = 2 * math.pi * j / int(r * 10)
            parts.append(part("CrownCog", kit.cube(), gilt, M((math.cos(a) * (r + 0.08), math.sin(a) * (r + 0.08), u), rot=(0, 0, math.degrees(a)), scale=(0.18, 0.12, 0.14))))
    for k in range(6):
        a = 2 * math.pi * k / 6
        parts.append(part("CrownRod", kit.cylinder(6, 0.04, 1.0), brass, M((math.cos(a) * 1.5, math.sin(a) * 1.5, BEAM_U - 0.6))))
    return [kit.join(parts, f"TheHour_{variant}")]
