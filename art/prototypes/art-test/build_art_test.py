"""PROTOTYPE (wayfinder ticket "Art style test: what Claude can build in Blender"). Throwaway.

Builds every art-test model from code, saves a .blend per model next to this script and exports FBX into
game/Assets/_Prototypes/ArtTest/Models/.

    blender -b --factory-startup --python art/prototypes/art-test/build_art_test.py -- <repo root> [model ...]

Colours are written as sRGB hex and stored linear, like Blender's colour pickers. The FBX is exported
WITHOUT bake_space_transform (the exporter flags it as broken with armatures); Unity's
"Bake Axis Conversion" does the Z-up -> Y-up conversion on import instead.
"""
import math
import os
import random
import sys

import bmesh
import bpy
from mathutils import Euler, Matrix, Quaternion, Vector

ARGS = sys.argv[sys.argv.index("--") + 1:]
ROOT = os.path.abspath(ARGS[0])
ONLY = set(ARGS[1:])
BLEND_DIR = os.path.join(ROOT, "art", "prototypes", "art-test")
FBX_DIR = os.path.join(ROOT, "game", "Assets", "_Prototypes", "ArtTest", "Models")
FPS = 30


# ---------------------------------------------------------------- helpers

def lin(hex_colour):
    h = hex_colour.lstrip("#")

    def c(x):
        x /= 255.0
        return x / 12.92 if x <= 0.04045 else ((x + 0.055) / 1.055) ** 2.4

    return (c(int(h[0:2], 16)), c(int(h[2:4], 16)), c(int(h[4:6], 16)), 1.0)


def mat(name, hex_colour):
    """Material whose name ending in _Glow makes Unity treat it as emissive."""
    m = bpy.data.materials.get(name)
    if m:
        return m
    m = bpy.data.materials.new(name)
    m.diffuse_color = lin(hex_colour)
    try:
        m.use_nodes = True
    except (AttributeError, TypeError):
        pass
    if m.node_tree:
        nodes = m.node_tree.nodes
        bsdf = next((n for n in nodes if n.type == "BSDF_PRINCIPLED"), None)
        if bsdf is None:
            bsdf = nodes.new("ShaderNodeBsdfPrincipled")
            out = next((n for n in nodes if n.type == "OUTPUT_MATERIAL"), None) or nodes.new("ShaderNodeOutputMaterial")
            m.node_tree.links.new(bsdf.outputs[0], out.inputs[0])
        bsdf.inputs["Base Color"].default_value = lin(hex_colour)
    return m


def M(loc=(0, 0, 0), rot=(0, 0, 0), scale=(1, 1, 1)):
    return (Matrix.Translation(loc)
            @ Euler([math.radians(a) for a in rot]).to_matrix().to_4x4()
            @ Matrix.Diagonal((*scale, 1.0)))


def cone(seg, r1, r2, depth):
    return lambda bm: bmesh.ops.create_cone(bm, cap_ends=True, cap_tris=False, segments=seg,
                                            radius1=r1, radius2=r2, depth=depth)


def sphere(u, v, r=1.0):
    return lambda bm: bmesh.ops.create_uvsphere(bm, u_segments=u, v_segments=v, radius=r)


def ico(sub, r=1.0):
    return lambda bm: bmesh.ops.create_icosphere(bm, subdivisions=sub, radius=r)


def cube():
    return lambda bm: bmesh.ops.create_cube(bm, size=1.0)


def tube(points, radii, seg=6):
    """Tapered tube along a polyline (tree trunks, branches)."""
    pts = [Vector(p) for p in points]

    def build(bm):
        rings = []
        for i, (p, r) in enumerate(zip(pts, radii)):
            d = (pts[min(i + 1, len(pts) - 1)] - pts[max(i - 1, 0)]).normalized()
            q = d.to_track_quat("Z", "Y")
            rings.append([bm.verts.new(p + q @ Vector((math.cos(a) * r, math.sin(a) * r, 0.0)))
                          for a in (2 * math.pi * k / seg for k in range(seg))])
        for a, b in zip(rings, rings[1:]):
            for k in range(seg):
                bm.faces.new((a[k], a[(k + 1) % seg], b[(k + 1) % seg], b[k]))
        bm.faces.new(list(reversed(rings[0])))
        bm.faces.new(rings[-1])
        bmesh.ops.recalc_face_normals(bm, faces=bm.faces)

    return build


