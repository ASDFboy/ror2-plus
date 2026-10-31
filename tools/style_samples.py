"""Re-renders the four sample items in the matte painted style. Original models.
Usage: /root/tools/bpyenv/bin/python tools/style_samples.py OUTDIR"""
import sys, os, math
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import bpy, bmesh
from ror2style import reset, paint, glowmat, smooth, cyl, sphere, tube, group, render

OUT = sys.argv[-1]

def meat():
    reset()
    flesh = paint('flesh', '#9c3a2a', wear=0.25)
    fat = paint('fat', '#e0b8a2', grime=0.15, wear=0.2)
    bone = paint('bone', '#d9ccb0', wear=0.4)
    bpy.ops.mesh.primitive_ico_sphere_add(radius=0.8, subdivisions=4)
    o = bpy.context.object; o.scale = (1.15, 0.85, 0.8)
    t = bpy.data.textures.new('lump', 'CLOUDS'); t.noise_scale = 0.7
    d = o.modifiers.new('d', 'DISPLACE'); d.texture = t; d.strength = 0.22
    o.data.materials.append(flesh); smooth(o, sub=1)
    parts = [o]
    # fat marbling: flattened ribbons pressed into the surface
    for pts, r in (([(-0.85, -0.45, 0.25), (-0.4, -0.7, 0.35), (0.1, -0.72, 0.3), (0.55, -0.55, 0.15)], 0.07),
                   ([(-0.6, -0.55, -0.25), (-0.1, -0.7, -0.15), (0.4, -0.6, -0.25)], 0.055),
                   ([(-0.2, -0.45, 0.6), (0.25, -0.5, 0.55), (0.6, -0.3, 0.45)], 0.05)):
        f = tube(pts, r, fat); f.scale = (1, 0.6, 1); parts.append(f)
    parts.append(cyl(0.13, 1.1, (0.95, 0, -0.35), rot=(0, math.radians(62), 0), m=bone))
    parts.append(sphere(0.17, (1.42, -0.1, -0.62), bone)); parts.append(sphere(0.17, (1.38, 0.13, -0.5), bone))
    group(parts, rot=(10, 8, 18))
    render(os.path.join(OUT, 'st_meat.png'))

def umbrella():
    reset()
    cloth_a = paint('clothA', '#3a6aa8', grime=0.15, wear=0.3)
    cloth_b = paint('clothB', '#d8cfb4', grime=0.15, wear=0.3)
    wood = paint('wood', '#6e4426', grime=0.25, wear=0.35)
    brass = paint('brass', '#b8893a', grime=0.2, wear=0.5)
    # dome canopy: top half of a sphere, 8 alternating panels, scalloped rim between ribs
    bpy.ops.mesh.primitive_uv_sphere_add(radius=1.25, segments=64, ring_count=24)
    o = bpy.context.object
    bm = bmesh.new(); bm.from_mesh(o.data)
    bmesh.ops.delete(bm, geom=[v for v in bm.verts if v.co.z < -0.05], context='VERTS')
    for v in bm.verts:
        v.co.z *= 0.55
        ang = math.atan2(v.co.y, v.co.x)
        if v.co.z < 0.25: v.co.z += 0.12 * abs(math.sin(ang * 4)) * (1 - v.co.z / 0.25)
    bm.to_mesh(o.data); bm.free()
    o.data.materials.append(cloth_a); o.data.materials.append(cloth_b)
    for p in o.data.polygons:
        ang = math.atan2(p.center.y, p.center.x)
        p.material_index = int(((ang + math.pi) / (2 * math.pi)) * 8) % 2
    s = o.modifiers.new('s', 'SOLIDIFY'); s.thickness = 0.05
    smooth(o)
    parts = [o]
    for k in range(8):  # ribs
        a = (k + 0.5) / 8 * 2 * math.pi
        pts = [(math.cos(a) * r, math.sin(a) * r, 0.69 * math.sqrt(max(0, 1 - (r / 1.25) ** 2)) + 0.01) for r in (0.1, 0.6, 1.0, 1.24)]
        parts.append(tube(pts, 0.018, wood))
    parts.append(sphere(0.08, (0, 0, 0.72), brass))
    parts.append(cyl(0.045, 1.9, (0, 0, -0.25), m=wood, verts=16))
    parts.append(tube([(0, 0, -1.15), (0, 0, -1.35), (0.18, 0, -1.55), (0.38, 0, -1.42), (0.42, 0, -1.25)], 0.055, wood))
    parts.append(cyl(0.065, 0.12, (0, 0, -1.12), m=brass, verts=16))
    group(parts, rot=(-12, 20, 10), loc=(0, 0, 0.3))
    render(os.path.join(OUT, 'st_umbrella.png'))

