"""Renders four sample item icons with Blender (bpy). Original models built from code.
Usage: /root/tools/bpyenv/bin/python tools/blender_samples.py OUTDIR"""
import sys, os, math
import bpy, bmesh
from mathutils import Vector, Euler

OUT = sys.argv[-1]

def reset():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    sc = bpy.context.scene
    sc.render.engine = 'CYCLES'
    sc.cycles.device = 'CPU'; sc.cycles.samples = 48; sc.cycles.use_denoising = True
    sc.render.resolution_x = sc.render.resolution_y = 512
    sc.render.film_transparent = True
    sc.view_settings.view_transform = 'Standard'; sc.view_settings.look = 'None'
    world = bpy.data.worlds.new('w'); sc.world = world; world.use_nodes = True
    bg = world.node_tree.nodes['Background']; bg.inputs[0].default_value = (0.32, 0.33, 0.42, 1); bg.inputs[1].default_value = 0.9
    cam_data = bpy.data.cameras.new('cam'); cam_data.type = 'ORTHO'; cam_data.ortho_scale = 2.7
    cam = bpy.data.objects.new('cam', cam_data); sc.collection.objects.link(cam); sc.camera = cam
    cam.location = (0, -6, 0); cam.rotation_euler = (math.radians(90), 0, 0)
    def light(name, typ, loc, energy, color, size=1.0):
        ld = bpy.data.lights.new(name, typ); ld.energy = energy; ld.color = color
        if typ == 'AREA': ld.size = size
        lo = bpy.data.objects.new(name, ld); sc.collection.objects.link(lo); lo.location = loc
        lo.rotation_euler = (Vector((0, 0, 0)) - Vector(loc)).to_track_quat('-Z', 'Y').to_euler()
    light('key', 'AREA', (-3, -4, 4), 900, (1.0, 0.96, 0.9), 3)
    light('rim', 'AREA', (3.5, 3, 2.5), 700, (0.75, 0.85, 1.0), 2)
    light('fill', 'AREA', (3, -4, -1.5), 180, (0.9, 0.85, 1.0), 4)

def mat(name, color, rough=0.5, metal=0.0, emit=None, emit_strength=0.0, transmission=0.0, alpha=1.0, sss=0.0):
    m = bpy.data.materials.new(name); m.use_nodes = True
    b = m.node_tree.nodes['Principled BSDF']
    b.inputs['Base Color'].default_value = (*color, 1)
    b.inputs['Roughness'].default_value = rough; b.inputs['Metallic'].default_value = metal
    if transmission: b.inputs['Transmission Weight'].default_value = transmission; b.inputs['IOR'].default_value = 1.45
    if emit: b.inputs['Emission Color'].default_value = (*emit, 1); b.inputs['Emission Strength'].default_value = emit_strength
    if sss: b.inputs['Subsurface Weight'].default_value = sss; b.inputs['Subsurface Radius'].default_value = (0.6, 0.2, 0.1)
    if alpha < 1: b.inputs['Alpha'].default_value = alpha
    return m

def srgb(h):
    h = h.lstrip('#'); c = [int(h[i:i + 2], 16) / 255 for i in (0, 2, 4)]
    return tuple(((x + 0.055) / 1.055) ** 2.4 if x > 0.04045 else x / 12.92 for x in c)

def finish(obj, m, bevel=0.0, subsurf=0, smooth=True):
    obj.data.materials.append(m)
    if bevel: mod = obj.modifiers.new('bev', 'BEVEL'); mod.width = bevel; mod.segments = 3
    if subsurf: mod = obj.modifiers.new('sub', 'SUBSURF'); mod.levels = mod.render_levels = subsurf
    if smooth:
        for p in obj.data.polygons: p.use_smooth = True
    return obj

def cyl(r, depth, loc, rot=(0, 0, 0), m=None, verts=48, bevel=0.02):
    bpy.ops.mesh.primitive_cylinder_add(radius=r, depth=depth, location=loc, rotation=rot, vertices=verts)
    o = bpy.context.object; finish(o, m, bevel=bevel); return o

def sphere(r, loc, m, scale=(1, 1, 1), sub=2):
    bpy.ops.mesh.primitive_uv_sphere_add(radius=r, location=loc, segments=48, ring_count=24)
    o = bpy.context.object; o.scale = scale; finish(o, m); return o

