"""Read-only video sampling. Writes only review artifacts beneath this directory."""
from pathlib import Path
from concurrent.futures import ThreadPoolExecutor
import subprocess, hashlib, json, argparse
from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parent
VIDEO = Path(r'C:\Users\JW\Videos\LastBell\Screen_Recording_20261023_130328_TimingHero.mp4')
FFMPEG = Path(r'C:\Users\JW\AppData\Roaming\vrew\ffmpeg_gpl_vgx_v3\ffmpeg.exe')
FONT = ImageFont.truetype('C:/Windows/Fonts/malgun.ttf', 22)

def extract(t, group='overview', width=1200):
    out = ROOT/group/f'{t:09.3f}.jpg'
    out.parent.mkdir(parents=True, exist_ok=True)
    if not out.exists():
        subprocess.run([str(FFMPEG),'-v','error','-ss',str(t),'-i',str(VIDEO),'-frames:v','1','-vf',f'scale={width}:-2','-q:v','2','-y',str(out)],check=True,stdout=subprocess.DEVNULL,stderr=subprocess.PIPE)
    return out

def sheets(times,group,width=800,cols=3,rows=5):
    files=list(ThreadPoolExecutor(max_workers=4).map(lambda t:extract(t,group,max(width,1200)),times))
    for batch in range(0,len(files),cols*rows):
        subset=files[batch:batch+cols*rows]
        height=round(width*1080/2400)
        canvas=Image.new('RGB',(cols*width,rows*(height+32)),(17,24,32))
        d=ImageDraw.Draw(canvas)
        for i,p in enumerate(subset):
            x=(i%cols)*width;y=(i//cols)*(height+32)
            im=Image.open(p);im.thumbnail((width,height))
            canvas.paste(im,(x,y+32));t=times[batch+i]
            d.text((x+10,y+1),f'{int(t)//60:02}:{t%60:06.3f}  |  t={t:.3f}s',fill='white',font=FONT)
        canvas.save(ROOT/f'{group}-sheet-{batch//(cols*rows)+1:02}.jpg',quality=94)
    return files

if __name__=='__main__':
    a=argparse.ArgumentParser();a.add_argument('--group',default='overview');a.add_argument('--times');a.add_argument('--step',type=float,default=6);a.add_argument('--start',type=float,default=0);a.add_argument('--end',type=float,default=746.5);a.add_argument('--width',type=int,default=800);args=a.parse_args()
    times=[float(v) for v in args.times.split(',')] if args.times else [round(args.start+i*args.step,4) for i in range(int((args.end-args.start)/args.step)+1)]
    files=sheets(times,args.group,args.width)
    if args.group=='overview':
        (ROOT/'source.json').write_text(json.dumps({'source':str(VIDEO),'sha256':hashlib.sha256(VIDEO.read_bytes()).hexdigest(),'bytes':VIDEO.stat().st_size,'durationSeconds':746.6,'resolution':[2400,1080],'reportedAverageFps':29.56,'note':'Variable frame rate. Requested seek times label still frames; precise event measurements require original PTS. User-supplied footage, local review only.','samples':times},indent=2),encoding='utf-8')
    print(f'{args.group}: {len(files)} frames, contact sheets written')
