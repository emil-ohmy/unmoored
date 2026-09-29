"""Render a contact sheet of every action in a rigged .blend, from the game's camera angle (Workbench).

    blender -b art/models/characters/apprentice.blend --python tools/blender/preview_poses.py -- <out_dir> [frames_per_action] [zoom] [Action,Action]

Writes <out_dir>/<action>_<n>.png; tools/preview-poses.sh stitches them into one sheet.
"""
import math
import os
import sys

import bpy
from mathutils import Vector

args = sys.argv[sys.argv.index("--") + 1:]
out_dir = os.path.abspath(args[0])
per_action = int(args[1]) if len(args) > 1 else 5
zoom = float(args[2]) if len(args) > 2 else 1.0  # >1 frames closer
only = args[3].split(",") if len(args) > 3 else None  # just these actions
os.makedirs(out_dir, exist_ok=True)

scene = bpy.context.scene
scene.render.engine = "BLENDER_WORKBENCH"
scene.display.shading.light = "STUDIO"
scene.display.shading.color_type = "MATERIAL"
scene.display.shading.show_object_outline = True
scene.render.resolution_x = scene.render.resolution_y = 300
scene.world = scene.world or bpy.data.worlds.new("World")

armature = next(o for o in scene.objects if o.type == "ARMATURE")
meshes = [o for o in scene.objects if o.type == "MESH"]
# Frame the body, not long props: use the rig's height rather than the mesh bounds.
height = max(max(b.head_local.z, b.tail_local.z) for b in armature.data.bones)

cam = bpy.data.objects.new("PreviewCam", bpy.data.cameras.new("PreviewCam"))
cam.data.lens = 50
scene.collection.objects.link(cam)
scene.camera = cam
target = Vector((0, 0, height * 0.45))
pitch, yaw = math.radians(50), math.radians(30)  # the game's fixed pitch, turned a little to show depth
cam.location = target + Vector((math.sin(yaw) * math.cos(pitch), -math.cos(yaw) * math.cos(pitch), math.sin(pitch))) * height * 4.0 / zoom
cam.rotation_euler = (target - cam.location).to_track_quat("-Z", "Y").to_euler()

for action in bpy.data.actions:
    if only and action.name not in only:
        continue
    armature.animation_data.action = action
    start, end = (int(f) for f in action.frame_range)
    for i in range(per_action):
        frame = round(start + (end - start) * i / max(1, per_action - 1))
        scene.frame_set(frame)
        scene.render.filepath = os.path.join(out_dir, f"{action.name}_{i}.png")
        bpy.ops.render.render(write_still=True)
    print(f"[preview] {action.name}: frames {start}-{end}")