def part(name, build, material, matrix=None, smooth=False, jitter=0.0, group=None):
    bm = bmesh.new()
    build(bm)
    if jitter:
        for v in bm.verts:
            v.co += Vector((random.uniform(-1, 1), random.uniform(-1, 1), random.uniform(-1, 1))) * jitter
    if matrix is not None:
        bmesh.ops.transform(bm, matrix=matrix, verts=bm.verts)
    me = bpy.data.meshes.new(name)
    bm.to_mesh(me)
    bm.free()
    for p in me.polygons:
        p.use_smooth = smooth
    me.materials.append(material)
    ob = bpy.data.objects.new(name, me)
    bpy.context.scene.collection.objects.link(ob)
    if group:
        ob.vertex_groups.new(name=group).add(list(range(len(me.vertices))), 1.0, "REPLACE")
    return ob


def join(objs, name):
    for o in bpy.context.scene.objects:
        o.select_set(False)
    for o in objs:
        o.select_set(True)
    bpy.context.view_layer.objects.active = objs[0]
    bpy.ops.object.join()
    ob = bpy.context.view_layer.objects.active
    ob.name = name
    ob.data.name = name
    return ob


def new_scene():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.context.scene.render.fps = FPS


def save_and_export(name, objs, animated=False):
    os.makedirs(BLEND_DIR, exist_ok=True)
    os.makedirs(FBX_DIR, exist_ok=True)
    bpy.ops.wm.save_as_mainfile(filepath=os.path.join(BLEND_DIR, f"{name}.blend"))
    for o in bpy.context.scene.objects:
        o.select_set(o in objs)
    bpy.ops.export_scene.fbx(
        filepath=os.path.join(FBX_DIR, f"{name}.fbx"),
        use_selection=True,
        object_types={"MESH", "ARMATURE", "EMPTY"},
        use_mesh_modifiers=True,
        apply_unit_scale=True,
        apply_scale_options="FBX_SCALE_ALL",
        axis_forward="-Z",
        axis_up="Y",
        bake_space_transform=False,
        mesh_smooth_type="FACE",
        add_leaf_bones=False,
        bake_anim=animated,
        bake_anim_use_all_actions=animated,
        bake_anim_use_nla_strips=False,
        bake_anim_force_startend_keying=True,
        path_mode="COPY",
        embed_textures=False,
    )
    print(f"[art-test] exported {name}.fbx")


# ---------------------------------------------------------------- models

def apprentice():
    """The Ferryman's Apprentice: small, cloaked, bone-white mask, oversized oar (blade up)."""
    random.seed(1)
    cloak, cloak_dark = mat("Cloak", "#9c3b2b"), mat("CloakDark", "#74291f")
    mask, ink, mark = mat("Mask", "#ece4d0"), mat("Ink", "#1a1622"), mat("MaskMark", "#b8352b")
    wood, blade = mat("OarWood", "#b08a5c"), mat("OarBlade", "#8f6d48")

    def cloak_body(bm):
        bmesh.ops.create_cone(bm, cap_ends=True, segments=10, radius1=0.40, radius2=0.13, depth=0.72)
        for v in bm.verts:
            if v.co.z < -0.3:  # ragged hem
                s = 1 + random.uniform(-0.06, 0.06)
                v.co.x *= s
                v.co.y *= s
                v.co.z += random.uniform(-0.025, 0.02)

    parts = [
        part("Cloak", cloak_body, cloak, M((0, 0, 0.36)), smooth=True),
        part("Hood", sphere(10, 7, 0.25), cloak, M((0, 0.02, 0.84), scale=(1, 1, 1.05)), smooth=True),
        part("HoodTip", cone(8, 0.11, 0.0, 0.34), cloak, M((0, 0.14, 1.03), rot=(-55, 0, 0)), smooth=True),
        part("Mask", sphere(12, 8), mask, M((0, -0.2, 0.83), scale=(0.17, 0.06, 0.21)), smooth=True),
        part("Mark", cube(), mark, M((0, -0.262, 0.765), scale=(0.018, 0.012, 0.07))),
    ]
    for s in (-1, 1):
        parts.append(part("Eye", sphere(8, 6), ink, M((s * 0.065, -0.255, 0.87), scale=(0.032, 0.02, 0.046)), smooth=True))
        parts.append(part("Foot", sphere(8, 6), ink, M((s * 0.11, -0.17, 0.035), scale=(0.075, 0.09, 0.045)), smooth=True))

    oar = M((0.36, -0.06, 0.0), rot=(6, 10, 0))  # pivot at the foot of the oar, leaning out to the right
    parts += [
        part("OarShaft", cone(6, 0.028, 0.028, 2.1), wood, oar @ M((0, 0, 1.05))),
        part("OarBlade", cube(), blade, oar @ M((0, 0, 1.95), scale=(0.17, 0.035, 0.55))),
        part("OarBand", cone(6, 0.036, 0.036, 0.05), mark, oar @ M((0, 0, 1.62))),
        part("Hand", sphere(8, 6, 0.065), cloak_dark, oar @ M((0, 0, 0.62)), smooth=True),
    ]
    ob = join(parts, "Apprentice")
    save_and_export("apprentice", [ob])


