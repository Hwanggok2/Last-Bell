"""Download generated PixelLab frames, retaining job IDs and source PNGs."""
import concurrent.futures, json, pathlib, urllib.request, time
ROOT=pathlib.Path(__file__).resolve().parents[1]
jobs=json.loads((ROOT/'Planning/Story/Monster-v83/jobs.json').read_text(encoding='utf-8'))['jobs']
def fetch(task):
    url,path=task
    if path.exists(): return
    path.parent.mkdir(parents=True,exist_ok=True)
    for attempt in range(4):
        try:
            data=urllib.request.urlopen(url,timeout=45).read()
            if not data.startswith(b'\x89PNG\r\n\x1a\n'): raise ValueError('Not PNG: '+url)
            path.write_bytes(data);return
        except Exception:
            if attempt==3: raise
            time.sleep(2)
tasks=[]
for item in jobs:
    for motion,count in [('attack',17),('stun',9)]:
        for i in range(count):
            tasks.append((f'https://api.pixellab.ai/mcp/images/{item[motion]}/download?index={i}',ROOT/f'Planning/Story/Monster-v83/raw/{item["id"]}/{motion}/{i:02}.png'))
with concurrent.futures.ThreadPoolExecutor(max_workers=6) as executor:
    list(executor.map(fetch,tasks))
print(f'Validated {len(tasks)} generated frame PNGs')
