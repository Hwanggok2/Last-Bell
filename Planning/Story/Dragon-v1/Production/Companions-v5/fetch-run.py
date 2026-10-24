"""Download observed PixelLab run frames and assemble source review; no pixel invention."""
from pathlib import Path
from concurrent.futures import ThreadPoolExecutor
import sys,json,urllib.request,io
from PIL import Image,ImageDraw
B=Path(__file__).resolve().parent
final='--final' in sys.argv
req=json.loads((B/('requests/run-quantize.json' if final else 'requests/run.json')).read_text(encoding='utf-8'))
def fetch(row):
 id,job,n=row;folder='frames/Run' if final else 'raw/Run';p=B/id/folder/f'{n:02d}.png';p.parent.mkdir(parents=True,exist_ok=True)
 if not p.exists() or ('--replace-tori' in sys.argv and id=='tori'):
  with urllib.request.urlopen(f'https://api.pixellab.ai/mcp/images/{job}/download?index={n}',timeout=60) as r:data=r.read()
  im=Image.open(io.BytesIO(data));assert im.size==((48,32) if id in ['barun','doto'] else (32,32));p.write_bytes(data)
 return p
with ThreadPoolExecutor(max_workers=8) as pool:paths=list(pool.map(fetch,[(r['id'],r['jobId'],n) for r in req for n in range(8 if final else 9)]))
sheet=Image.new('RGB',((8 if final else 9)*196,5*154),'#DBCFAE');d=ImageDraw.Draw(sheet)
for row,r in enumerate(sorted(req,key=lambda r:['tori','barun','doto','rina','momo'].index(r['id']))):
 for n in range(8 if final else 9):
  p=B/r['id']/('frames/Run' if final else 'raw/Run')/f'{n:02d}.png';im=Image.open(p).convert('RGBA');big=im.resize((im.width*4,128),Image.Resampling.NEAREST);sheet.paste(big,(n*196,row*154+20),big);d.text((n*196,row*154),f"{r['id']} {n} {im.getbbox()}",fill='#202D38');d.line((n*196,row*154+136,n*196+192,row*154+136),fill='#609287')
sheet.save(B/('run-review.png' if final else 'run-raw-review.png'))
print('Saved',len(paths),'run frames. Final=',final)
