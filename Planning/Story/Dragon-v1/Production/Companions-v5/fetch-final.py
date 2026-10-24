from pathlib import Path
from concurrent.futures import ThreadPoolExecutor
import json,urllib.request,io
from PIL import Image,ImageDraw
B=Path(__file__).resolve().parent
def fetch(row):
 id,job,n=row;motion='Idle' if n<4 else 'Attack';frame=n if n<4 else n-4
 p=B/id/'frames'/motion/f'{frame:02d}.png';p.parent.mkdir(parents=True,exist_ok=True)
 if not p.exists():
  req=urllib.request.Request(f'https://api.pixellab.ai/mcp/images/{job}/download?index={n}',headers={'User-Agent':'LastBell-asset-import'})
  with urllib.request.urlopen(req,timeout=60) as r:data=r.read()
  im=Image.open(io.BytesIO(data));assert im.size==(32,32);p.write_bytes(data)
 return p
def main():
 req=json.loads((B/'requests/quantize.json').read_text(encoding='utf-8'))
 with ThreadPoolExecutor(max_workers=8) as pool:paths=list(pool.map(fetch,[(r['id'],r['jobId'],n) for r in req for n in range(12)]))
 for r in req:
  for m in ['Idle','Attack']:
   metrics=[]
   for p in sorted((B/r['id']/'frames'/m).glob('*.png')):
    im=Image.open(p).convert('RGBA');px=list(im.getdata());metrics.append(dict(file=p.name,bounds=im.getbbox(),colors=len({v[:3] for v in px if v[3]}),alpha=sorted({v[3] for v in px}),bottomRow=[x for x in range(32) if im.getpixel((x,im.getbbox()[3]-1))[3]]))
   print(r['id'],m,json.dumps(metrics))
 print('Downloaded',len(paths),'quantized frames')
if __name__=='__main__':main()
