"""The Apprentice's oar at each of its five steps (import oars), built along its own axis: Z from the butt (0) to the
blade's tip (about 2.25), X across the blade, Y through it (-Y its front face). `build(step, base, group)` returns the
parts placed by `base`; every material is named for its step ("Oar2Iron_Metal"), so the game shows one step at a time
(OarLook.cs). Every step's blade reaches about as far as the first's (the swing's trail is fitted to the first). Past the first, the shaft stops at the blade's neck.

  0 The Apprentice's Oar: plain wood, worn smooth, a red band at the neck.
  1 The Oarhand's Oar: tarred, bound with twine; brass at the butt, the neck and across the blade; a red ribbon.
  2 The Boatman's Oar: dark oiled wood, banded in iron; a broader blade rimmed and pointed in iron, riveted; a boat-hook
    at the neck; a spiked butt.
  3 The Steersman's Oar: black wood, a notch for every crossing, wound in silver wire; a leaf-shaped blade framed in
    silver with a crescent on each face and swept fins at its neck; two Obols on cords; a silver pommel.
  4 The Ferryman's Oar: gold, all of it. A spiral ridge the whole way up, a gold ring round the shaft (an oar through a
    ring: his mark, made whole), a great flared blade edged in deeper gold with his mark glowing on both faces, and a
    pale gem at the butt and at the tip."""
import math

from mathutils import Vector

import kit

M = kit.M


