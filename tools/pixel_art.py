"""Tiny pixel-art engine: shaded primitives on a 32x32 grid, automatic ink outline and tier outline,
then nearest-neighbour upscale. Designs are original."""
import math
from PIL import Image

L = (-0.55, -0.7, 0.55)
_n = math.sqrt(sum(c * c for c in L)); L = tuple(c / _n for c in L)
INK = (24, 14, 34, 255)
TIER = {'white': (236, 236, 236, 255), 'green': (120, 214, 88, 255), 'red': (232, 74, 64, 255),
        'boss': (240, 208, 60, 255), 'equip': (255, 150, 40, 255), 'void': (206, 84, 240, 255)}

def hexc(h, a=255):
    h = h.lstrip('#'); return (int(h[0:2], 16), int(h[2:4], 16), int(h[4:6], 16), a)

class Canvas:
    def __init__(self, n=32):
        self.n = n; self.px = [[None] * n for _ in range(n)]

    def put(self, x, y, c):
        if 0 <= x < self.n and 0 <= y < self.n and c is not None: self.px[y][x] = c

    def shade(self, ramp, nx, ny, nz, bias=0.0):
        lam = max(0.0, nx * L[0] + ny * L[1] + nz * L[2]) + bias
        i = min(len(ramp) - 1, max(0, int(lam * len(ramp) * 0.999)))
        return hexc(ramp[i]) if isinstance(ramp[i], str) else ramp[i]

    def ellipsoid(self, cx, cy, rx, ry, ramp, bias=0.0, mask=None):
        for y in range(self.n):
            for x in range(self.n):
                dx = (x + 0.5 - cx) / rx; dy = (y + 0.5 - cy) / ry
                d = dx * dx + dy * dy
                if d <= 1 and (mask is None or mask(x, y, dx, dy)):
                    self.put(x, y, self.shade(ramp, dx, dy, math.sqrt(max(0, 1 - d)), bias))

    def tube(self, x0, y0, x1, y1, r, ramp, bias=0.0, caps=True):
        ax, ay = x1 - x0, y1 - y0; ln = math.hypot(ax, ay); ux, uy = ax / ln, ay / ln; px_, py_ = -uy, ux
        for y in range(self.n):
            for x in range(self.n):
                vx, vy = x + 0.5 - x0, y + 0.5 - y0
                t = vx * ux + vy * uy; s = vx * px_ + vy * py_
                if caps: t2 = min(max(t, 0), ln)
                else:
                    if t < 0 or t > ln: continue
                    t2 = t
                ex, ey = vx - t2 * ux, vy - t2 * uy
                d = math.hypot(ex, ey)
                if d <= r:
                    k = s / r if (0 <= t <= ln) else math.hypot(ex, ey) / r
                    nx, ny = (ex / r, ey / r) if not (0 <= t <= ln) else (px_ * s / r, py_ * s / r)
                    self.put(x, y, self.shade(ramp, nx, ny, math.sqrt(max(0, 1 - (d / r) ** 2)), bias))

    def poly(self, pts, color):
        c = hexc(color) if isinstance(color, str) else color
        for y in range(self.n):
            for x in range(self.n):
                if _inside(x + 0.5, y + 0.5, pts): self.put(x, y, c)

    def line(self, x0, y0, x1, y1, color):
        c = hexc(color) if isinstance(color, str) else color
        n = int(max(abs(x1 - x0), abs(y1 - y0))) + 1
        for i in range(n + 1):
            t = i / max(1, n); self.put(round(x0 + (x1 - x0) * t), round(y0 + (y1 - y0) * t), c)

    def dot(self, x, y, color): self.put(x, y, hexc(color) if isinstance(color, str) else color)

    def finish(self, tier, scale=4, out=None):
        n = self.n
        src = [row[:] for row in self.px]
        ink = [[None] * n for _ in range(n)]
        for y in range(n):
            for x in range(n):
                if src[y][x] is None and any(0 <= x + dx < n and 0 <= y + dy < n and src[y + dy][x + dx] is not None for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1))):
                    ink[y][x] = INK
        for y in range(n):
            for x in range(n):
                if ink[y][x]: src[y][x] = ink[y][x]
        tc = TIER[tier]
        final = [row[:] for row in src]
        for y in range(n):
            for x in range(n):
                if src[y][x] is None and any(0 <= x + dx < n and 0 <= y + dy < n and src[y + dy][x + dx] is not None for dx in (-1, 0, 1) for dy in (-1, 0, 1)):
                    final[y][x] = tc
        im = Image.new('RGBA', (n, n), (0, 0, 0, 0))
        for y in range(n):
            for x in range(n):
                if final[y][x]: im.putpixel((x, y), final[y][x])
        im = im.resize((n * scale, n * scale), Image.NEAREST)
        if out: im.save(out)
        return im

def _inside(x, y, pts):
    c = False; j = len(pts) - 1
    for i in range(len(pts)):
        xi, yi = pts[i]; xj, yj = pts[j]
        if (yi > y) != (yj > y) and x < (xj - xi) * (y - yi) / (yj - yi) + xi: c = not c
        j = i
    return c
