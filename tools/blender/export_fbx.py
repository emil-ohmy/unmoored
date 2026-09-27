"""Export the open .blend file to FBX with Unity-friendly settings.

Run headless (normally via tools/export-models.sh):
    blender -b art/models/crate.blend --python tools/blender/export_fbx.py -- <out_dir>

Output: <out_dir>/<blend name>.fbx
"""
import os
import sys

import bpy

args = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
if not args:
    sys.exit("usage: blender -b file.blend --python export_fbx.py -- <out_dir>")

out_dir = os.path.abspath(args[0])
os.makedirs(out_dir, exist_ok=True)
name = os.path.splitext(os.path.basename(bpy.data.filepath))[0]
out_path = os.path.join(out_dir, f"{name}.fbx")

bpy.ops.export_scene.fbx(
    filepath=out_path,
    # Only export what belongs in the game; cameras/lights stay in Blender.
    object_types={"MESH", "ARMATURE", "EMPTY"},
    use_mesh_modifiers=True,
    # Unity is Y-up, Blender is Z-up. These settings make 1 Blender unit = 1 Unity metre
    # with no stray 90° rotation or 100x scale on the imported root.
    apply_unit_scale=True,
    apply_scale_options="FBX_SCALE_ALL",
    axis_forward="-Z",
    axis_up="Y",
    bake_space_transform=True,
    mesh_smooth_type="FACE",
    add_leaf_bones=False,
    bake_anim=True,
    bake_anim_use_all_actions=True,
    path_mode="COPY",
    embed_textures=False,
)
print(f"[export_fbx] {out_path}")