def build(step, base, group=None):
    parts = []

    def mat(name, colour):
        return kit.mat(f"Oar{step}{name}", colour)

    def add(name, shape, material, local=None):
        parts.append(kit.part(name, shape, material, base @ (local if local is not None else M()), group=group))

    def cyl(name, r, z0, z1, material, seg=8, r_top=None):
        add(name, kit.cone(seg, r, r if r_top is None else r_top, z1 - z0), material, M((0, 0, (z0 + z1) / 2)))

    def box(name, centre, size, material, rot=(0, 0, 0)):
        add(name, kit.cube(), material, M(centre, rot=rot, scale=size))

    def dot(name, at, r, material):
        add(name, kit.ico(1, r), material, M(at))

    def line(name, points, r, material, seg=6):
        add(name, kit.tube(points, [r] * len(points), seg=seg), material)

    def helix(name, r, z0, z1, turns, thick, material):
        n = int(turns * 12) + 1
        pts = [(r * math.cos(2 * math.pi * turns * k / (n - 1)), r * math.sin(2 * math.pi * turns * k / (n - 1)),
                z0 + (z1 - z0) * k / (n - 1)) for k in range(n)]
        line(name, pts, thick, material, seg=4)

    def blade(name, outline, z0, thickness, material):
        """A flat blade from a closed outline of (x, z) points, z measured up from z0, `thickness` through Y."""
        add(name, kit.prism(outline, thickness), material, M((0, thickness / 2, z0), rot=(90, 0, 0)))

    def rim(name, outline, z0, r, material, y=0.0):
        pts = [(x, y, z0 + z) for x, z in outline] + [(outline[0][0], y, z0 + outline[0][1])]
        line(name, pts, r, material, seg=4)

    def ring(name, r, at_z, thick, material, seg=16):
        pts = [(r * math.cos(2 * math.pi * k / seg), r * math.sin(2 * math.pi * k / seg), at_z) for k in range(seg + 1)]
        line(name, pts, thick, material, seg=4)

    def mark(name, at_z, size, material, y):
        """His mark, an oar through a ring, inlaid on a blade face (y: the face; its sign picks which)."""
        pts = [(size * math.cos(a), y, at_z + size * math.sin(a)) for a in (2 * math.pi * k / 14 for k in range(15))]
        line(name + "Ring", pts, size * 0.17, material, seg=4)
        box(name + "Shaft", (0, y, at_z), (size * 0.26, 0.008, size * 3.6), material, rot=(0, 28, 0))
        box(name + "Blade", (-size * 0.75, y, at_z - size * 1.35), (size * 0.75, 0.008, size * 0.9), material, rot=(0, 28, 0))

    if step == 0:
        wood, bladewood, band = mat("Wood", "#b08a5c"), mat("Blade", "#8f6d48"), mat("Band", "#b8352b")
        cyl("Shaft", 0.028, 0.0, 2.1, wood, seg=6)
        box("Blade", (0, 0, 1.95), (0.17, 0.035, 0.55), bladewood)
        cyl("Band", 0.036, 1.595, 1.645, band, seg=6)

    elif step == 1:
        tar, bladewood = mat("Tar", "#3d2c21"), mat("Blade", "#4a3526")
        brass, twine, ribbon = mat("Brass_Metal", "#c9933f"), mat("Twine", "#a38b62"), mat("Ribbon", "#b8352b")
        cyl("Shaft", 0.03, 0.0, 1.72, tar, seg=6)
        box("Blade", (0, 0, 1.95), (0.18, 0.04, 0.55), bladewood)
        cyl("Butt", 0.037, 0.0, 0.07, brass)
        cyl("Collar", 0.042, 1.58, 1.68, brass)
        for z in (1.8, 2.08):
            box("Strap", (0, 0, z), (0.192, 0.05, 0.035), brass)
            for x in (-0.06, 0.06):
                for y in (-0.026, 0.026):
                    dot("Rivet", (x, y, z), 0.013, brass)
        box("Cap", (0, 0, 2.228), (0.192, 0.05, 0.03), brass)
        for k in range(7):
            cyl("Twine", 0.035, 0.44 + k * 0.042, 0.46 + k * 0.042, twine)
        line("Ribbon", [(0.04, 0, 1.63), (0.06, -0.02, 1.52), (0.05, -0.01, 1.4), (0.065, -0.03, 1.28)], 0.012, ribbon)

    elif step == 2:
        wood, bladewood, iron = mat("Wood", "#5c4030"), mat("Blade", "#6a4a34"), mat("Iron_Metal", "#6f737d")
        cyl("Shaft", 0.032, 0.0, 1.72, wood, seg=8)
        box("Blade", (0, 0, 1.935), (0.21, 0.045, 0.52), bladewood)
        for x in (-0.108, 0.108):
            box("Rim", (x, 0, 1.94), (0.026, 0.056, 0.54), iron)
        blade("Point", [(-0.121, 0), (0.121, 0), (0.05, 0.08), (0, 0.12), (-0.05, 0.08)], 2.195, 0.052, iron)
        for z in (1.78, 1.93, 2.08):
            for x in (-0.055, 0.055):
                for y in (-0.03, 0.03):
                    dot("Rivet", (x, y, z), 0.014, iron)
        for z in (0.3, 0.95, 1.3):
            cyl("Band", 0.039, z - 0.03, z + 0.03, iron)
        cyl("Collar", 0.05, 1.6, 1.68, iron, r_top=0.06)
        cyl("Foot", 0.04, 0.0, 0.06, iron)
        # The boat-hook, at the neck: a curved iron claw.
        line("Hook", [(0.03, 0, 1.36), (0.085, 0, 1.42), (0.115, 0, 1.5), (0.105, 0, 1.57), (0.07, 0, 1.585)], 0.016, iron)
        add("HookTip", kit.cone(6, 0.018, 0.0, 0.05), iron, M((0.05, 0, 1.58), rot=(0, -80, 0)))

    elif step == 3:
        ebony, silver = mat("Ebony", "#2b2327"), mat("Silver_Metal", "#c9ccd6")
        bone, coin, cord = mat("Bone", "#e2d6bc"), mat("Coin_Metal", "#b08a3e"), mat("Cord", "#5a4636")
        cyl("Shaft", 0.032, 0.05, 1.72, ebony, seg=8)
        helix("Wire", 0.034, 0.95, 1.6, 3.0, 0.008, silver)
        for k in range(8):
            cyl("Notch", 0.035, 0.14 + k * 0.045, 0.152 + k * 0.045, bone)
        leaf = [(-0.04, 0), (0.04, 0), (0.1, 0.1), (0.112, 0.34), (0.09, 0.5), (0.0, 0.59), (-0.09, 0.5), (-0.112, 0.34), (-0.1, 0.1)]
        blade("Blade", leaf, 1.66, 0.04, mat("BladeEbony", "#2b2327"))  # (its own: the Stroke's sheen lies on the blade alone)
        rim("Frame", leaf, 1.66, 0.023, silver)
        for y in (-0.022, 0.022):
            pts = [(0.055 * math.cos(a), y, 1.95 + 0.055 * math.sin(a)) for a in (math.radians(50 + 26 * k) for k in range(11))]
            line("Crescent", pts, 0.01, silver, seg=4)
        for side in (1, -1):
            fin = [(side * 0.03, 0), (side * 0.1, 0.05), (side * 0.13, 0.17), (side * 0.045, 0.13)]
            blade("Fin", fin if side > 0 else list(reversed(fin)), 1.48, 0.014, silver)
        add("Pommel", kit.sphere(8, 6, 0.045), silver, M((0, 0, 0.045)))
        for side, z in ((1, 1.38), (-1, 1.32)):
            line("Cord", [(side * 0.03, 0, 1.5), (side * 0.06, -0.01, z + 0.04)], 0.006, cord, seg=4)
            add("Obol", kit.cylinder(10, 0.032, 0.008), coin, M((side * 0.068, -0.012, z), rot=(90, 0, 0)))

    else:
        gold, deep = mat("Gold_Metal", "#e8b53a"), mat("DeepGold_Metal", "#b07c1e")
        his, gem = mat("Mark_Glow", "#9fc4ff"), mat("Gem_Glow", "#cfe8ff")
        cyl("Shaft", 0.034, 0.1, 1.68, gold, seg=10)
        helix("Ridge", 0.036, 0.16, 1.55, 5.0, 0.011, deep)
        for z in (0.5, 0.72):
            cyl("Grip", 0.042, z - 0.02, z + 0.02, deep)
        flare = [(-0.05, 0), (0.05, 0), (0.1, 0.04), (0.165, 0.015), (0.155, 0.1), (0.125, 0.145), (0.132, 0.36),
                 (0.1, 0.52), (0.036, 0.6), (0, 0.65), (-0.036, 0.6), (-0.1, 0.52), (-0.132, 0.36), (-0.125, 0.145),
                 (-0.155, 0.1), (-0.165, 0.015), (-0.1, 0.04)]
        blade("Blade", flare, 1.62, 0.046, mat("BladeGold_Metal", "#e8b53a"))  # (its own, as above)
        rim("Edge", flare, 1.62, 0.016, deep)
        for y in (-0.026, 0.026):
            mark("Mark", 1.97, 0.058, his, y)
        # The ring round the shaft, held by four spokes, a pale bead where each meets it.
        ring("Ring", 0.13, 1.42, 0.017, gold)
        for k in range(4):
            a = math.pi / 4 + k * math.pi / 2
            box("Spoke", (0.083 * math.cos(a), 0.083 * math.sin(a), 1.42), (0.1, 0.014, 0.014), deep, rot=(0, 0, math.degrees(a)))
            dot("Bead", (0.13 * math.cos(a), 0.13 * math.sin(a), 1.42), 0.022, gem)
        cyl("Collar", 0.045, 1.56, 1.64, deep, r_top=0.06)
        cyl("Finial", 0.03, 0.03, 0.16, deep, r_top=0.045)
        dot("ButtGem", (0, 0, 0.035), 0.034, gem)
        dot("TipGem", (0, 0, 2.28), 0.022, gem)
    return parts
