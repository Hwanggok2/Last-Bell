"""Download selected PixelLab job frames to this task's evidence folder only."""
import concurrent.futures,json,pathlib,urllib.request,time,sys
ROOT=pathlib.Path(__file__).resolve().parents[1]
source=ROOT/'Planning/Story/Monster-v85/residential'
jobs=json.loads((source/'jobs.json').read_text(encoding='utf-8'))['jobs']
def fetch(task):
    url,path=task
    if path.exists():return
    path.parent.mkdir(parents=True,exist_ok=True)
    for attempt in range(3):
        try:
            data=urllib.request.urlopen(url,timeout=30).read()
            if not data.startswith(b'\x89PNG\r\n\x1a\n'):raise ValueError('Not PNG: '+url)
            path.write_bytes(data);return
        except Exception:
            if attempt==2:raise
            time.sleep(1)
tasks=[]
for item in jobs:
    for motion,clip in item.get('clips',{}).items():
        if len(sys.argv)>1 and item['id']+'/'+motion not in sys.argv[1:]:continue
        for i in range(clip['count']):
            tasks.append((f'https://api.pixellab.ai/mcp/images/{clip["job"]}/download?index={i}',source/item['id']/clip['folder']/f'{i:02}.png'))
with concurrent.futures.ThreadPoolExecutor(max_workers=6) as pool:list(pool.map(fetch,tasks))
print(f'Downloaded and PNG-validated {len(tasks)} source frames')