def tube_curve(points, radius, m, res=12):
    cd = bpy.data.curves.new('c', 'CURVE'); cd.dimensions = '3D'; cd.bevel_depth = radius; cd.bevel_resolution = 6
    sp = cd.splines.new('BEZIER'); sp.bezier_points.add(len(points) - 1)
    for p, co in zip(sp.bezier_points, points):
        p.co = co; p.handle_left_type = p.handle_right_type = 'AUTO'
    o = bpy.data.objects.new('curve', cd); bpy.context.scene.collection.objects.link(o)
    o.data.materials.append(m); return o

def render(name):
    bpy.context.scene.render.filepath = os.path.join(OUT, name + '.png')
    bpy.ops.render.render(write_still=True)

# ---------------------------------------------------------------- items
def meat():
    reset()
    m = bpy.data.materials.new('meat'); m.use_nodes = True
    nt = m.node_tree; b = nt.nodes['Principled BSDF']
    noise = nt.nodes.new('ShaderNodeTexNoise'); noise.inputs['Scale'].default_value = 3.5; noise.inputs['Detail'].default_value = 6
    ramp = nt.nodes.new('ShaderNodeValToRGB')
    ramp.color_ramp.elements[0].position = 0.52; ramp.color_ramp.elements[0].color = (*srgb('#a8392c'), 1)
    ramp.color_ramp.elements[1].position = 0.58; ramp.color_ramp.elements[1].color = (*srgb('#f0c9b8'), 1)
    nt.links.new(noise.outputs['Fac'], ramp.inputs['Fac']); nt.links.new(ramp.outputs['Color'], b.inputs['Base Color'])
    b.inputs['Roughness'].default_value = 0.35; b.inputs['Subsurface Weight'].default_value = 0.2
    bone = mat('bone', srgb('#efe5cf'), rough=0.6)
    bpy.ops.mesh.primitive_ico_sphere_add(radius=0.75, subdivisions=5, location=(0, 0, 0))
    o = bpy.context.object; o.scale = (1.1, 0.8, 0.85)
    tex = bpy.data.textures.new('n', 'VORONOI'); tex.noise_scale = 0.55
    d = o.modifiers.new('d', 'DISPLACE'); d.texture = tex; d.strength = 0.16
    finish(o, m)
    grp = [o]
    # bone shaft starts inside the meat and pokes out the lower right
    grp.append(cyl(0.13, 1.0, (0.85, 0, -0.45), rot=(0, math.radians(62), 0), m=bone))
    grp.append(sphere(0.16, (1.3, -0.1, -0.68), bone)); grp.append(sphere(0.16, (1.27, 0.12, -0.56), bone))
    bpy.ops.object.empty_add(location=(0, 0, 0)); root = bpy.context.object
    for x in grp: x.parent = root
    root.rotation_euler = (0.25, 0.15, 0.3)
    render('bl_meat')

def umbrella():
    reset()
    a = mat('a', srgb('#2f66c4'), rough=0.45); b = mat('b', srgb('#1d3f86'), rough=0.45)
    wood = mat('wood', srgb('#7a4a26'), rough=0.55); gold = mat('gold', srgb('#d7a64a'), rough=0.3, metal=1)
    bpy.ops.mesh.primitive_cone_add(vertices=8, radius1=1.25, radius2=0.0, depth=0.55, location=(0, 0, 0.55))
    o = bpy.context.object; o.data.materials.append(a); o.data.materials.append(b)
    for i, p in enumerate(o.data.polygons):
        if p.normal.z < 0.99: p.material_index = i % 2
    bm = bmesh.new(); bm.from_mesh(o.data)
    for v in bm.verts:
        if v.co.z < 0.0: v.co.z -= 0.1
    bm.to_mesh(o.data)
    sol = o.modifiers.new('s', 'SOLIDIFY'); sol.thickness = 0.04
    grp = [o]
    grp.append(cyl(0.04, 1.9, (0, 0, -0.1), m=wood, verts=16, bevel=0.0))       # shaft from tip to below rim
    grp.append(sphere(0.07, (0, 0, 0.86), gold))                                  # finial on the tip
    grp.append(tube_curve([(0, 0, -1.02), (0, 0, -1.25), (0.22, 0, -1.38), (0.36, 0, -1.2)], 0.045, wood))  # J handle
    bpy.ops.object.empty_add(location=(0, 0, 0)); root = bpy.context.object
    for x in grp: x.parent = root
    root.rotation_euler = (math.radians(-18), math.radians(14), 0)
    root.location = (0, 0, 0.2)
    render('bl_umbrella')

