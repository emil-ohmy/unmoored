"""The Antediluvian Anchoress: a Laden who would not let the Ferryman go. She hauled his ferry's anchor up from the
riverbed and chained it to herself, so he would have to come back for it (her lantern hung on it, for him to see by), and kept vigil for him in the drowned chapel
until she Soured around it. Half again a Laden's size, veiled in old linen, the anchor held upright in front of her
like a shield (a cross over a crescent). Its chain is fastened at her waist with the Ferryman's hook, stolen off the
anchor line: the game draws the chain (from the hook to the anchor's ring) and flies the anchor itself when she lets
it out on the chain (the Anchor bone, which carries nothing else).

Clips: Vigil (kneeling over the anchor, head bowed: before the fight), Idle, Walk (the anchor dragged), GuardUp /
GuardWalk / Block (the anchor lifted in front of her face), Windup / Strike / Recover (the Anchor Drop: raised high,
brought down in front), SweepWindup / Sweep / SweepRecover (the Chain Sweep: the anchor let out and swung round),
CastWindup / Cast / Haul (Cast and Haul: hefted back, thrown, then hauled in hand over hand), Reel (pulling it home),
Stagger (the anchor slips from her hands), Death. Phase two: Rise (the anchor lifted to the sky and planted: the floor
starts to break), YankWindup / Yank / YankRecover (the Hook Yank: the anchor planted, the Ferryman's hook whirled on its
line, thrown, and hauled back with whoever it caught).
"""
import math

M, part = kit.M, kit.part  # `kit` is injected by build_model.py

BONES = {
    "Root": ((0, 0, 0), (0, 0, 0.5), None),
    "Spine": ((0, 0, 0.5), (0, -0.2, 1.5), "Root"),
    "Head": ((0, -0.2, 1.5), (0, -0.35, 1.95), "Spine"),
    "Arms": ((0, -0.1, 1.42), (0, -0.62, 1.2), "Spine"),   # both arms, from the shoulders: they carry the anchor
    "Anchor": ((0, -0.8, 1.2), (0, -0.8, 0.3), "Arms"),     # the anchor alone, pivoting at her grip
    "Hook": ((0, -0.62, 1.0), (0, -0.62, 0.8), "Spine"),    # the Ferryman's hook at her waist (hidden while it's thrown)
}

# Held low in front, crown on the ground: open (her face shows over the stock).
OPEN = {"Spine": (8, 0, 0), "Head": (6, 0, 0), "Arms": (6, 0, 0), "Anchor": ((-6, 0, 0), (0, 0, 0.2))}
# Lifted in front of her face: guarding.
GUARD = {"Spine": (-4, 0, 0), "Head": (10, 0, 0), "Arms": ((-14, 0, 0), (0, 0.02, 0.2)), "Anchor": (14, 0, 0),
         "Root": ((0, 0, 0), (0, 0, -0.05))}
# Raised high over her head for the drop, crown to the sky.
RAISED = {"Spine": (-18, 0, 0), "Head": (-12, 0, 0), "Arms": (-150, 0, 0), "Anchor": (0, 0, 0),
          "Root": ((0, 0, 0), (0, 0.1, 0.04))}
# Brought down in front: the crown buried in the floor a long stride ahead.
DROPPED = {"Spine": (34, 0, 0), "Head": (16, 0, 0), "Arms": ((-40, 0, 0), (0, -0.05, 0)), "Anchor": ((-48, 0, 0), (0, 0, -0.12)),
           "Root": ((0, 0, 0), (0, -0.25, -0.12))}
# Out on the chain, swung round her: the arms out to one side, hands on the chain.
SWING_R = {"Spine": (6, 0, 50), "Head": (4, 0, 22), "Arms": (-40, 0, 70), "Anchor": (0, 0, 0),
           "Root": ((0, 0, 0), (0, 0.04, -0.06))}
SWING_L = {"Spine": (10, 0, -60), "Head": (6, 0, -26), "Arms": (-40, 0, -80), "Anchor": (0, 0, 0),
           "Root": ((0, 0, 0), (0, -0.04, -0.08))}
