"""All ror2-plus item icons, modelled in Blender and shaded with the matte painted materials from ror2style.py.
Every design is original. Usage:
  /root/tools/bpyenv/bin/python tools/blender_icons.py OUTDIR [Name ...]     (items; all if none given)
  /root/tools/bpyenv/bin/python tools/blender_icons.py OUTDIR --voids [ID ...]"""
import sys, os, math, random, zlib
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import bpy, bmesh
from mathutils import Vector
import ror2style as R

D = math.radians
PAL = {
    'metal': ('#4a5262', {}), 'dark': ('#2a2f39', {}), 'steel': ('#8a93a3', {'wear': 0.6}), 'brass': ('#b8893a', {'wear': 0.55}),
    'gold': ('#d2a23c', {'wear': 0.6}), 'copper': ('#b0663a', {'wear': 0.5}), 'wood': ('#6e4426', {}), 'darkwood': ('#4a2c18', {}),
    'leather': ('#5c3c26', {}), 'rubber': ('#34302e', {}), 'bone': ('#d6c8aa', {}), 'flesh': ('#9c3a2a', {}), 'fat': ('#e0b8a2', {'grime': 0.15}),
    'pink': ('#c8707a', {}), 'pink2': ('#a8505e', {}), 'burgundy': ('#6e1e30', {}), 'red': ('#b0362c', {}), 'darkred': ('#6e1e1e', {}), 'orange': ('#d0702a', {}), 'yellow': ('#d6ae38', {}),
    'olive': ('#5e6a34', {}), 'green': ('#4f8a36', {}), 'lime': ('#93c045', {}), 'teal': ('#2f7a7a', {}), 'blue': ('#3a6aa8', {}),
    'navy': ('#253a66', {}), 'purple': ('#6a3a9a', {}), 'violet': ('#8a4ab8', {}), 'cream': ('#d9cfb4', {'grime': 0.2}),
    'paper': ('#e2d8bc', {'grime': 0.25}), 'white': ('#d8d6d0', {}), 'black': ('#26252c', {}), 'stone': ('#bdb8ae', {'grime': 0.35}),
    'glass': ('#9fb6c8', {'grime': 0.0, 'wear': 0.8, 'alpha': 0.35}), 'pglass': ('#8a7aa8', {'grime': 0.0, 'wear': 0.8, 'alpha': 0.35}),
    'clothg': ('#4f7a3e', {}), 'clothg2': ('#3a5c2c', {}), 'clothp': ('#5a2f86', {}), 'egg': ('#e4dcc4', {'grime': 0.2}),
    'chitin': ('#3a5a28', {}), 'chitin2': ('#26401a', {}), 'brain': ('#b8a07a', {'grime': 0.35}),
    'vdark': ('#2a1240', {'shadow_hue': 0.0}), 'vmid': ('#5a2484', {}), 'vflesh': ('#7a3a8a', {}), 'vshell': ('#3d2a5e', {}), 'vpale': ('#d8b8e8', {}),
}
GLOW = {'gcyan': ('#62d6f0', 1.6), 'gred': ('#ff4a3a', 2.0), 'gyellow': ('#ffd84a', 2.0), 'gorange': ('#ff8a2a', 2.2), 'ggreen': ('#9cff4a', 1.8),
        'gpurple': ('#c04aff', 2.0), 'gpink': ('#ff7ae8', 2.4), 'gwhite': ('#fff4e0', 1.8), 'gblue': ('#5a9aff', 2.0)}
_cache = {}
VOIDMODE = False

def _voidify(h):
    """Pull a colour toward the void palette while keeping some of its value contrast and hue."""
    import colorsys
    h = h.lstrip('#'); c = [int(h[i:i + 2], 16) / 255 for i in (0, 2, 4)]
    hh, l, s_ = colorsys.rgb_to_hls(*c)
    vh = 0.70 + (((hh - 0.70 + 0.5) % 1.0) - 0.5) * 0.15   # lavender/indigo like vanilla void items, a hint of the original hue
    vl = 0.3 + l * 0.58                        # keep relative lightness so parts stay readable
    vs = 0.45 + s_ * 0.25
    r, g, b = colorsys.hls_to_rgb(vh % 1.0, vl, vs)
    return '#%02x%02x%02x' % (int(r * 255), int(g * 255), int(b * 255))

def M(k):
    key = ('v_' if VOIDMODE else '') + k
    if key in _cache: return _cache[key]
    if VOIDMODE and k in GLOW and k not in ('gpink', 'gpurple'):
        m = R.glowmat(key, *GLOW['gpink' if sum(map(ord, k)) % 2 else 'gpurple'])
    elif k in GLOW: m = R.glowmat(key, *GLOW[k])
    else:
        hexc, kw = PAL[k]
        m = R.paint(key, _voidify(hexc) if VOIDMODE and not k.startswith('v') else hexc, **kw)
    _cache[key] = m; return m

def reset():
    _cache.clear(); R.reset()

# ---------------------------------------------------------------- part helpers
def C(r, h, loc=(0, 0, 0), rot=(0, 0, 0), m='metal', v=32, bev=0.015):
    return R.cyl(r, h, loc, tuple(D(a) for a in rot), M(m), verts=v, bevel=bev)

def S(r, loc=(0, 0, 0), m='metal', sc=(1, 1, 1)):
    return R.sphere(r, loc, M(m), sc)

def B(size, loc=(0, 0, 0), rot=(0, 0, 0), m='metal', bev=0.04):
    bpy.ops.mesh.primitive_cube_add(size=1, location=loc, rotation=tuple(D(a) for a in rot))
    o = bpy.context.object; o.scale = size; bpy.ops.object.transform_apply(scale=True)
    o.data.materials.append(M(m)); return R.smooth(o, bevel=bev, segments=3)

def K(r1, r2, h, loc=(0, 0, 0), rot=(0, 0, 0), m='metal', v=32, bev=0.0):
    bpy.ops.mesh.primitive_cone_add(radius1=r1, radius2=r2, depth=h, location=loc, rotation=tuple(D(a) for a in rot), vertices=v)
    o = bpy.context.object; o.data.materials.append(M(m)); return R.smooth(o, bevel=bev)

def T(R_, r, loc=(0, 0, 0), rot=(0, 0, 0), m='metal'):
    bpy.ops.mesh.primitive_torus_add(major_radius=R_, minor_radius=r, location=loc, rotation=tuple(D(a) for a in rot), major_segments=48, minor_segments=16)
    o = bpy.context.object; o.data.materials.append(M(m)); return R.smooth(o)

def Tu(pts, r, m='metal', taper=None, flat=None):
    o = R.tube(pts, r, M(m), taper)
    if flat: o.scale = flat
    return o

def Blob(r, loc=(0, 0, 0), sc=(1, 1, 1), m='flesh', amt=0.18, noise=0.7, kind='CLOUDS', sub=1, seed=0):
    bpy.ops.mesh.primitive_ico_sphere_add(radius=r, subdivisions=4, location=loc)
    o = bpy.context.object; o.scale = sc
    t = bpy.data.textures.new('t', kind); t.noise_scale = noise
    if hasattr(t, 'noise_basis'): pass
    d = o.modifiers.new('d', 'DISPLACE'); d.texture = t; d.strength = amt; d.texture_coords = 'OBJECT'
    if seed:
        e = bpy.data.objects.new('off', None); bpy.context.scene.collection.objects.link(e); e.location = (seed * 1.7, seed * 0.3, -seed); d.texture_coords_object = e
    o.data.materials.append(M(m)); return R.smooth(o, sub=sub)

def Gem(r, loc=(0, 0, 0), m='gcyan', sc=(1, 1, 1.4)):
    bpy.ops.mesh.primitive_ico_sphere_add(radius=r, subdivisions=1, location=loc)
    o = bpy.context.object; o.scale = sc; o.data.materials.append(M(m)); return o

def helix(r, h, turns, n=60, z0=0.0, axis='z'):
    pts = []
    for i in range(n + 1):
        t = i / n; a = t * turns * 2 * math.pi
        p = (math.cos(a) * r, math.sin(a) * r, z0 + t * h)
        pts.append(p if axis == 'z' else (p[2], p[1], p[0]))
    return pts

def ring_pts(R_, n=8, z=0.0, start=0.0):
    return [(math.cos(start + 2 * math.pi * i / n) * R_, math.sin(start + 2 * math.pi * i / n) * R_, z) for i in range(n)]

def G(parts, rot=(0, 0, 0), loc=(0, 0, 0)):
    return R.group([p for p in parts if p], rot, loc)

def zigzag(a, b, n=6, amp=0.12, seed=1):
    r = random.Random(seed); pts = []
    for i in range(n + 1):
        t = i / n; p = [a[k] + (b[k] - a[k]) * t for k in range(3)]
        if 0 < i < n: p = [p[0] + r.uniform(-amp, amp), p[1] + r.uniform(-amp, amp) * 0.3, p[2] + r.uniform(-amp, amp)]
        pts.append(tuple(p))
    return pts

# ---------------------------------------------------------------- items
ITEMS = {}
def item(fn): ITEMS[fn.__name__] = fn; return fn

@item
def MeatNugget():
    p = [Blob(0.8, sc=(1.15, 0.85, 0.8), m='flesh', amt=0.22)]
    for pts, r in (([(-0.85, -0.4, 0.2), (-0.4, -0.64, 0.32), (0.1, -0.67, 0.28), (0.55, -0.5, 0.12)], 0.07),
                   ([(-0.6, -0.5, -0.25), (-0.1, -0.66, -0.15), (0.4, -0.56, -0.25)], 0.055),
                   ([(-0.2, -0.38, 0.6), (0.25, -0.44, 0.56), (0.6, -0.26, 0.42)], 0.05)):
        p.append(Tu(pts, r, 'fat', flat=(1, 0.55, 1)))
    p += [C(0.13, 1.1, (0.95, 0, -0.35), (0, 62, 0), 'bone'), S(0.17, (1.42, -0.1, -0.62), 'bone'), S(0.17, (1.38, 0.13, -0.5), 'bone')]
    G(p, (10, 8, 18))

