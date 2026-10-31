"""Extracts reference data from staged vanilla RoR2 item bundles (for study only; nothing here ships).
Writes reference/vanilla/<item>/: icon png, textures, meshes (.obj), materials.json; and reference/vanilla/summary.json"""
import UnityPy, glob, os, re, json, sys, collections
SRC = "/mnt/user-data/uploads/Risk of Rain 2/Risk of Rain 2_Data/StreamingAssets/aa/StandaloneWindows64/"
OUT = "/home/claude/ror2-plus/reference/vanilla"
groups = collections.defaultdict(list)
for f in glob.glob(SRC + '*.bundle'):
    key = re.sub(r'_(static_)?(shader_)?assets_all_.*', '', os.path.basename(f)).replace('ror2-', '')
    groups[key].append(f)
summary = {}
for key, files in sorted(groups.items()):
    if key in ('base-common', 'base-shaders', 'base-commando', 'base-huntress'): continue
    env = UnityPy.load(*files); d = os.path.join(OUT, key); os.makedirs(d, exist_ok=True)
    info = {'meshes': [], 'textures': [], 'materials': {}, 'prefabs': []}
    for o in env.objects:
        t = o.type.name
        try:
            if t == 'Mesh':
                m = o.read(); obj = m.export()
                tris = sum(1 for l in obj.splitlines() if l.startswith('f '))
                verts = sum(1 for l in obj.splitlines() if l.startswith('v '))
                info['meshes'].append({'name': m.m_Name, 'tris': tris, 'verts': verts})
                open(os.path.join(d, m.m_Name + '.obj'), 'w').write(obj)
            elif t == 'Texture2D':
                tx = o.read(); info['textures'].append({'name': tx.m_Name, 'size': [tx.m_Width, tx.m_Height]})
                tx.image.save(os.path.join(d, tx.m_Name + '.png'))
            elif t == 'Material':
                mt = o.read(); tt = o.read_typetree()
                sh = None
                try: sh = mt.m_Shader.read().m_ParsedForm.m_Name
                except Exception: pass
                props = tt.get('m_SavedProperties', {})
                info['materials'][mt.m_Name] = {'shader': sh, 'keywords': tt.get('m_ValidKeywords') or tt.get('m_ShaderKeywords'),
                    'floats': {k: v for k, v in props.get('m_Floats', [])}, 'colors': {k: v for k, v in props.get('m_Colors', [])},
                    'textures': {k: v.get('m_Texture', {}).get('m_PathID', 0) != 0 for k, v in props.get('m_TexEnvs', [])}}
            elif t == 'GameObject':
                g = o.read()
                if re.match(r'^(Pickup|Display|mdl)', g.m_Name): info['prefabs'].append(g.m_Name)
        except Exception as e:
            info.setdefault('errors', []).append(f'{t}: {e}')
    info['prefabs'] = sorted(set(info['prefabs']))
    summary[key] = info
json.dump(summary, open(os.path.join(OUT, 'summary.json'), 'w'), indent=1, default=str)
for k, v in summary.items():
    print(k, '| meshes', [(m['name'], m['tris']) for m in v['meshes']], '| tex', [(t['name'], t['size'][0]) for t in v['textures']][:6], '| mats', list(v['materials'])[:4], '| err', len(v.get('errors', [])))
