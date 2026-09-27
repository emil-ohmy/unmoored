"""Mooring post: a tall post with a rope ring and a hanging lantern. The rest point.
The lantern glass is "Lantern_Glow"; the game dims it until the post is lit."""
import bpy

M, part = kit.M, kit.part  # `kit` is injected by build_model.py


def build():
    beam, rope = kit.mat("Beam", "#46382e"), kit.mat("Rope", "#b89e6e")
    metal, glow = kit.mat("Metal", "#2c2b31"), kit.mat("Lantern_Glow", "#ff9a3c")
    parts = [
        part("Post", kit.cone(7, 0.13, 0.12, 2.6), beam, M((0, 0, 0.0))),  # 1.3 m above the origin, 1.3 below
        part("Cap", kit.cone(7, 0.16, 0.02, 0.16), metal, M((0, 0, 1.38))),
        part("Arm", kit.cube(), metal, M((0.22, 0, 1.17), scale=(0.46, 0.05, 0.05))),
        part("Hook", kit.cube(), metal, M((0.42, 0, 1.11), scale=(0.02, 0.02, 0.08))),
        part("LanternTop", kit.cone(4, 0.14, 0.02, 0.12), metal, M((0.42, 0, 1.03), rot=(0, 0, 45))),
        part("LanternGlass", kit.cube(), glow, M((0.42, 0, 0.88), scale=(0.15, 0.15, 0.2))),
        part("LanternBase", kit.cube(), metal, M((0.42, 0, 0.76), scale=(0.18, 0.18, 0.03))),
    ]
    bpy.ops.mesh.primitive_torus_add(major_radius=0.17, minor_radius=0.035, major_segments=10, minor_segments=5,
                                     location=(0, 0, 0.5))
    ring = bpy.context.active_object
    ring.data.materials.append(rope)
    parts.append(ring)
    return [kit.join(parts, "MooringPost")]