@item
def Taser():
    p = [B((0.55, 0.32, 1.0), (0, 0, -0.15), m='dark', bev=0.1), B((0.58, 0.35, 0.22), (0, 0, 0.25), m='yellow', bev=0.06),
         B((0.3, 0.06, 0.3), (0, -0.17, -0.25), m='metal', bev=0.03), C(0.06, 0.04, (0, -0.2, -0.55), (90, 0, 0), 'gred'),
         C(0.045, 0.55, (-0.14, 0, 0.62), m='steel', v=16), C(0.045, 0.55, (0.14, 0, 0.62), m='steel', v=16),
         S(0.06, (-0.14, 0, 0.9), 'steel'), S(0.06, (0.14, 0, 0.9), 'steel')]
    p.append(Tu(zigzag((-0.12, -0.05, 0.92), (0.12, -0.05, 0.92), 4, 0.08), 0.022, 'gcyan'))
    p.append(Tu([(0.0, 0.05, -0.65), (0.25, 0.1, -0.95), (0.05, 0.1, -1.2), (-0.25, 0.08, -0.95)], 0.03, 'leather'))
    G(p, (0, 0, -25), (0, 0, 0.1))

@item
def Spikestrip():
    p = [B((2.2, 0.5, 0.12), (0, 0, -0.1), m='dark', bev=0.04)]
    for i in range(7):
        p.append(B((0.16, 0.52, 0.125), (-0.95 + i * 0.32, 0, -0.1), m='yellow' if i % 2 == 0 else 'black', bev=0.02))
    for i in range(7):
        for y in (-0.12, 0.12):
            p.append(K(0.07, 0.0, 0.32, (-0.95 + i * 0.32, y, 0.11), m='steel', v=12))
    p += [T(0.1, 0.03, (1.22, 0, -0.1), (90, 0, 0), 'steel'), T(0.1, 0.03, (1.38, 0, -0.1), (0, 0, 0), 'steel')]
    G(p, (20, 0, -25))

@item
def HermitsScarf():
    p = [T(0.55, 0.2, (0, 0, 0.35), (75, 0, 0), 'clothg'), T(0.55, 0.06, (0, -0.06, 0.48), (75, 0, 0), 'clothg2'),
         T(0.55, 0.06, (0, 0.06, 0.22), (75, 0, 0), 'clothg2')]
    p.append(Tu([(0.25, -0.35, 0.1), (0.35, -0.45, -0.4), (0.25, -0.4, -0.9)], 0.16, 'clothg', flat=(1, 0.4, 1)))
    p.append(Tu([(0.0, -0.38, 0.05), (-0.1, -0.5, -0.45), (-0.05, -0.45, -0.75)], 0.15, 'clothg2', flat=(1, 0.4, 1)))
    for i in range(5):
        p.append(C(0.018, 0.14, (0.15 + i * 0.05, -0.17, -0.98), m='cream', v=8))
        p.append(C(0.018, 0.14, (-0.15 + i * 0.05, -0.19, -0.83), m='cream', v=8))
    G(p, (5, 0, 15))

@item
def BarbedWire():
    pts = helix(0.55, 0.0, 1.0, 40)
    pts = [(x, y, 0.18 * math.sin(i / 40 * 4 * math.pi)) for i, (x, y, z) in enumerate(pts)]
    p = [Tu(pts, 0.045, 'steel'), Tu([(x * 0.85, y * 0.85, -z) for x, y, z in pts], 0.04, 'metal')]
    for i in range(0, 40, 5):
        x, y, z = pts[i]
        p.append(C(0.018, 0.28, (x, y, z), (45, 30 * i, 0), 'steel', v=8))
        p.append(C(0.018, 0.28, (x, y, z), (-45, 30 * i, 90), 'steel', v=8))
    G(p, (65, 0, 15))

@item
def VoltaicMitt():
    p = [Blob(0.42, (0, 0, 0), (1, 0.55, 1.05), 'blue', amt=0.04), C(0.36, 0.35, (0, 0, -0.6), m='brass', bev=0.04),
         T(0.37, 0.05, (0, 0, -0.5), m='copper'), T(0.37, 0.05, (0, 0, -0.68), m='copper')]
    for i, x in enumerate((-0.27, -0.09, 0.09, 0.27)):
        L = (0.45, 0.55, 0.52, 0.42)[i]
        p.append(Tu([(x, 0, 0.35), (x * 1.05, -0.05, 0.35 + L * 0.6), (x * 1.08, -0.15, 0.35 + L)], 0.1, 'blue'))
        p.append(S(0.085, (x * 1.08, -0.2, 0.35 + L), 'copper'))
    p.append(Tu([(-0.4, 0, 0.0), (-0.6, -0.08, 0.25), (-0.62, -0.18, 0.45)], 0.11, 'blue'))
    for k in range(3):
        p.append(Tu(zigzag((0.1 - k * 0.2, -0.3, 0.2), (0.3 - k * 0.25, -0.35, 1.05), 5, 0.1, seed=k + 3), 0.02, 'gcyan'))
    G(p, (5, 0, -15))

@item
def TimeKeepersSecret():
    p = [C(0.62, 0.1, (0, 0, 0.82), m='darkwood', bev=0.03), C(0.62, 0.1, (0, 0, -0.82), m='darkwood', bev=0.03)]
    for x, y, _ in ring_pts(0.52, 3, 0, 0.5):
        p.append(C(0.045, 1.55, (x, y, 0), m='brass', v=12))
    p += [S(0.42, (0, 0, 0.38), 'glass', (1, 1, 0.95)), S(0.42, (0, 0, -0.38), 'glass', (1, 1, 0.95)),
          K(0.3, 0.02, 0.32, (0, 0, -0.55), m='gyellow'), K(0.25, 0.0, 0.25, (0, 0, 0.32), (180, 0, 0), 'gyellow'), C(0.02, 0.5, (0, 0, -0.1), m='gyellow')]
    G(p, (10, 0, 12))

@item
def ChargefieldGenerator():
    p = [C(0.6, 0.25, (0, 0, -0.7), m='dark', bev=0.05), C(0.45, 0.2, (0, 0, -0.5), m='metal', bev=0.04), C(0.08, 0.9, (0, 0, -0.05), m='steel'),
         T(0.42, 0.045, (0, 0, 0.15), m='gcyan'), T(0.28, 0.04, (0, 0, 0.45), m='gcyan'), S(0.13, (0, 0, 0.55), 'gwhite')]
    for x, y, z in ring_pts(0.55, 4, -0.7, 0.4):
        p.append(B((0.12, 0.3, 0.3), (x, y, z), (0, 0, math.degrees(math.atan2(y, x))), 'yellow', bev=0.03))
    for k in range(3):
        a = k * 2.1
        p.append(Tu(zigzag((0.08 * math.cos(a), 0.08 * math.sin(a), 0.2), (0.4 * math.cos(a), 0.4 * math.sin(a), 0.15), 4, 0.06, seed=k), 0.018, 'gcyan'))
    G(p, (12, 0, 0))

@item
def ConcussionGrenade():
    p = [C(0.38, 1.0, (0, 0, -0.1), m='olive', bev=0.08)]
    for z in (-0.45, -0.1, 0.25): p.append(T(0.385, 0.03, (0, 0, z), m='olive'))
    p += [C(0.39, 0.12, (0, 0, 0.05), m='yellow', bev=0.02), C(0.2, 0.18, (0, 0, 0.5), m='metal'), C(0.12, 0.12, (0, 0, 0.62), m='steel'),
          B((0.1, 0.16, 0.65), (0.27, 0, 0.25), (0, -12, 0), 'steel', bev=0.02), T(0.13, 0.03, (-0.25, 0, 0.62), (0, 70, 0), 'steel')]
    G(p, (10, 0, -20))

@item
def DeadMansFoot():
    p = [C(0.3, 0.85, (0, 0, 0.25), m='leather', bev=0.06), B((0.95, 0.6, 0.38), (0.25, 0, -0.32), m='leather', bev=0.17),
         B((1.0, 0.64, 0.1), (0.25, 0, -0.52), m='rubber', bev=0.04), T(0.31, 0.04, (0, 0, 0.65), m='darkwood')]
    for z in (0.0, 0.2, 0.4): p.append(Tu([(0.2, -0.28, z), (0.32, -0.2, z + 0.05)], 0.018, 'cream'))
    for loc, r in (((-0.1, -0.3, 0.1), 0.1), ((0.45, -0.3, -0.18), 0.12), ((0.25, -0.31, -0.45), 0.08), ((-0.25, -0.2, 0.55), 0.07)):
        p.append(Blob(r, loc, (1, 0.6, 1.3), 'ggreen', amt=0.03))
    p.append(Tu([(0.45, -0.32, -0.3), (0.47, -0.33, -0.6), (0.46, -0.3, -0.72)], 0.03, 'ggreen', taper=[1, 0.7, 0.4]))
    G(p, (5, 0, 20))

@item
def PanicMines():
    def mine(x, z, s, col):
        return [C(0.5 * s, 0.16 * s, (x, 0, z), m='dark', bev=0.05), C(0.38 * s, 0.1 * s, (x, 0, z + 0.1 * s), m=col, bev=0.03),
                C(0.12 * s, 0.08 * s, (x, 0, z + 0.17 * s), m='gred'), T(0.42 * s, 0.025 * s, (x, 0, z + 0.06 * s), m='steel')]
    p = mine(-0.25, 0.1, 1.0, 'olive') + mine(0.55, -0.35, 0.75, 'metal')
    for x, y, z in ring_pts(0.47, 8, 0.1):
        p.append(C(0.03, 0.08, (x - 0.25, y, z), m='steel', v=8))
    G(p, (40, 0, 10))

@item
def ProphetsCape():
    bpy.ops.mesh.primitive_cone_add(radius1=0.85, radius2=0.25, depth=1.5, vertices=48, location=(0, 0, -0.2))
    o = bpy.context.object; bm = bmesh.new(); bm.from_mesh(o.data)
    for v in bm.verts:
        a = math.atan2(v.co.y, v.co.x)
        if v.co.z < 0: v.co.z += 0.08 * math.sin(a * 6); v.co.x *= 1 + 0.06 * math.sin(a * 5)
    bmesh.ops.delete(bm, geom=[f for f in bm.faces if f.normal.z > 0.9 or f.normal.z < -0.9], context='FACES')
    bm.to_mesh(o.data); bm.free(); o.data.materials.append(M('clothp'))
    s = o.modifiers.new('s', 'SOLIDIFY'); s.thickness = 0.06; R.smooth(o, sub=1)
    p = [o, S(0.38, (0, 0.05, 0.75), 'clothp', (1, 1, 1.1)), S(0.27, (0, -0.12, 0.7), 'black', (1, 0.6, 1.1)),
         T(0.86, 0.04, (0, 0, -0.93), m='gold'), Gem(0.1, (0, -0.33, 0.42), 'gyellow'), T(0.11, 0.03, (0, -0.32, 0.42), (90, 0, 0), 'gold')]
    G(p, (5, 0, 15))

