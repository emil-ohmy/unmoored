"""Build one model from its script, save its .blend and export its FBX.

    blender -b --factory-startup --python tools/blender/build_model.py -- art/models/characters/apprentice.py

The model script gets `kit` in scope and must define build() -> list of objects to export.
art/models/<category>/<name>.py -> art/models/<category>/<name>.blend
                               -> game/Assets/_Project/Art/Models/<Category>/<name>.fbx
A script may also define VARIANTS = ["a", "b", ...] and build(variant); each variant is saved and exported
as <name>_<variant>.
"""
import os
import runpy
import sys

import bpy

sys.path.insert(0, os.path.dirname(__file__))
import kit  # noqa: E402

source = os.path.abspath(sys.argv[sys.argv.index("--") + 1])
name = os.path.splitext(os.path.basename(source))[0]
category = os.path.basename(os.path.dirname(source))


def build(variant=None):
    out_name = f"{name}_{variant}" if variant else name
    kit.new_scene(seed=sum(map(ord, out_name)))
    module = runpy.run_path(source, init_globals={"kit": kit})
    objects = module["build"](variant) if variant else module["build"]()
    bpy.ops.wm.save_as_mainfile(filepath=os.path.join(os.path.dirname(source), f"{out_name}.blend"))
    kit.export_fbx(os.path.join(kit.FBX_ROOT, category.capitalize(), f"{out_name}.fbx"), objects)


variants = runpy.run_path(source, init_globals={"kit": kit}, run_name="probe").get("VARIANTS")
for v in variants or [None]:
    build(v)
