"""Renders an original, procedurally varied void icon for every row in void_items.py.
Eight void motifs (urchin, coral, shell, eye pod, crystals, tentacle bulb, orbit ring, hollow pod)
are varied per item from a seed, so every icon is distinct but they read as one family.
Usage: python3 -I tools/make_void_icons.py [ID ...]"""
import sys, os, math, random, zlib
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import sdf
from sdf import P, render
from void_items import ROWS
from concurrent.futures import ProcessPoolExecutor

sdf.TIER['void'] = (205, 70, 235)
DARK, MID, MAG, PINK, PALE, DEEP = '#3a1450', '#7a2ab0', '#c040e0', '#ff8ad8', '#ffd6ff', '#2c2a78'
ACCENT = {1: PALE, 2: MAG, 3: PINK}

def urchin(r, acc):
    base = r.uniform(0.32, 0.42)
    prims = [P('sphere', (base,), (0, 0, 0), color=r.choice([MID, DARK, DEEP]))]
    n = r.randint(7, 11)
    for i in range(n):
        th = math.acos(1 - 2 * (i + 0.5) / n); ph = math.pi * (1 + 5 ** 0.5) * i
        d = (math.sin(th) * math.cos(ph), math.cos(th), math.sin(th) * math.sin(ph))
        L = r.uniform(0.32, 0.5)
        prims.append(P('cap', ((d[0] * base * .8, d[1] * base * .8, d[2] * base * .8), (d[0] * (base + L), d[1] * (base + L), d[2] * (base + L)), r.uniform(.035, .06)), color=acc))
    prims.append(P('sphere', (base * .35,), (0, 0, -base * .85), color=PALE))
    return prims

def coral(r, acc):
    prims = [P('cap', ((0, -0.75, 0), (0, -0.1, 0), 0.11), color=MID)]
    def branch(x, y, ang, depth, rad):
        if depth == 0: prims.append(P('sphere', (rad * 1.6,), (x, y, 0), color=acc)); return
        L = r.uniform(0.22, 0.32)
        nx, ny = x + math.sin(ang) * L, y + math.cos(ang) * L
        prims.append(P('cap', ((x, y, 0), (nx, ny, r.uniform(-.1, .1)), rad), color=MID if depth > 1 else MAG, smooth=0.04))
        for da in (-r.uniform(.3, .6), r.uniform(.3, .6)): branch(nx, ny, ang + da, depth - 1, rad * 0.78)
    branch(0, -0.1, 0, 3, 0.09)
    return prims

def shell(r, acc):
    prims = []; y = -0.55; R = r.uniform(0.42, 0.5)
    for i in range(5):
        prims.append(P('torus', (R, R * 0.32), (0, y, 0), r=(r.uniform(-8, 8), 0, r.uniform(-8, 8)), color=[DARK, MID, MAG, MID, MAG][i]))
        y += R * 0.5; R *= 0.74
    prims.append(P('cone', (R * 1.2, 0.18), (0, y + 0.05, 0), color=acc))
    prims.append(P('sphere', (0.12,), (0, -0.5, 0), color=PALE))
    return prims

def eyepod(r, acc):
    R = r.uniform(0.48, 0.56)
    return [P('sphere', (R,), (0, 0, 0), color=r.choice([DARK, DEEP])),
            P('sphere', (R * 0.62,), (0, 0, -R * 0.8), sub=True),
            P('sphere', (R * 0.5,), (0, 0, -R * 0.2), color=PALE),
            P('sphere', (R * 0.25,), (r.uniform(-.05, .05), r.uniform(-.05, .05), -R * 0.62), color=acc),
            P('sphere', (R * 0.1,), (0, 0, -R * 0.8), color='#120518')]

def crystals(r, acc):
    prims = [P('ell', (0.55, 0.12, 0.4), (0, -0.62, 0), color=DARK)]
    for i in range(r.randint(4, 6)):
        x = r.uniform(-0.38, 0.38); h = r.uniform(0.28, 0.55); rad = r.uniform(0.1, 0.17)
        prims.append(P('cone', (rad, h), (x, -0.55 + h, r.uniform(-.15, .15)), r=(r.uniform(-12, 12), 0, -x * 40), color=r.choice([MAG, MID, acc, PINK])))
    return prims

def tentabulb(r, acc):
    prims = [P('ell', (0.42, 0.36, 0.4), (0, 0.25, 0), color=r.choice([MID, MAG])),
             P('sphere', (0.14,), (0.1, 0.32, -0.32), color=PALE)]
    for k in range(r.randint(3, 5)):
        x0 = -0.3 + 0.6 * k / 3 if k < 4 else 0; pts = [(x0 * 0.7, 0.0, 0)]
        for j in range(4): px, py, pz = pts[-1]; pts.append((px + r.uniform(-.12, .12), py - 0.18, pz + r.uniform(-.06, .06)))
        for j in range(4): prims.append(P('cap', (pts[j], pts[j + 1], 0.07 - 0.012 * j), color=DARK if j < 2 else acc, smooth=0.03))
    return prims

def orbitring(r, acc):
    prims = [P('torus', (0.5, 0.07), (0, 0, 0), r=(r.uniform(55, 75), 0, r.uniform(-20, 20)), color=MID),
             P('sphere', (0.24,), (0, 0, 0), color=r.choice([DARK, DEEP])),
             P('sphere', (0.11,), (0, 0, -0.18), color=acc)]
    for i in range(r.randint(3, 5)):
        a = i * 2 * math.pi / 5 + r.uniform(0, .5)
        prims.append(P('sphere', (r.uniform(.07, .12),), (math.cos(a) * 0.5, math.sin(a) * 0.18, math.sin(a) * 0.45), color=r.choice([PINK, PALE, MAG])))
    return prims

def hollowpod(r, acc):
    prims = [P('ell', (0.42, 0.6, 0.4), (0, 0, 0), color=r.choice([MID, DARK]))]
    for i in range(r.randint(2, 4)):
        prims.append(P('sphere', (r.uniform(0.12, 0.2),), (r.uniform(-0.2, 0.2), r.uniform(-0.35, 0.35), -0.36), sub=True))
    prims.append(P('ell', (0.28, 0.45, 0.28), (0, 0, 0), color=acc))
    prims.append(P('cone', (0.12, 0.15), (0, 0.66, 0), color=PALE))
    return prims

MOTIFS = [urchin, coral, shell, eyepod, crystals, tentabulb, orbitring, hollowpod]

def job(row):
    id_, orig, tier, name, spec = row
    seed = zlib.crc32(id_.encode())
    r = random.Random(seed)
    motif = MOTIFS[seed % len(MOTIFS)]
    prims = motif(r, ACCENT[tier])
    out = os.path.join(OUT, id_ + '.png')
    render(prims, 'void', out, cam_rot=(r.uniform(12, 28), r.uniform(-35, 35), r.uniform(-10, 10)))
    return id_

OUT = os.path.join(os.path.dirname(os.path.abspath(__file__)), '..', 'art', 'voidicons')
if __name__ == '__main__':
    os.makedirs(OUT, exist_ok=True)
    want = set(sys.argv[1:])
    rows = [r for r in ROWS if not want or r[0] in want]
    with ProcessPoolExecutor(2) as ex:
        list(ex.map(job, rows))
    print(len(rows), 'void icons')
