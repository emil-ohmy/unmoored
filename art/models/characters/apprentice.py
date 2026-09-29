"""The Ferryman's Apprentice: small, cloaked, bone-white mask, oversized oar (blade up). Rigged and animated.

Clip timings match the Middle combat tuning (the controller rescales them if tuning changes):
light 0.13 s windup / 0.10 s active / 0.28 s recovery; heavy 0.5 / 0.12 / 0.5; dash 0.38 s; heal 0.7 s.
RecoilHeavy / RecoilLight: bouncing off a Laden's heap (0.35 s).
"""
import math

from mathutils import Vector

M, part = kit.M, kit.part  # `kit` is injected by build_model.py

# Oar placement at rest: foot of the oar near the ground, leaning out to the right and slightly forward.
OAR = M((0.33, -0.08, 0.02), rot=(6, 8, 0))
HAND = OAR @ Vector((0, 0, 0.6))

BONES = {
    "Root": ((0, 0, 0), (0, 0, 0.2), None),
    "Pivot": ((0, 0, 0.45), (0, 0, 0.55), "Root"),  # centre of mass: whole-body leans (dash, death) pivot here
    "Body": ((0, 0, 0.1), (0, 0, 0.62), "Pivot"),
    "Head": ((0, 0, 0.62), (0, 0, 1.05), "Body"),
    "Oar": (tuple(HAND), tuple(OAR @ Vector((0, 0, 0.9))), "Body"),  # pivots at the hand
}

REST = {}
# Oar: (pitch, 0, yaw). +pitch tips the upright oar forward, +yaw swings it to the Apprentice's right.
# From the game camera a forward-tipped oar foreshortens and reads as "at the feet", so idle/run/dash carry it tipped back.
# The light swings are swung from the body, not flicked from the wrist. The hand rides a circle round the body's
# centre at arm's length and the oar points straight out along the same line, like a spoke, so hand and oar sweep
# together about the body (the eye reads the pivot, and the pivot is the body, not the hand). The torso turns with the
# swing and the head counter-turns so the mask keeps facing the target. Keys come nearly every frame so the hand
# follows the arc instead of cutting across it. Light1 is a forehand (from the hand's side across the front), Light2
# the backhand. Contact is mid-arc, the oar straight ahead (frame ~6: windup 0.13 s, active 0.10 s of 0.51 s).
HAND_REST = Vector((0.413, -0.143, 0.611))  # where the Oar bone's head (the hand) sits at rest
SWING_RADIUS = 0.46


def swing(phi, pitch=88.0, step=0.0, lift=0.0):
    """The swing pose at angle `phi` (degrees: 0 = straight ahead, + = the hand's side, round behind at +/-180)."""
    body_yaw = max(-55.0, min(55.0, 0.55 * phi))
    rad = math.radians(phi)
    hand_world = Vector((SWING_RADIUS * math.sin(rad), -SWING_RADIUS * math.cos(rad), HAND_REST.z + lift))
    by = math.radians(-body_yaw)  # the Oar's offset is in the (turning) body's frame
    local = Vector((hand_world.x * math.cos(by) - hand_world.y * math.sin(by),
                    hand_world.x * math.sin(by) + hand_world.y * math.cos(by), hand_world.z))
    offset = local - HAND_REST
    return {"Body": (4, 0, body_yaw), "Head": (0, 0, -0.5 * body_yaw),
            "Oar": ((pitch, 0, phi - body_yaw), tuple(offset)), "Root": ((0, 0, 0), (0, -step, 0))}


def swing_keys(arc):
    """[(frame, phi, pitch, step)] -> keys, between rest poses."""
    return [(1, REST)] + [(f, swing(phi, pitch, step)) for f, phi, pitch, step in arc] + [(15, REST)]


LIGHT1_ARC = [(3, 105, 80, 0.0), (4, 95, 82, 0.02), (5, 50, 86, 0.06), (6, 5, 88, 0.08), (7, -55, 88, 0.1),
              (8, -95, 85, 0.1), (10, -105, 82, 0.08)]
