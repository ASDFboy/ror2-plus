"""Four sample sprites in the pixel style (original designs)."""
import sys, os, math
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from pixel_art import Canvas

OUT = sys.argv[1]
MEAT = ['#4a1414', '#7c2420', '#a8392c', '#cf5a42', '#ee8a6a']
FAT = ['#c98f84', '#e8b4a6', '#fbd9cd']
BONE = ['#8f8170', '#c9bba4', '#efe6d4', '#fffaf0']

def meat():
    c = Canvas()
    lump = lambda x, y, dx, dy: (dx * dx + dy * dy) < 1 - 0.12 * math.sin(math.atan2(dy, dx) * 5) ** 2
    c.ellipsoid(15, 18, 11, 9, MEAT, mask=lump)
    c.ellipsoid(20, 13, 6, 5, MEAT, bias=0.08)
    # marbling
    for (x0, y0, x1, y1) in ((8, 17, 13, 15), (13, 15, 17, 18), (11, 22, 16, 21), (18, 20, 22, 22)):
        c.line(x0, y0, x1, y1, FAT[1])
    c.dot(9, 16, FAT[2]); c.dot(14, 16, FAT[2])
    # bone end poking out
    c.tube(22, 22, 28, 27, 1.6, BONE, caps=False)
    c.ellipsoid(28.5, 26, 2.2, 2.2, BONE); c.ellipsoid(27, 28.5, 2.2, 2.2, BONE)
    # glossy highlights
    for p in ((11, 13), (12, 13), (19, 10)): c.dot(*p, '#ffc2a8')
    return c

CANOPY_A = ['#1d3c7a', '#2856a8', '#3a7ad6', '#6aa6f0', '#a8d0ff']
CANOPY_B = ['#14295a', '#1d3f80', '#2a5cb0', '#4f86d6', '#86b4ee']
WOOD = ['#3a2214', '#5e3820', '#8a5630', '#b8804e']

def umbrella():
    c = Canvas()
    # canopy: half-ellipse dome with 6 alternating panels and a scalloped rim
    cx, cy, rx, ry = 16, 15, 14, 11
    for y in range(32):
        for x in range(32):
            dx = (x + 0.5 - cx) / rx; dy = (y + 0.5 - cy) / ry
            if dy > 0.05: continue
            rim = 0.05 - 0.09 * abs(math.sin((x + 0.5 - cx) / rx * math.pi * 3))
            d = dx * dx + dy * dy
            if d > 1 or dy > rim: continue
            ang = math.atan2(-dy, dx)
            panel = int((ang / math.pi) * 6) % 2
            ramp = CANOPY_A if panel == 0 else CANOPY_B
            c.put(x, y, c.shade(ramp, dx, dy, math.sqrt(max(0, 1 - d)), 0.1))
    # rib seams
    for k in range(1, 6):
        a = math.pi * k / 6
        c.line(cx, cy - ry + 1, cx + math.cos(a) * rx * 0.97, cy - math.sin(a) * 2, '#13254f')
    c.tube(16, 2, 16, 4.5, 1.2, ['#8a6a20', '#c9a24a', '#f0d27a'])
    # shaft and J handle
    c.tube(16, 15, 16, 27, 1.0, WOOD, caps=False)
    for i in range(10):
        a = math.pi * i / 9
        c.tube(19 - 3 * math.cos(a) - 0.01, 27 + 3 * math.sin(a), 19 - 3 * math.cos(a + 0.35) + 0.01, 27 + 3 * math.sin(a + 0.35), 1.1, WOOD)
    c.dot(11, 7, '#d8ecff'); c.dot(12, 6, '#d8ecff'); c.dot(10, 9, '#bcdcff')
    return c

METAL = ['#14171e', '#262b36', '#3c4352', '#5d6678', '#8d97ab']
BRASS = ['#5c3e14', '#8a6020', '#bf8e3a', '#e8c070']

def scope():
    c = Canvas()
    # main tube on a diagonal, bell at the front, eyepiece at the back
    c.tube(7, 23, 23, 9, 3.0, METAL, caps=False)
    c.tube(22, 10, 26.5, 6, 4.4, METAL, caps=False)
    c.tube(4.5, 25.5, 8, 22, 3.6, METAL, caps=False)
    # brass rings
    c.tube(21.6, 10.4, 22.6, 9.4, 4.6, BRASS, caps=False)
    c.tube(8.2, 21.8, 9.2, 20.8, 3.8, BRASS, caps=False)
    # turret knobs
    c.tube(14, 14.5, 11.5, 12, 1.6, BRASS); c.tube(16.8, 17.2, 19.2, 19.6, 1.4, BRASS)
    # front lens with glint
    c.ellipsoid(27.4, 5.1, 3.0, 3.0, ['#0d3a48', '#1a6f86', '#3fb4cf', '#a8f2ff'], mask=lambda x, y, dx, dy: dx + dy > -0.2)
    c.dot(27, 4, '#ffffff'); c.dot(28, 4, '#e0fbff')
    # rail highlight
    c.line(9, 20, 21, 8, '#a8b2c6')
    return c

GLASS = [(150, 120, 180, 150), (180, 160, 210, 165), (215, 205, 240, 180), (245, 240, 255, 200)]
FLUID = ['#3a0a5a', '#6a1898', '#a43ad6', '#de78ff', '#ffd0ff']
TENDRIL = ['#1e0830', '#3a1458', '#62248c', '#8c3ab8']

def void_syringe():
    c = Canvas()
    # barrel (glass) with glowing fluid inside, diagonal
    c.tube(9, 22, 21, 10, 4.2, GLASS, caps=False)
    c.tube(10.5, 20.5, 19, 12, 3.0, FLUID, bias=0.25, caps=False)
    # plunger + flange
    c.tube(5, 26, 9.5, 21.5, 1.4, METAL)
    c.tube(3, 25, 7, 29, 1.3, METAL)
    c.tube(6.5, 19.5, 11.5, 24.5, 1.1, METAL, caps=False)
    # hub and needle
    c.tube(21, 10, 23, 8, 2.0, METAL, caps=False)
    c.line(23, 8, 29, 2, '#d6dbe6'); c.line(24, 8, 29, 3, '#7c8496')
    # void tendrils curling off the barrel
    for (x0, y0, x1, y1, r) in ((12, 23, 15, 27, 1.2), (15, 27, 18, 27.5, 1.0), (19, 17, 23, 19, 1.1), (23, 19, 25, 22, 0.9), (10, 14, 7, 12, 1.0)):
        c.tube(x0, y0, x1, y1, r, TENDRIL)
    for p in ((18, 27), (25, 22), (7, 12)): c.dot(*p, '#e88cff')
    # bubbles and glass glint
    for p in ((14, 16), (17, 14), (12, 18)): c.dot(*p, '#ffe6ff')
    c.line(11, 17, 17, 11, (255, 255, 255, 210))
    return c

if __name__ == '__main__':
    os.makedirs(OUT, exist_ok=True)
    meat().finish('white', out=os.path.join(OUT, 'px_meat.png'))
    umbrella().finish('red', out=os.path.join(OUT, 'px_umbrella.png'))
    scope().finish('red', out=os.path.join(OUT, 'px_scope.png'))
    void_syringe().finish('void', out=os.path.join(OUT, 'px_voidsyringe.png'))
    print('ok')
