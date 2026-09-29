"""The drowned chapel: all that stands above the water of the Ferryman's old chapel is its upper floor, an octagon of
stone (top at z=0.5, sides down into the water) paved in rings around his mark, an oar through a ring, inlaid at the
centre. Pillar stumps at its corners (tall and broken at the altar end, worn down to stubs at the near end, so they
never hide the fight from the camera), a low broken parapet along the far sides, open edges at the near sides (the
water is a danger), the entrance in the middle of the near side. At the far end, the altar where the Anchoress keeps
her vigil, with a few candle stubs she keeps lit for a ferry that doesn't come.

The altar end is the model's front (-Y), so at yaw 0 it's in the world's +Z (the top of the screen).
"""
import math

M, part = kit.M, kit.part  # `kit` is injected by build_model.py

TOP = 0.5
R = 8.6  # to the corners; the flat sides face the altar, the entrance and either side


def corners(r):
    return [(r * math.cos(math.radians(22.5 + 45 * k)), r * math.sin(math.radians(22.5 + 45 * k))) for k in range(8)]


def inside(x, y, pts):
    hit = False
    for (x1, y1), (x2, y2) in zip(pts, pts[1:] + pts[:1]):
        if (y1 > y) != (y2 > y) and x < x1 + (y - y1) * (x2 - x1) / (y2 - y1):
            hit = not hit
    return hit