LIGHT2_ARC = [(3, -110, 80, 0.0), (4, -100, 82, 0.02), (5, -55, 86, 0.06), (6, -5, 88, 0.08), (7, 50, 88, 0.1),
              (8, 90, 85, 0.1), (10, 100, 82, 0.08)]
LIGHT1_HIT = swing(5, 88, 0.08)  # the contact pose (the recoil off a Laden's heap starts here)
# The heavy is a two-handed overhead chop: the oar goes up and back with the hands raised and the body rising, then
# the hands drive down in front, on the centre line (the attack's own lunge carries it forward; the clip doesn't step
# again, or the oar would bury itself in whatever it hits). The body only bends a little and the head stays level (it's
# the oar that lands, not the Apprentice's head). Contact at frame ~19 (windup 0.5 s, active 0.12 s of 1.12 s).
# It comes down on a slight diagonal (over one shoulder, across the front) and meets its target at chest height, the
# oar still pointing a little up: a chop that lands on the ground in front foreshortens, from the game's camera, into
# a stub hidden against the hood. The follow-through carries on down.
HEAVY_RAISED = {"Oar": ((-78, 0, 30), (-0.22, 0.08, 0.3)), "Body": (-12, 0, 22), "Head": (12, 0, -12), "Root": ((0, 0, 0), (0, 0.04, -0.03))}
HEAVY_CHOP = {"Oar": ((70, 0, -14), (-0.28, -0.22, 0.02)), "Body": (10, 0, -12), "Head": (-10, 0, 8), "Root": ((0, 0, 0), (0, -0.06, -0.06))}
ACTIONS = {
    "Idle": ("BEZIER", [
        (1, REST),
        (24, {"Body": (3, 0, 0), "Head": (-3, 0, 0), "Oar": (2, 0, 0), "Root": ((0, 0, 0), (0, 0, -0.015))}),
        (48, REST),
    ]),
    "Run": ("BEZIER", [
        (1, {"Body": (12, 0, 5), "Head": (-6, 0, 0), "Oar": (-18, 0, 0)}),
        (5, {"Body": (12, 0, 0), "Head": (-6, 0, 0), "Oar": (-15, 0, 3), "Root": ((0, 0, 0), (0, 0, 0.06))}),
        (9, {"Body": (12, 0, -5), "Head": (-6, 0, 0), "Oar": (-18, 0, 0)}),
        (13, {"Body": (12, 0, 0), "Head": (-6, 0, 0), "Oar": (-15, 0, -3), "Root": ((0, 0, 0), (0, 0, 0.06))}),
        (17, {"Body": (12, 0, 5), "Head": (-6, 0, 0), "Oar": (-18, 0, 0)}),
    ]),
    # Dash: a low forward lunge with the oar trailing behind (the i-frames are shown in game as ghost images).
    "Dash": ("BEZIER", [
        (1, {"Pivot": (12, 0, 0), "Head": (-8, 0, 0), "Oar": (-20, 0, 0), "Root": ((0, 0, 0), (0, 0, -0.05))}),
        (3, {"Pivot": (42, 0, 0), "Head": (-30, 0, 0), "Body": (6, 0, 0), "Oar": (-75, 0, -10), "Root": ((0, 0, 0), (0, 0, -0.1))}),
        (8, {"Pivot": (40, 0, 0), "Head": (-28, 0, 0), "Body": (6, 0, 0), "Oar": (-78, 0, -8), "Root": ((0, 0, 0), (0, 0, -0.1))}),
        (12, {"Pivot": (10, 0, 0), "Head": (-6, 0, 0), "Oar": (-15, 0, 0), "Root": ((0, 0, 0), (0, 0, -0.02))}),
    ]),
    "Light1": ("BEZIER", swing_keys(LIGHT1_ARC)),
    "Light2": ("BEZIER", swing_keys(LIGHT2_ARC)),
    "Heavy": ("BEZIER", [
        (1, REST),
        (10, {"Oar": ((-55, 0, 20), (-0.15, 0.05, 0.2)), "Body": (-9, 0, 15), "Head": (8, 0, -8), "Root": ((0, 0, 0), (0, 0.03, -0.02))}),
        (16, HEAVY_RAISED),
        (19, HEAVY_CHOP),
        (23, {"Oar": ((100, 0, -28), (-0.32, -0.24, -0.1)), "Body": (15, 0, -18), "Head": (-12, 0, 12), "Root": ((0, 0, 0), (0, -0.08, -0.1))}),
        (35, REST),
    ]),
    # Blocked by a Laden's heap: the oar strikes it and springs back, jarring the Apprentice (the recoil). Each starts
    # from its attack's contact pose, so the blow is seen to land before it bounces.
    "RecoilHeavy": ("BEZIER", [
        (1, HEAVY_CHOP),
        (3, {"Oar": (38, 0, 6), "Body": (-12, 0, 0), "Head": (-14, 0, 0), "Root": ((0, 0, 0), (0, 0.1, 0.0))}),
        (6, {"Oar": (18, 0, 4), "Body": (-8, 0, 0), "Head": (-8, 0, 0), "Root": ((0, 0, 0), (0, 0.06, -0.02))}),
        (10, REST),
    ]),
    "RecoilLight": ("BEZIER", [
        (1, LIGHT1_HIT),
        (3, {"Oar": (55, 0, 50), "Body": (-8, 0, 14), "Head": (-10, 0, 0), "Root": ((0, 0, 0), (0, 0.08, 0))}),
        (6, {"Oar": (30, 0, 25), "Body": (-4, 0, 6), "Head": (-5, 0, 0)}),
        (10, REST),
    ]),
    "Heal": ("BEZIER", [  # bows its head to the mask, a small glow of relief (0.7 s)
        (1, REST),
        (8, {"Body": (14, 0, 0), "Head": (22, 0, 0), "Oar": (-8, 0, 0), "Root": ((0, 0, 0), (0, 0, -0.04))}),
        (16, {"Body": (12, 0, 0), "Head": (18, 0, 0), "Oar": (-8, 0, 0), "Root": ((0, 0, 0), (0, 0, -0.04))}),
        (22, REST),
    ]),
    "Death": ("BEZIER", [  # crumples forward and stays down
        (1, REST),
        (8, {"Pivot": (35, 0, 0), "Head": (20, 0, 0), "Oar": (40, 0, 20), "Root": ((0, 0, 0), (0, 0, -0.1))}),
        (18, {"Pivot": (80, 0, 10), "Head": (25, 0, 0), "Oar": (85, 0, 35), "Root": ((0, 0, 0), (0, -0.25, -0.2))}),
        (30, {"Pivot": (82, 0, 10), "Head": (25, 0, 0), "Oar": (88, 0, 35), "Root": ((0, 0, 0), (0, -0.25, -0.22))}),
    ]),
    # Block: the oar held across the front, braced. Parry: a quick snap forward from the block (a perfect block).
    "Block": ("BEZIER", [
        (1, {"Oar": ((88, 0, -84), (-0.12, -0.34, 0.18)), "Body": (-6, 0, 0), "Head": (6, 0, 0), "Root": ((0, 0, 0), (0, 0, -0.04))}),
        (24, {"Oar": ((86, 0, -82), (-0.12, -0.34, 0.17)), "Body": (-5, 0, 0), "Head": (6, 0, 0), "Root": ((0, 0, 0), (0, 0, -0.05))}),
        (48, {"Oar": ((88, 0, -84), (-0.12, -0.34, 0.18)), "Body": (-6, 0, 0), "Head": (6, 0, 0), "Root": ((0, 0, 0), (0, 0, -0.04))}),
    ]),
    "Parry": ("BEZIER", [
        (1, {"Oar": ((88, 0, -84), (-0.12, -0.34, 0.18)), "Body": (-6, 0, 0), "Head": (6, 0, 0)}),
        (3, {"Oar": ((62, 0, -70), (-0.1, -0.52, 0.3)), "Body": (10, 0, 8), "Head": (-4, 0, 0), "Root": ((0, 0, 0), (0, -0.08, 0))}),
        (10, {"Oar": ((88, 0, -84), (-0.12, -0.34, 0.18)), "Body": (-4, 0, 0), "Head": (4, 0, 0)}),
    ]),
    "Hit": ("BEZIER", [
        (1, REST),
        (3, {"Body": (-18, 0, 0), "Head": (-12, 0, 0), "Oar": (-10, 0, 0), "Root": ((0, 0, 0), (0, 0.08, 0))}),
        (10, REST),
    ]),
}


