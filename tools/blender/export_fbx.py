"""Export a hand-made .blend (one with no build script) to FBX with the project's settings.

    blender -b art/models/environment/thing.blend --python tools/blender/export_fbx.py

Output: game/Assets/_Project/Art/Models/<Category>/<blend name>.fbx
"""
import os
import sys

import bpy

sys.path.insert(0, os.path.dirname(__file__))
import kit  # noqa: E402

source = bpy.data.filepath
name = os.path.splitext(os.path.basename(source))[0]
category = os.path.basename(os.path.dirname(source))
objects = [o for o in bpy.context.scene.objects if o.type in {"MESH", "ARMATURE", "EMPTY"}]
kit.export_fbx(os.path.join(kit.FBX_ROOT, category.capitalize(), f"{name}.fbx"), objects)
