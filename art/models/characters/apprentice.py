"""The Ferryman's Apprentice: small, cloaked, bone-white mask, oversized oar (blade up). Rigged and animated.

Clip timings match the Middle combat tuning (the controller rescales them if tuning changes):
light 0.13 s windup / 0.10 s active / 0.28 s recovery; heavy 0.5 / 0.12 / 0.5; dash 0.38 s; heal 0.7 s.
RecoilHeavy / RecoilLight: bouncing off a Laden's heap (0.35 s). HookAim (loop), HookThrow (0.15 s), HookPull, Pickup
(0.6 s), RaiseItem (2.4 s: a key item held up). VaultPlant (0.12 s), Vault (0.55 s), VaultLand (0.2 s): the Vault.
"""
import math

from mathutils import Vector

import oars

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
    "Hook": ((0.22, 0.16, 0.5), (0.22, 0.16, 0.7), "Body"),  # the Hook, hung at the back of the hip (hidden until found)
    # The bow lantern at the left hip: its own bone, so the game can hide it while it hangs on the boat's bow.
    "Lantern": ((-0.36, -0.06, 0.52), (-0.36, -0.06, 0.62), "Body"),
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
# The Stroke (with the Drowned Moon in the blade): drawn back low round the oar's side, crouched, the blade turned
# down to water that isn't there (StrokeDraw, held: a strain in it); let go, it sweeps low across the front like a
# paddle pulled through the water, and the wave leaves the blade as it passes the front (frame 6 of 18).
def low(phi, pitch, step=0.0, lift=-0.12, lean=14.0, crouch=0.08):
    pose = swing(phi, pitch, step, lift)
    pose["Body"] = (lean, 0, pose["Body"][2])
    pose["Root"] = ((0, 0, 0), (0, -step, -crouch))
    return pose


STROKE_DRAWN = low(150, 104)
STROKE_STRAIN = low(156, 108, lean=17.0, crouch=0.1)
# The Hook's whirl while aiming: round a small circle above the shoulder, turning with it, one turn per 16 frames.
HOOK_WHIRL_CENTRE = (-0.06, -0.2, 0.78)  # offset from its rest at the hip: up above the shoulder, a little ahead
HOOK_AIM_BODY = {"Body": (-5, 0, 0), "Head": (2, 0, 0), "Oar": (-28, 0, 4)}
HOOK_WHIRL = [
    (1 + 4 * k, dict(HOOK_AIM_BODY, Hook=((20, 0, 90 * k),
                                          (HOOK_WHIRL_CENTRE[0] + 0.22 * math.cos(math.radians(90 * k)),
                                           HOOK_WHIRL_CENTRE[1] + 0.22 * math.sin(math.radians(90 * k)),
                                           HOOK_WHIRL_CENTRE[2]))))
    for k in range(5)
]
OAR_RAISED = {"Pivot": (-5, 0, 0), "Body": (-10, 0, 0), "Head": (-24, 0, 0), "Oar": ((-6, -8, 0), (-0.19, 0.02, 0.94)),
              "Root": ((0, 0, 0), (0, 0, 0.03))}
RAISED = {"Pivot": (-4, 0, 0), "Body": (-8, 0, 0), "Head": (-20, 0, 0), "Oar": (12, 0, -24),
          "Hook": ((0, 0, 0), (-0.22, -0.2, 0.86)), "Root": ((0, 0, 0), (0, 0, 0.02))}