def build():
    cloak, cloak_dark = kit.mat("Cloak", "#9c3b2b"), kit.mat("CloakDark", "#74291f")
    mask, ink, mark = kit.mat("Mask", "#ece4d0"), kit.mat("Ink", "#1a1622"), kit.mat("MaskMark", "#b8352b")
    wood, blade = kit.mat("OarWood", "#b08a5c"), kit.mat("OarBlade", "#8f6d48")
    rng = kit.rng

    def cloak_body(bm):
        kit.cone(10, 0.40, 0.13, 0.72)(bm)
        for v in bm.verts:
            if v.co.z < -0.3:  # ragged hem
                s = 1 + rng.uniform(-0.06, 0.06)
                v.co.x *= s
                v.co.y *= s
                v.co.z += rng.uniform(-0.025, 0.02)

    parts = [
        part("Cloak", cloak_body, cloak, M((0, 0, 0.36)), smooth=True, group="Body"),
        part("Hood", kit.sphere(10, 7, 0.25), cloak, M((0, 0.02, 0.84), scale=(1, 1, 1.05)), smooth=True, group="Head"),
        part("HoodTip", kit.cone(8, 0.11, 0.0, 0.34), cloak, M((0, 0.14, 1.03), rot=(-55, 0, 0)), smooth=True, group="Head"),
        part("Mask", kit.sphere(12, 8), mask, M((0, -0.2, 0.83), scale=(0.17, 0.06, 0.21)), smooth=True, group="Head"),
        part("Mark", kit.cube(), mark, M((0, -0.262, 0.765), scale=(0.018, 0.012, 0.07)), group="Head"),
        part("OarShaft", kit.cylinder(6, 0.028, 2.1), wood, OAR @ M((0, 0, 1.05)), group="Oar"),
        part("OarBlade", kit.cube(), blade, OAR @ M((0, 0, 1.95), scale=(0.17, 0.035, 0.55)), group="Oar"),
        part("OarBand", kit.cylinder(6, 0.036, 0.05), mark, OAR @ M((0, 0, 1.62)), group="Oar"),
        part("Hand", kit.sphere(8, 6, 0.065), cloak_dark, OAR @ M((0, 0, 0.6)), smooth=True, group="Oar"),
    ]
    # The bow lantern, carried at the left hip on land (it hangs on the boat's bow when rowing). Cold, pale light.
    metal, soul = kit.mat("LanternMetal", "#2c2b31"), kit.mat("SoulLantern_Glow", "#cfe6ff")
    lx, ly, lz = -0.36, -0.06, 0.38
    parts += [
        part("LanternCord", kit.cube(), metal, M((lx + 0.02, ly, lz + 0.14), rot=(0, 18, 0), scale=(0.015, 0.015, 0.12)), group="Body"),
        part("LanternTop", kit.cone(4, 0.07, 0.01, 0.06), metal, M((lx, ly, lz + 0.08), rot=(0, 0, 45)), group="Body"),
        part("LanternGlass", kit.cube(), soul, M((lx, ly, lz), scale=(0.08, 0.08, 0.11)), group="Body"),
        part("LanternBase", kit.cube(), metal, M((lx, ly, lz - 0.065), scale=(0.1, 0.1, 0.02)), group="Body"),
    ]
    for s in (-1, 1):
        parts.append(part("Eye", kit.sphere(8, 6), ink, M((s * 0.065, -0.255, 0.87), scale=(0.032, 0.02, 0.046)), smooth=True, group="Head"))
        parts.append(part("Foot", kit.sphere(8, 6), ink, M((s * 0.11, -0.17, 0.035), scale=(0.075, 0.09, 0.045)), smooth=True, group="Root"))

    mesh = kit.join(parts, "Apprentice")
    armature = kit.rig("ApprenticeRig", BONES)
    kit.skin(mesh, armature)
    kit.animate(armature, ACTIONS)
    return [armature, mesh]