@item
def Insecticide():
    p = [C(0.36, 1.1, (0, 0, -0.15), m='white', bev=0.06), C(0.365, 0.45, (0, 0, -0.15), m='green', bev=0.01),
         K(0.36, 0.18, 0.22, (0, 0, 0.51), m='steel'), C(0.16, 0.2, (0, 0, 0.7), m='red', bev=0.03),
         B((0.12, 0.3, 0.08), (0, -0.18, 0.78), m='red', bev=0.03), S(0.09, (0, -0.37, -0.15), 'yellow', (1, 0.3, 1))]
    for i, (x, z) in enumerate(((-0.08, 0.83), (-0.25, 0.9), (-0.1, 1.0), (-0.32, 1.05), (-0.18, 1.15))):
        p.append(S(0.05 + 0.015 * i, (x, -0.45 - i * 0.05, z), 'ggreen'))
    G(p, (5, 0, 15))

@item
def DecayingSample():
    p = [C(0.38, 1.0, (0, 0, -0.1), m='glass', bev=0.04), C(0.32, 0.75, (0, 0, -0.2), m='ggreen', bev=0.02),
         C(0.42, 0.18, (0, 0, 0.47), m='metal', bev=0.04), C(0.2, 0.1, (0, 0, 0.6), m='steel'), C(0.4, 0.15, (0, 0, -0.62), m='metal', bev=0.04),
         C(0.385, 0.3, (0, 0, -0.05), m='paper', bev=0.0)]
    for loc, r in (((0.1, -0.1, 0.1), 0.1), ((-0.12, -0.05, -0.3), 0.08), ((0.05, 0.1, -0.4), 0.07)):
        p.append(Blob(r, loc, m='darkred', amt=0.04))
    G(p, (8, 0, -12))

@item
def FilialImprinting():
    p = [Blob(0.55, (0, 0, 0.1), (0.85, 0.85, 1.15), 'egg', amt=0.02)]
    for loc in ((0.2, -0.42, 0.3), (-0.25, -0.38, 0.0), (0.05, -0.47, -0.2), (0.3, -0.3, -0.25), (-0.15, -0.35, 0.5)):
        p.append(S(0.06, loc, 'leather', (1, 0.4, 0.8)))
    p.append(Tu(zigzag((-0.3, -0.45, 0.45), (0.25, -0.45, 0.6), 5, 0.06, seed=4), 0.015, 'black'))
    for k in range(9):
        a = k / 9 * 2 * math.pi
        p.append(Tu([(math.cos(a) * 0.6, math.sin(a) * 0.6, -0.55), (math.cos(a + 0.8) * 0.66, math.sin(a + 0.8) * 0.66, -0.48), (math.cos(a + 1.6) * 0.6, math.sin(a + 1.6) * 0.6, -0.58)], 0.035, 'wood'))
    G(p, (8, 0, 0))

@item
def ToxicCentipede():
    pts = [(-1.0 + i * 0.28, 0, 0.25 * math.sin(i * 0.9)) for i in range(8)]
    p = []
    for i, (x, y, z) in enumerate(pts):
        r = 0.2 if i < 7 else 0.23
        p.append(S(r, (x, y, z), 'chitin' if i % 2 else 'chitin2', (1.1, 1, 0.85)))
        if i < 7:
            for sgn in (-1, 1):
                p.append(Tu([(x, 0, z), (x + 0.05, sgn * 0.35, z - 0.15), (x + 0.1, sgn * 0.45, z - 0.35)], 0.025, 'chitin2'))
    hx, hy, hz = pts[-1]
    p += [S(0.05, (hx + 0.12, -0.15, hz + 0.08), 'gyellow'), S(0.05, (hx + 0.12, 0.15, hz + 0.08), 'gyellow'),
          Tu([(hx + 0.1, -0.08, hz + 0.15), (hx + 0.35, -0.2, hz + 0.45)], 0.015, 'chitin2'), Tu([(hx + 0.1, 0.08, hz + 0.15), (hx + 0.35, 0.2, hz + 0.45)], 0.015, 'chitin2')]
    for i in range(0, 7, 2): p.append(S(0.06, (pts[i][0], -0.18, pts[i][2] + 0.1), 'ggreen'))
    G(p, (15, 0, 10))

@item
def OldBox():
    p = [B((1.0, 1.0, 0.85), (0, 0, -0.35), m='wood', bev=0.04), B((1.04, 1.04, 0.08), (0, 0, 0.1), m='brass', bev=0.02)]
    for x in (-0.5, 0.5):
        for y in (-0.5, 0.5): p.append(B((0.12, 0.12, 0.88), (x, y, -0.35), m='brass', bev=0.02))
    p += [Tu(helix(0.12, 0.45, 5, 80, 0.12), 0.025, 'steel'), S(0.3, (0, 0, 0.85), 'cream'),
          K(0.22, 0.0, 0.4, (0, 0, 1.25), (0, 15, 0), 'red'), S(0.05, (0.1, 1.38, 0), 'gyellow'),
          S(0.06, (-0.1, -0.27, 0.9), 'black'), S(0.06, (0.1, -0.27, 0.9), 'black'), T(0.1, 0.025, (0, -0.25, 0.75), (70, 0, 0), 'darkred'),
          C(0.03, 0.3, (0.6, 0, -0.3), (0, 90, 0), 'steel'), B((0.05, 0.05, 0.3), (0.75, 0, -0.2), m='steel'), S(0.06, (0.75, 0, -0.03), 'red')]
    G(p, (10, 0, -25), (0, 0, -0.15))

@item
def BeatingEmbryo():
    p = [Blob(0.62, (0, 0, 0), (0.9, 0.85, 1.1), 'pink', amt=0.08), Blob(0.3, (0.1, -0.15, 0.55), (1, 1, 0.9), 'pink', amt=0.04),
         S(0.07, (0.12, -0.42, 0.62), 'black'), S(0.16, (0, -0.35, 0.0), 'gred')]
    for k in range(5):
        a = k * 1.3
        p.append(Tu([(0.5 * math.cos(a), -0.35, 0.5 * math.sin(a)), (0.25 * math.cos(a + 0.4), -0.5, 0.25 * math.sin(a + 0.4)), (0.05, -0.55, 0.02)], 0.018, 'darkred'))
    p.append(Tu([(-0.3, 0, -0.55), (-0.6, -0.1, -0.8), (-0.4, -0.2, -1.1), (-0.7, -0.1, -1.25)], 0.06, 'pink', taper=[1, 0.9, 0.8, 0.6]))
    G(p, (5, 0, 10))

@item
def AtgMk2():
    p = [C(0.22, 1.6, (0, 0, 0), m='white', bev=0.03), K(0.22, 0.0, 0.5, (0, 0, 1.05), m='red'), C(0.225, 0.12, (0, 0, 0.55), m='red', bev=0.01),
         C(0.225, 0.08, (0, 0, -0.3), m='black', bev=0.01), C(0.16, 0.12, (0, 0, -0.86), m='metal'), K(0.14, 0.22, 0.3, (0, 0, -1.05), m='gorange')]
    for k in range(4):
        a = k * 90 + 45
        p.append(B((0.04, 0.32, 0.4), (0.28 * math.cos(D(a)), 0.28 * math.sin(D(a)), -0.6), (0, 0, a + 90), 'red', bev=0.015))
    G(p, (0, 0, 0)).rotation_euler = (D(15), D(-45), D(-20))

@item
def PlasmaChain():
    p = []
    for i in range(6):
        x = -0.95 + i * 0.32
        p.append(T(0.2, 0.06, (x, 0, x * 0.4), (90 * (i % 2), 0, 0), 'steel'))
    p += [S(0.18, (0.98, 0, 0.42), 'gcyan'), S(0.1, (0.98, 0, 0.42), 'gwhite')]
    for k in range(4):
        p.append(Tu(zigzag((0.98, 0, 0.42), (0.98 + 0.4 * math.cos(k * 1.6), -0.1, 0.42 + 0.4 * math.sin(k * 1.6)), 4, 0.06, seed=k), 0.016, 'gcyan'))
    G(p, (0, 0, -15))

@item
def HeavenCracker():
    p = [K(0.28, 0.0, 1.5, (0, 0, 0.55), m='gold', v=48), Tu(helix(0.2, 1.25, 4, 100, -0.15), 0.035, 'brass', taper=[1.2 - 0.9 * i / 100 for i in range(101)]),
         C(0.36, 0.12, (0, 0, -0.25), m='brass', bev=0.04), C(0.1, 0.7, (0, 0, -0.65), m='leather', bev=0.02)]
    for z in (-0.45, -0.6, -0.75, -0.9): p.append(T(0.105, 0.02, (0, 0, z), m='darkwood'))
    p += [S(0.12, (0, 0, -1.03), 'brass'), S(0.09, (0, 0, 1.32), 'gyellow')]
    G(p, (0, -35, 0))

@item
def RapidMitosis():
    p = [Blob(0.5, (-0.35, 0, 0), (1, 0.9, 0.95), 'violet', amt=0.05), Blob(0.48, (0.38, 0, 0.05), (1, 0.9, 0.95), 'violet', amt=0.05),
         S(0.16, (-0.38, -0.38, 0.05), 'gpink'), S(0.15, (0.4, -0.37, 0.1), 'gpink'), S(0.25, (0.02, -0.05, 0.02), 'violet', (0.8, 0.9, 0.8))]
    for loc in ((-0.6, -0.35, 0.3), (-0.2, -0.42, -0.25), (0.6, -0.38, -0.2), (0.25, -0.42, 0.35)): p.append(S(0.04, loc, 'vpale'))
    G(p, (0, 0, 15))

