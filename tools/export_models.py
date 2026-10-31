"""Exports every ror2-plus icon design as a real 3D pickup model.
Per model: join all parts, apply modifiers, decimate, UV unwrap, bake the painted look into a texture.
Writes <OUT>/<Name>.rpm (mesh) and <OUT>/<Name>.jpg (texture). Names match RuntimeArt.PickupModel keys:
item class names for ror2-plus items, Void_<ID> for void items.
Usage: /root/tools/bpyenv/bin/python tools/export_models.py OUTDIR [--items|--voids] [names...] [--shard K/N]
Mesh format (little endian): b'RPM2', u32 vcount, u32 icount, i16[3*v] pos (/32767), i8[3*v] normal (/127),
u16[2*v] uv (/65535), u16[i] tris.
Coordinates are converted to Unity's (x, z, y), longest side scaled to 1.5 units, centred on the bounds."""
import sys, os, math, struct, zlib
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import bpy, bmesh
from mathutils import Vector
import ror2style as R
R.BAKE = True
import blender_icons as bi
import void_bases as vb
from PIL import Image

TEX = 256
MAX_TRIS = 2400
SIZE = 1.5

def prepare():
    sc = bpy.context.scene
    sc.cycles.samples = 12
    # drop see-through parts (glass): an opaque bake would hide what is inside them
    glass = [o for o in sc.objects if o.type in ('MESH', 'CURVE') and o.data.materials and all(m and m.get('alpha', 1.0) < 1.0 for m in o.data.materials)]
    bpy.context.view_layer.update()
    def wbox(o):
        cs = [o.matrix_world @ Vector(c) for c in o.bound_box]
        return Vector((min(c.x for c in cs), min(c.y for c in cs), min(c.z for c in cs))), Vector((max(c.x for c in cs), max(c.y for c in cs), max(c.z for c in cs)))
    boxes = [wbox(o) for o in glass]
    for o in glass: bpy.data.objects.remove(o, do_unlink=True)
    # tiny loose bits that only made sense behind the glass (snowflakes, bubbles) go too
    for o in list(sc.objects):
        if o.type != 'MESH' or max(o.dimensions) > 0.12: continue
        lo, hi = wbox(o); c = (lo + hi) / 2
        if any(all(bl[k] <= c[k] <= bh[k] for k in range(3)) for bl, bh in boxes): bpy.data.objects.remove(o, do_unlink=True)
    objs = [o for o in sc.objects if o.type in ('MESH', 'CURVE')]
    bpy.ops.object.select_all(action='DESELECT')
    for o in objs: o.select_set(True)
    bpy.context.view_layer.objects.active = objs[0]
    bpy.ops.object.convert(target='MESH')                      # applies modifiers, turns curves into meshes
    objs = [o for o in sc.objects if o.type == 'MESH']
    for o in objs:
        mw = o.matrix_world.copy(); o.parent = None; o.matrix_world = mw
    bpy.ops.object.select_all(action='DESELECT')
    for o in objs: o.select_set(True)
    bpy.context.view_layer.objects.active = objs[0]
    if len(objs) > 1: bpy.ops.object.join()
    o = bpy.context.view_layer.objects.active
    bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
    # merge coincident verts per material island, triangulate, decimate if heavy
    bm = bmesh.new(); bm.from_mesh(o.data)
    bmesh.ops.remove_doubles(bm, verts=bm.verts, dist=0.0005)
    bmesh.ops.triangulate(bm, faces=bm.faces)
    bm.to_mesh(o.data); bm.free()
    tris = len(o.data.polygons)
    if tris > MAX_TRIS:
        d = o.modifiers.new('dec', 'DECIMATE'); d.ratio = MAX_TRIS / tris
        bpy.ops.object.modifier_apply(modifier='dec')
    # normalise size and position
    bb = [Vector(c) for c in o.bound_box]
    lo = Vector((min(v.x for v in bb), min(v.y for v in bb), min(v.z for v in bb)))
    hi = Vector((max(v.x for v in bb), max(v.y for v in bb), max(v.z for v in bb)))
    s = SIZE / max(hi - lo)
    me = o.data; c = (lo + hi) / 2
    for v in me.vertices: v.co = (v.co - c) * s
    me.update()
    return o

