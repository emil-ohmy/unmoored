"""(tools/find-coplanar.sh) Find pairs of overlapping, coplanar faces facing the same way (z-fighting) in a model script's build.
Usage: blender -b --python coplanar.py -- name:variant"""
import os, sys, runpy, bpy
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import kit
from mathutils import Vector
kit.new_scene(0)
name = sys.argv[sys.argv.index("--") + 1]
model, _, variant = name.partition(":")
root = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "..", "art", "models")
path = next(os.path.join(d, f) for d, _, fs in os.walk(root) for f in fs if f == model + ".py")
module = runpy.run_path(path, init_globals={"kit": kit})
objs = module["build"](variant) if variant else module["build"]()
faces = []
for o in objs:
    if o.type != "MESH":
        continue
    me = o.data
    for p in me.polygons:
        n = (o.matrix_world.to_3x3() @ p.normal).normalized()
        vs = [o.matrix_world @ me.vertices[i].co for i in p.vertices]
        # axis-aligned faces only (what boxes make)
        axis = max(range(3), key=lambda a: abs(n[a]))
        if abs(n[axis]) < 0.999:
            continue
        d = round(vs[0][axis], 3)
        others = [a for a in range(3) if a != axis]
        lo = [min(v[a] for v in vs) for a in others]
        hi = [max(v[a] for v in vs) for a in others]
        faces.append((axis, 1 if n[axis] > 0 else -1, d, lo, hi, me.materials[p.material_index].name if me.materials else ""))
from collections import defaultdict
groups = defaultdict(list)
for f in faces:
    groups[(f[0], f[1], f[2])].append(f)
hits = 0
for key, fs in groups.items():
    for i in range(len(fs)):
        for j in range(i + 1, len(fs)):
            a, b = fs[i], fs[j]
            ox = min(a[4][0], b[4][0]) - max(a[3][0], b[3][0])
            oy = min(a[4][1], b[4][1]) - max(a[3][1], b[3][1])
            if ox > 0.005 and oy > 0.005:
                hits += 1
                if hits <= 12:
                    print(f"COPLANAR axis={key[0]} at {key[2]} {a[5]} / {b[5]} overlap {ox:.2f}x{oy:.2f}")
print(f"COPLANAR total {hits}")
