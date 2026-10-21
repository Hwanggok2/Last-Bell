"""Download only explicitly listed PixelLab generated assets; never rewrites Unity assets."""
import argparse, concurrent.futures, json, pathlib, urllib.request
ROOT = pathlib.Path(__file__).resolve().parent
parser=argparse.ArgumentParser()
parser.add_argument('clips',nargs='*')
args=parser.parse_args()
jobs=json.loads((ROOT/'jobs.json').read_text(encoding='utf-8'))['jobs']
if (ROOT/'motion-v10-jobs.json').exists():jobs+=json.loads((ROOT/'motion-v10-jobs.json').read_text(encoding='utf-8'))['jobs']
if (ROOT/'motion-v9-jobs.json').exists():jobs+=json.loads((ROOT/'motion-v9-jobs.json').read_text(encoding='utf-8'))['jobs']
if (ROOT/'motion-v7-jobs.json').exists():jobs+=json.loads((ROOT/'motion-v7-jobs.json').read_text(encoding='utf-8'))['jobs']
if (ROOT/'motion-v6-jobs.json').exists():jobs+=json.loads((ROOT/'motion-v6-jobs.json').read_text(encoding='utf-8'))['jobs']
if (ROOT/'player-v5-jobs.json').exists():jobs+=json.loads((ROOT/'player-v5-jobs.json').read_text(encoding='utf-8'))['jobs']
if (ROOT/'player-v5-run-job.json').exists():jobs.append(json.loads((ROOT/'player-v5-run-job.json').read_text(encoding='utf-8')))
if (ROOT/'player-v5-run-alt-job.json').exists():jobs.append(json.loads((ROOT/'player-v5-run-alt-job.json').read_text(encoding='utf-8')))
tasks=[]
for job in jobs:
    if job['id'] not in args.clips: continue
    if 'workbench' in job.get('method',''):
        print('Workbench clip uses preserved sheet and recipe:',job['id']);continue
    folder=ROOT/'clips'/job['id']/'frames'
    folder.mkdir(parents=True,exist_ok=True)
    for i in range(job['requestedGeneratedFrames']+int(job.get('inputFrameIncluded',True))):
        tasks.append((f"https://api.pixellab.ai/mcp/images/{job['jobId']}/download?index={i}",folder/f'{i:02}.png'))
if 'diagonal-slash' in args.clips:
    folder=ROOT/'clips'/'diagonal-slash';folder.mkdir(parents=True,exist_ok=True)
    for ext,name in [('png','sheet'),('gif','animation'),('json','recipe')]:
        tasks.append((f'https://api.pixellab.ai/mcp/pixel-tools/c2caf0c5-ed1f-4ee6-943e-6b794107d2cd/last-bell-long-diagonal-slash.{ext}',folder/f'{name}.{ext}'))
def download(task):
    url,path=task
    if path.exists() and path.stat().st_size and 'pixel-tools' not in url: return f'exists {path.relative_to(ROOT)}'
    try:
        data=urllib.request.urlopen(url,timeout=45).read()
        if path.suffix=='.png' and not data.startswith(b'\x89PNG'): raise ValueError('Not PNG')
        path.write_bytes(data)
        return f'saved {path.relative_to(ROOT)}'
    except Exception as error:return f'ERROR {path.relative_to(ROOT)}: {error}'
with concurrent.futures.ThreadPoolExecutor(max_workers=6) as pool:
    for result in pool.map(download,tasks):print(result)