def _surface_link(m):
    out = next(n for n in m.node_tree.nodes if n.type == 'OUTPUT_MATERIAL')
    return out, (out.inputs['Surface'].links[0].from_socket if out.inputs['Surface'].links else None)

def _is_glow(m):
    return 'alpha' not in m or m.name.endswith('nebula')

def _bake(o, img, kind):
    for m in o.data.materials:
        if not m: continue
        n = m.node_tree.nodes.get('__bake') or m.node_tree.nodes.new('ShaderNodeTexImage'); n.name = '__bake'; n.image = img
        for x in m.node_tree.nodes: x.select = False
        n.select = True; m.node_tree.nodes.active = n
    if kind == 'NORMAL':
        bpy.ops.object.bake(type='NORMAL', normal_space='TANGENT')
    else:
        bpy.ops.object.bake(type='EMIT')

def _save(img, path, jpg=True):
    img.filepath_raw = path + '.png'; img.file_format = 'PNG'; img.save()
    if jpg: Image.open(path + '.png').convert('RGB').save(path, quality=90); os.remove(path + '.png')
    else: os.replace(path + '.png', path)

def unwrap_and_bake(o, base):
    """Vanilla-style texture set: flat painted albedo (no baked key light), tangent normal map with rounded edges
    and a little surface grain, and an emission map for glowing parts. Writes base.jpg, base_n.png, base_e.jpg."""
    bpy.ops.object.select_all(action='DESELECT'); o.select_set(True); bpy.context.view_layer.objects.active = o
    bpy.ops.object.mode_set(mode='EDIT'); bpy.ops.mesh.select_all(action='SELECT')
    bpy.ops.uv.smart_project(angle_limit=math.radians(60), island_margin=0.012)
    bpy.ops.object.mode_set(mode='OBJECT')
    sc = bpy.context.scene; sc.render.bake.margin = 4
    mats = [m for m in o.data.materials if m]
    # albedo
    img = bpy.data.images.new('albedo', TEX, TEX, alpha=False); _bake(o, img, 'EMIT'); _save(img, base + '.jpg')
    # emission: glowing materials keep their light, everything else bakes black
    if any(_is_glow(m) for m in mats):
        saved = {}
        for m in mats:
            out, src = _surface_link(m); saved[m.name] = src
            if not _is_glow(m):
                e = m.node_tree.nodes.new('ShaderNodeEmission'); e.name = '__black'; e.inputs['Color'].default_value = (0, 0, 0, 1)
                m.node_tree.links.new(e.outputs[0], out.inputs['Surface'])
        img = bpy.data.images.new('emission', TEX, TEX, alpha=False); _bake(o, img, 'EMIT'); _save(img, base + '_e.jpg')
        for m in mats:
            out, _ = _surface_link(m)
            if saved[m.name] is not None: m.node_tree.links.new(saved[m.name], out.inputs['Surface'])
    # normal: bevelled edges + fine grain, through a temporary BSDF
    for m in mats:
        N = m.node_tree.nodes; L = m.node_tree.links; out, _ = _surface_link(m)
        bev = N.new('ShaderNodeBevel'); bev.inputs['Radius'].default_value = 0.03; bev.samples = 8
        noise = N.new('ShaderNodeTexNoise'); noise.inputs['Scale'].default_value = 40; noise.inputs['Detail'].default_value = 3
        bump = N.new('ShaderNodeBump'); bump.inputs['Strength'].default_value = 0.06
        L.new(noise.outputs['Fac'], bump.inputs['Height']); L.new(bev.outputs['Normal'], bump.inputs['Normal'])
        bsdf = N.new('ShaderNodeBsdfDiffuse'); L.new(bump.outputs['Normal'], bsdf.inputs['Normal']); L.new(bsdf.outputs[0], out.inputs['Surface'])
    img = bpy.data.images.new('normal', TEX, TEX, alpha=False); img.colorspace_settings.name = 'Non-Color'
    _bake(o, img, 'NORMAL'); _save(img, base + '_n.png', jpg=False)