@item
def RepulsionArmor():
    bpy.ops.mesh.primitive_uv_sphere_add(radius=0.75, segments=48, ring_count=24)
    o = bpy.context.object; o.scale = (1.0, 0.55, 1.15)
    bm = bmesh.new(); bm.from_mesh(o.data)
    bmesh.ops.delete(bm, geom=[v for v in bm.verts if v.co.y > 0.1], context='VERTS'); bm.to_mesh(o.data); bm.free()
    o.data.materials.append(M('steel')); s = o.modifiers.new('s', 'SOLIDIFY'); s.thickness = 0.08; R.smooth(o)
    p = [o, S(0.32, (-0.72, -0.05, 0.55), 'metal', (1, 0.8, 0.7)), S(0.32, (0.72, -0.05, 0.55), 'metal', (1, 0.8, 0.7)),
         C(0.2, 0.06, (0, -0.42, 0.1), (90, 0, 0), 'dark'), S(0.15, (0, -0.44, 0.1), 'gcyan'),
         B((0.08, 0.06, 1.0), (0, -0.4, -0.45), m='metal', bev=0.02)]
    for x, z in ((-0.4, 0.5), (0.4, 0.5), (-0.5, -0.3), (0.5, -0.3), (-0.3, -0.7), (0.3, -0.7)): p.append(S(0.04, (x, -0.38, z), 'brass'))
    G(p, (5, 0, 15))

@item
def LaserTurbine():
    p = [T(0.62, 0.12, (0, 0, 0), (90, 0, 0), 'dark'), C(0.18, 0.3, (0, 0, 0), (90, 0, 0), 'metal'), S(0.12, (0, -0.15, 0), 'gred')]
    for k in range(8):
        a = k * 45
        p.append(B((0.42, 0.04, 0.14), (0.33 * math.cos(D(a)), 0, 0.33 * math.sin(D(a))), (25, -a, 0), 'steel', bev=0.01))
    p += [B((0.3, 0.4, 0.5), (0, 0.2, -0.85), m='metal', bev=0.05), C(0.08, 0.4, (0, 0.1, -0.55), m='dark'),
          C(0.06, 0.25, (0.7, 0, 0), (0, 90, 0), 'brass'), C(0.06, 0.25, (-0.7, 0, 0), (0, 90, 0), 'brass')]
    G(p, (10, 0, -25))

@item
def OlLopper():
    p = [C(0.08, 1.4, (0, 0, -0.35), m='wood', bev=0.02)]
    for z in (-0.65, -0.75, -0.85, -0.95): p.append(T(0.085, 0.022, (0, 0, z), m='leather'))
    p += [S(0.11, (0, 0, -1.08), 'brass'), B((0.85, 0.08, 0.6), (0.38, 0, 0.45), m='steel', bev=0.03), B((0.86, 0.02, 0.12), (0.38, -0.02, 0.17), m='white', bev=0.01),
          C(0.06, 0.12, (0.62, 0, 0.6), (90, 0, 0), 'dark'), B((0.25, 0.1, 0.65), (0.0, 0, 0.45), m='metal', bev=0.03)]
    for loc in ((0.2, -0.05, 0.55), (0.55, -0.05, 0.35)): p.append(S(0.06, loc, 'darkred', (1.2, 0.3, 0.8)))
    G(p, (0, 0, 0)).rotation_euler = (D(10), D(30), D(-15))

@item
def PhotonJetpack():
    p = [B((0.8, 0.4, 0.95), (0, 0.1, 0.1), m='white', bev=0.12), B((0.6, 0.1, 0.5), (0, -0.12, 0.2), m='blue', bev=0.04)]
    for x in (-0.48, 0.48):
        p += [C(0.2, 0.85, (x, 0.15, 0), m='metal', bev=0.04), T(0.205, 0.03, (x, 0.15, 0.3), m='brass'),
              K(0.17, 0.24, 0.25, (x, 0.15, -0.52), m='dark'), K(0.2, 0.0, 0.55, (x, 0.15, -0.9), (180, 0, 0), 'gblue'), K(0.1, 0.0, 0.35, (x, 0.15, -0.8), (180, 0, 0), 'gwhite')]
    p += [Tu([(-0.3, -0.2, 0.5), (-0.35, -0.45, 0.2), (-0.3, -0.4, -0.3)], 0.04, 'leather'), Tu([(0.3, -0.2, 0.5), (0.35, -0.45, 0.2), (0.3, -0.4, -0.3)], 0.04, 'leather')]
    G(p, (5, 0, -20))

@item
def TelescopicSight():
    rot = (0, 90, 0)
    p = [C(0.2, 1.7, (0, 0, 0), rot, 'metal'), C(0.3, 0.45, (0.95, 0, 0), rot, 'metal'), C(0.26, 0.35, (-0.9, 0, 0), rot, 'metal'),
         C(0.31, 0.07, (0.72, 0, 0), rot, 'brass'), C(0.27, 0.07, (-0.72, 0, 0), rot, 'brass'),
         C(0.1, 0.25, (0.05, 0, 0.25), m='brass'), C(0.09, 0.22, (0.05, -0.25, 0), (90, 0, 0), 'brass'),
         C(0.12, 0.6, (0.05, 0, -0.27), rot, 'dark'), C(0.27, 0.02, (1.18, 0, 0), rot, 'gcyan', bev=0)]
    for x in (-0.3, 0.4): p.append(B((0.1, 0.25, 0.25), (x, 0, -0.32), m='dark', bev=0.02))
    G(p, (0, -35, -25))

@item
def FiremansBoots():
    p = [C(0.3, 0.85, (0, 0, 0.25), m='black', bev=0.06), B((0.95, 0.6, 0.38), (0.25, 0, -0.32), m='black', bev=0.17),
         B((1.0, 0.64, 0.1), (0.25, 0, -0.52), m='rubber', bev=0.04), C(0.305, 0.1, (0, 0, 0.5), m='yellow', bev=0.0),
         C(0.305, 0.1, (0, 0, 0.15), m='yellow', bev=0.0), T(0.31, 0.04, (0, 0, 0.67), m='red')]
    for x, y, h, m in ((0.65, 0.0, 0.5, 'gorange'), (0.85, 0.15, 0.35, 'gyellow'), (-0.2, 0.2, 0.45, 'gorange'), (0.4, 0.25, 0.4, 'gred')):
        p.append(K(0.12, 0.0, h, (x, y, -0.45 + h / 2), m=m, v=16))
    G(p, (5, 0, 20))

@item
def AncientScepter():
    p = [C(0.06, 2.0, (0, 0, -0.35), m='gold', bev=0.01), T(0.09, 0.03, (0, 0, 0.3), m='gold'), T(0.09, 0.03, (0, 0, -0.9), m='gold'),
         K(0.12, 0.05, 0.25, (0, 0, -1.4), m='gold'), S(0.22, (0, 0, 0.92), 'gpurple'), C(0.16, 0.1, (0, 0, 0.62), m='gold')]
    for k in range(4):
        a = k * math.pi / 2 + 0.4
        p.append(Tu([(0.1 * math.cos(a), 0.1 * math.sin(a), 0.65), (0.3 * math.cos(a), 0.3 * math.sin(a), 0.85), (0.15 * math.cos(a), 0.15 * math.sin(a), 1.15)], 0.035, 'gold', taper=[1, 0.8, 0.4]))
    G(p, (0, 0, 0)).rotation_euler = (D(5), D(25), D(10))

@item
def Umbrella():
    bpy.ops.mesh.primitive_uv_sphere_add(radius=1.25, segments=64, ring_count=24)
    o = bpy.context.object; bm = bmesh.new(); bm.from_mesh(o.data)
    bmesh.ops.delete(bm, geom=[v for v in bm.verts if v.co.z < -0.05], context='VERTS')
    for v in bm.verts:
        v.co.z *= 0.55; ang = math.atan2(v.co.y, v.co.x)
        if v.co.z < 0.25: v.co.z += 0.12 * abs(math.sin(ang * 4)) * (1 - v.co.z / 0.25)
    bm.to_mesh(o.data); bm.free()
    o.data.materials.append(M('red')); o.data.materials.append(M('cream'))
    for pl in o.data.polygons: pl.material_index = int(((math.atan2(pl.center.y, pl.center.x) + math.pi) / (2 * math.pi)) * 8) % 2
    s = o.modifiers.new('s', 'SOLIDIFY'); s.thickness = 0.05; R.smooth(o)
    p = [o]
    for k in range(8):
        a = (k + 0.5) / 8 * 2 * math.pi
        p.append(Tu([(math.cos(a) * r, math.sin(a) * r, 0.69 * math.sqrt(max(0, 1 - (r / 1.25) ** 2)) + 0.01) for r in (0.1, 0.6, 1.0, 1.24)], 0.018, 'darkwood'))
    p += [S(0.08, (0, 0, 0.72), 'brass'), C(0.045, 1.9, (0, 0, -0.25), m='darkwood', v=16),
          Tu([(0, 0, -1.15), (0, 0, -1.35), (0.18, 0, -1.55), (0.38, 0, -1.42), (0.42, 0, -1.25)], 0.055, 'darkwood'), C(0.065, 0.12, (0, 0, -1.12), m='brass', v=16)]
    G(p, (-12, 20, 10), (0, 0, 0.3))

@item
def LegendarySpark():
    p = [S(0.35, (0, 0, 0), 'gyellow'), S(0.22, (0, 0, 0), 'gwhite'), T(0.5, 0.035, (0, 0, 0), (70, 20, 0), 'gold'), T(0.5, 0.035, (0, 0, 0), (-30, 60, 0), 'gold')]
    for k in range(6):
        a = k * 1.05; b = 0.3 * (k % 3) - 0.3
        p.append(Tu(zigzag((0.3 * math.cos(a), 0, 0.3 * math.sin(a)), (1.05 * math.cos(a), b, 1.05 * math.sin(a)), 5, 0.1, seed=k + 9), 0.025, 'gyellow', taper=[1.2, 1, 0.9, 0.7, 0.5, 0.3]))
    G(p, (10, 0, 0))

