"""Original artifact icons: carved stone tablet with a raised glowing glyph, matte painted style.
Usage: /root/tools/bpyenv/bin/python tools/artifact_icons.py OUTDIR"""
import sys, os, math
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import bpy
from ror2style import reset, paint, glowmat, smooth, cyl, sphere, tube, group, render
from PIL import Image, ImageEnhance, ImageOps

OUT = sys.argv[-1]

def tablet(stone_hex):
    stone = paint('stone', stone_hex, grime=0.45, wear=0.55)
    bpy.ops.mesh.primitive_cube_add(size=2)
    o = bpy.context.object; o.scale = (1.15, 0.22, 1.15); o.data.materials.append(stone)
    b = o.modifiers.new('b', 'BEVEL'); b.width = 0.18; b.segments = 4
    t = bpy.data.textures.new('rock', 'CLOUDS'); t.noise_scale = 0.35
    o.modifiers.new('sub', 'SUBSURF').levels = 2
    d = o.modifiers.new('d', 'DISPLACE'); d.texture = t; d.strength = 0.05
    for p in o.data.polygons: p.use_smooth = True
    # inner recessed rim
    rim = cyl(1.0, 0.08, (0, -0.24, 0), (math.radians(90), 0, 0), paint('rim', stone_hex, grime=0.6, wear=0.7), verts=4)
    rim.rotation_euler[1] = math.radians(45); rim.scale = (1.0, 1.0, 1.0)
    rim.hide_render = True
    return [o]

def Y(z): return -0.33  # glyph plane just in front of the tablet

def glyph_distortion(m):
    parts = []
    pts = [(0.75 * (1 - i / 22) * math.cos(i * 0.62), -0.34, 0.75 * (1 - i / 22) * math.sin(i * 0.62)) for i in range(20)]
    parts.append(tube(pts, 0.07, m, taper=[1.0 - 0.6 * i / 20 for i in range(20)]))
    # broken shards flying off the spiral
    for x, z in ((0.78, 0.55), (-0.72, -0.6), (0.62, -0.72)):
        s = sphere(0.09, (x, -0.34, z), m, (1.4, 0.6, 0.6)); s.rotation_euler = (0, math.radians(x * 70), 0); parts.append(s)
    return parts

def glyph_spirit(m):
    parts = []
    # central flame with two swept wings curling outward
    parts.append(tube([(0, -0.34, -0.75), (0.12, -0.34, -0.2), (-0.1, -0.34, 0.3), (0.05, -0.34, 0.85)], 0.13, m, taper=[1.0, 0.9, 0.55, 0.1]))
    for sx in (1, -1):
        parts.append(tube([(0.1 * sx, -0.34, -0.55), (0.45 * sx, -0.34, -0.35), (0.75 * sx, -0.34, 0.05), (0.8 * sx, -0.34, 0.6)], 0.09, m, taper=[0.9, 0.8, 0.5, 0.1]))
        parts.append(tube([(0.15 * sx, -0.34, -0.35), (0.42 * sx, -0.34, -0.05), (0.52 * sx, -0.34, 0.45)], 0.06, m, taper=[0.8, 0.6, 0.1]))
    parts.append(sphere(0.15, (0, -0.34, -0.7), m))
    return parts

def glyph_origin(m):
    parts = []
    # three claw slashes over a downward-pointing eye
    for dx in (-0.4, 0.0, 0.4):
        pts = [(dx - 0.25, -0.34, 0.8), (dx - 0.05, -0.34, 0.2), (dx + 0.18, -0.34, -0.45)]
        parts.append(tube(pts, 0.085, m, taper=[0.2, 1.0, 0.25]))
    e = sphere(0.18, (0, -0.36, -0.68), m, (1.8, 0.5, 0.8)); parts.append(e)
    return parts

def glyph_tempus(m):
    parts = []
    # hourglass
    parts.append(tube([(-0.5, -0.34, 0.75), (0.5, -0.34, 0.75)], 0.07, m))
    parts.append(tube([(-0.5, -0.34, -0.75), (0.5, -0.34, -0.75)], 0.07, m))
    parts.append(tube([(-0.42, -0.34, 0.7), (-0.05, -0.34, 0.05), (-0.42, -0.34, -0.7)], 0.055, m))
    parts.append(tube([(0.42, -0.34, 0.7), (0.05, -0.34, 0.05), (0.42, -0.34, -0.7)], 0.055, m))
    parts.append(sphere(0.12, (0, -0.34, -0.55), m, (2.0, 0.5, 1.0)))
    parts.append(sphere(0.07, (0, -0.34, 0.45), m, (2.5, 0.5, 0.8)))
    return parts

ART = {
    'Distortion': ('#4b4558', '#c58bff', glyph_distortion),
    'Spirit':     ('#4a5560', '#8ef0ff', glyph_spirit),
    'Origin':     ('#5a4646', '#ff6a4a', glyph_origin),
    'Tempus':     ('#55513f', '#ffd36a', glyph_tempus),
}

def make(name):
    stone, glow, fn = ART[name]
    reset()
    parts = tablet(stone)
    parts += fn(glowmat('glyph', glow, 2.2))
    group(parts, rot=(0, 0, 0))
    cam = bpy.context.scene.camera
    cam.location = (0, -9, 0); cam.rotation_euler = (math.radians(90), 0, 0)
    raw = os.path.join(OUT, f'_raw_{name}.png'); render(raw)
    im = Image.open(raw).convert('RGBA'); bb = im.getbbox(); im = im.crop(bb)
    side = max(im.size); sq = Image.new('RGBA', (side, side)); sq.paste(im, ((side - im.width) // 2, (side - im.height) // 2))
    on = sq.resize((256, 256), Image.LANCZOS)
    on.save(os.path.join(OUT, f'Art_{name}_On.png'))
    g = ImageOps.grayscale(on.convert('RGB')); g = ImageEnhance.Brightness(g).enhance(0.6)
    off = Image.merge('RGBA', (g, g, g, on.split()[3])); off.save(os.path.join(OUT, f'Art_{name}_Off.png'))
    os.remove(raw)

if __name__ == '__main__':
    os.makedirs(OUT, exist_ok=True)
    for n in (sys.argv[2:-1] or ART): make(n)
    print('done')
