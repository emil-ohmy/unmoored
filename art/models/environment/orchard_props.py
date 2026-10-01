"""The orchard's things, left where they were the night the harvest didn't come: "basket" (a picking basket heaped with
apples), "crate" (a slatted apple crate, full), "ladder" (a tall picking ladder; not set against trees: it looked climbable),
"well" (a round stone well with its roof frame, windlass and bucket), "skep" (a straw beehive on a stand,
silent), "scarecrow" (on its pole, in a coat and hat, arms out), "pole" (a scarecrow's stake alone), "press" (the cider press: a heavy timber frame, its
great wooden screw and the tub under it), "apples" (fallen fruit in the leaves) and "lantern" (a lantern hung from a
shepherd's crook, warm: the orchard marks its ways with them). Origin at the base, game directions (see masonry.py)."""
import math
import masonry as m

VARIANTS = ["basket", "crate", "ladder", "well", "skep", "scarecrow", "press", "apples", "lantern", "pole"]


def build(variant):
    m.orchard()
    kit, M, part, box = m.kit, m.M, m.part, m.box
    rng = kit.rng
    wicker, wood = kit.mat("Wicker", "#8a6a3e"), kit.mat("OrchardWood", "#6a5038")
    apple, apple2 = kit.mat("Fruit", "#8a2a20"), kit.mat("Fruit2", "#9e4a24")
    parts = []

    def apples(cx, cy, z, spread, count, r=0.07):
        for _ in range(count):
            a, d = rng.uniform(0, math.tau), spread * math.sqrt(rng.random())
            parts.append(part("Apple", kit.ico(1, r), rng.choice([apple, apple2]), M((cx + math.cos(a) * d, cy + math.sin(a) * d, z + rng.uniform(0, r)))))

    if variant == "basket":
        parts.append(part("Basket", kit.cone(10, 0.24, 0.32, 0.36), wicker, M((0, 0, 0.18)), jitter=0.008))
        for k in range(4):
            parts.append(part("Weave", kit.cylinder(10, 0.25 + k * 0.02, 0.025), kit.mat("WickerDark", "#6a4f2c"), M((0, 0, 0.06 + k * 0.08))))
        parts.append(part("Handle", kit.tube([(-0.28, 0, 0.36), (0, 0, 0.62), (0.28, 0, 0.36)], [0.02] * 3, seg=4), wicker))
        apples(0, 0, 0.34, 0.22, 14)
    elif variant == "crate":
        for z in (0.05, 0.22, 0.39):
            for s in (-1, 1):
                parts.append(box("Slat", wood, -0.4, 0.4, s * 0.28 - 0.015, s * 0.28 + 0.015, z, z + 0.1))
                parts.append(box("Slat", wood, s * 0.4 - 0.015, s * 0.4 + 0.015, -0.28, 0.28, z, z + 0.1))
        parts.append(box("Bottom", wood, -0.4, 0.4, -0.28, 0.28, 0.0, 0.04))
        apples(0, 0, 0.44, 0.3, 22)
    elif variant == "ladder":
        h = 3.2
        for s in (-1, 1):
            parts.append(box("Rail", wood, s * 0.24 - 0.035, s * 0.24 + 0.035, -0.035, 0.035, 0, h))
        for k in range(10):
            z = 0.3 + k * 0.3
            w = 0.24 - k * 0.006
            parts.append(box("Rung", wood, -w, w, -0.025, 0.025, z, z + 0.04))
    elif variant == "well":
        stones = [m.mat(s) for s in m.STONE]
        for k in range(14):
            a = math.tau * k / 14
            for row in range(3):
                parts.append(part("WellStone", kit.cube(), stones[(k + row) % 3], M((math.cos(a + row * 0.2) * 0.75, math.sin(a + row * 0.2) * 0.75, 0.15 + row * 0.27),
                                                                                    rot=(0, 0, math.degrees(a + row * 0.2)), scale=(0.24, 0.36, 0.26))))
        parts.append(part("Water", kit.cylinder(14, 0.6, 0.02), kit.mat("WellWater", "#0c1a1e"), M((0, 0, 0.55))))
        for s in (-1, 1):
            parts.append(box("Post", wood, s * 0.85 - 0.06, s * 0.85 + 0.06, -0.06, 0.06, 0, 2.0))
        parts.append(part("Windlass", kit.cylinder(8, 0.07, 1.7), wood, M((0, 0, 1.45), rot=(0, 90, 0))))
        parts.append(part("Rope", kit.cylinder(4, 0.012, 0.6), kit.mat("Rope", "#a38b62"), M((0, 0, 1.12))))
        parts.append(part("Bucket", kit.cone(8, 0.13, 0.16, 0.22), wood, M((0, 0, 0.75))))
        for s in (-1, 1):
            parts.append(part("Roof", kit.cube(), kit.mat("Tiles1", "#7a4a3a"), M((0, s * 0.42, 2.15), rot=(-s * 35, 0, 0), scale=(2.0, 0.95, 0.06))))
    elif variant == "skep":
        straw = kit.mat("Straw", "#b08f52")
        parts.append(box("Stand", wood, -0.3, 0.3, -0.3, 0.3, 0.55, 0.62))
        for s in ((-1, -1), (-1, 1), (1, -1), (1, 1)):
            parts.append(box("Leg", wood, s[0] * 0.25 - 0.03, s[0] * 0.25 + 0.03, s[1] * 0.25 - 0.03, s[1] * 0.25 + 0.03, 0, 0.55))
        for k in range(6):
            r = 0.28 * math.cos(k / 6 * math.pi / 2) + 0.02
            parts.append(part("Coil", kit.cylinder(10, r, 0.09), straw, M((0, 0, 0.67 + k * 0.08))))
        parts.append(box("Door", kit.mat("Ink", "#1a1622"), -0.05, 0.05, -0.29, -0.25, 0.63, 0.7))
    elif variant == "scarecrow":
        coat, hat, sack = kit.mat("Coat", "#4a3a44"), kit.mat("Hat", "#2e2622"), kit.mat("Sack", "#a89068")
        parts.append(box("Pole", wood, -0.04, 0.04, -0.04, 0.04, 0, 2.2))
        parts.append(box("Arms", wood, -0.75, 0.75, -0.03, 0.03, 1.5, 1.56))
        parts.append(part("Coat", kit.cone(8, 0.32, 0.2, 0.85), coat, M((0, 0, 1.15)), jitter=0.02))
        for s in (-1, 1):
            parts.append(part("Sleeve", kit.tube([(s * 0.18, 0, 1.5), (s * 0.7, 0, 1.47)], [0.1, 0.07], seg=6), coat))
            parts.append(part("Straw", kit.cone(5, 0.06, 0.0, 0.18), kit.mat("Straw", "#b08f52"), M((s * 0.82, 0, 1.46), rot=(0, s * 90, 0))))
        parts.append(part("Head", kit.sphere(8, 6, 0.17), sack, M((0, 0, 1.78)), smooth=True))
        parts.append(part("HatBrim", kit.cylinder(10, 0.3, 0.03), hat, M((0, 0, 1.92), rot=(6, 0, 0))))
        parts.append(part("HatCrown", kit.cone(8, 0.15, 0.12, 0.22), hat, M((0, 0, 2.04), rot=(6, 0, 0))))
        for s in (-1, 1):
            parts.append(part("Eye", kit.cube(), kit.mat("Ink", "#1a1622"), M((s * 0.06, -0.16, 1.8), scale=(0.04, 0.01, 0.04))))
    elif variant == "press":
        heavy = kit.mat("PressWood", "#4e3a2c")
        for s in (-1, 1):
            parts.append(box("Upright", heavy, s * 0.7 - 0.12, s * 0.7 + 0.12, -0.12, 0.12, 0, 2.06))  # (under the head beam's top)
        parts.append(box("Head", heavy, -0.85, 0.85, -0.15, 0.15, 1.8, 2.1))
        parts.append(box("Bed", heavy, -0.85, 0.85, -0.6, 0.6, 0.35, 0.5))
        for s in ((-1, -1), (-1, 1), (1, -1), (1, 1)):
            parts.append(box("Foot", heavy, s[0] * 0.75 - 0.08, s[0] * 0.75 + 0.08, s[1] * 0.5 - 0.08, s[1] * 0.5 + 0.08, 0, 0.35))
        parts.append(part("Screw", kit.cylinder(10, 0.12, 1.3), wood, M((0, 0, 1.3))))
        for k in range(8):
            parts.append(part("Thread", kit.cylinder(10, 0.14, 0.03), heavy, M((0, 0, 0.8 + k * 0.13), rot=(8, 0, k * 20))))
        parts.append(box("Lever", wood, -0.9, 0.9, -0.04, 0.04, 1.6, 1.68))
        parts.append(part("Tub", kit.cylinder(12, 0.48, 0.32), wood, M((0, 0, 0.66))))
        parts.append(part("Pomace", kit.cylinder(12, 0.44, 0.04), kit.mat("Pomace", "#5a3a26"), M((0, 0, 0.82))))
        parts.append(part("Board", kit.cylinder(12, 0.4, 0.06), heavy, M((0, 0, 0.88))))
    elif variant == "pole":
        # A scarecrow's stake (the figure on it is a character: the Watchman, or one of the scarecrows he stands
        # among), driven into the ground, a few turns of rope near the top.
        parts.append(box("Stake", wood, -0.045, 0.045, -0.045, 0.045, -0.2, 1.55))
        for k in range(3):
            parts.append(part("Rope", kit.cylinder(6, 0.06, 0.02), kit.mat("Rope", "#a38b62"), M((0, 0, 1.2 + k * 0.03))))
    elif variant == "apples":
        apples(0, 0, 0.02, 0.6, 9)
    else:
        lamp, metal = kit.mat("OrchardLantern_Glow", "#ffb460"), kit.mat("LanternIron", "#2c2b31")
        crook = [(0, 0, 0), (0, 0, 2.2), (0.08, 0, 2.45), (0.3, 0, 2.52), (0.48, 0, 2.38)]
        parts.append(part("Crook", kit.tube(crook, [0.045, 0.04, 0.035, 0.03, 0.025], seg=5), wood))
        parts.append(part("Hook", kit.cylinder(4, 0.008, 0.18), metal, M((0.48, 0, 2.27))))
        parts.append(part("LampTop", kit.cone(4, 0.1, 0.02, 0.07), metal, M((0.48, 0, 2.16), rot=(0, 0, 45))))
        parts.append(part("LampGlass", kit.cube(), lamp, M((0.48, 0, 2.03), scale=(0.11, 0.11, 0.16))))
        parts.append(part("LampBase", kit.cube(), metal, M((0.48, 0, 1.94), scale=(0.13, 0.13, 0.02))))
    return [kit.join(parts, f"OrchardProp_{variant}")]