def build():
    rng = kit.rng
    side, curb = kit.mat("QuayStone", "#5d5a58"), kit.mat("CurbStone", "#726d68")
    slabs = [kit.mat("ChapelPaving1", "#86807a"), kit.mat("ChapelPaving2", "#7a756f"), kit.mat("ChapelPaving3", "#908a81")]
    inlay, pillar_stone = kit.mat("MarkInlay", "#b9b09a"), kit.mat("PillarStone", "#8a847c")
    wood, linen = kit.mat("BeamWood", "#4d3a2c"), kit.mat("AltarLinen", "#8c8470")
    altar_stone, wax, flame = kit.mat("AltarStone", "#9a948a"), kit.mat("Wax", "#d8cfb4"), kit.mat("Candle_Glow", "#ffc070")

    outline = corners(R)
    parts = [part("Base", kit.prism(outline, TOP + 1.5), side, M((0, 0, -1.5)))]

    # Paving: rings of slabs around the mark (the inner disc left for the inlay's ground), kept inside the curb.
    inner = corners(R - 0.55)
    parts.append(part("InlayGround", kit.cylinder(24, 1.35, 0.05), slabs[1], M((0, 0, TOP + 0.02))))
    r0, ring = 1.4, 0
    while r0 < R:
        width = 1.05 + 0.1 * ring
        mid = r0 + width / 2
        n = max(8, round(2 * math.pi * mid / 1.15))
        offset = rng.uniform(0, 2 * math.pi)
        for i in range(n):
            a = offset + 2 * math.pi * (i + 0.5) / n
            arc = 2 * math.pi * mid / n
            x, y = mid * math.cos(a), mid * math.sin(a)
            cs = [(x + dx, y + dy) for dx in (-width / 2, width / 2) for dy in (-width / 2, width / 2)]
            if not all(inside(cx, cy, inner) for cx, cy in cs):
                continue
            parts.append(part("Slab", kit.cube(), rng.choice(slabs),
                              M((x, y, TOP + 0.015 + rng.uniform(-0.008, 0.012)), rot=(0, 0, math.degrees(a) + 90 + rng.uniform(-2, 2)),
                                scale=(arc - 0.07, width - 0.07, 0.05))))
        r0 += width
        ring += 1
    # Where the rings don't reach the flat sides, a band of long edge slabs.
    for (x1, y1), (x2, y2) in zip(inner, inner[1:] + inner[:1]):
        length = math.hypot(x2 - x1, y2 - y1)
        n = max(1, round(length / 1.3))
        angle = math.degrees(math.atan2(y2 - y1, x2 - x1))
        nx, ny = -(y2 - y1) / length, (x2 - x1) / length  # inwards
        for i in range(n):
            t = (i + 0.5) / n
            parts.append(part("EdgeSlab", kit.cube(), rng.choice(slabs),
                              M((x1 + (x2 - x1) * t + nx * 0.42, y1 + (y2 - y1) * t + ny * 0.42, TOP + 0.01),
                                rot=(0, 0, angle + rng.uniform(-2, 2)), scale=(length / n - 0.08, 0.95, 0.04))))
    # The Ferryman's mark, inlaid at the centre, big: an oar through a ring (the blade towards the altar).
    parts += kit.ferryman_mark(inlay, M((0, 0, TOP + 0.03), rot=(-90, 0, 0), scale=(9, 2, 9)))

    # Curb stones along every edge.
    for (x1, y1), (x2, y2) in zip(outline, outline[1:] + outline[:1]):
        length = math.hypot(x2 - x1, y2 - y1)
        n = max(1, round(length / 0.9))
        angle = math.degrees(math.atan2(y2 - y1, x2 - x1))
        for i in range(n):
            t = (i + 0.5) / n
            parts.append(part("Curb", kit.cube(), curb,
                              M((x1 + (x2 - x1) * t, y1 + (y2 - y1) * t, TOP + 0.06), rot=(0, 0, angle + rng.uniform(-3, 3)),
                                scale=(length / n - 0.05, 0.42, 0.16 + rng.uniform(-0.02, 0.03)))))

    # A low, broken parapet along the far sides (the altar end and its neighbours) and half of each flank; the near
    # sides are open to the water.
    walled = {4: (0.0, 1.0), 5: (0.0, 1.0), 6: (0.0, 1.0), 3: (0.5, 1.0), 7: (0.0, 0.5)}  # side k runs corner k -> k+1
    for k, (t0, t1) in walled.items():
        (x1, y1), (x2, y2) = corners(R - 0.3)[k], corners(R - 0.3)[(k + 1) % 8]
        length = math.hypot(x2 - x1, y2 - y1)
        angle = math.degrees(math.atan2(y2 - y1, x2 - x1))
        n = round(length / 0.7)
        for i in range(n):
            t = (i + 0.5) / n
            if not t0 <= t <= t1 or rng.random() < 0.18:
                continue  # broken
            h = 0.55 + rng.uniform(-0.2, 0.08)
            parts.append(part("Parapet", kit.cube(), curb,
                              M((x1 + (x2 - x1) * t, y1 + (y2 - y1) * t, TOP + h / 2), rot=(0, 0, angle + rng.uniform(-3, 3)),
                                scale=(length / n - 0.04, 0.34, h))))

    # Pillars at the corners: tall and broken at the altar end (-Y), worn to stubs at the near end.
    for k, (x, y) in enumerate(corners(R - 0.55)):
        far = y < 0
        h = rng.uniform(2.4, 3.6) if far else rng.uniform(0.25, 0.7)
        parts += [
            part("Plinth", kit.cube(), pillar_stone, M((x, y, TOP + 0.15), rot=(0, 0, 22.5 + 45 * k), scale=(0.95, 0.95, 0.3))),
            part("Pillar", kit.cylinder(10, 0.34, h), pillar_stone, M((x, y, TOP + 0.3 + h / 2)), smooth=True),
        ]
        if far:  # a jagged, broken top
            parts.append(part("PillarBreak", kit.cube(), pillar_stone,
                              M((x + 0.05, y, TOP + 0.3 + h + 0.08), rot=(rng.uniform(-18, 18), rng.uniform(-18, 18), rng.uniform(0, 90)), scale=(0.5, 0.4, 0.3))))

    # The altar: a step, the block, a rotted cloth, and candle stubs burning low.
    ay = -(R * math.cos(math.radians(22.5)) - 1.6)
    parts += [
        part("AltarStep", kit.cube(), altar_stone, M((0, ay, TOP + 0.06), scale=(3.4, 1.7, 0.12))),
        part("Altar", kit.cube(), altar_stone, M((0, ay - 0.1, TOP + 0.12 + 0.46), scale=(2.1, 0.85, 0.92))),
        part("AltarCloth", kit.cube(), linen, M((0, ay - 0.1, TOP + 1.05), scale=(2.2, 0.95, 0.03))),
        part("AltarClothHang", kit.cube(), linen, M((0, ay + 0.33, TOP + 0.82), rot=(0, 0, 0), scale=(1.1, 0.03, 0.45))),
    ]
    for i, (dx, height) in enumerate(((-0.8, 0.22), (-0.55, 0.12), (-0.2, 0.28), (0.35, 0.16), (0.7, 0.24))):
        cx, cy = dx, ay - 0.1 + rng.uniform(-0.2, 0.15)
        parts += [
            part("Candle", kit.cylinder(6, 0.045, height), wax, M((cx, cy, TOP + 1.07 + height / 2))),
            part("CandleFlame", kit.sphere(6, 4, 0.035), flame, M((cx, cy, TOP + 1.07 + height + 0.04), scale=(1, 1, 1.6))),
        ]

    # Fallen roof beams at the edges, half in the water (the roof went long ago), and a toppled pew.
    parts += [
        part("Beam", kit.cube(), wood, M((6.6, -3.2, TOP + 0.1), rot=(8, 4, 64), scale=(0.34, 4.6, 0.3))),
        part("Beam", kit.cube(), wood, M((-7.6, 1.6, TOP - 0.05), rot=(-10, 6, -12), scale=(0.3, 3.6, 0.28))),
        part("PewSeat", kit.cube(), wood, M((-5.9, -4.4, TOP + 0.4), rot=(0, 0, 68), scale=(2.2, 0.5, 0.08))),
        part("PewBack", kit.cube(), wood, M((-6.15, -4.3, TOP + 0.75), rot=(-12, 0, 68), scale=(2.2, 0.07, 0.6))),
        part("PewLeg", kit.cube(), wood, M((-6.3, -3.4, TOP + 0.2), rot=(0, 0, 68), scale=(0.08, 0.45, 0.4))),
        part("PewLeg", kit.cube(), wood, M((-5.5, -5.4, TOP + 0.2), rot=(0, 0, 68), scale=(0.08, 0.45, 0.4))),
    ]
    return [kit.join(parts, "DrownedChapel")]
