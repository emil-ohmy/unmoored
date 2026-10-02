"""A Thresher (the Orchard): one of the orchard's old threshing machines, iron and oak on iron wheels, a reel of flails
at the front under an iron cowl, a boiler amidships with its stack, and in the driver's seat at the back, the man who
drove it, grown into its levers: a hunched, hooded Soured, its eyes green. Like every Soured, a green lantern hung
from its frame. About 2.6 m long. The reel spins in code (faster as it revs and charges), not in the clips.

Clips: Idle (shuddering, the driver bobbing), Rev (1.1 s: rocking back, shaking: the tell before a charge), Charge
(pitched forward, the driver hunched over the levers, shaking: looped), Crash (0.6 s: a jolt up and back, the driver
thrown forward over the levers), Stalled (slumped, steaming: looped), SweepWindup / Sweep (the front swung aside and
back across: the flails), Vent (a buck: steam blown from the stack), Stagger, Death (it lists over, the driver
slumping off the seat).
"""
import math

M, part = kit.M, kit.part  # `kit` is injected by build_model.py

REEL_Y, REEL_Z = -1.42, 0.72
BONES = {
    "Root": ((0, 0, 0), (0, 0, 0.4), None),
    "Body": ((0, 0.9, 0.7), (0, -0.9, 0.7), "Root"),
    "Reel": ((-0.72, REEL_Y, REEL_Z), (0.72, REEL_Y, REEL_Z), "Body"),
    "Driver": ((0, 0.95, 1.12), (0, 0.85, 1.6), "Body"),
    "Head": ((0, 0.85, 1.6), (0, 0.72, 1.86), "Driver"),
    "ArmL": ((-0.2, 0.85, 1.52), (-0.22, 0.6, 1.3), "Driver"),
    "ArmR": ((0.2, 0.85, 1.52), (0.22, 0.6, 1.3), "Driver"),
}

REST = {}
SHUDDER_A = {"Body": (0.6, 0.4, 0), "Driver": (4, 0, 0), "Root": ((0, 0, 0), (0, 0, 0.01))}
SHUDDER_B = {"Body": (-0.6, -0.4, 0), "Driver": (6, 0, 0)}
REVVED_A = {"Body": (-4, 1.2, 0), "Driver": (-6, 0, 0), "Head": (-8, 0, 0), "Root": ((0, 0, 0), (0, 0.05, 0.02))}
REVVED_B = {"Body": (-3, -1.2, 0), "Driver": (-4, 0, 0), "Head": (-6, 0, 0), "Root": ((0, 0, 0), (0, 0.06, 0.0))}
CHARGING_A = {"Body": (4, 1, 0), "Driver": (22, 0, 0), "Head": (10, 0, 0), "ArmL": (30, 0, 0), "ArmR": (30, 0, 0), "Root": ((0, 0, 0), (0, 0, 0.02))}
CHARGING_B = {"Body": (3, -1, 0), "Driver": (24, 0, 0), "Head": (12, 0, 0), "ArmL": (34, 0, 0), "ArmR": (26, 0, 0)}
SLUMPED = {"Driver": (40, 0, 8), "Head": (35, 0, 0), "ArmL": (20, 30, 0), "ArmR": (25, -35, 0), "Body": (2, 2, 0)}
SWUNG = {"Body": (0, 0, 28), "Driver": (0, 0, -10)}
SWEPT = {"Body": (0, 0, -32), "Driver": (8, 0, 12)}

ACTIONS = {
    "Idle": ("BEZIER", [(1, SHUDDER_A), (3, SHUDDER_B), (5, SHUDDER_A), (7, SHUDDER_B), (9, SHUDDER_A)]),
    "Rev": ("BEZIER", [(1, REST), (5, REVVED_A), (8, REVVED_B), (11, REVVED_A), (14, REVVED_B), (17, REVVED_A), (20, REVVED_B), (23, REVVED_A), (27, REVVED_B)]),
    "Charge": ("BEZIER", [(1, CHARGING_A), (3, CHARGING_B), (5, CHARGING_A)]),
    "Crash": ("BEZIER", [(1, CHARGING_A), (3, {"Body": (-10, 3, 0), "Driver": (55, 0, 0), "Head": (30, 0, 0), "Root": ((0, 0, 0), (0, 0.25, 0.15))}),
                         (8, dict(SLUMPED, Body=(3, 3, 0))), (15, SLUMPED)]),
    "Stalled": ("BEZIER", [(1, SLUMPED), (20, dict(SLUMPED, Driver=(42, 0, 6), Body=(1, 2, 0))), (40, SLUMPED)]),
    "SweepWindup": ("BEZIER", [(1, REST), (12, SWUNG), (15, dict(SWUNG, Body=(0, 0, 32)))]),
    "Sweep": ("BEZIER", [(1, SWUNG), (4, SWEPT), (8, dict(SWEPT, Body=(0, 0, -36)))]),
    "Recover": ("BEZIER", [(1, SWEPT), (18, REST)]),
    "Vent": ("BEZIER", [(1, REST), (3, {"Body": (-3, 0, 0), "Root": ((0, 0, 0), (0, 0, 0.18))}), (8, {"Body": (1, 0, 0)}), (12, REST)]),
    "Stagger": ("BEZIER", [(1, REST), (4, {"Body": (-4, 2, 0), "Driver": (-15, 0, 0), "Head": (-15, 0, 0)}), (14, REST)]),
    "Death": ("BEZIER", [(1, REST), (8, {"Body": (0, 10, 0), "Driver": (30, 20, 0), "Head": (30, 0, 0)}),
                        (22, {"Body": (0, 16, 0), "Driver": (70, 45, 0), "Head": (40, 0, 0), "ArmL": (20, 60, 0), "Root": ((0, 0, 0), (0, 0, -0.1))})]),
}


