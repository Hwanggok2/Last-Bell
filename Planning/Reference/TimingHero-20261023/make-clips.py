from pathlib import Path
import subprocess, concurrent.futures
from extract_evidence import VIDEO, FFMPEG
ROOT=Path(__file__).resolve().parent
OUT=ROOT/'clips';OUT.mkdir(exist_ok=True)
def clip(spec):
    name,start,duration=spec
    subprocess.run([str(FFMPEG),'-y','-hide_banner','-loglevel','error','-ss',str(start),'-i',str(VIDEO),'-t',str(duration),'-vf','scale=1200:-2','-c:v','libx264','-preset','fast','-crf','21','-c:a','aac','-b:a','128k','-movflags','+faststart',str(OUT/(name+'.mp4'))],check=True)
    return name
with concurrent.futures.ThreadPoolExecutor(max_workers=3) as pool:
    for name in pool.map(clip,[('reference-normal',294,10),('reference-combo',605.5,8),('reference-upgrade',696.5,15)]): print(name)