def scope():
    reset()
    metal = paint('metal', '#3d4452', grime=0.25, wear=0.6, shadow_hue=0.0)
    brass = paint('brass', '#b8893a', grime=0.2, wear=0.5)
    lens = glowmat('lens', '#67d4e8', 1.4)
    rot = (0, math.radians(90), 0)
    p = [cyl(0.2, 1.7, (0, 0, 0), rot, metal), cyl(0.3, 0.45, (0.95, 0, 0), rot, metal), cyl(0.26, 0.35, (-0.9, 0, 0), rot, metal),
         cyl(0.31, 0.07, (0.72, 0, 0), rot, brass), cyl(0.27, 0.07, (-0.72, 0, 0), rot, brass),
         cyl(0.1, 0.25, (0.05, 0, 0.25), (0, 0, 0), brass), cyl(0.09, 0.22, (0.05, -0.25, 0), (math.radians(90), 0, 0), brass),
         cyl(0.12, 0.6, (0.05, 0, -0.27), (0, math.radians(90), 0), metal), cyl(0.27, 0.02, (1.18, 0, 0), rot, lens, bevel=0)]
    group(p, rot=(0, -35, -25))
    render(os.path.join(OUT, 'st_scope.png'))

def void_syringe():
    reset()
    glass = paint('glass', '#8a7aa8', grime=0.0, wear=0.8, alpha=0.35)
    fluid = glowmat('fluid', '#b040f0', 1.6)
    metal = paint('metal', '#4a4f5e', grime=0.2, wear=0.5, shadow_hue=0.0)
    needle = paint('needle', '#c9ced8', grime=0.0, wear=0.6)
    flesh = paint('voidflesh', '#3d1a5c', grime=0.2, wear=0.4, shadow_hue=0.02)
    tipm = glowmat('tip', '#ff86ec', 2.5)
    rot = (0, math.radians(90), 0)
    p = [cyl(0.24, 1.2, (0, 0, 0), rot, glass, bevel=0.01), cyl(0.18, 0.95, (0.08, 0, 0), rot, fluid),
         cyl(0.08, 0.7, (-0.85, 0, 0), rot, metal), cyl(0.2, 0.06, (-1.2, 0, 0), rot, metal), cyl(0.35, 0.06, (-0.62, 0, 0), rot, metal),
         cyl(0.08, 0.2, (0.7, 0, 0), rot, metal), cyl(0.015, 0.7, (1.12, 0, 0), rot, needle, verts=12, bevel=0)]
    for pts in ([(-0.25, 0, 0.2), (-0.35, 0, 0.45), (-0.15, 0, 0.62)], [(0.3, 0, -0.2), (0.45, 0, -0.45), (0.3, 0, -0.62)], [(0.1, 0, 0.22), (0.25, 0, 0.45), (0.48, 0, 0.55)]):
        p.append(tube(pts, 0.05, flesh, taper=[1.0, 0.7, 0.4])); p.append(sphere(0.055, pts[-1], tipm))
    group(p, rot=(5, -40, -15))
    render(os.path.join(OUT, 'st_voidsyringe.png'))

if __name__ == '__main__':
    os.makedirs(OUT, exist_ok=True)
    meat(); umbrella(); scope(); void_syringe()
    print('done')
