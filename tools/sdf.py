"""Tiny numpy SDF raymarcher that renders toon-shaded item icons in the same
look as ClassicItemsReturns' 3D icons: cel-shaded body, dark ink line, and a
tier-coloured outer outline."""
import numpy as np
from PIL import Image, ImageFilter

TIER = {
    'white': (235, 235, 235), 'green': (110, 210, 80), 'red': (230, 70, 60),
    'boss': (240, 210, 60), 'equip': (255, 150, 40), 'lunar': (90, 160, 255),
}

def rot(rx=0, ry=0, rz=0):
    rx, ry, rz = np.radians([rx, ry, rz])
    X = np.array([[1, 0, 0], [0, np.cos(rx), -np.sin(rx)], [0, np.sin(rx), np.cos(rx)]])
    Y = np.array([[np.cos(ry), 0, np.sin(ry)], [0, 1, 0], [-np.sin(ry), 0, np.cos(ry)]])
    Z = np.array([[np.cos(rz), -np.sin(rz), 0], [np.sin(rz), np.cos(rz), 0], [0, 0, 1]])
    return Z @ Y @ X

def hexc(c):
    if isinstance(c, str):
        c = c.lstrip('#'); return np.array([int(c[i:i + 2], 16) for i in (0, 2, 4)]) / 255.
    return np.array(c) / 255.

class P:
    """Primitive. kind: sphere(r) box(x,y,z,rad) cap(a,b,r) cyl(r,h,rad) torus(R,r) cone(r,h) ell(x,y,z)"""
    def __init__(self, kind, args, pos=(0, 0, 0), r=(0, 0, 0), color='#ffffff', sub=False, smooth=0.0):
        self.kind, self.args, self.pos = kind, args, np.array(pos, float)
        self.R = rot(*r).T  # world->local
        self.color, self.sub, self.smooth = hexc(color), sub, smooth

    def sdf(self, p):
        q = (p - self.pos) @ self.R.T
        a = self.args; k = self.kind
        if k == 'sphere':
            return np.linalg.norm(q, axis=-1) - a[0]
        if k == 'ell':
            r = np.array(a[:3]); k0 = np.linalg.norm(q / r, axis=-1); k1 = np.linalg.norm(q / (r * r), axis=-1)
            return k0 * (k0 - 1.0) / np.maximum(k1, 1e-6)
        if k == 'box':
            b = np.array(a[:3]); rad = a[3] if len(a) > 3 else 0
            d = np.abs(q) - b + rad
            return np.linalg.norm(np.maximum(d, 0), axis=-1) + np.minimum(d.max(-1), 0) - rad
        if k == 'cap':
            A, B, r = np.array(a[0]), np.array(a[1]), a[2]
            pa, ba = q - A, B - A
            h = np.clip((pa @ ba) / (ba @ ba), 0, 1)
            return np.linalg.norm(pa - h[..., None] * ba, axis=-1) - r
        if k == 'cyl':  # along local y
            r, h = a[0], a[1]; rad = a[2] if len(a) > 2 else 0
            d = np.stack([np.linalg.norm(q[..., [0, 2]], axis=-1) - r + rad, np.abs(q[..., 1]) - h + rad], -1)
            return np.minimum(d.max(-1), 0) + np.linalg.norm(np.maximum(d, 0), axis=-1) - rad
        if k == 'torus':  # ring in local xz plane
            R, r = a
            qx = np.linalg.norm(q[..., [0, 2]], axis=-1) - R
            return np.sqrt(qx ** 2 + q[..., 1] ** 2) - r
        if k == 'cone':  # tip at +h on y, base radius r at y=-h
            r, h = a
            qx = np.linalg.norm(q[..., [0, 2]], axis=-1); qy = q[..., 1]
            t = np.clip((h - qy) / (2 * h), 0, 1)
            rr = r * t
            d_side = (qx - rr) * (2 * h) / np.hypot(2 * h, r)
            return np.maximum(d_side, np.maximum(-qy - h, qy - h))
        raise ValueError(k)

def scene_sdf(prims, p):
    d = np.full(p.shape[:-1], 1e9); mid = np.full(p.shape[:-1], -1)
    for i, pr in enumerate(prims):
        di = pr.sdf(p)
        if pr.sub:
            d = np.maximum(d, -di)
        elif pr.smooth > 0:
            k = pr.smooth; h = np.clip(0.5 + 0.5 * (di - d) / k, 0, 1)
            nd = di * (1 - h) + d * h - k * h * (1 - h)
            mid = np.where(di < d, i, mid); d = nd
        else:
            mid = np.where(di < d, i, mid); d = np.minimum(d, di)
    return d, mid