def scope():
    reset()
    metal = mat('metal', srgb('#2f3442'), rough=0.32, metal=0.85)
    brass = mat('brass', srgb('#c99a44'), rough=0.3, metal=1)
    lens = mat('lens', srgb('#3fb4cf'), rough=0.05, emit=srgb('#5fd6ff'), emit_strength=1.2)
    grp = []
    rot = (0, math.radians(90), 0)
    grp += [cyl(0.2, 1.7, (0, 0, 0), rot, metal), cyl(0.3, 0.45, (0.95, 0, 0), rot, metal), cyl(0.26, 0.35, (-0.9, 0, 0), rot, metal)]
    grp += [cyl(0.305, 0.07, (0.72, 0, 0), rot, brass), cyl(0.265, 0.07, (-0.72, 0, 0), rot, brass)]
    grp += [cyl(0.1, 0.25, (0.05, 0, 0.25), (0, 0, 0), brass), cyl(0.09, 0.22, (0.05, -0.25, 0), (math.radians(90), 0, 0), brass)]
    grp += [cyl(0.27, 0.02, (1.18, 0, 0), rot, lens, bevel=0.005)]
    bpy.ops.object.empty_add(location=(0, 0, 0)); root = bpy.context.object
    for o in grp: o.parent = root
    root.rotation_euler = (0, math.radians(-38), math.radians(-28))
    render('bl_scope')

def void_syringe():
    reset()
    glass = mat('glass', (0.85, 0.8, 0.95), rough=0.05, transmission=1.0)
    fluid = mat('fluid', srgb('#9b2ed6'), rough=0.2, emit=srgb('#c84bff'), emit_strength=2.5)
    metal = mat('metal', srgb('#3a3f4e'), rough=0.3, metal=0.9)
    needle = mat('needle', srgb('#d6dbe6'), rough=0.15, metal=1)
    tend = mat('tend', srgb('#3a1458'), rough=0.35, emit=srgb('#62248c'), emit_strength=0.3)
    tip = mat('tip', srgb('#ff8ef0'), rough=0.3, emit=srgb('#ff6ae8'), emit_strength=3)
    rot = (0, math.radians(90), 0); grp = []
    grp += [cyl(0.24, 1.2, (0, 0, 0), rot, glass, bevel=0.01), cyl(0.18, 0.95, (0.08, 0, 0), rot, fluid)]
    grp += [cyl(0.08, 0.7, (-0.85, 0, 0), rot, metal), cyl(0.2, 0.06, (-1.2, 0, 0), rot, metal), cyl(0.35, 0.05, (-0.62, 0, 0), (0, math.radians(90), 0), metal)]
    grp += [cyl(0.08, 0.2, (0.7, 0, 0), rot, metal), cyl(0.015, 0.7, (1.12, 0, 0), rot, needle, verts=12, bevel=0)]
    for pts in ([(-0.2, -0.24, 0.1), (-0.3, -0.45, 0.3), (-0.15, -0.6, 0.55)], [(0.3, -0.22, -0.12), (0.45, -0.5, -0.3), (0.3, -0.7, -0.5)], [(0.1, -0.2, 0.18), (0.2, -0.35, 0.45), (0.45, -0.4, 0.6)]):
        c = tube_curve(pts, 0.035, tend); grp.append(c)
        grp.append(sphere(0.05, pts[-1], tip))
    for p in ((-0.2, -0.1, 0.08), (0.25, -0.12, -0.05), (0.0, -0.13, -0.08)): grp.append(sphere(0.03, p, mat('b', (1, 0.9, 1), 0.1, emit=(1, 0.8, 1), emit_strength=2)))
    bpy.ops.object.empty_add(location=(0, 0, 0)); root = bpy.context.object
    for o in grp: o.parent = root
    root.rotation_euler = (math.radians(10), math.radians(-40), math.radians(-12))
    render('bl_voidsyringe')

if __name__ == '__main__':
    os.makedirs(OUT, exist_ok=True)
    meat(); umbrella(); scope(); void_syringe()
    print('done')
