"""Builds the GitHub Pages site in docs/ from the repo itself (item names, pickup text, icons, unlocks, voids),
so the page never drifts from the mod. Run from the repo root: python3 tools/make_site.py"""
import glob, html, json, os, re, shutil
from PIL import Image

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
DOCS = os.path.join(ROOT, 'docs'); IMG = os.path.join(DOCS, 'img')
REPO = 'https://github.com/ASDFboy/ror2-plus'
VERSION = json.load(open(os.path.join(ROOT, 'dist/manifest.json')))['version_number']
E = html.escape

def read(p): return open(os.path.join(ROOT, p), encoding='utf-8').read()

# ---------------------------------------------------------------- data from source
tiers = json.load(open(os.path.join(ROOT, 'tools/item_tiers.json')))
src = '\n'.join(open(f, encoding='utf-8').read() for f in glob.glob(os.path.join(ROOT, 'src/Items/Complete/*.cs')))
items = []
for m in re.finditer(r'class (\w+)\s*:\s*\w+<\1>\s*\{(.*?)(?=\n    public (?:abstract )?class |\Z)', src, re.S):
    cls, body = m.group(1), m.group(2)
    if cls not in tiers: continue
    name = re.search(r'(?:ItemName|EquipmentName) => "([^"]+)"', body)
    pick = re.search(r'PickupText => \$?"([^"]+)"', body)
    if not name: continue
    t = tiers[cls]
    if re.search(r'IsBoss\s*=>\s*true', body): t = 'boss'
    items.append({'cls': cls, 'name': name.group(1), 'tier': t, 'pickup': re.sub(r'\{[^}]*\}', '', pick.group(1)) if pick else ''})
order = {'white': 0, 'green': 1, 'red': 2, 'boss': 3, 'equip': 4}
items.sort(key=lambda i: (order[i['tier']], i['name']))

unlocks = re.findall(r'\("(\w+)", "(\w+)", (?:true|false), "([^"]*)", "([^"]*)"\)', read('src/Items/Complete/Unlocks.Generated.cs'))
readme = read('dist/README.md')
unlock_item = dict(re.findall(r'^- \*\*(.+?)\*\* — (.+?):', readme, re.M))        # item -> challenge name
challenge_to_item = {v: k for k, v in unlock_item.items()}

voids = []
for tier, label in (('common', 1), ('uncommon', 2), ('legendary', 3)):
    line = re.search(r'\*\*Void %s:\*\* (.+)' % tier, readme)
    for nm, orig in re.findall(r'([^,(]+?) \(([^)]+)\)', line.group(1) if line else ''):
        voids.append((nm.strip(), orig.strip(), label))
void_ids = [r[0] for r in re.findall(r'\("(\w+)", "[^"]+", (\d), "([^"]+)"', read('tools/void_items.py'))]
void_rows = {name: vid for vid, _, name in re.findall(r'\("(\w+)", "[^"]+", (\d), "([^"]+)"', read('tools/void_items.py'))}

lunar_table = re.search(r'\| Lunar \|.*?\n\n', readme, re.S).group(0)
lunar_rows = [[c.strip() for c in l.strip('|').split('|')] for l in lunar_table.strip().splitlines()[2:]]
artifacts = re.findall(r'^- \*\*(Distortion|Spirit|Origin|Tempus)\*\* — (.+)$', readme, re.M)

# ---------------------------------------------------------------- images
if os.path.isdir(IMG): shutil.rmtree(IMG)
for d in ('icons', 'ach', 'voids'): os.makedirs(os.path.join(IMG, d))
def small(srcp, dst, size):
    im = Image.open(srcp).convert('RGBA'); im.thumbnail((size, size), Image.LANCZOS); im.save(dst, optimize=True)
for i in items: small(os.path.join(ROOT, 'art/icons', i['cls'] + '.png'), os.path.join(IMG, 'icons', i['cls'] + '.png'), 128)
for a in ('Distortion', 'Spirit', 'Origin', 'Tempus'): small(os.path.join(ROOT, 'art/icons', f'Art_{a}_On.png'), os.path.join(IMG, 'icons', f'Art_{a}.png'), 128)
for f in glob.glob(os.path.join(ROOT, 'art/achievements/*.png')): shutil.copy(f, os.path.join(IMG, 'ach'))
for vid in void_rows.values():
    p = os.path.join(ROOT, 'art/voidicons', vid + '.png')
    if os.path.exists(p): small(p, os.path.join(IMG, 'voids', vid + '.png'), 96)
