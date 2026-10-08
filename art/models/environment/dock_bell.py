"""A dock's bell: a weathered timber post on a jetty with an arm at its top, a small bronze bell hanging from it on a
short chain, its rope looped down to hand height. Ring it, and the Ferryman's boat comes in to this dock. Origin at
the post's foot; the arm reaches along -Y (Blender), the bell at its end."""
M, part = kit.M, kit.part  # `kit` is injected by build_model.py


def build():
    timber = kit.mat("BellTimber", "#5e4634")
    bronze = kit.mat("BellBronze_Metal", "#9a7440")
    rope = kit.mat("BellRope", "#8a7656")
    iron = kit.mat("BellChain_Metal", "#3a3a40")
    parts = [
        part("Post", kit.cube(), timber, M((0, 0, 0.95), scale=(0.13, 0.13, 1.9))),
        part("Arm", kit.cube(), timber, M((0, -0.32, 1.84), scale=(0.08, 0.68, 0.09))),
        part("Brace", kit.cube(), timber, M((0, -0.16, 1.66), rot=(45, 0, 0), scale=(0.06, 0.06, 0.42))),
        part("Chain", kit.cylinder(4, 0.012, 0.16), iron, M((0, -0.6, 1.72))),
        part("Bell", kit.cone(12, 0.15, 0.06, 0.24), bronze, M((0, -0.6, 1.52))),
        part("Lip", kit.cylinder(12, 0.16, 0.03), bronze, M((0, -0.6, 1.41))),
        part("Crown", kit.sphere(8, 6, 0.05), bronze, M((0, -0.6, 1.65))),
        part("Clapper", kit.sphere(6, 4, 0.035), iron, M((0, -0.6, 1.37))),
        part("Rope", kit.cylinder(5, 0.015, 0.45), rope, M((0, -0.6, 1.13))),
        part("RopeEnd", kit.sphere(6, 4, 0.03), rope, M((0, -0.6, 0.9))),
    ]
    return [kit.join(parts, "DockBell")]
