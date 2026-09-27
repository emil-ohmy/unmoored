"""Rowing boat lofted from cross-sections: square stern, pointed bow along +Y. Keel near z=0."""
import bmesh

M, part = kit.M, kit.part  # `kit` is injected by build_model.py

TS = [0, 0.15, 0.35, 0.55, 0.75, 0.9, 1.0]
HALF_WIDTH = [0.5, 0.6, 0.63, 0.6, 0.48, 0.27, 0.03]
DEPTH = [0.34, 0.4, 0.42, 0.42, 0.39, 0.34, 0.3]
RIM = [0.42, 0.4, 0.39, 0.4, 0.43, 0.5, 0.6]


def build():
    hull, seat = kit.mat("Hull", "#4f7186"), kit.mat("Seat", "#7d6149")

    def shell(bm):
        sections = []
        for t, w, d, r in zip(TS, HALF_WIDTH, DEPTH, RIM):
            y = -1.3 + t * 2.7
            profile = [(-w, r), (-w * 0.82, r - d * 0.6), (0, r - d), (w * 0.82, r - d * 0.6), (w, r)]
            sections.append([bm.verts.new((x, y, z)) for x, z in profile])
        for a, b in zip(sections, sections[1:]):
            for k in range(4):
                bm.faces.new((a[k], a[k + 1], b[k + 1], b[k]))
        bm.faces.new(sections[0])  # transom
        bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
        bm.normal_update()  # solidify offsets along vertex normals, which are zero until computed
        bmesh.ops.solidify(bm, geom=bm.faces[:], thickness=0.045)
        bmesh.ops.recalc_face_normals(bm, faces=bm.faces)

    parts = [
        part("Hull", shell, hull),
        part("SeatAft", kit.cube(), seat, M((0, -0.45, 0.26), scale=(1.1, 0.24, 0.045))),
        part("SeatFwd", kit.cube(), seat, M((0, 0.45, 0.26), scale=(1.05, 0.22, 0.045))),
    ]
    return [kit.join(parts, "Boat")]
