"""Persist observed PixelLab frame URLs and assemble unedited nearest-neighbour review sheets."""
from pathlib import Path
from concurrent.futures import ThreadPoolExecutor
import json,re,urllib.request,hashlib
from PIL import Image,ImageDraw
BASE=Path(__file__).resolve().parent
def fetch(row):
    path,url=row
    path.parent.mkdir(parents=True,exist_ok=True)
    if not path.exists():
        with urllib.request.urlopen(url,timeout=90) as response:data=response.read()
        assert data[:8]==b'\x89PNG\r\n\x1a\n'
        path.write_bytes(data)
    return path
def main():
    tasks=[]
    records=json.loads((BASE/'requests/final-poll.json').read_text(encoding='utf-8'))
    for r in records:
        text=r['text'][0]['text']
        if 'status: completed' not in text:continue
        n=int(re.search(r'frames: (\d+)',text)[1]);url=re.search(r'download: (\S+)',text)[1]
        for i in range(n): tasks.append((BASE/r['id']/'raw'/r['motion']/f'{i:02}.png',url.split('?')[0]+f'?index={i}'))
    with ThreadPoolExecutor(max_workers=8) as pool:paths=list(pool.map(fetch,tasks))
    report=[]
    for actor in ['daren','miel']:
        for motion in ['Idle','Run','Attack','DodgeUp','DodgeBack','Hit']:
            files=sorted((BASE/actor/'raw'/motion).glob('*.png'))
            if not files:continue
            sheet=Image.new('RGB',(max(len(files),8)*140,160),'#D9DBC8');draw=ImageDraw.Draw(sheet)
            for i,p in enumerate(files):
                im=Image.open(p).convert('RGBA');pix=list(im.getdata());bounds=im.getbbox()
                report.append(dict(actor=actor,motion=motion,frame=i,size=list(im.size),bounds=bounds,colors=len({x[:3] for x in pix if x[3]}),alpha=sorted({x[3] for x in pix}),sha256=hashlib.sha256(p.read_bytes()).hexdigest()))
                sheet.paste(im.resize((128,128),Image.Resampling.NEAREST),(i*140,18),im.resize((128,128),Image.Resampling.NEAREST));draw.text((i*140+4,2),str(i),fill='black');draw.line((i*140,134,i*140+128,134),fill='#CC3355')
            dest=BASE/'reviews'/f'{actor}-{motion}.png';dest.parent.mkdir(exist_ok=True);sheet.save(dest)
    (BASE/'requests/frame-audit.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
    print(f'Downloaded {len(paths)} frames; generated contact sheets. Inspect reviews before approval.')
if __name__=='__main__':main()
