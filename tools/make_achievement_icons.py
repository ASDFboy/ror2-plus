"""Achievement (challenge) icons for every ror2-plus unlock, in the base game's item-unlock style, measured from the
vanilla icons in ror2-base-achievements (texAttackSpeedIcon, texFindTimedChestIcon, ...):

  128 px square, 2 px light rim in the tier hue, ~3 px dark band, then a swirling radial background in the tier hue
  that darkens toward the corners; the item fills most of the frame with a bright tier-coloured outline and soft glow.

Item art: ror2-plus items use their own icons (art/icons); ClassicItemsReturns items use their 3D icons from the asset
bundle. The icons' pickup stroke is trimmed off first, since achievement art uses a glow instead.
Output: art/achievements/<ID>.png (embedded in the DLL as icons.Ach_<ID>.png).
Run from the repo root: python3 tools/make_achievement_icons.py"""
import glob, json, math, os, re, sys
import numpy as np
from PIL import Image, ImageFilter

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
OUT = os.path.join(ROOT, 'art', 'achievements')
BUNDLE = os.path.join(ROOT, 'dist', 'plugins', 'ror2-plus', 'AssetBundles', 'classicitemsreturnsbundle')
W = 512                                   # working size; saved at 128 like vanilla

# rim, dark band, background dark -> light, outline glow   (measured from vanilla where available)
PALETTE = {
    'white': ((165, 178, 181), (101, 120, 123), [(52, 66, 70), (92, 110, 112), (140, 158, 156)], (226, 236, 236)),
    'green': ((66, 134, 49), (24, 77, 16), [(10, 40, 10), (24, 92, 16), (46, 128, 34)], (150, 240, 110)),
    'red':   ((156, 32, 24), (107, 12, 8), [(52, 4, 4), (110, 10, 8), (168, 30, 22)], (255, 140, 120)),
    'equip': ((165, 109, 8), (107, 52, 0), [(70, 20, 0), (150, 76, 6), (220, 150, 46)], (255, 206, 110)),
    'boss':  ((150, 172, 26), (84, 100, 8), [(34, 44, 4), (98, 118, 14), (170, 196, 40)], (232, 255, 120)),
    'lunar': ((40, 150, 180), (14, 72, 100), [(6, 24, 46), (20, 90, 130), (60, 170, 210)], (150, 230, 255)),
}

def rows():
    s = open(os.path.join(ROOT, 'src/Items/Complete/Unlocks.Generated.cs')).read()
    return re.findall(r'\("(\w+)", "(\w+)", (true|false), "([^"]*)"', s)

SRC = {f: open(f).read() for f in glob.glob(os.path.join(ROOT, 'src/**/*.cs'), recursive=True)}
TIERS = json.load(open(os.path.join(ROOT, 'tools/item_tiers.json')))

def source_of(token):
    """Returns (class name, source text) for the item/equipment class whose lang token is `token`:
    the nearest class declared before the token line that derives from an item/equipment base."""
    for f, s in SRC.items():
        m = re.search(r'LangTokenName\s*=>\s*"%s"' % re.escape(token), s)
        if not m: continue
        decls = [d for d in re.finditer(r'class (\w+)\s*:\s*(\w+)', s[:m.start()]) if re.search(r'Simple|Item|Equipment', d.group(2))]
        if decls:
            nxt = re.search(r'\n\s*(public |internal )?(abstract )?class ', s[m.end():])
            return decls[-1].group(1), s[decls[-1].start():m.end() + (nxt.start() if nxt else len(s))]
    return None, ''

def tier_of(cls, body, equip):
    if equip: return 'lunar' if re.search(r'IsLunar\s*=>\s*true', body) else ('boss' if re.search(r'IsBoss\s*=>\s*true', body) else 'equip')
    if cls in TIERS: t = TIERS[cls]; return {'white': 'white', 'green': 'green', 'red': 'red', 'boss': 'boss', 'equip': 'equip', 'lunar': 'lunar'}.get(t, 'white')
    m = re.search(r'Tier\s*=>\s*ItemTier\.(\w+)', body)
    return {'Tier1': 'white', 'Tier2': 'green', 'Tier3': 'red', 'Boss': 'boss', 'Lunar': 'lunar'}.get(m.group(1) if m else '', 'white')

_bundle = None
def bundle_sprite(name):
    global _bundle
    import UnityPy
    if _bundle is None:
        _bundle = {}
        for o in UnityPy.load(BUNDLE).objects:
            if o.type.name == 'Sprite':
                sp = o.read(); _bundle[sp.m_Name] = sp
    for pre in ('texIcon3d', 'texIcon', 'texIconClassic'):
        if pre + name in _bundle: return _bundle[pre + name].image.convert('RGBA')
    return None