# Hefted back over her right shoulder for the cast, then flung forward.
HEFTED = {"Spine": (-16, 0, 30), "Head": (-6, 0, 12), "Arms": (-140, 0, 40), "Anchor": (0, 0, 0),
          "Root": ((0, 0, 0), (0, 0.12, -0.04))}
THROWN = {"Spine": (26, 0, -12), "Head": (14, 0, -6), "Arms": (-70, 0, -10), "Anchor": (0, 0, 0),
          "Root": ((0, 0, 0), (0, -0.2, -0.1))}
# Planted: the anchor stood on its crown right in front of her, both hands on it (phase two's Rise, and the Yank).
PLANTED = {"Spine": (20, 0, 0), "Head": (10, 0, 0), "Arms": ((10, 0, 0), (0, 0, -0.05)), "Anchor": ((-10, 0, 0), (0, 0, 0.1)),
           "Root": ((0, 0, 0), (0, 0, -0.1))}
# Hauling in hand over hand: braced, leaning back, arms low in front.
HAUL_A = {"Spine": (-14, 0, 6), "Head": (8, 0, 0), "Arms": (-30, 0, 8), "Root": ((0, 0, 0), (0, 0.1, -0.12))}
HAUL_B = {"Spine": (-20, 0, -6), "Head": (6, 0, 0), "Arms": (-10, 0, -8), "Root": ((0, 0, 0), (0, 0.14, -0.14))}


def with_(pose, **changes):
    merged = dict(pose)
    merged.update(changes)
    return merged


