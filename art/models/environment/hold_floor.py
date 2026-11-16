"""The Hold's floor, the small things on it (MainSceneBuilder.HoldLayout scatters them where hold_drawn.py's spots say;
walked through: the scene builder takes their colliders away). Each variant is one piece, origin at its foot on the
floor, lying flat. Seen from the game camera (~12 m off), so a little larger than life and plain in shape.

- lichen_*, moss_*: low mats of grey-green growth, ragged at the edges, a hair above the stone.
- fungus_*: clusters of small pale caps on thin stalks; fungus_glow's caps glow a faint cold green (a little light
  to read the floor by in the black).
- puddle_*: shallow pools of black water, a paler wet rim round them.
- What the kept dropped: shoe, boot, rag, bottle, cup, comb, spoon, coins. Old, dark, a little sunk in the floor."""
import math

M, part = kit.M, kit.part  # `kit` is injected by build_model.py
VARIANTS = ["lichen_1", "lichen_2", "moss_1", "moss_2", "fungus_1", "fungus_2", "fungus_glow",
            "puddle_1", "puddle_2", "shoe", "boot", "rag", "bottle", "cup", "comb", "spoon", "coins"]


def blob(radius, lumps, dome, seg=18, rough=0.35, squash=1.0):
    """A flat irregular patch (a ragged outline, a gentle dome), lying on z = 0 and a hair over it."""
    rng = kit.rng
    phases = [(rng.uniform(0, 2 * math.pi), rng.uniform(0.3, 1.0)) for _ in range(lumps)]

    def r_at(a):
        r = 1.0
        for k, (ph, amp) in enumerate(phases):
            r += rough * amp / (k + 1) * math.sin((k + 2) * a + ph)
        return max(0.35, r) * radius

    def build(bm):
        centre = bm.verts.new((0.0, 0.0, dome + 0.012))
        ring = []
        mid = []
        for i in range(seg):
            a = 2 * math.pi * i / seg
            r = r_at(a)
            ring.append(bm.verts.new((math.cos(a) * r, math.sin(a) * r * squash, 0.012)))
            mid.append(bm.verts.new((math.cos(a) * r * 0.55, math.sin(a) * r * 0.55 * squash, 0.012 + dome * 0.75)))
        for i in range(seg):
            j = (i + 1) % seg
            bm.faces.new((centre, mid[i], mid[j]))
            bm.faces.new((mid[i], ring[i], ring[j], mid[j]))
    return build


def growth(kind):
    rng = kit.rng
    if kind.startswith("lichen"):
        # (play-test shots: big pale-flecked mats read as camouflage paint; smaller, nearer the stone's own tone)
        base = kit.mat("HoldLichen", "#3d4339")
        pale = kit.mat("HoldLichenPale", "#4c5346")
        parts = [part("Lichen", blob(rng.uniform(0.28, 0.4), 5, 0.01, rough=0.5), base, M(rot=(0, 0, rng.uniform(0, 360))))]
        for _ in range(rng.randint(1, 3)):  # paler rosettes on it
            x, y = rng.uniform(-0.18, 0.18), rng.uniform(-0.18, 0.18)
            parts.append(part("Rosette", blob(rng.uniform(0.05, 0.1), 4, 0.008, seg=10, rough=0.5), pale,
                              M((x, y, 0.004))))
        return parts
    # moss: deeper, greener, lumpier: a few overlapping cushions
    base = kit.mat("HoldMoss", "#3f4a38")
    dark = kit.mat("HoldMossDark", "#2f382c")
    parts = []
    for k in range(rng.randint(3, 5)):
        x, y = rng.uniform(-0.35, 0.35), rng.uniform(-0.35, 0.35)
        parts.append(part("Moss", blob(rng.uniform(0.2, 0.38), 4, rng.uniform(0.03, 0.06), rough=0.3),
                          base if k % 2 == 0 else dark, M((x, y, 0.0), rot=(0, 0, rng.uniform(0, 360))), smooth=True))
    return parts


def fungus(glow):
    rng = kit.rng
    # (the glow: a dim cold green, or the bloom burns it white; the plain caps a dull bone, not bright)
    cap = kit.mat("HoldFungusGlow_Glow", "#2f6b4e") if glow else kit.mat("HoldFungus", "#8a8373")
    gill = kit.mat("HoldFungusStalk", "#8e8676")
    parts = [part("Bed", blob(0.28, 4, 0.01), kit.mat("HoldMossDark", "#2f382c"), M())]
    for _ in range(rng.randint(5, 8)):
        x, y = rng.uniform(-0.2, 0.2), rng.uniform(-0.2, 0.2)
        h = rng.uniform(0.05, 0.16)
        r = rng.uniform(0.03, 0.07) * (1.15 if glow else 1.0)
        lean = (rng.uniform(-14, 14), rng.uniform(-14, 14), 0)
        parts.append(part("Stalk", kit.cylinder(6, r * 0.28, h), gill, M((x, y, h / 2), rot=lean)))
        parts.append(part("Cap", kit.sphere(8, 5, 1.0), cap, M((x, y, h), rot=lean, scale=(r, r, r * 0.5)), smooth=True))
    return parts


