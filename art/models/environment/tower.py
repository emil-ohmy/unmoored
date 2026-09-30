"""A stone watchtower in three storeys, each its own model so the game can hide the storeys above the Apprentice
while they're inside (Storeys): "ground" (flagstones, a door to the south, the stair up along the east wall),
"middle" (a boarded first floor, a door west onto a walkway, a balcony to the south, the stair to the roof along the
north wall) and "top" (the flagged roof, a crenellated parapet with a chained gap on the east for the Hook). 7 x 7 m,
storeys 2.4 m, doorways 2.2 m wide, stairs 1.55 m (a Laden fits through; its narrow stairs it doesn't). Coursed stone in the village's own palette, pale capstones on every wall top, timber door frames,
window slits. Every drop taller than 1.5 m is walled or railed. Game directions (see masonry.py); origin at the
ground floor's centre, at the quay's level."""
import masonry as m

VARIANTS = ["ground", "middle", "top"]
H = 2.4         # storey height (the first floor's top, the roof at twice it)
OUT, IN = 3.5, 3.15
RUN, STEPS = 0.333, 12
STAIR = 1.6     # the ground floor's stair runs up the east wall from here to IN (1.55 m wide)
ROOF_RUN = 0.3  # the roof stair's steps are a little shorter (it leaves room to step off at its top)
FOOT, GROUND_RUN, GROUND_STEPS = -1.3, 0.3, 11  # the ground floor's stair: 1.8 m clear in front of it (a Laden reaches it)
GROUND = 0.08   # the ground floor's flagstones stand clear of the quay's own paving


def stair_wall(parts, e0, e1, n0, n1, top, ceiling):
    """The stepped half-wall along a stair's open side, capped; never higher than `ceiling` (the floor above: where the
    stair arrives, it stands flush with that floor, so the way off the stair onto it is open)."""
    base = 0 if top < H + 0.01 else H
    wall_top = min(top + 0.9, ceiling - 0.1)
    parts.append(m.box("StairWall", m.mat(m.STONE[0]), e0, e1, n0, n1, base, wall_top))
    parts.append(m.box("StairCap", m.mat(m.CAP), e0 - 0.02, e1 + 0.02, n0, n1, wall_top, wall_top + 0.1))


