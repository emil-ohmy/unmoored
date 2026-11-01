"""Film a rigged .blend's actions, every frame, from two angles: the game's camera (from the south, above) and the side
(the figure in profile, facing left). Workbench, outlined, a ground plane under it.

    blender -b <model>.blend --python tools/blender/film_clips.py -- <out_dir> [Action,Action] [size]

Writes <out_dir>/<action>/game_0001.png ... and side_0001.png; tools/film-clips.sh turns them into films and sheets.
"""
import math
import os
import sys

import bpy
from mathutils import Vector

args = sys.argv[sys.argv.index("--") + 1:]
out_dir = os.path.abspath(args[0])
only = args[1].split(",") if len(args) > 1 and args[1] else None
size = int(args[2]) if len(args) > 2 else 420

scene = bpy.context.scene
scene.render.engine = "BLENDER_WORKBENCH"
scene.display.shading.light = "STUDIO"
scene.display.shading.color_type = "MATERIAL"
scene.display.shading.show_object_outline = True
scene.display.shading.show_shadows = True
scene.render.resolution_x = scene.render.resolution_y = size
scene.render.fps = 30
scene.world = scene.world or bpy.data.worlds.new("World")

armature = next(o for o in scene.objects if o.type == "ARMATURE")
height = max(max(b.head_local.z, b.tail_local.z) for b in armature.data.bones)
height = max(height, 1.2)

# A ground plane with a grid of lines (to read movement against).
bpy.ops.mesh.primitive_plane_add(size=height * 6, location=(0, 0, 0))
ground = bpy.context.object
mat = bpy.data.materials.new("FilmGround")
mat.diffuse_color = (0.42, 0.42, 0.44, 1)
ground.data.materials.append(mat)
line = bpy.data.materials.new("FilmLine")
line.diffuse_color = (0.3, 0.3, 0.32, 1)
step = 0.5
k = -height * 3
while k <= height * 3:
    for rot, loc in (((0, 0, 0), (k, 0, 0.002)), ((0, 0, math.pi / 2), (0, k, 0.002))):
        bpy.ops.mesh.primitive_plane_add(size=1, location=loc, rotation=rot)
        o = bpy.context.object
        o.scale = (0.012, height * 3, 1)
        o.data.materials.append(line)
    k += step


def camera(name, location, target):
    cam = bpy.data.objects.new(name, bpy.data.cameras.new(name))
    cam.data.lens = 50
    scene.collection.objects.link(cam)
    cam.location = location
    cam.rotation_euler = (Vector(target) - Vector(location)).to_track_quat("-Z", "Y").to_euler()
    return cam


target = Vector((0, -0.2, height * 0.5))
pitch = math.radians(50)
distance = float(os.environ.get("FILM_DISTANCE", "2.4"))  # (times its height)
game = camera("Game", target + Vector((0, -math.cos(pitch), math.sin(pitch))) * height * distance, target)
side = camera("Side", target + Vector((height * distance * 0.95, -0.2, height * 0.3)), target)

for action in bpy.data.actions:
    if only and action.name not in only:
        continue
    armature.animation_data.action = action
    start, end = (int(f) for f in action.frame_range)
    folder = os.path.join(out_dir, action.name)
    os.makedirs(folder, exist_ok=True)
    for frame in range(start, end + 1):
        scene.frame_set(frame)
        for cam, view in ((game, "game"), (side, "side")):
            scene.camera = cam
            scene.render.filepath = os.path.join(folder, f"{view}_{frame - start + 1:04d}.png")
            bpy.ops.render.render(write_still=True)
    print(f"[film] {action.name}: frames {start}-{end}")