# Soured rig: bone -> (head, tail, parent). Front is -Y.
SOURED_BONES = {
    "Root": ((0, 0, 0), (0, 0, 0.35), None),
    "Spine": ((0, 0, 0.35), (0, -0.18, 1.1), "Root"),
    "Head": ((0, -0.18, 1.1), (0, -0.34, 1.38), "Spine"),
    "ArmL": ((0.34, -0.12, 1.0), (0.52, -0.36, 0.22), "Spine"),
    "ArmR": ((-0.34, -0.12, 1.0), (-0.52, -0.36, 0.22), "Spine"),
}

# Poses are written in armature space (degrees about world X/Y/Z, metres) and converted to bone space.
# +X rotation leans an upright bone forward (towards -Y); for the hanging arms, -X swings them forward and up.
REST = {}
SOURED_ACTIONS = {
    # name: (interpolation, [(frame, {bone: (rx, ry, rz)}, root_offset)])
    "Idle": ("BEZIER", [
        (1, {}, (0, 0, 0)),
        (24, {"Spine": (4, 0, 0), "Head": (-5, 0, 0), "ArmL": (-6, 0, -3), "ArmR": (-6, 0, 3)}, (0, 0, -0.02)),
        (48, {}, (0, 0, 0)),
    ]),
    "Windup": ("BEZIER", [
        (1, {}, (0, 0, 0)),
        (6, {"Spine": (12, 0, 0), "ArmL": (-15, 0, 0), "ArmR": (-15, 0, 0)}, (0, 0.05, -0.08)),
        (18, {"Spine": (-28, 0, 0), "Head": (-18, 0, 0), "ArmL": (-160, -20, 0), "ArmR": (-160, 20, 0)}, (0, 0.12, 0.08)),
        (21, {"Spine": (-31, 0, 0), "Head": (-20, 0, 0), "ArmL": (-165, -22, 0), "ArmR": (-165, 22, 0)}, (0, 0.13, 0.09)),
    ]),
    "Strike": ("LINEAR", [
        (1, {"Spine": (-31, 0, 0), "Head": (-20, 0, 0), "ArmL": (-165, -22, 0), "ArmR": (-165, 22, 0)}, (0, 0.13, 0.09)),
        (4, {"Spine": (30, 0, 0), "Head": (8, 0, 0), "ArmL": (-60, -8, 0), "ArmR": (-60, 8, 0)}, (0, -0.55, -0.1)),
        (9, {"Spine": (33, 0, 0), "Head": (10, 0, 0), "ArmL": (-55, -8, 0), "ArmR": (-55, 8, 0)}, (0, -0.6, -0.12)),
    ]),
    "Recover": ("BEZIER", [
        (1, {"Spine": (33, 0, 0), "Head": (10, 0, 0), "ArmL": (-55, -8, 0), "ArmR": (-55, 8, 0)}, (0, -0.6, -0.12)),
        (8, {"Spine": (28, 0, 0), "Head": (18, 0, 0), "ArmL": (-18, -4, 0), "ArmR": (-18, 4, 0)}, (0, -0.6, -0.15)),
        (25, {}, (0, 0, 0)),
    ]),
}


