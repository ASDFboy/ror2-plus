"""Turns a transparent render into a finished icon matching the vanilla icon spec measured in reference/STYLE.md.
At 128 px vanilla icons have: a ~2 px tier-coloured stroke, then a ~1 px dark line, then the object. Void icons use a
two-tone stroke (light violet outside, magenta inside, no dark line) and a small void badge in the top-left corner.
Some icons get a few droplets scattered beside the object, outlined the same way. Everything here is drawn by us."""
import zlib, random, math
from PIL import Image, ImageFilter, ImageDraw

TIER = {'white': (178, 196, 190), 'green': (104, 182, 57), 'red': (239, 85, 62), 'boss': (173, 199, 35),
        'equip': (214, 130, 49), 'lunar': (52, 194, 227)}
VOID_OUT, VOID_IN = (176, 112, 214), (165, 50, 123)
INK = (24, 20, 33)
S = 512                     # working size (4x the 128 px vanilla spec)

def _mask(img, thr=60):
    return img.getchannel('A').point(lambda v: 255 if v > thr else 0)

def _grow(mask, px):
    return mask.filter(ImageFilter.MaxFilter(2 * px + 1)) if px > 0 else mask

def _layer(mask, rgb, blur=1.0):
    l = Image.new('RGBA', mask.size, rgb + (255,)); l.putalpha(mask.filter(ImageFilter.GaussianBlur(blur)) if blur else mask); return l

def _droplets(c, name):
    """A few droplets beside the object in its own darker colour (vanilla icons often have these)."""
    r = random.Random(zlib.crc32(('drops' + name).encode()))
    if r.random() > 0.45: return c
    a = _mask(c); bbox = a.getbbox()
    if not bbox: return c
    px = c.convert('RGB').resize((32, 32)); cols = [p for p, al in zip(px.getdata(), c.getchannel('A').resize((32, 32)).getdata()) if al > 200]
    cols.sort(key=sum); col = cols[len(cols) // 4] if cols else (60, 50, 60)
    d = ImageDraw.Draw(c)
    for _ in range(r.randint(1, 3)):
        for _try in range(20):
            x = r.uniform(bbox[0] - 30, bbox[2] + 30); y = r.uniform(bbox[1] - 30, bbox[3] + 30)
            rad = r.uniform(9, 18)
            if 40 < x < S - 40 and 40 < y < S - 40 and a.getpixel((int(x), int(y))) == 0 and _grow(a, 30).getpixel((int(x), int(y))) > 0: break
        else: continue
        d.ellipse((x - rad, y - rad * 1.15, x + rad, y + rad * 1.15), fill=col + (255,))
        d.ellipse((x - rad * 0.45, y - rad * 0.7, x - rad * 0.05, y - rad * 0.3), fill=tuple(min(255, v + 60) for v in col) + (255,))
    return c

def _void_badge(size=S):
    """Our own void marker: a pink disc with a dark open ring and a small rift below it, outlined like the icon."""
    b = Image.new('RGBA', (size, size), (0, 0, 0, 0)); d = ImageDraw.Draw(b)
    cx, cy, R = 70, 70, 46
    d.ellipse((cx - R - 10, cy - R - 10, cx + R + 10, cy + R + 10), fill=VOID_OUT + (255,))
    d.ellipse((cx - R - 4, cy - R - 4, cx + R + 4, cy + R + 4), fill=VOID_IN + (255,))
    d.ellipse((cx - R, cy - R, cx + R, cy + R), fill=(232, 96, 150, 255))
    d.ellipse((cx - 23, cy - 30, cx + 23, cy + 16), outline=(44, 10, 40, 255), width=10)
    d.ellipse((cx - 7, cy - 14, cx + 7, cy), fill=(44, 10, 40, 255))
    for k in (-1, 1):
        d.arc((cx - 19 + k * 12, cy + 8, cx + 19 + k * 12, cy + 40), 200 if k < 0 else 270, 340 if k < 0 else 20, fill=(44, 10, 40, 255), width=8)
    return b

def _paint(im, radius=3):
    """Kuwahara filter + gentle contrast/saturation lift: turns a clean render into flat painted brush strokes,
    which is the surface quality vanilla icons have."""
    import numpy as np
    from scipy.ndimage import uniform_filter
    from PIL import ImageEnhance
    a = np.asarray(im).astype(np.float32); rgb = a[..., :3]; al = a[..., 3:4]
    lum = rgb @ np.array([0.299, 0.587, 0.114], np.float32)
    k = radius + 1; best_v = None; out = np.zeros_like(rgb)
    for dy in (0, -radius):
        for dx in (0, -radius):
            m = np.stack([uniform_filter(rgb[..., c], k) for c in range(3)], -1)
            v = uniform_filter(lum * lum, k) - uniform_filter(lum, k) ** 2
            m = np.roll(m, (dy + radius // 2, dx + radius // 2), (0, 1)); v = np.roll(v, (dy + radius // 2, dx + radius // 2), (0, 1))
            if best_v is None: best_v, out = v, m
            else:
                sel = v < best_v; best_v = np.where(sel, v, best_v); out = np.where(sel[..., None], m, out)
    res = Image.fromarray(np.concatenate([np.clip(out, 0, 255), al], -1).astype(np.uint8), 'RGBA')
    res = ImageEnhance.Contrast(res).enhance(1.12); res = ImageEnhance.Color(res).enhance(1.1)
    res.putalpha(im.getchannel('A')); return res

def post(src, tier, size=256, name=None):
    im = Image.open(src).convert('RGBA')
    bbox = _mask(im, 20).getbbox()
    im = im.crop(bbox); w, h = im.size; sc = 420 / max(w, h)
    im = im.resize((max(1, int(w * sc)), max(1, int(h * sc))), Image.LANCZOS)
    c = Image.new('RGBA', (S, S), (0, 0, 0, 0)); c.alpha_composite(im, ((S - im.size[0]) // 2, (S - im.size[1]) // 2))
    c = _paint(c)
    if name: c = _droplets(c, name)
    a = _mask(c)
    out = Image.new('RGBA', (S, S), (0, 0, 0, 0))
    if tier == 'void':
        halo = _layer(_grow(a, 16), (210, 120, 230), 6.0)
        halo.putalpha(halo.getchannel('A').point(lambda v: int(v * 0.35)))
        out.alpha_composite(halo)
        out.alpha_composite(_layer(_grow(a, 12), VOID_OUT, 1.0))
        out.alpha_composite(_layer(_grow(a, 5), VOID_IN, 0.8))
    else:
        out.alpha_composite(_layer(_grow(a, 12), TIER[tier], 1.0))
        out.alpha_composite(_layer(_grow(a, 4), INK, 0.6))
    out.alpha_composite(c)
    if tier == 'void': out.alpha_composite(_void_badge())
    return out.resize((size, size), Image.LANCZOS)

if __name__ == '__main__':
    import sys
    post(sys.argv[1], sys.argv[2], name=sys.argv[4] if len(sys.argv) > 4 else None).save(sys.argv[3])