def q(x, lo, hi, scale): return max(lo, min(hi, int(round(x * scale))))
def encode(verts, tris):
    out = [b'RPM2', struct.pack('<II', len(verts), len(tris) * 3)]
    out += [struct.pack('<3h', *(q(c, -32767, 32767, 32767) for c in p)) for p, _, _ in verts]
    out += [struct.pack('<3b', *(q(c, -127, 127, 127) for c in n)) for _, n, _ in verts]
    out += [struct.pack('<2H', *(q(c, 0, 65535, 65535) for c in uv)) for _, _, uv in verts]
    out += [struct.pack('<3H', *t) for t in tris]
    return b''.join(out)

def write_mesh(o, path):
    me = o.data
    me.calc_loop_triangles()
    uvl = me.uv_layers.active.data
    cn = me.corner_normals
    verts, index, tris = [], {}, []
    for t in me.loop_triangles:
        tri = []
        for li in t.loops:
            l = me.loops[li]; p = me.vertices[l.vertex_index].co
            n = cn[li].vector if t.use_smooth else t.normal
            uv = uvl[li].uv
            key = (round(p.x, 5), round(p.y, 5), round(p.z, 5), round(n.x, 3), round(n.y, 3), round(n.z, 3), round(uv.x, 5), round(uv.y, 5))
            if key not in index:
                index[key] = len(verts); verts.append(((p.x, p.z, p.y), (n.x, n.z, n.y), (uv.x, uv.y)))
            tri.append(index[key])
        tris.append(tri)
    if len(verts) >= 65536: raise RuntimeError('too many vertices: %d' % len(verts))
    with open(path, 'wb') as f:
        f.write(encode(verts, tris))
    return len(verts), len(tris)

def export(name, build, out):
    bi.reset(); build()
    o = prepare()
    unwrap_and_bake(o, os.path.join(out, name))
    v, t = write_mesh(o, os.path.join(out, name + '.rpm'))
    print(f'OK {name}: {v} verts, {t} tris', flush=True)

def jobs(mode):
    from void_items import ROWS
    if mode in ('all', 'items'):
        for name, fn in bi.ITEMS.items(): yield name, fn
    if mode in ('all', 'voids'):
        for row in ROWS:
            vid, tier = row[0], row[2]
            def build(vid=vid, tier=tier):
                bi.VOIDMODE = True
                try: vb.BASES[vid](); vb.corrupt(vid, tier)
                finally: bi.VOIDMODE = False
            yield 'Void_' + vid, build

if __name__ == '__main__':
    args = sys.argv[1:]; out = args.pop(0); mode = 'all'; shard = (0, 1)
    if args and args[0] in ('--items', '--voids'): mode = args.pop(0)[2:]
    if '--shard' in args:
        i = args.index('--shard'); k, n = args[i + 1].split('/'); shard = (int(k), int(n)); del args[i:i + 2]
    want = set(args)
    os.makedirs(out, exist_ok=True)
    for idx, (name, build) in enumerate(jobs(mode)):
        if want and name not in want: continue
        if idx % shard[1] != shard[0]: continue
        if os.path.exists(os.path.join(out, name + '.rpm')) and os.path.exists(os.path.join(out, name + '.jpg')): continue
        try: export(name, build, out)
        except Exception:
            import traceback; print('FAILED', name, flush=True); traceback.print_exc()
    print('done')