def soured():
    """A Soured: hunched, hooded, long arms with claws, glowing eyes. Rigged, four actions."""
    random.seed(2)
    body, dark = mat("Soured", "#6c5c84"), mat("SouredDark", "#4c4060")
    eye, claw = mat("SouredEye_Glow", "#c8f07a"), mat("Claw", "#d6cdb8")

    arm_data = bpy.data.armatures.new("SouredRig")
    rig = bpy.data.objects.new("SouredRig", arm_data)
    bpy.context.scene.collection.objects.link(rig)
    bpy.context.view_layer.objects.active = rig
    bpy.ops.object.mode_set(mode="EDIT")
    for name, (head, tail, parent) in SOURED_BONES.items():
        eb = arm_data.edit_bones.new(name)
        eb.head, eb.tail = head, tail
        if parent:
            eb.parent = arm_data.edit_bones[parent]
    bpy.ops.object.mode_set(mode="OBJECT")

    def robe(bm):
        bmesh.ops.create_cone(bm, cap_ends=True, segments=9, radius1=0.5, radius2=0.22, depth=1.05)
        rim = sorted((v for v in bm.verts if v.co.z < -0.5 and v.co.length > 0.1),
                     key=lambda v: math.atan2(v.co.y, v.co.x))
        for i, v in enumerate(rim):  # tattered zig-zag hem
            v.co.z += 0.1 if i % 2 else 0.0

    parts = [
        part("Robe", robe, body, M((0, 0, 0.58), rot=(14, 0, 0)), smooth=True, group="Spine"),
        part("Hump", sphere(10, 7), body, M((0, -0.06, 1.0), scale=(0.36, 0.3, 0.26)), smooth=True, group="Spine"),
        part("Head", sphere(10, 7, 0.22), dark, M((0, -0.33, 1.2), scale=(1, 1.05, 0.95)), smooth=True, group="Head"),
        part("Hood", cone(8, 0.27, 0.06, 0.42), body, M((0, -0.26, 1.32), rot=(-35, 0, 0)), smooth=True, group="Head"),
    ]
    for s in (-1, 1):
        parts.append(part("Eye", sphere(8, 6, 0.04), eye, M((s * 0.08, -0.53, 1.2)), smooth=True, group="Head"))
    for name in ("ArmL", "ArmR"):
        head, tail = Vector(SOURED_BONES[name][0]), Vector(SOURED_BONES[name][1])
        d = tail - head
        along = d.to_track_quat("Z", "Y").to_matrix().to_4x4()
        parts.append(part(name + "Mesh", cone(6, 0.1, 0.045, d.length), body,
                          Matrix.Translation((head + tail) / 2) @ along, smooth=True, group=name))
        for k in (-1, 0, 1):
            direction = (d.normalized() + Vector((k * 0.3, 0, -0.25))).normalized()
            parts.append(part("Claw", cone(5, 0.03, 0.0, 0.18), claw,
                              Matrix.Translation(tail + direction * 0.08) @ direction.to_track_quat("Z", "Y").to_matrix().to_4x4(),
                              group=name))
    mesh = join(parts, "Soured")
    mesh.parent = rig
    mesh.modifiers.new("Armature", "ARMATURE").object = rig

    for name in SOURED_BONES:
        REST[name] = rig.data.bones[name].matrix_local.to_quaternion()
        rig.pose.bones[name].rotation_mode = "QUATERNION"

    rig.animation_data_create()
    prefs = bpy.context.preferences.edit
    for action_name, (interp, keys) in SOURED_ACTIONS.items():
        prefs.keyframe_new_interpolation_type = interp
        action = bpy.data.actions.new(action_name)
        action.use_fake_user = True
        rig.animation_data.action = action
        for frame, pose, root_offset in keys:
            for bone in SOURED_BONES:
                pb = rig.pose.bones[bone]
                world = Euler([math.radians(a) for a in pose.get(bone, (0, 0, 0))]).to_quaternion()
                pb.rotation_quaternion = REST[bone].inverted() @ world @ REST[bone]
                pb.location = (REST[bone].inverted() @ Vector(root_offset)) if bone == "Root" else (0, 0, 0)
                pb.keyframe_insert("rotation_quaternion", frame=frame)
                pb.keyframe_insert("location", frame=frame)
    prefs.keyframe_new_interpolation_type = "BEZIER"
    rig.animation_data.action = bpy.data.actions["Idle"]
    save_and_export("soured", [rig, mesh], animated=True)