small(os.path.join(ROOT, 'art/icons/ExpansionIcon.png'), os.path.join(IMG, 'emblem.png'), 256)
small(os.path.join(ROOT, 'art/icons/ExpansionIcon.png'), os.path.join(IMG, 'favicon.png'), 64)

# ---------------------------------------------------------------- html
TIER_LABEL = {'white': 'Common', 'green': 'Uncommon', 'red': 'Legendary', 'boss': 'Boss', 'equip': 'Equipment'}
def item_card(i):
    return (f'<figure class="card t-{i["tier"]}" data-tier="{i["tier"]}"><img src="img/icons/{i["cls"]}.png" alt="" loading="lazy" width="96" height="96">'
            f'<figcaption><b>{E(i["name"])}</b><span>{E(i["pickup"])}</span></figcaption></figure>')
cards = '\n'.join(item_card(i) for i in items)
chips = ''.join(f'<button class="chip t-{t}" data-f="{t}">{TIER_LABEL[t]} <small>{sum(1 for i in items if i["tier"] == t)}</small></button>' for t in order)

ach_html = []
for aid, tok, chname, desc in unlocks:
    ach_html.append(f'<li><img src="img/ach/{aid}.png" alt="" loading="lazy" width="64" height="64"><div><b>{E(chname)}</b>'
                    f'<span>{E(desc)}</span><em>Unlocks {E(challenge_to_item.get(chname, ""))}</em></div></li>')
void_html = ''.join(f'<img src="img/voids/{void_rows[n]}.png" alt="{E(n)}" title="{E(n)} — corrupts {E(o)}" loading="lazy" width="64" height="64">'
                    for n, o, _ in voids if n in void_rows)
lunar_html = ''.join(f'<tr><th>{E(r[0])}</th><td>{E(r[1])}</td><td>{E(r[2])}</td></tr>' for r in lunar_rows)
art_html = ''.join(f'<div class="art"><img src="img/icons/Art_{n}.png" alt="" width="72" height="72"><div><b>Artifact of {n}</b><span>{E(d[:1].upper() + d[1:])}</span></div></div>' for n, d in artifacts)
n_items = len(items); n_void = len(voids); n_unlock = len(unlocks)

