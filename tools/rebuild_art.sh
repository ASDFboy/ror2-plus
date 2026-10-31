#!/bin/bash
# Re-renders every icon (raw renders -> icon_post) and re-exports every 3D model. Two parallel shards each.
set -u
cd "$(dirname "$0")/.."
PY=/root/tools/bpyenv/bin/python
RAW=build/raw; mkdir -p $RAW/items $RAW/voids art/icons art/voidicons art/models_new
ITEMS=$(python3 -c "import json;print(' '.join(json.load(open('tools/item_tiers.json'))))")
VOIDS=$(python3 -I -c "import importlib.util as u;s=u.spec_from_file_location('v','tools/void_items.py');m=u.module_from_spec(s);s.loader.exec_module(m);print(' '.join(r[0] for r in m.ROWS))")
half() { python3 -c "import sys;a=sys.argv[2:];k=int(sys.argv[1]);print(' '.join(a[k::2]))" "$@"; }
log=build/rebuild.log; : > $log
( $PY tools/blender_icons.py $RAW/items $(half 0 $ITEMS) >>$log 2>&1; $PY tools/void_bases.py $RAW/voids $(half 0 $VOIDS) >>$log 2>&1; $PY tools/export_models.py art/models_new --shard 0/2 >>$log 2>&1; echo SHARD0_DONE >>$log ) &
( $PY tools/blender_icons.py $RAW/items $(half 1 $ITEMS) >>$log 2>&1; $PY tools/void_bases.py $RAW/voids $(half 1 $VOIDS) >>$log 2>&1; $PY tools/export_models.py art/models_new --shard 1/2 >>$log 2>&1; echo SHARD1_DONE >>$log ) &
wait
python3 - <<'PY'
import json, sys, glob, os, importlib.util as u
sys.path.insert(0, 'tools'); from icon_post import post
tiers = json.load(open('tools/item_tiers.json'))
for n, t in tiers.items():
    p = f'build/raw/items/{n}.png'
    if os.path.exists(p): post(p, t, name=n).save(f'art/icons/{n}.png')
    else: print('missing raw', n)
s = u.spec_from_file_location('v', 'tools/void_items.py'); m = u.module_from_spec(s); s.loader.exec_module(m)
for r in m.ROWS:
    p = f'build/raw/voids/{r[0]}.png'
    if os.path.exists(p): post(p, 'void', name=r[0]).save(f'art/voidicons/{r[0]}.png')
    else: print('missing raw void', r[0])
print('ICONS_DONE')
PY
echo ALL_DONE >> $log