@item
def ImpTentacle():
    pts = [(-0.9, 0, -0.9), (-0.5, -0.1, -0.5), (-0.15, 0, -0.35), (0.2, -0.1, 0.05), (0.15, 0, 0.5), (0.5, -0.1, 0.85)]
    p = [Tu(pts, 0.22, 'darkred', taper=[1.0, 0.9, 0.75, 0.6, 0.42, 0.25])]
    for i, (x, y, z) in enumerate([(-0.55, -0.28, -0.5), (-0.2, -0.24, -0.3), (0.12, -0.24, 0.05), (0.12, -0.18, 0.45)]):
        p.append(T(0.06 - 0.008 * i, 0.02, (x, y, z), (90, 0, 0), 'pink'))
    p += [K(0.07, 0.0, 0.3, (0.58, -0.1, 1.05), (0, 25, 0), 'bone'), S(0.24, (-0.95, 0, -0.95), 'vflesh')]
    G(p, (0, 0, 10))

@item
def BurningWitness():
    p = [S(0.55, (0, 0, -0.1), 'white'), S(0.26, (0, -0.38, -0.05), 'gorange', (1, 0.6, 1)), S(0.13, (0, -0.5, -0.05), 'black', (1, 0.5, 1)),
         S(0.04, (-0.05, -0.55, 0.0), 'gwhite')]
    for k in range(5):
        a = k * 1.25
        p.append(Tu([(0.5 * math.cos(a), -0.1, 0.5 * math.sin(a) - 0.1), (0.35 * math.cos(a + 0.3), -0.38, 0.35 * math.sin(a + 0.3) - 0.1)], 0.012, 'red'))
    for x, h, m in ((-0.25, 0.7, 'gorange'), (0.05, 0.95, 'gyellow'), (0.3, 0.65, 'gorange'), (-0.45, 0.45, 'gred'), (0.45, 0.4, 'gred')):
        p.append(K(0.14, 0.0, h, (x, 0.1, 0.35 + h / 2), m=m, v=16))
    G(p, (5, 0, 0))

@item
def IfritsHorn():
    pts = [(-0.7, 0, -0.85), (-0.45, 0, -0.2), (-0.05, 0, 0.3), (0.45, 0, 0.55), (0.85, 0, 0.45)]
    p = [Tu(pts, 0.3, 'darkwood', taper=[1.0, 0.8, 0.55, 0.32, 0.08])]
    for i, t in enumerate((0.15, 0.3, 0.45, 0.6)):
        x, z = -0.7 + t * 1.6, -0.85 + math.sin(t * math.pi * 0.9) * 1.3
        p.append(T(0.3 * (1 - t * 0.9), 0.03, (x, 0, z), (0, 50 - 50 * t, 0), 'bone'))
    p += [S(0.07, (0.85, 0, 0.45), 'gorange'), C(0.32, 0.12, (-0.7, 0, -0.9), (0, -10, 0), 'gold')]
    G(p, (0, 0, -15))

@item
def ScorchingShell():
    bpy.ops.mesh.primitive_uv_sphere_add(radius=0.9, segments=48, ring_count=24)
    o = bpy.context.object; bm = bmesh.new(); bm.from_mesh(o.data)
    bmesh.ops.delete(bm, geom=[v for v in bm.verts if v.co.z < 0.0 or (v.co.x > 0.35 and v.co.y > -0.2)], context='VERTS'); bm.to_mesh(o.data); bm.free()
    o.scale = (1, 1, 0.55); o.data.materials.append(M('darkred'))
    t = bpy.data.textures.new('t', 'VORONOI'); t.noise_scale = 0.3
    d = o.modifiers.new('d', 'DISPLACE'); d.texture = t; d.strength = 0.05
    s = o.modifiers.new('s', 'SOLIDIFY'); s.thickness = 0.1; R.smooth(o)
    p = [o]
    for k in range(5):
        a = k * 1.2
        p.append(Tu(zigzag((0, 0, 0.5), (0.8 * math.cos(a), 0.8 * math.sin(a), 0.15), 4, 0.06, seed=k + 20), 0.022, 'gorange'))
    for loc, h in (((-0.2, -0.1, 0.55), 0.35), ((0.15, -0.3, 0.45), 0.25)): p.append(K(0.08, 0.0, h, loc, m='gyellow', v=12))
    G(p, (35, 0, 20))

@item
def NematocystNozzle():
    bpy.ops.mesh.primitive_uv_sphere_add(radius=0.7, segments=48, ring_count=24)
    o = bpy.context.object; bm = bmesh.new(); bm.from_mesh(o.data)
    bmesh.ops.delete(bm, geom=[v for v in bm.verts if v.co.z < -0.1], context='VERTS')
    for v in bm.verts:
        if v.co.z < 0.1: v.co.z += 0.06 * math.sin(math.atan2(v.co.y, v.co.x) * 8)
    bm.to_mesh(o.data); bm.free(); o.data.materials.append(M('violet'))
    s = o.modifiers.new('s', 'SOLIDIFY'); s.thickness = 0.05; R.smooth(o)
    p = [o, S(0.3, (0, 0, 0.2), 'gpurple'), C(0.15, 0.3, (0, 0, 0.75), m='metal'), K(0.2, 0.1, 0.15, (0, 0, 0.95), m='steel')]
    r = random.Random(3)
    for k in range(7):
        a = k / 7 * 2 * math.pi
        x0, y0 = 0.5 * math.cos(a), 0.5 * math.sin(a)
        p.append(Tu([(x0, y0, -0.05), (x0 * 0.8 + r.uniform(-.1, .1), y0 * 0.8, -0.5), (x0 * 0.9 + r.uniform(-.15, .15), y0 * 0.9, -1.0)], 0.03, 'vpale', taper=[1, 0.7, 0.3]))
    G(p, (15, 0, 0), (0, 0, 0.1))

@item
def RottenBrain():
    p = [Blob(0.55, (-0.25, 0, 0), (0.9, 1.15, 0.85), 'brain', amt=0.1, noise=0.18, kind='VORONOI'),
         Blob(0.55, (0.25, 0, 0), (0.9, 1.15, 0.85), 'brain', amt=0.1, noise=0.18, kind='VORONOI', seed=2),
         Blob(0.3, (0, 0.2, -0.4), (1.2, 1, 0.7), 'brain', amt=0.05)]
    for loc in ((0.3, -0.5, -0.2), (-0.35, -0.48, 0.15), (0.0, -0.45, 0.35)):
        p.append(Blob(0.1, loc, (1, 0.6, 1), 'lime', amt=0.03))
    p.append(Tu([(0.3, -0.5, -0.25), (0.32, -0.48, -0.6)], 0.035, 'lime', taper=[1, 0.4]))
    G(p, (10, 0, 15))

@item
def SafeguardLantern():
    p = [C(0.42, 0.12, (0, 0, -0.72), m='dark', bev=0.04), C(0.36, 0.1, (0, 0, 0.62), m='dark', bev=0.04), K(0.36, 0.1, 0.28, (0, 0, 0.8), m='dark'),
         T(0.18, 0.035, (0, 0, 1.08), (90, 0, 0), 'dark'), C(0.3, 1.2, (0, 0, -0.05), m='glass', bev=0.02), S(0.2, (0, 0, -0.05), 'gyellow', (1, 1, 1.4)),
         K(0.1, 0.0, 0.3, (0, 0, 0.18), m='gwhite')]
    for x, y, z in ring_pts(0.33, 6, -0.05): p.append(C(0.025, 1.25, (x, y, z), m='brass', v=8))
    G(p, (10, 0, -10))

@item
def Snowglobe():
    p = [S(0.7, (0, 0, 0.2), 'glass'), C(0.62, 0.3, (0, 0, -0.55), m='wood', bev=0.06), C(0.66, 0.1, (0, 0, -0.75), m='darkwood', bev=0.03),
         C(0.45, 0.06, (0, 0, -0.38), m='white', bev=0.02), K(0.22, 0.0, 0.35, (-0.1, 0, -0.18), m='green'), K(0.16, 0.0, 0.25, (-0.1, 0, 0.02), m='green'),
         C(0.05, 0.1, (-0.1, 0, -0.38), m='wood'), B((0.2, 0.18, 0.18), (0.25, 0.05, -0.25), m='red', bev=0.03), K(0.15, 0, 0.15, (0.25, 0.05, -0.08), m='white', v=4)]
    r = random.Random(5)
    for i in range(12): p.append(S(0.025, (r.uniform(-.45, .45), r.uniform(-.45, .1), r.uniform(-.1, .7)), 'gwhite'))
    G(p, (10, 0, 0))

@item
def ExplorersKey():
    p = [T(0.32, 0.07, (-0.6, 0, 0.35), (90, 0, 0), 'gold'), Gem(0.1, (-0.6, -0.05, 0.35), 'gcyan'),
         C(0.07, 1.2, (0.05, 0, -0.2), (0, 0, 0), 'gold')]
    p[-1].rotation_euler = (0, D(-48), 0); p[-1].location = (0.0, 0, 0.0)
    p += [T(0.09, 0.03, (-0.32, 0, 0.12), (0, 42, 0), 'brass'), B((0.12, 0.08, 0.28), (0.42, 0, -0.42), (0, 42, 0), 'gold', bev=0.02),
          B((0.1, 0.08, 0.2), (0.3, 0, -0.32), (0, 42, 0), 'gold', bev=0.02)]
    G(p, (0, 0, -10))

@item
def InstantMinefield():
    p = [B((1.7, 1.0, 0.1), (0, 0, -0.35), m='olive', bev=0.04)]
    for (x, y), col in zip(((-0.5, 0.15), (0.35, 0.25), (0.0, -0.3)), ('metal', 'dark', 'olive')):
        p += [C(0.32, 0.12, (x, y, -0.22), m=col, bev=0.04), C(0.22, 0.06, (x, y, -0.13), m='steel', bev=0.02), C(0.07, 0.06, (x, y, -0.08), m='gred')]
    p += [B((0.3, 0.12, 0.06), (0.7, -0.35, -0.27), m='yellow', bev=0.02), B((0.3, 0.12, 0.06), (-0.7, -0.35, -0.27), m='black', bev=0.02)]
    G(p, (45, 0, 15))

@item
def CarraraMarble():
    p = [B((0.3, 0.3, 1.5), (-0.5, 0, -0.1), m='stone', bev=0.03), B((0.3, 0.3, 1.5), (0.5, 0, -0.1), m='stone', bev=0.03),
         B((1.4, 0.4, 0.25), (0, 0, 0.75), m='stone', bev=0.04), B((1.5, 0.45, 0.12), (0, 0, -0.88), m='stone', bev=0.03),
         B((0.68, 0.04, 1.3), (0, 0, -0.1), m='gcyan', bev=0.0), Gem(0.08, (0, -0.21, 0.75), 'gcyan')]
    for x in (-0.5, 0.5):
        for z in (-0.6, 0.0, 0.5): p.append(T(0.16, 0.02, (x, 0, z), m='stone'))
    G(p, (5, 0, 20))

