"""A Moth: a Soured that couldn't let go of a memory. Big, as moths go (half a metre across the wings), pale and
dusty, the wings marked with faded eye-spots and softly glowing along their edges; the same sickly green eyes as
every Soured. They come in flocks, and they come to light. Its origin is its body's centre (it flies: the game holds
it up).

Clips: Fly (a quick flutter, looping), Glide (wings held out, drifting), Stall (0.5 s: the tell, wings flung wide and
held, flaring), Dive (wings swept back, a dart), Stagger, Death (the wings fold and it crumples).
"""
import math

M, part = kit.M, kit.part  # `kit` is injected by build_model.py

BONES = {
    "Root": ((0, 0, -0.05), (0, 0, 0.05), None),
    "Body": ((0, 0.08, 0), (0, -0.1, 0), "Root"),
    "WingL": ((-0.03, 0, 0.02), (-0.28, 0, 0.02), "Body"),
    "WingR": ((0.03, 0, 0.02), (0.28, 0, 0.02), "Body"),
}

REST = {}
# Wings: +Y on WingR (and -Y on WingL) lifts it; Z sweeps it back. Body: +X pitches the nose down.
UP = {"WingL": (0, -55, 0), "WingR": (0, 55, 0)}
DOWN = {"WingL": (0, 35, 0), "WingR": (0, -35, 0)}
SPREAD = {"WingL": (0, -10, -10), "WingR": (0, 10, 10), "Body": (-20, 0, 0)}
SWEPT = {"WingL": (0, -15, 55), "WingR": (0, 15, -55), "Body": (25, 0, 0)}
FOLDED = {"WingL": (0, -80, 30), "WingR": (0, 80, -30), "Body": (60, 0, 0)}

ACTIONS = {
    "Fly": ("BEZIER", [(1, UP), (3, DOWN), (5, UP)]),
    "Glide": ("BEZIER", [(1, {"WingL": (0, -8, 0), "WingR": (0, 8, 0)}), (12, {"WingL": (0, -14, 0), "WingR": (0, 14, 0)}),
                         (24, {"WingL": (0, -8, 0), "WingR": (0, 8, 0)})]),
    "Stall": ("BEZIER", [(1, UP), (4, SPREAD), (12, dict(SPREAD, Body=(-28, 0, 0)))]),
    "Dive": ("BEZIER", [(1, SPREAD), (3, SWEPT), (9, SWEPT)]),
    "Stagger": ("BEZIER", [(1, REST), (3, {"Body": (0, 40, 0), "WingL": (0, -60, 0), "WingR": (0, 20, 0)}), (9, REST)]),
    "Death": ("BEZIER", [(1, REST), (4, dict(UP, Body=(0, 30, 0))), (14, FOLDED)]),
}


def build():
    body, dust = kit.mat("MothBody", "#6a5e52"), kit.mat("MothWing", "#cbbf9f")
    spot, edge = kit.mat("MothSpot", "#5a4a3a"), kit.mat("MothEdge_Glow", "#e8dcb8")
    eye, fur = kit.mat("SouredEye_Glow", "#b6f06a"), kit.mat("MothFur", "#9a8c74")
    parts = [
        part("Thorax", kit.sphere(8, 6, 0.06), fur, M((0, 0.02, 0), scale=(1, 1.1, 0.9)), smooth=True, group="Body"),
        part("Abdomen", kit.cone(8, 0.05, 0.015, 0.18), body, M((0, 0.13, -0.01), rot=(90, 0, 0)), smooth=True, group="Body"),
        part("Head", kit.sphere(8, 6, 0.035), fur, M((0, -0.06, 0.005)), smooth=True, group="Body"),
    ]
    for s, bone in ((-1, "WingL"), (1, "WingR")):
        parts.append(part("Eye", kit.sphere(6, 4, 0.014), eye, M((s * 0.022, -0.09, 0.015)), smooth=True, group="Body"))
        parts.append(part("Antenna", kit.tube([(s * 0.01, -0.08, 0.03), (s * 0.05, -0.15, 0.08), (s * 0.08, -0.17, 0.07)], [0.004] * 3, seg=3), body, group="Body"))
        # Fore and hind wings: flat fans out to the side, eye-spots, a glowing edge.
        fore = [(s * 0.03, -0.035), (s * 0.17, -0.1), (s * 0.29, -0.11), (s * 0.33, -0.02), (s * 0.27, 0.06), (s * 0.12, 0.07), (s * 0.04, 0.04)]
        hind = [(s * 0.03, 0.04), (s * 0.16, 0.06), (s * 0.25, 0.13), (s * 0.22, 0.22), (s * 0.12, 0.25), (s * 0.04, 0.15)]
        for name, outline in (("Forewing", fore), ("Hindwing", hind)):
            pts = outline if s > 0 else list(reversed(outline))
            parts.append(part(name, kit.prism([(x, y) for x, y in pts], 0.006), dust, M((0, 0, 0.018)), group=bone))
            loop = [(x, y, 0.024) for x, y in outline] + [(outline[0][0], outline[0][1], 0.024)]
            parts.append(part(name + "Edge", kit.tube(loop[1:-1], [0.005] * (len(loop) - 2), seg=3), edge, group=bone))
        parts.append(part("EyeSpot", kit.cylinder(8, 0.04, 0.004), spot, M((s * 0.2, -0.03, 0.026)), group=bone))
        parts.append(part("EyeSpot", kit.cylinder(8, 0.03, 0.004), spot, M((s * 0.15, 0.15, 0.026)), group=bone))
    mesh = kit.join(parts, "Moth")
    armature = kit.rig("MothRig", BONES)
    kit.skin(mesh, armature)
    kit.animate(armature, ACTIONS)
    return [armature, mesh]