def render(prims, tier, out, size=256, ss=2, cam_rot=(22, 30, 0), zoom=1.0, extra_glow=None):
    N = size * ss
    ys, xs = np.mgrid[0:N, 0:N]
    u = (xs + 0.5) / N * 2 - 1; v = 1 - (ys + 0.5) / N * 2
    R = rot(*cam_rot)
    span = 1.32 / zoom
    ro = np.stack([u * span, v * span, np.full_like(u, -5.0)], -1) @ R.T
    rd = np.broadcast_to(np.array([0, 0, 1.0]) @ R.T, ro.shape)
    t = np.zeros(u.shape); hit = np.zeros(u.shape, bool); alive = np.ones(u.shape, bool)
    flat_ro, flat_rd = ro.reshape(-1, 3), rd.reshape(-1, 3)
    t = t.reshape(-1); hit = hit.reshape(-1); alive = alive.reshape(-1)
    for _ in range(110):
        idx = np.nonzero(alive)[0]
        if idx.size == 0: break
        p = flat_ro[idx] + flat_rd[idx] * t[idx, None]
        d, _ = scene_sdf(prims, p)
        t[idx] += d * 0.9
        h = d < 1e-3; hit[idx[h]] = True; alive[idx[h]] = False
        alive[idx[t[idx] > 10]] = False
    img = np.zeros((N * N, 4))
    hi = np.nonzero(hit)[0]
    if hi.size:
        p = flat_ro[hi] + flat_rd[hi] * t[hi, None]
        _, mid = scene_sdf(prims, p)
        e = 1.5e-3; nrm = np.zeros_like(p)
        for k in range(3):
            o = np.zeros(3); o[k] = e
            nrm[:, k] = scene_sdf(prims, p + o)[0] - scene_sdf(prims, p - o)[0]
        nrm /= np.linalg.norm(nrm, axis=1, keepdims=True) + 1e-9
        col = np.array([prims[m].color for m in np.maximum(mid, 0)])
        L = np.array([-0.5, 0.75, -0.45]); L /= np.linalg.norm(L)
        ndl = nrm @ L
        band = np.where(ndl > 0.45, 1.0, np.where(ndl > -0.05, 0.78, 0.55))
        V = -np.array([0, 0, 1.0]) @ R.T
        Hh = (L + V); Hh /= np.linalg.norm(Hh)
        spec = (np.clip(nrm @ Hh, 0, 1) ** 40 > 0.5) * 0.35
        rim = (1 - np.clip(np.abs(nrm @ V), 0, 1)) ** 3 * 0.25
        shade = col * band[:, None] + spec[:, None] + rim[:, None] * col
        # cool tint in shadow like RoR2 icons
        shade = shade * (1 - 0.12 * (band < 0.6)[:, None]) + np.array([0.05, 0.05, 0.12]) * (band < 0.6)[:, None]
        img[hi, :3] = np.clip(shade, 0, 1); img[hi, 3] = 1
        # interior ink lines where material changes or depth jumps
    img = img.reshape(N, N, 4)
    mids = np.full(N * N, -1);
    if hi.size: mids[hi] = mid
    mids = mids.reshape(N, N); tt = t.reshape(N, N)
    edge = np.zeros((N, N), bool)
    for dy, dx in ((0, 1), (1, 0)):
        a = mids; b = np.roll(np.roll(mids, dy, 0), dx, 1)
        ta = tt; tb = np.roll(np.roll(tt, dy, 0), dx, 1)
        both = (a >= 0) & (b >= 0)
        edge |= both & ((a != b) | (np.abs(ta - tb) > 0.08))
    ink = Image.fromarray((edge * 255).astype(np.uint8)).filter(ImageFilter.MaxFilter(3))
    ink = np.array(ink) / 255.
    img[..., :3] = img[..., :3] * (1 - ink[..., None] * 0.85) + np.array([0.08, 0.07, 0.12]) * ink[..., None] * 0.85
    rgba = Image.fromarray((img * 255).astype(np.uint8), 'RGBA').resize((size, size), Image.LANCZOS)
    # silhouette ink + tier outline
    a = rgba.getchannel('A')
    ink_a = a.filter(ImageFilter.MaxFilter(5))
    tier_a = a.filter(ImageFilter.MaxFilter(13)).filter(ImageFilter.GaussianBlur(0.8))
    tc = TIER[tier]
    base = Image.new('RGBA', (size, size), (0, 0, 0, 0))
    glow = Image.new('RGBA', (size, size), tc + (255,)); glow.putalpha(tier_a)
    inkl = Image.new('RGBA', (size, size), (20, 18, 30, 255)); inkl.putalpha(ink_a)
    base.alpha_composite(glow); base.alpha_composite(inkl); base.alpha_composite(rgba)
    base.save(out)
    return base
