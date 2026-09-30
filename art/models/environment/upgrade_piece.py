"""The pieces that make the Apprentice stronger for good, each set on a little stand so it reads from the camera:
"ferry_nail" (a long boat nail from the old ferries, driven into a timber offcut, its head glowing faintly: the oar is
strengthened with them), "warm_stone" (a smooth stone, still warm, on a flat rock in a ring of ash: health) and
"held_breath" (a stoppered flask with a pale breath caught inside: stamina). Origin at the base."""
M, part = kit.M, kit.part  # `kit` is injected by build_model.py

VARIANTS = ["ferry_nail", "warm_stone", "held_breath"]


def build(variant):
    rock, ash = kit.mat("PieceRock", "#5d5a58"), kit.mat("PieceAsh", "#3d3935")
    if variant == "ferry_nail":
        timber, iron = kit.mat("PileWood", "#4f3f33"), kit.mat("HookIron", "#3b3a42")
        glow = kit.mat("FerryNail_Glow", "#9fc4ff")
        parts = [
            part("Offcut", kit.cube(), timber, M((0, 0, 0.09), rot=(0, 0, 12), scale=(0.42, 0.22, 0.18)), jitter=0.01),
            part("Shank", kit.cone(4, 0.03, 0.012, 0.46), iron, M((0.02, 0, 0.36), rot=(0, 9, 0))),
            part("Head", kit.cylinder(8, 0.06, 0.03), iron, M((0.057, 0, 0.59), rot=(0, 9, 0))),
            part("Gleam", kit.cylinder(8, 0.035, 0.012), glow, M((0.06, 0, 0.607), rot=(0, 9, 0))),
        ]
    elif variant == "warm_stone":
        glow = kit.mat("WarmStone_Glow", "#ff8a4a")
        parts = [
            part("Rock", kit.cylinder(7, 0.26, 0.14), rock, M((0, 0, 0.07)), jitter=0.02),
            part("Ash", kit.cylinder(10, 0.2, 0.02), ash, M((0, 0, 0.15))),
            part("Stone", kit.sphere(10, 7, 0.12), glow, M((0, 0, 0.24), scale=(1.1, 0.9, 0.7)), smooth=True),
        ]
    else:
        glass, cork = kit.mat("PieceGlass", "#7f96a0"), kit.mat("PieceCork", "#8a6a4a")
        glow = kit.mat("HeldBreath_Glow", "#d4ecff")
        parts = [
            part("Rock", kit.cylinder(7, 0.22, 0.12), rock, M((0, 0, 0.06)), jitter=0.02),
            part("Flask", kit.sphere(10, 7, 0.13), glow, M((0, 0, 0.25), scale=(1, 1, 0.95)), smooth=True),  # (all breath)
            part("Shoulder", kit.cylinder(8, 0.07, 0.03), glass, M((0, 0, 0.365))),
            part("Neck", kit.cylinder(8, 0.045, 0.12), glass, M((0, 0, 0.41))),
            part("Cork", kit.cylinder(8, 0.05, 0.06), cork, M((0, 0, 0.49))),
        ]
    return [kit.join(parts, f"UpgradePiece_{variant}")]