# Rowing (seated in the boat, facing the bow): a paddle stroke one side, the oar swung over, then the other side. The
# Apprentice sits low (the boat's rim hides the hem). Catch: the blade dipped in ahead; pull: drawn back along the side;
# lift: out of the water, and over. 48 frames: two strokes (the game sets the pace from how hard it's rowed).
SIT = (0, 0, -0.16)
ROW_R_CATCH = {"Body": (12, 0, -14), "Head": (-8, 0, 10), "Oar": ((25, 145, 0), (-0.05, -0.22, 0.1)), "Root": ((0, 0, 0), SIT)}
ROW_R_PULL = {"Body": (-4, 0, 12), "Head": (2, 0, -8), "Oar": ((-30, 145, 0), (-0.05, 0.2, 0.05)), "Root": ((0, 0, 0), SIT)}
ROW_R_LIFT = {"Body": (0, 0, 8), "Head": (0, 0, -4), "Oar": ((20, 110, 0), (-0.12, 0.1, 0.2)), "Root": ((0, 0, 0), SIT)}
ROW_OVER_L = {"Body": (4, 0, 0), "Head": (-2, 0, 0), "Oar": ((10, 0, 0), (-0.42, -0.1, 0.28)), "Root": ((0, 0, 0), SIT)}
ROW_L_CATCH = {"Body": (12, 0, 14), "Head": (-8, 0, -10), "Oar": ((25, -145, 0), (-0.78, -0.22, 0.1)), "Root": ((0, 0, 0), SIT)}
ROW_L_PULL = {"Body": (-4, 0, -12), "Head": (2, 0, 8), "Oar": ((-30, -145, 0), (-0.78, 0.2, 0.05)), "Root": ((0, 0, 0), SIT)}
ROW_L_LIFT = {"Body": (0, 0, -8), "Head": (0, 0, 4), "Oar": ((20, -110, 0), (-0.7, 0.1, 0.2)), "Root": ((0, 0, 0), SIT)}
ROW_OVER_R = {"Body": (4, 0, 0), "Head": (-2, 0, 0), "Oar": ((10, 0, 0), (-0.36, -0.1, 0.28)), "Root": ((0, 0, 0), SIT)}
# The Stroke from the boat: the oar raised straight up overhead in both hands, blade high (its glow seen, gathering:
# RowDraw, held, a strain in it); let go, it's thrashed back over the shoulder and down into the water behind the
# boat (RowSurge: the water driven back, the boat on), then brought round to rest across the boat.
ROW_DRAW = {"Pivot": (-4, 0, 0), "Body": (-10, 0, 0), "Head": (-22, 0, 0), "Oar": ((-6, -8, 0), (-0.19, 0.02, 0.86)), "Root": ((0, 0, 0), SIT)}
ROW_DRAW_STRAIN = {"Pivot": (-5, 0, 0), "Body": (-13, 0, 3), "Head": (-24, 0, -2), "Oar": ((-10, -6, 4), (-0.19, 0.04, 0.9)), "Root": ((0, 0, 0), SIT)}
ROW_THRASH_BACK = {"Body": (-20, 0, 8), "Head": (-14, 0, -4), "Oar": ((-70, -4, 6), (-0.2, 0.18, 0.7)), "Root": ((0, 0, 0), SIT)}
ROW_THRASH_IN = {"Body": (-8, 0, 14), "Head": (4, 0, -8), "Oar": ((-125, 0, 10), (-0.24, 0.32, 0.38)), "Root": ((0, 0, 0), SIT)}
ROW_THRASH_THROUGH = {"Body": (10, 0, 10), "Head": (-4, 0, -6), "Oar": ((-140, 0, 14), (-0.26, 0.36, 0.25)), "Root": ((0, 0, 0), SIT)}
# Resting on the oars: laid across the boat, blade out to the right, the hands in the lap.
ROW_REST = {"Body": (6, 0, 0), "Head": (4, 0, 0), "Oar": ((0, 90, 0), (-0.36, -0.12, -0.08)), "Root": ((0, 0, 0), SIT)}

# The Vault's poses. The oar's foot goes ahead by tipping its top back (negative pitch), the hand brought in and up.
VAULT_PLANT = {"Pivot": (16, 0, 0), "Body": (6, 0, 0), "Head": (-10, 0, 0), "Oar": ((-38, 0, -8), (-0.22, -0.16, 0.22)),
               "Root": ((0, 0, 0), (0, 0, -0.1))}
VAULT_RISE = {"Pivot": (26, 0, 0), "Body": (4, 0, 0), "Head": (-18, 0, 0), "Oar": ((-12, 0, -6), (-0.24, -0.1, 0.42)),
              "Root": ((0, 0, 0), (0, 0, 0.04))}
VAULT_OVER = {"Pivot": (40, 0, 0), "Body": (10, 0, 0), "Head": (-30, 0, 0), "Oar": ((-95, 0, -8), (-0.2, 0.12, 0.3)),
              "Root": ((0, 0, 0), (0, 0, 0.08))}
VAULT_BEYOND = {"Pivot": (18, 0, 0), "Body": (4, 0, 0), "Head": (-14, 0, 0), "Oar": ((-80, 0, -6), (-0.1, 0.1, 0.12)),
                "Root": ((0, 0, 0), (0, 0, 0.04))}
