"""The ror2-plus expansion emblem, in the style of the game's own expansion icons (texVoidExpansionIcon,
texHardwareExpansionIcon, texHallowedExpansionIcon): one flat saturated symbol, a dark outline, a soft white outer
stroke, and a few faint splatters of the same colour behind it. 512 x 512, like vanilla.
The symbol is our own: a chunky plus inside a broken ring (four arcs between the arms).
Writes art/icons/ExpansionIcon.png and art/icons/ExpansionIconDisabled.png."""
import math, os, random
import numpy as np
from scipy.ndimage import distance_transform_edt
from PIL import Image, ImageDraw, ImageFilter, ImageOps

def grow(mask, px):
    """Dilate a 0/255 mask by px (fast, via a distance transform)."""
    d = distance_transform_edt(np.asarray(mask) < 128)
    return Image.fromarray(((d <= px) * 255).astype(np.uint8))

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
SS = 4; N = 512 * SS
COLOR = (245, 128, 44)            # warm Risk of Rain orange; vanilla expansions use pink, cyan and violet
DARK = (52, 46, 50)

def symbol():
    m = Image.new('L', (N, N), 0); d = ImageDraw.Draw(m); c = N / 2
    arm, half = N * 0.11, N * 0.28                               # plus: arm half-width and length
    for w, h in ((arm, half), (half, arm)):
        d.rounded_rectangle((c - w, c - h, c + w, c + h), radius=int(N * 0.03), fill=255)
    R1, R2 = N * 0.39, N * 0.29                                   # ring between the arms
    for k in range(4):
        a0, a1 = 90 * k + 22, 90 * k + 68
        ring = Image.new('L', (N, N), 0); rd = ImageDraw.Draw(ring)
        rd.pieslice((c - R1, c - R1, c + R1, c + R1), a0, a1, fill=255)
        rd.ellipse((c - R2, c - R2, c + R2, c + R2), fill=0)
        m.paste(255, mask=ring)
    # dark hollow in the centre, like the inner line work on the vanilla emblems
    r = N * 0.055; d.ellipse((c - r, c - r, c + r, c + r), fill=0)
    return m

def build(color, splat=True):
    sym = symbol()
    out = Image.new('RGBA', (N, N), (0, 0, 0, 0))
    if splat:
        rnd = random.Random(7); sp = Image.new('L', (N, N), 0); sd = ImageDraw.Draw(sp)
        for _ in range(14):
            a = rnd.uniform(0, 6.283); r = rnd.uniform(N * 0.3, N * 0.46); s = rnd.uniform(N * 0.02, N * 0.06)
            x, y = N / 2 + math.cos(a) * r, N / 2 + math.sin(a) * r; sd.ellipse((x - s, y - s, x + s, y + s), fill=255)
        sp = sp.filter(ImageFilter.GaussianBlur(SS * 2))
        layer = Image.new('RGBA', (N, N), color + (255,)); layer.putalpha(sp.point(lambda v: int(v * 0.35))); out.alpha_composite(layer)
    halo = grow(sym, 22 * SS).filter(ImageFilter.GaussianBlur(5 * SS))
    w = Image.new('RGBA', (N, N), (255, 255, 255, 255)); w.putalpha(halo.point(lambda v: int(v * 0.9))); out.alpha_composite(w)
    ol = Image.new('RGBA', (N, N), DARK + (255,)); ol.putalpha(grow(sym, 12 * SS)); out.alpha_composite(ol)
    fill = Image.new('RGBA', (N, N), color + (255,)); fill.putalpha(sym); out.alpha_composite(fill)
    return out.resize((512, 512), Image.LANCZOS)

if __name__ == '__main__':
    build(COLOR).save(os.path.join(ROOT, 'art', 'icons', 'ExpansionIcon.png'))
    build((150, 150, 150), splat=False).save(os.path.join(ROOT, 'art', 'icons', 'ExpansionIconDisabled.png'))
    print('ok')