def item_art(cls, body):
    p = os.path.join(ROOT, 'art', 'icons', cls + '.png') if cls else None
    if p and os.path.exists(p): return Image.open(p).convert('RGBA')
    m = re.search(r'Load(?:Item)?Sprite\("(\w+)"\)', body)
    return bundle_sprite(m.group(1)) if m else None

def strip_stroke(im):
    """Trim the pickup icon's tier stroke + dark line (3 px at 128) so only the object remains."""
    im = im.resize((W, W), Image.LANCZOS) if im.size[0] != im.size[1] else im.resize((W, W), Image.LANCZOS)
    a = im.getchannel('A').point(lambda v: 255 if v > 90 else 0)
    a = a.filter(ImageFilter.MinFilter(25))           # ~3 px at 128 -> 12 px at 512, both sides of the filter
    a = a.filter(ImageFilter.GaussianBlur(1.2))
    out = im.copy(); out.putalpha(Image.fromarray(np.minimum(np.asarray(a), np.asarray(im.getchannel('A')))))
    return out

def background(tier, seed):
    rim, band, (d0, d1, d2), _ = PALETTE[tier]
    y, x = np.mgrid[0:W, 0:W].astype(np.float32)
    cx, cy = W * 0.5, W * 0.46
    r = np.hypot(x - cx, y - cy) / (W * 0.72); ang = np.arctan2(y - cy, x - cx)
    rng = np.random.default_rng(seed)
    swirl = 0.5 + 0.5 * np.sin(ang * 2 + r * 9 + rng.uniform(0, 6.28)) * np.exp(-r * 0.8)
    swirl += 0.22 * np.sin(ang * 3 - r * 14 + rng.uniform(0, 6.28))
    img = Image.fromarray((np.clip(swirl, 0, 1) * 255).astype(np.uint8)).filter(ImageFilter.GaussianBlur(10))
    t = np.asarray(img).astype(np.float32) / 255
    light = np.clip(1.05 - r * 0.95, 0, 1)                       # brighter centre, dark corners
    v = np.clip(0.35 * t + 0.75 * light - 0.1, 0, 1)
    c0, c1, c2 = (np.array(c, np.float32) for c in (d0, d1, d2))
    col = np.where(v[..., None] < 0.5, c0 + (c1 - c0) * (v[..., None] / 0.5), c1 + (c2 - c1) * ((v[..., None] - 0.5) / 0.5))
    bg = Image.fromarray(np.clip(col, 0, 255).astype(np.uint8), 'RGB').convert('RGBA')
    # frame: 2 px rim, ~3 px dark band (at 128) -> x4 here
    f = np.asarray(bg).copy(); k = W // 128
    f[:2 * k, :, :3] = f[-2 * k:, :, :3] = rim; f[:, :2 * k, :3] = f[:, -2 * k:, :3] = rim
    for s in (slice(2 * k, 5 * k), slice(W - 5 * k, W - 2 * k)):
        f[s, 2 * k:W - 2 * k, :3] = band; f[2 * k:W - 2 * k, s, :3] = band
    return Image.fromarray(f, 'RGBA')

def compose(art, tier, seed):
    _, _, _, glow = PALETTE[tier]
    art = strip_stroke(art)
    bbox = art.getchannel('A').point(lambda v: 255 if v > 40 else 0).getbbox()
    art = art.crop(bbox); s = (W * 0.80) / max(art.size)
    art = art.resize((max(1, int(art.size[0] * s)), max(1, int(art.size[1] * s))), Image.LANCZOS)
    layer = Image.new('RGBA', (W, W)); layer.alpha_composite(art, ((W - art.size[0]) // 2, (W - art.size[1]) // 2 + W // 64))
    a = layer.getchannel('A').point(lambda v: 255 if v > 60 else 0)
    out = background(tier, seed)
    soft = Image.new('RGBA', (W, W), glow + (255,)); soft.putalpha(a.filter(ImageFilter.MaxFilter(25)).filter(ImageFilter.GaussianBlur(14)).point(lambda v: int(v * 0.55)))
    line = Image.new('RGBA', (W, W), glow + (255,)); line.putalpha(a.filter(ImageFilter.MaxFilter(13)).filter(ImageFilter.GaussianBlur(1.5)))
    out.alpha_composite(soft); out.alpha_composite(line); out.alpha_composite(layer)
    return out.resize((128, 128), Image.LANCZOS)

def main():
    os.makedirs(OUT, exist_ok=True); made = []
    for i, (aid, token, equip, name) in enumerate(rows()):
        cls, body = source_of(token)
        art = item_art(cls, body)
        if art is None: print('NO ART', aid, token, cls); continue
        tier = tier_of(cls, body, equip == 'true')
        compose(art, tier, i * 7919).save(os.path.join(OUT, aid + '.png')); made.append((aid, tier))
    print(len(made), 'icons:', made)

if __name__ == '__main__':
    main()