VAULT_DOWN = {"Pivot": (4, 0, 0), "Head": (-4, 0, 0), "Oar": ((-30, 0, 0), (0, 0, 0.04))}
VAULT_CROUCH = {"Pivot": (14, 0, 0), "Body": (6, 0, 0), "Head": (-10, 0, 0), "Oar": ((-20, 0, 0), (0, -0.04, -0.02)),
                "Root": ((0, 0, 0), (0, 0, -0.12))}


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
    "StrokeDraw": ("BEZIER", [(1, STROKE_DRAWN), (12, STROKE_STRAIN), (24, STROKE_DRAWN)]),
    "Stroke": ("BEZIER", [
        (1, STROKE_STRAIN),
        (4, low(80, 102, 0.05)),
        (6, low(0, 100, 0.1, lean=10.0)),
        (8, low(-70, 98, 0.12, lift=-0.06, lean=8.0)),
        (10, low(-110, 92, 0.12, lift=0.0, lean=6.0, crouch=0.05)),
        (18, REST),
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
    # The Hook. Aiming: the Hook whirled on its chain above the shoulder, the oar held back (a loop; the game
    # hides the Hook until it's found). Throw (0.15 s): flung forward. Pull: leaning into the chain as it drags the
    # Apprentice across, oar trailing, feet just off the ground (held for the pull).
    "HookAim": ("BEZIER", HOOK_WHIRL),
    "HookThrow": ("LINEAR", [
        (1, dict(HOOK_AIM_BODY, Hook=((0, 0, 0), HOOK_WHIRL_CENTRE))),
        (4, {"Body": (14, 0, 0), "Head": (-6, 0, 0), "Oar": (-30, 0, 0), "Hook": ((80, 0, 0), (-0.1, -0.6, 0.35)),
             "Root": ((0, 0, 0), (0, -0.04, 0))}),
    ]),
    "HookPull": ("BEZIER", [
        (1, {"Pivot": (28, 0, 0), "Head": (-16, 0, 0), "Oar": (-55, 0, -12), "Hook": ((80, 0, 0), (-0.1, -0.55, 0.4)),
             "Root": ((0, 0, 0), (0, 0, 0.08))}),
        (8, {"Pivot": (32, 0, 0), "Head": (-18, 0, 0), "Oar": (-60, 0, -14), "Hook": ((82, 0, 0), (-0.1, -0.58, 0.42)),
             "Root": ((0, 0, 0), (0, 0, 0.1))}),
    ]),
    # Picking something up (0.6 s): a quick bow down to it and back up; the game takes the item at the bottom.
    "Pickup": ("BEZIER", [
        (1, REST),
        (5, {"Pivot": (32, 0, 0), "Head": (16, 0, 0), "Oar": (-12, 0, 0), "Root": ((0, 0, 0), (0, -0.04, -0.09))}),
        (8, {"Pivot": (30, 0, 0), "Head": (14, 0, 0), "Oar": (-12, 0, 0), "Root": ((0, 0, 0), (0, -0.04, -0.08))}),
        (14, REST),
    ]),
    # A key item (an Ability) held up (2.4 s): a crouch, then the Hook lifted high overhead in both hands, the head
    # tipped back to look up at it, the oar planted aside; held a moment, then lowered.
    "RaiseItem": ("BEZIER", [
        (1, REST),
        (8, {"Pivot": (10, 0, 0), "Head": (8, 0, 0), "Oar": (8, 0, -20), "Root": ((0, 0, 0), (0, 0, -0.07))}),
        (15, RAISED),
        (30, dict(RAISED, Hook=((0, 0, 8), (-0.22, -0.2, 0.9)))),
        (46, RAISED),
        (58, REST),
    ]),
    # A Pitch Pot tossed (0.4 s): a wind back, the body swung forward and round as it lets go (at ~45%), the oar
    # held back out of the way, then settled.
    "Toss": ("BEZIER", [
        (1, REST),
        (3, {"Pivot": (-6, 0, 0), "Body": (-14, 0, -24), "Head": (6, 0, 16), "Oar": (-10, 0, 10), "Root": ((0, 0, 0), (0, 0.04, -0.02))}),
        (5, {"Pivot": (10, 0, 0), "Body": (18, 0, 20), "Head": (-10, 0, -14), "Oar": (-14, 0, 14), "Root": ((0, 0, 0), (0, -0.06, -0.02))}),
        (7, {"Pivot": (8, 0, 0), "Body": (12, 0, 12), "Head": (-6, 0, -8), "Oar": (-10, 0, 8), "Root": ((0, 0, 0), (0, -0.04, -0.01))}),
        (10, REST),
    ]),
    # The oar strengthened (2.4 s): a crouch, then the oar hoisted straight up high beside the head, blade to the sky,
    # the head tipped back to look up at it; a small pump at the top (its new look arrives there), then lowered.
    "RaiseOar": ("BEZIER", [
        (1, REST),
        (8, {"Pivot": (10, 0, 0), "Head": (8, 0, 0), "Oar": ((10, 0, -10), (-0.1, 0.0, -0.08)), "Root": ((0, 0, 0), (0, 0, -0.08))}),
        (15, OAR_RAISED),
        (22, dict(OAR_RAISED, Oar=((-6, -8, 0), (-0.19, 0.02, 1.03)))),
        (30, OAR_RAISED),
        (46, OAR_RAISED),
        (58, REST),
    ]),
    "Heal": ("BEZIER", [  # bows its head to the mask, a small glow of relief (0.7 s)
        (1, REST),
        (8, {"Body": (14, 0, 0), "Head": (22, 0, 0), "Oar": (-8, 0, 0), "Root": ((0, 0, 0), (0, 0, -0.04))}),
        (16, {"Body": (12, 0, 0), "Head": (18, 0, 0), "Oar": (-8, 0, 0), "Root": ((0, 0, 0), (0, 0, -0.04))}),
        (22, REST),
    ]),
    # Death (2.2 s): reeling back from the blow, down onto the knees, the oar slipping from the hand; kneeling with
    # the head bowed while the bow lantern gutters out; then slumping forward to lie still.
    "Death": ("BEZIER", [
        (1, REST),
        (4, {"Pivot": (-24, 0, 8), "Head": (-20, 0, 0), "Oar": (-20, 0, 40), "Root": ((0, 0, 0), (0, 0.1, 0.02))}),
        (12, {"Pivot": (20, 0, 0), "Body": (6, 0, 0), "Head": (18, 0, 0), "Oar": (30, 0, 55), "Root": ((0, 0, 0), (0, 0.05, -0.2))}),
        (17, {"Pivot": (22, 0, 2), "Body": (6, 0, 0), "Head": (26, 0, 0), "Oar": ((80, 0, 75), (0.08, -0.05, -0.22)), "Root": ((0, 0, 0), (0, 0.05, -0.21))}),
        (28, {"Pivot": (22, 0, 4), "Body": (8, 0, 0), "Head": (34, 0, 0), "Oar": ((78, 0, 74), (0.08, -0.05, -0.23)), "Root": ((0, 0, 0), (0, 0.05, -0.21))}),
        (46, {"Pivot": (26, 0, -3), "Body": (8, 0, 0), "Head": (38, 0, 0), "Oar": ((76, 0, 76), (0.08, -0.06, -0.24)), "Root": ((0, 0, 0), (0, 0.05, -0.22))}),
        (55, {"Pivot": (62, 0, 10), "Head": (28, 0, 0), "Oar": ((78, 0, 76), (0.08, -0.06, -0.24)), "Root": ((0, 0, 0), (0, -0.1, -0.24))}),
        (61, {"Pivot": (84, 0, 12), "Head": (22, 0, 0), "Oar": ((78, 0, 76), (0.08, -0.06, -0.24)), "Root": ((0, 0, 0), (0, -0.24, -0.26))}),
        (66, {"Pivot": (82, 0, 12), "Head": (24, 0, 0), "Oar": ((78, 0, 76), (0.08, -0.06, -0.24)), "Root": ((0, 0, 0), (0, -0.24, -0.26))}),
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
    "Row": ("BEZIER", [
        (1, ROW_R_CATCH), (9, ROW_R_PULL), (14, ROW_R_LIFT), (19, ROW_OVER_L),
        (25, ROW_L_CATCH), (33, ROW_L_PULL), (38, ROW_L_LIFT), (43, ROW_OVER_R), (49, ROW_R_CATCH),
    ]),
    "RowDraw": ("BEZIER", [(1, ROW_DRAW), (12, ROW_DRAW_STRAIN), (24, ROW_DRAW)]),
    "RowSurge": ("BEZIER", [(1, ROW_DRAW_STRAIN), (3, ROW_THRASH_BACK), (5, ROW_THRASH_IN), (9, ROW_THRASH_THROUGH), (13, ROW_THRASH_THROUGH), (21, ROW_REST)]),
    "RowIdle": ("BEZIER", [
        (1, ROW_REST),
        (30, dict(ROW_REST, Body=(8, 0, 0), Root=((0, 0, 0), (0, 0, -0.17)))),
        (60, ROW_REST),
    ]),
    "Hit": ("BEZIER", [
        (1, REST),
        (3, {"Body": (-18, 0, 0), "Head": (-12, 0, 0), "Oar": (-10, 0, 0), "Root": ((0, 0, 0), (0, 0.08, 0))}),
        (10, REST),
    ]),
    # The Vault: the oar's foot planted ahead, crouched (VaultPlant, 0.12 s); up and over it, tipped forward, the oar
    # swinging back behind as it's let go of below (Vault, 0.55 s: the top at a third); down, a dip (VaultLand, 0.2 s,
    # blended straight into whatever comes next: there's no hold).
    "VaultPlant": ("BEZIER", [(1, REST), (4, VAULT_PLANT)]),
    "Vault": ("BEZIER", [(1, VAULT_PLANT), (4, VAULT_RISE), (7, VAULT_OVER), (12, VAULT_BEYOND), (17, VAULT_DOWN)]),
    "VaultLand": ("BEZIER", [(1, VAULT_DOWN), (3, VAULT_CROUCH), (7, REST)]),
}


def build():
    cloak, cloak_dark = kit.mat("Cloak", "#9c3b2b"), kit.mat("CloakDark", "#74291f")
    mask, ink, mark = kit.mat("Mask", "#ece4d0"), kit.mat("Ink", "#1a1622"), kit.mat("MaskMark", "#b8352b")
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
        part("Hand", kit.sphere(8, 6, 0.065), cloak_dark, OAR @ M((0, 0, 0.6)), smooth=True, group="Oar"),
    ]
    # The oar at each of its five steps, all carried; the game shows the one it's at (tools/blender/oars.py, OarLook.cs).
    for step in range(5):
        parts += oars.build(step, OAR, group="Oar")
    # The bow lantern, carried at the left hip on land (it hangs on the boat's bow when rowing). Cold, pale light.
    metal, soul = kit.mat("LanternMetal", "#2c2b31"), kit.mat("SoulLantern_Glow", "#cfe6ff")
    lx, ly, lz = -0.36, -0.06, 0.38
    parts += [
        part("LanternCord", kit.cube(), metal, M((lx + 0.02, ly, lz + 0.14), rot=(0, 18, 0), scale=(0.015, 0.015, 0.12)), group="Lantern"),
        part("LanternTop", kit.cone(4, 0.07, 0.01, 0.06), metal, M((lx, ly, lz + 0.08), rot=(0, 0, 45)), group="Lantern"),
        part("LanternGlass", kit.cube(), soul, M((lx, ly, lz), scale=(0.08, 0.08, 0.11)), group="Lantern"),
        part("LanternBase", kit.cube(), metal, M((lx, ly, lz - 0.065), scale=(0.1, 0.1, 0.02)), group="Lantern"),
    ]
    # The Hook (the Ferryman's, taken from his relic): a small iron grappling hook with a few links of chain, hung at the
    # back of the hip. Same dark iron as the relic and the hook posts' rings.
    iron = kit.mat("HookIron", "#3b3a42")
    hx, hy, hz, k = 0.22, 0.16, 0.5, 1.5  # k: sized up to read from the game's camera

    def at(dx, dy, dz):
        return (hx + dx * k, hy + dy * k, hz + dz * k)

    parts += [
        part("HookShank", kit.cylinder(5, 0.024 * k, 0.24 * k), iron, M(at(0, 0, 0.08)), group="Hook"),
        part("HookProngL", kit.tube([at(0, 0, -0.04), at(-0.07, 0, -0.06), at(-0.1, 0, 0.0)], [0.022 * k, 0.017 * k, 0.01 * k], seg=5), iron, group="Hook"),
        part("HookProngR", kit.tube([at(0, 0, -0.04), at(0.07, 0, -0.06), at(0.1, 0, 0.0)], [0.022 * k, 0.017 * k, 0.01 * k], seg=5), iron, group="Hook"),
        part("HookProngB", kit.tube([at(0, 0, -0.04), at(0, 0.07, -0.06), at(0, 0.1, 0.0)], [0.022 * k, 0.017 * k, 0.01 * k], seg=5), iron, group="Hook"),
        part("HookEye", kit.cylinder(6, 0.032 * k, 0.02 * k), iron, M(at(0, 0, 0.21), rot=(90, 0, 0)), group="Hook"),
    ]
    for i in range(3):
        parts.append(part("HookLink", kit.cylinder(5, 0.024 * k, 0.013 * k), iron, M(at(0, 0, 0.25 + 0.04 * i), rot=(90 * (i % 2), 0, 90 * (i % 2))), group="Hook"))
    for s in (-1, 1):
        parts.append(part("Eye", kit.sphere(8, 6), ink, M((s * 0.065, -0.255, 0.87), scale=(0.032, 0.02, 0.046)), smooth=True, group="Head"))
        parts.append(part("Foot", kit.sphere(8, 6), ink, M((s * 0.11, -0.17, 0.035), scale=(0.075, 0.09, 0.045)), smooth=True, group="Root"))

    mesh = kit.join(parts, "Apprentice")
    armature = kit.rig("ApprenticeRig", BONES)
    kit.skin(mesh, armature)
    kit.animate(armature, ACTIONS)
    return [armature, mesh]