page = f'''<!doctype html>
<html lang="en">
<head>
<meta charset="utf-8">
<meta name="viewport" content="width=device-width, initial-scale=1">
<title>ror2-plus</title>
<meta name="description" content="A Risk of Rain 2 expansion mod: every Risk of Rain and Returns item RoR2 never got, a void version of every item, unlocks and a lunar rework.">
<link rel="icon" href="img/favicon.png">
<link rel="preconnect" href="https://fonts.googleapis.com"><link rel="preconnect" href="https://fonts.gstatic.com" crossorigin>
<link href="https://fonts.googleapis.com/css2?family=Barlow+Condensed:wght@600;700&family=Barlow:wght@400;500;600&display=swap" rel="stylesheet">
<style>
:root {{
  --bg: #15171c; --panel: #1e2128; --panel2: #262a33; --line: #343945; --text: #e7e9ee; --dim: #9aa1ae;
  --accent: #f5802c; --white: #b2c4be; --green: #68b639; --red: #ef553e; --boss: #adc723; --equip: #d68231;
  --void: #b070d6; --lunar: #34c2e3;
}}
* {{ box-sizing: border-box; }}
html {{ scroll-behavior: smooth; }}
body {{ margin: 0; background: var(--bg); color: var(--text); font: 16px/1.55 Barlow, system-ui, sans-serif; }}
a {{ color: var(--accent); }}
h1, h2, h3 {{ font-family: "Barlow Condensed", sans-serif; letter-spacing: .02em; line-height: 1.1; margin: 0 0 .4em; }}
h2 {{ font-size: clamp(1.8rem, 4vw, 2.6rem); text-transform: uppercase; }}
h2 small {{ color: var(--dim); font-size: .55em; margin-left: .4em; letter-spacing: .05em; }}
.wrap {{ max-width: 1120px; margin: 0 auto; padding: 0 16px; }}
section {{ scroll-margin-top: 52px; padding: 64px 0; border-top: 1px solid var(--line); }}
.lead {{ color: var(--dim); max-width: 70ch; margin: 0 0 28px; }}
nav {{ position: sticky; top: 0; z-index: 5; background: rgba(21,23,28,.92); backdrop-filter: blur(6px); border-bottom: 1px solid var(--line); }}
nav .wrap {{ display: flex; gap: 20px; align-items: center; height: 52px; overflow-x: auto; white-space: nowrap; }}
nav a {{ color: var(--dim); text-decoration: none; font-weight: 600; font-size: .95rem; }}
nav a:hover {{ color: var(--text); }}
nav .brand {{ color: var(--text); font-family: "Barlow Condensed"; font-size: 1.3rem; display: flex; align-items: center; gap: 8px; margin-right: auto; }}
nav .brand img {{ width: 26px; height: 26px; }}
header {{ padding: 72px 0 64px; background: radial-gradient(ellipse at 50% 0%, rgba(245,128,44,.16), transparent 60%); }}
.hero {{ display: grid; grid-template-columns: 200px 1fr; gap: 40px; align-items: center; }}
.hero img {{ width: 200px; height: 200px; }}
.hero h1 {{ font-size: clamp(3rem, 9vw, 5.5rem); margin: 0; }}
.hero p {{ font-size: 1.15rem; color: var(--dim); max-width: 60ch; margin: 8px 0 24px; }}
.btns {{ display: flex; gap: 12px; flex-wrap: wrap; }}
.btn {{ display: inline-block; padding: 12px 22px; border-radius: 4px; font-weight: 600; text-decoration: none; border: 1px solid var(--line); color: var(--text); background: var(--panel); }}
.btn.primary {{ background: var(--accent); border-color: var(--accent); color: #1b0f05; }}
.btn:hover {{ filter: brightness(1.1); }}
.stats {{ display: grid; grid-template-columns: repeat(4, 1fr); gap: 12px; margin-top: 40px; }}
.stat {{ background: var(--panel); border: 1px solid var(--line); border-radius: 6px; padding: 16px; }}
.stat b {{ display: block; font-family: "Barlow Condensed"; font-size: 2.4rem; line-height: 1; }}
.stat span {{ color: var(--dim); font-size: .9rem; }}
.chips {{ display: flex; gap: 8px; flex-wrap: wrap; margin-bottom: 20px; }}
.chip {{ font: 600 .9rem Barlow; color: var(--text); background: var(--panel); border: 1px solid var(--line); border-radius: 999px; padding: 6px 14px; cursor: pointer; }}
.chip small {{ color: var(--dim); }}
.chip[aria-pressed="true"] {{ border-color: currentColor; }}
.chip.t-white {{ color: var(--white); }} .chip.t-green {{ color: var(--green); }} .chip.t-red {{ color: var(--red); }} .chip.t-boss {{ color: var(--boss); }} .chip.t-equip {{ color: var(--equip); }}
.grid {{ display: grid; grid-template-columns: repeat(auto-fill, minmax(230px, 1fr)); gap: 12px; }}
.card {{ margin: 0; display: flex; gap: 12px; align-items: center; background: var(--panel); border: 1px solid var(--line); border-left: 3px solid var(--c); border-radius: 6px; padding: 10px; }}
.card img {{ width: 72px; height: 72px; flex: none; }}
.card b {{ display: block; color: var(--c); font-weight: 600; }}
.card span {{ display: block; color: var(--dim); font-size: .88rem; line-height: 1.35; }}
.t-white {{ --c: var(--white); }} .t-green {{ --c: var(--green); }} .t-red {{ --c: var(--red); }} .t-boss {{ --c: var(--boss); }} .t-equip {{ --c: var(--equip); }}
.card.hide {{ display: none; }}
.voids {{ display: grid; grid-template-columns: repeat(auto-fill, minmax(64px, 1fr)); gap: 6px; background: radial-gradient(ellipse at center, rgba(176,112,214,.12), transparent 70%); padding: 12px; border-radius: 8px; }}
.voids img {{ width: 100%; height: auto; transition: transform .15s; }}
.voids img:hover {{ transform: scale(1.25); }}
.two {{ display: grid; grid-template-columns: 1fr 1fr; gap: 32px; }}
table {{ width: 100%; border-collapse: collapse; font-size: .95rem; }}
th, td {{ text-align: left; padding: 10px 12px; border-bottom: 1px solid var(--line); vertical-align: top; }}
thead th {{ color: var(--dim); font-weight: 600; font-size: .85rem; text-transform: uppercase; letter-spacing: .05em; }}
tbody th {{ color: var(--lunar); font-weight: 600; white-space: nowrap; }}
tbody td:nth-child(2) {{ color: var(--dim); }}
.table-scroll {{ overflow-x: auto; background: var(--panel); border: 1px solid var(--line); border-radius: 6px; }}
.ach {{ list-style: none; padding: 0; margin: 0; display: grid; grid-template-columns: repeat(auto-fill, minmax(320px, 1fr)); gap: 10px; }}
.ach li {{ display: flex; gap: 12px; background: var(--panel); border: 1px solid var(--line); border-radius: 6px; padding: 10px; }}
.ach img {{ width: 64px; height: 64px; flex: none; border-radius: 2px; }}
.ach b {{ display: block; }} .ach span {{ display: block; color: var(--dim); font-size: .9rem; line-height: 1.35; }}
.ach em {{ display: block; font-style: normal; font-size: .82rem; color: var(--accent); margin-top: 2px; }}
.art {{ display: flex; gap: 14px; align-items: flex-start; background: var(--panel); border: 1px solid var(--line); border-radius: 6px; padding: 14px; }}
.art img {{ width: 72px; height: 72px; flex: none; }} .art b {{ display: block; }} .art span {{ color: var(--dim); font-size: .95rem; }}
.arts {{ display: grid; grid-template-columns: 1fr 1fr; gap: 12px; }}
.lobby {{ display: flex; gap: 20px; align-items: center; background: var(--panel); border: 1px solid var(--line); border-radius: 6px; padding: 18px; margin-top: 20px; }}
.lobby img {{ width: 64px; height: 64px; }}
ol.steps {{ padding-left: 1.2em; }} ol.steps li {{ margin-bottom: 8px; }}
code {{ background: var(--panel2); padding: 2px 6px; border-radius: 3px; font-size: .9em; }}
footer {{ padding: 40px 0 60px; color: var(--dim); font-size: .9rem; border-top: 1px solid var(--line); }}
@media (max-width: 760px) {{
  .hero {{ grid-template-columns: 1fr; text-align: center; gap: 16px; }} .hero img {{ width: 140px; height: 140px; margin: 0 auto; }}
  .hero p {{ margin-left: auto; margin-right: auto; }} .btns {{ justify-content: center; }}
  .stats {{ grid-template-columns: repeat(2, 1fr); }} .two, .arts {{ grid-template-columns: 1fr; }}
  .ach {{ grid-template-columns: 1fr; }} section {{ padding: 44px 0; }}
}}
</style>
</head>
<body>
<nav><div class="wrap">
  <a class="brand" href="#top"><img src="img/emblem.png" alt="">ror2-plus</a>
  <a href="#items">Items</a><a href="#voids">Voids</a><a href="#unlocks">Unlocks</a><a href="#lunars">Lunars</a><a href="#install">Install</a><a href="{REPO}">GitHub</a>
</div></nav>

<header id="top"><div class="wrap">
  <div class="hero">
    <img src="img/emblem.png" alt="ror2-plus emblem">
    <div>
      <h1>ror2-plus</h1>
      <p>Every item and equipment from Risk of Rain and Risk of Rain Returns that Risk of Rain 2 never got, rebuilt to fit RoR2, plus a void version of every item that lacked one. It plugs into the game as its own expansion, next to the official DLCs.</p>
      <div class="btns">
        <a class="btn primary" href="{REPO}/releases/latest">Download v{VERSION}</a>
        <a class="btn" href="#install">How to install</a>
        <a class="btn" href="{REPO}">Source code</a>
      </div>
    </div>
  </div>
  <div class="stats">
    <div class="stat"><b>{n_items}</b><span>new items and equipment</span></div>
    <div class="stat"><b>{n_void}</b><span>new void items</span></div>
    <div class="stat"><b>{n_unlock}</b><span>unlockable challenges</span></div>
    <div class="stat"><b>4</b><span>classic artifacts</span></div>
  </div>
</div></header>

<section id="items"><div class="wrap">
  <h2>New items <small>{n_items}</small></h2>
  <p class="lead">Each one has a painted icon in the game's own style, a 3D pickup model, a spot on every survivor, its own logbook lore, and the game's own sounds and effects. They're balanced against vanilla items of the same rarity.</p>
  <div class="chips" role="group" aria-label="Filter by rarity"><button class="chip" data-f="all" aria-pressed="true">All <small>{n_items}</small></button>{chips}</div>
  <div class="grid" id="itemgrid">
{cards}
  </div>
</div></section>

<section id="voids"><div class="wrap">
  <h2>Void items <small>{n_void}</small></h2>
  <p class="lead">Every white, green and red item that didn't already have a void version now has one, from the base game, all the DLCs and ror2-plus itself. Like vanilla voids, picking one up corrupts its original. Hover an icon to see its name.</p>
  <div class="voids">{void_html}</div>
</div></section>

<section id="artifacts"><div class="wrap">
  <h2>Artifacts</h2>
  <p class="lead">Four more artifacts from the original game, adapted for RoR2.</p>
  <div class="arts">{art_html}</div>
</div></section>

<section id="unlocks"><div class="wrap">
  <h2>Unlocks <small>{n_unlock}</small></h2>
  <p class="lead">Like in Risk of Rain and Returns, many items have to be earned. Challenges that couldn't carry over to RoR2 were swapped for something in the same spirit. Each one has its own icon in the base game's unlock style. Don't want to grind? Turn off <code>Require Unlocks</code>.</p>
  <ul class="ach">{''.join(ach_html)}</ul>
</div></section>

<section id="lunars"><div class="wrap">
  <h2>Lunar rework</h2>
  <p class="lead">Lunars are harder to get, but their downsides are gentler. Each one keeps its trade-off. Every change has its own switch and number in the config.</p>
  <div class="table-scroll"><table>
    <thead><tr><th>Lunar</th><th>Vanilla downside</th><th>ror2-plus</th></tr></thead>
    <tbody>{lunar_html}</tbody>
  </table></div>
  <p class="lead" style="margin-top:20px">Harder to get, all adjustable and switchable: Lunar Pods cost 2 coins, Bazaar prices go up by half, coins drop less often and pods appear half as often. Optional extras: Bazaar-only lunars, and a trade-in cost of one white or green item.</p>
</div></section>

<section id="install"><div class="wrap two">
  <div>
    <h2>Install</h2>
    <ol class="steps">
      <li>Install <a href="https://thunderstore.io/c/riskofrain2/p/ebkr/r2modman/">r2modman</a> (free) and pick Risk of Rain 2.</li>
      <li>Download <b>ror2-plus-{VERSION}.zip</b> from the <a href="{REPO}/releases/latest">latest release</a>.</li>
      <li>In r2modman: <b>Settings → Import local mod</b>, choose the zip. Its dependencies (BepInEx, R2API) are listed in the mod and installed from Thunderstore.</li>
      <li>Launch modded. Don't run ClassicItemsReturns alongside it; ror2-plus already includes everything it has.</li>
    </ol>
    <p class="lead">Settings live in <code>BepInEx/config/com.ASDFboy.ror2plus.cfg</code>, or in the in-game Mod Options menu if you have Risk of Options.</p>
  </div>
  <div>
    <h2>In the lobby</h2>
    <p class="lead">ror2-plus is a real expansion, so it sits in the lobby's Expansions row next to Survivors of the Void and the other DLCs.</p>
    <div class="lobby"><img src="img/emblem.png" alt=""><div><b>ror2-plus</b><br><span style="color:var(--dim)">Switch it off for a run and its items, equipment, artifacts and lunar changes all sit that run out. No restart needed.</span></div></div>
  </div>
</div></section>

<footer><div class="wrap">
  <p>Made by ASDFboy and Claude. Built on <a href="https://github.com/DestroyedClone/RiskyClassicItems">ClassicItemsReturns</a> by DestroyedClone, Moffein and contributors, used with their permission. Item concepts from Risk of Rain and Risk of Rain Returns by Hopoo Games. Not affiliated with Hopoo Games or Gearbox.</p>
</div></footer>

<script>
(function () {{
  var chips = document.querySelectorAll('.chip'), cards = document.querySelectorAll('#itemgrid .card');
  chips.forEach(function (c) {{
    c.addEventListener('click', function () {{
      var f = c.getAttribute('data-f');
      chips.forEach(function (o) {{ o.setAttribute('aria-pressed', o === c ? 'true' : 'false'); }});
      cards.forEach(function (k) {{ k.classList.toggle('hide', f !== 'all' && k.getAttribute('data-tier') !== f); }});
    }});
  }});
}})();
</script>
</body>
</html>
'''
open(os.path.join(DOCS, 'index.html'), 'w', encoding='utf-8').write(page)
open(os.path.join(DOCS, '.nojekyll'), 'w').close()
print(f'docs/index.html: {n_items} items, {n_void} voids, {n_unlock} unlocks, {len(lunar_rows)} lunars, {len(artifacts)} artifacts')
