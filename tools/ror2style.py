"""Blender helpers for a matte, hand-painted item-icon look.
Shading is faked in the material (normal . light -> stepped colour ramp, ambient occlusion in creases,
painterly noise, worn edge highlights) and output as emission, so there are no reflections or shine."""
import math, os, colorsys
import bpy, bmesh
from mathutils import Vector

LIGHT = Vector((-0.55, -0.55, 0.65)).normalized()
BAKE = False   # True: flat ambient shading for texture baking (the game lights the model itself)

def reset(ortho=False):
    bpy.ops.wm.read_factory_settings(use_empty=True)
    sc = bpy.context.scene
    sc.render.engine = 'CYCLES'; sc.cycles.device = 'CPU'
    sc.cycles.samples = 24; sc.cycles.use_denoising = False; sc.cycles.max_bounces = 2
    sc.render.resolution_x = sc.render.resolution_y = 512
    sc.render.film_transparent = True
    sc.view_settings.view_transform = 'Standard'; sc.view_settings.look = 'None'
    w = bpy.data.worlds.new('w'); sc.world = w; w.use_nodes = True
    w.node_tree.nodes['Background'].inputs[1].default_value = 0.0
    cd = bpy.data.cameras.new('cam'); cd.lens = 85
    cam = bpy.data.objects.new('cam', cd); sc.collection.objects.link(cam); sc.camera = cam
    cam.location = (0, -11, 3.2); cam.rotation_euler = (math.radians(74), 0, 0)
    return sc

def _rgb(h):
    h = h.lstrip('#'); return tuple(int(h[i:i + 2], 16) / 255 for i in (0, 2, 4))

def _lin(c): return tuple(((x + 0.055) / 1.055) ** 2.4 if x > 0.04045 else x / 12.92 for x in c)

def _shift(c, dl, dh=0.0, ds=0.0):
    h, l, s = colorsys.rgb_to_hls(*c)
    return colorsys.hls_to_rgb((h + dh) % 1.0, max(0, min(1, l + dl)), max(0, min(1, s + ds)))