@item
def ShatteredMirror():
    p = [T(0.55, 0.07, (0, 0, 0.25), (90, 0, 0), 'gold'), C(0.55, 0.05, (0, 0, 0.25), (90, 0, 0), 'gcyan', bev=0.0),
         C(0.07, 0.75, (0, 0, -0.6), m='darkwood'), S(0.1, (0, 0, -1.0), 'gold'), T(0.09, 0.03, (0, 0, -0.25), m='gold')]
    for a, b in (((-0.3, -0.04, 0.5), (0.05, -0.04, 0.2)), ((0.05, -0.04, 0.2), (0.4, -0.04, 0.35)), ((0.05, -0.04, 0.2), (-0.05, -0.04, -0.2)), ((0.05, -0.04, 0.2), (0.2, -0.04, -0.15))):
        p.append(Tu([a, b], 0.012, 'black'))
    for loc, rot in (((0.75, -0.2, 0.6), (20, 30, 10)), ((0.85, -0.1, 0.25), (-30, 10, 45)), ((0.65, -0.3, -0.05), (10, -40, 20))):
        p.append(B((0.14, 0.02, 0.2), loc, rot, 'gcyan', bev=0.0))
    G(p, (5, 0, 15))

@item
def GoldPlatedBomb():
    p = [S(0.6, (0, 0, -0.1), 'gold'), C(0.2, 0.18, (0, 0, 0.55), m='brass'), T(0.2, 0.04, (0, 0, 0.47), m='brass'),
         Tu([(0, 0, 0.62), (0.1, 0, 0.85), (0.3, 0, 0.92), (0.42, 0, 1.05)], 0.035, 'rubber'), S(0.09, (0.45, 0, 1.08), 'gorange'), S(0.05, (0.45, -0.05, 1.08), 'gwhite')]
    for k in range(6): p.append(T(0.605, 0.02, (0, 0, -0.1), (90, 0, k * 30), 'brass'))
    G(p, (5, 0, 10))

@item
def DynamitePlunger():
    p = [B((0.8, 0.55, 0.6), (-0.3, 0, -0.45), m='wood', bev=0.04), C(0.05, 0.6, (-0.3, 0, 0.1), m='steel'), C(0.06, 0.7, (-0.3, 0, 0.42), (0, 90, 0), 'dark'),
         S(0.07, (-0.65, 0, 0.42), 'red'), S(0.07, (0.05, 0, 0.42), 'red')]
    for k in range(3): p.append(C(0.11, 0.8, (0.55 + 0.2 * (k % 2), 0.05 * k, -0.4 + (0.18 if k == 2 else 0)), (0, 0, 0), 'red', bev=0.02))
    p += [B((0.48, 0.3, 0.06), (0.65, 0.05, -0.5), m='paper', bev=0.01), Tu([(-0.0, -0.1, -0.3), (0.2, -0.25, -0.1), (0.5, -0.15, 0.05)], 0.025, 'red'),
          Tu([(0.0, -0.05, -0.5), (0.25, -0.3, -0.6), (0.55, -0.1, -0.15)], 0.025, 'black')]
    G(p, (10, 0, -15))

@item
def MaceReplica():
    p = [C(0.07, 1.5, (0, 0, -0.45), m='darkwood'), T(0.08, 0.025, (0, 0, -0.95), m='leather'), T(0.08, 0.025, (0, 0, -1.05), m='leather'),
         S(0.1, (0, 0, -1.22), 'steel'), C(0.12, 0.15, (0, 0, 0.3), m='steel'), S(0.38, (0, 0, 0.62), 'metal')]
    for k in range(10):
        th = math.acos(1 - 2 * (k + 0.5) / 10); ph = math.pi * (1 + 5 ** 0.5) * k
        d = Vector((math.sin(th) * math.cos(ph), math.sin(th) * math.sin(ph), math.cos(th)))
        c = Vector((0, 0, 0.62)) + d * 0.42
        o = K(0.08, 0.0, 0.3, tuple(c), m='steel', v=12); o.rotation_euler = d.to_track_quat('Z', 'Y').to_euler()
        p.append(o)
    G(p, (0, 30, -10))

@item
def CrudelyDrawnBuddy():
    p = [S(0.32, (0, 0, 0.62), 'paper', (1, 0.15, 1)), B((0.5, 0.06, 0.65), (0, 0, 0.0), m='paper', bev=0.03),
         B((0.14, 0.06, 0.55), (-0.38, 0, 0.12), (0, 35, 0), 'paper', bev=0.03), B((0.14, 0.06, 0.55), (0.38, 0, 0.0), (0, -50, 0), 'paper', bev=0.03),
         B((0.16, 0.06, 0.55), (-0.14, 0, -0.55), (0, 8, 0), 'paper', bev=0.03), B((0.16, 0.06, 0.55), (0.16, 0, -0.55), (0, -8, 0), 'paper', bev=0.03),
         S(0.04, (-0.1, -0.06, 0.68), 'black'), S(0.04, (0.11, -0.06, 0.66), 'black'),
         Tu([(-0.12, -0.06, 0.52), (0.0, -0.06, 0.48), (0.13, -0.06, 0.53)], 0.015, 'black'),
         Tu([(-0.15, -0.05, 0.2), (0.15, -0.05, -0.1)], 0.015, 'red'), Tu([(0.15, -0.05, 0.2), (-0.12, -0.05, -0.12)], 0.015, 'blue'),
         B((0.1, 0.3, 0.06), (0, 0.15, -0.9), m='wood', bev=0.02)]
    G(p, (5, 0, 12))

@item
def ShieldGenerator():
    bpy.ops.mesh.primitive_uv_sphere_add(radius=0.75, segments=48, ring_count=24)
    o = bpy.context.object; bm = bmesh.new(); bm.from_mesh(o.data)
    bmesh.ops.delete(bm, geom=[v for v in bm.verts if v.co.z < 0], context='VERTS'); bm.to_mesh(o.data); bm.free()
    o.location = (0, 0, -0.35); o.data.materials.append(M('glass')); R.smooth(o)
    p = [o, C(0.8, 0.25, (0, 0, -0.5), m='dark', bev=0.06), T(0.8, 0.05, (0, 0, -0.38), m='gcyan'), C(0.12, 0.45, (0, 0, -0.1), m='metal'),
         S(0.16, (0, 0, 0.15), 'gcyan')]
    for x, y, z in ring_pts(0.62, 6, -0.5): p.append(B((0.12, 0.12, 0.3), (x, y, z), m='yellow', bev=0.03))
    for k in range(4):
        a = k * 1.57 + 0.4
        p.append(Tu([(0, 0, 0.15), (0.35 * math.cos(a), 0.35 * math.sin(a), 0.25), (0.55 * math.cos(a), 0.55 * math.sin(a), 0.0)], 0.012, 'gcyan'))
    G(p, (20, 0, 0))

@item
def UnstableWatch():
    p = [C(0.62, 0.15, (0, 0, 0), (90, 0, 0), 'gold'), C(0.55, 0.02, (0, -0.08, 0), (90, 0, 0), 'cream', bev=0.0),
         C(0.05, 0.1, (0, -0.1, 0), (90, 0, 0), 'black'), B((0.04, 0.02, 0.38), (0, -0.1, 0.17), m='black', bev=0.0), B((0.04, 0.02, 0.28), (0.12, -0.1, -0.06), (0, -60, 0), 'black', bev=0.0),
         C(0.1, 0.15, (0, 0, 0.7), m='gold'), T(0.12, 0.035, (0, 0, 0.88), (90, 0, 0), 'gold')]
    for k in range(12):
        a = k / 12 * 2 * math.pi
        p.append(B((0.03, 0.02, 0.07), (0.46 * math.cos(a), -0.1, 0.46 * math.sin(a)), (0, -math.degrees(a) + 90, 0), 'black', bev=0.0))
    for k in range(4): p.append(T(0.07, 0.02, (0.15 + 0.12 * k, 0.05, 0.95 - 0.14 * k), (90, 0, 45 * k), 'brass'))
    for loc, rot in (((-0.7, -0.1, 0.3), (10, 40, 0)), ((-0.6, -0.2, -0.4), (40, 10, 30))): p.append(B((0.12, 0.02, 0.18), loc, rot, 'gcyan', bev=0.0))
    G(p, (5, 0, 15))

@item
def PillagedGold():
    r = random.Random(11); p = [B((0.75, 0.35, 0.3), (0.15, 0.25, 0.1), (0, 0, 20), 'gold', bev=0.06)]
    for i in range(14):
        x, y = r.uniform(-0.75, 0.75), r.uniform(-0.35, 0.35); z = -0.35 + 0.08 * (i % 4)
        p.append(C(0.18, 0.05, (x, y, z), (r.uniform(-25, 25), r.uniform(-25, 25), 0), 'gold', bev=0.01))
    p += [Gem(0.13, (-0.4, -0.25, 0.0), 'gred'), Gem(0.1, (0.5, -0.3, -0.15), 'gcyan')]
    G(p, (30, 0, 10))

@item
def CaptainsBrooch():
    p = [C(0.55, 0.12, (0, 0, 0.2), (90, 0, 0), 'gold'), C(0.42, 0.05, (0, -0.08, 0.2), (90, 0, 0), 'navy', bev=0.01),
         B((0.3, 0.08, 0.7), (-0.18, 0.05, -0.55), (0, -12, 0), 'red', bev=0.02), B((0.3, 0.08, 0.7), (0.18, 0.05, -0.55), (0, 12, 0), 'red', bev=0.02),
         B((0.3, 0.09, 0.1), (-0.25, 0.05, -0.88), (0, -12, 0), 'gold', bev=0.01), B((0.3, 0.09, 0.1), (0.25, 0.05, -0.88), (0, 12, 0), 'gold', bev=0.01)]
    for k in range(5):
        a = math.pi / 2 + k * 2 * math.pi / 5
        o = B((0.08, 0.05, 0.3), (0.13 * math.cos(a), -0.12, 0.2 + 0.13 * math.sin(a)), (0, 90 - math.degrees(a), 0), 'cream', bev=0.01); p.append(o)
    G(p, (5, 0, 12))