def jetty():
    """Jetty with planks, beams, posts and a Mooring post with rope and a hanging lantern."""
    random.seed(3)
    planks = [mat("Plank1", "#6e5746"), mat("Plank2", "#5f4b3d"), mat("Plank3", "#7a6250")]
    beam, rope = mat("Beam", "#46382e"), mat("Rope", "#b89e6e")
    metal, glow = mat("Metal", "#2c2b31"), mat("Lantern_Glow", "#ff9a3c")
    count, step = 14, 0.34
    length = (count - 1) * step
    parts = []
    for i in range(count):
        parts.append(part(f"Plank{i}", cube(), planks[i % 3],
                          M((random.uniform(-0.04, 0.04), i * step, random.uniform(-0.012, 0.012)),
                            rot=(0, random.uniform(-1.5, 1.5), random.uniform(-2.5, 2.5)),
                            scale=(1.7 + random.uniform(-0.1, 0.1), 0.29, 0.06))))
    for x in (-0.62, 0.62):
        parts.append(part("Beam", cube(), beam, M((x, length / 2, -0.08), scale=(0.12, length + 0.3, 0.1))))
        for y in (0.0, length / 3, 2 * length / 3, length):
            parts.append(part("Post", cone(6, 0.09, 0.09, 1.5), beam,
                              M((x * 1.16, y, -0.55), rot=(random.uniform(-3, 3), random.uniform(-3, 3), 0))))
    px, py = 0.74, length + 0.12
    parts += [
        part("MooringPost", cone(7, 0.13, 0.12, 2.5), beam, M((px, py, 0.0))),
        part("PostCap", cone(7, 0.16, 0.02, 0.16), metal, M((px, py, 1.33))),
        part("LanternArm", cube(), metal, M((px + 0.22, py, 1.12), scale=(0.46, 0.05, 0.05))),
        part("LanternHook", cube(), metal, M((px + 0.42, py, 1.06), scale=(0.02, 0.02, 0.08))),
        part("LanternTop", cone(4, 0.14, 0.02, 0.12), metal, M((px + 0.42, py, 0.98), rot=(0, 0, 45))),
        part("LanternGlass", cube(), glow, M((px + 0.42, py, 0.83), scale=(0.15, 0.15, 0.2))),
        part("LanternBase", cube(), metal, M((px + 0.42, py, 0.71), scale=(0.18, 0.18, 0.03))),
    ]
    bpy.ops.mesh.primitive_torus_add(major_radius=0.17, minor_radius=0.035, major_segments=10, minor_segments=5,
                                     location=(px, py, 0.45))
    ring = bpy.context.active_object
    ring.data.materials.append(rope)
    parts.append(ring)
    ob = join(parts, "Jetty")
    save_and_export("jetty", [ob])


def boat():
    """Rowing boat lofted from cross-sections, square stern, pointed bow."""
    hull, seat = mat("Hull", "#4f7186"), mat("Seat", "#7d6149")
    ts = [0, 0.15, 0.35, 0.55, 0.75, 0.9, 1.0]
    half_width = [0.5, 0.6, 0.63, 0.6, 0.48, 0.27, 0.03]
    depth = [0.34, 0.4, 0.42, 0.42, 0.39, 0.34, 0.3]
    rim = [0.42, 0.4, 0.39, 0.4, 0.43, 0.5, 0.6]

    def build(bm):
        sections = []
        for t, w, d, r in zip(ts, half_width, depth, rim):
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
        part("Hull", build, hull),
        part("SeatAft", cube(), seat, M((0, -0.45, 0.26), scale=(1.1, 0.24, 0.045))),
        part("SeatFwd", cube(), seat, M((0, 0.45, 0.26), scale=(1.05, 0.22, 0.045))),
    ]
    ob = join(parts, "Boat")
    save_and_export("boat", [ob])