def paint(name, base, grime=0.3, wear=0.35, glow=0.0, alpha=1.0, shadow_hue=0.0, steps=3):
    """Matte painted material. base: hex colour. glow adds self-illumination (for void cores, lenses)."""
    c = _rgb(base)
    sh = _shift(c, -0.3, shadow_hue, -0.1)
    shadow = tuple(0.7 * a + 0.3 * b for a, b in zip(sh, (0.10, 0.09, 0.18)))
    mid = c
    light = _shift(c, 0.1, -0.01, 0.0)
    edge = _shift(c, 0.28, -0.02, -0.1)
    m = bpy.data.materials.new(name); m.use_nodes = True; m['alpha'] = alpha
    nt = m.node_tree; N = nt.nodes; Lk = nt.links
    for n in list(N): N.remove(n)
    out = N.new('ShaderNodeOutputMaterial')
    geo = N.new('ShaderNodeNewGeometry')
    dot = N.new('ShaderNodeVectorMath'); dot.operation = 'DOT_PRODUCT'; dot.inputs[1].default_value = Vector((0, 0, 0.25)) if BAKE else LIGHT
    Lk.new(geo.outputs['Normal'], dot.inputs[0])
    mr = N.new('ShaderNodeMapRange'); mr.inputs['From Min'].default_value = -1; mr.inputs['From Max'].default_value = 1
    Lk.new(dot.outputs['Value'], mr.inputs['Value'])
    # painterly wobble on the lighting so steps aren't perfectly clean
    noise = N.new('ShaderNodeTexNoise'); noise.inputs['Scale'].default_value = 7; noise.inputs['Detail'].default_value = 4
    wob = N.new('ShaderNodeMath'); wob.operation = 'MULTIPLY_ADD'
    Lk.new(noise.outputs['Fac'], wob.inputs[0]); wob.inputs[1].default_value = 0.12; wob.inputs[2].default_value = -0.06
    add = N.new('ShaderNodeMath'); add.operation = 'ADD'
    Lk.new(mr.outputs['Result'], add.inputs[0]); Lk.new(wob.outputs['Value'], add.inputs[1])
    ramp = N.new('ShaderNodeValToRGB'); cr = ramp.color_ramp; cr.interpolation = 'EASE'
    cr.elements[0].position = 0.42; cr.elements[0].color = (*_lin(shadow), 1)
    cr.elements[1].position = 0.70; cr.elements[1].color = (*_lin(light), 1)
    e = cr.elements.new(0.50); e.color = (*_lin(mid), 1)
    Lk.new(add.outputs['Value'], ramp.inputs['Fac'])
    # ambient occlusion darkens creases
    ao = N.new('ShaderNodeAmbientOcclusion'); ao.samples = 8; ao.inputs['Distance'].default_value = 0.25
    aom = N.new('ShaderNodeMapRange'); aom.inputs['To Min'].default_value = 0.4
    Lk.new(ao.outputs['AO'], aom.inputs['Value'])
    mul = N.new('ShaderNodeMix'); mul.data_type = 'RGBA'; mul.blend_type = 'MULTIPLY'; mul.inputs['Factor'].default_value = 1
    Lk.new(ramp.outputs['Color'], mul.inputs[6])
    comb = N.new('ShaderNodeCombineColor')
    for i in range(3): Lk.new(aom.outputs['Result'], comb.inputs[i])
    Lk.new(comb.outputs['Color'], mul.inputs[7])
    # grime: large soft noise darkening
    gn = N.new('ShaderNodeTexNoise'); gn.inputs['Scale'].default_value = 5.0; gn.inputs['Detail'].default_value = 8
    gr = N.new('ShaderNodeValToRGB'); gr.color_ramp.elements[0].position = 0.45; gr.color_ramp.elements[1].position = 0.75
    gr.color_ramp.elements[0].color = (*_lin(_shift(c, -0.25, 0.04)), 1); gr.color_ramp.elements[1].color = (1, 1, 1, 1)
    Lk.new(gn.outputs['Fac'], gr.inputs['Fac'])
    gm = N.new('ShaderNodeMix'); gm.data_type = 'RGBA'; gm.blend_type = 'MULTIPLY'; gm.inputs['Factor'].default_value = grime
    Lk.new(mul.outputs[2], gm.inputs[6]); Lk.new(gr.outputs['Color'], gm.inputs[7])
    # worn edge highlights from the bevel node
    bev = N.new('ShaderNodeBevel'); bev.inputs['Radius'].default_value = 0.035
    edged = N.new('ShaderNodeVectorMath'); edged.operation = 'DOT_PRODUCT'
    Lk.new(bev.outputs['Normal'], edged.inputs[0]); Lk.new(geo.outputs['Normal'], edged.inputs[1])
    em = N.new('ShaderNodeMapRange'); em.inputs['From Min'].default_value = 0.985; em.inputs['From Max'].default_value = 0.85
    Lk.new(edged.outputs['Value'], em.inputs['Value'])
    lit = N.new('ShaderNodeMapRange'); Lk.new(dot.outputs['Value'], lit.inputs['Value']); lit.inputs['From Min'].default_value = -0.2
    ew = N.new('ShaderNodeMath'); ew.operation = 'MULTIPLY'; Lk.new(em.outputs['Result'], ew.inputs[0]); Lk.new(lit.outputs['Result'], ew.inputs[1])
    ewf = N.new('ShaderNodeMath'); ewf.operation = 'MULTIPLY'; Lk.new(ew.outputs['Value'], ewf.inputs[0]); ewf.inputs[1].default_value = wear
    edgemix = N.new('ShaderNodeMix'); edgemix.data_type = 'RGBA'
    Lk.new(ewf.outputs['Value'], edgemix.inputs['Factor']); Lk.new(gm.outputs[2], edgemix.inputs[6]); edgemix.inputs[7].default_value = (*_lin(edge), 1)
    br = N.new('ShaderNodeTexNoise'); br.inputs['Scale'].default_value = 28; br.inputs['Detail'].default_value = 2; br.inputs['Distortion'].default_value = 2.5
    brr = N.new('ShaderNodeMapRange'); brr.inputs['To Min'].default_value = 0.88; brr.inputs['To Max'].default_value = 1.08
    Lk.new(br.outputs['Fac'], brr.inputs['Value'])
    brc = N.new('ShaderNodeCombineColor')
    for i in range(3): Lk.new(brr.outputs['Result'], brc.inputs[i])
    brm = N.new('ShaderNodeMix'); brm.data_type = 'RGBA'; brm.blend_type = 'MULTIPLY'; brm.inputs['Factor'].default_value = 1
    Lk.new(edgemix.outputs[2], brm.inputs[6]); Lk.new(brc.outputs['Color'], brm.inputs[7])
    emis = N.new('ShaderNodeEmission'); emis.inputs['Strength'].default_value = 1.0 + glow
    Lk.new(brm.outputs[2], emis.inputs['Color'])
    shader = emis
    if alpha < 1:
        tr = N.new('ShaderNodeBsdfTransparent'); mx = N.new('ShaderNodeMixShader')
        mx.inputs['Fac'].default_value = alpha; Lk.new(tr.outputs[0], mx.inputs[1]); Lk.new(emis.outputs[0], mx.inputs[2]); shader = mx
    Lk.new(shader.outputs[0], out.inputs['Surface'])
    return m

