"""Helpers for building models from code in Blender (used by art/models/**/*.py via build_model.py).

Conventions:
- Front is -Y, up is +Z, 1 unit = 1 metre, origin at the model's base. export_fbx turns the front into Unity's
  forward (+Z), so transform.forward is where a character looks.
- Colours are sRGB hex strings (stored linear, like Blender's colour pickers). A material whose name ends in
  "_Glow" is imported by Unity as emissive.
- Rig poses are written in armature space: rotations are degrees about the world X/Y/Z axes (applied X, then Y,
  then Z) around the bone's head, offsets are metres. +X leans an upright bone forward (towards -Y).
"""
import math
import os
import random

import bmesh
import bpy
from mathutils import Euler, Matrix, Vector

REPO = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
FBX_ROOT = os.path.join(REPO, "game", "Assets", "_Project", "Art", "Models")
FPS = 30

rng = random.Random(0)


def new_scene(seed=0):
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.context.scene.render.fps = FPS
    rng.seed(seed)


# ---------------------------------------------------------------- materials

def lin(hex_colour):
    h = hex_colour.lstrip("#")

    def c(x):
        x /= 255.0
        return x / 12.92 if x <= 0.04045 else ((x + 0.055) / 1.055) ** 2.4

    return (c(int(h[0:2], 16)), c(int(h[2:4], 16)), c(int(h[4:6], 16)), 1.0)


def mat(name, hex_colour):
    m = bpy.data.materials.get(name)
    if m:
        return m
    m = bpy.data.materials.new(name)
    m.diffuse_color = lin(hex_colour)
    if m.node_tree is None:
        try:
            m.use_nodes = True  # needed before Blender 5; deprecated after
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


# ---------------------------------------------------------------- geometry

def M(loc=(0, 0, 0), rot=(0, 0, 0), scale=(1, 1, 1)):
    """Translation @ rotation (degrees, XYZ) @ scale."""
    return (Matrix.Translation(loc)
            @ Euler([math.radians(a) for a in rot]).to_matrix().to_4x4()
            @ Matrix.Diagonal((*scale, 1.0)))


def along(head, tail):
    """Matrix placing a Z-aligned primitive (centred at the origin) between two points."""
    head, tail = Vector(head), Vector(tail)
    d = tail - head
    return Matrix.Translation((head + tail) / 2) @ d.to_track_quat("Z", "Y").to_matrix().to_4x4()


def cone(seg, r1, r2, depth):
    return lambda bm: bmesh.ops.create_cone(bm, cap_ends=True, cap_tris=False, segments=seg,
                                            radius1=r1, radius2=r2, depth=depth)


def cylinder(seg, r, depth):
    return cone(seg, r, r, depth)


def sphere(u, v, r=1.0):
    return lambda bm: bmesh.ops.create_uvsphere(bm, u_segments=u, v_segments=v, radius=r)


def ico(sub, r=1.0):
    return lambda bm: bmesh.ops.create_icosphere(bm, subdivisions=sub, radius=r)


def cube():
    return lambda bm: bmesh.ops.create_cube(bm, size=1.0)


def prism(points, height):
    """Extrude a closed 2D outline (x, y) upwards from z=0 by `height`."""
    def build(bm):
        bottom = [bm.verts.new((x, y, 0.0)) for x, y in points]
        top = [bm.verts.new((x, y, height)) for x, y in points]
        n = len(points)
        bm.faces.new(list(reversed(bottom)))
        bm.faces.new(top)
        for i in range(n):
            bm.faces.new((bottom[i], bottom[(i + 1) % n], top[(i + 1) % n], top[i]))
        bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    return build


def tube(points, radii, seg=6):
    """Tapered tube along a polyline (trunks, branches, ropes)."""
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
    """Build one mesh object. `group` assigns every vertex to that bone's vertex group (rigid skinning)."""
    bm = bmesh.new()
    build(bm)
    if jitter:
        for v in bm.verts:
            v.co += Vector((rng.uniform(-1, 1), rng.uniform(-1, 1), rng.uniform(-1, 1))) * jitter
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