def rocks():
    """Rocks and islets: faceted icospheres, faces pointing up get a moss/earth material."""
    moss, earth = ("Moss", "#8a9868"), ("Earth", "#a67c45")
    specs = [
        ("rock_a", 11, 1, 0.22, (0.9, 0.75, 0.55), moss),
        ("rock_b", 12, 1, 0.25, (0.6, 0.55, 0.5), moss),
        ("rock_c", 13, 1, 0.20, (1.4, 1.0, 0.6), moss),
        ("islet_big", 21, 2, 0.12, (3.4, 2.9, 1.1), earth),
        ("islet_small", 22, 2, 0.14, (2.2, 1.8, 1.0), moss),
    ]
    for name, seed, sub, jitter, scale, top in specs:
        if ONLY and name not in ONLY and "rocks" not in ONLY:
            continue
        new_scene()

        def build(bm, seed=seed, sub=sub, jitter=jitter):
            random.seed(seed)
            bmesh.ops.create_icosphere(bm, subdivisions=sub, radius=1.0)
            for v in bm.verts:
                v.co += v.co.normalized() * random.uniform(-jitter, jitter)

        ob = part(name, build, mat("Rock", "#80838c"), M(scale=scale))
        ob.data.materials.append(mat(*top))
        for p in ob.data.polygons:
            p.material_index = 1 if p.normal.z > 0.75 else 0
        save_and_export(name, [ob])


def orchard_tree():
    """Autumn tree for the Orchard of Last Autumn: bent trunk, branches, faceted canopy blobs, fruit, fallen leaves."""
    random.seed(7)
    bark, fruit = mat("Bark", "#4a3426"), mat("Fruit", "#8e1f1f")
    leaves = [mat("LeafOrange", "#d9782d"), mat("LeafRust", "#b9522a"), mat("LeafAmber", "#e49c3c")]
    parts = [part("Trunk", tube([(0, 0, -0.1), (0.04, 0.02, 0.5), (0.14, 0.06, 1.0), (0.08, 0.12, 1.5)],
                                [0.22, 0.16, 0.13, 0.1]), bark)]
    for points in ([(0.14, 0.06, 1.0), (0.55, 0.1, 1.35), (0.85, 0.18, 1.55)],
                   [(0.1, 0.1, 1.25), (-0.35, 0.05, 1.55), (-0.6, -0.05, 1.7)],
                   [(0.08, 0.12, 1.5), (0.1, 0.5, 1.8)]):
        parts.append(part("Branch", tube(points, [0.08, 0.055, 0.035][:len(points)]), bark))
    blobs = [((0.1, 0.1, 2.05), 0.8), ((0.8, 0.2, 1.8), 0.62), ((-0.55, -0.02, 1.85), 0.6),
             ((0.12, 0.6, 1.8), 0.55), ((0.2, -0.5, 1.85), 0.58), ((0.4, 0.3, 2.35), 0.5)]
    for i, (centre, r) in enumerate(blobs):
        parts.append(part(f"Canopy{i}", ico(1), leaves[i % 3], M(centre, scale=(r, r, r * 0.8)), jitter=0.08))
    for _ in range(9):
        centre, r = random.choice(blobs)
        a = random.uniform(0, 2 * math.pi)
        pos = Vector(centre) + Vector((math.cos(a) * r * 0.7, math.sin(a) * r * 0.7, -r * 0.62))
        parts.append(part("Fruit", sphere(8, 6, 0.075), fruit, M(tuple(pos)), smooth=True))

    def leaf(bm):
        bm.faces.new([bm.verts.new(p) for p in ((0, 0, 0), (0.14, 0.03, 0), (0.05, 0.12, 0))])

    for k in range(14):
        a, r = random.uniform(0, 2 * math.pi), random.uniform(0.35, 1.0)
        parts.append(part("FallenLeaf", leaf, leaves[k % 3],
                          M((math.cos(a) * r, math.sin(a) * r, 0.02), rot=(0, 0, random.uniform(0, 360)))))
    ob = join(parts, "OrchardTree")
    save_and_export("orchard_tree", [ob])


MODELS = {"apprentice": apprentice, "soured": soured, "jetty": jetty, "boat": boat, "orchard_tree": orchard_tree}

for model_name, build_model in MODELS.items():
    if not ONLY or model_name in ONLY:
        new_scene()
        build_model()
rocks()
