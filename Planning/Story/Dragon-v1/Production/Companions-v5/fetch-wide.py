from pathlib import Path
from concurrent.futures import ThreadPoolExecutor
import json,urllib.request,io
from PIL import Image,ImageDraw
B=Path(__file__).resolve().parent
def fetch(row):
 id,job,n=row;p=B/id/'frames/AttackWide'/f'{n:02d}.png';p.parent.mkdir(parents=True,exist_ok=True)
 if not p.exists():
  with urllib.request.urlopen(f'https://api.pixellab.ai/mcp/images/{job}/download?index={n}',timeout=60) as r:data=r.read()
  im=Image.open(io.BytesIO(data));assert im.size==(48,32);p.write_bytes(data)
 return p
req=json.loads((B/'requests/attack-wide-quantize.json').read_text(encoding='utf-8'))
with ThreadPoolExecutor(max_workers=8) as pool:paths=list(pool.map(fetch,[(r['id'],r['jobId'],n) for r in req for n in range(8)]))
sheet=Image.new('RGB',(8*196,2*152),'#DCCFAF');d=ImageDraw.Draw(sheet)
for row,r in enumerate(req):
 for n in range(8):
  im=Image.open(B/r['id']/'frames/AttackWide'/f'{n:02d}.png').convert('RGBA');big=im.resize((192,128),Image.Resampling.NEAREST);sheet.paste(big,(n*196,row*152+20),big);d.text((n*196,row*152),f"{r['id']} {n} {im.getbbox()}",fill='#202D38');print(r['id'],n,im.getbbox())
sheet.save(B/'wide-review.png')
