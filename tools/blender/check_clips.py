"""Check a rigged .blend's actions, frame by frame (for animating: see CLAUDE.md, Animating a creature).

    blender -b <model>.blend --python tools/blender/check_clips.py -- [Action,Action]

For each action: its length; how low the posed mesh goes (below 0: through the floor, unless it's meant to be: a
Drowned under the water); for each bone's tip, its fastest speed (cm a frame) and its biggest jerk (the most its speed
changes from one frame to the next: a snap or a pop, unless it's an impact) and where; a strip of its speed through
the clip; and how far its last pose is from its first (a loop's should be ~0).
"""
import sys

import bpy

args = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
only = args[0].split(",") if args and args[0] else None
scene = bpy.context.scene
arm = next(o for o in scene.objects if o.type == "ARMATURE")
meshes = [o for o in scene.objects if o.type == "MESH"]
spark = " ▁▂▃▄▅▆▇█"


def lowest():
    dg = bpy.context.evaluated_depsgraph_get()
    low = 9.0
    for m in meshes:
        ev = m.evaluated_get(dg)
        me = ev.to_mesh()
        for v in me.vertices:
            low = min(low, (ev.matrix_world @ v.co).z)
        ev.to_mesh_clear()
    return low


for action in bpy.data.actions:
    if only and action.name not in only:
        continue
    arm.animation_data.action = action
    s, e = (int(f) for f in action.frame_range)
    tips = {b.name: [] for b in arm.pose.bones}
    rots = []
    low, low_at = 9.0, s
    for f in range(s, e + 1):
        scene.frame_set(f)
        for b in arm.pose.bones:
            tips[b.name].append(arm.matrix_world @ b.tail)
        rots.append({b.name: b.matrix.to_quaternion() for b in arm.pose.bones})
        if (f - s) % 2 == 0 or f == e:
            z = lowest()
            if z < low:
                low, low_at = z, f
    seam = max(min(a, 360 - a) for a in (rots[0][n].rotation_difference(rots[-1][n]).angle * 57.2958 for n in rots[0]))
    print(f"== {action.name}: {e - s + 1} frames ({(e - s) / 30:.2f} s)   lowest {low:+.2f} m (f{low_at})   last vs first {seam:.0f}°")
    for name, ps in tips.items():
        v = [(ps[i + 1] - ps[i]).length * 100 for i in range(len(ps) - 1)]
        if not v or max(v) < 0.05:
            continue
        top = max(v)
        jerk = [abs(v[i + 1] - v[i]) for i in range(len(v) - 1)]
        worst = max(range(len(jerk)), key=lambda i: jerk[i]) if jerk else 0
        strip = "".join(spark[min(8, int(x / top * 8 + 0.5))] for x in v)
        print(f"   {name:12s} max {top:5.1f} cm/f   jerk {max(jerk) if jerk else 0:5.1f} @f{worst + s + 1:<3d} {strip}")
