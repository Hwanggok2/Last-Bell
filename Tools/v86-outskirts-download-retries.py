"""Retain generated retry sources in separate folders, preserving all originals."""
import json,pathlib,urllib.request,concurrent.futures,sys
ROOT=pathlib.Path(__file__).resolve().parents[1];ART=ROOT/'Planning/Story/Monster-v86/outskirts'
rows=json.loads((ART/'retry-jobs.json').read_text(encoding='utf-8'))
if len(sys.argv)>1:rows=[r for r in rows if r['id'] in sys.argv[1:]]
def fetch(task):
    row,i=task;p=ART/row['id']/row['directory']/f'{i:02}.png'
    if p.exists():return
    p.parent.mkdir(parents=True,exist_ok=True)
    raw=urllib.request.urlopen(f'https://api.pixellab.ai/mcp/images/{row["job"]}/download?index={i}',timeout=60).read()
    assert raw.startswith(b'\x89PNG\r\n\x1a\n');p.write_bytes(raw)
with concurrent.futures.ThreadPoolExecutor(max_workers=6) as pool:list(pool.map(fetch,[(r,i) for r in rows for i in range(r['outputFrames'])]))
print('Retry PNGs retained without changing originals.')
