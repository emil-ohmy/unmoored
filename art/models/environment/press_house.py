"""The press house: a timber-framed barn on a stone plinth, 9 x 7 m, where the cider was pressed (it smells of
vinegar now). In three parts, so the builder can fade the ones above the Apprentice (Storeys): "ground" (the floor of
worn flagstones, the plinth and timber frame with pale ochre plaster between, a wide door in the west gable, a window
south, and a wooden stair up the east wall to the loft), "loft" (a boarded loft over the north part, 2.4 m up, its rail, the gable
walls) and "roof" (pitched east to west, faded clay tiles with moss, its rafters). Game directions (see masonry.py),
origin at the centre of the floor."""
import masonry as m

VARIANTS = ["ground", "loft", "roof"]
W, D = 9.0, 7.0
E0, E1, N0, N1 = -W / 2, W / 2, -D / 2, D / 2
LOFT = 2.4                     # the loft's floor
LOFT_N0 = 1.0                  # it covers the north part, from here back
STAIR_E0, STAIR_E1 = 3.15, 4.2 # up the east wall, rising north to the loft
RISE, RUN = 0.2, 0.333
DOOR = (-1.6, 0.6)             # the west door's span (north)
T = 0.3


def frame_wall(parts, e0, e1, n0, n1, u0, u1, openings=(), corners=True):
    """Timber posts about every 1.5 m, a rail at mid-height and a beam on top, pale plaster between (not across the
    openings: (a0, a1, b0, b1) along the wall's length and height)."""
    timber, plaster = m.mat(m.TIMBER), m.kit.mat("Plaster", "#b8a17a")
    along_e = (e1 - e0) >= (n1 - n0)
    a0, a1 = (e0, e1) if along_e else (n0, n1)

    def piece(name, material, lo, hi, b0, b1, inset=0.0):
        if along_e:
            parts.append(m.box(name, material, lo, hi, n0 + inset, n1 - inset, b0, b1))
        else:
            parts.append(m.box(name, material, e0 + inset, e1 - inset, lo, hi, b0, b1))

    def opened(lo, hi, b0, b1):
        return any(o[0] < hi and lo < o[1] and o[2] < b1 and b0 < o[3] for o in openings)

    count = max(2, round((a1 - a0) / 1.5))
    cuts = [a0 + (a1 - a0) * k / count for k in range(count + 1)]
    for o in openings:
        cuts += [o[0], o[1]]
    cuts = sorted(set(round(c, 3) for c in cuts if a0 <= c <= a1))
    mid = (u0 + u1) / 2
    for lo, hi in zip(cuts, cuts[1:]):
        # Only between the rails and the posts (no face shared with a timber's: two faces in one plane flicker).
        for b0, b1 in ((u0 + 0.18, mid), (mid + 0.18, u1 - 0.18)):
            if not opened(lo, hi, b0, b1):
                piece("Plaster", plaster, lo + 0.09, hi - 0.09, b0, b1, inset=0.06)
    for c in cuts:
        if not corners and (abs(c - a0) < 1e-3 or abs(c - a1) < 1e-3):
            continue  # (the corner posts are the long walls': two in one place would flicker)
        piece("Post", timber, max(a0, c - 0.09) if not corners else c - 0.09, min(a1, c + 0.09) if not corners else c + 0.09, u0, u1)
    for b in (u0, (u0 + u1) / 2, u1 - 0.18):
        for lo, hi in zip(cuts, cuts[1:]):
            if not opened(lo, hi, b, b + 0.18):
                piece("Rail", timber, lo + 0.09, hi - 0.09, b, b + 0.18, inset=0.02)  # (between the posts, set back a little:
                                                                                      # no two faces in one plane to flicker)