ACTIONS = {
    "Vigil": ("BEZIER", [  # kneeling over the anchor, head bowed: praying for a ferry that doesn't come
        (1, {"Spine": (24, 0, 0), "Head": (30, 0, 0), "Arms": (18, 0, 0), "Anchor": ((-18, 0, 0), (0, 0, 1.1)), "Root": ((0, 0, 0), (0, 0, -0.42))}),
        (40, {"Spine": (27, 0, 0), "Head": (34, 0, 0), "Arms": (20, 0, 0), "Anchor": ((-20, 0, 0), (0, 0, 1.16)), "Root": ((0, 0, 0), (0, 0, -0.44))}),
        (80, {"Spine": (24, 0, 0), "Head": (30, 0, 0), "Arms": (18, 0, 0), "Anchor": ((-18, 0, 0), (0, 0, 1.1)), "Root": ((0, 0, 0), (0, 0, -0.42))}),
    ]),
    "Idle": ("BEZIER", [
        (1, OPEN),
        (30, with_(OPEN, Spine=(11, 0, 0), Root=((0, 0, 0), (0, 0, -0.03)))),
        (60, OPEN),
    ]),
    "Walk": ("BEZIER", [  # a heavy, rolling walk, the anchor dragged in front
        (1, with_(OPEN, Spine=(10, 6, -6))),
        (13, with_(OPEN, Root=((0, 0, 0), (0, 0, 0.05)))),
        (25, with_(OPEN, Spine=(10, -6, 6))),
        (37, with_(OPEN, Root=((0, 0, 0), (0, 0, 0.05)))),
        (49, with_(OPEN, Spine=(10, 6, -6))),
    ]),
    "GuardUp": ("BEZIER", [
        (1, OPEN),
        (8, with_(GUARD, Arms=((-10, 0, 0), (0, 0.02, 0.12)))),
        (12, GUARD),
    ]),
    "GuardWalk": ("BEZIER", [
        (1, with_(GUARD, Spine=(-4, 3, 0))),
        (19, with_(GUARD, Root=((0, 0, 0), (0, 0, -0.02)))),
        (37, with_(GUARD, Spine=(-4, -3, 0))),
        (55, with_(GUARD, Root=((0, 0, 0), (0, 0, -0.02)))),
        (73, with_(GUARD, Spine=(-4, 3, 0))),
    ]),
    "Block": ("BEZIER", [
        (1, GUARD),
        (3, with_(GUARD, Spine=(-10, 0, 0), Arms=((-20, 0, 0), (0, 0.12, 0.2)), Root=((0, 0, 0), (0, 0.06, -0.05)))),
        (10, GUARD),
    ]),
    # The Anchor Drop: 1.1 s wind-up (up, up, and a hang at the top), a 0.35 s drop, 1.5 s to heave it free.
    "Windup": ("BEZIER", [
        (1, GUARD),
        (9, {"Spine": (12, 0, 0), "Arms": (6, 0, 0), "Anchor": (-10, 0, 0), "Root": ((0, 0, 0), (0, 0, -0.1))}),
        (26, with_(RAISED, Arms=(-138, 0, 0))),
        (34, RAISED),
    ]),
    "Strike": ("LINEAR", [
        (1, RAISED),
        (5, {"Spine": (20, 0, 0), "Head": (8, 0, 0), "Arms": (-70, 0, 0), "Anchor": (-30, 0, 0), "Root": ((0, 0, 0), (0, -0.15, -0.04))}),
        (9, DROPPED),
        (11, with_(DROPPED, Root=((0, 0, 0), (0, -0.27, -0.15)))),
    ]),
    "Recover": ("BEZIER", [
        (1, DROPPED),
        (20, with_(DROPPED, Spine=(30, 0, 0), Root=((0, 0, 0), (0, -0.25, -0.1)))),
        (32, with_(DROPPED, Spine=(18, 0, 0), Arms=((-30, 0, 0), (0, 0, 0)), Anchor=(-30, 0, 0), Root=((0, 0, 0), (0, -0.12, -0.05)))),
        (45, OPEN),
    ]),
    # The Chain Sweep: the anchor let out on its chain to her right (0.8 s), swung round in front to her left
    # (0.45 s), then hauled back into her hands (0.9 s). The game flies the anchor itself.
    "SweepWindup": ("BEZIER", [
        (1, GUARD),
        (14, with_(SWING_R, Arms=(-30, 0, 60))),
        (24, SWING_R),
    ]),
    "Sweep": ("LINEAR", [
        (1, SWING_R),
        (7, {"Spine": (12, 0, -5), "Head": (6, 0, -2), "Arms": (-45, 0, -5), "Root": ((0, 0, 0), (0, -0.1, -0.08))}),
        (14, SWING_L),
    ]),
    "SweepRecover": ("BEZIER", [
        (1, SWING_L),
        (12, with_(HAUL_A, Spine=(-10, 0, -30), Arms=(-30, 0, -30))),
        (22, HAUL_B),
        (28, OPEN),
    ]),
    # Cast and Haul: hefted back (1.0 s), flung (0.3 s), then hauled in (a loop, while the anchor drags home).
    "CastWindup": ("BEZIER", [
        (1, GUARD),
        (12, with_(HEFTED, Arms=(-110, 0, 30))),
        (26, HEFTED),
        (30, with_(HEFTED, Spine=(-20, 0, 36))),
    ]),
    "Cast": ("LINEAR", [
        (1, with_(HEFTED, Spine=(-20, 0, 36))),
        (5, {"Spine": (14, 0, 6), "Head": (6, 0, 0), "Arms": (-110, 0, 6), "Root": ((0, 0, 0), (0, -0.1, -0.06))}),
        (10, THROWN),
    ]),
    "Haul": ("BEZIER", [
        (1, HAUL_A),
        (9, HAUL_B),
        (17, HAUL_A),
    ]),
    "Reel": ("BEZIER", [  # pulling a dropped anchor home before she can fight with it again
        (1, HAUL_B),
        (8, HAUL_A),
        (16, OPEN),
    ]),
    # Staggered: the anchor slips from her hands and thuds down; she sags over it, then drags it back up.
    "Stagger": ("BEZIER", [
        (1, GUARD),
        (5, {"Spine": (-16, 0, 6), "Head": (-14, 0, 0), "Arms": ((-6, 0, 0), (0, 0.08, 0.1)), "Anchor": (10, 0, 0), "Root": ((0, 0, 0), (0, 0.12, 0))}),
        (12, {"Spine": (26, 0, -4), "Head": (24, 0, 0), "Arms": ((30, 0, 0), (0, 0, -0.1)), "Anchor": ((-40, 0, 10), (0, 0, 1.5)), "Root": ((0, 0, 0), (0, -0.05, -0.2))}),
        (38, {"Spine": (30, 0, -4), "Head": (28, 0, 0), "Arms": ((32, 0, 0), (0, 0, -0.12)), "Anchor": ((-42, 0, 10), (0, 0, 1.56)), "Root": ((0, 0, 0), (0, -0.05, -0.22))}),
        (52, OPEN),
    ]),
    # Phase two begins (2.2 s): the anchor raised slowly to the sky, held there trembling, then planted with all her weight
    # (at 2/3: the floor shudders and breaks), and she straightens.
    "Rise": ("BEZIER", [
        (1, GUARD),
        (20, with_(RAISED, Spine=(-26, 0, 0), Head=(-24, 0, 0))),
        (34, with_(RAISED, Spine=(-28, 0, 3), Head=(-26, 0, 0), Arms=(-152, 0, 2))),
        (40, with_(RAISED, Spine=(-30, 0, -3), Head=(-26, 0, 0), Arms=(-148, 0, -2))),
        (44, PLANTED),
        (46, with_(PLANTED, Root=((0, 0, 0), (0, 0, -0.16)))),
        (66, OPEN),
    ]),
    # The Hook Yank: the anchor planted, turned to swing the hook's line overhead (the game whirls the hook itself),
    # the throw, then the haul back.
    "YankWindup": ("BEZIER", [
        (1, GUARD),
        (10, with_(PLANTED, Spine=(-6, 0, 24), Head=(-14, 0, 10))),
        (20, with_(PLANTED, Spine=(-8, 0, 30), Head=(-16, 0, 12))),
        (33, with_(PLANTED, Spine=(-8, 0, 34), Head=(-16, 0, 14))),
    ]),
    "Yank": ("LINEAR", [
        (1, with_(PLANTED, Spine=(-8, 0, 34), Head=(-16, 0, 14))),
        (5, with_(PLANTED, Spine=(24, 0, -14), Head=(6, 0, -6))),
        (18, with_(PLANTED, Spine=(22, 0, -12), Head=(4, 0, -6))),
    ]),
    "YankRecover": ("BEZIER", [
        (1, with_(PLANTED, Spine=(22, 0, -12), Head=(4, 0, -6))),
        (8, with_(PLANTED, Spine=(-18, 0, 4), Head=(8, 0, 0), Root=((0, 0, 0), (0, 0.12, -0.12)))),
        (27, OPEN),
    ]),
    # Death: a jolt back, down onto her knees, and forward over the anchor.
    "Death": ("BEZIER", [
        (1, GUARD),
        (6, {"Spine": (-16, 0, 6), "Head": (-20, 0, 0), "Arms": (-24, 0, 0), "Root": ((0, 0, 0), (0, 0.1, 0))}),
        (16, {"Spine": (20, 0, -6), "Head": (26, 0, 0), "Arms": (18, 0, 0), "Anchor": ((-18, 0, 0), (0, 0, 0.8)), "Root": ((0, 0, 0), (0, 0, -0.42))}),
        (26, {"Spine": (48, 0, -8), "Head": (40, 0, 0), "Arms": (40, 0, 0), "Anchor": ((-60, 0, 0), (0, 0, 1.0)), "Root": ((0, 0, 0), (0, -0.1, -0.46))}),
    ]),
}