def glowmat(name, color, strength=2.0):
    m = bpy.data.materials.new(name); m.use_nodes = True
    N = m.node_tree.nodes
    for n in list(N): N.remove(n)
    o = N.new('ShaderNodeOutputMaterial'); e = N.new('ShaderNodeEmission')
    e.inputs['Color'].default_value = (*_lin(_rgb(color)), 1); e.inputs['Strength'].default_value = strength
    m.node_tree.links.new(e.outputs[0], o.inputs['Surface']); return m

def smooth(o, bevel=0.0, sub=0, segments=3):
    if bevel: b = o.modifiers.new('bev', 'BEVEL'); b.width = bevel; b.segments = segments; b.limit_method = 'ANGLE'
    if sub: s = o.modifiers.new('sub', 'SUBSURF'); s.levels = s.render_levels = sub
    for p in o.data.polygons: p.use_smooth = True
    return o

def cyl(r, depth, loc=(0, 0, 0), rot=(0, 0, 0), m=None, verts=32, bevel=0.015):
    bpy.ops.mesh.primitive_cylinder_add(radius=r, depth=depth, location=loc, rotation=rot, vertices=verts)
    o = bpy.context.object; o.data.materials.append(m); return smooth(o, bevel)

def sphere(r, loc=(0, 0, 0), m=None, scale=(1, 1, 1)):
    bpy.ops.mesh.primitive_uv_sphere_add(radius=r, location=loc, segments=32, ring_count=16)
    o = bpy.context.object; o.scale = scale; o.data.materials.append(m); return smooth(o)

def tube(points, radius, m, taper=None):
    cd = bpy.data.curves.new('c', 'CURVE'); cd.dimensions = '3D'; cd.bevel_depth = radius; cd.bevel_resolution = 4; cd.use_fill_caps = True
    sp = cd.splines.new('BEZIER'); sp.bezier_points.add(len(points) - 1)
    for i, (p, co) in enumerate(zip(sp.bezier_points, points)):
        p.co = co; p.handle_left_type = p.handle_right_type = 'AUTO'
        if taper: p.radius = taper[i]
    o = bpy.data.objects.new('curve', cd); bpy.context.scene.collection.objects.link(o); o.data.materials.append(m); return o

def group(objs, rot=(0, 0, 0), loc=(0, 0, 0)):
    bpy.ops.object.empty_add(location=(0, 0, 0)); root = bpy.context.object
    for o in objs: o.parent = root
    root.rotation_euler = tuple(math.radians(a) for a in rot); root.location = loc
    return root

def render(path):
    bpy.context.scene.render.filepath = path
    bpy.ops.render.render(write_still=True)
