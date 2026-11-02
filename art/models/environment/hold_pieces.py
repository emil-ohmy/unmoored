"""Set pieces for the Hold, the level (hold_drawn.py's cave; the scene builder places them where its spots say; the
arena's seal is the cave's own, hold_drawn.py builds it across the way in):

- eye_door: the door of eyes. A built wall in the living rock (dressed stone, dark and cold, the only straight lines
  in the Hold), a great doorway in it (4.2 m wide, 5 m tall, a heavy lintel, jambs of big blocks), shut by two leaves of
  the kept's pale living stone (they breathe, as the walls do), seamed down the middle, no handle, no lock. The eyes
  are the game's (hold.py's eye_* parts, many times their size, set into the leaves where EYE_DOOR_EYES says): shut till
  you come near, then they open one by one and follow you. It won't open (not yet: something for later). Origin at the
  wall's foot under the doorway's middle; its face at n = 0, out (towards you) is game north (+n): placed, the
  model's forward is out (not its back, as hold.py's eye parts).

The eyes' places on the door (x across from the middle, u up, and their scale over hold.py's eye parts), for the scene
builder: EYE_DOOR_EYES (MainSceneBuilder.HoldLayout keeps the same list)."""
import math
import os
import runpy

import numpy as np

HERE = os.path.dirname(os.path.abspath(__file__))
VARIANTS = ["eye_door"]

DOOR_W, DOOR_H = 4.2, 5.0          # the doorway
WALL_HALF, WALL_TOP = 3.4, 6.4     # the built wall round it
EYE_DOOR_EYES = [(0.0, 2.6, 3.0), (-1.2, 4.25, 1.8), (1.25, 4.3, 1.7), (-1.35, 1.0, 1.6), (1.4, 0.95, 1.75)]


def lib():
    h = runpy.run_path(os.path.join(HERE, "hold.py"), init_globals={"kit": kit}, run_name="hold_lib")
    return h


def eye_door(H):
    import masonry as m
    # The Hold's own stone: a dark, cold slate, the joints darker still (not the village's warm grey).
    m.STONE = ("HoldAshlar1", "#4d4a4f"), ("HoldAshlar2", "#555157"), ("HoldAshlar3", "#47444a")
    m.MORTAR = ("HoldAshlarJoint", "#232126")
    m.CAP = ("HoldAshlarCap", "#5d5960")
    m.PLINTH = ("HoldAshlarPlinth", "#3d3a3f")
    parts = []
    hw = DOOR_W / 2
    # The wall, the doorway left open in it (the leaves fill it).
    m.wall(parts, -WALL_HALF, WALL_HALF, -0.45, 0.0, -0.3, WALL_TOP, openings=[(-hw - 0.45, hw + 0.45, -0.3, DOOR_H + 0.75)],
           course=0.55, cap=False, plinth=True)
    stone = [m.mat(s) for s in m.STONE]
    rng = kit.rng
    # The jambs: big dressed blocks either side, standing proud of the wall.
    for sx in (-1, 1):
        u = -0.3
        k = 0
        while u < DOOR_H - 0.05:
            h = min(rng.uniform(0.75, 1.05), DOOR_H - u)
            wide = 0.45 if k % 2 else 0.6
            e0, e1 = (hw, hw + wide) if sx > 0 else (-hw - wide, -hw)
            parts.append(m.box("Jamb", rng.choice(stone), e0, e1, -0.5, 0.14, u + 0.015, u + h - 0.015))
            u += h
            k += 1
    # The lintel: one great stone over it, and the stones over that up to the wall's top.
    parts.append(m.box("Lintel", stone[1], -hw - 0.65, hw + 0.65, -0.5, 0.2, DOOR_H, DOOR_H + 0.75))
    m.wall(parts, -hw - 0.45, hw + 0.45, -0.45, 0.0, DOOR_H + 0.75, WALL_TOP, course=0.55, cap=False)
    # The leaves: the kept's living stone (it breathes), seamed down the middle, their faces swelling a little.
    MT = dict(H["materials"]())
    MT["kept"] = kit.mat("HoldDoorLeaf_Breathe", "#77707c")   # (the kept's stone, paler: the door has to read in the dark)
    mesh = H["Mesh"]()
    fbm = H["fbm"]
    for side in (-1, 1):
        xs = np.linspace(0.03, hw, 22) * side
        us = np.linspace(0.0, DOOR_H, 48)
        X, U = np.meshgrid(xs, us)
        # (out a little: a slow swell, folds like the walls', flat at the seam and the jambs)
        edge = np.minimum(np.abs(X) - 0.03, hw - np.abs(X)) / 0.25
        hold_in = np.clip(edge, 0, 1) * np.clip(U / 0.3, 0, 1) * np.clip((DOOR_H - U) / 0.3, 0, 1)
        swell = (0.06 + 0.05 * fbm(X * 0.9, U * 0.9, 3.3, 2, seed=41) + 0.03 * np.sin(U * 2.3 + X)) * hold_in
        swell = swell - 0.06 * np.exp(-((np.abs(X) - 0.03) / 0.08) ** 2)   # (the seam drawn in)
        V = np.stack([X.ravel(), (swell - 0.12).ravel(), U.ravel()], axis=1)
        rows, cols = U.shape
        idx = np.arange(rows * cols).reshape(rows, cols)
        a, b = idx[:-1, :-1].ravel(), idx[:-1, 1:].ravel()
        c, d = idx[1:, 1:].ravel(), idx[1:, :-1].ravel()
        T = np.vstack([np.stack([a, b, c], 1), np.stack([a, c, d], 1)])
        if side < 0:
            T = T[:, ::-1]
        # (pale veins wandering down them, as in the walls; dark at the seam, where they meet)
        cen = V[T].mean(axis=1)
        vein = fbm(cen[:, 0] * 1.1 + cen[:, 2] * 0.25, cen[:, 2] * 0.5, 4.1, 3, seed=43)
        seam = np.abs(cen[:, 0]) < 0.12
        mats = [MT["dark"] if sm else MT["vein"] if abs(v) < 0.045 else MT["kept"] for sm, v in zip(seam, vein)]
        mesh.patch(V, T, mats, True, (0, 1.0, 0))
        # its edges back into the wall (no gap to see through)
        BV, BT = [], []
        for x in (0.03 * side, hw * side):
            for u in us:
                BV += [(x, -0.12, u), (x, -0.5, u)]
        n = len(us)
        for s in range(2):
            o = s * 2 * n
            for j in range(n - 1):
                p = o + 2 * j
                BT += [(p, p + 2, p + 3), (p, p + 3, p + 1)]
        mesh.patch(np.array(BV), np.array(BT), MT["kept"], False, (-side, 0, 0))
    leaves = mesh.build("EyeDoor_Leaves")
    wall = kit.join(parts, "EyeDoor_Wall")
    print(f"[hold_pieces] eye_door: a doorway {DOOR_W} x {DOOR_H} m in a built wall {2 * WALL_HALF} m wide, {WALL_TOP} m "
          f"tall; {len(EYE_DOOR_EYES)} eyes in its leaves (x, u, scale): {EYE_DOOR_EYES}")
    return [wall, leaves]


def build(variant):
    H = lib()
    return eye_door(H)