def top_material(ob, material, min_normal_z=0.75):
    """Give upward-facing faces a second material (moss on rocks, paving on platforms)."""
    ob.data.materials.append(material)
    index = len(ob.data.materials) - 1
    for p in ob.data.polygons:
        if p.normal.z > min_normal_z:
            p.material_index = index


# ---------------------------------------------------------------- rigs and animation

def rig(name, bones):
    """bones: {name: (head, tail, parent or None)}. Returns the armature object."""
    data = bpy.data.armatures.new(name)
    ob = bpy.data.objects.new(name, data)
    bpy.context.scene.collection.objects.link(ob)
    bpy.context.view_layer.objects.active = ob
    bpy.ops.object.mode_set(mode="EDIT")
    for bone, (head, tail, parent) in bones.items():
        eb = data.edit_bones.new(bone)
        eb.head, eb.tail = head, tail
        if parent:
            eb.parent = data.edit_bones[parent]
    bpy.ops.object.mode_set(mode="OBJECT")
    for pb in ob.pose.bones:
        pb.rotation_mode = "QUATERNION"
    return ob


def skin(mesh, armature):
    mesh.parent = armature
    mesh.modifiers.new("Armature", "ARMATURE").object = armature


def animate(armature, actions):
    """actions: {name: (interpolation, [(frame, {bone: (rx, ry, rz) or ((rx, ry, rz), (dx, dy, dz))})])}.
    Every key sets every bone (unlisted bones return to rest), so each clip fully defines its poses."""
    rest = {b.name: b.matrix_local.to_quaternion() for b in armature.data.bones}
    armature.animation_data_create()
    prefs = bpy.context.preferences.edit
    for action_name, (interpolation, keys) in actions.items():
        prefs.keyframe_new_interpolation_type = interpolation
        action = bpy.data.actions.new(action_name)
        action.use_fake_user = True
        armature.animation_data.action = action
        for frame, pose in keys:
            for bone in armature.pose.bones:
                value = pose.get(bone.name, (0, 0, 0))
                rot, offset = value if isinstance(value[0], (tuple, list)) else (value, (0, 0, 0))
                q = rest[bone.name]
                world = Euler([math.radians(a) for a in rot]).to_quaternion()
                bone.rotation_quaternion = q.inverted() @ world @ q
                bone.location = q.inverted() @ Vector(offset)
                bone.keyframe_insert("rotation_quaternion", frame=frame)
                bone.keyframe_insert("location", frame=frame)
    prefs.keyframe_new_interpolation_type = "BEZIER"
    first = next(iter(actions))
    armature.animation_data.action = bpy.data.actions[first]


# ---------------------------------------------------------------- export

def export_fbx(path, objects):
    """Export `objects` to FBX. Rigged exports skip bake_space_transform (Blender flags it as broken with
    armatures); Unity's bakeAxisConversion handles those. Static exports bake it so their root has no rotation."""
    animated = any(o.type == "ARMATURE" for o in objects)
    # Turn the export 180° about Z so Blender's front (-Y) arrives as Unity's forward (+Z). FBX axis settings
    # can't do this: Unity converts from whatever axis system the file declares. Turning the top-level objects
    # (not their data) leaves rigs and animations untouched; the .blend is saved before this, unturned.
    turn = Matrix.Rotation(math.pi, 4, "Z")
    for o in objects:
        if o.parent is None:
            o.matrix_world = turn @ o.matrix_world
    for o in bpy.context.scene.objects:
        o.select_set(o in objects)
    os.makedirs(os.path.dirname(path), exist_ok=True)
    bpy.ops.export_scene.fbx(
        filepath=path,
        use_selection=True,
        object_types={"MESH", "ARMATURE", "EMPTY"},
        use_mesh_modifiers=True,
        apply_unit_scale=True,
        apply_scale_options="FBX_SCALE_ALL",
        axis_forward="-Z",
        axis_up="Y",
        bake_space_transform=not animated,
        mesh_smooth_type="FACE",
        add_leaf_bones=False,
        bake_anim=animated,
        bake_anim_use_all_actions=animated,
        bake_anim_use_nla_strips=False,
        bake_anim_force_startend_keying=True,
        path_mode="COPY",
        embed_textures=False,
    )
    print(f"[kit] exported {os.path.relpath(path, REPO)}")
