"""Loads exported .rpm/.jpg pickups back into Blender, lights them like a game scene, renders a contact sheet.
Usage: /root/tools/bpyenv/bin/python tools/preview_models.py MODELDIR OUT.png [names...]"""
import sys, os, struct, math, glob
import bpy
from PIL import Image
src, outp = sys.argv[1], sys.argv[2]; want = sys.argv[3:]
names = want or sorted(os.path.basename(p)[:-4] for p in glob.glob(os.path.join(src, '*.rpm')))
tiles = []
for name in names:
    bpy.ops.wm.read_factory_settings(use_empty=True)
    sc = bpy.context.scene; sc.render.engine = 'CYCLES'; sc.cycles.device = 'CPU'
    if sc.render.engine == 'CYCLES': sc.cycles.samples = 4; sc.cycles.use_denoising = False
    sc.render.resolution_x = sc.render.resolution_y = 200; sc.render.film_transparent = True
    d = open(os.path.join(src, name + '.rpm'), 'rb').read(); assert d[:4] == b'RPM2'
    vc, ic = struct.unpack_from('<II', d, 4); o = 12
    pos = [tuple(c / 32767 for c in struct.unpack_from('<3h', d, o + 6 * i)) for i in range(vc)]; o += 6 * vc + 3 * vc
    uv = [tuple(c / 65535 for c in struct.unpack_from('<2H', d, o + 4 * i)) for i in range(vc)]; o += 4 * vc
    idx = struct.unpack_from('<%dH' % ic, d, o)
    me = bpy.data.meshes.new(name)
    me.from_pydata([(p[0], p[2], p[1]) for p in pos], [], [idx[i:i + 3] for i in range(0, ic, 3)])
    uvl = me.uv_layers.new()
    for poly in me.polygons:
        poly.use_smooth = True
        for li in poly.loop_indices: uvl.data[li].uv = uv[me.loops[li].vertex_index]
    ob = bpy.data.objects.new(name, me); sc.collection.objects.link(ob); ob.rotation_euler = (0, 0, math.radians(25))
    m = bpy.data.materials.new('m'); m.use_nodes = True; N = m.node_tree.nodes
    t = N.new('ShaderNodeTexImage'); t.image = bpy.data.images.load(os.path.join(src, name + '.jpg'))
    bsdf = N['Principled BSDF']; bsdf.inputs['Roughness'].default_value = 0.9
    m.node_tree.links.new(t.outputs[0], bsdf.inputs['Base Color']); me.materials.append(m)
    np_ = os.path.join(src, name + '_n.png')
    if os.path.exists(np_):
        tn = N.new('ShaderNodeTexImage'); tn.image = bpy.data.images.load(np_); tn.image.colorspace_settings.name = 'Non-Color'
        nm = N.new('ShaderNodeNormalMap'); m.node_tree.links.new(tn.outputs[0], nm.inputs['Color']); m.node_tree.links.new(nm.outputs[0], bsdf.inputs['Normal'])
    ep = os.path.join(src, name + '_e.jpg')
    if os.path.exists(ep):
        te = N.new('ShaderNodeTexImage'); te.image = bpy.data.images.load(ep)
        m.node_tree.links.new(te.outputs[0], bsdf.inputs['Emission Color']); bsdf.inputs['Emission Strength'].default_value = 1.5
    sun = bpy.data.objects.new('sun', bpy.data.lights.new('sun', 'SUN')); sun.data.energy = 3.5; sun.rotation_euler = (math.radians(50), 0, math.radians(-40)); sc.collection.objects.link(sun)
    w = bpy.data.worlds.new('w'); sc.world = w; w.use_nodes = True; w.node_tree.nodes['Background'].inputs[1].default_value = 0.6
    cam = bpy.data.objects.new('cam', bpy.data.cameras.new('cam')); sc.collection.objects.link(cam); sc.camera = cam
    cam.location = (0, -4.2, 1.2); cam.rotation_euler = (math.radians(75), 0, 0)
    p = os.path.join(os.path.dirname(outp), '_prev_' + name + '.png'); sc.render.filepath = p; bpy.ops.render.render(write_still=True)
    tiles.append(p)
cols = min(8, len(tiles)); rows = (len(tiles) + cols - 1) // cols
sheet = Image.new('RGBA', (200 * cols, 200 * rows), (40, 40, 48, 255))
for i, p in enumerate(tiles):
    im = Image.open(p); sheet.paste(im, ((i % cols) * 200, (i // cols) * 200), im); os.remove(p)
sheet.save(outp); print('sheet', outp)
