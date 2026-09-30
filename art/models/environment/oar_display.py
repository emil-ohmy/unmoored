"""The oar at each of its steps on its own ("0" to "4"), upright, its butt at the origin: shown turning on the
character screen's Oar page (a camera of its own, out of the world). The same oars the Apprentice carries
(tools/blender/oars.py)."""
import oars

M = kit.M  # `kit` is injected by build_model.py

VARIANTS = ["0", "1", "2", "3", "4"]


def build(variant):
    return [kit.join(oars.build(int(variant), M()), f"OarDisplay_{variant}")]
