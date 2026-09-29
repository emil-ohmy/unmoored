"""A hook post: taller than any other post in the world (so it reads as its own thing), a weathered pile at a
platform's edge with an oversized iron ring on top and the Ferryman's mark inlaid in bone. From the ring a snapped
rope hangs down over the edge into the water: someone used to cross here, and the line broke. The ring is the same
dark iron as the Hook itself. Front (-Y) faces the gap it crosses; origin at its base."""
import math

M, part = kit.M, kit.part  # `kit` is injected by build_model.py

RING_Z, RING_R = 2.42, 0.3


def build():
    wood, iron = kit.mat("PileWood", "#4f3f33"), kit.mat("HookIron", "#3b3a42")
    rope, bone = kit.mat("Rope", "#8f7d5e"), kit.mat("MarkBone", "#c9bfa6")
    parts = [
        part("Pile", kit.cylinder(7, 0.17, 2.2), wood, M((0, 0, 1.1), rot=(-3, 2, 0)), jitter=0.012),
        part("Cap", kit.cone(7, 0.18, 0.12, 0.08), wood, M((0, 0, 2.24))),
        part("Lashing", kit.cylinder(7, 0.19, 0.14), rope, M((0, 0, 1.95))),
        part("Band", kit.cylinder(7, 0.185, 0.06), iron, M((0, 0, 0.35))),
        part("RingMount", kit.cube(), iron, M((0, -0.02, 2.22), scale=(0.1, 0.1, 0.16))),
        # The ring: big, upright, across the direction of the gap (you see it face-on from the far side).
        part("Ring", kit.tube([(RING_R * math.cos(a), 0.0, RING_Z + RING_R * math.sin(a))
                               for a in (2 * math.pi * k / 14 - math.pi / 2 for k in range(15))], [0.035] * 15, seg=5), iron),
        # The snapped line: tied round the ring's foot, sagging out over the edge and down into the water.
        part("Rope", kit.tube([(0.02, -0.04, RING_Z - RING_R), (0.05, -0.3, 1.85), (0.06, -0.62, 1.1), (0.04, -0.82, 0.35),
                               (0.02, -0.92, -0.35)], [0.028, 0.026, 0.024, 0.022, 0.02], seg=5), rope),
        part("RopeKnot", kit.sphere(6, 4, 0.05), rope, M((0.02, -0.03, RING_Z - RING_R + 0.02))),
    ]
    for dx in (-0.03, 0.035):  # frayed strands where it broke off the knot's far side
        parts.append(part("Strand", kit.tube([(0.02, 0.02, RING_Z - RING_R), (0.02 + dx, 0.12, RING_Z - RING_R - 0.12)], [0.008, 0.004], seg=3), rope))
    parts += kit.ferryman_mark(bone, M((0, -0.175, 1.45)))
    return [kit.join(parts, "HookPost")]