def build(variant):
    iron = m.kit.mat("TowerIron", "#2c2b31")
    parts = []
    if variant == "ground":
        m.flagstones(parts, -IN, IN, -IN, IN, GROUND)
        m.flagstones(parts, -1.1, 1.1, -OUT, -IN, GROUND, base=False)  # the threshold
        m.wall(parts, -OUT, OUT, -OUT, -IN, 0, H, openings=[(-1.1, 1.1, 0, 2.0)], plinth=True)
        m.lintel(parts, -1.1, 1.1, -OUT, -IN, 0, 2.0)
        m.wall(parts, -OUT, OUT, IN, OUT, 0, H, openings=[(-0.35, 0.35, 1.2, 1.8)], plinth=True)
        m.wall(parts, -OUT, -IN, -IN, IN, 0, H, openings=[(-0.35, 0.35, 1.2, 1.8)], plinth=True)
        m.wall(parts, IN, OUT, -IN, IN, 0, H, plinth=True)
        rise = (H - GROUND) / GROUND_STEPS
        for i in range(GROUND_STEPS):  # up along the east wall, rising north
            n0 = FOOT + i * GROUND_RUN
            top = GROUND + (i + 1) * rise
            m.step(parts, STAIR, IN, n0, n0 + GROUND_RUN, 0, top, tone=i)
            stair_wall(parts, STAIR - 0.16, STAIR, n0, n0 + GROUND_RUN, top, H)
    elif variant == "middle":
        top_of_stair = FOOT + GROUND_STEPS * GROUND_RUN
        hole = (STAIR - 0.05, IN, FOOT - 0.1, top_of_stair)  # over the stair from the ground floor
        m.planks(parts, -IN, IN, -IN, IN, H, holes=[hole])
        # Railed along its west side only where the drop to the stair below is too tall to take (the stair's lower half);
        # beside its top steps, the first floor opens straight onto the stair.
        m.balustrade(parts, STAIR - 0.1, FOOT - 0.1, STAIR - 0.1, -0.4, H)
        m.balustrade(parts, STAIR - 0.1, FOOT - 0.15, IN, FOOT - 0.15, H)
        m.wall(parts, -OUT, OUT, -OUT, -IN, H, 2 * H, openings=[(-1.1, 1.1, H, H + 2.0)])
        m.lintel(parts, -1.1, 1.1, -OUT, -IN, H, H + 2.0)
        m.wall(parts, -OUT, OUT, IN, OUT, H, 2 * H, openings=[(-1.6, -1.1, H + 1.1, H + 1.8)])
        m.wall(parts, -OUT, -IN, -IN, IN, H, 2 * H, openings=[(-1.1, 1.1, H, H + 2.0)])
        m.lintel(parts, -OUT, -IN, -1.1, 1.1, H, H + 2.0)
        m.wall(parts, IN, OUT, -IN, IN, H, 2 * H, openings=[(-1.0, -0.5, H + 1.1, H + 1.8)])
        # The balcony over the ground floor's door: flagged, a beam under its edge, railed.
        m.flagstones(parts, -2.0, 2.0, -5.3, -OUT, H, thickness=0.25)
        parts.append(m.box("BalconyBeam", m.mat(m.TIMBER), -2.0, 2.0, -5.3, -5.05, H - 0.5, H - 0.25))
        parts.append(m.box("BalconyBeam", m.mat(m.TIMBER), -1.9, -1.7, -5.3, -OUT, H - 0.5, H - 0.25))
        parts.append(m.box("BalconyBeam", m.mat(m.TIMBER), 1.7, 1.9, -5.3, -OUT, H - 0.5, H - 0.25))
        m.balustrade(parts, -2.0, -5.22, 2.0, -5.22, H)
        m.balustrade(parts, -1.92, -5.22, -1.92, -OUT, H)
        m.balustrade(parts, 1.92, -5.22, 1.92, -OUT, H)
        for i in range(STEPS):  # up along the north wall, rising west from the landing in the north-east corner
            e1 = STAIR - 0.05 - i * ROOF_RUN
            top = H + (i + 1) * 0.2
            m.step(parts, e1 - ROOF_RUN, e1, 2.0, IN, H, top, tone=i)
            stair_wall(parts, e1 - ROOF_RUN, e1, 1.84, 2.0, top, 99.0)
    else:
        top = 2 * H
        west = STAIR - 0.05 - STEPS * ROOF_RUN  # where the stair from the first floor arrives
        hole = (west - 0.1, STAIR, 1.95, IN)
        m.flagstones(parts, -OUT, OUT, -OUT, OUT, top, holes=[hole])
        m.balustrade(parts, west - 0.1, 1.9, STAIR, 1.9, top)
        m.balustrade(parts, STAIR + 0.05, 1.9, STAIR + 0.05, IN, top)
        # The parapet, crenellated, with a gap on the east for the Hook (chained across: it holds you).
        m.wall(parts, -OUT, OUT, -OUT, -3.2, top, top + 1.0, course=0.45)
        m.wall(parts, -OUT, OUT, 3.2, OUT, top, top + 1.0, course=0.45)
        m.wall(parts, -OUT, -3.2, -3.2, 3.2, top, top + 1.0, course=0.45)
        m.wall(parts, 3.2, OUT, -3.2, -0.8, top, top + 1.0, course=0.45)
        m.wall(parts, 3.2, OUT, 0.8, 3.2, top, top + 1.0, course=0.45)
        cap = m.mat(m.CAP)
        for k in range(4):
            e = -2.8 + k * 1.85
            parts.append(m.box("Merlon", cap, e - 0.3, e + 0.3, -OUT - 0.02, -3.18, top + 1.0, top + 1.4))
            parts.append(m.box("Merlon", cap, e - 0.3, e + 0.3, 3.18, OUT + 0.02, top + 1.0, top + 1.4))
            n = -2.8 + k * 1.85
            parts.append(m.box("Merlon", cap, -OUT - 0.02, -3.18, n - 0.3, n + 0.3, top + 1.0, top + 1.4))
        parts += [
            m.box("Chain", iron, 3.3, 3.4, -0.8, 0.8, top + 0.5, top + 0.62),
            m.box("ChainPost", iron, 3.25, 3.45, -0.85, -0.75, top, top + 0.7),
            m.box("ChainPost", iron, 3.25, 3.45, 0.75, 0.85, top, top + 0.7),
        ]
    return [m.kit.join(parts, f"Tower_{variant}")]
