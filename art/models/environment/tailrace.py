"""The Orchard's tailrace (the Stroke on the water): off the bottom terrace, out in the river, two dry-stone walls
standing up out of the water make a channel six metres wide; at its head, a small stone landing walled in on three
sides, the water welling out from a dark culvert under it and running hard down the channel to its mouth. Rowing
alone can't climb it: the Stroke's surge can. Tall piers mark the mouth.

Game directions (see masonry.py): origin at the middle of the landing's back wall, at the water's surface; the channel
runs north. Where the game needs them (MainSceneBuilder.Orchard: Tailrace):
  the channel's water: e -3.0 .. 3.0, n 4.0 .. 14.0 (the current from n 6.0 to the mouth, flowing north)
  the landing: e -3.0 .. 3.0, n 0 .. 4.0, its top at 0.5; the culvert in its front face, e -0.8 .. 0.8
  the walls' tops at 0.95 (a boat doesn't ride over them); the piers at the mouth, n 13.6 .. 14.0
"""
import masonry as m

M, part = m.M, m.part
kit = m.kit

TOP = 0.5
WALL = 0.95
LENGTH = 14.0


def build():
    m.orchard()
    parts = []
    cap = m.mat(m.CAP)
    dark = kit.mat("CulvertDark", "#0d0b0a")
    timber = m.mat(m.TIMBER)
    # The walls round it, standing in the river.
    m.wall(parts, -3.4, -3.0, -0.4, LENGTH, -0.9, WALL, course=0.45, plinth=True)   # west
    m.wall(parts, 3.0, 3.4, -0.4, LENGTH, -0.9, WALL, course=0.45, plinth=True)     # east
    m.wall(parts, -3.0, 3.0, -0.4, 0.0, -0.9, WALL, course=0.45, plinth=True)       # behind the landing
    # The landing: earth inside its front wall, flagged on top; the culvert's mouth low in its face.
    parts.append(m.box("Earth", m.mat(m.MORTAR), -3.0, 3.0, 0.0, 3.6, -0.9, TOP - 0.1))
    m.wall(parts, -3.0, 3.0, 3.6, 4.0, -0.9, TOP, course=0.45, plinth=True, openings=[(-0.8, 0.8, -0.9, 0.3)])
    parts.append(m.box("Culvert", dark, -0.8, 0.8, 2.6, 3.65, -0.9, 0.3))
    parts.append(m.box("CulvertArch", cap, -1.0, 1.0, 3.62, 4.08, 0.3, 0.42))
    m.flagstones(parts, -3.0, 3.0, 0.0, 3.6, TOP + 0.02)
    # A mooring post at the landing's edge.
    parts.append(m.box("MooringPost", timber, 2.1, 2.3, 3.7, 3.9, TOP, TOP + 0.75))
    # The piers at the mouth: taller, capped, so the way in reads from across the water.
    for e0, e1 in ((-3.5, -2.9), (2.9, 3.5)):
        m.wall(parts, e0, e1, LENGTH - 0.4, LENGTH + 0.2, WALL, WALL + 0.7, course=0.35)
        parts.append(m.box("PierCap", cap, e0 - 0.05, e1 + 0.05, LENGTH - 0.45, LENGTH + 0.25, WALL + 0.7, WALL + 0.8))
    return [kit.join(parts, "Tailrace")]