def build():
    oak, dark_oak = kit.mat("ThresherOak", "#6b4a30"), kit.mat("ThresherOakDark", "#4c3524")
    iron, cowl = kit.mat("ThresherIron_Metal", "#4a4a52"), kit.mat("ThresherCowl_Metal", "#5a5560")
    boiler, rust = kit.mat("ThresherBoiler_Metal", "#3d3a3f"), kit.mat("ThresherRust", "#7a4a2c")
    firebox = kit.mat("ThresherFire_Glow", "#ff8a3a")
    robe, hood = kit.mat("ThresherDriverRobe", "#5c6058"), kit.mat("ThresherDriverHood", "#41443e")
    eye, claw, belt = kit.mat("SouredEye_Glow", "#c8f07a"), kit.mat("Claw", "#d6cdb8"), kit.mat("ThresherChain", "#2c2a2e")
    lantern_glass = kit.mat("SouredLantern_Glow", "#b6f06a")
    parts = []
    B = "Body"
    # The frame: two long oak beams and cross-members, a floor of boards.
    for s in (-1, 1):
        parts.append(part("Beam", kit.cube(), oak, M((s * 0.6, 0.0, 0.66), scale=(0.12, 2.3, 0.16)), group=B))
    for y in (-1.0, -0.1, 0.8):
        parts.append(part("Cross", kit.cube(), dark_oak, M((0, y, 0.66), scale=(1.3, 0.12, 0.12)), group=B))
    parts.append(part("Boards", kit.cube(), dark_oak, M((0, 0.45, 0.76), scale=(1.1, 1.1, 0.05)), group=B))
    # Wheels: two great iron-tyred ones at the back, two small at the front (spoked).
    def wheel(x, y, r, w):
        out = [part("Tyre", kit.cylinder(14, r, w), iron, M((x, y, r), rot=(0, 90, 0)), group=B),
               part("Hub", kit.cylinder(8, r * 0.22, w + 0.06), rust, M((x, y, r), rot=(0, 90, 0)), group=B)]
        for k in range(6):
            a = k * 30
            out.append(part("Spoke", kit.cube(), oak, M((x, y, r), rot=(a, 0, 0), scale=(w * 0.6, 0.05, r * 1.8)), group=B))
        return out
    for s in (-1, 1):
        parts += wheel(s * 0.78, 0.72, 0.62, 0.14)
        parts += wheel(s * 0.68, -0.68, 0.38, 0.1)
    # The boiler amidships, its stack, the firebox glowing.
    parts += [
        part("Boiler", kit.cylinder(12, 0.38, 0.9), boiler, M((0, 0.0, 1.22)), group=B),
        part("BoilerCap", kit.sphere(10, 6, 0.38), boiler, M((0, 0.0, 1.67), scale=(1, 1, 0.4)), smooth=True, group=B),
        part("Band", kit.cylinder(12, 0.4, 0.05), rust, M((0, 0.0, 1.0)), group=B),
        part("Band", kit.cylinder(12, 0.4, 0.05), rust, M((0, 0.0, 1.45)), group=B),
        part("Stack", kit.cylinder(8, 0.09, 0.8), iron, M((0, -0.18, 2.05)), group=B),
        part("StackLip", kit.cylinder(8, 0.13, 0.08), iron, M((0, -0.18, 2.47)), group=B),
        part("Firebox", kit.cube(), firebox, M((0, -0.37, 0.98), scale=(0.22, 0.03, 0.14)), group=B),
    ]
    # The cowl over the reel, and the reel: an axle, two spoked ends and six toothed flail bars.
    # The cowl: an iron hood over the back of the reel (open over the front and below, where the flails come round).
    for k in range(5):
        a0 = math.radians(5 + k * 22)  # (0: straight back; 90: overhead: the front of the reel left bare, flails turning)
        y, z = REEL_Y + math.cos(a0) * 0.64 + 0.04, REEL_Z + math.sin(a0) * 0.64
        parts.append(part("Cowl", kit.cube(), cowl, M((0, y, z), rot=(math.degrees(a0) + 90, 0, 0), scale=(1.6, 0.26, 0.035)), group=B))
    parts.append(part("Axle", kit.cylinder(8, 0.05, 1.6), iron, M((0, REEL_Y, REEL_Z), rot=(0, 90, 0)), group="Reel"))
    for s in (-1, 1):
        parts.append(part("ReelEnd", kit.cylinder(10, 0.46, 0.04), rust, M((s * 0.7, REEL_Y, REEL_Z), rot=(0, 90, 0)), group="Reel"))
    for k in range(6):
        a = math.tau * k / 6
        y, z = REEL_Y + math.cos(a) * 0.44, REEL_Z + math.sin(a) * 0.44
        parts.append(part("Flail", kit.cube(), iron, M((0, y, z), rot=(math.degrees(a), 0, 0), scale=(1.42, 0.06, 0.1)), group="Reel"))
        for t in range(5):
            x = -0.56 + t * 0.28
            ty, tz = REEL_Y + math.cos(a) * 0.52, REEL_Z + math.sin(a) * 0.52
            parts.append(part("Tooth", kit.cone(4, 0.035, 0.0, 0.12), iron, kit.along((x, y, z), (x, ty, tz)), group="Reel"))
    # The drive chain: from the boiler down the right side to the reel (the Hook's to wrench off).
    chain = [(0.66, -0.05, 1.05), (0.7, -0.7, 0.95), (0.74, REEL_Y, REEL_Z)]
    parts.append(part("Chain", kit.tube(chain, [0.035, 0.035, 0.035], seg=5), belt, group=B))
    # The seat at the back, the levers, and the driver grown into them.
    parts += [
        part("Seat", kit.cube(), oak, M((0, 0.98, 1.02), scale=(0.5, 0.4, 0.06)), group=B),
        part("SeatBack", kit.cube(), oak, M((0, 1.18, 1.25), rot=(-12, 0, 0), scale=(0.5, 0.05, 0.45)), group=B),
    ]
    for s in (-1, 1):
        parts.append(part("Lever", kit.cylinder(5, 0.02, 0.7), rust, kit.along((s * 0.22, 0.55, 0.78), (s * 0.22, 0.5, 1.45)), group=B))
        parts.append(part("Knob", kit.sphere(6, 4, 0.04), oak, M((s * 0.22, 0.5, 1.46)), group=B))
    D = "Driver"
    parts += [
        part("Robe", kit.cone(8, 0.3, 0.18, 0.55), robe, M((0, 0.95, 1.33), rot=(12, 0, 0)), smooth=True, group=D),
        part("Head", kit.sphere(9, 6, 0.15), hood, M((0, 0.78, 1.72)), smooth=True, group="Head"),
        part("Hood", kit.cone(8, 0.2, 0.04, 0.36), hood, M((0, 0.84, 1.82), rot=(-35, 0, 0)), smooth=True, group="Head"),
        part("Tendril", kit.tube([(0.12, 0.9, 1.15), (0.2, 0.72, 0.95), (0.22, 0.55, 0.8)], [0.03, 0.02, 0.012], seg=5), robe, group=D),
        part("Tendril", kit.tube([(-0.12, 0.9, 1.15), (-0.2, 0.75, 0.92), (-0.22, 0.56, 0.8)], [0.03, 0.02, 0.012], seg=5), robe, group=D),
    ]
    for s in (-1, 1):
        parts.append(part("Eye", kit.sphere(7, 5, 0.03), eye, M((s * 0.055, 0.64, 1.72)), smooth=True, group="Head"))
        bone = "ArmL" if s < 0 else "ArmR"
        parts.append(part("Sleeve", kit.cone(6, 0.07, 0.04, 0.38), robe, kit.along((s * 0.2, 0.85, 1.52), (s * 0.22, 0.52, 1.42)), smooth=True, group=bone))
        parts.append(part("Claw", kit.cone(5, 0.03, 0.0, 0.08), claw, kit.along((s * 0.22, 0.52, 1.42), (s * 0.22, 0.48, 1.47)), group=bone))
    # Like every Soured, a green lantern: hung from the frame by the seat.
    parts += [
        part("BeltLanternTop", kit.cone(4, 0.07, 0.01, 0.05), iron, M((0.62, 1.05, 1.2), rot=(0, 0, 45)), group=B),
        part("BeltLanternGlass", kit.cube(), lantern_glass, M((0.62, 1.05, 1.12), scale=(0.09, 0.09, 0.12)), group=B),
        part("BeltLanternBase", kit.cube(), iron, M((0.62, 1.05, 1.05), scale=(0.11, 0.11, 0.015)), group=B),
    ]
    mesh = kit.join(parts, "Thresher")
    armature = kit.rig("ThresherRig", BONES)
    kit.skin(mesh, armature)
    kit.animate(armature, ACTIONS)
    return [armature, mesh]