@item
def SuperMassiveLeech():
    pts = [(-1.0, 0, -0.6), (-0.5, -0.05, -0.2), (0.0, 0, 0.0), (0.45, -0.05, 0.3), (0.8, 0, 0.6)]
    p = [Tu(pts, 0.32, 'darkred', taper=[0.6, 0.95, 1.0, 0.95, 0.85])]
    for i in range(1, 9):
        t = i / 9; x = -1.0 + 1.8 * t; z = -0.6 + 1.2 * t
        p.append(T(0.3 * (0.75 + 0.25 * math.sin(t * math.pi)), 0.03, (x, -0.02, z), (0, 90 - 34, 0), 'flesh'))
    p += [T(0.2, 0.06, (0.85, 0, 0.65), (0, -55, 0), 'pink'), S(0.17, (0.87, 0, 0.67), 'black', (1, 1, 0.4))]
    for k in range(8):
        a = k / 8 * 2 * math.pi
        o = K(0.03, 0.0, 0.1, (0.85 + 0.12 * math.cos(a) * 0.57, 0.12 * math.sin(a), 0.65 + 0.12 * math.cos(a) * 0.82), m='bone', v=8); p.append(o)
    G(p, (10, 0, 15))


# ---------------------------------------------------------------- second-pass redesigns
def Star(r_out, r_in, depth, loc, m, rot=(90, 0, 0)):
    bm = bmesh.new(); top = []; bot = []
    for k in range(10):
        a = math.pi / 2 + k * math.pi / 5; rr = r_out if k % 2 == 0 else r_in
        top.append(bm.verts.new((math.cos(a) * rr, math.sin(a) * rr, depth / 2))); bot.append(bm.verts.new((math.cos(a) * rr, math.sin(a) * rr, -depth / 2)))
    bm.faces.new(top); bm.faces.new(list(reversed(bot)))
    for k in range(10): bm.faces.new((top[k], top[(k + 1) % 10], bot[(k + 1) % 10], bot[k]))
    me = bpy.data.meshes.new('star'); bm.to_mesh(me); bm.free()
    o = bpy.data.objects.new('star', me); bpy.context.scene.collection.objects.link(o)
    o.location = loc; o.rotation_euler = tuple(D(a) for a in rot); o.data.materials.append(M(m))
    b = o.modifiers.new('b', 'BEVEL'); b.width = 0.02; b.segments = 2
    return o

@item
def Taser():
    p = [B((0.6, 0.38, 1.05), (0, 0, -0.2), m='dark', bev=0.12), B((0.64, 0.41, 0.26), (0, 0, 0.28), m='yellow', bev=0.07),
         B((0.36, 0.06, 0.36), (0, -0.2, -0.3), m='metal', bev=0.03), C(0.08, 0.05, (0, -0.24, -0.62), (90, 0, 0), 'gred'),
         C(0.07, 0.6, (-0.17, 0, 0.68), m='steel', v=16), C(0.07, 0.6, (0.17, 0, 0.68), m='steel', v=16),
         S(0.09, (-0.17, 0, 0.98), 'steel'), S(0.09, (0.17, 0, 0.98), 'steel'), S(0.09, (0, -0.05, 1.0), 'gcyan')]
    for k in range(3): p.append(Tu(zigzag((-0.15, -0.05, 0.98), (0.15, -0.05, 0.98), 4, 0.1, seed=k + 1), 0.025, 'gcyan'))
    G(p, (0, 0, -25), (0, 0, 0.05))

@item
def Spikestrip():
    p = [B((2.1, 0.75, 0.16), (0, 0, -0.1), m='dark', bev=0.05)]
    for i in range(6):
        p.append(B((0.18, 0.77, 0.165), (-0.85 + i * 0.34, 0, -0.1), m='yellow' if i % 2 == 0 else 'black', bev=0.02))
    for i in range(6):
        for y in (-0.2, 0.2): p.append(K(0.11, 0.0, 0.45, (-0.85 + i * 0.34, y, 0.2), m='steel', v=12))
    G(p, (50, 0, -30))

@item
def HermitsScarf():
    loop = T(0.5, 0.14, (0, 0, 0.35), (80, 0, 0), 'clothg'); loop.scale = (1, 1, 0.75)
    p = [loop]
    for k in range(5): p.append(T(0.5, 0.03, (0, -0.04 + 0.02 * k, 0.35), (80, 0, 0), 'clothg2' if k % 2 else 'cream'))
    for x0, col in ((0.18, 'clothg'), (-0.12, 'clothg2')):
        t = Tu([(x0, -0.42, 0.2), (x0 + 0.08, -0.5, -0.2), (x0 - 0.02, -0.48, -0.65), (x0 + 0.04, -0.45, -0.95)], 0.17, col, flat=(1, 0.35, 1)); p.append(t)
        for z in (-0.3, -0.6): p.append(Tu([(x0 - 0.15, -0.5, z), (x0 + 0.17, -0.5, z)], 0.025, 'cream'))
        for i in range(5): p.append(C(0.018, 0.16, (x0 - 0.1 + i * 0.05, -0.46, -1.1), m='cream', v=8))
    G(p, (5, 0, 12))

@item
def RepulsionArmor():
    bpy.ops.mesh.primitive_uv_sphere_add(radius=0.8, segments=48, ring_count=24)
    o = bpy.context.object; o.scale = (1.0, 0.5, 1.1)
    bm = bmesh.new(); bm.from_mesh(o.data)
    bmesh.ops.delete(bm, geom=[v for v in bm.verts if v.co.y > 0.15 or v.co.z > 0.7], context='VERTS'); bm.to_mesh(o.data); bm.free()
    o.data.materials.append(M('steel')); s = o.modifiers.new('s', 'SOLIDIFY'); s.thickness = 0.09; R.smooth(o)
    p = [o]
    for sgn in (-1, 1):
        sh = S(0.34, (0.62 * sgn, 0.0, 0.42), 'metal', (1.1, 0.75, 0.55)); p.append(sh)
        p.append(T(0.3, 0.03, (0.62 * sgn, -0.02, 0.33), (0, 0, 0), 'brass'))
    p += [C(0.24, 0.07, (0, -0.4, 0.1), (90, 0, 0), 'dark'), S(0.17, (0, -0.43, 0.1), 'gcyan'), T(0.24, 0.035, (0, -0.42, 0.1), (90, 0, 0), 'brass'),
          B((0.1, 0.06, 0.75), (0, -0.38, -0.45), m='metal', bev=0.02), B((1.2, 0.08, 0.1), (0, -0.3, 0.62), m='brass', bev=0.03)]
    for x, z in ((-0.45, 0.35), (0.45, 0.35), (-0.55, -0.25), (0.55, -0.25), (-0.35, -0.65), (0.35, -0.65)): p.append(S(0.045, (x, -0.36, z), 'brass'))
    G(p, (5, 0, 18))

@item
def ScorchingShell():
    bpy.ops.mesh.primitive_uv_sphere_add(radius=0.95, segments=48, ring_count=24)
    o = bpy.context.object; bm = bmesh.new(); bm.from_mesh(o.data)
    import random as _r; rr = _r.Random(7)
    bmesh.ops.delete(bm, geom=[v for v in bm.verts if v.co.y > -0.05 + 0.25 * math.sin(math.atan2(v.co.z, v.co.x) * 3) * 0.6], context='VERTS')
    bm.to_mesh(o.data); bm.free()
    o.scale = (1, 0.5, 0.95); o.data.materials.append(M('darkred'))
    t = bpy.data.textures.new('t', 'VORONOI'); t.noise_scale = 0.25
    d = o.modifiers.new('d', 'DISPLACE'); d.texture = t; d.strength = 0.06
    s = o.modifiers.new('s', 'SOLIDIFY'); s.thickness = 0.14; R.smooth(o)
    p = [o]
    for k in range(6):
        a = k * 1.05 + 0.3
        p.append(Tu(zigzag((0.0, -0.5, 0.0), (0.85 * math.cos(a), -0.25, 0.8 * math.sin(a)), 5, 0.08, seed=k + 20), 0.03, 'gorange'))
    for k in range(4):
        a = k * 1.6 + 0.2
        p.append(T(0.25 + 0.18 * k, 0.025, (0, -0.4 + 0.05 * k, 0), (90, 0, 0), 'black'))
    for loc, h in (((-0.25, -0.45, 0.6), 0.4), ((0.3, -0.42, 0.5), 0.3), ((0.0, -0.48, 0.75), 0.5)): p.append(K(0.09, 0.0, h, loc, m='gyellow', v=12))
    G(p, (10, 0, 15))

@item
def RottenBrain():
    p = [Blob(0.55, (-0.27, 0, 0), (0.85, 1.15, 0.8), 'pink', amt=0.05), Blob(0.55, (0.27, 0, 0), (0.85, 1.15, 0.8), 'pink', amt=0.05, seed=2),
         Blob(0.3, (0, 0.25, -0.42), (1.2, 1, 0.6), 'pink', amt=0.04), C(0.12, 0.5, (0, 0.1, -0.6), m='pink')]
    r = random.Random(4)
    for side in (-1, 1):
        for k in range(7):
            pts = []
            z0 = r.uniform(-0.35, 0.4); x0 = side * r.uniform(0.1, 0.45)
            for j in range(5):
                pts.append((x0 + side * 0.08 * math.sin(j * 1.8 + k), -0.55 * math.sqrt(max(0.05, 1 - (x0 / 0.75) ** 2 - (z0 / 0.6) ** 2)) - 0.02, z0 + 0.09 * j - 0.18))
            p.append(Tu(pts, 0.05, 'pink2'))
    for loc in ((0.3, -0.5, -0.25), (-0.35, -0.48, 0.2), (0.05, -0.55, 0.35), (-0.15, -0.5, -0.3)):
        p.append(Blob(0.09, loc, (1, 0.5, 1), 'lime', amt=0.02))
    p.append(Tu([(0.3, -0.52, -0.3), (0.32, -0.5, -0.65)], 0.035, 'lime', taper=[1, 0.35]))
    p.append(Tu([(0, -0.62, 0.45), (0, -0.66, 0.0), (0, -0.6, -0.4)], 0.025, 'darkred'))
    G(p, (10, 0, 15))