def build():
    rng = kit.rng
    robe, dark = kit.mat("Soured", "#5e5074"), kit.mat("SouredDark", "#3e3450")
    linen, linen_dark = kit.mat("VeilLinen", "#aca38f"), kit.mat("VeilLinenDark", "#7d7666")
    eye, claw = kit.mat("SouredEye_Glow", "#c8f07a"), kit.mat("Claw", "#d6cdb8")
    iron, rust = kit.mat("AnchorIron", "#6c6b75"), kit.mat("AnchorRust", "#7a4a32")
    weed, barnacle = kit.mat("RiverWeed", "#4d5c38"), kit.mat("Barnacle", "#9d9786")
    hook = kit.mat("HookIron", "#3b3a42")
    # (the ring and the arms get their own materials, the same iron, so the game can find them: the chain ties to the
    # ring; the arms' lowest point is the crown that hits the floor)
    ring_iron, arms_iron = kit.mat("AnchorRing", "#6c6b75"), kit.mat("AnchorArms", "#6c6b75")

    def robe_shape(bm):
        kit.cone(11, 0.74, 0.34, 1.5)(bm)
        rim = sorted((v for v in bm.verts if v.co.z < -0.7 and v.co.length > 0.1), key=lambda v: math.atan2(v.co.y, v.co.x))
        for i, v in enumerate(rim):
            v.co.z += 0.12 if i % 2 else 0.0  # tattered, silted hem

    parts = [
        part("Robe", robe_shape, robe, M((0, 0.02, 0.78), rot=(10, 0, 0)), smooth=True, group="Spine"),
        part("Hump", kit.sphere(10, 7), robe, M((0, 0.06, 1.38), scale=(0.56, 0.46, 0.38)), smooth=True, group="Spine"),
        part("Head", kit.sphere(10, 7, 0.26), dark, M((0, -0.3, 1.7), scale=(1, 1.05, 0.95)), smooth=True, group="Head"),
        # The veil: a tall pointed hood of old linen with a wimple band at the brow, and a long drape down her back.
        part("Veil", kit.sphere(10, 7), linen, M((0, -0.16, 1.8), scale=(0.34, 0.34, 0.37)), smooth=True, group="Head"),
        part("VeilPeak", kit.cone(8, 0.2, 0.03, 0.26), linen, M((0, -0.1, 2.12), rot=(-28, 0, 0)), smooth=True, group="Head"),
        part("Wimple", kit.cylinder(12, 0.29, 0.08), linen_dark, M((0, -0.32, 1.84), rot=(-22, 0, 0)), smooth=True, group="Head"),
        part("VeilDrape", kit.cone(9, 0.26, 0.44, 0.85), linen_dark, M((0, 0.16, 1.36), rot=(20, 0, 0)), smooth=True, group="Spine"),
    ]
    for s in (-1, 1):
        parts.append(part("Eye", kit.sphere(8, 6, 0.045), eye, M((s * 0.09, -0.54, 1.7)), smooth=True, group="Head"))
        shoulder, hand = (s * 0.5, -0.1, 1.42), (s * 0.2, -0.72, 1.2)
        parts.append(part("Arm", kit.cone(6, 0.14, 0.08, 0.72), robe, kit.along(shoulder, hand), smooth=True, group="Arms"))
        parts.append(part("Claw", kit.cone(5, 0.05, 0.0, 0.16), claw, kit.along(hand, (s * 0.1, -0.84, 1.18)), group="Arms"))

    # The chain round her waist (the anchor's chain is fastened to it with the Ferryman's hook, at the front).
    links = 22
    for i in range(links):
        a = 2 * math.pi * i / links
        x, y = 0.6 * math.cos(a), 0.56 * math.sin(a)
        parts.append(part("WaistLink", kit.cube(), iron, M((x, y, 0.98 + 0.03 * math.sin(3 * a)), rot=(90 * (i % 2), 0, math.degrees(a) + 90),
                                                            scale=(0.16, 0.05, 0.07)), group="Spine"))
    # The Ferryman's hook, closed over the waist chain at her belly: where the anchor's chain starts.
    k = 1.8
    hook_at = (0.0, -0.62, 1.0)

    def h(dx, dy, dz):
        return (hook_at[0] + dx * k, hook_at[1] + dy * k, hook_at[2] + dz * k)

    parts += [
        part("FerrymansHookShank", kit.cylinder(5, 0.024 * k, 0.2 * k), hook, M(h(0, 0, -0.1), rot=(0, 0, 0)), group="Hook"),
        part("FerrymansHookProngL", kit.tube([h(0, 0, -0.2), h(-0.07, 0, -0.23), h(-0.1, -0.02, -0.16)], [0.02 * k, 0.016 * k, 0.01 * k], seg=5), hook, group="Hook"),
        part("FerrymansHookProngR", kit.tube([h(0, 0, -0.2), h(0.07, 0, -0.23), h(0.1, -0.02, -0.16)], [0.02 * k, 0.016 * k, 0.01 * k], seg=5), hook, group="Hook"),
        part("FerrymansHookEye", kit.cylinder(6, 0.032 * k, 0.02 * k), hook, M(h(0, 0, 0.0), rot=(90, 0, 0)), group="Hook"),
    ]


    # The anchor, upright in front of her: ring on top, the stock across (a cross), the shank down to the crown, and
    # the two arms curving up either side into flukes (the crescent). River weed and barnacles on it; rust in the seams.
    y = -0.8
    ring = [(0.13 * math.cos(a), y, 1.66 + 0.13 * math.sin(a)) for a in (2 * math.pi * i / 12 for i in range(13))]
    arm = [(-0.66, y, 0.66), (-0.52, y, 0.36), (-0.3, y, 0.18), (0, y, 0.12), (0.3, y, 0.18), (0.52, y, 0.36), (0.66, y, 0.66)]
    anchor = [
        part("AnchorRing", kit.tube(ring, [0.035] * len(ring), seg=5), ring_iron),
        part("AnchorShank", kit.cylinder(7, 0.075, 1.5), iron, M((0, y, 0.8))),
        part("AnchorStock", kit.cube(), iron, M((0, y, 1.44), scale=(1.0, 0.1, 0.11))),
        part("AnchorStockEnd", kit.sphere(6, 5, 0.075), iron, M((-0.52, y, 1.44))),
        part("AnchorStockEnd", kit.sphere(6, 5, 0.075), iron, M((0.52, y, 1.44))),
        part("AnchorArms", kit.tube(arm, [0.06, 0.075, 0.085, 0.095, 0.085, 0.075, 0.06], seg=7), arms_iron),
        part("AnchorCrown", kit.sphere(8, 6, 0.12), rust, M((0, y, 0.13), scale=(1.2, 0.8, 0.8))),
        part("AnchorRust", kit.cube(), rust, M((0, y, 1.36), scale=(0.2, 0.11, 0.04))),
    ]
    for s in (-1, 1):
        anchor.append(part("AnchorFluke", kit.cube(), iron, M((s * 0.7, y, 0.74), rot=(0, s * 32, 0), scale=(0.2, 0.06, 0.3))))
        anchor.append(part("AnchorFlukeRust", kit.cube(), rust, M((s * 0.71, y - 0.01, 0.72), rot=(0, s * 32, 0), scale=(0.1, 0.06, 0.12))))
    for x, z, length in ((-0.34, 1.38, 0.34), (0.2, 1.38, 0.24), (0.42, 1.38, 0.3), (-0.05, 0.2, 0.18)):
        anchor.append(part("Weed", kit.cone(5, 0.04, 0.005, length), weed, M((x, y - 0.03, z - length / 2), rot=(180 + rng.uniform(-10, 10), 0, 0))))
    for i in range(9):
        t = rng.uniform(0.1, 0.9)
        x = -0.6 + 1.2 * t
        z = 0.12 + 0.5 * (x / 0.66) ** 2 + 0.02
        anchor.append(part("Barnacle", kit.cone(5, 0.035, 0.012, 0.03), barnacle, M((x, y - 0.07, z), rot=(90, 0, 0))))
    # A Soured's lantern, sickly green like her eyes, hung from the stock's end: a light for the ferry to see by.
    lantern_glass = kit.mat("SouredLantern_Glow", "#b6f06a")
    lx = 0.56
    anchor += [
        part("BeltLanternCord", kit.cube(), iron, M((lx, y - 0.02, 1.33), scale=(0.015, 0.015, 0.12))),
        part("BeltLanternTop", kit.cone(4, 0.1, 0.01, 0.08), iron, M((lx, y - 0.02, 1.25), rot=(0, 0, 45))),
        part("BeltLanternGlass", kit.cube(), lantern_glass, M((lx, y - 0.02, 1.14), scale=(0.12, 0.12, 0.15))),
        part("BeltLanternBase", kit.cube(), iron, M((lx, y - 0.02, 1.055), scale=(0.14, 0.14, 0.02))),
    ]
    for p in anchor:
        p.vertex_groups.new(name="Anchor").add(list(range(len(p.data.vertices))), 1.0, "REPLACE")
    parts += anchor

    mesh = kit.join(parts, "Anchoress")
    armature = kit.rig("AnchoressRig", BONES)
    kit.skin(mesh, armature)
    kit.animate(armature, ACTIONS)
    return [armature, mesh]
