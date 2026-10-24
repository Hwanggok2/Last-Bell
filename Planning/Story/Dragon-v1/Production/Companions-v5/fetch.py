"""Download only observed PixelLab job PNG URLs into this owned art directory."""
from pathlib import Path
from concurrent.futures import ThreadPoolExecutor
import json,urllib.request,io
from PIL import Image,ImageDraw
B=Path(__file__).resolve().parent
def fetch(row):
 id,motion,job,n=row
 p=B/id/'raw'/motion/f'{n:02d}.png';p.parent.mkdir(parents=True,exist_ok=True)
 if not p.exists():
  req=urllib.request.Request(f'https://api.pixellab.ai/mcp/images/{job}/download?index={n}',headers={'User-Agent':'LastBell-asset-import'})
  with urllib.request.urlopen(req,timeout=60) as r:data=r.read()
  im=Image.open(io.BytesIO(data));assert im.size==(32,32)
  p.write_bytes(data)
 return p
def main():
 requests=json.loads((B/'requests/motion.json').read_text(encoding='utf-8'))
 rows=[(r['id'],r['motion'],r['jobId'],i) for r in requests for i in range(5 if r['motion']=='idle' else 9)]
 with ThreadPoolExecutor(max_workers=8) as pool:paths=list(pool.map(fetch,rows))
 sheet=Image.new('RGB',(9*132,10*152),'#DCCFAF');d=ImageDraw.Draw(sheet)
 for row,r in enumerate(requests):
  for n in range(5 if r['motion']=='idle' else 9):
   im=Image.open(B/r['id']/'raw'/r['motion']/f'{n:02d}.png').convert('RGBA');px=list(im.getdata());big=im.resize((128,128),Image.Resampling.NEAREST)
   x,y=n*132,row*152;sheet.paste(big,(x,y+20),big);d.text((x,y),f"{r['id']} {r['motion']} {n}",fill='#202D38')
 sheet.save(B/'raw-review.png');print(f'Downloaded/reviewed {len(paths)} raw frames')
if __name__=='__main__':main()