@item
def CaptainsBrooch():
    p = [C(0.55, 0.12, (0, 0, 0.2), (90, 0, 0), 'gold'), C(0.43, 0.05, (0, -0.08, 0.2), (90, 0, 0), 'teal', bev=0.01),
         Gem(0.17, (0, -0.16, 0.2), 'gcyan', (1, 0.6, 1.3))]
    for k in range(8):   # compass-rose points around the gem
        a = k * math.pi / 4; L = 0.33 if k % 2 == 0 else 0.22
        o = K(0.05, 0.0, L, (math.cos(a) * (0.15 + L / 2), -0.12, 0.2 + math.sin(a) * (0.15 + L / 2)), m='gold', v=4)
        o.rotation_euler = (0, D(90) - a, 0); p.append(o)
    p += [B((0.3, 0.08, 0.7), (-0.18, 0.05, -0.55), (0, -12, 0), 'burgundy', bev=0.02), B((0.3, 0.08, 0.7), (0.18, 0.05, -0.55), (0, 12, 0), 'burgundy', bev=0.02),
          B((0.05, 0.085, 0.7), (-0.18, 0.04, -0.55), (0, -12, 0), 'gold', bev=0.01), B((0.05, 0.085, 0.7), (0.18, 0.04, -0.55), (0, 12, 0), 'gold', bev=0.01)]
    for k in range(16):
        a = k / 16 * 2 * math.pi
        p.append(S(0.035, (0.5 * math.cos(a), -0.07, 0.2 + 0.5 * math.sin(a)), 'brass'))
    G(p, (5, 0, 12))

# ---------------------------------------------------------------- void generator
VOIDCOLS = {1: 'vpale', 2: 'gpink', 3: 'gpurple'}

def void_icon(seed_s, tier):
    seed = zlib.crc32(seed_s.encode()); r = random.Random(seed); acc = VOIDCOLS[tier]
    body = r.choice(['vdark', 'vmid', 'vshell', 'vflesh'])
    motif = seed % 10; p = []
    if motif == 0:  # urchin
        p.append(Blob(0.45, m=body, amt=0.06, seed=r.randint(1, 9)))
        n = r.randint(9, 14)
        for k in range(n):
            th = math.acos(1 - 2 * (k + 0.5) / n); ph = math.pi * (1 + 5 ** 0.5) * k
            d = Vector((math.sin(th) * math.cos(ph), math.sin(th) * math.sin(ph), math.cos(th)))
            L = r.uniform(0.35, 0.6)
            p.append(Tu([tuple(d * 0.4), tuple(d * (0.4 + L * 0.6) + Vector((0, 0, 0.05))), tuple(d * (0.4 + L))], 0.06, 'vmid', taper=[1, 0.6, 0.2]))
            p.append(S(0.045, tuple(d * (0.42 + L)), acc))
        p.append(S(0.15, (0, -0.4, 0.05), acc))
    elif motif == 1:  # coral
        p.append(Blob(0.35, (0, 0, -0.75), (1.4, 1.2, 0.5), body, amt=0.06))
        def branch(o, ang, depth, rad):
            L = r.uniform(0.3, 0.42)
            e = (o[0] + math.sin(ang) * L, o[1] + r.uniform(-0.1, 0.1), o[2] + math.cos(ang) * L)
            p.append(Tu([o, e], rad, 'vmid' if depth > 1 else 'vflesh', taper=[1, 0.8]))
            if depth == 0: p.append(S(rad * 1.5, e, acc)); return
            for da in (-r.uniform(0.35, 0.7), r.uniform(0.35, 0.7)): branch(e, ang + da, depth - 1, rad * 0.75)
        branch((0, 0, -0.6), r.uniform(-0.2, 0.2), 3, 0.11)
    elif motif == 2:  # spiral shell
        pts = []
        for i in range(60):
            t = i / 59; a = t * 3.2 * math.pi; rr = 0.65 * (1 - t * 0.85)
            pts.append((math.cos(a) * rr, 0, -0.6 + t * 1.3 + math.sin(a) * rr * 0.4))
        p.append(Tu(pts, 0.3, body, taper=[1 - 0.85 * i / 59 for i in range(60)]))
        p.append(S(0.18, (0.0, -0.3, -0.4), acc)); p.append(Blob(0.25, (0, 0.05, -0.55), (1.3, 1, 0.6), 'vdark', amt=0.04))
    elif motif == 3:  # eye pod
        p += [Blob(0.62, m=body, amt=0.08, seed=r.randint(1, 9)), S(0.36, (0, -0.42, 0.05), 'vpale'), S(0.2, (0, -0.65, 0.05), acc), S(0.09, (0, -0.8, 0.05), 'black')]
        for k in range(5):
            a = k * 1.25
            p.append(Tu([(0.55 * math.cos(a), 0, 0.55 * math.sin(a)), (0.8 * math.cos(a + 0.3), 0.1, 0.8 * math.sin(a + 0.3)), (0.95 * math.cos(a + 0.7), 0.0, 0.95 * math.sin(a + 0.7))], 0.06, 'vflesh', taper=[1, 0.6, 0.2]))
    elif motif == 4:  # crystal cluster
        p.append(Blob(0.4, (0, 0, -0.7), (1.6, 1.2, 0.5), 'vdark', amt=0.08))
        for k in range(r.randint(5, 7)):
            x = r.uniform(-0.5, 0.5); h = r.uniform(0.5, 1.1); rad = r.uniform(0.1, 0.18)
            o = K(rad, 0.0, h, (x, r.uniform(-0.2, 0.2), -0.65 + h / 2), m=r.choice(['vmid', 'vshell', acc]), v=6)
            o.rotation_euler = (D(r.uniform(-15, 15)), D(-x * 40), 0); p.append(o)
    elif motif == 5:  # tentacle mass
        p += [Blob(0.42, (0, 0, 0.3), (1.1, 1, 0.9), body, amt=0.1, seed=r.randint(1, 9)), S(0.12, (0.15, -0.38, 0.4), acc), S(0.08, (-0.15, -0.36, 0.25), acc)]
        for k in range(r.randint(4, 6)):
            a = k * 1.2 + r.uniform(0, 0.4); pts = [(0.3 * math.cos(a), 0.3 * math.sin(a) * 0.4, 0.0)]
            for j in range(4): px, py, pz = pts[-1]; pts.append((px * 1.15 + r.uniform(-.15, .15), py + r.uniform(-.08, .08), pz - 0.25))
            p.append(Tu(pts, 0.09, 'vflesh', taper=[1, 0.8, 0.6, 0.4, 0.15]))
            p.append(S(0.04, pts[-1], acc))
    elif motif == 6:  # orbit ring
        p += [Blob(0.32, m=body, amt=0.05), S(0.13, (0, -0.28, 0.05), acc), T(0.7, 0.06, (0, 0, 0), (r.uniform(55, 75), r.uniform(-20, 20), 0), 'vshell'),
              T(0.85, 0.03, (0, 0, 0), (r.uniform(100, 120), r.uniform(-30, 30), 0), 'vmid')]
        for k in range(r.randint(3, 5)):
            a = k * 2 * math.pi / 5 + r.uniform(0, 0.5)
            p.append(Blob(0.1, (0.72 * math.cos(a), 0.2 * math.sin(a), 0.6 * math.sin(a)), m=r.choice(['vflesh', 'vmid']), amt=0.02))
    elif motif == 7:  # hollow pod
        p += [Blob(0.55, (0, 0, 0), (0.8, 0.8, 1.2), body, amt=0.06, seed=r.randint(1, 9)), S(0.32, (0, -0.15, 0.0), acc, (0.9, 0.9, 1.3)),
              K(0.12, 0.0, 0.35, (0, 0, 0.8), m='vmid', v=12)]
        for k in range(r.randint(3, 5)):
            p.append(Blob(0.1, (r.uniform(-0.3, 0.3), -0.42, r.uniform(-0.5, 0.5)), (1, 0.4, 1), 'vdark', amt=0.02))
    elif motif == 8:  # chitin carapace
        bpy.ops.mesh.primitive_uv_sphere_add(radius=0.75, segments=48, ring_count=24)
        o = bpy.context.object; bm = bmesh.new(); bm.from_mesh(o.data)
        bmesh.ops.delete(bm, geom=[v for v in bm.verts if v.co.y > 0.1], context='VERTS'); bm.to_mesh(o.data); bm.free()
        o.scale = (1, 0.6, 1.1); o.data.materials.append(M(body)); s = o.modifiers.new('s', 'SOLIDIFY'); s.thickness = 0.08; R.smooth(o)
        p.append(o)
        for z in (-0.4, 0.0, 0.4): p.append(T(0.68 * math.cos(z), 0.04, (0, -0.1, z), (90, 0, 0), 'vmid'))
        p += [S(0.18, (0, -0.42, 0.0), acc), S(0.08, (-0.3, -0.4, 0.45), acc), S(0.08, (0.3, -0.4, -0.45), acc)]
    else:  # horned spiral
        for sgn in (-1, 1):
            p.append(Tu([(0.15 * sgn, 0, -0.3), (0.5 * sgn, -0.05, 0.1), (0.45 * sgn, -0.1, 0.6), (0.25 * sgn, 0, 0.85)], 0.2, 'vshell', taper=[1, 0.75, 0.45, 0.1]))
        p += [Blob(0.4, (0, 0, -0.4), (1.2, 1, 0.9), body, amt=0.08), S(0.15, (0, -0.35, -0.3), acc)]
    G(p, (r.uniform(0, 15), 0, r.uniform(-25, 25)))

# ---------------------------------------------------------------- main
if __name__ == '__main__':
    args = [a for a in sys.argv[1:]]
    out = args[0]; rest = args[1:]
    os.makedirs(out, exist_ok=True)
    if rest and rest[0] == '--voids':
        from void_items import ROWS
        want = set(rest[1:])
        for row in ROWS:
            if want and row[0] not in want: continue
            reset(); void_icon(row[0], row[2]); R.render(os.path.join(out, row[0] + '.png'))
    else:
        for name, fn in ITEMS.items():
            if rest and name not in rest: continue
            reset(); fn(); R.render(os.path.join(out, name + '.png'))
    print('done')