def puddle():
    rng = kit.rng
    water = kit.mat("HoldPuddle", "#030405")  # (not _Metal: its glint from the lantern burned the pool white)
    rim = kit.mat("HoldPuddleRim", "#26272b")
    s = rng.uniform(0.7, 1.1)
    squash = rng.uniform(0.55, 0.8)
    return [part("Rim", blob(s * 1.1, 5, 0.0, seg=24, rough=0.25, squash=squash), rim, M()),
            part("Water", blob(s, 5, 0.0, seg=24, rough=0.25, squash=squash), water, M((0, 0, 0.004)))]


def dropped(kind):
    """What the kept dropped: dark, worn, half sunk (a little under z = 0)."""
    rng = kit.rng
    leather = kit.mat("HoldLeather", "#3b2c24")
    cloth = kit.mat("HoldRag", "#5c5552")
    glass = kit.mat("HoldGlass_Metal", "#2b3a35")
    clay = kit.mat("HoldClay", "#6b5a4b")
    bone = kit.mat("HoldBone", "#a69f8c")
    tarnish = kit.mat("HoldTarnish_Metal", "#6e6650")
    if kind == "shoe":
        return [part("Sole", kit.cube(), kit.mat("HoldSole", "#24201d"), M((0, 0, 0.012), scale=(0.11, 0.3, 0.025))),
                part("Upper", kit.sphere(10, 6, 1.0), leather, M((0, -0.02, 0.03), scale=(0.06, 0.15, 0.055)), smooth=True),
                part("Heel", kit.cube(), leather, M((0, 0.11, 0.045), scale=(0.1, 0.07, 0.07)))]
    if kind == "boot":  # fallen over on its side
        return [part("Foot", kit.sphere(10, 6, 1.0), leather, M((0, -0.05, 0.05), scale=(0.15, 0.065, 0.055)), smooth=True),
                part("Shaft", kit.cylinder(8, 0.06, 0.24), leather, M((0.13, 0.0, 0.055), rot=(0, 90, 8))),
                part("Sole", kit.cube(), kit.mat("HoldSole", "#24201d"), M((0, -0.05, 0.05), rot=(0, 0, 0), scale=(0.3, 0.02, 0.1)))]
    if kind == "rag":
        return [part("Rag", blob(0.3, 6, 0.025, seg=20, rough=0.5, squash=0.7), cloth, M(rot=(0, 0, rng.uniform(0, 360))), smooth=True),
                part("Fold", blob(0.14, 4, 0.04, seg=12, rough=0.4), cloth, M((0.08, 0.04, 0.0)), smooth=True)]
    if kind == "bottle":  # on its side
        return [part("Body", kit.cylinder(10, 0.045, 0.2), glass, M((0, 0, 0.045), rot=(90, 0, 0)), smooth=True),
                part("Neck", kit.cylinder(8, 0.018, 0.07), glass, M((0, -0.13, 0.045), rot=(90, 0, 0)))]
    if kind == "cup":  # tipped over
        return [part("Cup", kit.cone(10, 0.05, 0.06, 0.1), clay, M((0, 0, 0.05), rot=(90, 0, 0)), smooth=True)]
    if kind == "comb":
        parts = [part("Back", kit.cube(), bone, M((0, 0, 0.006), scale=(0.16, 0.025, 0.01)))]
        for i in range(9):
            parts.append(part("Tooth", kit.cube(), bone, M((-0.07 + i * 0.0175, 0.03, 0.005), scale=(0.007, 0.04, 0.007))))
        return parts
    if kind == "spoon":
        return [part("Bowl", kit.sphere(8, 5, 1.0), tarnish, M((0, -0.07, 0.01), scale=(0.035, 0.05, 0.012)), smooth=True),
                part("Handle", kit.cube(), tarnish, M((0, 0.05, 0.008), rot=(4, 0, 0), scale=(0.015, 0.16, 0.006)))]
    if kind == "coins":
        parts = []
        for _ in range(rng.randint(4, 7)):
            x, y = rng.uniform(-0.09, 0.09), rng.uniform(-0.09, 0.09)
            parts.append(part("Coin", kit.cylinder(10, 0.022, 0.006), tarnish,
                              M((x, y, 0.004), rot=(rng.uniform(-10, 10), rng.uniform(-10, 10), 0))))
        return parts
    raise ValueError(kind)


def build(variant):
    if variant.startswith(("lichen", "moss")):
        parts = growth(variant)
    elif variant.startswith("fungus"):
        parts = fungus(variant == "fungus_glow")
    elif variant.startswith("puddle"):
        parts = puddle()
    else:
        parts = dropped(variant)
    ob = kit.join(parts, "HoldFloor_" + variant)
    # (a little larger than life: seen from the game camera; the belongings more so, or they're specks)
    if not variant.startswith(("lichen", "moss", "puddle")):
        from mathutils import Matrix
        ob.data.transform(Matrix.Scale(1.35, 4))
    return [ob]