def build(variant):
    m.orchard()
    parts = []
    if variant == "ground":
        m.flagstones(parts, E0 + T, E1 - T, N0 + T, N1 - T, 0.02)
        for e0, e1, n0, n1, holes in (
                (E0, E1, N0, N0 + T, [(-2.2, -0.8, 1.0, 1.8)]),
                (E0, E1, N1 - T, N1, []),
                (E0, E0 + T, N0 + T, N1 - T, [(DOOR[0], DOOR[1], -0.4, 2.2)]),
                (E1 - T, E1, N0 + T, N1 - T, [])):
            plinth_holes = [(h[0] - 0.03, h[1] + 0.03, -0.4, 1.0) for h in holes if h[2] < 0.9]  # (a hair wider: the jambs cover its edge)
            m.wall(parts, e0, e1, n0, n1, -0.4, 0.9, openings=plinth_holes, course=0.3)
            frame_wall(parts, e0, e1, n0, n1, 0.9, LOFT + 0.2, openings=holes, corners=(e1 - e0) > (n1 - n0))
        m.lintel(parts, E0, E0 + T, DOOR[0], DOOR[1], 0.0, 2.2)
        # The stair up the east wall, rising north to the loft.
        steps = round(LOFT / RISE)
        foot = LOFT_N0 - steps * RUN
        timber = m.mat(m.TIMBER)
        for i in range(steps):
            parts.append(m.box("Stair", timber, STAIR_E0, STAIR_E1, foot + i * RUN, foot + (i + 1) * RUN, (i + 1) * RISE - 0.08, (i + 1) * RISE))
            # (its side flush with the treads: open, so it can be stepped onto from the floor beside it)
            parts.append(m.box("Stringer", timber, STAIR_E0 - 0.06, STAIR_E0, foot + i * RUN, foot + (i + 1) * RUN, 0.0, (i + 1) * RISE - 0.09))
        return [m.kit.join(parts, "PressHouse_ground")]
    if variant == "loft":
        m.planks(parts, E0 + T, E1 - T, LOFT_N0, N1 - T, LOFT, holes=[(STAIR_E0 - 0.05, E1 - T, LOFT_N0, LOFT_N0 + 0.05)])
        timber = m.mat(m.TIMBER)
        # Its rail along the open edge (the stair's head left open).
        parts.append(m.box("LoftRail", timber, E0 + T, STAIR_E0 - 0.1, LOFT_N0 - 0.035, LOFT_N0 + 0.035, LOFT + 0.9, LOFT + 1.0))
        for k in range(7):
            e = E0 + T + 0.1 + k * (STAIR_E0 - 0.2 - E0 - T) / 6
            parts.append(m.box("LoftPost", timber, e - 0.05, e + 0.05, LOFT_N0 - 0.05, LOFT_N0 + 0.05, LOFT, LOFT + 0.96))
        # The walls above the ground floor's, to the eaves, and the gables (east, west).
        for e0, e1, n0, n1 in ((E0, E1, N0, N0 + T), (E0, E1, N1 - T, N1), (E0, E0 + T, N0 + T, N1 - T), (E1 - T, E1, N0 + T, N1 - T)):
            frame_wall(parts, e0, e1, n0, n1, LOFT + 0.2, LOFT + 1.4, corners=(e1 - e0) > (n1 - n0))
        plaster = m.kit.mat("Plaster", "#b8a17a")
        for e0, e1 in ((E0, E0 + T), (E1 - T, E1)):
            for k in range(6):
                half = (D / 2) * (1 - k / 6)
                parts.append(m.box("Gable", plaster, e0, e1, -half, half, LOFT + 1.4 + k * 0.3, LOFT + 1.4 + (k + 1) * 0.3))
        return [m.kit.join(parts, "PressHouse_loft")]
    # The roof: two pitches meeting at a ridge running east to west.
    tiles = [m.kit.mat("Tiles1", "#7a4a3a"), m.kit.mat("Tiles2", "#6b4134"), m.kit.mat("Moss", "#4d5236")]
    rng = m.kit.rng
    eaves, ridge = LOFT + 1.4, LOFT + 1.4 + 1.9
    import math
    slope = math.degrees(math.atan2(ridge - eaves, D / 2 + 0.4))
    for side in (-1, 1):
        for k in range(10):
            t = (k + 0.5) / 10
            n = side * (D / 2 + 0.4) * (1 - t)
            u = eaves + (ridge - eaves) * t
            mat = tiles[2] if rng.random() < 0.15 else tiles[k % 2]
            parts.append(m.part("Tiles", m.kit.cube(), mat, m.M((0, -n, u), rot=(side * slope, 0, 0), scale=(W + 0.8 + (k % 2) * 0.04 + (0.02 if side > 0 else 0.0), (D / 2 + 0.4) / 10 * 1.1 / math.cos(math.radians(slope)), 0.12))))
    parts.append(m.box("Ridge", m.mat(m.TIMBER), E0 - 0.4, E1 + 0.4, -0.12, 0.12, ridge - 0.05, ridge + 0.12))
    return [m.kit.join(parts, "PressHouse_roof")]
